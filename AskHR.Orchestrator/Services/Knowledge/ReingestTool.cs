using AskHR.Common.Dtos.Documents;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.Knowledge;

public sealed class ReingestTool : IReingestTool
{
    private readonly AgentDbContext _dbContext;
    private readonly IDocumentIngestionService _documentIngestionService;
    private readonly IOptions<ReingestMigrationOptions> _options;
    private readonly ILogger<ReingestTool> _logger;

    public ReingestTool(
        AgentDbContext dbContext,
        IDocumentIngestionService documentIngestionService,
        IOptions<ReingestMigrationOptions> options,
        ILogger<ReingestTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _documentIngestionService = documentIngestionService ?? throw new ArgumentNullException(nameof(documentIngestionService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReingestMigrationSummary> RunAsync(ReingestMigrationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dryRun = request.DryRun ?? _options.Value.DryRun;
        var defaultRoles = CleanList(request.DefaultRoles ?? _options.Value.DefaultRoles);
        var defaultBusinessUnits = CleanList(request.DefaultBusinessUnits ?? _options.Value.DefaultBusinessUnits);
        var defaultSensitivityLevel = TrimToNull(request.DefaultSensitivityLevel) ?? _options.Value.DefaultSensitivityLevel;

        var query = _dbContext.Documents
            .Where(x => x.KnowledgeDocId != null);

        if (request.AgentId.HasValue)
        {
            query = query.Where(x => x.AgentId == request.AgentId.Value);
        }

        var documents = await query
            .OrderBy(x => x.AgentId)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var items = new List<ReingestMigrationItem>(documents.Count);
        foreach (var document in documents)
        {
            var previousKnowledgeDocId = document.KnowledgeDocId;
            if (dryRun)
            {
                items.Add(new ReingestMigrationItem(document.Id, document.Name, previousKnowledgeDocId, document.KnowledgeDocId, false, null));
                continue;
            }

            ApplyDefaultPermissions(document, defaultRoles, defaultBusinessUnits, defaultSensitivityLevel);

            try
            {
                await _documentIngestionService.ReindexDocumentAsync(document, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                items.Add(new ReingestMigrationItem(
                    document.Id,
                    document.Name,
                    previousKnowledgeDocId,
                    document.KnowledgeDocId,
                    document.IngestStatus == IngestStatus.Success,
                    document.IngestErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Re-ingest migration failed for document {DocumentId}", document.Id);
                items.Add(new ReingestMigrationItem(document.Id, document.Name, previousKnowledgeDocId, document.KnowledgeDocId, false, ex.Message));
            }
        }

        return new ReingestMigrationSummary(
            dryRun,
            documents.Count,
            items.Count(x => x.Reindexed),
            items.Count(x => x.Error is not null),
            items);
    }

    private static void ApplyDefaultPermissions(Document document, List<string> defaultRoles, List<string> defaultBusinessUnits, string defaultSensitivityLevel)
    {
        if (document.Roles.Count == 0)
        {
            document.Roles = defaultRoles;
        }

        if (document.BusinessUnits.Count == 0)
        {
            document.BusinessUnits = defaultBusinessUnits;
        }

        if (string.IsNullOrWhiteSpace(document.SensitivityLevel))
        {
            document.SensitivityLevel = defaultSensitivityLevel;
        }
    }

    private static List<string> CleanList(IEnumerable<string>? values) =>
        values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

