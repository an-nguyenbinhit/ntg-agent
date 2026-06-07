---
type: task
task: "TASK-0601"
story: "S-0601"
status: todo
owner:
tags: [scrum, task, governance]
related: ["[[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]"]
created: 2026-06-07
updated: 2026-06-07
---

# TASK-0601: Audit / Feedback Event Schema

> Story: **S-0601** · Epic: [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] · Sprint: [[sprint-02]]
> Schema nền tảng cho audit trail + feedback + handoff — nhiều story phụ thuộc (S-0104, S-0602, S-0603), nên chốt sớm.

## Mục tiêu

Định nghĩa event contract chung để mọi lượt trả lời, feedback và handoff ghi nhất quán, masked đúng và truy vết được.

## Acceptance Criteria

- [ ] Audit event mỗi câu trả lời: `questionHash`, masked question, citations, `confidence`, `fallbackReason`, `auditMetadata` (provider/model/retrievalStrategy), timestamp, channel.
- [ ] Masking tối thiểu: email, phone, employee id, person name, free-text comment ([[units-retrieval-answer#UoB-01: Answer Policy Question]] §8.5).
- [ ] Feedback event (S-0602): like/dislike/comment gắn vào answer id.
- [ ] Severity tagging P1/P2/P3 (S-0603) + handoff event (S-0104) tái dùng cùng schema.
- [ ] Retention/quyền xem audit theo config thống nhất; raw text chỉ lưu khi HR/Legal approve.

## Thiết kế kỹ thuật

- Event contract gốc: [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] §9 — task này khóa JSON schema + storage shape.
- Là nguồn dữ liệu cho Analytics Dashboard (S-0503) và là output của [[TASK-0101-core-retrieval]] (`auditMetadata`).

## Ràng buộc cần giữ

- Không log PII chưa masked; hash + retention policy bắt buộc.
- Schema ổn định/versioned để story downstream không rework.

## Notes / Decisions

-
