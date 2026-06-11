using AskHR.Common.Dtos.ModelRouting;
using AskHR.Orchestrator.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed class ModelRouter : IModelRouter
{
    private readonly AgentDbContext _dbContext;
    private readonly IOptions<ModelRoutingOptions> _options;
    private readonly IConfiguration _configuration;

    public ModelRouter(AgentDbContext dbContext, IOptions<ModelRoutingOptions> options, IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<ResolvedModelRoute> ResolveAsync(
        ModelCapability capability,
        Guid? agentId = null,
        string? dataClass = null,
        CancellationToken cancellationToken = default)
    {
        ResolvedModelRoute? resolved = null;

        var capabilityString = capability.ToString();
        var dbRoute = await _dbContext.ModelRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Enabled && r.Feature == capabilityString, cancellationToken);

        if (dbRoute is not null)
        {
            resolved = await ResolveDbRouteAsync(capability, dbRoute, cancellationToken);
        }

        if (resolved is null)
        {
            var configuredRoute = _options.Value.Routes.TryGetValue(capability, out var route)
                ? route
                : null;

            resolved = configuredRoute is not null
                ? await ResolveConfiguredRouteAsync(capability, configuredRoute, cancellationToken)
                : null;
        }

        resolved ??= await ResolveAgentRouteAsync(capability, agentId, cancellationToken);

        if (!IsApprovedForDataClass(resolved, dataClass))
        {
            var fallback = resolved.Fallbacks.FirstOrDefault(x => IsApprovedForDataClass(x, dataClass));
            if (fallback is not null)
            {
                return fallback with { Capability = capability };
            }

            throw new InvalidOperationException($"No approved model route for capability '{capability}' and data class '{dataClass ?? "default"}'.");
        }

        return resolved;
    }

    private async Task<ResolvedModelRoute?> ResolveDbRouteAsync(ModelCapability capability, Models.Providers.ModelRoute dbRoute, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dbRoute.PrimaryProvider) || string.IsNullOrWhiteSpace(dbRoute.PrimaryModel))
            return null;

        var primaryRoute = await BuildResolvedRouteAsync(capability, dbRoute.PrimaryProvider, dbRoute.PrimaryModel, dbRoute.DataPolicy, cancellationToken);
        if (primaryRoute is null) return null;

        var fallbacks = new List<ResolvedModelRoute>();
        if (dbRoute.Fallbacks != null)
        {
            foreach (var fallback in dbRoute.Fallbacks)
            {
                var fbRoute = await BuildResolvedRouteAsync(capability, fallback.Provider, fallback.Model, dbRoute.DataPolicy, cancellationToken);
                if (fbRoute is not null)
                {
                    fallbacks.Add(fbRoute);
                }
            }
        }

        return primaryRoute with { Fallbacks = fallbacks };
    }

    private async Task<ResolvedModelRoute?> BuildResolvedRouteAsync(ModelCapability capability, string provider, string model, string dataClass, CancellationToken cancellationToken)
    {
        var metadata = await _dbContext.ProviderMetadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Provider == provider && m.ApprovalStatus == "ProductionApproved", cancellationToken);

        if (metadata is null) return null;

        string? apiKey = null;
        string? endpoint = null;

        if (!string.IsNullOrWhiteSpace(metadata.SecretReference))
        {
            apiKey = _configuration[metadata.SecretReference] ?? _configuration[$"{metadata.SecretReference}:ApiKey"];
            endpoint = _configuration[$"{metadata.SecretReference}:Endpoint"];
        }

        return new ResolvedModelRoute(
            capability,
            provider,
            model,
            TrimToNull(endpoint),
            TrimToNull(apiKey),
            null,
            TrimToNull(dataClass),
            []);
    }

    private async Task<ResolvedModelRoute?> ResolveConfiguredRouteAsync(
        ModelCapability capability,
        ModelRouteOptions route,
        CancellationToken cancellationToken)
    {
        if (route.AgentId.HasValue)
        {
            var agentRoute = await ResolveAgentRouteAsync(capability, route.AgentId, cancellationToken);
            return agentRoute with
            {
                RouteName = route.RouteName ?? agentRoute.RouteName,
                DataClass = route.DataClass ?? agentRoute.DataClass,
                Fallbacks = await ResolveFallbacksAsync(capability, route.Fallbacks, cancellationToken)
            };
        }

        if (string.IsNullOrWhiteSpace(route.Provider) || string.IsNullOrWhiteSpace(route.Model))
        {
            return null;
        }

        return new ResolvedModelRoute(
            capability,
            route.Provider.Trim(),
            route.Model.Trim(),
            TrimToNull(route.Endpoint),
            TrimToNull(route.ApiKey),
            TrimToNull(route.RouteName),
            TrimToNull(route.DataClass),
            await ResolveFallbacksAsync(capability, route.Fallbacks, cancellationToken));
    }

    private async Task<IReadOnlyList<ResolvedModelRoute>> ResolveFallbacksAsync(
        ModelCapability capability,
        IReadOnlyList<ModelRouteOptions> fallbacks,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedModelRoute>();
        foreach (var fallback in fallbacks)
        {
            var route = await ResolveConfiguredRouteAsync(capability, fallback, cancellationToken);
            if (route is not null)
            {
                resolved.Add(route);
            }
        }

        return resolved;
    }

    private async Task<ResolvedModelRoute> ResolveAgentRouteAsync(
        ModelCapability capability,
        Guid? agentId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Agents.AsNoTracking().Where(x => x.IsPublished);
        var agent = agentId.HasValue
            ? await query.FirstOrDefaultAsync(x => x.Id == agentId.Value, cancellationToken)
            : await query.OrderByDescending(x => x.IsDefault).ThenBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);

        if (agent is null)
        {
            throw new InvalidOperationException("No published agent is available for model route fallback.");
        }

        if (string.IsNullOrWhiteSpace(agent.ProviderName) || string.IsNullOrWhiteSpace(agent.ProviderModelName))
        {
            throw new InvalidOperationException($"Agent '{agent.Id}' does not have provider/model configuration.");
        }

        return new ResolvedModelRoute(
            capability,
            agent.ProviderName,
            agent.ProviderModelName,
            TrimToNull(agent.ProviderEndpoint),
            TrimToNull(agent.ProviderApiKey),
            $"agent:{agent.Id}",
            null,
            []);
    }

    private static bool IsApprovedForDataClass(ResolvedModelRoute route, string? dataClass)
        => string.IsNullOrWhiteSpace(dataClass) ||
           string.IsNullOrWhiteSpace(route.DataClass) ||
           string.Equals(route.DataClass, dataClass, StringComparison.OrdinalIgnoreCase);

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
