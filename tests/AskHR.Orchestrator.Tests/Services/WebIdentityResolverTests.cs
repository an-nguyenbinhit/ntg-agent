using System.Security.Claims;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Identity;
using AskHR.Orchestrator.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class WebIdentityResolverTests
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
    public async Task ResolveUserIdAsync_WithAuthenticatedNameIdentifierGuid_ReturnsClaimValue()
    {
        var expectedUserId = Guid.NewGuid();
        var resolver = new WebIdentityResolver(_context);
        var httpContext = CreateContext(new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()));

        var result = await resolver.ResolveUserIdAsync(httpContext);

        Assert.That(result, Is.EqualTo(expectedUserId));
    }

    [Test]
    public async Task ResolveUserIdAsync_WithEmailClaim_ReturnsMatchingInternalUserId()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "jane", Email = "jane@askhr.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resolver = new WebIdentityResolver(_context);
        var httpContext = CreateContext(new Claim("preferred_username", "jane@askhr.com"));

        var result = await resolver.ResolveUserIdAsync(httpContext);

        Assert.That(result, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task ResolveUserIdAsync_WhenUnauthenticated_ReturnsNull()
    {
        var resolver = new WebIdentityResolver(_context);
        var httpContext = new DefaultHttpContext();

        var result = await resolver.ResolveUserIdAsync(httpContext);

        Assert.That(result, Is.Null);
    }

    private static DefaultHttpContext CreateContext(params Claim[] claims)
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
    }
}
