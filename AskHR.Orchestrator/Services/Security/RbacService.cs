using AskHR.Common.Dtos.Constants;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Services.Security;

public sealed class RbacService : IRbacService
{
    private readonly AgentDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly bool _mockAuthorizationEnabled;

    public RbacService(AgentDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _mockAuthorizationEnabled = _configuration.GetValue("Authorization:Mock:Enabled", false);
    }

    public async Task<AuthorizationContext> ResolveAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var roleIds = userId.HasValue
            ? await _dbContext.UserRoles
                .Where(x => x.UserId == userId.Value)
                .Select(x => x.RoleId)
                .ToListAsync(cancellationToken)
            : [new Guid(Constants.AnonymousRoleId)];

        if (roleIds.Count == 0)
        {
            return AuthorizationContext.Anonymous(await GetAnonymousAllowedTagsAsync(cancellationToken));
        }

        var allowedTags = await _dbContext.TagRoles
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.TagId.ToString())
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleNames = await _dbContext.Roles
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Name)
            .Where(x => x != string.Empty)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!userId.HasValue)
        {
            return AuthorizationContext.Anonymous(allowedTags);
        }

        var mockRoles = GetConfiguredList("Authorization:Mock:Roles");
        var roles = roleNames
            .Concat(mockRoles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId.Value, cancellationToken);

        var businessUnits = profile?.BusinessUnits ?? [];
        if (businessUnits.Count == 0) businessUnits = GetConfiguredList("Authorization:Mock:BusinessUnits");

        var countries = profile?.Countries ?? [];
        if (countries.Count == 0) countries = GetConfiguredList("Authorization:Mock:Countries");

        var legalEntities = profile?.LegalEntities ?? [];
        if (legalEntities.Count == 0) legalEntities = GetConfiguredList("Authorization:Mock:LegalEntities");

        var level = profile?.Level ?? GetConfiguredValue("Authorization:Mock:Level");
        var sensitivityLevel = profile?.SensitivityLevel ?? GetConfiguredValue("Authorization:Mock:SensitivityLevel") ?? "Internal";

        return new AuthorizationContext
        {
            UserId = userId.Value,
            IsAnonymous = false,
            Roles = roles,
            AllowedTags = allowedTags,
            BusinessUnits = businessUnits,
            Countries = countries,
            LegalEntities = legalEntities,
            Level = level,
            SensitivityLevel = sensitivityLevel
        };
    }

    private async Task<List<string>> GetAnonymousAllowedTagsAsync(CancellationToken cancellationToken)
    {
        var anonymousRoleId = new Guid(Constants.AnonymousRoleId);
        return await _dbContext.TagRoles
            .Where(x => x.RoleId == anonymousRoleId)
            .Select(x => x.TagId.ToString())
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private List<string> GetConfiguredList(string key)
    {
        // TODO: Replace this Development-only shim with persisted user profile/HRIS data.
        if (!_mockAuthorizationEnabled)
        {
            return [];
        }

        var section = _configuration.GetSection(key);
        if (section.Exists())
        {
            return section.Get<string[]>()?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        }

        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private string? GetConfiguredValue(string key)
    {
        return _mockAuthorizationEnabled ? _configuration[key] : null;
    }
}
