# AI-DLC State Tracking

## Project Information

- **Project**: AskHR / AskHR
- **Project Type**: Brownfield application with AI-DLC requirements baseline
- **Start Date**: 2026-06-06T00:00:00Z
- **Current Phase**: CONSTRUCTION
- **Current Stage**: Sprint 10 - Document Metadata Management & UI (Complete)

## Workspace State

- **Existing Application Code**: Yes
- **Primary Artifacts**: `aidlc-docs/requirements.md`, `aidlc-docs/units-retrieval-answer.md`, `aidlc-docs/units-security-identity.md`, `aidlc-docs/units-channels.md`, `aidlc-docs/units-governance-ops.md`, `aidlc-docs/ADR-001-agent-runtime.md`, `aidlc-docs/project-management/product-backlog.md`
- **Workspace Root**: `D:\Projects\ntg-agent`
- **Documentation Directory**: `aidlc-docs/`

## Extension Configuration

| Extension | Enabled | Decided At |
|---|---|---|
| None detected | N/A | Requirements Scrum-Readiness Review |

## Stage Progress

| Stage | Status | Notes |
|---|---|---|
| Workspace Detection | Complete | AskHR detected as a .NET brownfield application. |
| Requirements Analysis | Complete | Requirement source and UoB decomposition exist in `aidlc-docs/`. |
| User Stories | Complete | Product backlog maps UoB epics to S-0101..S-0803. |
| Workflow Planning | Complete | Sprint execution artifacts exist under `project-management/sprints/`. |
| Application Design | Drafted | Domain/service boundaries are represented by UoB docs, ADR-001, `IKnowledgeService`, `AuthorizationContext`, RBAC, and Kernel Memory adapter contracts. |
| Units Generation | Complete | UoB-01 through UoB-08 are mapped into Scrum execution artifacts. |
| Code Generation | In Progress | Sprint 10 completed S-0501 document metadata UI/API, tag-ID canonicalization, explicit re-index permission snapshots, and legacy corpus re-ingest support. |
| Build and Test | In Progress | Targeted AskHR.Orchestrator tests pass for DocumentsController, DocumentIngestionService, and ReingestTool. |

## Current Recommendation

### Current Recommendation Override - 2026-06-13

1. Re-ingest migration endpoint `POST /api/migration/reingest` now restamps legacy corpus with canonical tag IDs. Default `DryRun=true`; set `DryRun=false` only after corpus/index backup or inventory is available.
2. `DocumentMetadataUpdateRequest.TagIds` is the canonical write contract. `Tags` remains for display and legacy compatibility.
3. Re-index operations pass explicit `DocumentPermissionMetadata` snapshots to avoid committing pending DB metadata before external Knowledge re-index succeeds.
4. Next engineering focus should be live Admin Portal verification against `AskHR.AppHost` with seeded documents and a real Knowledge index.
