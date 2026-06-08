using AskHR.Common.Dtos.Audit;

namespace AskHR.Orchestrator.Services.Audit;

public sealed class LoggingAuditEventSink : IAuditEventSink
{
    private readonly ILogger<LoggingAuditEventSink> _logger;

    public LoggingAuditEventSink(ILogger<LoggingAuditEventSink> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task WriteAsync(AuditEventDto auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        _logger.LogInformation(
            "AuditEvent {EventType} agent:{AgentId} user:{UserId} anonymous:{IsAnonymous} channel:{Channel} hash:{TextHash} provider:{Provider} model:{Model} fallback:{FallbackReason} citations:{CitationCount}",
            auditEvent.EventType,
            auditEvent.AgentId,
            auditEvent.UserId,
            auditEvent.IsAnonymous,
            auditEvent.Channel,
            auditEvent.TextHash,
            auditEvent.Provider,
            auditEvent.Model,
            auditEvent.FallbackReason,
            auditEvent.CitationCount);

        return Task.CompletedTask;
    }
}
