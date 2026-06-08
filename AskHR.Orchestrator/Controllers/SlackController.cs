using System.Text.RegularExpressions;
using System.Text.Json;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Slack;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Controllers;

[Route("api/slack")]
[ApiController]
public sealed partial class SlackController : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IPolicyAnswerService _answerService;
    private readonly ISlackResponseClient _slackResponseClient;
    private readonly ISlackRequestVerifier _requestVerifier;
    private readonly ISlackEventDeduplicator _eventDeduplicator;
    private readonly ISlackIdentityResolver _identityResolver;
    private readonly AgentDbContext _dbContext;
    private readonly IOptions<SlackOptions> _options;

    public SlackController(
        IPolicyAnswerService answerService,
        ISlackResponseClient slackResponseClient,
        ISlackRequestVerifier requestVerifier,
        ISlackEventDeduplicator eventDeduplicator,
        ISlackIdentityResolver identityResolver,
        AgentDbContext dbContext,
        IOptions<SlackOptions> options)
    {
        _answerService = answerService ?? throw new ArgumentNullException(nameof(answerService));
        _slackResponseClient = slackResponseClient ?? throw new ArgumentNullException(nameof(slackResponseClient));
        _requestVerifier = requestVerifier ?? throw new ArgumentNullException(nameof(requestVerifier));
        _eventDeduplicator = eventDeduplicator ?? throw new ArgumentNullException(nameof(eventDeduplicator));
        _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    [HttpPost("events")]
    public async Task<ActionResult<object>> EventsAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (!_requestVerifier.Verify(Request, rawBody))
        {
            return Unauthorized();
        }

        var request = JsonSerializer.Deserialize<SlackEventsRequest>(rawBody, SerializerOptions);
        if (request is null)
        {
            return BadRequest("Invalid Slack event payload.");
        }

        if (!_eventDeduplicator.TryAccept(request.EventId))
        {
            return Ok(new SlackGatewayResponse(true, null, null, null, "duplicate-event"));
        }

        if (string.Equals(request.Type, "url_verification", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { challenge = request.Challenge });
        }

        var slackEvent = request.Event;
        if (slackEvent is null)
        {
            return BadRequest("Missing Slack event payload.");
        }

        if (!string.IsNullOrWhiteSpace(slackEvent.BotId))
        {
            return Ok(new SlackGatewayResponse(true, slackEvent.Channel, slackEvent.ThreadTs, null, "ignored-bot-event"));
        }

        if (!string.Equals(slackEvent.Type, "app_mention", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(slackEvent.Type, "message", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new SlackGatewayResponse(true, slackEvent.Channel, slackEvent.ThreadTs, null, "ignored-event-type"));
        }

        if (string.IsNullOrWhiteSpace(slackEvent.Text) || string.IsNullOrWhiteSpace(slackEvent.Channel))
        {
            return Ok(new SlackGatewayResponse(true, slackEvent.Channel, slackEvent.ThreadTs, null, "empty-message"));
        }

        var agentId = await ResolveAgentIdAsync(cancellationToken);
        var threadTs = slackEvent.ThreadTs ?? slackEvent.Ts ?? string.Empty;
        var question = MentionRegex().Replace(slackEvent.Text, string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return Ok(new SlackGatewayResponse(true, slackEvent.Channel, threadTs, null, "empty-question-after-normalization"));
        }

        var authorization = await _identityResolver.ResolveAsync(slackEvent.User, cancellationToken);
        var answer = await _answerService.AnswerAsync(
            new AskHrRequest(
                agentId,
                question,
                Channel: "slack",
                ChannelUserId: slackEvent.User,
                ThreadId: threadTs,
                Metadata: new Dictionary<string, string>
                {
                    ["slackChannel"] = slackEvent.Channel,
                    ["slackThreadTs"] = threadTs
                }),
            authorization,
            cancellationToken);

        await _slackResponseClient.PostAnswerAsync(slackEvent.Channel, threadTs, answer, cancellationToken);
        return Ok(new SlackGatewayResponse(true, slackEvent.Channel, threadTs, answer.AnswerText, null));
    }

    private async Task<Guid> ResolveAgentIdAsync(CancellationToken cancellationToken)
    {
        if (_options.Value.DefaultAgentId.HasValue)
        {
            return _options.Value.DefaultAgentId.Value;
        }

        return await _dbContext.Agents
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    [GeneratedRegex(@"<@[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();
}
