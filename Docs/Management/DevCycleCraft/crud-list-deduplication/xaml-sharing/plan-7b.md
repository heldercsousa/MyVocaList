# Step 7b — Migrate VenuesPage.xaml to CrudListView

**Depends on:** Step 7a (CrudListView created, build green)  
**Unblocks:** Step 7c (PeoplePage migration)  
**Risk:** Low — simplest page, no entity-specific extras beyond icon

---

## Entity-Specific Details

| Property | Value |
|----------|-------|
| `ItemsSource` | `{Binding Venues}` |
| `SelectedItemsSource` | `{Binding SelectedVenuesRaw}` |
| `IsEmptyNoItems` | `{Binding IsEmptyNoVenues}` |
| `SearchPlaceholder` | `"Search venues..."` |
| `EmptyNoItemsIllustration` | `"nightlife_outlined"` |
| `EmptyNoItemsHeadline` | `"No venue registered"` |
| `FabCommand` | `{Binding AddVenueCommand}` |
| `FabDescription` | `"Add venue"` |
| `AppBarSubtitle` | not set (hidden) |
| `FilterContent` | not set |
| `ItemTapCommand` | not set |

**Item template leading:** `ListItemLeadingIcon Icon="place_outlined"`  
**Item template trailing (unselected):** `CheckEdit IsChecked="False"`  
**Item template trailing (selected):** `CheckEdit IsChecked="True"` with `CheckedCheckBoxColor`

---

## Files Owned

| Action | File |
|--------|------|
| Edit | `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` |
| Edit | `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` |

---

## Tasks

- [ ] **Edit `VenuesPage.xaml`**
  - Move `ItemTemplate` DataTemplate into page `<ContentPage.Resources>` as a keyed resource (or inline in CrudListView element)
  - Move `SelectedItemTemplate` DataTemplate — same
  - Replace the entire `<Grid>` body (ShimmerView → BottomSheet) with `<views:CrudListView ...>` with all required BindableProperties
  - Keep `Shell.BackButtonBehavior` (unchanged)
  - Keep `Shell.TitleView` with SmallAppBar + SearchAppBar (unchanged)
  - Add `xmlns:views="clr-namespace:MyVocaList.UI.Views"` namespace

- [ ] **Edit `VenuesPage.xaml.cs`**
  - Remove `ConfirmSheetStateRequired` event subscription lambda from constructor
  - Remove `SelectionItemsWireUpRequired` event subscription lambda from constructor
  - Constructor body reduces to: `InitializeComponent()`, `_viewModel = viewModel`, `BindingContext = _viewModel`, `AttachViewModel()`

- [ ] **`dotnet build` — 0 errors**
- [ ] **`dotnet test` — 0 failures**
- [ ] **Emulator smoke test**
  - Venues list loads
  - Shimmer shows on first open
  - Search works (debounced)
  - Multi-select + FloatingToolbar appear on item tap
  - Select All works
  - Delete confirmation BottomSheet opens and closes
  - FAB navigates to VenueFormPage
  - Back button dismisses search mode / confirm sheet / navigates back

---

## Demo

> Open VenuesPage on emulator. Add a venue. Verify it appears. Select it. Delete it via
> confirmation sheet. Confirm it disappears.

---

## Review lane: Standard
