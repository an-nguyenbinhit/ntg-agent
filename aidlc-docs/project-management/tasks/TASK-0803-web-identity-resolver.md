---
type: task
task: "TASK-0803"
story: "S-0803"
status: done
owner:
tags: [scrum, task, web-chat, identity]
related: ["[[units-channels#UoB-08: Web Chat Channel]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]"]
created: 2026-06-08
updated: 2026-06-08
---

# TASK-0803: Web Identity Resolver to Authorization Context

> Story: **S-0803** · Epic: [[units-channels#UoB-08: Web Chat Channel]] · Sprint: [[sprint-03]]

## Mục tiêu

Resolve identity từ Web request sang internal user id để dùng chung `AuthorizationContext` với Slack/API answer pipeline.

## Acceptance Criteria

- [x] Authenticated cookie/JWT request resolve được GUID claim: `NameIdentifier`, `oid`, `sub`.
- [x] Nếu GUID claim không map trực tiếp, resolver dùng email claim: `email`, `preferred_username`, `upn`.
- [x] Email claim map sang internal `User.Id`, sau đó controller gọi shared `IRbacService.ResolveAsync`.
- [x] Anonymous/unauthenticated request không giả quyền user nội bộ.
- [x] Unit test cover GUID claim, email lookup, unauthenticated request.

## Thiết kế kỹ thuật

- Implemented `WebIdentityResolver : IIdentityResolver`.
- DI now maps `IIdentityResolver` to `WebIdentityResolver`.
- `AnswersController` continues to resolve final `AuthorizationContext` through `IRbacService`, so web/API and Slack remain aligned at RBAC boundary.

## Ràng buộc cần giữ

- Không dùng prompt để enforce quyền.
- Không bypass `IRbacService`.
- Email matching hiện dựa vào SQL Server default case-insensitive collation; nếu đổi DB provider cần chuẩn hóa email persisted/indexed.

## Notes / Decisions

- 2026-06-08: Default identity contract is JWT/cookie claims. Entra ID/MSAL should provide `oid` plus `preferred_username`/`email`; cookie auth can continue using `NameIdentifier`.
