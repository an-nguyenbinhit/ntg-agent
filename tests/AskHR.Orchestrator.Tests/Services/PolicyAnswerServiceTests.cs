using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class PolicyAnswerServiceTests
{
    private Mock<IKnowledgeService> _knowledgeService = null!;
    private Mock<IModelGateway> _modelGateway = null!;
    private Mock<IAuditEventSink> _auditSink = null!;
    private PolicyAnswerService _service = null!;

    [SetUp]
    public void Setup()
    {
        _knowledgeService = new Mock<IKnowledgeService>();
        _modelGateway = new Mock<IModelGateway>();
        _auditSink = new Mock<IAuditEventSink>();
        _service = new PolicyAnswerService(
            _knowledgeService.Object,
            _modelGateway.Object,
            _auditSink.Object,
            new AuditTextProtector(),
            Options.Create(new AnswerPipelineOptions { MinRelevance = 0.1 }),
            NullLogger<PolicyAnswerService>.Instance);
    }

    [Test]
    public async Task AnswerAsync_WhenNoCitations_ReturnsFallbackAndDoesNotCallModel()
    {
        var request = new AskHrRequest(Guid.NewGuid(), "Out of scope?");
        _knowledgeService
            .Setup(x => x.SearchAsync(request.Question, request.AgentId, It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult());

        var response = await _service.AnswerAsync(request, AuthorizationContext.Anonymous());

        Assert.That(response.FallbackReason, Is.EqualTo("no-grounding-citations"));
        Assert.That(response.Citations, Is.Empty);
        _modelGateway.Verify(x => x.CompleteAsync(It.IsAny<ModelCompletionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditSink.Verify(x => x.WriteAsync(It.Is<AuditEventDto>(e => e.FallbackReason == "no-grounding-citations"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AnswerAsync_WithGroundingCitations_CallsAnswerGenerationCapability()
    {
        var request = new AskHrRequest(Guid.NewGuid(), "Annual leave policy?");
        _knowledgeService
            .Setup(x => x.SearchAsync(request.Question, request.AgentId, It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSearchResult());
        _modelGateway
            .Setup(x => x.CompleteAsync(It.IsAny<ModelCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelCompletionResponse(
                "Employees have annual leave according to the cited policy [1].",
                new ModelRouteDto(ModelCapability.AnswerGeneration, "AzureOpenAI", "gpt-4.1-mini", RouteName: "answer")));

        var response = await _service.AnswerAsync(request, AuthorizationContext.Anonymous());

        Assert.That(response.FallbackReason, Is.Null);
        Assert.That(response.Citations, Has.Count.EqualTo(1));
        Assert.That(response.AnswerText, Does.Contain("[1]"));
        _modelGateway.Verify(x => x.CompleteAsync(
            It.Is<ModelCompletionRequest>(r => r.Capability == ModelCapability.AnswerGeneration && r.AgentId == request.AgentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SearchResult BuildSearchResult()
    {
        var tags = new TagCollection
        {
            { "documentName", ["leave-policy.md"] },
            { "sourceType", ["text"] },
            { "sourcePath", ["leave-policy.md"] }
        };

        return new SearchResult
        {
            Results =
            [
                new Citation
                {
                    DocumentId = "doc-1",
                    SourceName = "leave-policy.md",
                    Partitions =
                    [
                        new Citation.Partition
                        {
                            Text = "Employees receive annual leave according to tenure.",
                            Relevance = 0.92F,
                            Tags = tags
                        }
                    ]
                }
            ]
        };
    }
}
