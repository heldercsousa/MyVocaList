---
id: remove-dead-search-code-after-confirming-nothing-uses-it
title: Remove dead search code after confirming nothing uses it
status: ✅ Done
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: Search code that nothing on screen uses is removed, after a check confirms it has no real consumers.
gate: Starts after the DX to Syncfusion migration lands, to avoid conflicts with it.
closed: 2026-11
kind: change
---

# Remove dead search code after confirming nothing uses it

Search code that nothing on screen uses is removed, after a check confirms it has no real consumers.

Found by the code-sharing research of 2026-11-24 ([findings.md](../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5, rows B12 and B11).

## Candidates

- **B12:** `SongFormViewModel` `ArtistSuggestions`, `SearchArtistsCommand` and `_searchGeneration`. No XAML binds them; only the BUG-051 and BUG-056 tests use them.
- **B11:** the cancel-previous code in `YouTubeSearchViewModel.cs:51-58`, which `AllowConcurrentExecutions=false` makes unreachable.

## Before removing anything

Helder asked for proof that nothing uses the code. For each candidate, grep every XAML binding, `x:Reference`, command binding, code-behind reference and test, on `develop` and on the migration branches, and record the result in the task-log. Tests that only cover dead code are removed with it; a test that covers live behaviour stays.

## Same file, worth doing together

The same SongForm artist search has two small defects from the findings: **B1**, it drops its cancellation token (`SongFormViewModel.cs:522-523`), and **B2**, it uses the literal `maxResults: 5` instead of `SearchConstants.MaxSearchResults`. Fixing them here keeps the SongForm clean-up in one change.

No code changes until the DevExpress to Syncfusion migration lands: another session is migrating the same files, and a change now would conflict with it. Fix the Syncfusion versions (`CrudListViewSf`, `AutocompleteFieldSf`), not the DevExpress originals that the migration deletes.
