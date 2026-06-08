namespace AskHR.Common.Dtos.Answers;

public sealed record AskHrAnswerResponse(
    string AnswerText,
    IReadOnlyList<AnswerCitationDto> Citations,
    double Confidence,
    string? FallbackReason,
    AnswerAuditMetadataDto AuditMetadata,
    Guid? ConversationId = null,
    Guid? MessageId = null,
    bool IsHandoff = false,
    string? HandoffId = null);
