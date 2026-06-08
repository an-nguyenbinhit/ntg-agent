using System.Runtime.CompilerServices;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Answers;

public sealed class AskHrStreamService : IAskHrStreamService
{
    private readonly IPolicyAnswerService _answerService;
    private readonly ILogger<AskHrStreamService> _logger;

    public AskHrStreamService(IPolicyAnswerService answerService, ILogger<AskHrStreamService> logger)
    {
        _answerService = answerService ?? throw new ArgumentNullException(nameof(answerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<AskHrStreamEvent> StreamAnswerAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AskHrAnswerResponse? answer = null;
        AskHrStreamEvent? errorEvent = null;
        try
        {
            answer = await _answerService.AnswerAsync(request, authorization, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AskHR web answer stream failed for agent {AgentId}", request.AgentId);
            errorEvent = new AskHrStreamEvent(
                AskHrStreamEventType.Error,
                Content: "I could not complete this answer. Please try again or contact HR.",
                ErrorCode: "answer-stream-failed");
        }

        if (errorEvent is not null)
        {
            yield return errorEvent;
            yield break;
        }

        if (answer is null)
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(answer.AnswerText))
        {
            yield return new AskHrStreamEvent(AskHrStreamEventType.Token, Content: answer.AnswerText);
        }

        foreach (var citation in answer.Citations)
        {
            yield return new AskHrStreamEvent(AskHrStreamEventType.Citation, Citation: citation);
        }

        yield return new AskHrStreamEvent(AskHrStreamEventType.Done, Answer: answer);
    }
}
