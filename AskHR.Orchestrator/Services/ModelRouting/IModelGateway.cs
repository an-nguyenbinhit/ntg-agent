using AskHR.Common.Dtos.ModelRouting;
using Microsoft.Extensions.AI;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed record ModelCompletionRequest(
    ModelCapability Capability,
    Guid? AgentId,
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options = null,
    string? DataClass = null,
    string? ProviderOverride = null,
    string? ModelOverride = null,
    string? RouteNameOverride = null);

public sealed record ModelCompletionResponse(
    string Text,
    ModelRouteDto Route,
    ChatResponse? RawResponse = null);

public sealed record ModelStreamResponse(
    string TextDelta,
    ModelRouteDto Route,
    ChatResponseUpdate? RawUpdate = null);

public interface IModelGateway
{
    Task<ModelCompletionResponse> CompleteAsync(ModelCompletionRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ModelStreamResponse> StreamCompleteAsync(ModelCompletionRequest request, CancellationToken cancellationToken = default);
}
