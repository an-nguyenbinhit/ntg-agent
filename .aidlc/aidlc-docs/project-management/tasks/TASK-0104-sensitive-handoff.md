---
type: task
task: "TASK-0104"
story: "S-0104"
status: todo
owner:
tags: [scrum, task, escalation]
related: ["[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-channels#UoB-03: Slack Mention & Thread Context]]", "[[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]"]
created: 2026-06-07
updated: 2026-06-07
---

# TASK-0104: Sensitive Topic Freeze + Warm Handoff Contract

> Story: **S-0104** · Epic: [[units-retrieval-answer#UoB-01: Answer Policy Question]] · Sprint: [[sprint-03]]
> Đóng gói context + routing cho warm handoff chủ đề nhạy cảm/vượt thẩm quyền (cross UoB-01/03/06).

## Mục tiêu

Phát hiện chủ đề nhạy cảm → freeze auto-answer → đóng gói context đã masked và chuyển cho HR Advisor thật, thông báo user, sinh audit event.

## Acceptance Criteria

- [ ] Detection dựa trên *nội dung user chủ động nói* (quấy rối, sức khỏe tâm lý, xung đột, khiếu nại) — không profiling cảm xúc ngầm.
- [ ] **Freeze**: dừng pipeline trả lời tự động; không đưa lời khuyên legal/medical/tâm lý.
- [ ] **Handoff context**: topic, mức nhạy cảm, lịch sử hội thoại đã masked (theo [[TASK-0601-audit-event-schema]]).
- [ ] **Routing**: chuyển cho HR Advisor theo config; channel routing dùng [[units-channels#UoB-03: Slack Mention & Thread Context]].
- [ ] **User feedback**: thông báo "đang chuyển cho người phụ trách", không treo.
- [ ] Sinh handoff event cho [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

## Thiết kế kỹ thuật

- Skill `sensitive-handoff` (curated) — xem [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11.6.
- Chạy trên workflow human-in-the-loop + checkpoint của MAF ([[ADR-001-agent-runtime]] §5), nhưng RBAC/masking vẫn server-side.
- Web Chat phát event `handoff` qua streaming ([[TASK-0801-web-chat-streaming]]).

## Ràng buộc cần giữ

- Context handoff chỉ chứa dữ liệu đã masked; HR Advisor là role người thật.
- Detection ≠ tự xử lý: bot không phán xử khiếu nại (out of scope [[requirements]] §2).

## Notes / Decisions

-
