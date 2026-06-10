namespace AskHR.Common.Dtos.Agents;

public class SkillDto
{
    public Guid Id { get; set; }
    public string SkillId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Owner { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Draft";
    public int Version { get; set; } = 1;

    public SkillScopeDto Scope { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
    public SkillAnswerPolicyDto AnswerPolicy { get; set; } = new();

    public List<string> Tools { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
    public SkillEscalationDto Escalation { get; set; } = new();

    public string? PrimaryProvider { get; set; }
    public string? PrimaryModel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SkillScopeDto
{
    public List<string> Topics { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> BusinessUnits { get; set; } = new();
}

public class SkillAnswerPolicyDto
{
    public bool RequireCitation { get; set; } = true;
    public bool RefuseIfExpired { get; set; } = true;
    public List<string> ClarifyingQuestions { get; set; } = new();
}

public class SkillEscalationDto
{
    public string FallbackContact { get; set; } = string.Empty;
    public string SeverityHint { get; set; } = "P1";
}
