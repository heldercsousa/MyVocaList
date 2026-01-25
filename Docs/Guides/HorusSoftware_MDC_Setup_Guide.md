# HorusSoftware MaterialDesignControls - Setup Guide

## Package Information

| Property | Value |
|----------|-------|
| Package Name | `HorusStudio.Maui.MaterialDesignControls` |
| Latest Version | 10.0.0 (December 19, 2025) |
| License | MIT (FREE) |
| Platforms | Android, iOS, macOS (Windows upcoming) |
| Repository | https://github.com/HorusSoftwareUY/Maui.MaterialDesignControls |
| NuGet | https://www.nuget.org/packages/HorusStudio.Maui.MaterialDesignControls |

## Step 1: Install NuGet Package

```bash
dotnet add package HorusStudio.Maui.MaterialDesignControls --version 10.0.0
```

Or add to your `.csproj`:

```xml
<PackageReference Include="HorusStudio.Maui.MaterialDesignControls" Version="10.0.0" />
```

## Step 2: Update MauiProgram.cs

**IMPORTANT**: Order matters. Register HorusSoftware MDC AFTER UraniumUI to avoid conflicts.

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
            
            // UraniumUI - Register FIRST (base MD3 framework)
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            
            // HorusSoftware MDC - Register AFTER UraniumUI
            .UseMaterialDesignControls(ConfigureMaterialDesignControls)
            
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureMaterialDesignControls(MaterialDesignControlsBuilder options)
    {
        // Enable debug logging during development
#if DEBUG
        options.EnableDebug();
#endif

        // Exception handling - route to Serilog instead of Debug.WriteLine
        options.OnException((sender, exception) =>
        {
            // TODO: Replace with Serilog when configured
            System.Diagnostics.Debug.WriteLine($"[MDC ERROR] {sender}: {exception}");
        });

        // Configure themes from ResourceDictionaries (recommended for MyVocaList)
        // This will read from MaterialColors.xaml using your existing color definitions
        options.ConfigureThemesFromResources();
        
        // Alternative: Configure themes programmatically
        // options.ConfigureThemes(
        //     lightTheme: CreateLightTheme(),
        //     darkTheme: CreateDarkTheme()
        // );

        // Date/Time formats: by default, uses US formats. Must be updated to use App´s language user settings
        options.ConfigureStringFormat(new MaterialFormatOptions
        {
            DateFormat = "MM/dd/yyyy",
            TimeFormat = "HH:mm"
        });
    }

    // Optional: Programmatic theme configuration
    // Use this if ConfigureThemesFromResources() doesn't pick up your colors
    private static MaterialTheme CreateDarkTheme()
    {
        return new MaterialTheme
        {
            // Primary (Pink)
            Primary = Color.FromArgb("#FFFFB2BE"),
            OnPrimary = Color.FromArgb("#FF660025"),
            PrimaryContainer = Color.FromArgb("#FF900038"),
            OnPrimaryContainer = Color.FromArgb("#FFFFD9DE"),
            
            // Secondary (Purple)
            Secondary = Color.FromArgb("#FFE2B5FF"),
            OnSecondary = Color.FromArgb("#FF4D007A"),
            SecondaryContainer = Color.FromArgb("#FF662592"),
            OnSecondaryContainer = Color.FromArgb("#FFF3DAFF"),
            
            // Tertiary (Gold)
            Tertiary = Color.FromArgb("#FFE9C400"),
            OnTertiary = Color.FromArgb("#FF3A3000"),
            TertiaryContainer = Color.FromArgb("#FF544600"),
            OnTertiaryContainer = Color.FromArgb("#FFFFE16D"),
            
            // Error
            Error = Color.FromArgb("#FFFFB4A9"),
            OnError = Color.FromArgb("#FF690002"),
            ErrorContainer = Color.FromArgb("#FF930005"),
            OnErrorContainer = Color.FromArgb("#FFFFDAD5"),
            
            // Surface & Background
            Background = Color.FromArgb("#FF1C1621"),
            OnBackground = Color.FromArgb("#FFE6DDEA"),
            Surface = Color.FromArgb("#FF1C1621"),
            OnSurface = Color.FromArgb("#FFE6DDEA"),
            SurfaceVariant = Color.FromArgb("#FF4E3F53"),
            OnSurfaceVariant = Color.FromArgb("#FFD1BFD6"),
            
            // Outline
            Outline = Color.FromArgb("#FF9A899F"),
            OutlineVariant = Color.FromArgb("#FF4E3F53"),
            
            // Inverse
            InverseSurface = Color.FromArgb("#FFE6DDEA"),
            InverseOnSurface = Color.FromArgb("#FF312A36"),
            InversePrimary = Color.FromArgb("#FFBC004B"),
            
            // Scrim
            Scrim = Color.FromArgb("#FF000000")
        };
    }
}
```

## Step 3: Update App.xaml.cs

**CRITICAL**: Initialize MDC components AFTER `InitializeComponent()`.

```csharp
using HorusStudio.Maui.MaterialDesignControls;

namespace MyVocaList;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // IMPORTANT: Initialize HorusSoftware MDC after InitializeComponent
        MaterialDesignControls.InitializeComponents();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
```

## Step 4: XAML Namespace Setup

Add both namespaces to your XAML pages:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<uranium:UraniumContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui"
    xmlns:mdc="clr-namespace:HorusStudio.Maui.MaterialDesignControls;assembly=HorusStudio.Maui.MaterialDesignControls"
    x:Class="MyVocaList.UI.Pages.SomePage"
    Title="Page Title">

    <!-- Use UraniumUI controls with 'uranium:' prefix -->
    <!-- Use HorusSoftware MDC controls with 'mdc:' prefix -->

</uranium:UraniumContentPage>
```

## Available Controls

### HorusSoftware MDC Controls (use these)

| Control | Purpose | MyVocaList Use Case |
|---------|---------|---------------------|
| `mdc:MaterialFloatingButton` | FAB | Quick "Add Song" action |
| `mdc:MaterialCard` | MD3 Cards | Song items in queue |
| `mdc:MaterialSnackbar` | Toast messages | "Song added" feedback |
| `mdc:MaterialChip` | Filter chips | Genre/artist filters |
| `mdc:MaterialProgressIndicator` | Loading states | Song loading |
| `mdc:MaterialSegmentedButton` | Toggle groups | View mode selection |
| `mdc:MaterialTopAppBar` | App bar | Page headers |
| `mdc:MaterialNavigationDrawer` | Side drawer | Main navigation |
| `mdc:MaterialDivider` | Visual separator | List dividers |
| `mdc:MaterialLabel` | Typography | MD3 text styles |
| `mdc:MaterialIconButton` | Icon-only buttons | Actions |

### UraniumUI Controls (continue using)

| Control | Purpose |
|---------|---------|
| `TextField` | Text input with validation |
| `EditorField` | Multi-line text with validation |
| `PickerField` | Dropdown selection |
| `DatePickerField` | Date selection |
| `TimePickerField` | Time selection |
| `CheckBox` | Checkbox with validation |
| `RadioButton` | Radio button groups |
| `DataGrid` | Data tables |
| `TreeView` | Hierarchical lists |
| `TabView` | Tab navigation |
| `ExpanderView` | Collapsible sections |
| `BottomSheetView` | Bottom sheets |
| `Dropdown` | Dropdown menus |

## Component Usage Examples

### MaterialFloatingButton (FAB)

```xml
<!-- Standard FAB -->
<mdc:MaterialFloatingButton 
    Icon="add.png"
    Command="{Binding AddSongCommand}" />

<!-- Extended FAB with text -->
<mdc:MaterialFloatingButton 
    Icon="add.png"
    Text="Add Song"
    Type="Extended"
    Command="{Binding AddSongCommand}" />

<!-- Small FAB -->
<mdc:MaterialFloatingButton 
    Icon="add.png"
    Type="Small"
    Command="{Binding AddSongCommand}" />
```

### MaterialCard

```xml
<mdc:MaterialCard Type="Elevated">
    <VerticalStackLayout>
        <Label Text="Song Title" StyleClass="Title.Medium" />
        <Label Text="Artist Name" StyleClass="Body.Medium" />
    </VerticalStackLayout>
</mdc:MaterialCard>

<mdc:MaterialCard Type="Outlined">
    <!-- Content -->
</mdc:MaterialCard>

<mdc:MaterialCard Type="Filled">
    <!-- Content -->
</mdc:MaterialCard>
```

### MaterialSnackbar (from code-behind)

```csharp
// Inject IMaterialSnackbar via DI or resolve from services
public partial class QueuePage : UraniumContentPage
{
    private readonly IMaterialSnackbar _snackbar;

    public QueuePage(IMaterialSnackbar snackbar)
    {
        InitializeComponent();
        _snackbar = snackbar;
    }

    private async Task OnSongAdded()
    {
        await _snackbar.ShowAsync("Song added to queue", "UNDO", async () =>
        {
            // Undo action
            await UndoAddSong();
        });
    }
}
```

### MaterialChip

```xml
<HorizontalStackLayout>
    <mdc:MaterialChip Text="Rock" Type="Filter" IsSelected="{Binding IsRockSelected}" />
    <mdc:MaterialChip Text="Pop" Type="Filter" IsSelected="{Binding IsPopSelected}" />
    <mdc:MaterialChip Text="Jazz" Type="Filter" IsSelected="{Binding IsJazzSelected}" />
</HorizontalStackLayout>
```

### MaterialProgressIndicator

```xml
<!-- Circular indeterminate -->
<mdc:MaterialProgressIndicator Type="Circular" IsIndeterminate="True" />

<!-- Linear determinate -->
<mdc:MaterialProgressIndicator Type="Linear" Progress="{Binding LoadingProgress}" />
```

## DI Registration for Snackbar

Add to `MauiProgram.cs`:

```csharp
// After builder.Build() but before return
var app = builder.Build();

// Or register in services before build
builder.Services.AddSingleton<IMaterialSnackbar>(sp => 
    new MaterialSnackbar());

return builder.Build();
```

## Library Comparison: When to Use Which

| Feature | UraniumUI | HorusSoftware MDC | Recommendation |
|---------|-----------|-------------------|----------------|
| FAB | No | Yes | **MDC** |
| Chips | No | Yes | **MDC** |
| Segmented Buttons | No | Yes | **MDC** |
| Progress Indicators | Basic | Full MD3 | **MDC** |
| Snackbar | Via Toolkit | Native MD3 | **MDC** |
| TextField + Validation | Yes | Yes | **UraniumUI** |
| DataGrid | Yes | No | **UraniumUI** |
| TreeView | Yes | No | **UraniumUI** |
| BottomSheet | Yes | Coming Soon | **UraniumUI** |
| Dialogs | Yes | Coming Soon | **UraniumUI** |

## Troubleshooting

### Issue: Controls not rendering

1. Verify `MaterialDesignControls.InitializeComponents()` is called in `App.xaml.cs`
2. Check namespace is correct: `HorusStudio.Maui.MaterialDesignControls`
3. Ensure package version 10.0.0 is installed

### Issue: Theme colors not applied

1. Check if `ConfigureThemesFromResources()` can find your ResourceDictionary
2. Verify color key names match MDC expected names
3. Use programmatic `ConfigureThemes()` as fallback

### Issue: Conflicts with UraniumUI

1. Register HorusSoftware MDC AFTER UraniumUI in MauiProgram.cs
2. Use different XAML prefixes (`uranium:` vs `mdc:`)
3. Don't mix same control types from both libraries

## Next Steps

1. Install the NuGet package
2. Update MauiProgram.cs with the configuration
3. Update App.xaml.cs with initialization
4. Test with a simple MaterialFloatingButton on DesignSystemPage
