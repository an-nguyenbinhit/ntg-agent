using AskHR.Common.Dtos.ModelRouting;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed record ResolvedModelRoute(
    ModelCapability Capability,
    string Provider,
    string Model,
    string? Endpoint,
    string? ApiKey,
    string? RouteName,
    string? DataClass,
    IReadOnlyList<ResolvedModelRoute> Fallbacks)
{
    public ModelRouteDto ToDto()
        => new(
            Capability,
            Provider,
            Model,
            Endpoint,
            RouteName,
            DataClass,
            Fallbacks.Select(x => new ModelRouteFallbackDto(x.Provider, x.Model, x.Endpoint, x.DataClass)).ToList());
}
