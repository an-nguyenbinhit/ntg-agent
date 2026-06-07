---
type: task
task: "TASK-0501"
story: "S-0501"
status: todo
owner:
tags: [scrum, task, admin-portal]
related: ["[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
created: 2026-06-07
updated: 2026-06-07
---

# TASK-0501: Admin Portal REST Endpoints Schema

> Story: **S-0501** · Epic: [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] · Sprint: [[sprint-04]]
> Định nghĩa REST endpoints cho control plane: quản lý tài liệu, mapping quyền và trạng thái ingest.

## Mục tiêu

Chốt API surface để HR Admin quản lý corpus và permission mapping mà không cần developer.

## Acceptance Criteria

- [ ] CRUD tài liệu + upload (`.docx`/`.md` theo template; file lệch template → `NeedsNormalization`).
- [ ] Endpoint gán/sửa permission metadata (`allowedRoles`/`tags`/`businessUnit`/`sensitivity`) cho tài liệu/chunk.
- [ ] Trigger re-index (liên kết S-0203) + xem trạng thái ingest/freshness/conflict queue.
- [ ] RBAC trên chính Admin API (vd `SystemAdmin` vs `HR Knowledge Owner`); audit mọi thao tác admin.
- [ ] Document metadata theo [[requirements]] §3.5 (Owner, Version, Effective/Expired, BU, Level, Sensitivity).

## Thiết kế kỹ thuật

- **Endpoints**: định nghĩa path/method/DTO trong task này (REST; cân nhắc OpenAPI spec).
- Persona/provider config (S-0502) và Skill registry/authoring (S-0105) là endpoint nhóm riêng, link sang stories tương ứng.
- Conflict detection + freshness contract gốc: [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]].

## Ràng buộc cần giữ

- Admin thao tác đều audit; secret theo Key Vault, cấm raw API key trong UI.
- Approval Workflow trước publish version quan trọng ([[units-governance-ops#UoB-05: Admin Portal / Monitoring]]).

## Notes / Decisions

-
