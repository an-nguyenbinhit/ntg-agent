using System.Globalization;
using AskHR.Common.Dtos.Constants;
using AskHR.Common.Dtos.Documents;
using AskHR.Common.Dtos.Security;
using AskHR.Common.Dtos.Services;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Controllers;

[Route("api/documents/download")]
[ApiController]
[Authorize]
public sealed class DocumentsDownloadController : ControllerBase
{
    private const string AnyTagValue = "__any__";
    private readonly AgentDbContext _agentDbContext;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IIdentityResolver _identityResolver;
    private readonly IRbacService _rbacService;
    private static readonly FileExtensionContentTypeProvider MimeProvider = new();

    public DocumentsDownloadController(
        AgentDbContext agentDbContext,
        IKnowledgeService knowledgeService,
        IIdentityResolver identityResolver,
        IRbacService rbacService)
    {
        _agentDbContext = agentDbContext ?? throw new ArgumentNullException(nameof(agentDbContext));
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
    }

    [HttpGet("{agentId:guid}/{id:guid}")]
    public async Task<IActionResult> GetDocumentById(Guid agentId, Guid id, CancellationToken ct)
    {
        var document = await _agentDbContext.Documents
            .AsNoTracking()
            .Include(d => d.DocumentTags)
            .FirstOrDefaultAsync(d => d.Id == id && d.AgentId == agentId, ct);

        if (document is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin"))
        {
            var userId = await _identityResolver.ResolveUserIdAsync(HttpContext, ct);
            if (userId is null)
            {
                return Unauthorized();
            }

            var authorization = await _rbacService.ResolveAsync(userId, ct);
            if (!CanAccessDocument(document, authorization))
            {
                return Forbid();
            }
        }

        return document.Type switch
        {
            DocumentType.File or DocumentType.Text => await HandleKnowledgeFileDownloadAsync(document, agentId, ct),
            DocumentType.WebPage => await HandleWebPageDownloadAsync(document, ct),
            _ => NotFound("Unsupported document type.")
        };
    }

    private async Task<IActionResult> HandleKnowledgeFileDownloadAsync(Document document, Guid agentId, CancellationToken ct)
    {
        if (document.KnowledgeDocId is null)
        {
            return NotFound("No knowledge document id.");
        }

        var fileName = FileTypeService.SanitizeFileName(document.Name);
        var contentType = FileTypeService.GetContentType(fileName);
        var content = await _knowledgeService.ExportDocumentAsync(document.KnowledgeDocId, fileName, agentId, ct);

        if (content is null)
        {
            return NotFound();
        }

        var stream = await content.GetStreamAsync();
        return File(stream, contentType, fileName);
    }

    private async Task<IActionResult> HandleWebPageDownloadAsync(Document document, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(document.Url))
        {
            return NotFound("Webpage URL not found.");
        }

        if (!Uri.TryCreate(document.Url, UriKind.Absolute, out var uri))
        {
            return BadRequest("Invalid webpage URL.");
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(document.Url, ct);
            response.EnsureSuccessStatusCode();

            var headerType = response.Content.Headers.ContentType?.MediaType;
            var inferredType = headerType ?? GetContentTypeFromUrlPath(uri.AbsolutePath);
            var extension = FileTypeService.GetFileExtensionFromContentType(inferredType, uri.ToString());
            var fileName = $"{FileTypeService.SanitizeFileName(document.Name)}{extension}";
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return File(stream, inferredType, fileName);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to download content from URL: {ex.Message}");
        }
    }

    private static bool CanAccessDocument(Document document, AuthorizationContext authorization)
    {
        if (!string.Equals(document.ApprovalStatus.ToString(), authorization.ApprovalStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HasTagAccess(document, authorization)
            && MatchesAxis(document.Roles, authorization.Roles)
            && MatchesAxis(document.BusinessUnits, authorization.BusinessUnits)
            && MatchesAxis(document.Countries, authorization.Countries)
            && MatchesAxis(document.LegalEntities, authorization.LegalEntities)
            && MatchesSensitivity(document.SensitivityLevel, authorization.SensitivityLevel);
    }

    private static bool HasTagAccess(Document document, AuthorizationContext authorization)
    {
        var documentTags = ExpandPublicTags(document.DocumentTags.Select(x => x.TagId.ToString()));
        if (documentTags.Count == 0)
        {
            return false;
        }

        var allowedTags = ExpandPublicTags(authorization.AllowedTags);
        allowedTags.Add(Constants.PublicAllTagValue);
        return documentTags.Intersect(allowedTags, StringComparer.OrdinalIgnoreCase).Any();
    }

    private static bool MatchesAxis(IEnumerable<string>? documentValues, IEnumerable<string>? authorizationValues)
    {
        var required = NormalizeList(documentValues);
        if (required.Count == 0 || required.Contains(AnyTagValue, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var granted = NormalizeList(authorizationValues);
        return required.Intersect(granted, StringComparer.OrdinalIgnoreCase).Any();
    }

    private static bool MatchesSensitivity(string? documentSensitivity, string? authorizationSensitivity)
    {
        var required = Normalize(documentSensitivity);
        if (required is null || string.Equals(required, AnyTagValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ExpandSensitivityLevels(authorizationSensitivity).Contains(required, StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> ExpandPublicTags(IEnumerable<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Any(x =>
                string.Equals(x, Constants.PublicAllTagValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, Constants.PublicTagId, StringComparison.OrdinalIgnoreCase)))
        {
            normalized.Add(Constants.PublicAllTagValue);
        }

        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> NormalizeList(IEnumerable<string>? values)
        => values?
            .Select(Normalize)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLower(CultureInfo.InvariantCulture);

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

    private static string GetContentTypeFromUrlPath(string urlPath)
    {
        var fileName = Path.GetFileName(urlPath);
        if (!string.IsNullOrEmpty(fileName) && MimeProvider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }
}
