using System.Net.Http.Headers;
using System.Net.Http.Json;
using AskHR.Common.Dtos.Answers;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Channels.Teams;

public sealed class TeamsResponseClient : ITeamsResponseClient
{
    private const string AdaptiveCardContentType = "application/vnd.microsoft.card.adaptive";

    private readonly HttpClient _httpClient;
    private readonly ITeamsAdaptiveCardFormatter _cardFormatter;
    private readonly IOptions<TeamsOptions> _options;
    private readonly ILogger<TeamsResponseClient> _logger;

    public TeamsResponseClient(
        HttpClient httpClient,
        ITeamsAdaptiveCardFormatter cardFormatter,
        IOptions<TeamsOptions> options,
        ILogger<TeamsResponseClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cardFormatter = cardFormatter ?? throw new ArgumentNullException(nameof(cardFormatter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> PostStatusMessageAsync(TeamsActivity sourceActivity, string text, CancellationToken cancellationToken = default)
    {
        return await PostReplyAsync(sourceActivity, new
        {
            type = "message",
            text
        }, cancellationToken);
    }

    public async Task PostAnswerAsync(TeamsActivity sourceActivity, AskHrAnswerResponse answer, CancellationToken cancellationToken = default)
    {
        var card = _cardFormatter.BuildAnswerCard(answer);
        await PostReplyAsync(sourceActivity, new
        {
            type = "message",
            text = answer.AnswerText,
            attachments = new[]
            {
                new
                {
                    contentType = AdaptiveCardContentType,
                    content = card
                }
            }
        }, cancellationToken);
    }

    private async Task<string?> PostReplyAsync(TeamsActivity sourceActivity, object payload, CancellationToken cancellationToken)
    {
        var token = _options.Value.BotFrameworkToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("Teams Bot Framework token is not configured; skipping reply for activity {ActivityId}", sourceActivity.Id);
            return null;
        }

        var serviceUrl = sourceActivity.ServiceUrl?.TrimEnd('/');
        var conversationId = sourceActivity.Conversation?.Id;
        var activityId = sourceActivity.Id;
        if (string.IsNullOrWhiteSpace(serviceUrl) ||
            string.IsNullOrWhiteSpace(conversationId) ||
            string.IsNullOrWhiteSpace(activityId))
        {
            _logger.LogWarning("Teams activity is missing serviceUrl, conversation id, or activity id.");
            return null;
        }

        var requestUri = $"{serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Teams Bot Framework reply failed with {StatusCode}: {Body}", response.StatusCode, body);
            return null;
        }

        var responseBody = await response.Content.ReadFromJsonAsync<TeamsReplyResponse>(cancellationToken: cancellationToken);
        return responseBody?.Id;
    }

    private sealed record TeamsReplyResponse(string? Id);
}

