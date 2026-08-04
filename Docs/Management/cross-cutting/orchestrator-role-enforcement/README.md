---
id: orchestrator-role-enforcement
title: "**Orchestrator Role Enforcement — Root Cause Investigation**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 20
goal: "close the negative-space gap where rules forbade the orchestrator writing code but never reading source."
pointer: cross-cutting/orchestrator-role-enforcement/
---

# Orchestrator Role Enforcement — Root Cause Investigation

Root cause: negative-space omission — rules forbade the orchestrator writing code but never
reading source, and no read-scope list existed. Added a HARD RULE (MAY-read allow-list / MAY-NOT
deny-list, delegation requirement, plan-mode reconciliation, session-start self-check) plus
surgical cross-references from the workflow and project rule documents.

> Migrated from `Docs/Management/cross-cutting-log.md` (T12a Wave K, F-1a log-pointer batch 1).
> Slug and folder shape are agent-authored. Goal text is reworded from the archived Notes cell
> (verbatim text tripped the file-path-beyond-pointer banned pattern via rule-file names and a
> commit hash); meaning preserved.
