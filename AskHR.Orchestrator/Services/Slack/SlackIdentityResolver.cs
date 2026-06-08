using System.Text.Json.Serialization;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.Slack;

public sealed class SlackIdentityResolver : ISlackIdentityResolver
{
    private static readonly TimeSpan EmailCacheDuration = TimeSpan.FromHours(1);

    private readonly HttpClient _httpClient;
    private readonly AgentDbContext _dbContext;
    private readonly IRbacService _rbacService;
    private readonly IMemoryCache _cache;
    private readonly IOptions<SlackOptions> _options;
    private readonly ILogger<SlackIdentityResolver> _logger;

    public SlackIdentityResolver(
        HttpClient httpClient,
        AgentDbContext dbContext,
        IRbacService rbacService,
        IMemoryCache cache,
        IOptions<SlackOptions> options,
        ILogger<SlackIdentityResolver> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthorizationContext> ResolveAsync(string? slackUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slackUserId))
        {
            return AuthorizationContext.Anonymous();
        }

        var email = await ResolveEmailAsync(slackUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthorizationContext.Anonymous();
        }

        // SQL Server's default collation is case-insensitive, matching Email's storage and lookup elsewhere.
        var normalizedEmail = email.Trim();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        return await _rbacService.ResolveAsync(user?.Id, cancellationToken);
    }

    private async Task<string?> ResolveEmailAsync(string slackUserId, CancellationToken cancellationToken)
    {
        var cacheKey = $"slack-user-email:{slackUserId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedEmail))
        {
            return cachedEmail;
        }

        if (string.IsNullOrWhiteSpace(_options.Value.BotToken))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"users.info?user={Uri.EscapeDataString(slackUserId)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Value.BotToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Slack users.info failed with {StatusCode} for user {SlackUserId}", response.StatusCode, slackUserId);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<SlackUsersInfoResponse>(cancellationToken: cancellationToken);
            var email = payload?.Ok == true ? payload.User?.Profile?.Email : null;

            _cache.Set(cacheKey, email, EmailCacheDuration);
            return email;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Failed to resolve Slack user email for {SlackUserId}", slackUserId);
            return null;
        }
    }

    private sealed record SlackUsersInfoResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }

        [JsonPropertyName("user")]
        public SlackUser? User { get; init; }
    }

    private sealed record SlackUser
    {
        [JsonPropertyName("profile")]
        public SlackUserProfile? Profile { get; init; }
    }

    private sealed record SlackUserProfile
    {
        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }
}
