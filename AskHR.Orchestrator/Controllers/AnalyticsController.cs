using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Orchestrator.Data;
using System.Linq;

namespace AskHR.Orchestrator.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly AgentDbContext _context;

    public AnalyticsController(AgentDbContext context)
    {
        _context = context;
    }

    [HttpGet("token-usage/summary")]
    public async Task<IActionResult> GetTokenUsageSummary(CancellationToken ct)
    {
        var last30Days = DateTime.UtcNow.AddDays(-30);
        
        var stats = await _context.TokenUsages
            .Where(t => t.CreatedAt >= last30Days)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalInputTokens = g.Sum(x => x.InputTokens ?? 0),
                TotalOutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                TotalTokens = g.Sum(x => x.TotalTokens ?? 0)
            })
            .OrderBy(s => s.Date)
            .ToListAsync(ct);

        var totalUsage = new
        {
            TotalInput = stats.Sum(x => x.TotalInputTokens),
            TotalOutput = stats.Sum(x => x.TotalOutputTokens),
            TotalOverall = stats.Sum(x => x.TotalTokens),
            DailyStats = stats
        };

        return Ok(totalUsage);
    }
}
