namespace AskHR.Orchestrator.Models.Configuration;

/// <summary>
/// Configuration for the document ingestion source (UoB-02). Supports the default
/// "AdminUpload" mode (documents only enter via the Admin UI) and "WatchFolder" mode,
/// where the document watcher service monitors <see cref="WatchPath"/> and
/// automatically (re-)indexes new or changed files.
/// </summary>
public class IngestionSourceOptions
{
    public const string SectionName = "IngestionSource";
    public const string WatchFolderMode = "WatchFolder";

    /// <summary>"AdminUpload" (default) or "WatchFolder".</summary>
    public string Mode { get; set; } = "AdminUpload";

    /// <summary>Local directory monitored for new/changed knowledge files when Mode is "WatchFolder".</summary>
    public string? WatchPath { get; set; }

    /// <summary>Agent that watched documents are ingested for. Falls back to the published default agent when unset.</summary>
    public Guid? AgentId { get; set; }

    /// <summary>Optional folder that watched documents are placed in.</summary>
    public Guid? FolderId { get; set; }

    /// <summary>User recorded as creator/updater for watcher-ingested documents. Defaults to the seeded admin user.</summary>
    public Guid SystemUserId { get; set; } = new("e0afe23f-b53c-4ad8-b718-cb4ff5bb9f71");

    /// <summary>Quiet period after the last file-system event before a file is ingested, coalescing duplicate events.</summary>
    public int DebounceSeconds { get; set; } = 3;

    public bool IsWatchFolderEnabled =>
        string.Equals(Mode, WatchFolderMode, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(WatchPath);
}
