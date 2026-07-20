# CRUD Page Design Laws — App Bar, List Layout, FloatingToolbar — laws and variants

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## App Bar — Laws and Variants

### Law
Every list page uses `SmallAppBar` as the sole `Shell.TitleView` occupant (no search-mode toggle, no wrapper `Grid`). Search is a persistent `SearchBar` (MD3 "Search bar", standalone/docked) docked at Row 0 of `CrudListView` — always visible, never replacing the app bar. `SearchAppBar` (Shell.TitleView bar-swap) is retired for CRUD list pages; it remains in use only by the 4 picker pages (`SongPickerPage`, `ArtistPickerPage`, `QueueSongPickerPage`, `YouTubeSearchPage`) pending their own migration (BACKLOG follow-up).

### Standard configuration (Venues reference)
```xml
<Shell.TitleView>
    <appbars:SmallAppBar
        Title="{Binding AppBarTitle}"
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
...
<views:CrudListView
    ItemsSource="{Binding Venues}"
    SearchText="{Binding SearchText, Mode=TwoWay}"
    SearchPlaceholder="Search venues..."
    ... />
```
`CrudListView` owns the `SearchBar` internally (Row 0 of its root Grid) and propagates `SearchText`/`SearchPlaceholder` to it. Pages never place a `SearchBar` element directly in page XAML.

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
- Do not add `SearchAppBar`, `IsSearchMode`, or a TitleView `Grid`+toggle to a CRUD list page. Search is the persistent `SearchBar` inside `CrudListView` — see the Law above.

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
