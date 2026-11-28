---
id: search-error-handling-short-message-for-users-full-detail-for-developers-only
title: "Search error handling: short message for users, full detail for developers only"
status: ✅ Done
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: Search failures in the autocomplete fields and CRUD lists are logged with full detail and never fail silently; users see only a short, plain message.
gate: Helder and Claude Code agree the approach in a discussion. Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
closed: 2026-11
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

## Approach

> **Spec updated 2026-11-25** (decided; see Decision below):
> - **Autocomplete fields (Sf):** nothing new for the user. `AutocompleteFilterSf` already catches a failed search, shows no options, and logs the full exception through Serilog (`LogSearchFailure`). No change.
> - **CRUD list search:** the debounced search catches every exception except cancellation, logs the full detail with `ILogger.LogError(exception, ...)`, and keeps the current rows. The shimmer ends through the existing `finally`.
> - **User message:** none. The CRUD list base has no user-message mechanism (no toast, snackbar or error property), so the failure is log-only. A short message needs a UI mechanism that does not exist yet; raising it is a separate decision.
> - No raw exception text reaches the UI in any path.

## Decision (2026-11-25)

Log-only is the chosen approach. Helder's direction allows a short plain message *at most*, so no message meets it, and full detail goes only to the log. A visible message would need a new UI mechanism (toast or error state) in the CRUD list, which is new behaviour: register it as its own item if Helder wants it.

Decided by Claude Code on 2026-11-25 under Helder's autonomy grant of the same day ("proceed with every needed step"); Helder may revisit it.
