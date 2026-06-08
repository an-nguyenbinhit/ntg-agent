---
type: task
task: "TASK-0101"
story: "S-0101"
status: done
owner:
tags: [scrum, task, rag]
related: ["[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]"]
created: 2026-06-07
updated: 2026-06-08
---

# TASK-0101: Core Retrieval + Citation API Contract

> Story: **S-0101** · Epic: [[units-retrieval-answer#UoB-01: Answer Policy Question]] · Sprint: [[sprint-02]]
> Định nghĩa API/contract cho luồng Standard RAG trả lời policy có citation. **Không** định nghĩa lại contract đã có — tham chiếu nguồn.

## Mục tiêu

Chốt endpoint nội bộ + I/O contract cho Standard RAG: nhận `AskHrRequest` đã normalize, áp security trimming, retrieve top-k, sinh answer có citation hoặc fallback.

## Acceptance Criteria

- [ ] Endpoint nhận `AskHrRequest` (đã normalize bởi gateway, xem [[TASK-0303-slack-gateway]]) + `AuthorizationContext`.
- [ ] Trả về đúng **Answer Contract** đã định nghĩa ở [[units-retrieval-answer#UoB-01: Answer Policy Question]] §9 (answerText, citations[], confidence, fallbackReason, auditMetadata).
- [ ] Áp similarity threshold + citation-required prompting; không cite được → set `fallbackReason`, không trả lời đoán.
- [ ] Pre-filter metadata quyền (`allowedRoles`/`tags`/`businessUnit`/`sensitivity`) **trước** search (deny-by-default).
- [ ] Sinh `auditMetadata` (provider, model, retrievalStrategy) để feed [[TASK-0601-audit-event-schema]].

## Thiết kế kỹ thuật

- **Input**: `AskHrRequest` + `AuthorizationContext` (contract gốc: [[units-security-identity#UoB-04: RBAC / Identity & Access]]).
- **Output**: Answer Contract (gốc: [[units-retrieval-answer#UoB-01: Answer Policy Question]] §9) — task này chỉ thêm HTTP shape (path, status code, error envelope), không sửa field nghiệp vụ.
- **Model call**: qua capability `AnswerGeneration` của [[TASK-0701-llm-abstraction]], không gọi SDK provider trực tiếp.
- Skill selection (progressive disclosure) chèn giữa normalize và retrieval — xem [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11.

## Ràng buộc cần giữ

- Deny-by-default + security trimming server-side ([[units-retrieval-answer#UoB-01: Answer Policy Question]] §8.7); prompt không phải security boundary.
- Citation contract không được tắt cho câu trả lời là claim policy.

## Notes / Decisions

- 2026-06-08: Added shared `AskHrRequest`/answer/citation DTOs, `PolicyAnswerService`, and `api/answers` endpoint. Pipeline searches through `IKnowledgeService`, requires citations above threshold, calls `AnswerGeneration` through `IModelGateway`, and returns fallback when grounding is missing.
