# Code Principles

## Language
All code, comments, logs, and UI text must be English only.

## Spec Language — Determinism

In spec files (`requirements.md`, `design.md`) and in task descriptions, vague quality adjectives are **prohibited**. They force agents to invent their own thresholds, producing code that is technically compliant but misaligned with intent.

**Prohibited terms:** fast, slow, quick, responsive, robust, secure, user-friendly, intuitive, handles gracefully, works correctly, performs well, reasonable, appropriate, suitable, adequate.

**Replace with measurable thresholds:**

| Instead of | Write |
|------------|-------|
| "the list loads quickly" | "the list renders within 300 ms on a mid-range Android device" |
| "handles errors gracefully" | "returns `(false, \"message\")` on failure; no exception escapes the service boundary" |
| "the form validates correctly" | "name ≤ 30 chars; empty name returns `(false, \"Name is required\")`" |
| "secure storage" | "stored via `SecureStorage.SetAsync`; never in `Preferences` or plain SQLite" |

If the threshold is not yet known, write: `[threshold TBD — establish before implementation starts]`. This is valid in a draft spec; it is **not** valid when a task is dispatched to a subagent.

**Rule:** Any acceptance criterion containing a prohibited term is not ready for implementation. The Tester cannot write a deterministic test from it; the Builder cannot implement it without guessing.

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

## Architecture Constraints
- Business logic lives in **Services** only — never in ViewModels or pages
- Repository interfaces in **Domain** — implementations in **Infra**
- Only the **MAUI** project references Infra (for DI wiring, AppDbContext, migrations)
- Services depend only on Domain interfaces — never on Infra types directly
- DTOs are records in the **Contracts** project

## C# Style

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

### Service Return Patterns (confirmed in codebase)
Services return tuples for operations that can fail:
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

### Services — GlobalUsings.cs
```csharp
Microsoft.Extensions.Logging
```

### Infra — GlobalUsings.cs
```csharp
Microsoft.Extensions.Logging
System.Text
```

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

## EF Core / SQLite
- Migrations applied on startup via `MigrateAsync()` — blocking call via `Task.Run(...).GetAwaiter().GetResult()`
- `__EFMigrationsLock` row is cleared before each `MigrateAsync()` call (SQLite single-user workaround)
- `CollationInterceptor` applied globally for case-insensitive search
