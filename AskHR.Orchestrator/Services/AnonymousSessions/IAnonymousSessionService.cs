using AskHR.Common.Dtos.AnonymousSessions;

namespace AskHR.Orchestrator.Services.AnonymousSessions;

public interface IAnonymousSessionService
{
    Task<RateLimitStatus> CheckRateLimitAsync(Guid sessionId, string? ipAddress);
    
    Task IncrementMessageCountAsync(Guid sessionId, string? ipAddress);
}
