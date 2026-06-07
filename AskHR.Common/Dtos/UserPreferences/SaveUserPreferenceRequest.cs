using System.ComponentModel.DataAnnotations;

namespace AskHR.Common.Dtos.UserPreferences;

public record SaveUserPreferenceRequest(
    [Required] Guid SelectedAgentId,
    bool? IsLongTermMemoryEnabled = null,
    bool? IsMemorySearchEnabled = null,
    string? AppearanceTheme = null,
    string? AccentColor = null
);
