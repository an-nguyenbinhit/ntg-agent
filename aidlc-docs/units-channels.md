---
type: uob
uob: ["03", "08"]
status: draft
owner: HR / Eng
tags: [slack, web-chat, escalation, rbac]
related: ["[[requirements]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]"]
created: 2026-06-06
updated: 2026-06-07
---

# Units — Channels

> Nhóm UoB kênh giao tiếp của AskHR: Slack gateway với `@mention`/DM/thread context (UoB-03) và Web Chat với streaming/conversation history (UoB-08). Hợp nhất từ hai file UoB gốc, **giữ nguyên nội dung**; mỗi UoB là một section H2.

## Mục lục

- [[#UoB-03: Slack Mention & Thread Context]]
- [[#UoB-08: Web Chat Channel]]



## UoB-03: Slack Mention & Thread Context

> **AI-DLC Inception artifact.** UoB này là Slack gateway/adapter: nhận Slack event, chuẩn hóa context, resolve identity, gọi RAG Pipeline và post response về đúng channel/thread.

### 1. Overview

Slack là channel đầu tiên của AskHR. Gateway chịu trách nhiệm nhận `@mention` hoặc DM, lấy thread context phù hợp, resolve Slack user thành identity nội bộ, gọi [[units-retrieval-answer#UoB-01: Answer Policy Question]] và trả kết quả về Slack bằng `mrkdwn`.

Gateway không chứa business logic trả lời policy; logic đó thuộc RAG Pipeline.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Slack events, DM, `@mention`, thread context, loading state, Slack response formatting, timeout/error UX, admin notification. |
| Out of scope | Answer generation, retrieval, document ingestion, RBAC engine. |
| Explicit non-goals | Không đưa Slack SDK type vào Application/Domain layer; không tự đoán policy answer trong gateway. |
| Security boundary | Gateway chỉ resolve identity và truyền Authorization Context; quyền vẫn enforce trong retrieval metadata filter. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| User | Gửi DM hoặc `@mention` bot trong Slack. | Bot hiểu đúng context và trả lời đúng thread. |
| Slack Workspace Admin | Cài đặt app, cấp OAuth scopes. | Scope tối thiểu, rõ dữ liệu bot đọc được. |
| IT Support | Theo dõi lỗi vận hành và Slack/API rate limits. | Có trace, alert và retry/fallback rõ ràng. |
| HR Admin | Nhận escalation/feedback severity. | Biết lỗi nào cần review ngay. |

### 4. Preconditions

- Slack app đã cài và cấp scopes tối thiểu.
- Slack user có thể map sang internal user qua email hoặc mapping table; nếu fail thì dùng `anonymous`.
- UoB-01 nhận normalized request, không phụ thuộc Slack-specific payload.
- Admin notification channel đã được cấu hình.

### 5. Main Flow

1. User gửi DM hoặc `@mention @AskHR <question>` trong Slack.
2. Gateway nhận Slack event và xác định đây là message mới hay reply trong thread.
3. Gateway lấy thread context theo strategy cấu hình.
4. Gateway hiển thị loading state, ví dụ reaction hoặc thinking message.
5. `SlackIdentityResolver` map `slack_user_id` sang internal user và lấy Authorization Context.
6. Gateway gọi UoB-01 bằng normalized request: question, context, channel metadata, Authorization Context.
7. Gateway nhận answer, citations, fallback reason và audit metadata.
8. Gateway format response bằng Slack `mrkdwn` và post vào đúng thread/channel.
9. Gateway xóa/cập nhật loading state và phát feedback action nếu được cấu hình.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Mention không phải câu hỏi | User tag bot trong hội thoại không cần trả lời. | Intent pre-filter bỏ qua hoặc phản hồi nhẹ, không chạy full RAG. |
| Thread quá dài | Context vượt token budget. | Dùng strategy `TokenBudget` hoặc `SummarizeOlder`. |
| Không resolve identity | Slack external/guest hoặc user chưa sync. | Gán `anonymous`, chỉ cho tag `public-all`. |
| Rate limit/spam | Slack API hoặc model/provider bị giới hạn. | Queue/throttle, trả fallback nếu timeout. |
| Pipeline error | UoB-01 hoặc provider lỗi. | User nhận fallback thân thiện; admin nhận operational alert. |
| Answer quality issue | Dislike/conflict/expired/low confidence. | Route severity P1/P2/P3 theo contract với UoB-06. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| Trigger accuracy | Bot phân biệt DM/mention hợp lệ với tag ngẫu nhiên. |
| Thread context quality | Follow-up trong thread được hiểu đúng qua test multi-turn. |
| Channel UX | Có loading state, response đúng thread, format không vỡ layout. |
| Security | Slack user luôn được resolve hoặc fallback `anonymous`; không bypass RBAC. |
| Observability | Operational errors và severity signals đi đúng admin route. |

### 8. Decisions

#### 8.1 Listen Scope

- Listen scope mặc định: **DM + `@mention`**.
- Hỗ trợ channel listening cho danh sách channel HR riêng qua config.

```jsonc
{
  "ListenScope": "MentionAndDM"
}
```

#### 8.2 Thread Context Strategy

| Strategy | Cách hoạt động | Khi dùng |
|---|---|---|
| `RecentN` | Lấy N message gần nhất. | Thread ngắn, rẻ, đơn giản. |
| `TokenBudget` | Lấy message gần nhất cho tới khi chạm token budget. | Default đề xuất vì kiểm soát cost/latency tốt hơn. |
| `SummarizeOlder` | Giữ K message mới + summary phần cũ. | Thread dài hoặc nhiều lượt qua lại. |

```jsonc
{
  "ThreadContext": {
    "Strategy": "TokenBudget",
    "MaxMessages": 10,
    "MaxTokens": 2000,
    "SummarizeThreshold": 3000
  }
}
```

#### 8.3 Intent Pre-Classification

- Default: bật auto pre-filter bằng model nhỏ/rẻ hoặc rule-based fallback.
- Mục tiêu: tránh chạy full RAG cho mention nhầm, cảm ơn, chit-chat hoặc tag bot không phải câu hỏi HR.
- Rủi ro chính: false-negative. Cần log case bị filter để review.

```jsonc
{
  "IntentPreFilter": {
    "Enabled": true,
    "Mode": "Auto"
  }
}
```

#### 8.4 Multi-Channel Readiness

- Không listen toàn workspace mặc định; channel listening bật theo cấu hình.
- Thiết kế `ChannelProfile` theo `channelId` để có persona/listen mode riêng.
- Không duplicate core RAG logic cho từng channel.

#### 8.5 Error & Timeout UX

- User không bị im lặng khi pipeline lỗi.
- Fallback message thân thiện và hướng dẫn liên hệ HR.
- Admin notification gồm timestamp, user, channel, question hash, error type, correlation id.

```jsonc
{
  "ErrorHandling": {
    "TimeoutSeconds": 20,
    "NotifyOn": ["Timeout", "PipelineError", "RateLimited"],
    "AdminNotifyChannel": "#askhr-admin-alerts"
  }
}
```

#### 8.6 Slack OAuth Scopes

| Scope | Mục đích |
|---|---|
| `app_mentions:read` | Nhận event khi bot bị mention. |
| `chat:write` | Post response. |
| `im:history`, `im:read`, `im:write` | Đọc/ghi DM. |
| `channels:history` | Đọc thread trong public channel khi bot có quyền. |
| `groups:history` | Đọc thread trong private channel khi bot được mời. |
| `users:read` | Lấy profile/email để identity mapping. |
| `reactions:write` | Loading state bằng emoji/reaction. |

Review thực tế để thu hẹp scope nếu không cần đọc channel history.

#### 8.7 Slack Identity Resolution

- Ưu tiên map `slack_user_id` qua email từ `users:read`.
- Fallback: mapping table `slack_user_id` ↔ `internal_user_id`.
- Không map được thì dùng `anonymous`.
- Cache Authorization Context với TTL ngắn theo UoB-04.

#### 8.8 P1/P2/P3 Severity Routing

| Mức | Tín hiệu | Routing |
|---|---|---|
| **P1** | Sai policy ảnh hưởng quyền lợi, expired/conflict doc, Dislike trên Benefit/Leave. | Notify ngay admin channel + email nếu có. |
| **P2** | Sai quy trình, Dislike trên Process/Onboarding, low confidence nhưng đã trả lời. | Near-real-time hoặc gộp trong giờ làm việc. |
| **P3** | No-source, out-of-scope, missing information. | Log + daily/weekly digest cho HR. |

UoB-03 route notification. Tín hiệu detection đến từ [[units-retrieval-answer#UoB-01: Answer Policy Question]] và [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

Với tín hiệu **sensitive / out-of-authority** từ UoB-01 (quấy rối, sức khỏe tâm lý, xung đột, khiếu nại), gateway thực hiện **warm handoff**: freeze auto-answer và chuyển context cho **HR Advisor** thay vì chỉ notify severity.

Escalation delivery target là config, không hardcode vào Slack gateway:

```jsonc
{
  "EscalationTargets": [
    {
      "type": "SlackChannel",
      "target": "#askhr-admin-alerts",
      "severity": ["P1", "P2"]
    },
    {
      "type": "Email",
      "target": "hr-advisor@company.com",
      "severity": ["P1"]
    },
    {
      "type": "ServiceDeskTicket",
      "target": "HR-ServiceDesk",
      "severity": ["P1", "P2", "P3"]
    }
  ]
}
```

### 9. Contracts

#### Normalized Request to RAG Pipeline

```jsonc
{
  "channelType": "Slack",
  "channelId": "C123",
  "threadId": "1700000000.000100",
  "messageId": "1700000000.000200",
  "userQuestion": "Annual leave carry over thế nào?",
  "conversationContext": "string",
  "authorizationContext": {
    "userId": "internal-user-id",
    "roles": ["Employee"],
    "allowedTags": ["public-all", "vn-policy"],
    "businessUnits": ["Vietnam"],
    "countries": ["Vietnam"],
    "legalEntities": ["VN-Legal-Entity"],
    "level": "Staff",
    "isAnonymous": false
  }
}
```

#### Severity Config

```jsonc
{
  "Severity": {
    "Enabled": true,
    "ImmediateNotifyOn": ["P1"],
    "P3Digest": {
      "Enabled": true,
      "Frequency": "Daily"
    }
  }
}
```

### 10. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]]: RAG Pipeline xử lý answer, citation, fallback.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]]: `SlackIdentityResolver` và Authorization Context.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: feedback/severity signal và audit events.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]: cấu hình admin notification, channel profile và monitoring view.

### Changelog

- Chuẩn hóa gateway boundary: Slack adapter chỉ normalize request/response, không chứa answer logic.
- Chuyển decision config sang `jsonc`.
- Làm rõ severity P1/P2/P3 là routing concern của UoB-03, còn signal đến từ UoB-01/UoB-06.
- Bỏ delivery-wave framing: listen scope (DM + `@mention`) và channel listening nêu dưới dạng capability/config.
- Thêm warm handoff cho tín hiệu sensitive/out-of-authority: freeze auto-answer + chuyển context cho HR Advisor.



## UoB-08: Web Chat Channel

> **AI-DLC Inception artifact.** UoB này bổ sung Web Chat cho AskHR, dùng chung RAG Pipeline, Authorization Context, feedback và audit contracts với Slack.

### 1. Overview

Web Chat là channel bổ sung cho nhân viên hỏi AskHR ngoài Slack. Channel này hỗ trợ streaming token-by-token, conversation history, auto-generated title, summarization history cũ, long-term memory an toàn và feedback trên từng assistant message.

Web Chat không tạo knowledge base hoặc bot logic riêng. Nó dùng chung UoB-01, UoB-04 và UoB-06.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Web chat UI, authenticated session, anonymous mode, conversation/message persistence, streaming, title generation, history summarization, feedback UI, web identity resolver, fallback UX. |
| Out of scope | RAG answer logic, RBAC engine, feedback/audit processing, Admin dashboard. |
| Explicit non-goals | Không bypass citation/grounding/security rules vì user đã login web; không lưu sensitive HR issue làm memory nếu chưa có policy rõ. |
| Security boundary | Web session phải resolve thành Authorization Context; anonymous vẫn deny-by-default. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| Authenticated User | Nhân viên login qua web. | Có conversation history, streaming và answer đúng quyền. |
| Anonymous / Guest User | Chưa login hoặc session không map được. | Chỉ thấy tài liệu `public-all`, không lộ tài liệu BU. |
| HR Admin | Cấu hình persona/tone/channel profile. | Điều chỉnh trải nghiệm web không đổi core RAG. |
| IT Support | Theo dõi lỗi web channel. | Có trace request, latency, auth mapping, browser/session info. |

### 4. Preconditions

- Web app có authentication/session handling.
- `WebIdentityResolver` map session/SSO token sang internal user hoặc `anonymous`.
- Backend có streaming transport phù hợp với Angular hosting.
- Conversation persistence tuân theo masking/retention của UoB-06.

### 5. Main Flow

1. User mở Web Chat và login qua SSO/internal auth; nếu chưa login thì dùng anonymous mode theo policy.
2. `WebIdentityResolver` gọi [[units-security-identity#UoB-04: RBAC / Identity & Access]] để tạo Authorization Context.
3. User gửi câu hỏi; UI tạo hoặc tiếp tục conversation.
4. Backend chuẩn hóa `AskHrRequest` và gọi [[units-retrieval-answer#UoB-01: Answer Policy Question]].
5. Backend stream response token-by-token nếu validation strategy cho phép.
6. Backend lưu message, citations, confidence, provider/model, token usage và masked text theo UoB-06.
7. Nếu conversation mới, hệ thống generate title ngắn.
8. Khi history dài vượt token budget, hệ thống tóm tắt phần cũ thành `ConversationSummary`.
9. User bấm Like/Dislike/comment; feedback event đi vào UoB-06.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Session hết hạn giữa streaming | Token/session invalid. | Dừng request an toàn, không tiếp tục trả nội dung, yêu cầu login lại. |
| Không resolve identity | SSO/mapping fail. | Fallback `anonymous`, chỉ tag `public-all`. |
| Conversation quá dài | History vượt token budget. | Dùng summary cũ + N messages gần nhất; không gửi raw history vô hạn. |
| Out-of-scope | UoB-01 không đủ nguồn. | Hiển thị HR contact/Service Desk link theo config. |
| Sensitive / out-of-authority | UoB-01 phát tín hiệu freeze auto-answer. | Hiển thị handoff UX, gửi context đã masked cho HR Advisor qua UoB-06/UoB-05; không tiếp tục stream answer tự động. |
| Streaming lỗi giữa chừng | Network/backend/provider issue. | Mark message `Failed`; không lưu partial answer như answer hợp lệ nếu chưa complete. |
| User xóa conversation | User muốn ẩn lịch sử UI. | Tách user-visible deletion với audit retention bắt buộc. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| Shared core | Web Chat dùng chung RAG Pipeline, citation contract và Authorization Context như Slack. |
| Streaming reliability | `Completed` và `Failed` message status được phân biệt rõ. |
| Context management | Title và summary giúp UX tốt nhưng không làm mất grounding/citation. |
| Privacy | Long-term memory không chứa sensitive HR issue hoặc dữ liệu ngoài policy. |
| Handoff safety | Sensitive handoff không lưu memory cá nhân và không expose severity nội bộ cho user. |
| Feedback loop | Mọi feedback message tạo event cho UoB-06. |

### 8. Decisions

#### 8.1 Channel Abstraction

Web Chat implement `WebChatGateway`, cùng normalized contract với Slack gateway. Core UoB-01 không biết request đến từ Slack hay Web.

#### 8.2 Conversation Persistence

- SQL Server lưu `Conversation`, `Message`, `ConversationSummary`, `FeedbackRef`.
- Message status: `Pending`, `Streaming`, `Completed`, `Failed`.
- Citations và audit metadata lưu riêng để dashboard truy xuất.

#### 8.3 Streaming

- Backend hỗ trợ SignalR hoặc SSE; chọn trong Construction dựa trên Angular integration và hosting.
- Streaming chỉ là transport UX.
- Answer chỉ mark `Completed` khi citation/grounding checks pass.
- Nếu validation cần chạy trước khi show answer, ưu tiên generate-then-stream final answer hoặc stream sau validation tùy benchmark latency.

#### 8.4 History Summarization & Long-Term Memory

- Short-term context: N messages gần nhất + summary cũ, tương tự thread context strategy của UoB-03.
- Long-term memory chỉ bật cho authenticated user.
- Chỉ lưu preference an toàn: language, office/country nếu policy cho phép, preferred tone.
- Không lưu sensitive HR issue làm memory nếu không có consent/policy rõ.

#### 8.5 Feedback UI

- Mỗi assistant message có Like, Dislike và optional comment/report issue.
- Dislike comment đi qua masking trước khi lưu.
- Feedback UI chỉ tạo event, không sửa audit metadata.

#### 8.6 Web Escalation UX

- Fallback hiển thị HR email, Slack channel HR hoặc Service Desk link theo config.
- P1/P2 severity là thông tin nội bộ; user không cần thấy severity label.
- Sensitive handoff hiển thị trạng thái đang chuyển cho HR Advisor và khóa message khỏi long-term memory.

#### 8.7 Search Transparency UX

- Trong quá trình streaming hoặc chờ kết quả RAG, Web Chat nên hiển thị trạng thái tìm kiếm thân thiện (ví dụ: `🔍 Đang tìm kiếm tài liệu với từ khóa: "chính sách thai sản"...`).
- Tuyệt đối không hiển thị tham số kỹ thuật thô (raw JSON tool call arguments) trên UI của End-User để tránh gây rối (cognitive overload).
- Toàn bộ dữ liệu tool call / search query thô phải được đẩy về [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] để lưu vết phục vụ Admin debug.

### 9. Contracts

#### AskHrRequest

```jsonc
{
  "channelType": "Web",
  "conversationId": "string",
  "messageId": "string",
  "userQuestion": "string",
  "conversationContext": "string",
  "authorizationContext": {
    "userId": "internal-user-id",
    "roles": ["Employee"],
    "allowedTags": ["public-all", "vn-policy"],
    "businessUnits": ["Vietnam"],
    "countries": ["Vietnam"],
    "legalEntities": ["VN-Legal-Entity"],
    "level": "Staff",
    "isAnonymous": false
  },
  "channelProfile": {
    "persona": "default",
    "language": "vi"
  }
}
```

#### Message Persistence

```jsonc
{
  "conversationId": "string",
  "messageId": "string",
  "role": "Assistant",
  "status": "Completed",
  "contentMasked": "string",
  "citations": [],
  "createdAt": "2026-06-06T00:00:00Z"
}
```

### 10. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]]: shared answer pipeline, grounding, citation và fallback.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]]: web session to Authorization Context.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]: cấu hình web channel profile, HR contact, conversation review.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: message/feedback/audit events.

### Changelog

- Chuẩn hóa Web Chat thành channel adapter dùng chung core RAG/authorization.
- Làm rõ streaming không thay thế validation/citation contract.
- Tách user-visible deletion khỏi audit retention.
- Bổ sung yêu cầu Search Transparency UX (§8.7) để hiển thị trạng thái tìm kiếm thân thiện và lưu vết tham số kỹ thuật.

## Changelog (Consolidation)

- 2026-06-07: Hợp nhất UoB-03 (Slack Mention & Thread Context) + UoB-08 (Web Chat Channel) vào `units-channels.md`. Mỗi UoB là một section H2, sub-section demote một cấp, per-UoB Table of Contents thay bằng "Mục lục" ở đầu file, và wikilink cập nhật sang dạng `[[file#heading]]`. Nội dung nghiệp vụ giữ nguyên.

