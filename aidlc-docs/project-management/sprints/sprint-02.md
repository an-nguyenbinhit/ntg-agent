---
type: sprint
sprint: "02"
status: construction
owner: Scrum Master
tags: [scrum, sprint]
related: ["[[product-backlog]]", "[[Home]]"]
start: 2026-06-23
end: 2026-07-04
created: 2026-06-07
updated: 2026-06-08
---

# Sprint 02 — Core Trả Lời & Slack Gateway

> **Sprint Goal**: Hoàn thiện luồng trả lời policy đầu-cuối qua Slack — chuẩn hóa request, retrieve có security trimming, sinh câu trả lời có citation, fallback khi thiếu nguồn và ghi audit trail. Hết sprint: user `@mention` trong Slack nhận câu trả lời có citation (hoặc fallback đúng), mọi lượt đều được audit.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-06-23 → 2026-07-04 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked
> Thứ tự To Do phản ánh phụ thuộc: provider abstraction + gateway trước → core answer → audit → Slack end-to-end.

### To Do
- [ ] **S-0701** Abstraction layer đa provider (GitHub Models/OpenAI/Azure OpenAI/Gemini/Anthropic) · [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] · [[TASK-0701-llm-abstraction]]
- [ ] **S-0303** Slack gateway chuẩn hóa request thành `AskHrRequest` (core không phụ thuộc Slack type) · [[units-channels#UoB-03: Slack Mention & Thread Context]] · [[TASK-0303-slack-gateway]]
- [ ] **S-0101** Core retrieval policy chỉ từ corpus HR, có citation hợp lệ · [[units-retrieval-answer#UoB-01: Answer Policy Question]] · [[TASK-0101-core-retrieval]]
- [ ] **S-0102** Fallback & out-of-scope: từ chối lịch sự, chỉ tới HR contact · [[units-retrieval-answer#UoB-01: Answer Policy Question]]
- [ ] **S-0601** Audit trail mỗi câu trả lời (masked question + hash) · [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] · [[TASK-0601-audit-event-schema]]
- [ ] **S-0301** User gọi bot qua `@mention`/DM và nhận trả lời trong thread · [[units-channels#UoB-03: Slack Mention & Thread Context]]

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
_(trống)_

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-06-23 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [ ] Unit test cho logic core (retrieval, citation, fallback, provider routing).
- [ ] Integration test: Slack `@mention` → answer/fallback sinh đúng audit event.
- [ ] Tài liệu kỹ thuật ở `tasks/` được link và cập nhật.
- [ ] Không vi phạm ràng buộc RBAC/citation/deny-by-default của [[units-retrieval-answer#UoB-01: Answer Policy Question]] §8.7.

## Sprint Review / Retro

## Implementation Notes

- 2026-06-08: Implemented model routing, answer pipeline, fallback, audit logging, Slack Events adapter, and tests.
- Code-complete: S-0701, S-0101, S-0102.
- Application code added but rollout hardening remains: S-0303, S-0601, S-0301.
- Remaining Slack hardening: bot token config, Slack user identity mapping, thread read permissions, and integration verification.
- 2026-06-08: Reviewed all uncommitted Sprint 02 code (model routing, answer pipeline, audit, Slack adapter); fixed CA1822/CA1859/CA1869/CA1304/CA1311/CA1862 analyzer findings surfaced by Release build. Implemented `SlackIdentityResolver` to close the identity-mapping gap — Slack `chat.postMessage`/`api/answers` now resolve `AuthorizationContext` per user via email lookup instead of always running anonymous. Remaining Slack hardening: bot token/signing secret config and thread read permissions for production rollout.

_(điền cuối sprint)_
