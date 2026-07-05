# Component Change Governance — Reference

> Extracted from `.claude/rules/component-change-governance.md` (2026-07-05, rules-file-refactoring Task 04). The rule file is now a routing table; this file holds the full detail. Discovered via the `myvocalist-coding` skill map or the rule's routing table.
> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.
> MD3 terminology rule: all component names use official MD3 terms — see `CLAUDE.md § Constitutional Constraints`.

Custom shared UI components are consumed by many pages. A change to one ripples across every consumer. This file makes such changes a deliberate, tracked, reviewed act — never a side effect of a feature task.

---

## Scope — what is a "governed component"

Any reusable custom UI component consumed by 2+ pages/views. Current set (non-exhaustive — add new ones here as they are created):

| Component | Location (typical) |
|-----------|--------------------|
| `SearchAppBar` | `UI/Components/` |
| `SmallAppBar` | `UI/Components/` |
| `ListItem` | `UI/Components/` |
| `EmptyState` | `UI/Components/` |
| `AutocompleteField` | `UI/Components/` |
| `CrudListView` | `UI/Components/` |

> Rule: A component is governed the moment a second consumer binds to it. When you create a component's second consumer, add the component to this table in the same commit.
> Rationale: the blast radius of a shared component is proportional to its consumer count — governance must begin before the count grows silently.

---

## The Rule — no governed-component change without all four gates

A modification to any governed component (XAML, code-behind, `BindableProperty`, visual tree, default style, or public binding contract) is FORBIDDEN unless ALL of the following exist first:

1. **A dedicated task** for the component change, with an explicit MD3 compliance review of the change against m3.material.io.
2. **A consumer map** — a systematic list of every page/view that consumes the component (grep all `<local:ComponentName` / `x:Reference` usages; do not rely on memory).
3. **A per-consumer risk assessment** — for each consumer in the map, one line: what could break and the verification step.
4. **Helder approval** recorded before any implementation begins.

> Rationale: a shared component touched casually inside a feature breaks unrelated pages with no review trail; the four gates force the blast radius to be measured and approved before code changes.

---

## No bundling — HARD RULE

A governed-component change may NEVER be bundled into a feature task, bug-fix task, or any other task. It must be its own tracked task with its own task-log entry.

> Rationale: bundling hides the component change inside an unrelated diff, so the consumer-wide impact is never reviewed and regressions surface in pages no one tested.

---

## Required artifacts (per component-change task)

```markdown
- [ ] **[COMPONENT] Change <ComponentName> — <one-line what>** [SEQUENTIAL]
  - **MD3 review:** <which MD3 spec section the change is checked against>
  - **Consumer map:** <list every page/view binding to the component>
  - **Per-consumer risk:**
    | Consumer | What could break | Verification |
    |----------|------------------|-------------|
  - **Helder approval:** <date / pending>
  - **Files owned:** the component file(s) ONLY
  - **Demo:** <observable result in each consumer after the change>
```

A component-change task whose task-log entry lacks the consumer map or the per-consumer risk table is invalid (treat like a missing `Changed files` section — see `workflow.md` Rule 5).

---

## Relationship to other rules

- Sizing/wave/single-writer rules still apply (`workflow.md` Rule 2). The component file is a single-writer hotspot for the duration of the task.
- This rule strengthens `workflow.md` Rule 1's spec-decision table: a governed-component change is always at least "Standard" ceremony regardless of estimated effort, because its blast radius is wide.
- Adding a brand-new component (no existing consumers) is NOT governed by this file — it follows the normal feature workflow. Governance begins at the second consumer.

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
