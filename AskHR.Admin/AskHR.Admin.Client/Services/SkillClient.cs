using AskHR.Common.Dtos.Agents;
using System.Net.Http.Json;

namespace AskHR.Admin.Client.Services;

public class SkillClient(HttpClient httpClient)
{
    public async Task<List<SkillDto>?> GetSkillsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<SkillDto>>("api/Skills");
    }

    public async Task<SkillDto?> GetSkillAsync(Guid id)
    {
        return await httpClient.GetFromJsonAsync<SkillDto>($"api/Skills/{id}");
    }

    public async Task<SkillDto?> CreateSkillAsync(SkillDto skill)
    {
        var response = await httpClient.PostAsJsonAsync("api/Skills", skill);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SkillDto>();
    }

    public async Task UpdateSkillAsync(Guid id, SkillDto skill)
    {
        var response = await httpClient.PutAsJsonAsync($"api/Skills/{id}", skill);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSkillAsync(Guid id)
    {
        var response = await httpClient.DeleteAsync($"api/Skills/{id}");
        response.EnsureSuccessStatusCode();
    }
}
