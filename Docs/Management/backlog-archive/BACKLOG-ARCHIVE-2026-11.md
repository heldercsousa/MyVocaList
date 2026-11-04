# BACKLOG Archive — 2026-11

> Closed backlog rows completed in 2026-11, moved out of `Docs/Management/BACKLOG.md`. Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-08-18 | Recalibrate agent-dispatch settings — orchestrator read-ban and subagent-for-trivia (under: **Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception**) | ✅ Done | Goal: Agent-dispatch settings over-trigger: the orchestrator is barred from reading any source file, and tiny tasks spawn fresh subagents that reload full context they never use. Gate: Amendment shipped 2026-11-03 (approved, pushed). Effectiveness is reviewed in a separate post-ship review item. Pointer: `DevCycleCraft/inline-trivial-fix/changes/2026-08-18-recalibrate-agent-dispatch-settings-orchestrator-read-ban-and-subagent-for-trivia/`. |
| 2026-11-03 | Second trim of CLAUDE.md and workflow.md (under: **Rules File Refactoring — Reduce Unconditional Load**) | ✅ Done | Goal: Both files load into every session and every subagent. Move rationale and procedure into library files so only never-miss rules stay always-loaded. Gate: Helder authorised 2026-11-03; done when both are measurably smaller with no never-miss rule lost. Pointer: `DevCycleCraft/rules-file-refactoring/changes/2026-11-03-second-trim-of-claude-md-and-workflow-md/`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
