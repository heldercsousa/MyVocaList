# Theme & Locale Rules

## Theme Setup

DevExpress is initialized in `MauiProgram.cs` with:
```csharp
.UseDevExpress(useLocalization: false)
.UseDevExpressCollectionView()
.UseDevExpressControls()
.UseDevExpressEditors()
```

No `ThemeManager` call is used. The app is **dark mode only** (light mode is planned for v2.0).

## Theme Approach

**Applied palette:** Indigo (exported from Google Material Theme Builder, 2026-02-27)
**Approach:** Option B — Full token override in `MaterialColors.xaml`. No `ThemeManager.Theme` seed color is set; all colors live in the XAML ResourceDictionary.
**Approximate seed color:** `#4858AB` (indigo — derived from `InversePrimary` value)

**Tonal palette section note:** `MaterialColors.xaml` still contains a tonal palette section (Primary0–Primary100, etc.) from the previous Pink/Purple/Gold theme. These are only referenced by gradient brushes and represent design debt — they do not affect semantic token usage. Update separately if gradients need to match the new palette.

## Color System

The app uses a Material Design 3 color system defined in:
- `Resources/Styles/MaterialColors.xaml` — tonal palettes + semantic tokens
- `Resources/Styles/MaterialStyles.xaml` — styles for DX controls, Shell, typography

Both are merged in `App.xaml` via `ResourceDictionary.MergedDictionaries`.

### Semantic Tokens (Dark Mode — Active)

| Token | Value | Use |
|-------|-------|-----|
| `Primary` | `#BAC3FF` | Buttons, active states, icons |
| `OnPrimary` | `#15267B` | Text/icon on primary |
| `PrimaryContainer` | `#5C6BC0` | Container backgrounds |
| `OnPrimaryContainer` | `#F8F6FF` | Text on primary container |
| `Secondary` | `#BFC4EC` | Secondary actions |
| `OnSecondary` | `#292E4E` | Text on secondary |
| `SecondaryContainer` | `#3F4566` | Chip/badge backgrounds |
| `OnSecondaryContainer` | `#AEB3DA` | Text on secondary container |
| `Tertiary` | `#FFABF2` | Accent color |
| `OnTertiary` | `#551153` | Text on tertiary |
| `Error` | `#FFB4AB` | Error states |
| `OnError` | `#690005` | Text on error |
| `Background` | `#121318` | Page backgrounds |
| `Surface` | `#121318` | Cards, sheets |
| `OnSurface` | `#E3E1E9` | Primary text |
| `OnSurfaceVariant` | `#C6C5D3` | Secondary text, icons |
| `SurfaceVariant` | `#454651` | Dividers, chips |
| `SurfaceContainerLowest` | `#0D0E13` | Deepest surface (new in indigo theme) |
| `SurfaceContainerLow` | `#1B1B21` | List item backgrounds |
| `SurfaceContainer` | `#1F1F25` | Tab bar, elevated surfaces |
| `SurfaceContainerHigh` | `#29292F` | Pressed states |
| `SurfaceContainerHighest` | `#34343A` | Input field backgrounds |
| `Outline` | `#8F909D` | Borders |
| `OutlineVariant` | `#454651` | Subtle dividers |

### Custom Semantic Colors (not MD3 standard)
| Token | Value | Use |
|-------|-------|-----|
| `Success` | `#2E7D32` | Positive feedback |
| `Warning` | `#FF9800` | Caution states |
| `Info` | `#2196F3` | Informational |

### Gradients (defined in MaterialColors.xaml)
- `AppBackgroundGradient` — page background
- `CardBackgroundGradient` — card surfaces
- `SelectedGradient` — selected item highlight
- `ButtonGradient` — button backgrounds
- `FabGradient` — FAB background

## Referencing Colors

**In XAML — two valid ways:**

```xml
<!-- StaticResource (compile-time resolved, preferred for defined tokens) -->
BackgroundColor="{StaticResource Surface}"
TextColor="{StaticResource OnSurface}"

<!-- dx:ThemeColor (DevExpress runtime token, required for some DX properties) -->
CheckedCheckBoxColor="{dx:ThemeColor Primary}"
BorderColor="{dx:ThemeColor Primary}"
```

**Rule:** Prefer `{StaticResource X}` for layout properties. Use `{dx:ThemeColor X}` when DevExpress requires it (e.g., `CheckEdit.CheckedCheckBoxColor`, `DXButton.IconColor` in certain contexts). Both token names are identical.

**Never use:** raw hex colors, `Color.FromArgb`, or `Colors.X` in XAML.

**Hex format in MaterialColors.xaml:** Always use plain 6-digit hex (`#RRGGBB`) for opaque colors. Never use the 8-digit `#FFRRGGBB` form — MAUI accepts both but mixing them in the same file is a maintenance hazard.

## Typography

Fonts configured in `MauiProgram.cs`:
- `RobotoRegular` — body text, labels
- `RobotoMedium` — medium-weight labels, button text
- `RobotoBold` — headings, brand name

Typography styles defined in `MaterialStyles.xaml`:
```xml
StyleClass="Display.Large"   <!-- 57pt RobotoRegular -->
StyleClass="Headline.Large"  <!-- 32pt RobotoRegular -->
StyleClass="Title.Medium"    <!-- 16pt RobotoMedium -->
StyleClass="Body.Medium"     <!-- 14pt RobotoRegular -->
StyleClass="Label.Large"     <!-- 14pt RobotoMedium -->
```

## Defined Styles (MaterialStyles.xaml)

### DXButton styles
- `FilledButton` — Primary background, OnPrimary text, CornerRadius=20
- `FilledTonalButton` — SecondaryContainer background
- `OutlinedButton` — Transparent + Outline border
- `TextButton` — Transparent, Primary text
- `FlyoutMenuButton` — Navigation drawer items

### TextEdit (implicit style)
- `BoxMode="Outlined"`, `FocusedBorderColor=Primary`, `BorderColor=Outline`
- `BackgroundColor=SurfaceContainerHighest`, `TextColor=OnSurface`
- Note: `BoxCornerRadius` removed in DevExpress 25.1.3+

### Shell (implicit style)
- `BackgroundColor=Surface`, `ForegroundColor=OnSurface`
- `NavBarHasShadow=False`, `TabBarBackgroundColor=SurfaceContainer`

### Container styles
- `PageContainer` — VerticalStackLayout, Padding=16, Spacing=16
- `SectionContainer` — VerticalStackLayout, Spacing=12, Margin bottom=24

## Locale

`useLocalization: false` is set in `UseDevExpress()`. No localization framework is active.
All text is English only. No `.resx` files exist yet.
