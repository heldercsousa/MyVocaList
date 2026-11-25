---
id: BUG-089
title: "CRUD list search: load-more and pull-to-refresh can overwrite newer search results (suspected race)"
status: 🟡 In Progress
severity: Major
target: 2026-11-24
section: BusinessFeatures
parent: cross-cutting
goal: A list search always shows the rows for the latest query; an older load-more page or refresh never replaces or joins them.
gate: Confirm with a failing test first. Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
kind: bug
---

# CRUD list search: load-more and pull-to-refresh can overwrite newer search results (suspected race)

A list search always shows the rows for the latest query; an older load-more page or refresh never replaces or joins them.

Found by the code-sharing research of 2026-11-24 ([findings.md](../../../../DevCycleCraft/crud-list-deduplication/changes/2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5, rows B6, B7 and B8). Registered by Helder's decision of 2026-11-24 as its own bug, not nested under another item.

## Suspected defects

- **B6:** a load-more page from the old query can be appended after a new search has replaced the rows (`CrudListViewModelBase.cs:272-316`).
- **B7:** pull-to-refresh has no cancellation token and no generation check, so a slow refresh can overwrite newer search results (`CrudListViewModelBase.cs:262-267`).
- **B8 (low):** the stale check runs before the UI dispatch, so stale rows can pass briefly, probably hidden under the shimmer (`CrudListViewModelBase.cs:209, 214`).

None of these has been reproduced; they come from reading the code.

## Severity

Registered as Major because the user cannot tell that the rows are stale, and may open, edit or delete an item that does not match the search. If the failing test shows the effect is only cosmetic, downgrade it to Minor.

## How to fix

1. Write a failing test that reproduces B6 and one for B7 (Red), using the existing `ManualTimeProvider` or `TaskCompletionSource` to control timing.
2. Put load-more and refresh under the same generation guard the search already uses (Green).

No code changes until the DevExpress to Syncfusion migration lands: another session is migrating the same files, and a change now would conflict with it. Fix the Syncfusion versions (`CrudListViewSf`, `AutocompleteFieldSf`), not the DevExpress originals that the migration deletes.
