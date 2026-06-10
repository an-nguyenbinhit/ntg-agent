---
type: sprint
sprint: "04"
status: planning
owner: Scrum Master
tags: [scrum, sprint]
related: ["[[product-backlog]]", "[[Home]]"]
start: 2026-07-21
end: 2026-08-01
created: 2026-06-07
updated: 2026-06-07
---

# Sprint 04 — Admin Portal & Cấu Hình

> **Sprint Goal**: Giao quyền tự vận hành cho HR/Admin qua control plane: quản lý tài liệu/mapping, cấu hình persona/tone và provider, trigger re-index, authoring Skill (progressive disclosure), chọn provider/model per skill và xem token usage/monitoring. Hết sprint: HR thêm/sửa tài liệu, Skill và provider config **không cần developer**.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-07-21 → 2026-08-01 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked
> Thứ tự To Do phản ánh phụ thuộc: Admin Portal (control plane) trước → re-index/persona → Skill config → provider per skill → monitoring.

### To Do
- [x] **S-0501** Quản lý tài liệu/mapping qua control plane (Admin Portal) · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] · [[TASK-0501-admin-api]]
- [x] **S-0502** Cấu hình persona/tone và provider config · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]
- [x] **S-0203** Re-index khi tài liệu mới/đổi (hash/timestamp/trigger), không cron cứng · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]
- [ ] **S-0105** Cấu hình Skill (instructions/answer policy) qua progressive disclosure · [[units-retrieval-answer#UoB-01: Answer Policy Question]]
- [ ] **S-0702** Chọn provider/model per skill mà không sửa code · [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]
- [ ] **S-0503** Xem token usage & monitoring · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
- [x] **S-0501** Quản lý tài liệu/mapping qua control plane (Admin Portal) · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] · [[TASK-0501-admin-api]]
- [x] **S-0502** Cấu hình persona/tone và provider config · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]
- [x] **S-0203** Re-index khi tài liệu mới/đổi (hash/timestamp/trigger), không cron cứng · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]

## Status Reconciliation

- 2026-06-08: S-0501 is implemented. Added `IngestStatus` enum and metadata properties (`Roles`, `BusinessUnits`, `SensitivityLevel`) to `Document` entity and EF Core DB schema. Added `UpdateDocumentMetadata` and `ReindexDocument` endpoints to `DocumentsController`. Re-indexing logic properly replaces Kernel Memory document index and updates metadata in the Orchestrator DB. All 278 Unit tests pass.
- 2026-06-08: S-0502 is implemented. Added Persona config (`Tone`, `CreativityCap`, `AllowedEmojis`, `ChannelProfile`) to `Agent` entity. Deprecated old provider fields on `Agent`. Created `ProviderMetadata` and `ModelRoute` entities to adhere to UoB-07. Added EF Core configurations with JSON value converters. Created `ProvidersAdminController` with full CRUD endpoints for provider and route configuration. Project compiles successfully with 0 errors.
- 2026-06-10: S-0203 is implemented. Extracted hash/re-index logic from `DocumentsController` into shared `DocumentIngestionService`. Added `DocumentWatcherService` (BackgroundService + `FileSystemWatcher`, per-file debounce, no cron) that auto re-indexes new/changed files under `IngestionSource:WatchPath` when `IngestionSource:Mode = "WatchFolder"` (default remains `AdminUpload`, watcher off). Unchanged files are skipped by SHA256 hash comparison; changed files preserve stored permission metadata. Open question for HR/PO: if the source folder moves to Azure Blob/SharePoint, switch trigger to Azure Event Grid instead of `FileSystemWatcher`. All 310 unit tests pass.

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-07-21 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [ ] Unit test cho admin CRUD, re-index trigger, skill registry, provider route resolution.
- [ ] Integration test: HR đổi Skill/provider config → có hiệu lực mà không deploy lại; token usage hiển thị đúng.
- [ ] Tài liệu kỹ thuật ở `tasks/` được link và cập nhật.
- [ ] Skill/persona config KHÔNG override security trimming, citation hay deny-by-default ([[units-retrieval-answer#UoB-01: Answer Policy Question]] §11.5); secret theo Key Vault, cấm raw API key trong UI.

## Sprint Review / Retro

_(điền cuối sprint)_
