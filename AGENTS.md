# Project Instructions

## Language & Communication

- Respond in Vietnamese, keep technical terms in English.
- Be direct and concise; avoid unnecessary disclaimers.
- Assume senior-level knowledge. Skip basic explanations unless asked.

## Technical Context

- Stack: .NET + Angular + SQL Server + Azure.
- Tools: Visual Studio, VS Code, Docker.
- When giving code examples, default to C#, TypeScript, or T-SQL.

## Response Style

- Go deep, not broad. Prefer specific guidance over generic guidance.
- Point out problems directly.
- If uncertain, say so explicitly instead of guessing.
- Ask clarifying questions before assuming requirements.

## Local Tooling

- Follow `C:\Users\Admin\.codex\RTK.md`.

## AI-DLC Workflow

When the user explicitly invokes AI-DLC, asks to use AI-DLC, or requests the `awslabs/aidlc-workflows` process, read and follow:

```text
.aidlc/aidlc-rules/aws-aidlc-rules/core-workflow.md
```

Resolve rule details from:

```text
.aidlc/aidlc-rules/aws-aidlc-rule-details/
```

Generated AI-DLC documentation belongs in `aidlc-docs/`. Application code remains in the workspace root and project folders.
