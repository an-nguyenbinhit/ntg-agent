---
type: task
task: "TASK-0801"
story: "S-0801"
status: partial
owner:
tags: [scrum, task, web-chat]
related: ["[[units-channels#UoB-08: Web Chat Channel]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]"]
created: 2026-06-07
updated: 2026-06-07
---

# TASK-0801: Web Chat Streaming Contract (SSE / SignalR)

> Story: **S-0801** · Epic: [[units-channels#UoB-08: Web Chat Channel]] · Sprint: [[sprint-03]]
> Thiết kế transport streaming token-by-token cho Web Chat và contract message/event.

## Mục tiêu

Chốt transport (SSE vs SignalR) và định nghĩa streaming event contract để web client nhận answer token-by-token kèm citation cuối luồng, đúng Authorization Context.

## Acceptance Criteria

- [ ] So sánh + chốt **SSE vs SignalR** (tham chiếu spike Sprint-0 ở [[requirements]] §10.2).
- [ ] Event contract: `token`, `citation`, `done`, `error`, `handoff` (khi freeze theo [[TASK-0104-sensitive-handoff]]).
- [ ] Stream chỉ bắt đầu sau khi resolve `AuthorizationContext` (S-0803) — security trimming áp trước.
- [ ] Reconnect/resume + hủy stream (user rời trang) không rò rỉ context.
- [ ] Answer cuối luồng khớp Answer Contract ([[units-retrieval-answer#UoB-01: Answer Policy Question]] §9).

## Thiết kế kỹ thuật

- **Transport**: quyết định ghi vào Notes/Decisions sau spike.
- **Identity**: web identity resolver (S-0803) → `AuthorizationContext` dùng chung [[units-security-identity#UoB-04: RBAC / Identity & Access]].
- Conversation history + feedback (S-0802) tách story riêng nhưng cùng sprint.

## Ràng buộc cần giữ

- Streaming không bỏ qua citation/fallback; freeze chủ đề nhạy cảm vẫn áp ([[TASK-0104-sensitive-handoff]]).
- Security trimming server-side trước khi phát token đầu tiên.

## Notes / Decisions

- 2026-06-08: Chose HTTP JSON streaming for the first contract slice because existing WebClient already consumes `IAsyncEnumerable` over HTTP. Added `/api/answers/stream` and `AskHrStreamEvent` with `token`, `citation`, `done`, `error`, and reserved `handoff`.
- 2026-06-08: Current implementation emits a coarse `token` event after `PolicyAnswerService` completes. True token-by-token streaming is not done yet; it requires adding streaming support to `IModelGateway`/provider adapters and then updating WebClient to consume the AskHR-specific stream.
