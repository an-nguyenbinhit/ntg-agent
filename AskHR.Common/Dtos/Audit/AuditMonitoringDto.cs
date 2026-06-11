namespace AskHR.Common.Dtos.Audit;

public sealed record AuditMonitoringDto(
    int TotalEvents,
    long TotalPromptTokens,
    long TotalCompletionTokens,
    long TotalTokens,
    double AverageLatencyMs,
    Dictionary<string, long> TokensByModel,
    Dictionary<string, double> AverageLatencyByModel,
    Dictionary<string, int> EventsByChannel,
    Dictionary<string, int> EventsByType);
