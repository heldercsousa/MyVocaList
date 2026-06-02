# Crash & Error Reporting — Tasks
**Status:** ✅ Complete — 2026-05-31

---

## Completed Tasks

- [x] **Task 1 — Add NuGet packages**
  - Added `Sentry.Maui`, `Sentry.Serilog`, `Microsoft.Extensions.Configuration.Json` to `MyVocaList.csproj`
  - Versions declared in `Directory.Packages.props`

- [x] **Task 2 — Config files and gitignore**
  - Created `appsettings.template.json` (committed placeholder)
  - Created `appsettings.json` (gitignored, local DSN — fill from Sentry project settings)
  - Added `**/appsettings.json` to `.gitignore`
  - Declared both files as `EmbeddedResource` in `MyVocaList.csproj`
  - Registered `appsettings.template.json` in `MyVocaList.sln`

- [x] **Task 3 — Refactor LoggingConfiguration**
  - Replaced orphaned `ConfigureSerilog` extension method with `Build(IConfiguration)` returning `Serilog.Core.Logger`
  - Sentry sink added inside `#if !DEBUG` when DSN is non-empty
  - File sink and Debug sink always active

- [x] **Task 4 — Wire Serilog and Sentry in MauiProgram.cs**
  - Config loaded from embedded assembly resource at startup
  - `builder.Logging.AddSerilog(LoggingConfiguration.Build(config))` replaces orphaned debug-only logging
  - `UseSentry(...)` added to builder chain inside `#if !DEBUG` with device enrichment
  - `GetOrCreateSessionId()` helper added — per-install anonymous GUID via `Preferences`

- [x] **Task 5 — Fix App.xaml.cs (AC-CRASH-07)**
  - `GlobalExceptionHandler.Initialize()` now called as first statement in `App` constructor

- [x] **Task 6 — Final build verification**
  - Debug build: ✅ 0 errors
  - Release build: pre-existing CS8612/CS8604/CA2024 in unrelated files (not introduced by this feature)
  - All crash-reporting code compiles clean in both configurations

---

## Deferred

- [ ] **Integration smoke test** — trigger a test exception in a debug-Sentry sandbox project and verify event shape in dashboard. Deferred to first release cycle.
- [ ] **Multi-env DSN** — introduce `appsettings.Release.json` for staging vs production DSN separation. See BACKLOG.md entry. Deferred before first store release.
