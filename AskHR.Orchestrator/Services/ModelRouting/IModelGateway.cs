using AskHR.Common.Dtos.ModelRouting;
using Microsoft.Extensions.AI;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed record ModelCompletionRequest(
    ModelCapability Capability,
    Guid? AgentId,
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options = null,
    string? DataClass = null);

public sealed record ModelCompletionResponse(
    string Text,
    ModelRouteDto Route,
    ChatResponse? RawResponse = null);

public interface IModelGateway
{
    Task<ModelCompletionResponse> CompleteAsync(ModelCompletionRequest request, CancellationToken cancellationToken = default);
}
