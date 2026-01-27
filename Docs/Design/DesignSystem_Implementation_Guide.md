# MyVocaList Design System Implementation Guide

> **Purpose**: Complete implementation reference for building MD3-compliant UI
> **Audience**: Claude Code executing design system tasks
> **Related**: `MD3_Reference.md` for pure specifications
> **Version**: 2.0 - Includes Roboto typography and complete component coverage

---

## Table of Contents

1. [Library Setup](#part-1-library-setup)
2. [Roboto Typography Setup](#part-2-roboto-typography-setup)
3. [Thread-Safe Infrastructure](#part-3-thread-safe-infrastructure)
4. [Component Library Mapping](#part-4-component-library-mapping)
5. [Icons Setup](#part-5-icons-setup)
6. [Priority Tiers](#part-6-priority-tiers)
7. [MaterialStyles.xaml Updates](#part-7-materialstyles-xaml-updates)
8. [Demo Pages Specification](#part-8-demo-pages-specification)
9. [XAML Patterns Reference](#part-9-xaml-patterns-reference)
10. [Accessibility Implementation](#part-10-accessibility-implementation)
11. [Critical MD3 Values](#part-11-critical-md3-values)
12. [Success Criteria](#part-12-success-criteria)
13. [Troubleshooting](#part-13-troubleshooting)

---

## Part 1: Library Setup

### Package Installation

```bash
# UraniumUI (already installed)
dotnet add package UraniumUI.Material --version 2.14.0

# HorusSoftware MaterialDesignControls (NEW)
dotnet add package HorusStudio.Maui.MaterialDesignControls --version 10.0.0

# Material Symbols Icons (for icon fonts)
dotnet add package UraniumUI.Icons.MaterialSymbols --version 2.10.0
```

### MauiProgram.cs Configuration

```csharp
using CommunityToolkit.Maui;
using HorusStudio.Maui.MaterialDesignControls;
using Microsoft.Extensions.Logging;
using UraniumUI;

namespace MyVocaList;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseMaterialDesignControls(ConfigureMDC)
            .ConfigureFonts(fonts =>
            {
                // MD3 Default: Roboto (replaces OpenSans)
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                
                // Material Symbols Icons
                fonts.AddMaterialSymbolsFonts();
            });

        // Register Services
        builder.Services.AddSingleton<IThreadSafeDialogService, ThreadSafeDialogService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureMDC(MaterialDesignControlsBuilder options)
    {
#if DEBUG
        options.EnableDebug();
#endif
        options.OnException((sender, ex) =>
        {
            System.Diagnostics.Debug.WriteLine($"[MDC] {sender}: {ex}");
        });
        
        options.ConfigureThemesFromResources();
        options.ConfigureStringFormat(new MaterialFormatOptions
        {
            DateFormat = "dd/MM/yyyy",
            TimeFormat = "HH:mm"
        });
    }
}
```

### App.xaml.cs Initialization

```csharp
using HorusStudio.Maui.MaterialDesignControls;

namespace MyVocaList;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MaterialDesignControls.InitializeComponents(); // CRITICAL: After InitializeComponent
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
```

### XAML Namespaces (All Pages)

```xml
xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui"
xmlns:mdc="clr-namespace:HorusStudio.Maui.MaterialDesignControls;assembly=HorusStudio.Maui.MaterialDesignControls"
xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"
```

---

## Part 2: Roboto Typography Setup

### Download Roboto Fonts

Download from Google Fonts: https://fonts.google.com/specimen/Roboto

**Required Files** (place in `Resources/Fonts/`):

| File | Weight | Usage |
|------|--------|-------|
| Roboto-Regular.ttf | 400 | Body, Display, Headline |
| Roboto-Medium.ttf | 500 | Title, Label |
| Roboto-Bold.ttf | 700 | Emphasis (custom) |

### Font Registration in MauiProgram.cs

```csharp
.ConfigureFonts(fonts =>
{
    // MD3 Roboto Typography
    fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
    fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
    fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
    
    // Material Symbols Icons
    fonts.AddMaterialSymbolsFonts();
})
```

### Typography StyleClasses (UraniumUI)

UraniumUI provides these StyleClasses automatically:

| StyleClass | Size | Weight | Use Case |
|------------|------|--------|----------|
| `Display.Large` | 57sp | 400 | Hero text |
| `Display.Medium` | 45sp | 400 | Large headlines |
| `Display.Small` | 36sp | 400 | Section headers |
| `Headline.Large` | 32sp | 400 | Page titles |
| `Headline.Medium` | 28sp | 400 | Card titles |
| `Headline.Small` | 24sp | 400 | Subsections |
| `Title.Large` | 22sp | 400 | App bar, dialog |
| `Title.Medium` | 16sp | 500 | List headers |
| `Title.Small` | 14sp | 500 | Tabs, chips |
| `Body.Large` | 16sp | 400 | Primary content |
| `Body.Medium` | 14sp | 400 | Secondary content |
| `Body.Small` | 12sp | 400 | Captions |
| `Label.Large` | 14sp | 500 | Buttons, inputs |
| `Label.Medium` | 12sp | 500 | Tags, badges |
| `Label.Small` | 11sp | 500 | Timestamps |

### Usage in XAML

```xml
<Label Text="Page Title" StyleClass="Headline.Large" />
<Label Text="Card Title" StyleClass="Title.Medium" />
<Label Text="Body content here" StyleClass="Body.Medium" />
<Label Text="12:34 PM" StyleClass="Label.Small" />
```

---

## Part 3: Thread-Safe Infrastructure

### ThreadSafeViewModelBase

Location: `UI/ViewModels/ThreadSafeViewModelBase.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Base ViewModel with thread-safe UI update helpers
/// </summary>
public abstract class ThreadSafeViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        RunOnUiThread(() => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

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

    protected async Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> asyncAction)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
            return await Application.Current.Dispatcher.DispatchAsync(asyncAction);
        else
            return await asyncAction();
    }
}
```

### ThreadSafeDialogService

Location: `UI/Services/ThreadSafeDialogService.cs`

```csharp
namespace MyVocaList.UI.Services;

/// <summary>
/// Thread-safe wrapper for dialog operations
/// </summary>
public interface IThreadSafeDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    Task AlertAsync(string title, string message, string accept = "OK");
    Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}

public class ThreadSafeDialogService : IThreadSafeDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            return await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayAlert(title, message, accept, cancel));
        }
        return await Application.Current!.MainPage!.DisplayAlert(title, message, accept, cancel);
    }

    public async Task AlertAsync(string title, string message, string accept = "OK")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayAlert(title, message, accept));
            return;
        }
        await Application.Current!.MainPage!.DisplayAlert(title, message, accept);
    }

    public async Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            return await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayPromptAsync(title, message, accept, cancel));
        }
        return await Application.Current!.MainPage!.DisplayPromptAsync(title, message, accept, cancel);
    }
}
```

### DI Registration

In `MauiProgram.cs` (inside `CreateMauiApp()`):

```csharp
// Register Services
builder.Services.AddSingleton<IThreadSafeDialogService, ThreadSafeDialogService>();
```

### Thread-Safe Collection Pattern

```csharp
private ObservableCollection<ItemViewModel> _items = new();
public ObservableCollection<ItemViewModel> Items => _items;

private async Task LoadDataAsync()
{
    // Heavy work on background thread
    var data = await Task.Run(() => _repository.GetAll());
    
    // Marshal collection updates to UI thread
    RunOnUiThread(() =>
    {
        _items.Clear();
        foreach (var item in data)
            _items.Add(new ItemViewModel(item));
    });
}
```

---

## Part 4: Component Library Mapping

### Complete Component Matrix

| Component | Library | XAML Usage | Notes |
|-----------|---------|------------|-------|
| **Buttons** | UraniumUI | `StyleClass="FilledButton"` | 5 variants |
| **Icon Buttons** | MDC | `<mdc:MaterialIconButton>` | 4 variants |
| **Typography** | UraniumUI | `StyleClass="Headline.Large"` | 15 roles |
| **TextField** | UraniumUI | `<uranium:TextField>` | With validation |
| **Cards** | MDC | `<mdc:MaterialCard Type="Elevated">` | 3 types |
| **FAB** | MDC | `<mdc:MaterialFloatingButton>` | 3 sizes |
| **Snackbar** | MDC | `IMaterialSnackbar` | Via service |
| **Chips** | MDC | `<mdc:MaterialChip>` | 4 types |
| **Progress** | MDC | `<mdc:MaterialProgressIndicator>` | Linear/Circular |
| **Divider** | MDC | `<mdc:MaterialDivider>` | Horizontal/Vertical |
| **Checkbox** | MAUI | `<CheckBox>` | Style with colors |
| **Radio** | MAUI | `<RadioButton>` | Style with colors |
| **Switch** | MAUI | `<Switch>` | Style with colors |
| **Slider** | MAUI | `<Slider>` | Style with colors |
| **Lists** | MAUI | `<CollectionView>` | With DataTemplates |
| **Swipe Actions** | MAUI | `<SwipeView>` | Native component |
| **Dialogs** | Service | `IThreadSafeDialogService` | Thread-safe |
| **Date Picker** | MDC | `<mdc:MaterialDatePicker>` | Modal picker |
| **Time Picker** | MDC | `<mdc:MaterialTimePicker>` | Dial/Input |
| **Colors** | Shared | `MaterialColors.xaml` | Tonal palettes |

### Button Variants (UraniumUI)

```xml
<Button Text="Filled" StyleClass="FilledButton" />
<Button Text="Filled Tonal" StyleClass="FilledTonalButton" />
<Button Text="Elevated" StyleClass="ElevatedButton" />
<Button Text="Outlined" StyleClass="OutlinedButton" />
<Button Text="Text" StyleClass="TextButton" />
```

### Button with Icon

```xml
<Button Text="Add Item" StyleClass="FilledButton">
    <Button.ImageSource>
        <FontImageSource FontFamily="MaterialOutlined" 
                         Glyph="{x:Static m:MaterialOutlined.Add}" />
    </Button.ImageSource>
</Button>
```

### Card Types (MDC)

```xml
<!-- Elevated Card: Shadow + SurfaceContainerLow -->
<mdc:MaterialCard Type="Elevated">
    <VerticalStackLayout>
        <Label Text="Elevated Card" StyleClass="Title.Medium" />
        <Label Text="With shadow elevation" StyleClass="Body.Medium" />
    </VerticalStackLayout>
</mdc:MaterialCard>

<!-- Filled Card: SurfaceContainerHighest, no shadow -->
<mdc:MaterialCard Type="Filled">
    <Label Text="Filled Card" StyleClass="Body.Medium" />
</mdc:MaterialCard>

<!-- Outlined Card: 1dp outline, Surface background -->
<mdc:MaterialCard Type="Outlined">
    <Label Text="Outlined Card" StyleClass="Body.Medium" />
</mdc:MaterialCard>
```

### FAB Sizes (MDC)

```xml
<!-- Small FAB: 40x40dp -->
<mdc:MaterialFloatingButton Icon="add.png" Type="Small" />

<!-- Standard FAB: 56x56dp (default) -->
<mdc:MaterialFloatingButton Icon="add.png" />

<!-- Large FAB: 96x96dp -->
<mdc:MaterialFloatingButton Icon="add.png" Type="Large" />

<!-- Extended FAB: With text label -->
<mdc:MaterialFloatingButton Icon="add.png" Text="Add Song" Type="Extended" />
```

### Selection Controls (MAUI + Styling)

```xml
<!-- Checkbox -->
<CheckBox IsChecked="True" Color="{StaticResource Primary}" />

<!-- Radio Button Group -->
<RadioButton Content="Option A" GroupName="options" />
<RadioButton Content="Option B" GroupName="options" />

<!-- Switch -->
<Switch IsToggled="True" 
        OnColor="{StaticResource Primary}"
        ThumbColor="{StaticResource OnPrimary}" />

<!-- Slider -->
<Slider Minimum="0" Maximum="100" Value="50"
        MinimumTrackColor="{StaticResource Primary}"
        MaximumTrackColor="{StaticResource SurfaceContainerHighest}"
        ThumbColor="{StaticResource Primary}" />
```

### List with DataTemplate

```xml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Grid Padding="16,12" ColumnDefinitions="Auto,*,Auto">
                <!-- Leading: Avatar/Icon -->
                <Frame Grid.Column="0" WidthRequest="40" HeightRequest="40"
                       CornerRadius="20" BackgroundColor="{StaticResource PrimaryContainer}">
                    <Label Text="{Binding Initials}" StyleClass="Title.Small"
                           HorizontalOptions="Center" VerticalOptions="Center" />
                </Frame>
                
                <!-- Content: Title + Subtitle -->
                <VerticalStackLayout Grid.Column="1" Padding="16,0">
                    <Label Text="{Binding Title}" StyleClass="Body.Large" />
                    <Label Text="{Binding Subtitle}" StyleClass="Body.Medium"
                           TextColor="{StaticResource OnSurfaceVariant}" />
                </VerticalStackLayout>
                
                <!-- Trailing: Icon/Action -->
                <Image Grid.Column="2" WidthRequest="24" HeightRequest="24">
                    <Image.Source>
                        <FontImageSource FontFamily="MaterialOutlined"
                                         Glyph="{x:Static m:MaterialOutlined.Chevron_right}"
                                         Color="{StaticResource OnSurfaceVariant}" />
                    </Image.Source>
                </Image>
            </Grid>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

### SwipeView with Actions

```xml
<SwipeView>
    <SwipeView.RightItems>
        <SwipeItems Mode="Reveal">
            <SwipeItem Text="Delete" 
                       BackgroundColor="{StaticResource Error}"
                       Command="{Binding DeleteCommand}" />
        </SwipeItems>
    </SwipeView.RightItems>
    
    <SwipeView.LeftItems>
        <SwipeItems Mode="Reveal">
            <SwipeItem Text="Archive"
                       BackgroundColor="{StaticResource Secondary}"
                       Command="{Binding ArchiveCommand}" />
        </SwipeItems>
    </SwipeView.LeftItems>
    
    <!-- Item Content -->
    <Grid Padding="16">
        <Label Text="{Binding Title}" StyleClass="Body.Large" />
    </Grid>
</SwipeView>
```

---

## Part 5: Icons Setup

### Material Symbols Configuration

Material Symbols is a variable icon font with 2,500+ icons.

**Package** (already in setup):
```bash
dotnet add package UraniumUI.Icons.MaterialSymbols --version 2.10.0
```

**Font Registration** (in ConfigureFonts):
```csharp
fonts.AddMaterialSymbolsFonts();
```

**XAML Namespace**:
```xml
xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"
```

### Icon Usage Patterns

**In Button:**
```xml
<Button Text="Home" StyleClass="FilledButton">
    <Button.ImageSource>
        <FontImageSource FontFamily="MaterialOutlined" 
                         Glyph="{x:Static m:MaterialOutlined.Home}" />
    </Button.ImageSource>
</Button>
```

**Standalone Icon:**
```xml
<Image WidthRequest="24" HeightRequest="24">
    <Image.Source>
        <FontImageSource FontFamily="MaterialOutlined"
                         Glyph="{x:Static m:MaterialOutlined.Settings}"
                         Color="{StaticResource OnSurface}" />
    </Image.Source>
</Image>
```

**In Navigation:**
```xml
<!-- Note: TabBar icons use SVG files, not FontImageSource -->
<Tab Title="Home" Icon="home.svg">
    <ShellContent ContentTemplate="{DataTemplate local:HomePage}" />
</Tab>
```

### Common Icons Reference

| Icon | Glyph | Use Case |
|------|-------|----------|
| Add | `MaterialOutlined.Add` | FAB, create actions |
| Delete | `MaterialOutlined.Delete` | Remove items |
| Edit | `MaterialOutlined.Edit` | Modify content |
| Settings | `MaterialOutlined.Settings` | Configuration |
| Search | `MaterialOutlined.Search` | Search actions |
| Close | `MaterialOutlined.Close` | Dismiss, cancel |
| Check | `MaterialOutlined.Check` | Confirm, done |
| Menu | `MaterialOutlined.Menu` | Navigation drawer |
| More Vert | `MaterialOutlined.More_vert` | Overflow menu |
| Arrow Back | `MaterialOutlined.Arrow_back` | Navigation |
| Home | `MaterialOutlined.Home` | Home screen |
| Person | `MaterialOutlined.Person` | User, profile |
| Star | `MaterialOutlined.Star` | Favorites |
| Play | `MaterialOutlined.Play_arrow` | Media playback |
| Mic | `MaterialOutlined.Mic` | Voice/karaoke |
| Queue Music | `MaterialOutlined.Queue_music` | Song queue |

### Icon Sizes by Context

| Context | Size | Touch Target |
|---------|------|--------------|
| Navigation Bar | 24dp | 48dp |
| App Bar Actions | 24dp | 48dp |
| FAB (Standard) | 24dp | 56dp |
| FAB (Large) | 36dp | 96dp |
| List Leading | 24dp | - |
| Button with Icon | 18dp | 40dp |
| Chip | 18dp | 32dp |

---

## Part 6: Priority Tiers

### TIER 1: Critical (Must Implement)

| Component | Status | Implementation |
|-----------|--------|----------------|
| Color System | EXISTS | MaterialColors.xaml |
| Typography (15 roles) | NEEDS UPDATE | Add Roboto fonts |
| Buttons (5 variants) | EXISTS | UraniumUI StyleClass |
| Cards (3 types) | NEEDS MDC | mdc:MaterialCard |
| FAB (3 sizes) | NEEDS MDC | mdc:MaterialFloatingButton |
| TextField | EXISTS | uranium:TextField |
| Snackbar | NEEDS MDC | IMaterialSnackbar |
| Lists | EXISTS | CollectionView + templates |
| Icons | NEEDS SETUP | Material Symbols |

### TIER 2: Important (Should Implement)

| Component | Status | Implementation |
|-----------|--------|----------------|
| Dialogs | NEEDS SERVICE | IThreadSafeDialogService |
| SwipeView | NATIVE | MAUI SwipeView |
| Checkbox | NATIVE | MAUI + styling |
| Switch | NATIVE | MAUI + styling |
| Progress | NEEDS MDC | mdc:MaterialProgressIndicator |
| Divider | NEEDS MDC | mdc:MaterialDivider |

### TIER 3: Nice-to-Have (May Skip for MVP)

| Component | Notes |
|-----------|-------|
| Chips | Only if genre filters needed |
| Bottom Sheets | May use dialogs instead |
| Segmented Buttons | Only if view modes needed |
| Slider | Only if volume/progress needed |
| Radio Buttons | Only if exclusive selection needed |
| Date/Time Pickers | Only if scheduling needed |

### NOT IMPLEMENTING (MVP)

- M3 Expressive spring physics motion
- Shape morphing animations
- Container transform transitions
- Carousel
- Complex adaptive layouts
- Navigation Rail (compact devices only)
- Side Sheets
- Rich Tooltips

---

## Part 7: MaterialStyles.xaml Updates

### Updated MaterialStyles.xaml with Roboto

Add/update in `Resources/Styles/MaterialStyles.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary 
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui">

    <!-- ========================================================================= -->
    <!-- CONTAINER STYLES                                                          -->
    <!-- ========================================================================= -->

    <Style x:Key="PageContainer" TargetType="VerticalStackLayout">
        <Setter Property="Padding" Value="16" />
        <Setter Property="Spacing" Value="16" />
    </Style>

    <Style x:Key="SectionContainer" TargetType="VerticalStackLayout">
        <Setter Property="Spacing" Value="12" />
        <Setter Property="Padding" Value="0" />
        <Setter Property="Margin" Value="0,0,0,24" />
    </Style>

    <!-- ========================================================================= -->
    <!-- MATERIAL DESIGN 3 CARDS                                                   -->
    <!-- ========================================================================= -->

    <Style x:Key="ElevatedCard" TargetType="Frame">
        <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerLow}" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="16" />
        <Setter Property="HasShadow" Value="True" />
        <Setter Property="Margin" Value="0" />
    </Style>

    <Style x:Key="OutlinedCard" TargetType="Frame">
        <Setter Property="BackgroundColor" Value="{StaticResource Surface}" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="16" />
        <Setter Property="HasShadow" Value="False" />
        <Setter Property="BorderColor" Value="{StaticResource Outline}" />
        <Setter Property="Margin" Value="0" />
    </Style>

    <Style x:Key="FilledCard" TargetType="Frame">
        <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerHighest}" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="16" />
        <Setter Property="HasShadow" Value="False" />
        <Setter Property="Margin" Value="0" />
    </Style>

    <!-- ========================================================================= -->
    <!-- CUSTOM CARDS (MyVocaList-specific)                                        -->
    <!-- ========================================================================= -->

    <Style x:Key="GradientCard" TargetType="Frame">
        <Setter Property="Background" Value="{StaticResource CardBackgroundGradient}" />
        <Setter Property="CornerRadius" Value="16" />
        <Setter Property="HasShadow" Value="False" />
        <Setter Property="Padding" Value="16" />
    </Style>

    <Style x:Key="WelcomeCard" TargetType="Frame">
        <Setter Property="BackgroundColor" Value="{StaticResource PrimaryContainer}" />
        <Setter Property="CornerRadius" Value="16" />
        <Setter Property="HasShadow" Value="False" />
        <Setter Property="Padding" Value="16" />
    </Style>

    <!-- ========================================================================= -->
    <!-- FAB CONTAINER                                                             -->
    <!-- ========================================================================= -->

    <Style x:Key="FabContainer" TargetType="uranium:StatefulContentView">
        <Setter Property="WidthRequest" Value="56" />
        <Setter Property="HeightRequest" Value="56" />
        <Setter Property="Background" Value="{StaticResource FabGradient}" />
        <Setter Property="Shadow">
            <Shadow Brush="{StaticResource Shadow}" Offset="0,2" Radius="4" Opacity="0.3" />
        </Setter>
    </Style>

    <!-- ========================================================================= -->
    <!-- SHELL & NAVIGATION STYLES                                                 -->
    <!-- ========================================================================= -->

    <Style TargetType="Shell" ApplyToDerivedTypes="True">
        <Setter Property="Shell.BackgroundColor" Value="{StaticResource Surface}" />
        <Setter Property="Shell.ForegroundColor" Value="{StaticResource OnSurface}" />
        <Setter Property="Shell.TitleColor" Value="{StaticResource OnSurface}" />
        <Setter Property="Shell.NavBarHasShadow" Value="False" />
        <Setter Property="Shell.TabBarBackgroundColor" Value="{StaticResource SurfaceContainer}" />
        <Setter Property="Shell.TabBarForegroundColor" Value="{StaticResource Primary}" />
        <Setter Property="Shell.TabBarUnselectedColor" Value="{StaticResource OnSurfaceVariant}" />
    </Style>

    <Style TargetType="NavigationPage">
        <Setter Property="BarBackgroundColor" Value="{StaticResource Surface}" />
        <Setter Property="BarTextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- ========================================================================= -->
    <!-- MATERIAL DESIGN 3 TYPOGRAPHY (Roboto)                                     -->
    <!-- Note: UraniumUI handles implicit styling - these are explicit overrides   -->
    <!-- ========================================================================= -->

    <!-- Display Typography -->
    <Style TargetType="Label" Class="Display.Large">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="57" />
        <Setter Property="LineHeight" Value="1.12" />
    </Style>

    <Style TargetType="Label" Class="Display.Medium">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="45" />
        <Setter Property="LineHeight" Value="1.15" />
    </Style>

    <Style TargetType="Label" Class="Display.Small">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="36" />
        <Setter Property="LineHeight" Value="1.22" />
    </Style>

    <!-- Headline Typography -->
    <Style TargetType="Label" Class="Headline.Large">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="32" />
        <Setter Property="LineHeight" Value="1.25" />
    </Style>

    <Style TargetType="Label" Class="Headline.Medium">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="28" />
        <Setter Property="LineHeight" Value="1.28" />
    </Style>

    <Style TargetType="Label" Class="Headline.Small">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="24" />
        <Setter Property="LineHeight" Value="1.33" />
    </Style>

    <!-- Title Typography -->
    <Style TargetType="Label" Class="Title.Large">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="22" />
        <Setter Property="LineHeight" Value="1.27" />
    </Style>

    <Style TargetType="Label" Class="Title.Medium">
        <Setter Property="FontFamily" Value="RobotoMedium" />
        <Setter Property="FontSize" Value="16" />
        <Setter Property="LineHeight" Value="1.5" />
    </Style>

    <Style TargetType="Label" Class="Title.Small">
        <Setter Property="FontFamily" Value="RobotoMedium" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="LineHeight" Value="1.43" />
    </Style>

    <!-- Body Typography -->
    <Style TargetType="Label" Class="Body.Large">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="16" />
        <Setter Property="LineHeight" Value="1.5" />
    </Style>

    <Style TargetType="Label" Class="Body.Medium">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="LineHeight" Value="1.43" />
    </Style>

    <Style TargetType="Label" Class="Body.Small">
        <Setter Property="FontFamily" Value="RobotoRegular" />
        <Setter Property="FontSize" Value="12" />
        <Setter Property="LineHeight" Value="1.33" />
    </Style>

    <!-- Label Typography -->
    <Style TargetType="Label" Class="Label.Large">
        <Setter Property="FontFamily" Value="RobotoMedium" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="LineHeight" Value="1.43" />
    </Style>

    <Style TargetType="Label" Class="Label.Medium">
        <Setter Property="FontFamily" Value="RobotoMedium" />
        <Setter Property="FontSize" Value="12" />
        <Setter Property="LineHeight" Value="1.33" />
    </Style>

    <Style TargetType="Label" Class="Label.Small">
        <Setter Property="FontFamily" Value="RobotoMedium" />
        <Setter Property="FontSize" Value="11" />
        <Setter Property="LineHeight" Value="1.45" />
    </Style>

    <!-- ========================================================================= -->
    <!-- LIST ITEM STYLES                                                          -->
    <!-- ========================================================================= -->

    <Style x:Key="ListItemContainer" TargetType="Grid">
        <Setter Property="Padding" Value="16,12" />
        <Setter Property="MinimumHeightRequest" Value="56" />
    </Style>

    <Style x:Key="ListItemTwoLine" TargetType="Grid">
        <Setter Property="Padding" Value="16,12" />
        <Setter Property="MinimumHeightRequest" Value="72" />
    </Style>

    <Style x:Key="ListItemThreeLine" TargetType="Grid">
        <Setter Property="Padding" Value="16,12" />
        <Setter Property="MinimumHeightRequest" Value="88" />
    </Style>

    <!-- ========================================================================= -->
    <!-- SELECTION CONTROL STYLES                                                  -->
    <!-- ========================================================================= -->

    <Style x:Key="MD3CheckBox" TargetType="CheckBox">
        <Setter Property="Color" Value="{StaticResource Primary}" />
        <Setter Property="MinimumWidthRequest" Value="48" />
        <Setter Property="MinimumHeightRequest" Value="48" />
    </Style>

    <Style x:Key="MD3Switch" TargetType="Switch">
        <Setter Property="OnColor" Value="{StaticResource Primary}" />
        <Setter Property="ThumbColor" Value="{StaticResource OnPrimary}" />
        <Setter Property="MinimumWidthRequest" Value="52" />
        <Setter Property="MinimumHeightRequest" Value="48" />
    </Style>

    <Style x:Key="MD3Slider" TargetType="Slider">
        <Setter Property="MinimumTrackColor" Value="{StaticResource Primary}" />
        <Setter Property="MaximumTrackColor" Value="{StaticResource SurfaceContainerHighest}" />
        <Setter Property="ThumbColor" Value="{StaticResource Primary}" />
        <Setter Property="MinimumHeightRequest" Value="48" />
    </Style>

</ResourceDictionary>
```

---

## Part 8: Demo Pages Specification

### Page Structure

```
UI/Pages/DesignSystem/
├── DesignSystemPage.xaml          # Navigation hub (EXISTS - update)
├── HomePage.xaml                   # Welcome (EXISTS - update)
├── ComponentsPage_Typography.xaml  # NEW: All 15 typography roles
├── ComponentsPage_Buttons.xaml     # NEW: Button variants + icons
├── ComponentsPage_Cards.xaml       # NEW: Card types
├── ComponentsPage_Inputs.xaml      # NEW: TextField, selection controls
├── ComponentsPage_Lists.xaml       # NEW: List patterns, swipe
├── ComponentsPage_Feedback.xaml    # NEW: FAB, Snackbar, Progress
└── PlainMauiPage.xaml             # Performance comparison (EXISTS)
```

### Task List for Claude Code

#### Task 1: Download and Add Roboto Fonts

1. Download from https://fonts.google.com/specimen/Roboto
2. Place in `Resources/Fonts/`:
   - Roboto-Regular.ttf
   - Roboto-Medium.ttf
   - Roboto-Bold.ttf
3. Ensure Build Action: `MauiFont`

#### Task 2: Update MauiProgram.cs

- Replace OpenSans with Roboto fonts
- Add Material Symbols font registration
- Add DI service registration

```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
    fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
    fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
    fonts.AddMaterialSymbolsFonts();
})

// In CreateMauiApp():
builder.Services.AddSingleton<IThreadSafeDialogService, ThreadSafeDialogService>();
```

#### Task 3: Update App.xaml.cs

- Add `MaterialDesignControls.InitializeComponents()`

#### Task 4: Create ThreadSafeViewModelBase

- Location: `UI/ViewModels/ThreadSafeViewModelBase.cs`
- Use code from Part 3

#### Task 5: Create ThreadSafeDialogService

- Location: `UI/Services/ThreadSafeDialogService.cs`
- Use code from Part 3

#### Task 6: Update MaterialStyles.xaml

- Add Roboto FontFamily to all typography styles
- Add list item styles
- Add selection control styles
- Use code from Part 7

#### Task 7: Create ComponentsPage_Typography.xaml

Show all 15 typography roles with Roboto:

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <!-- Display Styles -->
            <Label Text="Display Styles" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            <Label Text="Display Large (57sp)" StyleClass="Display.Large" />
            <Label Text="Display Medium (45sp)" StyleClass="Display.Medium" />
            <Label Text="Display Small (36sp)" StyleClass="Display.Small" />
            
            <!-- Headline Styles -->
            <Label Text="Headline Styles" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            <Label Text="Headline Large (32sp)" StyleClass="Headline.Large" />
            <Label Text="Headline Medium (28sp)" StyleClass="Headline.Medium" />
            <Label Text="Headline Small (24sp)" StyleClass="Headline.Small" />
            
            <!-- Title Styles -->
            <Label Text="Title Styles" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            <Label Text="Title Large (22sp)" StyleClass="Title.Large" />
            <Label Text="Title Medium (16sp)" StyleClass="Title.Medium" />
            <Label Text="Title Small (14sp)" StyleClass="Title.Small" />
            
            <!-- Body Styles -->
            <Label Text="Body Styles" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            <Label Text="Body Large (16sp) - Primary content text" StyleClass="Body.Large" />
            <Label Text="Body Medium (14sp) - Secondary content" StyleClass="Body.Medium" />
            <Label Text="Body Small (12sp) - Captions" StyleClass="Body.Small" />
            
            <!-- Label Styles -->
            <Label Text="Label Styles" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            <Label Text="Label Large (14sp)" StyleClass="Label.Large" />
            <Label Text="Label Medium (12sp)" StyleClass="Label.Medium" />
            <Label Text="Label Small (11sp)" StyleClass="Label.Small" />
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

#### Task 8: Create ComponentsPage_Buttons.xaml

Show all button variants with icons:

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="Button Variants" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <Button Text="Filled Button" StyleClass="FilledButton" />
            <Button Text="Filled Tonal" StyleClass="FilledTonalButton" />
            <Button Text="Elevated" StyleClass="ElevatedButton" />
            <Button Text="Outlined" StyleClass="OutlinedButton" />
            <Button Text="Text Button" StyleClass="TextButton" />
            
            <Label Text="Buttons with Icons" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <Button Text="Add Item" StyleClass="FilledButton">
                <Button.ImageSource>
                    <FontImageSource FontFamily="MaterialOutlined" 
                                     Glyph="{x:Static m:MaterialOutlined.Add}" />
                </Button.ImageSource>
            </Button>
            
            <Button Text="Delete" StyleClass="OutlinedButton">
                <Button.ImageSource>
                    <FontImageSource FontFamily="MaterialOutlined" 
                                     Glyph="{x:Static m:MaterialOutlined.Delete}" />
                </Button.ImageSource>
            </Button>
            
            <Label Text="Icon Buttons (MDC)" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <HorizontalStackLayout Spacing="16">
                <mdc:MaterialIconButton Icon="star.png" />
                <mdc:MaterialIconButton Icon="settings.png" />
                <mdc:MaterialIconButton Icon="search.png" />
            </HorizontalStackLayout>
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

#### Task 9: Create ComponentsPage_Cards.xaml

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="Card Types (MDC)" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <mdc:MaterialCard Type="Elevated">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Elevated Card" StyleClass="Title.Medium" />
                    <Label Text="With shadow and SurfaceContainerLow background" 
                           StyleClass="Body.Medium" />
                </VerticalStackLayout>
            </mdc:MaterialCard>
            
            <mdc:MaterialCard Type="Filled">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Filled Card" StyleClass="Title.Medium" />
                    <Label Text="SurfaceContainerHighest background, no shadow" 
                           StyleClass="Body.Medium" />
                </VerticalStackLayout>
            </mdc:MaterialCard>
            
            <mdc:MaterialCard Type="Outlined">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Outlined Card" StyleClass="Title.Medium" />
                    <Label Text="1dp outline, Surface background" 
                           StyleClass="Body.Medium" />
                </VerticalStackLayout>
            </mdc:MaterialCard>
            
            <Label Text="Card Types (Frame Fallback)" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <Frame Style="{StaticResource ElevatedCard}">
                <Label Text="Frame-based Elevated Card" StyleClass="Body.Medium" />
            </Frame>
            
            <Frame Style="{StaticResource FilledCard}">
                <Label Text="Frame-based Filled Card" StyleClass="Body.Medium" />
            </Frame>
            
            <Frame Style="{StaticResource OutlinedCard}">
                <Label Text="Frame-based Outlined Card" StyleClass="Body.Medium" />
            </Frame>
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

#### Task 10: Create ComponentsPage_Inputs.xaml

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="Text Fields (UraniumUI)" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <uranium:TextField Title="Standard Input" />
            <uranium:TextField Title="With Placeholder" Placeholder="Enter text..." />
            <uranium:TextField Title="Required Field" IsRequired="True" />
            <uranium:TextField Title="With Icon" Icon="{x:Static m:MaterialOutlined.Search}" />
            
            <Label Text="Selection Controls" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <HorizontalStackLayout Spacing="16" VerticalOptions="Center">
                <CheckBox Style="{StaticResource MD3CheckBox}" />
                <Label Text="Checkbox option" StyleClass="Body.Medium" VerticalOptions="Center" />
            </HorizontalStackLayout>
            
            <HorizontalStackLayout Spacing="16" VerticalOptions="Center">
                <Switch Style="{StaticResource MD3Switch}" />
                <Label Text="Toggle switch" StyleClass="Body.Medium" VerticalOptions="Center" />
            </HorizontalStackLayout>
            
            <Label Text="Volume" StyleClass="Label.Medium" />
            <Slider Style="{StaticResource MD3Slider}" Value="50" />
            
            <Label Text="Pickers (MDC)" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <mdc:MaterialDatePicker Title="Select Date" />
            <mdc:MaterialTimePicker Title="Select Time" />
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

#### Task 11: Create ComponentsPage_Lists.xaml

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="List Items" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <!-- One-line item -->
            <Grid Style="{StaticResource ListItemContainer}" ColumnDefinitions="*,Auto">
                <Label Text="One-line list item" StyleClass="Body.Large" VerticalOptions="Center" />
                <Image Grid.Column="1" WidthRequest="24" HeightRequest="24">
                    <Image.Source>
                        <FontImageSource FontFamily="MaterialOutlined"
                                         Glyph="{x:Static m:MaterialOutlined.Chevron_right}"
                                         Color="{StaticResource OnSurfaceVariant}" />
                    </Image.Source>
                </Image>
            </Grid>
            
            <mdc:MaterialDivider />
            
            <!-- Two-line item with avatar -->
            <Grid Style="{StaticResource ListItemTwoLine}" ColumnDefinitions="Auto,*,Auto">
                <Frame Grid.Column="0" WidthRequest="40" HeightRequest="40"
                       CornerRadius="20" BackgroundColor="{StaticResource PrimaryContainer}"
                       Padding="0" HasShadow="False">
                    <Label Text="JD" StyleClass="Title.Small"
                           HorizontalOptions="Center" VerticalOptions="Center" />
                </Frame>
                
                <VerticalStackLayout Grid.Column="1" Padding="16,0" VerticalOptions="Center">
                    <Label Text="Two-line item" StyleClass="Body.Large" />
                    <Label Text="Secondary text here" StyleClass="Body.Medium"
                           TextColor="{StaticResource OnSurfaceVariant}" />
                </VerticalStackLayout>
                
                <Image Grid.Column="2" WidthRequest="24" HeightRequest="24" VerticalOptions="Center">
                    <Image.Source>
                        <FontImageSource FontFamily="MaterialOutlined"
                                         Glyph="{x:Static m:MaterialOutlined.More_vert}"
                                         Color="{StaticResource OnSurfaceVariant}" />
                    </Image.Source>
                </Image>
            </Grid>
            
            <mdc:MaterialDivider />
            
            <Label Text="Swipe Actions" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <SwipeView>
                <SwipeView.RightItems>
                    <SwipeItems Mode="Reveal">
                        <SwipeItem Text="Delete" BackgroundColor="{StaticResource Error}" />
                    </SwipeItems>
                </SwipeView.RightItems>
                <SwipeView.LeftItems>
                    <SwipeItems Mode="Reveal">
                        <SwipeItem Text="Archive" BackgroundColor="{StaticResource Secondary}" />
                    </SwipeItems>
                </SwipeView.LeftItems>
                
                <Grid Style="{StaticResource ListItemTwoLine}" BackgroundColor="{StaticResource Surface}">
                    <VerticalStackLayout VerticalOptions="Center">
                        <Label Text="Swipeable item" StyleClass="Body.Large" />
                        <Label Text="Swipe left or right" StyleClass="Body.Medium"
                               TextColor="{StaticResource OnSurfaceVariant}" />
                    </VerticalStackLayout>
                </Grid>
            </SwipeView>
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

#### Task 12: Create ComponentsPage_Feedback.xaml

```xml
<uranium:UraniumContentPage 
    x:Class="MyVocaList.UI.Pages.DesignSystem.ComponentsPage_Feedback"
    ...>
    
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="FAB Sizes" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <HorizontalStackLayout Spacing="24" HorizontalOptions="Center">
                <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                    <mdc:MaterialFloatingButton Icon="add.png" Type="Small" />
                    <Label Text="Small" StyleClass="Label.Small" HorizontalOptions="Center" />
                </VerticalStackLayout>
                
                <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                    <mdc:MaterialFloatingButton Icon="add.png" />
                    <Label Text="Standard" StyleClass="Label.Small" HorizontalOptions="Center" />
                </VerticalStackLayout>
                
                <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                    <mdc:MaterialFloatingButton Icon="add.png" Type="Large" />
                    <Label Text="Large" StyleClass="Label.Small" HorizontalOptions="Center" />
                </VerticalStackLayout>
            </HorizontalStackLayout>
            
            <mdc:MaterialFloatingButton Icon="add.png" Text="Add Song" Type="Extended" 
                                        HorizontalOptions="Center" />
            
            <Label Text="Snackbar" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <Button Text="Show Snackbar" StyleClass="FilledButton" Clicked="OnShowSnackbar" />
            <Button Text="Show Snackbar with Action" StyleClass="OutlinedButton" 
                    Clicked="OnShowSnackbarWithAction" />
            
            <Label Text="Progress Indicators" StyleClass="Title.Medium" />
            <mdc:MaterialDivider />
            
            <Label Text="Indeterminate Circular" StyleClass="Label.Medium" />
            <mdc:MaterialProgressIndicator Type="Circular" IsIndeterminate="True" 
                                           HorizontalOptions="Center" />
            
            <Label Text="Determinate Linear (50%)" StyleClass="Label.Medium" />
            <mdc:MaterialProgressIndicator Type="Linear" Progress="0.5" />
            
            <Label Text="Indeterminate Linear" StyleClass="Label.Medium" />
            <mdc:MaterialProgressIndicator Type="Linear" IsIndeterminate="True" />
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

Code-behind:

```csharp
using HorusStudio.Maui.MaterialDesignControls;

namespace MyVocaList.UI.Pages.DesignSystem;

public partial class ComponentsPage_Feedback : UraniumUI.Pages.UraniumContentPage
{
    public ComponentsPage_Feedback()
    {
        InitializeComponent();
    }

    private async void OnShowSnackbar(object sender, EventArgs e)
    {
        var snackbar = Handler?.MauiContext?.Services.GetService<IMaterialSnackbar>();
        if (snackbar != null)
        {
            await snackbar.ShowAsync("This is a snackbar message");
        }
    }

    private async void OnShowSnackbarWithAction(object sender, EventArgs e)
    {
        var snackbar = Handler?.MauiContext?.Services.GetService<IMaterialSnackbar>();
        if (snackbar != null)
        {
            await snackbar.ShowAsync("Item deleted", "UNDO", () =>
            {
                // Undo action
            });
        }
    }
}
```

#### Task 13: Update DesignSystemPage.xaml (Navigation Hub)

```xml
<uranium:UraniumContentPage ...>
    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            
            <Label Text="Design System" StyleClass="Headline.Large" />
            <Label Text="MD3 Component Library" StyleClass="Body.Medium" 
                   TextColor="{StaticResource OnSurfaceVariant}" />
            
            <mdc:MaterialDivider Margin="0,8" />
            
            <Label Text="Foundations" StyleClass="Title.Medium" />
            
            <Button Text="Typography (15 Roles)" StyleClass="FilledTonalButton"
                    Clicked="NavigateToTypography" />
            
            <Label Text="Components" StyleClass="Title.Medium" />
            
            <Button Text="Buttons" StyleClass="FilledTonalButton"
                    Clicked="NavigateToButtons" />
            <Button Text="Cards" StyleClass="FilledTonalButton"
                    Clicked="NavigateToCards" />
            <Button Text="Inputs" StyleClass="FilledTonalButton"
                    Clicked="NavigateToInputs" />
            <Button Text="Lists" StyleClass="FilledTonalButton"
                    Clicked="NavigateToLists" />
            <Button Text="Feedback" StyleClass="FilledTonalButton"
                    Clicked="NavigateToFeedback" />
            
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

Code-behind:

```csharp
namespace MyVocaList.UI.Pages.DesignSystem;

public partial class DesignSystemPage : UraniumUI.Pages.UraniumContentPage
{
    public DesignSystemPage()
    {
        InitializeComponent();
    }

    private async void NavigateToTypography(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Typography");

    private async void NavigateToButtons(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Buttons");

    private async void NavigateToCards(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Cards");

    private async void NavigateToInputs(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Inputs");

    private async void NavigateToLists(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Lists");

    private async void NavigateToFeedback(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ComponentsPage_Feedback");
}
```

#### Task 14: Register Routes in AppShell.xaml.cs

```csharp
using MyVocaList.UI.Pages.DesignSystem;

namespace MyVocaList;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register Design System routes
        Routing.RegisterRoute("ComponentsPage_Typography", typeof(ComponentsPage_Typography));
        Routing.RegisterRoute("ComponentsPage_Buttons", typeof(ComponentsPage_Buttons));
        Routing.RegisterRoute("ComponentsPage_Cards", typeof(ComponentsPage_Cards));
        Routing.RegisterRoute("ComponentsPage_Inputs", typeof(ComponentsPage_Inputs));
        Routing.RegisterRoute("ComponentsPage_Lists", typeof(ComponentsPage_Lists));
        Routing.RegisterRoute("ComponentsPage_Feedback", typeof(ComponentsPage_Feedback));
    }
}
```

---

## Part 9: XAML Patterns Reference

### Page Template (UraniumUI)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<uranium:UraniumContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui"
    xmlns:mdc="clr-namespace:HorusStudio.Maui.MaterialDesignControls;assembly=HorusStudio.Maui.MaterialDesignControls"
    xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"
    x:Class="MyVocaList.UI.Pages.DesignSystem.ComponentsPage_Example"
    Title="Page Title">

    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            <!-- Content -->
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

### Section with Divider

```xml
<Label Text="Section Title" StyleClass="Title.Medium" />
<mdc:MaterialDivider />
<!-- Section content -->
```

### Card with Media + Content

```xml
<mdc:MaterialCard Type="Elevated">
    <VerticalStackLayout Spacing="0">
        <!-- Media area -->
        <Image Source="album_art.png" Aspect="AspectFill" HeightRequest="180" />
        
        <!-- Content area -->
        <VerticalStackLayout Padding="16" Spacing="8">
            <Label Text="Song Title" StyleClass="Title.Medium" />
            <Label Text="Artist Name" StyleClass="Body.Medium"
                   TextColor="{StaticResource OnSurfaceVariant}" />
        </VerticalStackLayout>
        
        <!-- Action area -->
        <HorizontalStackLayout Padding="8" Spacing="8">
            <Button Text="Play" StyleClass="TextButton" />
            <Button Text="Add to Queue" StyleClass="TextButton" />
        </HorizontalStackLayout>
    </VerticalStackLayout>
</mdc:MaterialCard>
```

### Empty State Pattern

```xml
<VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center" Spacing="16" Padding="32">
    <Image WidthRequest="96" HeightRequest="96" Opacity="0.6">
        <Image.Source>
            <FontImageSource FontFamily="MaterialOutlined"
                             Glyph="{x:Static m:MaterialOutlined.Queue_music}"
                             Color="{StaticResource OnSurfaceVariant}"
                             Size="96" />
        </Image.Source>
    </Image>
    <Label Text="No songs in queue" StyleClass="Title.Medium" HorizontalOptions="Center" />
    <Label Text="Add songs to get started" StyleClass="Body.Medium"
           TextColor="{StaticResource OnSurfaceVariant}" HorizontalOptions="Center" />
    <Button Text="Browse Songs" StyleClass="FilledButton" />
</VerticalStackLayout>
```

---

## Part 10: Accessibility Implementation

### Touch Target Requirements

**Minimum size: 48x48dp for all interactive elements**

```xml
<!-- Ensure minimum touch target -->
<Button Text="Action" StyleClass="FilledButton"
        MinimumWidthRequest="48" MinimumHeightRequest="48" />

<CheckBox Style="{StaticResource MD3CheckBox}" />
<!-- Style already includes MinimumWidthRequest="48" MinimumHeightRequest="48" -->
```

### Spacing Between Targets

**Minimum: 8dp between touch targets**

```xml
<HorizontalStackLayout Spacing="8">
    <Button Text="Save" StyleClass="FilledButton" />
    <Button Text="Cancel" StyleClass="OutlinedButton" />
</HorizontalStackLayout>
```

### Semantic Properties

```xml
<Button Text="Add song to queue"
        SemanticProperties.Description="Adds the current song to the karaoke queue"
        SemanticProperties.Hint="Double tap to add" />

<Image Source="album.png"
       SemanticProperties.Description="Album artwork for Song Title by Artist Name" />
```

### Color Independence

Never use color alone - always add icon or text:

```xml
<!-- Error state: Color + Icon + Text -->
<HorizontalStackLayout Spacing="8">
    <Image WidthRequest="20" HeightRequest="20">
        <Image.Source>
            <FontImageSource FontFamily="MaterialOutlined"
                             Glyph="{x:Static m:MaterialOutlined.Error}"
                             Color="{StaticResource Error}" />
        </Image.Source>
    </Image>
    <Label Text="Error: Invalid input" StyleClass="Body.Small"
           TextColor="{StaticResource Error}" />
</HorizontalStackLayout>
```

### Focus Indicators

MAUI provides default focus indicators. Ensure visible contrast:

```xml
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup x:Name="CommonStates">
        <VisualState x:Name="Focused">
            <VisualState.Setters>
                <Setter Property="BorderColor" Value="{StaticResource Primary}" />
                <Setter Property="BorderWidth" Value="2" />
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

---

## Part 11: Critical MD3 Values

### Touch Targets

| Element | Minimum Size | Recommended |
|---------|--------------|-------------|
| All interactive | 48x48dp | 56x56dp (primary) |
| Spacing between | 8dp | 16dp |

### Typography Sizes

| Role | Size | Minimum for accessibility |
|------|------|---------------------------|
| Body text | 14-16sp | 12sp minimum |
| Buttons | 14sp | - |
| Captions | 12sp | - |
| Labels | 11-14sp | - |

### Contrast Ratios

| Content | Ratio | WCAG |
|---------|-------|------|
| Normal text | 4.5:1 | AA |
| Large text | 3:1 | AA |
| UI components | 3:1 | AA |

### Spacing Scale

| Token | Value | Use |
|-------|-------|-----|
| Extra-small | 4dp | Tight grouping |
| Small | 8dp | Related items |
| Medium | 16dp | Default spacing |
| Large | 24dp | Section separation |
| Extra-large | 32dp | Major sections |

### Component Dimensions

| Component | Height | Notes |
|-----------|--------|-------|
| Button | 40dp | All variants |
| FAB (small) | 40dp | - |
| FAB (standard) | 56dp | Default |
| FAB (large) | 96dp | - |
| Chip | 32dp | - |
| Text Field | 56dp | 44dp dense |
| List item (1-line) | 56dp | - |
| List item (2-line) | 72dp | - |
| List item (3-line) | 88dp | - |
| Navigation Bar | 80dp | - |
| Top App Bar | 64dp | - |

### Corner Radius Scale

| Token | Value | Components |
|-------|-------|------------|
| None | 0dp | Rectangles |
| Extra-small | 4dp | Text fields, menus |
| Small | 8dp | Chips, buttons |
| Medium | 12dp | Cards |
| Large | 16dp | FAB |
| Extra-large | 28dp | Bottom sheets |
| Full | 50% | Pills, avatars |

---

## Part 12: Success Criteria

### Phase 1: Foundation Setup

- [ ] Roboto fonts downloaded and registered
- [ ] Material Symbols icons working
- [ ] MDC initializes without errors
- [ ] `ThreadSafeViewModelBase` compiles
- [ ] `ThreadSafeDialogService` registered in DI
- [ ] App launches on Android emulator

### Phase 2: Demo Pages Created

- [ ] ComponentsPage_Typography shows all 15 roles
- [ ] ComponentsPage_Buttons shows 5 variants + icons
- [ ] ComponentsPage_Cards shows 3 types (MDC + Frame)
- [ ] ComponentsPage_Inputs shows TextField + selection controls
- [ ] ComponentsPage_Lists shows list patterns + swipe
- [ ] ComponentsPage_Feedback shows FAB + Snackbar + Progress
- [ ] Navigation between pages works

### Phase 3: Quality Validation

- [ ] No frame skips during scrolling
- [ ] No threading errors in debug output
- [ ] Typography renders with Roboto
- [ ] Color theme applies to all components
- [ ] Icons render correctly
- [ ] Touch targets meet 48dp minimum
- [ ] Snackbar displays on button click

---

## Part 13: Troubleshooting

### Roboto Fonts Not Loading

1. Verify files in `Resources/Fonts/`
2. Check Build Action: `MauiFont`
3. Verify registration in `ConfigureFonts`:
```csharp
fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
```
4. Clean and rebuild solution

### Material Symbols Not Rendering

1. Verify package installed: `UraniumUI.Icons.MaterialSymbols`
2. Check font registration: `fonts.AddMaterialSymbolsFonts();`
3. Verify XAML namespace: `xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols..."`
4. Use correct FontFamily: `MaterialOutlined`

### MDC Controls Not Rendering

1. Verify `MaterialDesignControls.InitializeComponents()` in App.xaml.cs
2. Check namespace: `HorusStudio.Maui.MaterialDesignControls`
3. Ensure package version 10.0.0
4. Register MDC AFTER UraniumUI in MauiProgram.cs

### Theme Colors Not Applied to MDC

1. Check `ConfigureThemesFromResources()` finds MaterialColors.xaml
2. Fallback: Use programmatic configuration:
```csharp
options.ConfigureThemes(themes =>
{
    themes.Primary = Color.FromArgb("#FFB2BE");
    // ... other colors
});
```

### StyleClass Not Working

1. Ensure using UraniumUI page: `<uranium:UraniumContentPage>`
2. Check ResourceDictionary merge order in App.xaml
3. Verify UraniumUI.Material StyleResource is included

### Frame Skips / Performance Issues

1. Move heavy work to `Task.Run()`
2. Use `RunOnUiThread()` for collection updates
3. Check for large images without caching
4. Profile with Visual Studio diagnostics

### Snackbar Not Showing

1. Verify `Handler?.MauiContext` is not null
2. Check service registration in DI
3. Ensure calling from UI thread
4. Try explicit service resolution:
```csharp
var snackbar = Application.Current?.Handler?.MauiContext?.Services.GetService<IMaterialSnackbar>();
```