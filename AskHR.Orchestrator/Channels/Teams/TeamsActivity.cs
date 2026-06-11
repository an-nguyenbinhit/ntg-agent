using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskHR.Orchestrator.Channels.Teams;

public sealed record TeamsActivity
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("serviceUrl")]
    public string? ServiceUrl { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("from")]
    public TeamsChannelAccount? From { get; init; }

    [JsonPropertyName("recipient")]
    public TeamsChannelAccount? Recipient { get; init; }

    [JsonPropertyName("conversation")]
    public TeamsConversationAccount? Conversation { get; init; }

    [JsonPropertyName("channelData")]
    public TeamsChannelData? ChannelData { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record TeamsChannelAccount
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("aadObjectId")]
    public string? AadObjectId { get; init; }

    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; init; }
}

public sealed record TeamsConversationAccount
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("conversationType")]
    public string? ConversationType { get; init; }

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }
}

public sealed record TeamsChannelData
{
    [JsonPropertyName("tenant")]
    public TeamsTenant? Tenant { get; init; }

    [JsonPropertyName("channel")]
    public TeamsChannel? Channel { get; init; }

    [JsonPropertyName("team")]
    public TeamsTeam? Team { get; init; }

    [JsonPropertyName("from")]
    public TeamsChannelAccount? From { get; init; }
}

public sealed record TeamsTenant
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public sealed record TeamsChannel
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public sealed record TeamsTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public sealed record TeamsGatewayResponse(bool Ok, string? ConversationId, string? ActivityId, string Reason);

