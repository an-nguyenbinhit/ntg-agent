using AskHR.Common.Dtos.Knowledge;

namespace AskHR.Orchestrator.Services.Knowledge;

/// <summary>
/// Gateway over the remote Kernel Memory service's <c>/backend-info</c> endpoint.
/// Kept separate from <see cref="IKnowledgeService"/> because the backend probe is
/// infrastructure introspection over raw HTTP, whereas <see cref="IKnowledgeService"/>
/// is functional RAG access through the Kernel Memory SDK (which cannot reach custom
/// endpoints).
/// </summary>
public interface IKnowledgeBackendProbe
{
    Task<KnowledgeBackendInfoDto> GetBackendInfoAsync(CancellationToken cancellationToken = default);
}
