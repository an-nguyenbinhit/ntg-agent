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
    IngestStatus IngestStatus,
    string? IngestErrorMessage)
{
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    public string FormattedUpdatedAt => UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
};
