using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Channels.Teams;

public sealed class MemoryTeamsActivityDeduplicator : ITeamsActivityDeduplicator
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<TeamsOptions> _options;

    public MemoryTeamsActivityDeduplicator(IMemoryCache cache, IOptions<TeamsOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool TryAccept(string? activityId)
    {
        if (string.IsNullOrWhiteSpace(activityId))
        {
            return true;
        }

        var key = $"teams-activity:{activityId}";
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, TimeSpan.FromSeconds(_options.Value.PipelineTimeoutSeconds + 300));
        return true;
    }
}

