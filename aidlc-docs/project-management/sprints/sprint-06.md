---
type: sprint
sprint: "06"
status: in-progress
owner: Scrum Master
tags: [scrum, sprint, migration]
related: ["[[product-backlog]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
start: 2026-06-11
end: 2026-06-24
created: 2026-06-11
updated: 2026-06-11
---

# Sprint 06 - Re-ingest Migration

> **Sprint Goal**: Provide a dry-run-first migration path for re-ingesting existing documents with permission metadata.

## Sprint Backlog

### In Progress

- [/] **Re-ingest Migration** Existing indexed documents can be re-indexed with default permission metadata after inventory approval. [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]

### Blocked

- [-] Production re-ingest execution: awaiting corpus/index inventory or backup export.

### Done

- [x] Re-ingest migration endpoint `POST /api/migration/reingest`, default `DryRun=true`.
- [x] Unit tests for re-ingest tool.

## Verification

- Automated: `dotnet test tests/AskHR.Orchestrator.Tests/AskHR.Orchestrator.Tests.csproj`.

## Open Questions



