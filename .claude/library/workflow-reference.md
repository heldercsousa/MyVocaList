# Development Workflow — Reference (full detail)

> **This is the on-demand detail file for `.claude/rules/workflow.md`.** The rule file is a routing table (always loaded); this file holds the full procedure detail (Rules 1–8, decision tables, examples, formats) and is loaded on demand. The never-miss HARD RULEs and every inbound `§`-anchor heading remain inline in the routing table — cite `workflow.md § <heading>` for those; cite this file for procedure detail.
>
> These rules are enforced by hooks. Violating them costs rework. Follow them exactly.

---

> **This file is now an index** (split 2026-07-14 for token-scoped subagent reads). Read ONLY the section file(s) your task needs — never all of them. Inbound `§` references resolve via the table below.

| Section file | Covers |
|---|---|
| `workflow-hooks-invariant.md` | Hook Enforcement Notes + SDD Invariant — self-enforced rules, spec-before-code invariant detail |
| `workflow-rule-1.md` | Rule 1 — Spec-First (full detail) — spec decision table, new-feature workflow, spec quality gate, spike pattern |
| `workflow-rule-2.md` | Rule 2 — Subagent Delegation (full detail) — task sizing, wave rules, exit checklist, sequential-only registry |
| `workflow-rule-3.md` | Rule 3 — Commit After Every Task (full detail) — completion gates, session-end spec ritual |
| `workflow-rule-4.md` | Rule 4 — Tasks.md Source of Truth (full detail + DRY Onion phases) — task atomization, task-entry format, phase examples, lease reclaim |
| `workflow-rule-5.md` | Rule 5 — Task Status Registration (full detail) — task-log template, status vocabulary, AC matrix |
| `workflow-rules-6-7-8.md` | Rules 6–8 — Research Gate, Session Start, Collision Check — Context7 gate, session-start reading order, collision responses |
