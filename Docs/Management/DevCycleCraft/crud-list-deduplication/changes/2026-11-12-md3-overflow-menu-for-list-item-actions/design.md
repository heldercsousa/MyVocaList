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
| Menu item label text | Roboto 14pt / line 20pt / tracking 0.1pt / weight 500 (= Label Large) | `FontFamily=RobotoMedium`, `FontSize=14` → see note |
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
- **Back ownership, revised 2026-11-20 after the T6 emulator finding:**
  - What we found: `DXPopup` shows the menu in its **own focused Android window**. `dumpsys window` lists a separate `ty=APPLICATION` window of 360×305, and that window is the IME input target. Back is delivered to that window, which has no back handler, so the press is swallowed. The page's `OnBackButtonPressed` is never called; a diagnostic log was silent.
  - DevExpress 25.2.4 has no back-related DXPopupBase member (checked against the XML docs).
  - Fix: while the menu is open, `CrudListView` registers a back handler **on the popup's window**. `itemMenu.Opened` registers it and `itemMenu.Closed` removes it. The handler routes to `ViewModel.HandleBack()`, so the ViewModel stays the single source of truth.
    - Android 13+ (API 33): `OnBackInvokedDispatcher` from `FindOnBackInvokedDispatcher()` on the popup content's platform view, registered with `PRIORITY_DEFAULT`.
    - API 24–32: a `KEYCODE_BACK` key listener on the popup content's root platform view, made focusable and focused.
  - The code is Android-only, under `#if ANDROID`, in `CrudListView.xaml.cs` or a small `Platforms/Android` helper. It is UI plumbing with no business logic.
- Original text: the page's `OnBackButtonPressed` → `ViewModel.HandleBack()` stays the single owner. If on device DXPopup consumes the back press itself (closing through TwoWay), the outcome still satisfies REQ-OVFM-13b, but only if the page does not *also* pop. T6 verifies that a single back press with the menu open leaves the page in place. If the page pops, stop and escalate to Helder.

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

---

## Revision 2026-11-20b — shared Menu component + final MD3 revision (unshipped; in-place)

Trigger: Helder's T7 feedback. "It's a button that will certainly be used in other places, so must not be hard-coded inside the crudlistview. It must be a shared component ready to usage anywhere in the UI." This section **supersedes** § Component decision "Chosen: no menu component", and the parts of § CrudListView and § ListItemOverflowButton named below. Everything else above still holds.

### Final MD3 revision (Playwright + Chrome extension crawl, 2026-11-20; token table read with "Expand all" + "Hide previews")

| Claim | Verbatim | URL | Impact |
|-------|----------|-----|--------|
| Container shape | "Menu container shape … Rounded corners … 16dp" | …/menus/specs (Menus - Common › Shape / Default) | Confirms Helder's T7 acceptance. `CornerRadius=16` is now verified. |
| Item shapes | "Menu item shape … 4dp" · "Menu item first child shape … 12dp" · "Menu item first child inner corner corner size … 4dp" · "Menu item last child shape … 12dp" · "Menu item last child inner corner corner size … 4dp" | …/menus/specs | **Delta.** Items get 4dp corners. The first item's top corners and the last item's bottom corners are 12dp (REQ-OVFM-21). |
| Group / inset | "Menu group padding 2dp" · measurements diagram (alt "Vertical menu marked with spacing and padding measurements."): items inset **4** from the container sides and **2** top/bottom, with **12** from the item edge to the icon | …/menus/specs | **Delta.** Container content `Padding="4,2"`, item `Padding="12,0"`. The icon still sits 16dp from the container edge, which matches "Menu item leading space 16dp". |
| Elevation | "Menu container elevation … 3dp" | …/menus/specs | `AllowShadow=True` (DX default shadow; no numeric elevation API). |
| Layout / type (re-confirmed) | "Menu item height 44dp" · "Menu item between space 12dp" · "Menu item leading icon size 20dp" · label "Roboto · 20pt · 14pt · 0.1pt · 500" | …/menus/specs | Unchanged. |
| Trigger pressed while open | "When a menu is opened, the corresponding button or icon button should remain the same visually, with the addition of a pressed state." · alt "The overflow icon remains the same, even after the menu is opened." | …/menus/guidelines | **Delta** (REQ-OVFM-22). |
| Disabled, not removed | "When a menu item can only be used under specific conditions, it should appear disabled rather than be removed." | …/menus/guidelines | **Delta** (REQ-OVFM-24). |
| Initial focus | "When a menu opens, focus should be placed on the first menu item." | …/menus/accessibility | **Delta** (REQ-OVFM-23). |
| Exit | "Selecting an option · Tapping Escape or outside of the menu · Using the system back button" | …/menus/accessibility | Already met (REQ-OVFM-13a/13b; Helder T7). |
| Labels | "The accessibility label should be the same as the menu item text." · "the icon's accessibility label should be marked as decorative" | …/menus/accessibility | The DXButton's content is its label, and the icon is not separately focusable. No change. |
| Targets | "Targets should be 48x48dp or larger" (under *slot* accessibility) | …/menus/guidelines + accessibility | Applies to slot elements. The menu item keeps the 44dp height token; the full-width row exceeds 48dp wide. Noted, no change. |
| Toolbar vs menu | "Use a menu to show a temporary set of actions. To show actions on screen at all times, use a toolbar instead" | …/menus/overview | Fits the overflow use. |
| Bottom sheet on compact | "Consider adapting menus into bottom sheets on small screens." | …/menus/guidelines | Unchanged decision (advice, not rule; see § MD3 evidence). |
| Motion | "Menus use an enter and exit transition." | …/menus/guidelines | DXPopup's built-in open/close animation. Expressive motion stays out of scope. |

### Component decision (revised)

- **Names (W9).** The `MenuItem` style key and `MenuItemModel` do not clash with `Microsoft.Maui.Controls.MenuItem`: a style key is a string, and the record has a different name. The implementor confirms at build time that `Menu` does not collide with a type in the global usings. On a collision, qualify the name; do not rename it.
- **`Menu`** — `UI/Components/Menus/Menu.xaml(.cs)`, namespace `MyVocaList.UI.Components.Menus`, XAML prefix `menus`. The MD3 vertical menu, standard colours. A ContentView that wraps **one** `DXPopup`. It owns:
  - the chrome: 16dp corners, shadow, Surface container low, 112–280 width, `Padding="4,2"`;
  - the item template: a `MenuItem`-style DXButton per `MenuItemModel`, with the first/last child corners (REQ-OVFM-21);
  - the Android back route: `PopupBackHandler` attached on Opened and detached on Closed, **moved out of `CrudListView`**. The default action closes the menu (`IsOpen=false`). For a TwoWay-bound host, the ViewModel's `OnIsItemMenuOpenChanged(false)` then clears its state, so REQ-OVFM-13b's outcome is unchanged;
  - initial accessibility focus on the first item when the menu opens (REQ-OVFM-23).
  - Bindable: `ItemsSource` (`IEnumerable<MenuItemModel>`), `IsOpen` (TwoWay default, mirrors the popup's live `IsOpen`), `PlacementTarget` (View), `CloseOnItemSelected` (default true).
  - API: `Show(View anchor)`, `Close()`, events `Opened` and `Closed`.
  - Item selection rule (REQ-OVFM-19): **run the item's command first, then close only if still open.** Closing first would clear `ItemActionsTarget` through the TwoWay write-back before Edit, Delete or a page action reads it. The rule is a pure helper, `MenuItemSelection.Select(...)` (Level B tests).
  - If the class name `Menu` collides with a framework type in this project's global usings, keep the MD3 name and qualify it. Do not rename it.
- **`OverflowMenuButton`** — `UI/Components/Menus/OverflowMenuButton.xaml(.cs)`. The MD3 icon button with `more_vert_outlined`: `StandardIconButton`, 48×48, `SemanticProperties.Description` default "More options".
  - Bindable: `Menu` (explicit; `x:Reference` from anywhere on a page), `Command` / `CommandParameter`, `Description`.
  - On tap it resolves the menu: (1) the explicit `Menu`, else (2) the nearest ancestor `IMenuHost.HostedMenu`. This replaces the hard-coded `CrudListView` parent walk. If neither is found, it logs a warning and does nothing.
  - It then sets `menu.PlacementTarget = this`. If `Command` is set, it runs `Command.Execute(CommandParameter ?? BindingContext)`; the ViewModel opens the menu through the `IsOpen` binding. Otherwise it calls `menu.Show(this)`.
  - The resolution order is a pure helper, `MenuResolution.Resolve(explicitMenu, parentChain)` (Level B tests, REQ-OVFM-20).
  - Pressed while open (REQ-OVFM-22): while the resolved menu is open **and** this button is its `PlacementTarget`, the button shows the pressed state layer (On surface variant at 10% opacity, the MD3 pressed-state opacity). It clears on `Closed`.
- **`IMenuHost`** — `UI/Components/Menus/IMenuHost.cs`, with `Menu? HostedMenu { get; }`. `CrudListView` implements it.
- **`MenuItemModel`** — `UI/ViewModels/MenuItemModel.cs`, `public sealed record MenuItemModel(string Text, string Icon, ICommand Command, object? CommandParameter = null);`. It is a UI-agnostic descriptor (only `System.Windows.Input`). `ItemMenuAction` stays as the authoring type for derived ViewModels; it cannot be bound to a button.
- **`ListItemOverflowButton`** (governed) stays as a **thin wrapper** around `OverflowMenuButton`, so Venues and People need no edits. It keeps the selection-mode `IsVisible` binding and binds `Command` to `CrudListPageBase.BindingContext.OpenItemMenuCommand`, with `CommandParameter={Binding .}`. Its code-behind keeps only the constructor.
- **`CrudListView`** (governed) becomes a consumer.
  - It replaces the inline `DXPopup` with `<menus:Menu x:Name="itemMenu" IsOpen="{Binding IsItemMenuOpen, Mode=TwoWay}" ItemsSource="{Binding ItemMenuItems}" Closed="OnItemMenuClosed"/>` and implements `IMenuHost`.
  - It deletes `OnItemMenuOpened`, `_menuBackRoute`, its back callback and `ShowItemMenu`.
  - It **keeps** the Delete → confirm hand-off (`_showConfirmWhenMenuClosed`), which reads `itemMenu.IsOpen`.
- **Deferred (YAGNI): a button-owned menu** (`Items` on `OverflowMenuButton`, a code-created DXPopup). The DevExpress docs do not confirm that a DXPopup outside the visual tree renders on Android or inherits BindingContext. That needs a device spike, and there is no consumer yet. Any page can already use the menu by placing a `Menu` and referencing it.
- Rejected again: a per-row DXPopup in each template (§ Component decision).

### Signatures (binding; spec-review B1)

```csharp
namespace MyVocaList.UI.Components.Menus;

public interface IMenuHost { Menu? HostedMenu { get; } }

public partial class Menu : ContentView            // wraps one dx:DXPopup
{
    // Bindable properties (name / type / default)
    public IEnumerable<MenuItemModel>? ItemsSource { get; set; }   // null
    public bool IsOpen { get; set; }                               // false, BindingMode.TwoWay default
    public View? PlacementTarget { get; set; }                     // null
    public bool CloseOnItemSelected { get; set; }                  // true
    public void Show(View anchor);   // PlacementTarget = anchor; IsOpen = true
    public void Close();             // IsOpen = false
    public event EventHandler? Opened;
    public event EventHandler? Closed;   // raised for every close path (outside tap, back, item, IsOpen=false)
}

public partial class OverflowMenuButton : ContentView
{
    public Menu? Menu { get; set; }                  // null
    public ICommand? Command { get; set; }           // null
    public object? CommandParameter { get; set; }    // null
    public string Description { get; set; }          // "More options"
}

// Pure helpers (no MAUI handlers; unit-testable in Tests/Unit/Components/)
public static class MenuItemSelection
{
    // Runs item.Command (when CanExecute(item.CommandParameter)), THEN closes if closeOnItemSelected && isOpen().
    public static void Select(MenuItemModel item, Func<bool> isOpen, Action close, bool closeOnItemSelected);
}
public static class MenuResolution
{
    // explicitMenu ?? first non-null hostMenu(a) over ancestors (nearest first); null when none.
    public static TMenu? Resolve<TMenu>(TMenu? explicitMenu, IEnumerable<object> ancestors, Func<object, TMenu?> hostMenu)
        where TMenu : class;
}
public static class MenuItemShape
{
    // count == 1 → 12 all; index 0 → (12,12,4,4); index count-1 → (4,4,12,12); otherwise 4.
    // Microsoft.Maui.CornerRadius order: TopLeft, TopRight, BottomLeft, BottomRight.
    public static CornerRadius For(int index, int count);
}
```

`namespace MyVocaList.UI.ViewModels; public sealed record MenuItemModel(string Text, string Icon, ICommand Command, object? CommandParameter = null);`

### Mechanisms (spec-review B2, W2–W6)

- **Per-corner shape (REQ-OVFM-21).** `DXButton` inherits `DXViewBase.CornerRadius`, whose type is `Microsoft.Maui.CornerRadius` (Context7 `/websites/devexpress_maui`: "public CornerRadius CornerRadius { get; set; }"; the DX docs also show per-corner XAML `CornerRadius="20,0,0,0"`). `Menu` builds its item buttons in code-behind whenever `ItemsSource` changes. This is view plumbing, not a `BindableLayout`, because each button needs its index. Each item is a `DXButton` with `Style=MenuItem`, `Text`, `Icon`, `Command`, `CommandParameter` and `CornerRadius = MenuItemShape.For(i, n)`. A tap calls `MenuItemSelection.Select`, so the button's own `Command` is not set; this keeps the selection order in a single place.
- **Disabled, not removed (REQ-OVFM-24).** `Menu` sets `button.IsEnabled = Command.CanExecute(CommandParameter)` and refreshes it on `CanExecuteChanged`, which does not depend on DXButton command semantics. It unsubscribes when the items are rebuilt. No current item can be disabled (Edit and Delete always execute), so verification is a reviewer check of this code.
- **Pressed while open (REQ-OVFM-22).** `OverflowMenuButton` sets its DXButton `BackgroundColor` to the pressed state layer, On surface variant at 10% opacity (the MD3 pressed state-layer opacity), when it opens a menu, and subscribes once to that menu's `Closed` event. The `Closed` handler restores `Transparent` and unsubscribes. `Closed` fires for every close path, including the TwoWay write-back, because `Menu` raises it from the popup's own `Closed` event.
- **Initial focus (REQ-OVFM-23).** On `Opened`, `Menu` calls `SemanticExtensions.SetSemanticFocus(firstButton)` (Microsoft.Maui). Manual step: with TalkBack on, open the menu, and the first item is announced.
- **Back ownership (W5).** `Menu` owns back while it is open: `PopupBackHandler` → `Close()`. When a ViewModel binds `IsOpen` TwoWay, as CrudListView does, the write-back runs `OnIsItemMenuOpenChanged(false)` and clears `ItemActionsTarget`; REQ-OVFM-06 and 13b still hold. `ICrudListViewModel.HandleBack()`'s menu branch now serves only a page-level back (for example a page that receives back first). It stays as defence in depth. For hosts that bind no ViewModel, `Menu` is the only owner of back.
- **Selection timing (W6).** `MenuItemSelection.Select` runs the command first. `EditItemCommand`, `DeleteItemCommand` and `ExecuteItemMenuActionCommand` capture `ItemActionsTarget` and call `CloseItemActions()` synchronously, before their first `await`. Once the command returns, `isOpen()` is false and `Select` does not close again. The existing REQ-OVFM-03, 04 and 08 tests cover target capture. The new test `Select_RunsCommandBeforeClose` fails if close is called first.

### Contract additions

- `ICrudListViewModel.ItemMenuItems { get; }`: `IReadOnlyList<MenuItemModel>`, built once in `CrudListViewModelBase`:
  - `Edit → EditItemCommand`;
  - then each `ItemMenuActions` entry → `(ExecuteItemMenuActionCommand, action)`;
  - then `Delete → DeleteItemCommand`.
  - The order moves from XAML into the ViewModel, where it is tested (REQ-OVFM-18; REQ-OVFM-11 now verified there).
  - Text "Edit"/"Delete", icons `edit_outlined`/`delete_outlined`.
- Every other menu member and its behaviour is unchanged. All existing REQ-OVFM tests stay valid.

### Styles

- `MenuItem` (DXButton): `Padding` changes from `16,0` to `12,0` and gains `CornerRadius=4`. Everything else is unchanged.
- Optional `MenuSurface` values are allowed if the chrome would otherwise be hard-coded twice. Only `Menu` uses them today.

### Governance (revision 2026-11-20b)

Gates for the governed components touched (`CrudListView`, `ListItemOverflowButton`):
1. Dedicated tasks, one per governed component, as no-bundling requires: **T11b** `CrudListView` and **T11c** `ListItemOverflowButton`. Each has its own task-log entry with the map and the risk rows. The new components and the VM addition (**T11a**) are not governed-component changes. The MD3 review is above.
2. Consumer map: the table below, from a grep on 2026-11-20.
3. Per-consumer risk: the same table.
4. **Helder approval:** recorded 2026-11-20, from his T7 follow-up message in session: "It must be a shared component ready to usage anywhere in the UI … proceed with coding accordingly … Do all autonomously … you're authorized to decide anything".

| Consumer | Component | What could break | Verification |
|----------|-----------|------------------|--------------|
| `CrudListView` (owner) | `Menu` | The back route is lost. The confirm hand-off breaks if `Menu.IsOpen` is stale. The target is cleared if the menu closes before the command runs. | Emulator: with the menu open, back closes it and the page stays. Delete shows the confirm sheet once, after the menu closes. Edit opens the tapped row. |
| `VenuesPage.xaml` | `ListItemOverflowButton` → `OverflowMenuButton` | The ancestor `IMenuHost` lookup through DX cells; anchoring; the pressed state is not cleared | Emulator: first, middle and last row; the menu is anchored; the pressed state shows while open and clears on close |
| `PeoplePage.xaml` | same | same | same |
| `ArtistsPage.xaml`, `SongsPage.xaml` | `CrudListView` without the button | XAML regression only | Emulator: the list renders, the trailing buttons work, back is unchanged |

**Classification (spec-review W1).** `Menu` is consumed by `CrudListView`, which 4 pages use. `OverflowMenuButton` is consumed by `ListItemOverflowButton`, which 2 pages use. Counted transitively, both already have 2+ pages, so they are **governed from creation**. They are registered in the governed table of `component-safety-gate.md`, with their usage contract, in the T11a commit. Creating them needs no four gates, because they have no prior version. Every later change does.
