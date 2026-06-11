namespace AskHR.Common.Dtos.Audit;

public sealed record AuditEventDto(
    string EventType,
    Guid AgentId,
    Guid? UserId,
    bool IsAnonymous,
    string Channel,
    string MaskedText,
    string TextHash,
    string? Provider,
    string? Model,
    string? FallbackReason,
    int CitationCount,
    long? PromptTokens = null,
    long? CompletionTokens = null,
    long? TotalTokens = null,
    long LatencyMs = 0,
    DateTimeOffset CreatedAt = default);
