using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.TokenUsage;
using AskHR.Orchestrator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FeedbackAdminController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Open",
        "Triaged",
        "Resolved",
        "Ignored"
    };

    private static readonly HashSet<string> AllowedSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        "P1",
        "P2",
        "P3"
    };

    private readonly AgentDbContext _dbContext;

    public FeedbackAdminController(AgentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("events")]
    public async Task<ActionResult<FeedbackEventQueryResult>> GetEvents(
        [FromQuery] string? severity = null,
        [FromQuery] string? status = null,
        [FromQuery] string? rating = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1)
            return BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100");

        var baseQuery = _dbContext.FeedbackEvents.AsNoTracking();

        if (from.HasValue)
            baseQuery = baseQuery.Where(f => f.CreatedAt >= from.Value);

        if (to.HasValue)
            baseQuery = baseQuery.Where(f => f.CreatedAt <= to.Value);

        var countQuery = baseQuery;
        var filteredQuery = ApplyFilters(baseQuery, severity, status, rating);

        var totalCount = await filteredQuery.CountAsync();
        var events = await filteredQuery
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackEventAdminDto(
                f.Id,
                f.MessageId,
                f.UserId,
                f.IsAnonymous,
                f.Rating,
                f.CommentMasked,
                f.Topic,
                f.SeverityCandidate,
                f.Status,
                f.CreatedAt))
            .ToListAsync();

        var result = new FeedbackEventQueryResult(
            new PagedResult<FeedbackEventAdminDto>(
                events,
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling(totalCount / (double)pageSize)),
            await CountBySeverityAsync(countQuery),
            await CountByStatusAsync(countQuery),
            await CountByRatingAsync(countQuery));

        return Ok(result);
    }

    [HttpPut("events/{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] FeedbackEventUpdateDto dto)
    {
        if (!AllowedStatuses.Contains(dto.Status))
            return BadRequest("Status must be Open, Triaged, Resolved, or Ignored");

        if (!string.IsNullOrWhiteSpace(dto.SeverityCandidate) && !AllowedSeverities.Contains(dto.SeverityCandidate))
            return BadRequest("SeverityCandidate must be P1, P2, or P3");

        var feedbackEvent = await _dbContext.FeedbackEvents.FindAsync(id);

        if (feedbackEvent is null)
            return NotFound();

        feedbackEvent.Status = dto.Status;
        feedbackEvent.SeverityCandidate = string.IsNullOrWhiteSpace(dto.SeverityCandidate)
            ? null
            : dto.SeverityCandidate.ToUpperInvariant();

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static IQueryable<Models.Audit.FeedbackEvent> ApplyFilters(
        IQueryable<Models.Audit.FeedbackEvent> query,
        string? severity,
        string? status,
        string? rating)
    {
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(f => f.SeverityCandidate == severity);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        if (!string.IsNullOrWhiteSpace(rating))
            query = query.Where(f => f.Rating == rating);

        return query;
    }

    private static async Task<Dictionary<string, int>> CountBySeverityAsync(IQueryable<Models.Audit.FeedbackEvent> query)
        => await query
            .GroupBy(f => f.SeverityCandidate ?? "Unclassified")
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    private static async Task<Dictionary<string, int>> CountByStatusAsync(IQueryable<Models.Audit.FeedbackEvent> query)
        => await query
            .GroupBy(f => f.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    private static async Task<Dictionary<string, int>> CountByRatingAsync(IQueryable<Models.Audit.FeedbackEvent> query)
        => await query
            .GroupBy(f => f.Rating)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
}
