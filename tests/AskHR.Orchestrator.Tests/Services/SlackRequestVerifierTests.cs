using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class SlackRequestVerifierTests
{
    [Test]
    public void Verify_WithValidSignature_ReturnsTrue()
    {
        const string secret = "signing-secret";
        const string body = """{"type":"event_callback"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = ComputeSignature(secret, timestamp, body);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Slack-Request-Timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture);
        context.Request.Headers["X-Slack-Signature"] = signature;
        var verifier = new SlackRequestVerifier(
            Options.Create(new SlackOptions { SigningSecret = secret }),
            TimeProvider.System);

        var result = verifier.Verify(context.Request, body);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Verify_WithInvalidSignature_ReturnsFalse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Slack-Request-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        context.Request.Headers["X-Slack-Signature"] = "v0=bad";
        var verifier = new SlackRequestVerifier(
            Options.Create(new SlackOptions { SigningSecret = "signing-secret" }),
            TimeProvider.System);

        var result = verifier.Verify(context.Request, "{}");

        Assert.That(result, Is.False);
    }

    private static string ComputeSignature(string secret, long timestamp, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"v0:{timestamp}:{body}"));
        return "v0=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
