using AskHR.Common.Dtos.Knowledge;
using System.Net.Http.Json;

namespace AskHR.Admin.Client.Services;

public class KnowledgeClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    // Get the active knowledge backend info (vector store / embedding / text models).
    public async Task<KnowledgeBackendInfoDto?> GetBackendInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/knowledge/backend-info", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("User does not have Admin role");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<KnowledgeBackendInfoDto>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to fetch knowledge backend info: {ex.Message}", ex);
        }
    }
}
