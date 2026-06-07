using AskHR.AITools.SearchOnlineTool.Extensions;
using AskHR.MCP.Server.Services;
using AskHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<MonkeyService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .AddAiTool();

var app = builder.Build();

app.MapMcp();

app.MapDefaultEndpoints();

app.Run();
