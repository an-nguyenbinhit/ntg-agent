using AskHR.Common.Dtos.Answers;

namespace AskHR.Orchestrator.Services.Escalation;

public sealed class RuleBasedSeverityClassifier : ISeverityClassifier
{
    private static readonly string[] SensitiveP1Terms =
    [
        "harassment",
        "sexual harassment",
        "discrimination",
        "bullying",
        "retaliation",
        "violence",
        "threat",
        "self harm",
        "suicide",
        "mental health crisis",
        "quay roi",
        "quay rối",
        "phan biet doi xu",
        "phân biệt đối xử",
        "bao luc",
        "bạo lực",
        "tu tu",
        "tự tử"
    ];

    private static readonly string[] BenefitsP1Terms =
    [
        "benefit",
        "benefits",
        "insurance",
        "compensation",
        "salary",
        "payroll",
        "leave entitlement",
        "maternity",
        "paternity",
        "medical leave",
        "bao hiem",
        "bảo hiểm",
        "luong",
        "lương",
        "thai san",
        "thai sản",
        "nghi phep",
        "nghỉ phép"
    ];

    private static readonly string[] ProcessP2Terms =
    [
        "onboarding",
        "offboarding",
        "timesheet",
        "working time",
        "working hours",
        "procedure",
        "process",
        "approval",
        "quy trinh",
        "quy trình",
        "gio lam",
        "giờ làm",
        "cham cong",
        "chấm công"
    ];

    public SeverityClassification Classify(AskHrRequest request, IReadOnlyList<AnswerCitationDto>? citations = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var text = request.Question ?? string.Empty;
        if (ContainsAny(text, SensitiveP1Terms))
        {
            return new SeverityClassification(EscalationSeverity.P1, "Sensitive HR issue", "sensitive-topic", RequiresWarmHandoff: true);
        }

        if (ContainsAny(text, BenefitsP1Terms))
        {
            return new SeverityClassification(EscalationSeverity.P1, "Benefits/leave/pay", "policy-impacting-topic", RequiresWarmHandoff: false);
        }

        if (ContainsAny(text, ProcessP2Terms))
        {
            return new SeverityClassification(EscalationSeverity.P2, "HR process", "process-topic", RequiresWarmHandoff: false);
        }

        if (citations is { Count: 0 })
        {
            return new SeverityClassification(EscalationSeverity.P3, "Missing information", "no-grounding-citations", RequiresWarmHandoff: false);
        }

        return new SeverityClassification(EscalationSeverity.P3, "General HR policy", "general-topic", RequiresWarmHandoff: false);
    }

    private static bool ContainsAny(string value, IEnumerable<string> terms)
    {
        foreach (var term in terms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
