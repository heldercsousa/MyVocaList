# UraniumUI styles and icons setup for .NET MAUI 8.0

**UraniumUI does not include built-in typography style classes** like `Headline.Large` — this is the root cause of the styling issue. The `StyleClass` syntax is correct, but those specific styles must be created manually. For icons, the most common problem is missing font registration in `ConfigureFonts()`. Additionally, **UraniumUI v2.13+ dropped .NET 8 support**, so .NET 8 projects must use v2.12.1 or earlier.

## Typography styles must be defined manually

UraniumUI's Material theme provides StyleClasses for buttons (`ElevatedButton`, `FilledButton`, `OutlinedButton`, `TextButton`, `FilledTonalButton`), dividers, and component-specific styles like `TreeView.Label` — but **no typography classes** for Labels. The `StyleClass="Headline.Large"` syntax is valid XAML; dots are permitted in style class names and UraniumUI uses them internally (e.g., `TreeView.Label.Selected`). The problem is simply that these typography styles don't exist in the library.

**The correct approach** requires defining custom styles in your `Resources/Styles/Styles.xaml`:

```xml
<!-- Material Design 3 Typography System -->
<Style TargetType="Label" Class="Headline.Large">
    <Setter Property="FontSize" Value="32" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>
<Style TargetType="Label" Class="Headline.Medium">
    <Setter Property="FontSize" Value="28" />
</Style>
<Style TargetType="Label" Class="Body.Large">
    <Setter Property="FontSize" Value="16" />
</Style>
<Style TargetType="Label" Class="Body.Medium">
    <Setter Property="FontSize" Value="14" />
</Style>
<Style TargetType="Label" Class="Title.Large">
    <Setter Property="FontSize" Value="22" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>
```

Then consume them with `StyleClass`:

```xml
<Label StyleClass="Headline.Large" Text="Page Title" />
<Label StyleClass="Body.Medium" Text="Regular content text" />
```

Note the distinction: use `Class` attribute when **defining** the style on a `<Style>` element, and `StyleClass` when **applying** it to UI elements. Multiple classes can be combined with commas: `StyleClass="Headline.Large, Important"`.

## Icons require explicit font registration

UraniumUI provides four icon packages, with **MaterialSymbols recommended** since MaterialIcons was deprecated in v2.8.0:

| Package | NuGet Name | Extension Method |
|---------|-----------|------------------|
| Material Symbols | `UraniumUI.Icons.MaterialSymbols` | `AddMaterialSymbolsFonts()` |
| Font Awesome | `UraniumUI.Icons.FontAwesome` | `AddFontAwesomeIconFonts()` |
| Segoe Fluent | `UraniumUI.Icons.SegoeFluent` | `AddFluentIconFonts()` |
| Material Icons | `UraniumUI.Icons.MaterialIcons` | `AddMaterialIconFonts()` *(deprecated)* |

The critical missing step for most users is calling the font registration method inside `ConfigureFonts()`:

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseUraniumUI()              // Required
        .UseUraniumUIMaterial()      // Required
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddMaterialSymbolsFonts();  // ← Icons won't work without this
        });
    return builder.Build();
}
```

## XAML syntax for displaying icons

Each icon package requires its own namespace declaration and uses specific FontFamily names:

**Material Symbols namespace and usage:**
```xml
xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"

<Image>
    <Image.Source>
        <FontImageSource 
            FontFamily="MaterialOutlined" 
            Glyph="{x:Static m:MaterialOutlined.Home}" 
            Color="Black" />
    </Image.Source>
</Image>
```

Available MaterialSymbols font families: `MaterialOutlined`, `MaterialRound`, `MaterialSharp`, `MaterialOutlinedFilled`, `MaterialRoundFilled`, `MaterialSharpFilled`.

**Font Awesome namespace and usage:**
```xml
xmlns:fa="clr-namespace:UraniumUI.Icons.FontAwesome;assembly=UraniumUI.Icons.FontAwesome"

<Image>
    <Image.Source>
        <FontImageSource 
            FontFamily="FASolid" 
            Glyph="{x:Static fa:Solid.User}" 
            Color="Orange" />
    </Image.Source>
</Image>
```

Font Awesome families: `FASolid`, `FARegular`.

**In buttons:**
```xml
<Button Text="Save">
    <Button.ImageSource>
        <FontImageSource 
            FontFamily="MaterialOutlined" 
            Glyph="{x:Static m:MaterialOutlined.Save}" 
            Color="White" />
    </Button.ImageSource>
</Button>
```

## Required App.xaml configuration

The Material theme's StyleResource must be added to your merged dictionaries with proper references:

```xml
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:material="http://schemas.enisn-projects.io/dotnet/maui/uraniumui/material"
             x:Class="YourApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary x:Name="appColors" Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary x:Name="appStyles" Source="Resources/Styles/Styles.xaml" />
                
                <material:StyleResource ColorsOverride="{x:Reference appColors}" 
                                        BasedOn="{x:Reference appStyles}" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

If you encounter "Cannot resolve type" errors with the schema URI, use the full CLR namespace instead:
```xml
xmlns:material="clr-namespace:UraniumUI.Material.Controls;assembly=UraniumUI.Material"
```

## Version compatibility is critical for .NET 8

**UraniumUI v2.13.0 dropped .NET 8 support** and now targets .NET 9 only. For .NET MAUI 8.0 projects, install v2.12.1:

```bash
dotnet add package UraniumUI.Material --version 2.12.1
dotnet add package UraniumUI.Icons.MaterialSymbols --version 2.12.1
```

If you experience random breakages after Visual Studio updates, lock your MAUI version in the `.csproj`:
```xml
<PropertyGroup>
    <MauiVersion>8.0.91</MauiVersion>
</PropertyGroup>
```

## Troubleshooting checklist

Before debugging further, verify these common failure points:

- **Icons invisible**: Always specify a `Color` on `FontImageSource` — icons render transparent by default
- **StyleClass ignored in containers**: Replace `Frame` with `Border` when wrapping styled content (Frame overwrites child StyleClass values)
- **Wrong icon glyph class**: Use `MaterialOutlined.Star` not `MaterialRegular.Star` after migrating from deprecated MaterialIcons
- **Extension methods not recognized**: Ensure you've added `using UraniumUI;` at the top of MauiProgram.cs

## UraniumUI resources code source references for further customization:
https://github.com/enisn/UraniumUI/blob/develop/src/UraniumUI.Material/Resources

## Conclusion

The typography issue stems from a misconception — UraniumUI expects you to define your own Label styles using the `Class` attribute on Style elements. The syntax `StyleClass="Headline.Large"` is correct XAML, but those classes simply aren't provided. For icons, the fix is straightforward: call `AddMaterialSymbolsFonts()` (or equivalent) inside `ConfigureFonts()`, declare the proper XAML namespace, and use `FontImageSource` with `{x:Static}` glyph references. Staying on **UraniumUI v2.12.1 or earlier** is essential for .NET 8 compatibility until you're ready to migrate to .NET 9.

