# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NTG Agent is a .NET 10 AI chatbot application built with:
- **.NET Aspire** for distributed application orchestration
- **Blazor** (WebAssembly and Server) for web interfaces
- **Microsoft Agent Framework** for AI agent capabilities
- **Kernel Memory** for RAG (Retrieval Augmented Generation)
- **SQL Server** for persistence
- **MCP (Model Context Protocol)** for extensible AI tool integration
- Multiple LLM providers: GitHub Models, OpenAI, Azure OpenAI, Google Gemini

## Development Commands

### Build and Test
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run tests with coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/NTG.Agent.Orchestrator.Tests
```

### Database Migrations
```bash
# Add migration for Orchestrator (required for new entity changes)
dotnet ef migrations add MigrationName --project NTG.Agent.Orchestrator

# Update Orchestrator database
dotnet ef database update --project NTG.Agent.Orchestrator

# Add migration for Admin (if Admin entities changed)
dotnet ef migrations add MigrationName --project NTG.Agent.Admin/NTG.Agent.Admin

# Update Admin database
dotnet ef database update --project NTG.Agent.Admin/NTG.Agent.Admin
```

### Running the Application
```bash
# Run the Aspire AppHost (starts all services)
dotnet run --project NTG.Agent.AppHost
```

This launches the Aspire Dashboard with:
- **NTG.Agent.WebClient**: End-user chat interface
- **NTG.Agent.Admin**: Admin portal (default: admin@ntgagent.com / Ntg@123)
- **NTG.Agent.Orchestrator**: Backend API
- **NTG.Agent.Knowledge**: Document ingestion and RAG service
- **NTG.Agent.MCP.Server**: MCP server for AI tools

### Secrets Management
```bash
# Set GitHub Models token for Knowledge service
dotnet user-secrets set "KernelMemory:Services:OpenAI:APIKey" "<token>" --project NTG.Agent.Knowledge

# Set Google Search credentials for MCP.Server
dotnet user-secrets set "Google:ApiKey" "<key>" --project NTG.Agent.MCP.Server
dotnet user-secrets set "Google:SearchEngineId" "<id>" --project NTG.Agent.MCP.Server
```

## Architecture

### Service Dependencies

```
AppHost (Aspire Orchestrator)
├── MCP.Server (Tools)
├── Knowledge (RAG/Document Processing)
├── Orchestrator (Backend API)
│   ├── References: MCP.Server, Knowledge
│   └── Database: AgentDbContext (SQL Server)
├── WebClient (End-user UI)
│   └── References: Orchestrator
└── Admin (Admin UI with YARP BFF)
    └── References: Orchestrator
```

### Key Projects

**NTG.Agent.Orchestrator** - Backend API
- **Controllers/**: REST API endpoints
- **Services/Agents/**: Agent service, factory, and orchestration
- **Services/Knowledge/**: RAG integration with Kernel Memory
- **Services/Memory/**: Long-term user memory management
- **Services/TokenTracking/**: Token usage tracking
- **Data/AgentDbContext.cs**: Main EF Core context
- **Models/**: Domain entities (Agents, Chat, Documents, Tags, Identity, TokenUsage, UserPreferences)
- **Plugins/**: Semantic Kernel plugins for agent capabilities

**NTG.Agent.Knowledge** - Document Ingestion Service
- Uses Kernel Memory for document processing, embedding generation, and vector search
- Configured via extensive appsettings.json (embeddings, storage, ingestion pipeline)
- Provides API for document upload and semantic search

**NTG.Agent.MCP.Server** - MCP Tool Server
- Exposes tools to AI agents via Model Context Protocol
- **McpTools/**: Individual MCP tool implementations
- **Services/**: Supporting services for tools

**NTG.Agent.Admin** - Admin Portal
- Blazor server/WASM hybrid with YARP reverse proxy for BFF pattern
- Manages agents, documents, folders, users, and configuration

**NTG.Agent.WebClient** - End-User Interface
- Blazor WebAssembly for chat interface
- Real-time streaming chat responses

**NTG.Agent.Common** - Shared Library
- **Dtos/**: Shared data transfer objects across all projects
- Organized by domain: Agents, Chats, Documents, TokenUsage, etc.

**AITools/** - Agent Tool Plugins
- **NTG.Agent.AITools.SimpleTools**: Basic tool implementations
- **NTG.Agent.AITools.SearchOnlineTool**: Web search and scraping tools

### Data Models

Core entities in `AgentDbContext`:
- **Agent**: AI assistant configuration with instructions and tools
- **Conversation**: Chat session container
- **ChatMessage**: Individual messages with roles (User/Assistant/System)
- **Document**: Uploaded files with folder association
- **Folder**: Hierarchical organization for documents
- **Tag** / **TagRole**: Content categorization with role-based access
- **User** / **Role** / **UserRole**: Identity (shared with ASP.NET Identity tables)
- **UserPreference**: Per-user or per-session preferences (including memory settings)
- **TokenUsage**: LLM token consumption tracking

### AI Agent Flow

1. User sends message via WebClient/Admin → Orchestrator API
2. `AgentService.ChatStreamingAsync()` orchestrates:
   - Validates conversation and prepares history
   - Retrieves user tags for access control
   - Processes OCR for image uploads
   - Calls RAG: `IKnowledgeService` queries Kernel Memory for relevant documents
   - Retrieves user long-term memory: `IUserMemoryService` fetches stored context
   - Builds chat context with system prompt, history, RAG context, and memory
   - Creates agent via `IAgentFactory` with configured LLM provider and tools
   - Streams response back to client
   - Extracts and stores new memories if LongTermMemory.Enabled
3. Agent can invoke tools from MCP.Server or AITools plugins
4. Token usage tracked via `ITokenTrackingService`

### Authentication & Authorization

- **Shared cookie authentication** across services (`.AspNetCore.Identity.Application`)
- **YARP reverse proxy** in Admin project implements BFF (Backend for Frontend) pattern
- Data protection keys shared via filesystem (`../key/`)
- Supports both authenticated users and anonymous sessions (tracked via SessionId)
- Admin role required for admin portal access

### Long-Term Memory (LTM)

Configurable user memory system in `appsettings.json`:
```json
{
  "LongTermMemory": {
    "Enabled": true,
    "MinimumConfidenceThreshold": 0.3,
    "MaxMemoriesToRetrieve": 20
  }
}
```
- Extracts and stores user-specific facts during conversations
- Retrieves relevant memories for context in subsequent chats
- Can be disabled to reduce token consumption

### Observability

- **OpenTelemetry** integration for traces, metrics, and logs
- Custom metrics: `agent_interactions_total`, `agent_response_time_seconds`
- Aspire Dashboard for development monitoring
- SonarCloud for code quality and coverage

## Code Conventions

Follow conventions in [.github/copilot-instructions.md](.github/copilot-instructions.md):
- Use **C# 12** features (file-scoped namespaces, record types, pattern matching)
- Follow **async/await** throughout
- Use **dependency injection** consistently
- **PascalCase** for classes, methods, properties
- **camelCase** for local variables, private fields
- Prefix interfaces with 'I'
- Entity Framework: Code-first migrations, fluent API configuration
- Blazor: Use `@rendermode InteractiveServer` or `InteractiveWebAssembly`
- Implement proper error handling with structured logging

### Entity Conventions
```csharp
public class ExampleEntity
{
    public ExampleEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Important Notes

- **Database connections**: Default uses Windows Authentication to local SQL Server. Update connection strings in `appsettings.Development.json` for Orchestrator, Admin, and Knowledge if needed.
- **First-time setup**: Run `dotnet ef database update` for both Orchestrator and Admin projects before starting AppHost.
- **LLM configuration**: Configure agent provider in Admin portal after first run (Agent Management → Agent Default).
- **Secrets**: Never commit secrets to source control. Use user-secrets for local development.
- **Code quality**: Solution uses `Directory.Build.props` to enforce code analysis and style rules on build.
- **Testing**: xUnit for test framework. Tests in `tests/` folder.
- **SonarCloud exclusions**: AppHost, Program.cs, Migrations, razor CSS, and wwwroot are excluded from coverage.

## Common Tasks

### Adding a New Entity
1. Create model class in `NTG.Agent.Orchestrator/Models/<Domain>/`
2. Add `DbSet<T>` to `AgentDbContext`
3. Configure relationships in `OnModelCreating()` if needed
4. Run `dotnet ef migrations add AddEntity --project NTG.Agent.Orchestrator`
5. Apply migration: `dotnet ef database update --project NTG.Agent.Orchestrator`

### Adding a New API Endpoint
1. Create controller in `NTG.Agent.Orchestrator/Controllers/`
2. Use `[ApiController]` and `[Route("api/[controller]")]`
3. Inject required services via constructor
4. Use async methods and proper error handling
5. Create DTOs in `NTG.Agent.Common/Dtos/<Feature>/`

### Adding a New MCP Tool
1. Create tool class in `NTG.Agent.MCP.Server/McpTools/`
2. Use `[McpTool]` attribute and implement tool method
3. Tool automatically registered via `WithToolsFromAssembly()`
4. Reference tool in Admin portal when configuring agents

### Adding a New Agent Plugin
1. Create plugin class in `NTG.Agent.Orchestrator/Plugins/`
2. Use Semantic Kernel plugin patterns
3. Register in `AgentFactory.CreateAgent()`
4. Document tool in agent instructions for LLM awareness
