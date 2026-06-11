using System.Text.RegularExpressions;
using AskHR.Common.Dtos.Answers;
using AskHR.Orchestrator.Channels.Teams;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Services.Answers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AskHR.Orchestrator.Controllers;

[Route("api/messages")]
[ApiController]
public sealed partial class TeamsGatewayController : ControllerBase
{
    private const string LoadingMessageText = "Processing your HR question...";
    private const string TimeoutMessageText = "The HR answer pipeline timed out. Please try again.";
    private const string PipelineErrorMessageText = "The HR answer pipeline failed. Please try again or contact HR.";

    private readonly AgentDbContext _dbContext;
    private readonly IOptions<TeamsOptions> _options;
    private readonly ITeamsActivityDeduplicator _activityDeduplicator;
    private readonly ILogger<TeamsGatewayController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TeamsGatewayController(
        AgentDbContext dbContext,
        IOptions<TeamsOptions> options,
        ITeamsActivityDeduplicator activityDeduplicator,
        ILogger<TeamsGatewayController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _activityDeduplicator = activityDeduplicator ?? throw new ArgumentNullException(nameof(activityDeduplicator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    [HttpPost]
    public async Task<ActionResult<TeamsGatewayResponse>> MessagesAsync([FromBody] TeamsActivity activity, CancellationToken cancellationToken)
    {
        if (activity is null)
        {
            return BadRequest();
        }

        if (!string.Equals(activity.Type, "message", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new TeamsGatewayResponse(true, activity.Conversation?.Id, activity.Id, "ignored-activity-type"));
        }

        if (!_activityDeduplicator.TryAccept(activity.Id))
        {
            return Ok(new TeamsGatewayResponse(true, activity.Conversation?.Id, activity.Id, "duplicate-activity"));
        }

        var question = NormalizeQuestion(activity.Text);
        if (string.IsNullOrWhiteSpace(question))
        {
            return Ok(new TeamsGatewayResponse(true, activity.Conversation?.Id, activity.Id, "empty-question"));
        }

        var agentId = await ResolveAgentIdAsync(cancellationToken);
        var pipelineTimeout = TimeSpan.FromSeconds(_options.Value.PipelineTimeoutSeconds);
        _ = ProcessQuestionInBackgroundAsync(agentId, question, activity, pipelineTimeout);

        return Ok(new TeamsGatewayResponse(true, activity.Conversation?.Id, activity.Id, "processing"));
    }

    private async Task ProcessQuestionInBackgroundAsync(Guid agentId, string question, TeamsActivity activity, TimeSpan pipelineTimeout)
    {
        using var scope = _scopeFactory.CreateScope();
        var answerService = scope.ServiceProvider.GetRequiredService<IPolicyAnswerService>();
        var responseClient = scope.ServiceProvider.GetRequiredService<ITeamsResponseClient>();
        var identityResolver = scope.ServiceProvider.GetRequiredService<ITeamsIdentityResolver>();

        using var pipelineCts = new CancellationTokenSource(pipelineTimeout);
        var pipelineToken = pipelineCts.Token;

        try
        {
            await responseClient.PostStatusMessageAsync(activity, LoadingMessageText, pipelineToken);

            var authorization = await identityResolver.ResolveAsync(activity, pipelineToken);
            var answer = await answerService.AnswerAsync(
                new AskHrRequest(
                    agentId,
                    question,
                    Channel: "teams",
                    ChannelUserId: activity.From?.AadObjectId ?? activity.From?.Id,
                    ThreadId: activity.Conversation?.Id,
                    Metadata: BuildMetadata(activity)),
                authorization,
                pipelineToken);

            await responseClient.PostAnswerAsync(activity, answer, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Teams answer pipeline timed out after {TimeoutSeconds}s for activity {ActivityId}", pipelineTimeout.TotalSeconds, activity.Id);
            await responseClient.PostStatusMessageAsync(activity, TimeoutMessageText, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams answer pipeline failed for activity {ActivityId}", activity.Id);
            await responseClient.PostStatusMessageAsync(activity, PipelineErrorMessageText, CancellationToken.None);
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

    private static Dictionary<string, string> BuildMetadata(TeamsActivity activity)
    {
        var metadata = new Dictionary<string, string>();
        AddIfPresent(metadata, "teamsConversationType", activity.Conversation?.ConversationType);
        AddIfPresent(metadata, "teamsTenantId", activity.Conversation?.TenantId ?? activity.ChannelData?.Tenant?.Id);
        AddIfPresent(metadata, "teamsTeamId", activity.ChannelData?.Team?.Id);
        AddIfPresent(metadata, "teamsChannelId", activity.ChannelData?.Channel?.Id);
        AddIfPresent(metadata, "teamsActivityId", activity.Id);
        return metadata;
    }

    private static void AddIfPresent(Dictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static string NormalizeQuestion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var withoutMention = MentionRegex().Replace(text, string.Empty);
        var withoutHtmlTags = HtmlTagRegex().Replace(withoutMention, string.Empty);
        return System.Net.WebUtility.HtmlDecode(withoutHtmlTags).Trim();
    }

    [GeneratedRegex("<at>.*?</at>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
}

