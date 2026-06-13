using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using AskHR.Common.Dtos.Chats;
using AskHR.Common.Dtos.Constants;
using AskHR.Common.Dtos.Security;
using AskHR.Common.Dtos.TokenUsage;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Dtos;
using AskHR.Orchestrator.Exceptions;
using AskHR.Orchestrator.Models.Chat;
using AskHR.Orchestrator.Models.TokenUsage;
using AskHR.Orchestrator.Plugins;
using AskHR.Orchestrator.Services.AnonymousSessions;
using AskHR.Orchestrator.Services.DocumentAnalysis;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.Memory;
using AskHR.Orchestrator.Services.Security;
using System.Collections.Concurrent;
using System.Text;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace AskHR.Orchestrator.Services.Agents;

public class AgentService
{
    private readonly IAgentFactory _agentFactory;
    private readonly AgentDbContext _agentDbContext;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IAnonymousSessionService _anonymousSessionService;
    private readonly IIpAddressService _ipAddressService;
    private readonly IRbacService _rbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserMemoryService _memoryService;
    private readonly IDocumentAnalysisService _documentAnalysisService;
    private readonly AskHR.Orchestrator.Services.Audit.IAuditEventSink _auditEventSink;
    private readonly ILogger<AgentService> _logger;
    private const int MAX_LATEST_MESSAGE_TO_KEEP_FULL = 5;

    public AgentService(
        IAgentFactory agentFactory,
        AgentDbContext agentDbContext,
        IKnowledgeService knowledgeService,
        IAnonymousSessionService anonymousSessionService,
        IIpAddressService ipAddressService,
        IRbacService rbacService,
        IHttpContextAccessor httpContextAccessor,
        IUserMemoryService memoryService,
        IDocumentAnalysisService documentAnalysisService,
        AskHR.Orchestrator.Services.Audit.IAuditEventSink auditEventSink,
        ILogger<AgentService> logger)
    {
        _agentFactory = agentFactory;
        _agentDbContext = agentDbContext;
        _knowledgeService = knowledgeService;
        _anonymousSessionService = anonymousSessionService;
        _ipAddressService = ipAddressService;
        _rbacService = rbacService;
        _httpContextAccessor = httpContextAccessor;
        _memoryService = memoryService;
        _logger = logger;
        _documentAnalysisService = documentAnalysisService;
        _auditEventSink = auditEventSink;
    }

    public async IAsyncEnumerable<PromptResponse> ChatStreamingAsync(Guid? userId, PromptRequestForm promptRequest)
    {
        var startTime = DateTime.UtcNow;
        var anonymousSessionId = Guid.Empty;
        string? anonymousIpAddress = null;

        // Rate limit check for anonymous users
        if (!userId.HasValue)
        {
            if (!Guid.TryParse(promptRequest.SessionId, out anonymousSessionId))
            {
                throw new InvalidOperationException("A valid Session ID is required for unauthenticated requests.");
            }

            var httpContext = _httpContextAccessor.HttpContext;
            anonymousIpAddress = httpContext != null ? _ipAddressService.GetClientIpAddress(httpContext) : null;
            
            var rateLimitStatus = await _anonymousSessionService.CheckRateLimitAsync(anonymousSessionId, anonymousIpAddress);
            
            if (!rateLimitStatus.CanSendMessage)
            {
                throw new AnonymousRateLimitExceededException(
                    "You've reached the message limit for anonymous users. Please sign in to continue.",
                    rateLimitStatus);
            }
        }
        
        var conversation = await ValidateConversation(userId, promptRequest);
        var history = await PrepareConversationHistory(userId, promptRequest.SessionId, promptRequest.AgentId, conversation);
        var authorization = await _rbacService.ResolveAsync(userId);
        var ocrDocuments = new List<string>();
        if (_documentAnalysisService.IsEnabled && promptRequest.Documents is not null && promptRequest.Documents.Any())
        {
            ocrDocuments = await _documentAnalysisService.ExtractDocumentData(promptRequest.Documents);
        }

        if (conversation.Name == "New Conversation")
        {
            var nameTokenUsage = new TokenUsageInfo();
            var nameStart = DateTime.UtcNow;
            conversation.Name = await GenerateConversationName(promptRequest.Prompt, nameTokenUsage);
            _agentDbContext.Conversations.Update(conversation);
            await _agentDbContext.SaveChangesAsync();
            await TrackTokenUsageAsync(userId, promptRequest.SessionId, promptRequest.AgentId, new ConversationListItem(conversation.Id, conversation.Name), null, OperationTypes.GenerateName, nameTokenUsage, DateTime.UtcNow - nameStart);
        }

        // Track text and thinking content separately; thinking is persisted but excluded from AI history.
        var agentMessageSb = new StringBuilder();
        var thinkingMessageSb = new StringBuilder();
        var tokenUsageInfo = new TokenUsageInfo();
        // Track when the thinking phase begins and ends so we can persist the duration
        DateTime? thinkingStartedAt = null;
        DateTime? thinkingEndedAt = null;

        await foreach (var item in InvokePromptStreamingInternalAsync(promptRequest, history, authorization, ocrDocuments, tokenUsageInfo, userId))
        {
            if (item.ContentType == PromptContentType.SearchQuery)
            {
                yield return item;
                continue;
            }

            if (item.ContentType == PromptContentType.Thinking)
            {
                // Record the start timestamp on the first thinking chunk
                thinkingStartedAt ??= DateTime.UtcNow;
                thinkingMessageSb.Append(item.Content);
            }
            else
            {
                // Record the end timestamp on the first non-thinking chunk after thinking started
                if (thinkingStartedAt.HasValue && !thinkingEndedAt.HasValue)
                    thinkingEndedAt = DateTime.UtcNow;
                agentMessageSb.Append(item.Content);
            }

            yield return item;
        }

        var responseTime = DateTime.UtcNow - startTime;
        // Calculate thinking duration; falls back to end-of-stream if no non-thinking chunk followed
        int? thinkingDurationMs = thinkingStartedAt.HasValue
            ? (int)((thinkingEndedAt ?? DateTime.UtcNow) - thinkingStartedAt.Value).TotalMilliseconds
            : null;

        try
        {
            var savedMessage = await SaveMessages(
                userId, promptRequest, conversation,
                agentMessageSb.ToString(),
                thinkingMessageSb.Length > 0 ? thinkingMessageSb.ToString() : null,
                thinkingDurationMs,
                ocrDocuments);

            // Increment anonymous session counter after successful message
            if (!userId.HasValue)
            {
                await _anonymousSessionService.IncrementMessageCountAsync(anonymousSessionId, anonymousIpAddress);
            }

            // Use the Reasoning operation type when the model produced reasoning/thinking tokens.
            // For OpenAI, ReasoningTokens is populated from UsageDetails.ReasoningTokenCount.
            // For Anthropic, the SDK folds thinking tokens into OutputTokenCount and never sets
            // ReasoningTokenCount, so we fall back to checking for thinking content in the stream.
            var hasThinking = tokenUsageInfo.ReasoningTokens > 0 || thinkingMessageSb.Length > 0;
            var chatOperationType = hasThinking ? OperationTypes.Reasoning : OperationTypes.Chat;
            await TrackTokenUsageAsync(userId, promptRequest.SessionId, promptRequest.AgentId, new ConversationListItem(conversation.Id, conversation.Name), savedMessage.Id, chatOperationType, tokenUsageInfo, responseTime);

            if (userId is Guid userGuid)
            {
                await _memoryService.ProcessAndStoreMemoriesAsync(promptRequest.Prompt, userGuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save messages or post-process after streaming for conversation {ConversationId}", promptRequest.ConversationId);
        }
    }

    private async Task<Conversation> ValidateConversation(Guid? userId, PromptRequestForm promptRequest)
    {
        var conversationId = promptRequest.ConversationId;
        Conversation? conversation;

        if (userId.HasValue)
        {
            conversation = await _agentDbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
        }
        else
        {
            if (!Guid.TryParse(promptRequest.SessionId, out var sessionId))
                throw new InvalidOperationException("A valid Session ID is required for unauthenticated requests.");

            conversation = await _agentDbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.SessionId == sessionId);
        }

        return conversation ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");
    }

    private async Task<List<PChatMessage>> PrepareConversationHistory(Guid? userId, string? sessionId, Guid agentId, Conversation conversation)
    {
        var historyMessages = await _agentDbContext.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.UpdatedAt)
            .ToListAsync();

        if (historyMessages.Count <= MAX_LATEST_MESSAGE_TO_KEEP_FULL) return historyMessages;

        var toSummarize = historyMessages.Take(historyMessages.Count - MAX_LATEST_MESSAGE_TO_KEEP_FULL).ToList();
        var tokenUsageInfo = new TokenUsageInfo();
        var startTime = DateTime.UtcNow;
        var summary = await SummarizeMessagesAsync(toSummarize, tokenUsageInfo);
        var responseTime = DateTime.UtcNow - startTime;
        var summaryMsg = historyMessages.FirstOrDefault(m => m.IsSummary) ?? new PChatMessage
        {
            UserId = userId,
            Conversation = conversation,
            Role = ChatRole.System,
            IsSummary = true
        };

        summaryMsg.Content = $"Summary of earlier conversation: {summary}";
        summaryMsg.UpdatedAt = DateTime.UtcNow;

        _agentDbContext.Update(summaryMsg);

        await TrackTokenUsageAsync(userId, sessionId, agentId, new ConversationListItem(conversation.Id, conversation.Name), null, OperationTypes.SummarizeMessages, tokenUsageInfo, responseTime);

        return new List<PChatMessage> { summaryMsg }
            .Concat(historyMessages.TakeLast(MAX_LATEST_MESSAGE_TO_KEEP_FULL))
            .ToList();
    }

    private async Task<PChatMessage> SaveMessages(Guid? userId, PromptRequestForm promptRequest, Conversation conversation, string assistantReply, string? thinkingContent, int? thinkingDurationMs, List<string> ocrDocuments)
    {
        // Note: conversation name generation was moved to before streaming in ChatStreamingAsync.
        var userMessage = new PChatMessage { UserId = userId, Conversation = conversation, Content = promptRequest.Prompt, Role = ChatRole.User };
        var assistantMessage = new PChatMessage
        {
            UserId = userId,
            Conversation = conversation,
            Content = assistantReply,
            ThinkingContent = thinkingContent,
            ThinkingDurationMs = thinkingDurationMs,
            Role = ChatRole.Assistant
        };

        _agentDbContext.ChatMessages.AddRange(userMessage, assistantMessage);

        await _agentDbContext.SaveChangesAsync();

        return assistantMessage;
    }

    private async IAsyncEnumerable<PromptResponse> InvokePromptStreamingInternalAsync(
        PromptRequestForm promptRequest,
        List<PChatMessage> history,
        AuthorizationContext authorization,
        List<string> ocrDocuments,
        TokenUsageInfo tokenUsageInfo,
        Guid? userId)
    {
        if (promptRequest.AgentId == new Guid("760887e0-babd-41ae-aec1-b6ac3803d348"))
        {
            await foreach (var response in TestOrchestratorInvokePromptStreamingInternalAsync(promptRequest, history, userId, tokenUsageInfo))
            {
                yield return new PromptResponse(response);
            }
        }
        else
        {
            var agent = await _agentFactory.CreateAgent(promptRequest.AgentId);

            var chatHistory = new List<ChatMessage>();

            // Inject long-term memories for authenticated users
            await InjectLongTermMemories(userId, chatHistory, promptRequest.Prompt);

            foreach (var msg in history.OrderBy(m => m.CreatedAt))
            {
                chatHistory.Add(new ChatMessage(msg.Role, msg.Content));
            }

            var prompt = BuildPromptAsync(promptRequest, ocrDocuments);

            var userMessage = BuildUserMessage(promptRequest, prompt);

            chatHistory.Add(userMessage);

            var searchQueries = new ConcurrentQueue<string>();
            AITool memorySearch = new KnowledgePlugin(
                _knowledgeService,
                authorization,
                promptRequest.AgentId,
                query => searchQueries.Enqueue(query)).AsAITool();

            var chatOptions = new ChatOptions
            {
                Tools = [memorySearch]
            };

            await foreach (var update in agent.RunStreamingAsync(chatHistory, options: new ChatClientAgentRunOptions(chatOptions)))
            {
                while (searchQueries.TryDequeue(out var searchQuery))
                {
                    yield return new PromptResponse(searchQuery, PromptContentType.SearchQuery);
                }

                foreach (var item in update.Contents)
                {
                    if (item is TextReasoningContent reasoningContent)
                    {
                        yield return new PromptResponse(reasoningContent.Text, PromptContentType.Thinking);
                    }
                    else if (item is TextContent textContent)
                    {
                        yield return new PromptResponse(textContent.Text);
                    }
                }

                while (searchQueries.TryDequeue(out var searchQuery))
                {
                    yield return new PromptResponse(searchQuery, PromptContentType.SearchQuery);
                }

                ExtractTokenUsage(update.RawRepresentation, tokenUsageInfo);
            }
        }
    }

    private static void ExtractTokenUsage(object? rawRepresentation, TokenUsageInfo tokenUsage)
    {
        if (rawRepresentation is not ChatResponseUpdate update) return;
        var usageContent = update.Contents?.OfType<UsageContent>().FirstOrDefault();
        ExtractTokenUsage(usageContent?.Details, tokenUsage);
    }
    private static void ExtractTokenUsage(UsageDetails? usageDetails, TokenUsageInfo tokenUsage)
    {
        if (usageDetails == null) return;
        tokenUsage.InputTokens = usageDetails.InputTokenCount;
        tokenUsage.OutputTokens = usageDetails.OutputTokenCount;
        tokenUsage.ReasoningTokens = usageDetails.ReasoningTokenCount;
        tokenUsage.TotalTokens = usageDetails.TotalTokenCount;
    }

    private async IAsyncEnumerable<string> TestOrchestratorInvokePromptStreamingInternalAsync(
        PromptRequestForm promptRequest,
        List<PChatMessage> history,
        Guid? userId,
        TokenUsageInfo tokenUsageInfo)
    {
        var triageAgent = await _agentFactory.CreateAgent(promptRequest.AgentId);
        var csharpAgent = await _agentFactory.CreateAgent(new Guid("684604F0-3362-4499-A9B9-24AF973DCEBA")); // Gemini Agent ID
        var javaAgent = await _agentFactory.CreateAgent(new Guid("25ACDA2A-413F-49B6-BBE3-CE1435885F3F")); // Azure OpenAI Agent ID
        
        // Suppress MAAIW001 as CreateHandoffBuilderWith is marked for evaluation purposes
        #pragma warning disable MAAIW001
        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            .WithHandoffs(triageAgent, [csharpAgent, javaAgent])
            .Build();
        #pragma warning restore MAAIW001

        var chatHistory = new List<ChatMessage>();
        foreach (var msg in history.OrderBy(m => m.CreatedAt))
        {
            chatHistory.Add(new ChatMessage(msg.Role, msg.Content));
        }

        var prompt = BuildPromptAsync(promptRequest, []);

        var userMessage = BuildUserMessage(promptRequest, prompt);

        chatHistory.Add(userMessage);
        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, chatHistory);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentResponseEvent are && are.Response?.Usage != null)
            {
                tokenUsageInfo.InputTokens += are.Response.Usage.InputTokenCount ?? 0;
                tokenUsageInfo.OutputTokens += are.Response.Usage.OutputTokenCount ?? 0;
                tokenUsageInfo.ReasoningTokens += are.Response.Usage.ReasoningTokenCount ?? 0;
                tokenUsageInfo.TotalTokens += are.Response.Usage.TotalTokenCount ?? 0;
            }

            if (evt is WorkflowOutputEvent e)
            {
                yield return e.Data?.ToString() ?? string.Empty;
            }
        }
        // Inject long-term memories for authenticated users
        await InjectLongTermMemories(userId, chatHistory, promptRequest.Prompt);
    }

    private async Task InjectLongTermMemories(Guid? userId, List<ChatMessage> chatHistory, string userPrompt)
    {
        if (userId is Guid userIdGuid)
        {
            var memoryMessage = await _memoryService.RetrieveAndFormatMemoriesForChatAsync(userIdGuid, userPrompt);

            if (memoryMessage != null)
            {
                chatHistory.Add(memoryMessage);
            }
        }
    }
    private static ChatMessage BuildUserMessage(PromptRequestForm promptRequest, string prompt)
    {
        var userMessage = new ChatMessage(ChatRole.User, prompt);

        return userMessage;
    }

    private static string BuildPromptAsync(PromptRequest<UploadItemForm> promptRequest, List<string> ocrDocuments)
    {
        if (ocrDocuments.Count != 0)
        {
            return BuildOcrPromptAsync(promptRequest.Prompt, ocrDocuments);
        }

        return BuildTextOnlyPrompt(promptRequest.Prompt);

    }

    private async Task<string> GenerateConversationName(string question, TokenUsageInfo tokenUsageInfo)
    {
        var agent = await _agentFactory.CreateBasicAgent("Generate a short, descriptive conversation name (= 5 words).");
        var results = await agent.RunAsync(question);
        ExtractTokenUsage(results.Usage, tokenUsageInfo);
        return results.Text;
    }

    private async Task<string> SummarizeMessagesAsync(List<PChatMessage> messages, TokenUsageInfo tokenUsageInfo)
    {
        if (messages.Count == 0) return string.Empty;

        var chatHistory = new List<ChatMessage>();
        foreach (var msg in messages)
        {
            chatHistory.Add(new ChatMessage(msg.Role, msg.Content));
        }

        var agent = await _agentFactory.CreateBasicAgent("Summarize the following chat into a concise paragraph that captures key points.");
        var runResults = await agent.RunAsync(chatHistory);

        ExtractTokenUsage(runResults.Usage, tokenUsageInfo);
        return runResults.Text;
    }

    private static string BuildTextOnlyPrompt(string userPrompt) =>
        $@"
            Question: {userPrompt}. Context: Use search knowledge base tool if available.
            Given the context and provided history information, tools definitions and prior knowledge, reply to the user question. Include citations to the context where appropriate.
            If the answer is not in the context, try to use the search online tool if available or inform the user that you can't answer the question.
        ";


    private static string BuildOcrPromptAsync(string userPrompt, List<string> ocrDocuments)
    {
        var prompt = $@"
            You are a helpful document assistant.
            I will provide one or more documents with text, tables, and selection marks.
            Answer the user's question naturally, as a human would.
            Do not invent information or include irrelevant details.

            Documents:
            {string.Join(Environment.NewLine + Environment.NewLine, ocrDocuments)}

            User query: {userPrompt}
            ";

        return prompt;
    }

    private async Task TrackTokenUsageAsync(
        Guid? userId,
        string? sessionId,
        Guid agentId,
        ConversationListItem conversation,
        Guid? messageId,
        string operationType,
        TokenUsageInfo tokenUsageInfo,
        TimeSpan responseTime)
    {
        var agentConfig = await _agentDbContext.Agents.FirstOrDefaultAsync(a => a.Id == agentId);
        if (agentConfig == null) return;

        var sessionIdGuid = !userId.HasValue && Guid.TryParse(sessionId, out var sid) ? sid : (Guid?)null;

        var tokenUsage = new TokenUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionIdGuid,
            ConversationId = conversation.Id,
            MessageId = messageId,
            AgentId = agentId,
            ModelName = agentConfig.ProviderModelName,
            ProviderName = agentConfig.ProviderName,
            InputTokens = tokenUsageInfo.InputTokens,
            OutputTokens = tokenUsageInfo.OutputTokens,
            ReasoningTokens = tokenUsageInfo.ReasoningTokens,
            TotalTokens = tokenUsageInfo.TotalTokens,
            InputTokenCost = null,
            OutputTokenCost = null,
            ReasoningTokenCost = null,
            TotalCost = null,
            OperationType = operationType,
            ResponseTime = responseTime,
            CreatedAt = DateTime.UtcNow
        };

        _agentDbContext.TokenUsages.Add(tokenUsage);
        await _agentDbContext.SaveChangesAsync();

        // Emit an audit event for the token usage
        var channel = userId.HasValue ? "authenticated" : "anonymous";
        var isAnonymous = !userId.HasValue;
        
        // Use messageId string or fallback
        var textContext = messageId?.ToString() ?? "TokenUsageEvent";

        var auditEvent = new AskHR.Common.Dtos.Audit.AuditEventDto(
            $"chat.{operationType.ToLowerInvariant()}",
            agentId,
            userId,
            isAnonymous,
            channel,
            MaskedText: "***", 
            TextHash: "***", 
            Provider: agentConfig.ProviderName,
            Model: agentConfig.ProviderModelName,
            FallbackReason: null,
            CitationCount: 0,
            PromptTokens: tokenUsageInfo.InputTokens,
            CompletionTokens: tokenUsageInfo.OutputTokens,
            TotalTokens: tokenUsageInfo.TotalTokens,
            LatencyMs: (long)responseTime.TotalMilliseconds,
            CreatedAt: DateTimeOffset.UtcNow
        );

        await _auditEventSink.WriteAsync(auditEvent);
    }
}
