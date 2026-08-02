# Development Workflow — Reference — Rule 2 — Subagent Delegation (full detail)

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 2 — Subagent Delegation

**All coding is done by subagents. The main agent handles shell-only steps.**

> **Orchestrator never reads source files `[HARD RULE]`:** the main/orchestrator agent must not read `.cs`, `.xaml`, or any other source file — all code inspection (including plan-mode codebase exploration) is delegated to an Explore/Plan subagent. Read-scope allow/deny list, plan-mode reconciliation, and session-start self-check: see `.claude/agents/orchestrator.md § Orchestrator Read-Scope`.

| Main agent does | Subagent does |
|----------------|---------------|
| `dotnet build` | Any file creation or edit |
| `dotnet test` | ViewModels, pages, services, repositories |
| `dotnet ef migrations add` | XAML, code-behind, DI registration |
| `git status`, `git add`, `git commit` | Route additions, AppShell registration |
| Reading spec before briefing subagent | Everything in `crud-pages.md` |

> **Orchestrator-side protocols** (pre-dispatch checklist, pre-wave dependency check, briefing protocol, role scope declaration, adversarial critic, wave handoff, discovery briefs, multi-wave checkpoint, merge sequencing, worktrees, kill criteria, approval authority matrix, review lanes, DGI classification): see `.claude/agents/orchestrator.md`.

> **Implementor-side protocols** (pre-task context gate, intent verification, E2E emulator gate, bounded autonomy rule, spec gap escalation format, subagent return protocol, scope constraints, living spec protocol, post-edit re-read, MCP isolation): see `.claude/agents/implementor.md`.

### Task sizing limits — context window budget

| Task type | Max files touched | Max estimated effort |
|-----------|-------------------|----------------------|
| New service method + tests | 4 files | 90 min |
| New ViewModel + page + tests | 5 files | 2 hours |
| Migration + repository + tests | 4 files | 90 min |
| UI page only (XAML + code-behind) | 3 files | 60 min |
| Full CRUD feature (all layers) | Split into 2+ tasks | — |

**Rule:** If a proposed task exceeds these limits, split it before dispatching.

**Warning sign:** A subagent briefing that lists > 5 files or > 2 hours of estimated work is a sizing violation.

### Wave-based parallelism — hard cap

- **Maximum 4 subagents may run in parallel at any one time.**
- Work is dispatched in waves: spawn up to 4 subagents, wait for all to complete, then start the next wave.
- Never spawn a 5th concurrent subagent — stagger instead.
- After a subagent completes, its context is discarded. Do not reuse the same subagent instance for a second task.
- **Git worktrees are mandatory for every parallel wave (2+ concurrent subagents).** See `orchestrator.md § Git Worktrees as Isolation Primitive`. Check for the native `EnterWorktree` tool first; fall back to `git worktree add .worktrees/<name>` only if unavailable.

### Single-writer rule for hotspot files

**At any given moment, each file in the repository must have at most one active writer.**

Before dispatching any wave, the main agent performs the file overlap check. If two tasks in the wave list the same file in `Files owned`, they cannot run in parallel — serialize them.

### Sequential-only file registry

Some files must never be edited by more than one agent at a time:

| File | Reason |
|------|--------|
| `MauiProgram.cs` | DI registration — ordering matters; parallel edits produce conflicts |
| `AppShell.xaml` / `AppShell.xaml.cs` | Route registration — one canonical route table |
| `AppDbContext.cs` | EF Core model config — entity set definitions must be coherent |
| Any file under a `Migrations/` folder | EF migrations are sequential by design. Match on the folder, not on a `*Migration.cs` name pattern — EF generates `20260407190608_PersonConfigFixes.cs`, which no name-suffix rule catches (corrected 2026-07-21) |
| `GlobalUsings.cs` (any project) | Global using declarations — merge conflicts produce duplicate errors |
| `Directory.Build.props` | Shared MSBuild properties — parallel edits produce conflicts |
| `tasks.md` (any spec) | Task status tracking — parallel checkbox edits produce divergent state |

**How to add entries:** If a session discovers a new hotspot file, add it to this registry before ending the session.

### Subagent exit checklist (mandatory before returning)

Every subagent must complete ALL of these steps in order before stopping:

1. **Invoke `superpowers:verification-before-completion`** — catches non-negotiable violations (DevExpress-first, SafeAreaEdges, English-only, no DisplayAlert, etc.)
2. **Build:** Run `dotnet build` and confirm 0 errors. If build fails, apply the build retry cap (max 3 attempts). Document result in Verification evidence.
3. **Test:** If any `.cs` implementation file was changed, run `dotnet test` and confirm 0 failures. Document result. Skip only if no code files were modified.
4. **Post-edit re-read:** Re-read the affected section of every edited file and confirm correctness.
5. **.sln registration — BLOCKING:** For every file created, moved, or deleted in `Docs/` or `.claude/`: update `MyVocaList.sln` now (same commit). See `constraints-registry.md § Visual Studio Solution (.sln)` for exact pattern. Do NOT skip even if only docs changed.
6. **Living spec check:** Review decisions made during implementation — write back any undocumented decisions to the spec.
7. **Task-log:** Complete the task-log entry including Changed files, Verification evidence, and AC traceability matrix (if applicable).
8. **Commit:** Commit all changed files including any spec updates.
9. **Push:** `git push origin HEAD`

**The `Stop` hook warns if uncommitted changes remain. Treat it as a hard gate.**

A subagent that stops without completing all 8 steps has not finished the task.

### Post-wave verification — main agent runs build independently

After every wave completes, the main agent must run the build and tests independently:

1. Run `dotnet build` — confirm 0 errors. Do not proceed to the next wave if there are errors.
2. Run `dotnet test` — confirm 0 failures. Investigate any new failures before proceeding.
3. Review the task-log entries from the wave — confirm all entries have Verification evidence and Changed files.
4. If a subagent reported `Build failure` or `blocked: spec gap`, resolve before dispatching the next wave.

---

## Generated artifacts — write-ownership protocol

**1. Which artifacts are generated.**

| Artifact | Class | Rule |
|----------|-------|------|
| `Docs/Management/BACKLOG.md` (inside `BACKLOG:GENERATED` fences) | generated | regenerate, never hand-edit |
| `Docs/Management/backlog-archive/*.md` (inside fences) | generated | regenerate, never hand-edit |
| `Docs/Management/**/README.md` frontmatter block | **source** | edit the value, then `regen` |
| `README.md` body, `task-log.md`, `tasks.md`, `plan.md`, spec files, `LEDGER.md`, changelog | hand-written | ordinary merge; land on develop |

Everything **outside** a fence in a generated file (headers, Row rules, prose) is hand-written and
byte-preserved by the generator — editing there is always safe.

**2. When the pre-commit gate rejects a stale BACKLOG: regenerate, then re-inspect — never
`--no-verify`.** Run `backlog_gen.py regen`, then `git diff` the regenerated files **before**
committing. If the diff touches only rows whose frontmatter this session changed → commit. If it
touches rows this session never touched, another session's frontmatter change is unregenerated:
**stop and escalate** — do not commit, and do not revert their rows. Blind regeneration is safe
for the *rendered* file (it is a total function of frontmatter) but the diff is the only signal
that a second writer is active, so it must be read, not skipped.

**3. How a second session learns a region is owned — LEDGER names the owner, the lease proves it is
alive.** A session doing a **bulk** operation over generated artifacts (a migration, a mass status
sweep, anything that rewrites rows it did not author) declares it in its `LEDGER.md` row before its
first write: **owner session id · branch · since-date**, retired when the branch merges.

A second session that wants to regenerate resolves that declaration as follows:

| LEDGER declaration | Check | Action |
|--------------------|-------|--------|
| none | — | no owner; proceed normally |
| present, names a session id | `classify()` that session's `.claude/leases/<id>.json` | **fresh** → owner is live: edit your own item's frontmatter only, let the owner regenerate, do not run bare `regen`. **stale** → the owner session is dead: take over, note the takeover in the LEDGER row, proceed |
| present, no session id (legacy row) | — | treat as owned until the row's branch merges, or ask Helder |

**The lease is session-keyed, not unit-keyed** — `.claude/leases/<session_id>.json`, written by the
heartbeat hook, carrying `branch`/`worktree`/`task_id`. There is **no API to claim a named unit**,
so "claim the generated region" is not something the lease can express; what it can do, and all
this protocol asks of it, is answer *"is the session named in that LEDGER row still alive?"* Use
`lease_lib.classify` (read-only) for that. **Do not use `reclaim.py` here** — it *overwrites* the
target's claim on a stale result, which is correct for taking over a `[~]` task and wrong for
asking a liveness question about a region.

**Ordinary single-item work (`register` / `status` on one item) takes no claim.** It writes one
frontmatter block plus a deterministic re-render, and the pre-commit gate already fails it loud if
another session left the rendered files stale — detection is mechanical, so the coordination cost
buys nothing. Requiring a claim per write would make `register` unusable and push agents toward
`--no-verify`, which is strictly worse than the race it would be guarding against. The rule is
proportional to blast radius.

*Rationale for the split, recorded because it is a policy choice and not a derivation:* the
failure this protocol exists to prevent is **one session's rows being rewritten from another
session's stale view of the tree**. Only a bulk rewrite can do that; a single-item write cannot,
because regeneration is a total function of frontmatter and touches no row whose source it did not
read.

**4. Concurrent `register` / `status` are safe within one repo; the hazard is cross-worktree ID
allocation.** Both verbs are atomic (folder + `README.md` + `.sln` + re-render written in one pass,
rolled back on failure), so a half-registration cannot trip the `.sln` HARD GATE. What they cannot
see is a `BUG-NNN` allocated in a *different worktree* that has not merged yet — two worktrees can
allocate the same ID. That is detected at merge (duplicate `id` is a validation error, so the
merge commit cannot pass the gate) and fixed with `backlog_gen.py renumber BUG-NNN`, which rewrites
the folder name and the `id:` key. Renumbering is safe because the ID lives in exactly two places;
every other reference is a path.

---
