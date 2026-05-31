# Crash & Error Reporting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire Sentry crash reporting and Serilog into the MAUI app so unhandled exceptions and Error/Fatal logs are automatically forwarded to Sentry in release builds.

**Architecture:** Three wiring changes to the MAUI head project: (1) add Sentry + Serilog packages, (2) refactor `LoggingConfiguration` to expose a `Build()` method and add the Sentry sink in release builds, (3) register both in `MauiProgram.cs` and call `GlobalExceptionHandler.Initialize()` in `App.xaml.cs`. No domain, infra, or service logic changes.

**Tech Stack:** `Sentry.Maui` (MAUI-native crash SDK), `Sentry.Serilog` (Serilog→Sentry sink), `Microsoft.Extensions.Configuration.Json`, `Serilog` (already present)

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `MyVocaList/MyVocaList.csproj` | Modify | Add `Sentry.Maui`, `Sentry.Serilog`, `Microsoft.Extensions.Configuration.Json` packages; declare `appsettings.json` + `appsettings.template.json` as `EmbeddedResource` |
| `MyVocaList/appsettings.template.json` | Create | Committed placeholder — empty DSN string |
| `MyVocaList/appsettings.json` | Create (gitignored) | Local DSN configuration — filled by Helder, never committed |
| `.gitignore` | Modify | Add `**/appsettings.json` exclusion |
| `MyVocaList/Extensions/LoggingConfiguration.cs` | Modify | Add `Build(IConfiguration)` static method returning `Serilog.ILogger`; add Sentry sink in release builds |
| `MyVocaList/MauiProgram.cs` | Modify | Load `appsettings.json` from embedded resource; register Serilog via `AddSerilog`; add `UseSentry` in `#if !DEBUG`; add `GetOrCreateSessionId()` helper |
| `MyVocaList/App.xaml.cs` | Modify | Call `GlobalExceptionHandler.Initialize()` as first statement in constructor |
| `MyVocaList.sln` | Modify | Register `appsettings.template.json` in crash-reporting Solution Folder |

---

## Task 1 — Add NuGet packages

**Files:**
- Modify: `MyVocaList/MyVocaList.csproj`

**Risk:** Low — additive package references

- [ ] **Step 1: Add package references**

Open `MyVocaList/MyVocaList.csproj`. Find the existing Serilog block (around line 115):
```xml
<!-- Serilog Packages -->
<PackageReference Include="Serilog" />
<PackageReference Include="Serilog.Extensions.Logging" />
<PackageReference Include="Serilog.Sinks.Debug" />
<PackageReference Include="Serilog.Sinks.File" />
<PackageReference Include="Serilog.Enrichers.Thread" />
```

Replace with:
```xml
<!-- Serilog Packages -->
<PackageReference Include="Serilog" />
<PackageReference Include="Serilog.Extensions.Logging" />
<PackageReference Include="Serilog.Sinks.Debug" />
<PackageReference Include="Serilog.Sinks.File" />
<PackageReference Include="Serilog.Enrichers.Thread" />
<!-- Crash Reporting -->
<PackageReference Include="Sentry.Maui" />
<PackageReference Include="Sentry.Serilog" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" />
```

- [ ] **Step 2: Build to confirm packages resolve**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-incremental
```

Expected: Build succeeds. New packages are downloaded from NuGet.

- [ ] **Step 3: Commit**

```bash
git add MyVocaList/MyVocaList.csproj
git commit -m "feat(crash-reporting): add Sentry.Maui, Sentry.Serilog, and Configuration.Json packages"
```

---

## Task 2 — Config files and gitignore

**Files:**
- Create: `MyVocaList/appsettings.template.json`
- Create: `MyVocaList/appsettings.json` (gitignored — local only)
- Modify: `.gitignore`
- Modify: `MyVocaList/MyVocaList.csproj` (EmbeddedResource declarations)
- Modify: `MyVocaList.sln` (Solution Folder registration)

**Risk:** Low — no code changes; config + project structure only

- [ ] **Step 1: Create the committed template**

Create `MyVocaList/appsettings.template.json`:
```json
{
  "Sentry": {
    "Dsn": ""
  }
}
```

- [ ] **Step 2: Create the local config file**

Create `MyVocaList/appsettings.json`:
```json
{
  "Sentry": {
    "Dsn": ""
  }
}
```

Leave `Dsn` empty for now. Helder will fill in the real DSN from the Sentry project settings page.

- [ ] **Step 3: Gitignore the local config**

Open `.gitignore` at the repo root. Find the `.claude/settings.local.json` entry and add directly below it:
```
# Local Sentry DSN — never commit; fill from Sentry project settings
**/appsettings.json
```

- [ ] **Step 4: Declare both files as EmbeddedResource in the csproj**

Open `MyVocaList/MyVocaList.csproj`. Add a new `ItemGroup` after the existing package references:
```xml
<ItemGroup>
  <!-- Embedded config — appsettings.json is gitignored (local DSN); template is committed -->
  <EmbeddedResource Include="appsettings.json" Condition="Exists('appsettings.json')" />
  <EmbeddedResource Include="appsettings.template.json" />
</ItemGroup>
```

The `Condition="Exists(...)"` means a clean clone (no `appsettings.json`) still builds. `MauiProgram.cs` handles a missing/empty DSN gracefully.

- [ ] **Step 5: Build to confirm**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-incremental
```

Expected: Build succeeds.

- [ ] **Step 6: Register appsettings.template.json in MyVocaList.sln**

Open `MyVocaList.sln`. Find the Solution Folder for the MAUI head project (`MyVocaList`) and add:
```
appsettings.template.json = appsettings.template.json
```

(If no dedicated solution folder exists for config files, add it to the root `MyVocaList` project's SolutionItems section.)

- [ ] **Step 7: Commit**

```bash
git add MyVocaList/appsettings.template.json MyVocaList/MyVocaList.csproj .gitignore MyVocaList.sln
git commit -m "feat(crash-reporting): add appsettings config files and embed in build"
```

Note: `appsettings.json` is gitignored — it must NOT appear in `git status` staged files. If it appears, check that the `.gitignore` entry was saved correctly.

---

## Task 3 — Refactor LoggingConfiguration

**Files:**
- Modify: `MyVocaList/Extensions/LoggingConfiguration.cs`

**Risk:** Low — adds a new method; existing `ConfigureSerilog` is currently unused (orphaned) so breaking it doesn't regress anything

The current `LoggingConfiguration` has only `ConfigureSerilog(this MauiAppBuilder builder)` which both configures `Log.Logger` and calls `builder.Logging.AddSerilog()`. The design requires a `Build(IConfiguration config)` method returning `Serilog.ILogger` so that `MauiProgram.cs` can call `builder.Services.AddSerilog(LoggingConfiguration.Build(config))`.

In release builds, the returned logger must include the Sentry sink at `Error`+ level, with `Information`+ breadcrumbs.

- [ ] **Step 1: Replace LoggingConfiguration.cs content**

Replace the entire file `MyVocaList/Extensions/LoggingConfiguration.cs` with:

```csharp
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace MyVocaList.Extensions;

/// <summary>
/// Builds the application Serilog logger. Call <see cref="Build"/> once at startup
/// and pass the result to <c>builder.Services.AddSerilog()</c>.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Builds and returns the configured Serilog logger.
    /// In release builds, also attaches the Sentry sink for Error/Fatal events.
    /// </summary>
    public static Serilog.Core.Logger Build(IConfiguration config)
    {
#if DEBUG
        var minimumLevel = LogEventLevel.Debug;
#else
        var minimumLevel = LogEventLevel.Warning;
#endif

        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "myvocalist-.log");

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft.Maui", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}");

#if !DEBUG
        var dsn = config["Sentry:Dsn"];
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            // Forward Error/Fatal to Sentry; attach Information+ entries as breadcrumbs
            loggerConfig.WriteTo.Sentry(o =>
            {
                o.Dsn = dsn;
                o.MinimumBreadcrumbLevel = LogEventLevel.Information;
                o.MinimumEventLevel = LogEventLevel.Error;
            });
        }
#endif

        return loggerConfig.CreateLogger();
    }
}
```

- [ ] **Step 2: Build to confirm**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-incremental
```

Expected: 0 errors. The removed `ConfigureSerilog` method had no callers in the codebase so no reference errors.

- [ ] **Step 3: Commit**

```bash
git add MyVocaList/Extensions/LoggingConfiguration.cs
git commit -m "feat(crash-reporting): refactor LoggingConfiguration.Build() with Sentry sink in release builds"
```

---

## Task 4 — Wire Serilog and Sentry in MauiProgram.cs

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`

**Risk:** Medium — touches startup; a bad Sentry DSN must not crash the app

`MauiProgram.cs` currently:
- Does NOT call `ConfigureSerilog` (it was orphaned)
- Has `builder.Logging.AddDebug()` inside `#if DEBUG` only

This task:
1. Reads `appsettings.json` from the embedded assembly resource
2. Calls `builder.Services.AddSerilog(LoggingConfiguration.Build(config))` — registers Serilog for the full DI pipeline (replaces the orphaned `ConfigureSerilog`)
3. In release builds, calls `builder.UseSentry(...)` on the MAUI builder chain with device enrichment and anonymous session ID
4. Adds a private `GetOrCreateSessionId()` helper in `MauiProgram`

- [ ] **Step 1: Add using directives at the top of MauiProgram.cs**

Open `MyVocaList/MauiProgram.cs`. The file starts with several `using` statements. Add these two that are not already present:

```csharp
using Microsoft.Extensions.Configuration;
using MyVocaList.Extensions;
```

- [ ] **Step 2: Add config loading and Serilog/Sentry wiring**

Find the beginning of `CreateMauiApp()` body, just before `var builder = MauiApp.CreateBuilder();`. Add config loading there:

```csharp
public static MauiApp CreateMauiApp()
{
    // Load appsettings.json embedded resource (optional — missing file is fine)
    var assembly = typeof(MauiProgram).Assembly;
    IConfiguration config = new ConfigurationBuilder().Build(); // empty fallback
    var resourceName = assembly.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));
    if (resourceName != null)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
            config = new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    var builder = MauiApp.CreateBuilder();
    // ... rest of existing code
```

- [ ] **Step 3: Replace DEBUG-only logging block with Serilog registration**

Find the current logging block near the end of `CreateMauiApp()`:
```csharp
#if DEBUG
        builder.Logging.AddDebug();
#endif
```

Replace it with:
```csharp
// Register Serilog (file + debug sinks always; Sentry sink in release builds)
builder.Services.AddSerilog(LoggingConfiguration.Build(config));
```

- [ ] **Step 4: Add UseSentry to the MAUI builder chain**

Find the existing builder chain:
```csharp
builder
    .UseMauiApp<App>()
    .UseMauiCommunityToolkit()
    .UseDevExpress(useLocalization: false)
    ...
```

Wrap `UseSentry` around the chain **in release builds only**. Change it to:

```csharp
#if !DEBUG
builder
    .UseMauiApp<App>()
    .UseMauiCommunityToolkit()
    .UseDevExpress(useLocalization: false)
    .UseDevExpressCollectionView()
    .UseDevExpressControls()
    .UseDevExpressEditors()
    .UseSentry(options =>
    {
        options.Dsn = config["Sentry:Dsn"] ?? string.Empty;
        options.Release = AppInfo.VersionString;
        options.Environment = "production";
        options.AttachScreenshot = false;
        options.ConfigureScope(scope =>
        {
            scope.SetTag("device.model", DeviceInfo.Model);
            scope.SetTag("os.version", DeviceInfo.VersionString);
            scope.SetExtra("session_id", GetOrCreateSessionId());
        });
    })
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
        fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
        fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
    });
#else
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
#endif
```

- [ ] **Step 5: Add GetOrCreateSessionId helper**

After the closing brace of `CreateMauiApp()`, add the private helper inside the `MauiProgram` class:

```csharp
private static string GetOrCreateSessionId()
{
    const string key = "session_id";
    var id = Preferences.Get(key, null);
    if (id == null)
    {
        id = Guid.NewGuid().ToString("N");
        Preferences.Set(key, id);
    }
    return id;
}
```

- [ ] **Step 6: Build to confirm**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-incremental
```

Expected: 0 errors, 0 warnings about unresolved references.

- [ ] **Step 7: Commit**

```bash
git add MyVocaList/MauiProgram.cs
git commit -m "feat(crash-reporting): wire Serilog and Sentry in MauiProgram.cs; add session ID enrichment"
```

---

## Task 5 — Fix App.xaml.cs: initialize GlobalExceptionHandler

**Files:**
- Modify: `MyVocaList/App.xaml.cs`

**Risk:** Low — one line added; `GlobalExceptionHandler.Initialize()` is idempotent

The spec (AC-CRASH-07) requires `GlobalExceptionHandler.Initialize()` to be called before the first page is displayed. Currently `App.xaml.cs` does NOT call it. The constructor calls `InitializeComponent()` first, but `GlobalExceptionHandler.Initialize()` must be called even earlier (before any exception can occur during app init).

- [ ] **Step 1: Add Initialize() as the very first line of App constructor**

Open `MyVocaList/App.xaml.cs`. Find the constructor:

```csharp
public App(IServiceProvider serviceProvider)
{
    _serviceProvider = serviceProvider;
    InitializeComponent();
    _ = WarmUpDevExpressAsync();
    _ = MigrateAsync();
}
```

Change it to:

```csharp
public App(IServiceProvider serviceProvider)
{
    GlobalExceptionHandler.Initialize();
    _serviceProvider = serviceProvider;
    InitializeComponent();
    _ = WarmUpDevExpressAsync();
    _ = MigrateAsync();
}
```

- [ ] **Step 2: Add the using directive if not already present**

Check the top of `App.xaml.cs` for `using MyVocaList.Services;`. If it is not there, add it.

- [ ] **Step 3: Build to confirm**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-incremental
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/App.xaml.cs
git commit -m "fix(crash-reporting): call GlobalExceptionHandler.Initialize() before first page (AC-CRASH-07)"
```

---

## Task 6 — Final build verification

**Risk:** Low — read-only verification

- [ ] **Step 1: Clean build in release configuration**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release --no-incremental
```

Expected: 0 errors. Sentry is compiled in (release mode). No missing namespace errors.

- [ ] **Step 2: Debug build still compiles without Sentry**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug --no-incremental
```

Expected: 0 errors. Sentry packages are absent from the Debug build output path (not called).

- [ ] **Step 3: Confirm GlobalExceptionHandler.Initialize() is called**

Read `MyVocaList/App.xaml.cs` and verify `GlobalExceptionHandler.Initialize()` is the first statement in the `App` constructor.

- [ ] **Step 4: Confirm no Sentry calls in DEBUG path**

Search for `UseSentry` in `MauiProgram.cs` — confirm it is inside `#if !DEBUG`.

Search for `WriteTo.Sentry` in `LoggingConfiguration.cs` — confirm it is inside `#if !DEBUG`.

- [ ] **Step 5: Update tasks.md and BACKLOG.md**

Check off all tasks in `Docs/Management/BusinessFeatures/crash-reporting/tasks.md`.
Update `Docs/Management/BACKLOG.md` crash-reporting row to `✅ Done`.

- [ ] **Step 6: Final commit**

```bash
git add Docs/Management/BusinessFeatures/crash-reporting/tasks.md \
        Docs/Management/BACKLOG.md
git commit -m "chore(crash-reporting): mark feature complete in tasks.md and BACKLOG.md"
```

---

## Self-Review — Spec Coverage Check

| AC | Covered by |
|----|-----------|
| AC-CRASH-01 — Unhandled exception captured | Task 5 (GlobalExceptionHandler.Initialize) + Task 4 (UseSentry wires MAUI exception pipeline) |
| AC-CRASH-02 — Required context (version, OS, device, session ID, no PII) | Task 4 GetOrCreateSessionId + ConfigureScope tags |
| AC-CRASH-03 — Serilog Error/Fatal → Sentry with breadcrumbs | Task 3 WriteTo.Sentry with MinimumBreadcrumbLevel=Information |
| AC-CRASH-04 — Debug isolation (#if !DEBUG guards) | Tasks 3 and 4 both use #if !DEBUG |
| AC-CRASH-05 — Email alert on first occurrence | Out of scope (Sentry dashboard config — no app code) |
| AC-CRASH-06 — File sink active | Task 3 Build() method includes WriteTo.File always |
| AC-CRASH-07 — GlobalExceptionHandler.Initialize() before first page | Task 5 |
