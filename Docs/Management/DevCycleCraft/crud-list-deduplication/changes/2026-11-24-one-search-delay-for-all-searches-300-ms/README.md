---
id: one-search-delay-for-all-searches-300-ms
title: One search delay for all searches (300 ms)
status: ✅ Done
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: Every search in the app waits the same 300 ms after typing, taken from one shared constant instead of separate literals.
gate: Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
closed: 2026-11
kind: change
---

# One search delay for all searches (300 ms)

Every search in the app waits the same 300 ms after typing, taken from one shared constant instead of separate literals.

Found by the code-sharing research of 2026-11-24 ([findings.md](../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5, row B3).

## Decision

Helder allowed either 300 ms or 400 ms as the single delay, only if it makes sense. Choosing **300 ms**:

- The autocomplete fields already use a named, tested 300 ms constant. The CRUD list's 400 ms is an undocumented literal (`CrudListViewModelBase.cs:333`) with no requirement behind it.
- The searches run on local SQLite with full-text indexes, so a shorter wait does not overload anything.
- One constant means one place to change it, for every search.

> **Spec updated 2026-11-25:** every search waits `SearchConstants.AutocompleteDebounceMilliseconds` (300 ms) after the last keystroke, including the CRUD list (`CrudListViewModelBase.TriggerSearchDebounce`, previously a literal 400). The constant keeps its name because the DX field still references it; the name is a candidate for a rename after the DX removal. <!-- impl decision: the DX original still references the constant, so no rename in this change -->

## What to do

- Replace the 400 ms literal with the shared constant.
- The CRUD list tests wait the real 400 ms (`CrudListViewModelBaseTests.cs:108-141`, `LoadingHoldShimmerTests.cs`). Update the spec first, then the tests, so the change is not a silent test edit.
- Check on the device that 300 ms does not make the list shimmer on every keystroke. If it does, report it to Helder rather than picking another value.

No code changes until the DevExpress to Syncfusion migration lands: another session is migrating the same files, and a change now would conflict with it. Fix the Syncfusion versions (`CrudListViewSf`, `AutocompleteFieldSf`), not the DevExpress originals that the migration deletes.
