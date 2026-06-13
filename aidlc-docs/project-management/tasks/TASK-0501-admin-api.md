---
type: task
task: "TASK-0501"
story: "S-0501"
status: done
owner:
tags: [scrum, task, admin-portal]
related: ["[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]"]
created: 2026-06-07
updated: 2026-06-13
---

# TASK-0501: Admin Portal REST Endpoints Schema

> Story: **S-0501** · Epic: [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] · Sprint: [[sprint-10]]

## Goal

Provide an Admin control plane for HR corpus documents, permission metadata, approval state, and re-index status without developer intervention.

## Acceptance Criteria

- [x] CRUD document upload/download/delete paths exist.
- [x] Endpoint updates permission metadata: `allowedRoles`, `tagIds`, `businessUnits`, country/entity/level axes, and sensitivity.
- [x] Manual re-index is available and returns failed ingest state as `502 BadGateway`.
- [x] Admin API remains role-protected and logs business events for document operations.
- [x] Document metadata supports owner, version, effective/expired dates, BU, level, and sensitivity fields.

## Decisions

- `DocumentMetadataUpdateRequest.TagIds` is the canonical write contract.
- `DocumentListItem.Tags` remains display-only; `DocumentListItem.TagIds` drives Admin edits.
- Legacy `Tags` input is still resolved by existing tag name or GUID string for compatibility; unknown tags return `400 BadRequest`.
- Re-index operations use explicit `DocumentPermissionMetadata` snapshots so pending DB metadata is not committed before external Knowledge re-index succeeds.
- Bulk re-ingest uses `POST /api/migration/reingest` to restamp existing Knowledge entries with tag-ID permission metadata.

## Notes

- 2026-06-13: S-0501 implemented in Admin UI/API. Tags are canonicalized as `TagIds`; `Tags` remains display/legacy compatibility.
