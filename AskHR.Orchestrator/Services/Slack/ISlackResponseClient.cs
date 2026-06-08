using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Services.Slack;

public interface ISlackResponseClient
{
    Task PostAnswerAsync(string channel, string threadTs, AskHrAnswerResponse answer, CancellationToken cancellationToken = default);

    /// <summary>Posts a status message (e.g. loading or error notice) and returns its Slack message timestamp, or null if it could not be posted.</summary>
    Task<string?> PostStatusMessageAsync(string channel, string threadTs, string text, CancellationToken cancellationToken = default);

    /// <summary>Replaces the text of a previously posted message via chat.update.</summary>
    Task UpdateMessageAsync(string channel, string ts, string text, CancellationToken cancellationToken = default);
}
