# Plan: Update crud-pages.md for CrudListView Pattern (Step 7-guidelines)

## Context

`CrudListView` (a `ContentView` subclass) was created in Step 7a to centralise the ~78 lines
of structurally identical XAML that every CRUD list page repeated:
ShimmerView + DXCollectionView + FloatingToolbar + FAB + EmptyStates + confirm BottomSheet.

Pages using this component are now thin shells: `Shell.TitleView` + one `<views:CrudListView>` element + entity-specific `DataTemplate` slots.

The guideline file `.claude/library/crud-pages.md` describes the old manual pattern in several
sections. A single subagent editing only that file must bring it up to date.

---

## Scope

**File edited:** `.claude/library/crud-pages.md` — one file, no code changes, no build required.

**Files NOT touched:** Any `.cs`, `.xaml`, `.csproj`, `.sln`, or spec file.

---

## Section-by-Section Instructions

### 1. Add a new top-level section immediately after the Three Laws (before "Spec-First Development")

**Title:** `## CrudListView — The Standard List Shell`

**Content to write:**

Explain that as of Step 7 of the CRUD list deduplication effort, all new CRUD list pages **must** use `CrudListView` (`MyVocaList/UI/Views/CrudListView.xaml`) as the page body. The old manual pattern of writing ShimmerView + DXCollectionView + FloatingToolbar + FAB + EmptyState + BottomSheet directly in each page XAML is **deprecated**.

Include the following sub-sections:

#### 1a. What CrudListView provides (do not rewrite these)

List the shared elements that CrudListView owns internally — the agent must not reproduce them in page XAML:

- `ShimmerView` wrapping `DXCollectionView` (6 `SkeletonBone` bones)
- `DXCollectionView` with `SelectionMode="Multiple"`, `IsPullToRefreshEnabled`, `IsLoadMoreEnabled`, `Margin="0,0,0,88"`, `Scrolled`, `SelectionChanged` events, optional `Tap` event (via `ItemTapCommand`)
- `FloatingToolbar` (Action1=SelectAll, Action2=Edit, Action3=Delete) + FAB — centered, `Margin="0,0,0,16"`
- Two `EmptyState` components: "no items" (entity-specific text/icon) and "no results" (`search_outlined`)
- Confirm `BottomSheet` (`HalfExpandedRatio="0.28"`, `AllowedState="HalfExpanded"`)
- Optional `FilterContent` slot (Row 0 of internal Grid, hidden until set)

#### 1b. BindableProperties table

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

Note why `IsEmptyNoItems` is a BindableProperty but `IsEmptyNoResults` is not: `IsEmptyNoResults` is named identically in all ViewModels and is part of `ICrudListViewModel`, so CrudListView binds to it directly via BindingContext. `IsEmptyNoItems` is named differently per VM (`IsEmptyNoVenues`, `IsEmptyNoArtists`, etc.) and therefore must be passed from the page.

#### 1c. ICrudListViewModel contract

Every ViewModel for a CRUD list page must implement `ICrudListViewModel`:

```csharp
public interface ICrudListViewModel : INotifyPropertyChanged
{
    BottomSheetState ConfirmSheetState { get; set; }
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }
    IRelayCommand CloseSearchCommand { get; }
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
```

CrudListView subscribes to `BindingContextChanged`, casts to `ICrudListViewModel`, and subscribes to `PropertyChanged` to drive `confirmSheet.Show()` / `confirmSheet.Close()`.

#### 1d. CrudListPageBase — what it does and what it no longer does

`CrudListPageBase` is the required base class for all CRUD list pages.

**Still provided by CrudListPageBase (do not re-implement in pages):**
- `OnAppearing()` — calls `ListViewModel.InitializeAsync()`
- `OnBackButtonPressed()` — dismiss confirm sheet → close search → default back
- `AttachViewModel()` — subscribes page to VM `PropertyChanged` for back-button logic

**[Obsolete] events — do NOT subscribe to these in new pages:**
- `ConfirmSheetStateRequired` — compiler warning message: `"Replaced by CrudListView internal wiring. Will be deleted in Step 7e after all pages migrate."`
- `SelectionItemsWireUpRequired` — compiler warning message: `"Replaced by CrudListView internal wiring. Will be deleted in Step 7e after all pages migrate."`

Quote these messages verbatim in `crud-pages.md` so future agents recognise the CS0618 warning if they accidentally use these events.

These events will be deleted after all existing pages finish migrating (Step 7e). New pages must not subscribe to them.

#### 1e. New page XAML skeleton (VenuesPage reference)

Provide the concrete skeleton based on VenuesPage post-migration target:

```xml
<pages:CrudListPageBase
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:pages="clr-namespace:MyVocaList.UI.Pages"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:views="clr-namespace:MyVocaList.UI.Views"
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

**What stays in the page XAML (above skeleton):**
- `Shell.BackButtonBehavior` — always `IsVisible="False" IsEnabled="False"`
- `Shell.TitleView` — `SmallAppBar` + `SearchAppBar` in a `Grid` (unchanged from old pattern)
- `<views:CrudListView>` with BindableProperty attributes and `ItemTemplate`/`SelectedItemTemplate` slots

**What is removed from page XAML (handled by CrudListView):**
- `ShimmerView` + `SkeletonBone` list
- `DXCollectionView` element (with all its attributes and events)
- `FloatingToolbar` + FAB `HorizontalStackLayout`
- Both `EmptyState` elements
- `dx:BottomSheet` (confirm sheet)

#### 1f. Optional slot: FilterContent

For pages that show a filter row above the list (e.g. ArtistsPage with a FilterChipGroup):

```xml
<views:CrudListView ...>
    <views:CrudListView.FilterContent>
        <!-- Any View — shown in Row 0 above the list -->
    </views:CrudListView.FilterContent>
    ...
</views:CrudListView>
```

When `FilterContent` is `null` (default), Row 0 has `Height="0"` and is invisible.
When set, Row 0 becomes `Auto` height and the view is displayed.

#### 1g. Optional slot: ItemTapCommand

For pages that navigate on item tap (e.g. SongsPage):

```xml
<views:CrudListView
    ItemTapCommand="{Binding NavigateToSongCommand}"
    ...>
```

When `ItemTapCommand` is `null` (default), no `Tap` event handler is wired.
When set, CrudListView wires `DXCollectionView.Tap` and invokes the command with the tapped item as parameter.

---

### 2. Update "List Layout — Laws and Variants" section

**Header change:** Add a deprecation notice at the top of the section:

> **Note:** Writing `DXCollectionView` directly in page XAML is the old pattern. As of Step 7, use
> `<views:CrudListView>` instead and pass entity-specific DataTemplates via `ItemTemplate` and
> `SelectedItemTemplate`. The standard configuration below is now internal to `CrudListView` — it is
> shown here as reference only.

Keep the existing standard configuration code block and variants table unchanged — they document
what CrudListView does internally, which is still useful reference material.

---

### 3. Update "FloatingToolbar — Laws and Variants" section

**Header change:** Add a deprecation notice at the top of the section:

> **Note:** Writing `FloatingToolbar` + FAB directly in page XAML is the old pattern. As of Step 7,
> CrudListView owns the toolbar and FAB. The standard slot assignments (Action1=SelectAll,
> Action2=Edit, Action3=Delete) are hardcoded in CrudListView. For pages with different action
> needs, this section will be expanded when CrudListView gains configurable toolbar slots.

Keep the existing slot table and FAB coexistence note — they describe what CrudListView implements.

---

### 4. Update "Code-Behind Checklist (list page)" section

Replace the current checklist content with the new minimal code-behind pattern:

```csharp
// Required: typed ViewModel property for compiled bindings in DataTemplates
public MyViewModel ViewModel => _viewModel;

// Required: abstract ListViewModel property for CrudListPageBase
protected override ICrudListViewModel ListViewModel => _viewModel;

protected override void OnAppearing()
{
    base.OnAppearing();   // CrudListPageBase calls InitializeAsync() here
    AttachViewModel();    // subscribe to PropertyChanged for OnBackButtonPressed logic
}
```

Add a note: `OnCollectionViewScrolled`, `OnSelectionChanged`, `OnConfirmSheetStateChanged`, and the
SelectedItems wire-up are all handled internally by `CrudListView`. Do not add them to the
page code-behind.

Keep the `OnBackButtonPressed` documentation — it is still handled by `CrudListPageBase`
(not by the page code-behind directly).

---

### 5. Update "Confirm-Delete BottomSheet" section

Add a note at the top:

> **Note:** As of Step 7, the confirm BottomSheet lives inside `CrudListView`. Do not add a
> `dx:BottomSheet` to page XAML. The ViewModel properties `ConfirmSheetState`, `ConfirmMessage`,
> `ConfirmActionText`, `ConfirmActionCommand`, and `DismissConfirmCommand` are still required on
> the ViewModel — CrudListView reads them from the `ICrudListViewModel` BindingContext.

Keep the ViewModel property list as reference — it is still required. Remove the XAML snippet
(or mark it as "internal to CrudListView — do not copy to page XAML").

---

### 6. Update "Shimmer Skeleton" section

Add a note at the top:

> **Note:** As of Step 7, the shimmer skeleton is internal to `CrudListView`. Do not add
> `ShimmerView` or `SkeletonBone` elements to page XAML. The ViewModel `IsInitialLoading`
> property is still required and is bound by CrudListView via `ICrudListViewModel` BindingContext.

Keep the bone specification (HeightRequest=56, CornerRadius=0, Margin="0,1") as internal
reference. Keep the `await Task.Yield()` rule — it still applies.

---

### 7. Update "ViewModel Checklist (list page)" section

No removals — all properties are still required on the ViewModel. Add one clarification
row to the table:

| `bool IsEmptyNoResults` | Used by CrudListView via ICrudListViewModel — no BindableProperty needed on the page |

Clarify that `IsEmptyNoItems` is **not** on the interface — it is passed from the page via
`CrudListView.IsEmptyNoItems` BindableProperty because the property name differs per entity.

---

## What the Subagent Must NOT Do

- Must not edit any `.cs`, `.xaml`, `.csproj`, `.sln`, or spec file
- Must not create new files — only edit `.claude/library/crud-pages.md`
- Must not remove existing content that is still valid (App Bar section, Form Page section, DI Registration section, Empty State section)
- Must not change the Three Laws section
- Must not rename existing section headers (other than adding "(old pattern)" or "reference" notes)

---

## Verification

After editing, the subagent must confirm:

1. All 7 instruction items above are reflected in the file
2. The new `CrudListView` section appears before "Spec-First Development"
3. The BindableProperties table is complete (13 rows)
4. The new page XAML skeleton compiles mentally — `views:CrudListView` element with all required attributes is present
5. No existing sections were deleted
6. Build is not required (docs-only edit), but the subagent must do a post-edit re-read of the full file
