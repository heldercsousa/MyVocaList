# Development Workflow

> These rules are enforced by hooks. Violating them costs rework. Follow them exactly.

---

## SDD Invariant

> **Spec changes before code changes.**

This is the single invariant that governs all development in MyVocaList. It cannot be overridden by time pressure, perceived simplicity, or subagent autonomy.

- If a new requirement arises during implementation, update the spec first — then update the code.
- If code contradicts the spec, the code is wrong — the spec is not wrong.
- If the spec is incomplete, stop and clarify with Helder — do not improvise.
- A subagent that modifies behavior not described in the spec has violated this invariant, regardless of whether the change "makes sense."

This invariant applies to all agents (main and sub) at all times.

---

## Rule 1 — Spec-First

**Before writing any implementation code for a feature, read `Docs/specs/[feature]/design.md`.**

No exceptions. Code written without reading the spec is code that may contradict it.

### Spec structure (copy from `Docs/specs/venues/`)
| File | What it answers |
|------|----------------|
| `requirements.md` | User stories, acceptance criteria, validation rules, out-of-scope |
| `design.md` | Architecture, interfaces, page structure, interaction flows, key decisions |
| `tasks.md` | Ordered checkboxed tasks — check off as each completes |

#### requirements.md — mandatory sections
- **User stories** — "As a [role], I want [action] so that [value]"
- **Acceptance criteria** — one per user story (see Given/When/Then format below)
- **Validation rules** — field constraints, business invariants
- **Out of Scope** — explicit list of what this feature does NOT do; prevents scope creep during implementation
- **Domain Vocabulary** — define every domain term used in the spec (e.g. "Round", "Queue Entry", "Absence"). All stakeholders and agents must use these exact terms — no synonyms.

#### design.md — mandatory sections
- **Architecture** — which layers are affected, how they interact
- **Interfaces** — new or modified service/repository interfaces with signatures
- **Page structure** — screens, navigation flows
- **Interaction flows** — sequence of user actions and system responses
- **Invariants & Postconditions** — system invariants that must hold after every operation (e.g. "Queue always has at least one active singer", "Round number is monotonically increasing")
- **Key Decisions** — see Key Decisions section below

### Spec-update gate — after implementation

When a subagent's work reveals a discrepancy between the spec and the delivered code (even a "minor" one), the following must happen before the task is marked `To Review`:

1. Update `requirements.md` or `design.md` to reflect what was actually built.
2. Note the change in the task-log as `Spec updated — re-planning required` if it affects subsequent tasks.
3. Never leave the spec stale at the end of a task. A stale spec is technical debt that compounds with every subsequent wave.

> **Staleness prevention:** Every implementation task must end with a brief spec-review question: "Does the spec still accurately describe what was built?" If the answer is no, fix the spec before committing.

### New feature workflow
1. **Brainstorm** — invoke `superpowers:brainstorming`
2. **Write spec** — write all three files; user reviews and approves
3. **Write plan** — invoke `superpowers:writing-plans`
4. **Implement** — delegate to a subagent (see Rule 2)
5. **Phase-gate review** — invoke `/project:review` after each phase before starting the next
   - After spec writing: review spec for completeness before writing the plan
   - After plan writing: review plan for coherence before dispatching subagents
   - After each implementation wave: review output before dispatching the next wave
   - At feature close-out: final review to confirm spec matches delivered behavior

### SDD decision table for medium-complexity tasks

For tasks that don't fit cleanly into "small isolated" or "new feature," use this decision table:

| Signal | SDD action |
|--------|-----------|
| Change touches ≥ 2 layers (e.g. Domain + UI) | Write `design.md` before starting |
| Change introduces a new repository interface | Write `design.md` + update `requirements.md` |
| Change affects an existing public contract (DTO, interface signature) | Write `design.md`; flag downstream consumers in `tasks.md` |
| Change is reversible and affects only one file | Commit message spec is sufficient |
| You find yourself asking "where should this logic live?" | Stop — write a `design.md` |
| Estimated time > 2 hours | Full three-file spec required |

When uncertain: start with a two-sentence design note in the task-log. If it grows beyond 5 lines, promote it to `design.md`.

### When to skip SDD (spec bypass rule)

Not every change requires a full three-file spec. Use this table:

| Task type | Spec required? | Minimum artifact |
|-----------|---------------|-----------------|
| New feature (any complexity) | Yes | All three files: `requirements.md`, `design.md`, `tasks.md` |
| Non-trivial refactor (cross-layer, affects interfaces) | Yes | `design.md` + `tasks.md` |
| Small isolated change (< 1 hour, single file, no interface change) | No | Descriptive commit message |
| Bug fix | No | Commit message as spec (see Bug Fix Pattern) |
| Docs/rules/config update | No | Commit message |
| Spike / discovery work | No | `findings.md` artifact (see Discovery Mode) |

**Rule:** When in doubt, write a spec. A 10-minute spec prevents a 2-hour rewrite.

**Spec bypass guard:** Even when skipping a full spec, the SDD Invariant still applies. "No spec" does not mean "no constraints" — it means the commit message, the task description, or a brief inline note serves as the specification.

---

## Rule 2 — Subagent Delegation

**All coding is done by subagents. The main agent handles shell-only steps.**

| Main agent does | Subagent does |
|----------------|---------------|
| `dotnet build` | Any file creation or edit |
| `dotnet test` | ViewModels, pages, services, repositories |
| `dotnet ef migrations add` | XAML, code-behind, DI registration |
| `git status`, `git add`, `git commit` | Route additions, AppShell registration |
| Reading spec before briefing subagent | Everything in `crud-pages.md` |

### Wave-based parallelism — hard cap
- **Maximum 4 subagents may run in parallel at any one time.**
- Work is dispatched in waves: spawn up to 4 subagents, wait for all to complete, then start the next wave.
- Never spawn a 5th concurrent subagent — stagger instead.
- After a subagent completes, its context is discarded. Do not reuse the same subagent instance for a second task.

### Briefing protocol — paths only, never paste content
- Subagent briefings must reference **file paths**, not paste file content inline.
- Tell the subagent which files to read; let its own `Read` calls bring the content into its context.
- Pasting rule file content into a briefing multiplies token cost by the number of subagents — never do it.
- Pre-read the spec yourself and hand the subagent concrete, scoped instructions (not "based on what you find").

### Subagent return protocol — status signal only
Subagents communicate completion **only** by:
1. Updating the task-log beside the plan file (see Rule 5) with the task status:
   - `To Review` — build passed; task ready for review
   - `Build failure` — build failed after 3 attempts; one-line reason appended
   - `blocked: spec gap` — spec ambiguity found; question + options + recommendation documented; agent stops and does NOT choose unilaterally
2. Committing and pushing all changes (`git push origin HEAD`)
3. Stopping (exiting their session)

Subagents must **not** return summaries, explanations, or diffs to the caller.
The caller reads the task-log if it needs outcome details — never the subagent's session context.

### How to brief a subagent
Give it: the spec file paths, the tasks to complete, the rules files to read (paths only), and the
constraint that it must build and fix errors before returning.

### When to take back control
- After the subagent returns: run `dotnet build` and `dotnet test` as main agent
- If a shell command is needed mid-way (migrations, file moves): do it inline, then re-delegate

### Subagent exit checklist (mandatory before returning)
Every subagent must, in this order:
1. Invoke `superpowers:verification-before-completion` — catches non-negotiable violations
2. Build (0 errors)
3. Commit changed files
4. Push (`git push origin HEAD`)

The `Stop` hook warns if uncommitted changes remain.

---

## Rule 3 — Commit After Every Task

**Run `/project:commit` after every task from `tasks.md` is complete.**

A session that ends with uncommitted changes is a session where progress is at risk.
The `Stop` hook warns you — treat it as a hard gate, not a suggestion.

### What counts as "task complete"
- The code builds with no errors
- Tests pass (if the task touched tested code)
- The checkbox in `tasks.md` is checked

---

## Rule 4 — Tasks.md Is the Source of Truth

Check off each task in `Docs/specs/[feature]/tasks.md` as it completes.
The task list is the audit trail for the feature — keep it accurate.

**Sequential constraint:** Never start a task that depends on the output of an incomplete task. Tasks marked `[SEQUENTIAL]` in tasks.md must wait for their predecessor to be committed before starting.

**Parallel exception:** Tasks marked `[P]` (independent, different files/layers) may be dispatched simultaneously as a wave per Rule 2. All tasks in a wave must complete and commit before the next wave begins.

---

## Rule 6 — Research Tool Gate (Context7 → Exa → WebSearch)

Before any web research query, follow this three-tier hierarchy:

1. **Library / framework / SDK / API docs** → Context7 (`mcp__context7__resolve-library-id` → `mcp__context7__query-docs`)
2. **General web research** (comparisons, news, tool evaluations, articles, anything non-library) → Exa MCP (`exa_search`)
3. **Raw `WebSearch` / `WebFetch`** → last-resort fallback only when both Context7 and Exa return no useful result

This applies to **both the main agent and all subagents.**
Reason: `WebFetch` pulls 5,000–15,000 tokens of raw HTML per page; Context7 and Exa return structured results at a fraction of that cost. Exa's query-dependent highlights reduce output tokens by 50–75% vs raw web search.

---

## Rule 5 — Task Status Registration

Agents record task outcomes manually in the task-log file. The `Stop` hook warns if uncommitted changes remain when a session ends.

### Task-log file location
Task-log files live **beside the plan file** in `Docs/superpowers/plans/`, named `<plan-name>-task-log.md`.
Example: plan at `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` → log at `Docs/superpowers/plans/2026-04-23-artists-songs-catalog-task-log.md`.
Tasks without a plan association are logged to `Docs/superpowers/plans/unassigned-task-log.md`.

> `Docs/DevEnv/plans/` is for SDD research files only — do not place task-logs there.

### Task-log format (per task entry)
```
---
## Task: <title>
**Plan:** <plan file relative path>
**Status:** in progress | Check build | To Review | Build failure | blocked: spec gap | Spec updated — re-planning required | Early task done | Review task done
**Started:** MM/DD/YYYY
**Completed:** MM/DD/YYYY

### Changed files:
- `relative/path/to/file.cs` [— optional business reason if non-obvious]

### Build notes
[Only present if build was checked — records error summary and diagnosis]
```

### Task statuses
| Status | Meaning |
|--------|---------|
| `in progress` | Task started, work underway |
| `Check build` | Code changed — build verification pending (set on task completion if code files were modified) |
| `To Review` | Build passed — task ready for code review (subagent writes this on successful exit) |
| `Build failure` | Build failed after 3 attempts — needs investigation (subagent writes this on exit) |
| `blocked: spec gap` | Spec ambiguity found — question + options + recommendation documented; waiting for clarification |
| `Spec updated — re-planning required` | Implementation revealed a spec gap; spec updated; tasks.md may need re-ordering |
| `Early task done` | New asset/enhancement completed and committed |
| `Review task done` | Review task completed |
