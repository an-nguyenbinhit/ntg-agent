using AskHR.Orchestrator.Services.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskHR.Orchestrator.Controllers;

[Route("api/migration")]
[ApiController]
[Authorize(Roles = "Admin")]
public sealed class MigrationController : ControllerBase
{
    private readonly IReingestTool _reingestTool;

    public MigrationController(IReingestTool reingestTool)
    {
        _reingestTool = reingestTool ?? throw new ArgumentNullException(nameof(reingestTool));
    }

    [HttpPost("reingest")]
    public async Task<ActionResult<ReingestMigrationSummary>> ReingestAsync([FromBody] ReingestMigrationRequest? request, CancellationToken cancellationToken)
    {
        var summary = await _reingestTool.RunAsync(request ?? new ReingestMigrationRequest(), cancellationToken);
        return Ok(summary);
    }
}
