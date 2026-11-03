# Requirements — Recalibrate agent-dispatch settings

> Change to the ITF lane (parent: `DevCycleCraft/inline-trivial-fix/`). Decisions taken by Helder
> 2026-11-03 after a `/claude-code-setup:claude-automation-recommender` pass — see `README.md`
> § Decisions. The parent's AC-ITF-* criteria stay in force except where an AC below narrows or
> widens them.

## User story

As Helder, I want the orchestrator to confirm a diagnosis and apply a one-line fix itself, so that a
fully understood trivial change does not cost a ~50k-token subagent cold start.

## Acceptance criteria

| ID | Criterion | Verified by |
|----|-----------|-------------|
| AC-ADR-01 | **Read lane.** The orchestrator may run Grep and read **at most 2** source files (read-only) per diagnosis question, to confirm a diagnosis, a signature, or that a member exists. A question that 2 files do not settle is delegated to an `Explore` subagent. Reading to learn how a flow works is never in the lane. | Prose rule — `orchestrator.md § Orchestrator Read-Scope`; audited in review |
| AC-ADR-02 | **C2 widened.** Diagnosis done through the read lane counts as "fully diagnosed" for ITF C2. | Prose rule — `workflow-rule-2.md` C2 |
| AC-ADR-03 | **C5 soft set.** With an active, valid declaration whose `sole_writer` field is the JSON boolean `true`, an edit to `MauiProgram.cs`, `GlobalUsings.cs` or `Directory.Build.props` that satisfies C1 is permitted by Guard 3. | `test_constitutional_guard.py` |
| AC-ADR-04 | **C5 soft set still blocks by default.** If `sole_writer` is absent, `false`, or any non-boolean value (e.g. the string `"true"`), an edit to a soft-set file is blocked with a C5 message. | `test_constitutional_guard.py` |
| AC-ADR-05 | **C5 hard set never relaxes.** `AppDbContext.cs`, `tasks.md`, and any file under a `Migrations/` folder are blocked with C5 even when `sole_writer` is `true`. | `test_constitutional_guard.py` |
| AC-ADR-06 | **Sole-writer check.** The orchestrator sets `sole_writer: true` only after confirming that no unmerged task branch changes the file (`git diff --name-only develop...<branch>` over `git branch --no-merged develop`) and no dispatched subagent owns it (LEDGER Active rows). | Prose rule — `/sln-itf` step 2 |
| AC-ADR-07 | **Hooks stop contradicting the lanes.** The `UserPromptSubmit` gate text, the `PostCompact` reminder and the `.cs`-edit reminder name the ITF and read lanes; the `.cs`-edit reminder is silent when `.itf-active` exists in the edited file's worktree. | Manual — hook output inspected |
| AC-ADR-08 | **Cheaper subagent defaults.** `implementor`, `verifier`, `spec-reviewer` and `plan-reviewer` declare `model: sonnet`; `orchestrator.md` states the difficulty-to-model default. | Frontmatter inspection |
| AC-ADR-09 | **`/sln-itf` command.** `.claude/commands/sln-itf.md` walks C0–C8, the sole-writer check, the marker, the task-log `ITF:` line, build/test, the commit trailer, and marker deletion. | File inspection |

## Out of scope

- The `type: agent` `TaskCreated` / `TaskCompleted` hooks (an agent per task event, auto commit + push,
  stale paths) — registered as a separate item.
- C1's 5-line cap, C3, C4, C6, C7 and C8 — unchanged.
- Any mechanical enforcement of the read lane — it stays a prose rule, like C2 and C6.
