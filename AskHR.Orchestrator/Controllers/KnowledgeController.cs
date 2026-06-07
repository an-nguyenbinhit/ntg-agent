using AskHR.Common.Dtos.Knowledge;
using AskHR.Orchestrator.Services.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskHR.Orchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeBackendProbe _backendProbe;

    public KnowledgeController(IKnowledgeBackendProbe backendProbe)
    {
        _backendProbe = backendProbe ?? throw new ArgumentNullException(nameof(backendProbe));
    }

    /// <summary>
    /// Returns the active knowledge (RAG) backend — vector store, embedding and text
    /// models currently registered in the Kernel Memory service. Read-only.
    /// </summary>
    [HttpGet("backend-info")]
    public async Task<ActionResult<KnowledgeBackendInfoDto>> GetBackendInfo(CancellationToken cancellationToken)
    {
        var info = await _backendProbe.GetBackendInfoAsync(cancellationToken);
        return Ok(info);
    }
}
