# 00 — Original Ask + Enhanced Pipeline Spec

## Verbatim user request (two messages, 2026-06-20)

> "enter plan mode. Analyse in `Docs/Management/BACKLOG.md` the task **'BACKLOG-first Registration
> Enforcement'**. Proceed as an orchestrator only, and delegate to 2 Opus subagents to analyse the
> context of the settings and figure out what may be wrong and what could be done, each one
> independently of the other, and each analysing entire internal settings. When they complete, You
> must neither analyse the [2] subagents result (receive context from them), nor be in charge of
> analyse anything about. The analysis of the 2 agents answers must be compared by another 2
> subagents, also Opus model, which both will be receiving the 2 initial subagents results, and each
> will create a plan with changes recommended. Then, after having their planning, you will spawn 1
> subagent, also Opus, to compare these 2 plans and determine the differences and what makes more
> sense or not under the current settings and the goals established."
>
> "Then, this 5th agent will write the final plan! After this final plan, you must spawn 2 agents,
> Opus, to review properly, gathering current settings and reading the 5th agent plan. Then, finally,
> you, by yourself, will analyse these 2 reviews and decide which of them are better based on the
> settings defined currently."

## Follow-up instruction (persistence + resumability, 2026-06-20)

> The plannings and reviews must be stored in a *task-dedicated folder* (NOT loose in
> `Docs/Management/` directly), organized and labeled with what each one is, plus the original
> (enhanced) ask — so that if this session is interrupted (including for being too large) the work
> can be resumed without restoring the session. (This folder is the result of that instruction.)

## Enhanced pipeline specification (orchestrator's operational reading)

- **Role:** orchestrator / router ONLY. Does not analyze intermediate outputs; forwards each wave's
  output verbatim into the next wave. The single reserved exception: the FINAL step, where the
  orchestrator alone decides which of the two reviews is stronger, judged against the project's
  current settings.
- **All subagents: Opus.** All waves are read-only analysis returning text (the originating run was
  conducted under plan mode).
- **Waves:**
  - **Wave 1 — 2 independent analysts (A, B):** identical brief; each sweeps the entire internal
    settings/governance (`BACKLOG.md`, `CLAUDE.md`, `.claude/rules/*`, `.claude/settings.json`,
    hook/lease scripts, the memory system); no knowledge of the other.
  - **Wave 2 — 2 independent planners (1, 2):** each receives BOTH analyses verbatim; each writes an
    independent recommended-changes plan.
  - **Wave 3 — 1 synthesis agent (the "5th agent"):** receives BOTH plans; compares, resolves factual
    disputes against ground truth, writes the final consolidated plan.
  - **Wave 4 — 2 independent reviewers (1, 2):** each independently gathers current settings and
    reviews the final plan; returns a verdict + required changes.
  - **Final — orchestrator alone:** decides which review is stronger, based on current settings.

## Subject under analysis

`Docs/Management/BACKLOG.md` line 150 (Dev Cycle Craft, `💡 Pending`, dated 2026-06-20):

> **BACKLOG-first Registration Enforcement** — "Agents must register work items in BACKLOG.md
> (nested under parent feature when applicable) before writing to memory. Memory is personal/
> device-scoped and not team-visible. Tooling opportunity: hook or review gate that detects
> memory-only registrations and blocks session end unless BACKLOG was updated. Current rule in
> workflow.md Rule 1 (Proactive BACKLOG triage) exists but is not mechanically enforced. Details:
> `DevCycleCraft/backlog-first-registration/spec.md` (create when moved to 📋 Spec)."
