using System.Net.Http.Json;
using AskHR.Common.Dtos.Knowledge;

namespace AskHR.Orchestrator.Services.Knowledge;

/// <summary>
/// Typed <see cref="HttpClient"/> gateway that queries the Kernel Memory service for
/// the active knowledge backend. The probe degrades gracefully: any failure surfaces
/// as an unhealthy result rather than throwing, so the Admin panel can still render.
/// </summary>
public sealed class KnowledgeBackendProbe : IKnowledgeBackendProbe
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KnowledgeBackendProbe> _logger;

    public KnowledgeBackendProbe(HttpClient httpClient, ILogger<KnowledgeBackendProbe> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeBackendInfoDto> GetBackendInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _httpClient.GetFromJsonAsync<KnowledgeBackendInfoDto>(
                "backend-info", cancellationToken);

            return info ?? new KnowledgeBackendInfoDto { Healthy = false };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to probe knowledge backend info from Kernel Memory service");
            return new KnowledgeBackendInfoDto { Healthy = false };
        }
    }
}
