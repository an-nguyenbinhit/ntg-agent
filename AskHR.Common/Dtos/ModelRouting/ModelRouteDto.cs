namespace AskHR.Common.Dtos.ModelRouting;

public sealed record ModelRouteDto(
    ModelCapability Capability,
    string Provider,
    string Model,
    string? Endpoint = null,
    string? RouteName = null,
    string? DataClass = null,
    IReadOnlyList<ModelRouteFallbackDto>? Fallbacks = null);

public sealed record ModelRouteFallbackDto(
    string Provider,
    string Model,
    string? Endpoint = null,
    string? DataClass = null);
