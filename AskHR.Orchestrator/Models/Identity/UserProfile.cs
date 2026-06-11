namespace AskHR.Orchestrator.Models.Identity;

public class UserProfile
{
    public Guid UserId { get; set; }
    
    public List<string> BusinessUnits { get; set; } = [];
    
    public List<string> Countries { get; set; } = [];
    
    public List<string> LegalEntities { get; set; } = [];
    
    public string? Level { get; set; }
    
    public string? SensitivityLevel { get; set; }

    public User? User { get; set; }
}
