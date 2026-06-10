using AskHR.Common.Dtos.Agents;
using AskHR.Orchestrator.Models.Identity;

namespace AskHR.Orchestrator.Models.Agents;

public class Agent
{
    public Agent()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Instructions { get; set; } = string.Empty;

    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string ProviderName { get; set; } = string.Empty;

    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string ProviderModelName { get; set; } = string.Empty;

    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string ProviderEndpoint { get; set; } = string.Empty;

    [Obsolete("Use ProviderMetadata and ModelRoute instead (UoB-07)")]
    public string ProviderApiKey { get; set; } = string.Empty;

    // Persona Configurations (UoB-05)
    public string Tone { get; set; } = string.Empty;
    public float CreativityCap { get; set; } = 0.5f;
    public List<string> AllowedEmojis { get; set; } = new();
    public string ChannelProfile { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>Whether this agent uses Fast or Thinking (reasoning) mode.</summary>
    public AgentMode Mode { get; set; } = AgentMode.Fast;

    public string? McpServer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public Guid UpdatedByUserId { get; set; }

    public User UpdatedByUser { get; set; } = null!;

    public ICollection<AgentTools> AgentTools { get; set; } = new List<AgentTools>();

}
