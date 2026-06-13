using AskHR.Common.Dtos.Documents;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using AskHR.Orchestrator.Models.Tags;
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
        List<Guid> defaultTagIds = dryRun ? [] : await ResolveDefaultTagIdsAsync(request.DefaultTagIds, cancellationToken);

        var query = _dbContext.Documents
            .Include(x => x.DocumentTags)
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

            ApplyDefaultPermissions(document, defaultRoles, defaultBusinessUnits, defaultSensitivityLevel, defaultTagIds);
            var permissions = BuildPermissionSnapshot(document);

            try
            {
                await _documentIngestionService.ReindexDocumentAsync(document, permissions, cancellationToken);
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

    private void ApplyDefaultPermissions(
        Document document,
        List<string> defaultRoles,
        List<string> defaultBusinessUnits,
        string defaultSensitivityLevel,
        List<Guid> defaultTagIds)
    {
        if (document.Roles.Count == 0)
        {
            document.Roles = [.. defaultRoles];
        }

        if (document.BusinessUnits.Count == 0)
        {
            document.BusinessUnits = [.. defaultBusinessUnits];
        }

        if (string.IsNullOrWhiteSpace(document.SensitivityLevel))
        {
            document.SensitivityLevel = defaultSensitivityLevel;
        }

        if (document.DocumentTags.Count == 0)
        {
            foreach (var tagId in defaultTagIds)
            {
                var documentTag = new DocumentTag
                {
                    DocumentId = document.Id,
                    TagId = tagId
                };
                _dbContext.DocumentTags.Add(documentTag);
            }
        }
    }

    private static DocumentPermissionMetadata BuildPermissionSnapshot(Document document)
    {
        return new DocumentPermissionMetadata
        {
            Roles = document.Roles,
            BusinessUnits = document.BusinessUnits,
            Countries = document.Countries,
            LegalEntities = document.LegalEntities,
            ApplicableLevels = document.ApplicableLevels,
            ApplicableTo = document.ApplicableLevels,
            SensitivityLevel = document.SensitivityLevel,
            ApprovalStatus = document.ApprovalStatus.ToString()
        }.WithAllowedTags(document.DocumentTags.Select(x => x.TagId.ToString()));
    }

    private async Task<List<Guid>> ResolveDefaultTagIdsAsync(List<Guid>? requestTagIds, CancellationToken cancellationToken)
    {
        var requested = requestTagIds is { Count: > 0 }
            ? requestTagIds.Distinct().ToList()
            : CleanList(_options.Value.DefaultTagIds)
                .Select(x => Guid.TryParse(x, out var tagId) ? tagId : throw new InvalidOperationException($"Invalid ReingestMigration default tag id '{x}'."))
                .Distinct()
                .ToList();

        if (requested.Count == 0)
        {
            return [];
        }

        var existing = await _dbContext.Tags
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var missing = requested.Except(existing).ToList();
        if (missing.Count != 0)
        {
            throw new InvalidOperationException($"ReingestMigration default tags do not exist: {string.Join(", ", missing)}.");
        }

        return requested;
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
