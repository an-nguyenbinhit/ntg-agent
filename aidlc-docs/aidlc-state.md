# AI-DLC State Tracking

## Project Information

- **Project**: AskHR / AskHR
- **Project Type**: Brownfield application với AI-DLC requirements baseline
- **Start Date**: 2026-06-06T00:00:00Z
- **Current Phase**: CONSTRUCTION
- **Current Stage**: Sprint 04 - Admin Portal & Cấu Hình

## Workspace State

- **Existing Application Code**: Yes
- **Primary Artifacts**: `aidlc-docs/requirements.md`, `aidlc-docs/units-retrieval-answer.md`, `aidlc-docs/units-security-identity.md`, `aidlc-docs/units-channels.md`, `aidlc-docs/units-governance-ops.md`, `aidlc-docs/ADR-001-agent-runtime.md`, `aidlc-docs/project-management/product-backlog.md`, `aidlc-docs/project-management/sprints/sprint-01.md`
- **Workspace Root**: `D:\Projects\ntg-agent`
- **Documentation Directory**: `aidlc-docs/`

## Extension Configuration

| Extension | Enabled | Decided At |
|---|---|---|
| None detected | N/A | Requirements Scrum-Readiness Review |

## Stage Progress

| Stage | Status | Notes |
|---|---|---|
| Workspace Detection | Complete | Đã detect AskHR là ứng dụng .NET brownfield; AI-DLC docs định nghĩa baseline requirement trong repo này. |
| Requirements Analysis | Complete | Requirement source và UoB decomposition đã có; kickoff decisions được track trong `requirements.md` sections 3 và 10. |
| User Stories | Complete | Product backlog đã map UoB epics sang stories S-0101..S-0803 trong `project-management/product-backlog.md`. |
| Workflow Planning | Complete | Sprint sequence và Sprint 01-04 đã có trong `project-management/sprints/`. |
| Application Design | Drafted | Domain/service boundaries được thể hiện qua UoB docs, ADR-001, `IKnowledgeService`, `AuthorizationContext`, RBAC và Kernel Memory adapter contracts hiện có. |
| Units Generation | Complete | UoB-01 đến UoB-08 đã có và đã map vào Scrum execution artifacts. |
| Code Generation | In Progress | Sprint 01-03 đã code-complete. Sprint 04 chuẩn bị bắt đầu với S-0501 Admin Portal API. |
| Build and Test | In Progress | Orchestrator unit tests là verification target hiện tại. |

## Current Recommendation

### Current Recommendation Override - 2026-06-08

1. Sprint 03 (Web Chat & Escalation) đã hoàn thành toàn bộ To-Do và test integration cơ bản.
2. S-0201 production re-ingest remains blocked until corpus/index inventory is available; do not use old default index for production rollout.
3. Current Sprint 04 focus: Triển khai REST Endpoints cho Admin Portal (S-0501) để quản lý tài liệu, phân quyền (RBAC metadata), theo dõi quá trình re-ingest và trạng thái document. Mở đầu bằng việc thiết kế cấu trúc API cho Knowledge management.

Sprint 02 đã code-complete cho answer pipeline/model routing/audit logging/Slack gateway. Trạng thái tiếp theo:

1. Re-ingest các document đã được index trước khi permission metadata được bổ sung.
2. Đảm bảo mỗi document re-ingested có `allowedRoles`, `businessUnits` và `sensitivity` metadata.
3. Treat missing metadata as deny-by-default; không dùng document từ default index cũ cho Sprint 02 answer flows.

Trọng tâm engineering sau re-ingest:

1. UoB-01 Answer Policy Question: S-0101, S-0102.
2. UoB-03 Slack Channel: S-0301, S-0303.
3. UoB-06 Audit / Analytics: S-0601.
4. UoB-07 Multi-Provider / Model Configuration: S-0701.

Sprint-0 business inputs vẫn cần owner xác nhận rõ trước khi rollout rộng Sprint 02: HR fallback contacts, document template approval, admin roles, retention approval, RAG latency benchmark và MAF baseline version.
