using AskHR.Common.Dtos.Audit;
using System.Net.Http.Json;

namespace AskHR.Admin.Client.Services;

public class FeedbackClient(HttpClient httpClient)
{
    public async Task<FeedbackEventQueryResult?> GetEventsAsync(
        string? severity = null,
        string? status = null,
        string? rating = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        AddQuery(query, "severity", severity);
        AddQuery(query, "status", status);
        AddQuery(query, "rating", rating);
        AddQuery(query, "from", from?.ToString("O"));
        AddQuery(query, "to", to?.ToString("O"));

        return await httpClient.GetFromJsonAsync<FeedbackEventQueryResult>($"api/FeedbackAdmin/events?{string.Join("&", query)}");
    }

    public async Task UpdateEventAsync(Guid id, FeedbackEventUpdateDto update)
    {
        var response = await httpClient.PutAsJsonAsync($"api/FeedbackAdmin/events/{id}", update);
        response.EnsureSuccessStatusCode();
    }

    private static void AddQuery(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }
}
