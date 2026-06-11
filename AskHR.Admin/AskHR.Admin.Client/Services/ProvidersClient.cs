using AskHR.Common.Dtos.Providers;
using System.Net.Http.Json;

namespace AskHR.Admin.Client.Services;

public class ProvidersClient(HttpClient httpClient)
{
    public async Task<List<ProviderMetadataDto>?> GetProviderMetadataAsync()
        => await httpClient.GetFromJsonAsync<List<ProviderMetadataDto>>("api/ProvidersAdmin/metadata");

    public async Task<List<ModelRouteDto>?> GetModelRoutesAsync()
        => await httpClient.GetFromJsonAsync<List<ModelRouteDto>>("api/ProvidersAdmin/routes");
}
