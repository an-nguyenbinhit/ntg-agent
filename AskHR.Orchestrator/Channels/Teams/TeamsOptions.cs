namespace AskHR.Orchestrator.Channels.Teams;

public sealed class TeamsOptions
{
    public const string SectionName = "Teams";

    public Guid? DefaultAgentId { get; init; }

    public int PipelineTimeoutSeconds { get; init; } = 25;

    public string? BotFrameworkToken { get; init; }

    public Dictionary<string, string> UserMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

