using AskHR.Common.Dtos.ModelRouting;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
using AskHR.Orchestrator.Services.ModelRouting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class ModelRouterTests
{
    private AgentDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task ResolveAsync_WithConfiguredRoute_ReturnsCapabilityRoute()
    {
        var router = new ModelRouter(
            _context,
            Options.Create(new ModelRoutingOptions
            {
                Routes = new()
                {
                    [ModelCapability.AnswerGeneration] = new ModelRouteOptions
                    {
                        RouteName = "answer-primary",
                        Provider = "AzureOpenAI",
                        Model = "gpt-4.1-mini",
                        Endpoint = "https://example.openai.azure.com",
                        DataClass = "hr-policy"
                    }
                }
            }));

        var route = await router.ResolveAsync(ModelCapability.AnswerGeneration, dataClass: "hr-policy");

        Assert.That(route.Provider, Is.EqualTo("AzureOpenAI"));
        Assert.That(route.Model, Is.EqualTo("gpt-4.1-mini"));
        Assert.That(route.RouteName, Is.EqualTo("answer-primary"));
    }

    [Test]
    public async Task ResolveAsync_WithoutConfiguredRoute_UsesPublishedDefaultAgent()
    {
        var agentId = Guid.NewGuid();
        _context.Agents.Add(new Agent
        {
            Id = agentId,
            Name = "Default",
            ProviderName = "GitHubModel",
            ProviderModelName = "openai/gpt-4.1-mini",
            ProviderEndpoint = "https://models.github.ai/inference",
            ProviderApiKey = "key",
            IsDefault = true,
            IsPublished = true
        });
        await _context.SaveChangesAsync();

        var router = new ModelRouter(_context, Options.Create(new ModelRoutingOptions()));

        var route = await router.ResolveAsync(ModelCapability.IntentClassifier);

        Assert.That(route.Provider, Is.EqualTo("GitHubModel"));
        Assert.That(route.Model, Is.EqualTo("openai/gpt-4.1-mini"));
        Assert.That(route.RouteName, Is.EqualTo($"agent:{agentId}"));
    }
}
