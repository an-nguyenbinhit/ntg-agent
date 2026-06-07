---
type: moc
status: source-of-truth
owner: HR / Eng
tags: [moc]
related: ["[[requirements]]", "[[units-retrieval-answer]]", "[[units-security-identity]]", "[[units-channels]]", "[[units-governance-ops]]", "[[ADR-001-agent-runtime]]"]
created: 2026-06-06
updated: 2026-06-07
---

# AskHR Documentation Home

> Đây là MOC (Map of Content) cho toàn bộ tri thức dự án **AskHR**. Cập nhật file này mỗi khi thêm requirement, UoB hoặc tài liệu kiến trúc cấp cao mới.

## Source of Truth

| Tài liệu | Vai trò |
|---|---|
| [[requirements]] | Requirement nguồn hợp nhất cho sản phẩm AskHR; các UoB được decompose từ tài liệu này. |
| [[ADR-001-agent-runtime]] | ADR accepted cho agent runtime/orchestration substrate dựa trên Microsoft Agent Framework (.NET). |

## Units of Business

8 UoB được gom thành **4 file theo chủ đề**; mỗi UoB là một section H2 trong file tương ứng.

| File | UoB | Phạm vi chính | Tags |
|---|---|---|---|
| [[units-retrieval-answer]] | UoB-01 [[units-retrieval-answer#UoB-01: Answer Policy Question\|Answer Policy Question]] · UoB-02 [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents\|Ingest & Index]] | Trả lời policy chỉ từ corpus HR (grounding/citation/fallback); ingest/chunk/embed/index tài liệu HR kèm permission metadata. | #rag #rbac #governance |
| [[units-security-identity]] | UoB-04 [[units-security-identity#UoB-04: RBAC / Identity & Access\|RBAC / Identity & Access]] | RBAC, identity resolution và security-trimmed retrieval theo role/tag/BU/level. | #rbac #security |
| [[units-channels]] | UoB-03 [[units-channels#UoB-03: Slack Mention & Thread Context\|Slack Mention & Thread Context]] · UoB-08 [[units-channels#UoB-08: Web Chat Channel\|Web Chat Channel]] | Slack gateway (`@mention`/DM/thread, loading/error UX) và Web Chat (streaming, history, feedback, web identity). | #slack #web-chat #escalation #rbac |
| [[units-governance-ops]] | UoB-05 [[units-governance-ops#UoB-05: Admin Portal / Monitoring\|Admin Portal / Monitoring]] · UoB-06 [[units-governance-ops#UoB-06: Feedback, Audit & Analytics\|Feedback, Audit & Analytics]] · UoB-07 [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration\|Multi-Provider Config]] | Admin Portal/control plane, token usage & monitoring; feedback, audit trail, P1/P2/P3 signal, analytics; abstraction & governance đa provider/model. | #admin-portal #governance #escalation #multi-provider |

## Tag Taxonomy

| Tag | Ý nghĩa |
|---|---|
| #rag | Retrieval-Augmented Generation, grounding, citation. |
| #rbac | Phân quyền, role/tag mapping, Authorization Context. |
| #security | Security trimming, dữ liệu nhạy cảm, privacy controls. |
| #slack | Slack integration, `@mention`, DM, thread context. |
| #governance | Data owner, versioning, approval, document freshness. |
| #escalation | HR fallback, P1/P2/P3 severity, notification routing. |
| #admin-portal | Quản trị và giám sát qua control plane. |
| #multi-provider | Cấu hình nhiều LLM provider/model. |
| #web-chat | Web chat, streaming, conversation history. |

## Decomposition Coverage

Xem chi tiết tại [[requirements]] §10. Các gap đã được decompose:

| Gap trong requirement | Tài liệu cover |
|---|---|
| Admin Portal / Quản trị & Giám sát | [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] |
| Feedback & Audit / Analytics | [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] |
| Multi-provider config | [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] |
| Web Chat channel | [[units-channels#UoB-08: Web Chat Channel]] |
| Agent Skills / Config Plane | [[units-retrieval-answer#UoB-01: Answer Policy Question]], [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] |
| Agent runtime / orchestration substrate | [[ADR-001-agent-runtime]], [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] |

## Execution Layer (Scrum)

Lớp thực thi Scrum nằm **trong** `aidlc-docs/` tại `project-management/`. UoB ở đây đóng vai trò **Epic**.

| Tài liệu | Vai trò |
|---|---|
| [[product-backlog]] | Hub PM + map 8 UoB (Epic) ra Story, ưu tiên và trạng thái. |
| [[sprint-01]] | Nền tảng RBAC & Index (P0). |
| [[sprint-02]] | Core trả lời & Slack Gateway (P1). |
| [[sprint-03]] | Web Chat & Escalation/UX (P2/P3). |
| [[sprint-04]] | Admin Portal & Cấu hình (P2/P3). |

## Vault Conventions

- Mọi note trong `aidlc-docs/` phải có frontmatter.
- Dùng wikilink dạng double-bracket để liên kết tài liệu liên quan thay vì lặp nội dung; file đã hợp nhất thì link tới section bằng `[[file#heading]]`.
- Khi thêm note cấp cao mới, cập nhật `related` trong frontmatter và bảng tương ứng trong Home.
- Giữ thuật ngữ nhất quán: **HR Admin**, **HR Knowledge Owner**, **User**, **Slack Bot**, **RAG Pipeline**, **Admin Portal**, **AuthorizationContext**.
- Trước Scrum planning, dùng [[requirements]] §10.2 (Scrum Readiness) làm checklist Definition of Ready ở mức requirement.

## Changelog

- 2026-06-07: Gom 8 file UoB thành 4 file theo chủ đề (`units-retrieval-answer`, `units-security-identity`, `units-channels`, `units-governance-ops`); đổi tên `AI-requirement` → `requirements`, gộp `README` vào `product-backlog`. Cập nhật bảng UoB, Decomposition Coverage và Execution Layer sang link `[[file#heading]]`.
- Chuẩn hóa heading, loại bỏ emoji khỏi tiêu đề để tài liệu đọc ổn định hơn trong nhiều renderer.
- Chuyển danh sách UoB và tag taxonomy sang bảng để dễ scan.
- Thêm quy ước thuật ngữ chung cho toàn bộ bộ tài liệu.
