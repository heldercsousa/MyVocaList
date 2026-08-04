---
id: inline-trivial-fix
title: "**Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception**"
status: "🟡 In Progress"
target: 2026-07-12
section: DevCycleCraft
kind: feature
order: 20
goal: "let the orchestrator apply a fully-diagnosed 1-file/≤5-line fix inline instead of paying a ~25–35k-token subagent round-trip."
gate: "Helder observes the first live ITF fix end-to-end before ✅ — opportunistic, waits for a qualifying fix."
pointer: DevCycleCraft/inline-trivial-fix/
---

# Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception

Let the orchestrator apply a fully-diagnosed 1-file/≤5-line fix inline instead of paying a ~25–35k-token subagent round-trip. Specs: `requirements.md`, `design.md`, `task-log.md`.

**Notes overflow (transcribed from the pre-migration BACKLOG row):** Spec approved by Helder 2026-07-21 (spec-reviewer PASS); rule amendments applied. Rules amended + Guard 3 merged to develop 2026-07-21 (verifier CONDITIONAL PASS, all findings resolved; 33/33 green). Resume instructions in the folder's `handoff.md`.
