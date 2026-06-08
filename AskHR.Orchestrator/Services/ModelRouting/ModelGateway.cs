using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed class ModelGateway : IModelGateway
{
    private readonly IModelRouter _router;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<ModelGateway> _logger;

    public ModelGateway(IModelRouter router, IChatClientFactory chatClientFactory, ILogger<ModelGateway> logger)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModelCompletionResponse> CompleteAsync(ModelCompletionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = await _router.ResolveAsync(request.Capability, request.AgentId, request.DataClass, cancellationToken);
        var client = _chatClientFactory.Create(route);

        _logger.LogDebug(
            "Calling model route {RouteName} provider {Provider} model {Model} for {Capability}",
            route.RouteName,
            route.Provider,
            route.Model,
            request.Capability);

        var response = await client.GetResponseAsync(request.Messages, request.Options, cancellationToken);
        var text = ExtractText(response);

        return new ModelCompletionResponse(text, route.ToDto(), response);
    }

    public async IAsyncEnumerable<ModelStreamResponse> StreamCompleteAsync(
        ModelCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = await _router.ResolveAsync(request.Capability, request.AgentId, request.DataClass, cancellationToken);
        var client = _chatClientFactory.Create(route);
        var routeDto = route.ToDto();

        _logger.LogDebug(
            "Streaming model route {RouteName} provider {Provider} model {Model} for {Capability}",
            route.RouteName,
            route.Provider,
            route.Model,
            request.Capability);

        await foreach (var update in client.GetStreamingResponseAsync(request.Messages, request.Options, cancellationToken))
        {
            var text = ExtractText(update);
            if (!string.IsNullOrEmpty(text))
            {
                yield return new ModelStreamResponse(text, routeDto, update);
            }
        }
    }

    private static string ExtractText(ChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text.Trim();
        }

        var builder = new StringBuilder();
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents.OfType<TextContent>())
            {
                builder.Append(content.Text);
            }
        }

        return builder.ToString().Trim();
    }

    private static string ExtractText(ChatResponseUpdate update)
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            return update.Text;
        }

        var builder = new StringBuilder();
        foreach (var content in update.Contents.OfType<TextContent>())
        {
            builder.Append(content.Text);
        }

        return builder.ToString();
    }
}
