using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Channels.Teams;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Answers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AskHR.Orchestrator.Tests.Controllers;

[TestFixture]
public class TeamsGatewayControllerTests
{
    private static readonly Guid AgentId = Guid.NewGuid();
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private AgentDbContext _context = null!;
    private Mock<IPolicyAnswerService> _answerService = null!;
    private Mock<ITeamsResponseClient> _responseClient = null!;
    private Mock<ITeamsIdentityResolver> _identityResolver = null!;
    private Mock<ITeamsActivityDeduplicator> _deduplicator = null!;
    private TeamsGatewayController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);

        _answerService = new Mock<IPolicyAnswerService>();
        _responseClient = new Mock<ITeamsResponseClient>();
        _identityResolver = new Mock<ITeamsIdentityResolver>();
        _deduplicator = new Mock<ITeamsActivityDeduplicator>();

        _deduplicator.Setup(x => x.TryAccept(It.IsAny<string?>())).Returns(true);
        _identityResolver
            .Setup(x => x.ResolveAsync(It.IsAny<TeamsActivity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationContext.Anonymous());

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IPolicyAnswerService))).Returns(_answerService.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ITeamsResponseClient))).Returns(_responseClient.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ITeamsIdentityResolver))).Returns(_identityResolver.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        _controller = new TeamsGatewayController(
            _context,
            Options.Create(new TeamsOptions { DefaultAgentId = AgentId, PipelineTimeoutSeconds = 1 }),
            _deduplicator.Object,
            NullLogger<TeamsGatewayController>.Instance,
            scopeFactory.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task MessagesAsync_AcksImmediatelyAndProcessesNormalizedQuestionInBackground()
    {
        var completed = new TaskCompletionSource();
        AskHrRequest? capturedRequest = null;

        _answerService
            .Setup(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<AskHrRequest, AuthorizationContext, CancellationToken>((request, _, _) => capturedRequest = request)
            .ReturnsAsync(CreateAnswer("Leave policy is 12 days."));
        _responseClient
            .Setup(x => x.PostAnswerAsync(It.IsAny<TeamsActivity>(), It.IsAny<AskHrAnswerResponse>(), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .Returns(Task.CompletedTask);

        var result = await _controller.MessagesAsync(CreateActivity("<at>AskHR</at> what is the leave policy?"), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var response = (TeamsGatewayResponse)ok.Value!;
        Assert.That(response.Reason, Is.EqualTo("processing"));

        await completed.Task.WaitAsync(WaitTimeout);
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Question, Is.EqualTo("what is the leave policy?"));
        Assert.That(capturedRequest.Channel, Is.EqualTo("teams"));
        Assert.That(capturedRequest.Metadata!["teamsConversationType"], Is.EqualTo("personal"));
    }

    [Test]
    public async Task MessagesAsync_DuplicateActivity_DoesNotStartPipeline()
    {
        _deduplicator.Setup(x => x.TryAccept("activity-1")).Returns(false);

        var result = await _controller.MessagesAsync(CreateActivity("hello"), CancellationToken.None);

        var ok = (OkObjectResult)result.Result!;
        var response = (TeamsGatewayResponse)ok.Value!;
        Assert.That(response.Reason, Is.EqualTo("duplicate-activity"));
        _answerService.Verify(x => x.AnswerAsync(It.IsAny<AskHrRequest>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TeamsActivity CreateActivity(string text) => new()
    {
        Type = "message",
        Id = "activity-1",
        ServiceUrl = "https://smba.trafficmanager.net/amer/",
        Text = text,
        From = new TeamsChannelAccount { Id = "29:user", AadObjectId = "aad-123" },
        Conversation = new TeamsConversationAccount { Id = "conversation-1", ConversationType = "personal", TenantId = "tenant-1" }
    };

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

