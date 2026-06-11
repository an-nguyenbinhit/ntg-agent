using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AskHR.Common.Dtos.AnonymousSessions;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Dtos;
using AskHR.Orchestrator.Models.AnonymousSessions;

namespace AskHR.Orchestrator.Services.AnonymousSessions;

public class AnonymousSessionService : IAnonymousSessionService
{
    private readonly AgentDbContext _context;
    private readonly AnonymousUserSettings _settings;
    private readonly IIpAddressService _ipAddressService;
    private readonly ILogger<AnonymousSessionService> _logger;

    public AnonymousSessionService(
        AgentDbContext context,
        IOptions<AnonymousUserSettings> settings,
        IIpAddressService ipAddressService,
        ILogger<AnonymousSessionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _ipAddressService = ipAddressService ?? throw new ArgumentNullException(nameof(ipAddressService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RateLimitStatus> CheckRateLimitAsync(Guid sessionId, string? ipAddress)
    {

        var session = await GetOrCreateSessionAsync(sessionId, ipAddress);

        // Check if time-based reset is needed
        var hoursSinceReset = (DateTime.UtcNow - session.ResetAt).TotalHours;
        if (hoursSinceReset >= _settings.ResetPeriodHours)
        {
            await ResetSessionAsync(session);
        }

        var status = new RateLimitStatus
        {
            CurrentCount = session.MessageCount,
            MaxMessages = _settings.MaxMessagesPerSession,
            RemainingMessages = Math.Max(0, _settings.MaxMessagesPerSession - session.MessageCount),
            ResetAt = session.ResetAt.AddHours(_settings.ResetPeriodHours)
        };

        // Check if manually blocked
        if (session.IsBlocked)
        {
            status.CanSendMessage = false;
            status.IsReadOnlyMode = false;
            status.BlockReason = "blocked";
            
            _logger.LogWarning(
                "Blocked anonymous session {SessionId} attempted to send message",
                sessionId);
            
            return status;
        }

        // Check IP-based limit
        if (_settings.EnableIpTracking && !string.IsNullOrEmpty(ipAddress))
        {
            var ipAllowed = await _ipAddressService.IsIpAllowedAsync(ipAddress);
            if (!ipAllowed)
            {
                status.CanSendMessage = false;
                status.IsReadOnlyMode = true;
                status.BlockReason = "ip_limit";
                
                _logger.LogWarning(
                    "IP address {IpAddress} exceeded daily limit for session {SessionId}",
                    ipAddress,
                    sessionId);
                
                return status;
            }
        }

        // Check session limit
        if (session.MessageCount >= _settings.MaxMessagesPerSession)
        {
            status.CanSendMessage = false;
            status.IsReadOnlyMode = true;
            status.BlockReason = "session_limit";
            
            _logger.LogInformation(
                "Anonymous session {SessionId} reached message limit: {Count}/{Max}",
                sessionId,
                session.MessageCount,
                _settings.MaxMessagesPerSession);
            
            return status;
        }

        status.CanSendMessage = true;
        status.IsReadOnlyMode = false;
        return status;
    }

    public async Task IncrementMessageCountAsync(Guid sessionId, string? ipAddress)
    {
        var session = await GetOrCreateSessionAsync(sessionId, ipAddress);
        session.MessageCount++;
        session.LastMessageAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<AnonymousSession> GetOrCreateSessionAsync(Guid sessionId, string? ipAddress)
    {
        var session = await _context.AnonymousSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            session = new AnonymousSession
            {
                SessionId = sessionId,
                IpAddress = ipAddress,
                MessageCount = 0
            };
            _context.AnonymousSessions.Add(session);
            await _context.SaveChangesAsync();
        }
        else if (session.IpAddress != ipAddress && !string.IsNullOrEmpty(ipAddress))
        {
            // Update IP if changed (user might be on different network)
            session.IpAddress = ipAddress;
            await _context.SaveChangesAsync();
        }

        return session;
    }

    private async Task ResetSessionAsync(AnonymousSession session)
    {
        session.MessageCount = 0;
        session.ResetAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }


}
