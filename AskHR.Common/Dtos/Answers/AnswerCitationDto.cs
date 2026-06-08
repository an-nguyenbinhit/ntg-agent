namespace AskHR.Common.Dtos.Answers;

public sealed record AnswerCitationDto(
    string? DocumentId,
    string DocumentName,
    string SourceType,
    string? SourcePath,
    string? SourceUrl,
    string Snippet,
    double Relevance);
