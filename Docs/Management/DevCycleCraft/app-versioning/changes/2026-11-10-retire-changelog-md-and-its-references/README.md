---
id: retire-changelog-md-and-its-references
title: Retire changelog.md and its references
status: ✅ Done
target: 2026-11-10
section: DevCycleCraft
parent: app-versioning
goal: The project changelog is archived and no guideline, command or agent asks anyone to update it.
gate: Helder reviews the amended context files.
closed: 2026-11
kind: change
---

# Retire changelog.md and its references

The project changelog is archived and no guideline, command or agent asks anyone to update it.

## Ruling

Helder, 2026-11-10: "Changelog should be retired… I don't see advantage on maintaining it anymore —
including references to it across the guidelines."

## Why (analysis from the BID session, 2026-11-09)

- It is not auto-loaded per session, so it costs no context — but nobody reads it either.
- The per-commit rule is ignored in practice: 46 entries since July, 30 of them `amend` records.
- Its only live role is the rules-amendment record (old rule / new rule / effective date), which
  duplicates the `amend:` commit body that CLAUDE.md already requires. Git history is the record.
- User-facing release notes come from `backlog-archive/` per release, not from this file.
- Not to be confused with per-feature `spec-changelog.md` (spec-writing-guide) — that stays.

## Steps (references found 2026-11-10 by `git grep -i "changelog\.md\|sln-changelog"`)

| File | Change |
|------|--------|
| `Docs/Changelog/changelog.md` | Move to `Docs/Changelog/archive/changelog-2025-2026.md` (frozen, header note "retired 2026-11-10"); update `MyVocaList.sln` (HARD GATE). |
| `CLAUDE.md` § Amending These Rules | Drop step (4); the `amend:` commit body (step 3) carries old rule / new rule / effective date. Drop `/sln-changelog` from § Commands. |
| `.claude/commands/sln-changelog.md` | Delete. |
| `.claude/commands/sln-commit.md` | Step 4 "Update changelog" and step 9 "Run changelog command" removed; step 2 "and changelog" removed; line 29 "changelog-only commits" removed. |
| `.claude/agents/orchestrator.md` | Lines ~218–229 (changelog as parallel-conflict file + post-merge entry) and ~395/426 mentions. |
| `.claude/rules/workflow.md` | "Docs land on develop" list mentions changelog. |
| `.claude/scripts/backlog/tests/test_orphan_check_widening.py` | Check whether it fixtures the changelog path; adjust if so. |
| Historical docs (`Docs/Management/**` plans, task-logs, archive) | Leave as-is — history, not guidelines. |

Context files are amended with the `amend:` prefix and reviewed by Helder before commit
(CLAUDE.md § Authorship).
