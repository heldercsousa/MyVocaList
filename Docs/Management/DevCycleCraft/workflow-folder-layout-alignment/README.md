---
id: workflow-folder-layout-alignment
title: **Workflow & Folder Layout Alignment**
status: ✅ Done
target: 2026-05
section: DevCycleCraft
goal: resolve SDD/skills/custom-rules conflicts and canonicalize the Docs/ layout.
pointer: DevCycleCraft/workflow-folder-layout-alignment/
closed: 2026-08
order: 30
kind: feature
---

# Workflow & Folder Layout Alignment

Resolve SDD/skills/custom-rules conflicts and canonicalize the Docs/ layout. Specs: `plan.md`, `findings.md`.

> **Closed ✅ Done [2026-08-03] -- delivered, then superseded.** Audited against the shipped
> state on 2026-08-03: every decision D1-D9 in `findings.md` is in force, and nothing
> actionable remains.
>
> | Decision | Where it shipped |
> |---|---|
> | D1 -- plan + task-log beside spec | `CLAUDE.md` § Docs/ Folder Layout user-preference overrides |
> | D2 -- `spec-changelog.md` as a separate file | `.claude/library/spec-writing-guide.md` § spec-changelog.md |
> | D3 / D5 -- review by a fresh subagent, `/sln-review` conditional | `CLAUDE.md` § Commands, `workflow.md` Rule 2 |
> | D6 -- slim `orchestrator.md` | `.claude/agents/orchestrator.md` |
> | D7 / D8 / D9 -- spec + plan reviewer subagents | `.claude/agents/spec-reviewer.md`, `.claude/agents/plan-reviewer.md` |
>
> **Phase 1 (the file moves) was overtaken, not skipped.** It targeted a `Docs/specs/[feature]/`
> layout; the **Spec Evolution, Versioning & Feature-Folder Organization** feature
> (`DevCycleCraft/spec-evolution-versioning/`, ✅ Done 2026-08) then moved everything to
> `Docs/Management/[section-or-filing-dir]/[feature]/` and made BACKLOG rows generated from
> folder frontmatter. Both `Docs/superpowers/` and `Docs/specs/` no longer exist on disk, so
> the Phase 1 checkboxes are unreachable by construction rather than outstanding.
>
> The Permissions Note at the end of `findings.md` was already marked superseded
> (2026-07-03). This folder is history; the canonical layout rule now lives in
> `CLAUDE.md` § Docs/ Folder Layout.
