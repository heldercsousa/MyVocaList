---
id: trim-always-loaded-context-and-enforce-process-steps-by-checks
title: Trim always-loaded context and enforce process steps by checks
status: ✅ Done
target: 2026-11-10
section: DevCycleCraft
parent: docs-context-scope-control
goal: Always-loaded rules and hook text carry only rules no hook enforces; each process step that matters has a failing check or a pointer.
gate: Helder reviews the amend diff; the three checks pass their tests.
closed: 2026-11
kind: change
---

# Trim always-loaded context and enforce process steps by checks

Always-loaded rules and hook text carry only rules no hook enforces; each process step that matters has a failing check or a pointer.

## Result (2026-11-10)

Always-loaded rules text 12,696 -> 10,019 tokens (est. bytes/2.6); per-prompt, Stop and PostCompact hook text cut by 50-90%. Rules removed or moved: duplicates collapsed to one home (read-scope -> CLAUDE.md Roles; SDD invariant -> CLAUDE.md Methodology; ITF conditions -> `workflow-rule-2.md`; BACKLOG generated -> one bullet), footers and header repeats deleted, routing tables folded. No `[HARD RULE]` / `[Unamendable]` item removed.

## Gap table - does anything fail if the step is skipped?

| Step | Enforced today | Proposed check | Status |
|------|----------------|----------------|--------|
| Code edit off a task branch; native dialogs; BACKLOG hand-edit | Guards 1/2, protect-generated, `regen --check` | none | enforced |
| `.sln` registration of moved/deleted `Docs/` files | auto-register on `Write` only | pre-commit check on staged `Docs/` adds, moves, deletes | done 2026-11-10 (`process_checks.py`, pre-commit) |
| `SafeAreaEdges="Container"`, or `"All"` on pages with text input (ruling 2026-11-10; waiver list dropped) | nothing | pre-commit check on staged `.xaml` | done 2026-11-10 (`process_checks.py`, pre-commit) |
| `To Review` task-log entry without `### Changed files` | nothing | pre-commit check on staged `task-log.md` | done 2026-11-10 (`process_checks.py`, pre-commit) |
| `tasks.md` box checked at commit | nothing | advisory in Stop hook | proposed |
| LEDGER row per live task branch | row length only | advisory: `git worktree list` branches without a row | proposed |
| `// [AC] REQ-` comment on tests | nothing | advisory scan of changed test files | proposed |
| Governed-component edit (4 gates) | nothing | block governed-list files unless an approval marker exists | proposed |
| No C#-side string normalization | nothing | advisory grep (false positives likely) | proposed |
| **Unregistered work** (a commit of work with no BACKLOG item) | nothing | `commit-msg` hook: `Item: <backlog-id>` trailer required on feat/refactor/perf/amend, exempt docs/chore/merge/fix (Helder ruling 2026-11-10) | done 2026-11-10 |
