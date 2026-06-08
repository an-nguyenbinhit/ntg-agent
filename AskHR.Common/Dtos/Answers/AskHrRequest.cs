namespace AskHR.Common.Dtos.Answers;

public sealed record AskHrRequest(
    Guid AgentId,
    string Question,
    string Channel = "api",
    string? ChannelUserId = null,
    string? ThreadId = null,
    string? SessionId = null,
    Dictionary<string, string>? Metadata = null);
