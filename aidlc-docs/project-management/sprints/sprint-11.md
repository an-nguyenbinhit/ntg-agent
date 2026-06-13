---
type: sprint
sprint: "11"
status: in-progress
owner: Scrum Master
tags: [scrum, sprint, testing, e2e, verification]
related: ["[[product-backlog]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]"]
created: 2026-06-13
updated: 2026-06-13
---

# Sprint 11 - Admin Portal Verification & E2E Testing

> **Sprint Goal**: Thiết lập hệ thống kiểm thử tự động (E2E) sử dụng Playwright để xác thực các tính năng quan trọng của Admin Portal như: hiển thị danh sách tài liệu, chỉnh sửa metadata, và thao tác Re-index.

## Scope Decisions

- Sử dụng `NUnit` làm base framework cho Playwright E2E tests.
- Tạo project mới `tests/AskHR.Admin.E2ETests`.
- Viết kịch bản tự động mô phỏng luồng: truy cập Admin Portal, xem Documents, chỉnh sửa Tag, và Re-index.

## Sprint Backlog

### To Do

- [ ] S-1101: Thiết lập Playwright E2E project (`AskHR.Admin.E2ETests`).
- [ ] S-1102: Viết test cases xác minh UI danh sách tài liệu (Documents Tab).
- [ ] S-1103: Viết test case xác minh UI chỉnh sửa document metadata (Tag, Role) và Re-index.

## Verification

- `rtk dotnet test tests\AskHR.Admin.E2ETests\AskHR.Admin.E2ETests.csproj --no-restore`
