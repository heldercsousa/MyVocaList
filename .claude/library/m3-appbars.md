# M3 Components — M3 Top App Bars (Small, Search, standalone) + shared base class + tokens + files

> Section file split from `m3-components.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `m3-components.md`.

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

> **Status (2026-07-19):** the bar-swap pattern above is **retired for CRUD list pages** — see `crud-appbar-list-toolbar.md § App Bar — Laws and Variants`. It remains valid for the 4 picker pages (`SongPickerPage`, `ArtistPickerPage`, `QueueSongPickerPage`, `YouTubeSearchPage`) until their own migration (BACKLOG follow-up).

## M3 Search (standalone/detached — implemented: `SearchBar`)

Shipped as `MyVocaList/UI/Components/AppBars/SearchBar.xaml(.cs)`, subclassing `AppBarBase`. Docked at Row 0 of `CrudListView` — always visible inside page content, never replacing `Shell.TitleView`'s `SmallAppBar`. Governed component (4 consumers: Venues/People/Artists/Songs via `CrudListView`) — see `component-safety-gate.md`.

| Diff from SearchAppBar | Standalone value |
|---|---|
| Height | 56dp (not 64dp) |
| Shape | Pill: DXBorder CornerRadius="28" |
| Background | SurfaceContainerLow (not Surface); elevated → SurfaceContainer |
| Margins | 16dp horizontal, 8dp vertical |
| Leading icon | `search_outlined`, OnSurfaceVariant, non-interactive (no button, no back-arrow toggle) |

Differs from the bar-swap `SearchAppBar` beyond size/shape: no `BackCommand`, no auto-focus on visibility, no leading-icon toggle — it never hides or replaces another bar, so none of that state machinery applies. TextEdit properties (typography, clear icon, keyboard, ReturnType) are transplanted unchanged from `SearchAppBar`'s search input slot; reuses `AppBarBase.IsElevated`/`UpdateContainerColor()`.

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
