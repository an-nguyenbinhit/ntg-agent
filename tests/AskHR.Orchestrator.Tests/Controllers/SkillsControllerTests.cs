using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskHR.Common.Dtos.Agents;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Agents;
using System.Security.Claims;

namespace AskHR.Orchestrator.Tests.Controllers;

[TestFixture]
public class SkillsControllerTests
{
    private AgentDbContext _context;
    private SkillsController _controller;
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

        _controller = new SkillsController(_context)
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

    private async Task<Guid> SeedSingleSkill()
    {
        var skillId = Guid.NewGuid();
        var skill = new Skill
        {
            Id = skillId,
            SkillId = "test-skill-1",
            Name = "Test Skill",
            Description = "This is a test skill",
            Enabled = true,
            Owner = "Admin",
            ApprovalStatus = "Approved",
            Instructions = "Test instructions",
            PrimaryProvider = "OpenAI",
            PrimaryModel = "gpt-4",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = _testAdminUserId,
            Scope = new SkillScope { Topics = ["HR", "IT"], Tags = [], BusinessUnits = [] },
            AnswerPolicy = new SkillAnswerPolicy { RequireCitation = true, ClarifyingQuestions = [] },
            Escalation = new SkillEscalation { FallbackContact = "hr@company.com" }
        };

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return skillId;
    }

    [Test]
    public async Task GetSkills_ReturnsOkWithList()
    {
        await SeedSingleSkill();

        var result = await _controller.GetSkills(CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var list = okResult!.Value as IEnumerable<SkillDto>;
        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetSkill_WhenExists_ReturnsOk()
    {
        var id = await SeedSingleSkill();

        var result = await _controller.GetSkill(id, CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as SkillDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo(id));
        Assert.That(dto.Name, Is.EqualTo("Test Skill"));
    }

    [Test]
    public async Task GetSkill_WhenNotExists_ReturnsNotFound()
    {
        var result = await _controller.GetSkill(Guid.NewGuid(), CancellationToken.None);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateSkill_ValidDto_ReturnsCreatedAtAction()
    {
        var newDto = new SkillDto
        {
            SkillId = "new-skill",
            Name = "New Skill",
            Description = "New skill description",
            Enabled = true,
            PrimaryProvider = "Azure",
            PrimaryModel = "gpt-4"
        };

        var result = await _controller.CreateSkill(newDto, CancellationToken.None);

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var createdResult = result as CreatedAtActionResult;
        var dto = createdResult!.Value as SkillDto;

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.Not.EqualTo(Guid.Empty));

        var dbSkill = await _context.Skills.FindAsync(dto.Id);
        Assert.That(dbSkill, Is.Not.Null);
        Assert.That(dbSkill!.Name, Is.EqualTo("New Skill"));
        Assert.That(dbSkill.PrimaryProvider, Is.EqualTo("Azure"));
        Assert.That(dbSkill.UpdatedByUserId, Is.EqualTo(_testAdminUserId));
    }

    [Test]
    public async Task UpdateSkill_WhenExists_ReturnsNoContent()
    {
        var id = await SeedSingleSkill();
        var dto = new SkillDto
        {
            Id = id,
            SkillId = "test-skill-1",
            Name = "Updated Skill",
            PrimaryProvider = "Anthropic",
            PrimaryModel = "claude-3-opus"
        };

        var result = await _controller.UpdateSkill(id, dto, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NoContentResult>());

        var dbSkill = await _context.Skills.FindAsync(id);
        Assert.That(dbSkill!.Name, Is.EqualTo("Updated Skill"));
        Assert.That(dbSkill.PrimaryProvider, Is.EqualTo("Anthropic"));
    }

    [Test]
    public async Task UpdateSkill_IdMismatch_ReturnsBadRequest()
    {
        var id = await SeedSingleSkill();
        var dto = new SkillDto { Id = Guid.NewGuid() }; // Mismatch

        var result = await _controller.UpdateSkill(id, dto, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestResult>());
    }

    [Test]
    public async Task DeleteSkill_WhenExists_ReturnsNoContent()
    {
        var id = await SeedSingleSkill();

        var result = await _controller.DeleteSkill(id, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var dbSkill = await _context.Skills.FindAsync(id);
        Assert.That(dbSkill, Is.Null);
    }

    [Test]
    public async Task DeleteSkill_WhenNotExists_ReturnsNotFound()
    {
        var result = await _controller.DeleteSkill(Guid.NewGuid(), CancellationToken.None);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
