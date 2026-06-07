using Microsoft.SemanticKernel.Data;

namespace AskHR.AITools.SearchOnlineTool.Services;

public interface ITextSearchService
{
    IAsyncEnumerable<TextSearchResult> SearchAsync(string query, int top);
}
