using AskHR.Orchestrator.Models.Identity;

namespace AskHR.Orchestrator.Models.Agents;

public class Skill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // String identifier like "leave-policy"
    public string SkillId { get; set; } = string.Empty; 
    
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Owner { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Draft"; // Draft, Pending, Approved
    public int Version { get; set; } = 1;

    public SkillScope Scope { get; set; } = new();
    
    public string Instructions { get; set; } = string.Empty;
    
    public SkillAnswerPolicy AnswerPolicy { get; set; } = new();

    public List<string> Tools { get; set; } = new();

    public List<string> Attachments { get; set; } = new();

    public SkillEscalation Escalation { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}

public class SkillScope
{
    public List<string> Topics { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> BusinessUnits { get; set; } = new();
}

public class SkillAnswerPolicy
{
    public bool RequireCitation { get; set; } = true;
    public bool RefuseIfExpired { get; set; } = true;
    public List<string> ClarifyingQuestions { get; set; } = new();
}

public class SkillEscalation
{
    public string FallbackContact { get; set; } = string.Empty;
    public string SeverityHint { get; set; } = "P1";
}
