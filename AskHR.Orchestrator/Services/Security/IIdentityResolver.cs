using Microsoft.AspNetCore.Http;

namespace AskHR.Orchestrator.Services.Security;

public interface IIdentityResolver
{
    Task<Guid?> ResolveUserIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
