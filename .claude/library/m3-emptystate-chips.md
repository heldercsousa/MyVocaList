# M3 Components — M3 Empty State + Filter Chip

> Section file split from `m3-components.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `m3-components.md`.

## M3 Empty State component

| Slot | BindableProperty | Control | Style |
|---|---|---|---|
| Illustration | `Illustration` (string icon name) | `dx:DXButton` (display-only) | `EmptyStateIllustration` |
| Headline | `Headline` (string) | `Label` | `EmptyStateHeadline` |
| Supporting text | `SupportingText` (string, optional) | `Label` | `Body.Medium` + `OnSurfaceVariant` |

Usage:
```xml
<states:EmptyState
    Illustration="nightlife_outlined"
    Headline="No items yet"
    IsVisible="{Binding IsEmpty}"
    Margin="32,32,32,80" />
```

Namespace: `xmlns:states="clr-namespace:MyVocaList.UI.Components.States"`

### NavDrawer section header typography fix
- **Was:** `RobotoMedium 14sp` (= Label Large)
- **Correct:** `RobotoMedium 12sp` = Label Medium per MD3 Navigation Drawer spec
- **In code:** `Style="{StaticResource NavDrawerSectionHeader}"` (sets 12sp Medium)

---

## Filter Chip (MD3)

**MD3 reference:** m3.material.io/components/chips/overview — Filter variant
**DevExpress component:** `dxe:FilterChipGroup` (see `devexpress-patterns.md § FilterChipGroup`)

### When to use Filter Chips

- Toggling a list between discrete views or subsets (e.g. Authors / Performers)
- Multiple filter chips can be selected simultaneously (non-exclusive by default)
- Place directly below the TopAppBar / TitleView, above the list content

### Layout pattern (Filter Chip row above list)

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- chip row -->
        <RowDefinition Height="*" />     <!-- list -->
    </Grid.RowDefinitions>

    <dxe:FilterChipGroup Grid.Row="0"
                         SelectedItems="{Binding SelectedFilters, Mode=TwoWay}"
                         Margin="16,4">
        <dxe:FilterChipGroup.ItemsSource>
            <x:Array Type="{x:Type x:String}">
                <x:String>Label A</x:String>
                <x:String>Label B</x:String>
            </x:Array>
        </dxe:FilterChipGroup.ItemsSource>
    </dxe:FilterChipGroup>

    <dxcv:DXCollectionView Grid.Row="1" ... />
</Grid>
```

### ViewModel: chip selection → domain enum mapping

```csharp
partial void OnSelectedFiltersChanged(System.Collections.IList value)
{
    var selected = value?.Cast<string>().ToHashSet(StringComparer.Ordinal) ?? [];
    Filter = (selected.Contains("Label A"), selected.Contains("Label B")) switch
    {
        (true, false) => MyFilter.AOnly,
        (false, true) => MyFilter.BOnly,
        _             => MyFilter.All
    };
}
```

### MD3 terminology

| Term | Meaning |
|------|---------|
| Filter chip | Chip that filters a content set; can be selected/deselected |
| Filter chip group | Row of filter chips (one or more active at a time) |
| Selected state | Chip is highlighted; contributes to the active filter |
