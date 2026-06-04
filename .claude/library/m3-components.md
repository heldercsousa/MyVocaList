# M3 Components — AppBar Patterns

## MD3 Terminology Conventions

### "Body" means a structural slot — not text content
In MD3 component anatomy, **"body"** refers to a **structural container or slot**:
- Bottom sheet: Container → Header → **Body** (entire scrollable content area)
- Dialog: Container → Header → **Body** (supporting text + actions area)

**Never** name a BindableProperty `Body` for text content — it collides with MD3's container/slot meaning.

**Use `SupportingText` instead** — MD3's cross-component term for secondary descriptive text. Consistent across Lists, Cards, Chips, Dialogs, and Empty state (supporting text slot). Our existing `ListItem.SupportingText` already follows this.

### Complete MD3 type scale — MAUI StyleClass keys

| MD3 role | Style class | Family | sp | Weight |
|---|---|---|---|---|
| Display Large | `Display.Large` | RobotoRegular | 57 | Regular |
| Headline Large | `Headline.Large` | RobotoRegular | 32 | Regular |
| Title Large | `Title.Large` | RobotoRegular | 22 | Regular |
| Title Medium | `Title.Medium` | RobotoMedium | 16 | Medium |
| Body Large | `Body.Large` | RobotoRegular | 16 | Regular |
| Body Medium | `Body.Medium` | RobotoRegular | 14 | Regular |
| Body Small | `Body.Small` | RobotoRegular | 12 | Regular |
| Label Large | `Label.Large` | RobotoMedium | 14 | Medium |
| Label Medium | `Label.Medium` | RobotoMedium | 12 | Medium |
| Label Small | `Label.Small` | RobotoMedium | 11 | Medium |

> All 10 entries are defined in `MaterialStyles.xaml` as `StyleClass` entries. `Label.Small` weight is Medium per MD3 spec.

### Anatomy slot terms used in this codebase

| MD3 anatomy term | Used for |
|---|---|
| `Headline` | Primary text in list items, empty states, dialogs |
| `SupportingText` | Secondary/descriptive text (replaces "Body" for text) |
| `Illustration` | Icon or image in empty states |
| `LeadingContent` | Left slot in list items |
| `TrailingContent` | Right slot in list items |
| `Overline` | Label above headline in list items |

---

## M3 Small Top App Bar (Shell.TitleView context)

| Spec | Value | Token |
|---|---|---|
| Height | 64dp | HeightRequest="64" |
| Background (default) | Surface | `{StaticResource Surface}` |
| Background (scrolled) | SurfaceContainer | `{StaticResource SurfaceContainer}` |
| Leading icon color | OnSurface | `{StaticResource OnSurface}` |
| Trailing icon color | OnSurfaceVariant | `{StaticResource OnSurfaceVariant}` |
| Title typography | titleLarge: 22sp, RobotoRegular | `FontSize="22" FontFamily="RobotoRegular"` |
| Subtitle typography | bodyMedium: 14sp, RobotoRegular | `FontSize="14" FontFamily="RobotoRegular"` |
| Icon touch targets | 48×48dp | `WidthRequest="48" HeightRequest="48" CornerRadius="24"` |
| Corner radius | 0dp | No CornerRadius on container |
| Column layout | `Auto,*,Auto,Auto,Auto` | Col0=leading, Col1=headline, Col2-4=trailing |

## M3 Search App Bar (Shell.TitleView context — SearchAppBar)

Same container specs as Small Top App Bar (64dp, same columns). Only the center slot differs.

### Search input slot (TextEdit)
| Property | Value |
|---|---|
| Typography | bodyLarge: 16sp, RobotoRegular |
| BackgroundColor | Transparent |
| BorderColor | Transparent |
| FocusedBorderColor | Transparent |
| TextColor | OnSurface |
| PlaceholderColor | OnSurfaceVariant |
| ClearIconVisibility | Auto |
| ClearIconColor | OnSurfaceVariant |
| Keyboard | Text |
| ReturnType | Search |

### Leading icon behavior (code-behind)
```
Always: Icon = "arrow_back_outlined", SemanticDescription = "Back"

OnLeadingButtonClicked: SearchText = "", Unfocus(), BackCommand?.Execute(null)

OnIsVisible → true: searchEdit.Focus() — keyboard opens automatically
```

### BackCommand
- Always invoked when the back arrow is tapped
- ViewModel sets this to whatever navigation/state-reset is needed (e.g. IsSearchMode = false)

### Pattern: Search replaces app bar (secondary action via trailing icon)

**When:** A trailing search icon in SmallAppBar triggers IsSearchMode → SmallAppBar hides, SearchAppBar shows.

**MD3 rule (confirmed m3.material.io/components/search/guidelines):**
- Leading icon must be `arrow_back_outlined` **immediately** when SearchAppBar becomes visible — never `search_outlined`.
- "Focus is released when the back icon is selected" — tapping back dismisses search (returns to SmallAppBar), NOT page navigation.
- Auto-focus the text field when SearchAppBar becomes visible so the keyboard opens immediately.

**The `search → back on focus` transition** applies only to **persistent inline search bars** (always present, not replacing the app bar). Do not use it for the app-bar-swap pattern.

## M3 Search (standalone/detached — NOT yet implemented)

When a search bar appears inline inside page content (not in Shell.TitleView):

| Diff from SearchAppBar | Standalone value |
|---|---|
| Height | 56dp (not 64dp) |
| Shape | Pill: DXBorder CornerRadius="28" |
| Background | SurfaceContainerLow (not Surface) |
| Margins | 16dp horizontal |
| liftOnScroll elevated color | SurfaceContainerLow → SurfaceContainer |

All code-behind logic (leading icon toggle, BackCommand, TextEdit properties) is identical — reuse AppBarBase.

## Shared Base Class Pattern

**Problem**: MAUI XAML compiler generates `partial class X : ContentView` from `<ContentView>` root.
If code-behind declares `partial class X : AppBarBase`, CS0263 results.

**Fix**: Use the actual base class as the XAML root element:
```xml
<appbars:AppBarBase
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    x:Class="MyVocaList.UI.Components.AppBars.SmallAppBar"
    ...>
```
MAUI compiler then generates `partial class SmallAppBar : AppBarBase` — no conflict.

## AppBarBase — BindableProperty ownership

`AppBarBase` owns all shared BPs. The `declaringType` (3rd param) must be `typeof(AppBarBase)`:
```csharp
BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(AppBarBase), ...)
```

Subclass-specific BPs use `typeof(SmallAppBar)` / `typeof(SearchAppBar)` as declaring type.

## Token mapping (M3 canonical → codebase StaticResource names)

| M3 canonical name | Codebase token |
|---|---|
| colorSurface | Surface |
| colorSurfaceContainer | SurfaceContainer |
| colorSurfaceContainerLow | SurfaceContainerLow |
| colorOnSurface | OnSurface |
| colorOnSurfaceVariant | OnSurfaceVariant |
| colorOutline | Outline |
| colorOutlineVariant | OutlineVariant |
| colorPrimary | Primary |
| colorError | Error |

## Files

| File | Role |
|---|---|
| `MyVocaList/UI/Components/AppBars/AppBarBase.cs` | Shared base: IsElevated, Action1–3 slots |
| `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml/.cs` | Title + Subtitle + nav icon + trailing actions |
| `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml/.cs` | Search input + auto leading icon + trailing actions |

Namespace declaration for usage in pages:
```xml
xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
```

## M3 Lists — list item component

### Official M3 terminology
- Component: **Lists** (container = `DXCollectionView`, no wrapper needed)
- Row: **list item** → `ListItem` component
- Anatomy slots: **Container**, **Headline** (required), **Overline text** (optional),
  **Supporting text** (optional), **Leading element** (optional), **Trailing element** (optional)

### Set variants (determined by slot population)

| Set | Content | Height | Leading/Trailing alignment |
|---|---|---|---|
| 1-line | Headline only | ≈56dp | Center |
| 2-line | Headline + Supporting text (1 line), or Overline + Headline | ≈72dp | Center |
| 3-line | Headline + Supporting text (2 lines) | ≈88dp | **Top (8dp from top edge)** |

3-line rule: set `SupportingMaxLines="2"` on `ListItem` — leading/trailing/text column all top-align.

### Typography tokens

| Slot | M3 style | sp | Family | Color |
|---|---|---|---|---|
| Overline text | labelSmall | 11 | RobotoRegular | OnSurfaceVariant |
| Headline | bodyLarge | 16 | RobotoRegular | OnSurface |
| Supporting text | bodyMedium | 14 | RobotoRegular | OnSurfaceVariant |

### Interactive vs Non-interactive

- `IsInteractive="True"` (default): participates in DXCollectionView tap/ripple, keyboard focus
- `IsInteractive="False"`: `InputTransparent=True`, no state layer, display-only
  → Use for: section headers embedded in list, info-only rows, category labels

### Single-action vs Multi-action lists

- **Single-action**: entire row is one tap target → `DXCollectionView.Tap` event handles it
- **Multi-action**: row + independently tappable trailing element
  → Place `DXButton` (with its own `Command`) in `TrailingContent`
  → In MAUI, `InputTransparent=False` on a child element intercepts touch before DXCollectionView
  → No special component change — same `ListItem`

### Text-only selection — checkbox placement rule (M3)

> "Primary actions go LEFT. Secondary actions go RIGHT."

| Item type | Selection control slot | Reason |
|---|---|---|
| Text-only (no leading/trailing) | `TrailingContent` (RIGHT) | MD3 baseline spec: "With text and trailing checkbox" — trailing is the default selection control slot |
| With leading element (icon/avatar/image), no trailing action | `TrailingContent` (RIGHT) | Leading slot occupied by element; checkbox stays trailing — MD3 spec: "With leading icon and trailing checkbox" |
| With trailing action button (multi-action row) | `LeadingContent` (LEFT) | Trailing slot is taken by the independent action button; checkbox moves left — MD3 multi-action pattern. The leading icon is dropped to avoid crowding. |

**Multi-action row layout (e.g. Artist row with Catalog button):**
```xml
<lists:ListItem.LeadingContent>
    <!-- Checkbox is the ONLY leading element — person icon removed to avoid crowding -->
    <dx:CheckEdit IsChecked="False" InputTransparent="True" VerticalOptions="Center" />
</lists:ListItem.LeadingContent>
<lists:ListItem.TrailingContent>
    <!-- Catalog navigation button gets its own independent touch target -->
    <dx:DXButton Style="{StaticResource IconButton}"
                 Icon="queue_music_outlined"
                 InputTransparent="False" />
</lists:ListItem.TrailingContent>
```

`IsSelected=true` → container `BackgroundColor=SecondaryContainer` (applies regardless).

### Leading element presets

| Component | Size | Shape |
|---|---|---|
| `ListItemLeadingIcon` | 24dp icon / 40dp area | None |
| `ListItemLeadingAvatar` | 40dp circle | CornerRadius=20 |
| `ListItemLeadingImage` | 56dp square | CornerRadius=4 |

Namespace: `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"`

### Container padding and spacing

- Container: `Padding="16,0,24,0"` (16dp start, 24dp end)
- Leading slot: `Margin="0,0,16,0"` right-gap to text (invisible = zero space)
- Trailing slot: `Margin="8,0,0,0"` left-gap from text
- `MinimumHeightRequest="56"` on container Grid
- `ColumnSpacing="0"` (spacing managed via Margin on slots)

### Known gotchas
- `ColumnSpacing` applies even to invisible Auto columns → use `Margin` on slots instead
- 3-line `VerticalOptions=Start` must also have `Margin.Top=8` to match M3 8dp offset
- `LeadingContent` / `TrailingContent` are `View` BPs — set via XAML child element syntax:
  ```xml
  <lists:ListItem.LeadingContent>
      <lists:ListItemLeadingIcon Icon="person_outlined" />
  </lists:ListItem.LeadingContent>
  ```
- `IsSelected` drives row bg only — consumer must also update `CheckEdit.IsChecked` separately
- For multi-action trailing: bind via `Source={x:Reference page}` (compiled binding issue in DataTemplate)

---

## M3 Floating Toolbar

> **M3 Expressive component.** The floating toolbar (and "vibrant" color scheme) are M3 Expressive features — not in standard M3. "Vibrant" is the official spec term (confirmed via m3.material.io 2026-04-04).
>
> **Color design decision.** Official M3 Expressive vibrant uses `PrimaryContainer` bg + `OnPrimaryContainer` icons. This project uses `SecondaryContainer` bg + `OnSecondaryContainer` icons — a deliberate choice for better contrast in the indigo dark theme. The selected-state contrast (Primary bg + OnPrimary icon) applies against SecondaryContainer, not PrimaryContainer.
>
> **Height.** Confirmed from official MD3 measurements: floating toolbar = **64dp**. FAB = 56dp. Default padding = 8dp. Slot spacing = 4dp. FAB gap = 8dp. Screen margins: 16dp minimum left/right, 16dp above OS bottom bar (40dp total including OS bar). Confirmed via official spec images 2026-04-06.

### Spec

| Property | Value |
|---|---|
| Height | **64dp** (per official MD3 Expressive spec) |
| FAB height | 56dp |
| Width | Wrap content (auto-sizes to populated slots) |
| Padding (H, outside icon slots) | **8dp** |
| Slot spacing (between buttons) | **4dp** |
| FAB gap (toolbar ↔ FAB) | **8dp** (HorizontalStackLayout Spacing) |
| Background | **SecondaryContainer** (project vibrant — see note above) |
| Shape | CornerRadius = **32dp** (half of 64dp for full pill) |
| Screen margin left/right | **16dp minimum** |
| Screen margin bottom | **16dp** above OS bottom bar (40dp total including ~24dp OS nav bar) |
| Elevation | Omitted — tint conveys elevation in dark mode |
| Icon button tap zone | **48×48dp** (confirmed by spec: "minimum 48×48dp target area") |
| Icon size | 24dp (DXButton default) |
| Icon color (rest) | **OnSecondaryContainer** |
| Icon bg (selected) | **Primary** |
| Icon color (selected) | **OnPrimary** |
| Max slots | 5 |
| Scroll animation | NOT used — persistent by design (valid per M3 spec; animation adds threading risk) |
| Position | See FAB Coexistence below |

### Color tokens (dark mode — project vibrant)
| Token | Hex |
|---|---|
| SecondaryContainer | `#3F4566` |
| OnSecondaryContainer | `#AEB3DA` |
| Primary | `#BAC3FF` |
| OnPrimary | `#15267B` |

### When to use
- 2–5 page-specific actions (edit, delete, format, share, select-all)
- Use SmallAppBar trailing Action1–3 slots when ≤ 3 actions suffice
- Hide toolbar when contextual action bar (multi-select Shell.TitleView) is active

### When NOT to use
- Single action → use FAB instead
- Navigation actions → use Shell flyout
- ≤ 3 actions already covered by SmallAppBar trailing slots

### Anatomy
```
[DXBorder: pill, 64dp tall, CornerRadius=32, SecondaryContainer (vibrant), Padding="8,0"]
  └── HorizontalStackLayout (Spacing="4")
        ├── Slot 1: DXButton 48×48 (hidden if ActionNIcon empty)
        ├── Slot 2: DXButton 48×48
        ├── Slot 3: DXButton 48×48
        ├── Slot 4: DXButton 48×48
        └── Slot 5: DXButton 48×48
```

### Component
File: `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml` + `.xaml.cs`
Namespace: `xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"`

BindableProperties per slot (N = 1–5):
- `ActionNIcon` (string) — slot hidden when null/empty
- `ActionNCommand` (ICommand)
- `ActionNDescription` (string) — SemanticProperties.Description for TalkBack
- `ActionNIsSelected` (bool) — applies `Primary` bg + `OnPrimary` icon when selected (contrasts against vibrant bg)

### Page integration pattern

Toolbar and FAB are placed in a single `HorizontalStackLayout`, centered at the bottom.
FAB is to the RIGHT of the toolbar. `VerticalOptions=Center` on both aligns them vertically
(toolbar 64dp > FAB 56dp — center alignment keeps them visually level).

```xml
<!-- Root: single-cell Grid (toolbar+FAB row overlays content) -->
<Grid>
    <!-- Content list — bottom margin clears the combined bar -->
    <!-- Formula: max(toolbar 64dp, FAB 56dp) + 16dp margin + 8dp breathing = 88dp -->
    <dxcv:DXCollectionView Margin="0,0,0,88" ... />

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

**Never** use independent overlays (FAB at bottom-right with Margin formula, toolbar separately centered). That pattern is retired.

### FAB coexistence
FAB is placed to the RIGHT of `FloatingToolbar` inside a shared `HorizontalStackLayout`.
The whole unit is `HorizontalOptions=Center`, not independently positioned.
`DXCollectionView.Margin="0,0,0,88"` — max(toolbar 64dp, FAB 56dp) + 16dp margin + 8dp breathing.

### Accessibility
- Every `ActionNDescription` is mandatory — TalkBack reads it as the button label
- Touch targets: 48×48dp (DXButton WidthRequest/HeightRequest)
- Focus order: left-to-right (HorizontalStackLayout natural order)

### Scroll animation — deliberately omitted
`DXCollectionView.CollectionViewScrolled` fires every frame. Multiple overlapping
`TranslateTo` calls create animation jitter. Debounce via `CancellationTokenSource` adds
race condition risk between scroll callbacks and animation state. For a queue management
admin app the UX benefit is marginal. Persistent toolbar is valid per MD3 spec.

### Global using
Add `MyVocaList.UI.Components.Toolbars` to `GlobalUsings.cs` **only when 2+ pages use it**.
Reference the namespace directly per-page until then.

### No DX/MAUI built-in
DevExpress MAUI has no Toolbar component. .NET MAUI `ToolbarItem` adds to Shell top bar only.
FloatingToolbar is always a custom `ContentView` — same approach as AppBars.

---

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
