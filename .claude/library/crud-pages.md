# CRUD Page Design Laws

> This document defines the **laws and decision guidance** for building list/form page pairs in MyVocaList.
> It is NOT a copy-paste template. It tells you **what is non-negotiable**, **what varies**, and **how to decide**.
>
> Reference implementation: `Docs/specs/venues/` (requirements + design + tasks — all confirmed working).

---

## The Three Laws (non-negotiable)

**1. MD3 compliance always.**
Every layout decision must be traceable to the Material Design 3 specification. No custom UI patterns that contradict MD3 — not for convenience, not for "it looks fine". If in doubt, check the spec first.

**2. Use existing custom components when the slot fits.**
`SmallAppBar`, `SearchAppBar`, `ListItem`, `ListItemLeadingIcon`, `ListItemLeadingAvatar`, `ListItemLeadingImage`, `FloatingToolbar` — these exist and are MD3-compliant. Use them. Only build a new component when no existing custom component covers the need.

**3. DevExpress first. Custom second.**
Always check `.claude/rules/devexpress-patterns.md` before reaching for a MAUI stock control or a custom component. A DX control that covers 90% of the need beats a custom component written from scratch. Build custom only when DX has no equivalent.

---

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

## Page migration checklist

Use when migrating an existing CRUD list page to `CrudListView` (or building a new one from scratch).

**XAML**
- [ ] Root element is `<pages:CrudListPageBase>` with `xmlns:pages="clr-namespace:MyVocaList.UI.Pages.Base"`
- [ ] `xmlns:views="clr-namespace:MyVocaList.UI.Components"` declared for CrudListView
- [ ] `SafeAreaEdges="Container"` present on the root element
- [ ] `Shell.BackButtonBehavior IsVisible="False" IsEnabled="False"` present
- [ ] `Shell.TitleView` contains `Grid` with `SmallAppBar` + `SearchAppBar`
- [ ] Single `<views:CrudListView>` element as page content — no manual ShimmerView, DXCollectionView, FloatingToolbar, EmptyState, or BottomSheet in the page XAML
- [ ] All required BindableProperties set (`ItemsSource`, `SelectedItemsSource`, `IsEmptyNoItems`, `EmptyNoItemsIllustration`, `EmptyNoItemsHeadline`, `FabCommand`, `FabDescription`)
- [ ] `ItemTemplate` and `SelectedItemTemplate` slots defined with entity-specific DataTemplates

**Code-behind**
- [ ] Class inherits `CrudListPageBase`
- [ ] `ListViewModel` abstract property implemented (`protected override ICrudListViewModel ListViewModel => _viewModel;`)
- [ ] `ViewModel` public property present for compiled-binding DataTemplates
- [ ] `AttachViewModel()` called from constructor
- [ ] No `OnCollectionViewScrolled`, `OnSelectionChanged`, or `OnConfirmSheetStateChanged` overrides — CrudListView owns these

**ViewModel**
- [ ] Inherits `CrudListViewModelBase<TDto>`
- [ ] All abstract methods implemented: `FetchPageAsync`, `FetchMoreAsync`, `ExecuteDeleteAsync`, `BuildDeleteConfirmMessage`, `NavigateToAddAsync`, `NavigateToEditAsync`, `RaiseEntityEmptyStateProperties`
- [ ] Entity-specific `IsEmptyNoXxx` bool property present (e.g. `IsEmptyNoVenues`) and raised inside `RaiseEntityEmptyStateProperties`
- [ ] `IList SelectedXxxRaw` non-generic wrapper property present for `SelectedItemsSource` binding

**DI**
- [ ] Page and ViewModel both registered as `AddTransient` in `MauiProgram.cs`

---

## Spec-First Development

Every new CRUD feature gets a spec before any code is written. Copy the structure from `Docs/specs/venues/` — three files:

| File | What it answers |
|------|----------------|
| `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/requirements.md` | What the feature must do. User stories, acceptance criteria, data model, validation rules, out-of-scope. |
| `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/design.md` | How it works technically. Architecture layers, interfaces, page structure, interaction flows, error handling, key decisions. |
| `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/tasks.md` | Ordered, checkboxed implementation steps. Checked off as work completes. |

The spec is the contract. Code that contradicts the spec is a bug or a spec update — one of the two must change.

**Requirement syntax (GEARS):** Write acceptance criteria as `shall` statements:
```
When [trigger], the [subject] shall [behavior].
While [state], the [subject] shall [behavior].
If [condition], then the [subject] shall [behavior].
```
One behavior per sentence. One sentence per line.

### Collaborative workflow — how to start a new CRUD with Claude

1. **Brainstorm** — invoke `superpowers:brainstorming`. Discuss the feature together: data model, UX flows, edge cases, approaches. Reach agreement on the design before any writing.
2. **Write spec** — Claude writes `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/requirements.md`, `design.md`, `tasks.md` based on the agreed design. User reviews and approves.
3. **Write plan** — invoke `superpowers:writing-plans`. Claude produces `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/plan.md` — the step-by-step implementation plan with code templates.
4. **Implement** — invoke `superpowers:executing-plans` (or `superpowers:subagent-driven-development`). Follow the plan task by task, building against the spec.
5. **Review** — invoke `superpowers:requesting-code-review` after each major task or phase.

Never skip straight to implementation. Brainstorm → Spec → Plan → Implement → Review, in that order.

---

## App Bar — Laws and Variants

### Law
Every list page uses `SmallAppBar` as the default title bar, placed in `Shell.TitleView`. `SearchAppBar` replaces it when search is active. Both components are always in the `Shell.TitleView` — a `Grid` wrapper with `IsVisible` toggling between them via `InverseBoolConverter`.

### Standard configuration (Venues reference)
```xml
<Shell.TitleView>
    <Grid>
        <appbars:SmallAppBar
            Title="{Binding AppBarTitle}"
            Action1Icon="search_outlined"
            Action1Command="{Binding OpenSearchCommand}"
            IsElevated="{Binding IsScrolled}"
            IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
        <appbars:SearchAppBar
            SearchText="{Binding SearchText, Mode=TwoWay}"
            Placeholder="Search [entity]..."
            BackCommand="{Binding CloseSearchCommand}"
            IsElevated="{Binding IsScrolled}"
            IsVisible="{Binding IsSearchMode}" />
    </Grid>
</Shell.TitleView>
```

### Variants — allowed adaptations

| Scenario | Adaptation |
|----------|-----------|
| Additional trailing actions (e.g. filter, sort) | Add `Action2Icon`/`Action2Command` (and `Action3` if needed) to `SmallAppBar` |
| No search on this page | Omit `SearchAppBar` entirely; omit `Action1` search icon |
| Complex filter (not simple text search) | Keep `SmallAppBar` with a filter icon that opens a bottom sheet or navigates to a filter page; **do not** embed a filter form in the app bar |
| Selection count in title | `AppBarTitle` derived property — "EntityName" when 0 selected, "N selected" when N ≥ 1. Always via `SmallAppBar.Title` binding, never a separate contextual bar. |
| Root tab page (no back button) | Omit `NavigationIcon` on `SmallAppBar` |
| Secondary page (has back) | Set `NavigationIcon="arrow_back_outlined"` + `NavigationCommand` |

### Never
- Do not build a custom title bar Grid in `Shell.TitleView` as a replacement for `SmallAppBar`. The old multi-select contextual bar pattern (5-column Grid) is retired.
- Do not place a search bar inside the page content area (below the app bar). Search belongs in `Shell.TitleView` via `SearchAppBar`.

---

## List Layout — Laws and Variants

> **Note:** Writing `DXCollectionView` directly in page XAML is the old pattern. As of Step 7, use
> `<views:CrudListView>` instead and pass entity-specific DataTemplates via `ItemTemplate` and
> `SelectedItemTemplate`. The standard configuration below is now internal to `CrudListView` — it is
> shown here as reference only.

### Law
All list rows use the `ListItem` component. No card layouts, no custom `DXBorder`-wrapped rows. `DXCollectionView` is always the container.

### Standard configuration
```xml
<dxcv:DXCollectionView
    SelectionMode="Multiple"
    IndicatorColor="{StaticResource Primary}"
    Margin="0,0,0,88"
    Scrolled="OnCollectionViewScrolled"
    SelectionChanged="OnSelectionChanged"
    ...>
    <dxcv:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="dto:MyDto">
            <lists:ListItem Headline="{Binding Name}" IsSelected="False">
                <!-- LeadingContent and TrailingContent adapt per page -->
            </lists:ListItem>
        </DataTemplate>
    </dxcv:DXCollectionView.ItemTemplate>
    <dxcv:DXCollectionView.SelectedItemTemplate>
        <DataTemplate x:DataType="dto:MyDto">
            <lists:ListItem Headline="{Binding Name}" IsSelected="True">
                <!-- Same structure, CheckEdit IsChecked="True", CheckedCheckBoxColor set -->
            </lists:ListItem>
        </DataTemplate>
    </dxcv:DXCollectionView.SelectedItemTemplate>
</dxcv:DXCollectionView>
```

### Variants — allowed adaptations

| Slot | Options | Notes |
|------|---------|-------|
| `LeadingContent` | `ListItemLeadingIcon` / `ListItemLeadingAvatar` / `ListItemLeadingImage` / omitted | Pick the M3 preset that matches the entity type |
| `Headline` | Any string binding | Required — always populated |
| `SupportingText` | Optional — add when a second line of info helps the user | Drives 2-line or 3-line list item height |
| `Overline` | Optional — label above headline | Use sparingly; reserved for category/type labels |
| `TrailingContent` | `CheckEdit` (selection) / independent action button / metadata label / omitted | Multi-action: use a `DXButton` with its own `Command` |
| Selection | Always-on `SelectionMode.Multiple` (hardcoded in XAML) | Do not add mode-toggle logic |

### Never
- Do not wrap list rows in `SwipeContainer` for delete. Delete is through the `FloatingToolbar`.
- Do not use `SelectionMode.None` or mode-toggle patterns. Selection is always on.
- Do not put `SelectionMode` in the ViewModel. It is a constant — hardcode it in XAML.

---

## FloatingToolbar — Laws and Variants

> **Note:** Writing `FloatingToolbar` + FAB directly in page XAML is the old pattern. As of Step 7,
> CrudListView owns the toolbar and FAB. The standard slot assignments (Action1=SelectAll,
> Action2=Edit, Action3=Delete) are hardcoded in CrudListView. For pages with different action
> needs, this section will be expanded when CrudListView gains configurable toolbar slots.

### Law
Every list page that has page-level actions uses `FloatingToolbar`, always centered at the bottom with `Margin="0,0,0,16"`. The toolbar is always visible — slots enable/disable via command `CanExecute`, not by hiding the toolbar.

### Standard slot assignments (Venues reference)
| Slot | Action | CanExecute |
|------|--------|-----------|
| Action1 | Select All (toggle) | Always enabled |
| Action2 | Edit | `SelectedCount == 1` |
| Action3 | Delete | `SelectedCount > 0` |

### Variants — allowed adaptations

| Scenario | Adaptation |
|----------|-----------|
| Different actions | Assign the slots that make sense for the entity. Slots 1–5 are available. |
| No edit (e.g. participation log) | Omit Edit slot; use remaining slots for other actions |
| Extra actions (share, archive, export) | Add Action4 / Action5 — still `FloatingToolbar`, same component |
| Read-only list | Omit `FloatingToolbar` entirely if there are no page-level actions |
| Select All not needed | Replace slot 1 with the most common action for this entity |

### IsSelected visual feedback
Always wire `ActionNIsSelected` to the relevant CanExecute property so the user sees which actions are active:
```xml
Action2IsSelected="{Binding CanEditSelected}"
Action3IsSelected="{Binding CanDeleteSelected}"
```

### FAB coexistence
FAB is placed to the RIGHT of `FloatingToolbar` inside a shared `HorizontalStackLayout`:
```xml
<HorizontalStackLayout HorizontalOptions="Center" VerticalOptions="End"
                       Margin="0,0,0,16" Spacing="8">
    <toolbars:FloatingToolbar VerticalOptions="Center" ... />
    <dx:DXButton Style="{StaticResource Fab}" Icon="add_outlined"
                 VerticalOptions="Center" Command="{Binding AddCommand}" />
</HorizontalStackLayout>
```
`DXCollectionView` bottom margin = `88` (max(toolbar 64, FAB 56) + 16 margin + 8 breathing).
Do NOT use separate overlays with Margin formulas — that was the old pattern.

---

## Form Page — Laws and Variants

### Law
Add/Edit forms are always separate Shell navigation pages. Never use a `BottomSheet` for a form that accepts keyboard input — the keyboard covers the sheet on Android.

### Standard layout
```xml
<ContentPage SafeAreaEdges="All" ...>
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">
            <!-- fields -->
            <!-- action buttons -->
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

`SafeAreaEdges="All"` + `ScrollView` handles keyboard avoidance automatically.

### Save/Cancel placement (full-screen forms)

**Law:** full-screen CRUD forms use a native Shell `ToolbarItem` for Save, in the top app bar's trailing slot — never an in-body button. The native Shell back button is the sole dismiss/discard action; no in-body Cancel button.

```xml
<ContentPage.ToolbarItems>
    <ToolbarItem Text="Save" Command="{Binding SaveCommand}" />
</ContentPage.ToolbarItems>
```

Rationale: Cancel is redundant with back-navigation once a form occupies the whole screen (it remains meaningful for bottom sheets/modals — see the sheet/modal form pattern, which keeps in-sheet Save/Cancel). Save reads better as a top-app-bar action per MD3's full-screen-dialog guidance. Full research + decision trail: `Docs/Management/DevCycleCraft/crud-form-action-pattern/design.md`.

**Currently non-compliant (as of 2026-07-12):** `ArtistFormPage`, `PersonFormPage`, `VenueFormPage` still use the old inline Cancel+Save pattern — they are pending a bottom-sheet/modal conversion decision (BACKLOG rows 43-45); only `SongFormPage` has been migrated to this law so far. Do not treat the other three as a bug — they are tracked separately. If a form's bottom-sheet conversion is later declined, migrate it to this ToolbarItem pattern as a follow-up task.

### Validation (law)
All form fields validate per the **Form Validation Standard** in `dialogs-validation.md` — validate on blur
(dirty fields), switch to keystroke-on-error so the error clears the moment it is fixed, and use Save as the
safety net for cross-field / uniqueness / DB checks. Errors are inline per field via `HasError`/`ErrorText`
only — never a summary, dialog, or snackbar. Validation rules live in `Validate<Field>Input` service methods
(business logic in Services), not in ViewModels or pages. This is a pointer; the standard is single-sourced in
`dialogs-validation.md`.

### Never
- Do not use `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` for validation or confirmation. See `dialogs-validation.md`.
- Do not validate on Save only (the "Wall of Red" anti-pattern) — see the Form Validation Standard in `dialogs-validation.md`.
- Do not use `FloatingToolbar` on a form page — it is hidden behind the keyboard on Android.

---

## ViewModel Checklist (list page)

Every list ViewModel shall have:

| Property / Command | Purpose |
|--------------------|---------|
| `ObservableRangeCollection<TDto> Items` | The list data |
| `ObservableRangeCollection<TDto> SelectedItems` | Selected rows |
| `IList SelectedItemsRaw` | Non-generic wrapper for DXCollectionView binding |
| `bool IsInitialLoading` | Drives `ShimmerView` |
| `bool IsRefreshing` | Drives pull-to-refresh |
| `bool HasMoreItems` | Drives load-more |
| `string SearchText` | Bound to `SearchAppBar` (if search is present) |
| `bool IsSearchMode` | Controls SmallAppBar ↔ SearchAppBar swap (if search is present) |
| `bool IsScrolled` | Drives `IsElevated` on both app bars |
| `int SelectedCount` | Updated by `OnSelectionChanged` in code-behind |
| `string AppBarTitle` | Derived: entity name or "N selected" |
| `bool CanEditSelected` | `SelectedCount == 1` (if edit is supported) |
| `bool CanDeleteSelected` | `SelectedCount > 0` (if delete is supported) |
| `BottomSheetState ConfirmSheetState` | Drives `ConfirmSheet` component (if destructive action exists) |
| `RefreshCommand`, `LoadMoreCommand`, `AddCommand` | Standard list commands |
| `EditSelectedCommand`, `DeleteSelectedCommand`, `SelectAllCommand` | Action commands (adapt to page needs) |
| `OpenSearchCommand`, `CloseSearchCommand` | Search commands (omit if no search) |

> **CrudListView binding notes:**
> - `bool IsEmptyNoResults` is part of `ICrudListViewModel` — CrudListView binds to it directly via BindingContext; no BindableProperty on the page is needed.
> - `bool IsEmptyNoItems` is **not** on the interface — it must be passed from the page via `CrudListView.IsEmptyNoItems` BindableProperty because the property name differs per entity (`IsEmptyNoVenues`, `IsEmptyNoArtists`, etc.).

---

## Code-Behind Checklist (list page)

Every list page code-behind shall have:

```csharp
public partial class MyEntityPage : CrudListPageBase
{
    private readonly MyEntityViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public MyEntityViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public MyEntityPage(MyEntityViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();   // subscribe PropertyChanged for OnBackButtonPressed logic
    }
}
```

`OnCollectionViewScrolled`, `OnSelectionChanged`, `OnConfirmSheetStateChanged`, and the SelectedItems wire-up are all handled internally by `CrudListView`. Do not add them to the page code-behind.

`OnAppearing` and `OnBackButtonPressed` are handled by `CrudListPageBase` — do not override them unless adding page-specific logic on top.

---

## Confirm-Delete BottomSheet

> **Note:** As of Step 7, the confirm BottomSheet lives inside `CrudListView`. Do not add a
> `dx:BottomSheet` to page XAML. The ViewModel properties `ConfirmSheetState`, `ConfirmMessage`,
> `ConfirmActionText`, `ConfirmActionCommand`, and `DismissConfirmCommand` are still required on
> the ViewModel — CrudListView binds them in its internal XAML directly via `BindingContext`
> (runtime binding — these are NOT on `ICrudListViewModel`; they resolve through the concrete VM).

Use the standard confirm BottomSheet for any destructive action. `HalfExpandedRatio=0.28` for single-message confirmations.

The VM holds `ConfirmSheetState`, `ConfirmMessage`, `ConfirmActionText`, `ConfirmActionCommand`, `DismissConfirmCommand`. CrudListView observes `ConfirmSheetState` via `ICrudListViewModel.PropertyChanged` to open/close the sheet internally — do not wire this in the page code-behind. The remaining four properties (`ConfirmMessage`, etc.) are bound by CrudListView's internal XAML via BindingContext and do not require the interface.

See `dialogs-validation.md` for the XAML snippet (internal to CrudListView — do not copy to page XAML).

---

## Shimmer Skeleton

> **Note:** As of Step 7, the shimmer skeleton is internal to `CrudListView`. Do not add
> `ShimmerView` or `SkeletonBone` elements to page XAML. The ViewModel `IsInitialLoading`
> property is still required — CrudListView binds it directly in its internal XAML via
> `BindingContext` (runtime binding — `IsInitialLoading` is NOT on `ICrudListViewModel`;
> it resolves through the concrete VM type at runtime).

Skeleton bones match the `ListItem` height: `HeightRequest="56"`, `CornerRadius="0"`, `Margin="0,1"` (1dp separator gap). Use 6 bones. Apply the `SkeletonBone` named style — no inline props needed (internal reference):

```xml
<dx:DXBorder Style="{StaticResource SkeletonBone}" />
```

Always `await Task.Yield()` before the first data fetch in `InitializeAsync()` so the shimmer renders before the load begins.

---

## DI Registration

| Type | Lifetime | Reason |
|------|----------|--------|
| List page | `AddTransient` | Fresh instance per navigation |
| Form page | `AddTransient` | Fresh instance per navigation |
| List ViewModel | `AddTransient` | Fresh instance per navigation |
| Form ViewModel | `AddTransient` | Fresh instance per navigation |
| Service | `AddScoped` | Per-lifetime scope |
| Repository | `AddScoped` | Per-lifetime scope |

---

## Empty State

Use the `EmptyState` component for all empty/no-results states:

```xml
<states:EmptyState
    Illustration="nightlife_outlined"
    Headline="No items registered"
    IsVisible="{Binding IsEmptyNoItems}"
    Margin="32,32,32,80" />
```

Namespace: `xmlns:states="clr-namespace:MyVocaList.UI.Components.States"`

Two standard ViewModel properties drive visibility:
- `IsEmptyNoItems` — no records exist at all
- `IsEmptyNoResults` — search returned no matches