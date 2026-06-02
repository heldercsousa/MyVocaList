# Design — About Page

## Architecture

A single read-only Shell page with a lightweight ViewModel. No repository, no database access. All data comes from two static sources: MAUI built-ins (`AppInfo`) and the planned `IWhatsNewService` (sourced from `releases.json`).

**Layers affected:** MAUI (UI only) + minimal constant addition to Contracts.

**No prerequisite on What's New.** About ships with a stub implementation of `IWhatsNewService` that always returns `null`. The What's New section is hidden (AC-AB-07). When What's New is implemented later, the stub is replaced with the real service — About page code changes: zero.

---

## Page Structure

```
AboutPage (Shell page, read-only, no scrollbar-required content)
│
├── AppBar
│   ├── Title: "v{AppInfo.VersionString}"         ← AC-AB-01
│   └── Back button (Shell default)
│
└── ScrollView
    └── VerticalStackLayout (padding: page standard)
        │
        ├── [Logo image]                           ← AC-AB-02
        ├── "MyVocaList"  (Title.Large)            ← AC-AB-02
        ├── "Karaoke queue management for..."  
        │    (Body.Medium, muted)                  ← AC-AB-03
        ├── "Since 2025"  (Body.Small, muted)      ← AC-AB-04
        │
        ├── [Divider]
        │
        ├── "License"  (Label.Large, section header)
        │   ├── "CC BY-NC-ND 4.0"  (Body.Medium)  ← AC-AB-05
        │   ├── "Free for personal and non-commercial use. No derivatives."
        │   │    (Body.Small, muted)               ← AC-AB-05
        │   └── "© 2025 Helder Sousa"  (Body.Small)← AC-AB-05
        │
        ├── [Divider]  (hidden if no release entry — AC-AB-07)
        │
        └── "What's New in v{Version}"  (Label.Large, section header)
             (hidden if no release entry — AC-AB-07)
            ├── "{Date}"  (Body.Small, muted)
            ├── Highlights: BindableLayout on VerticalStackLayout
            │   └── "• {highlight}"  (Body.Medium) per item
            └── Fixes (if any): BindableLayout
                └── "• {fix}"  (Body.Small, muted) per item
```

---

## ViewModel — `AboutViewModel`

```csharp
public sealed partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty] private string _version;
    [ObservableProperty] private ReleaseEntry? _currentRelease;

    public bool HasReleaseNotes => CurrentRelease is not null;

    // Exposes the formatted "Since XXXX" string for XAML binding.
    public string Since => $"Since {AppConstants.FoundedYear}";

    public AboutViewModel(IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;
        Version = $"v{AppInfo.VersionString}";
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        CurrentRelease = await _whatsNewService.GetCurrentReleaseAsync(ct);
    }
}
```

**Stub strategy:** `IWhatsNewService` and `ReleaseEntry` are defined here as a minimal interface + DTO, owned by the About page feature for now. The stub implementation (`NullWhatsNewService`) always returns `null` — the What's New section stays hidden. When the What's New feature is implemented, it replaces `NullWhatsNewService` with the real service and adds `GetPendingReleaseAsync` to the interface. About page is untouched.

**Note on `GetCurrentReleaseAsync`:** Returns the `ReleaseEntry` for the current app version unconditionally (no seen-check). The About page always shows the notes regardless of modal dismissal history.

---

## Interface & Stub — `IWhatsNewService`

**New interface** in `MyVocaList.Domain/ServicesInterfaces/`:

```csharp
public interface IWhatsNewService
{
    /// Returns the ReleaseEntry for the current app version, or null if no entry exists.
    Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default);
}
```

**Stub** registered for About page (in `MauiProgram.cs`):

```csharp
// Temporary stub — replaced when What's New feature is implemented
builder.Services.AddSingleton<IWhatsNewService, NullWhatsNewService>();
```

```csharp
internal sealed class NullWhatsNewService : IWhatsNewService
{
    public Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
        => Task.FromResult<ReleaseEntry?>(null);
}
```

**`ReleaseEntry` DTO** (new, in `MyVocaList.Contracts/DTOs/`):

```csharp
public record ReleaseEntry(
    string Version,
    string Date,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Fixes);
```

When What's New is implemented, it: adds `GetPendingReleaseAsync` to the interface, replaces `NullWhatsNewService` registration with the real `WhatsNewService`, and owns the JSON loading logic. `GetCurrentReleaseAsync` moves to the real service at that point.

---

## Constants

Add to `MyVocaList.Contracts` (new file or existing `AppConstants.cs`):

```csharp
public static partial class AppConstants
{
    public const int FoundedYear = 2025;
}
```

The ViewModel exposes `Since` as a computed string property (`$"Since {AppConstants.FoundedYear}"`). XAML binds to `{Binding Since}` — no x:Static formatting gymnastics required.

---

## Navigation

**Route constant:** `Routes.About = "about"` (add to `Routes.cs`)

**Menu entry** (add to `NavigationConfig.cs`, System group, before Exit):
```csharp
new MenuItemViewModel("About", Routes.About, "info_outline")
```

**Shell registration** (`AppShell.xaml`):
```xml
<ShellContent Route="about" ContentTemplate="{DataTemplate pages:AboutPage}" />
```

---

## DI Registration (`MauiProgram.cs`)

```csharp
builder.Services.AddTransient<AboutPage>();
builder.Services.AddTransient<AboutViewModel>();
```

---

## Files to Create / Modify

| Action | File |
|---|---|
| Create | `MyVocaList/UI/Pages/About/AboutPage.xaml` |
| Create | `MyVocaList/UI/Pages/About/AboutPage.xaml.cs` |
| Create | `MyVocaList/UI/ViewModels/AboutViewModel.cs` |
| Modify | `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs` — add `GetCurrentReleaseAsync` |
| Modify | `MyVocaList.Services/WhatsNewService.cs` — implement `GetCurrentReleaseAsync` |
| Modify | `MyVocaList.Contracts/AppConstants.cs` (or create) — add `FoundedYear = 2025` |
| Modify | `MyVocaList/Navigation/Routes.cs` — add `About` route |
| Modify | `MyVocaList/Navigation/NavigationConfig.cs` — add System menu entry |
| Modify | `MyVocaList/AppShell.xaml` — register route |
| Modify | `MyVocaList/MauiProgram.cs` — DI registration |
| Modify | `MyVocaList.Tests` — unit tests for `GetCurrentReleaseAsync` (new service method) |

---

## Invariants & Postconditions

- The page never makes a network call.
- `AboutViewModel` is stateless after `InitializeAsync` — it holds no mutable user data.
- `HasReleaseNotes` is derived from `CurrentRelease`; no separate boolean field.
- `GetCurrentReleaseAsync` must never throw — return `null` on any parse error (consistent with existing `GetPendingReleaseAsync` error contract).
- The page has `SafeAreaEdges="Container"` (MAUI 10 constitutional constraint).

---

## Key Decisions

| Decision | Rationale |
|---|---|
| New `GetCurrentReleaseAsync` method on existing service | Avoids leaking seen-check logic into the About page; single responsibility per method |
| `ReleaseEntry?` nullable on ViewModel | Drives conditional XAML visibility without a separate flag — clean binding |
| Constants in `MyVocaList.Contracts` | Contracts is the right layer for values shared across layers (Domain, Services, UI) |
| `AddTransient` for page + VM | Consistent with all other pages/VMs in the project; no shared state needed |
