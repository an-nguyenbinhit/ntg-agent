using Anthropic;
using Anthropic.Core;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed class ProviderChatClientFactory : IChatClientFactory
{
    public IChatClient Create(ResolvedModelRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        return route.Provider switch
        {
            "AzureOpenAI" => CreateAzureOpenAI(route),
            "Anthropic" => CreateAnthropic(route),
            "OpenAI" or "GitHubModel" or "GoogleGemini" => CreateOpenAICompatible(route),
            _ => throw new NotSupportedException($"Model provider '{route.Provider}' is not supported.")
        };
    }

    private static IChatClient CreateOpenAICompatible(ResolvedModelRoute route)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(route.Endpoint))
        {
            options.Endpoint = new Uri(route.Endpoint);
        }

        return new OpenAIClient(new ApiKeyCredential(RequireApiKey(route)), options)
            .GetChatClient(route.Model)
            .AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAI(ResolvedModelRoute route)
    {
        if (string.IsNullOrWhiteSpace(route.Endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI route requires an endpoint.");
        }

        return new AzureOpenAIClient(new Uri(route.Endpoint), new ApiKeyCredential(RequireApiKey(route)))
            .GetChatClient(route.Model)
            .AsIChatClient();
    }

    private static IChatClient CreateAnthropic(ResolvedModelRoute route)
        => new AnthropicClient(new ClientOptions { ApiKey = RequireApiKey(route) })
            .AsIChatClient(defaultModelId: route.Model);

    private static string RequireApiKey(ResolvedModelRoute route)
        => string.IsNullOrWhiteSpace(route.ApiKey)
            ? throw new InvalidOperationException($"Route '{route.RouteName ?? route.Provider}' requires an API key.")
            : route.ApiKey;
}
