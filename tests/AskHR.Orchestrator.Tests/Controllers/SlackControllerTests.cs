using System.Text;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Common.Dtos.Slack;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AskHR.Orchestrator.Tests.Controllers;

[TestFixture]
public class SlackControllerTests
{
    private static readonly Guid AgentId = Guid.NewGuid();
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private AgentDbContext _context = null!;
    private Mock<IPolicyAnswerService> _answerService = null!;
    private Mock<ISlackResponseClient> _responseClient = null!;
    private Mock<ISlackRequestVerifier> _requestVerifier = null!;
    private Mock<ISlackEventDeduplicator> _eventDeduplicator = null!;
    private Mock<ISlackIdentityResolver> _identityResolver = null!;
    private SlackController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);

        _answerService = new Mock<IPolicyAnswerService>();
        _responseClient = new Mock<ISlackResponseClient>();
        _requestVerifier = new Mock<ISlackRequestVerifier>();
        _eventDeduplicator = new Mock<ISlackEventDeduplicator>();
        _identityResolver = new Mock<ISlackIdentityResolver>();

        _requestVerifier.Setup(x => x.Verify(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);
        _eventDeduplicator.Setup(x => x.TryAccept(It.IsAny<string?>())).Returns(true);
        _identityResolver
            .Setup(x => x.ResolveAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationContext.Anonymous());

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IPolicyAnswerService))).Returns(_answerService.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ISlackResponseClient))).Returns(_responseClient.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ISlackIdentityResolver))).Returns(_identityResolver.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        _controller = new SlackController(
            _answerService.Object,
            _responseClient.Object,
            _requestVerifier.Object,
            _eventDeduplicator.Object,
            _identityResolver.Object,
            _context,
            Options.Create(new SlackOptions { DefaultAgentId = AgentId, PipelineTimeoutSeconds = 1 }),
            NullLogger<SlackController>.Instance,
            scopeFactory.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task EventsAsync_AcksImmediatelyWithoutWaitingForThePipeline()
    {
        SetRequestBody("U1", "<@BOT> what is the leave policy?");
        var answerStarted = new TaskCompletionSource();
        var releaseAnswer = new TaskCompletionSource();
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1700.0002");
        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (AskHrRequest _, AuthorizationContext _, CancellationToken _) =>
            {
                answerStarted.TrySetResult();
                await releaseAnswer.Task;
                return CreateAnswer("Leave policy is 12 days per year.");
            });

        var result = await _controller.EventsAsync(CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var response = ok.Value as SlackGatewayResponse;
        Assert.That(response!.AnswerText, Is.Null);
        Assert.That(response.Reason, Is.EqualTo("processing"));

        // Cleanly let the detached background task finish so it doesn't leak across tests.
        releaseAnswer.TrySetResult();
        await answerStarted.Task.WaitAsync(WaitTimeout);
    }

    [Test]
    public async Task BackgroundPipeline_WhenItSucceeds_ShowsLoadingThenReplacesItWithFinalAnswer()
    {
        SetRequestBody("U1", "<@BOT> what is the leave policy?");
        var completed = new TaskCompletionSource();
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1700.0002");
        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAnswer("Leave policy is 12 days per year."));
        _responseClient
            .Setup(x => x.UpdateMessageAsync("C1", "1700.0002", "Leave policy is 12 days per year.", It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .Returns(Task.CompletedTask);

        await _controller.EventsAsync(CancellationToken.None);
        await completed.Task.WaitAsync(WaitTimeout);

        _responseClient.Verify(x => x.PostStatusMessageAsync("C1", "1700.0001", It.Is<string>(s => s.Contains("xử lý")), It.IsAny<CancellationToken>()), Times.Once);
        _responseClient.Verify(x => x.UpdateMessageAsync("C1", "1700.0002", "Leave policy is 12 days per year.", It.IsAny<CancellationToken>()), Times.Once);
        _responseClient.Verify(x => x.PostAnswerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AskHrAnswerResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task BackgroundPipeline_WhenAnswerServiceThrows_ReplacesLoadingMessageWithErrorText()
    {
        SetRequestBody("U1", "<@BOT> what is the leave policy?");
        var completed = new TaskCompletionSource();
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1700.0002");
        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _responseClient
            .Setup(x => x.UpdateMessageAsync("C1", "1700.0002", It.Is<string>(s => s.Contains("lỗi")), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .Returns(Task.CompletedTask);

        await _controller.EventsAsync(CancellationToken.None);
        await completed.Task.WaitAsync(WaitTimeout);

        _responseClient.Verify(x => x.UpdateMessageAsync("C1", "1700.0002", It.Is<string>(s => s.Contains("lỗi")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task BackgroundPipeline_WhenItExceedsThePipelineTimeout_ReplacesLoadingMessageWithTimeoutText()
    {
        SetRequestBody("U1", "<@BOT> what is the leave policy?");
        var completed = new TaskCompletionSource();
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1700.0002");
        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (AskHrRequest _, AuthorizationContext _, CancellationToken token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return CreateAnswer("too late");
            });
        _responseClient
            .Setup(x => x.UpdateMessageAsync("C1", "1700.0002", It.Is<string>(s => s.Contains("nhiều thời gian hơn")), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .Returns(Task.CompletedTask);

        await _controller.EventsAsync(CancellationToken.None);
        await completed.Task.WaitAsync(WaitTimeout);

        _responseClient.Verify(x => x.UpdateMessageAsync("C1", "1700.0002", It.Is<string>(s => s.Contains("nhiều thời gian hơn")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task BackgroundPipeline_WhenLoadingMessageCouldNotBePosted_PostsErrorAsNewStatusMessage()
    {
        SetRequestBody("U1", "<@BOT> what is the leave policy?");
        var completed = new TaskCompletionSource();
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.Is<string>(s => s.Contains("xử lý")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _responseClient
            .Setup(x => x.PostStatusMessageAsync("C1", "1700.0001", It.Is<string>(s => s.Contains("lỗi")), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .ReturnsAsync((string?)null);
        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await _controller.EventsAsync(CancellationToken.None);
        await completed.Task.WaitAsync(WaitTimeout);

        _responseClient.Verify(x => x.PostStatusMessageAsync("C1", "1700.0001", It.Is<string>(s => s.Contains("lỗi")), It.IsAny<CancellationToken>()), Times.Once);
        _responseClient.Verify(x => x.UpdateMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetRequestBody(string slackUserId, string text)
    {
        var payload = $$"""
        {
            "type": "event_callback",
            "event_id": "Ev123",
            "event": {
                "type": "app_mention",
                "user": "{{slackUserId}}",
                "text": "{{text}}",
                "channel": "C1",
                "ts": "1700.0001"
            }
        }
        """;

        var httpContext = _controller.ControllerContext.HttpContext;
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
    }

    private static AskHrAnswerResponse CreateAnswer(string text) => new(
        text,
        [],
        0.9,
        null,
        new AnswerAuditMetadataDto(
            ModelCapability.AnswerGeneration,
            "AzureOpenAI",
            "gpt-4.1-mini",
            "answer",
            "baseline-rag",
            0,
            null,
            10));
}
