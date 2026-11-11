# Design — CRUD list selection gestures

Requirements: `requirements.md` (REQ-CLSG-01..14). Versions (central package management, `Directory.Packages.props`, consumed by `MyVocaList.csproj`): Microsoft.Maui.Controls 10.0.41, DevExpress.Maui.* 25.2.4.

**Layers affected:** UI only — ViewModels (`CrudListViewModelBase`, `ICrudListViewModel`), components (`CrudListView`, `ListItemCheckbox`, `FloatingToolbar`), `CrudListPageBase`, 4 page XAMLs, `MaterialStyles.xaml`. No Domain / Infrastructure / Services / schema change.

## MD3 evidence

Source: m3.material.io pages rendered on 2026-11-11 with Playwright (npx cache) driving installed Edge, text taken from `main.innerText`; the site is JS-rendered. Raw captures are session scratch files and are not kept; the quotes below are verbatim. Pages: toolbars/{overview,guidelines,specs,accessibility}, app-bars/{overview,guidelines,specs,accessibility}, menus/{overview,guidelines}, lists/guidelines, bottom-sheets/guidelines.

| Question | Finding | Quote |
|---|---|---|
| Minimum number of floating-toolbar actions? | **None.** No minimum appears on any rendered toolbar page; the stated limits concern having too many controls. | "Be cautious of including too many controls as it can be overwhelming." · "All elements need a minimum 48x48dp target area to be accessible." · "The floating toolbar's container should be fully visible on screen." |
| Toolbar + FAB pairing | Allowed; documented as a configuration. | "A FAB can be placed next to a floating toolbar to present one high-priority action alongside a unified set of toolbar actions." · Specs: "On Jetpack Compose, floating toolbar with FAB is fully supported. On other platforms, each component needs to be added separately." |
| Contextual use | The floating toolbar fits selection actions. | "Floating toolbar — Floats above the body content. It's best used for contextual actions relevant to the body content or the specific page." · Vibrant "can also indicate a temporary change in the page behavior, such as entering edit mode." |
| Emphasis | Only one action is emphasized. | "Don't emphasize multiple buttons with bold, primary colors, such as a button and FAB together. Emphasize one action at a time." |
| Vibrant toggle colors | A selected toggle is not Primary. | Specs, Vibrant: "Primary container · Filled button (Primary, On primary) · Toggle tonal button: (Surface container, On surface) · Standard button (On primary container)". |
| Context menu trigger | Press-and-hold opens a menu. | Menus: "A menu should open when a person: … Performs a specific action to trigger the menu, like right-click or press-and-hold." · "Use menus in situations that need extra actions, like: … Context menus". |
| Menu on mobile | A modal bottom sheet replaces the menu. | Bottom sheets: "Use a modal bottom sheet as an alternative to inline menus or simple dialogs on mobile" · "can contain … menu items (in list or grid layouts)". |
| Multi-select list items | No extra nested action per row, so the actions go in the sheet rather than a per-row overflow button. | Lists: "Multi-select list items: Pair well with checkboxes and switches · Can't have secondary nested actions". "Trailing selection control - checkbox". |
| App bar as fallback | Not needed. | App bars: "1–2 essential actions" · "If the product has many actions, place those in a toolbar." |

**Decision:**
- The floating toolbar has 2 icon buttons (Select all toggle, Delete) and is paired with the FAB.
- Per-item Edit/Delete goes in an item actions sheet: a modal bottom sheet with menu items, opened by press-and-hold in selection mode.
- `dx:BottomSheet` also satisfies the "Native dialogs" constitutional constraint.

## Gesture state machine

| Mode | Tap item | Long-press item | Tap checkbox (`ListItemCheckbox`) |
|---|---|---|---|
| Browse (`IsSelectionMode=false`; DX `SelectionMode=None`) | VM `ItemTapCommand` → `NavigateToEditAsync(item)` | VM `ItemLongPressCommand` → `BeginSelection(item)` | VM `ToggleItemSelectionCommand` → `BeginSelection(item)` |
| Selection (`IsSelectionMode=true`; DX `SelectionMode=Multiple`) | DX native toggle → `SelectionChanged` → `OnSelectionChanged(count)`; VM `ItemTapCommand` is a no-op | VM `ItemLongPressCommand` → open item actions sheet | VM `ToggleItemSelectionCommand` → add/remove |

The checkbox is a `dx:DXButton`, which consumes the touch, so DX `Tap` does not fire for it. The Songs and Artists row buttons work the same way today.

## Invariants (after any settled operation)

- I1 `IsSelectionMode == (SelectedCount > 0)`. `OnSelectedCountChanged` sets `IsSelectionMode = value > 0`. The one exception is inside `BeginSelection`, which sets `IsSelectionMode = true` before the add so the view switches DX to `Multiple` first.
- I2 `SelectedCount == SelectedItems.Count`. Every VM mutation of `SelectedItems` sets `SelectedCount` in the same UI-thread block.
- I3 `ItemActionsTarget != null` ⇔ `ItemActionsSheetState != Hidden`.
- I4 DX `SelectionMode == (IsSelectionMode ? Multiple : None)`, and `collectionView.SelectedItems` is the VM's `SelectedItems` instance.

## Contracts

### `ICrudListViewModel` (full delta)

```csharp
// removed
bool CanEditSelected { get; }
IAsyncRelayCommand EditSelectedCommand { get; }

// added
bool IsSelectionMode { get; }
BottomSheetState ItemActionsSheetState { get; set; }   // view writes Hidden on swipe-dismiss
IAsyncRelayCommand<object> ItemTapCommand { get; }
IRelayCommand<object> ItemLongPressCommand { get; }
IRelayCommand<object> ToggleItemSelectionCommand { get; }
IAsyncRelayCommand EditItemCommand { get; }             // acts on ItemActionsTarget
IRelayCommand DeleteItemCommand { get; }                // acts on ItemActionsTarget
IRelayCommand DismissItemActionsCommand { get; }
bool HandleBack();                                      // REQ-CLSG-12; true = consumed
```

Grep 2026-11-11: `CrudListViewModelBase<TItem>` is the only implementer of `ICrudListViewModel`.

### `CrudListViewModelBase<TItem>`

```csharp
[ObservableProperty] private bool _isSelectionMode;
[ObservableProperty] private BottomSheetState _itemActionsSheetState = BottomSheetState.Hidden;
protected TItem ItemActionsTarget { get; private set; }        // default when none

partial void OnSelectedCountChanged(int value)  // existing; drop the CanEditSelected/EditSelectedCommand lines
    → ...; IsSelectionMode = value > 0; ...

private Task OnItemTappedAsync(object p)  => !IsSelectionMode && p is TItem i ? NavigateToEditAsync(i) : Task.CompletedTask;
private void OnItemLongPressed(object p)   // p not TItem → return
    → !IsSelectionMode ? BeginSelection(i) : OpenItemActions(i);
private void ToggleItemSelection(object p) // p not TItem → return
    → !IsSelectionMode ? BeginSelection(i)
      : RunOnUiThread(() => { if (!SelectedItems.Remove(i)) SelectedItems.Add(i); SelectedCount = SelectedItems.Count; });
private void BeginSelection(TItem i)
    → IsSelectionMode = true; RunOnUiThread(() => { SelectedItems.ReplaceRange([i]); SelectedCount = 1; });
private void OpenItemActions(TItem i) → ItemActionsTarget = i; ItemActionsSheetState = HalfExpanded;
private void CloseItemActions()       → ItemActionsSheetState = Hidden; ItemActionsTarget = default;
private async Task EditItemAsync()    → var t = ItemActionsTarget; CloseItemActions(); if (t is not null) await NavigateToEditAsync(t);
private void DeleteItem()             → var t = ItemActionsTarget; CloseItemActions(); if (t is not null) RequestDelete([t]);
private void RequestDelete(IList<TItem> items)   // replaces RequestBatchDelete; IList matches BuildDeleteConfirmMessage(IList<TItem>)
    → same body as today's RequestBatchDelete, using `items`;
      DeleteSelectedCommand → RequestDelete(SelectedItems.ToList())
public bool HandleBack()
    → if ItemActionsSheetState != Hidden: CloseItemActions(); return true
      if ConfirmSheetState != Hidden: DismissConfirmSheet(); return true
      if IsSelectionMode: RunOnUiThread(() => { SelectedItems.ClearRange(); SelectedCount = 0; }); return true
      return false
```

Removed: `EditSelectedCommand`, `CanEditSelected`, `ExecuteEditSelectedAsync`.

`ToggleItemSelection` mutates with `Remove`/`Add`: one item, one `CollectionChanged`. That is equivalent to one `ReplaceRange` per UI-thread block (code-style § UI Thread Performance). Gesture commands run on the UI thread, so `RunOnUiThread` executes inline there and in tests.

### `CrudListView` (governed — 4 consumers)

- **Gestures:** in XAML, `Tap="OnItemTap"` and `LongPress="OnItemLongPress"`. The handlers execute `_viewModel?.ItemTapCommand` / `ItemLongPressCommand` with `e.Item`.
- **Dead code removed:** the `ItemTapCommand` bindable property, its CLR wrapper, `OnItemTapCommandChanged` and `OnCollectionViewItemTapped`. Grep 2026-11-11 found no XAML consumer.
- **Mode switch (I4):** `SelectionMode="Multiple"` is removed from XAML; the initial value is `None`. In `OnViewModelPropertyChanged`, `IsSelectionMode` →
  - `collectionView.SelectionMode = IsSelectionMode ? Multiple : None`;
  - when it becomes `Multiple`, also `collectionView.SelectedItems = SelectedItemsSource`.
  
  The reassignment ensures DX reflects the VM collection after the switch. In `BeginSelection`, `IsSelectionMode = true` raises PropertyChanged synchronously, so the switch happens *before* the `ReplaceRange([item])` runs.
- **Event-order guard:** `OnSelectionChanged` → `Dispatcher.Dispatch(() => _viewModel?.OnSelectionChanged(CurrentSelectedCount()))`. The count is **re-read inside** the dispatched action, not captured before it, so a stale queued value (for example a `0` raised by the None→Multiple switch) can never override the settled state. `CrudListPageBase.OnSelectionChanged` / `OnCollectionViewScrolled` / `OnConfirmSheetStateChanged` (`CrudListPageBase.cs:~33-83`) are unwired leftovers: grep 2026-11-11 found no XAML `SelectionChanged=` other than `CrudListView.xaml`. T3 deletes them so there is no second path that bypasses the guard.
  - Case A — `Tap` before `SelectionChanged`: the VM is still in selection mode, so the tap is a no-op.
  - Case B — `SelectionChanged` before `Tap`, both in the same native callback: the count update is queued, so the VM is still in selection mode when `Tap` runs.
  - Case C — DX raises `Tap` in a *later* dispatcher cycle than `SelectionChanged`: the mode has already ended, and the tap would navigate.
  
  Case C is covered by device checklist step 4. If it fails, the fallback is a `CrudListView` constant `TapAfterSelectionChangeGuard = TimeSpan.FromMilliseconds(300)`: ignore `Tap` within that window after the last `SelectionChanged`.
- **Floating toolbar** (the old `Action2` Edit block is deleted; Delete moves from Action3 to Action2; no `Action3*` attributes remain):
  ```xml
  <toolbars:FloatingToolbar VerticalOptions="Center"
      IsVisible="{Binding IsSelectionMode}"
      Action1Icon="done_all_outlined" Action1Command="{Binding SelectAllCommand}"
      Action1Description="Select all" Action1IsSelected="{Binding IsAllSelected}"
      Action2Icon="delete_outlined" Action2Command="{Binding DeleteSelectedCommand}"
      Action2Description="Delete selected" />
  ```
- **Item actions sheet**, a sibling of `confirmSheet`:
  ```xml
  <dx:BottomSheet x:Name="itemActionsSheet" Grid.Row="2" AllowedState="HalfExpanded"
                  HalfExpandedRatio="0.22" IsModal="True" ShowGrabber="True" AllowDismiss="True"
                  BackgroundColor="{StaticResource Surface}" CornerRadius="28"
                  StateChanged="OnItemActionsSheetStateChanged">
      <VerticalStackLayout Padding="0,8">
          <dx:DXButton Style="{StaticResource BottomSheetMenuItem}" Icon="edit_outlined"
                       Content="Edit" Command="{Binding EditItemCommand}" />
          <dx:DXButton Style="{StaticResource BottomSheetMenuItem}" Icon="delete_outlined"
                       Content="Delete" Command="{Binding DeleteItemCommand}" />
      </VerticalStackLayout>
  </dx:BottomSheet>
  ```
  Opening and closing follow the `confirmSheet` pattern (`Show(state, null)` / `Close()` on `ItemActionsSheetState`). `OnItemActionsSheetStateChanged` syncs a swipe-dismiss back as `DismissItemActionsCommand`.
- **Sheet hand-off:** when `ConfirmSheetState` becomes visible:
  - if `itemActionsSheet.State == Hidden`, show `confirmSheet` immediately;
  - otherwise set `_showConfirmWhenActionsHidden = true` and show `confirmSheet` from `OnItemActionsSheetStateChanged` once it reports `Hidden`.

### `ListItemCheckbox` (new, `UI/Components/Lists/ListItemCheckbox.xaml(.cs)`)

```xml
<ContentView x:Name="self" ...>
  <dx:DXButton BackgroundColor="Transparent" WidthRequest="48" HeightRequest="48" Padding="0"
               SemanticProperties.Description="Select"
               Command="{Binding BindingContext.ToggleItemSelectionCommand,
                         Source={RelativeSource AncestorType={x:Type pages:CrudListPageBase}}}"
               CommandParameter="{Binding}">
      <dx:CheckEdit InputTransparent="True" IsChecked="{Binding IsChecked, Source={x:Reference self}}"
                    CheckedCheckBoxColor="{StaticResource Primary}" />
  </dx:DXButton>
</ContentView>
```

- `IsChecked` bindable property (bool, default false).
- **Ancestor choice:** `CrudListPageBase` is the pattern already proven inside DX item templates (`SongsPage.xaml:60`, `AncestorType=CrudListPageBase, Path=BindingContext.LaunchYouTubeSearchCommand`). `CrudListView` as the ancestor is unproven there, so it is not used.
- Pages use `<lists:ListItemCheckbox />` in `ItemTemplate` and `<lists:ListItemCheckbox IsChecked="True" />` in `SelectedItemTemplate`.
- **Governed** once the second page adopts it (T3) — registered in `component-safety-gate.md` by T4.

### `FloatingToolbar` (single consumer — grep 2026-11-11 over all `*.xaml`: only `CrudListView.xaml:91`)

- `ApplySelectedState`: selected = `SurfaceContainer` background + `OnSurface` icon; unselected = transparent + `OnSecondaryContainer` (unchanged).
- Update the class and helper comments ("persistent", "Primary bg"). The comment "up to 5 actions" stays true.

### `CrudListPageBase.OnBackButtonPressed`

`return ListViewModel.HandleBack() || base.OnBackButtonPressed();`

`VenuesPage.OnBackButtonPressed` already calls the base first and shows its exit sheet only when the base returns false, so Venues keeps its exit confirmation. Venues is the only page override (grep 2026-11-11).

### Style `BottomSheetMenuItem` (new, `MaterialStyles.xaml`, beside the other `BottomSheet*` styles; none of them is a menu item)

`dx:DXButton`:
- `BackgroundColor` Transparent, `TextColor` `OnSurface`, `IconColor` `OnSurfaceVariant`;
- `HorizontalOptions` Fill, `HorizontalContentAlignment` Start, `HeightRequest` 56, `Padding` 16,0, `FontSize` 16 (MD3 bodyLarge).

The naming follows the existing `BottomSheet*` keys (`BottomSheetTitle`, `BottomSheetCancelAction`); "menu item" is the MD3 term.

## Testing

Level A, `CrudListViewModelBaseTests` with the existing `TestCrudListViewModel` double (`TItem = string`). **T1 extends the double**:
- it records `NavigateToEditAsync` / `ExecuteDeleteAsync` arguments (lists of the items passed);
- it exposes `Items`, `SelectedItems` and `ItemActionsTarget` through public test accessors.

Tests use `// [AC] REQ-CLSG-xx:` headers.
- **U ACs** (requirements table): 01, 01b, 02, 04a–d, 05a–b, 06, 07, 08, 12 — one test each, plus non-`TItem` no-op cases.
- **D ACs:** 03, 09, 10, 11, 13, 14 — Helder's on-device checklist, recorded in `task-log.md`.
