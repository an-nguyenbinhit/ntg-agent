namespace AskHR.Orchestrator.Models.Audit;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid AgentId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsAnonymous { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string MaskedText { get; set; } = string.Empty;
    public string TextHash { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? FallbackReason { get; set; }
    public int CitationCount { get; set; }
    public long? PromptTokens { get; set; }
    public long? CompletionTokens { get; set; }
    public long? TotalTokens { get; set; }
    public long LatencyMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
