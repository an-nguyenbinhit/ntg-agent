using AskHR.Common.Dtos.Answers;
using AskHR.Orchestrator.Services.Escalation;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class SeverityClassifierTests
{
    [Test]
    public void Classify_WithSensitiveTopic_ReturnsP1WarmHandoff()
    {
        var classifier = new RuleBasedSeverityClassifier();
        var request = new AskHrRequest(Guid.NewGuid(), "I need help with harassment from my manager.");

        var result = classifier.Classify(request);

        Assert.That(result.Severity, Is.EqualTo(EscalationSeverity.P1));
        Assert.That(result.RequiresWarmHandoff, Is.True);
        Assert.That(result.Reason, Is.EqualTo("sensitive-topic"));
    }

    [Test]
    public void Classify_WithNoCitations_ReturnsP3()
    {
        var classifier = new RuleBasedSeverityClassifier();
        var request = new AskHrRequest(Guid.NewGuid(), "Where is the cafeteria?");

        var result = classifier.Classify(request, []);

        Assert.That(result.Severity, Is.EqualTo(EscalationSeverity.P3));
        Assert.That(result.RequiresWarmHandoff, Is.False);
        Assert.That(result.Reason, Is.EqualTo("no-grounding-citations"));
    }
}
