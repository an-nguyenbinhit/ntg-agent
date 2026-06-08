namespace AskHR.Common.Dtos.Answers;

public sealed record AskHrAnswerResponse(
    string AnswerText,
    IReadOnlyList<AnswerCitationDto> Citations,
    double Confidence,
    string? FallbackReason,
    AnswerAuditMetadataDto AuditMetadata);
