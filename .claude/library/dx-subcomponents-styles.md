# DevExpress MAUI Component Patterns — ContentView BindableProperty wiring, Named Styles, ListItemLeadingMonogram, FilterChipGroup, BoxView.Color

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## ContentView sub-components — BindableProperty wiring patterns

Two confirmed patterns for wiring BindableProperties to XAML elements in `ContentView` components:

### Pattern A: XAML self-binding (AppBarBase subclasses)
Use when the BP value maps 1:1 to a single XAML property with no derived logic.
```xml
<dx:DXButton Icon="{Binding NavigationIcon, Source={x:Reference self}}" ... />
```

### Pattern B: propertyChanged callback → direct element set (ListItem)
Use when derived logic is needed (visibility toggle, alignment update, multi-element change).
```csharp
public static readonly BindableProperty OverlineProperty =
    BindableProperty.Create(nameof(Overline), typeof(string), typeof(ListItem), "",
        propertyChanged: (b, _, n) =>
        {
            var item = (ListItem)b;
            item.overlineLabel.Text = (string)n;
            item.overlineLabel.IsVisible = !string.IsNullOrEmpty((string)n);
        });
```

**Rule:** Prefer Pattern B when `propertyChanged` has any logic beyond a direct value pass-through. Never duplicate logic in both a XAML binding AND a `propertyChanged` callback — pick one.


---

## Named Styles — complete list

All defined in `MaterialStyles.xaml`.

| Key | TargetType | Purpose |
|---|---|---|
| `StandardIconButton` | `dx:DXButton` | Trailing/action icon buttons (48×48, OnSurfaceVariant) |
| `VibrantToolbarIconButton` | `dx:DXButton` | Icon buttons inside vibrant toolbar (48×48, OnSecondaryContainer). "Vibrant" is official M3 Expressive terminology. Project uses SecondaryContainer bg by design (official spec uses PrimaryContainer). |
| `NavigationIconButton` | `dx:DXButton` | Leading/nav icon buttons (48×48, OnSurface) |
| `Fab` | `dx:DXButton` | Floating action button (56×56, CornerRadius=16, Primary). Place to the RIGHT of FloatingToolbar in a shared `HorizontalStackLayout` — see `m3-components.md`. |
| `Divider` | `BoxView` | 1dp divider line (OutlineVariant). Uses `Color` not `BackgroundColor`. |
| `SkeletonBone` | `dx:DXBorder` | Shimmer skeleton bone (56dp, CornerRadius=0, SurfaceContainerHighest) |
| `BottomSheetDestructiveAction` | `dx:DXButton` | Destructive action in BottomSheet (Error text, Fill, 56dp) |
| `BottomSheetCancelAction` | `dx:DXButton` | Cancel in BottomSheet (Primary text, Fill, 56dp) |
| `EmptyStateHeadline` | `Label` | Headline in EmptyState (RobotoMedium 16sp, OnSurfaceVariant, centered) |
| `EmptyStateIllustration` | `dx:DXButton` | Icon in EmptyState (display-only, 80×80, 64dp icon) |
| `NavDrawerSectionHeader` | `Label` | Nav drawer group title (RobotoMedium 12sp, OnSurfaceVariant) |

## ListItemLeadingMonogram (formerly ListItemLeadingAvatar)

Renamed per MD3 terminology. MD3 distinguishes:
- **Monogram**: initials text in a circle — `ListItemLeadingMonogram`
- **Avatar**: photo/image of a person — not yet implemented

BindableProperties: `Initials` (string), `MonogramColor` (Color), `InitialsColor` (Color)

## FilterChipGroup

`dxe:FilterChipGroup` renders MD3 Filter Chips for toggling list filters.

**Namespace:** `xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"`

### With inline string items (no DisplayMember needed)

```xml
<dxe:FilterChipGroup SelectedItems="{Binding SelectedRoleFilters, Mode=TwoWay}"
                     Margin="16,4">
    <dxe:FilterChipGroup.ItemsSource>
        <x:Array Type="{x:Type x:String}">
            <x:String>Authors</x:String>
            <x:String>Performers</x:String>
        </x:Array>
    </dxe:FilterChipGroup.ItemsSource>
</dxe:FilterChipGroup>
```

### With ViewModel-bound items (use DisplayMember)

```xml
<dxe:FilterChipGroup ItemsSource="{Binding FilterItems}"
                     SelectedItems="{Binding SelectedItems, Mode=TwoWay}"
                     DisplayMember="DisplayName" />
```

### ViewModel binding type

`SelectedItems` expects `System.Collections.IList`. Initialize to `new List<object>()`.

```csharp
[ObservableProperty] private System.Collections.IList _selectedRoleFilters = new List<object>();

partial void OnSelectedRoleFiltersChanged(System.Collections.IList value)
{
    var selected = value?.Cast<string>().ToHashSet(StringComparer.Ordinal) ?? [];
    RoleFilter = (selected.Contains("Authors"), selected.Contains("Performers")) switch
    {
        (true, false)  => ArtistRoleFilter.AuthorsOnly,
        (false, true)  => ArtistRoleFilter.PerformersOnly,
        _              => ArtistRoleFilter.All
    };
}
```

### Layout note

Place FilterChipGroup in a separate `Auto` row above the collection view:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <dxe:FilterChipGroup Grid.Row="0" ... />
    <dxcv:DXCollectionView Grid.Row="1" ... />
</Grid>
```

Do NOT stack FilterChipGroup inside the ShimmerView — it must remain visible during loading.

---

## BoxView.Color vs BackgroundColor

Always use `BoxView.Color` — it is BoxView's own fill property.
`BackgroundColor` (from `VisualElement`) also renders but is semantically wrong.
The `Divider` named style uses `Color` canonically.
