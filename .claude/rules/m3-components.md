# M3 Components — AppBar Patterns

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

### Leading icon auto-behavior (code-behind)
```
Search state (default):     Icon = "search_outlined",      SemanticDescription = "Search"
Active state (focused/text): Icon = "arrow_back_outlined",  SemanticDescription = "Back"

OnFocused  → _isSearchFocused = true  → UpdateLeadingIcon()
OnUnfocused → _isSearchFocused = false → UpdateLeadingIcon()
SearchText changed → UpdateLeadingIcon()

OnLeadingButtonClicked:
  if focused OR has text → SearchText = "", Unfocus(), BackCommand?.Execute(null)
  else                   → searchEdit.Focus()
```

### BackCommand
- Invoked when the back arrow is tapped and field is cleared
- ViewModel sets this to whatever navigation/state-reset is needed
- NOT invoked when user just taps the search icon to focus

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
| Text-only (no leading/trailing) | `LeadingContent` (LEFT) | Selection is primary action |
| With leading element (icon/avatar/image) | `TrailingContent` (RIGHT) | Leading slot occupied; don't stack |

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
