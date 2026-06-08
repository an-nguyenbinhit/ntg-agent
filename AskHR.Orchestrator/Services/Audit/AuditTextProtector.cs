using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AskHR.Orchestrator.Services.Audit;

public sealed partial class AuditTextProtector : IAuditTextProtector
{
    public string Mask(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var masked = EmailRegex().Replace(text, "[email]");
        masked = PhoneRegex().Replace(masked, "[phone]");
        masked = GuidRegex().Replace(masked, "[id]");
        return masked;
    }

    public string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?\d[\d\s().-]{7,}\d)(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();
}
