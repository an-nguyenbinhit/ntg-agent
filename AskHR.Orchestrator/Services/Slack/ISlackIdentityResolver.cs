using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Slack;

public interface ISlackIdentityResolver
{
    Task<AuthorizationContext> ResolveAsync(string? slackUserId, CancellationToken cancellationToken = default);
}
