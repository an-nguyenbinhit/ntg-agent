namespace AskHR.Orchestrator.Services.Audit;

public interface IAuditTextProtector
{
    string Mask(string text);

    string Hash(string text);
}
