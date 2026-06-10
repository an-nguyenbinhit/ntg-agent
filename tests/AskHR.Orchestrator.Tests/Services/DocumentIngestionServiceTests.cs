using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using AskHR.Common.Dtos.Documents;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Documents;
using AskHR.Orchestrator.Services.Knowledge;
using System.Text;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class DocumentIngestionServiceTests
{
    private AgentDbContext _context;
    private Mock<IKnowledgeService> _mockKnowledgeService;
    private DocumentIngestionService _service;
    private Guid _agentId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);
        _mockKnowledgeService = new Mock<IKnowledgeService>();
        _service = new DocumentIngestionService(_context, _mockKnowledgeService.Object, Mock.Of<ILogger<DocumentIngestionService>>());
        _agentId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static MemoryStream CreateStream(string content) => new(Encoding.UTF8.GetBytes(content));

    [Test]
    public void Constructor_WhenAgentDbContextIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentIngestionService(null!, _mockKnowledgeService.Object, Mock.Of<ILogger<DocumentIngestionService>>()));
    }

    [Test]
    public void Constructor_WhenKnowledgeServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentIngestionService(_context, null!, Mock.Of<ILogger<DocumentIngestionService>>()));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentIngestionService(_context, _mockKnowledgeService.Object, null!));
    }

    [Test]
    public async Task ComputeHashAsync_ResetsStreamPositionAndIsDeterministic()
    {
        using var stream1 = CreateStream("hello world");
        using var stream2 = CreateStream("hello world");

        var hash1 = await _service.ComputeHashAsync(stream1);
        var hash2 = await _service.ComputeHashAsync(stream2);

        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(stream1.Position, Is.Zero);
    }

    [Test]
    public async Task IngestFileAsync_WhenFileIsNew_CreatesDocument()
    {
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), "policy.md", _agentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kd-1");

        using var stream = CreateStream("policy content");
        var outcome = await _service.IngestFileAsync(stream, "policy.md", _agentId, null, _userId);

        Assert.That(outcome, Is.EqualTo(DocumentIngestionOutcome.Created));
        var document = await _context.Documents.SingleAsync();
        Assert.That(document.Name, Is.EqualTo("policy.md"));
        Assert.That(document.KnowledgeDocId, Is.EqualTo("kd-1"));
        Assert.That(document.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(document.Hash, Is.Not.Null.And.Not.Empty);
        Assert.That(document.CreatedByUserId, Is.EqualTo(_userId));
    }

    [Test]
    public async Task IngestFileAsync_WhenContentUnchanged_SkipsReindex()
    {
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), _agentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kd-1");

        using var stream1 = CreateStream("same content");
        await _service.IngestFileAsync(stream1, "policy.md", _agentId, null, _userId);

        using var stream2 = CreateStream("same content");
        var outcome = await _service.IngestFileAsync(stream2, "policy.md", _agentId, null, _userId);

        Assert.That(outcome, Is.EqualTo(DocumentIngestionOutcome.Unchanged));
        _mockKnowledgeService.Verify(
            x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task IngestFileAsync_WhenContentChanged_ReindexesAndRemovesOldEntry()
    {
        var importResults = new Queue<string>(["kd-1", "kd-2"]);
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), _agentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => importResults.Dequeue());

        using var stream1 = CreateStream("version 1");
        await _service.IngestFileAsync(stream1, "policy.md", _agentId, null, _userId);

        using var stream2 = CreateStream("version 2");
        var outcome = await _service.IngestFileAsync(stream2, "policy.md", _agentId, null, _userId);

        Assert.That(outcome, Is.EqualTo(DocumentIngestionOutcome.Updated));
        var document = await _context.Documents.SingleAsync();
        Assert.That(document.KnowledgeDocId, Is.EqualTo("kd-2"));
        Assert.That(document.IngestStatus, Is.EqualTo(IngestStatus.Success));
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync("kd-1", _agentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IngestFileAsync_WhenContentChanged_PreservesStoredPermissions()
    {
        var existing = new Document
        {
            Id = Guid.NewGuid(),
            Name = "policy.md",
            AgentId = _agentId,
            KnowledgeDocId = "kd-1",
            Hash = "OLDHASH",
            Roles = ["HR"],
            BusinessUnits = ["VN"],
            SensitivityLevel = "Internal"
        };
        _context.Documents.Add(existing);
        await _context.SaveChangesAsync();

        DocumentPermissionMetadata? capturedPermissions = null;
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), _agentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, Guid, DocumentPermissionMetadata, CancellationToken>((_, _, _, permissions, _) => capturedPermissions = permissions)
            .ReturnsAsync("kd-2");

        using var stream = CreateStream("new content");
        var outcome = await _service.IngestFileAsync(stream, "policy.md", _agentId, null, _userId);

        Assert.That(outcome, Is.EqualTo(DocumentIngestionOutcome.Updated));
        Assert.That(capturedPermissions, Is.Not.Null);
        Assert.That(capturedPermissions!.Roles, Is.EquivalentTo(new[] { "HR" }));
        Assert.That(capturedPermissions.BusinessUnits, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(capturedPermissions.SensitivityLevel, Is.EqualTo("Internal"));
    }

    [Test]
    public async Task IngestFileAsync_WhenImportFails_MarksDocumentFailed()
    {
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), _agentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ingestion backend unavailable"));

        using var stream = CreateStream("policy content");
        var outcome = await _service.IngestFileAsync(stream, "policy.md", _agentId, null, _userId);

        Assert.That(outcome, Is.EqualTo(DocumentIngestionOutcome.Failed));
        var document = await _context.Documents.SingleAsync();
        Assert.That(document.IngestStatus, Is.EqualTo(IngestStatus.Failed));
        Assert.That(document.IngestErrorMessage, Is.EqualTo("ingestion backend unavailable"));
    }
}
