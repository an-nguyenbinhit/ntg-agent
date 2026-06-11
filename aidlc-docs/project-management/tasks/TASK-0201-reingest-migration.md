---
type: task
task: "TASK-0201-MIGRATION"
story: "S-0201"
status: in-progress
owner: Eng
tags: [scrum, task, ingestion, migration, rbac]
related: ["[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]", "[[sprint-06]]"]
created: 2026-06-11
updated: 2026-06-11
---

# TASK-0201-MIGRATION: Re-ingest existing documents with permission metadata

## Scope

- Provide a controlled migration endpoint for existing documents that already have `KnowledgeDocId`.
- Apply default permission metadata only when a document has no explicit roles/business units/sensitivity.
- Re-index through existing `IDocumentIngestionService` so Kernel Memory metadata stays consistent.

## Safeguards

- `ReingestMigrationOptions.DryRun` defaults to `true`.
- Production execution requires corpus/index inventory or backup export.
- Old index cleanup is delegated to the existing re-index flow after successful import; no blind delete.

## Endpoint

`POST /api/migration/reingest`

```jsonc
{
  "agentId": "optional-agent-id",
  "dryRun": false,
  "defaultRoles": ["Employee"],
  "defaultBusinessUnits": ["All"],
  "defaultSensitivityLevel": "Public"
}
```

## Verification

- Unit tests cover dry-run no-op and non-dry-run default metadata application.
- Manual production verification remains blocked until corpus/index inventory is supplied.
