using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Audit;
using AskHR.Common.Dtos.Security;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Chat;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.Escalation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class WarmHandoffServiceTests
{
    [Test]
    public async Task CreateAsync_MasksConversationContextAndWritesAudit()
    {
        await using var context = new AgentDbContext(new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var auditSink = new Mock<IAuditEventSink>();
        var protector = new AuditTextProtector();
        var service = new WarmHandoffService(context, protector, auditSink.Object);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Sensitive issue"
        };
        context.Conversations.Add(conversation);
        context.ChatMessages.Add(new PChatMessage
        {
            Conversation = conversation,
            ConversationId = conversation.Id,
            UserId = conversation.UserId,
            Role = ChatRole.User,
            Content = "Contact me at employee@example.com"
        });
        await context.SaveChangesAsync();

        var request = new AskHrRequest(
            Guid.NewGuid(),
            "I want to report harassment. Call +1 555 123 4567",
            Channel: "web",
            ThreadId: conversation.Id.ToString());
        var classification = new SeverityClassification(EscalationSeverity.P1, "Sensitive HR issue", "sensitive-topic", true);

        var result = await service.CreateAsync(
            request,
            new AuthorizationContext { UserId = conversation.UserId, IsAnonymous = false },
            classification);

        Assert.That(result.HandoffId, Does.StartWith("handoff-"));
        Assert.That(result.MaskedConversationContext, Has.Some.Contains("[email]"));
        Assert.That(result.MaskedConversationContext, Has.Some.Contains("[phone]"));
        auditSink.Verify(x => x.WriteAsync(
            It.Is<AuditEventDto>(e => e.EventType == "handoff.created" && e.FallbackReason!.Contains(result.HandoffId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
