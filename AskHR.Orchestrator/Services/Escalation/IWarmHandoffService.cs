using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Escalation;

public sealed record WarmHandoffResult(
    string HandoffId,
    string UserMessage,
    IReadOnlyList<string> MaskedConversationContext);

public interface IWarmHandoffService
{
    Task<WarmHandoffResult> CreateAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        SeverityClassification classification,
        CancellationToken cancellationToken = default);
}
