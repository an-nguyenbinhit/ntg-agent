using System.Threading.Channels;
using AskHR.Common.Dtos.Audit;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Audit;

namespace AskHR.Orchestrator.Services.Audit;

public sealed class QueuedDatabaseAuditEventSink : BackgroundService, IAuditEventSink
{
    private readonly Channel<AuditEventDto> _queue = Channel.CreateUnbounded<AuditEventDto>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly Channel<FeedbackEventDto> _feedbackQueue = Channel.CreateUnbounded<FeedbackEventDto>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedDatabaseAuditEventSink> _logger;

    public QueuedDatabaseAuditEventSink(IServiceScopeFactory scopeFactory, ILogger<QueuedDatabaseAuditEventSink> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task WriteAsync(AuditEventDto auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (!_queue.Writer.TryWrite(auditEvent))
        {
            _logger.LogError("Failed to enqueue audit event {EventType} hash:{TextHash}", auditEvent.EventType, auditEvent.TextHash);
        }

        return Task.CompletedTask;
    }

    public Task WriteFeedbackAsync(FeedbackEventDto feedbackEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedbackEvent);

        if (!_feedbackQueue.Writer.TryWrite(feedbackEvent))
        {
            _logger.LogError("Failed to enqueue feedback event {FeedbackId} for message:{MessageId}", feedbackEvent.FeedbackId, feedbackEvent.MessageId);
        }

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processAuditTask = ProcessAuditEventsAsync(stoppingToken);
        var processFeedbackTask = ProcessFeedbackEventsAsync(stoppingToken);
        await Task.WhenAll(processAuditTask, processFeedbackTask);
    }

    private async Task ProcessAuditEventsAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

                dbContext.AuditEvents.Add(new AuditEvent
                {
                    EventType = auditEvent.EventType,
                    AgentId = auditEvent.AgentId,
                    UserId = auditEvent.UserId,
                    IsAnonymous = auditEvent.IsAnonymous,
                    Channel = auditEvent.Channel,
                    MaskedText = auditEvent.MaskedText,
                    TextHash = auditEvent.TextHash,
                    Provider = auditEvent.Provider,
                    Model = auditEvent.Model,
                    FallbackReason = auditEvent.FallbackReason,
                    CitationCount = auditEvent.CitationCount,
                    PromptTokens = auditEvent.PromptTokens,
                    CompletionTokens = auditEvent.CompletionTokens,
                    TotalTokens = auditEvent.TotalTokens,
                    LatencyMs = auditEvent.LatencyMs,
                    CreatedAt = auditEvent.CreatedAt
                });

                await dbContext.SaveChangesAsync(stoppingToken);
                LogStoredAuditEvent(auditEvent);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist audit event {EventType} hash:{TextHash}", auditEvent.EventType, auditEvent.TextHash);
            }
        }
    }

    private async Task ProcessFeedbackEventsAsync(CancellationToken stoppingToken)
    {
        await foreach (var feedbackEvent in _feedbackQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

                dbContext.FeedbackEvents.Add(new FeedbackEvent
                {
                    Id = feedbackEvent.FeedbackId,
                    MessageId = feedbackEvent.MessageId,
                    UserId = feedbackEvent.UserId,
                    IsAnonymous = feedbackEvent.IsAnonymous,
                    Rating = feedbackEvent.Rating,
                    CommentMasked = feedbackEvent.CommentMasked,
                    Topic = feedbackEvent.Topic,
                    SeverityCandidate = feedbackEvent.SeverityCandidate,
                    Status = feedbackEvent.Status,
                    CreatedAt = feedbackEvent.CreatedAt
                });

                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Stored feedback event {FeedbackId} for message:{MessageId}", feedbackEvent.FeedbackId, feedbackEvent.MessageId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist feedback event {FeedbackId}", feedbackEvent.FeedbackId);
            }
        }
    }

    private void LogStoredAuditEvent(AuditEventDto auditEvent)
    {
        _logger.LogInformation(
            "Stored audit event {EventType} agent:{AgentId} user:{UserId} anonymous:{IsAnonymous} channel:{Channel} hash:{TextHash} provider:{Provider} model:{Model} fallback:{FallbackReason} citations:{CitationCount}",
            auditEvent.EventType,
            auditEvent.AgentId,
            auditEvent.UserId,
            auditEvent.IsAnonymous,
            auditEvent.Channel,
            auditEvent.TextHash,
            auditEvent.Provider,
            auditEvent.Model,
            auditEvent.FallbackReason,
            auditEvent.CitationCount);
    }
}
