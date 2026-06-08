---
type: task
task: "TASK-0303"
story: "S-0303"
status: partial
owner:
tags: [scrum, task, slack]
related: ["[[units-channels#UoB-03: Slack Mention & Thread Context]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]"]
created: 2026-06-07
updated: 2026-06-08
---

# TASK-0303: Slack Webhook → AskHrRequest Normalization

> Story: **S-0303** · Epic: [[units-channels#UoB-03: Slack Mention & Thread Context]] · Sprint: [[sprint-02]]
> Định nghĩa schema payload Slack vào và contract `AskHrRequest` ra, để core RAG không phụ thuộc type Slack.

## Mục tiêu

Chuẩn hóa mọi event Slack (`@mention`, DM, thread reply) thành một `AskHrRequest` duy nhất + đính kèm `AuthorizationContext` đã resolve.

## Acceptance Criteria

- [ ] Map Slack Events API payload (`app_mention`, `message.im`, `conversations.replies`) → `AskHrRequest`.
- [ ] `AskHrRequest` gồm: `question`, `threadContext[]`, `channelProfile`, `locale`, và `authorizationContext`.
- [ ] Verify Slack signature + xử lý retry/dedup (event_id) trước khi đẩy vào pipeline.
- [ ] Tôn trọng Rate Limits Slack; chỉ đọc thread khi bot/user có quyền (private channel).
- [ ] Core RAG ([[TASK-0101-core-retrieval]]) nhận `AskHrRequest` mà không import type Slack.

## Thiết kế kỹ thuật

- **Transport**: Socket Mode (ưu tiên self-host) hoặc Webhook — chốt ở Planning.
- **AskHrRequest** (contract chuẩn hóa, gốc mô tả ở [[units-channels#UoB-03: Slack Mention & Thread Context]]): định nghĩa JSON schema đầy đủ trong task này.
- **AuthorizationContext**: resolve qua [[units-security-identity#UoB-04: RBAC / Identity & Access]] (Slack user → identity).
- Loading/error UX (S-0302) tách sang [[sprint-03]].

## Ràng buộc cần giữ

- Adapter chỉ chuẩn hóa request/response; không nhúng business logic RAG.
- Không đọc nội dung ngoài quyền của bot/user trong private channel.

## Notes / Decisions

- 2026-06-08: Added `api/slack/events`, Slack Events DTOs, signature verification, event dedup, mention normalization, default-agent routing, and `SlackResponseClient` for `chat.postMessage`. Remaining rollout hardening: Slack user identity mapping and thread read permissions.
- 2026-06-08: Added `SlackIdentityResolver` — maps `slack_user_id` to internal `User` via `users.info` email lookup (cached) and resolves `AuthorizationContext` through `IRbacService`, falling back to `anonymous` when the lookup fails or no internal user matches. Wired into `SlackController` so answers respect RBAC instead of always running anonymous. Remaining rollout hardening: thread read permissions and bot token/signing secret config for production workspaces.
