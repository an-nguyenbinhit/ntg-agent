using AskHR.Common.Dtos.Audit;
using System.Net.Http.Json;

namespace AskHR.Admin.Client.Services;

public class MonitoringClient(HttpClient httpClient)
{
    public async Task<AuditMonitoringDto?> GetAuditSummaryAsync(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var query = new List<string>();
        AddQuery(query, "from", from?.ToString("O"));
        AddQuery(query, "to", to?.ToString("O"));

        var url = query.Count == 0
            ? "api/Monitoring/audit-summary"
            : $"api/Monitoring/audit-summary?{string.Join("&", query)}";

        return await httpClient.GetFromJsonAsync<AuditMonitoringDto>(url);
    }

    private static void AddQuery(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }
}
