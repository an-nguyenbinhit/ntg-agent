using AskHR.Common.Dtos.Documents;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using AskHR.Orchestrator.Services.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class ReingestToolTests
{
    private AgentDbContext _context = null!;
    private Mock<IDocumentIngestionService> _ingestionService = null!;
    private ReingestTool _tool = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);
        _ingestionService = new Mock<IDocumentIngestionService>();
        _tool = new ReingestTool(
            _context,
            _ingestionService.Object,
            Options.Create(new ReingestMigrationOptions()),
            NullLogger<ReingestTool>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task RunAsync_DryRun_DoesNotReindexDocuments()
    {
        await AddDocumentAsync("policy.pdf", "km-1");

        var summary = await _tool.RunAsync(new ReingestMigrationRequest(DryRun: true));

        Assert.That(summary.DryRun, Is.True);
        Assert.That(summary.Scanned, Is.EqualTo(1));
        Assert.That(summary.Reindexed, Is.EqualTo(0));
        _ingestionService.Verify(x => x.ReindexDocumentAsync(It.IsAny<Document>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_WhenNotDryRun_AppliesDefaultMetadataAndReindexes()
    {
        var document = await AddDocumentAsync("policy.pdf", "km-1");
        var defaultTag = await AddTagAsync("Public");
        DocumentPermissionMetadata? capturedPermissions = null;
        _ingestionService
            .Setup(x => x.ReindexDocumentAsync(It.IsAny<Document>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<Document, DocumentPermissionMetadata, CancellationToken>((d, permissions, _) =>
            {
                capturedPermissions = permissions;
                d.KnowledgeDocId = "km-2";
                d.IngestStatus = IngestStatus.Success;
            })
            .Returns(Task.CompletedTask);

        var summary = await _tool.RunAsync(new ReingestMigrationRequest(DryRun: false));

        Assert.That(summary.Reindexed, Is.EqualTo(1));
        Assert.That(document.Roles, Is.EqualTo(new[] { "Employee" }));
        Assert.That(document.BusinessUnits, Is.EqualTo(new[] { "All" }));
        Assert.That(document.SensitivityLevel, Is.EqualTo("Public"));
        Assert.That(document.KnowledgeDocId, Is.EqualTo("km-2"));
        Assert.That(await _context.DocumentTags.CountAsync(x => x.DocumentId == document.Id && x.TagId == defaultTag.Id), Is.EqualTo(1));
        Assert.That(capturedPermissions, Is.Not.Null);
        Assert.That(capturedPermissions!.AllowedTags, Is.EquivalentTo(new[] { defaultTag.Id.ToString() }));
    }

    [Test]
    public async Task RunAsync_WhenDefaultTagDoesNotExist_ThrowsBeforeReindex()
    {
        await AddDocumentAsync("policy.pdf", "km-1");
        var missingTagId = Guid.NewGuid();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _tool.RunAsync(new ReingestMigrationRequest(DryRun: false, DefaultTagIds: [missingTagId])));

        Assert.That(ex!.Message, Does.Contain(missingTagId.ToString()));
        _ingestionService.Verify(x => x.ReindexDocumentAsync(It.IsAny<Document>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<Document> AddDocumentAsync(string name, string knowledgeDocId)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = name,
            AgentId = Guid.NewGuid(),
            KnowledgeDocId = knowledgeDocId,
            Type = DocumentType.File,
            CreatedByUserId = Guid.NewGuid(),
            UpdatedByUserId = Guid.NewGuid()
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    private async Task<Models.Tags.Tag> AddTagAsync(string name)
    {
        var tag = new Models.Tags.Tag
        {
            Id = new Guid("10dd4508-4e35-4c63-bd74-5d90246c7770"),
            Name = name
        };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }
}
