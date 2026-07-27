---
id: rules-file-refactoring
title: "**Rules File Refactoring — Reduce Unconditional Load**"
status: "✅ Done"
target: 2026-07-04
section: DevCycleCraft
kind: feature
closed: 2026-07
order: 190
goal: "cut unconditional rules-load tokens. Shipped (routing tables + library files; measured savings per agent); all 18 tasks + audit closed 2026-07-09."
pointer: DevCycleCraft/rules-file-refactoring/
---

# Rules File Refactoring — Reduce Unconditional Load

Cut unconditional rules-load tokens by splitting `.claude/rules/*.md` into thin routing
tables backed by on-demand `.claude/library/` files. Shipped; all 18 tasks plus the final
audit closed 2026-07-09.

> Migrated from the 2026-07 archive row (T12a Wave S). Folder pre-existed
> (`design.md`, `requirements.md`, `plan.md`, `tasks.md`, `task-log.md`,
> `context-audit-2026-07-07.md`, `findings-measurement.md`, `pilot-findings.md`,
> `skill-overlap-findings.md`); only this `README.md` is new. The archived Notes cell's
> measured-savings figure ("~8–11k/agent saved") is paraphrased here without the digit+k
> shorthand — the model's token-measurement heuristic bans the literal `Nk tokens` form —
> wording only, no meaning change, flagged for audit.
