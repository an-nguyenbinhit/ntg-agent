---
type: task
task: "TASK-0701"
story: "S-0701"
status: done
owner:
tags: [scrum, task, multi-provider]
related: ["[[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]", "[[ADR-001-agent-runtime]]"]
created: 2026-06-07
updated: 2026-06-08
---

# TASK-0701: LLM Abstraction Layer (Capability-Based Routing)

> Story: **S-0701** · Epic: [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] · Sprint: [[sprint-02]]
> Interface design để application gọi theo *capability* thay vì hardcode provider/model SDK.

## Mục tiêu

Một abstraction cho phép đổi provider/model per capability (`AnswerGeneration`, `IntentClassifier`, `GroundednessJudge`, `Embedding`) mà không đụng feature khác, có fallback an toàn và audit được.

## Acceptance Criteria

- [ ] Interface gọi theo capability, không gọi `gpt-*` / SDK provider trực tiếp.
- [ ] `ModelRoute` map capability → provider/model + fallback (contract gốc: [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] §9).
- [ ] Fallback chỉ sang provider **cùng capability, đã approve cho data class**, không vỡ citation/grounding.
- [ ] Embedding consistency: đổi embedding model/dimension ⇒ index version mới + re-index; không trộn embedding space.
- [ ] Secret qua Key Vault (SQL chỉ giữ reference); cấm raw API key trong UI; chỉ `SystemAdmin` đổi route production.

## Thiết kế kỹ thuật

- **Provider clients**: Azure OpenAI (default), OpenAI, GitHub Models, Gemini, Anthropic — khớp MAF model clients ([[ADR-001-agent-runtime]] §5).
- **ModelRoute resolution**: per skill override (xem S-0702, [[sprint-04]]) nằm trên default route.
- Định nghĩa interface + DTO trong task này; mapping nghiệp vụ giữ ở UoB-07.

## Ràng buộc cần giữ

- Provider ngoài Azure chỉ bật sau Security/Legal approval + benchmark golden-set.
- Runtime là engine, không phải trust boundary ([[ADR-001-agent-runtime]] §2).

## Notes / Decisions

- 2026-06-08: Implemented `ModelCapability`, `ModelRouteDto`, `IModelRouter`, `ModelRouter`, `IModelGateway`, and provider-backed `IChatClientFactory` using Microsoft.Extensions.AI. Default route can come from `ModelRouting` config or the published agent provider config.
