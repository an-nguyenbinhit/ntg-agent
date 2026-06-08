using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Security;

public interface IRbacService
{
    Task<AuthorizationContext> ResolveAsync(Guid? userId, CancellationToken cancellationToken = default);
}
