# CLAUDE.md - MyVocaList

## App
Karaoke queue management. .NET MAUI 9.0 (net9.0-android, net9.0-ios).

## Language
Code, comments, logs, UI text: **English only**

## Translation
**CRITICAL**: All text in codebase must be English. Translate any non-English text (comments, strings, logs, UI labels) immediately.

## Comments
- **Only**: Methods and properties when name isn't self-explanatory
- **Never**: Inside method bodies
- **Must**: Be formatted, say WHAT (not HOW/WHY), updated when code changes
- **Can't**: Have symbols

## Architecture
```
Domain → Contracts → Services → Infrastructure → View
(Entities)  (DTOs)    (Logic)    (EF+SQLite)    (MAUI)
```
- Business logic **only** in Services
- Interface + Implementation in **same folder**
- DTOs as records
- Prefer composition over inheritance
- MAUI pages follow DevExpress patterns

## DDD Patterns
| Pattern | Implementation |
|---------|----------------|
| Aggregates/Entities | Base classes |
| Value Objects | Records |
| Domain Events | MediatR notifications |
| CQRS | Command/Query handlers |
| Repository | EF Core 9 + SQLite |

## TDD
- Test-first: Domain + Services
- Stack: xUnit, FluentAssertions, NSubstitute

## Error Handling
- **Avoid**: try-catch, `Debug.WriteLine`, `Console.WriteLine`
- **Use**: Serilog via `ILogger<T>`
- **Use**: Guard pattern for validation

```csharp
// ✅ Correct
Guard.AgainstNullOrWhiteSpace(name, nameof(name));

// ❌ Wrong
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException();
```

## UI Thread Safety - CRITICAL

**MANDATORY**: All UI operations MUST execute on platform's native UI thread.

### Golden Rules
1. **NEVER block UI thread** - No `Task.Wait()`, `.Result`, or synchronous I/O
2. **ALWAYS marshal UI updates** - Use `Dispatcher` for cross-thread UI access
3. **NEVER modify ObservableCollection from background threads**
4. **ALWAYS use `async Task`** - Never `async void` (except event handlers)

### Required Pattern for UI Updates

```csharp
// CORRECT - Safe UI update from any thread
Application.Current?.Dispatcher.Dispatch(() =>
{
    myLabel.Text = "Updated";
    MyCollection.Add(newItem);
});

// CORRECT - Async version
await Application.Current.Dispatcher.DispatchAsync(async () =>
{
    await SomeAsyncUiWork();
});
```

### Forbidden Patterns

```csharp
// WRONG - Blocks UI thread
var result = SomeAsyncMethod().Result;

// WRONG - Cross-thread UI access
await Task.Run(() => 
{
    myLabel.Text = "Crash!";
});

// WRONG - MainThread has Windows issues
MainThread.BeginInvokeOnMainThread(() => { }); // Use Dispatcher instead
```

### Background Work Pattern

```csharp
// Heavy work on background, marshal results to UI
var data = await Task.Run(() => HeavyComputation());

Application.Current?.Dispatcher.Dispatch(() =>
{
    Items.Clear();
    foreach (var item in data)
        Items.Add(item);
});
```

### ViewModel Base Helper

```csharp
protected void RunOnUiThread(Action action)
{
    if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        Application.Current.Dispatcher.Dispatch(action);
    else
        action();
}

protected Task RunOnUiThreadAsync(Func<Task> asyncAction)
{
    if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        return Application.Current.Dispatcher.DispatchAsync(asyncAction);
    else
        return asyncAction();
}
```

## Git Commits
```
<type>: <summary>

- detail 1
- detail 2

Co-Authored-By: Claude <noreply@anthropic.com>
```
Types: `feat:`, `fix:`, `refactor:`, `docs:`, `perf:`, `test:`

## Changelog
- Location: `Docs/Changelog/changelog.md`
- Format: `- **MM/dd/yyyy** - Type - Description`
- Types: Enhancement | Fix
- **Update after every completed task**

## Workflow
**CRITICAL**: After completing any task:
1. Update `Docs/Changelog/changelog.md`
2. Git commit with all changes
3. Push to remote
Never skip these steps.

## Theme & Locale
- **Theme**: Dark mode ONLY
- **Locale**: US English (en-US). Date: MM/dd/yyyy. Time: h:mm tt

## No Hard-Coded Values
**CRITICAL**: Never hard-code colors, dimensions, or style values in C# code.
- **Colors**: Use DevExpress `{dx:ThemeColor}` tokens only
- **Styles**: Define in XAML, reference via StaticResource
- **MauiProgram.cs**: Configuration only, NO color definitions

```csharp
// ❌ WRONG - Hard-coded
button.BackgroundColor = Colors.Red;

// ✅ CORRECT - DevExpress theme token
BackgroundColor="{dx:ThemeColor Primary}"
```

## Stack
```
.NET MAUI 9.0 (Android + iOS)
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
DevExpress MAUI v24.2+ (FREE for mobile)
```

## UI Framework: DevExpress MAUI

### Why DevExpress
- **Native code:** Objective-C (iOS) + Java/Kotlin (Android) - NOT C# wrappers
- **Performance:** 70% faster scrolling, 20% faster startup vs MAUI native
- **MD3 Compliant:** Full Material Design 3 implementation
- **FREE:** No cost for Android + iOS mobile apps

### Essential Components

| Component | Use Case |
|-----------|----------|
| `DXCollectionView` | High-performance lists, song queues |
| `DataGridView` | Tabular data, edit mode |
| `TextEdit, ComboBoxEdit, DateEdit, TimeEdit` | Form inputs |
| `CheckEdit, SwitchEdit` | Selection controls |
| `DXButton` | All 5 MD3 button types |
| `DXBorder` | Containers with rounded corners |
| `DXScrollView` | Scrollable containers |
| `TabView, Drawer` | Navigation |
| `DXPopup, BottomSheet` | Dialogs, action sheets |
| `ShimmerView` | Loading skeleton states |
| `ChartView, PieChartView` | Analytics visualizations |

### XAML Namespaces

```xml
xmlns:dx="http://schemas.devexpress.com/maui"
xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
xmlns:dxg="clr-namespace:DevExpress.Maui.DataGrid;assembly=DevExpress.Maui.DataGrid"
xmlns:dxc="clr-namespace:DevExpress.Maui.Charts;assembly=DevExpress.Maui.Charts"
```

### Theme System

**Configure in MauiProgram.cs (BEFORE builder):**

```csharp
// Built-in themes
ThemeManager.Theme = new Theme(ThemeSeedColor.Pink);
// Options: Blue, TealGreen, Cyan, Green, Yellow, Orange, Red, Pink, Purple, Violet

// Or custom seed color
ThemeManager.Theme = new Theme(Color.FromArgb("#FFB2BE"));
```

**Use tokens in XAML:**

```xml
<dx:DXButton BackgroundColor="{dx:ThemeColor Primary}"
             TextColor="{dx:ThemeColor OnPrimary}"
             CornerRadius="20"/>
```

**Available tokens:** Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer, Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer, Surface, OnSurface, SurfaceContainer, SurfaceContainerLow, SurfaceContainerHigh, SurfaceContainerHighest, OnSurfaceVariant, Outline, OutlineVariant, Error, OnError, ErrorContainer, OnErrorContainer

### DevExpress XAML Patterns

**Buttons (5 MD3 types):**
```xml
<!-- Filled Button (high emphasis) -->
<dx:DXButton Content="Save"
             BackgroundColor="{dx:ThemeColor Primary}"
             TextColor="{dx:ThemeColor OnPrimary}"
             CornerRadius="20" HeightRequest="40" Padding="24,0"/>

<!-- Filled Tonal (medium emphasis) -->
<dx:DXButton Content="Next"
             BackgroundColor="{dx:ThemeColor SecondaryContainer}"
             TextColor="{dx:ThemeColor OnSecondaryContainer}"
             CornerRadius="20" HeightRequest="40"/>

<!-- Outlined (medium emphasis) -->
<dx:DXButton Content="Cancel"
             BackgroundColor="Transparent"
             TextColor="{dx:ThemeColor Primary}"
             BorderColor="{dx:ThemeColor Outline}"
             BorderThickness="1"
             CornerRadius="20" HeightRequest="40"/>

<!-- Text Button (low emphasis) -->
<dx:DXButton Content="Skip"
             BackgroundColor="Transparent"
             TextColor="{dx:ThemeColor Primary}"
             HeightRequest="40"/>
```

**Form Editors:**
```xml
<dxe:TextEdit LabelText="Song Title"
              Text="{Binding Title}"
              BoxBackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
              FocusedBorderColor="{dx:ThemeColor Primary}"/>

<dxe:ComboBoxEdit LabelText="Genre"
                  ItemsSource="{Binding Genres}"
                  SelectedItem="{Binding SelectedGenre}"/>

<dxe:DateEdit LabelText="Event Date"
              Date="{Binding EventDate}"/>

<dxe:CheckEdit Label="Mark as favorite"
               IsChecked="{Binding IsFavorite}"
               Color="{dx:ThemeColor Primary}"/>

<dxe:SwitchEdit IsToggled="{Binding IsActive}"
                Color="{dx:ThemeColor Primary}"/>
```

**High-Performance Lists:**
```xml
<dxcv:DXCollectionView ItemsSource="{Binding Songs}"
                       SelectionMode="Single"
                       SelectedItem="{Binding SelectedSong}"
                       AllowCascadeUpdate="True">
    
    <dxcv:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="local:SongViewModel">
            <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerLow}"
                         CornerRadius="12" Padding="16" Margin="0,0,0,8">
                <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                    <!-- Item content -->
                </Grid>
            </dx:DXBorder>
        </DataTemplate>
    </dxcv:DXCollectionView.ItemTemplate>
</dxcv:DXCollectionView>
```

**Containers:**
```xml
<!-- Card-like container -->
<dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerLow}"
             CornerRadius="12" Padding="16">
    <VerticalStackLayout Spacing="12">
        <!-- Content -->
    </VerticalStackLayout>
</dx:DXBorder>

<!-- Scrollable page -->
<dx:DXScrollView>
    <VerticalStackLayout Padding="16" Spacing="24">
        <!-- Page content -->
    </VerticalStackLayout>
</dx:DXScrollView>
```

**Dialogs:**
```xml
<dx:DXPopup IsOpen="{Binding ShowDialog}">
    <dx:DXBorder BackgroundColor="{dx:ThemeColor Surface}"
                 CornerRadius="28" Padding="24">
        <!-- Dialog content -->
    </dx:DXBorder>
</dx:DXPopup>
```

### Typography (Roboto Font Family)

**Register fonts in MauiProgram.cs:**
```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
    fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
    fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
})
```

**MD3 Typography Roles:**
```xml
<!-- Display Large (57sp) - Hero text -->
<Label Text="MyVocaList" FontFamily="RobotoRegular" FontSize="57"/>

<!-- Headline Large (32sp) - Page titles -->
<Label Text="Song Queue" FontFamily="RobotoRegular" FontSize="32"/>

<!-- Title Medium (16sp) - Card titles -->
<Label Text="Now Playing" FontFamily="RobotoMedium" FontSize="16"/>

<!-- Body Medium (14sp) - Content -->
<Label Text="Artist name" FontFamily="RobotoRegular" FontSize="14"/>

<!-- Label Large (14sp) - Buttons -->
<Label Text="ADD TO QUEUE" FontFamily="RobotoMedium" FontSize="14"/>
```

### Performance Optimization

**CRITICAL for DevExpress:**

1. **Always use compiled bindings:**
```xml
<DataTemplate x:DataType="local:SongViewModel">
    <Label Text="{Binding Title}"/> <!-- 8-20x faster -->
</DataTemplate>
```

2. **DXCollectionView optimization:**
```xml
<dxcv:DXCollectionView AllowCascadeUpdate="True" ... />
```

3. **Test in Release mode** - AOT compilation + linker trimming
4. **Background work pattern** - Heavy operations with `Task.Run()`, marshal to UI thread

### Custom Controls - Avoid When Possible

**Before creating custom control:**
1. Check DevExpress library first
2. Check if MAUI native control works
3. Only then consider custom

**If creating custom control:**
- Include thread-safety documentation
- Add unit tests for thread safety
- Document why DevExpress/MAUI don't work
- Use DevExpress theme tokens for styling

## XAML Styling Rules

**NEVER use inline property values:**

```xml
<!-- ❌ WRONG - Inline values -->
<VerticalStackLayout Spacing="16" Padding="8">
<Label FontSize="14" TextColor="Red">

<!-- ✅ CORRECT - Theme tokens and explicit sizing when needed -->
<VerticalStackLayout Spacing="16" Padding="16">
<Label FontFamily="RobotoRegular" FontSize="14" 
       TextColor="{dx:ThemeColor OnSurface}">
```

**Exception:** BoxView dividers may use `HeightRequest="1"` for 1px structural height only.

## Workflow References

- **DevExpress Docs:** https://docs.devexpress.com/MAUI/
- **MAUI 9 Docs:** https://learn.microsoft.com/en-us/dotnet/maui/
- **MD3 Guidelines:** https://m3.material.io/

## .NET MAUI 9.0 - Current Recommended Version

**Released:** November 2024  
**Status:** Latest stable, production-ready  
**Features:**
- Handler-based architecture (faster than legacy renderers)
- Improved startup time
- Better memory management
- Native control performance
- Full compatibility with DevExpress MAUI v24.2+

**Target Frameworks:**
- `net9.0-android` (API 21+)
- `net9.0-ios` (iOS 14.2+)

**NOT supported:** Windows (dropped for native performance gains)