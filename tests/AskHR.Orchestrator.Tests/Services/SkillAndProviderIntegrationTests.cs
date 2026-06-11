using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.ModelRouting;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
using AskHR.Orchestrator.Models.Providers;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.Escalation;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.KernelMemory;
using Moq;
using System.Runtime.CompilerServices;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class SkillAndProviderIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;
    private AgentDbContext _context = null!;
    private Mock<IKnowledgeService> _mockKnowledge = null!;
    private Mock<IChatClientFactory> _mockChatClientFactory = null!;
    private Mock<IAuditEventSink> _mockAuditSink = null!;
    private Mock<IChatClient> _mockChatClient = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AgentDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddMemoryCache();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            {"Providers:Test:ApiKey", "fake-key"},
            {"Providers:Test:Endpoint", "https://fake"}
        }!).Build();
        services.AddSingleton<IConfiguration>(config);

        _mockKnowledge = new Mock<IKnowledgeService>();
        _mockChatClientFactory = new Mock<IChatClientFactory>();
        _mockAuditSink = new Mock<IAuditEventSink>();
        _mockChatClient = new Mock<IChatClient>();

        services.AddSingleton(_mockKnowledge.Object);
        services.AddSingleton(_mockChatClientFactory.Object);
        services.AddSingleton(_mockAuditSink.Object);
        
        services.AddSingleton<IModelRouter, ModelRouter>();
        services.AddSingleton(Options.Create(new ModelRoutingOptions()));
        services.AddSingleton<IModelGateway, ModelGateway>();
        
        services.AddSingleton<IAuditTextProtector, AuditTextProtector>();
        services.AddSingleton<ISeverityClassifier, RuleBasedSeverityClassifier>();
        services.AddScoped<IWarmHandoffService, WarmHandoffService>();
        services.AddSingleton(Options.Create(new AnswerPipelineOptions { MinRelevance = 0.1, MaxFacts = 3 }));
        services.AddScoped<IPolicyAnswerService, PolicyAnswerService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AgentDbContext>();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    [Test]
    public async Task HrUpdatesSkillConfig_TakesEffectWithoutRedeploy()
    {
        // Arrange
        var answerService = _serviceProvider.GetRequiredService<IPolicyAnswerService>();
        var cache = _serviceProvider.GetRequiredService<IMemoryCache>();

        _context.ProviderMetadatas.Add(new ProviderMetadata { Id = Guid.NewGuid(), Provider = "OpenAI", ApprovalStatus = "ProductionApproved", SecretReference = "Providers:Test" });
        _context.ProviderMetadatas.Add(new ProviderMetadata { Id = Guid.NewGuid(), Provider = "Anthropic", ApprovalStatus = "ProductionApproved", SecretReference = "Providers:Test" });
        
        var modelRoute = new ModelRoute
        {
            Id = Guid.NewGuid(),
            Feature = "AnswerGeneration",
            PrimaryProvider = "OpenAI",
            PrimaryModel = "gpt-4o",
            Fallbacks = new List<FallbackRoute> { new FallbackRoute { Provider = "Anthropic", Model = "claude-3-opus" } },
            Enabled = true,
            DataPolicy = "hr-policy"
        };
        _context.ModelRoutes.Add(modelRoute);

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            SkillId = "it-support",
            Name = "IT Support",
            Description = "IT helpdesk",
            ApprovalStatus = "Approved",
            Enabled = true,
            Scope = new SkillScope { Topics = ["password", "login"] },
            PrimaryProvider = "OpenAI",
            PrimaryModel = "gpt-4o",
            Instructions = "Old instructions"
        };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        var request = new AskHrRequest(Guid.NewGuid(), "How to reset password?");
        _mockKnowledge.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSearchResult());

        _mockChatClient.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IList<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(StreamUpdate("Test answer"));

        // Capture the resolved route to verify
        ResolvedModelRoute? capturedRoute1 = null;
        ResolvedModelRoute? capturedRoute2 = null;

        _mockChatClientFactory.Setup(x => x.Create(It.IsAny<ResolvedModelRoute>()))
            .Returns<ResolvedModelRoute>(route => 
            {
                if (capturedRoute1 == null) capturedRoute1 = route;
                else capturedRoute2 = route;
                return _mockChatClient.Object;
            });

        // Act 1: Call once to populate cache
        var stream1 = answerService.StreamAnswerAsync(request, AuthorizationContext.Anonymous());
        await foreach(var _ in stream1) { } // Drain

        Assert.That(capturedRoute1, Is.Not.Null);
        Assert.That(capturedRoute1!.Provider, Is.EqualTo("OpenAI"));
        Assert.That(capturedRoute1.Model, Is.EqualTo("gpt-4o"));

        // Update Skill in DB
        skill.Instructions = "New updated instructions!";
        skill.PrimaryProvider = "Anthropic";
        skill.PrimaryModel = "claude-3-opus";
        await _context.SaveChangesAsync();

        // Expire cache manually to simulate expiration (as it is set to 30s)
        cache.Remove("PolicyAnswerService:ApprovedSkills");

        // Act 2: Call again
        var stream2 = answerService.StreamAnswerAsync(request, AuthorizationContext.Anonymous());
        await foreach(var _ in stream2) { } // Drain

        // Assert 2: Verifies that the new config was picked up and routed properly
        Assert.That(capturedRoute2, Is.Not.Null);
        Assert.That(capturedRoute2!.Provider, Is.EqualTo("Anthropic"));
        Assert.That(capturedRoute2.Model, Is.EqualTo("claude-3-opus"));
        
        _mockChatClient.Verify(x => x.GetStreamingResponseAsync(
            It.Is<IList<ChatMessage>>(list => list.Any(m => m.Text != null && m.Text.Contains("Old instructions"))),
            It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockChatClient.Verify(x => x.GetStreamingResponseAsync(
            It.Is<IList<ChatMessage>>(list => list.Any(m => m.Text != null && m.Text.Contains("New updated instructions!"))),
            It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamUpdate(string content, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(new TextContent(content));
        yield return update;
    }

    private static SearchResult BuildSearchResult()
    {
        return new SearchResult
        {
            Results = [
                new Citation {
                    DocumentId = "doc-1",
                    SourceName = "doc.md",
                    Partitions = [new Citation.Partition { Text = "Reset password via self-service portal.", Relevance = 0.9F, Tags = new TagCollection() }]
                }
            ]
        };
    }
}
