using System.Text.Json.Serialization;

namespace AskHR.Common.Dtos.Answers;

[JsonConverter(typeof(JsonStringEnumConverter<AskHrStreamEventType>))]
public enum AskHrStreamEventType
{
    Token,
    Citation,
    Done,
    Error,
    Handoff
}

public sealed record AskHrStreamEvent(
    AskHrStreamEventType Type,
    string? Content = null,
    AnswerCitationDto? Citation = null,
    AskHrAnswerResponse? Answer = null,
    string? ErrorCode = null,
    string? Severity = null,
    Guid? ConversationId = null,
    Guid? MessageId = null,
    string? HandoffId = null);
