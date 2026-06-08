using DtoConstants = AskHR.Common.Dtos.Constants.Constants;

namespace AskHR.Common.Dtos.Security;

public sealed record AuthorizationContext
{
    public Guid? UserId { get; init; }

    public bool IsAnonymous { get; init; }

    public List<string> Roles { get; init; } = [];

    public List<string> AllowedTags { get; init; } = [];

    public List<string> BusinessUnits { get; init; } = [];

    public List<string> Countries { get; init; } = [];

    public List<string> LegalEntities { get; init; } = [];

    public string? Level { get; init; }

    public string? SensitivityLevel { get; init; }

    public static AuthorizationContext Anonymous(IEnumerable<string>? allowedTags = null)
    {
        var tags = (allowedTags ?? [])
            .Append(DtoConstants.PublicAllTagValue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorizationContext
        {
            IsAnonymous = true,
            Roles = ["anonymous"],
            AllowedTags = tags,
            SensitivityLevel = "Public"
        };
    }
}
