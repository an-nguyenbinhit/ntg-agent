namespace AskHR.Orchestrator.Services.Answers;

public sealed class AnswerPipelineOptions
{
    public double MinRelevance { get; init; } = 0.1;

    public int MaxFacts { get; init; } = 6;

    public int MaxSnippetCharacters { get; init; } = 1200;

    public string RetrievalStrategy { get; init; } = "StandardRag";

    public string FallbackAnswer { get; init; } = "Tôi không tìm thấy thông tin đủ tin cậy trong tài liệu HR hiện có. Vui lòng liên hệ HR để được xác nhận.";
}
