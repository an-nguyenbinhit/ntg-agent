using Microsoft.KernelMemory;
using System.Globalization;
namespace AskHR.Orchestrator.Services.Knowledge;

public class KernelMemoryKnowledge : IKnowledgeService
{
    private readonly IKernelMemory _kernelMemory;
    private readonly ILogger<KernelMemoryKnowledge> _logger;
    private const string TagNameAgentId = "agentId";
    private const string TagNameTags = "tags";

    public KernelMemoryKnowledge(IKernelMemory kernelMemory, ILogger<KernelMemoryKnowledge> logger)
    {
        _kernelMemory = kernelMemory ?? throw new ArgumentNullException(nameof(kernelMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<string> ImportDocumentAsync(Stream content, string fileName, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
    {
        var tagCollection = ComposeTags(agentId, tags);
        return await _kernelMemory.ImportDocumentAsync(content, fileName, index: IndexFor(agentId), tags: tagCollection, cancellationToken: cancellationToken);
    }

    public async Task RemoveDocumentAsync(string documentId, Guid agentId, CancellationToken cancellationToken = default)
    {
        await _kernelMemory.DeleteDocumentAsync(documentId, index: IndexFor(agentId), cancellationToken: cancellationToken);
    }
    public async Task<SearchResult> SearchAsync(string query, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
    {
        var filters = ComposeFilters(agentId, tags);

        // Deny-by-default: every query is scoped to the agent's own index, so a missing or
        // empty permission filter can never fall through to a global, cross-agent search.
        var result = await _kernelMemory.SearchAsync(
            query: query,
            index: IndexFor(agentId),
            filters: filters.Count > 0 ? filters : null,
            limit: 3,
            cancellationToken: cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("KernelMemoryKnowledge.SearchAsync: {Query}, tags:{Tags} => {Result}", query, string.Join(", ", tags), result.ToJson());
        }
        return result;
    }

    public async Task<SearchResult> SearchAsync(string query, Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Scoped to the agent's index (deny-by-default); never a global, cross-agent search.
        return await _kernelMemory.SearchAsync(query, index: IndexFor(agentId), limit: 3, cancellationToken: cancellationToken);
    }

    public async Task<string> ImportWebPageAsync(string url, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Invalid URL provided.", nameof(url));
        }
        var tagCollection = ComposeTags(agentId, tags);
        var documentId = await _kernelMemory.ImportWebPageAsync(url, index: IndexFor(agentId), tags: tagCollection, cancellationToken: cancellationToken);
        return documentId;
    }

    public async Task<string> ImportTextContentAsync(string content, string fileName, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        var tagCollection = ComposeTags(agentId, tags);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return await _kernelMemory.ImportDocumentAsync(stream, fileName, index: IndexFor(agentId), tags: tagCollection, cancellationToken: cancellationToken);
    }

    public async Task<StreamableFileContent> ExportDocumentAsync(string documentId, string fileName, Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _kernelMemory.ExportFileAsync(documentId, fileName, index: IndexFor(agentId), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Resolves the per-agent Kernel Memory index. Each agent's documents live in their own
    /// index, giving hard store-level isolation: an agent physically cannot retrieve another
    /// agent's chunks even if tag filtering is misconfigured. The name is derived
    /// deterministically from the agent id so it needs no extra config and cannot collide.
    /// </summary>
    private static string IndexFor(Guid agentId)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("A valid agentId is required to resolve the knowledge index.", nameof(agentId));
        }

        return $"agent-{agentId:N}";
    }

    private static TagCollection ComposeTags(Guid agentId, IEnumerable<string> tags)
    {
        if (tags == null || agentId == Guid.Empty)
        {
            return new TagCollection();
        }

        var formattedTags = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLower(CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();

        if (formattedTags.Count == 0)
        {
            return new TagCollection();
        }

        return new TagCollection
        {
            { TagNameAgentId, agentId.ToString().ToLower(CultureInfo.InvariantCulture) },
            { TagNameTags, formattedTags.Cast<string?>().ToList() }
        };
    }
    private static List<MemoryFilter> ComposeFilters(Guid agentId, IEnumerable<string> tags)
    {
        if (tags == null || agentId == Guid.Empty)
        {
            return new List<MemoryFilter>();
        }
        var formattedTags = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLower(CultureInfo.InvariantCulture))
            .Distinct();

        var filters = formattedTags
               .Select(tag => {
                   var memoryFilter = MemoryFilters.ByTag(TagNameTags, tag);
                   memoryFilter.Add(TagNameAgentId, agentId.ToString().ToLower(CultureInfo.InvariantCulture));
                   return memoryFilter;
               })
               .ToList();
        return filters;
    }
}
