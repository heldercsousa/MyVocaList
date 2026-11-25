---
id: search-error-handling-short-message-for-users-full-detail-for-developers-only
title: "Search error handling: short message for users, full detail for developers only"
status: 💡 Pending
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: Search failures in the autocomplete fields and CRUD lists are logged with full detail and never fail silently; users see only a short, plain message.
gate: Helder and Claude Code agree the approach in a discussion. Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
kind: change
---

# Search error handling: short message for users, full detail for developers only

Search failures in the autocomplete fields and CRUD lists are logged with full detail and never fail silently; users see only a short, plain message.

Found by the code-sharing research of 2026-11-24 ([findings.md](../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5, row B5).

## Problem

Search failures other than cancellation become unobserved tasks in the DevExpress autocomplete debounce and in the CRUD list debounce (`CrudListViewModelBase.cs:329-337`, `CrudListPageBase.cs:77`). They are logged only when the garbage collector finalizes the task, the user sees nothing, and the list can stay empty or stale. `LoadMoreAsync` and the YouTube search already log their failures.

## Helder's direction (2026-11-24)

- Users must not see verbose errors. They get a short, plain message at most.
- Full error detail is for developers only.
- Helder and Claude Code discuss and agree the approach before any code is written.

## Topics for the discussion

- Where the full detail goes (Serilog log, debug-only display, or both).
- What the user sees in an autocomplete field, which may not show a "no results" row or a loading indicator (Helder's ruling of 2026-11-11), and what the user sees in a CRUD list.
- Whether the existing `GlobalExceptionHandler` categories cover it.

No code changes until the DevExpress to Syncfusion migration lands: another session is migrating the same files, and a change now would conflict with it. Fix the Syncfusion versions (`CrudListViewSf`, `AutocompleteFieldSf`), not the DevExpress originals that the migration deletes.
