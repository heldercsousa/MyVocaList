---
id: timing-tests-for-the-autocomplete-search
title: Timing tests for the autocomplete search
status: ✅ Done
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: The autocomplete's wait-then-search logic has behaviour tests, so later changes to it have a safety net.
gate: Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
closed: 2026-11
kind: change
---

# Timing tests for the autocomplete search

The autocomplete's wait-then-search logic has behaviour tests, so later changes to it have a safety net.

Found by the code-sharing research of 2026-11-24 ([findings.md](../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 6).

## Problem

The autocomplete's wait-then-search logic has no behaviour tests. The pick counter, the echo suppression, the mode recheck and the apply step are untested; only the pure logic and the 300 ms constant are.

## What to do

- Write characterization tests for the Syncfusion autocomplete (`AutocompleteFieldSf`) that pin its current behaviour: the wait, latest search wins, a pick cancels the pending search, and the result is applied.
- Control time with the existing `ManualTimeProvider` (`PlaceLookupGateTests.cs:155-222`) and the `TimeProvider` already registered in DI, so the tests do not wait in real time.
- These tests are the precondition for any later refactor of the search routine, including a shared helper.

No code changes until the DevExpress to Syncfusion migration lands: another session is migrating the same files, and a change now would conflict with it. Fix the Syncfusion versions (`CrudListViewSf`, `AutocompleteFieldSf`), not the DevExpress originals that the migration deletes.

## Decision (2026-11-25)

The internal test-only constructor added to the governed `AutocompleteFieldSf` is accepted: it changes no runtime behaviour, and its consumer map and risk table are in the task-log. The pick routing and non-empty result path need a XAML host and stay untested.

Decided by Claude Code on 2026-11-25 under Helder's autonomy grant of the same day ("proceed with every needed step"); Helder may revisit it.
