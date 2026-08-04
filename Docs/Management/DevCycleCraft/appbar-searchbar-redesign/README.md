---
id: appbar-searchbar-redesign
title: **AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar**
status: ✅ Done
target: 2026-07-10
section: DevCycleCraft
goal: kill the bar-swap search toggle — persistent MD3 `SearchBar` hosted in `CrudListView`.
gate: Helder — confirm D-1 (SearchAppBar survives for 4 picker pages) + emulator smoke test before ✅.
pointer: DevCycleCraft/appbar-searchbar-redesign/
closed: 2026-08
order: 150
kind: feature
---

# AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar

Kill the bar-swap search toggle — persistent MD3 `SearchBar` hosted in `CrudListView`. Specs: `requirements.md`, `design.md`, `tasks.md`, `task-log.md`.

**Notes overflow (transcribed from the pre-migration BACKLOG row):** Decision + spec approved; spec-reviewer PASS.

> **Closed ✅ Done [2026-08-03].** Design and D-1 (SearchAppBar retained for the 4 picker
> pages) confirmed by Helder. `feature/persistent-searchbar` (T1-T11) merged into
> `develop`; `dotnet build` 0 errors and 513/513 tests green at merge. Guideline
> amendments to `crud-appbar-list-toolbar.md`, `m3-appbars.md` and
> `component-safety-gate.md` (which now lists `SearchBar` as a governed component) landed
> with the merge.
>
> **Carried forward, not part of this closure:** BUG-048 (CrudListView pagination reload)
> and BUG-049 (VenueFormPage post-save navigation), both found during the smoke test and
> both pre-existing rather than regressions of this feature -- see `task-log.md`.
