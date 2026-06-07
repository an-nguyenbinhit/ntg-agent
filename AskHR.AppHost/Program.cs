var builder = DistributedApplication.CreateBuilder(args);

var mcpServer = builder.AddProject<Projects.AskHR_MCP_Server>("askhr-mcp-server");
var knowledge = builder.AddProject<Projects.AskHR_Knowledge>("askhr-knowledge");

var orchestrator = builder.AddProject<Projects.AskHR_Orchestrator>("askhr-orchestrator")
    .WithExternalHttpEndpoints()
    .WithReference(mcpServer)
    .WithReference(knowledge);

builder.AddProject<Projects.AskHR_WebClient>("askhr-webclient")
    .WithExternalHttpEndpoints()
    .WithReference(orchestrator)
    .WaitFor(orchestrator);

builder.AddProject<Projects.AskHR_Admin>("askhr-admin")
    .WithExternalHttpEndpoints()
    .WithReference(orchestrator)
    .WaitFor(orchestrator);

builder.Build().Run();
