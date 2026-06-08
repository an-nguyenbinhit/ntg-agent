using System.Net;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class SlackResponseClientTests
{
    [Test]
    public async Task PostStatusMessageAsync_WhenSlackAccepts_ReturnsMessageTimestamp()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"ok":true,"ts":"1234.5678"}""");

        var ts = await client.PostStatusMessageAsync("C123", "1111.0000", "Đang xử lý...");

        Assert.That(ts, Is.EqualTo("1234.5678"));
    }

    [Test]
    public async Task PostStatusMessageAsync_WhenSlackRejects_ReturnsNull()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"ok":false,"error":"channel_not_found"}""");

        var ts = await client.PostStatusMessageAsync("C123", "1111.0000", "Đang xử lý...");

        Assert.That(ts, Is.Null);
    }

    [Test]
    public async Task PostStatusMessageAsync_WhenHttpCallFails_ReturnsNull()
    {
        var client = CreateClient(HttpStatusCode.InternalServerError, "{}");

        var ts = await client.PostStatusMessageAsync("C123", "1111.0000", "Đang xử lý...");

        Assert.That(ts, Is.Null);
    }

    [Test]
    public async Task PostStatusMessageAsync_WithoutBotToken_ReturnsNullWithoutCallingSlack()
    {
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://slack.com/api/") };
        var client = new SlackResponseClient(httpClient, Options.Create(new SlackOptions { BotToken = null }), NullLogger<SlackResponseClient>.Instance);

        var ts = await client.PostStatusMessageAsync("C123", "1111.0000", "Đang xử lý...");

        Assert.That(ts, Is.Null);
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task UpdateMessageAsync_SendsChatUpdateRequest()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true}""")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://slack.com/api/") };
        var client = new SlackResponseClient(httpClient, Options.Create(new SlackOptions { BotToken = "xoxb-test-token" }), NullLogger<SlackResponseClient>.Instance);

        await client.UpdateMessageAsync("C123", "1234.5678", "Final answer");

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.AbsolutePath, Does.EndWith("chat.update"));
    }

    private static SlackResponseClient CreateClient(HttpStatusCode statusCode, string responseBody)
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

        return new SlackResponseClient(httpClient, Options.Create(new SlackOptions { BotToken = "xoxb-test-token" }), NullLogger<SlackResponseClient>.Instance);
    }
}
