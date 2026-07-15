# CRUD Page Design Laws

> This document defines the **laws and decision guidance** for building list/form page pairs in MyVocaList.
> It is NOT a copy-paste template. It tells you **what is non-negotiable**, **what varies**, and **how to decide**.
>
> Reference implementation: `Docs/specs/venues/` (requirements + design + tasks — all confirmed working).

---

## The Three Laws (non-negotiable)

**1. MD3 compliance always.**
Every layout decision must be traceable to the Material Design 3 specification. No custom UI patterns that contradict MD3 — not for convenience, not for "it looks fine". If in doubt, check the spec first.

**2. Use existing custom components when the slot fits.**
`SmallAppBar`, `SearchAppBar`, `ListItem`, `ListItemLeadingIcon`, `ListItemLeadingAvatar`, `ListItemLeadingImage`, `FloatingToolbar` — these exist and are MD3-compliant. Use them. Only build a new component when no existing custom component covers the need.

**3. DevExpress first. Custom second.**
Always check `.claude/rules/devexpress-patterns.md` before reaching for a MAUI stock control or a custom component. A DX control that covers 90% of the need beats a custom component written from scratch. Build custom only when DX has no equivalent.

---

> **This file is now an index** (split 2026-07-14 for token-scoped subagent reads). Read ONLY the section file(s) your task needs — never all of them. Inbound `§` references resolve via the table below.

| Section file | Covers |
|---|---|
| `crud-listview.md` | CrudListView — the standard list shell — CrudListView usage |
| `crud-migration-specfirst.md` | Page migration checklist + spec-first development — migration steps, spec-first for CRUD |
| `crud-appbar-list-toolbar.md` | App Bar, List Layout, FloatingToolbar — laws and variants — app bar variants, list layout, toolbar laws |
| `crud-form-page.md` | Form Page — laws and variants — form page laws |
| `crud-checklists.md` | ViewModel + Code-Behind checklists (list page) — VM checklist, code-behind checklist |
| `crud-supporting.md` | Confirm-Delete BottomSheet, Shimmer, DI Registration, Empty State — delete confirmation, skeleton, DI, empty state |
