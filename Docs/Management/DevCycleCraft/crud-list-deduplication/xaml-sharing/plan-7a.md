# Step 7a — Implement CrudListView + Update CrudListPageBase

**Depends on:** `design.md` approved  
**Unblocks:** Step 7b (VenuesPage migration)  
**Risk:** Medium — new shared component + interface change affect all 4 pages at compile time

---

## Scope

Create the `CrudListView` ContentView that will host the shared structural XAML for all
4 CRUD list pages. Extend `ICrudListViewModel` with `IsEmptyNoResults`. Simplify
`CrudListPageBase` by removing the two event declarations that are no longer needed.

No page XAML files are touched in this step — pages still compile against their old
structure. This step must be build-green before any page migration starts.

---

## Files Owned

| Action | File |
|--------|------|
| Create | `MyVocaList/UI/Views/CrudListView.xaml` |
| Create | `MyVocaList/UI/Views/CrudListView.xaml.cs` |
| Edit | `MyVocaList/UI/Pages/ICrudListViewModel.cs` |
| Edit | `MyVocaList/UI/Pages/CrudListPageBase.cs` |
| Edit | `MyVocaList.sln` |

---

## Tasks

- [ ] **Extend `ICrudListViewModel`**
  - Add `bool IsEmptyNoResults { get; }` to the interface
  - All 4 VMs already implement this property — no changes needed in VMs
  - Build must stay green after this change

- [ ] **Create `CrudListView.xaml`**
  - Root: `ContentView` with `x:DataType="pages:ICrudListViewModel"` for the shared bindings
  - Internal Grid: `RowDefinition Height="Auto"` (FilterContent slot, shown only when set) + `RowDefinition Height="*"` (rest)
  - ShimmerView + 6 SkeletonBones in LoadingView
  - DXCollectionView in ShimmerView.Content:
    - `ItemsSource="{Binding Source={RelativeSource AncestorType={x:Type views:CrudListView}}, Path=ItemsSource}"`
    - `IsRefreshing`, `PullToRefreshCommand`, `IsLoadMoreEnabled`, `LoadMoreCommand`, `IndicatorColor`, `SelectionMode`, `UseRippleEffect`, `ItemSeparatorThickness`, `Margin` — same as current pages
    - `ItemTemplate="{Binding Source={RelativeSource AncestorType={x:Type views:CrudListView}}, Path=ItemTemplate}"`
    - `SelectedItemTemplate` — same pattern
    - `Scrolled="OnCollectionViewScrolled"` — handler in CrudListView code-behind
    - `SelectionChanged="OnSelectionChanged"` — handler in CrudListView code-behind
    - `Tap` — wired conditionally when `ItemTapCommand != null`
  - EmptyState "no items" bound to `IsEmptyNoItems` BindableProperty
  - EmptyState "no results" bound to `IsEmptyNoResults` via BindingContext (ICrudListViewModel)
  - FloatingToolbar — all 3 actions bound via BindingContext (shared commands on ICrudListViewModel)
  - FAB DXButton — `Command="{Binding Source={RelativeSource AncestorType=...}, Path=FabCommand}"`
  - BottomSheet confirmSheet — StateChanged handled in code-behind

- [ ] **Create `CrudListView.xaml.cs`**
  - Declare all 14 BindableProperties (see design.md table)
  - `OnSelectedItemsSourceChanged`: sets `collectionView.SelectedItems = newValue`
  - `OnFilterContentChanged`: shows/hides the filter row in the internal Grid
  - `BindingContextChanged` handler: casts to `ICrudListViewModel`; subscribes to `PropertyChanged`
  - `OnViewModelPropertyChanged`: when `ConfirmSheetState` changes → `confirmSheet.Show()` / `confirmSheet.Close()`
  - `OnCollectionViewScrolled`: calls `ViewModel.IsScrolled = e.Offset > 0`
  - `OnSelectionChanged`: calls `ViewModel.OnSelectionChanged(count)`
  - `OnConfirmSheetStateChanged`: bidirectional sync (sheet closed by gesture → VM)

- [ ] **Simplify `CrudListPageBase`**
  - **Move to CrudListView** (remove from CrudListPageBase): `OnViewModelPropertyChanged`, `OnConfirmSheetStateChanged`, `OnCollectionViewScrolled`, `OnSelectionChanged`
  - **Mark `[Obsolete]` in this step** (delete in Step 7e): `ConfirmSheetStateRequired` event, `SelectionItemsWireUpRequired` event
  - **Keep unchanged**: `AttachViewModel()`, `OnAppearing()`, `OnBackButtonPressed()`

  Rationale: `OnBackButtonPressed` reads `ListViewModel.ConfirmSheetState` from the VM interface — it does not access any XAML element — so it remains valid in the base without `OnViewModelPropertyChanged`. `AttachViewModel` may still be useful for any future base-class property-changed handling; keep it.

  Exact `[Obsolete]` message: `"Replaced by CrudListView internal wiring. Will be deleted in Step 7e after all pages migrate."`

- [ ] **Register in `.sln`**
  - Add `CrudListView.xaml` and `CrudListView.xaml.cs` to the appropriate Solution Folder
  - See `constraints-registry.md § Visual Studio Solution (.sln)` for GUID pattern

- [ ] **`dotnet build` — 0 errors**
- [ ] **`dotnet test` — 0 failures**

---

## Demo

> Build succeeds with 0 errors. CrudListView appears in VS Solution Explorer. No existing
> page behaviour changes (pages still use their own XAML; CrudListView is not yet referenced
> by any page).

---

## Review lane: Standard
