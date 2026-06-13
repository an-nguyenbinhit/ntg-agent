using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using AskHR.Orchestrator.Controllers;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.Security;
using AskHR.Orchestrator.Models.Documents;
using AskHR.ServiceDefaults.Logging;
using AskHR.ServiceDefaults.Logging.Metrics;
using System.Security.Claims;
using System.Text;
using AskHR.Common.Dtos.Documents;
using AskHR.Common.Dtos.Security;
using Microsoft.Extensions.Logging;
namespace AskHR.Orchestrator.Tests.Controllers;
[TestFixture]
public class DocumentsControllerTests
{
    private AgentDbContext _context;
    private Mock<IKnowledgeService> _mockKnowledgeService;
    private Mock<ILogger<DocumentsController>> _mockLogger;
    private Mock<IMetricsCollector> _mockMetrics;
    private Mock<IIdentityResolver> _mockIdentityResolver;
    private Mock<IRbacService> _mockRbacService;
    private IDocumentIngestionService _ingestionService;
    private DocumentsController _controller;
    private DocumentsDownloadController _downloadController;
    private Guid _testUserId;
    private Guid _testAgentId;
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AgentDbContext(options);
        _mockKnowledgeService = new Mock<IKnowledgeService>();
        _mockLogger = new Mock<ILogger<DocumentsController>>();
        _mockMetrics = new Mock<IMetricsCollector>();
        _mockIdentityResolver = new Mock<IIdentityResolver>();
        _mockRbacService = new Mock<IRbacService>();
        var mockScope = new Mock<IDisposable>();
        var mockTimer = new Mock<IDisposable>();
        _mockMetrics.Setup(x => x.StartTimer(It.IsAny<string>(), It.IsAny<(string, string)[]>())).Returns(mockTimer.Object);
        _testUserId = Guid.NewGuid();
        _testAgentId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "mock"));
        _mockIdentityResolver
            .Setup(x => x.ResolveUserIdAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUserId);
        _mockRbacService
            .Setup(x => x.ResolveAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationContext
            {
                UserId = _testUserId,
                Roles = ["Admin"],
                SensitivityLevel = "Confidential"
            });
        _ingestionService = new DocumentIngestionService(_context, _mockKnowledgeService.Object, Mock.Of<ILogger<DocumentIngestionService>>());
        _controller = new DocumentsController(_context, _mockKnowledgeService.Object, _ingestionService, _mockLogger.Object, _mockMetrics.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        _downloadController = new DocumentsDownloadController(
            _context,
            _mockKnowledgeService.Object,
            _mockIdentityResolver.Object,
            _mockRbacService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
    [Test]
    public void Constructor_WhenAgentDbContextIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentsController(null!, _mockKnowledgeService.Object, _ingestionService, _mockLogger.Object, _mockMetrics.Object));
    }
    [Test]
    public void Constructor_WhenKnowledgeServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentsController(_context, null!, _ingestionService, _mockLogger.Object, _mockMetrics.Object));
    }
    [Test]
    public void Constructor_WhenIngestionServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentsController(_context, _mockKnowledgeService.Object, null!, _mockLogger.Object, _mockMetrics.Object));
    }
    [Test]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentsController(_context, _mockKnowledgeService.Object, _ingestionService, null!, _mockMetrics.Object));
    }
    [Test]
    public void Constructor_WhenMetricsIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentsController(_context, _mockKnowledgeService.Object, _ingestionService, _mockLogger.Object, null!));
    }
    [Test]
    public async Task GetDocumentsByAgentId_WhenNoDocuments_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetDocumentsByAgentId(_testAgentId, null);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var documents = okResult.Value as List<DocumentListItem>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents, Is.Empty);
    }
    [Test]
    public async Task GetDocumentsByAgentId_WhenDocumentsExist_ReturnsDocuments()
    {
        // Arrange
        var tag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "Test Tag" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Test Document",
            AgentId = _testAgentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = tag.Id, Tag = tag }
            }
        };
        _context.Tags.Add(tag);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetDocumentsByAgentId(_testAgentId, null);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var documents = okResult.Value as List<DocumentListItem>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Name, Is.EqualTo("Test Document"));
        Assert.That(documents[0].Tags, Contains.Item("Test Tag"));
    }
    [Test]
    public async Task GetDocumentsByAgentId_WhenFolderIsRoot_ReturnsRootDocuments()
    {
        // Arrange
        var rootFolder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            AgentId = _testAgentId,
            ParentId = null
        };
        var document1 = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Root Document",
            AgentId = _testAgentId,
            FolderId = rootFolder.Id
        };
        var document2 = new Document
        {
            Id = Guid.NewGuid(),
            Name = "No Folder Document",
            AgentId = _testAgentId,
            FolderId = null
        };
        _context.Folders.Add(rootFolder);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetDocumentsByAgentId(_testAgentId, rootFolder.Id);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var documents = okResult.Value as List<DocumentListItem>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents, Has.Count.EqualTo(2));
    }
    [Test]
    public async Task GetDocumentsByAgentId_WhenSpecificFolder_ReturnsOnlyFolderDocuments()
    {
        // Arrange
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Specific Folder",
            AgentId = _testAgentId,
            ParentId = Guid.NewGuid()
        };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Folder Document",
            AgentId = _testAgentId,
            FolderId = folder.Id
        };
        _context.Folders.Add(folder);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.GetDocumentsByAgentId(_testAgentId, folder.Id);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var documents = okResult.Value as List<DocumentListItem>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Name, Is.EqualTo("Folder Document"));
    }
    [Test]
    public async Task UploadDocuments_WhenNoFiles_ReturnsBadRequest()
    {
        // Arrange
        var files = new FormFileCollection();
        // Act
        var result = await _controller.UploadDocuments(_testAgentId, files, null, new List<string>());
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value, Is.EqualTo("No files uploaded."));
    }
    [Test]
    public void UploadDocuments_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext.HttpContext.User = anonymousUser;
        var files = new FormFileCollection
        {
            CreateTestFile("test.txt", "test content")
        };
        // Act & Assert
        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.UploadDocuments(_testAgentId, files, null, new List<string>()));
        Assert.That(exception.Message, Is.EqualTo("User is not authenticated."));
    }
    [Test]
    public async Task UploadDocuments_WhenValidFiles_UploadsSuccessfully()
    {
        // Arrange
        var files = new FormFileCollection
        {
            CreateTestFile("policy.md", "test content")
        };
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var tags = new List<string> { tag1Id.ToString(), tag2Id.ToString() };
        _mockKnowledgeService.Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("knowledge-doc-id");
        // Act
        var result = await _controller.UploadDocuments(_testAgentId, files, null, tags);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var savedDocument = await _context.Documents.FirstOrDefaultAsync();
        Assert.That(savedDocument, Is.Not.Null);
        Assert.That(savedDocument.Name, Is.EqualTo("policy.md"));
        Assert.That(savedDocument.AgentId, Is.EqualTo(_testAgentId));
        Assert.That(savedDocument.KnowledgeDocId, Is.EqualTo("knowledge-doc-id"));
        Assert.That(savedDocument.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(savedDocument.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
        var documentTags = await _context.DocumentTags.Where(dt => dt.DocumentId == savedDocument.Id).ToListAsync();
        Assert.That(documentTags, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UploadDocuments_WhenExistingReindexFails_PreservesPreviousHashAndMetadata()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "policy.md",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Hash = "old-hash",
            Roles = ["HR"],
            BusinessUnits = ["VN"],
            SensitivityLevel = "Internal",
            ApprovalStatus = ApprovalStatus.Approved,
            IngestStatus = IngestStatus.Success
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var files = new FormFileCollection
        {
            CreateTestFile("policy.md", "new content")
        };
        _mockKnowledgeService
            .Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), "policy.md", _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-knowledge-doc-id");
        _mockKnowledgeService
            .Setup(x => x.RemoveDocumentAsync("old-knowledge-doc-id", _testAgentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        // Act
        var result = await _controller.UploadDocuments(_testAgentId, files, null, [Guid.NewGuid().ToString()]);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.Hash, Is.EqualTo("old-hash"));
        Assert.That(updatedDocument.KnowledgeDocId, Is.EqualTo("old-knowledge-doc-id"));
        Assert.That(updatedDocument.Roles, Is.EquivalentTo(new[] { "HR" }));
        Assert.That(updatedDocument.BusinessUnits, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(updatedDocument.SensitivityLevel, Is.EqualTo("Internal"));
        Assert.That(updatedDocument.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(updatedDocument.IngestStatus, Is.EqualTo(IngestStatus.Failed));
        Assert.That(await _context.DocumentTags.CountAsync(dt => dt.DocumentId == document.Id), Is.EqualTo(0));
    }

    [Test]
    public async Task UploadDocuments_WhenUnsupportedKnowledgeFile_ReturnsBadRequestAndDoesNotImport()
    {
        // Arrange
        var files = new FormFileCollection
        {
            CreateTestFile("notes.txt", "test content")
        };

        // Act
        var result = await _controller.UploadDocuments(_testAgentId, files, null, new List<string>());

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value?.ToString(), Does.Contain("Unsupported knowledge file type: notes.txt"));
        _mockKnowledgeService.Verify(x => x.ImportDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<DocumentPermissionMetadata>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(await _context.Documents.CountAsync(), Is.EqualTo(0));
    }
    [Test]
    public async Task UploadDocuments_WhenEmptyFile_SkipsFile()
    {
        // Arrange
        var files = new FormFileCollection
        {
            CreateTestFile("empty.txt", ""),
            CreateTestFile("valid.md", "valid content")
        };
        _mockKnowledgeService.Setup(x => x.ImportDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("knowledge-doc-id");
        // Act
        var result = await _controller.UploadDocuments(_testAgentId, files, null, new List<string>());
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var documentsCount = await _context.Documents.CountAsync();
        Assert.That(documentsCount, Is.EqualTo(1)); // Only the valid file should be saved
    }
    [Test]
    public async Task DeleteDocument_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext.HttpContext.User = anonymousUser;
        // Act
        var result = await _controller.DeleteDocument(Guid.NewGuid(), _testAgentId);
        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }
    [Test]
    public async Task DeleteDocument_WhenDocumentNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteDocument(Guid.NewGuid(), _testAgentId);
        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
    [Test]
    public async Task DeleteDocument_WhenDocumentExists_DeletesSuccessfully()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Test Document",
            AgentId = _testAgentId,
            KnowledgeDocId = "knowledge-doc-id"
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.DeleteDocument(document.Id, _testAgentId);
        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        var deletedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(deletedDocument, Is.Null);
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync("knowledge-doc-id", _testAgentId, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Test]
    public async Task DeleteDocument_WhenNoKnowledgeDocId_DeletesWithoutKnowledgeService()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Test Document",
            AgentId = _testAgentId,
            KnowledgeDocId = null
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Act
        var result = await _controller.DeleteDocument(document.Id, _testAgentId);
        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        var deletedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(deletedDocument, Is.Null);
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteDocument_WhenAgentDoesNotMatch_ReturnsNotFound()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Test Document",
            AgentId = Guid.NewGuid(),
            KnowledgeDocId = null
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteDocument(document.Id, _testAgentId);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        var existingDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(existingDocument, Is.Not.Null);
    }

    [Test]
    public async Task UpdateDocumentMetadata_WhenDocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new DocumentMetadataUpdateRequest(["HR"], ["Finance"], "Confidential");
        // Act
        var result = await _controller.UpdateDocumentMetadata(_testAgentId, Guid.NewGuid(), request, CancellationToken.None);
        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
    [Test]
    public void UpdateDocumentMetadata_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext.HttpContext.User = anonymousUser;
        var request = new DocumentMetadataUpdateRequest(["HR"], ["Finance"], "Confidential");
        // Act & Assert
        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.UpdateDocumentMetadata(_testAgentId, Guid.NewGuid(), request, CancellationToken.None));
        Assert.That(exception.Message, Is.EqualTo("User is not authenticated."));
    }
    [Test]
    public async Task UpdateDocumentMetadata_WhenWebPageDocument_UpdatesMetadataAndReindexesSuccessfully()
    {
        // Arrange
        var tag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "HR Policy" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Success
        };
        _context.Tags.Add(tag);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        DocumentPermissionMetadata? capturedPermissions = null;
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(document.Url, _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid, DocumentPermissionMetadata, CancellationToken>((_, _, permissions, _) => capturedPermissions = permissions)
            .ReturnsAsync("new-knowledge-doc-id");
        var effectiveDate = new DateTime(2026, 1, 1);
        var expiredDate = new DateTime(2026, 12, 31);
        var request = new DocumentMetadataUpdateRequest(
            ["HR"],
            ["Finance"],
            "Confidential",
            "Jane Owner",
            "2.1",
            effectiveDate,
            expiredDate,
            ["VN", "SG"],
            ["NTG-VN"],
            ["L2", "Manager"],
            null,
            [tag.Id]);
        // Act
        var result = await _controller.UpdateDocumentMetadata(_testAgentId, document.Id, request, CancellationToken.None);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var response = okResult.Value as DocumentListItem;
        Assert.That(response, Is.Not.Null);
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.Roles, Is.EquivalentTo(new[] { "HR" }));
        Assert.That(updatedDocument.BusinessUnits, Is.EquivalentTo(new[] { "Finance" }));
        Assert.That(updatedDocument.SensitivityLevel, Is.EqualTo("Confidential"));
        Assert.That(updatedDocument.Owner, Is.EqualTo("Jane Owner"));
        Assert.That(updatedDocument.Version, Is.EqualTo("2.1"));
        Assert.That(updatedDocument.EffectiveDate, Is.EqualTo(effectiveDate));
        Assert.That(updatedDocument.ExpiredDate, Is.EqualTo(expiredDate));
        Assert.That(updatedDocument.Countries, Is.EquivalentTo(new[] { "VN", "SG" }));
        Assert.That(updatedDocument.LegalEntities, Is.EquivalentTo(new[] { "NTG-VN" }));
        Assert.That(updatedDocument.ApplicableLevels, Is.EquivalentTo(new[] { "L2", "Manager" }));
        Assert.That(updatedDocument.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(updatedDocument.KnowledgeDocId, Is.EqualTo("new-knowledge-doc-id"));
        Assert.That(response!.Owner, Is.EqualTo("Jane Owner"));
        Assert.That(response.Version, Is.EqualTo("2.1"));
        Assert.That(response.Countries, Is.EquivalentTo(new[] { "VN", "SG" }));
        Assert.That(response.LegalEntities, Is.EquivalentTo(new[] { "NTG-VN" }));
        Assert.That(response.ApplicableLevels, Is.EquivalentTo(new[] { "L2", "Manager" }));
        Assert.That(capturedPermissions, Is.Not.Null);
        Assert.That(capturedPermissions!.Countries, Is.EquivalentTo(new[] { "VN", "SG" }));
        Assert.That(capturedPermissions.LegalEntities, Is.EquivalentTo(new[] { "NTG-VN" }));
        Assert.That(capturedPermissions.ApplicableLevels, Is.EquivalentTo(new[] { "L2", "Manager" }));
        Assert.That(capturedPermissions.ApplicableTo, Is.EquivalentTo(new[] { "L2", "Manager" }));
        Assert.That(capturedPermissions.AllowedTags, Is.EquivalentTo(new[] { tag.Id.ToString() }));
        Assert.That(response.TagIds, Is.EquivalentTo(new[] { tag.Id }));
        Assert.That(response.Tags, Is.EquivalentTo(new[] { tag.Name }));
        var documentTags = await _context.DocumentTags.Where(dt => dt.DocumentId == document.Id).ToListAsync();
        Assert.That(documentTags.Select(dt => dt.TagId), Is.EquivalentTo(new[] { tag.Id }));
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync("old-knowledge-doc-id", _testAgentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDocumentMetadata_WhenLegacyTagNamesProvided_ResolvesExistingTagIds()
    {
        // Arrange
        var tag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "Benefits" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Success
        };
        _context.Tags.Add(tag);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        DocumentPermissionMetadata? capturedPermissions = null;
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(document.Url, _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid, DocumentPermissionMetadata, CancellationToken>((_, _, permissions, _) => capturedPermissions = permissions)
            .ReturnsAsync("new-knowledge-doc-id");
        var request = new DocumentMetadataUpdateRequest(["HR"], ["Finance"], "Internal", Tags: [tag.Name]);

        // Act
        var result = await _controller.UpdateDocumentMetadata(_testAgentId, document.Id, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That(capturedPermissions, Is.Not.Null);
        Assert.That(capturedPermissions!.AllowedTags, Is.EquivalentTo(new[] { tag.Id.ToString() }));
        Assert.That(await _context.DocumentTags.CountAsync(dt => dt.DocumentId == document.Id && dt.TagId == tag.Id), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateDocumentMetadata_WhenUnknownTagIdProvided_ReturnsBadRequestAndDoesNotReindex()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Success
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        var request = new DocumentMetadataUpdateRequest(["HR"], ["Finance"], "Internal", TagIds: [Guid.NewGuid()]);

        // Act
        var result = await _controller.UpdateDocumentMetadata(_testAgentId, document.Id, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        _mockKnowledgeService.Verify(x => x.ImportWebPageAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(await _context.DocumentTags.CountAsync(dt => dt.DocumentId == document.Id), Is.EqualTo(0));
    }

    [Test]
    public async Task UpdateDocumentMetadata_WhenReindexFails_RollsBackMetadataAndReturnsBadGateway()
    {
        // Arrange
        var originalTag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "Original" };
        var newTag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "New" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "policy.md",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.File,
            IngestStatus = IngestStatus.Success,
            Roles = ["Employee"],
            BusinessUnits = ["VN"],
            SensitivityLevel = "Internal",
            Owner = "Original Owner",
            Version = "1.0",
            Countries = ["VN"],
            LegalEntities = ["NTG"],
            ApplicableLevels = ["L1"],
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = originalTag.Id, Tag = originalTag }
            }
        };
        _context.Tags.AddRange(originalTag, newTag);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        _mockKnowledgeService.Setup(x => x.ExportDocumentAsync("old-knowledge-doc-id", It.IsAny<string>(), _testAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.KernelMemory.StreamableFileContent)null!);
        var request = new DocumentMetadataUpdateRequest(
            ["HR"],
            ["Finance"],
            "Confidential",
            "New Owner",
            "2.0",
            Countries: ["SG"],
            LegalEntities: ["NTS"],
            ApplicableLevels: ["L2"],
            TagIds: [newTag.Id]);
        // Act
        var result = await _controller.UpdateDocumentMetadata(_testAgentId, document.Id, request, CancellationToken.None);
        // Assert
        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(StatusCodes.Status502BadGateway));
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.IngestStatus, Is.EqualTo(IngestStatus.Failed));
        Assert.That(updatedDocument.IngestErrorMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(updatedDocument.KnowledgeDocId, Is.EqualTo("old-knowledge-doc-id"));
        Assert.That(updatedDocument.Roles, Is.EquivalentTo(new[] { "Employee" }));
        Assert.That(updatedDocument.BusinessUnits, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(updatedDocument.SensitivityLevel, Is.EqualTo("Internal"));
        Assert.That(updatedDocument.Owner, Is.EqualTo("Original Owner"));
        Assert.That(updatedDocument.Version, Is.EqualTo("1.0"));
        Assert.That(updatedDocument.Countries, Is.EquivalentTo(new[] { "VN" }));
        Assert.That(updatedDocument.LegalEntities, Is.EquivalentTo(new[] { "NTG" }));
        Assert.That(updatedDocument.ApplicableLevels, Is.EquivalentTo(new[] { "L1" }));
        var documentTags = await _context.DocumentTags.Where(dt => dt.DocumentId == document.Id).ToListAsync();
        Assert.That(documentTags.Select(dt => dt.TagId), Is.EquivalentTo(new[] { originalTag.Id }));
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Test]
    public async Task ReindexDocument_WhenDocumentNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _controller.ReindexDocument(_testAgentId, Guid.NewGuid(), CancellationToken.None);
        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
    [Test]
    public async Task ReindexDocument_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var tag = new Models.Tags.Tag { Id = Guid.NewGuid(), Name = "Private" };
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Failed,
            IngestErrorMessage = "previous failure",
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = tag.Id, Tag = tag }
            }
        };
        _context.Tags.Add(tag);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        DocumentPermissionMetadata? capturedPermissions = null;
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(document.Url, _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid, DocumentPermissionMetadata, CancellationToken>((_, _, permissions, _) => capturedPermissions = permissions)
            .ReturnsAsync("new-knowledge-doc-id");
        // Act
        var result = await _controller.ReindexDocument(_testAgentId, document.Id, CancellationToken.None);
        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(updatedDocument.IngestErrorMessage, Is.Null);
        Assert.That(capturedPermissions, Is.Not.Null);
        Assert.That(capturedPermissions!.AllowedTags, Is.EquivalentTo(new[] { tag.Id.ToString() }));
    }
    [Test]
    public async Task ReindexDocument_WhenFails_ReturnsBadGateway()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "policy.md",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.File,
            IngestStatus = IngestStatus.Success
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        _mockKnowledgeService.Setup(x => x.ExportDocumentAsync("old-knowledge-doc-id", It.IsAny<string>(), _testAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.KernelMemory.StreamableFileContent)null!);
        // Act
        var result = await _controller.ReindexDocument(_testAgentId, document.Id, CancellationToken.None);
        // Assert
        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(StatusCodes.Status502BadGateway));
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.IngestStatus, Is.EqualTo(IngestStatus.Failed));
    }

    [Test]
    public async Task UpdateDocumentApprovalStatus_WhenApproved_ReindexesAndSavesApproval()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Success,
            ApprovalStatus = ApprovalStatus.Pending
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(document.Url, _testAgentId, It.Is<DocumentPermissionMetadata>(p => p.ApprovalStatus == ApprovalStatus.Approved.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-knowledge-doc-id");

        // Act
        var result = await _controller.UpdateDocumentApprovalStatus(
            _testAgentId,
            document.Id,
            new DocumentApprovalUpdateRequest(ApprovalStatus.Approved),
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(updatedDocument.ApprovedByUserId, Is.EqualTo(_testUserId));
        Assert.That(updatedDocument.KnowledgeDocId, Is.EqualTo("new-knowledge-doc-id"));
        _mockKnowledgeService.Verify(x => x.RemoveDocumentAsync("old-knowledge-doc-id", _testAgentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateDocumentApprovalStatus_WhenOldKnowledgeRemovalFails_RollsBackApprovalAndKnowledgeDocId()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "https://example.com",
            Url = "https://example.com",
            AgentId = _testAgentId,
            KnowledgeDocId = "old-knowledge-doc-id",
            Type = DocumentType.WebPage,
            IngestStatus = IngestStatus.Success,
            ApprovalStatus = ApprovalStatus.Pending
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        _mockKnowledgeService
            .Setup(x => x.ImportWebPageAsync(document.Url, _testAgentId, It.Is<DocumentPermissionMetadata>(p => p.ApprovalStatus == ApprovalStatus.Approved.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-knowledge-doc-id");
        _mockKnowledgeService
            .Setup(x => x.RemoveDocumentAsync("old-knowledge-doc-id", _testAgentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        // Act
        var result = await _controller.UpdateDocumentApprovalStatus(
            _testAgentId,
            document.Id,
            new DocumentApprovalUpdateRequest(ApprovalStatus.Approved),
            CancellationToken.None);

        // Assert
        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status502BadGateway));
        var updatedDocument = await _context.Documents.FindAsync(document.Id);
        Assert.That(updatedDocument!.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
        Assert.That(updatedDocument.ApprovedByUserId, Is.Null);
        Assert.That(updatedDocument.KnowledgeDocId, Is.EqualTo("old-knowledge-doc-id"));
        Assert.That(updatedDocument.IngestStatus, Is.EqualTo(IngestStatus.Failed));
    }

    [Test]
    public async Task ImportWebPage_WhenUrlIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new ImportWebPageRequest("", null, new List<string>());
        // Act
        var result = await _controller.ImportWebPage(_testAgentId, request);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value, Is.EqualTo("URL is required."));
    }
    [Test]
    public void ImportWebPage_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext.HttpContext.User = anonymousUser;
        var request = new ImportWebPageRequest("https://example.com", null, new List<string>());
        // Act & Assert
        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.ImportWebPage(_testAgentId, request));
        Assert.That(exception.Message, Is.EqualTo("User is not authenticated."));
    }
    [Test]
    public async Task ImportWebPage_WhenValidRequest_ImportsSuccessfully()
    {
        // Arrange
        var url = "https://example.com";
        var folderId = Guid.NewGuid();
        var tags = new List<string> { Guid.NewGuid().ToString() };
        var request = new ImportWebPageRequest(url, folderId, tags);
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(url, _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("document-id");
        // Act
        var result = await _controller.ImportWebPage(_testAgentId, request);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo("document-id"));
        var savedDocument = await _context.Documents.FirstOrDefaultAsync();
        Assert.That(savedDocument, Is.Not.Null);
        Assert.That(savedDocument.Name, Is.EqualTo(url));
        Assert.That(savedDocument.Url, Is.EqualTo(url));
        Assert.That(savedDocument.Type, Is.EqualTo(DocumentType.WebPage));
        Assert.That(savedDocument.FolderId, Is.EqualTo(folderId));
        Assert.That(savedDocument.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(savedDocument.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
    }
    [Test]
    public async Task ImportWebPage_WhenKnowledgeServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var request = new ImportWebPageRequest("https://example.com", null, new List<string>());
        _mockKnowledgeService.Setup(x => x.ImportWebPageAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Import failed"));
        // Act
        var result = await _controller.ImportWebPage(_testAgentId, request);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value, Is.EqualTo("Failed to import webpage: Import failed"));
    }
    [Test]
    public async Task UploadTextContent_WhenContentIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new UploadTextContentRequest("Title", "", null, new List<string>());
        // Act
        var result = await _controller.UploadTextContent(_testAgentId, request);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value, Is.EqualTo("Content is required."));
    }
    [Test]
    public void UploadTextContent_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext.HttpContext.User = anonymousUser;
        var request = new UploadTextContentRequest("Title", "Content", null, new List<string>());
        // Act & Assert
        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.UploadTextContent(_testAgentId, request));
        Assert.That(exception.Message, Is.EqualTo("User is not authenticated."));
    }
    [Test]
    public async Task UploadTextContent_WhenValidRequest_UploadsSuccessfully()
    {
        // Arrange
        var content = "This is test content";
        var title = "Test Title";
        var folderId = Guid.NewGuid();
        var tags = new List<string> { Guid.NewGuid().ToString() };
        var request = new UploadTextContentRequest(title, content, folderId, tags);
        _mockKnowledgeService.Setup(x => x.ImportTextContentAsync(content, $"{title}.txt", _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("knowledge-doc-id");
        // Act
        var result = await _controller.UploadTextContent(_testAgentId, request);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var savedDocument = await _context.Documents.FirstOrDefaultAsync();
        Assert.That(savedDocument, Is.Not.Null);
        Assert.That(savedDocument.Name, Is.EqualTo($"{title}.txt"));
        Assert.That(savedDocument.Type, Is.EqualTo(DocumentType.Text));
        Assert.That(savedDocument.FolderId, Is.EqualTo(folderId));
        Assert.That(savedDocument.KnowledgeDocId, Is.EqualTo("knowledge-doc-id"));
        Assert.That(savedDocument.IngestStatus, Is.EqualTo(IngestStatus.Success));
        Assert.That(savedDocument.ApprovalStatus, Is.EqualTo(ApprovalStatus.Pending));
    }
    [Test]
    public async Task UploadTextContent_WhenNoTitle_UsesDefaultTitle()
    {
        // Arrange
        var content = "This is test content";
        var request = new UploadTextContentRequest("", content, null, new List<string>());
        _mockKnowledgeService.Setup(x => x.ImportTextContentAsync(content, "Text Content.txt", _testAgentId, It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("knowledge-doc-id");
        // Act
        var result = await _controller.UploadTextContent(_testAgentId, request);
        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var savedDocument = await _context.Documents.FirstOrDefaultAsync();
        Assert.That(savedDocument, Is.Not.Null);
        Assert.That(savedDocument.Name, Is.EqualTo("Text Content.txt"));
    }
    [Test]
    public async Task UploadTextContent_WhenKnowledgeServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var request = new UploadTextContentRequest("Title", "Content", null, new List<string>());
        _mockKnowledgeService.Setup(x => x.ImportTextContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DocumentPermissionMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Import failed"));
        // Act
        var result = await _controller.UploadTextContent(_testAgentId, request);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.Value, Is.EqualTo("Failed to upload text content: Import failed"));
    }
    [Test]
    public async Task GetDocumentById_WhenDocumentNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, Guid.NewGuid(), CancellationToken.None);
        // Assert
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
    [Test]
    public async Task GetDocumentById_WhenFileDocumentWithoutKnowledgeDocId_ReturnsNotFound()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test.txt",
            AgentId = _testAgentId,
            Type = DocumentType.File,
            KnowledgeDocId = null // No knowledge document ID
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, document.Id, CancellationToken.None);
        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("No knowledge document id."));
    }
    [Test]
    public async Task GetDocumentById_WhenWebPageDocument_HandlesDownload()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "Example Page",
            AgentId = _testAgentId,
            Type = DocumentType.WebPage,
            Url = "https://example.com"
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        // Note: This test would require mocking HttpClient which is complex
        // In practice, you might want to extract the HTTP logic to a separate service
        // For now, we'll test the URL validation part
        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, document.Id, CancellationToken.None);
        // Assert
        // This would normally test the HTTP download, but since we can't easily mock HttpClient
        // we're just verifying the method executes without null reference exceptions
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetDocumentById_WhenNonAdminHasDocumentTagAccess_AllowsDownloadFlow()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        ], "mock"));
        _downloadController.ControllerContext.HttpContext.User = user;
        _mockRbacService
            .Setup(x => x.ResolveAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationContext
            {
                UserId = _testUserId,
                AllowedTags = [tagId.ToString()],
                SensitivityLevel = "Internal"
            });

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test.txt",
            AgentId = _testAgentId,
            Type = DocumentType.File,
            KnowledgeDocId = null,
            ApprovalStatus = ApprovalStatus.Approved,
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = tagId }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, document.Id, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        Assert.That(((NotFoundObjectResult)result).Value, Is.EqualTo("No knowledge document id."));
    }

    [Test]
    public async Task GetDocumentById_WhenNonAdminLacksDocumentTagAccess_ReturnsForbid()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        ], "mock"));
        _downloadController.ControllerContext.HttpContext.User = user;
        _mockRbacService
            .Setup(x => x.ResolveAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationContext
            {
                UserId = _testUserId,
                AllowedTags = [Guid.NewGuid().ToString()],
                SensitivityLevel = "Internal"
            });

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test.txt",
            AgentId = _testAgentId,
            Type = DocumentType.File,
            KnowledgeDocId = null,
            ApprovalStatus = ApprovalStatus.Approved,
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = Guid.NewGuid() }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, document.Id, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetDocumentById_WhenUserCannotBeResolved_ReturnsUnauthorized()
    {
        // Arrange
        _downloadController.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        _mockIdentityResolver
            .Setup(x => x.ResolveUserIdAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = "test.txt",
            AgentId = _testAgentId,
            Type = DocumentType.File,
            KnowledgeDocId = null,
            ApprovalStatus = ApprovalStatus.Approved,
            DocumentTags = new List<DocumentTag>
            {
                new DocumentTag { TagId = Guid.NewGuid() }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _downloadController.GetDocumentById(_testAgentId, document.Id, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
    }

    private static FormFile CreateTestFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
        return formFile;
    }
}
