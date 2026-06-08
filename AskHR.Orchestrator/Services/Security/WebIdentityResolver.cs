using System.Security.Claims;
using AskHR.Orchestrator.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Services.Security;

public sealed class WebIdentityResolver : IIdentityResolver
{
    private static readonly string[] GuidClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "oid",
        "sub"
    ];

    private static readonly string[] EmailClaimTypes =
    [
        ClaimTypes.Email,
        "email",
        "preferred_username",
        "upn"
    ];

    private readonly AgentDbContext _dbContext;

    public WebIdentityResolver(AgentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Guid?> ResolveUserIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (var claimType in GuidClaimTypes)
        {
            var value = httpContext.User.FindFirstValue(claimType);
            if (Guid.TryParse(value, out var userId))
            {
                return userId;
            }
        }

        var email = EmailClaimTypes
            .Select(httpContext.User.FindFirstValue)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        // SQL Server default collation is case-insensitive; keep the predicate provider-friendly.
        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Email == email)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
