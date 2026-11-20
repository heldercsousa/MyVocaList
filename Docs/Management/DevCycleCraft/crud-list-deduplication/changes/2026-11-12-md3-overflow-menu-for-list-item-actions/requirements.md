# Requirements — MD3 overflow menu for list item actions

**Change folder:** `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-12-md3-overflow-menu-for-list-item-actions/`
**Decision basis:** Helder's request on 2026-11-20 (autonomous grant, plan approved 2026-11-20). DevExpress-first check and MD3 evidence: `design.md`.
**Scope:**
- Shared code: `CrudListViewModelBase`, `ICrudListViewModel`, `CrudListView`, and the new `ListItemOverflowButton`.
- Consumers: Venues and People rows.
- Artists and Songs adopt the menu in their own items:
  - `artists-list-rows-catalog-action-moves-into-the-overflow-menu`
  - `songs-list-rows-youtube-action-moves-into-the-overflow-menu-checkbox-moves-leading`

## Why / backward compatibility

- **Why:** in browse mode a row has no way to reach its own actions. Tap edits, long-press starts selection, and the only Edit/Delete surface (the item actions sheet) opens on press-and-hold in selection mode. MD3 lists support "icon buttons, overflow menus" inside list items, and an overflow menu is a documented menu use case.
- **Backward compatibility:**
  - Users: Venues and People rows gain a trailing "More options" icon button. Every existing gesture is unchanged. The selection-mode item actions sheet stays.
  - Code: additive only. No member is removed from `ICrudListViewModel`.
  - No data or schema impact.

## Terminology (MD3 + project)

- **Overflow icon button**: the row's trailing icon button with the `more_vert` icon (MD3 Icon buttons, Menus § Overflow menus). Component: `ListItemOverflowButton`.
- **Menu**: the MD3 vertical menu, standard color mapping, opened by the overflow icon button. Built on `DXPopup`.
- **Menu item**: one choice in the menu, with a leading icon and label text.
- **Page-specific menu item**: a menu item a page adds between Edit and Delete (for example Open catalog on Artists).
- **Browse mode / selection mode**: as defined in `../2026-11-11-crud-list-selection-gestures/requirements.md`.

## Acceptance criteria

### ViewModel (unit-testable, Level A)

- **REQ-OVFM-01:** In browse mode, `OpenItemMenuCommand` with a list item sets `ItemActionsTarget` to that item and `IsItemMenuOpen` to true. The selection is unchanged and stays empty.
- **REQ-OVFM-02a:** `OpenItemMenuCommand` does nothing in selection mode.
- **REQ-OVFM-02b:** `OpenItemMenuCommand` does nothing when the parameter is not a list item.
- **REQ-OVFM-03:** With the menu open, `EditItemCommand` closes the menu (`IsItemMenuOpen` false, `ItemActionsTarget` cleared) and navigates to the edit form of the target item.
- **REQ-OVFM-04:** With the menu open, `DeleteItemCommand` closes the menu and opens the delete confirmation for exactly the target item.
- **REQ-OVFM-05a:** `DismissItemMenuCommand` closes the menu and clears `ItemActionsTarget` without navigating or deleting.
- **REQ-OVFM-05b:** Setting `IsItemMenuOpen = false` directly clears `ItemActionsTarget` without navigating or deleting. This is the view's TwoWay write-back after an outside tap or the popup's own close. Closing an already-closed menu is a no-op.
- **REQ-OVFM-06:** With the menu open, `HandleBack()` closes the menu and returns true. It does not leave selection mode and does not close any other sheet in the same call.
- **REQ-OVFM-07:** `ItemMenuActions` is empty by default. A derived ViewModel can add page-specific menu items, and they keep the order it gives them.
- **REQ-OVFM-08:** With the menu open, `ExecuteItemMenuActionCommand` with a page-specific menu item closes the menu, then runs that item's action once, passing the target item.
- **REQ-OVFM-15:** With the menu open, `ItemTapCommand` only closes the menu. It does not navigate.
- **REQ-OVFM-16:** With the menu open, `ItemLongPressCommand` only closes the menu. It does not start selection. This also guards against a tap passing through the popup, which has no scrim, to the row beneath it.

### UI (manual, Android emulator, then Helder on device)

- **REQ-OVFM-09:** In browse mode every Venues and People row shows a trailing overflow icon button: a 48×48dp touch target, the `more_vert` icon, and the accessible name "More options". Tapping it opens the menu next to the button, and the row itself is not tapped (no navigation).
- **REQ-OVFM-10:** In selection mode the overflow icon button is hidden on every row, selected and unselected. MD3: multi-select list items "Can't have secondary nested actions".
- **REQ-OVFM-11:** Menu items appear in this order: Edit, then any page-specific menu items, then Delete.
- **REQ-OVFM-12a:** The `MenuItem` style sets these values from the MD3 vertical menu tokens (`design.md § Tokens`). This is a XAML property check:
  - `HeightRequest` 44
  - `Padding` 16,0
  - `IconIndent` 12
  - `IconWidth`/`IconHeight` 20
  - Label Large font values
  - text color On surface
  - icon color On surface variant
- **REQ-OVFM-12b:** The menu container sets `BackgroundColor` to Surface container low and `AllowShadow` to true. Its corner radius is confirmed by Helder in T7 (Claude's unverified call: 16).
- **REQ-OVFM-13a:** A tap outside the menu closes it. It does not navigate, delete or change the selection, even when the tap lands on another row.
- **REQ-OVFM-13b:** The system back gesture with the menu open closes the menu only: the page stays and the list mode is unchanged.
- **REQ-OVFM-14:** A menu opened from the last visible row is not cut off. It opens above the button or is otherwise repositioned. MD3: "If a menu is in a position to be cut off, it should automatically reposition".
- **REQ-OVFM-17:** Delete from the menu shows the delete confirmation sheet once, for the tapped row, after the menu has closed.

## Out of scope

- Moving the Artists catalog action and the Songs YouTube action into the menu (separate items).
- The selection-mode item actions sheet, which is unchanged.
- MD3 Expressive motion, menu groups, dividers, selected states, submenus, trailing text and badges.
- ListItem slot and spacing changes (separate item under `m3-lists`).
