# Crash & Error Reporting — Tasks
**Status:** Pending spec approval — tasks to be detailed during plan phase

---

## Placeholder

Tasks will be written after Helder approves `requirements.md` and `design.md`.

High-level phases anticipated:

1. Add NuGet packages (`Sentry.Maui`, `Sentry.Serilog`)
2. Add `appsettings.json` + `appsettings.template.json`; update `.gitignore`
3. Fix `LoggingConfiguration` — add `Build(IConfiguration)` overload; register in `MauiProgram.cs`
4. Fix `GlobalExceptionHandler` — call `Initialize()` from `App.xaml.cs`
5. Wire Sentry in `MauiProgram.cs` (release-only guard)
6. Add session ID enrichment
7. Write integration smoke test (verify event shape in Sentry sandbox project)
