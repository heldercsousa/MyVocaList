---
id: crud-list-selection-gestures
title: CRUD list selection gestures
status: ✅ Done
closed: 2026-11
target: 2026-11-11
section: DevCycleCraft
parent: crud-list-deduplication
goal: tap edits, long-press starts selection, tap toggles in selection mode; per-item Edit/Delete via modal bottom sheet; floating toolbar = Select all + Delete paired with FAB.
kind: change
---

# CRUD list selection gestures

tap edits, long-press starts selection, tap toggles in selection mode; per-item Edit/Delete via modal bottom sheet; floating toolbar = Select all + Delete paired with FAB.


Post-ship change to `crud-list-deduplication` (`CrudListView` + `CrudListViewModelBase`), applying to
all 4 CRUD list pages (Venues, People, Artists, Songs).

- `requirements.md` — ACs REQ-CLSG-01..13
- `design.md` — MD3 evidence (m3.material.io, rendered 2026-11-11) + technical approach
- `plan.md` — approved execution plan (Helder, 2026-11-11, autonomous grant)
- `tasks.md` / `task-log.md`

Supersedes `floatingtoolbar-visibility-verify` (toolbar now visible only in selection mode).
