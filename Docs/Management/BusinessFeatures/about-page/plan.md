# About Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a read-only About page to the flyout System menu that displays app version, branding, license, and "Since 2025" — with a hidden What's New section wired to a null stub pending the real What's New feature.

**Architecture:** New Shell page (`AboutPage`) with a lightweight `AboutViewModel`. A `NullWhatsNewService` stub (always returns null) satisfies `IWhatsNewService` so the What's New section stays hidden. When the real What's New feature ships, only `MauiProgram.cs` changes (swap the registration) — the page and VM are untouched.

**Tech Stack:** .NET MAUI 10 · CommunityToolkit.Mvvm · DevExpress MAUI v25.2.4 · C# 13

---

## File Map

| Action | File | Responsibility |
|---|---|---|
| Create | `MyVocaList.Contracts/DTOs/ReleaseEntry.cs` | DTO owned by Contracts; consumed by About and later by What's New |
| Create | `MyVocaList.Contracts/AppConstants.cs` | `FoundedYear = 2025` constant |
| Create | `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs` | Service contract; one method for now |
| Create | `MyVocaList.Services/NullWhatsNewService.cs` | Stub: always returns null |
| Create | `MyVocaList/UI/ViewModels/AboutViewModel.cs` | Version, Since, HasReleaseNotes, CurrentRelease |
| Create | `MyVocaList/UI/Pages/About/AboutPage.xaml` | Read-only page layout |
| Create | `MyVocaList/UI/Pages/About/AboutPage.xaml.cs` | Code-behind; wires VM via OnAppearing |
| Modify | `MyVocaList/Navigation/Routes.cs` | Add `About = "about"` constant |
| Modify | `MyVocaList/Navigation/NavigationConfig.cs` | Add About entry to System group before Exit |
| Modify | `MyVocaList/AppShell.xaml` | Register `about` route + ShellContent |
| Modify | `MyVocaList/MauiProgram.cs` | Register NullWhatsNewService, AboutViewModel, AboutPage |
| Modify | `MyVocaList/GlobalUsings.cs` | Add `global using MyVocaList.UI.Pages.About;` |
| Modify | `MyVocaList.Tests/Unit/Services/` | Add NullWhatsNewServiceTests |
| Modify | `MyVocaList.sln` | Register all new files in solution folders |

---

## Task 1: ReleaseEntry DTO

**Files:**
- Create: `MyVocaList.Contracts/DTOs/ReleaseEntry.cs`

- [ ] **Create the DTO file**

```csharp
namespace MyVocaList.Contracts.DTOs;

public record ReleaseEntry(
    string Version,
    string Date,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Fixes);
```

- [ ] **Build to verify no errors**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

- [ ] **Commit**

```
git add MyVocaList.Contracts/DTOs/ReleaseEntry.cs
git commit -m "feat(about): add ReleaseEntry DTO"
```

---

## Task 2: AppConstants

**Files:**
- Create: `MyVocaList.Contracts/AppConstants.cs`

- [ ] **Create the constants file**

```csharp
namespace MyVocaList.Contracts;

public static class AppConstants
{
    public const int FoundedYear = 2025;
}
```

- [ ] **Build to verify**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

- [ ] **Commit**

```
git add MyVocaList.Contracts/AppConstants.cs
git commit -m "feat(about): add AppConstants with FoundedYear"
```

---

## Task 3: IWhatsNewService interface

**Files:**
- Create: `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs`

- [ ] **Create the interface**

```csharp
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IWhatsNewService
{
    /// <summary>Returns the release entry for the current app version, or null if no entry exists.</summary>
    Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default);
}
```

> Note: The explicit `using MyVocaList.Contracts.DTOs;` is required here because Domain's GlobalUsings does not import it globally. Do NOT add it to Domain's GlobalUsings — it is too narrow a reference for a global using.

- [ ] **Build to verify**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

- [ ] **Commit**

```
git add MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs
git commit -m "feat(about): add IWhatsNewService interface"
```

---

## Task 4: NullWhatsNewService stub + tests (TDD)

**Files:**
- Create: `MyVocaList.Services/NullWhatsNewService.cs`
- Modify: `MyVocaList.Tests/Unit/Services/NullWhatsNewServiceTests.cs` (create if missing)

- [ ] **Write the failing test first**

Create or open `MyVocaList.Tests/Unit/Services/NullWhatsNewServiceTests.cs`:

```csharp
using MyVocaList.Services;

namespace MyVocaList.Tests.Unit.Services;

public class NullWhatsNewServiceTests
{
    // [AC] AC-AB-07: What's New section hidden when no release entry — stub always returns null
    [Fact]
    public async Task GetCurrentReleaseAsync_AlwaysReturnsNull()
    {
        var sut = new NullWhatsNewService();

        var result = await sut.GetCurrentReleaseAsync();

        Assert.Null(result);
    }

    // [AC] AC-AB-09: No network dependency — stub never throws
    [Fact]
    public async Task GetCurrentReleaseAsync_WithCancellationToken_DoesNotThrow()
    {
        var sut = new NullWhatsNewService();
        using var cts = new CancellationTokenSource();

        var exception = await Record.ExceptionAsync(() => sut.GetCurrentReleaseAsync(cts.Token));

        Assert.Null(exception);
    }
}
```

- [ ] **Run to confirm Red**

```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~NullWhatsNewServiceTests"
```

Expected: build error (`NullWhatsNewService` not found) — confirms test is wired correctly.

- [ ] **Create the stub implementation**

```csharp
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <summary>
/// Temporary stub — always returns null so the What's New section stays hidden.
/// Replace this registration in MauiProgram.cs when the real WhatsNewService is implemented.
/// </summary>
internal sealed class NullWhatsNewService : IWhatsNewService
{
    /// <inheritdoc />
    public Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
        => Task.FromResult<ReleaseEntry?>(null);
}
```

- [ ] **Run to confirm Green**

```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~NullWhatsNewServiceTests"
```

Expected: 2 tests pass.

- [ ] **Commit**

```
git add MyVocaList.Services/NullWhatsNewService.cs
git add MyVocaList.Tests/Unit/Services/NullWhatsNewServiceTests.cs
git commit -m "feat(about): add NullWhatsNewService stub with tests"
```

---

## Task 5: AboutViewModel

**Files:**
- Create: `MyVocaList/UI/ViewModels/AboutViewModel.cs`

> **Testing note:** `AboutViewModel` is Level C risk (trivial property assignment from MAUI built-ins; no business logic). `AppInfo.VersionString` is a MAUI runtime value not available in plain .NET tests without device context. Verified on emulator in Task 9.

- [ ] **Create the ViewModel**

```csharp
namespace MyVocaList.UI.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IWhatsNewService _whatsNewService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReleaseNotes))]
    private ReleaseEntry? _currentRelease;

    public string Version { get; } = $"v{AppInfo.VersionString}";
    public string Since { get; } = $"Since {AppConstants.FoundedYear}";
    public bool HasReleaseNotes => CurrentRelease is not null;

    public AboutViewModel(IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        CurrentRelease = await _whatsNewService.GetCurrentReleaseAsync(ct);
    }
}
```

> `ReleaseEntry` is in `MyVocaList.Contracts.DTOs` — add `global using MyVocaList.Contracts.DTOs;` to `MyVocaList/GlobalUsings.cs` in the next step.

- [ ] **Add global using for Contracts.DTOs to MAUI project**

Open `MyVocaList/GlobalUsings.cs` and add after the existing `MyVocaList.Contracts.DTOs.List` line:

```csharp
global using MyVocaList.Contracts.DTOs;
```

- [ ] **Build to verify**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

- [ ] **Commit**

```
git add MyVocaList/UI/ViewModels/AboutViewModel.cs
git add MyVocaList/GlobalUsings.cs
git commit -m "feat(about): add AboutViewModel"
```

---

## Task 6: Navigation wiring

**Files:**
- Modify: `MyVocaList/Navigation/Routes.cs`
- Modify: `MyVocaList/Navigation/NavigationConfig.cs`
- Modify: `MyVocaList/AppShell.xaml`

- [ ] **Add route constant to Routes.cs**

In `MyVocaList/Navigation/Routes.cs`, add after the `Backup` constant:

```csharp
public const string About = "about";
```

Result:

```csharp
public const string Backup = "backup";
public const string About = "about";      // ← new
public const string Exit = "exit";
```

- [ ] **Add menu entry to NavigationConfig.cs**

In `MyVocaList/Navigation/NavigationConfig.cs`, inside the System `MenuGroup`, insert the About entry **before** the Exit entry:

```csharp
new MenuGroup("System", [
    new MenuItemDescription("Preferences",    "settings_outlined",             Routes.Preferences, navigateCommand),
    new MenuItemDescription("Backup & Restore","cloud_sync_outlined",          Routes.Backup,      navigateCommand),
    new MenuItemDescription("About",          "info_outlined",                 Routes.About,       navigateCommand),   // ← new
    new MenuItemDescription("Exit",           "logout_outlined",               Routes.Exit,        navigateCommand)
])
```

- [ ] **Register the Shell route in AppShell.xaml**

In `MyVocaList/AppShell.xaml`, find where other ShellContent entries are declared and add:

```xml
<ShellContent Route="about" ContentTemplate="{DataTemplate pages:AboutPage}" />
```

> The `pages:AboutPage` binding requires the `MyVocaList.UI.Pages.About` namespace, which is added to GlobalUsings in Task 7.

- [ ] **Build to verify** (AboutPage doesn't exist yet — expect a CS0246 type error for AboutPage; that's correct at this stage, proceed to Task 7)

```
dotnet build MyVocaList.sln
```

If only error is `AboutPage not found` — expected, continue. Any other errors: fix before proceeding.

- [ ] **Commit**

```
git add MyVocaList/Navigation/Routes.cs
git add MyVocaList/Navigation/NavigationConfig.cs
git add MyVocaList/AppShell.xaml
git commit -m "feat(about): add About route and menu entry"
```

---

## Task 7: AboutPage XAML + code-behind + DI

**Files:**
- Create: `MyVocaList/UI/Pages/About/AboutPage.xaml`
- Create: `MyVocaList/UI/Pages/About/AboutPage.xaml.cs`
- Modify: `MyVocaList/MauiProgram.cs`
- Modify: `MyVocaList/GlobalUsings.cs`

- [ ] **Create the page code-behind**

Create `MyVocaList/UI/Pages/About/AboutPage.xaml.cs`:

```csharp
namespace MyVocaList.UI.Pages.About;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AboutViewModel vm)
            await vm.InitializeAsync();
    }
}
```

- [ ] **Create the page XAML**

Create `MyVocaList/UI/Pages/About/AboutPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    x:Class="MyVocaList.UI.Pages.About.AboutPage"
    x:DataType="vm:AboutViewModel"
    Title="{Binding Version}"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="24">

            <!-- Identity -->
            <VerticalStackLayout HorizontalOptions="Center" Spacing="8">
                <Image
                    Source="appicon.png"
                    HeightRequest="80"
                    WidthRequest="80"
                    HorizontalOptions="Center" />
                <Label
                    Text="MyVocaList"
                    StyleClass="Title.Large"
                    HorizontalOptions="Center"
                    TextColor="{StaticResource OnSurface}" />
                <Label
                    Text="Karaoke queue management for live events."
                    StyleClass="Body.Medium"
                    HorizontalOptions="Center"
                    TextColor="{StaticResource OnSurfaceVariant}" />
                <Label
                    Text="{Binding Since}"
                    StyleClass="Body.Small"
                    HorizontalOptions="Center"
                    TextColor="{StaticResource OnSurfaceVariant}" />
            </VerticalStackLayout>

            <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />

            <!-- License -->
            <VerticalStackLayout Spacing="4">
                <Label
                    Text="License"
                    StyleClass="Title.Small"
                    TextColor="{StaticResource OnSurface}" />
                <Label
                    Text="CC BY-NC-ND 4.0"
                    StyleClass="Body.Medium"
                    TextColor="{StaticResource OnSurface}" />
                <Label
                    Text="Free for personal and non-commercial use. No derivatives."
                    StyleClass="Body.Small"
                    TextColor="{StaticResource OnSurfaceVariant}" />
                <Label
                    Text="© 2025 Helder Sousa"
                    StyleClass="Body.Small"
                    TextColor="{StaticResource OnSurfaceVariant}" />
            </VerticalStackLayout>

            <!-- What's New — hidden when HasReleaseNotes is false (stub phase) -->
            <VerticalStackLayout IsVisible="{Binding HasReleaseNotes}" Spacing="8">
                <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />
                <Label
                    Text="{Binding CurrentRelease.Version, StringFormat='What\'s New in v{0}'}"
                    StyleClass="Title.Small"
                    TextColor="{StaticResource OnSurface}" />
                <Label
                    Text="{Binding CurrentRelease.Date}"
                    StyleClass="Body.Small"
                    TextColor="{StaticResource OnSurfaceVariant}" />
                <VerticalStackLayout
                    BindableLayout.ItemsSource="{Binding CurrentRelease.Highlights}"
                    Spacing="4">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <Label
                                Text="{Binding ., StringFormat='• {0}'}"
                                StyleClass="Body.Medium"
                                TextColor="{StaticResource OnSurface}" />
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </VerticalStackLayout>
                <VerticalStackLayout
                    BindableLayout.ItemsSource="{Binding CurrentRelease.Fixes}"
                    Spacing="4">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <Label
                                Text="{Binding ., StringFormat='• {0}'}"
                                StyleClass="Body.Small"
                                TextColor="{StaticResource OnSurfaceVariant}" />
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </VerticalStackLayout>
            </VerticalStackLayout>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

- [ ] **Add global using for AboutPage namespace**

In `MyVocaList/GlobalUsings.cs`, add alongside the other page namespace imports:

```csharp
global using MyVocaList.UI.Pages.About;
```

- [ ] **Register in MauiProgram.cs**

In `MyVocaList/MauiProgram.cs`, find the section where other pages and ViewModels are registered (AddTransient calls) and add:

```csharp
builder.Services.AddTransient<AboutPage>();
builder.Services.AddTransient<AboutViewModel>();
```

Also add the stub service registration. Find where other singleton services are registered and add:

```csharp
// Temporary stub — replace with WhatsNewService when What's New feature is implemented
builder.Services.AddSingleton<IWhatsNewService, NullWhatsNewService>();
```

- [ ] **Build to verify**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors, 0 warnings (besides any pre-existing ones).

- [ ] **Run all tests**

```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```

Expected: all tests pass including the 2 NullWhatsNewService tests.

- [ ] **Commit**

```
git add MyVocaList/UI/Pages/About/AboutPage.xaml
git add MyVocaList/UI/Pages/About/AboutPage.xaml.cs
git add MyVocaList/GlobalUsings.cs
git add MyVocaList/MauiProgram.cs
git commit -m "feat(about): add AboutPage XAML, code-behind, and DI registration"
```

---

## Task 8: Solution file registration

**Files:**
- Modify: `MyVocaList.sln`

> Read `constraints-registry.md § Visual Studio Solution (.sln)` for the exact pattern before editing. New BusinessFeatures solution folder parent GUID is `{8AB01C9F-E0FD-49D5-AE2C-E27AD8C8F05D}`. Use the next available sequential GUID from the last used `{FA1234BC-0001-4000-8000-000000000014}` — check the .sln for the actual last used number before picking.

- [ ] **Add solution folder and file entries**

Add a new solution folder project for `about-page` with entries for all new spec + source files:

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "about-page", "about-page", "{FA1234BC-0001-4000-8000-000000000015}"
	ProjectSection(SolutionItems) = preProject
		Docs\Management\BusinessFeatures\about-page\requirements.md = Docs\Management\BusinessFeatures\about-page\requirements.md
		Docs\Management\BusinessFeatures\about-page\design.md = Docs\Management\BusinessFeatures\about-page\design.md
		Docs\Management\BusinessFeatures\about-page\tasks.md = Docs\Management\BusinessFeatures\about-page\tasks.md
		Docs\Management\BusinessFeatures\about-page\plan.md = Docs\Management\BusinessFeatures\about-page\plan.md
	EndProjectSection
EndProject
```

Add the NestedProjects entry in `GlobalSection(NestedProjects)`:

```
{FA1234BC-0001-4000-8000-000000000015} = {8AB01C9F-E0FD-49D5-AE2C-E27AD8C8F05D}
```

- [ ] **Verify in Visual Studio**

Open the solution in VS. Confirm:
- `about-page` folder appears under BusinessFeatures in Solution Explorer
- All 4 doc files are visible inside it
- No duplicate entries or broken paths

- [ ] **Commit**

```
git add MyVocaList.sln
git commit -m "chore: register about-page spec files in solution"
```

---

## Task 9: E2E verification on emulator

> This is the acceptance gate. Run the app on Android emulator and verify all ACs.

- [ ] **Deploy to emulator and open the flyout**

Verify AC-AB-08: "About" appears in the System group after "Backup & Restore" and before "Exit".

- [ ] **Tap About — verify AppBar**

Verify AC-AB-01: AppBar title shows `v1.0.0` (or current version string).

- [ ] **Verify page body**

- AC-AB-02: App logo (appicon.png) and "MyVocaList" text visible
- AC-AB-03: "Karaoke queue management for live events." visible beneath the title
- AC-AB-04: "Since 2025" visible below the goal sentence
- AC-AB-05: "License" section with "CC BY-NC-ND 4.0", summary, and "© 2025 Helder Sousa"
- AC-AB-07: No "What's New" section visible (stub returns null)

- [ ] **Verify offline (AC-AB-09)**

Enable Airplane Mode on the emulator. Open About page. Confirm all content loads with no error or spinner.

- [ ] **Final commit (if any last-minute fixes)**

```
git add -p
git commit -m "fix(about): [description of any fix found during E2E]"
```

---

## Future handoff note

When the What's New feature is implemented:
1. Replace `builder.Services.AddSingleton<IWhatsNewService, NullWhatsNewService>()` with `builder.Services.AddSingleton<IWhatsNewService, WhatsNewService>()` in `MauiProgram.cs`
2. `WhatsNewService` implements `GetCurrentReleaseAsync` (already in the interface) plus the new `GetPendingReleaseAsync` method
3. `AboutPage` and `AboutViewModel`: no changes required — the What's New section lights up automatically when `GetCurrentReleaseAsync` returns a non-null entry
