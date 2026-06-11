using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Channels.Teams;

public interface ITeamsIdentityResolver
{
    Task<AuthorizationContext> ResolveAsync(TeamsActivity activity, CancellationToken cancellationToken = default);
}

