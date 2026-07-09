# Component Change Governance — Routing Table

> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.
> **This file is a routing table.** Full detail (governed-component list, four-gate specifics, artifacts template, no-bundling rationale) lives in `.claude/library/component-safety-gate.md`, loaded on demand via the `myvocalist-coding` skill map. The non-negotiables below stay inline because they are `[HARD RULE]` and must never be missed.

A change to any **governed component** (a custom UI component consumed by 2+ pages/views — e.g. `SearchAppBar`, `SmallAppBar`, `ListItem`, `EmptyState`, `AutocompleteField`, `CrudListView`) ripples across every consumer. Such a change is a deliberate, tracked, reviewed act — never a side effect of a feature task.

## The Rule `[HARD RULE]` — all four gates required, in order, before any edit

1. **Dedicated task** for the component change + explicit MD3 review against m3.material.io.
2. **Consumer map** — grep every `<local:ComponentName` / `x:Reference` usage (never from memory).
3. **Per-consumer risk assessment** — one line per consumer: what could break + verification step.
4. **Helder approval** recorded before implementation begins.

## No bundling `[HARD RULE]`

A governed-component change may NEVER be bundled into a feature/bug/other task. Its own task, its own task-log entry (with consumer map + per-consumer risk table, or the entry is invalid).

## Scope trigger

A component becomes governed **the moment a second consumer binds to it** — add it to the list in `component-safety-gate.md` in the same commit. A brand-new component with no consumers is NOT governed (normal feature workflow).

| Need | Source |
|------|--------|
| Governed-component list, four-gate detail, required-artifacts template, relationship to `workflow.md` Rules 1–2 | `.claude/library/component-safety-gate.md` |

> **Authorship note:** Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/component-safety-gate.md`.
