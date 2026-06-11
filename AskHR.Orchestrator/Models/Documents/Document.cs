using AskHR.Common.Dtos.Documents;

namespace AskHR.Orchestrator.Models.Documents;

public class Document
{
    public Document()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Hash { get; set; }
    public string? KnowledgeDocId { get; set; }
    public Guid? FolderId { get; set; }
    public Guid AgentId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DocumentType Type { get; set; } = DocumentType.File;

    public string? Owner { get; set; }
    public string? Version { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public List<string> Countries { get; set; } = [];
    public List<string> LegalEntities { get; set; } = [];
    public List<string> ApplicableLevels { get; set; } = [];

    public List<string> Roles { get; set; } = [];
    public List<string> BusinessUnits { get; set; } = [];
    public string? SensitivityLevel { get; set; }
    public IngestStatus IngestStatus { get; set; } = IngestStatus.Pending;
    public string? IngestErrorMessage { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved; // Default to Approved for backward compatibility
    public DateTime? NextReviewDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    // Navigation properties
    public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
}
