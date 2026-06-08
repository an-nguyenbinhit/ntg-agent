using AskHR.Common.Dtos.ModelRouting;

namespace AskHR.Common.Dtos.Answers;

public sealed record AnswerAuditMetadataDto(
    ModelCapability Capability,
    string Provider,
    string Model,
    string? RouteName,
    string RetrievalStrategy,
    int CitationCount,
    string? FallbackReason,
    long LatencyMs);
