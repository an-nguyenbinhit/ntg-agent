using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.Configuration;
using AskHR.Orchestrator.Services.Knowledge;

namespace AskHR.Orchestrator.Tests.Services;

[TestFixture]
public class DocumentWatcherServiceTests
{
    private Mock<IDocumentIngestionService> _mockIngestionService;
    private ServiceProvider _serviceProvider;
    private string _watchPath;
    private string _databaseName;

    [SetUp]
    public void Setup()
    {
        _mockIngestionService = new Mock<IDocumentIngestionService>();
        _databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<AgentDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        services.AddScoped<IDocumentIngestionService>(_ => _mockIngestionService.Object);
        _serviceProvider = services.BuildServiceProvider();

        _watchPath = Path.Combine(Path.GetTempPath(), $"askhr-watch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_watchPath);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_watchPath))
        {
            Directory.Delete(_watchPath, recursive: true);
        }
    }

    private DocumentWatcherService CreateService(IngestionSourceOptions options)
    {
        return new DocumentWatcherService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            Mock.Of<ILogger<DocumentWatcherService>>());
    }

    private async Task<string> CreateWatchedFileAsync(string fileName, string content = "policy content")
    {
        var path = Path.Combine(_watchPath, fileName);
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Test]
    public async Task ExecuteAsync_WhenModeIsAdminUpload_DoesNothing()
    {
        using var service = CreateService(new IngestionSourceOptions { Mode = "AdminUpload", WatchPath = _watchPath });

        await service.StartAsync(CancellationToken.None);

        Assert.That(service.ExecuteTask, Is.Not.Null);
        // The service should exit on its own (without StopAsync) because the mode disables it.
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(service.ExecuteTask.IsCompletedSuccessfully, Is.True);
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task ExecuteAsync_WhenWatchPathDoesNotExist_DoesNothing()
    {
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = Path.Combine(_watchPath, "missing-subfolder")
        });

        await service.StartAsync(CancellationToken.None);

        Assert.That(service.ExecuteTask, Is.Not.Null);
        // The service should exit on its own (without StopAsync) because the path is missing.
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(service.ExecuteTask.IsCompletedSuccessfully, Is.True);
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public void QueueFile_WhenFileTypeIsUnsupported_IsIgnored()
    {
        using var service = CreateService(new IngestionSourceOptions { Mode = IngestionSourceOptions.WatchFolderMode, WatchPath = _watchPath });

        service.QueueFile(Path.Combine(_watchPath, "malware.exe"));

        Assert.That(service.PendingFileCount, Is.Zero);
    }

    [Test]
    public void QueueFile_WhenFileTypeIsSupported_IsQueued()
    {
        using var service = CreateService(new IngestionSourceOptions { Mode = IngestionSourceOptions.WatchFolderMode, WatchPath = _watchPath });

        service.QueueFile(Path.Combine(_watchPath, "policy.md"));
        service.QueueFile(Path.Combine(_watchPath, "policy.md")); // duplicate event coalesces

        Assert.That(service.PendingFileCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ProcessDueFilesAsync_WhenDebounceWindowNotElapsed_DoesNotIngest()
    {
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = Guid.NewGuid(),
            DebounceSeconds = 60
        });
        var filePath = await CreateWatchedFileAsync("policy.md");
        service.QueueFile(filePath);

        await service.ProcessDueFilesAsync(CancellationToken.None);

        Assert.That(service.PendingFileCount, Is.EqualTo(1));
        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessDueFilesAsync_WhenDebounceWindowElapsed_IngestsFile()
    {
        var agentId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var systemUserId = Guid.NewGuid();
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = agentId,
            FolderId = folderId,
            SystemUserId = systemUserId,
            DebounceSeconds = -1
        });
        var filePath = await CreateWatchedFileAsync("policy.md");
        service.QueueFile(filePath);

        await service.ProcessDueFilesAsync(CancellationToken.None);

        Assert.That(service.PendingFileCount, Is.Zero);
        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), "policy.md", agentId, folderId, systemUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessFileAsync_WhenFileNoLongerExists_DoesNotIngest()
    {
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = Guid.NewGuid()
        });

        await service.ProcessFileAsync(Path.Combine(_watchPath, "deleted.md"), CancellationToken.None);

        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessFileAsync_WhenFileIsEmpty_DoesNotIngest()
    {
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = Guid.NewGuid()
        });
        var filePath = await CreateWatchedFileAsync("policy.md", content: string.Empty);

        await service.ProcessFileAsync(filePath, CancellationToken.None);

        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessFileAsync_WhenNoAgentConfiguredOrPublished_SkipsFileWithoutThrowing()
    {
        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = null
        });
        var filePath = await CreateWatchedFileAsync("policy.md");

        Assert.DoesNotThrowAsync(() => service.ProcessFileAsync(filePath, CancellationToken.None));
        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ProcessFileAsync_WhenAgentIdNotConfigured_FallsBackToPublishedDefaultAgent()
    {
        var defaultAgentId = Guid.NewGuid();
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
            dbContext.Agents.Add(new Models.Agents.Agent
            {
                Id = defaultAgentId,
                Name = "Default Agent",
                IsPublished = true,
                IsDefault = true
            });
            await dbContext.SaveChangesAsync();
        }

        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = null
        });
        var filePath = await CreateWatchedFileAsync("policy.md");

        await service.ProcessFileAsync(filePath, CancellationToken.None);

        _mockIngestionService.Verify(
            x => x.IngestFileAsync(It.IsAny<Stream>(), "policy.md", defaultAgentId, It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessFileAsync_WhenIngestionThrows_DoesNotPropagate()
    {
        _mockIngestionService
            .Setup(x => x.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("backend down"));

        using var service = CreateService(new IngestionSourceOptions
        {
            Mode = IngestionSourceOptions.WatchFolderMode,
            WatchPath = _watchPath,
            AgentId = Guid.NewGuid()
        });
        var filePath = await CreateWatchedFileAsync("policy.md");

        Assert.DoesNotThrowAsync(() => service.ProcessFileAsync(filePath, CancellationToken.None));
    }
}
