using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Services.Escalation;

public sealed class WarmHandoffService : IWarmHandoffService
{
    private const int MaxContextMessages = 12;

    private readonly AgentDbContext _context;
    private readonly IAuditTextProtector _textProtector;
    private readonly IAuditEventSink _auditSink;

    public WarmHandoffService(
        AgentDbContext context,
        IAuditTextProtector textProtector,
        IAuditEventSink auditSink)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _textProtector = textProtector ?? throw new ArgumentNullException(nameof(textProtector));
        _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    }

    public async Task<WarmHandoffResult> CreateAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        SeverityClassification classification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(classification);

        var handoffId = $"handoff-{Guid.NewGuid():N}";
        var context = await LoadMaskedConversationContextAsync(request, cancellationToken);

        await _auditSink.WriteAsync(new AuditEventDto(
            "handoff.created",
            request.AgentId,
            authorization.UserId,
            authorization.IsAnonymous,
            request.Channel,
            _textProtector.Mask(request.Question),
            _textProtector.Hash(request.Question),
            null,
            null,
            $"{classification.Severity}:{classification.Reason}:{handoffId}",
            0,
            CreatedAt: DateTimeOffset.UtcNow), cancellationToken);

        return new WarmHandoffResult(
            handoffId,
            "This topic needs a human HR advisor. I have paused the automated answer and prepared the conversation context for HR follow-up.",
            context);
    }

    private async Task<IReadOnlyList<string>> LoadMaskedConversationContextAsync(AskHrRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ThreadId, out var conversationId))
        {
            return [_textProtector.Mask(request.Question)];
        }

        var messages = await _context.ChatMessages
            .Where(x => x.ConversationId == conversationId && !x.IsSummary)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxContextMessages)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Content)
            .ToListAsync(cancellationToken);

        messages.Add(request.Question);
        return messages.Select(_textProtector.Mask).ToList();
    }
}
