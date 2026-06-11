using AskHR.Common.Dtos.TokenUsage;

namespace AskHR.Common.Dtos.Audit;

public sealed record FeedbackEventAdminDto(
    Guid Id,
    Guid MessageId,
    Guid? UserId,
    bool IsAnonymous,
    string Rating,
    string? CommentMasked,
    string? Topic,
    string? SeverityCandidate,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record FeedbackEventUpdateDto(
    string? SeverityCandidate,
    string Status);

public sealed record FeedbackEventQueryResult(
    PagedResult<FeedbackEventAdminDto> Events,
    Dictionary<string, int> CountBySeverity,
    Dictionary<string, int> CountByStatus,
    Dictionary<string, int> CountByRating);
