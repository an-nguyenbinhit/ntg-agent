---
type: sprint
sprint: "10"
status: planned
owner: Scrum Master
tags: [scrum, sprint, admin, documents, rbac]
related: ["[[product-backlog]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
created: 2026-06-11
updated: 2026-06-11
---

# Sprint 10 - Document Metadata Management & UI

> **Sprint Goal**: Hoàn thiện UI quản lý Metadata tài liệu ở Admin Portal (S-0501), cho phép HR Admin chỉnh sửa Roles/Tags/BU/Sensitivity và trigger Manual Re-index. Qua đó gỡ Blocker "Historical corpus re-ingest" của Sprint 09.

## Scope Decisions

- UI sửa Metadata (EditDocumentModal) sẽ gọi `UpdateDocumentMetadata` (đã có từ Sprint 09).
- Tính năng sửa `Tags` tạm thời chưa tích hợp vào Modal vì endpoint `UpdateDocumentMetadata` ở `DocumentsController.cs` chưa nhận field `Tags`. Sẽ bổ sung sau nếu cần, hoặc người dùng có thể xóa tài liệu và upload lại với tag mới.
- Tính năng "Manual Re-index" sẽ được trigger ngầm định khi HR lưu metadata mới, đồng thời cung cấp một nút độc lập "Re-index" trên UI đề phòng file hỏng.

## Sprint Backlog

### To Do

- [ ] S-0501: Thêm `UpdateDocumentMetadataAsync` và `ReindexDocumentAsync` vào `DocumentClient`.
- [ ] S-0501: Tạo `EditDocumentModal.razor` cho phép edit Roles, BusinessUnits, SensitivityLevel.
- [ ] S-0501: Tích hợp nút Edit và Reindex vào lưới dữ liệu của `DocumentsTab.razor`.

