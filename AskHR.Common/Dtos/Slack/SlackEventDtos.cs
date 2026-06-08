using System.Text.Json.Serialization;

namespace AskHR.Common.Dtos.Slack;

public sealed record SlackEventsRequest
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("challenge")]
    public string? Challenge { get; init; }

    [JsonPropertyName("event_id")]
    public string? EventId { get; init; }

    [JsonPropertyName("event_time")]
    public long? EventTime { get; init; }

    [JsonPropertyName("event")]
    public SlackEventPayload? Event { get; init; }
}

public sealed record SlackEventPayload
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("user")]
    public string? User { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("channel")]
    public string? Channel { get; init; }

    [JsonPropertyName("ts")]
    public string? Ts { get; init; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTs { get; init; }

    [JsonPropertyName("bot_id")]
    public string? BotId { get; init; }
}

public sealed record SlackGatewayResponse(
    bool Accepted,
    string? Channel,
    string? ThreadTs,
    string? AnswerText,
    string? Reason);
