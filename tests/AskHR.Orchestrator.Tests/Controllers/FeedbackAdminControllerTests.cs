using AskHR.Common.Dtos.Audit;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Audit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Tests.Controllers;

[TestFixture]
public class FeedbackAdminControllerTests
{
    private AgentDbContext _context = null!;
    private FeedbackAdminController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AgentDbContext(options);
        _controller = new FeedbackAdminController(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetEvents_WithSeverityFilter_ReturnsPagedEventsAndCounts()
    {
        var p1Event = new FeedbackEvent
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Rating = "Negative",
            SeverityCandidate = "P1",
            Status = "Open",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var p3Event = new FeedbackEvent
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Rating = "Positive",
            SeverityCandidate = "P3",
            Status = "Resolved",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.FeedbackEvents.AddRange(p1Event, p3Event);
        await _context.SaveChangesAsync();

        var result = await _controller.GetEvents(severity: "P1");

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var payload = (FeedbackEventQueryResult)okResult.Value!;

        Assert.That(payload.Events.TotalCount, Is.EqualTo(1));
        Assert.That(payload.Events.Items.Single().Id, Is.EqualTo(p1Event.Id));
        Assert.That(payload.CountBySeverity["P1"], Is.EqualTo(1));
        Assert.That(payload.CountBySeverity["P3"], Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateEvent_WithValidStatusAndSeverity_UpdatesEvent()
    {
        var feedbackEvent = new FeedbackEvent
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Rating = "Negative",
            Status = "Open",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.FeedbackEvents.Add(feedbackEvent);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateEvent(
            feedbackEvent.Id,
            new FeedbackEventUpdateDto("p2", "Triaged"));

        Assert.That(result, Is.TypeOf<NoContentResult>());

        var updated = await _context.FeedbackEvents.FindAsync(feedbackEvent.Id);
        Assert.That(updated!.SeverityCandidate, Is.EqualTo("P2"));
        Assert.That(updated.Status, Is.EqualTo("Triaged"));
    }

    [Test]
    public async Task UpdateEvent_WithInvalidStatus_ReturnsBadRequest()
    {
        var feedbackEvent = new FeedbackEvent
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Rating = "Negative",
            Status = "Open",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.FeedbackEvents.Add(feedbackEvent);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateEvent(
            feedbackEvent.Id,
            new FeedbackEventUpdateDto("P2", "Closed"));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
}
