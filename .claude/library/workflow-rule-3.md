# Development Workflow — Reference — Rule 3 — Commit After Every Task (full detail)

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 3 — Commit After Every Task

**Run `/sln-commit` after every task from `tasks.md` is complete.**

A session that ends with uncommitted changes is a session where progress is at risk. The `Stop` hook warns you — treat it as a hard gate, not a suggestion.

> `/sln-review` — when using `superpowers:subagent-driven-development`, review is automatic via fresh spec-compliance and code-quality subagents (the skill handles this). When executing manually (not via the skill), `/sln-review` is the trigger. Subagents do not invoke `/sln-review`.

### What counts as "task complete"
- The code builds with no errors
- Tests pass (if the task touched tested code)
- The checkbox in `tasks.md` is checked

### Task completion verification gates

Before checking the box and committing:

**1. Demo statement verification**
If the task has a demo statement, confirm it can be executed. A task whose demo statement cannot be verified is NOT complete.

**2. DI registration check**
If the task introduces a new service, repository, ViewModel, or page, confirm that it is registered in `MauiProgram.cs`. An unregistered type will compile but fail at runtime.

**3. Acceptance criteria check**
For every acceptance criterion the task was supposed to satisfy: confirm it is satisfied. Record evidence in the task-log's AC traceability matrix.

**4. Solution item registration check — BLOCKING**
For **every file created, moved, or deleted** in `Docs/` or `.claude/`: confirm the change is reflected in `MyVocaList.sln`. Do not skip this even if no other file was changed. An unregistered file is invisible in VS IDE — Helder cannot see or navigate to it. See `constraints-registry.md § Visual Studio Solution (.sln)` for the exact edit pattern (new folder, NestedProjects entry, GUID sequence).

### Session-End Spec Update Ritual

Before ending any session in which implementation occurred:

1. **Review every spec file touched this session** (`requirements.md`, `design.md`, `tasks.md`)
2. For each spec file, ask: "Does this file still accurately describe what was built?"
3. If the answer is "no" or "partially", branch on whether the feature has shipped:
   - **Not shipped** — add a `> **Spec updated [YYYY-MM-DD]:**` note in place; update ACs, signatures, or invariants to reflect delivered behavior.
   - **Shipped** — the spec is immutable history. Create `changes/YYYY-MM-DD-<slug>/` beside it with its own spec files cross-referencing the original, and `backlog_gen.py register` it. Do not edit the shipped spec.
4. **Update `tasks.md`**: check off all completed tasks; add `[CANCELLED: reason]` to tasks no longer needed
5. Commit all spec updates in the session's final commit
6. If any item's `status:`/`gate:` changed, update its `README.md` frontmatter and run `backlog_gen.py regen` — the pre-commit gate rejects a commit that leaves the rendered files stale.

**Trigger questions (ask before ending any session):**
- "Did I implement something that the spec does not describe?"
- "Did I discover a constraint that is not in the spec?"
- "Did I make a decision that future agents will need to know?"
- "Is the spec now more ambiguous than before my session?"

---
