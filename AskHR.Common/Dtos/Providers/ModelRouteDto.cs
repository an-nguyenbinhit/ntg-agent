namespace AskHR.Common.Dtos.Providers;

public class FallbackRouteDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class ModelRouteDto
{
    public Guid Id { get; set; }
    public string Feature { get; set; } = string.Empty;
    public string PrimaryProvider { get; set; } = string.Empty;
    public string PrimaryModel { get; set; } = string.Empty;
    public List<FallbackRouteDto> Fallbacks { get; set; } = new();
    public List<string> RequiredCapabilities { get; set; } = new();
    public string DataPolicy { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
