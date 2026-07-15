# CRUD Page Design Laws — CrudListView — the standard list shell

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## CrudListView — The Standard List Shell

As of Step 7 of the CRUD list deduplication effort, all **new** CRUD list pages **must** use `CrudListView` (`MyVocaList/UI/Views/CrudListView.xaml`) as the page body. The old manual pattern of writing `ShimmerView` + `DXCollectionView` + `FloatingToolbar` + FAB + `EmptyState` + `dx:BottomSheet` directly in each page XAML is **deprecated**. Pages are now thin shells: a `Shell.TitleView` (SmallAppBar + SearchAppBar) plus one `<views:CrudListView>` element with entity-specific `DataTemplate` slots.

### What CrudListView provides (do not reproduce in page XAML)

CrudListView owns the following elements internally. Pages must **not** add these themselves:

- `ShimmerView` wrapping `DXCollectionView` (6 `SkeletonBone` bones)
- `DXCollectionView` with `SelectionMode="Multiple"`, `IsPullToRefreshEnabled`, `IsLoadMoreEnabled`, `Margin="0,0,0,88"`, `Scrolled`, `SelectionChanged` events, optional `Tap` event (via `ItemTapCommand`)
- `FloatingToolbar` (Action1=SelectAll, Action2=Edit, Action3=Delete) + FAB — centered, `Margin="0,0,0,16"`
- Two `EmptyState` components: "no items" (entity-specific text/icon) and "no results" (`search_outlined`)
- Confirm `BottomSheet` (`HalfExpandedRatio="0.28"`, `AllowedState="HalfExpanded"`)
- Optional `FilterContent` slot (Row 0 of internal Grid, hidden until set)

### BindableProperties

| Property | Type | Default | Set by page | Notes |
|----------|------|---------|-------------|-------|
| `ItemsSource` | `IList` | `null` | Yes | Bound to ViewModel collection (e.g. `{Binding Venues}`) |
| `SelectedItemsSource` | `IList` | `null` | Yes | Bound to `SelectedVenuesRaw` (IList wrapper) — wired to `DXCollectionView.SelectedItems` internally |
| `ItemTemplate` | `DataTemplate` | `null` | Yes | Entity-specific unselected row template |
| `SelectedItemTemplate` | `DataTemplate` | `null` | Yes | Entity-specific selected row template |
| `SearchPlaceholder` | `string` | `""` | Yes | For documentation/intent — not rendered inside CrudListView |
| `EmptyNoItemsIllustration` | `string` | `""` | Yes | Icon name for "no items" state (e.g. `"nightlife_outlined"`) |
| `EmptyNoItemsHeadline` | `string` | `""` | Yes | Text for "no items" state (e.g. `"No venue registered"`) |
| `IsEmptyNoItems` | `bool` | `false` | Yes | Bound to VM property that differs per page (e.g. `{Binding IsEmptyNoVenues}`) |
| `FabCommand` | `ICommand` | `null` | Yes | Add command (e.g. `{Binding AddVenueCommand}`) |
| `FabDescription` | `string` | `""` | Yes | `SemanticProperties.Description` for accessibility |
| `FabIcon` | `string` | `"add_outlined"` | Optional | Override only when not the default add action |
| `FilterContent` | `View` | `null` | Optional | Any view shown above the list (e.g. `FilterChipGroup`) |
| `ItemTapCommand` | `ICommand` | `null` | Optional | DXCollectionView Tap command; wired only when non-null |

**Why `IsEmptyNoItems` is a BindableProperty but `IsEmptyNoResults` is not:** `IsEmptyNoResults` is named identically in all ViewModels and is part of `ICrudListViewModel`, so CrudListView binds to it directly via BindingContext. `IsEmptyNoItems` is named differently per VM (`IsEmptyNoVenues`, `IsEmptyNoArtists`, etc.) and therefore must be passed from the page.

### ICrudListViewModel contract

Every ViewModel for a CRUD list page must implement `ICrudListViewModel`:

```csharp
public interface ICrudListViewModel : INotifyPropertyChanged
{
    // Search / scroll state
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }
    IRelayCommand CloseSearchCommand { get; }

    // Loading state
    bool IsInitialLoading { get; }
    bool IsRefreshing { get; set; }
    IAsyncRelayCommand RefreshCommand { get; }
    bool HasMoreItems { get; }
    IRelayCommand LoadMoreCommand { get; }

    // Selection state
    int SelectedCount { get; }
    bool IsAllSelected { get; }
    bool CanEditSelected { get; }
    bool CanDeleteSelected { get; }

    // Toolbar commands
    IRelayCommand SelectAllCommand { get; }
    IAsyncRelayCommand EditSelectedCommand { get; }
    IRelayCommand DeleteSelectedCommand { get; }

    // Confirm bottom sheet
    BottomSheetState ConfirmSheetState { get; set; }
    string ConfirmMessage { get; }
    string ConfirmActionText { get; }
    IAsyncRelayCommand ConfirmActionCommand { get; }
    IRelayCommand DismissConfirmCommand { get; }

    // Lifecycle
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
```

CrudListView subscribes to `BindingContextChanged`, casts to `ICrudListViewModel`, and subscribes to `PropertyChanged` to drive `confirmSheet.Show()` / `confirmSheet.Close()`.

**Use `CrudListViewModelBase<TItem>` as the base class** (`MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`). It implements all members above plus pagination, search debounce, and confirm-sheet logic. Concrete VMs override the abstract methods: `FetchPageAsync`, `FetchMoreAsync`, `ExecuteDeleteAsync`, `BuildDeleteConfirmMessage`, `NavigateToAddAsync`, `NavigateToEditAsync`, and `RaiseEntityEmptyStateProperties`.

### CrudListPageBase — what it does

`CrudListPageBase` is the required base class for all CRUD list pages.

> **Pattern intent (documented pattern, NOT a governed component):** `CrudListPageBase` — together with `CrudListView` and `CrudListViewModelBase<T>` — is the standard pattern for **List pages in general**, not only entity/CRUD-member lists. Its purpose is to cut duplication, reduce error risk, and keep every List page behaving identically (leading-icon behavior, hardware back, shimmer, confirm sheet, pagination). Deliberate **exceptions** exist and more may appear — e.g. the Autocomplete component's in-sheet / full-screen result list is not a "List page" in this sense and does not inherit this base. This base class is intentionally kept as a **documented pattern rather than a governed component** (it is not on the `component-safety-gate.md` governed list): changes still go through normal spec + review and must update every List-page consumer consistently, but they do not require the four-gate component-change ceremony.

**Provided by CrudListPageBase (do not re-implement in pages):**
- `OnAppearing()` — calls `ListViewModel.InitializeAsync()`
- `OnBackButtonPressed()` — dismiss confirm sheet → close search → default Shell back
- `AttachViewModel()` — call from the constructor to subscribe `ListViewModel.PropertyChanged`
- `OnCollectionViewScrolled` / `OnSelectionChanged` / `OnConfirmSheetStateChanged` — protected event handlers (not wired from page code-behind; CrudListView handles these internally)

The `[Obsolete]` events `ConfirmSheetStateRequired` and `SelectionItemsWireUpRequired` were deleted in Step 7e. Do not reference them.

### New page XAML skeleton (VenuesPage reference)

```xml
<pages:CrudListPageBase
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:pages="clr-namespace:MyVocaList.UI.Pages.Base"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:views="clr-namespace:MyVocaList.UI.Components"
    x:Class="MyVocaList.UI.Pages.Venues.VenuesPage"
    x:DataType="vm:VenuesViewModel"
    Title="Venues"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <Shell.BackButtonBehavior>
        <BackButtonBehavior IsVisible="False" IsEnabled="False" />
    </Shell.BackButtonBehavior>

    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar
                Title="{Binding AppBarTitle}"
                NavigationIcon="arrow_back_outlined"
                NavigationCommand="{Binding GoBackCommand}"
                Action1Icon="search_outlined"
                Action1Command="{Binding OpenSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
            <appbars:SearchAppBar
                SearchText="{Binding SearchText, Mode=TwoWay}"
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
                <lists:ListItem Headline="{Binding Name}" IsSelected="False">
                    <lists:ListItem.LeadingContent>
                        <lists:ListItemLeadingIcon Icon="place_outlined" />
                    </lists:ListItem.LeadingContent>
                    <lists:ListItem.TrailingContent>
                        <dx:CheckEdit IsChecked="False" InputTransparent="True" VerticalOptions="Center" />
                    </lists:ListItem.TrailingContent>
                </lists:ListItem>
            </DataTemplate>
        </views:CrudListView.ItemTemplate>
        <views:CrudListView.SelectedItemTemplate>
            <DataTemplate x:DataType="dto:VenueListItemDto">
                <lists:ListItem Headline="{Binding Name}" IsSelected="True">
                    <lists:ListItem.LeadingContent>
                        <lists:ListItemLeadingIcon Icon="place_outlined" />
                    </lists:ListItem.LeadingContent>
                    <lists:ListItem.TrailingContent>
                        <dx:CheckEdit IsChecked="True"
                                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                      InputTransparent="True"
                                      VerticalOptions="Center" />
                    </lists:ListItem.TrailingContent>
                </lists:ListItem>
            </DataTemplate>
        </views:CrudListView.SelectedItemTemplate>
    </views:CrudListView>

</pages:CrudListPageBase>
```

**What stays in the page XAML:**
- `Shell.BackButtonBehavior` — always `IsVisible="False" IsEnabled="False"`
- `Shell.TitleView` — `SmallAppBar` + `SearchAppBar` in a `Grid` (unchanged from old pattern)
- `<views:CrudListView>` with BindableProperty attributes and `ItemTemplate`/`SelectedItemTemplate` slots

**What is removed from page XAML (now handled by CrudListView):**
- `ShimmerView` + `SkeletonBone` list
- `DXCollectionView` element (with all its attributes and events)
- `FloatingToolbar` + FAB `HorizontalStackLayout`
- Both `EmptyState` elements
- `dx:BottomSheet` (confirm sheet)

### Optional slot: FilterContent

For pages that show a filter row above the list (e.g. ArtistsPage with a FilterChipGroup):

```xml
<views:CrudListView ...>
    <views:CrudListView.FilterContent>
        <!-- Any View — shown in Row 0 above the list -->
    </views:CrudListView.FilterContent>
    ...
</views:CrudListView>
```

When `FilterContent` is `null` (default), Row 0 has `Height="0"` and is invisible. When set, Row 0 becomes `Auto` height and the view is displayed.

### Optional slot: ItemTapCommand

For pages that navigate on item tap (e.g. SongsPage):

```xml
<views:CrudListView
    ItemTapCommand="{Binding NavigateToSongCommand}"
    ...>
```

When `ItemTapCommand` is `null` (default), no `Tap` event handler is wired. When set, CrudListView wires `DXCollectionView.Tap` and invokes the command with the tapped item as parameter.

---
