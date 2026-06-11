using AskHR.Common.Dtos.Audit;
using AskHR.Orchestrator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class MonitoringController : ControllerBase
{
    private readonly AgentDbContext _dbContext;

    public MonitoringController(AgentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("audit-summary")]
    public async Task<ActionResult<AuditMonitoringDto>> GetAuditSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var query = _dbContext.AuditEvents.AsNoTracking();

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        var totalEvents = await query.CountAsync();
        var totalPromptTokens = await query.SumAsync(e => e.PromptTokens ?? 0);
        var totalCompletionTokens = await query.SumAsync(e => e.CompletionTokens ?? 0);
        var totalTokens = await query.SumAsync(e => e.TotalTokens ?? 0);
        var averageLatencyMs = totalEvents == 0 ? 0 : await query.AverageAsync(e => e.LatencyMs);

        var tokensByModel = await query
            .Where(e => e.Model != null)
            .GroupBy(e => e.Model!)
            .Select(g => new { Model = g.Key, Tokens = g.Sum(e => e.TotalTokens ?? 0) })
            .OrderByDescending(x => x.Tokens)
            .Take(10)
            .ToDictionaryAsync(x => x.Model, x => x.Tokens);

        var averageLatencyByModel = await query
            .Where(e => e.Model != null)
            .GroupBy(e => e.Model!)
            .Select(g => new { Model = g.Key, Latency = g.Average(e => e.LatencyMs) })
            .OrderByDescending(x => x.Latency)
            .Take(10)
            .ToDictionaryAsync(x => x.Model, x => x.Latency);

        var eventsByChannel = await query
            .GroupBy(e => e.Channel)
            .Select(g => new { Channel = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Channel, x => x.Count);

        var eventsByType = await query
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.EventType, x => x.Count);

        return Ok(new AuditMonitoringDto(
            totalEvents,
            totalPromptTokens,
            totalCompletionTokens,
            totalTokens,
            averageLatencyMs,
            tokensByModel,
            averageLatencyByModel,
            eventsByChannel,
            eventsByType));
    }
}
