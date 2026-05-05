# Development Workflow

> These rules are enforced by hooks. Violating them costs rework. Follow them exactly.

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

### New feature workflow
1. **Brainstorm** — invoke `superpowers:brainstorming`
2. **Write spec** — write all three files; user reviews and approves
3. **Write plan** — invoke `superpowers:writing-plans`
4. **Implement** — delegate to a subagent (see Rule 2)
5. **Review** — invoke `/project:review` after each phase

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
