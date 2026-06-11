using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Common.Dtos.Providers;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Providers;
using System.Security.Claims;

namespace AskHR.Orchestrator.Tests.Controllers;

[TestFixture]
public class ProvidersAdminControllerTests
{
    private AgentDbContext _context;
    private ProvidersAdminController _controller;
    private Guid _testAdminUserId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AgentDbContext(options);
        _testAdminUserId = Guid.NewGuid();

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _testAdminUserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
        ], "mock"));

        _controller = new ProvidersAdminController(_context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = adminUser }
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Guid> SeedProviderMetadata()
    {
        var id = Guid.NewGuid();
        _context.ProviderMetadatas.Add(new ProviderMetadata
        {
            Id = id,
            Provider = "OpenAI",
            ApprovalStatus = "Approved",
            DataResidency = "US",
            Capabilities = ["Chat", "Embeddings"],
            SecretReference = "kv-openai",
            HealthStatus = "Healthy",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = _testAdminUserId
        });
        await _context.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedModelRoute()
    {
        var id = Guid.NewGuid();
        _context.ModelRoutes.Add(new ModelRoute
        {
            Id = id,
            Feature = "Chat",
            PrimaryProvider = "OpenAI",
            PrimaryModel = "gpt-4",
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = _testAdminUserId,
            RequiredCapabilities = ["Chat"],
            DataPolicy = "Standard",
            Fallbacks = []
        });
        await _context.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task GetProviderMetadatas_ReturnsOk()
    {
        await SeedProviderMetadata();
        var result = await _controller.GetProviderMetadatas();

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var list = okResult!.Value as IEnumerable<ProviderMetadataDto>;
        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateProviderMetadata_ReturnsCreatedAtAction()
    {
        var dto = new ProviderMetadataDto
        {
            Provider = "Azure",
            ApprovalStatus = "Pending"
        };

        var result = await _controller.CreateProviderMetadata(dto);

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var createdResult = result as CreatedAtActionResult;
        var newDto = createdResult!.Value as ProviderMetadataDto;

        Assert.That(newDto, Is.Not.Null);
        Assert.That(newDto!.Id, Is.Not.EqualTo(Guid.Empty));

        var dbItem = await _context.ProviderMetadatas.FindAsync(newDto.Id);
        Assert.That(dbItem, Is.Not.Null);
        Assert.That(dbItem!.Provider, Is.EqualTo("Azure"));
    }

    [Test]
    public async Task UpdateProviderMetadata_WhenExists_ReturnsNoContent()
    {
        var id = await SeedProviderMetadata();
        var dto = new ProviderMetadataDto
        {
            Id = id,
            Provider = "OpenAI Updated"
        };

        var result = await _controller.UpdateProviderMetadata(id, dto);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var dbItem = await _context.ProviderMetadatas.FindAsync(id);
        Assert.That(dbItem!.Provider, Is.EqualTo("OpenAI Updated"));
    }

    [Test]
    public async Task UpdateProviderMetadata_IdMismatch_ReturnsBadRequest()
    {
        var id = await SeedProviderMetadata();
        var dto = new ProviderMetadataDto { Id = Guid.NewGuid() };

        var result = await _controller.UpdateProviderMetadata(id, dto);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetModelRoutes_ReturnsOk()
    {
        await SeedModelRoute();
        var result = await _controller.GetModelRoutes();

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var list = okResult!.Value as IEnumerable<ModelRouteDto>;
        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateModelRoute_ReturnsCreatedAtAction()
    {
        var dto = new ModelRouteDto
        {
            Feature = "Embeddings",
            PrimaryProvider = "OpenAI",
            PrimaryModel = "text-embedding-3-small",
            Enabled = true
        };

        var result = await _controller.CreateModelRoute(dto);

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var createdResult = result as CreatedAtActionResult;
        var newDto = createdResult!.Value as ModelRouteDto;

        Assert.That(newDto, Is.Not.Null);
        Assert.That(newDto!.Id, Is.Not.EqualTo(Guid.Empty));

        var dbItem = await _context.ModelRoutes.FindAsync(newDto.Id);
        Assert.That(dbItem, Is.Not.Null);
        Assert.That(dbItem!.Feature, Is.EqualTo("Embeddings"));
    }

    [Test]
    public async Task UpdateModelRoute_WhenExists_ReturnsNoContent()
    {
        var id = await SeedModelRoute();
        var dto = new ModelRouteDto
        {
            Id = id,
            Feature = "Chat Updated",
            PrimaryProvider = "Anthropic",
            PrimaryModel = "claude-3-haiku",
            Enabled = false
        };

        var result = await _controller.UpdateModelRoute(id, dto);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var dbItem = await _context.ModelRoutes.FindAsync(id);
        Assert.That(dbItem!.Feature, Is.EqualTo("Chat Updated"));
        Assert.That(dbItem.PrimaryProvider, Is.EqualTo("Anthropic"));
        Assert.That(dbItem.Enabled, Is.False);
    }
}
