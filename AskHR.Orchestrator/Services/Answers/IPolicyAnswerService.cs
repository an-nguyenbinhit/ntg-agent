using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Answers;

public interface IPolicyAnswerService
{
    Task<AskHrAnswerResponse> AnswerAsync(AskHrRequest request, AuthorizationContext authorization, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AskHrStreamEvent> StreamAnswerAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        CancellationToken cancellationToken = default);
}
