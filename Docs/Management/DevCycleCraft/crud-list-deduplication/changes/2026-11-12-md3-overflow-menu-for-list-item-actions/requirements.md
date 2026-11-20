# Requirements — MD3 overflow menu for list item actions

**Change folder:** `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-12-md3-overflow-menu-for-list-item-actions/`
**Decision basis:** Helder's request on 2026-11-20 (autonomous grant, plan approved 2026-11-20). DevExpress-first check and MD3 evidence: `design.md`.
**Scope:**
- Shared code: `CrudListViewModelBase`, `ICrudListViewModel`, `CrudListView`, and the new `ListItemOverflowButton`.
- Revision 2026-11-20b adds the reusable `Menu`, `OverflowMenuButton`, `IMenuHost` and `MenuItemModel` (`UI/Components/Menus/`, `UI/ViewModels/`). These are usable on any page.
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
- **Menu**: the MD3 vertical menu, standard color mapping, opened by the overflow icon button. Built on `DXPopup`. Since revision 2026-11-20b it is the reusable component `Menu`; `OverflowMenuButton` is the reusable overflow icon button, and `ListItemOverflowButton` wraps it for list rows.
- **`IMenuHost`**: a view that hosts one `Menu` for the buttons inside its templates (for example `CrudListView`).
- **`MenuItemModel`**: the UI-agnostic menu item descriptor (text, icon, command, parameter).
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
- **REQ-OVFM-11:** Menu items appear in this order: Edit, then any page-specific menu items, then Delete. *(Since 2026-11-20b the order is unit-tested through REQ-OVFM-18; the UI check remains a visual confirmation.)*
- **REQ-OVFM-12a:** *(Padding superseded by the 2026-11-20b amendment below: 12,0 plus CornerRadius 4.)* The `MenuItem` style sets these values from the MD3 vertical menu tokens (`design.md § Tokens`). This is a XAML property check:
  - `HeightRequest` 44
  - `Padding` 16,0
  - `IconIndent` 12
  - `IconWidth`/`IconHeight` 20
  - Label Large font values
  - text color On surface
  - icon color On surface variant
- **REQ-OVFM-12b:** The menu container sets `BackgroundColor` to Surface container low and `AllowShadow` to true. Its corner radius is 16 *(verified 2026-11-20b against the MD3 token and accepted by Helder in T7; see the amendment below)*.
- **REQ-OVFM-13a:** A tap outside the menu closes it. It does not navigate, delete or change the selection, even when the tap lands on another row.
- **REQ-OVFM-13b:** The system back gesture with the menu open closes the menu only: the page stays and the list mode is unchanged.
- **REQ-OVFM-14:** A menu opened from the last visible row is not cut off. It opens above the button or is otherwise repositioned. MD3: "If a menu is in a position to be cut off, it should automatically reposition".
- **REQ-OVFM-17:** Delete from the menu shows the delete confirmation sheet once, for the tapped row, after the menu has closed.

### Revision 2026-11-20b — shared Menu component + final MD3 revision (`design.md § Revision 2026-11-20b`)

- **REQ-OVFM-12a (amended):** `MenuItem` sets `Padding` to **12,0** (was 16,0) and adds `CornerRadius` 4. Every other value is unchanged. The icon stays 16dp from the container edge because the menu insets items by 4dp.
- **REQ-OVFM-12b (amended):** the container corner radius is 16. This is verified against the MD3 token "Menu container shape … 16dp" and was accepted by Helder in T7.
- **REQ-OVFM-18:** `ItemMenuItems` is Edit, then the page-specific menu items in their given order, then Delete. Each page item's command is `ExecuteItemMenuActionCommand` with the `ItemMenuAction` as its parameter. The list is built once. (VM, Level A)
- **Verification levels:** "XAML/code check" means a reviewer check by the verifier subagent against the diff, not an automated test. Items marked Level B have unit tests in `Tests/Unit/Components/`.
- **REQ-OVFM-19:** selecting a menu item runs its command first. The menu then closes only if it is still open and `CloseOnItemSelected` is true. A null command does not throw. (`MenuItemSelection`, Level B)
- **REQ-OVFM-20:** `OverflowMenuButton` resolves its menu in this order: the explicit `Menu` first, else the nearest ancestor `IMenuHost`, else none (warning logged, no action). (`MenuResolution`, Level B)
- **REQ-OVFM-21:** menu items have 4dp corners. The first item's top corners and the last item's bottom corners are 12dp; a single item has 12dp on all corners. The menu content padding is 4,2. (`MenuItemShape.For`, Level B; padding by reviewer check; then visual)
- **REQ-OVFM-22:** while a menu is open, the overflow icon button that opened it shows the pressed state layer, which clears when the menu closes. Other rows' buttons are unaffected. (manual)
- **REQ-OVFM-23:** when a menu opens, accessibility focus moves to the first menu item. (manual with TalkBack: the first item is announced on open; reviewer check of `SemanticExtensions.SetSemanticFocus`)
- **REQ-OVFM-24:** a menu item whose command cannot execute appears disabled rather than being removed. (reviewer check: `Menu` sets `IsEnabled` from `CanExecute` and refreshes on `CanExecuteChanged`. No current item can be disabled.)
- **REQ-OVFM-25:** `Menu` and `OverflowMenuButton` have no dependency on `CrudListView`, `ICrudListViewModel` or `CrudListPageBase`, so they can be used on any page: place a `Menu` and set `OverflowMenuButton.Menu="{x:Reference …}"`. (code check: no such reference in `UI/Components/Menus/`)

## Out of scope

- Moving the Artists catalog action and the Songs YouTube action into the menu (separate items).
- The selection-mode item actions sheet, which is unchanged.
- MD3 Expressive motion, menu groups, dividers, selected states, submenus, trailing text and badges.
- A button-owned menu (`OverflowMenuButton.Items` with a code-created DXPopup). It is deferred until a consumer exists and a device spike passes (`design.md § Revision 2026-11-20b`).
- ListItem slot and spacing changes (separate item under `m3-lists`).
