namespace AskHR.Orchestrator.Services.Knowledge;

public sealed class ReingestMigrationOptions
{
    public const string SectionName = "ReingestMigration";

    public bool DryRun { get; init; } = true;

    public List<string> DefaultRoles { get; init; } = ["Employee"];

    public List<string> DefaultBusinessUnits { get; init; } = ["All"];

    public string DefaultSensitivityLevel { get; init; } = "Public";
}

