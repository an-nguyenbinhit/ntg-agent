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
    private readonly Action<string>? _onSearchQuery;

    public KnowledgePlugin(
        IKnowledgeService knowledgeService,
        AuthorizationContext authorization,
        Guid agentId,
        Action<string>? onSearchQuery = null)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _agentId = agentId;
        _onSearchQuery = onSearchQuery;
    }

    [Description("Search knowledge base")]
    public async Task<SearchResult> SearchAsync([Description("the value to search")]string query)
    {
        _onSearchQuery?.Invoke(query);
        var result =  await _knowledgeService.SearchAsync(query, _agentId, _authorization);
        return result;
    }

    public AITool AsAITool()
    {
        return AIFunctionFactory.Create(this.SearchAsync, new AIFunctionFactoryOptions { Name = "memory"});
    }
}
