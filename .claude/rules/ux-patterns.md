# UX Patterns

## Touch Targets

All interactive controls must be at least **48×48dp** (WCAG 2.5.5 / MD3).

```xml
<dx:DXButton WidthRequest="48" HeightRequest="48" CornerRadius="24" ... />
```

Icon-only FABs use 56×56 with CornerRadius=16 (MD3 standard FAB shape).

## MD3 Contextual Action Bar (Multi-Select Mode)

Canonical layout: **5-column Grid** in `Shell.TitleView`.

| Column | Content | Width |
|--------|---------|-------|
| 0 | Cancel (X icon, far left) | Auto |
| 1 | Selection count label | * (expands) |
| 2 | Select All icon | Auto |
| 3 | Edit icon (conditional on single selection) | Auto |
| 4 | Delete icon | Auto |

```xml
<Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto"
      ColumnSpacing="4" Margin="0,0,16,0"
      VerticalOptions="Center"
      IsVisible="{Binding ShowMultiSelectToolbar}">

    <!-- Cancel — X icon, far left -->
    <dx:DXButton Grid.Column="0" Icon="close_outlined"
                 WidthRequest="48" HeightRequest="48" CornerRadius="24"
                 BackgroundColor="Transparent"
                 IconColor="{StaticResource OnSurface}"
                 HorizontalContentAlignment="Center"
                 SemanticProperties.Description="Cancel selection"
                 Command="{Binding CancelSelectionCommand}" />

    <!-- Count -->
    <Label Grid.Column="1" Text="{Binding SelectedCountText}"
           FontFamily="RobotoMedium" FontSize="18"
           TextColor="{StaticResource OnSurface}" VerticalOptions="Center" />

    <!-- Select All -->
    <dx:DXButton Grid.Column="2" Icon="done_all_outlined"
                 WidthRequest="48" HeightRequest="48" CornerRadius="24"
                 BackgroundColor="Transparent"
                 IconColor="{StaticResource OnSurface}"
                 HorizontalContentAlignment="Center"
                 SemanticProperties.Description="Select all"
                 Command="{Binding SelectAllCommand}" />

    <!-- Edit (single-select only) -->
    <dx:DXButton Grid.Column="3" Icon="edit_outlined"
                 WidthRequest="48" HeightRequest="48" CornerRadius="24"
                 BackgroundColor="Transparent"
                 IconColor="{StaticResource OnSurface}"
                 HorizontalContentAlignment="Center"
                 IsVisible="{Binding CanEditSelected}"
                 SemanticProperties.Description="Edit selected"
                 Command="{Binding EditSelectedCommand}" />

    <!-- Delete -->
    <dx:DXButton Grid.Column="4" Icon="delete_outlined"
                 WidthRequest="48" HeightRequest="48" CornerRadius="24"
                 BackgroundColor="Transparent"
                 IconColor="{StaticResource Error}"
                 HorizontalContentAlignment="Center"
                 SemanticProperties.Description="Delete selected"
                 Command="{Binding DeleteSelectedCommand}" />
</Grid>
```

**Toolbar right margin:** `Margin="0,0,16,0"` on the Grid — matches Shell's default title padding.

## Form Action Buttons vs Toolbar Action Icons

These are intentionally different — do not "unify" them:

| Context | Pattern | Reason |
|---------|---------|--------|
| Add/Edit form (Shell nav page) | Labeled buttons — `Cancel` + `Save` | User needs label clarity when entering data |
| Contextual action bar (multi-select) | Icon-only buttons (48×48) | Space is constrained; actions are reversible/confirmable |

Both are correct per MD3. Labeled toolbar actions (`Content="Select all"`) are acceptable
only when icon meaning is ambiguous.

## Multi-Select Mode Behavior

- **Enter**: Long press on an item → `EnterMultiSelectMode(item)` → item is pre-selected → haptic feedback
- **Tap in multi-select**: DXCollectionView handles selection natively — do NOT double-toggle in `OnItemTapped`
- **Tap in normal mode**: Navigate to detail/edit page
- **Select All toggle**: If all selected → deselect all but **remain in multi-select mode** (do not exit)
- **Exit**: Cancel button or selection count drops to 0 via selection event (suppress exit when programmatically clearing)
- **Swipe suppressed during multi-select**: `OnSwipeItemShowing` → `e.Cancel = true` when `IsMultiSelectMode`

```csharp
// Select All — stays in multi-select even when deselecting
private void ToggleSelectAll()
{
    if (IsAllSelected)
    {
        _suppressSelectionChangedExit = true;
        RunOnUiThread(() =>
        {
            SelectedItems.ClearRange();
            _suppressSelectionChangedExit = false;
        });
        SelectedCount = 0;
        return;
    }
    IsMultiSelectMode = true;
    _suppressSelectionChangedExit = true;
    RunOnUiThread(() =>
    {
        SelectedItems.ReplaceRange([.. Items]);
        _suppressSelectionChangedExit = false;
    });
    SelectedCount = Items.Count;
}
```

## Empty State Positioning

Empty state containers must be **vertically centered**, not top-aligned:

```xml
<VerticalStackLayout VerticalOptions="Center"
                     HorizontalOptions="Center"
                     Spacing="8" Margin="32">
    <!-- icon, heading, subtitle -->
</VerticalStackLayout>
```

Never use `VerticalOptions="Start"` for empty/error/loading placeholder states.

## Badge / Chip Pattern (event count)

Use `dx:DXBorder` + `Label` for informational badges. Show count, not just a binary flag:

```xml
<dx:DXBorder IsVisible="{Binding HasEvents}"
             BackgroundColor="{StaticResource SecondaryContainer}"
             CornerRadius="12"
             Padding="8,4">
    <Label Text="{Binding EventCount, StringFormat='{0} events'}"
           FontFamily="RobotoMedium" FontSize="11"
           TextColor="{StaticResource OnSecondaryContainer}"
           VerticalOptions="Center" />
</dx:DXBorder>
```

`HasEvents` is a derived property (`EventCount > 0`) on the DTO — not a separate DB column.
The repository counts via `v.Events.Count()` in the EF Core projection (translated to SQL COUNT).
