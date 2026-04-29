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

### How to brief a subagent
Give it: the spec file paths, the tasks to complete, the patterns to follow (rules files), and the
constraint that it must build and fix errors before returning. Never say "based on what you find" —
pre-read the spec and hand the subagent concrete instructions.

### When to take back control
- After the subagent returns: run `dotnet build` and `dotnet test` as main agent
- If a shell command is needed mid-way (migrations, file moves): do it inline, then re-delegate

### Subagent exit checklist (mandatory before returning)
Every subagent must, in this order: build (0 errors) → commit changed files → push (`git push origin HEAD`).
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
Never start a new task before the previous one is committed.
The task list is the audit trail for the feature — keep it accurate.

---

## Rule 5 — Task Status Registration (Placeholder)

All agent task outcomes — regardless of status — must be recorded.
Current location: `Docs/task-log.md` (enforced by the `TaskCompleted` hook).
This will be replaced by the SDD-defined tracking mechanism once those specs are finalized (see S3.1, S8.1).
