using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
using AskHR.Orchestrator.Models.Chat;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.Escalation;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.KernelMemory;

namespace AskHR.Orchestrator.Services.Answers;

public sealed class PolicyAnswerService : IPolicyAnswerService
{
    private const string TagDocumentName = "documentName";
    private const string TagSourcePath = "sourcePath";
    private const string TagSourceType = "sourceType";
    private const string TagSourceUrl = "sourceUrl";
    private const string HandoffMessagePrefix = "This topic needs a human HR advisor.";
    private const string SkillRegistryCacheKey = "PolicyAnswerService:ApprovedSkills";

    private readonly IKnowledgeService _knowledgeService;
    private readonly IModelGateway _modelGateway;
    private readonly AgentDbContext _context;
    private readonly IAuditEventSink _auditEventSink;
    private readonly IAuditTextProtector _auditTextProtector;
    private readonly ISeverityClassifier _severityClassifier;
    private readonly IWarmHandoffService _warmHandoffService;
    private readonly IMemoryCache _cache;
    private readonly IOptions<AnswerPipelineOptions> _options;
    private readonly ILogger<PolicyAnswerService> _logger;

    public PolicyAnswerService(
        IKnowledgeService knowledgeService,
        IModelGateway modelGateway,
        AgentDbContext context,
        IAuditEventSink auditEventSink,
        IAuditTextProtector auditTextProtector,
        ISeverityClassifier severityClassifier,
        IWarmHandoffService warmHandoffService,
        IMemoryCache cache,
        IOptions<AnswerPipelineOptions> options,
        ILogger<PolicyAnswerService> logger)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _modelGateway = modelGateway ?? throw new ArgumentNullException(nameof(modelGateway));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditEventSink = auditEventSink ?? throw new ArgumentNullException(nameof(auditEventSink));
        _auditTextProtector = auditTextProtector ?? throw new ArgumentNullException(nameof(auditTextProtector));
        _severityClassifier = severityClassifier ?? throw new ArgumentNullException(nameof(severityClassifier));
        _warmHandoffService = warmHandoffService ?? throw new ArgumentNullException(nameof(warmHandoffService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AskHrAnswerResponse> AnswerAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        CancellationToken cancellationToken = default)
    {
        AskHrAnswerResponse? finalAnswer = null;
        await foreach (var streamEvent in StreamAnswerAsync(request, authorization, cancellationToken))
        {
            if (streamEvent.Answer is not null)
            {
                finalAnswer = streamEvent.Answer;
            }
        }

        return finalAnswer ?? throw new InvalidOperationException("Answer pipeline did not produce a final answer.");
    }

    public async IAsyncEnumerable<AskHrStreamEvent> StreamAnswerAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(authorization);

        if (request.AgentId == Guid.Empty)
        {
            throw new ArgumentException("A valid agentId is required.", nameof(request));
        }

        var timer = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            var fallback = await CompleteFallbackAsync(request, authorization, "empty-question", null, null, [], timer, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallback.AnswerText))
            {
                yield return new AskHrStreamEvent(AskHrStreamEventType.Token, Content: fallback.AnswerText);
            }

            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Done,
                Answer: fallback,
                ConversationId: fallback.ConversationId,
                MessageId: fallback.MessageId);
            yield break;
        }

        if (await IsConversationFrozenAsync(request, cancellationToken))
        {
            var frozenClassification = new SeverityClassification(EscalationSeverity.P1, "Sensitive HR issue", "conversation-frozen", RequiresWarmHandoff: true);
            var handoff = new WarmHandoffResult(
                $"handoff-{Guid.NewGuid():N}",
                "This conversation is already paused for HR follow-up. I will not continue automated answers in this thread.",
                []);
            var frozenResponse = await CompleteHandoffAsync(request, authorization, frozenClassification, handoff, timer, cancellationToken);

            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Handoff,
                Content: handoff.UserMessage,
                Answer: frozenResponse,
                Severity: frozenClassification.Severity.ToString(),
                ConversationId: frozenResponse.ConversationId,
                MessageId: frozenResponse.MessageId,
                HandoffId: handoff.HandoffId);
            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Done,
                Answer: frozenResponse,
                Severity: frozenClassification.Severity.ToString(),
                ConversationId: frozenResponse.ConversationId,
                MessageId: frozenResponse.MessageId,
                HandoffId: handoff.HandoffId);
            yield break;
        }

        var initialClassification = _severityClassifier.Classify(request);
        if (initialClassification.RequiresWarmHandoff)
        {
            var handoff = await _warmHandoffService.CreateAsync(request, authorization, initialClassification, cancellationToken);
            var handoffResponse = await CompleteHandoffAsync(request, authorization, initialClassification, handoff, timer, cancellationToken);

            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Handoff,
                Content: handoff.UserMessage,
                Answer: handoffResponse,
                Severity: initialClassification.Severity.ToString(),
                ConversationId: handoffResponse.ConversationId,
                MessageId: handoffResponse.MessageId,
                HandoffId: handoff.HandoffId);
            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Done,
                Answer: handoffResponse,
                Severity: initialClassification.Severity.ToString(),
                ConversationId: handoffResponse.ConversationId,
                MessageId: handoffResponse.MessageId,
                HandoffId: handoff.HandoffId);
            yield break;
        }

        var selectedSkill = await ResolveSkillAsync(request, cancellationToken);
        yield return new AskHrStreamEvent(AskHrStreamEventType.SearchQuery, Content: request.Question);

        var searchResult = await _knowledgeService.SearchAsync(request.Question, request.AgentId, authorization, cancellationToken);
        var citations = await AddDocumentDownloadMetadataAsync(
            request.AgentId,
            ExtractCitations(searchResult)
            .Where(x => x.Relevance >= _options.Value.MinRelevance)
            .OrderByDescending(x => x.Relevance)
            .Take(Math.Max(1, _options.Value.MaxFacts))
            .ToList(),
            cancellationToken);
        var classification = _severityClassifier.Classify(request, citations);

        if (citations.Count == 0)
        {
            var fallback = await CompleteFallbackAsync(request, authorization, "no-grounding-citations", null, null, citations, timer, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallback.AnswerText))
            {
                yield return new AskHrStreamEvent(AskHrStreamEventType.Token, Content: fallback.AnswerText, Severity: classification.Severity.ToString());
            }

            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Done,
                Answer: fallback,
                Severity: classification.Severity.ToString(),
                ConversationId: fallback.ConversationId,
                MessageId: fallback.MessageId);
            yield break;
        }

        foreach (var citation in citations)
        {
            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Citation,
                Citation: citation,
                Severity: classification.Severity.ToString());
        }

        var answerBuilder = new StringBuilder();
        string? provider = null;
        string? model = null;
        string? routeName = null;

        var messages = BuildMessages(request.Question, citations, selectedSkill);
        await foreach (var modelResponse in _modelGateway.StreamCompleteAsync(
            new ModelCompletionRequest(
                ModelCapability.AnswerGeneration,
                request.AgentId,
                messages,
                new ChatOptions { Temperature = 0 },
                "hr-policy",
                selectedSkill?.PrimaryProvider,
                selectedSkill?.PrimaryModel,
                selectedSkill is null ? null : $"skill:{selectedSkill.SkillId}"),
            cancellationToken))
        {
            provider ??= modelResponse.Route.Provider;
            model ??= modelResponse.Route.Model;
            routeName ??= modelResponse.Route.RouteName;

            if (!string.IsNullOrEmpty(modelResponse.TextDelta))
            {
                answerBuilder.Append(modelResponse.TextDelta);
                yield return new AskHrStreamEvent(
                    AskHrStreamEventType.Token,
                    Content: modelResponse.TextDelta,
                    Severity: classification.Severity.ToString());
            }
        }

        if (string.IsNullOrWhiteSpace(answerBuilder.ToString()))
        {
            var fallback = await CompleteFallbackAsync(request, authorization, "empty-model-answer", provider, model, citations, timer, cancellationToken);
            yield return new AskHrStreamEvent(AskHrStreamEventType.Token, Content: fallback.AnswerText, Severity: classification.Severity.ToString());
            yield return new AskHrStreamEvent(
                AskHrStreamEventType.Done,
                Answer: fallback,
                Severity: classification.Severity.ToString(),
                ConversationId: fallback.ConversationId,
                MessageId: fallback.MessageId);
            yield break;
        }

        var response = new AskHrAnswerResponse(
            answerBuilder.ToString().Trim(),
            citations,
            CalculateConfidence(citations),
            null,
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                provider ?? "unknown",
                model ?? "unknown",
                routeName,
                _options.Value.RetrievalStrategy,
                citations.Count,
                null,
                timer.ElapsedMilliseconds));

        response = await PersistConversationTurnAsync(request, authorization, response, cancellationToken);
        await WriteAuditAsync(request, authorization, response, cancellationToken);

        yield return new AskHrStreamEvent(
            AskHrStreamEventType.Done,
            Answer: response,
            Severity: classification.Severity.ToString(),
            ConversationId: response.ConversationId,
            MessageId: response.MessageId);
    }

    private List<ChatMessage> BuildMessages(string question, List<AnswerCitationDto> citations, Skill? skill)
    {
        var facts = new StringBuilder();
        for (var i = 0; i < citations.Count; i++)
        {
            var citation = citations[i];
            facts
                .Append(CultureInfo.InvariantCulture, $"[{i + 1}] ")
                .Append(citation.DocumentName)
                .Append(": ")
                .AppendLine(TrimSnippet(citation.Snippet));
        }

        var skillInstructions = string.IsNullOrWhiteSpace(skill?.Instructions)
            ? string.Empty
            : $"""

                Skill instructions:
                {skill.Instructions.Trim()}
                """;

        return
        [
            new ChatMessage(ChatRole.System, """
                You answer HR policy questions only from the provided facts.
                If the facts do not contain the answer, say you could not find reliable HR documentation.
                Do not use outside knowledge. Keep the answer concise and cite source numbers like [1].
                """ + skillInstructions),
            new ChatMessage(ChatRole.User, $"""
                Question:
                {question}

                Facts:
                {facts}
                """)
        ];
    }

    private async Task<Skill?> ResolveSkillAsync(AskHrRequest request, CancellationToken cancellationToken)
    {
        var skills = await _cache.GetOrCreateAsync(
            SkillRegistryCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _context.Skills
                    .AsNoTracking()
                    .Where(x => x.Enabled && x.ApprovalStatus == "Approved")
                    .ToListAsync(cancellationToken);
            }) ?? [];

        return skills
            .Select(skill => new { Skill = skill, Score = ScoreSkill(skill, request.Question) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Skill.Name)
            .Select(x => x.Skill)
            .FirstOrDefault();
    }

    private static int ScoreSkill(Skill skill, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return 0;
        }

        var normalizedQuestion = NormalizeMatchText(question);
        var score = 0;

        score += ContainsPhrase(normalizedQuestion, skill.SkillId) ? 100 : 0;
        score += ContainsPhrase(normalizedQuestion, skill.Name) ? 60 : 0;
        score += CountMatches(normalizedQuestion, skill.Scope.Topics) * 30;
        score += CountMatches(normalizedQuestion, skill.Scope.Tags) * 20;
        score += CountMatches(normalizedQuestion, skill.Scope.BusinessUnits) * 10;

        foreach (var term in TokenizeForMatching($"{skill.Name} {skill.Description}"))
        {
            if (normalizedQuestion.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        return score;
    }

    private static int CountMatches(string normalizedQuestion, IEnumerable<string>? values)
        => values?.Count(value => ContainsPhrase(normalizedQuestion, value)) ?? 0;

    private static bool ContainsPhrase(string normalizedQuestion, string? value)
    {
        var normalizedValue = NormalizeMatchText(value);
        return !string.IsNullOrWhiteSpace(normalizedValue)
            && normalizedQuestion.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> TokenizeForMatching(string value)
        => NormalizeMatchText(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeMatchText(string? value)
        => string.Join(' ', (value ?? string.Empty).ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private async Task<AskHrAnswerResponse> CompleteFallbackAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        string fallbackReason,
        string? provider,
        string? model,
        List<AnswerCitationDto> citations,
        Stopwatch timer,
        CancellationToken cancellationToken)
    {
        var response = new AskHrAnswerResponse(
            _options.Value.FallbackAnswer,
            citations,
            0,
            fallbackReason,
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                provider ?? "none",
                model ?? "none",
                null,
                _options.Value.RetrievalStrategy,
                citations.Count,
                fallbackReason,
                timer.ElapsedMilliseconds));

        response = await PersistConversationTurnAsync(request, authorization, response, cancellationToken);
        await WriteAuditAsync(request, authorization, response, cancellationToken);
        return response;
    }

    private async Task<AskHrAnswerResponse> CompleteHandoffAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        SeverityClassification classification,
        WarmHandoffResult handoff,
        Stopwatch timer,
        CancellationToken cancellationToken)
    {
        var response = new AskHrAnswerResponse(
            handoff.UserMessage,
            [],
            0,
            "warm-handoff",
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                "none",
                "none",
                null,
                _options.Value.RetrievalStrategy,
                0,
                $"{classification.Severity}:{classification.Reason}",
                timer.ElapsedMilliseconds),
            IsHandoff: true,
            HandoffId: handoff.HandoffId);

        response = await PersistConversationTurnAsync(request, authorization, response, cancellationToken);
        await WriteAuditAsync(request, authorization, response, cancellationToken);
        return response;
    }

    private async Task WriteAuditAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        AskHrAnswerResponse response,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEventDto(
            "answer.generated",
            request.AgentId,
            authorization.UserId,
            authorization.IsAnonymous,
            request.Channel,
            _auditTextProtector.Mask(request.Question),
            _auditTextProtector.Hash(request.Question),
            response.AuditMetadata.Provider,
            response.AuditMetadata.Model,
            response.FallbackReason,
            response.Citations.Count,
            PromptTokens: response.AuditMetadata.PromptTokens,
            CompletionTokens: response.AuditMetadata.CompletionTokens,
            TotalTokens: response.AuditMetadata.TotalTokens,
            LatencyMs: response.AuditMetadata.LatencyMs,
            CreatedAt: DateTimeOffset.UtcNow);

        try
        {
            await _auditEventSink.WriteAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit event {EventType} hash:{TextHash}", auditEvent.EventType, auditEvent.TextHash);
        }
    }

    private async Task<AskHrAnswerResponse> PersistConversationTurnAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        AskHrAnswerResponse response,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ThreadId, out var conversationId))
        {
            return response;
        }

        var conversation = await ResolveConversationAsync(conversationId, request, authorization, cancellationToken);
        if (conversation is null)
        {
            return response;
        }

        var now = DateTime.UtcNow;
        var userMessage = new PChatMessage
        {
            UserId = authorization.UserId,
            Conversation = conversation,
            ConversationId = conversation.Id,
            AgentId = request.AgentId,
            Content = request.Question,
            Role = ChatRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };
        var persistedAnswerText = AppendCitationAppendixIfMissing(response.AnswerText, response.Citations);
        var assistantMessage = new PChatMessage
        {
            UserId = authorization.UserId,
            Conversation = conversation,
            ConversationId = conversation.Id,
            AgentId = request.AgentId,
            Content = persistedAnswerText,
            Role = ChatRole.Assistant,
            CreatedAt = now,
            UpdatedAt = now
        };

        conversation.UpdatedAt = now;
        if (string.Equals(conversation.Name, "New Conversation", StringComparison.OrdinalIgnoreCase))
        {
            conversation.Name = BuildConversationName(request.Question);
        }

        _context.ChatMessages.AddRange(userMessage, assistantMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return response with
        {
            AnswerText = persistedAnswerText,
            ConversationId = conversation.Id,
            MessageId = assistantMessage.Id
        };
    }

    private async Task<Conversation?> ResolveConversationAsync(
        Guid conversationId,
        AskHrRequest request,
        AuthorizationContext authorization,
        CancellationToken cancellationToken)
    {
        if (!authorization.IsAnonymous && authorization.UserId is Guid userId)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId, cancellationToken);
        }

        if (Guid.TryParse(request.SessionId, out var sessionId))
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(x => x.Id == conversationId && x.SessionId == sessionId, cancellationToken);
        }

        return null;
    }

    private async Task<bool> IsConversationFrozenAsync(AskHrRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ThreadId, out var conversationId))
        {
            return false;
        }

        return await _context.ChatMessages
            .AnyAsync(x =>
                x.ConversationId == conversationId
                && x.Role == ChatRole.Assistant
                && x.Content.StartsWith(HandoffMessagePrefix), cancellationToken);
    }

    private static string BuildConversationName(string question)
    {
        var normalized = string.Join(' ', question.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 60 ? normalized : normalized[..60];
    }

    private static List<AnswerCitationDto> ExtractCitations(SearchResult searchResult)
    {
        var citations = new List<AnswerCitationDto>();
        foreach (var result in searchResult.Results)
        {
            foreach (var partition in result.Partitions)
            {
                var tags = partition.Tags;
                var documentName = ReadTag(tags, TagDocumentName)
                    ?? result.SourceName
                    ?? result.DocumentId
                    ?? "unknown";

                citations.Add(new AnswerCitationDto(
                    result.DocumentId,
                    documentName,
                    ReadTag(tags, TagSourceType) ?? "unknown",
                    ReadTag(tags, TagSourcePath),
                    ReadTag(tags, TagSourceUrl),
                    partition.Text ?? string.Empty,
                    partition.Relevance));
            }
        }

        return citations;
    }

    private static string? ReadTag(TagCollection tags, string name)
        => tags.ContainsKey(name)
            ? tags[name].FirstOrDefault()
            : null;

    private static string AppendCitationAppendixIfMissing(string answerText, IReadOnlyList<AnswerCitationDto> citations)
    {
        if (citations.Count == 0 || answerText.Contains("citation-sources", StringComparison.OrdinalIgnoreCase))
        {
            return answerText;
        }

        return answerText + BuildCitationAppendix(citations);
    }

    private static string BuildCitationAppendix(IReadOnlyList<AnswerCitationDto> citations)
    {
        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("<section class=\"citation-sources\" aria-label=\"Sources\">");
        builder.AppendLine("<div class=\"citation-title\">Sources</div>");
        builder.AppendLine("<ol class=\"citation-list\">");

        var sources = citations
            .Select((citation, index) => new
            {
                Citation = citation,
                Number = index + 1,
                Url = BuildCitationUrl(citation)
            })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Url) ? x.Citation.DocumentName : x.Url, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

        foreach (var source in sources)
        {
            var citation = source.Citation;
            var documentName = WebUtility.HtmlEncode(citation.DocumentName);
            var sourceType = WebUtility.HtmlEncode(citation.SourceType);

            builder.Append("<li class=\"citation-item\">");
            if (!string.IsNullOrWhiteSpace(source.Url))
            {
                builder.Append("<a class=\"citation-link\" href=\"")
                    .Append(WebUtility.HtmlEncode(source.Url))
                    .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">")
                    .Append("<span class=\"citation-index\">[")
                    .Append(source.Number)
                    .Append("]</span>")
                    .Append("<span class=\"citation-name\">")
                    .Append(documentName)
                    .Append("</span>")
                    .Append("<span class=\"citation-type\">")
                    .Append(sourceType)
                    .Append("</span>")
                    .Append("<i class=\"bi bi-box-arrow-up-right\"></i>")
                    .Append("</a>");
            }
            else
            {
                builder.Append("<span class=\"citation-link citation-link-disabled\">")
                    .Append("<span class=\"citation-index\">[")
                    .Append(source.Number)
                    .Append("]</span>")
                    .Append("<span class=\"citation-name\">")
                    .Append(documentName)
                    .Append("</span>")
                    .Append("<span class=\"citation-type\">")
                    .Append(sourceType)
                    .Append("</span>")
                    .Append("</span>");
            }

            builder.AppendLine("</li>");
        }

        builder.AppendLine("</ol>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string? BuildCitationUrl(AnswerCitationDto citation)
    {
        if (!string.IsNullOrWhiteSpace(citation.SourceUrl)
            && Uri.TryCreate(citation.SourceUrl, UriKind.Absolute, out var sourceUri)
            && (sourceUri.Scheme == Uri.UriSchemeHttp || sourceUri.Scheme == Uri.UriSchemeHttps))
        {
            return sourceUri.ToString();
        }

        if (citation.AgentId is Guid agentId && citation.DatabaseDocumentId is Guid documentId)
        {
            return $"/api/documents/download/{agentId:D}/{documentId:D}";
        }

        return null;
    }

    private async Task<List<AnswerCitationDto>> AddDocumentDownloadMetadataAsync(
        Guid agentId,
        List<AnswerCitationDto> citations,
        CancellationToken cancellationToken)
    {
        if (citations.Count == 0)
        {
            return citations;
        }

        var knowledgeDocumentIds = citations
            .Select(x => x.DocumentId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (knowledgeDocumentIds.Count == 0)
        {
            return citations
                .Select(x => x with { AgentId = agentId })
                .ToList();
        }

        var documents = await _context.Documents
            .AsNoTracking()
            .Where(x => x.AgentId == agentId
                && x.KnowledgeDocId != null
                && knowledgeDocumentIds.Contains(x.KnowledgeDocId))
            .Select(x => new { x.KnowledgeDocId, x.Id })
            .ToListAsync(cancellationToken);

        var documentsByKnowledgeId = documents
            .Where(x => x.KnowledgeDocId is not null)
            .GroupBy(x => x.KnowledgeDocId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);

        return citations
            .Select(citation =>
            {
                Guid? databaseDocumentId = null;
                if (citation.DocumentId is not null
                    && documentsByKnowledgeId.TryGetValue(citation.DocumentId, out var mappedDocumentId))
                {
                    databaseDocumentId = mappedDocumentId;
                }

                return citation with
                {
                    AgentId = agentId,
                    DatabaseDocumentId = databaseDocumentId
                };
            })
            .ToList();
    }

    private string TrimSnippet(string value)
    {
        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= _options.Value.MaxSnippetCharacters
            ? normalized
            : normalized[.._options.Value.MaxSnippetCharacters];
    }

    private static double CalculateConfidence(List<AnswerCitationDto> citations)
        => citations.Count == 0
            ? 0
            : Math.Clamp(citations.Average(x => x.Relevance), 0, 1);
}
