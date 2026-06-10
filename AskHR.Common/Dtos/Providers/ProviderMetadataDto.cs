namespace AskHR.Common.Dtos.Providers;

public class ProviderMetadataDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string DataResidency { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public string SecretReference { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
}
