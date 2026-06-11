namespace AskHR.Common.Dtos.Documents;

public record DocumentMetadataUpdateRequest(
    List<string>? Roles,
    List<string>? BusinessUnits,
    string? SensitivityLevel,
    string? Owner = null,
    string? Version = null,
    DateTime? EffectiveDate = null,
    DateTime? ExpiredDate = null,
    List<string>? Countries = null,
    List<string>? LegalEntities = null,
    List<string>? ApplicableLevels = null,
    List<string>? Tags = null
);
