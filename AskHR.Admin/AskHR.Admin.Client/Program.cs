using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AskHR.Admin.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

Uri baseUri = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddHttpClient<AgentClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<DocumentClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<FolderClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<TagClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<TokenUsageClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<MonitoringClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<FeedbackClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<ProvidersClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<KnowledgeClient>(client =>
{
    client.BaseAddress = baseUri;
});

builder.Services.AddHttpClient<SkillClient>(client =>
{
    client.BaseAddress = baseUri;
});

await builder.Build().RunAsync();
