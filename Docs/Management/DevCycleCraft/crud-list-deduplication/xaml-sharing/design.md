# CRUD XAML Sharing — Design Decision

## Problem

Steps 1–5 of CRUD list deduplication centralised all C# logic into `CrudListPageBase` and
`CrudListViewModelBase<TItem>`. The 4 CRUD XAML pages still repeat ~130–190 lines of
structurally identical markup:

| Block | Lines (approx.) | Identical across all 4? |
|-------|-----------------|------------------------|
| ShimmerView + 6 SkeletonBones | 15 | Yes |
| DXCollectionView (config, events) | 12 | Yes (Tap event on Songs only) |
| FloatingToolbar | 14 | Yes |
| FAB DXButton | 5 | Yes (command + description differ) |
| BottomSheet confirmSheet | 22 | Yes |
| EmptyState "no results" | 5 | Yes (illustration always search_outlined) |
| EmptyState "no items" | 5 | Illustration + headline differ |
| **Total shared block** | **~78** | — |

ArtistsPage adds a FilterChipGroup row (+13 lines). Total duplication: **~776 lines across
4 files** for the structural shell, before the entity-specific DataTemplates.

---

## Options Evaluated

### Option A — `CrudListView` ContentView with BindableProperties ✅ CHOSEN

Standard MAUI composite-control pattern. A `ContentView` subclass owns all shared XAML
elements and exposes `BindableProperty` slots for entity-specific data.

**Pros:**
- Full compiled-binding support inside the ContentView (`x:DataType="ICrudListViewModel"` for shared parts)
- Entity-specific DataTemplates stay typed (`x:DataType="dto:VenueListItemDto"`) — passed in as `DataTemplate` BindableProperties
- Pages remain thin shells: Shell.TitleView + one `<views:CrudListView ...>` element
- CrudListPageBase simplifies (events removed)
- MAUI-native, no third-party dependency, works with DevExpress elements inside

**Cons:**
- BindableProperty boilerplate in `CrudListView.xaml.cs` (~14 properties)
- SelectedItems wiring moves from page constructor into ContentView property-changed handler

### Option B — ControlTemplate on ContentPage

**Rejected.** `ControlTemplate` supports one `ContentPresenter` slot. The three-slot
requirement (ItemTemplate, SelectedItemTemplate, FilterContent) cannot be expressed cleanly.
Passing typed DataTemplates through ControlTemplate requires dynamic resource lookups that
break compiled bindings.

### Option C — No XAML sharing

**Rejected.** ~440 lines of copy-paste structural markup. Any fix to the BottomSheet or
FloatingToolbar must be applied 4 times. Risk of drift grows with each future CRUD page.

---

## Chosen Design — `CrudListView` ContentView

### File locations

| File | Purpose |
|------|---------|
| `MyVocaList/UI/Views/CrudListView.xaml` | Shared structural XAML |
| `MyVocaList/UI/Views/CrudListView.xaml.cs` | BindableProperties + internal wiring |

### BindableProperties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ItemsSource` | `IList` | `null` | Entity collection (Venues, Songs, etc.) |
| `SelectedItemsSource` | `IList` | `null` | For DXCollectionView.SelectedItems |
| `ItemTemplate` | `DataTemplate` | `null` | Unselected row template |
| `SelectedItemTemplate` | `DataTemplate` | `null` | Selected row template |
| `SearchPlaceholder` | `string` | `""` | SearchAppBar placeholder text |
| `EmptyNoItemsIllustration` | `string` | `""` | Icon name for "no items" EmptyState |
| `EmptyNoItemsHeadline` | `string` | `""` | "No venue registered", etc. |
| `IsEmptyNoItems` | `bool` | `false` | Bound to entity-specific VM bool property |
| `FabCommand` | `ICommand` | `null` | AddVenueCommand, AddSongCommand, etc. |
| `FabDescription` | `string` | `""` | SemanticProperties.Description |
| `FabIcon` | `string` | `"add_outlined"` | FAB icon |
| `FilterContent` | `View` | `null` | Optional slot above list (ArtistsPage chips) |
| `ItemTapCommand` | `ICommand` | `null` | Optional DXCollectionView Tap command |

### ICrudListViewModel extension

Add `bool IsEmptyNoResults { get; }` to the interface.

> **Why `IsEmptyNoResults` on the interface vs `IsEmptyNoItems` as a BindableProperty:**
> `IsEmptyNoResults` means "search returned no matches" — this condition is semantically identical
> across all entities and the property is named the same in all 4 VMs. `CrudListView` can bind to
> it directly via BindingContext (no BindableProperty needed). `IsEmptyNoItems` means "no entities
> of this type exist at all" — the condition is the same but the property is named differently per
> VM (`IsEmptyNoVenues`, `IsEmptyNoArtists`, etc.). It must be passed in from the page as a
> BindableProperty. **Do NOT add `IsEmptyNoResults` as a BindableProperty on `CrudListView`.** All 4 ViewModels already implement
this property with the same name — adding it to the interface is a zero-risk compile-time
contract that lets `CrudListView` bind to it without knowing the concrete VM type.

```csharp
public interface ICrudListViewModel : INotifyPropertyChanged
{
    BottomSheetState ConfirmSheetState { get; set; }
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }   // ← new
    int SelectedCount { get; }
    IRelayCommand CloseSearchCommand { get; }
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
```

### CrudListPageBase changes

Mark the two event declarations `[Obsolete]` in Step 7a (pages still compile); delete them
in Step 7e after all page constructors have dropped their subscriptions:

```csharp
// OBSOLETE in 7a, DELETED in 7e:
[Obsolete("Replaced by CrudListView internal wiring. Remove after all pages migrate.")]
protected event EventHandler<BottomSheetState> ConfirmSheetStateRequired;
[Obsolete("Replaced by CrudListView internal wiring. Remove after all pages migrate.")]
protected event EventHandler SelectionItemsWireUpRequired;
```

**Handlers that move to `CrudListView` (remove from `CrudListPageBase`):**
- `OnViewModelPropertyChanged` — ConfirmSheetState observation moves to CrudListView
- `OnConfirmSheetStateChanged` — bidirectional sheet sync moves to CrudListView
- `OnCollectionViewScrolled` — moves to CrudListView (collectionView is inside it)
- `OnSelectionChanged` — moves to CrudListView

**Handlers that stay in `CrudListPageBase`:**
- `AttachViewModel()` — still subscribes to PropertyChanged for `OnBackButtonPressed` logic
- `OnAppearing()` — calls `InitializeAsync()` unchanged
- `OnBackButtonPressed()` — checks `ListViewModel.ConfirmSheetState` and `IsSearchMode`;
  these properties come through `ICrudListViewModel` so no XAML element access is needed

> Note: `OnBackButtonPressed` reads `ListViewModel.ConfirmSheetState` directly from the VM
> (not from the confirmSheet element) — so it does NOT require `OnViewModelPropertyChanged`
> to remain in `CrudListPageBase`. The VM property is the source of truth; the sheet UI is
> driven from it, not the other way around.

Each page code-behind constructor loses the two event-subscription lambda blocks.

### Internal wiring inside CrudListView

`CrudListView` subscribes to `BindingContextChanged` and casts to `ICrudListViewModel`.
It then:

1. Observes `ViewModel.ConfirmSheetState` via `PropertyChanged` — calls
   `confirmSheet.Show()` / `confirmSheet.Close()` internally (no page event needed)
2. Applies `SelectedItemsSource` to `collectionView.SelectedItems` in
   `OnSelectedItemsSourceChanged` property-changed handler

### Resulting page XAML skeleton (VenuesPage example)

```xml
<pages:CrudListPageBase ...
    x:DataType="vm:VenuesViewModel">

    <Shell.BackButtonBehavior>
        <BackButtonBehavior IsVisible="False" IsEnabled="False" />
    </Shell.BackButtonBehavior>

    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar Title="{Binding AppBarTitle}"
                                 NavigationIcon="arrow_back_outlined"
                                 NavigationCommand="{Binding GoBackCommand}"
                                 Action1Icon="search_outlined"
                                 Action1Command="{Binding OpenSearchCommand}"
                                 IsElevated="{Binding IsScrolled}"
                                 IsVisible="{Binding IsSearchMode,
                                     Converter={StaticResource InverseBoolConverter}}" />
            <appbars:SearchAppBar SearchText="{Binding SearchText, Mode=TwoWay}"
                                  Placeholder="Search venues..."
                                  BackCommand="{Binding CloseSearchCommand}"
                                  IsElevated="{Binding IsScrolled}"
                                  IsVisible="{Binding IsSearchMode}" />
        </Grid>
    </Shell.TitleView>

    <views:CrudListView
        ItemsSource="{Binding Venues}"
        SelectedItemsSource="{Binding SelectedVenuesRaw}"
        IsEmptyNoItems="{Binding IsEmptyNoVenues}"
        SearchPlaceholder="Search venues..."
        EmptyNoItemsIllustration="nightlife_outlined"
        EmptyNoItemsHeadline="No venue registered"
        FabCommand="{Binding AddVenueCommand}"
        FabDescription="Add venue">
        <views:CrudListView.ItemTemplate>
            <DataTemplate x:DataType="dto:VenueListItemDto">
                <!-- entity-specific row content -->
            </DataTemplate>
        </views:CrudListView.ItemTemplate>
        <views:CrudListView.SelectedItemTemplate>
            <DataTemplate x:DataType="dto:VenueListItemDto">
                <!-- entity-specific selected row content -->
            </DataTemplate>
        </views:CrudListView.SelectedItemTemplate>
    </views:CrudListView>

</pages:CrudListPageBase>
```

### ArtistsPage FilterContent slot

ArtistsPage passes its FilterChipGroup via the `FilterContent` BindableProperty:

```xml
<views:CrudListView ...>
    <views:CrudListView.FilterContent>
        <dxe:FilterChipGroup SelectedItems="{Binding SelectedRoleFilters, Mode=TwoWay}"
                             Margin="16,4,16,4">
            ...
        </dxe:FilterChipGroup>
    </views:CrudListView.FilterContent>
    ...
</views:CrudListView>
```

Inside `CrudListView.xaml`, if `FilterContent != null`, show it above the list in a 2-row
Grid; otherwise use a single-row Grid (no wasted space).

---

## Estimated Line Reduction

| File | Before | After | Saved |
|------|--------|-------|-------|
| VenuesPage.xaml | 187 | ~60 | ~127 |
| PeoplePage.xaml | ~190 | ~65 | ~125 |
| SongsPage.xaml | 185 | ~65 | ~120 |
| ArtistsPage.xaml | 214 | ~75 | ~139 |
| CrudListView.xaml (new) | — | ~120 | — |
| **Net** | **~776** | **~385** | **~391 lines (~50%)** |

---

## Constraints & Risks

| Risk | Mitigation |
|------|-----------|
| DXCollectionView inside ContentView: SelectedItems wiring | Set via BindableProperty OnPropertyChanged in code-behind |
| BottomSheet inside ContentView: show/close timing | ContentView subscribes to ICrudListViewModel.PropertyChanged directly |
| Compiled bindings (`x:DataType`) on shared vs entity parts | Shared: `x:DataType="ICrudListViewModel"` on CrudListView internals; entity DataTemplates passed typed from page |
| ArtistsPage FilterChipGroup Grid layout change | Handled via conditional row in CrudListView internal Grid |
| SongsPage OnItemTapped (currently empty no-op) | Wire via `ItemTapCommand` BindableProperty; SongsPage passes a no-op command or null |
| `.sln` registration of new files | Mandatory in commit per constraints-registry.md |
| Component Change Governance | CrudListView is a NEW component — governance rule applies to future changes, not creation |

---

## Implementation Order

Tasks are sequential. Each depends on the previous being build-green:

1. **Step 7a** — Create CrudListView + update ICrudListViewModel + simplify CrudListPageBase
2. **Step 7b** — Migrate VenuesPage (simplest — no extras)
3. **Step 7c** — Migrate PeoplePage (Monogram leading content)
4. **Step 7d** — Migrate SongsPage (Subtitle + ItemTapCommand)
5. **Step 7e** — Migrate ArtistsPage (FilterContent slot + ViewCatalog trailing — most complex)
