---
id: crudlistview-one-list-mode-at-a-time-multi-action-browse-multi-select-selection-with-toolbar-edit-for-exactly-one
title: "CrudListView: one list mode at a time (multi-action browse, multi-select selection) with toolbar Edit for exactly one"
status: 💡 Pending
target: 2026-11-12
section: DevCycleCraft
parent: crud-list-deduplication
goal: "Browse: tap edits, long-press starts selection, trailing overflow icon button opens the item actions overflow menu (an MD3 menu, not a bottom sheet). Selection: tap and long-press toggle, overflow hidden (MD3: multi-select items can't have nested actions); toolbar is Select all, Edit (exactly one selected) and Delete."
gate: Four component-governance gates; Helder approves before coding.
kind: change
---

# CrudListView: one list mode at a time (multi-action browse, multi-select selection) with toolbar Edit for exactly one

Browse: tap edits, long-press starts selection, trailing overflow icon button opens the item actions overflow menu (an MD3 menu, not a bottom sheet). Selection: tap and long-press toggle, overflow hidden (MD3: multi-select items can't have nested actions); toolbar is Select all, Edit (exactly one selected) and Delete.


## Carried forward (2026-11-14)

- REQ-CLSG-06 deviation from the selection-gestures change: long-press in selection mode does nothing on device. This spec redefines it as a toggle, with no menu (see `../2026-11-11-crud-list-selection-gestures/task-log.md`, T5).
- Order: implemented after the ListItem change, the MD3 overflow menu and the overflow button. The four page tasks start after this one.
