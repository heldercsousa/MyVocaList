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

## Color System

The app uses a Material Design 3 color system defined in:
- `Resources/Styles/MaterialColors.xaml` — tonal palettes + semantic tokens
- `Resources/Styles/MaterialStyles.xaml` — styles for DX controls, Shell, typography

Both are merged in `App.xaml` via `ResourceDictionary.MergedDictionaries`.

### Semantic Tokens (Dark Mode — Active)

| Token | Value | Use |
|-------|-------|-----|
| `Primary` | Primary80 `#FFFFB2BE` | Buttons, active states, icons |
| `OnPrimary` | Primary20 `#FF660025` | Text/icon on primary |
| `PrimaryContainer` | Primary30 `#FF900038` | Container backgrounds |
| `OnPrimaryContainer` | Primary90 `#FFFFD9DE` | Text on primary container |
| `Secondary` | Secondary80 `#FFE2B5FF` | Secondary actions |
| `OnSecondary` | Secondary20 `#FF4D007A` | Text on secondary |
| `SecondaryContainer` | Secondary30 `#FF662592` | Chip/badge backgrounds |
| `OnSecondaryContainer` | Secondary90 `#FFF3DAFF` | Text on secondary container |
| `Tertiary` | Tertiary80 `#FFE9C400` | Gold accents |
| `OnTertiary` | Tertiary20 `#FF3A3000` | Text on tertiary |
| `Error` | Error80 `#FFFFB4A9` | Error states |
| `OnError` | Error20 `#FF690002` | Text on error |
| `Background` | Neutral10 `#FF1C1621` | Page backgrounds |
| `Surface` | Neutral10 `#FF1C1621` | Cards, sheets |
| `OnSurface` | Neutral90 `#FFE6DDEA` | Primary text |
| `OnSurfaceVariant` | NeutralVariant80 `#FFD1BFD6` | Secondary text, icons |
| `SurfaceVariant` | NeutralVariant30 `#FF4E3F53` | Dividers, chips |
| `SurfaceContainerLow` | `#FF251E2A` | List item backgrounds |
| `SurfaceContainer` | `#FF312A36` | Tab bar, elevated surfaces |
| `SurfaceContainerHigh` | `#FF47404D` | Pressed states |
| `SurfaceContainerHighest` | `#FF5F5765` | Input field backgrounds |
| `Outline` | NeutralVariant60 `#FF9A899F` | Borders |
| `OutlineVariant` | NeutralVariant30 `#FF4E3F53` | Subtle dividers |

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
