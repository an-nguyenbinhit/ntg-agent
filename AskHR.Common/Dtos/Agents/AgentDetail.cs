using System.ComponentModel.DataAnnotations;

namespace AskHR.Common.Dtos.Agents;

public class AgentDetail
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Agent Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Agent Name must be between 1 and 200 characters")]
    public string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public string? Instructions { get; set; }
    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string? ProviderName { get; set; } = string.Empty;
    
    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string? ProviderEndpoint { get; set; } = string.Empty;
    
    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string? ProviderApiKey { get; set; } = string.Empty;
    
    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string? ProviderModelName { get; set; } = string.Empty;

    // Persona Configurations (UoB-05)
    public string Tone { get; set; } = string.Empty;
    public float CreativityCap { get; set; } = 0.5f;
    public List<string> AllowedEmojis { get; set; } = new();
    public string ChannelProfile { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }
    public bool IsPublished { get; set; }
    public string? McpServer { get; set; }

    /// <summary>Determines whether this agent uses Fast or Thinking (reasoning) mode.</summary>
    public AgentMode Mode { get; set; } = AgentMode.Fast;

    public int MaxTokens { get; set; } = 2000;

    public string ToolCount { get; set; } = "0";

    public AgentDetail()
    {
        Name = string.Empty;
    }

    public AgentDetail(
        Guid id,
        string name,
        string? instructions,
        string? providerName,
        string? providerEndpoint,
        string? providerApiKey,
        string? providerModelName)
    {
        Id = id;
        Name = name;
        Instructions = instructions;
        ProviderName = providerName;
        ProviderEndpoint = providerEndpoint;
        ProviderApiKey = providerApiKey;
        ProviderModelName = providerModelName;
    }
}
