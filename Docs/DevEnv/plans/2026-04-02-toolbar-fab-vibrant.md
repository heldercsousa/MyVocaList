# Toolbar Vibrant Color + FAB Coexistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct the FloatingToolbar to use the MD3 vibrant color scheme (SecondaryContainer background) and place the FAB beside the toolbar in a single centered row — not independently at bottom-right.

**Architecture:** The FloatingToolbar component's internal colors are updated (bg + icon defaults + selected state). VenuesPage layout is restructured from two independent overlays to one `HorizontalStackLayout` wrapper containing both toolbar and FAB, centered at the bottom. The rules files are updated to lock in the confirmed spec.

**Tech Stack:** .NET MAUI 10 · DevExpress MAUI v25.2.4 · C# 13 · XAML

---

## Files

| File | Change |
|---|---|
| `MyVocaList/Resources/Styles/MaterialStyles.xaml` | Add `VibrantToolbarIconButton` style |
| `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml` | Change bg → `SecondaryContainer`, use `VibrantToolbarIconButton` |
| `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml.cs` | Update `ApplySelectedState` for vibrant bg |
| `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` | Replace two independent overlays with combined `HorizontalStackLayout` row |
| `.claude/rules/m3-components.md` | Fix M3 Floating Toolbar spec: vibrant color, FAB coexistence layout |
| `.claude/rules/crud-pages.md` | Fix FAB coexistence section with correct layout pattern |
| `.claude/rules/devexpress-patterns.md` | Update toolbar section with vibrant pattern |

---

## MD3 Spec — What the Correct Design Is

From the MD3 toolbar guidelines (confirmed against official m3.material.io images):

| Property | Standard | **Vibrant (required for this app)** |
|---|---|---|
| Background | `SurfaceContainerHigh` | **`SecondaryContainer`** |
| Default icon color | `OnSurfaceVariant` | **`OnSecondaryContainer`** |
| Selected icon bg | `SecondaryContainer` | **`Primary`** (contrast against vibrant bg) |
| Selected icon color | `OnSecondaryContainer` | **`OnPrimary`** |
| Height | 48dp | 48dp |
| Shape | CornerRadius=24 (pill) | CornerRadius=24 (pill) |

**FAB coexistence layout (confirmed from MD3 image):**
- Toolbar and FAB are placed in a single `HorizontalStackLayout` (not independent overlays)
- `HorizontalOptions=Center VerticalOptions=End Margin="0,0,0,16"`  
- FAB is to the RIGHT of the toolbar, `VerticalOptions=Center`, `Spacing=8`
- FAB height (56dp) > toolbar height (48dp), so `VerticalOptions=Center` aligns them
- `DXCollectionView.Margin="0,0,0,80"` — unchanged: max(56,48) + 16 + 8 = 80dp

---

## Task 1 — Add `VibrantToolbarIconButton` named style

**File:** `MyVocaList/Resources/Styles/MaterialStyles.xaml`

- [ ] Open `MaterialStyles.xaml` and locate the `StandardIconButton` style (currently at ~line 164)

- [ ] Add the new style immediately after `StandardIconButton`:

```xml
<!-- MD3: Vibrant toolbar icon button — for use inside SecondaryContainer toolbar -->
<Style x:Key="VibrantToolbarIconButton" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="IconColor" Value="{StaticResource OnSecondaryContainer}" />
    <Setter Property="PressedBackgroundColor" Value="{StaticResource SecondaryContainer}" />
    <Setter Property="WidthRequest" Value="48" />
    <Setter Property="HeightRequest" Value="48" />
    <Setter Property="CornerRadius" Value="24" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
</Style>
```

- [ ] Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore`
  Expected: `0 Error(s)`

---

## Task 2 — Update FloatingToolbar.xaml (vibrant colors)

**File:** `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml`

- [ ] Replace the `DXBorder BackgroundColor` and all five button styles:

**Old `DXBorder` opening tag:**
```xml
<dx:DXBorder CornerRadius="24"
             HeightRequest="48"
             BackgroundColor="{StaticResource SurfaceContainerHigh}"
             Padding="4,0">
```

**New:**
```xml
<dx:DXBorder CornerRadius="24"
             HeightRequest="48"
             BackgroundColor="{StaticResource SecondaryContainer}"
             Padding="4,0">
```

- [ ] Replace all five `Style="{StaticResource StandardIconButton}"` with `Style="{StaticResource VibrantToolbarIconButton}"`:
  - `action1Button`
  - `action2Button`
  - `action3Button`
  - `action4Button`
  - `action5Button`

- [ ] Update the XAML comment block at the top to reflect vibrant:

```xml
<!--
    M3 Floating Toolbar — pill container with up to 5 icon-only action slots.
    Height: 48dp · Shape: CornerRadius=24 (pill) · Background: SecondaryContainer (vibrant)
    Icon color: OnSecondaryContainer (default) · Selected: Primary bg + OnPrimary icon
    Slots with null/empty ActionNIcon are hidden and collapse automatically.
-->
```

- [ ] Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore`
  Expected: `0 Error(s)`

---

## Task 3 — Update FloatingToolbar.xaml.cs (selected state for vibrant bg)

**File:** `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml.cs`

The current `ApplySelectedState` applies `SecondaryContainer` bg when selected — which is invisible on a `SecondaryContainer` toolbar background. Fix: use `Primary` bg + `OnPrimary` icon for selected state.

- [ ] Replace the `ApplySelectedState` method:

```csharp
/// <summary>Applies the MD3 selected/unselected state colors to a vibrant toolbar icon button.</summary>
private void ApplySelectedState(DXButton button, bool isSelected)
{
    // Vibrant toolbar bg = SecondaryContainer.
    // Selected: Primary bg + OnPrimary icon (contrasts against SecondaryContainer).
    // Unselected: transparent bg + OnSecondaryContainer icon (default).
    var bgKey = isSelected ? "Primary" : null;
    var iconKey = isSelected ? "OnPrimary" : "OnSecondaryContainer";

    button.BackgroundColor = bgKey != null &&
        Application.Current?.Resources.TryGetValue(bgKey, out var bg) == true
        ? (Color)bg
        : Colors.Transparent;

    if (Application.Current?.Resources.TryGetValue(iconKey, out var iconColor) == true)
        button.IconColor = (Color)iconColor;
}
```

- [ ] Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore`
  Expected: `0 Error(s)`

---

## Task 4 — Update VenuesPage.xaml (side-by-side toolbar + FAB)

**File:** `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`

The current layout has two independent Grid overlays (FAB at bottom-right, toolbar centered). Replace them with a single `HorizontalStackLayout` that holds both, centered at the bottom.

- [ ] Remove the existing FloatingToolbar declaration AND the FAB declaration (both overlays) and replace with:

```xml
<!-- Toolbar + FAB: centered row, 16dp from bottom (MD3 vibrant toolbar + FAB coexistence) -->
<!-- Combined height: max(FAB 56dp, toolbar 48dp) = 56dp + 16dp margin = 72dp clearance -->
<HorizontalStackLayout HorizontalOptions="Center"
                       VerticalOptions="End"
                       Margin="0,0,0,16"
                       Spacing="8">
    <toolbars:FloatingToolbar
        VerticalOptions="Center"
        Action1Icon="done_all_outlined"
        Action1Command="{Binding SelectAllCommand}"
        Action1Description="Select all"
        Action1IsSelected="{Binding IsAllSelected}"
        Action2Icon="edit_outlined"
        Action2Command="{Binding EditSelectedCommand}"
        Action2Description="Edit selected"
        Action2IsSelected="{Binding CanEditSelected}"
        Action3Icon="delete_outlined"
        Action3Command="{Binding DeleteSelectedCommand}"
        Action3Description="Delete selected"
        Action3IsSelected="{Binding CanDeleteSelected}" />
    <dx:DXButton Style="{StaticResource Fab}"
                 Icon="add_outlined"
                 VerticalOptions="Center"
                 SemanticProperties.Description="Add venue"
                 Command="{Binding AddVenueCommand}" />
</HorizontalStackLayout>
```

- [ ] Verify `DXCollectionView` still has `Margin="0,0,0,80"` — no change needed (80dp = 56 + 16 + 8 breathing).

- [ ] Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore`
  Expected: `0 Error(s)`

---

## Task 5 — Update rules: m3-components.md

**File:** `.claude/rules/m3-components.md`

Locate the `## M3 Floating Toolbar` section and replace the **Spec table** and **color tokens** sections with corrected values:

- [ ] Replace the `### Spec` table:

```markdown
### Spec

| Property | Value |
|---|---|
| Height | 48dp |
| Width | Wrap content (auto-sizes to populated slots) |
| Padding (H, outside icon slots) | 4dp |
| Background | **SecondaryContainer** (vibrant — project standard) |
| Shape | CornerRadius = 24dp (full pill) |
| Elevation | Level 3 per spec — **omitted** (tint conveys elevation in dark mode) |
| Icon button tap zone | 48×48dp, CornerRadius=24 |
| Icon size | 24dp (DXButton default) |
| Icon color (rest) | **OnSecondaryContainer** |
| Icon bg (selected) | **Primary** |
| Icon color (selected) | **OnPrimary** |
| Max slots | 5 |
| Scroll animation | NOT used — persistent by design |
| Position | See FAB Coexistence below |
```

- [ ] Replace the **Color tokens (dark mode)** section:

```markdown
### Color tokens (dark mode — vibrant)
| Token | Hex |
|---|---|
| SecondaryContainer | `#3F4566` |
| OnSecondaryContainer | `#AEB3DA` |
| Primary | `#BAC3FF` |
| OnPrimary | `#15267B` |
```

- [ ] Replace the **Page integration pattern** (entire code block under `### Page integration pattern`):

```markdown
### Page integration pattern

Toolbar and FAB are placed in a single `HorizontalStackLayout`, centered at the bottom.
FAB is to the right of the toolbar. `VerticalOptions=Center` on both aligns them
(FAB 56dp > toolbar 48dp).

```xml
<!-- Root: single-cell Grid (toolbar+FAB row overlays content) -->
<Grid>
    <!-- Content list — bottom margin keeps last item above the combined bar -->
    <!-- Formula: max(FAB 56, toolbar 48) + 16 margin + 8 breathing = 80dp -->
    <dxcv:DXCollectionView Margin="0,0,0,80" ... />

    <!-- Combined toolbar + FAB row — centered, 16dp above safe area bottom -->
    <HorizontalStackLayout HorizontalOptions="Center"
                           VerticalOptions="End"
                           Margin="0,0,0,16"
                           Spacing="8">
        <toolbars:FloatingToolbar
            VerticalOptions="Center"
            Action1Icon="edit_outlined"
            Action1Command="{Binding EditCommand}"
            Action1Description="Edit"
            Action2Icon="delete_outlined"
            Action2Command="{Binding DeleteCommand}"
            Action2Description="Delete" />
        <dx:DXButton Style="{StaticResource Fab}"
                     Icon="add_outlined"
                     VerticalOptions="Center"
                     Command="{Binding AddCommand}" />
    </HorizontalStackLayout>
</Grid>
```
```

- [ ] Remove the old `### FAB coexistence` subsection entirely (it described the independent-overlay pattern).

---

## Task 6 — Update rules: crud-pages.md

**File:** `.claude/rules/crud-pages.md`

- [ ] Replace the `### FAB coexistence` block under the FloatingToolbar section:

```markdown
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
`DXCollectionView` bottom margin = `80` (max(FAB 56, toolbar 48) + 16 margin + 8 breathing).
Do NOT use separate overlays with Margin formulas — that was the old pattern.
```

---

## Task 7 — Update rules: devexpress-patterns.md

**File:** `.claude/rules/devexpress-patterns.md`

- [ ] Locate the `## MD3 App Bar Components` or the icon-only button example showing `Margin="0,0,16,88"` in the FAB coexistence note and update any stale Margin formula references.

- [ ] Find the named styles table row for `Fab` and verify the description still reads correctly (it does — `Fab` style itself doesn't encode position).

- [ ] Add a note to the `## DXButton` section under FAB:

```markdown
**FAB + FloatingToolbar coexistence:** Place both inside a `HorizontalStackLayout` (FAB to the right),
not as independent Grid overlays. See `m3-components.md` → M3 Floating Toolbar → Page integration pattern.
```

---

## Self-Review

**Spec coverage:**
- ✅ Vibrant bg (`SecondaryContainer`) — Task 2
- ✅ Correct icon color (`OnSecondaryContainer`) — Tasks 1 + 2  
- ✅ Correct selected state (`Primary` / `OnPrimary`) — Task 3
- ✅ FAB beside toolbar, centered row — Task 4
- ✅ Rules updated so future pages get it right — Tasks 5–7

**Placeholder scan:** None.

**Type consistency:**
- `VibrantToolbarIconButton` defined in Task 1, referenced in Task 2 ✅
- `ApplySelectedState` keys `"Primary"` / `"OnPrimary"` / `"OnSecondaryContainer"` all exist in `MaterialColors.xaml` ✅
- `HorizontalStackLayout` with `Spacing="8"` — standard MAUI layout, no DX dep ✅
