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

## Spec-First Development

Every new CRUD feature gets a spec before any code is written. Copy the structure from `Docs/specs/venues/` — three files:

| File | What it answers |
|------|----------------|
| `Docs/specs/[feature]/requirements.md` | What the feature must do. User stories, acceptance criteria, data model, validation rules, out-of-scope. |
| `Docs/specs/[feature]/design.md` | How it works technically. Architecture layers, interfaces, page structure, interaction flows, error handling, key decisions. |
| `Docs/specs/[feature]/tasks.md` | Ordered, checkboxed implementation steps. Checked off as work completes. |

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
2. **Write spec** — Claude writes `Docs/specs/[feature]/requirements.md`, `design.md`, `tasks.md` based on the agreed design. User reviews and approves.
3. **Write plan** — invoke `superpowers:writing-plans`. Claude produces `Docs/superpowers/plans/YYYY-MM-DD-[feature].md` — the step-by-step implementation plan with code templates.
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

### Law
All list rows use the `ListItem` component. No card layouts, no custom `DXBorder`-wrapped rows. `DXCollectionView` is always the container.

### Standard configuration
```xml
<dxcv:DXCollectionView
    SelectionMode="Multiple"
    IndicatorColor="{StaticResource Primary}"
    Margin="0,0,0,80"
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
FAB stays at bottom-right, independent of `FloatingToolbar`:
```xml
Margin="0,0,16,88"   <!-- 48 toolbar + 16 toolbar margin + 24 gap -->
```
`DXCollectionView` bottom margin = `80` (`48 + 16 + 16 breathing`).

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

### Action buttons — when to use inline labeled buttons

Use `HorizontalStackLayout(End)` with `OutlinedButton("Cancel")` + `FilledButton("Save")` when:
- The form is simple (1–3 fields)
- The buttons appear below the last field in the scroll flow

```xml
<HorizontalStackLayout HorizontalOptions="End" Spacing="8">
    <dx:DXButton Content="Cancel" Style="{StaticResource OutlinedButton}" Padding="24,0"
                 Command="{Binding CancelCommand}" />
    <dx:DXButton Content="Save" Style="{StaticResource FilledButton}" Padding="24,0"
                 Command="{Binding SaveCommand}" />
</HorizontalStackLayout>
```

### Action buttons — when to use a sticky bottom bar

Use a pinned bottom action bar (outside the `ScrollView`, inside a `Grid`) when:
- The form has many fields and the buttons would scroll out of view
- The user needs persistent access to Save/Cancel regardless of scroll position

```xml
<Grid RowDefinitions="*,Auto">
    <ScrollView Grid.Row="0">
        <VerticalStackLayout Padding="24" Spacing="16">
            <!-- fields only, no buttons here -->
        </VerticalStackLayout>
    </ScrollView>
    <HorizontalStackLayout Grid.Row="1" HorizontalOptions="End" Spacing="8" Padding="24,12">
        <dx:DXButton Content="Cancel" Style="{StaticResource OutlinedButton}" Padding="24,0"
                     Command="{Binding CancelCommand}" />
        <dx:DXButton Content="Save" Style="{StaticResource FilledButton}" Padding="24,0"
                     Command="{Binding SaveCommand}" />
    </HorizontalStackLayout>
</Grid>
```

### Never
- Do not use `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` for validation or confirmation. See `dialogs-validation.md`.
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

---

## Code-Behind Checklist (list page)

Every list page code-behind shall have:

```csharp
// Typed ViewModel property — required for compiled bindings in DataTemplates
public MyViewModel ViewModel => _viewModel;

protected override void OnAppearing()
{
    base.OnAppearing();
    collectionView.SelectedItems = _viewModel.SelectedItemsRaw;  // IList assignment
    _ = _viewModel.InitializeAsync();
}

private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
{
    _viewModel.IsScrolled = e.Offset > 0;   // NOT e.VerticalOffset — confirmed API
}

private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
{
    var count = (collectionView.SelectedItems as ICollection)?.Count ?? 0;
    _viewModel.OnSelectionChanged(count);
}

protected override bool OnBackButtonPressed()
{
    // Priority: confirm sheet first → search → default Shell behavior
    if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
    {
        _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
        return true;
    }
    if (_viewModel.IsSearchMode)
    {
        _viewModel.CloseSearchCommand.Execute(null);
        return true;
    }
    return false;
}
```

---

## Confirm-Delete BottomSheet

Use the standard confirm BottomSheet for any destructive action. `HalfExpandedRatio=0.28` for single-message confirmations.

The VM holds `ConfirmSheetState`, `ConfirmMessage`, `ConfirmActionText`, `ConfirmActionCommand`, `DismissConfirmCommand`. Code-behind observes `ConfirmSheetState` via `PropertyChanged` to open/close the sheet.

See `dialogs-validation.md` for the full XAML snippet.

---

## Shimmer Skeleton

Skeleton bones match the `ListItem` height: `HeightRequest="56"`, `CornerRadius="0"`, `Margin="0,1"` (1dp separator gap). Use 6 bones. Apply the `SkeletonBone` named style — no inline props needed:

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
