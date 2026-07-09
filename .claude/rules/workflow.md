# Development Workflow — Routing Table

> **This file is a routing table.** Full procedure detail for every rule (decision tables, examples, task/task-log formats, checklists, phase templates) lives in `.claude/library/workflow-reference.md`, loaded on demand. Never-miss HARD RULEs and every inbound `§`-anchor heading stay inline below. These rules are hook-enforced — violating them costs rework.

| Need the full detail of | Source |
|-------------------------|--------|
| Any rule's decision tables, examples, formats, phase templates, checklists | `.claude/library/workflow-reference.md § Rule N` |
| Orchestrator protocols (pre-dispatch, briefing, waves, worktrees, review lanes) | `.claude/agents/orchestrator.md` |
| Implementor protocols (context gate, E2E gate, escalation, return protocol) | `.claude/agents/implementor.md` |
| Spec anatomy, AC format, rebuild test | `.claude/library/spec-writing-guide.md` |
| Session artifacts (ACTIVE-CONSIDERATIONS, findings, handoff formats) | `.claude/library/session-ops.md` |

---

## Hook Enforcement (never-miss)

Hooks in `.claude/settings.json` enforce specific rules automatically. Self-enforced rules (no hook) must be applied consciously.

| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` | Session ends with uncommitted changes | Rule 3 — commit after every task (+ Verifier dispatch reminder) |
| `PostCompact` | Context compaction | Session resume — re-read spec |
| `PostToolUse` (Services/*.cs) | Edit to a Services file | testing.md — TDD reminder |
| `SessionStart` | New session | Hook health verification |

**Session-start hook health check:** confirm `.claude/settings.json` is valid JSON and the `Stop` hook references the correct script; fix any misconfigured hook before dispatching a subagent. Self-enforced rules (no hook — apply consciously) and full notes: `workflow-reference.md § Hook Enforcement Notes`.

---

## SDD Invariant (never-miss)

> **Spec changes before code changes.**

- New requirement mid-implementation → update the spec first, then the code.
- Code contradicts the spec → the code is wrong (the spec is not wrong).
- Spec incomplete → stop and clarify with Helder; do not improvise.
- A subagent that modifies behavior not described in the spec has violated this invariant.

Applies to all agents (main and sub) at all times.

---

## Rule 1 — Spec-First

**Before writing any implementation code for a feature, read `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/design.md`.** No exceptions. Code written without reading the spec may contradict it.

- **Spec is source of truth:** spec complete + approved → fix the code; spec has a gap → stop, clarify with Helder, update spec, then fix code. Never silently fix code and leave the spec describing something that no longer exists.
- **Spec structure:** `requirements.md` (stories, ACs, validation, out-of-scope) · `design.md` (architecture, interfaces, flows, decisions) · `tasks.md` (ordered checkboxes).
- **Key thresholds:** ≥ 2 layers OR > 2 hours → full ceremony (all three spec files). Single file, < 1 hour → light. Typo / cosmetic / bug fix → no spec, commit message is the artifact. **When in doubt, write a spec.**
- **Constitution check (2a):** verify the feature violates no CLAUDE.md Non-Negotiable before writing the spec.
- **BACKLOG.md is the source of truth for feature sequencing** — the main agent updates status at each milestone (💡→📋→🗺️→🟢→🟡→✅). Untracked work discovered mid-session gets a brief BACKLOG row *before* proceeding.

Full spec-decision table, new-feature workflow (steps 0–5), proactive-triage format, spec quality gate + four-gate, SDD decision table, discovery mode, brownfield rule, J-Curve: `workflow-reference.md § Rule 1`.

### Spec quality four-gate review

Before a spec is ready for implementation it must pass all four gates — **Correctness** (matches what Helder described), **Completeness** (every story has a criterion; error paths covered), **Consistency** (requirements and design agree), **Testability** (a test can be written from every AC without asking questions). Determinism / prohibited vague terms: `code-style-reference.md`. Full checklist: `workflow-reference.md § Spec quality gate`.

### Spike validation task pattern

A **spike** is a time-boxed exploration producing a `findings.md` artifact, not production code. Rules: spike code is throwaway (no production files edited); the time-box is a hard stop; success → proceed to spec; failure → escalate to Helder (do not unilaterally pick an alternative); inconclusive → document with a recommendation. `[SPIKE]` task format + discovery mode: `workflow-reference.md § Spike validation task pattern`.

---

## Rule 2 — Subagent Delegation

**All coding is done by subagents. The main agent handles shell-only steps** (`dotnet build`/`test`, `dotnet ef`, `git`, reading the spec before briefing).

> **Orchestrator never reads source files `[HARD RULE]`:** the main/orchestrator agent must not read `.cs`, `.xaml`, or any other source file — all code inspection (including plan-mode exploration) is delegated to an Explore/Plan subagent. Allow/deny list + session-start self-check: `.claude/agents/orchestrator.md § Orchestrator Read-Scope`.

- **Wave cap `[HARD RULE]`:** max **4** subagents in parallel; dispatch in waves, wait for all, then next wave. Discard a subagent's context after it completes — never reuse the instance.
- **Git worktrees mandatory for every parallel wave (2+ concurrent subagents)** — `orchestrator.md § Git Worktrees as Isolation Primitive`. Prefer native `EnterWorktree`; fall back to `git worktree add`.
- **Single-writer rule:** at any moment each file has at most one active writer. Before a wave, run the file-overlap check; if two tasks list the same `Files owned`, serialize them.
- **Task sizing:** if a briefing lists > 5 files or > 2 hours of work, it is a sizing violation — split before dispatching.
- **Exit checklist (every subagent, in order):** verification-before-completion → build (0 errors, 3-attempt cap) → test (if `.cs` changed) → post-edit re-read → `.sln` registration → living-spec check → task-log → commit → push. Stopping before all steps = task not finished.

Task sizing table, wave rules, exit checklist detail, post-wave verification: `workflow-reference.md § Rule 2`. Orchestrator/implementor protocols: `orchestrator.md` / `implementor.md`.

### Sequential-only file registry

These files must never have concurrent writers (parallel edits produce conflicts/duplicate errors): `MauiProgram.cs`, `AppShell.xaml(.cs)`, `AppDbContext.cs`, any `*Migration.cs`, any `GlobalUsings.cs`, `Directory.Build.props`, any spec `tasks.md`. Rationale per file + how to add entries: `workflow-reference.md § Sequential-only file registry`.

---

## Rule 3 — Commit After Every Task

**Run `/sln-commit` after every completed task from `tasks.md`.** A session ending with uncommitted changes is at risk — the `Stop` hook warns; treat it as a hard gate.

- **Task complete =** builds with 0 errors + tests pass (if tested code touched) + checkbox checked in `tasks.md`.
- **Completion gates before committing:** demo statement verifiable · new service/repo/VM/page registered in `MauiProgram.cs` · ACs satisfied with evidence in the task-log matrix · **`.sln` registration for every file created/moved/deleted in `Docs/` or `.claude/` — BLOCKING**.
- **Session-End Spec Update Ritual:** review every spec file touched; if it no longer describes what was built, add a `> **Spec updated [YYYY-MM-DD]:**` note; check off completed tasks / mark `[CANCELLED: reason]`; commit spec updates in the final commit.

`/sln-review` is automatic via fresh subagents under `subagent-driven-development`; when executing manually, it is the trigger. Full gates + ritual triggers: `workflow-reference.md § Rule 3`.

### Bug Fix Pattern — commit message as spec

Bug fixes need no three-file spec; the commit message **is** the spec:

```
fix: [component] — [symptom]

Root cause: [one sentence]
Fix: [one sentence]
Regression risk: [None | Low | Medium — reason]
```

If the bug reveals a missing AC, add it to `requirements.md` in the fix commit. Bug *tracking* (BUG-NNN, severity, regression tests): `.claude/rules/bug-tracking.md`.

---

## Rule 4 — Tasks.md Is the Source of Truth

Check off each task in the feature's `tasks.md` as it completes. **Sequential constraint:** never start a task that depends on an incomplete task. **Parallel exception:** `[P]` tasks may run as a wave (Rule 2).

| Marker | Meaning |
|--------|---------|
| `[ ]` | Available — not started |
| `[~]` | In progress — claimed by a dispatched subagent (never dispatch a `[~]` task) |
| `[x]` | Done — committed |
| `[CANCELLED: reason]` | Removed from scope |

- **DRY Onion ordering `[HARD RULE]`:** Domain → Infra → Services → UI. Do not dispatch a Wave N+1 task until all Wave N tasks producing types it consumes are committed.
- **Lease-aware `[~]` reclaim (never-miss):** a `[~]` claim is a lease, not a lock. Classify via `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>`: `fresh` → leave it, pick the next `[ ]`; `reclaimed` → you own it, `resume.py <owner_session_id>` and continue (leave marker `[~]`); `lost` → pick the next `[ ]`. Only reset `[~]`→`[ ]` when the claim is **stale** AND you choose not to reclaim. Never reset a `fresh` claim.

Task-atomization checklist, task-entry format (`Produces`/`Consumes`/`Risk`/`Files owned`/`Demo`/`Review lane`), DRY Onion phase example: `workflow-reference.md § Rule 4`.

---

## Rule 5 — Task Status Registration

Agents record task outcomes in the feature's `task-log.md` (plan in `plan.md`; unassigned tasks → `Docs/DevEnv/plans/unassigned-task-log.md`).

- **Proof of action (never-miss):** a task-log entry claiming `To Review` without a `### Changed files` section listing every created/modified file is **invalid**.
- **AC traceability matrix** (AC ID | Criterion | Implementation location | Test method) required for user-facing behavior. Missing rows = missing tests = incomplete feature.

Full task-log entry template, status vocabulary (`in progress`/`Check build`/`To Review`/`Build failure`/`blocked: spec gap`/…), matrix example: `workflow-reference.md § Rule 5`.

---

## Rule 6 — Research Tool Gate (never-miss)

Before any web research query, follow this order — **both main agent and subagents**:

1. **Library / framework / SDK / API docs** → Context7 (`resolve-library-id` → `query-docs`)
2. **General web research** (comparisons, news, tool evaluations) → `WebSearch` / `WebFetch` — only when Context7 does not cover the topic

---

## Rule 7 — Session Start Protocol

Every implementation/planning session begins with this reading order before any code is written or subagent dispatched. **Do not skip; do not resume from memory alone.**

0. **Hook health verification** (see Hook Enforcement above) — fix misconfigured hooks first.
1. **Active handoff file** `…/[feature]/handoff.md` if present — else read `Docs/Management/BACKLOG.md` for the current `🟡 In Progress` / highest `🟢 Ready` item.
2. **`ACTIVE-CONSIDERATIONS.md`** (if it exists) — priority stack + open items.
3. **`…/[feature]/tasks.md`** — done / `[~]` / pending.
4. **`…/[feature]/requirements.md`** — refresh ACs.
5. **`…/[feature]/design.md`** — refresh architecture + interface signatures.
6. **`…/[feature]/task-log.md`** — check for unresolved `blocked:` / `Spec updated — re-planning required`.
7. **Lease claim refresh + resume-pointer read** — classify existing `[~]` claims via `reclaim.py` (Rule 4); read the resume pointer with `python .claude/scripts/lease/resume.py <session_id>`; the heartbeat hook keeps this session's own claim fresh automatically; record progress via `resume.py --set <session_id> "<continue-from-here>"`.

**Anti-glob rule (never-miss):** never `Glob("Docs/**")` or equivalent open-ended scans during session start or briefing — read only the files above plus the active feature spec. Steps 3–7 may be scoped to the active feature. Session-artifact formats: `session-ops.md`.

---

## Rule 8 — GitHub MCP Pre-Task Collision Check

Before dispatching any wave that modifies files, confirm no other agent/branch is modifying the same files.

- **GitHub MCP available:** check open PRs on the branch base (any touching a wave `Files owned` = collision risk) · review last 10 commits · confirm no `[~]` task is owned by another live session.
- **Not available:** `git log --oneline -10`, `git status`, scan `tasks.md` for stray `[~]`, and run the **lease liveness check** — classify each `[~]` with no known running agent via `reclaim.py` (`lease_lib.classify`) **before** assuming abandonment. A `fresh` result means another live session owns it — do **not** reset to `[ ]`.

Collision-type response table: `workflow-reference.md § Rule 8`.
