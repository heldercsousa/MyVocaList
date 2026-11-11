# Bug Tracking — Routing Table

> Full detail (BACKLOG nesting example, severity definitions, task-log-entry table, bug-fix workflow): `.claude/library/bug-tracking-reference.md`. The `HARD RULE` below stays inline. `workflow.md § Bug Fix Pattern` defines how to *commit* a fix; this file defines how bugs are *tracked*.

## Non-negotiables (inline)

- **ID:** never hand-allocated. `backlog_gen.py register --kind bug` derives the next `BUG-NNN` from `max(id)` over live folders ∪ all `backlog-archive/` months, so a retired number is never reused. A collision after a cross-worktree merge is fixed with `backlog_gen.py renumber BUG-NNN`, never by hand.
- **Placement:** register BEFORE fixing. **Critical/Major** → own folder `<parent-feature>/bugs/YYYY-MM-DD-BUG-NNN-<slug>/README.md`, created by `register`. **Minor** → **no folder**; the commit message is the artifact (a `severity: Minor` folder aborts regeneration). Cross-cutting bugs nest under `BusinessFeatures/cross-cutting/` (distinct from top-level `Docs/Management/cross-cutting/`), never free-floating.
- **Severity:** **Critical** (data loss / corruption / crash / security), **Major** (core feature unusable, no workaround), **Minor** (cosmetic / edge-case / easy workaround).

### Regression-test requirement per severity `[HARD RULE]`

A fix is not complete until its regression test has been seen to FAIL before and PASS after (`testing.md § Regression tests`).

| Severity | Regression test |
|----------|-----------------|
| **Critical** | MANDATORY — failing test first (Red), then fix (Green). No exceptions. |
| **Major** | MANDATORY where testable (Service/ViewModel/Repository); UI-only → document manual E2E in task-log. |
| **Minor** | Optional — only if likely to recur or the fix is non-obvious. |

Task-log entry: Critical/Major = required (Changed files + Verification evidence); Minor = commit message only.
