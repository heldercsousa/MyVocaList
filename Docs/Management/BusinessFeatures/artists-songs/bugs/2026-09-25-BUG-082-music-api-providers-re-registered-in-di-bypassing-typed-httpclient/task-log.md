---
## Task: Fix BUG-082 — music providers resolved with unconfigured HttpClient
**Plan:** N/A (bug fix — commit message is the spec, per `workflow.md § Bug Fix Pattern`)
**Status:** To Review
**Started:** 2026-09-25
**Completed:** 2026-09-25

### Root cause

`ServiceCollectionExtensions.cs` (`AddAppServices`) registers a typed HttpClient per provider via
`services.AddHttpClient<MusicBrainzProvider>(...)` / `AddHttpClient<DeezerProvider>(...)`, setting
`BaseAddress` and `User-Agent`. This typed-client wiring only takes effect when the container
resolves the provider **as its own concrete type** (`MusicBrainzProvider`/`DeezerProvider`) —
`IHttpClientFactory`'s typed-client pipeline intercepts activation of exactly that type.

Immediately below, the code re-registered the providers a second time:

```csharp
services.AddScoped<IMusicMetadataProvider, MusicBrainzProvider>();
services.AddScoped<IMusicMetadataProvider, DeezerProvider>();
```

`AddScoped<TService, TImplementation>` builds `TImplementation` via `ActivatorUtilities`, which
resolves each constructor parameter from the container directly — for `HttpClient` this falls
through to the DI container's own (unconfigured) `HttpClient` resolution, not the typed-client
factory pipeline. The result: every consumer that resolves `IMusicMetadataProvider` (the aggregate
`IMusicMetadataService` used by the song-form autocomplete and picker pages) got a **second,
independent instance** of `MusicBrainzProvider`/`DeezerProvider` with `HttpClient.BaseAddress ==
null` and no `User-Agent` header. Every request against a relative URL (`"artist?query=..."`,
`"search/artist?q=..."`) threw `InvalidOperationException` ("An invalid request URI was provided"),
which each provider's own broad catch swallowed, returning an empty result set silently to the UI.

Evidence: `bugs/2026-09-25-BUG-082-.../README.md`; `changes/2026-09-24-song-form-title-first.../findings.md § 1`.

### Fix

Changed the second registration to resolve the *typed-client instance* instead of constructing a
new one:

```csharp
services.AddScoped<IMusicMetadataProvider>(sp => sp.GetRequiredService<MusicBrainzProvider>());
services.AddScoped<IMusicMetadataProvider>(sp => sp.GetRequiredService<DeezerProvider>());
```

`sp.GetRequiredService<MusicBrainzProvider>()` resolves the concrete type, which *is* intercepted
by the typed-client pipeline, so the provider that ends up behind `IMusicMetadataProvider` is the
same instance (same scope) that has the configured `HttpClient`. MusicBrainz stays registered —
its removal is a separate, already-planned decision tracked in the title-first song-form item, out
of scope here.

### Changed files
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` — fixed the `IMusicMetadataProvider`
  registrations to resolve through the typed-client factory.
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` — added the
  regression test `AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient`.

### Red (before fix) — `AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient`

```
[xUnit.net 00:00:02.14]     MyVocaList.Tests.Unit.DependencyInjection.AppServicesRegistrationTests.AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient [FAIL]
  Mensagem de erro:
   Assert.NotNull() Failure: Value is null
  Rastreamento de pilha:
     at MyVocaList.Tests.Unit.DependencyInjection.AppServicesRegistrationTests.AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient() in ...AppServicesRegistrationTests.cs:line 92
Com falha! – Com falha:     1, Aprovado:     0, Ignorado:     0, Total:     1
```
(Failure is `httpClient.BaseAddress` being null — the exact defect BUG-082 describes.)

### Green (after fix)

```
Aprovado!  – Com falha:     0, Aprovado:     1, Ignorado:     0, Total:     1, Duração: 120 ms
```

### Build notes

- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: **0 errors**, 2 warnings (DevExpress
  evaluation-license notices only, pre-existing, unrelated to this change).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`:
  - Run 1: 634 passed, 1 failed — `SongServiceReadScopeTests.ExistsByTitleForArtistAsync_UnknownTitle_ReturnsFalse`
    threw `Microsoft.Data.Sqlite` `ObjectDisposedException` inside `UnitOfWorkTestHost.Build` →
    `EnsureCreated()`. This matches the known flaky **BUG-076** (random SQLite `ObjectDisposedException`,
    ~1 in 5 runs), unrelated to this change (it is not in the DI/provider registration path).
  - Run 2 (re-run per BUG-076 known-flake protocol): **635 passed, 0 failed.**
- Files written and re-read after edit: `ServiceCollectionExtensions.cs` (re-read lines 33-49 to
  confirm the two `AddScoped<IMusicMetadataProvider>(sp => ...)` factory registrations),
  `AppServicesRegistrationTests.cs` (re-read the new test method + added usings).

### Manual check for Helder

Once the title-first autocomplete exists on `SongFormPage`, confirm on-device that a Deezer search
(e.g. searching an artist name not already in the local catalog) returns results instead of an
empty list — this was the user-visible symptom BUG-082 explains.

### Commit(s)

See git log on `fix/bug-082-provider-di` for the fix commit (bug-fix format, `workflow.md § Bug Fix
Pattern`). Not merged, not pushed — worktree branch only, per briefing.
