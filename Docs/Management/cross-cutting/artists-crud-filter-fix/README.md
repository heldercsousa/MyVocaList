---
id: artists-crud-filter-fix
title: "**Artists CRUD List filter issue**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 130
goal: "fix the Author/Performer FilterChipGroup not rendering in ArtistsPage — CrudListView filter slot hosting bug. Fixed; Helder gate: emulator smoke test of the chips."
pointer: cross-cutting/artists-crud-filter-fix/
---

# Artists CRUD List filter issue

Fixed 2026-06-14 (`fix/artists-filter-regression`, merged to develop). Root cause:
`CrudListView` hosted `FilterContent` in a bare `ContentPresenter`, which only renders inside a
`ControlTemplate` — so the Author/Performer chips never appeared. Fix: replaced with a
`ContentView` host. Only `ArtistsPage` uses the slot; other CRUD pages collapse gracefully.
⏳ Helder: emulator smoke test to confirm chips render + filter.

> Migrated from the 2026-06 archive row (T12a Wave M, F-1a batch 3). Goal text is reworded from
> the archived Notes cell (verbatim text tripped the file-path-beyond-pointer banned pattern via
> `workflow.md`); meaning preserved.
