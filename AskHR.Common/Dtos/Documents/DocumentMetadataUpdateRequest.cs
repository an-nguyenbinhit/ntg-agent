namespace AskHR.Common.Dtos.Documents;

public record DocumentMetadataUpdateRequest(List<string>? Roles, List<string>? BusinessUnits, string? SensitivityLevel, List<string>? Tags = null);
