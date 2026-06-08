using AskHR.Common.Dtos.ModelRouting;

namespace AskHR.Orchestrator.Services.ModelRouting;

public interface IModelRouter
{
    Task<ResolvedModelRoute> ResolveAsync(ModelCapability capability, Guid? agentId = null, string? dataClass = null, CancellationToken cancellationToken = default);
}
