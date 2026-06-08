namespace AskHR.Orchestrator.Services.Slack;

public sealed class SlackOptions
{
    public string? BotToken { get; init; }

    public string? SigningSecret { get; init; }

    public bool EnableSignatureVerification { get; init; } = true;

    public int EventDeduplicationMinutes { get; init; } = 10;

    public Guid? DefaultAgentId { get; init; }
}
