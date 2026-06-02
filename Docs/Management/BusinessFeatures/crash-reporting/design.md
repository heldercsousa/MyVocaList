# Crash & Error Reporting — Design
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

---

## Chosen Stack

| Component | Package | Purpose |
|-----------|---------|---------|
| `Sentry.Maui` | NuGet | MAUI-native crash SDK; hooks into MAUI exception pipeline |
| `Sentry.Serilog` | NuGet | Serilog sink that forwards Error/Fatal log events to Sentry |

**Why Sentry:** Only mature SDK with native .NET MAUI bindings. AppCenter retired June 2025. Firebase Crashlytics has no .NET bindings. Free tier (5K events/month) covers MVP usage. Email alerts and webhook support built in.

### Alternative hosting options (not implemented at MVP)
| Option | Trade-off |
|--------|-----------|
| **Sentry cloud** *(chosen)* | Zero ops; free tier; email alerts; webhook support |
| **Self-hosted Sentry (Docker)** | Full data ownership; ~2h setup; requires a VPS; upgrade path if privacy requirements change |
| **Seq** | Local structured log viewer; no mobile crash SDK; not a replacement for Sentry |

---

## DSN Storage

**Recommended pattern:** `appsettings.json` (gitignored) + committed `appsettings.template.json` placeholder.

```json
// appsettings.json (gitignored — Helder fills this locally)
{
  "Sentry": {
    "Dsn": "https://<key>@<org>.ingest.sentry.io/<project>"
  }
}

// appsettings.template.json (committed)
{
  "Sentry": {
    "Dsn": ""
  }
}
```

**Alternative patterns (documented for future reference):**
| Pattern | When to use |
|---------|------------|
| Environment variable `SENTRY_DSN` injected at build time | If a CI/CD pipeline is added later |
| Android `secrets.xml` / iOS `Info.plist` entry | Platform-native config; avoids .NET config layer; more ceremony |
| Azure Key Vault | Overkill for single-dev MVP |

---

## Initialization Sequence

### MauiProgram.cs changes

```csharp
// 1. Read appsettings.json
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// 2. Wire Serilog (fixes orphaned LoggingConfiguration)
builder.Services.AddSerilog(LoggingConfiguration.Build(config));

// 3. Wire Sentry (release builds only)
#if !DEBUG
builder.UseMauiApp<App>()
       .UseSentry(options =>
       {
           options.Dsn = config["Sentry:Dsn"];
           options.Release = AppInfo.VersionString;
           options.Environment = "production";
           options.AttachScreenshot = false; // privacy
           options.AddIntegration(new SerilogIntegration()); // breadcrumbs from Serilog
       });
#endif
```

### App.xaml.cs changes

```csharp
public App(...)
{
    GlobalExceptionHandler.Initialize(); // fixes current bug — must be first
    InitializeComponent();
    MainPage = new AppShell();
}
```

### LoggingConfiguration.cs changes

Add a `Build(IConfiguration config)` overload that accepts config (no behavioral change — same sinks, same levels). The `SentrySink` is added to the Serilog pipeline only in release builds, forwarding `Error`+`Fatal` events and attaching `Information`+ breadcrumbs.

---

## Sentry Event Enrichment

```csharp
options.ConfigureScope(scope =>
{
    scope.SetTag("device.model", DeviceInfo.Model);
    scope.SetTag("os.version", DeviceInfo.VersionString);
    scope.SetExtra("session_id", GetOrCreateSessionId()); // GUID, stored in Preferences
});
```

`GetOrCreateSessionId()` reads `Preferences.Get("session_id", null)`; if null, generates a new `Guid.NewGuid().ToString("N")` and persists it. This is not reset between launches — it is a per-install identifier with no PII.

---

## Layers Affected

| Layer | Change |
|-------|--------|
| `MyVocaList` (MAUI head) | `MauiProgram.cs` — Sentry init + Serilog registration |
| `MyVocaList` (MAUI head) | `App.xaml.cs` — `GlobalExceptionHandler.Initialize()` call |
| `MyVocaList` (MAUI head) | `appsettings.json` (gitignored), `appsettings.template.json` (committed) |
| `MyVocaList.Services` | `GlobalExceptionHandler.cs` — no logic change; already correct |
| `MyVocaList` (MAUI head) | `Extensions/LoggingConfiguration.cs` — add `Build(IConfiguration)` overload |
| `.csproj` | Add `Sentry.Maui`, `Sentry.Serilog` package references |

No domain, infra, or repository changes.

---

## Invariants & Postconditions

- Sentry SDK must initialize before any page is displayed; a failure to init (bad DSN, no network) must not crash the app.
- Debug builds must never send traffic to Sentry — enforced via `#if !DEBUG` guard, not runtime config.
- `GlobalExceptionHandler.Initialize()` is idempotent — safe to call multiple times (no-op on subsequent calls).
- Session ID is stable for the lifetime of an install; cleared only if `Preferences` are cleared (app uninstall/reinstall).

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| Sentry cloud over self-hosted | Zero ops for MVP; self-hosted is the documented upgrade path |
| `appsettings.json` for DSN | Consistent with .NET config patterns; gitignore keeps secrets out of repo |
| No screenshot capture | Privacy — karaoke queue may display singer names |
| Breadcrumb floor at `Information` | `Debug`/`Verbose` are too noisy and would exhaust the 50-breadcrumb window |
| No user-visible crash UI at MVP | Scope; GlobalExceptionHandler logs and lets the OS handle the crash presentation |
