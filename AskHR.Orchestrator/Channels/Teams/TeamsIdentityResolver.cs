using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Channels.Teams;

public sealed class TeamsIdentityResolver : ITeamsIdentityResolver
{
    private readonly AgentDbContext _dbContext;
    private readonly IRbacService _rbacService;
    private readonly IOptions<TeamsOptions> _options;

    public TeamsIdentityResolver(AgentDbContext dbContext, IRbacService rbacService, IOptions<TeamsOptions> options)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AuthorizationContext> ResolveAsync(TeamsActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var aadObjectId = FirstNonEmpty(
            activity.From?.AadObjectId,
            activity.ChannelData?.From?.AadObjectId);

        if (!string.IsNullOrWhiteSpace(aadObjectId))
        {
            var mappedUserId = await ResolveMappedUserIdAsync(aadObjectId, cancellationToken);
            if (mappedUserId.HasValue)
            {
                return await _rbacService.ResolveAsync(mappedUserId.Value, cancellationToken);
            }
        }

        var userPrincipalName = FirstNonEmpty(
            activity.From?.UserPrincipalName,
            activity.ChannelData?.From?.UserPrincipalName);

        if (!string.IsNullOrWhiteSpace(userPrincipalName))
        {
            var userId = await ResolveUserIdByEmailAsync(userPrincipalName, cancellationToken);
            if (userId.HasValue)
            {
                return await _rbacService.ResolveAsync(userId.Value, cancellationToken);
            }
        }

        return AuthorizationContext.Anonymous();
    }

    private async Task<Guid?> ResolveMappedUserIdAsync(string aadObjectId, CancellationToken cancellationToken)
    {
        var trimmedAadObjectId = aadObjectId.Trim();
        if (_options.Value.UserMappings.TryGetValue(trimmedAadObjectId, out var mappedValue) &&
            !string.IsNullOrWhiteSpace(mappedValue))
        {
            if (Guid.TryParse(mappedValue, out var mappedGuid))
            {
                return await _dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Id == mappedGuid)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await ResolveUserIdByEmailAsync(mappedValue, cancellationToken);
        }

        if (Guid.TryParse(trimmedAadObjectId, out var aadGuid))
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == aadGuid)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<Guid?> ResolveUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Email == normalizedEmail)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
}

