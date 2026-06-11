---
type: sprint
sprint: "06"
status: in-progress
owner: Scrum Master
tags: [scrum, sprint, teams, migration]
related: ["[[product-backlog]]", "[[units-channels#UoB-09: MS Teams Channel]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
start: 2026-06-11
end: 2026-06-24
created: 2026-06-11
updated: 2026-06-11
---

# Sprint 06 - MS Teams Channel & Re-ingest Migration

> **Sprint Goal**: Add MS Teams channel adapter using the shared AskHR answer pipeline, and provide a dry-run-first migration path for re-ingesting existing documents with permission metadata.

## Sprint Backlog

### In Progress
- [/] **S-0901** User chats with AskHR through MS Teams with SSO identity resolution. [[units-channels#UoB-09: MS Teams Channel]]
- [/] **Re-ingest Migration** Existing indexed documents can be re-indexed with default permission metadata after inventory approval. [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]

### Blocked
- [-] Azure Bot Service manual verification: awaiting approved Bot Service resource, Teams app registration, and callback permission.
- [-] Production re-ingest execution: awaiting corpus/index inventory or backup export.

### Done
- [x] Teams webhook endpoint `POST /api/messages`.
- [x] Teams identity resolver for `aadObjectId` mapping and UPN/email fallback.
- [x] Teams Adaptive Card answer formatter with feedback actions.
- [x] Re-ingest migration endpoint `POST /api/migration/reingest`, default `DryRun=true`.
- [x] Unit tests for Teams resolver, Adaptive Card formatter, Teams gateway, and re-ingest tool.

## Verification

- Automated: `dotnet test tests/AskHR.Orchestrator.Tests/AskHR.Orchestrator.Tests.csproj`.
- Manual: configure Azure Bot Service messaging endpoint to local ngrok URL `/api/messages`, test DM and `@mention`, validate `AuthorizationContext` and security trimming.

## Open Questions

- Listen scope: DM + `@mention` only, or full HR channel listening.
- Teams reply format: keep Adaptive Cards as default, with Markdown fallback only if card rendering causes issues.
- Identity mapping: confirm whether `aadObjectId` maps directly to internal user id, or via configured mapping/HRIS sync.

