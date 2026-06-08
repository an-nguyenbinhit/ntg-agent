namespace AskHR.Orchestrator.Services.Slack;

public interface ISlackEventDeduplicator
{
    bool TryAccept(string? eventId);
}
