namespace AskHR.Orchestrator.Channels.Teams;

public interface ITeamsActivityDeduplicator
{
    bool TryAccept(string? activityId);
}

