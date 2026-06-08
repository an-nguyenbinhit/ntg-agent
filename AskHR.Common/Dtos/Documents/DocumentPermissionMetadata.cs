namespace AskHR.Common.Dtos.Documents;

public sealed record DocumentPermissionMetadata
{
    public List<string> AllowedTags { get; init; } = [];

    public List<string> Roles { get; init; } = [];

    public List<string> BusinessUnits { get; init; } = [];

    public List<string> Countries { get; init; } = [];

    public List<string> LegalEntities { get; init; } = [];

    public List<string> ApplicableTo { get; init; } = [];

    public string? SensitivityLevel { get; init; }

    public static DocumentPermissionMetadata FromTags(IEnumerable<string>? tags)
    {
        return new DocumentPermissionMetadata
        {
            AllowedTags = tags?.ToList() ?? []
        };
    }

    public DocumentPermissionMetadata WithAllowedTags(IEnumerable<string>? tags)
    {
        return this with
        {
            AllowedTags = tags?.ToList() ?? []
        };
    }
}
