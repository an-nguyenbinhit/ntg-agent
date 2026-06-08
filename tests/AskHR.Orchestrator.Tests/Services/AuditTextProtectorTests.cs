using AskHR.Orchestrator.Services.Audit;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class AuditTextProtectorTests
{
    [Test]
    public void Mask_ReplacesCommonPii()
    {
        var protector = new AuditTextProtector();

        var masked = protector.Mask("Email me at user@example.com or call +84 912 345 678.");

        Assert.That(masked, Does.Contain("[email]"));
        Assert.That(masked, Does.Contain("[phone]"));
        Assert.That(masked, Does.Not.Contain("user@example.com"));
    }

    [Test]
    public void Hash_IsStableSha256Hex()
    {
        var protector = new AuditTextProtector();

        var hash = protector.Hash("same text");

        Assert.That(hash, Has.Length.EqualTo(64));
        Assert.That(hash, Is.EqualTo(protector.Hash("same text")));
    }
}
