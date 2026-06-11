---
type: sprint
sprint: "09"
status: planned
owner: Scrum Master
tags: [scrum, sprint, rbac, security, teams]
related: ["[[product-backlog]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]", "[[TASK-0901-ms-teams-channel]]"]
start: 2026-06-11
end: 2026-06-24
created: 2026-06-11
updated: 2026-06-11
---

# Sprint 09 - RBAC Hardening & Operational Closure

> **Sprint Goal**: Khong rebuild P0 foundation da co. Sprint nay dong cac gap con lai quanh RBAC verification, operational docs, va MS Teams verification blocker.

## Scope Decisions

- `AuthorizationContext`, `DocumentPermissionMetadata`, `RbacService`, Admin upload permission metadata, va Kernel Memory deny-by-default filters da duoc implement tu Sprint 01.
- Khong tao schema/interface duplicate trong `AskHR.Common`.
- Khong hard-code admin mapping moi trong code. Tiep tuc dung DB-backed role/tag mapping va `Authorization:Mock:*` chi la dev shim.
- `AskHR.Knowledge` hien la Kernel Memory service wrapper; security trimming dang enforce trong Orchestrator adapter truoc khi goi Kernel Memory. Neu Knowledge API can thanh trust boundary doc lap, can story rieng de them endpoint co `AuthorizationContext` va reject unfiltered search.

## Sprint Backlog

### Done

- [x] **RBAC test hardening**: Them unit tests cho `RbacService` anonymous fallback, no-role fallback, DB role/tag/profile resolution, va mock profile axes.

### In Progress

- [/] **Security-trimmed retrieval verification**: Duy tri `KernelMemoryKnowledgeTests` cho deny-by-default, anonymous public-only, sensitivity cap, per-agent index isolation.
- [/] **Backlog/docs sync**: Product backlog dang stale so voi code/audit; can sync trang thai P0 da done va Sprint hien tai.

### Blocked

- [-] **S-0901 MS Teams manual verification**: Code gateway/resolver/card formatter da co test. Manual verification can Azure Bot Service + Teams app registration/ngrok endpoint.
- [-] **Historical corpus re-ingest**: Can corpus/index inventory hoac backup export de re-ingest tai lieu cu voi permission metadata moi.

## Verification

- Automated: `dotnet test tests/AskHR.Orchestrator.Tests/AskHR.Orchestrator.Tests.csproj --no-restore --artifacts-path .artifacts/test-rbac --filter "FullyQualifiedName~RbacServiceTests|FullyQualifiedName~KernelMemoryKnowledgeTests"`.
- Manual: tao 2 document test `public-all/Public` va `vn-policy/Internal`; anonymous chi thay public, HR VN thay private neu co role/tag/BU phu hop.

## Notes

- Sprint 09 uu tien hardening va closure vi P0 foundation da nam trong codebase.
- Neu yeu cau audit "so chunk bi filter", Kernel Memory hien khong tra pre-filter candidate count; can instrumentation rieng thay vi ghi nhan gia dinh.
