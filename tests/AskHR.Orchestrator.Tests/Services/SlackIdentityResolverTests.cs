using System.Net;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Identity;
using AskHR.Orchestrator.Services.Security;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class SlackIdentityResolverTests
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
    public async Task ResolveAsync_WithoutSlackUserId_ReturnsAnonymous()
    {
        var resolver = CreateResolver(HttpStatusCode.OK, """{"ok":true,"user":{"profile":{"email":"someone@askhr.com"}}}""");

        var result = await resolver.ResolveAsync(null);

        Assert.That(result.IsAnonymous, Is.True);
        _rbacService.Verify(x => x.ResolveAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ResolveAsync_WhenEmailMatchesInternalUser_ResolvesAuthorizationForThatUser()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "jane", Email = "jane@askhr.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var expectedContext = new AuthorizationContext { UserId = user.Id, Roles = ["employee"] };
        _rbacService
            .Setup(x => x.ResolveAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContext);

        var resolver = CreateResolver(HttpStatusCode.OK, """{"ok":true,"user":{"profile":{"email":"jane@askhr.com"}}}""");

        var result = await resolver.ResolveAsync("U123");

        Assert.That(result, Is.SameAs(expectedContext));
        _rbacService.Verify(x => x.ResolveAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_WhenSlackLookupFails_ReturnsAnonymous()
    {
        var resolver = CreateResolver(HttpStatusCode.InternalServerError, "{}");

        var result = await resolver.ResolveAsync("U123");

        Assert.That(result.IsAnonymous, Is.True);
        _rbacService.Verify(x => x.ResolveAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private SlackIdentityResolver CreateResolver(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://slack.com/api/") };

        return new SlackIdentityResolver(
            httpClient,
            _context,
            _rbacService.Object,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new SlackOptions { BotToken = "xoxb-test-token" }),
            NullLogger<SlackIdentityResolver>.Instance);
    }
}
