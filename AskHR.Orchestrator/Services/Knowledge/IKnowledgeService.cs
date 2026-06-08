using Microsoft.KernelMemory;
using AskHR.Common.Dtos.Documents;
using AskHR.Common.Dtos.Security;

namespace AskHR.Orchestrator.Services.Knowledge;

public interface IKnowledgeService
{
    public Task<SearchResult> SearchAsync(string query, Guid agentId, AuthorizationContext authorization, CancellationToken cancellationToken = default);

    public Task<string> ImportDocumentAsync(Stream content, string fileName, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default);

    public Task RemoveDocumentAsync(string documentId, Guid agentId, CancellationToken cancellationToken = default);

    public Task<string> ImportWebPageAsync(string url, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default);

    public Task<string> ImportTextContentAsync(string content, string fileName, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default);

    public Task<StreamableFileContent> ExportDocumentAsync(string documentId, string fileName, Guid agentId, CancellationToken cancellationToken = default);
}
