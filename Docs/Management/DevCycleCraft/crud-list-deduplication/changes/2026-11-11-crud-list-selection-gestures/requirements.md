# Requirements — CRUD list selection gestures

**Change folder:** `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-11-crud-list-selection-gestures/`
**Decision basis:** Helder's request 2026-11-11 (autonomous grant; plan approved 2026-11-11). MD3 evidence: `design.md § MD3 evidence`.
**Scope:** every `CrudListView` consumer — Venues, People, Artists, Songs. Behavior lives in shared code (`CrudListViewModelBase`, `CrudListView`, `CrudListPageBase`); pages change only their row selection control.

## Why / backward compatibility

- **Why:** editing took 3 gestures (select → Edit → form) and the toolbar's Edit button was only usable with exactly one item selected. Tap-to-edit plus long-press-to-select is the mobile multi-select convention.
- **Backward compatibility:**
  - Users: a tap no longer selects in browse mode; a long-press or the checkbox does.
  - Code: `ICrudListViewModel.EditSelectedCommand` and `CanEditSelected` are removed. `CrudListView.ItemTapCommand` is removed (grep 2026-11-11: no XAML consumer).
  - No data/schema impact.

## Terminology (MD3 + project)

- **Browse mode** — no list item selected (`IsSelectionMode == false`).
- **Selection mode** — ≥ 1 list item selected (`IsSelectionMode == true`).
- **Trailing selection control** — the list item's checkbox (MD3 Lists § Selection controls), component `ListItemCheckbox`.
- **Floating toolbar** paired with a **FAB** (MD3 Toolbars § Floating toolbar with FAB).
- **Item actions sheet** (project name) — an MD3 **modal bottom sheet** holding **menu items** for one list item. MD3: modal bottom sheets are an "alternative to inline menus … on mobile". It is opened by **press-and-hold** (MD3 Menus § Opening menus).

## User stories

- **US-1:** As an admin, I want a single tap on a list item to open its edit form, so editing is one gesture away.
- **US-2:** As an admin, I want to start multi-selection with a long-press and then extend it with simple taps, so batch delete stays fast.
- **US-3:** As an admin in selection mode, I want to edit or delete one specific item without clearing my selection first.
- **US-4:** As a developer, I want this behavior defined once in shared code, so every CRUD list page inherits it with no per-page logic.

## Acceptance criteria

Verification: **U** = unit test on `CrudListViewModelBase` (`CrudListViewModelBaseTests`); **D** = Helder's on-device checklist (`plan.md § Verification`), because the behavior is native DX gesture/visual behavior.

| ID | Criterion (EARS) | Verif. |
|----|------------------|--------|
| REQ-CLSG-01 | When the user taps a list item in browse mode, the system shall navigate to that item's edit form and shall not select it. | U |
| REQ-CLSG-01b | When the VM receives a tap in selection mode, the system shall not navigate. | U |
| REQ-CLSG-02 | When the user long-presses a list item in browse mode, the system shall select exactly that item and enter selection mode (`IsSelectionMode`, `SelectedCount == 1`, the item in `SelectedItems`). | U |
| REQ-CLSG-03 | When the user taps a list item in selection mode, the system shall toggle that item's selected state (DX native `Multiple` selection). | D |
| REQ-CLSG-04a | When the selected count becomes 0 through a selection change from the view (`OnSelectionChanged(0)`), the system shall leave selection mode. | U |
| REQ-CLSG-04b | When the Select all toggle clears the selection, the system shall leave selection mode. | U |
| REQ-CLSG-04c | When a confirmed delete completes, the system shall clear the selection and leave selection mode. | U |
| REQ-CLSG-04d | When the list reloads (refresh, search) in selection mode, the system shall clear the selection and leave selection mode (existing reload behavior). | U |
| REQ-CLSG-05a | When the user taps an unselected item's trailing selection control in browse mode, the system shall select it and enter selection mode, without navigating. | U |
| REQ-CLSG-05b | When the user taps an item's trailing selection control in selection mode, the system shall toggle that item; deselecting the last item leaves selection mode. | U |
| REQ-CLSG-06 | When the user long-presses a list item in selection mode, the system shall open the item actions sheet for that item (`ItemActionsTarget == item`, `ItemActionsSheetState == HalfExpanded`) and leave the selection unchanged. | U |
| REQ-CLSG-07 | When the user chooses **Edit** in the item actions sheet, the system shall hide the sheet, clear the target and navigate to the target item's edit form. | U |
| REQ-CLSG-08 | When the user chooses **Delete** in the item actions sheet, the system shall hide the sheet and open the delete confirmation for exactly that item. Confirming shall call `ExecuteDeleteAsync` with only that item. | U |
| REQ-CLSG-09 | The floating toolbar shall hold exactly two icon buttons, paired with the page FAB: **Select all** (`done_all_outlined`, description "Select all"; selects all items, or clears the selection when all are selected) and **Delete** (`delete_outlined`, description "Delete selected"; asks to confirm deleting all selected items). | D |
| REQ-CLSG-10 | While in browse mode, the system shall hide the floating toolbar and show only the FAB; while in selection mode, it shall show both. (Supersedes `floatingtoolbar-visibility-verify`.) | D |
| REQ-CLSG-11 | The floating toolbar's Select all toggle shall show its selected state with `SurfaceContainer` background and `OnSurface` icon (MD3 vibrant "toggle tonal"), never `Primary`. The Delete button shall have no selected-state highlight, so the FAB is the only `Primary`-emphasized action. | D |
| REQ-CLSG-12 | When the user presses back, the system shall, in order: close the open item actions sheet; else close the open confirm sheet; else, in selection mode, clear the selection; else perform the page's default back behavior (Venues keeps its exit confirmation). `HandleBack()` returns whether back was consumed. | U |
| REQ-CLSG-13 | Pages shall contain no gesture or selection logic. Each page declares only its row templates, using `ListItemCheckbox` as the trailing selection control (8 occurrences: 2 per page). | D (review) |
| REQ-CLSG-14 | While the Songs YouTube button or the Artists catalog button inside a row is tapped, the system shall run only that button's command (no navigation, no selection change). | D |

## Removed behavior (spec change)

- `EditSelectedCommand` / `CanEditSelected` (the toolbar's "Edit selected", enabled only with exactly one item selected) — removed. Replaced by REQ-CLSG-01 and REQ-CLSG-07. The tests encoding it (`PersonsViewModelTests.CanEditSelected_*`) are removed with this spec change; the `CanDeleteSelected_*` tests stay.
- Tap-to-select in browse mode — replaced by REQ-CLSG-01/02.
- Floating toolbar "always visible" — replaced by REQ-CLSG-10.

## Edge cases

- **Non-`TItem` parameter** to `ItemTapCommand`, `ItemLongPressCommand` or `ToggleItemSelectionCommand` → no-op (U, covered in the 01/02/05 tests).
- **Long-press while a sheet is open** → no-op: the modal scrim blocks list gestures (D).
- **Delete from the item actions sheet while other items are selected** → after the reload, the selection is cleared (REQ-CLSG-04c). Keeping the remaining selection is out of scope.
- **Load more in selection mode** → the selection is kept (`AddRange` does not clear it).
- **Empty list** → no gestures are possible; the toolbar stays hidden (browse mode).

## Out of scope

- `ListItem` component (governed) — unchanged.
- Songs YouTube / Artists catalog nested row buttons stay. They are an existing MD3 "secondary nested actions" deviation, noted only.
- Keeping the remaining selection after a delete.
