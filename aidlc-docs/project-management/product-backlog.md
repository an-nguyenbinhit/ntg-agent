---
type: product-backlog
status: draft
owner: Product Owner / Eng / Scrum Master
tags: [scrum, backlog, process]
related: ["[[Home]]", "[[requirements]]"]
created: 2026-06-07
updated: 2026-06-07
---

# Project Management & Product Backlog — AskHR

> Lớp **thực thi Scrum** của AskHR, tích hợp bên trong `aidlc-docs/` sau khi nới lỏng quy định của quy trình AI-DLC (`CLAUDE.md`). File này vừa là **hub mô tả lớp PM** vừa là **product backlog** map 8 UoB (Epic) ra Story. Story chỉ tóm tắt; nguồn chân lý nghiệp vụ là các `units-*` (xem [[Home]]).

## Quan hệ với `aidlc-docs/` (AI-DLC)

| Lớp | Vị trí | Vai trò | Ai quản lý |
|---|---|---|---|
| **Inception / Knowledge** | `aidlc-docs/` | Requirement, UoB (Epic), ADR. Source of truth nghiệp vụ & kiến trúc. | AI-DLC workflow + người |
| **Execution / Scrum** | `project-management/` | Backlog, Sprint board, Task chi tiết. Cập nhật hằng ngày. | Team (thủ công) |

- **UoB = Epic.** Mỗi UoB là một Epic; sau khi hợp nhất tài liệu, các UoB sống dưới dạng section trong các file `units-*` (vd [[units-retrieval-answer#UoB-01: Answer Policy Question]]). Story/Task được xé nhỏ ở file này và `tasks/`.
- **Không trùng lặp nội dung nghiệp vụ.** Task chỉ link tới UoB tương ứng thay vì copy lại scope/contract.
- **Construction artifacts của AI-DLC** (`{unit}-functional-design.md`, `{unit}-code-generation-plan.md`, `aidlc-state.md`...) vẫn nằm phẳng trong `aidlc-docs/`. Task trong sprint **link** tới chúng, không thay thế chúng.

## Cấu trúc thư mục

```text
project-management/
├── product-backlog.md   # File này — hub PM + toàn bộ Epic → Story, ưu tiên, trạng thái
├── sprints/
│   ├── sprint-01.md      # Nền tảng RBAC & Index (P0)
│   ├── sprint-02.md      # Core trả lời & Slack Gateway (P1)
│   ├── sprint-03.md      # Web Chat & Escalation/UX (P2/P3)
│   └── sprint-04.md      # Admin Portal & Cấu hình (P2/P3)
└── tasks/
    ├── task-template.md  # Mẫu task chi tiết
    └── TASK-xxxx-*.md    # Task cần document riêng (API contract, schema...)
```

## Quy ước

- **ID Story**: `S-<UoB><seq>` ví dụ `S-0101` = UoB-01, story 1. **ID Task**: `TASK-<seq>`.
- **Priority**: `P0` blocker nền tảng · `P1` core MVP · `P2` quan trọng · `P3` sau.
- **Trạng thái Kanban**: `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked.
- Estimate (story point) để trống — team điền ở Sprint Planning.
- Tạo file riêng trong `tasks/` **chỉ khi** task cần chi tiết kỹ thuật (API contract, DB schema). Task nhỏ ghi inline trong `sprint-xx.md`.
- Mọi note giữ frontmatter và `[[wikilink]]` theo đúng Vault Conventions của [[Home]].
- ⚠️ **Yêu cầu Obsidian** (hoặc extension Foam/Markdown Memo trong VS Code) để resolve `[[ ]]`. VS Code Markdown gốc không hỗ trợ.

## Epic → Story

### Epic UoB-01 — Answer Policy Question · [[units-retrieval-answer#UoB-01: Answer Policy Question]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0101 | Là User, tôi nhận câu trả lời policy chỉ từ corpus HR, có citation hợp lệ. | P1 | | [ ] |
| S-0102 | Là User, khi không đủ nguồn, bot từ chối lịch sự và chỉ tới HR contact (out-of-scope). | P1 | | [ ] |
| S-0103 | Là HR, retrieval được security-trim theo Authorization Context (deny-by-default). | P0 | | [x] |
---
type: product-backlog
status: draft
owner: Product Owner / Eng / Scrum Master
tags: [scrum, backlog, process]
related: ["[[Home]]", "[[requirements]]"]
created: 2026-06-07
updated: 2026-06-07
---

# Project Management & Product Backlog — AskHR

> Lớp **thực thi Scrum** của AskHR, tích hợp bên trong `aidlc-docs/` sau khi nới lỏng quy định của quy trình AI-DLC (`CLAUDE.md`). File này vừa là **hub mô tả lớp PM** vừa là **product backlog** map 8 UoB (Epic) ra Story. Story chỉ tóm tắt; nguồn chân lý nghiệp vụ là các `units-*` (xem [[Home]]).

## Quan hệ với `aidlc-docs/` (AI-DLC)

| Lớp | Vị trí | Vai trò | Ai quản lý |
|---|---|---|---|
| **Inception / Knowledge** | `aidlc-docs/` | Requirement, UoB (Epic), ADR. Source of truth nghiệp vụ & kiến trúc. | AI-DLC workflow + người |
| **Execution / Scrum** | `project-management/` | Backlog, Sprint board, Task chi tiết. Cập nhật hằng ngày. | Team (thủ công) |

- **UoB = Epic.** Mỗi UoB là một Epic; sau khi hợp nhất tài liệu, các UoB sống dưới dạng section trong các file `units-*` (vd [[units-retrieval-answer#UoB-01: Answer Policy Question]]). Story/Task được xé nhỏ ở file này và `tasks/`.
- **Không trùng lặp nội dung nghiệp vụ.** Task chỉ link tới UoB tương ứng thay vì copy lại scope/contract.
- **Construction artifacts của AI-DLC** (`{unit}-functional-design.md`, `{unit}-code-generation-plan.md`, `aidlc-state.md`...) vẫn nằm phẳng trong `aidlc-docs/`. Task trong sprint **link** tới chúng, không thay thế chúng.

## Cấu trúc thư mục

```text
project-management/
├── product-backlog.md   # File này — hub PM + toàn bộ Epic → Story, ưu tiên, trạng thái
├── sprints/
│   ├── sprint-01.md      # Nền tảng RBAC & Index (P0)
│   ├── sprint-02.md      # Core trả lời & Slack Gateway (P1)
│   ├── sprint-03.md      # Web Chat & Escalation/UX (P2/P3)
│   └── sprint-04.md      # Admin Portal & Cấu hình (P2/P3)
└── tasks/
    ├── task-template.md  # Mẫu task chi tiết
    └── TASK-xxxx-*.md    # Task cần document riêng (API contract, schema...)
```

## Quy ước

- **ID Story**: `S-<UoB><seq>` ví dụ `S-0101` = UoB-01, story 1. **ID Task**: `TASK-<seq>`.
- **Priority**: `P0` blocker nền tảng · `P1` core MVP · `P2` quan trọng · `P3` sau.
- **Trạng thái Kanban**: `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked.
- Estimate (story point) để trống — team điền ở Sprint Planning.
- Tạo file riêng trong `tasks/` **chỉ khi** task cần chi tiết kỹ thuật (API contract, DB schema). Task nhỏ ghi inline trong `sprint-xx.md`.
- Mọi note giữ frontmatter và `[[wikilink]]` theo đúng Vault Conventions của [[Home]].
- ⚠️ **Yêu cầu Obsidian** (hoặc extension Foam/Markdown Memo trong VS Code) để resolve `[[ ]]`. VS Code Markdown gốc không hỗ trợ.

## Epic → Story

### Epic UoB-01 — Answer Policy Question · [[units-retrieval-answer#UoB-01: Answer Policy Question]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0101 | Là User, tôi nhận câu trả lời policy chỉ từ corpus HR, có citation hợp lệ. | P1 | | [x] |
| S-0102 | Là User, khi không đủ nguồn, bot từ chối lịch sự và chỉ tới HR contact (out-of-scope). | P1 | | [x] |
| S-0103 | Là HR, retrieval được security-trim theo Authorization Context (deny-by-default). | P0 | | [x] |
| S-0104 | Là User, câu hỏi nhạy cảm/vượt thẩm quyền được freeze + warm handoff cho HR. | P2 | | [x] |
| S-0105 | Là HR, cấu hình Skill (instructions/answer policy) qua progressive disclosure. | P2 | | [x] |

### Epic UoB-02 — Ingest & Index HR Documents · [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0201 | Là HR, ingest `.docx/.md/PDF` → chunk + embed + index với citation metadata. | P0 | | [x] |
| S-0202 | Là HR, mỗi chunk gắn permission metadata (role/tag/BU/sensitivity). | P0 | | [x] |
| S-0203 | Là HR, re-index khi tài liệu mới/đổi (hash/timestamp/trigger), không cron cứng. | P2 | | [x] |

### Epic UoB-03 — Slack Mention & Thread Context · [[units-channels#UoB-03: Slack Mention & Thread Context]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0301 | Là User, gọi bot qua `@mention`/DM trong Slack và nhận trả lời trong thread. | P1 | | [x] |
| S-0302 | Là User, thấy loading state và error UX khi pipeline lỗi/timeout. | P2 | | [x] |
| S-0303 | Là dev, Slack gateway chuẩn hóa request thành `AskHrRequest` (core không phụ thuộc Slack type). | P1 | | [x] |

### Epic UoB-04 — RBAC & Identity Access · [[units-security-identity#UoB-04: RBAC / Identity & Access]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0401 | Là hệ thống, resolve identity → Authorization Context (roles/tags/BU/level). | P0 | | [x] |
| S-0402 | Là HR, security-trimmed retrieval enforce server-side theo role/tag. | P0 | | [x] |

### Epic UoB-05 — Admin Portal & Monitoring · [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0501 | Là HR Admin, quản lý tài liệu/mapping qua control plane. | P2 | | [/] |
| S-0502 | Là HR Admin, cấu hình persona/tone và provider config. | P2 | | [x] |
| S-0503 | Là HR Admin, xem token usage & monitoring. | P3 | | [x] |

### Epic UoB-06 — Feedback, Audit & Analytics · [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0601 | Là hệ thống, ghi audit trail mỗi câu trả lời (masked question + hash). | P1 | | [x] |
| S-0602 | Là User, gửi feedback (👍/👎) trên câu trả lời. | P2 | | [x] |
| S-0603 | Là HR, phân loại severity P1/P2/P3 và routing escalation. | P2 | | [x] |

### Epic UoB-07 — Multi-Provider Config · [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0701 | Là dev, abstraction layer cho GitHub Models/OpenAI/Azure OpenAI/Gemini/Anthropic. | P1 | | [x] |
| S-0702 | Là HR Admin, chọn provider/model per skill mà không sửa code. | P2 | | [x] |

### Epic UoB-08 — Web Chat Channel · [[units-channels#UoB-08: Web Chat Channel]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0801 | Là User, chat qua web với streaming response. | P2 | | [x] |
| S-0802 | Là User, xem conversation history và gửi feedback trên web. | P3 | | [x] |
| S-0803 | Là hệ thống, web identity resolver → Authorization Context. | P2 | | [x] |

### Epic UoB-09 — MS Teams Channel · [[units-channels#UoB-09: MS Teams Channel]]
| ID | Story | Pri | Pts | Status |
|---|---|---|---|---|
| S-0901 | Là User, chat qua MS Teams với SSO. | P2 | | [/] |

## Đề xuất thứ tự Sprint (gợi ý)

1. **Nền tảng (P0)**: S-0201, S-0202, S-0401, S-0402, S-0103 — không có RBAC + index thì các UoB khác không chạy đúng.
2. **Core trả lời (P1)**: S-0101, S-0102, S-0301, S-0303, S-0601, S-0701.
3. **Hoàn thiện (P2/P3)**: phần còn lại.

## Changelog

- 2026-06-07: Gộp `README.md` (mô tả lớp PM + cấu trúc + quy ước) vào đầu file này; cập nhật link Epic sang dạng `[[units-*#UoB-xx]]` sau khi hợp nhất tài liệu UoB.
- 2026-06-11: Thêm UoB-09 / S-0901 cho MS Teams Channel; code gateway/resolver/card formatter đang In Progress, manual Azure Bot Service verification chờ hạ tầng.
- 2026-06-11: Sprint 08 Admin Portal implementation completed for S-0503, S-0602/S-0603, and S-0702; added monitoring, feedback, and provider-routing screens.
- 2026-06-11: Bắt đầu Sprint 09; cập nhật trạng thái các task P0 (S-0103, S-0201, S-0202, S-0401, S-0402) thành Done vì foundation đã được build từ Sprint 01.
- 2026-06-11: Rà soát toàn bộ codebase, chuyển sang Sprint 10; cập nhật Done cho các tính năng thực tế đã code xong ở các sprint cũ (Slack, Handoff, Skill). S-0501 chuyển sang In Progress.
