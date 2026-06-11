---
type: sprint
sprint: "05"
status: done
owner: Scrum Master
tags: [scrum, sprint, web-chat]
related: ["[[product-backlog]]", "[[Home]]"]
start: 2026-08-04
end: 2026-08-15
created: 2026-06-10
updated: 2026-06-10
---

# Sprint 05 — Web Chat Channel & End-User Experience

> **Sprint Goal**: Xây dựng Web Chat Channel cho end-user, tích hợp với Answer Pipeline. Hỗ trợ realtime streaming (SignalR), hiển thị lịch sử hội thoại, feedback (like/dislike), và resolve identity từ phiên đăng nhập web sang Authorization Context. Mở rộng khả năng truy cập AskHR ra ngoài Slack.

## Thông tin

| | |
|---|---|
| Khoảng thời gian | 2026-08-04 → 2026-08-15 (2 tuần) |
| Capacity | _điền sau Planning_ |
| Committed points | _điền sau Planning_ |

## Sprint Backlog (Kanban)

> `[ ]` To Do · `[/]` In Progress · `[x]` Done · `[-]` Blocked

### To Do
_(trống)_

### In Progress
_(trống)_

### Blocked
_(trống)_

### Done
- [x] **S-0801** Là User, chat qua web với streaming response (SignalR/SSE) · [[units-channels#UoB-08: Web Chat Channel]]
- [x] **S-0802** Là User, xem conversation history và gửi feedback trên web · [[units-channels#UoB-08: Web Chat Channel]]
- [x] **S-0803** Là hệ thống, web identity resolver → Authorization Context · [[units-channels#UoB-08: Web Chat Channel]]

## Status Reconciliation
- 2026-06-10: Sprint 05 vừa được kick-off sau khi hoàn tất Sprint 04. Đang trong quá trình planning kiến trúc.

## Daily Log

| Ngày | Cập nhật | Blocker |
|---|---|---|
| 2026-08-04 | _Sprint planning_ | |

## Definition of Done

- [ ] Code review pass.
- [ ] Unit tests cho Web Chat Gateway, Identity Resolver.
- [ ] Integration test/E2E test đảm bảo Web Chat gọi được Pipeline với Authorization Context hợp lệ và stream answer đúng format.
- [ ] UI Angular components responsive, hiển thị Search Transparency UX rõ ràng.
- [ ] Tài liệu kỹ thuật cập nhật nếu có API mới (SignalR Hub).

## Sprint Review / Retro

_(điền cuối sprint)_
