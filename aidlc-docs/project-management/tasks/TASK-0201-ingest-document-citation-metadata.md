---
type: task
task: "TASK-0201"
story: "S-0201"
status: done
owner: Eng
tags: [scrum, task, ingestion, rag, citation]
related: ["[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]", "[[sprint-01]]"]
created: 2026-06-08
updated: 2026-06-08
---

# TASK-0201: Ingest document và citation metadata

> Story: **S-0201** · Epic: [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] · Sprint: [[sprint-01]]

## Mục tiêu

Cho phép HR Admin ingest `.docx`, `.md`, `.pdf` vào Kernel Memory pipeline để extract, chunk, embed và index theo per-agent index, kèm citation metadata và permission metadata.

## Acceptance Criteria

- [x] Admin upload chỉ chấp nhận file knowledge trong scope S-0201: `.docx`, `.md`, `.pdf`.
- [x] API server-side từ chối file ngoài scope trước khi gọi Kernel Memory.
- [x] Kernel Memory ingestion được gọi với per-agent index `agent-{agentId}`.
- [x] Permission metadata từ S-0202 được stamp vào Kernel Memory tags: `allowedRoles`, `businessUnits`, `countries`, `legalEntities`, `sensitivity`, `applicableTo`.
- [x] Citation metadata tối thiểu được stamp vào Kernel Memory tags: `documentName`, `sourceType`, `sourcePath` hoặc `sourceUrl`.
- [x] Unit tests cover upload file type guard và citation tags.

## Thiết kế kỹ thuật

- UI: `AskHR.Admin.Client/Components/AddKnowledgeForm.razor` dùng accept list riêng cho knowledge ingestion.
- API: `DocumentsController.UploadDocuments` validate extension bằng `FileTypeService.IsSupportedKnowledgeFile`.
- Adapter: `KernelMemoryKnowledge` thêm citation tags trước khi gọi `IKernelMemory.ImportDocumentAsync` / `ImportWebPageAsync`.
- Kernel Memory service config đã có pipeline `extract -> partition -> gen_embeddings -> save_records`; chunking config hiện tại: `MaxTokensPerParagraph=1000`, `OverlappingTokens=100`.

## Ràng buộc cần giữ

- RBAC/security trimming vẫn enforce server-side qua `KernelMemoryKnowledge.ComposeFilters`.
- Per-agent index isolation không đổi.
- Citation tags là metadata bổ sung; downstream answer contract S-0101 vẫn phải validate citation/grounding trước khi trả lời user.
- Re-ingest tài liệu cũ là operational data migration riêng, không thể đánh dấu done nếu chưa có corpus/index mục tiêu để chạy.

## Notes / Decisions

- Không tự viết parser/chunker riêng trong Sprint 01; Kernel Memory là ingestion engine hiện tại.
- Structured Word/Markdown template enforcement sau này cần task riêng nếu HR chốt template bắt buộc.
