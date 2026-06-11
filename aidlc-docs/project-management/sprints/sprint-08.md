---
type: sprint
sprint: "08"
status: in-progress
owner: Scrum Master
tags: [scrum, sprint, admin-portal, monitoring, feedback, providers]
related: ["[[product-backlog]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]", "[[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]"]
start: 2026-06-11
end: 2026-06-24
created: 2026-06-11
updated: 2026-06-11
---

# Sprint 08 - Admin Portal Enhancements & Ops Management

> **Sprint Goal**: Turn Sprint 07 backend audit, feedback, token, and provider-route plumbing into usable Admin Portal operations screens.

## Scope Decisions

- UI library: use existing Blazor Web App + Bootstrap components. No MudBlazor, FluentUI, or Radzen dependency added.
- Warm handoff UI: out of scope for Sprint 08 until a handoff inbox/notification API exists.
- Provider routing verification: implemented as admin-side configuration validation over `ProviderMetadata` and `ModelRoute`; no live model probe endpoint yet.

## Sprint Backlog

### Done

- [x] **S-0503** Ops Monitoring dashboard backed by `AuditEvent` token and latency fields.
- [x] **S-0602/S-0603** Feedback Management page with severity/status filters and update workflow.
- [x] **S-0702** Provider Routing page with provider health/approval metadata and route verification.
- [x] Admin navigation wired for Ops Monitoring, Feedback, and Provider Routing.
- [x] `FeedbackAdminController` API with paging, filters, counts, and severity/status updates.

### In Progress

- [/] Manual Admin Portal verification against `AskHR.AppHost` with seeded/live data.

### Out of Scope

- Slack channel implementation (`S-0301`).
- Warm handoff HR inbox/notifications (`S-0104` UI).
- Live provider model test/probe endpoint.

## Verification

- Automated: `dotnet build AskHR.sln`.
- Automated: `dotnet test tests/AskHR.Orchestrator.Tests/AskHR.Orchestrator.Tests.csproj --no-restore`.
- Manual: run `AskHR.AppHost`, sign in as `admin@askhr.com`, verify `/monitoring`, `/feedback`, `/providers`, and `/token-usage`.

## Notes

- `TokenUsageManagement` remains the detailed user/history drill-down page.
- `/monitoring` reads `AuditEvents` because latency exists there, not in `TokenUsageStatsDto`.
