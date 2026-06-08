using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.Slack;

public sealed class MemorySlackEventDeduplicator : ISlackEventDeduplicator
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<SlackOptions> _options;

    public MemorySlackEventDeduplicator(IMemoryCache cache, IOptions<SlackOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool TryAccept(string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return true;
        }

        var key = $"slack-event:{eventId}";
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, TimeSpan.FromMinutes(Math.Max(1, _options.Value.EventDeduplicationMinutes)));
        return true;
    }
}
