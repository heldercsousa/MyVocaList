---
id: always-loaded-context-path-scoped-rules-spike-and-superpowers-plugin-cost
title: "Always-loaded context: path-scoped rules spike and superpowers plugin cost"
status: 🔵 Superseded
target: —
parent: docs-context-scope-control
goal: Find out whether code-only rules files can load on demand without failing to reach subagents, and measure the fixed cost of the superpowers plugin, before any decision.
gate: Helder decides on the spike result and the measured plugin cost.
closed: 2026-11
kind: change
---

# Always-loaded context: path-scoped rules spike and superpowers plugin cost

Find out whether code-only rules files can load on demand without failing to reach subagents, and measure the fixed cost of the superpowers plugin, before any decision.

## Scope

Split from `../2026-11-15-token-usage-reduction-read-guards-worktree-location-backlog-and-ledger-redesign-always-loaded-context/` on 2026-11-15. Requirements and design are already written there:

- **Path-scoped rules spike:** `requirements.md` REQ-TUR-06 and `design.md § E2`. One hour, output `findings.md`. Nothing is scoped unless questions (a), (b) and (c) all pass.
- **Superpowers plugin cost:** `requirements.md` REQ-TUR-09 and `design.md § E5`. Baseline from 2026-11-15, plugin on: 31.6k tokens at the first prompt, skills listing 2.5k.

## Closed without work

Dropped 2026-11-19 during backlog cleanup. Evaluated and rejected: path-scoping the code-only rules saves at most about 2.4k tokens (about 7% of the 31.6k baseline) and risks the rules silently not reaching subagents; the plugin cost is a fixed cost with no decision attached. Not to be re-proposed without a new measured need.
