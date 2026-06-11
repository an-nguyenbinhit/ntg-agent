using System.Text.Json.Nodes;
using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Channels.Teams;

public interface ITeamsAdaptiveCardFormatter
{
    JsonObject BuildAnswerCard(AskHrAnswerResponse answer);
}

