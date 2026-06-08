using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AskHR.Orchestrator.Services.Security;

public sealed class HttpContextIdentityResolver : IIdentityResolver
{
    public Task<Guid?> ResolveUserIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdValue, out var userId))
        {
            return Task.FromResult<Guid?>(userId);
        }

        return Task.FromResult<Guid?>(null);
    }
}
