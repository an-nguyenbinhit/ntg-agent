namespace AskHR.Orchestrator.Services.Knowledge;

public interface IReingestTool
{
    Task<ReingestMigrationSummary> RunAsync(ReingestMigrationRequest request, CancellationToken cancellationToken = default);
}

public sealed record ReingestMigrationRequest(
    Guid? AgentId = null,
    bool? DryRun = null,
    List<string>? DefaultRoles = null,
    List<string>? DefaultBusinessUnits = null,
    string? DefaultSensitivityLevel = null,
    List<Guid>? DefaultTagIds = null);

public sealed record ReingestMigrationSummary(
    bool DryRun,
    int Scanned,
    int Reindexed,
    int Failed,
    IReadOnlyList<ReingestMigrationItem> Items);

public sealed record ReingestMigrationItem(
    Guid DocumentId,
    string DocumentName,
    string? PreviousKnowledgeDocId,
    string? CurrentKnowledgeDocId,
    bool Reindexed,
    string? Error);
