using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory;
using AskHR.Orchestrator.Data;
using AskHR.Orchestrator.Models.AnonymousSessions;
using AskHR.Orchestrator.Models.Configuration;
using AskHR.Orchestrator.Services.Agents;
using AskHR.Orchestrator.Services.Answers;
using AskHR.Orchestrator.Services.AnonymousSessions;
using AskHR.Orchestrator.Services.Audit;
using AskHR.Orchestrator.Services.DocumentAnalysis;
using AskHR.Orchestrator.Services.Knowledge;
using AskHR.Orchestrator.Services.Memory;
using AskHR.Orchestrator.Services.ModelRouting;
using AskHR.Orchestrator.Services.Security;
using AskHR.Orchestrator.Services.Slack;
using AskHR.Orchestrator.Services.TokenTracking;
using AskHR.ServiceDefaults;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

const string SourceName = "AskHR.Orchestrator";
const string ServiceName = "Orchestrator";

var builder = WebApplication.CreateBuilder(args);

// Endpoint to the Aspire Dashboard
var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? throw new ConfigurationException("OTEL_EXPORTER_OTLP_ENDPOINT configuration key is required but not found");

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(ServiceName);

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(SourceName)
    .AddSource("*Microsoft.Extensions.AI") // Listen to the Experimental.Microsoft.Extensions.AI source for chat client telemetry
    .AddSource("*Microsoft.Extensions.Agents*") // Listen to the Experimental.Microsoft.Extensions.Agents source for agent telemetry
    .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter(SourceName)
    .AddMeter("*Microsoft.Agents.AI") // Agent Framework metrics
    .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Debug);
});

using var activitySource = new ActivitySource(SourceName);
using var meter = new Meter(SourceName);

// Create custom metrics
var interactionCounter = meter.CreateCounter<int>("agent_interactions_total", description: "Total number of agent interactions");
var responseTimeHistogram = meter.CreateHistogram<double>("agent_response_time_seconds", description: "Agent response time in seconds");

builder.AddServiceDefaults();

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<LongTermMemorySettings>(builder.Configuration.GetSection("LongTermMemory"));
builder.Services.Configure<DocumentIntelligenceSettings>(builder.Configuration.GetSection("Azure:DocumentIntelligence"));
builder.Services.Configure<ModelRoutingOptions>(builder.Configuration.GetSection("ModelRouting"));
builder.Services.Configure<AnswerPipelineOptions>(builder.Configuration.GetSection("AnswerPipeline"));
builder.Services.Configure<SlackOptions>(builder.Configuration.GetSection("Slack"));

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("../key/"))
    .SetApplicationName("AskHR");

builder.Services.Configure<AnonymousUserSettings>(
    builder.Configuration.GetSection("AnonymousUserSettings"));

builder.Services.AddScoped<IAgentFactory,AgentFactory>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<IKnowledgeService, KernelMemoryKnowledge>();
builder.Services.AddScoped<IModelRouter, ModelRouter>();
builder.Services.AddScoped<IChatClientFactory, ProviderChatClientFactory>();
builder.Services.AddScoped<IModelGateway, ModelGateway>();
builder.Services.AddScoped<IPolicyAnswerService, PolicyAnswerService>();
builder.Services.AddScoped<IAuditTextProtector, AuditTextProtector>();
builder.Services.AddScoped<IAuditEventSink, LoggingAuditEventSink>();
builder.Services.AddScoped<IIdentityResolver, HttpContextIdentityResolver>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISlackRequestVerifier, SlackRequestVerifier>();
builder.Services.AddSingleton<ISlackEventDeduplicator, MemorySlackEventDeduplicator>();
builder.Services.AddScoped<ISlackIdentityResolver, SlackIdentityResolver>();
builder.Services.AddScoped<IUserMemoryService, UserMemoryService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<ITokenTrackingService, TokenTrackingService>();
builder.Services.AddScoped<IAnonymousSessionService, AnonymousSessionService>();
builder.Services.AddScoped<IIpAddressService, IpAddressService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ISlackResponseClient, SlackResponseClient>(client =>
{
    client.BaseAddress = new Uri("https://slack.com/api/");
});
builder.Services.AddHttpClient<ISlackIdentityResolver, SlackIdentityResolver>(client =>
{
    client.BaseAddress = new Uri("https://slack.com/api/");
});

builder.Services.AddScoped<IKernelMemory>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = Environment.GetEnvironmentVariable($"services__askhr-knowledge__https__0") 
                   ?? Environment.GetEnvironmentVariable($"services__askhr-knowledge__http__0") 
                   ?? throw new InvalidOperationException("KernelMemory Endpoint configuration is required");
    var apiKey = configuration["KernelMemory:ApiKey"] 
                ?? throw new InvalidOperationException("KernelMemory:ApiKey configuration is required");

    return new MemoryWebClient(endpoint, apiKey);
});

// Typed HttpClient gateway for knowledge backend introspection (read-only Admin panel).
// Uses the same Kernel Memory endpoint + API key as the MemoryWebClient above.
builder.Services.AddHttpClient<IKnowledgeBackendProbe, KnowledgeBackendProbe>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = Environment.GetEnvironmentVariable("services__askhr-knowledge__https__0")
                   ?? Environment.GetEnvironmentVariable("services__askhr-knowledge__http__0")
                   ?? throw new InvalidOperationException("KernelMemory Endpoint configuration is required");
    var apiKey = configuration["KernelMemory:ApiKey"]
                ?? throw new InvalidOperationException("KernelMemory:ApiKey configuration is required");

    client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Authorization", apiKey);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Process X-Forwarded-For and X-Forwarded-Proto headers
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Clear default networks and proxies - be explicit about what we trust
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    // TODO: In production, configure specific known proxies/load balancers:
    // options.KnownProxies.Add(IPAddress.Parse("10.0.0.100"));
    // options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    
    // For Azure App Service, Azure Front Door, or other cloud services:
    // The proxy IP addresses may be dynamic, so you may need to trust all proxies
    // ONLY do this if your application is not directly accessible from the internet
    // and all traffic goes through your trusted infrastructure
    // Uncomment the following line only if needed:
    // options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("0.0.0.0"), 0));
    // options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("::"), 0));
});

builder.Services.AddAuthentication("Identity.Application")
    .AddCookie("Identity.Application", option => option.Cookie.Name = ".AspNetCore.Identity.Application");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error-development");
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseForwardedHeaders();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
