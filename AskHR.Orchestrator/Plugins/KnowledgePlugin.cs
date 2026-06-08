using Microsoft.Extensions.AI;
using Microsoft.KernelMemory;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Services.Knowledge;
using System.ComponentModel;

namespace AskHR.Orchestrator.Plugins;

public sealed class KnowledgePlugin
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly AuthorizationContext _authorization;
    private readonly Guid _agentId;

    public KnowledgePlugin(IKnowledgeService knowledgeService, AuthorizationContext authorization, Guid agentId)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _agentId = agentId;
    }

    [Description("Search knowledge base")]
    public async Task<SearchResult> SearchAsync([Description("the value to search")]string query)
    {
        var result =  await _knowledgeService.SearchAsync(query, _agentId, _authorization);
        return result;
    }

    public AITool AsAITool()
    {
        return AIFunctionFactory.Create(this.SearchAsync, new AIFunctionFactoryOptions { Name = "memory"});
    }
}
