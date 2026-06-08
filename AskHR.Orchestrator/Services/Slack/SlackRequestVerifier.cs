using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Services.Slack;

public sealed class SlackRequestVerifier : ISlackRequestVerifier
{
    private const string SignatureHeader = "X-Slack-Signature";
    private const string TimestampHeader = "X-Slack-Request-Timestamp";
    private readonly IOptions<SlackOptions> _options;
    private readonly TimeProvider _timeProvider;

    public SlackRequestVerifier(IOptions<SlackOptions> options, TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public bool Verify(HttpRequest request, string rawBody)
    {
        if (!_options.Value.EnableSignatureVerification || string.IsNullOrWhiteSpace(_options.Value.SigningSecret))
        {
            return true;
        }

        if (!request.Headers.TryGetValue(SignatureHeader, out var signatureValues) ||
            !request.Headers.TryGetValue(TimestampHeader, out var timestampValues) ||
            !long.TryParse(timestampValues.FirstOrDefault(), out var unixSeconds))
        {
            return false;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if ((_timeProvider.GetUtcNow() - timestamp).Duration() > TimeSpan.FromMinutes(5))
        {
            return false;
        }

        var baseString = $"v0:{unixSeconds}:{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Value.SigningSecret));
        var expected = "v0=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString))).ToLowerInvariant();
        var actual = signatureValues.FirstOrDefault() ?? string.Empty;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
    }
}
