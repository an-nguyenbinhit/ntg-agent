using Microsoft.EntityFrameworkCore;
using AskHR.Common.Dtos.Documents;
using AskHR.Common.Dtos.Services;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using System.Security.Cryptography;

namespace AskHR.Orchestrator.Services.Knowledge;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly AgentDbContext _agentDbContext;
    private readonly IKnowledgeService _knowledgeService;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(AgentDbContext agentDbContext, IKnowledgeService knowledgeService, ILogger<DocumentIngestionService> logger)
    {
        _agentDbContext = agentDbContext ?? throw new ArgumentNullException(nameof(agentDbContext));
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ComputeHashAsync(Stream content, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content, ct);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return Convert.ToHexString(hashBytes);
    }

    public async Task ReindexDocumentAsync(Document document, CancellationToken ct = default)
    {
        var permissions = await GetStoredPermissionsAsync(document, ct);
        var previousKnowledgeDocId = document.KnowledgeDocId;

        try
        {
            string newKnowledgeDocId;
            if (document.Type == DocumentType.WebPage)
            {
                if (string.IsNullOrWhiteSpace(document.Url))
                {
                    throw new InvalidOperationException("Document has no source URL to re-index from.");
                }

                newKnowledgeDocId = await _knowledgeService.ImportWebPageAsync(document.Url, document.AgentId, permissions, ct);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(previousKnowledgeDocId))
                {
                    throw new InvalidOperationException("Document has no existing knowledge base content to re-index from.");
                }

                var fileName = FileTypeService.SanitizeFileName(document.Name);
                var content = await _knowledgeService.ExportDocumentAsync(previousKnowledgeDocId, fileName, document.AgentId, ct)
                    ?? throw new InvalidOperationException("Unable to retrieve the document's existing content from the knowledge base.");

                await using var stream = await content.GetStreamAsync();
                newKnowledgeDocId = await _knowledgeService.ImportDocumentAsync(stream, fileName, document.AgentId, permissions, ct);
            }

            document.KnowledgeDocId = newKnowledgeDocId;
            document.IngestStatus = IngestStatus.Success;
            document.IngestErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(previousKnowledgeDocId) && previousKnowledgeDocId != newKnowledgeDocId)
            {
                await _knowledgeService.RemoveDocumentAsync(previousKnowledgeDocId, document.AgentId, ct);
            }
        }
        catch (Exception ex)
        {
            document.KnowledgeDocId = previousKnowledgeDocId;
            document.IngestStatus = IngestStatus.Failed;
            document.IngestErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to re-index document {DocumentId} for agent {AgentId}", document.Id, document.AgentId);
        }
    }

    public async Task ReindexExistingDocumentWithStreamAsync(Document document, Stream newStream, DocumentPermissionMetadata permissions, CancellationToken ct = default)
    {
        var previousKnowledgeDocId = document.KnowledgeDocId;
        try
        {
            var fileName = FileTypeService.SanitizeFileName(document.Name);
            var newKnowledgeDocId = await _knowledgeService.ImportDocumentAsync(newStream, fileName, document.AgentId, permissions, ct);

            document.KnowledgeDocId = newKnowledgeDocId;
            document.IngestStatus = IngestStatus.Success;
            document.IngestErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(previousKnowledgeDocId) && previousKnowledgeDocId != newKnowledgeDocId)
            {
                await _knowledgeService.RemoveDocumentAsync(previousKnowledgeDocId, document.AgentId, ct);
            }
        }
        catch (Exception ex)
        {
            document.KnowledgeDocId = previousKnowledgeDocId;
            document.IngestStatus = IngestStatus.Failed;
            document.IngestErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to re-index document {DocumentId} with new stream for agent {AgentId}", document.Id, document.AgentId);
        }
    }

    public async Task<DocumentIngestionOutcome> IngestFileAsync(Stream content, string fileName, Guid agentId, Guid? folderId, Guid userId, CancellationToken ct = default)
    {
        var hash = await ComputeHashAsync(content, ct);

        var existingDocument = await _agentDbContext.Documents
            .FirstOrDefaultAsync(d => d.Name == fileName && d.FolderId == folderId && d.AgentId == agentId, ct);

        if (existingDocument is null)
        {
            return await IngestNewFileAsync(content, fileName, agentId, folderId, userId, hash, ct);
        }

        if (string.Equals(existingDocument.Hash, hash, StringComparison.OrdinalIgnoreCase))
        {
            return DocumentIngestionOutcome.Unchanged;
        }

        existingDocument.Hash = hash;
        existingDocument.UpdatedByUserId = userId;
        existingDocument.UpdatedAt = DateTime.UtcNow;

        // Preserve the permission metadata the document already has; the watch folder
        // carries no permission information of its own.
        var permissions = await GetStoredPermissionsAsync(existingDocument, ct);
        await ReindexExistingDocumentWithStreamAsync(existingDocument, content, permissions, ct);
        await _agentDbContext.SaveChangesAsync(ct);

        return existingDocument.IngestStatus == IngestStatus.Success
            ? DocumentIngestionOutcome.Updated
            : DocumentIngestionOutcome.Failed;
    }

    private async Task<DocumentIngestionOutcome> IngestNewFileAsync(Stream content, string fileName, Guid agentId, Guid? folderId, Guid userId, string hash, CancellationToken ct)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            AgentId = agentId,
            FolderId = folderId,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            Type = DocumentType.File,
            Hash = hash,
            ApprovalStatus = ApprovalStatus.Pending
        };

        try
        {
            // No permission metadata is available from the source; approval remains pending
            // until an admin reviews and reindexes the document as approved.
            document.KnowledgeDocId = await _knowledgeService.ImportDocumentAsync(content, FileTypeService.SanitizeFileName(fileName), agentId, new DocumentPermissionMetadata { ApprovalStatus = ApprovalStatus.Pending.ToString() }, ct);
            document.IngestStatus = IngestStatus.Success;
        }
        catch (Exception ex)
        {
            document.IngestStatus = IngestStatus.Failed;
            document.IngestErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to ingest new document {FileName} for agent {AgentId}", fileName, agentId);
        }

        _agentDbContext.Documents.Add(document);
        await _agentDbContext.SaveChangesAsync(ct);

        return document.IngestStatus == IngestStatus.Success
            ? DocumentIngestionOutcome.Created
            : DocumentIngestionOutcome.Failed;
    }

    private async Task<DocumentPermissionMetadata> GetStoredPermissionsAsync(Document document, CancellationToken ct)
    {
        var tags = await _agentDbContext.DocumentTags
            .Where(dt => dt.DocumentId == document.Id)
            .Select(dt => dt.Tag.Name)
            .ToListAsync(ct);

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
        }.WithAllowedTags(tags);
    }
}
