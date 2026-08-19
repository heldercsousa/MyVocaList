---
id: recalibrate-agent-dispatch-settings-orchestrator-read-ban-and-subagent-for-trivia
title: Recalibrate agent-dispatch settings — orchestrator read-ban and subagent-for-trivia
status: 💡 Pending
target: 2026-08-18
section: DevCycleCraft
parent: inline-trivial-fix
goal: "Agent-dispatch settings over-trigger: the orchestrator is barred from reading any source file, and tiny tasks spawn fresh subagents that reload full context they never use."
gate: Review alongside the ITF lane's first live fix so both share one calibration pass.
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
