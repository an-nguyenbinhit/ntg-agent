using System.Text.RegularExpressions;
using System.Text.Json;
using AskHR.Common.Dtos.Answers;
using AskHR.Common.Dtos.Slack;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Controllers;

[Route("api/slack")]
[ApiController]
public sealed partial class SlackController : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string LoadingMessageText = "Đang xử lý câu hỏi của bạn... :hourglass_flowing_sand:";
    private const string TimeoutMessageText = "Xin lỗi, việc xử lý câu hỏi mất nhiều thời gian hơn dự kiến. Vui lòng thử lại sau.";
    private const string PipelineErrorMessageText = "Xin lỗi, đã có lỗi xảy ra khi xử lý câu hỏi của bạn. Vui lòng thử lại sau hoặc liên hệ HR.";

    private readonly IPolicyAnswerService _answerService;
    private readonly ISlackResponseClient _slackResponseClient;
    private readonly ISlackRequestVerifier _requestVerifier;
    private readonly ISlackEventDeduplicator _eventDeduplicator;
    private readonly ISlackIdentityResolver _identityResolver;
    private readonly AgentDbContext _dbContext;
    private readonly IOptions<SlackOptions> _options;
    private readonly ILogger<SlackController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SlackController(
        IPolicyAnswerService answerService,
        ISlackResponseClient slackResponseClient,
        ISlackRequestVerifier requestVerifier,
        ISlackEventDeduplicator eventDeduplicator,
        ISlackIdentityResolver identityResolver,
        AgentDbContext dbContext,
        IOptions<SlackOptions> options,
        ILogger<SlackController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _answerService = answerService ?? throw new ArgumentNullException(nameof(answerService));
        _slackResponseClient = slackResponseClient ?? throw new ArgumentNullException(nameof(slackResponseClient));
        _requestVerifier = requestVerifier ?? throw new ArgumentNullException(nameof(requestVerifier));
        _eventDeduplicator = eventDeduplicator ?? throw new ArgumentNullException(nameof(eventDeduplicator));
        _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
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

        // Slack requires the events endpoint to ack within ~3 seconds, otherwise it drops the
        // connection and retries the event. The answer pipeline can easily exceed that, so the
        // actual processing (identity resolution, loading/typing message, pipeline call, and the
        // final chat.update) runs detached on a background task in its own DI scope — independent
        // of HttpContext.RequestAborted — while we ack the event immediately below.
        var pipelineTimeout = TimeSpan.FromSeconds(_options.Value.PipelineTimeoutSeconds);
        _ = ProcessQuestionInBackgroundAsync(agentId, question, slackEvent.Channel, threadTs, slackEvent.User, pipelineTimeout);

        return Ok(new SlackGatewayResponse(true, slackEvent.Channel, threadTs, null, "processing"));
    }

    private async Task ProcessQuestionInBackgroundAsync(
        Guid agentId,
        string question,
        string channel,
        string threadTs,
        string? slackUserId,
        TimeSpan pipelineTimeout)
    {
        using var scope = _scopeFactory.CreateScope();
        var answerService = scope.ServiceProvider.GetRequiredService<IPolicyAnswerService>();
        var responseClient = scope.ServiceProvider.GetRequiredService<ISlackResponseClient>();
        var identityResolver = scope.ServiceProvider.GetRequiredService<ISlackIdentityResolver>();

        using var pipelineCts = new CancellationTokenSource(pipelineTimeout);
        var pipelineToken = pipelineCts.Token;

        string? loadingMessageTs = null;
        try
        {
            loadingMessageTs = await responseClient.PostStatusMessageAsync(channel, threadTs, LoadingMessageText, pipelineToken);

            var authorization = await identityResolver.ResolveAsync(slackUserId, pipelineToken);
            var answer = await answerService.AnswerAsync(
                new AskHrRequest(
                    agentId,
                    question,
                    Channel: "slack",
                    ChannelUserId: slackUserId,
                    ThreadId: threadTs,
                    Metadata: new Dictionary<string, string>
                    {
                        ["slackChannel"] = channel,
                        ["slackThreadTs"] = threadTs
                    }),
                authorization,
                pipelineToken);

            if (loadingMessageTs is not null)
            {
                await responseClient.UpdateMessageAsync(channel, loadingMessageTs, answer.AnswerText, CancellationToken.None);
            }
            else
            {
                await responseClient.PostAnswerAsync(channel, threadTs, answer, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Slack answer pipeline timed out after {TimeoutSeconds}s for channel {Channel}", pipelineTimeout.TotalSeconds, channel);
            await ReportPipelineFailureAsync(responseClient, channel, threadTs, loadingMessageTs, TimeoutMessageText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slack answer pipeline failed for channel {Channel}", channel);
            await ReportPipelineFailureAsync(responseClient, channel, threadTs, loadingMessageTs, PipelineErrorMessageText);
        }
    }

    private static async Task ReportPipelineFailureAsync(ISlackResponseClient responseClient, string channel, string threadTs, string? loadingMessageTs, string errorText)
    {
        if (loadingMessageTs is not null)
        {
            await responseClient.UpdateMessageAsync(channel, loadingMessageTs, errorText, CancellationToken.None);
        }
        else
        {
            await responseClient.PostStatusMessageAsync(channel, threadTs, errorText, CancellationToken.None);
        }
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
