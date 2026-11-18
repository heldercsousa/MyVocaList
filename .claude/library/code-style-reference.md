# Code Style Reference

> On-demand reference. Loaded via the `myvocalist-coding` skill or a routing pointer from `.claude/rules/code-principles.md`.
> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.
> Spec language determinism (prohibited vague terms): see `workflow.md § Spec quality four-gate review`.

## Contents

- [XML Documentation Comments](#xml-documentation-comments)
- [Nullable Reference Types — DISABLED](#nullable-reference-types--disabled)
- [C# Style](#c-style)
- [Service Return Patterns](#service-return-patterns)
- [Exception Handling](#exception-handling)
- [Global Usings](#global-usings)
- [Pagination](#pagination)
- [DI Registration Conventions](#di-registration-conventions-mauiprogramcs)
- [UI Thread Performance — ObservableRangeCollection](#ui-thread-performance--observablerangecollection)
- [Loading Pattern — Lists and Data Loads](#loading-pattern--lists-and-data-loads)
- [Static Analysis Suppressions](#static-analysis-suppressions)

---

## XML Documentation Comments
- **Interfaces are the source of truth for method documentation.** Write `<summary>`, `<param>`, and `<returns>` on the interface method.
- **Implementing types must use `<inheritdoc />`** — never duplicate or rephrase the interface comment.
- Exception: a type may add `<remarks>` for implementation-specific notes, but the base comment must still be `<inheritdoc />`.

```csharp
// Interface — owns the documentation
public interface IVenueService
{
    /// <summary>Creates a new venue with the given name.</summary>
    /// <param name="name">The display name for the venue.</param>
    /// <returns>(true, message) on success; (false, reason) on failure.</returns>
    Task<(bool success, string message)> CreateVenueAsync(string name);
}

// Implementation — inherits it
public sealed class VenueService : IVenueService
{
    /// <inheritdoc />
    public async Task<(bool success, string message)> CreateVenueAsync(string name) { ... }
}
```

## Nullable Reference Types — DISABLED
`Directory.Build.props` sets `<Nullable>disable</Nullable>` as a baseline, but every individual `.csproj` overrides to `<Nullable>enable</Nullable>` with specific warnings suppressed (CS8618, CS8601, CS8603, CS8604, CS8625, CS8602, etc.).

**Effective behavior:** Nullable analysis is on but lenient — warnings that would require defensive rewrites of existing patterns are suppressed.

- **Do NOT** add `?` to reference types or use null-forgiving operators `!` beyond what already exists
- **Do NOT** add `= string.Empty` initializers just to satisfy nullability analysis
- **Do NOT** add null guards that exist solely to satisfy the nullable analyzer (real null guards for real nulls are fine)
- Do not change suppressed warning sets without discussing it first
- This is a deliberate project decision — do not "fix" it by enabling stricter checking

## C# Style

### Design Principles
- Prefer composition over inheritance

### Modern C# (13+)
- Use `record` for DTOs and value objects
- Use pattern matching over `if/switch` chains where it reads clearly
- Use `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`
- Use collection expressions `[item1, item2]` over `new List<T> { ... }`
- Prefer primary constructors for simple types

### Naming
- Interfaces: `IVenueService`, `IVenueRepository`
- Services: `VenueService`, `VenueRepository`
- ViewModels: `VenuesViewModel` (plural for list pages)
- Commands: verb + noun + `Command` e.g. `AddVenueCommand`
- Async methods: `VerbNounAsync` e.g. `CreateVenueAsync`

### Projects and folders
- Each layer project lives in a folder named after its layer only. The project file, assembly and root
  namespace keep the `MyVocaList.` prefix:

  | Folder | Project file | Root namespace |
  |--------|--------------|----------------|
  | `Domain/` | `MyVocaList.Domain.csproj` | `MyVocaList.Domain` |
  | `Contracts/` | `MyVocaList.Contracts.csproj` | `MyVocaList.Contracts` |
  | `Services/` | `MyVocaList.Services.csproj` | `MyVocaList.Services` |
  | `Infra/` | `MyVocaList.Infra.csproj` | `MyVocaList.Infra` |
  | `Extensions/` | `MyVocaList.Extensions.csproj` | `MyVocaList.Extensions` (e.g. `MyVocaList.Extensions.Strings`) |
  | `MyVocaList/` | `MyVocaList.csproj` (MAUI app) | `MyVocaList` |
  | `MyVocaList.Tests/` | `MyVocaList.Tests.csproj` | `MyVocaList.Tests` |

- A new layer project follows the same rule: folder `<Layer>/`, project `MyVocaList.<Layer>.csproj`.
  A `ProjectReference` uses the folder path, e.g. `..\Extensions\MyVocaList.Extensions.csproj`.
- The `Extensions/` project holds extension methods shared by more than one layer (string trimming
  used by Services, Infra and the app). The app's own `MyVocaList/Extensions/` folder (DI
  registration, logging setup, XAML markup extensions) is a different thing: it is app-only code,
  even though it also uses the `MyVocaList.Extensions` namespace.

### Async
- Always `async/await` for I/O — never `.Result` or `.Wait()`
- Use `CancellationToken` on all async service methods
- `Task.Run` is acceptable for debounce patterns in ViewModels (see `VenuesViewModel.TriggerSearchDebounce`)

### ViewModel Pattern (CommunityToolkit.Mvvm)
```csharp
public abstract partial class ViewModelBase : ObservableObject
{
    protected void RunOnUiThread(Action action) { ... }
    protected Task RunOnUiThreadAsync(Func<Task> asyncAction) { ... }
}

// Fields generate properties via source generator
[ObservableProperty] private bool _isLoading;

// partial methods for change notification
partial void OnIsLoadingChanged(bool value) => NotifyEmptyStates();
```

## Service Return Patterns
Services return tuples for operations that can fail (confirmed in codebase):
```csharp
// Create: returns (success, message, entity?)
Task<(bool success, string message, Venue? venue)> CreateVenueAsync(string name);

// Update/Delete: returns (success, message)
Task<(bool success, string message)> UpdateVenueAsync(int id, string name);

// Validation: returns (isValid, message)
(bool isValid, string message) ValidateNameInput(string name);
```

Never throw exceptions for expected business failures (name too long, duplicate, etc.) — return `(false, message)`.

## Exception Handling

### GlobalExceptionHandler (Services project)
Handles three categories:
1. `AppDomain.CurrentDomain.UnhandledException` — logged as Fatal, app may terminate
2. `TaskScheduler.UnobservedTaskException` — logged as Error, marked observed (prevents crash)
3. Android: `AndroidEnvironment.UnhandledExceptionRaiser` — logged as Fatal, marked handled

**Rule:** Do NOT add try-catch for exceptions that `GlobalExceptionHandler` will catch. Let unexpected exceptions bubble up so they are logged with full context.

### Allowed try-catch patterns

**1. Cancellation (OperationCanceledException) — silently ignore:**
```csharp
catch (OperationCanceledException)
{
    // Cancellation requested — silently return
}
```

**2. Disposal races on CancellationTokenSource:**
```csharp
try { _searchCts?.Cancel(); _searchCts?.Dispose(); }
catch { /* ignore disposal races */ }
```

**3. First-run DB table absence:**
```csharp
try { await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsLock"); }
catch { /* Table does not exist on first run — safe to ignore. */ }
```

**4. Error recovery with logging (not swallowed):**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to load more venues (page {Page})", loadingPage);
    RunOnUiThread(() => IsRefreshing = false);
}
```

**Never:** empty catch blocks that swallow errors silently (except disposal races as above).

## Global Usings

### Directory.Build.props (all projects)
```csharp
System
System.Collections.Generic
System.Collections.ObjectModel
System.Linq
System.Threading
System.Threading.Tasks
System.Windows.Input
```

> *Verify against the project's `Directory.Build.props` — this list is a snapshot, not the authoritative source.*

### MyVocaList (MAUI) — GlobalUsings.cs
```csharp
CommunityToolkit.Mvvm.ComponentModel
CommunityToolkit.Mvvm.Input
DevExpress.Maui
DevExpress.Maui.CollectionView
DevExpress.Maui.Controls
DevExpress.Maui.Core
Microsoft.Extensions.Logging
MyVocaList.Contracts.DTOs.List
MyVocaList.Navigation
MyVocaList.Services
MyVocaList.UI.Models
MyVocaList.UI.Pages.*  (all page namespaces)
MyVocaList.UI.Services
MyVocaList.UI.ViewModels
```

> *Verify against the project's `GlobalUsings.cs` — this list is a snapshot, not the authoritative source.*

### Services — GlobalUsings.cs
```csharp
Microsoft.Extensions.Logging
```

> *Verify against the project's `GlobalUsings.cs` — this list is a snapshot, not the authoritative source.*

### Infra — GlobalUsings.cs
```csharp
Microsoft.Extensions.Logging
System.Text
```

> *Verify against the project's `GlobalUsings.cs` — this list is a snapshot, not the authoritative source.*

### Rule for new usings
- Applies to 2+ types in one project → add to that project's `GlobalUsings.cs`
- Applies across 2+ projects → add to `Directory.Build.props`

## Pagination
`AppPagination.DefaultPageSize = 20` in `MyVocaList.Contracts` is the **single source of truth** for page size.
Never declare a local `const int PageSize` or hardcode `20` — always reference `AppPagination.DefaultPageSize`.

## DI Registration Conventions (MauiProgram.cs)
- `AddSingleton` — AppShell, AppShellViewModel, ISnackbarService (shared state)
- `AddScoped` — Repositories, Services, IDatabaseInit (per-lifetime scope)
- `AddTransient` — Pages, ViewModels (new instance per navigation)

## UI Thread Performance — ObservableRangeCollection

`ObservableRangeCollection.ReplaceRange` and `ClearRange` fire `CollectionChanged(Reset)`.
Each `Reset` inside a `RunOnUiThread` block triggers a full DXCollectionView re-render of all items.

**Rules:**
- Never call `ReplaceRange` more than once per `RunOnUiThread` block. Two calls = two full render passes = ANR risk.
- After a list refresh or search, **clear selection** (`ClearRange` + `SelectedCount = 0`) — never restore selection via `ReplaceRange`. Restoring selection fires a second `Reset` and is confusing UX (selection crossing a data reload boundary).
- All collection mutations that must happen together belong in a single `RunOnUiThread(() => { ... })` call — but keep the work inside minimal: no LINQ queries, no service calls, only collection operations and property assignments.

```csharp
// Correct — single Reset, selection cleared
RunOnUiThread(() =>
{
    Venues.ReplaceRange(list);
    if (SelectedVenues.Count > 0)
    {
        SelectedVenues.ClearRange();
        SelectedCount = 0;
    }
    NotifyEmptyStates();
});

// Wrong — two Resets, two full re-renders, ANR risk
RunOnUiThread(() =>
{
    Venues.ReplaceRange(list);
    var restored = Venues.Where(v => selectedIds.Contains(v.Id)).ToList(); // LINQ on UI thread
    SelectedVenues.ReplaceRange(restored); // second Reset
    SelectedCount = SelectedVenues.Count;
    NotifyEmptyStates();
});
```

## Loading Pattern — Lists and Data Loads

> Helder, 2026-11-17: the way the lists load is the app pattern "whenever a list is loaded", for every
> current and future list, so that no agent bypasses it. Specs: REQ-SLD-32 to 34 (song form),
> `Docs/Management/BusinessFeatures/cross-cutting/changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/` (REQ-DLH-01 to 05).

**Lists — never hand-roll loading.**
- Every list ViewModel derives from `CrudListViewModelBase<TItem>` and overrides only `FetchPageAsync` / `FetchMoreAsync` (one service call each, passing the `CancellationToken`). Every list page derives from `CrudListPageBase` and uses `CrudListView`.
- The base owns: the fetch inside `Task.Run` (Microsoft.Data.Sqlite runs "async" calls synchronously, so a direct await freezes the UI thread), the 400 ms search debounce, the shimmer flag bound by `CrudListView`, result application in one `RunOnUiThread` block, and the debug-build hold. A list that needs other loading behaviour changes the base in its own task — never a copy in the derived class.
- Shimmer: shown whenever the list replaces its rows (first load, later visit, search, filter reload) until the new rows are applied, in **every build**. Pull-to-refresh keeps the DevExpress refresh circle and load-more keeps the DevExpress load-more indicator; neither shows the shimmer.

**Any other screen that loads data before it can be used** (an edit form, a picker):
- The service call runs off the UI thread (`Task.Run`); a loading flag is set before it and cleared only after the results are applied on the UI thread (`RunOnUiThreadAsync`); the page shows a `dx:ShimmerView` (or the DX loading control of that screen) bound to the flag.
- Commands that would write (Save) do nothing while the flag is true. A failed load is caught, logged, shown with a message, and the screen navigates back — it never stays open half-filled.
- Tests wait for the load to end (`WaitUntilAsync(() => !vm.IsXxxLoading)` or an exposed load task), never `Task.Yield()` alone.

**Field searches** (autocomplete, remote APIs): the search runs off the UI thread and the field shows that a search is running. Pending item: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-17-remote-and-local-field-searches-show-a-loading-indicator/`.

**Debug-build hold:** in debug builds only, the app start-up sets a minimum loading time (1,500 ms) so the shimmer can be checked; release builds compile no hold; unit tests run with no hold. New loads that show a shimmer use the same hold, never their own delay.

## Static Analysis Suppressions

**Never add a new suppression** (`#pragma warning disable`, `[SuppressMessage]`, `// ReSharper disable`) without:
1. A comment explaining why the suppression is necessary (one sentence minimum)
2. An expiry comment if the suppression is temporary: `// TODO: Remove after [condition]`
3. Logging the suppression in `.claude/exception-registry.md` if it suppresses a constitutional rule

Existing suppressions in `Directory.Build.props` for nullable analysis (CS8618, CS8601, CS8603, CS8604, CS8625, CS8602, etc.) are pre-approved project decisions — do not add to that list without architecture review.

If you cannot fix a warning without suppressing it, log a `blocked: spec gap` status and stop.

---

> **Authorship note:** Content extracted verbatim from `.claude/rules/code-principles.md` (Rules File Refactoring, Phase 0 spike, 2026-07-04). Zero content change — container change only. Human-review before relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
