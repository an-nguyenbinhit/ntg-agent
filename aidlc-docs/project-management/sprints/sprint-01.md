---
type: sprint
sprint: "01"
status: in-progress
owner: Scrum Master
tags: [scrum, sprint]
related: ["[[product-backlog]]", "[[product-backlog]]"]
start: 2026-06-09
end: 2026-06-20
created: 2026-06-07
updated: 2026-06-08
---

# Sprint 01 - Nền tảng RBAC & Index

> **Sprint Goal**: Dựng được nền tảng bắt buộc cho mọi UoB: ingest/index tài liệu HR có permission metadata và resolve Authorization Context với security-trimmed retrieval. Hết sprint phải retrieve được chunk đúng quyền.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-06-09 -> 2026-06-20 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked

### To Do
_(trống)_

### In Progress
_(trống)_

### Blocked
- [-] **Data Migration** Re-ingest tài liệu cũ trong default index và gắn permission metadata (`allowedRoles`, `businessUnits`, `sensitivity`) trước khi Sprint 02 dùng retrieval end-to-end. Blocker: cần danh sách corpus/index production hoặc backup export để chạy migration thật.

### Done
- [x] **S-0201** Ingest `.docx/.md/PDF` -> chunk + embed + index có citation metadata · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] · [[TASK-0201-ingest-document-citation-metadata]] - _Admin upload/API chỉ nhận `.docx/.md/.pdf`; Kernel Memory pipeline extract/partition/embed/save theo per-agent index; citation tags `documentName/sourceType/sourcePath/sourceUrl` được stamp cùng permission metadata; unit test bổ sung._
- [x] **S-0402** Security-trimmed retrieval enforce server-side · [[units-security-identity#UoB-04: RBAC / Identity & Access]] - _Per-agent index + deny-by-default trong `KernelMemoryKnowledge`; xóa lỗ allow-by-default (search toàn cục khi filter rỗng); 26/26 unit test pass._
- [x] **S-0401** Resolve identity -> Authorization Context · [[units-security-identity#UoB-04: RBAC / Identity & Access]] - _Thêm `IIdentityResolver`, `IRbacService`, `AuthorizationContext`; RBAC lấy role/tag hiện có và hỗ trợ mock BU/sensitivity bằng config._
- [x] **S-0202** Gắn permission metadata (role/tag/BU/sensitivity) cho mỗi chunk · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] - _Upload API/Admin form nhận roles/BU/sensitivity; Kernel Memory tags có `allowedRoles`, `businessUnits`, `sensitivity`._
- [x] **S-0103** Deny-by-default trên retrieval theo Authorization Context · [[units-retrieval-answer#UoB-01: Answer Policy Question]] - _Search dùng `AuthorizationContext`; filter OR giữa values và AND giữa axes; anonymous chỉ public scope; empty context dùng deny-all filter._

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-06-07 | Construction sớm: per-agent index + deny-by-default trong `KernelMemoryKnowledge` (S-0402 Done), security-trimmed retrieval enforce server-side. Thêm read-only Knowledge Backend panel ở Agent Dashboard + Azure AI Search scaffold. 2 commit code, 26/26 unit test pass. | Tài liệu cũ ở default index cần re-ingest |
| 2026-06-07 | Tiếp tục Sprint 01: thêm AuthorizationContext/DocumentPermissionMetadata, resolver/RBAC service, Admin upload metadata, Kernel Memory permission tags + AuthorizationContext filters; fix `__any__` sentinel, bỏ Cartesian filters, enforce sensitivity cho anonymous, và ghi chú operational tradeoff cho Sensitivity trống. Orchestrator tests 240/240 pass. | Cần re-ingest tài liệu để có metadata role/BU/sensitivity mới |
| 2026-06-08 | Hoàn tất S-0201 implementation guard: upload/API chỉ nhận `.docx/.md/.pdf`, Kernel Memory import stamp citation metadata tags, thêm `TASK-0201`. | Re-ingest corpus cũ chưa chạy vì chưa có danh sách corpus/index đích |
| 2026-06-09 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [x] Unit test cho logic core (chunking/ingestion boundary, permission filter).
- [x] Tài liệu kỹ thuật ở `tasks/` được link.
- [x] Không vi phạm ràng buộc RBAC/citation của [[units-retrieval-answer#UoB-01: Answer Policy Question]] §8.7.

## Sprint Review / Retro

_(điền cuối sprint)_
