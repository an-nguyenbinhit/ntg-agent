using AskHR.Orchestrator.Models.Identity;

namespace AskHR.Orchestrator.Models.Providers;

public class FallbackRoute
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class ModelRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Feature name, e.g. AnswerGeneration, IntentClassifier, Embedding.</summary>
    public string Feature { get; set; } = string.Empty;
    
    public string PrimaryProvider { get; set; } = string.Empty;
    public string PrimaryModel { get; set; } = string.Empty;
    
    public List<FallbackRoute> Fallbacks { get; set; } = new();
    
    public List<string> RequiredCapabilities { get; set; } = new();
    
    public string DataPolicy { get; set; } = "InternalApprovedOnly";
    
    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}
