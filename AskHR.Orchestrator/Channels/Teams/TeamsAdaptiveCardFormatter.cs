using System.Text.Json.Nodes;
using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Channels.Teams;

public sealed class TeamsAdaptiveCardFormatter : ITeamsAdaptiveCardFormatter
{
    public JsonObject BuildAnswerCard(AskHrAnswerResponse answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var body = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = answer.AnswerText,
                ["wrap"] = true
            }
        };

        if (answer.Citations.Count > 0)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = BuildCitationText(answer.Citations),
                ["wrap"] = true,
                ["spacing"] = "Medium",
                ["isSubtle"] = true
            });
        }

        return new JsonObject
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = "1.5",
            ["body"] = body,
            ["actions"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Action.Submit",
                    ["title"] = "Helpful",
                    ["data"] = new JsonObject
                    {
                        ["action"] = "feedback",
                        ["value"] = "positive",
                        ["messageId"] = answer.MessageId?.ToString()
                    }
                },
                new JsonObject
                {
                    ["type"] = "Action.Submit",
                    ["title"] = "Not helpful",
                    ["data"] = new JsonObject
                    {
                        ["action"] = "feedback",
                        ["value"] = "negative",
                        ["messageId"] = answer.MessageId?.ToString()
                    }
                }
            }
        };
    }

    private static string BuildCitationText(IReadOnlyList<AnswerCitationDto> citations)
    {
        var names = citations
            .Select(x => string.IsNullOrWhiteSpace(x.SourceUrl) ? x.DocumentName : $"{x.DocumentName} ({x.SourceUrl})")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3);

        return "Sources: " + string.Join("; ", names);
    }
}

