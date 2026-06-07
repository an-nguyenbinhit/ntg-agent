---
type: uob
uob: ["04"]
status: draft
owner: HR / Eng
tags: [rbac, security]
related: ["[[requirements]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]", "[[units-channels#UoB-03: Slack Mention & Thread Context]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]"]
created: 2026-06-06
updated: 2026-06-07
---

# Units — Security & Identity

> Nhóm UoB phân quyền của AskHR: RBAC, identity resolution và security-trimmed retrieval theo role/tag/BU/level (UoB-04). Hợp nhất từ file UoB gốc, **giữ nguyên nội dung**.

## Mục lục

- [[#UoB-04: RBAC / Identity & Access]]


## UoB-04: RBAC / Identity & Access

> **AI-DLC Inception artifact.** UoB này là authorization boundary dùng chung cho AskHR. Nó resolve identity, tính quyền truy cập và cung cấp Authorization Context để RAG retrieval được security-trimmed.

### 1. Overview

AskHR phải đảm bảo user chỉ retrieve và nhận citation từ tài liệu mà role/tag/business unit của họ được phép thấy. Dù corpus chỉ dùng Internal-Public data, security trimming vẫn là requirement bắt buộc.

UoB-04 không generate answer. Nó cung cấp identity và authorization data để UoB-01 enforce quyền ở tầng retrieval.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | User identity, role, tag, business unit, `anonymous` role, Authorization Context, admin-managed mapping store. |
| Access enforcement | Department/Business Unit + Tag/Role; trục Country/Level/Legal Entity qua cùng schema phân quyền. |
| Relevance (personalization) | `Level`/`applicableTo` làm tín hiệu relevance cho Advanced RAG (chọn đúng biến thể policy theo cấp bậc/đối tượng), **tách biệt** với access. |
| Explicit non-goals | Không đoán role khi identity không resolve được; không dựa vào prompt để enforce quyền. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| Authenticated User | User map được sang internal identity. | Chỉ thấy nội dung đúng quyền. |
| Anonymous User | Guest, external hoặc user chưa sync. | Chỉ thấy tài liệu `public-all` nếu HR cho phép. |
| HR Admin | Quản lý role/tag/user/document mapping. | Cấu hình quyền không cần developer. |
| System Admin | Quản lý integration với identity provider. | Đồng bộ identity an toàn, trace được. |

### 4. Preconditions

- Channel gateway cung cấp channel user id hoặc session identity.
- Mapping store có user-role, role-tag, document-tag hoặc folder-tag mapping.
- UoB-02 đã gắn permission metadata cho chunk.
- Admin Portal kiểm soát quyền ai được sửa mapping.

### 5. Main Flow

1. Request đến từ Slack/Web Chat kèm user id hoặc session identity.
2. `IIdentityResolver` map channel identity sang internal user.
3. RBAC service lấy roles, allowed tags và business units từ mapping store.
4. Service trả Authorization Context cho gateway/RAG Pipeline.
5. UoB-01 áp metadata filter trên vector store: role/tag/BU/sensitivity.
6. Nếu sau filter không còn chunk hợp lệ, UoB-01 fallback như no-source.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Identity không resolve được | Slack external, web session lỗi, user chưa sync. | Gán `anonymous`, deny-by-default, chỉ tag `public-all`. |
| User nhiều role/BU | User thuộc nhiều nhóm. | Union quyền hợp lệ, vẫn filter theo tag/sensitivity. |
| Tài liệu thiếu metadata quyền | UoB-02 ingest thiếu role/tag. | Chunk bị deny-by-default và cảnh báo HR Admin. |
| Mapping thay đổi | HR đổi role hoặc tag. | Request mới dùng quyền mới; cache TTL ngắn. |
| Private Slack channel | Bot được mention trong private channel. | Vẫn enforce theo user role/tag; channel privacy không thay thế RBAC. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| No data leakage | User không nhận chunk/citation ngoài quyền. |
| Deny-by-default | Chunk thiếu metadata không active cho retrieval. |
| Admin operability | HR Admin sửa mapping qua portal không cần developer. |
| Latency | Identity resolution không làm pipeline vượt mục tiêu latency; dùng TTL cache có kiểm soát. |
| Auditability | Có log resolve/trim đủ để điều tra rò rỉ hoặc mapping sai. |

### 8. Decisions

#### 8.1 Permission Model

- Dùng **RBAC + Tag-based access**.
- `BusinessUnit` là trục tag đặc biệt vì data source chia theo BU.
- Hệ thống phải hỗ trợ trục Country/Level/Legal Entity trong schema phân quyền.
- `Level`/`applicableTo` còn là tín hiệu **relevance** cho Advanced RAG (xem [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] §9), tách biệt với access enforcement.
- Deny-by-default là nguyên tắc bất biến.

#### 8.2 Anonymous Role

- `anonymous` là system role.
- Default chỉ được cấp tag `public-all`.
- HR quyết định tài liệu nào có tag `public-all`.

```jsonc
{
  "AnonymousAccess": {
    "Enabled": true,
    "AllowedTags": ["public-all"]
  }
}
```

#### 8.3 Identity Resolver Abstraction

| Channel | Resolver | Mapping strategy |
|---|---|---|
| Slack | `SlackIdentityResolver` | `slack_user_id` → email hoặc mapping table. |
| Web | `WebIdentityResolver` | Session/SSO token → internal user. |
| Future channel | Channel-specific adapter | Implement `IIdentityResolver`. |

Cache Authorization Context 5-15 phút, config được. TTL ngắn giúp cân bằng latency và quyền mới khi HR đổi mapping.

#### 8.4 Enforcement Layer

Quyền phải enforce trong vector store query bằng filterable metadata. Prompt-level guardrail chỉ là lớp diễn đạt, không phải security boundary.

#### 8.5 Admin Mapping

- HR Admin quản lý role, tag, user-role mapping và document/folder-tag mapping qua [[units-governance-ops#UoB-05: Admin Portal / Monitoring]].
- UoB-04 sở hữu data model và authorization logic; Admin Portal chỉ là UI/control plane.
- Auto-tagging theo folder/BU được phép nhưng phải cho override.

#### 8.6 Audit

Log tối thiểu:

- user id hoặc anonymous id.
- roles/tags/BU resolved.
- query hash.
- số chunk trước/sau trim.
- reason nếu chunk bị loại do permission.

Log phải tuân theo masking/retention của [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

### 9. Data Model

```jsonc
{
  "User": {
    "id": "internal-user-id",
    "email": "user@company.com",
    "businessUnits": ["Vietnam"],
    "countries": ["Vietnam"],
    "legalEntities": ["VN-Legal-Entity"],
    "level": "Staff",
    "isActive": true
  },
  "Role": {
    "id": "Employee",
    "tags": ["public-all", "vn-policy"]
  },
  "DocumentPermission": {
    "documentId": "doc-001",
    "allowedRoles": ["Employee", "Manager"],
    "tags": ["vn-policy"],
    "businessUnit": "Vietnam",
    "country": "Vietnam",
    "legalEntity": "VN-Legal-Entity",
    "applicableTo": ["Manager"],
    "sensitivity": "Internal"
  }
}
```

`allowedRoles`/`tags`/`businessUnit`/`country`/`legalEntity`/`sensitivity` là trục **access** (enforce bằng metadata filter). `applicableTo`/`level` là tín hiệu **relevance** cho Advanced RAG ([[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] §9): khi user được phép thấy nhiều biến thể policy, retrieval chọn đúng biến thể theo `level` của user, không trộn mức.

### 10. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]]: dùng Authorization Context để security-trim retrieval.
- [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]: gắn metadata quyền cho chunk khi ingest.
- [[units-channels#UoB-03: Slack Mention & Thread Context]]: implement Slack identity adapter.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]: UI quản trị mapping.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: audit/retention/masking cho authorization logs.

### Changelog

- Làm rõ RBAC là mandatory dù corpus là Internal-Public.
- Tách security boundary khỏi prompt guardrail.
- Chuẩn hóa Authorization Context, `anonymous` role và deny-by-default behavior.
- Chuyển sang dạng requirement: trục Country/Level/Legal Entity là requirement trong schema; thêm `Level`/`applicableTo` làm tín hiệu relevance (Advanced RAG) tách biệt access; thêm `level` vào User và `country`/`applicableTo` vào DocumentPermission.

## Changelog (Consolidation)

- 2026-06-07: Hợp nhất UoB-04 (RBAC / Identity & Access) vào `units-security-identity.md`. Mỗi UoB là một section H2, sub-section demote một cấp, per-UoB Table of Contents thay bằng "Mục lục" ở đầu file, và wikilink cập nhật sang dạng `[[file#heading]]`. Nội dung nghiệp vụ giữ nguyên.
