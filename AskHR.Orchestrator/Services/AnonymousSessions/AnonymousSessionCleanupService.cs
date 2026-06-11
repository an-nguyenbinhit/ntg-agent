using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.AnonymousSessions;

namespace AskHR.Orchestrator.Services.AnonymousSessions;

public class AnonymousSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnonymousSessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public AnonymousSessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<AnonymousSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnonymousSessionCleanupService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during anonymous session cleanup.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("AnonymousSessionCleanupService is stopping.");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<AnonymousUserSettings>>().Value;

        var cutoffDate = DateTime.UtcNow.AddDays(-settings.SessionExpirationDays);

        var expiredSessions = await context.AnonymousSessions
            .Where(s => s.LastMessageAt < cutoffDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count > 0)
        {
            var expiredSessionIds = expiredSessions.Select(s => s.SessionId).ToList();
            
            var expiredConversations = await context.Conversations
                .Where(c => c.SessionId.HasValue && expiredSessionIds.Contains(c.SessionId.Value))
                .ToListAsync(cancellationToken);

            if (expiredConversations.Count > 0)
            {
                context.Conversations.RemoveRange(expiredConversations);
            }

            context.AnonymousSessions.RemoveRange(expiredSessions);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} expired anonymous sessions and their conversations (older than {Days} days)",
                expiredSessions.Count,
                settings.SessionExpirationDays);
        }
    }
}
