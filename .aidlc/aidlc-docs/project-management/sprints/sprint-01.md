---
type: sprint
sprint: "01"
status: planning
owner: Scrum Master
tags: [scrum, sprint]
related: ["[[product-backlog]]", "[[product-backlog]]"]
start: 2026-06-09
end: 2026-06-20
created: 2026-06-07
updated: 2026-06-07
---

# Sprint 01 — Nền tảng RBAC & Index

> **Sprint Goal**: Dựng được nền tảng bắt buộc cho mọi UoB — ingest/index tài liệu HR có permission metadata và resolve Authorization Context với security-trimmed retrieval. Hết sprint phải retrieve được chunk đúng quyền.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-06-09 → 2026-06-20 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked

### To Do
- [ ] **S-0201** Ingest `.docx/.md/PDF` → chunk + embed + index có citation metadata · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]
- [ ] **S-0202** Gắn permission metadata (role/tag/BU/sensitivity) cho mỗi chunk · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]
- [ ] **S-0401** Resolve identity → Authorization Context · [[units-security-identity#UoB-04: RBAC / Identity & Access]]
- [ ] **S-0402** Security-trimmed retrieval enforce server-side · [[units-security-identity#UoB-04: RBAC / Identity & Access]]
- [ ] **S-0103** Deny-by-default trên retrieval theo Authorization Context · [[units-retrieval-answer#UoB-01: Answer Policy Question]]

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
_(trống)_

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-06-09 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [ ] Unit test cho logic core (chunking, permission filter).
- [ ] Tài liệu kỹ thuật (nếu cần) ở `tasks/` được link.
- [ ] Không vi phạm ràng buộc RBAC/citation của [[units-retrieval-answer#UoB-01: Answer Policy Question]] §8.7.

## Sprint Review / Retro

_(điền cuối sprint)_
