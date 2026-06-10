using AskHR.Orchestrator.Models.Identity;

namespace AskHR.Orchestrator.Models.Providers;

public class ProviderMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Name of the provider, e.g. AzureOpenAI, OpenAI, Anthropic.</summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>Status like ProductionApproved, Pending, Disabled.</summary>
    public string ApprovalStatus { get; set; } = "Pending";
    
    public string DataResidency { get; set; } = string.Empty;
    
    /// <summary>List of capabilities: chat, embeddings, streaming, structured-output.</summary>
    public List<string> Capabilities { get; set; } = new();
    
    /// <summary>Reference to the secret store, e.g. kv://askhr/prod/azure-openai.</summary>
    public string SecretReference { get; set; } = string.Empty;
    
    public string HealthStatus { get; set; } = "Unknown";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}
