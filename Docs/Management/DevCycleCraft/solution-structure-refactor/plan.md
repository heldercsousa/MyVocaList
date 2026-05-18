# Solution Structure Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Clean up solution structure by moving service interfaces to Domain, deleting unused IDatabaseInit, renaming Snackbar to Component, reorganising MAUI project files, and replacing manual DI registrations with namespace-based auto-scanning.

**Architecture:** Domain owns all contracts (repository + service interfaces). Services project retains only implementations. MAUI project files co-locate ViewModels with their pages. MauiProgram.cs uses inline reflection scanning keyed on namespace/base-type instead of manual per-type registrations.

**Tech Stack:** .NET MAUI 10 · C# 13 · Microsoft.Extensions.DependencyInjection · Reflection

---

## Task 1: Create Domain/ServicesInterfaces folder and move interfaces

**Files:**
- Create: `Domain/ServicesInterfaces/IVenueService.cs`
- Create: `Domain/ServicesInterfaces/IPersonService.cs`
- Create: `Domain/ServicesInterfaces/IQueueService.cs`
- Create: `Domain/ServicesInterfaces/ILanguageService.cs`
- Create: `Domain/ServicesInterfaces/ITextNormalizationService.cs`
- Delete: `Services/IVenueService.cs`
- Delete: `Services/IPersonService.cs`
- Delete: `Services/IQueueService.cs`
- Delete: `Services/ILanguageService.cs`
- Delete: `Services/ITextNormalizationService.cs`

**Step 1: Create IVenueService.cs in Domain/ServicesInterfaces**

```csharp
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces
{
    public interface IVenueService
    {
        (bool isValid, string message) ValidateNameInput(string name);
        Task<(bool success, string message)> CreateVenueAsync(string name);
        Task<(bool success, string message)> UpdateVenueAsync(int id, string newName);
        Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);
        bool ShouldShowCharacterCounter(int currentLength);
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
        Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string query = null);
    }
}
```

**Step 2: Create IPersonService.cs in Domain/ServicesInterfaces**

```csharp
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces
{
    public interface IPersonService
    {
        int MaxInputLength { get; }
        int MaxDatabaseLength { get; }
        int ShowCounterAt { get; }

        (bool isValid, string message) ValidateNameInput(string name);
        (bool isValid, string message) ValidateNameForDatabase(string name);
        (bool isValid, string message) ValidateBirthday(string birthday);
        (bool isValid, string message) ValidateEmail(string email);

        Task<(bool success, string message, Person? person)> CreatePersonAsync(string fullName, string birthday = null, string email = null);
        Task<Person?> GetPersonByIdAsync(int id);
        Task<Person?> GetPersonByNameAsync(string name);
        Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = 5);
        Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3);

        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
        bool ShouldShowCharacterCounter(int currentLength);
    }
}
```

**Step 3: Create IQueueService.cs in Domain/ServicesInterfaces**

```csharp
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces
{
    /// <summary>
    /// Interface for queue and event operations.
    /// Person operations are delegated to IPersonService.
    /// </summary>
    public interface IQueueService
    {
        Task<(bool success, string message, Person? addedDomainPerson)> AddPersonToQueueAsync(
            string fullName, string birthday = null, string email = null);
        Task RecordParticipationAsync(int personId, ParticipationStatus status);

        Task<Event?> GetActiveEventAsync();
        Task SetActiveEventAsync(int eventId);
        Task<IEnumerable<Venue>> GetAllEstablishmentsAsync();
        Task<IEnumerable<Event>> GetAllEventsAsync();
    }
}
```

**Step 4: Create ILanguageService.cs in Domain/ServicesInterfaces**

```csharp
namespace MyVocaList.Domain.ServicesInterfaces
{
    public interface ILanguageService
    {
        Task<string> GetUserLanguageAsync();
        Task SetUserLanguageAsync(string languageCode);
        bool IsLanguageSelected();
    }
}
```

**Step 5: Create ITextNormalizationService.cs in Domain/ServicesInterfaces**

```csharp
namespace MyVocaList.Domain.ServicesInterfaces
{
    /// <summary>Interface for multilingual text normalization.</summary>
    public interface ITextNormalizationService
    {
        /// <summary>Normalizes name by removing accents and special characters.</summary>
        string NormalizeName(string name);

        /// <summary>Detects if text contains Arabic characters (RTL).</summary>
        bool ContainsArabicText(string text);

        /// <summary>Detects if text contains Asian characters (CJK).</summary>
        bool ContainsAsianText(string text);

        /// <summary>Removes special characters keeping letters, numbers and spaces.</summary>
        string SanitizeInput(string input);

        /// <summary>Normalizes search input for optimized search.</summary>
        string NormalizeSearchTerm(string searchTerm);
    }
}
```

**Step 6: Delete the 5 old interface files from Services/**

Delete:
- `Services/IVenueService.cs`
- `Services/IPersonService.cs`
- `Services/IQueueService.cs`
- `Services/ILanguageService.cs`
- `Services/ITextNormalizationService.cs`

**Step 7: Update namespace in all 5 service implementations**

Add `using MyVocaList.Domain.ServicesInterfaces;` to each implementation file:
- `Services/VenueService.cs`
- `Services/PersonService.cs`
- `Services/QueueService.cs`
- `Services/LanguageService.cs`
- `Services/TextNormalizationService.cs`

**Step 8: Update GlobalUsings.cs in MAUI project**

In `MyVocaList/GlobalUsings.cs`, add:
```csharp
global using MyVocaList.Domain.ServicesInterfaces;
```

**Step 9: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

**Step 10: Commit**

```bash
git add Domain/ServicesInterfaces/ Services/ MyVocaList/GlobalUsings.cs
git commit -m "refactor(domain): move service interfaces to Domain/ServicesInterfaces"
```

---

## Task 2: Delete IDatabaseInit and DatabaseInit

**Files:**
- Delete: `Domain/IDatabaseInit.cs`
- Delete: `Infra/DatabaseInit.cs`
- Modify: `MyVocaList/MauiProgram.cs`

**Step 1: Delete both files**

Delete `Domain/IDatabaseInit.cs` and `Infra/DatabaseInit.cs`.

**Step 2: Remove from MauiProgram.cs**

Remove this line:
```csharp
builder.Services.AddScoped<IDatabaseInit, DatabaseInit>();
```

Also remove `using MyVocaList.Domain;` if `IDatabaseInit` was its only usage.

**Step 3: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

**Step 4: Commit**

```bash
git add Domain/IDatabaseInit.cs Infra/DatabaseInit.cs MyVocaList/MauiProgram.cs
git commit -m "refactor: delete unused IDatabaseInit and DatabaseInit"
```

---

## Task 3: Rename Snackbar to SnackbarComponent, move to UI/Components

**Files:**
- Create: `MyVocaList/UI/Components/SnackbarComponent.cs`
- Delete: `MyVocaList/UI/Services/SnackbarService.cs`

**Step 1: Create SnackbarComponent.cs**

```csharp
using CommunityToolkit.Maui.Alerts;

namespace MyVocaList.UI.Components;

/// <summary>Thread-safe snackbar notification component.</summary>
public interface ISnackbarComponent
{
    Task ShowSuccessAsync(string message);
    Task ShowErrorAsync(string message);
}

/// <summary>Snackbar implementation using CommunityToolkit.Maui.</summary>
public class SnackbarComponent : ISnackbarComponent
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    public async Task ShowSuccessAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    public async Task ShowErrorAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    private async Task ShowSnackbarAsync(string message)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
                await DisplaySnackbarAsync(message));
            return;
        }

        await DisplaySnackbarAsync(message);
    }

    private static async Task DisplaySnackbarAsync(string message)
    {
        var snackbar = Snackbar.Make(message, duration: Duration);
        await snackbar.Show();
    }
}
```

**Step 2: Delete old file and folder**

Delete `MyVocaList/UI/Services/SnackbarService.cs`. Delete the now-empty `MyVocaList/UI/Services/` folder.

**Step 3: Update GlobalUsings.cs**

In `MyVocaList/GlobalUsings.cs`:
- Replace: `global using MyVocaList.UI.Services;`
- With: `global using MyVocaList.UI.Components;`

**Step 4: Rename all references**

In `MyVocaList/UI/ViewModels/VenuesViewModel.cs` and `VenueFormViewModel.cs`:
- Replace `ISnackbarService` → `ISnackbarComponent`

In `MyVocaList/MauiProgram.cs`:
- Replace `ISnackbarService, SnackbarService` → `ISnackbarComponent, SnackbarComponent`
  (this line will be removed entirely in Task 6, but fix it now so the build passes)

**Step 5: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

**Step 6: Commit**

```bash
git add MyVocaList/UI/Components/ MyVocaList/UI/Services/ MyVocaList/GlobalUsings.cs MyVocaList/UI/ViewModels/VenuesViewModel.cs MyVocaList/UI/ViewModels/VenueFormViewModel.cs MyVocaList/MauiProgram.cs
git commit -m "refactor(ui): rename SnackbarService to SnackbarComponent, move to UI/Components"
```

---

## Task 4: Move ObservableRangeCollection to UI/Models, delete UI/Collections

**Files:**
- Create: `MyVocaList/UI/Models/ObservableRangeCollection.cs`
- Delete: `MyVocaList/UI/Collections/ObservableRangeCollection.cs`

**Step 1: Create ObservableRangeCollection.cs in UI/Models**

Same content, updated namespace only:

```csharp
using System.Collections.Specialized;

namespace MyVocaList.UI.Models
{
    /// <summary>
    /// Minimal ObservableRangeCollection to perform batch updates with a single Reset notification.
    /// Keeps API small but effective for reducing layout churn.
    /// </summary>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public ObservableRangeCollection() : base() { }
        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection ?? Array.Empty<T>()) { }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            CheckReentrancy();
            bool added = false;
            foreach (var item in items)
            {
                Items.Add(item);
                added = true;
            }
            if (added)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            Items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                    Items.Add(item);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ClearRange()
        {
            if (Items.Count == 0) return;
            CheckReentrancy();
            Items.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
        }
    }
}
```

**Step 2: Delete old file and folder**

Delete `MyVocaList/UI/Collections/ObservableRangeCollection.cs` and the `MyVocaList/UI/Collections/` folder.

**Step 3: Remove stale using in VenuesViewModel.cs**

Remove the line `using MyVocaList.UI.Collections;` — the type is now in `MyVocaList.UI.Models` which is already in GlobalUsings.cs.

**Step 4: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

**Step 5: Commit**

```bash
git add MyVocaList/UI/Models/ObservableRangeCollection.cs MyVocaList/UI/Collections/ MyVocaList/UI/ViewModels/VenuesViewModel.cs
git commit -m "refactor(ui): move ObservableRangeCollection to UI/Models, delete UI/Collections"
```

---

## Task 5: Relocate ViewModels alongside their pages, delete UI/ViewModels

**Files:**
- Create: `MyVocaList/AppShellViewModel.cs`
- Create: `MyVocaList/UI/ViewModelBase.cs`
- Create: `MyVocaList/UI/Pages/Venues/VenuesViewModel.cs`
- Create: `MyVocaList/UI/Pages/Venues/VenueFormViewModel.cs`
- Delete: `MyVocaList/UI/ViewModels/` (all 4 files + folder)

**Step 1: Move AppShellViewModel — update namespace to `MyVocaList`**

Copy full file content to `MyVocaList/AppShellViewModel.cs`, change:
```csharp
namespace MyVocaList.UI.ViewModels;
```
to:
```csharp
namespace MyVocaList;
```

**Step 2: Move ViewModelBase — update namespace to `MyVocaList.UI`**

Copy full file content to `MyVocaList/UI/ViewModelBase.cs`, change:
```csharp
namespace MyVocaList.UI.ViewModels;
```
to:
```csharp
namespace MyVocaList.UI;
```

**Step 3: Move VenuesViewModel — update namespace to `MyVocaList.UI.Pages.Venues`**

Copy full file content to `MyVocaList/UI/Pages/Venues/VenuesViewModel.cs`, change:
```csharp
namespace MyVocaList.UI.ViewModels
```
to:
```csharp
namespace MyVocaList.UI.Pages.Venues
```

**Step 4: Move VenueFormViewModel — update namespace to `MyVocaList.UI.Pages.Venues`**

Copy full file content to `MyVocaList/UI/Pages/Venues/VenueFormViewModel.cs`, change:
```csharp
namespace MyVocaList.UI.ViewModels
```
to:
```csharp
namespace MyVocaList.UI.Pages.Venues
```

**Step 5: Delete the UI/ViewModels folder**

Delete all 4 original `.cs` files and the `MyVocaList/UI/ViewModels/` directory.

**Step 6: Update GlobalUsings.cs**

In `MyVocaList/GlobalUsings.cs`:
- Remove: `global using MyVocaList.UI.ViewModels;`
- Add: `global using MyVocaList.UI;`

`AppShellViewModel` is now in the root `MyVocaList` namespace — no extra using needed.
`VenuesViewModel` and `VenueFormViewModel` are in `MyVocaList.UI.Pages.Venues` — already covered by the existing `global using MyVocaList.UI.Pages.Venues;`.

**Step 7: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors. Fix any residual `using MyVocaList.UI.ViewModels` references if found.

**Step 8: Commit**

```bash
git add MyVocaList/AppShellViewModel.cs MyVocaList/UI/ViewModelBase.cs MyVocaList/UI/Pages/Venues/VenuesViewModel.cs MyVocaList/UI/Pages/Venues/VenueFormViewModel.cs MyVocaList/UI/ViewModels/
git commit -m "refactor(ui): co-locate ViewModels with pages, delete UI/ViewModels folder"
```

---

## Task 6: Replace manual DI registrations with namespace-based auto-scanning

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`

**Step 1: Replace MauiProgram.cs with the following**

```csharp
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace MyVocaList;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressEditors()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
            });

#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        // ── Database ──────────────────────────────────────────────────────────
        builder.Services.AddSingleton<CollationInterceptor>();
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyVocaList.db");
            options.UseSqlite($"Data Source={dbPath}")
                   .AddInterceptors(sp.GetRequiredService<CollationInterceptor>());
        });

        // ── Auto-registration by namespace ────────────────────────────────────
        // Scoped — Repositories  (MyVocaList.Infra.Repository)
        RegisterByNamespace(builder.Services,
            typeof(MyVocaList.Infra.Repository.VenueRepository).Assembly,
            "MyVocaList.Infra.Repository",
            ServiceLifetime.Scoped,
            registerByInterface: true);

        // Scoped — Services  (MyVocaList.Services)
        RegisterByNamespace(builder.Services,
            typeof(MyVocaList.Services.VenueService).Assembly,
            "MyVocaList.Services",
            ServiceLifetime.Scoped,
            registerByInterface: true);

        // Singleton — UI Components  (MyVocaList.UI.Components)
        RegisterByNamespace(builder.Services,
            typeof(MauiProgram).Assembly,
            "MyVocaList.UI.Components",
            ServiceLifetime.Singleton,
            registerByInterface: true);

        // Transient — ViewModels  (inherits ViewModelBase, excl. AppShellViewModel)
        RegisterByBaseType(builder.Services,
            typeof(MauiProgram).Assembly,
            typeof(UI.ViewModelBase),
            ServiceLifetime.Transient,
            exclude: [typeof(AppShellViewModel)]);

        // Transient — Pages  (inherits ContentPage, excl. AppShell)
        RegisterByBaseType(builder.Services,
            typeof(MauiProgram).Assembly,
            typeof(ContentPage),
            ServiceLifetime.Transient,
            exclude: [typeof(AppShell)]);

        // ── Shell (Singleton — explicit wiring required) ──────────────────────
        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Registers all concrete types whose namespace starts with <paramref name="namespacePrefix"/>
    /// found in <paramref name="assembly"/>. When <paramref name="registerByInterface"/> is true,
    /// each type is registered against its first implemented interface; otherwise as concrete type.
    /// </summary>
    private static void RegisterByNamespace(
        IServiceCollection services,
        System.Reflection.Assembly assembly,
        string namespacePrefix,
        ServiceLifetime lifetime,
        bool registerByInterface)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && t.Namespace != null
                        && t.Namespace.StartsWith(namespacePrefix, StringComparison.Ordinal));

        foreach (var type in types)
        {
            if (registerByInterface)
            {
                var iface = type.GetInterfaces().FirstOrDefault();
                if (iface == null) continue;
                services.Add(new ServiceDescriptor(iface, type, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(type, type, lifetime));
            }
        }
    }

    /// <summary>
    /// Registers all concrete types that inherit <paramref name="baseType"/> found in
    /// <paramref name="assembly"/>, excluding types in <paramref name="exclude"/>.
    /// Registered as concrete type (no interface).
    /// </summary>
    private static void RegisterByBaseType(
        IServiceCollection services,
        System.Reflection.Assembly assembly,
        Type baseType,
        ServiceLifetime lifetime,
        Type[] exclude)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && baseType.IsAssignableFrom(t)
                        && !exclude.Contains(t));

        foreach (var type in types)
            services.Add(new ServiceDescriptor(type, type, lifetime));
    }
}
```

**Step 2: Build**

```
dotnet build MyVocaList.sln
```

Expected: 0 errors.

**Step 3: Commit**

```bash
git add MyVocaList/MauiProgram.cs
git commit -m "refactor(di): replace manual DI registrations with namespace/basetype auto-scanning"
```

---

## Task 7: Update docs to reflect new structure

**Files:**
- Modify: `CLAUDE.md`
- Modify: `.claude/rules/code-principles.md`

**Step 1: Update CLAUDE.md Architecture section**

Add after the existing bullet points:
```
- Service interfaces live in **Domain/ServicesInterfaces** — same principle as repository interfaces
- UI components (MAUI-specific wrappers) live in **MyVocaList/UI/Components**
- ViewModels are co-located with their pages; AppShellViewModel lives at the project root beside AppShell.xaml
- ViewModelBase lives at MyVocaList/UI/ViewModelBase.cs
```

**Step 2: Update code-principles.md Global Usings section**

Update the MAUI GlobalUsings list to reflect current state:
- Remove: `MyVocaList.UI.ViewModels`, `MyVocaList.UI.Services`
- Add: `MyVocaList.UI`, `MyVocaList.UI.Components`, `MyVocaList.Domain.ServicesInterfaces`

Update the DI Registration Conventions section to note that repositories, services, components, ViewModels, and pages are auto-registered via namespace/base-type scanning. Only Database infrastructure and Shell singletons are registered manually.

**Step 3: Commit**

```bash
git add CLAUDE.md .claude/rules/code-principles.md
git commit -m "docs: update architecture docs to reflect refactored solution structure"
```

---

## Completion Checklist

- [ ] `Domain/ServicesInterfaces/` contains all 5 service interfaces with correct namespace
- [ ] `Services/` contains only implementations — no `I*.cs` files remain
- [ ] `IDatabaseInit.cs` and `DatabaseInit.cs` deleted, DI registration removed
- [ ] `UI/Components/SnackbarComponent.cs` exists with `ISnackbarComponent` interface
- [ ] `UI/Services/` folder deleted
- [ ] `UI/Models/ObservableRangeCollection.cs` exists with `MyVocaList.UI.Models` namespace
- [ ] `UI/Collections/` folder deleted
- [ ] `AppShellViewModel.cs` at project root with `MyVocaList` namespace
- [ ] `UI/ViewModelBase.cs` with `MyVocaList.UI` namespace
- [ ] `VenuesViewModel.cs` and `VenueFormViewModel.cs` in `UI/Pages/Venues/` with correct namespace
- [ ] `UI/ViewModels/` folder deleted
- [ ] `MauiProgram.cs` uses inline auto-scanning; only Database + Shell registered manually
- [ ] Build passes with 0 errors
- [ ] CLAUDE.md and code-principles.md updated
