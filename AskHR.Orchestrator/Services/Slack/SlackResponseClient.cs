using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AskHR.Common.Dtos.Answers;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.Slack;

public sealed class SlackResponseClient : ISlackResponseClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SlackOptions> _options;
    private readonly ILogger<SlackResponseClient> _logger;

    public SlackResponseClient(HttpClient httpClient, IOptions<SlackOptions> options, ILogger<SlackResponseClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PostAnswerAsync(string channel, string threadTs, AskHrAnswerResponse answer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.BotToken))
        {
            _logger.LogInformation("Slack bot token is not configured; skipping chat.postMessage for channel {Channel}", channel);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat.postMessage");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Value.BotToken);
        request.Content = JsonContent.Create(new
        {
            channel,
            thread_ts = threadTs,
            text = answer.AnswerText
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Slack chat.postMessage failed with {StatusCode}: {Body}", response.StatusCode, body);
        }
    }

    public async Task<string?> PostStatusMessageAsync(string channel, string threadTs, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.BotToken))
        {
            _logger.LogInformation("Slack bot token is not configured; skipping status message for channel {Channel}", channel);
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat.postMessage");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Value.BotToken);
        request.Content = JsonContent.Create(new
        {
            channel,
            thread_ts = threadTs,
            text
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Slack chat.postMessage failed with {StatusCode}: {Body}", response.StatusCode, body);
            return null;
        }

        return TryReadMessageTimestamp(body);
    }

    public async Task UpdateMessageAsync(string channel, string ts, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.BotToken))
        {
            _logger.LogInformation("Slack bot token is not configured; skipping chat.update for channel {Channel}", channel);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat.update");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Value.BotToken);
        request.Content = JsonContent.Create(new
        {
            channel,
            ts,
            text
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Slack chat.update failed with {StatusCode}: {Body}", response.StatusCode, body);
        }
    }

    private string? TryReadMessageTimestamp(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("ok", out var okProperty) && okProperty.GetBoolean() &&
                document.RootElement.TryGetProperty("ts", out var tsProperty))
            {
                return tsProperty.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Slack chat.postMessage response: {Body}", responseBody);
        }

        return null;
    }
}
