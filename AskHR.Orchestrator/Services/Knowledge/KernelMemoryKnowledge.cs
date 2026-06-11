using Microsoft.KernelMemory;
using System.Globalization;
using AskHR.Common.Dtos.Documents;
using AskHR.Common.Dtos.Security;
using DtoConstants = AskHR.Common.Dtos.Constants.Constants;

namespace AskHR.Orchestrator.Services.Knowledge;

public class KernelMemoryKnowledge : IKnowledgeService
{
    private readonly IKernelMemory _kernelMemory;
    private readonly ILogger<KernelMemoryKnowledge> _logger;
    private const string TagNameAgentId = "agentId";
    private const string TagNameTags = "tags";
    private const string TagNameRoles = "allowedRoles";
    private const string TagNameBusinessUnits = "businessUnits";
    private const string TagNameCountries = "countries";
    private const string TagNameLegalEntities = "legalEntities";
    private const string TagNameApplicableTo = "applicableTo";
    private const string TagNameSensitivity = "sensitivity";
    private const string TagNameApprovalStatus = "approvalStatus";
    private const string TagNameDocumentName = "documentName";
    private const string TagNameSourcePath = "sourcePath";
    private const string TagNameSourceType = "sourceType";
    private const string TagNameSourceUrl = "sourceUrl";
    private const string AnyTagValue = "__any__";
    private const string DenyAllTagValue = "__deny_all__";

    public KernelMemoryKnowledge(IKernelMemory kernelMemory, ILogger<KernelMemoryKnowledge> logger)
    {
        _kernelMemory = kernelMemory ?? throw new ArgumentNullException(nameof(kernelMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> ImportDocumentAsync(Stream content, string fileName, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
        => ImportDocumentAsync(content, fileName, agentId, DocumentPermissionMetadata.FromTags(tags), cancellationToken);

    public async Task<string> ImportDocumentAsync(Stream content, string fileName, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default)
    {
        var tagCollection = ComposeTags(agentId, permissions);
        AddCitationTags(tagCollection, fileName, "file", sourcePath: fileName);
        return await _kernelMemory.ImportDocumentAsync(content, fileName, index: IndexFor(agentId), tags: tagCollection, cancellationToken: cancellationToken);
    }

    public async Task RemoveDocumentAsync(string documentId, Guid agentId, CancellationToken cancellationToken = default)
    {
        await _kernelMemory.DeleteDocumentAsync(documentId, index: IndexFor(agentId), cancellationToken: cancellationToken);
    }

    public Task<SearchResult> SearchAsync(string query, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
        => SearchAsync(query, agentId, new AuthorizationContext { AllowedTags = tags }, cancellationToken);

    public Task<SearchResult> SearchAsync(string query, Guid agentId, Guid userId, CancellationToken cancellationToken = default)
        => SearchAsync(query, agentId, new AuthorizationContext { UserId = userId }, cancellationToken);

    public async Task<SearchResult> SearchAsync(string query, Guid agentId, AuthorizationContext authorization, CancellationToken cancellationToken = default)
    {
        var filters = ComposeFilters(agentId, authorization);

        var result = await _kernelMemory.SearchAsync(
            query: query,
            index: IndexFor(agentId),
            filters: filters,
            limit: 3,
            cancellationToken: cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "KernelMemoryKnowledge.SearchAsync: {Query}, user:{UserId}, anonymous:{IsAnonymous}, roles:{Roles}, tags:{Tags}, bu:{BusinessUnits} => {Result}",
                query,
                authorization.UserId,
                authorization.IsAnonymous,
                string.Join(", ", authorization.Roles),
                string.Join(", ", authorization.AllowedTags),
                string.Join(", ", authorization.BusinessUnits),
                result.ToJson());
        }
        return result;
    }

    public Task<string> ImportWebPageAsync(string url, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
        => ImportWebPageAsync(url, agentId, DocumentPermissionMetadata.FromTags(tags), cancellationToken);

    public async Task<string> ImportWebPageAsync(string url, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Invalid URL provided.", nameof(url));
        }

        var tagCollection = ComposeTags(agentId, permissions);
        AddCitationTags(tagCollection, url, "web", sourceUrl: url);
        var documentId = await _kernelMemory.ImportWebPageAsync(url, index: IndexFor(agentId), tags: tagCollection, cancellationToken: cancellationToken);
        return documentId;
    }

    public Task<string> ImportTextContentAsync(string content, string fileName, Guid agentId, List<string> tags, CancellationToken cancellationToken = default)
        => ImportTextContentAsync(content, fileName, agentId, DocumentPermissionMetadata.FromTags(tags), cancellationToken);

    public async Task<string> ImportTextContentAsync(string content, string fileName, Guid agentId, DocumentPermissionMetadata permissions, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        }

        var tagCollection = ComposeTags(agentId, permissions);
        AddCitationTags(tagCollection, fileName, "text", sourcePath: fileName);

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
        ValidateAgentId(agentId);
        return $"agent-{agentId:N}";
    }

    private static void ValidateAgentId(Guid agentId)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("A valid agentId is required to resolve the knowledge index.", nameof(agentId));
        }
    }

    internal static TagCollection ComposeTags(Guid agentId, DocumentPermissionMetadata permissions)
    {
        ValidateAgentId(agentId);
        permissions ??= new DocumentPermissionMetadata();

        var tags = new TagCollection
        {
            { TagNameAgentId, agentId.ToString().ToLower(CultureInfo.InvariantCulture) }
        };

        AddTags(tags, TagNameTags, ExpandPublicTags(permissions.AllowedTags));
        AddTagsOrAny(tags, TagNameRoles, permissions.Roles);
        AddTagsOrAny(tags, TagNameBusinessUnits, permissions.BusinessUnits);
        AddTagsOrAny(tags, TagNameCountries, permissions.Countries);
        AddTagsOrAny(tags, TagNameLegalEntities, permissions.LegalEntities);
        // Relevance metadata for future level-aware retrieval; not an access axis yet.
        AddTags(tags, TagNameApplicableTo, permissions.ApplicableLevels.Concat(permissions.ApplicableTo));
        AddTagsOrAny(tags, TagNameSensitivity, [permissions.SensitivityLevel]);
        AddTags(tags, TagNameApprovalStatus, [string.IsNullOrWhiteSpace(permissions.ApprovalStatus) ? ApprovalStatus.Approved.ToString() : permissions.ApprovalStatus]);

        return tags;
    }

    internal static List<MemoryFilter> ComposeFilters(Guid agentId, AuthorizationContext authorization)
    {
        ValidateAgentId(agentId);
        if (authorization is null)
        {
            return [DenyAllFilter(agentId)];
        }

        var allowedTags = ExpandPublicTags(authorization.AllowedTags);
        if (authorization.IsAnonymous)
        {
            allowedTags = allowedTags.Append(DtoConstants.PublicAllTagValue).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var anonymousSensitivity = WithAny(["public"]);
            return [BuildFilter(agentId, [(TagNameTags, allowedTags), (TagNameSensitivity, anonymousSensitivity), (TagNameApprovalStatus, [authorization.ApprovalStatus.ToLower(CultureInfo.InvariantCulture)])])];
        }

        var hasAllowedTags = allowedTags.Count > 0;
        allowedTags = allowedTags.Append(DtoConstants.PublicAllTagValue).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var roles = WithAny(authorization.Roles);
        var businessUnits = WithAny(authorization.BusinessUnits);
        var countries = WithAny(authorization.Countries);
        var legalEntities = WithAny(authorization.LegalEntities);
        var sensitivityLevels = WithAny(ExpandSensitivityLevels(authorization.SensitivityLevel));
        var hasEntitlementAxis =
            hasAllowedTags ||
            HasSpecificValue(roles) ||
            HasSpecificValue(businessUnits) ||
            HasSpecificValue(countries) ||
            HasSpecificValue(legalEntities);

        if (!hasEntitlementAxis)
        {
            return [DenyAllFilter(agentId)];
        }

        var requiredAxes = new List<(string Name, List<string> Values)>();
        AddAxis(requiredAxes, TagNameTags, allowedTags);
        AddAxis(requiredAxes, TagNameRoles, roles);
        AddAxis(requiredAxes, TagNameBusinessUnits, businessUnits);
        AddAxis(requiredAxes, TagNameCountries, countries);
        AddAxis(requiredAxes, TagNameLegalEntities, legalEntities);
        AddAxis(requiredAxes, TagNameSensitivity, sensitivityLevels);
        AddAxis(requiredAxes, TagNameApprovalStatus, [authorization.ApprovalStatus.ToLower(CultureInfo.InvariantCulture)]);

        return [BuildFilter(agentId, requiredAxes)];
    }

    private static void AddAxis(List<(string Name, List<string> Values)> axes, string name, IEnumerable<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Count > 0)
        {
            axes.Add((name, normalized));
        }
    }

    private static MemoryFilter BuildFilter(Guid agentId, IReadOnlyList<(string Name, List<string> Values)> axes)
    {
        var filter = AgentScopedFilter(agentId);
        foreach (var axis in axes)
        {
            filter.Add(axis.Name, axis.Values.Cast<string?>().ToList());
        }

        return filter;
    }

    private static MemoryFilter AgentScopedFilter(Guid agentId)
    {
        var filter = new MemoryFilter();
        filter.Add(TagNameAgentId, agentId.ToString().ToLower(CultureInfo.InvariantCulture));
        return filter;
    }

    private static MemoryFilter DenyAllFilter(Guid agentId)
    {
        var filter = AgentScopedFilter(agentId);
        filter.Add(TagNameTags, DenyAllTagValue);
        return filter;
    }

    private static void AddTags(TagCollection tags, string name, IEnumerable<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Count > 0)
        {
            tags.Add(name, normalized.Cast<string?>().ToList());
        }
    }

    private static void AddCitationTags(
        TagCollection tags,
        string documentName,
        string sourceType,
        string? sourcePath = null,
        string? sourceUrl = null)
    {
        AddRawTag(tags, TagNameDocumentName, documentName);
        AddRawTag(tags, TagNameSourceType, sourceType);
        // For uploaded/text documents this currently mirrors documentName; future
        // document sources can set a folder path or connector path here.
        AddRawTag(tags, TagNameSourcePath, sourcePath);
        AddRawTag(tags, TagNameSourceUrl, sourceUrl);
    }

    private static void AddRawTag(TagCollection tags, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            tags.Add(name, [value.Trim()]);
        }
    }

    private static void AddTagsOrAny(TagCollection tags, string name, IEnumerable<string?>? values)
    {
        var normalized = NormalizeList(values);
        tags.Add(name, (normalized.Count == 0 ? [AnyTagValue] : normalized).Cast<string?>().ToList());
    }

    private static List<string> WithAny(IEnumerable<string?>? values)
    {
        return NormalizeList(values)
            .Append(AnyTagValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasSpecificValue(IEnumerable<string> values)
    {
        return values.Any(x => !string.Equals(x, AnyTagValue, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ExpandPublicTags(IEnumerable<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Any(x =>
                string.Equals(x, DtoConstants.PublicAllTagValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, DtoConstants.PublicTagId, StringComparison.OrdinalIgnoreCase)))
        {
            normalized.Add(DtoConstants.PublicAllTagValue);
        }

        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> NormalizeList(IEnumerable<string?>? values)
    {
        return values?
            .Select(Normalize)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLower(CultureInfo.InvariantCulture);
    }

    private static List<string> ExpandSensitivityLevels(string? sensitivityLevel)
    {
        return Normalize(sensitivityLevel) switch
        {
            null => [],
            "public" => ["public"],
            "internal" => ["public", "internal"],
            "confidential" => ["public", "internal", "confidential"],
            var value => [value]
        };
    }
}
