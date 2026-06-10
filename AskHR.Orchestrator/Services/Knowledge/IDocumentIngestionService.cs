using AskHR.Common.Dtos.Documents;
using AskHR.Orchestrator.Models.Documents;

namespace AskHR.Orchestrator.Services.Knowledge;

public enum DocumentIngestionOutcome
{
    Created = 1,
    Updated = 2,
    Unchanged = 3,
    Failed = 4
}

/// <summary>
/// Shared document ingestion logic (hashing, import and re-index) used by both the
/// Documents API and the watch-folder background service, so re-indexing does not
/// have to go through an HTTP request.
/// </summary>
public interface IDocumentIngestionService
{
    /// <summary>
    /// Computes the SHA256 hash (uppercase hex) of a seekable stream and resets its position to the start.
    /// </summary>
    Task<string> ComputeHashAsync(Stream content, CancellationToken ct = default);

    /// <summary>
    /// Re-imports a document into the knowledge base using its currently stored permission metadata, replacing
    /// the previous knowledge base entry. Updates the document's <see cref="Document.IngestStatus"/> and
    /// <see cref="Document.IngestErrorMessage"/> based on the outcome. Caller is responsible for persisting changes.
    /// </summary>
    Task ReindexDocumentAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Re-imports a document into the knowledge base from new file content, replacing the previous knowledge
    /// base entry. Updates the document's <see cref="Document.IngestStatus"/> and
    /// <see cref="Document.IngestErrorMessage"/> based on the outcome. Caller is responsible for persisting changes.
    /// </summary>
    Task ReindexExistingDocumentWithStreamAsync(Document document, Stream newStream, DocumentPermissionMetadata permissions, CancellationToken ct = default);

    /// <summary>
    /// Ingests a file from a non-interactive source (e.g. the watch folder): creates a new document when the
    /// file is unknown, re-indexes when its content hash changed, and skips it when unchanged.
    /// Persists all changes before returning.
    /// </summary>
    Task<DocumentIngestionOutcome> IngestFileAsync(Stream content, string fileName, Guid agentId, Guid? folderId, Guid userId, CancellationToken ct = default);
}
