using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Services.Slack;

public interface ISlackResponseClient
{
    Task PostAnswerAsync(string channel, string threadTs, AskHrAnswerResponse answer, CancellationToken cancellationToken = default);
}
