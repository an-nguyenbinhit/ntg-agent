namespace AskHR.Common.Dtos.Audit;

public sealed record FeedbackEventDto(
    Guid FeedbackId,
    Guid MessageId,
    Guid? UserId,
    bool IsAnonymous,
    string Rating,
    string? CommentMasked,
    string? Topic,
    string? SeverityCandidate,
    string Status,
    DateTimeOffset CreatedAt);
