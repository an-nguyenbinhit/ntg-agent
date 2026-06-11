namespace AskHR.Orchestrator.Models.Audit;

public class FeedbackEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsAnonymous { get; set; }
    public string Rating { get; set; } = string.Empty;
    public string? CommentMasked { get; set; }
    public string? Topic { get; set; }
    public string? SeverityCandidate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
