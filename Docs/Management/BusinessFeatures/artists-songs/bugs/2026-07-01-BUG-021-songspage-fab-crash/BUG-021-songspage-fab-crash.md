# BUG-021 — SongsPage: App Crashes When FAB (Add) Tapped (post BUG-016/BUG-020 fix)

**Severity:** Critical — app crash; user cannot create a new song
**Discovered:** 2026-07-01 — Helder, manual smoke test (Visual Studio debug session)
**Reporter:** Helder
**Status:** Fixed — emulator-verified 2026-07-03 (TEST-011, `Docs/Management/EMULATOR_TEST_MASTER_LIST.md`): FAB opens SongFormPage, no DI resolution error, no crash.

---

## Symptom

Tapping the FAB button (Add) on `SongsPage` crashes the app. This bug is a continuation of
BUG-016 (route collision) and BUG-020 (SecureStorage exception in `async void OnAppearing`).
Both earlier fixes addressed real but different issues; the crash persisted. Unlike the
earlier occurrences, this one was captured in a Visual Studio debug session with the actual
exception (see `BUG-021-songspage-fab-crash-debug-exception.png`):

- Outer: `Android.Runtime.JavaProxyThrowable`
- Inner (`System.Exception`, from Microsoft.Extensions.DependencyInjection):
  **"Unable to resolve service for type 'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer'
  while attempting to activate 'MyVocaList.Services.ArtistResolutionService'."**

## Expected

Tapping the FAB navigates to `SongFormPage` in add-mode and the page renders normally.

## Root Cause

`ISimilarityScorer` (implemented by `MyVocaList.Infra.Similarity.SimilarityScorer`) was never
registered in the DI container (`MyVocaList/MauiProgram.cs`). Navigating to `SongFormPage`
activates `SongFormViewModel` → `ISongResolutionService` (`SongResolutionService`) →
`IArtistResolutionService` (`ArtistResolutionService`), and **both** resolution services take
`ISimilarityScorer` in their constructors. The container throws at navigation time when it
reaches the first unregistered dependency.

BUG-020's investigation had checked only `SongFormViewModel`'s *direct* constructor
dependencies (all registered) and missed this *transitive* gap two levels down.

A full walk of the `SongFormPage`/`SongFormViewModel` dependency chain
(`IArtistService`, `ISongService`, `ISongResolutionService` → `IArtistResolutionService` →
`ISimilarityScorer`, `ISnackbarComponent`, `ILogger<>`, `ISongKaraokeUrlService` →
`ISongKaraokeUrlRepository`, `ISecureStorageWrapper`, `IMessenger`, plus all repository
dependencies) confirmed `ISimilarityScorer` was the **only** missing registration.

## Fix

1. Registered `ISimilarityScorer` → `SimilarityScorer` as **Scoped** (consistent with its
   consumers `IArtistResolutionService` / `ISongResolutionService`, per
   `code-principles.md § DI Registration Conventions`).
2. To make the registration graph testable (a DI gap is invisible to compile/build), the
   platform-independent registrations (repositories, music metadata HTTP providers, business
   services) were extracted verbatim from `MauiProgram.CreateMauiApp` into
   `ServiceCollectionExtensions.AddAppServices(this IServiceCollection)`
   (`MyVocaList/Extensions/ServiceCollectionExtensions.cs`). `MauiProgram.cs` now calls
   `builder.Services.AddAppServices();` — registration behavior is identical.
   MAUI-platform-only registrations (DbContext paths, SecureStorage, Snackbar, navigation,
   Shell, pages, ViewModels) remain in `MauiProgram.cs`.

**Files changed:**
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` — new; extracted registrations plus
  the missing `ISimilarityScorer` registration (the fix)
- `MyVocaList/MauiProgram.cs` — replaced the extracted registration blocks with a single
  `AddAppServices()` call
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` — new; three
  DI-resolution regression tests

**Regression risk:** Low — registrations were moved verbatim into the extension method
(registration order between distinct service types is not significant for MS.DI); the only
behavioral change is the added `ISimilarityScorer` registration, which turns a guaranteed
crash into a working resolution.

## Regression Test (Critical — mandatory before close)

`MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`:

1. `AddAppServices_ResolvingArtistResolutionService_Succeeds` — resolves
   `IArtistResolutionService` from the app's real registration graph.
   **Red** (before fix): failed with exactly the production exception —
   `System.InvalidOperationException : Unable to resolve service for type
   'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer' while attempting to activate
   'MyVocaList.Services.ArtistResolutionService'.`
   **Green** (after adding the registration): passes.
2. `AddAppServices_ResolvingSongResolutionService_Succeeds` — covers the second consumer of
   `ISimilarityScorer`.
3. `AddAppServices_ResolvingSongFormViewModelGraph_Succeeds` — activates the full
   `SongFormViewModel` dependency graph with MAUI-platform-only dependencies
   (`ISnackbarComponent`, `ISecureStorageWrapper`, `IMessenger`) mocked — guards against any
   future transitive DI gap in the SongsPage Add flow.

**Not yet verified:** emulator smoke test (tap FAB → SongFormPage renders) — no emulator was
available in this session. Unlike BUG-020, this fix targets the exception actually captured
in the debugger, so confidence is high; Helder should still confirm on-device.
