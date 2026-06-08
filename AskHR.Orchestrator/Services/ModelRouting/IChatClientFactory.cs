using Microsoft.Extensions.AI;

namespace AskHR.Orchestrator.Services.ModelRouting;

public interface IChatClientFactory
{
    IChatClient Create(ResolvedModelRoute route);
}
