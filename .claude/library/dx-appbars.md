# DevExpress MAUI Component Patterns — MD3 App Bar components

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## MD3 App Bar Components — confirmed in AppBars/

Two reusable `ContentView` components in `MyVocaList/UI/Components/AppBars/`. DevExpress has no built-in top app bar — these components use `DXButton` for icon buttons and `TextEdit`+`DXBorder` for the search field internally.

### SmallAppBar
Place inside `Shell.TitleView`. Supports: nav icon, title (22sp Regular, OnSurface),
subtitle (14sp Regular, OnSurfaceVariant), up to 3 trailing action icons (48×48, OnSurfaceVariant),
and scroll-elevation via `IsElevated` (Surface → SurfaceContainer).

Namespace: `xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"`

```xml
<Shell.TitleView>
    <appbars:SmallAppBar
        Title="{Binding PageTitle}"
        NavigationIcon="arrow_back_outlined"
        NavigationCommand="{Binding BackCommand}"
        Action1Icon="search_outlined"
        Action1Command="{Binding OpenSearchCommand}"
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
```

BindableProperties: `Title`, `Subtitle`, `NavigationIcon`, `NavigationCommand`,
`Action1Icon/Command`, `Action2Icon/Command`, `Action3Icon/Command`, `IsElevated`

### SearchAppBar
The search field IS the app bar — replaces SmallAppBar when search is the primary page action (M3 "Search app bar" pattern). Place inside `Shell.TitleView`.

```xml
<Shell.TitleView>
    <appbars:SearchAppBar
        SearchText="{Binding SearchText, Mode=TwoWay}"
        Placeholder="Search venues..."
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
```

BindableProperties: `SearchText` (TwoWay), `Placeholder`, `LeadingIcon`, `LeadingCommand`,
`TrailingIcon`, `TrailingCommand`, `IsElevated`

### IsElevated — scroll detection pattern
The page is responsible for detecting scroll and setting `IsElevated` via a ViewModel property.
**Event args type is `DXCollectionViewScrolledEventArgs`; offset property is `e.Offset` — see Known Gotchas.**

```csharp
// In page code-behind — listen to DXCollectionView scroll
private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
{
    _viewModel.IsScrolled = e.Offset > 0;
}
```

---
