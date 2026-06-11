using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Channels.Teams;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Identity;
using AskHR.Orchestrator.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class TeamsIdentityResolverTests
{
    private AgentDbContext _context = null!;
    private Mock<IRbacService> _rbacService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);
        _rbacService = new Mock<IRbacService>();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task ResolveAsync_WithoutTeamsIdentity_ReturnsAnonymous()
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(new TeamsActivity());

        Assert.That(result.IsAnonymous, Is.True);
        _rbacService.Verify(x => x.ResolveAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ResolveAsync_WithConfiguredAadMappingToEmail_ResolvesInternalUser()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "jane", Email = "jane@askhr.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var expected = new AuthorizationContext { UserId = user.Id, Roles = ["employee"] };
        _rbacService
            .Setup(x => x.ResolveAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var resolver = CreateResolver(new TeamsOptions
        {
            UserMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["aad-123"] = "jane@askhr.com"
            }
        });

        var result = await resolver.ResolveAsync(new TeamsActivity
        {
            From = new TeamsChannelAccount { AadObjectId = "aad-123" }
        });

        Assert.That(result, Is.SameAs(expected));
    }

    [Test]
    public async Task ResolveAsync_WithUserPrincipalNameFallback_ResolvesInternalUser()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "minh", Email = "minh@askhr.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _rbacService
            .Setup(x => x.ResolveAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationContext { UserId = user.Id, Roles = ["hr"] });

        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(new TeamsActivity
        {
            From = new TeamsChannelAccount { UserPrincipalName = "minh@askhr.com" }
        });

        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.Roles, Does.Contain("hr"));
    }

    private TeamsIdentityResolver CreateResolver(TeamsOptions? options = null) =>
        new(
            _context,
            _rbacService.Object,
            Options.Create(options ?? new TeamsOptions()));
}

