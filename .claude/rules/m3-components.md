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

---

## M3 Floating Toolbar

> **M3 Expressive component.** The floating toolbar (and "vibrant" color scheme) are M3 Expressive features — not in standard M3. "Vibrant" is the official spec term (confirmed via m3.material.io 2026-04-04).
>
> **Color design decision.** Official M3 Expressive vibrant uses `PrimaryContainer` bg + `OnPrimaryContainer` icons. This project uses `SecondaryContainer` bg + `OnSecondaryContainer` icons — a deliberate choice for better contrast in the indigo dark theme. The selected-state contrast (Primary bg + OnPrimary icon) applies against SecondaryContainer, not PrimaryContainer.
>
> **Height.** The spec states docked toolbars are 64dp. The floating toolbar has separate measurements (not available in text form from m3.material.io). Project uses 48dp — a compact pill appropriate for a floating overlay.

### Spec

| Property | Value |
|---|---|
| Height | 48dp (project choice; docked = 64dp per spec) |
| Width | Wrap content (auto-sizes to populated slots) |
| Padding (H, outside icon slots) | 4dp |
| Background | **SecondaryContainer** (project vibrant — see note above) |
| Shape | CornerRadius = 24dp (full pill — confirmed: spec prohibits square buttons in floating toolbar) |
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
[DXBorder: pill, SecondaryContainer (vibrant), Level 3 elevation via tint]
  └── HorizontalStackLayout
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
(FAB 56dp > toolbar 48dp — center alignment keeps them visually level).

```xml
<!-- Root: single-cell Grid (toolbar+FAB row overlays content) -->
<Grid>
    <!-- Content list — bottom margin clears the combined bar -->
    <!-- Formula: max(FAB 56dp, toolbar 48dp) + 16dp margin + 8dp breathing = 80dp -->
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

**Never** use independent overlays (FAB at bottom-right with Margin formula, toolbar separately centered). That pattern is retired.

### FAB coexistence
FAB is placed to the RIGHT of `FloatingToolbar` inside a shared `HorizontalStackLayout`.
The whole unit is `HorizontalOptions=Center`, not independently positioned.
`DXCollectionView.Margin="0,0,0,80"` — max(56,48) + 16 + 8 breathing.

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
