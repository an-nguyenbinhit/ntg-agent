---
type: uob
uob: ["05", "06", "07"]
status: draft
owner: HR / Eng / IT
tags: [admin-portal, governance, rbac, escalation, multi-provider]
related: ["[[requirements]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]", "[[units-channels#UoB-03: Slack Mention & Thread Context]]", "[[units-channels#UoB-08: Web Chat Channel]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]", "[[ADR-001-agent-runtime]]"]
created: 2026-06-06
updated: 2026-06-07
---

# Units — Governance & Ops

> Nhóm UoB quản trị/vận hành của AskHR: Admin Portal & monitoring (UoB-05), Feedback/Audit/Analytics & escalation (UoB-06) và Multi-Provider / Model Configuration (UoB-07). Hợp nhất từ ba file UoB gốc, **giữ nguyên nội dung**; mỗi UoB là một section H2.

## Mục lục

- [[#UoB-05: Admin Portal / Monitoring]]
- [[#UoB-06: Feedback, Audit & Analytics]]
- [[#UoB-07: Multi-Provider / Model Configuration]]


## UoB-05: Admin Portal / Monitoring

> **AI-DLC Inception artifact.** UoB này định nghĩa Admin Portal - control plane để HR/Admin vận hành AskHR mà không phụ thuộc developer.

### 1. Overview

Admin Portal là control plane của AskHR. Portal cho phép HR/Admin quản lý tài liệu, folder, tag/role mapping, persona, provider config, token usage, audit/history và trạng thái vận hành.

Nếu thiếu portal, các pipeline có thể chạy về kỹ thuật nhưng không vận hành được trong thực tế vì HR không thể tự cập nhật knowledge base, review feedback hoặc xử lý document freshness.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Dashboard, document/folder management, role/tag mapping UI, agent/persona config, token usage, health/monitoring, feedback/audit review, approval/freshness queue. |
| Out of scope | Ingest/chunk/embed logic, authorization engine, provider routing engine, feedback event processing. |
| Explicit non-goals | Không bypass security trimming; không hiển thị raw PII/log cho role không đủ quyền. |
| Security boundary | Mọi màn hình document/log/config phải kiểm tra admin role và masking/retention policy. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| HR Admin | Quản lý tài liệu, mapping, persona, feedback review. | Tự vận hành knowledge base và chất lượng câu trả lời. |
| HR Knowledge Owner | Review document conflict, freshness, approval queue. | Biết tài liệu nào cần sửa, approve hoặc retire. |
| IT Support | Theo dõi lỗi vận hành, provider health, latency, token usage. | Debug nhanh khi bot lỗi hoặc chi phí tăng. |
| System Admin | Quản lý quyền admin, provider config, secrets reference. | Kiểm soát cấu hình nhạy cảm và audit config changes. |

### 4. Preconditions

- Admin authentication/authorization đã có role `HRAdmin`, `KnowledgeOwner`, `ITSupport`, `SystemAdmin`.
- Backend expose application services từ từng UoB; portal không gọi trực tiếp database để bypass business rules.
- Audit masking/retention policy từ [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] đã được thống nhất.
- Provider credentials lưu trong secret store; portal chỉ thao tác secret reference.

### 5. Main Flow

1. Admin đăng nhập qua SSO/internal auth.
2. Portal resolve admin role và áp quyền màn hình.
3. Dashboard hiển thị ingestion status, unresolved feedback, P1/P2 incidents, document freshness, token usage và provider health.
4. HR Admin upload/replace document hoặc mở document record để xem version, tags, owner, effective/expired date.
5. HR Admin gán role/tag/folder mapping; mapping lưu vào store của [[units-security-identity#UoB-04: RBAC / Identity & Access]].
6. HR Knowledge Owner review queue: conflict, expired document, pending approval, unanswered/out-of-scope questions.
7. IT Support xem operational health: failed ingestion, RAG latency, provider error rate, Slack/Web Chat errors.
8. Mọi thay đổi config/mapping quan trọng được audit.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Admin thiếu quyền | User không có role phù hợp. | Từ chối truy cập, log audit event, không leak dữ liệu. |
| Mapping làm document không ai thấy | Admin remove toàn bộ role/tag hợp lệ. | Warning trước khi save, trừ khi cố ý quarantine. |
| Provider config sai | Health check fail. | Không promote config mới thành active; giữ route active cũ. |
| Document expired/conflict | Freshness/conflict rule phát hiện. | Đưa vào review queue; không tự sửa nội dung. |
| Log có PII | Message/comment chứa dữ liệu nhạy cảm. | Hiển thị masked view theo role; raw view chỉ khi có quyền và approval. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| HR operability | HR Admin upload/update/gán quyền/xem index status không cần developer. |
| Permission correctness | Mapping thay đổi được enforce ở request kế tiếp hoặc sau TTL cache. |
| Operational visibility | Admin thấy P1/P2/P3, fallback, feedback, token usage, document freshness trong một control surface. |
| Secure admin UX | Không màn hình nào leak raw conversation/document ngoài quyền admin. |
| Config auditability | Mọi thay đổi provider/persona/mapping có who/what/when/reason. |

### 8. Decisions

#### 8.1 Portal as Control Plane

- Portal gọi application services của từng UoB.
- Không duplicate ingest, retrieval, authorization hoặc provider routing logic trong frontend.
- Mỗi module UI có service boundary rõ ràng.

#### 8.2 Target Stack

| Layer | Technology |
|---|---|
| Frontend | Angular admin app. |
| Backend | ASP.NET Core API, Clean Architecture theo từng UoB service. |
| Data | SQL Server cho config, audit metadata, version records, usage metrics. |
| Azure | Application Insights / Azure Monitor, Azure OpenAI, Azure AI Search status. |

#### 8.3 Admin Authorization

| Role | Quyền chính |
|---|---|
| `HRAdmin` | Sửa tài liệu, tags, mapping nghiệp vụ, review feedback. |
| `KnowledgeOwner` | Approve document/version, xử lý conflict/freshness. |
| `ITSupport` | Xem health/log kỹ thuật, không mặc định thấy raw employee message. |
| `SystemAdmin` | Sửa provider credentials/routing và cấu hình nhạy cảm. |

#### 8.4 Document Freshness & Approval

- Portal sở hữu UI queue.
- Rule detection lấy tín hiệu từ UoB-02 và UoB-06.
- Document/version chưa approve không active trong vector store.

```jsonc
{
  "DocumentFreshness": {
    "ReviewAfterDays": 180,
    "WarnBeforeDays": 30,
    "OwnerEscalationChannel": "#askhr-admin-alerts"
  }
}
```

#### 8.5 Token Usage Dashboard

- Track theo provider, model, channel, feature, business unit, user group.
- Mặc định chỉ lưu metadata aggregate; không expose nội dung câu hỏi qua usage dashboard.
- Alert khi vượt daily/monthly budget.

```jsonc
{
  "UsageAlert": {
    "DailyTokenBudget": 1000000,
    "MonthlyCostBudget": 500,
    "NotifyChannel": "#askhr-admin-alerts"
  }
}
```

#### 8.6 Agent Config Plane (Skills)

Admin Portal cho HR cấu hình **hành vi** bot, không chỉ văn phong. Config được phân lớp:

| Lớp | HR điều khiển gì | Ai sửa |
|---|---|---|
| Persona | Tone, style, creativity cap, emoji. | HR Admin |
| Skill Registry | Bật/tắt, version, approve, scope từng năng lực. | HR Admin + Knowledge Owner |
| Skill Authoring | Trigger, instructions, clarifying questions, attachments, escalation của từng skill. | HR Knowledge Owner |
| Routing | Intent/topic/BU nào dùng skill nào; fallback skill. | HR Admin |
| Guardrails | Boundary cứng (never-answer salary/PII, require-citation, refuse-if-expired) và sensitive-topic handoff (chủ đề nhạy cảm/vượt thẩm quyền → HR Advisor). | System Admin + HR Admin |

Nguyên tắc:

- **Skill là config object HR sở hữu**, không phải code. Thêm năng lực mới không cần developer — đóng đúng promise "HR tự vận hành".
- **Progressive disclosure**: bot chỉ nạp skill khi câu hỏi match `description`. Giai đoạn Discovery tái dùng Intent Pre-Filter của [[units-channels#UoB-03: Slack Mention & Thread Context]] §8.3, giảm token (gắn budget [§8.5](#85-token-usage-dashboard)). Schema skill đầy đủ định nghĩa ở [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11.
- **Governance bắt buộc**: mỗi skill có `owner`, `version`, `approvalStatus`, `enabled`. Mọi thay đổi audit who/what/when/reason và đẩy event sang [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]. Skill `Draft`/chưa approve không active cho user traffic.
- **Provider-agnostic**: skill mô tả năng lực, không gọi model cụ thể; model route do [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] quyết định.
- **Skill set**: cung cấp tối thiểu các skill HR-curated (`policy-lookup`, `ambiguity-clarifier`, `escalate-to-hr`, `sensitive-handoff`) + boundary registry sạch để HR thêm skill không cần developer. Skill planner (chain nhiều skill) thuộc năng lực Agentic RAG.

```jsonc
{
  "SkillGovernance": {
    "RequireApprovalBeforeActivate": true,
    "DefaultStatus": "Draft",
    "AuditOnChange": true,
    "InitialSkillLimit": 3
  }
}
```

### 9. Modules

| Module | Chức năng | UoB/service phụ thuộc |
|---|---|---|
| Documents | Upload, replace, status, version, metadata, freshness. | [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] |
| Access | Roles, tags, user-role mapping, folder/document-tag mapping. | [[units-security-identity#UoB-04: RBAC / Identity & Access]] |
| Persona | Style prompt, allowed tone, creativity cap, danh sách emoji, channel profile. | [[units-retrieval-answer#UoB-01: Answer Policy Question]], [[units-channels#UoB-03: Slack Mention & Thread Context]], [[units-channels#UoB-08: Web Chat Channel]] |
| Skill Registry | Catalog năng lực bot: enable/disable, version, owner, approval status, scope theo topic/tag/BU. | [[units-retrieval-answer#UoB-01: Answer Policy Question]], [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] |
| Skill Authoring | Tạo/sửa skill: `description` (trigger), instructions, clarifying questions, attachments, escalation. | [[units-retrieval-answer#UoB-01: Answer Policy Question]], [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] |
| Routing | Map intent/topic/BU sang skill; chọn fallback skill khi không match. | [[units-channels#UoB-03: Slack Mention & Thread Context]], [[units-security-identity#UoB-04: RBAC / Identity & Access]] |
| Guardrails | Boundary dạng config: never-answer list (salary/PII), require-citation default, refuse-if-expired, sensitive-topic handoff. | [[units-retrieval-answer#UoB-01: Answer Policy Question]], [[units-security-identity#UoB-04: RBAC / Identity & Access]] |
| Feedback & Audit | Search conversation, review feedback, assign severity. | [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] |
| Analytics | Token usage, top questions, fallback rate, success rate. | [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] |
| Provider | Active model/provider, fallback order, health. | [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] |

> **Lưu ý**: 5 module `Persona`, `Skill Registry`, `Skill Authoring`, `Routing`, `Guardrails` hợp thành **Agent Config Plane** (xem §8.6). Trước đây nhóm này gộp trong một module "Agent" chỉ gồm persona/tone; tách ra để HR cấu hình được *hành vi* bot, không chỉ *văn phong*.

### 10. Dependencies

- [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]: document upload/version/status/freshness.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]]: role/tag/user mapping and admin authorization.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: feedback, audit log, analytics and masking policy.
- [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]: provider/model/fallback configuration.

### Changelog

- Chuẩn hóa Admin Portal thành control plane, không chứa business logic lõi.
- Thêm module matrix để map UI với UoB/service phụ thuộc.
- Làm rõ admin roles và raw log/PII visibility rules.
- Tách module "Agent" thành Agent Config Plane 5 lớp (Persona, Skill Registry, Skill Authoring, Routing, Guardrails) và thêm decision §8.6 cho skill governance.
- Skill set nêu dạng capability; đổi `MvpSkillLimit`→`InitialSkillLimit`.
- Thêm sensitive-topic handoff vào Guardrails (§9 module + §8.6).



## UoB-06: Feedback, Audit & Analytics

> **AI-DLC Inception artifact.** UoB này biến tín hiệu sau câu trả lời thành feedback loop cho HR review, audit/compliance và cải thiện corpus.

### 1. Overview

UoB-06 thu thập Like/Dislike/comment trên từng assistant message, lưu audit trail của question/answer/citation/permission context, sinh severity candidate P1/P2/P3 và cung cấp metrics để HR cải thiện tài liệu sau go-live.

UoB này không tự sửa policy và không tự publish answer mới. Mọi thay đổi knowledge quan trọng cần HR review/approval.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Feedback events, audit events, conversation/message metadata, severity candidates, analytics metrics, masking, retention, review workflow input. |
| Out of scope | Dashboard UI, Slack notification routing, answer generation, document ingestion. |
| Explicit non-goals | Không dùng feedback để tự động sửa policy hoặc auto-publish câu trả lời. |
| Privacy boundary | Raw text chỉ lưu khi HR/Legal approve; default là masked text + hash. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| User | Like/Dislike/comment/report issue trên answer. | Báo câu trả lời sai/thiếu nhanh, không phải mở ticket riêng. |
| HR Knowledge Owner | Review feedback và audit trail. | Biết tài liệu/câu trả lời nào cần sửa hoặc bổ sung. |
| HR Admin | Xem analytics, export report, review queue. | Theo dõi accuracy, top questions, fallback rate, satisfaction. |
| IT Support | Debug request và lỗi vận hành. | Có correlation id, latency, provider/model, retrieval metadata. |

### 4. Preconditions

- UoB-01 trả answer kèm `conversationId`, `messageId`, citations, confidence, retrieval metadata, provider/model và masked authorization summary.
- Channel adapter hiển thị feedback action.
- Admin Portal có review queue và audit search.
- Masking/retention policy được cấu hình trước khi lưu production traffic.

### 5. Main Flow

1. UoB-01 hoặc channel gateway phát sinh `ConversationEvent` sau mỗi answer/fallback.
2. Channel hiển thị feedback actions: Like, Dislike, optional comment/report issue.
3. User gửi feedback; hệ thống lưu `FeedbackEvent` gắn với assistant message.
4. Nếu feedback là Dislike/report, hệ thống tạo severity candidate dựa trên topic, metadata và signal từ UoB-01.
5. Event đi vào review queue của [[units-governance-ops#UoB-05: Admin Portal / Monitoring]].
6. Với P1/P2 candidate, UoB-06 phát signal để [[units-channels#UoB-03: Slack Mention & Thread Context]] route notification.
7. Analytics aggregates cập nhật: helpful rate, fallback rate, top questions, disliked topics, citation coverage, latency/cost.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Feedback không có comment | User chỉ bấm Like/Dislike. | Vẫn lưu event; severity dựa trên metadata, không đoán quá mức. |
| Nhiều feedback cùng message | User đổi rating hoặc nhiều user feedback. | Giữ event history; dashboard dùng latest state hoặc aggregate tùy view. |
| Message chứa PII | Question/comment có email, phone, employee id, tên người. | Mask trước khi lưu default view; raw chỉ theo policy approve. |
| Citation expired/conflict | Answer cite document expired hoặc conflict. | Tạo review item severity cao hơn. |
| Out-of-scope lặp lại | Nhiều query tương tự fallback. | Group theo normalized topic/question hash để HR bổ sung tài liệu. |
| Sensitive handoff | UoB-01 freeze auto-answer vì chủ đề nhạy cảm/vượt thẩm quyền. | Lưu handoff event đã masked, gắn owner HR Advisor và trạng thái review; không dùng làm sentiment profiling. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| Traceability | Mỗi assistant message truy vết được user/channel/time/answer/citation/model/confidence/permission summary. |
| Reviewability | HR thấy top disliked answers, unanswered questions, fallback rate và document risk. |
| Severity signal quality | P1/P2/P3 signal đủ rõ để UoB-03 route notify mà không tự đoán. |
| Privacy | Audit log không trở thành nguồn rò rỉ PII hoặc tài liệu ngoài quyền. |
| Analytics usefulness | Metrics hỗ trợ quyết định bổ sung/cập nhật tài liệu. |

### 8. Decisions

#### 8.1 Severity Signal Contract

| Candidate | Trigger |
|---|---|
| **P1** | Dislike/report trên Benefit/Leave/quyền lợi, answer dùng expired doc, conflict doc hoặc HR manually marks as policy-impacting. |
| **P2** | Dislike/report trên Process/Onboarding/Working Time, low confidence nhưng vẫn trả lời, user báo sai quy trình. |
| **P3** | No-source, out-of-scope, missing information, low-confidence fallback. |

UoB-06 sinh signal; UoB-03 route notify.

#### 8.2 Retention & Masking

- Default lưu masked text + hash.
- Raw text chỉ lưu khi HR/Legal approve.
- Masking tối thiểu: email, phone, employee id, person name nếu nhận diện được, free-text comment.

```jsonc
{
  "AuditRetention": {
    "RawDays": 0,
    "MaskedDays": 365,
    "KeepAggregatesForever": true
  }
}
```

#### 8.3 Required Analytics

- Top questions theo topic.
- Top unanswered/out-of-scope questions.
- Helpful rate / dislike rate theo topic, document, channel.
- Citation coverage.
- Retrieval confidence distribution.
- Token/cost/latency theo provider/model/channel.
- Document risk: expired citation, conflict count, freshness overdue.

#### 8.4 Review Workflow

- Mỗi feedback issue có owner, status, severity, resolution note.
- Resolution không sửa answer cũ.
- Nếu cần sửa policy, tạo task cho UoB-02 hoặc approval queue trong UoB-05.
- Khi resolved, link tới document version hoặc config change đã xử lý.

### 9. Event Contracts

#### ConversationEvent

```jsonc
{
  "conversationId": "string",
  "messageId": "string",
  "channelType": "Slack",
  "channelThreadId": "string",
  "userId": "internal-user-id-or-anonymous",
  "timestamp": "2026-06-06T00:00:00Z",
  "questionHash": "sha256",
  "questionTextMasked": "string",
  "answerTextMasked": "string",
  "searchQueries": ["string"],
  "toolCallArguments": {},
  "citations": [],
  "retrievalConfidence": 0.82,
  "authorizationContextSummary": {
    "roles": ["Employee"],
    "businessUnits": ["Vietnam"],
    "countries": ["Vietnam"],
    "legalEntities": ["VN-Legal-Entity"],
    "level": "Staff",
    "isAnonymous": false
  },
  "provider": "AzureOpenAI",
  "model": "string",
  "latencyMs": 3200,
  "tokenUsage": {
    "promptTokens": 1000,
    "completionTokens": 300
  }
}
```

#### FeedbackEvent

```jsonc
{
  "feedbackId": "string",
  "messageId": "string",
  "userId": "internal-user-id",
  "rating": "Like",
  "commentMasked": null,
  "topic": "Leave",
  "severityCandidate": "P1",
  "status": "Open",
  "createdAt": "2026-06-06T00:00:00Z"
}
```

### 10. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]]: answer metadata, citations, confidence, fallback reason.
- [[units-channels#UoB-03: Slack Mention & Thread Context]]: feedback action trên Slack và severity notification routing.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]]: Authorization Context summary và quyền xem audit.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]: review queue, analytics dashboard, audit search.

### Changelog

- Chuẩn hóa event model sang `jsonc` contract.
- Làm rõ default privacy: masked text + hash, raw text chỉ khi HR/Legal approve.
- Tách severity signal generation khỏi notification routing.
- Bổ sung `searchQueries` và `toolCallArguments` vào `ConversationEvent` để phục vụ debug RAG pipeline.



## UoB-07: Multi-Provider / Model Configuration

> **AI-DLC Inception artifact.** UoB này định nghĩa abstraction và governance cho nhiều LLM/embedding provider mà không làm loãng quyết định Azure-first.

### 1. Overview

AskHR cần tránh hardcode provider/model vào RAG Pipeline. UoB-07 cung cấp provider registry, model route, fallback policy, health check và token/cost tracking cho các provider như Azure OpenAI, OpenAI, GitHub Models, Gemini và Anthropic.

Hệ thống dùng **Azure OpenAI** làm default cho chat và embeddings vì stack mục tiêu là Azure.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Provider registry, model capability metadata, route config, fallback order, health check, per-feature model selection, token/cost metadata, secret reference. |
| Out of scope | Business logic answer policy, vector store adapter, UI chỉnh config. |
| Explicit non-goals | Không cho admin nhập raw API key trong UI; không gửi HR data sang provider chưa được Security/Legal approve. |
| Governance boundary | Provider phải có approval status và data policy trước khi dùng production. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| System Admin | Quản lý provider/model/fallback route. | Cấu hình an toàn, rollback được, không hardcode. |
| IT Support | Theo dõi health, error, cost, quota. | Biết provider nào lỗi hoặc tăng chi phí. |
| HR Admin | Cấu hình persona/tone ở mức nghiệp vụ. | Không cần hiểu provider internals. |
| RAG Pipeline | Consumer của model abstraction. | Gọi model theo capability, không phụ thuộc SDK cụ thể. |

### 4. Preconditions

- Secret store đã có sẵn hoặc được provision trước khi bật provider.
- Security/Legal approval xác định provider nào được xử lý HR data.
- UoB-05 có UI/control plane để sửa route theo role `SystemAdmin`.
- UoB-06 nhận provider/model/token metadata để analytics.

### 5. Main Flow

1. System Admin cấu hình provider trong Admin Portal.
2. Mỗi provider có metadata: capability, max context, streaming, structured output, tool calling, embeddings, data residency, cost tier, approval status.
3. RAG Pipeline yêu cầu capability theo feature, ví dụ `AnswerGeneration`, `IntentClassifier`, `GroundednessJudge`, `Embedding`.
4. Model router chọn route active dựa trên config, health status và data policy.
5. Nếu primary provider lỗi và fallback hợp lệ, router chuyển sang fallback.
6. UoB-06 nhận provider/model/token/cost/latency metadata cho audit và analytics.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Provider lỗi tạm thời | Rate limit, quota, auth, model unavailable. | Fallback nếu provider thay thế cùng capability và đã approve cho data class. |
| Provider không support streaming | Feature cần streaming token-by-token. | Không route vào provider đó trừ khi config chấp nhận degrade. |
| Embedding model đổi | Model/dimension/space khác. | Tạo index version mới và re-index; không trộn embedding space. |
| Security chưa approve | Provider ở trạng thái `Disabled` hoặc `BenchmarkOnly`. | Không dùng production traffic. |
| Model output drift | Benchmark/golden set fail. | Không promote model active; tạo review item. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| Provider abstraction | Không có SDK/provider-specific type trong core use case của UoB-01/UoB-02. |
| Feature routing | Có thể đổi model cho intent classification hoặc groundedness judge mà không đổi answer generation model. |
| Safe fallback | Fallback không vi phạm data policy và không làm mất citation/grounding contract. |
| Cost observability | Token/cost/latency theo provider/model được ghi vào UoB-06. |
| Embedding consistency | Không trộn embedding model/dimension trong cùng active index. |

### 8. Decisions

#### 8.1 Default Provider

- Default: **Azure OpenAI** cho chat + embeddings.
- Lý do: cùng Azure stack, phù hợp Azure AI Search và đơn giản hóa networking/IAM/observability.
- Provider khác chỉ bật sau Security/Legal approval và benchmark trên golden Q&A set.

#### 8.2 Capability-Based Routing

- Application gọi theo feature/capability, không gọi trực tiếp `gpt-*` hoặc provider SDK.
- Infrastructure adapter map route sang SDK cụ thể.
- Route phải audit được khi thay đổi.

#### 8.3 Embedding Contract

- Embedding provider/model là contract đặc biệt.
- Phải lưu `embeddingProvider`, `embeddingModel`, `embeddingDimension`, `indexVersion`.
- Đổi embedding model đồng nghĩa tạo index version mới và re-index qua [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]].
- Không fallback embedding provider trong cùng index nếu vector space khác nhau.

#### 8.4 Health Check & Fallback

Health check phân biệt:

- auth failure.
- quota/rate limit.
- latency spike.
- model unavailable.
- content/data policy block.

Fallback chỉ chạy nếu capability tương đương, provider đã approve cho data class và feature không yêu cầu embedding-space consistency.

#### 8.5 Secrets & Config

- Secret lưu trong Azure Key Vault hoặc equivalent secret store.
- SQL Server chỉ lưu secret reference.
- Production config change cần role `SystemAdmin`.
- HR Admin không được sửa provider route/credential.

#### 8.6 Agent Runtime

- Provider abstraction chạy trên agent runtime chốt ở [[ADR-001-agent-runtime]] (Microsoft Agent Framework), khớp Azure-first nhưng không khóa provider.
- Model client của runtime là consumer của `ModelRoute`/`ProviderMetadata`; runtime **không** được bypass capability routing hay data policy của UoB này.
- Tính năng experimental của runtime (vd compaction) không phải guarantee; xem ADR §6.

### 9. Contracts

#### ModelRoute

```jsonc
{
  "feature": "AnswerGeneration",
  "primaryProvider": "AzureOpenAI",
  "primaryModel": "gpt-4.1-mini",
  "fallbacks": [
    {
      "provider": "OpenAI",
      "model": "gpt-4.1-mini"
    }
  ],
  "requiredCapabilities": ["chat", "streaming", "structured-output"],
  "dataPolicy": "InternalApprovedOnly",
  "enabled": true
}
```

#### ProviderMetadata

```jsonc
{
  "provider": "AzureOpenAI",
  "approvalStatus": "ProductionApproved",
  "dataResidency": "AzureRegionControlled",
  "capabilities": ["chat", "embeddings", "streaming", "structured-output"],
  "secretReference": "kv://askhr/prod/azure-openai",
  "healthStatus": "Healthy"
}
```

### 10. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]]: dùng model route cho answer generation, intent classification, optional groundedness judge.
- [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]: dùng embedding route và version index theo embedding model.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]: UI cấu hình provider/model/fallback và health.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: nhận provider/model/token/cost metadata.

### Changelog

- Chuẩn hóa provider config thành capability-based routing.
- Làm rõ Azure OpenAI là default provider, provider khác cần approval + benchmark.
- Azure OpenAI là default provider (requirement); provider khác cần Security/Legal approval + benchmark golden-set.
- Thêm contract `ModelRoute` và `ProviderMetadata` bằng `jsonc`.
- Thêm §8.6 Agent Runtime trỏ tới [[ADR-001-agent-runtime]] (MAF làm runtime substrate, không bypass capability routing/data policy).

## Changelog (Consolidation)

- 2026-06-07: Hợp nhất UoB-05 (Admin Portal / Monitoring) + UoB-06 (Feedback, Audit & Analytics) + UoB-07 (Multi-Provider / Model Configuration) vào `units-governance-ops.md`. Mỗi UoB là một section H2, sub-section demote một cấp, per-UoB Table of Contents thay bằng "Mục lục" ở đầu file, và wikilink cập nhật sang dạng `[[file#heading]]`. Nội dung nghiệp vụ giữ nguyên.
