using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Services.Answers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class AskHrStreamServiceTests
{
    private Mock<IPolicyAnswerService> _answerService = null!;
    private AskHrStreamService _streamService = null!;

    [SetUp]
    public void Setup()
    {
        _answerService = new Mock<IPolicyAnswerService>();
        _streamService = new AskHrStreamService(_answerService.Object, NullLogger<AskHrStreamService>.Instance);
    }

    [Test]
    public async Task StreamAnswerAsync_WhenAnswerSucceeds_EmitsTokenCitationAndDone()
    {
        var request = new AskHrRequest(Guid.NewGuid(), "Annual leave?");
        var authorization = AuthorizationContext.Anonymous();
        var citation = new AnswerCitationDto(
            "doc-1",
            "leave-policy.md",
            "markdown",
            "leave-policy.md",
            null,
            "Employees receive annual leave.",
            0.91);
        var answer = new AskHrAnswerResponse(
            "Employees receive annual leave [1].",
            [citation],
            0.91,
            null,
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                "AzureOpenAI",
                "gpt-4.1-mini",
                "answer",
                "baseline-rag",
                1,
                null,
                42));

        _answerService
            .Setup(x => x.AnswerAsync(request, authorization, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

        var events = await CollectAsync(_streamService.StreamAnswerAsync(request, authorization));

        Assert.That(events.Select(x => x.Type), Is.EqualTo(new[]
        {
            AskHrStreamEventType.Token,
            AskHrStreamEventType.Citation,
            AskHrStreamEventType.Done
        }));
        Assert.That(events[0].Content, Is.EqualTo(answer.AnswerText));
        Assert.That(events[1].Citation, Is.SameAs(citation));
        Assert.That(events[2].Answer, Is.SameAs(answer));
    }

    [Test]
    public async Task StreamAnswerAsync_WhenAnswerFails_EmitsErrorEvent()
    {
        var request = new AskHrRequest(Guid.NewGuid(), "Annual leave?");
        var authorization = AuthorizationContext.Anonymous();
        _answerService
            .Setup(x => x.AnswerAsync(request, authorization, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var events = await CollectAsync(_streamService.StreamAnswerAsync(request, authorization));

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Type, Is.EqualTo(AskHrStreamEventType.Error));
        Assert.That(events[0].ErrorCode, Is.EqualTo("answer-stream-failed"));
    }

    private static async Task<List<AskHrStreamEvent>> CollectAsync(IAsyncEnumerable<AskHrStreamEvent> stream)
    {
        var events = new List<AskHrStreamEvent>();
        await foreach (var streamEvent in stream)
        {
            events.Add(streamEvent);
        }

        return events;
    }
}
