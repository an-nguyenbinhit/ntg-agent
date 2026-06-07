# AI-DLC State Tracking

## Project Information

- **Project**: NTG Agent / AskHR
- **Project Type**: Brownfield application with AI-DLC requirements baseline
- **Start Date**: 2026-06-06T00:00:00Z
- **Current Phase**: INCEPTION
- **Current Stage**: Requirements Scrum-Readiness Review

## Workspace State

- **Existing Application Code**: Yes
- **Primary Artifacts**: `aidlc-docs/requirements.md`, `aidlc-docs/units-retrieval-answer.md`, `aidlc-docs/units-security-identity.md`, `aidlc-docs/units-channels.md`, `aidlc-docs/units-governance-ops.md` (8 UoB gom theo chủ đề), `aidlc-docs/ADR-001-agent-runtime.md`
- **Workspace Root**: `D:\Projects\ntg-agent`
- **Documentation Directory**: `aidlc-docs/`

## Extension Configuration

| Extension | Enabled | Decided At |
|---|---|---|
| None detected | N/A | Requirements Scrum-Readiness Review |

## Stage Progress

| Stage | Status | Notes |
|---|---|---|
| Workspace Detection | Complete | Existing NTG Agent .NET application detected; AI-DLC docs define the AskHR requirement baseline inside this repo. |
| Requirements Analysis | Complete with Sprint-0 inputs | Requirement source and UoB decomposition exist; kickoff decisions are tracked in `requirements.md` §3 and §10. |
| User Stories | Pending | Scrum decomposition can start from UoB documents after Sprint-0 readiness checks. |
| Workflow Planning | Pending | Next step is backlog/workflow planning by UoB dependency order. |
| Application Design | Pending | Needed for domain model, service contracts, infrastructure boundaries and NFR decisions. |
| Units Generation | Drafted | UoB-01 through UoB-08 are available as business units; engineering units still need Scrum task breakdown. |

## Current Recommendation

Start Scrum decomposition from these epics:

1. UoB-04 RBAC / Identity & Access
2. UoB-02 Ingest & Index HR Documents
3. UoB-01 Answer Policy Question
4. UoB-03 Slack Channel
5. UoB-06 Feedback / Audit / Analytics
6. UoB-05 Admin Portal / Monitoring
7. UoB-07 Multi-Provider / Model Configuration
8. UoB-08 Web Chat Channel

Sprint-0 must close the source connector, document template, HR fallback contacts, admin roles, retention approval, RAG latency benchmark and MAF baseline version before implementation stories are committed.
