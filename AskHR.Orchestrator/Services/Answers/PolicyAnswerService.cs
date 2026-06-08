using System.Diagnostics;
using System.Globalization;
using System.Text;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;

namespace AskHR.Orchestrator.Services.Answers;

public sealed class PolicyAnswerService : IPolicyAnswerService
{
    private const string TagDocumentName = "documentName";
    private const string TagSourcePath = "sourcePath";
    private const string TagSourceType = "sourceType";
    private const string TagSourceUrl = "sourceUrl";

    private readonly IKnowledgeService _knowledgeService;
    private readonly IModelGateway _modelGateway;
    private readonly IAuditEventSink _auditEventSink;
    private readonly IAuditTextProtector _auditTextProtector;
    private readonly IOptions<AnswerPipelineOptions> _options;
    private readonly ILogger<PolicyAnswerService> _logger;

    public PolicyAnswerService(
        IKnowledgeService knowledgeService,
        IModelGateway modelGateway,
        IAuditEventSink auditEventSink,
        IAuditTextProtector auditTextProtector,
        IOptions<AnswerPipelineOptions> options,
        ILogger<PolicyAnswerService> logger)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _modelGateway = modelGateway ?? throw new ArgumentNullException(nameof(modelGateway));
        _auditEventSink = auditEventSink ?? throw new ArgumentNullException(nameof(auditEventSink));
        _auditTextProtector = auditTextProtector ?? throw new ArgumentNullException(nameof(auditTextProtector));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AskHrAnswerResponse> AnswerAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(authorization);

        if (request.AgentId == Guid.Empty)
        {
            throw new ArgumentException("A valid agentId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return await FallbackAsync(request, authorization, "empty-question", null, null, Stopwatch.StartNew(), cancellationToken);
        }

        var timer = Stopwatch.StartNew();
        var searchResult = await _knowledgeService.SearchAsync(request.Question, request.AgentId, authorization, cancellationToken);
        var citations = ExtractCitations(searchResult)
            .Where(x => x.Relevance >= _options.Value.MinRelevance)
            .OrderByDescending(x => x.Relevance)
            .Take(Math.Max(1, _options.Value.MaxFacts))
            .ToList();

        if (citations.Count == 0)
        {
            return await FallbackAsync(request, authorization, "no-grounding-citations", null, null, timer, cancellationToken);
        }

        try
        {
            var messages = BuildMessages(request.Question, citations);
            var modelResponse = await _modelGateway.CompleteAsync(
                new ModelCompletionRequest(
                    ModelCapability.AnswerGeneration,
                    request.AgentId,
                    messages,
                    new ChatOptions { Temperature = 0 },
                    "hr-policy"),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(modelResponse.Text))
            {
                return await FallbackAsync(request, authorization, "empty-model-answer", modelResponse.Route.Provider, modelResponse.Route.Model, timer, cancellationToken);
            }

            var response = new AskHrAnswerResponse(
                modelResponse.Text,
                citations,
                CalculateConfidence(citations),
                null,
                new AnswerAuditMetadataDto(
                    ModelCapability.AnswerGeneration,
                    modelResponse.Route.Provider,
                    modelResponse.Route.Model,
                    modelResponse.Route.RouteName,
                    _options.Value.RetrievalStrategy,
                    citations.Count,
                    null,
                    timer.ElapsedMilliseconds));

            await WriteAuditAsync(request, authorization, response, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Answer generation failed for agent {AgentId}", request.AgentId);
            return await FallbackAsync(request, authorization, "model-generation-failed", null, null, timer, cancellationToken);
        }
    }

    private List<ChatMessage> BuildMessages(string question, List<AnswerCitationDto> citations)
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

        return
        [
            new ChatMessage(ChatRole.System, """
                You answer HR policy questions only from the provided facts.
                If the facts do not contain the answer, say you could not find reliable HR documentation.
                Do not use outside knowledge. Keep the answer concise and cite source numbers like [1].
                """),
            new ChatMessage(ChatRole.User, $"""
                Question:
                {question}

                Facts:
                {facts}
                """)
        ];
    }

    private async Task<AskHrAnswerResponse> FallbackAsync(
        AskHrRequest request,
        AuthorizationContext authorization,
        string fallbackReason,
        string? provider,
        string? model,
        Stopwatch timer,
        CancellationToken cancellationToken)
    {
        var response = new AskHrAnswerResponse(
            _options.Value.FallbackAnswer,
            [],
            0,
            fallbackReason,
            new AnswerAuditMetadataDto(
                ModelCapability.AnswerGeneration,
                provider ?? "none",
                model ?? "none",
                null,
                _options.Value.RetrievalStrategy,
                0,
                fallbackReason,
                timer.ElapsedMilliseconds));

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
            DateTimeOffset.UtcNow);

        await _auditEventSink.WriteAsync(auditEvent, cancellationToken);
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
