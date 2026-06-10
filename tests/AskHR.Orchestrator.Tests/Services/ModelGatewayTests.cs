using AskHR.Common.Dtos.ModelRouting;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class ModelGatewayTests
{
    [Test]
    public async Task CompleteAsync_WithApprovedFallbackOverride_UsesFallbackCredentials()
    {
        var primary = new ResolvedModelRoute(
            ModelCapability.AnswerGeneration,
            "OpenAI",
            "gpt-4o-mini",
            "https://api.openai.com/v1",
            "openai-key",
            "primary",
            "hr-policy",
            [
                new ResolvedModelRoute(
                    ModelCapability.AnswerGeneration,
                    "AzureOpenAI",
                    "gpt-4o",
                    "https://azure.example.com",
                    "azure-key",
                    "azure-fallback",
                    "hr-policy",
                    [])
            ]);
        var router = new Mock<IModelRouter>();
        router.Setup(x => x.ResolveAsync(ModelCapability.AnswerGeneration, It.IsAny<Guid?>(), "hr-policy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(primary);

        ResolvedModelRoute? capturedRoute = null;
        var factory = new Mock<IChatClientFactory>();
        factory.Setup(x => x.Create(It.IsAny<ResolvedModelRoute>()))
            .Callback<ResolvedModelRoute>(route => capturedRoute = route)
            .Returns(new StaticChatClient("ok"));

        var gateway = new ModelGateway(router.Object, factory.Object, NullLogger<ModelGateway>.Instance);

        await gateway.CompleteAsync(new ModelCompletionRequest(
            ModelCapability.AnswerGeneration,
            Guid.NewGuid(),
            [new ChatMessage(ChatRole.User, "Question")],
            DataClass: "hr-policy",
            ProviderOverride: "AzureOpenAI",
            ModelOverride: "gpt-4o",
            RouteNameOverride: "skill:leave-policy"));

        Assert.That(capturedRoute, Is.Not.Null);
        Assert.That(capturedRoute!.Provider, Is.EqualTo("AzureOpenAI"));
        Assert.That(capturedRoute.Model, Is.EqualTo("gpt-4o"));
        Assert.That(capturedRoute.Endpoint, Is.EqualTo("https://azure.example.com"));
        Assert.That(capturedRoute.ApiKey, Is.EqualTo("azure-key"));
        Assert.That(capturedRoute.RouteName, Is.EqualTo("skill:leave-policy"));
    }

    [Test]
    public void CompleteAsync_WithUnapprovedOverride_ThrowsBeforeCreatingClient()
    {
        var route = new ResolvedModelRoute(
            ModelCapability.AnswerGeneration,
            "OpenAI",
            "gpt-4o-mini",
            "https://api.openai.com/v1",
            "openai-key",
            "primary",
            "hr-policy",
            []);
        var router = new Mock<IModelRouter>();
        router.Setup(x => x.ResolveAsync(ModelCapability.AnswerGeneration, It.IsAny<Guid?>(), "hr-policy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        var factory = new Mock<IChatClientFactory>();
        var gateway = new ModelGateway(router.Object, factory.Object, NullLogger<ModelGateway>.Instance);

        Assert.ThrowsAsync<InvalidOperationException>(() => gateway.CompleteAsync(new ModelCompletionRequest(
            ModelCapability.AnswerGeneration,
            Guid.NewGuid(),
            [new ChatMessage(ChatRole.User, "Question")],
            DataClass: "hr-policy",
            ProviderOverride: "AzureOpenAI",
            ModelOverride: "gpt-4o")));
        factory.Verify(x => x.Create(It.IsAny<ResolvedModelRoute>()), Times.Never);
    }

    private sealed class StaticChatClient : IChatClient
    {
        private readonly string _response;

        public StaticChatClient(string response)
        {
            _response = response;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response)));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, _response);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
