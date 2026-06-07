---
type: adr
adr: "001"
status: accepted
owner: Eng / IT
tags: [architecture, agent-runtime, orchestration, nfr, multi-provider]
related: ["[[requirements]]", "[[units-retrieval-answer#UoB-01: Answer Policy Question]]", "[[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]", "[[units-channels#UoB-03: Slack Mention & Thread Context]]"]
created: 2026-06-07
updated: 2026-06-07
---

# ADR-001: Agent Runtime — Microsoft Agent Framework vs Build-Your-Own

> **AI-DLC artifact (Construction / NFR decision).** ADR này chốt *engine* điều phối agent cho AskHR. Đây là quyết định tech-stack, **không** thêm requirement nghiệp vụ mới và **không** mở scope sang transactional agent.

## Table of Contents

- [1. Context](#1-context)
- [2. Decision](#2-decision)
- [3. Scope Boundary (quan trọng)](#3-scope-boundary-quan-trọng)
- [4. Options Considered](#4-options-considered)
- [5. Capability Mapping](#5-capability-mapping)
- [6. Risks & Mitigations](#6-risks--mitigations)
- [7. Consequences](#7-consequences)
- [8. Construction Follow-ups](#8-construction-follow-ups)

## 1. Context

AskHR đằng nào cũng phải xây các năng lực runtime sau (đã là requirement, không phải mới):

- **Session/state**: lưu `conversation`/`message`, long-term memory cho user authenticated ([[requirements]] §1).
- **Context compaction**: tự tóm tắt history cũ để giảm token ([[requirements]] §1).
- **Orchestration nhiều bước**: Agentic RAG planner chain skill, Agent Skills config plane ([[requirements]] §9; [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11).
- **Human-in-the-loop**: warm handoff chủ đề nhạy cảm + Approval Workflow ([[requirements]] §8, §10).
- **Multi-provider**: Azure OpenAI default, hỗ trợ OpenAI/GitHub Models/Gemini/Anthropic ([[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]]).

Câu hỏi: tự build plumbing này, hay đứng trên một agent runtime có sẵn?

Microsoft Agent Framework (MAF) là kế thừa hợp nhất của **AutoGen** (multi-agent) + **Semantic Kernel** (enterprise: session state, middleware, telemetry), bổ sung **graph workflows** (type-safe routing, checkpointing, human-in-the-loop). MAF support provider Azure OpenAI / OpenAI / Anthropic / Ollama / Foundry — khớp định hướng Azure-first nhưng không khóa provider.

## 2. Decision

**Adopt MAF làm runtime substrate** cho orchestration + session state + multi-agent RAG, với hai ràng buộc cứng:

1. MAF là **engine**, không phải trust boundary. RBAC/security trimming và citation enforcement vẫn enforce **server-side, deny-by-default** — middleware MAF chỉ là nơi *gọi* chúng, không thay thế ([[units-retrieval-answer#UoB-01: Answer Policy Question]] §11: skill/prompt không được làm yếu RBAC/citation).
2. Không phụ thuộc tính năng **experimental** của MAF (xem §6) như guarantee production.

Lựa chọn này áp dụng được cho cả Semantic Kernel hoặc LangGraph nếu sau này đổi (xem §4); ADR mặc định MAF vì khớp Azure-first + đã gộp AutoGen multi-agent.

## 3. Scope Boundary (quan trọng)

ADR này **không** biện minh cho việc biến AskHR thành transactional agent. Các ví dụ "gọi API bảng lương", "nộp đơn nghỉ", "phê duyệt nghỉ việc không lương" nằm **ngoài scope hiện tại**:

- Corpus không index Salary/PII → không có payroll API ([[requirements]] §4).
- Bot không ra quyết định thay HR ([[requirements]] §7).
- Chủ đề nhạy cảm → freeze + warm handoff cho HR Advisor, không tự xử lý ([[requirements]] §8).

MAF có khả năng tool-calling/write-action, nhưng AskHR chỉ dùng cho **read-only retrieval orchestration + handoff**. Mở sang write-action là **quyết định nghiệp vụ của HR**, phải qua ADR/scope-change riêng — không bật ngầm qua framework.

## 4. Options Considered

| Option | Ưu | Nhược | Đánh giá |
|---|---|---|---|
| **A. MAF (chọn)** | Gộp AutoGen + SK; session state, middleware, workflow HITL, checkpointing, multi-provider sẵn; Azure-first | Mới, một số phần experimental; non-Azure model "at your own risk" | **Chọn** — khớp stack, giảm plumbing |
| B. Semantic Kernel thuần | Ổn định hơn MAF, cùng hệ MS | Multi-agent orchestration yếu hơn; MS định hướng kế thừa bằng MAF | Fallback nếu MAF churn |
| C. LangGraph / framework khác | Graph orchestration chín, cộng đồng lớn | Lệch khỏi Azure-first; phải tự lo tích hợp Foundry/SK feature | Cân nhắc nếu rời hệ MS |
| D. Build-your-own | Kiểm soát tối đa, không phụ thuộc | Tự viết session/compaction/HITL/checkpoint — tốn, dễ lỗi | Loại — tái phát minh bánh xe |

## 5. Capability Mapping

| Requirement AskHR | Thành phần MAF | Ghi chú |
|---|---|---|
| Session/state + long-term memory | `AgentSession`, Context Providers, Workflow checkpointing | Map [[requirements]] §1 |
| Context compaction | Compaction strategies (Summarization/SlidingWindow/ToolResult/Truncation) | **Experimental** — xem §6 |
| RBAC trimming + citation enforcement | Middleware / Context Provider (before/after hook) | Chỉ là điểm gọi; trust boundary vẫn server-side |
| Agentic RAG planner chain skill | Graph Workflows (type-safe routing) | Map [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11 |
| Warm handoff + Approval Workflow | Workflow human-in-the-loop + checkpoint | Map [[requirements]] §8, §10; routing [[units-channels#UoB-03: Slack Mention & Thread Context]] |
| Multi-provider/model | Model clients (Azure OpenAI/OpenAI/Anthropic/Ollama/Foundry) | Map [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] |

## 6. Risks & Mitigations

| Rủi ro | Giảm thiểu |
|---|---|
| **Compaction experimental** (C# cần `#pragma MAAI001`, Python `agent_framework._compaction`) | Tự giữ summarization (đã là requirement §1) làm xương sống; MAF compaction là bonus, không phải guarantee |
| **Framework churn** (MAF còn mới) | Giữ application logic tách khỏi MAF qua port/adapter; Semantic Kernel là fallback (Option B) |
| **Compliance / data flow** — MAF ghi rõ non-Azure model "at your own risk", trách nhiệm data boundary thuộc về mình | Default Azure OpenAI; provider ngoài Azure phải qua review governance ([[requirements]] §3, §13); masking/retention giữ nguyên |
| **Harness phá trust boundary** | Bất biến: RBAC deny-by-default + citation enforce server-side; middleware không phải security boundary |
| **Scope creep sang transactional** | §3 — write-action cần ADR/scope-change riêng của HR |

## 7. Consequences

- AskHR dùng **.NET** runtime cho MAF để khớp target stack ASP.NET Core / Azure / SQL Server; Python chỉ là fallback spike nếu một capability MAF bắt buộc chưa có parity trong .NET.
- Giảm khối lượng tự viết cho session/state, HITL, multi-agent routing.
- Tạo điểm tích hợp sạch (middleware/context provider) để cắm RBAC trimming, citation, guardrail.
- Phải pin version MAF và theo dõi promote của các API experimental.

## 8. Construction Follow-ups

| Item | Cần chốt khi |
|---|---|
| Dùng Foundry Agents (service-managed context) hay self-managed (cần compaction)? | NFR/Infrastructure design vì ảnh hưởng trực tiếp tới mục compaction §6 |
| Pin version MAF nào làm baseline? | Trước Code Generation |
| Benchmark MAF graph workflow + streaming + HITL trên golden scenario | Sprint-0 technical spike |
