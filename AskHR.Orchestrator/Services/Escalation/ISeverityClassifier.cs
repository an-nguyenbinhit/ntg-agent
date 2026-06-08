using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Services.Escalation;

public enum EscalationSeverity
{
    P1 = 1,
    P2 = 2,
    P3 = 3
}

public sealed record SeverityClassification(
    EscalationSeverity Severity,
    string Topic,
    string Reason,
    bool RequiresWarmHandoff);

public interface ISeverityClassifier
{
    SeverityClassification Classify(AskHrRequest request, IReadOnlyList<AnswerCitationDto>? citations = null);
}
