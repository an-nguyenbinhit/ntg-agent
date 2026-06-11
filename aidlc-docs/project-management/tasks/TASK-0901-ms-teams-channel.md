---
type: task
task: "TASK-0901"
story: "S-0901"
status: blocked
owner: Eng
tags: [scrum, task, teams, bot-framework, rbac]
related: ["[[units-channels#UoB-09: MS Teams Channel]]", "[[sprint-06]]"]
created: 2026-06-11
updated: 2026-06-11
---

# TASK-0901: MS Teams Channel

## Scope

- Add Bot Framework-compatible webhook endpoint at `POST /api/messages`.
- Normalize Teams activity into `AskHrRequest` with `Channel = "teams"`.
- Resolve Teams user identity through `aadObjectId` mapping, then `userPrincipalName`/email fallback.
- Return answer using Adaptive Card with citations and feedback actions.

## Implementation

- `AskHR.Orchestrator/Channels/Teams/TeamsIdentityResolver.cs`
- `AskHR.Orchestrator/Channels/Teams/TeamsAdaptiveCardFormatter.cs`
- `AskHR.Orchestrator/Channels/Teams/TeamsResponseClient.cs`
- `AskHR.Orchestrator/Controllers/TeamsGatewayController.cs`

## Verification

- Unit tests cover identity fallback, card mapping, mention normalization, duplicate activity handling, and background pipeline invocation.
- Manual Teams client verification is blocked until Azure Bot Service and Teams app registration are available.

## Status

- Code path is implemented and covered by local unit tests.
- Remaining work is operational verification against Azure Bot Service and a registered Teams app; no additional application code is planned before that environment exists.
