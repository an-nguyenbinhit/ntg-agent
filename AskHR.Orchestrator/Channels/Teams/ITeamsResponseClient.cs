using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Channels.Teams;

public interface ITeamsResponseClient
{
    Task<string?> PostStatusMessageAsync(TeamsActivity sourceActivity, string text, CancellationToken cancellationToken = default);

    Task PostAnswerAsync(TeamsActivity sourceActivity, AskHrAnswerResponse answer, CancellationToken cancellationToken = default);
}

