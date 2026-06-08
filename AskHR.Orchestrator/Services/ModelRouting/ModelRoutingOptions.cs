using AskHR.Common.Dtos.ModelRouting;

namespace AskHR.Orchestrator.Services.ModelRouting;

public sealed class ModelRoutingOptions
{
    public Dictionary<ModelCapability, ModelRouteOptions> Routes { get; init; } = [];
}

public sealed class ModelRouteOptions
{
    public string? RouteName { get; init; }

    public Guid? AgentId { get; init; }

    public string? Provider { get; init; }

    public string? Model { get; init; }

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string? DataClass { get; init; }

    public List<ModelRouteOptions> Fallbacks { get; init; } = [];
}
