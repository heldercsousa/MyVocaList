# Design — MD3 overflow menu for list item actions

ACs: `requirements.md`. Governed components touched: `CrudListView` (four gates below). New component: `ListItemOverflowButton`. It becomes governed with its second consumer (People) and is added to `component-safety-gate.md` in the same commit.

## DevExpress-first check (Context7 `/websites/devexpress_maui`, package 25.2.4, 2026-11-20)

| Need | DevExpress offer | Verdict |
|------|------------------|---------|
| Menu control | **None.** There is no DXContextMenu, popup menu or overflow-menu control, and none appears in the demo app code (`DXContextMenu`: 0 hits). | — |
| Anchored temporary surface | `DXPopup` with `PlacementTarget`: "Sets the PlacementTarget property to a button so the popup appears relative to it." It opens below the target by default, with `Placement`, `HorizontalAlignment`/`VerticalAlignment`, `CornerRadius`, `AllowShadow`, `IsOpen` and a `Closed` event. "If you use the PlacementTarget property, the DXPopup is displayed without its scrim." | **Use** |
| Menu built from it | DevExpress's own scenario, "Context Menu Actions in Popup" (docs.devexpress.com/MAUI/404342): "a context menu (based on the DXPopup control) for CollectionView items. The context menu includes two Buttons." | **Follow** |
| Menu item | `DXButton` with `Icon` + `Content`, start-aligned. Already used this way by the `BottomSheetMenuItem` style (`MaterialStyles.xaml`). | **Use** (new style) |

## Component decision (supersedes README "Decision 2026-11-12: MenuItem on the shared slot base")

- **Chosen: no menu component.**
  - The menu is one `DXPopup` hosted in `CrudListView`.
  - Each menu item is a `DXButton` with a new `MenuItem` style.
  - Only one view is new: `ListItemOverflowButton`.
- **Rejected: a `MenuItem` ContentView on a `SlotItemBase` shared with `ListItem`.**
  - Its extra parts (trailing text, badge, slot collapse, selected state) serve no current menu item. Every menu item today is icon + label, which a DXButton already renders.
  - It would also chain this item to the still-pending ListItem slot-base change.
  - Revisit only if a menu needs trailing text, a badge or selection.
- **Rejected: per-row `DXPopup` inside each DataTemplate.** It means one popup per realized cell plus duplicated XAML in 8 templates.
- **Rejected: plain DXButton in each row template, no component.** The 4 ItemTemplates would repeat the icon, style, accessible name, the selection-mode visibility binding and the anchor hand-off. The earlier README set exactly this as the condition for a reusable view.

## MD3 evidence (Playwright crawl of m3.material.io, 2026-11-20; captures in the session scratchpad)

| Claim | Verbatim quote | URL |
|-------|----------------|-----|
| An overflow menu is a menu use case | "Use menus in situations that need extra actions, like: Overflow menus · Text field dropdown menus · Select menus · Context menus" | m3.material.io/components/menus/guidelines |
| Opened by an icon button | "A menu should open when a person: Selects an element, such as an icon, button, or text field" · "Menus can open from many components, including icon buttons, split buttons, and text fields" | …/menus/guidelines · …/menus/overview |
| List items may carry an overflow menu | "List items can contain multiple actions at once, like selection, icon buttons, overflow menus, and more." · alt: "A 3-item song list where each item has 2 trailing icons: a bookmark and overflow menu." | …/lists/guidelines |
| Hidden in selection mode | "Multi-select list items: … Can't have secondary nested actions" | …/lists/guidelines |
| Placement and reposition | "It typically appears below, next to, or in front of the element that generates it." · "If a menu is in a position to be cut off, it should automatically reposition to appear to the left, right, or above the element that generates it." | …/menus/guidelines |
| Dismiss | Accessibility: "Tapping Escape or outside of the menu" closes it | …/menus/accessibility |
| Button look while open | "When a menu is opened, the corresponding button or icon button should remain the same visually, with the addition of a pressed state." | …/menus/guidelines |
| Vertical menu over baseline | "M3 expressive vertical menus are recommended for new designs." | …/menus/specs |
| Standard color mapping | "Standard: Surface-based" · vertical standard color roles include "Surface container low" (container) and "On surface" / "On surface variant" (label / icons) | …/menus/specs |
| Bottom-sheet alternative noted, not taken | "Consider adapting menus into bottom sheets on small screens." This is advice ("consider"), not a rule. Helder's item goal asks for an MD3 menu, and the superseded CrudListView item said "an MD3 menu, not a bottom sheet". | …/menus/guidelines |

### Tokens: vertical menu, "Menus - Common" › Layout / Typography (verbatim values)

This settles the two discrepancies left open in the README:
- **Item height:** 44dp for the vertical menu vs 48dp for the baseline menu.
- **Icon-to-label gap:** 12dp in both. The vertical menu's "Menu item between space" is 12dp, and the baseline value is "Padding between elements within a list item: 12dp". 8dp is not a menu token; 8dp is the vertical menu's item top and bottom space.

| Token | Value | Implementation |
|-------|-------|----------------|
| Menu item height | 44dp | `HeightRequest=44` |
| Menu item top / bottom space | 8dp / 8dp | absorbed by the 44dp height with a 20dp label line |
| Menu item leading / trailing space | 16dp / 16dp | `Padding="16,0"` |
| Menu item between space | 12dp | DXButton `IconIndent=12` |
| Menu item leading icon size | 20dp | `IconWidth/IconHeight=20` |
| Menu item label text | Roboto 14pt / line 20pt / tracking 0.1pt / weight 500 (= Label Large) | `FontSize=14`, `FontAttributes=Bold` → see note |
| Menu group padding | 2dp | `Padding="0,2"` on the menu's item stack |
| Container color (standard) | Surface container low | `{StaticResource SurfaceContainerLow}` |
| Label / icon color | On surface / On surface variant | `TextColor=OnSurface`, `IconColor=OnSurfaceVariant` |
| Container elevation | token present, value not exposed in the table | `AllowShadow=True` (DX default shadow) |
| Container shape | token `md.comp.menus.container.shape` present, **value not exposed in the crawled table** | **Claude's call (unverified):** `CornerRadius=16`. The vertical menu has "round corners", unlike the baseline menu's 4dp. To be confirmed by Helder on device. |
| Container width | the vertical value is not exposed; baseline is "112dp min, 280dp max" | `MinimumWidthRequest=112`, `MaximumWidthRequest=280` |

Note on weight 500: MAUI has no Medium weight on `FontAttributes`, and DXButton takes no `StyleClass`. The builder reads the `Label.Large` style class in `MaterialStyles.xaml` and copies its font setters (`FontFamily`, `FontSize`, `FontAttributes`, `CharacterSpacing` where present) onto the `MenuItem` DXButton style. The menu label then matches every other Label Large in the app.

Expressive motion: out of scope (memory rule: only if DevExpress provides it built-in, and DXPopup does not).

## Contracts

### `ICrudListViewModel` (additive)

```csharp
bool IsItemMenuOpen { get; set; }                     // TwoWay with DXPopup.IsOpen
IReadOnlyList<ItemMenuAction> ItemMenuActions { get; }
ICommand OpenItemMenuCommand { get; }                 // parameter: the list item
ICommand DismissItemMenuCommand { get; }
ICommand ExecuteItemMenuActionCommand { get; }        // parameter: ItemMenuAction
```

### `ItemMenuAction` (new, `UI/ViewModels/ItemMenuAction.cs`)

```csharp
public sealed record ItemMenuAction(string Text, string Icon, Func<object, Task> Execute);
```

### `CrudListViewModelBase<TItem>`

- `protected virtual IReadOnlyList<ItemMenuAction> BuildItemMenuActions() => [];` The public `ItemMenuActions` returns its result, cached on first read.
- `protected static ItemMenuAction MenuAction(string text, string icon, Func<TItem, Task> execute)` is a typed helper for derived ViewModels.
- `OpenItemMenu(object p)`: returns if `IsSelectionMode` or `p is not TItem`. Otherwise sets `ItemActionsTarget = item` and `IsItemMenuOpen = true`.
- `CloseItemActions()` (existing) now also sets `IsItemMenuOpen = false`. As a result `EditItemAsync`, `DeleteItem` and the new paths all close the menu. `DismissItemMenuCommand` calls it.
- `ExecuteItemMenuActionAsync(ItemMenuAction a)`: captures the target, calls `CloseItemActions()`, then `await a.Execute(target)` if the target is non-null. If `Execute` throws, the exception propagates to `GlobalExceptionHandler` through the async command. The menu is already closed.
- `HandleBack()`: a new first branch, `if (IsItemMenuOpen) { CloseItemActions(); return true; }`, placed before the sheet branch.
- `OnIsItemMenuOpenChanged(false)` (the TwoWay write-back) **always** sets `ItemActionsTarget = default`. This is safe for Delete and page actions, because both capture the target before closing.
  - Closing is idempotent. When the VM sets false, the binding pushes false to the popup. The popup's `Closed` write-back then finds false already set, so the property does not change and nothing re-runs.
- `OnItemTappedAsync` and `OnItemLongPressed` each get a new first branch: `if (IsItemMenuOpen) { CloseItemActions(); return; }` (REQ-OVFM-15/16). Without a scrim, an outside tap may reach the row beneath the popup. This guard makes that tap only dismiss the menu, and it also covers the case "long-press another row while the menu is open".

### Invariants

- `ItemActionsTarget != null` ⇔ the menu is open (browse mode) **or** the item actions sheet is open (selection mode). The two are never open together: the menu opens only in browse mode, the sheet only in selection mode, and any row gesture while the menu is open closes it first.
- `IsItemMenuOpen` ⇒ `!IsSelectionMode`.

### Edge cases

| Case | Behaviour |
|------|-----------|
| Double tap on the overflow button | The second `OpenItemMenu` re-sets the same target and leaves the menu open (idempotent). |
| Row scrolled or recycled while the menu is open | The scroll begins with an outside touch, so the popup closes. Verified in T6. |
| List reload (search text change) while the menu is open | The target is captured by value, so Edit/Delete act on the captured entity. Not specially handled. |
| `ExecuteItemMenuAction` with a null target | The action does not run. |
| Page-specific action throws | `GlobalExceptionHandler` handles it. The menu is already closed. |

### `CrudListView` (governed)

- XAML: one `DXPopup x:Name="itemMenu"`, using the prefix that CrudListView already maps to `DevExpress.Maui.Controls`, the namespace of `BottomSheet` and `DXPopup`:
  - `IsOpen="{Binding IsItemMenuOpen, Mode=TwoWay}"`, `AllowShadow`, `CornerRadius`, `BackgroundColor=SurfaceContainerLow`, `HorizontalAlignment="End"`.
  - Content: a `VerticalStackLayout` with Edit (`edit_outlined`, `EditItemCommand`), a `BindableLayout` over `ItemMenuActions` (each item → `ExecuteItemMenuActionCommand` bound through `Source={x:Reference root}, Path=BindingContext.…`, because the child's BindingContext is the `ItemMenuAction`; `CommandParameter={Binding .}`), and Delete (`delete_outlined`, `DeleteItemCommand`).
- Code-behind: `public void ShowItemMenu(View anchor, object item)` sets `itemMenu.PlacementTarget = anchor`, then executes `ViewModel.OpenItemMenuCommand` with `item`.
- **Delete → confirm hand-off (REQ-OVFM-17):** this mirrors the existing sheet hand-off at `CrudListView.xaml.cs` ~247-251.
  - When `ConfirmSheetState` becomes visible, check the popup. If `itemMenu.IsOpen` is still true, the menu is still on screen: set a `_showConfirmWhenMenuClosed` flag and defer the sheet. Otherwise show it at once.
  - `itemMenu.Closed` handler: if the flag is set, clear it and show the confirm sheet. It is the same flush as the existing sheet-hidden path.
  - The handler **never** executes `DismissItemMenuCommand`. The TwoWay `IsOpen` binding already writes false back, and that write-back is idempotent.
- **Back ownership:** the page's `OnBackButtonPressed` → `ViewModel.HandleBack()` stays the single owner. If on device DXPopup consumes the back press itself (closing through TwoWay), the outcome still satisfies REQ-OVFM-13b, but only if the page does not *also* pop. T6 verifies that a single back press with the menu open leaves the page in place. If the page pops, stop and escalate to Helder.

### `ListItemOverflowButton` (new, `UI/Components/Lists/`)

- ContentView with a `DXButton`:
  - `Style=StandardIconButton`, `WidthRequest/HeightRequest=48` (as `ListItemCheckbox`), `Icon=more_vert_outlined`.
  - `SemanticProperties.Description="More options"`.
  - `IsVisible="{Binding BindingContext.IsSelectionMode, Source={RelativeSource AncestorType={x:Type pages:CrudListPageBase}}, Converter={StaticResource InverseBoolConverter}}"`. This uses the same RelativeSource source as `ListItemCheckbox`'s command and the app's existing `InverseBoolConverter`.
  - This binding is the **only** guard on unselected rows in selection mode, because those rows still use `ItemTemplate`.
- On `Clicked`: walk `Parent` up to the first `CrudListView` and call `ShowItemMenu(this, BindingContext)`.
  - `RelativeSource AncestorType` already resolves through DX cells for `ListItemCheckbox`, so the parent chain reaches the host.
  - If no host is found, log a warning and do nothing.
- Asset: `Resources/Images/more_vert_outlined.svg` (Material Symbols "more_vert", outlined, 24-unit viewBox, same format as `edit_outlined.svg`).

### Pages

Venues and People `ItemTemplate`: the trailing slot becomes `HorizontalStackLayout Spacing=8` with `ListItemOverflowButton` followed by the existing `ListItemCheckbox`. This mirrors the Artists and Songs rows. `SelectedItemTemplate` is unchanged, because it is only used in selection mode, where the button is hidden.

## State machine

| State | Event | Next |
|-------|-------|------|
| Browse | overflow tap | Menu open (target = row) |
| Menu open | Edit | Browse → edit form |
| Menu open | Delete | Browse + confirm sheet (target only) |
| Menu open | page action | Browse, then the action runs |
| Menu open | outside tap / back | Browse (target cleared) |
| Selection | overflow | not reachable (button hidden) |

## Governance: CrudListView consumer map (grep `<views:CrudListView`, 2026-11-20)

Gates:
1. Dedicated task (T4) and the MD3 review (this file).
2. Consumer map (below).
3. Risk per consumer (below).
4. **Helder approval:** recorded 2026-11-20. He approved the plan in session ("tackle on … MD3 overflow menu for list item actions … You're authorized in advance to all the steps autonomously"), and that plan named the CrudListView change and its four gates.

The T4 task-log entry repeats this map and the risk table.

| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `UI/Pages/Venues/VenuesPage.xaml:26` | Popup host added to the shared view: layout or z-order of the FAB, toolbar or sheets; back handling | Emulator: open and close the menu, Edit, Delete, back in each state; FAB and toolbar unchanged |
| `UI/Pages/People/PeoplePage.xaml:25` | Same as Venues | Same as Venues |
| `UI/Pages/Artists/ArtistsPage.xaml:27` | No row button yet, so the popup never opens; risk limited to the added XAML and HandleBack branch | Emulator: list renders, catalog button works, back unchanged |
| `UI/Pages/Songs/SongsPage.xaml:26` | Same as Artists | Emulator: list renders, YouTube button works, back unchanged |

`ListItemOverflowButton` consumers after this change: Venues and People `ItemTemplate`. Risk: the button swallows the row tap, or it is visible in selection mode. Verify both on the emulator.

## Risks

- **DXPopup anchored inside a virtualized DXCollectionView cell.** This covers position, recycling while scrolling, and repositioning near the bottom (REQ-OVFM-14). If DX properties can't fix a problem, stop and escalate to Helder. Do not switch to the bottom sheet alone.
- **No scrim with `PlacementTarget`: an outside tap may reach the row beneath.** Mitigation is the VM guard (REQ-OVFM-15/16). T6 verifies REQ-OVFM-13a by tapping another row with the menu open.
- **Back press consumed by both the popup and the page.** Ownership is under § CrudListView. T6 verifies REQ-OVFM-13b, and an extra page pop is escalated.
