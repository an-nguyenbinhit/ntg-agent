---
type: sprint
sprint: "03"
status: planning
owner: Scrum Master
tags: [scrum, sprint]
related: ["[[product-backlog]]", "[[Home]]"]
start: 2026-07-07
end: 2026-07-18
created: 2026-06-07
updated: 2026-06-07
---

# Sprint 03 — Web Chat & Escalation/UX

> **Sprint Goal**: Mở kênh Web Chat (streaming, history) với identity resolver dùng chung Authorization Context, và hoàn thiện vòng feedback/escalation: feedback của user, phân loại severity P1/P2/P3, warm handoff chủ đề nhạy cảm và error/loading UX cho Slack. Hết sprint: user hỏi được qua web có streaming + đúng quyền; câu nhạy cảm được freeze + handoff cho HR.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-07-07 → 2026-07-18 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked
> Thứ tự To Do phản ánh phụ thuộc: web identity trước web chat; feedback/severity (dựa trên audit Sprint 02) trước warm handoff.

### To Do
- [ ] **S-0803** Web identity resolver → Authorization Context (dùng chung contract với Slack) · [[units-channels#UoB-08: Web Chat Channel]]
- [ ] **S-0801** Web chat streaming response · [[units-channels#UoB-08: Web Chat Channel]] · [[TASK-0801-web-chat-streaming]]
- [ ] **S-0802** Conversation history + gửi feedback trên web · [[units-channels#UoB-08: Web Chat Channel]]
- [ ] **S-0602** User gửi feedback (👍/👎) trên câu trả lời · [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]
- [ ] **S-0603** Phân loại severity P1/P2/P3 và routing escalation · [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]
- [ ] **S-0104** Câu nhạy cảm/vượt thẩm quyền: freeze + warm handoff cho HR · [[units-retrieval-answer#UoB-01: Answer Policy Question]] · [[TASK-0104-sensitive-handoff]]
- [ ] **S-0302** Loading state và error UX khi pipeline lỗi/timeout (Slack) · [[units-channels#UoB-03: Slack Mention & Thread Context]]

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
_(trống)_

## Status Reconciliation

- 2026-06-08: S-0803 is implemented. `WebIdentityResolver` resolves authenticated web cookie/JWT claims (`NameIdentifier`, `oid`, `sub`) or email claims (`email`, `preferred_username`, `upn`) to internal user identity, then shared `IRbacService` builds `AuthorizationContext`.
- 2026-06-08: S-0801 is in progress. `/api/answers/stream` now emits `AskHrStreamEvent` contract (`token`, `citation`, `done`, `error`, `handoff`) through the AskHR answer pipeline. Current token event is coarse after `PolicyAnswerService` completes; true token-by-token model streaming still requires extending `IModelGateway` and wiring WebClient.
- 2026-06-08: S-0801 true token streaming implemented. `IModelGateway` streams model chunks, `PolicyAnswerService` streams citation/token/done events, and WebClient consumes `/api/answers/stream` for text-only AskHR chat.
- 2026-06-08: S-0802/S-0602 implemented for AskHR web stream using existing SQL-backed `Conversation`/`ChatMessages` and existing reaction/comment endpoints. Final stream `done` returns `MessageId` so feedback buttons bind to the persisted assistant message.
- 2026-06-08: S-0603/S-0104 implemented for Sprint 03 scope with static rule-based severity and warm handoff audit packaging. Sensitive conversations are frozen by persisted handoff assistant messages; no external notification integration is included.

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-07-07 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [ ] Unit test cho web identity resolver, severity classifier, handoff packaging.
- [ ] Integration test: web chat streaming end-to-end có security trimming; warm handoff sinh đúng audit event.
- [ ] Tài liệu kỹ thuật ở `tasks/` được link và cập nhật.
- [ ] Không vi phạm ràng buộc RBAC/citation; warm handoff chỉ đóng gói context đã masked theo [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

## Sprint Review / Retro

_(điền cuối sprint)_
