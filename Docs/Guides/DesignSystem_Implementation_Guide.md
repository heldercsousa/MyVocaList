# MyVocaList Design System Implementation Guide

> **Purpose**: Complete implementation reference for building MD3-compliant UI
> **Audience**: Claude Code executing design system tasks
> **Related**: `MD3_Reference.md` for pure specifications

---

## Part 1: Library Setup

### Package Installation

```bash
# UraniumUI (already installed)
dotnet add package UraniumUI.Material --version 2.14.0

# HorusSoftware MaterialDesignControls (NEW)
dotnet add package HorusStudio.Maui.MaterialDesignControls --version 10.0.0
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
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

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

### XAML Namespaces

```xml
xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui"
xmlns:mdc="clr-namespace:HorusStudio.Maui.MaterialDesignControls;assembly=HorusStudio.Maui.MaterialDesignControls"
```

---

## Part 2: Thread-Safe Infrastructure

### ThreadSafeViewModelBase

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

### Thread-Safe Collection Updates

```csharp
// Pattern for updating ObservableCollection from background work
private ObservableCollection<ItemViewModel> _items = new();
public ObservableCollection<ItemViewModel> Items => _items;

private async Task LoadDataAsync()
{
    // Heavy work on background
    var data = await Task.Run(() => _repository.GetAll());
    
    // Marshal to UI thread
    RunOnUiThread(() =>
    {
        _items.Clear();
        foreach (var item in data)
            _items.Add(new ItemViewModel(item));
    });
}
```

---

## Part 3: Component Library Mapping

### Which Library for What

| Component | Library | Usage |
|-----------|---------|-------|
| Buttons | UraniumUI | `StyleClass="FilledButton"` |
| Typography | UraniumUI | `StyleClass="Headline.Large"` |
| TextField + Validation | UraniumUI | `<uranium:TextField>` |
| Cards | MDC | `<mdc:MaterialCard>` |
| FAB | MDC | `<mdc:MaterialFloatingButton>` |
| Snackbar | MDC | `IMaterialSnackbar` |
| Chips | MDC | `<mdc:MaterialChip>` |
| Progress | MDC | `<mdc:MaterialProgressIndicator>` |
| Divider | MDC | `<mdc:MaterialDivider>` |
| Lists | MAUI | `CollectionView` + styling |
| Swipe Actions | MAUI | `SwipeView` |
| Dialogs | Service | `IThreadSafeDialogService` |
| Colors | Shared | `MaterialColors.xaml` |

---

## Part 4: Priority Tiers

### TIER 1: Critical (Must Implement)

| Component | Status | Demo Page Section |
|-----------|--------|-------------------|
| Color System | EXISTS | Colors showcase |
| Typography (15 roles) | EXISTS | Typography showcase |
| Buttons (5 variants) | EXISTS | Buttons showcase |
| Cards (3 types) | NEEDS MDC | Cards showcase |
| FAB (3 sizes) | NEEDS MDC | FAB showcase |
| TextField | EXISTS | Inputs showcase |
| Snackbar | NEEDS MDC | Feedback showcase |

### TIER 2: Important (Should Implement)

| Component | Status | Demo Page Section |
|-----------|--------|-------------------|
| Dialogs | NEEDS SERVICE | Feedback showcase |
| SwipeView | NATIVE | Interactions showcase |

### TIER 3: Nice-to-Have (May Skip)

| Component | Notes |
|-----------|-------|
| Chips | Only if genre filters needed |
| Bottom Sheets | May use dialogs instead |
| Progress Indicators | Only if slow operations |
| Segmented Buttons | Only if view modes needed |

### NOT IMPLEMENTING

- Custom FAB (MDC provides)
- Custom Progress (MDC provides)
- Motion system with spring physics
- Shape morphing animations
- Container transform transitions
- Carousel
- Complex adaptive layouts

---

## Part 5: Demo Pages Specification

### Page Structure

```
UI/Pages/DesignSystem/
├── DesignSystemPage.xaml          # Main hub (EXISTS - update)
├── HomePage.xaml                   # Welcome (EXISTS - update)
├── ComponentsPage_Buttons.xaml     # NEW
├── ComponentsPage_Cards.xaml       # NEW
├── ComponentsPage_Inputs.xaml      # NEW
├── ComponentsPage_Feedback.xaml    # NEW
└── PlainMauiPage.xaml             # Performance comparison (EXISTS)
```

### Task List for Claude Code

#### Task 1: Update MauiProgram.cs
- Add HorusSoftware MDC registration
- Configure exception handling
- Configure theme from resources

#### Task 2: Update App.xaml.cs
- Add `MaterialDesignControls.InitializeComponents()`

#### Task 3: Create ThreadSafeViewModelBase
- Location: `UI/ViewModels/ThreadSafeViewModelBase.cs`
- Use code from Part 2

#### Task 4: Create ThreadSafeDialogService
- Location: `UI/Services/ThreadSafeDialogService.cs`
- Use code from Part 2
- Register in DI

#### Task 5: Create ComponentsPage_Buttons.xaml
Show all button variants:
```xml
<!-- UraniumUI Buttons -->
<Button Text="Filled" StyleClass="FilledButton" />
<Button Text="Filled Tonal" StyleClass="FilledTonalButton" />
<Button Text="Outlined" StyleClass="OutlinedButton" />
<Button Text="Text" StyleClass="TextButton" />
<Button Text="Elevated" StyleClass="ElevatedButton" />

<!-- MDC IconButton -->
<mdc:MaterialIconButton Icon="star.png" />
```

#### Task 6: Create ComponentsPage_Cards.xaml
Show all card types:
```xml
<mdc:MaterialCard Type="Elevated">
    <Label Text="Elevated Card" StyleClass="Body.Medium" />
</mdc:MaterialCard>

<mdc:MaterialCard Type="Filled">
    <Label Text="Filled Card" StyleClass="Body.Medium" />
</mdc:MaterialCard>

<mdc:MaterialCard Type="Outlined">
    <Label Text="Outlined Card" StyleClass="Body.Medium" />
</mdc:MaterialCard>
```

#### Task 7: Create ComponentsPage_Inputs.xaml
Show input components:
```xml
<!-- UraniumUI TextField -->
<uranium:TextField Title="Standard Input" />
<uranium:TextField Title="With Validation" IsRequired="True" />

<!-- MDC Pickers -->
<mdc:MaterialDatePicker Title="Date" />
<mdc:MaterialTimePicker Title="Time" />
```

#### Task 8: Create ComponentsPage_Feedback.xaml
Show feedback components:
```xml
<!-- FAB variations -->
<mdc:MaterialFloatingButton Icon="add.png" Type="Small" />
<mdc:MaterialFloatingButton Icon="add.png" />
<mdc:MaterialFloatingButton Icon="add.png" Text="Add Song" Type="Extended" />

<!-- Snackbar trigger button -->
<Button Text="Show Snackbar" Clicked="OnShowSnackbar" StyleClass="FilledButton" />

<!-- Progress indicators -->
<mdc:MaterialProgressIndicator Type="Circular" IsIndeterminate="True" />
<mdc:MaterialProgressIndicator Type="Linear" Progress="0.5" />
```

Code-behind for Snackbar:
```csharp
private async void OnShowSnackbar(object sender, EventArgs e)
{
    var snackbar = Handler.MauiContext.Services.GetService<IMaterialSnackbar>();
    await snackbar.ShowAsync("Message sent", "UNDO", () => { /* undo action */ });
}
```

#### Task 9: Update DesignSystemPage.xaml
Convert to navigation hub:
```xml
<VerticalStackLayout Style="{StaticResource PageContainer}">
    <Label Text="Design System" StyleClass="Headline.Large" />
    
    <Button Text="Buttons" Clicked="NavigateToButtons" StyleClass="FilledTonalButton" />
    <Button Text="Cards" Clicked="NavigateToCards" StyleClass="FilledTonalButton" />
    <Button Text="Inputs" Clicked="NavigateToInputs" StyleClass="FilledTonalButton" />
    <Button Text="Feedback" Clicked="NavigateToFeedback" StyleClass="FilledTonalButton" />
</VerticalStackLayout>
```

#### Task 10: Register Routes
In AppShell.xaml.cs:
```csharp
Routing.RegisterRoute("ComponentsPage_Buttons", typeof(ComponentsPage_Buttons));
Routing.RegisterRoute("ComponentsPage_Cards", typeof(ComponentsPage_Cards));
Routing.RegisterRoute("ComponentsPage_Inputs", typeof(ComponentsPage_Inputs));
Routing.RegisterRoute("ComponentsPage_Feedback", typeof(ComponentsPage_Feedback));
```

---

## Part 6: XAML Patterns Reference

### Page Template (UraniumUI)
```xml
<?xml version="1.0" encoding="utf-8" ?>
<uranium:UraniumContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:uranium="http://schemas.enisn-projects.io/dotnet/maui/uraniumui"
    xmlns:mdc="clr-namespace:HorusStudio.Maui.MaterialDesignControls;assembly=HorusStudio.Maui.MaterialDesignControls"
    x:Class="MyVocaList.UI.Pages.DesignSystem.ComponentsPage_Example"
    Title="Page Title">

    <ScrollView>
        <VerticalStackLayout Style="{StaticResource PageContainer}">
            <!-- Content -->
        </VerticalStackLayout>
    </ScrollView>
</uranium:UraniumContentPage>
```

### Section Container Pattern
```xml
<VerticalStackLayout Style="{StaticResource SectionContainer}">
    <Label Text="Section Title" StyleClass="Title.Medium" />
    <BoxView HeightRequest="1" Color="{StaticResource Outline}" />
    <!-- Section content -->
</VerticalStackLayout>
```

### Card with Content Pattern
```xml
<mdc:MaterialCard Type="Elevated">
    <VerticalStackLayout>
        <Label Text="Card Title" StyleClass="Title.Medium" />
        <Label Text="Card description text" StyleClass="Body.Medium" />
    </VerticalStackLayout>
</mdc:MaterialCard>
```

---

## Part 7: Success Criteria

### Phase 1 Complete When:
- [ ] HorusSoftware MDC initializes without errors
- [ ] `ThreadSafeViewModelBase` exists and compiles
- [ ] `ThreadSafeDialogService` exists and compiles
- [ ] App launches on Android emulator

### Phase 2 Complete When:
- [ ] All 4 component demo pages created
- [ ] Navigation between pages works
- [ ] FAB renders correctly
- [ ] Cards render with correct styling
- [ ] Snackbar displays on button click

### Phase 3 Complete When:
- [ ] No frame skips during scrolling
- [ ] No threading errors in debug output
- [ ] All typography styles render correctly
- [ ] Color theme applies to all components

---

## Part 8: Troubleshooting

### MDC Controls Not Rendering
1. Verify `MaterialDesignControls.InitializeComponents()` in App.xaml.cs
2. Check namespace: `HorusStudio.Maui.MaterialDesignControls`
3. Ensure package version 10.0.0

### Theme Colors Not Applied to MDC
1. Check `ConfigureThemesFromResources()` finds MaterialColors.xaml
2. Fallback: Use programmatic `ConfigureThemes()` with explicit colors

### Conflicts Between Libraries
1. Register MDC AFTER UraniumUI in MauiProgram.cs
2. Use distinct prefixes: `uranium:` vs `mdc:`
3. Don't mix same component from both libraries

### Frame Skips
1. Check for synchronous operations on UI thread
2. Move heavy work to `Task.Run()`
3. Use `RunOnUiThread()` for collection updates
4. Check for large images without caching
