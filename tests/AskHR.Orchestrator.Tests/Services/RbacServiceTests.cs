using AskHR.Common.Dtos.Constants;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Identity;
using AskHR.Orchestrator.Models.Tags;
using AskHR.Orchestrator.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class RbacServiceTests
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
    public async Task ResolveAsync_WhenUserIdIsNull_ReturnsAnonymousPublicContext()
    {
        var service = CreateService();

        var result = await service.ResolveAsync(null);

        Assert.That(result.IsAnonymous, Is.True);
        Assert.That(result.Roles, Is.EquivalentTo(new[] { "anonymous" }));
        Assert.That(result.AllowedTags, Does.Contain(Constants.PublicAllTagValue));
        Assert.That(result.SensitivityLevel, Is.EqualTo("Public"));
    }

    [Test]
    public async Task ResolveAsync_WhenUserHasNoRoles_FallsBackToAnonymous()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "no-roles", Email = "no-roles@askhr.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var service = CreateService();

        var result = await service.ResolveAsync(user.Id);

        Assert.That(result.IsAnonymous, Is.True);
        Assert.That(result.UserId, Is.Null);
        Assert.That(result.Roles, Is.EquivalentTo(new[] { "anonymous" }));
        Assert.That(result.AllowedTags, Does.Contain(Constants.PublicAllTagValue));
    }

    [Test]
    public async Task ResolveAsync_WhenUserHasRoleTagAndProfile_ReturnsAuthorizationContext()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "hr-vn", Email = "hr-vn@askhr.com" };
        var role = new Role { Id = Guid.NewGuid(), Name = "HR" };
        var tag = new Tag { Id = Guid.NewGuid(), Name = "VN Policy" };
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.Tags.Add(tag);
        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        _context.TagRoles.Add(new TagRole { Id = Guid.NewGuid(), TagId = tag.Id, RoleId = role.Id });
        _context.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            BusinessUnits = ["Vietnam"],
            Countries = ["VN"],
            LegalEntities = ["NTG-VN"],
            Level = "Manager",
            SensitivityLevel = "Confidential"
        });
        await _context.SaveChangesAsync();
        var service = CreateService();

        var result = await service.ResolveAsync(user.Id);

        Assert.That(result.IsAnonymous, Is.False);
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.Roles, Is.EquivalentTo(new[] { "HR" }));
        Assert.That(result.AllowedTags, Is.EquivalentTo(new[] { tag.Id.ToString() }));
        Assert.That(result.BusinessUnits, Is.EquivalentTo(new[] { "Vietnam" }));
        Assert.That(result.Countries, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(result.LegalEntities, Is.EquivalentTo(new[] { "NTG-VN" }));
        Assert.That(result.Level, Is.EqualTo("Manager"));
        Assert.That(result.SensitivityLevel, Is.EqualTo("Confidential"));
    }

    [Test]
    public async Task ResolveAsync_WhenMockAuthorizationEnabled_FillsMissingProfileAxes()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "mocked", Email = "mocked@askhr.com" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Employee" };
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _context.SaveChangesAsync();
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Authorization:Mock:Enabled"] = "true",
            ["Authorization:Mock:Roles:0"] = "MockRole",
            ["Authorization:Mock:BusinessUnits:0"] = "Vietnam",
            ["Authorization:Mock:Countries:0"] = "VN",
            ["Authorization:Mock:LegalEntities:0"] = "NTG-VN",
            ["Authorization:Mock:Level"] = "Staff",
            ["Authorization:Mock:SensitivityLevel"] = "Internal"
        });

        var result = await service.ResolveAsync(user.Id);

        Assert.That(result.IsAnonymous, Is.False);
        Assert.That(result.Roles, Is.EquivalentTo(new[] { "Employee", "MockRole" }));
        Assert.That(result.BusinessUnits, Is.EquivalentTo(new[] { "Vietnam" }));
        Assert.That(result.Countries, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(result.LegalEntities, Is.EquivalentTo(new[] { "NTG-VN" }));
        Assert.That(result.Level, Is.EqualTo("Staff"));
        Assert.That(result.SensitivityLevel, Is.EqualTo("Internal"));
    }

    private RbacService CreateService(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>())
            .Build();

        return new RbacService(_context, configuration);
    }
}
