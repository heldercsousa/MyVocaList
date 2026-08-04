# Bug Tracking — Routing Table

> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.
> **This file is a routing table.** Full detail (BACKLOG nesting example, severity definitions, task-log-entry table, bug-fix workflow) lives in `.claude/library/bug-tracking-reference.md`, loaded on demand via the `myvocalist-coding` skill map. The `HARD RULE` below stays inline — it must never be missed.

`workflow.md § Bug Fix Pattern` defines how to *commit* a bug fix. This file defines how bugs are *tracked*: ID, BACKLOG placement, severity, and the regression test each class requires.

## Non-negotiables (inline)

- **ID:** never hand-allocated. `backlog_gen.py register --kind bug` derives the next `BUG-NNN` from `max(id)` over live folders **∪ all `backlog-archive/` months**, so a retired number can never be reused. Used in commit subject + generated row + task-log. A collision after a cross-worktree merge is fixed with `backlog_gen.py renumber BUG-NNN`, never by hand.
- **Placement:** register BEFORE fixing (proactive triage). **Critical/Major** → its own folder `<parent-feature>/bugs/YYYY-MM-DD-BUG-NNN-<slug>/README.md`, created by `register` (the folder name is derived, never typed). **Minor** → **no folder**; the commit message is the artifact. A `severity: Minor` folder is a validation error that aborts regeneration. Cross-cutting bugs nest under `BusinessFeatures/cross-cutting/` (distinct from the top-level `Docs/Management/cross-cutting/` filing directory — do not confuse the two), never free-floating.
- **Severity:** **Critical** (data loss / corruption / crash / security), **Major** (core feature unusable, no workaround), **Minor** (cosmetic / edge-case / easy workaround).

### Regression-test requirement per severity `[HARD RULE]`

A fix is not complete until its regression test has been seen to FAIL before and PASS after (`testing.md § Regression tests`).

| Severity | Regression test |
|----------|-----------------|
| **Critical** | MANDATORY — failing test first (Red), then fix (Green). No exceptions. |
| **Major** | MANDATORY where testable (Service/ViewModel/Repository); UI-only → document manual E2E in task-log. |
| **Minor** | Optional — only if likely to recur or the fix is non-obvious. |

### Task-log entry: Critical/Major = required (Changed files + Verification evidence); Minor = commit message only.

| Need | Source |
|------|--------|
| BACKLOG nesting example, severity definitions + examples, task-log-entry table, full bug-fix workflow | `.claude/library/bug-tracking-reference.md` |

> **Authorship note:** Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Approval is provisional: these definitions are hooked to the current feature-development/spec-update approach and MUST be revisited when the **Spec Evolution, Versioning & Feature-Folder Organization** feature (BACKLOG 2026-07-09) lands (e.g. bug-fix→spec-version binding). Full content preserved in `library/bug-tracking-reference.md`.
