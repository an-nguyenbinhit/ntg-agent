using System.Text.Json;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Orchestrator.Channels.Teams;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class TeamsAdaptiveCardFormatterTests
{
    [Test]
    public void BuildAnswerCard_IncludesAnswerCitationsAndFeedbackActions()
    {
        var formatter = new TeamsAdaptiveCardFormatter();

        var card = formatter.BuildAnswerCard(new AskHrAnswerResponse(
            "Leave policy is 12 days.",
            [new AnswerCitationDto("doc-1", "Leave Handbook", "file", null, null, "snippet", 0.9)],
            0.95,
            null,
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                "AzureOpenAI",
                "gpt-4.1-mini",
                "answer",
                "baseline-rag",
                0,
                null,
                10),
            MessageId: Guid.Parse("11111111-1111-1111-1111-111111111111")));

        var json = card.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

        Assert.That(json, Does.Contain("\"type\":\"AdaptiveCard\""));
        Assert.That(json, Does.Contain("Leave policy is 12 days."));
        Assert.That(json, Does.Contain("Leave Handbook"));
        Assert.That(json, Does.Contain("\"value\":\"positive\""));
        Assert.That(json, Does.Contain("\"value\":\"negative\""));
        Assert.That(json, Does.Contain("11111111-1111-1111-1111-111111111111"));
    }
}

