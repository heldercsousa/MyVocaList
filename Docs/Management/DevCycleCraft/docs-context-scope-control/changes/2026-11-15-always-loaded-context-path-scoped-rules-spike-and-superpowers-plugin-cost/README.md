---
id: always-loaded-context-path-scoped-rules-spike-and-superpowers-plugin-cost
title: "Always-loaded context: path-scoped rules spike and superpowers plugin cost"
status: 💡 Pending
target: —
parent: docs-context-scope-control
goal: Find out whether code-only rules files can load on demand without failing to reach subagents, and measure the fixed cost of the superpowers plugin, before any decision.
gate: Helder decides on the spike result and the measured plugin cost.
kind: change
---

# Always-loaded context: path-scoped rules spike and superpowers plugin cost

Find out whether code-only rules files can load on demand without failing to reach subagents, and measure the fixed cost of the superpowers plugin, before any decision.

## Scope

Split from `../2026-11-15-token-usage-reduction-read-guards-worktree-location-backlog-and-ledger-redesign-always-loaded-context/` on 2026-11-15. Requirements and design are already written there:

- **Path-scoped rules spike:** `requirements.md` REQ-TUR-06 and `design.md § E2`. One hour, output `findings.md`. Nothing is scoped unless questions (a), (b) and (c) all pass.
- **Superpowers plugin cost:** `requirements.md` REQ-TUR-09 and `design.md § E5`. Baseline from 2026-11-15, plugin on: 31.6k tokens at the first prompt, skills listing 2.5k.
