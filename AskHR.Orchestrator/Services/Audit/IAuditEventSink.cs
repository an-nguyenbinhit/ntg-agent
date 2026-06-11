using AskHR.Common.Dtos.Audit;

namespace AskHR.Orchestrator.Services.Audit;

public interface IAuditEventSink
{
    Task WriteAsync(AuditEventDto auditEvent, CancellationToken cancellationToken = default);
    Task WriteFeedbackAsync(FeedbackEventDto feedbackEvent, CancellationToken cancellationToken = default);
}
