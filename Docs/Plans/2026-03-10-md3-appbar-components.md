# MD3 App Bar Components — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create two reusable MD3-compliant app bar ContentView components (`SmallAppBar` and `SearchAppBar`) and document VenuesPage non-compliance issues for future remediation.

**Architecture:** Both components are `ContentView` subclasses in `MyVocaList/UI/Components/AppBars/`. They expose `BindableProperty` fields for all customizable aspects and use DevExpress controls internally (`DXButton` for icon buttons, `TextEdit` + `DXBorder` for the search field). No ViewModel dependency — pure UI components placed inside `Shell.TitleView`.

**Tech Stack:** .NET MAUI 10 · C# 13 · DevExpress MAUI v24.2+ (`DXButton`, `TextEdit`, `DXBorder`) · CommunityToolkit.Mvvm (bindable properties) · XAML compiled bindings

---

## M3 Non-Compliance Audit — VenuesPage

Source: [Material Components Android — TopAppBar.md](https://github.com/material-components/material-components-android/blob/master/docs/components/TopAppBar.md)

| # | Issue | Current Implementation | M3 Spec | Severity |
|---|-------|----------------------|---------|----------|
| 1 | **Search is below the app bar, not IN it** | Shell TitleView shows "Venues" + separate `DXBorder` search strip below in `Grid.Row="0"` of page content | "Search app bar" = the search field **replaces** the headline text inside the bar container. One bar, not two. | High — doubles top chrome height |
| 2 | **Title typography** | `FontSize="20"` + `FontFamily="RobotoMedium"` | `textAppearanceTitleLarge` = **22sp**, Regular weight | Medium |
| 3 | **No scroll-elevation** | Bar stays `colorSurface` always | On scroll (`liftOnScroll`): container changes from `colorSurface` → `colorSurfaceContainer` | Medium |
| 4 | **No trailing action icons in Small bar** | Normal mode has zero action icons in TitleView | Small app bar supports 1–3 trailing icon buttons (`colorOnSurfaceVariant`) + overflow | Low — FAB covers primary action; secondary actions not needed yet |
| 5 | **Inner Grid HeightRequest="48"** | `HeightRequest="48"` on the Shell.TitleView inner Grid | Shell provides ~56dp container; inner content should fill it (`VerticalOptions="Fill"`) | Low |

> **Scope of this plan:** Build the new reusable components only. VenuesPage remediation is tracked separately and is NOT in scope here — no existing file is modified.

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml` | Create | XAML layout: nav icon, title/subtitle, 3 action icon slots |
| `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml.cs` | Create | BindableProperties: Title, Subtitle, NavigationIcon, NavigationCommand, Action1–3 Icon/Command, IsElevated |
| `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml` | Create | XAML layout: leading icon button, TextEdit search field, trailing icon button |
| `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs` | Create | BindableProperties: SearchText, Placeholder, LeadingIcon, LeadingCommand, TrailingIcon, TrailingCommand, IsElevated |
| `MyVocaList/GlobalUsings.cs` | Modify | Add `MyVocaList.UI.Components.AppBars` if used in 2+ pages |

---

## Chunk 1: SmallAppBar Component

### Task 1: Create SmallAppBar XAML

**Files:**
- Create: `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml`
- Create: `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml.cs`

**M3 spec applied:**
- Container: `colorSurface` default, `colorSurfaceContainer` when `IsElevated=true`
- Navigation icon: 24dp icon in 48×48 touch target, `colorOnSurface`
- Title: 22sp Regular, `colorOnSurface` (TitleLarge)
- Subtitle: 14sp Regular, `colorOnSurfaceVariant` (TitleMedium)
- Action icons: 24dp icons in 48×48 touch targets, `colorOnSurfaceVariant`
- Container height: fills Shell.TitleView (Shell provides the outer ~56dp container; we use `VerticalOptions="Fill"`)
- Elevation transition: background color change only (no box shadow in MAUI flat design)

- [ ] **Step 1: Create the XAML layout**

Create `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    x:Class="MyVocaList.UI.Components.AppBars.SmallAppBar"
    x:Name="self">

    <!-- Container: color transitions between Surface and SurfaceContainer on IsElevated -->
    <Grid x:Name="container"
          ColumnDefinitions="Auto,*,Auto,Auto,Auto"
          ColumnSpacing="0"
          BackgroundColor="{StaticResource Surface}"
          VerticalOptions="Fill"
          Padding="4,0">

        <!-- Leading: Navigation icon (optional) -->
        <dx:DXButton Grid.Column="0"
                     x:Name="navButton"
                     Icon="{Binding NavigationIcon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurface}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasNavigationIcon, Source={x:Reference self}}"
                     Command="{Binding NavigationCommand, Source={x:Reference self}}" />

        <!-- Title + Subtitle stack (center column, expands) -->
        <VerticalStackLayout Grid.Column="1"
                             VerticalOptions="Center"
                             Spacing="0"
                             Padding="4,0">
            <Label x:Name="titleLabel"
                   Text="{Binding Title, Source={x:Reference self}}"
                   FontFamily="RobotoRegular"
                   FontSize="22"
                   TextColor="{StaticResource OnSurface}"
                   LineBreakMode="TailTruncation"
                   MaxLines="1" />
            <Label x:Name="subtitleLabel"
                   Text="{Binding Subtitle, Source={x:Reference self}}"
                   FontFamily="RobotoRegular"
                   FontSize="14"
                   TextColor="{StaticResource OnSurfaceVariant}"
                   LineBreakMode="TailTruncation"
                   MaxLines="1"
                   IsVisible="{Binding HasSubtitle, Source={x:Reference self}}" />
        </VerticalStackLayout>

        <!-- Trailing action 1 -->
        <dx:DXButton Grid.Column="2"
                     x:Name="action1Button"
                     Icon="{Binding Action1Icon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasAction1, Source={x:Reference self}}"
                     Command="{Binding Action1Command, Source={x:Reference self}}" />

        <!-- Trailing action 2 -->
        <dx:DXButton Grid.Column="3"
                     x:Name="action2Button"
                     Icon="{Binding Action2Icon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasAction2, Source={x:Reference self}}"
                     Command="{Binding Action2Command, Source={x:Reference self}}" />

        <!-- Trailing action 3 -->
        <dx:DXButton Grid.Column="4"
                     x:Name="action3Button"
                     Icon="{Binding Action3Icon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasAction3, Source={x:Reference self}}"
                     Command="{Binding Action3Command, Source={x:Reference self}}" />
    </Grid>
</ContentView>
```

- [ ] **Step 2: Create the code-behind with BindableProperties**

Create `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml.cs`:

```csharp
namespace MyVocaList.UI.Components.AppBars;

public partial class SmallAppBar : ContentView
{
    // ── Title ──────────────────────────────────────────────────────────────

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SmallAppBar), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ── Subtitle ───────────────────────────────────────────────────────────

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateSubtitleVisibility());

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    // ── Navigation icon ────────────────────────────────────────────────────

    public static readonly BindableProperty NavigationIconProperty =
        BindableProperty.Create(nameof(NavigationIcon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateNavIconVisibility());

    public string NavigationIcon
    {
        get => (string)GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public static readonly BindableProperty NavigationCommandProperty =
        BindableProperty.Create(nameof(NavigationCommand), typeof(ICommand), typeof(SmallAppBar));

    public ICommand NavigationCommand
    {
        get => (ICommand)GetValue(NavigationCommandProperty);
        set => SetValue(NavigationCommandProperty, value);
    }

    public bool HasNavigationIcon => !string.IsNullOrEmpty(NavigationIcon);

    // ── Action 1 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action1IconProperty =
        BindableProperty.Create(nameof(Action1Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action1Icon
    {
        get => (string)GetValue(Action1IconProperty);
        set => SetValue(Action1IconProperty, value);
    }

    public static readonly BindableProperty Action1CommandProperty =
        BindableProperty.Create(nameof(Action1Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action1Command
    {
        get => (ICommand)GetValue(Action1CommandProperty);
        set => SetValue(Action1CommandProperty, value);
    }

    public bool HasAction1 => !string.IsNullOrEmpty(Action1Icon);

    // ── Action 2 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action2IconProperty =
        BindableProperty.Create(nameof(Action2Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action2Icon
    {
        get => (string)GetValue(Action2IconProperty);
        set => SetValue(Action2IconProperty, value);
    }

    public static readonly BindableProperty Action2CommandProperty =
        BindableProperty.Create(nameof(Action2Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action2Command
    {
        get => (ICommand)GetValue(Action2CommandProperty);
        set => SetValue(Action2CommandProperty, value);
    }

    public bool HasAction2 => !string.IsNullOrEmpty(Action2Icon);

    // ── Action 3 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action3IconProperty =
        BindableProperty.Create(nameof(Action3Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action3Icon
    {
        get => (string)GetValue(Action3IconProperty);
        set => SetValue(Action3IconProperty, value);
    }

    public static readonly BindableProperty Action3CommandProperty =
        BindableProperty.Create(nameof(Action3Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action3Command
    {
        get => (ICommand)GetValue(Action3CommandProperty);
        set => SetValue(Action3CommandProperty, value);
    }

    public bool HasAction3 => !string.IsNullOrEmpty(Action3Icon);

    // ── IsElevated (scroll lift) ───────────────────────────────────────────

    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(SmallAppBar), false,
            propertyChanged: (b, _, __) => ((SmallAppBar)b).UpdateContainerColor());

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SmallAppBar()
    {
        InitializeComponent();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void UpdateContainerColor()
    {
        if (Resources.TryGetValue(IsElevated ? "SurfaceContainer" : "Surface", out var color))
            container.BackgroundColor = (Color)color;
    }

    private void UpdateSubtitleVisibility()
    {
        OnPropertyChanged(nameof(HasSubtitle));
    }

    private void UpdateNavIconVisibility()
    {
        OnPropertyChanged(nameof(HasNavigationIcon));
    }

    private void UpdateActionVisibility()
    {
        OnPropertyChanged(nameof(HasAction1));
        OnPropertyChanged(nameof(HasAction2));
        OnPropertyChanged(nameof(HasAction3));
    }
}
```

- [ ] **Step 3: Build and verify no errors**

```bash
cd C:/Users/helde/source/repos/MyVocaList
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore -v quiet
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/AppBars/SmallAppBar.xaml
git add MyVocaList/UI/Components/AppBars/SmallAppBar.xaml.cs
git commit -m "feat(components): add MD3-compliant SmallAppBar ContentView"
```

---

## Chunk 2: SearchAppBar Component

### Task 2: Create SearchAppBar XAML

**Files:**
- Create: `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml`
- Create: `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs`

**M3 spec applied:**
- Search bar shape: full-round pill, `CornerRadius="28"` (height 56dp → radius = half)
- Background: `colorSurfaceContainerHigh` (`SurfaceContainerHighest` in our tokens)
- Leading icon: `search_outlined` by default (24dp, `colorOnSurfaceVariant`), switchable to `arrow_back_outlined` when navigating back
- Input field: `colorOnSurface` text, `colorOnSurfaceVariant` placeholder
- Clear button: shown automatically when text is non-empty (`colorOnSurfaceVariant`)
- Trailing icon: optional (avatar, filter, etc.) — 48×48 touch target
- Container: same as SmallAppBar — `colorSurface` or `colorSurfaceContainer` when elevated
- The entire search bar IS the top bar content (no separate title label)

- [ ] **Step 1: Create the XAML layout**

Create `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    x:Class="MyVocaList.UI.Components.AppBars.SearchAppBar"
    x:Name="self">

    <!-- Container: color transitions on IsElevated -->
    <Grid x:Name="container"
          ColumnDefinitions="Auto,*,Auto"
          ColumnSpacing="0"
          BackgroundColor="{StaticResource Surface}"
          VerticalOptions="Fill"
          Padding="4,0">

        <!-- Leading icon button (search or back) -->
        <dx:DXButton Grid.Column="0"
                     x:Name="leadingButton"
                     Icon="{Binding LeadingIcon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasLeadingIcon, Source={x:Reference self}}"
                     Command="{Binding LeadingCommand, Source={x:Reference self}}" />

        <!-- Search field pill -->
        <dx:DXBorder Grid.Column="1"
                     BackgroundColor="{StaticResource SurfaceContainerHighest}"
                     CornerRadius="28"
                     Padding="4,0"
                     VerticalOptions="Center"
                     Margin="0,6">
            <dxe:TextEdit x:Name="searchEdit"
                          Text="{Binding SearchText, Source={x:Reference self}, Mode=TwoWay}"
                          PlaceholderText="{Binding Placeholder, Source={x:Reference self}}"
                          StartIcon="search_outlined"
                          StartIconColor="{StaticResource OnSurfaceVariant}"
                          BoxMode="Outlined"
                          BorderColor="Transparent"
                          FocusedBorderColor="Transparent"
                          BackgroundColor="Transparent"
                          TextColor="{StaticResource OnSurface}"
                          PlaceholderColor="{StaticResource OnSurfaceVariant}"
                          ClearIconVisibility="Auto"
                          ClearIconColor="{StaticResource OnSurfaceVariant}" />
        </dx:DXBorder>

        <!-- Trailing icon button (optional) -->
        <dx:DXButton Grid.Column="2"
                     x:Name="trailingButton"
                     Icon="{Binding TrailingIcon, Source={x:Reference self}}"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     BackgroundColor="Transparent"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     HorizontalContentAlignment="Center"
                     VerticalOptions="Center"
                     IsVisible="{Binding HasTrailingIcon, Source={x:Reference self}}"
                     Command="{Binding TrailingCommand, Source={x:Reference self}}" />
    </Grid>
</ContentView>
```

- [ ] **Step 2: Create the code-behind with BindableProperties**

Create `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs`:

```csharp
namespace MyVocaList.UI.Components.AppBars;

public partial class SearchAppBar : ContentView
{
    // ── SearchText ─────────────────────────────────────────────────────────

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchAppBar), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    // ── Placeholder ────────────────────────────────────────────────────────

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchAppBar), "Search...");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // ── Leading icon ───────────────────────────────────────────────────────

    public static readonly BindableProperty LeadingIconProperty =
        BindableProperty.Create(nameof(LeadingIcon), typeof(string), typeof(SearchAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SearchAppBar)b).OnPropertyChanged(nameof(HasLeadingIcon)));

    public string LeadingIcon
    {
        get => (string)GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public static readonly BindableProperty LeadingCommandProperty =
        BindableProperty.Create(nameof(LeadingCommand), typeof(ICommand), typeof(SearchAppBar));

    public ICommand LeadingCommand
    {
        get => (ICommand)GetValue(LeadingCommandProperty);
        set => SetValue(LeadingCommandProperty, value);
    }

    public bool HasLeadingIcon => !string.IsNullOrEmpty(LeadingIcon);

    // ── Trailing icon ──────────────────────────────────────────────────────

    public static readonly BindableProperty TrailingIconProperty =
        BindableProperty.Create(nameof(TrailingIcon), typeof(string), typeof(SearchAppBar), string.Empty,
            propertyChanged: (b, _, __) => ((SearchAppBar)b).OnPropertyChanged(nameof(HasTrailingIcon)));

    public string TrailingIcon
    {
        get => (string)GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public static readonly BindableProperty TrailingCommandProperty =
        BindableProperty.Create(nameof(TrailingCommand), typeof(ICommand), typeof(SearchAppBar));

    public ICommand TrailingCommand
    {
        get => (ICommand)GetValue(TrailingCommandProperty);
        set => SetValue(TrailingCommandProperty, value);
    }

    public bool HasTrailingIcon => !string.IsNullOrEmpty(TrailingIcon);

    // ── IsElevated (scroll lift) ───────────────────────────────────────────

    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(SearchAppBar), false,
            propertyChanged: (b, _, __) => ((SearchAppBar)b).UpdateContainerColor());

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SearchAppBar()
    {
        InitializeComponent();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void UpdateContainerColor()
    {
        if (Resources.TryGetValue(IsElevated ? "SurfaceContainer" : "Surface", out var color))
            container.BackgroundColor = (Color)color;
    }
}
```

- [ ] **Step 3: Build and verify no errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore -v quiet
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/AppBars/SearchAppBar.xaml
git add MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs
git commit -m "feat(components): add MD3-compliant SearchAppBar ContentView"
```

---

## Chunk 3: Usage Documentation & Rules Update

### Task 3: Document usage patterns and update devexpress-patterns.md

**Files:**
- Modify: `.claude/rules/devexpress-patterns.md` — add AppBars section

- [ ] **Step 1: Add AppBars usage section to devexpress-patterns.md**

Append the following section to `.claude/rules/devexpress-patterns.md`:

```markdown
## MD3 App Bar Components — confirmed in AppBars/

Two reusable `ContentView` components in `MyVocaList/UI/Components/AppBars/`:

### SmallAppBar
Place inside `Shell.TitleView`. Supports: nav icon, title (22sp Regular, OnSurface),
subtitle (14sp Regular, OnSurfaceVariant), up to 3 trailing action icons (48×48, OnSurfaceVariant),
and scroll-elevation via `IsElevated` (Surface → SurfaceContainer).

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

### SearchAppBar
The search field IS the app bar — replaces SmallAppBar when search is the primary page action.
Place inside `Shell.TitleView`. Leading icon defaults to hidden; set `LeadingIcon="arrow_back_outlined"`
when the bar needs a back button.

```xml
<Shell.TitleView>
    <appbars:SearchAppBar
        SearchText="{Binding SearchText, Mode=TwoWay}"
        Placeholder="Search venues..."
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
```

### IsElevated — scroll detection pattern
Set `IsElevated` from a ViewModel property driven by scroll position. In the page code-behind,
listen to `DXCollectionView.Scrolled` or `ScrollView.Scrolled`:

```csharp
private void OnCollectionViewScrolled(object sender, CollectionViewScrolledEventArgs e)
{
    _viewModel.IsScrolled = e.VerticalOffset > 0;
}
```

### MD3 non-compliance notes for VenuesPage (tracked, not yet remediated)
1. Search bar is in page content (Row 0), not in Shell.TitleView → use SearchAppBar instead
2. Title FontSize="20" should be 22sp (TitleLarge)
3. No scroll-elevation behavior → bind SmallAppBar.IsElevated to a scroll-driven property
```

- [ ] **Step 2: Build final verification**

```bash
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore -v quiet
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add .claude/rules/devexpress-patterns.md
git commit -m "docs(rules): document MD3 app bar components and VenuesPage non-compliance"
```

---

## Definition of Done

- [ ] `SmallAppBar.xaml` + `SmallAppBar.xaml.cs` created and building cleanly
- [ ] `SearchAppBar.xaml` + `SearchAppBar.xaml.cs` created and building cleanly
- [ ] All BindableProperties work: Title, Subtitle, NavigationIcon/Command, Action1–3 Icon/Command, SearchText, Placeholder, LeadingIcon/Command, TrailingIcon/Command, IsElevated
- [ ] `IsElevated=true` changes container background from `Surface` to `SurfaceContainer`
- [ ] M3 compliance audit documented in devexpress-patterns.md
- [ ] Zero build errors after each task
- [ ] 2 commits made (one per component + 1 docs)
