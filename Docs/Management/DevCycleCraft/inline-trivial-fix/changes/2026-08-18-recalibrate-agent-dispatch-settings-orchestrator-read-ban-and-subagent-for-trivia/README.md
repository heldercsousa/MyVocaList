---
id: recalibrate-agent-dispatch-settings-orchestrator-read-ban-and-subagent-for-trivia
title: Recalibrate agent-dispatch settings — orchestrator read-ban and subagent-for-trivia
status: 🟡 In Progress
target: 2026-08-18
section: DevCycleCraft
parent: inline-trivial-fix
goal: "Agent-dispatch settings over-trigger: the orchestrator is barred from reading any source file, and tiny tasks spawn fresh subagents that reload full context they never use."
gate: Amendment applied 2026-11-03 and awaiting Helder's review of the rules diff before the amend commit. Done once the first live /sln-itf fix runs end to end without a dispatch.
kind: change
---

# Recalibrate agent-dispatch settings — orchestrator read-ban and subagent-for-trivia

Agent-dispatch settings over-trigger: the orchestrator is barred from reading any source file, and tiny tasks spawn fresh subagents that reload full context they never use.


## Observations that prompted this (Helder, 2026-08-18)

Two settings appear miscalibrated in opposite directions, and they compound:

1. **Orchestrator read-ban is absolute.** `CLAUDE.md § Roles` and `workflow.md` Rule 2 forbid the
   orchestrator from reading *any* `.cs`/`.xaml` file. The rule's purpose is to stop the orchestrator
   from silently drifting into implementation, but as written it also blocks cheap read-only acts —
   confirming a method signature, checking whether a field exists — forcing a subagent round-trip
   that costs far more than the read it replaces.

2. **Subagents are dispatched for trivia.** A fresh subagent loads the full rules/skills/memory
   preamble before it reads a single project file. When the task is a two-line lookup, nearly all of
   that loaded context goes unused. The ITF lane (this item's parent) already carves out an exception
   for *writes*; no equivalent exists for *reads*.

## Questions for the review

- Should the read-ban distinguish read-only inspection from edit-intent? If so, what bounds keep it
  from eroding into "the orchestrator implements things"?
- Is there a read-side analogue to ITF's C0–C8 gate list, or does ITF simply widen to cover reads?
- What is the actual measured cost of a trivial subagent dispatch? The ITF spec cites ~25–35k tokens
  for a fix round-trip; the read-only case has not been measured.

## Relationship to the parent

This nests under the ITF lane because it is the same trade-off — orchestrator autonomy vs. drift
risk — seen from the read side rather than the write side. Reviewing them together avoids setting
two inconsistent thresholds.

## Second observation (Helder, 2026-11-03)

A spike page crashed on device because it was never registered in DI. Diagnosis was complete from the stack trace and the fix was one registration line, yet the orchestrator spawned an implementor (about 50k tokens to start) because the read-ban plus "all coding by subagents" left no other lane. MauiProgram.cs being sequential-only also excludes it from ITF. Helder stopped the agent: "We need to stop spawning subagents to do things so simple ... update the internal settings to avoid spawning for situations like that." Inputs for the calibration pass: (1) allow inline one-line fixes on throwaway spike branches; (2) reconsider excluding sequential-only files from ITF when no other writer is live; (3) let the orchestrator grep/read a single source file to confirm a diagnosis.

## Automation-recommender pass (2026-11-03)

Run before deciding, at Helder's request. Findings that bear on this item:

- **Hooks pushed toward dispatch on every turn.** The `UserPromptSubmit` gate (`settings.local.json`)
  matched almost any prompt and injected "the main agent does shell only and never writes code",
  which overrode the ITF lane in practice; it also cited the retired `/project:commit`. The `.cs`-edit
  reminder fired even inside a declared ITF edit, and the `PostCompact` reminder restated the ban.
- **No agent declared `model:`**, so every implementor and reviewer ran on Opus (open task from the
  2026-07-23 model-routing feedback).
- **Cold-start cost.** About 60 KB of always-loaded instructions (`CLAUDE.md`, `.claude/rules/*`,
  global + memory index), roughly 15k tokens, load before skills, the system prompt and the
  SessionStart preamble, consistent with the ~50k observed start. Not measured per dispatch.
- **`type: agent` hooks on `TaskCreated` / `TaskCompleted`** start an agent per task event and run
  `git add -A && commit && push`. Out of scope here; registered as its own item.
- Nothing useful under MCP servers or plugins.

## Decisions (Helder, 2026-11-03)

| Question | Decision |
|----------|----------|
| Read side | **Read lane:** grep + at most 2 source files per written question, read-only, to confirm a diagnosis/signature/member. Wider questions go to Explore. Prose-enforced. |
| C5 | **Soft/hard split.** `MauiProgram.cs`, `GlobalUsings.cs`, `Directory.Build.props` are ITF-eligible with `"sole_writer": true` after the sole-writer check; `AppShell`, `AppDbContext.cs`, `tasks.md`, `Migrations/` never. Guard 3 enforces it. |
| Spike branches | Already task branches under C7; stated explicitly. |
| Extras | Hooks reworded and the `.cs` reminder made ITF-aware · `model: sonnet` on implementor/verifier/reviewers + a difficulty→model table in `orchestrator.md` · `/sln-itf` command · agent-hook item registered separately. |

Acceptance criteria: `requirements.md`.
