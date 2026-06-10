using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AskHR.Common.Dtos.Services;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Configuration;
using System.Collections.Concurrent;

namespace AskHR.Orchestrator.Services.Knowledge;

/// <summary>
/// Background service for "WatchFolder" ingestion mode (UoB-02 / S-0203). Monitors the configured
/// <see cref="IngestionSourceOptions.WatchPath"/> with a <see cref="FileSystemWatcher"/> and triggers
/// re-indexing through <see cref="IDocumentIngestionService"/> when a knowledge file is created or
/// changed — no periodic cron scan involved. Events are debounced per file so that bursts of
/// file-system notifications (e.g. while a file is still being written) result in a single ingestion.
/// </summary>
public sealed class DocumentWatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionSourceOptions _options;
    private readonly ILogger<DocumentWatcherService> _logger;

    // Path of a pending file mapped to the UTC time its debounce window expires.
    private readonly ConcurrentDictionary<string, DateTime> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);

    public DocumentWatcherService(IServiceScopeFactory scopeFactory, IOptions<IngestionSourceOptions> options, ILogger<DocumentWatcherService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal int PendingFileCount => _pendingFiles.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsWatchFolderEnabled)
        {
            _logger.LogInformation("Document watcher is disabled (IngestionSource:Mode is '{Mode}').", _options.Mode);
            return;
        }

        if (!Directory.Exists(_options.WatchPath))
        {
            _logger.LogWarning("Document watcher not started: watch path '{WatchPath}' does not exist.", _options.WatchPath);
            return;
        }

        using var watcher = new FileSystemWatcher(_options.WatchPath!)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        watcher.Created += (_, e) => QueueFile(e.FullPath);
        watcher.Changed += (_, e) => QueueFile(e.FullPath);
        watcher.Renamed += (_, e) => QueueFile(e.FullPath);
        watcher.Error += (_, e) => _logger.LogError(e.GetException(), "Document watcher error on '{WatchPath}'.", _options.WatchPath);
        watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Document watcher started on '{WatchPath}'.", _options.WatchPath);

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessDueFilesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }
    }

    internal void QueueFile(string fullPath)
    {
        if (!FileTypeService.IsSupportedKnowledgeFile(fullPath))
        {
            return;
        }

        _pendingFiles[fullPath] = DateTime.UtcNow.AddSeconds(_options.DebounceSeconds);
    }

    internal async Task ProcessDueFilesAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _pendingFiles)
        {
            if (entry.Value > now)
            {
                continue;
            }

            // Skip if a newer file-system event re-queued the file after we snapshotted it.
            if (!_pendingFiles.TryRemove(KeyValuePair.Create(entry.Key, entry.Value)))
            {
                continue;
            }

            await ProcessFileAsync(entry.Key, ct);
        }
    }

    internal async Task ProcessFileAsync(string fullPath, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                return;
            }

            await using var stream = await OpenWithRetryAsync(fullPath, ct);
            if (stream is null)
            {
                _logger.LogWarning("Could not open '{FilePath}' for ingestion; the file stayed locked.", fullPath);
                return;
            }

            if (stream.Length == 0)
            {
                // The file was created but not written yet; a Changed event will re-queue it once it has content.
                _logger.LogDebug("Skipping empty file '{FilePath}'.", fullPath);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var agentId = await ResolveAgentIdAsync(scope.ServiceProvider, ct);
            if (agentId is null)
            {
                _logger.LogWarning("Skipping '{FilePath}': no IngestionSource:AgentId configured and no published agent found.", fullPath);
                return;
            }

            var ingestionService = scope.ServiceProvider.GetRequiredService<IDocumentIngestionService>();
            var outcome = await ingestionService.IngestFileAsync(stream, Path.GetFileName(fullPath), agentId.Value, _options.FolderId, _options.SystemUserId, ct);

            _logger.LogInformation("Document watcher processed '{FilePath}' with outcome {Outcome}.", fullPath, outcome);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document watcher failed to process '{FilePath}'.", fullPath);
        }
    }

    private async Task<Guid?> ResolveAgentIdAsync(IServiceProvider services, CancellationToken ct)
    {
        if (_options.AgentId.HasValue)
        {
            return _options.AgentId.Value;
        }

        var dbContext = services.GetRequiredService<AgentDbContext>();
        var agentId = await dbContext.Agents
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        return agentId;
    }

    private static async Task<FileStream?> OpenWithRetryAsync(string fullPath, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (FileNotFoundException)
            {
                // File was removed between the event and processing.
                return null;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                // File is likely still being written; give the writer time to finish.
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
