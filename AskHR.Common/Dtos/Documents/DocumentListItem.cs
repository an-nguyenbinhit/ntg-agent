using System.Globalization;

namespace AskHR.Common.Dtos.Documents;

public record DocumentListItem (
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Tags,
    List<string> Roles,
    List<string> BusinessUnits,
    string? SensitivityLevel,
    string? Owner,
    string? Version,
    DateTime? EffectiveDate,
    DateTime? ExpiredDate,
    List<string> Countries,
    List<string> LegalEntities,
    List<string> ApplicableLevels,
    IngestStatus IngestStatus,
    string? IngestErrorMessage,
    ApprovalStatus ApprovalStatus,
    DateTime? NextReviewDate)
{
    public string FormattedUpdatedAt => UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
};
