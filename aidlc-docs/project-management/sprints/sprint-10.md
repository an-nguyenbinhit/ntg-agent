---
type: sprint
sprint: "10"
status: completed
owner: Scrum Master
tags: [scrum, sprint, admin, documents, rbac]
related: ["[[product-backlog]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
created: 2026-06-11
updated: 2026-06-13
---

# Sprint 10 - Document Metadata Management & UI

> **Sprint Goal**: Complete S-0501 document metadata management in Admin Portal, including role/tag/BU/sensitivity edits, manual re-index, and legacy corpus tag-ID restamping.

## Scope Decisions

- `EditDocumentModal` calls `UpdateDocumentMetadata` and submits canonical `TagIds`; `Tags` remains response/display and legacy request compatibility only.
- Manual re-index uses explicit `DocumentPermissionMetadata` snapshots with tag IDs, avoiding DB commits before external Knowledge re-index succeeds.
- Historical corpus re-index is handled by `POST /api/migration/reingest`; default dry-run remains true and default tag fallback is `Constants.PublicTagId`.

## Sprint Backlog

### Done

- [x] S-0501: Added `UpdateDocumentMetadataAsync`, `ReindexDocumentAsync`, and approval update support in `DocumentClient`.
- [x] S-0501: Added `EditDocumentModal.razor` for roles, business units, sensitivity, owner/version/date metadata, and tag ID selection.
- [x] S-0501: Integrated Edit, Re-index, Approve, Reject, and operation feedback into `DocumentsTab.razor`.
- [x] S-0501: Fixed tag identity correctness by indexing `AllowedTags` as tag IDs and validating tags before Knowledge re-index.
- [x] S-0501: Hardened bulk re-ingest path for legacy corpus tag-ID restamping.

## Verification

- `rtk dotnet test tests\AskHR.Orchestrator.Tests\AskHR.Orchestrator.Tests.csproj --no-restore`
- `rtk dotnet build AskHR.sln --no-restore`
