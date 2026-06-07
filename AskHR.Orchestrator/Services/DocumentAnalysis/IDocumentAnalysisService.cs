using AskHR.Orchestrator.Dtos;

namespace AskHR.Orchestrator.Services.DocumentAnalysis;

public interface IDocumentAnalysisService
{
    /// <summary>
    /// Indicates whether Azure Document Intelligence is enabled and configured.
    /// When false, <see cref="ExtractDocumentData"/> should return an empty list.
    /// </summary>
    bool IsEnabled { get; }

    Task<List<string>> ExtractDocumentData(IEnumerable<UploadItemForm> uploadItemContents);
}