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
- [ ] **S-0501** Quản lý tài liệu/mapping qua control plane (Admin Portal) · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] · [[TASK-0501-admin-api]]
- [ ] **S-0502** Cấu hình persona/tone và provider config · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]
- [ ] **S-0203** Re-index khi tài liệu mới/đổi (hash/timestamp/trigger), không cron cứng · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]
- [ ] **S-0105** Cấu hình Skill (instructions/answer policy) qua progressive disclosure · [[units-retrieval-answer#UoB-01: Answer Policy Question]]
- [ ] **S-0702** Chọn provider/model per skill mà không sửa code · [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]
- [ ] **S-0503** Xem token usage & monitoring · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
_(trống)_

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
