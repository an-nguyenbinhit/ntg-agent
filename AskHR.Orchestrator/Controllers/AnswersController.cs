using AskHR.Common.Dtos.Answers;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskHR.Orchestrator.Controllers;

[Route("api/answers")]
[ApiController]
public sealed class AnswersController : ControllerBase
{
    private readonly IPolicyAnswerService _answerService;
    private readonly IIdentityResolver _identityResolver;
    private readonly IRbacService _rbacService;

    public AnswersController(IPolicyAnswerService answerService, IIdentityResolver identityResolver, IRbacService rbacService)
    {
        _answerService = answerService ?? throw new ArgumentNullException(nameof(answerService));
        _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AskHrAnswerResponse>> AnswerAsync([FromBody] AskHrRequest request, CancellationToken cancellationToken)
    {
        var userId = await _identityResolver.ResolveUserIdAsync(HttpContext, cancellationToken);
        var authorization = await _rbacService.ResolveAsync(userId, cancellationToken);
        var answer = await _answerService.AnswerAsync(request with { Channel = string.IsNullOrWhiteSpace(request.Channel) ? "api" : request.Channel }, authorization, cancellationToken);
        return Ok(answer);
    }
}
