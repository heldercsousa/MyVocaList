# Design — DbContext lifetime & unit-of-work pattern

> **Candidate C is chosen (§ 8, APPROVED by Helder).** Three candidates are presented with the same
> worked example so they can be compared side by side; § 8 records the approved decisions that
> make Candidate C's shape final (Revision 8 adds the failure-tuple save-skip mechanism, § 6b;
> Revision 9, 2026-08-04, makes the unrecognised-`TResult` fallback fail-closed instead of
> fail-open, § 6b / § 8). No production code has been written for this change.
>
> Framework claims below are sourced from Context7 (version-pinned): EF Core docs
> `/dotnet/entityframework.docs`, .NET MAUI `/dotnet/maui@10.0.51`. Both servers were available.

---

## 1. Framework facts this design rests on

| Fact | Source |
|------|--------|
| MAUI creates a DI scope **per Window**, from the application-level `IServiceProvider`. There is no per-page and no per-navigation scope. | `/dotnet/maui@10.0.51` `docs/design/Scoping.md` — "When a new window is created in .NET MAUI, a new scope is generated from the Application-level IServiceProvider." |
| MAUI's internal `MauiFactory` resolver has **no support for scoped services**; MS.Ext.DI is used for the main implementation precisely because of this. | `/dotnet/maui@10.0.51` `docs/design/HandlerResolution.md` |
| `DbContext` is **not thread-safe**; contexts must not be shared between threads and all async calls must be awaited before reuse. | `/dotnet/entityframework.docs` — *DbContext Lifetime, Configuration, and Initialization § The DbContext lifetime* |
| An `InvalidOperationException` from EF Core **can put the context in an unrecoverable state**. | ibid. |
| The documented remedies for concurrent access are: register scoped and create a scope per unit of work with `IServiceScopeFactory`, **or** register transient. | ibid. § *Concurrent access prevention* |
| `AddDbContextFactory<T>` registers the factory **and** the context type; contexts obtained from the factory are **not** managed by the provider and must be disposed explicitly. | ibid. + `what-is-new/ef-core-6.0` |

**Consequence:** `Scoped` is not a wrong lifetime in the abstract — it is wrong *here* because on a
single-window mobile app the window scope IS the app session. Candidate A exists precisely because
this is a missing-scope problem, not a wrong-lifetime problem.

> **Reviewer-finding correction (verified via Context7, EF Core docs `/dotnet/entityframework.docs`,
> `what-is-new/ef-core-6.0`):** a spec reviewer asserted `AddDbContextFactory` does **not** also
> register the `DbContext` type for direct injection. That assertion is incorrect. The EF Core
> documentation states verbatim:
>
> > "The `AddDbContextFactory` method in EF Core 6.0 now also registers the `DbContext` type directly
> > as a scoped service. This allows applications to resolve both a scoped `DbContext` instance and a
> > factory for creating `DbContext` instances from the dependency injection container. Instances
> > created by the factory must be explicitly disposed, while injected `DbContext` instances are
> > disposed when their scope is disposed."
>
> This is not re-litigated further in this document. **Confirmed consequence for this design:**
> repositories keep constructor-injecting `AppDbContext` directly — as they do today — with **zero**
> signature edits, because `AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` registers
> `AppDbContext` itself as scoped in addition to `IDbContextFactory<AppDbContext>`. `IUnitOfWork` uses
> the factory registration to create the per-operation scope; repository constructors resolve the
> scoped `AppDbContext` from inside that scope exactly as `AddDbContext` would have provided it.

## 2. Current state

```
MauiProgram.cs:61   AddDbContext<AppDbContext>(...)          → Scoped → 1 instance per Window → 1 per app session
MauiProgram.cs:71-72 AddScoped IBackupRepository/IBackupService
ServiceCollectionExtensions.cs  23 × AddScoped (all repos + all services)
                                → 25 de-facto singletons, all sharing the one context

Repositories: ctor-inject AppDbContext directly. IDbContextFactory used nowhere.
Saves:        6 pass-through SaveChangesAsync + ~21 call sites in services. No choke point.
Families:     Infra/Repository/*  (BaseRepository<T> descendants + 5 standalone)
              Infra/Repositories/* (EventRepository, QueueRepository)
Correct today: App.xaml.cs:35, :54 — the only two manual scopes, both correct.
```

### 2a. Derived counts (replaces prior estimates — Grep/Bash verified against current `develop` HEAD)

**Repository interface method signatures, both families.**

```
$ for f in Domain/RepositoryInterface/*.cs Domain/Interfaces/IEventRepository.cs Domain/Interfaces/IQueueRepository.cs; do
    grep -cE '\);\s*$' "$f"
  done
```

> **Caveat:** this `grep -cE '\);\s*$'` counts lines ending in `);` — i.e. trailing-paren lines, not
> declarations — and happens to give the right total (80) for these particular interface files because
> every method signature here is single-line. It would **miscount** a file containing a multi-line
> signature (parameters wrapped across lines) or a trailing `);` that isn't a method declaration (e.g.
> a multi-line default-parameter expression). Treat this number as verified-by-inspection for the
> current file set, not as a generally reliable counting method — do not reuse this one-liner on a
> file with wrapped signatures without spot-checking the result.

| File | Methods |
|---|---|
| `Domain/RepositoryInterface/IArtistRepository.cs` | 13 |
| `Domain/RepositoryInterface/IBackupRepository.cs` | 4 |
| `Domain/RepositoryInterface/IBaseRepository.cs` | 8 |
| `Domain/RepositoryInterface/ICatalogRepository.cs` | 6 |
| `Domain/RepositoryInterface/IEventParticipationRepository.cs` | 1 |
| `Domain/RepositoryInterface/IEventRepository.cs` | 3 |
| `Domain/RepositoryInterface/IPersonRepository.cs` | 6 |
| `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs` | 7 |
| `Domain/RepositoryInterface/ISongRepository.cs` | 13 |
| `Domain/RepositoryInterface/IVenueRepository.cs` | 7 |
| `Domain/Interfaces/IEventRepository.cs` (duplicate family) | 6 |
| `Domain/Interfaces/IQueueRepository.cs` | 6 |
| **Total** | **80** |

**Service methods that need a UoW wrap.** The prior draft derived this count from a method-**name**
verb regex (`Create*`/`Update*`/`Delete*`/`Remove*`/`Add*`/`Record*`/`Commit*`/`Reorder*`/…), which
undercounts: it misses any mutator whose name doesn't happen to contain one of those verb substrings,
even though its body still reaches a write. Recomputed by **behavior** instead — every public service
method that (transitively) reaches a repository mutation call site (`Add`/`Update`/`Delete`/`Remove`/
`Set…Async`/`Reorder…Async`, or a direct `…Repository.SaveChangesAsync()` after mutating a fetched
entity in place) was found via:

```
$ grep -noE '_\w*[Rr]epository\.\w*(Add|Update|Delete|Remove|Set|Reorder|Merge)\w*\(' Services/*.cs
$ grep -noE '_\w*[Rr]epository\.SaveChangesAsync\(' Services/*.cs
$ grep -noE '_repo\.\w*(Add|Update|Delete|Remove|Set|Save)\w*\(' Services/*.cs   # BackupService's field is named _repo, not *Repository
```

then each call site was mapped back to its enclosing public method (or, if the call site is inside a
private helper, to the public method that reaches that helper — e.g. `QueueService.
GetOrCreateDefaultEventAsync` is private and folds into `RecordParticipationAsync`, § 10 note).

**35 methods across 12 files** (name-regex figure in parentheses where it differed):

| File | Methods | Mutating methods |
|---|---|---|
| `ArtistResolutionService` | 1 (1) | `CommitAsync` |
| `ArtistService` | 3 (3) | `CreateArtistAsync`, `UpdateArtistAsync`, `DeleteArtistsAsync` |
| `BackupService` | **1 (0 — missing file)** | `CreateFullBackupAsync` (`_repo.AddAsync` + `SaveChangesAsync`, `BackupService.cs:60-61`) — not in the prior draft's 11-file list at all |
| `CatalogService` | 2 (2) | `AddSongToCatalogAsync`, `RemoveSongFromCatalogAsync` |
| `EventService` | **5 (1)** | `CreateEventAsync`, `StartEventAsync`, `PauseEventAsync`, `ResumeEventAsync`, `FinishEventAsync` — the name regex only matched `CreateEventAsync`; `Start`/`Pause`/`Resume`/`Finish` each call `_eventRepository.UpdateAsync` (`EventService.cs:103,129,155,182`) but don't contain a listed verb |
| `PersonService` | 3 (3) | `CreatePersonAsync`, `UpdatePersonAsync`, `DeletePersonsAsync` |
| `QueueService` | **3 (2)** | `AddPersonToQueueAsync`, `RecordParticipationAsync`, `SetActiveEventAsync` — the regex missed `SetActiveEventAsync` (`QueueService.cs:107-110`, calls `_eventRepository.SetActiveEventAsync`); name contains "Set", not a listed verb |
| `QueueServiceNew` | **6 (2)** | `EnqueueSingerAsync`, `RegisterParticipationAsync`, `StopPerformanceAsync`, `MarkAbsentAsync`, `UpdateSongSelectionAsync`, `ReorderQueueAsync` — the regex only matched `UpdateSongSelectionAsync`/`ReorderQueueAsync`; `Enqueue*`/`Register*`/`Stop*`/`Mark*` each reach `_queueRepository.AddAsync`/`UpdateAsync` (`QueueServiceNew.cs:86,114,150,178`) but don't contain a listed verb |
| `SongKaraokeUrlService` | 3 (2 mutating + `RecordPlayAsync`, counted = 3) | `AddUrlAsync`, `RemoveUrlAsync`, `RecordPlayAsync` |
| `SongResolutionService` | 1 (1) | `CommitAsync` |
| `SongService` | 4 (4) | `CreateSongAsync`, `UpdateSongAsync`, `CreateSongWithUrlsAsync`, `DeleteSongsAsync` |
| `VenueService` | 3 (3) | `CreateVenueAsync`, `UpdateVenueAsync`, `DeleteVenuesAsync` |
| **Total** | **35 (25)** | |

**Correction to prior draft:** the earlier "~120 signature edits" figure for Candidate B and the
"~60+ method signatures" figure in § 5 were estimates. Recomputed from the 80-method repository total
above and the 35-method service total above: Candidate B adds one `AppDbContext` parameter to every
one of the **80** repository interface methods (interface + implementation = **160** edits, not
counting the ~10 repository class files touched once each) plus a `db`-threading change to each of the
**35** service methods — the side-by-side table in § 7 below uses these corrected numbers.

## 3. The worked example (identical for all three candidates)

The BUG-068 path. Today:

```csharp
// Services/SongService.cs:99-144  (current)
var song = await _songRepository.GetByIdAsync(id, ct);        // NoTracking → untracked instance
...
await _songRepository.UpdateAsync(song, ct);                  // _db.Songs.Update(song)
await _songRepository.SaveChangesAsync(ct);                   // ← throws if an earlier op left
                                                              //   this key tracked in the shared context

// Infra/Repository/SongRepository.cs  (current)
public class SongRepository : ISongRepository
{
    private readonly AppDbContext _db;                        // ← session-lifetime
    public SongRepository(AppDbContext db) => _db = db;

    public async Task<Song> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task UpdateAsync(Song song, CancellationToken ct) { _db.Songs.Update(song); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
```

---

## 4. Candidate A — Real DI scope per unit of work, created in ONE place

Keep `AddDbContext` (Scoped) and all 25 `AddScoped` registrations. Introduce a single well-named
place that creates a real `IServiceScope` per unit of work, via `IServiceScopeFactory` — the remedy
EF Core documents first.

**Where the single place goes.** The only boundary that is both (a) singular and (b) aligned with a
unit of work is **the ViewModel command invocation**. A ViewModel would not inject `ISongService`;
it would inject a scope-running primitive and resolve the service inside:

```csharp
// Registration — unchanged from today
builder.Services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlite(...)
        .AddInterceptors(sp.GetRequiredService<CollationInterceptor>(),
                         sp.GetRequiredService<TransactionLogInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddScoped<ISongService, SongService>();      // unchanged
builder.Services.AddScoped<ISongRepository, SongRepository>();// unchanged
builder.Services.AddSingleton<IScopedOperation, ScopedOperation>();   // the ONE new type

// ViewModel — the unit-of-work boundary
var (ok, msg) = await _op.RunAsync<ISongService, (bool, string)>(
    svc => svc.UpdateSongAsync(id, Title, Featured, Lyrics, HasManualEdits, ct: ct));

// SongService.UpdateSongAsync — UNCHANGED, byte for byte
// SongRepository            — UNCHANGED, byte for byte
```

`ScopedOperation.RunAsync` is ~8 lines: `using var scope = _scopeFactory.CreateScope();
return await body(scope.ServiceProvider.GetRequiredService<TService>());`

**Cross-repository / multi-save flows:** all unchanged. `CreateSongWithUrlsAsync` keeps its single
save; `QueueService.GetOrCreateDefaultEventAsync`'s two saves now share one scoped context, which is
correct. `ArtistResolutionService.CommitAsync` also unchanged — `CreateArtistAsync` saves, then the
returned entity is mutated and saved again **on the same still-live scoped context**, which is legal
because the entity is already tracked by that context (the second `Update` re-attaches the *same*
instance, not a second instance with the same key). This is the cheapest resolution of the hardest
case of the three candidates.

**Files touched:** ~1 new type + every ViewModel that calls a service (~15-20 files). Zero service
files, zero repository files, zero interface files.

**DRY score:** repository methods **0** added lines. Service methods **0** added lines. ViewModel
call sites: the call becomes a lambda — roughly **+1 line and +1 level of nesting per command**,
~30-40 call sites. Net: best possible DRY on the Infra/Services side; the cost migrates to the UI
layer.

**New-developer legibility:** **poor-to-fair.** The boundary is at the ViewModel, far from where the
saves happen. Reading `SongService.UpdateSongAsync` in isolation gives no clue that a unit of work
exists or where it ends. The context stays ambient inside Services — exactly the property REQ-UOW-12
rejects. It reads as "framework magic that a new developer must be told about".

**Leaks:** real and serious. `AppShellViewModel` and `AppShell` are `AddSingleton`
(`MauiProgram.cs:109-110`). Any singleton that injects a scoped service creates a **captive
dependency** — the scoped instance is resolved from the root provider and lives forever, silently
re-creating exactly the bug being fixed. Under Candidate A this failure mode is *invisible*: the code
compiles and runs, and the bug reappears only on the paths that go through the singleton. Guarding it
requires `ValidateScopes = true`, which MAUI does not enable by default and which is a startup-only
check.

**Interceptors:** unchanged — the options lambda still receives `sp`. ✔ REQ-UOW-14. `TransactionLogInterceptor`
now sees only the current scope's entries. ✔ REQ-UOW-15.

**BaseRepository / families / 6 pass-throughs:** all survive untouched. **No DRY win.**

**Testability:** `TestDbContextFactory` would need a real `ServiceProvider` in tests to exercise the
scope, or tests keep constructing repositories directly (in which case tests no longer exercise the
production lifetime — the pattern is untested by construction). Existing workarounds at
`CatalogRepositoryTests.cs:66` / `ArtistRepositoryTests.cs:366` would still be needed for tests that
share one context across arrange/act. ✘ REQ-UOW-17.

---

## 5. Candidate B — `AddDbContextFactory` + explicit short-lived context

The service method is the unit of work. Repositories become **stateless**; the context is passed in.
`SaveChangesAsync` leaves the repository interfaces entirely.

```csharp
builder.Services.AddDbContextFactory<AppDbContext>((sp, o) => o.UseSqlite(...)
        .AddInterceptors(sp.GetRequiredService<CollationInterceptor>(),
                         sp.GetRequiredService<TransactionLogInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddSingleton<ISongRepository, SongRepository>();   // stateless → singleton

// Services/SongService.cs
public async Task<(bool success, string message)> UpdateSongAsync(int id, string title, ..., CancellationToken ct = default)
{
    var (isValid, message) = ValidateTitleInput(title);
    if (!isValid) return (false, message);
    title = title.Trim();

    await using var db = await _factory.CreateDbContextAsync(ct);          // ← +1 line
    var song = await _songRepository.GetByIdAsync(db, id, ct);             // ← db threaded through
    if (song == null) return (false, "Song not found");
    if (await _songRepository.ExistsByTitleForArtistAsync(db, song.ArtistId, title, id, ct))
        return (false, "A song with this title already exists for this artist");

    song.Title = title; song.UpdatedAt = DateTime.UtcNow; /* … */
    await _songRepository.UpdateAsync(db, song, ct);
    await db.SaveChangesAsync(ct);                                          // ← the one save
    return (true, $"Song updated to '{title}'");
}

// Infra/Repository/SongRepository.cs — stateless
public class SongRepository : ISongRepository
{
    public Task<Song> GetByIdAsync(AppDbContext db, int id, CancellationToken ct)
        => db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task UpdateAsync(AppDbContext db, Song song, CancellationToken ct)
    { db.Songs.Update(song); return Task.CompletedTask; }
    // SaveChangesAsync: DELETED
}
```

**Cross-repository / multi-save flows:**
- `CreateSongWithUrlsAsync` — natural fit. One `db`, both repositories, one save. FK fixup via
  navigation still works because both entities are in the same context. ✔
- `GetOrCreateDefaultEventAsync`, `SetActiveEventAsync`, `ReorderAsync` — one `db`, one save. ✔
- **`ArtistResolutionService.CommitAsync` — the hardest case, and it gets worse.** It calls
  `_artistService.CreateArtistAsync`, which under Candidate B opens **its own** context and saves.
  The returned `Artist` is then mutated and saved again — through a *different* context. That is
  precisely the detached-entity/duplicate-key hazard, re-created at a different layer. Resolutions,
  none free: (i) split every service into a `…Core(db, …)` overload plus a public wrapper that opens
  the context — doubling the service surface; (ii) let `CommitAsync` bypass `IArtistService` and
  drive `IArtistRepository` directly, duplicating the creation rules the service owns; (iii) pass
  `AppDbContext` through the *service* signatures too, at which point `db` is a parameter on
  virtually everything. All three are worse than the status quo on comprehensibility.

**Files touched:** 2 registration files + 12 repository interface files + ~10 repository
implementation files + ~15 service files. **~40 files, 80 repository method signatures (derived,
§ 2a) × 2 (interface + implementation) = 160 signature edits.**

**DRY score:** service methods **+1 to +2 lines each × 35 methods (derived, § 2a) = ~50-70 lines**;
repository methods **+1 parameter on every one of the 80 methods**, doubled across interface +
implementation → **160 signature edits**. This is boilerplate-by-convention. Against constraint 3 it
is the **worst** of the three, by a wide margin.

**New-developer legibility:** **excellent.** `await using var db = …` at the top and
`db.SaveChangesAsync` at the bottom make the boundary unmissable. This is the candidate's real merit.

**Interceptors:** `AddDbContextFactory` takes the same `(sp, options)` overload — preserved. ✔

**BaseRepository / families / pass-throughs:** all 6 pass-throughs **deleted** (REQ-UOW-11 ✔).
`BaseRepository<T>` survives but must be rewritten to take `db` per method, losing its `_context`
field — at which point it is a static helper in all but name. The two families remain two families.

**Testability:** best of the three. `TestDbContextFactory` becomes a genuine `IDbContextFactory<AppDbContext>`
implementation over the SQLite temp file — no shape change, and it starts modelling production
exactly. Both tracking workarounds are deletable (REQ-UOW-17 ✔).

**Note on `AddPooledDbContextFactory`:** rejected. Pooling exists to amortise context construction
under server-grade request rates; a single-user mobile app does not benefit, and pooling adds a
reset-semantics footgun (pooled contexts retain configuration but reset state) for zero measured
gain. Revisit only if profiling shows context construction on a hot path.

---

## 6. Candidate C — Scope-per-operation, declared at the service boundary (synthesis)

**The proposal.** Take Candidate A's mechanism (a real DI scope, so nothing about constructor
injection changes) and move the boundary to Candidate B's location (the service method, so it is
visible where the writes are). Express it once, as a single `IUnitOfWork` primitive.

```csharp
// ── Registration ─────────────────────────────────────────────────────────────
builder.Services.AddDbContextFactory<AppDbContext>((sp, o) => o.UseSqlite(...)
        .AddInterceptors(sp.GetRequiredService<CollationInterceptor>(),
                         sp.GetRequiredService<TransactionLogInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
        lifetime: ServiceLifetime.Scoped);
// AddDbContextFactory also registers AppDbContext itself, so scoped resolution still works
// and repositories keep constructor-injecting AppDbContext unchanged.

builder.Services.AddSingleton<IUnitOfWork, UnitOfWork>();   // the ONE new type
// all 25 AddScoped registrations: UNCHANGED
```

```csharp
// ── The one new type (~35 lines with the ambient-join, written once, never repeated) ────
public interface IUnitOfWork
{
    /// <summary>Primary API. Runs <paramref name="body"/> against a repository of type
    /// <typeparamref name="TRepo"/> resolved inside a fresh (or ambiently-joined, § 6a) DI scope
    /// owning one AppDbContext. Disposes the context on exit.
    /// <para><b>Save-skip (Revision 8, § 6b):</b> after <paramref name="body"/> returns without
    /// throwing, <c>ExecuteAsync</c> inspects <typeparamref name="TResult"/> for this codebase's
    /// failure-tuple convention (<c>code-style-reference.md § Service Return Patterns</c>) and saves
    /// only when it signals success. Service code is unchanged — it keeps returning its ordinary
    /// tuple; the save is skipped automatically when the tuple's leading <c>bool</c> is <c>false</c>.
    /// <b>Fail-closed (Revision 9, § 6b):</b> a <typeparamref name="TResult"/> that carries no
    /// recognised success signal is refused, not guessed — this overload throws
    /// <see cref="InvalidOperationException"/> naming the two valid fixes (implement
    /// <see cref="IUnitOfWorkOutcome"/>, or use the no-signal overload below if the method has no
    /// failure mode) — see § 6b for the exhaustive rule.</para></summary>
    Task<TResult> ExecuteAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>No-signal overload (Revision 8, § 6b). Use for service methods that return bare
    /// <see cref="Task"/> — e.g. <c>QueueService.RecordParticipationAsync</c>,
    /// <c>QueueService.SetActiveEventAsync</c>, <c>SongKaraokeUrlService.RecordPlayAsync</c> — which
    /// have no failure tuple to inspect. <b>Always saves</b> when <paramref name="body"/> returns
    /// without throwing; this is the safe default for a method with no success/failure signal at all
    /// (§ 6b "no-signal fallback").</summary>
    Task ExecuteAsync<TRepo>(Func<TRepo, Task> body, CancellationToken ct = default);

    /// <summary>Same as <see cref="ExecuteAsync{TRepo,TResult}"/> but never saves (Revision 6). Use
    /// for read-only service methods so the method name itself carries the intent — a reviewer
    /// reading the call site does not need to open the body to know whether it writes.</summary>
    Task<TResult> ExecuteReadAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>Escape hatch (Revision 7). Use ONLY for genuinely multi-repository flows or flows
    /// that must call into another service (§ 6a's three nested chains) — resolving from
    /// <see cref="IServiceProvider"/> is a service-locator pattern and is a last resort, not the
    /// default. Save-skip behavior (Revision 8, § 6b) applies identically to the typed overload.</summary>
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>Escape-hatch no-signal variant (Revision 8, § 6b). Always saves; for
    /// multi-repository/nested-service flows whose body returns bare <see cref="Task"/>.</summary>
    Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);

    /// <summary>Escape-hatch read variant — never saves.</summary>
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
}

public sealed class UnitOfWork(IServiceScopeFactory scopeFactory) : IUnitOfWork
{
    // AsyncLocal flag joins an already-open unit of work instead of nesting a second scope —
    // ships now per Revision 2, not deferred. Holds across the 3-level chain found in § 6a
    // (SongResolutionService.CommitAsync → ArtistResolutionService.CommitAsync → ArtistService.CreateArtistAsync).
    private static readonly AsyncLocal<IServiceProvider?> _ambientScope = new();

    public Task<TResult> ExecuteAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default)
        => ExecuteAsync<TResult>(sp => body(sp.GetRequiredService<TRepo>()), ct);

    public Task ExecuteAsync<TRepo>(Func<TRepo, Task> body, CancellationToken ct = default)
        => ExecuteAsync(sp => body(sp.GetRequiredService<TRepo>()), ct);

    public Task<TResult> ExecuteReadAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default)
        => ExecuteReadAsync<TResult>(sp => body(sp.GetRequiredService<TRepo>()), ct);

    public async Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        if (_ambientScope.Value is { } joined) return await body(joined);   // join, don't nest

        await using var scope = scopeFactory.CreateAsyncScope();
        _ambientScope.Value = scope.ServiceProvider;
        try
        {
            var result = await body(scope.ServiceProvider);
            // Save-skip (Revision 8, § 6b): save only when the result signals success.
            if (ResultSignalsSuccess(result))
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync(ct);
            return result;
        }
        finally { _ambientScope.Value = null; }
    }

    // No-signal overload (Revision 8, § 6b) — always saves. For bodies with nothing to inspect
    // (bare Task): RecordParticipationAsync, SetActiveEventAsync, RecordPlayAsync.
    public async Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
    {
        if (_ambientScope.Value is { } joined) { await body(joined); return; }

        await using var scope = scopeFactory.CreateAsyncScope();
        _ambientScope.Value = scope.ServiceProvider;
        try
        {
            await body(scope.ServiceProvider);
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync(ct);   // always — no signal to inspect
        }
        finally { _ambientScope.Value = null; }
    }

    public async Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        if (_ambientScope.Value is { } joined) return await body(joined);
        await using var scope = scopeFactory.CreateAsyncScope();
        _ambientScope.Value = scope.ServiceProvider;   // symmetric with ExecuteAsync (§ 9: at most
        try                                            // one AppDbContext per unit of work) — a nested
        {                                               // ExecuteAsync/ExecuteReadAsync call must join
            return await body(scope.ServiceProvider);   // this scope rather than opening a second one
        }
        finally { _ambientScope.Value = null; }
        // no SaveChangesAsync — read-only, per Revision 6
    }

    // Save-skip signal detection (Revision 9, § 6b). Exhaustive by construction — every branch is
    // either a recognised signal or the explicit, fail-closed refusal below (never a silent guess,
    // and never a silent commit).
    private static bool ResultSignalsSuccess<TResult>(TResult result)
    {
        // 1) This codebase's universal Service Return Pattern: (bool success, string message, ...).
        //    Every C# ValueTuple, of every arity, implements System.Runtime.CompilerServices.ITuple —
        //    this is a real structural type check, not reflection over field names.
        if (result is System.Runtime.CompilerServices.ITuple t && t.Length > 0 && t[0] is bool tupleSuccess)
            return tupleSuccess;

        // 2) Named result types opt in explicitly by implementing IUnitOfWorkOutcome
        //    (e.g. BackupResult — appending ": IUnitOfWorkOutcome" is a blocking prerequisite of
        //    wrapping BackupService in Wave 5, § 10, not a later or optional step; see § 6b).
        if (result is IUnitOfWorkOutcome outcome)
            return outcome.Success;

        // 3) No recognised signal -> refuse to guess.
        throw new InvalidOperationException(
            $"{typeof(TResult).Name} carries no success signal. " +
            "Implement IUnitOfWorkOutcome, or use the " +
            "no-signal ExecuteAsync overload if this method " +
            "has no failure mode.");
    }
}

/// <summary>Opt-in marker for named result records/types that carry a success signal but are not a
/// ValueTuple (Revision 8, § 6b). Implement this instead of relying on structural tuple detection
/// when a mutating service method's natural return type is a named type, e.g.
/// <c>public record BackupResult(bool Success, string Message, ...) : IUnitOfWorkOutcome;</c>
/// <b>Fail-closed (Revision 9):</b> a named result type passed to the value-returning
/// <c>ExecuteAsync</c> that does NOT implement this interface is no longer assumed successful — it
/// throws <see cref="InvalidOperationException"/> instead. Implementing this interface is therefore
/// mandatory, not optional, for any named result type used with that overload.</summary>
public interface IUnitOfWorkOutcome
{
    bool Success { get; }
}
```

> **Bug fix (this revision):** the prior draft's `ExecuteReadAsync` created a scope without setting
> `_ambientScope.Value`, so a nested `ExecuteAsync`/`ExecuteReadAsync` call inside its `body` would open
> a **second** scope+context instead of joining the read scope — contradicting § 9's "at most one
> `AppDbContext` per unit of work" invariant. Fixed by setting/clearing `_ambientScope.Value` the same
> way `ExecuteAsync` does; unintentional bug, not a design change.

**Inputs / Outputs / Preconditions — `IUnitOfWork.ExecuteAsync<TRepo, TResult>` (primary API):**
- **Inputs:** `body` — a delegate receiving the resolved `TRepo` instance, returning `Task<TResult>`;
  `ct` — optional cancellation token, defaults to `default`.
- **Outputs:** `TResult` returned by `body` on success. On failure inside `body`, whatever the body
  itself returns per `code-style-reference.md § Service Return Patterns` (a `(false, message)` tuple)
  — `ExecuteAsync` itself does not catch or translate exceptions; an exception from `body` propagates
  after the `using` disposes the scope (no partial state survives, REQ-UOW-06). **Fail-closed on an
  unrecognised `TResult` (Revision 9, § 6b):** if `TResult` is neither a `ValueTuple` with a leading
  `bool` nor an `IUnitOfWorkOutcome` implementer, `ExecuteAsync` throws `InvalidOperationException`
  before any save is attempted, naming the two valid fixes (implement `IUnitOfWorkOutcome`, or use
  the no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task>, ct)` overload).
- **Preconditions:** `TRepo` must be resolvable from the DI container (registered `AddScoped`/
  `AddSingleton`, unchanged from today, § 2a "Reviewer-finding correction"). Caller must not hold a
  reference to `TRepo` or the `AppDbContext` behind it beyond the lifetime of `body` — doing so
  recreates a captive dependency (§ 9 Invariants).
- **Postconditions:** exactly one `SaveChangesAsync` executes (unless `body` itself calls a nested
  `ExecuteAsync`, which joins the ambient scope and does not trigger a second save); the scope and its
  `AppDbContext` are disposed before `ExecuteAsync` returns.

**Inputs / Outputs / Preconditions — `IUnitOfWork.ExecuteReadAsync<TRepo, TResult>`:**
- **Inputs / Preconditions:** identical to `ExecuteAsync`.
- **Outputs:** `TResult` returned by `body`. **No `SaveChangesAsync` is ever called** — calling
  `ExecuteReadAsync` with a body that mutates tracked entities is a misuse of the API (the mutation is
  silently lost when the scope disposes); this is intentional per Revision 6 and is the tradeoff for
  the method name carrying the read/write intent.

```csharp
// ── Services/SongService.cs ─────────────────────────────────────────────────
public Task<(bool success, string message)> UpdateSongAsync(int id, string title, ..., CancellationToken ct = default)
    => _uow.ExecuteAsync<ISongRepository, (bool, string)>(async repo =>      // ← the ONLY added line
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid) return (false, message);
        title = title.Trim();

        var song = await repo.GetByIdAsync(id, ct);
        if (song == null) return (false, "Song not found");
        if (await repo.ExistsByTitleForArtistAsync(song.ArtistId, title, id, ct))
            return (false, "A song with this title already exists for this artist");

        song.Title = title; song.UpdatedAt = DateTime.UtcNow; /* … */
        await repo.UpdateAsync(song, ct);
        return (true, $"Song updated to '{title}'");                        // save happens once, implicitly, in UnitOfWork
    }, ct);

// ── Infra/Repository/SongRepository.cs ───────────────────────────────────────
public class SongRepository(AppDbContext db) : ISongRepository        // ctor UNCHANGED
{
    public Task<Song> GetByIdAsync(int id, CancellationToken ct)
        => db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task UpdateAsync(Song song, CancellationToken ct) { db.Songs.Update(song); return Task.CompletedTask; }
    // SaveChangesAsync: DELETED — the save is the unit of work's job, not the repository's
}
```

**API shape — decided (Revision 7): the typed overload is the primary API, not a variant.**
`ExecuteAsync<TRepo, TResult>(repo => …)` is what a service method calls for the single-repository
majority (~30 of the 35 methods in § 2a) — the dependency is named in the signature, not pulled from
a bag. The `sp => …` / `IServiceProvider` overload is **retained only as an escape hatch** for
genuinely multi-repository flows (`CreateSongWithUrlsAsync`, `GetOrCreateDefaultEventAsync`,
`ReorderQueueAsync`, and the three nested-service chains in § 6a). Resolving a dependency from an
`IServiceProvider` bag is a service-locator pattern — acceptable here only as a last resort per
standard DI guidance, and the design says so explicitly rather than presenting both overloads as
equally idiomatic. `IUnitOfWork` therefore exposes two method families (see § 8, Decision: typed
overload preferred):

```csharp
public interface IUnitOfWork
{
    // Primary API — single-repository majority. Typed, no service-locator smell.
    Task<TResult> ExecuteAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);
    Task<TResult> ExecuteReadAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);

    // Escape hatch — genuinely multi-repository / nested-service flows only.
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
}
```

**Cross-repository / multi-save flows:**
- `CreateSongWithUrlsAsync` — both repositories resolved from the one `sp`; both share the scope's
  context; `UnitOfWork` saves once. The "N3: one SaveChangesAsync commits both atomically" comment
  becomes structurally guaranteed rather than a convention. FK fixup via navigation preserved. ✔
- `GetOrCreateDefaultEventAsync` — two repositories, one scope, one save. Two saves collapse to one.
  ✔ REQ-UOW-08.
- `SetActiveEventAsync` — two updates, one save. Unchanged in shape. ✔
- `QueueRepository.ReorderAsync` — the loop of `Update` calls inside an explicit transaction stays
  inside the repository; the transaction is now scoped to a context that dies with the operation. ✔
- **`ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`,
  `QueueService.AddPersonToQueueAsync` — the ambient-scope join ships in this change, not deferred.**
  The full cross-service mutating call-site audit below (§ 6a) found **three** confirmed nested
  call sites, not one — the deferral condition ("ship the join only when a second nesting case
  appears") was already false at spec-write time. Shipping without the join means each nested call
  opens its own scoped context, reproducing the exact two-context hazard BUG-068 already exhibited.
  `CommitAsync` opens the unit of work and the inner service (`ArtistService`, `SongService`,
  `PersonService`) exposes a method that does *not* open a second one — it is avoidable here because
  everything inside the lambda shares one scope. The inner service is resolved from `sp` (or from the
  typed overload, § 8) and, when it also calls `ExecuteAsync`, the primitive honours an
  **ambient-scope rule**: `ExecuteAsync` joins an already-open unit of work instead of nesting (an
  `AsyncLocal<IServiceProvider>` flag inside `UnitOfWork`, ~5 more lines). With that, each nested
  flow becomes: create → mutate → **one** save, and the save→mutate→save sequence disappears
  entirely. ✔ REQ-UOW-09, REQ-UOW-22, REQ-UOW-23.
  **This ambient-scope rule is the single piece of hand-rolled machinery in Candidate C.** It ships
  in Wave 3 (Infra, § 10), alongside the `UnitOfWork` implementation itself, so every Wave 5 service
  rewrite can rely on it from the start rather than retrofitting it later.

### 6a. Cross-service mutating call-site audit (full, Grep+Read verified — not estimated)

Every service field of type `I*Service` was enumerated (`grep -n "private readonly I\w*Service" Services/*.cs`)
and every use of that field was inspected for calls into a *mutating* method of another service.

| Caller | Call site | Callee | Nesting depth | Notes |
|---|---|---|---|---|
| `ArtistResolutionService.CommitAsync` | `ArtistResolutionService.cs:121` | `_artistService.CreateArtistAsync` | 1 | Already flagged in the original spec draft. |
| `QueueService.AddPersonToQueueAsync` | `QueueService.cs:56` | `_personService.CreatePersonAsync` | 1 | **Not previously in the spec.** Confirmed via field `IPersonService _personService` (`QueueService.cs:16`). |
| `SongResolutionService.CommitAsync` | `SongResolutionService.cs:166` | `_songService.CreateSongAsync` | 1 | **Not previously in the spec.** |
| `SongResolutionService.CommitAsync` | `SongResolutionService.cs:184` | `_songService.CreateSongWithUrlsAsync` | 1 | Same method, alternate branch. |
| `SongResolutionService.CommitAsync` | `SongResolutionService.cs:208`, `:232` | `_songService.UpdateSongAsync` | 1 | Two call sites, two branches. |
| `SongResolutionService.CommitAsync` | `SongResolutionService.cs:260` | `_artistResolution.CommitAsync` | 1 (→ 2 via the row above) | **Compounds with the first row**: `SongResolutionService.CommitAsync` → `ArtistResolutionService.CommitAsync` → `ArtistService.CreateArtistAsync` is a **3-level nested unit-of-work chain**, the deepest case found. The `AsyncLocal` join must hold across all three levels, not just one hop. |
| `SongResolutionService.ResolveAsync` (read path) | `SongResolutionService.cs:39`, `:254` | `_artistResolution.ResolveAsync` | 1 | Read-only — no save involved; relevant to Revision 6 (`ExecuteReadAsync` also needs the join for consistent read snapshots across the two calls, though it never saves). |
| `QueueService.AddPersonToQueueAsync` (read path) | `QueueService.cs:51` | `_personService.GetPersonByNameAsync` | 1 | Read-only — no save involved; the existing-person lookup that precedes the `CreatePersonAsync` row above. Listed for consistency with the `SongResolutionService.ResolveAsync` read row above — both are read-only nested calls the ambient join must also handle correctly under `ExecuteReadAsync`. |

Services checked with an injected `I*Service` field and found to have **no** mutating cross-service
call: `NextSingerAlertService` (`_notifications` is `INotificationService`, a UI/OS notification
wrapper, not a data-writing service — `Show`/`Cancel`, no `AppDbContext` involved). `SongService`
injects `ISongKaraokeUrlService` (`_urlService`) but only calls `ExtractVideoId` (pure function, no
persistence) — not a nesting case.

**Conclusion:** three independent nested call chains (four counting the 3-level compound), all
confirmed by Grep + Read against current `develop` HEAD. Revision 2 stands: the join is not
speculative machinery for a hypothetical future case — the case already exists three times over.

**Files touched:** 1 new type + 2 registration files + ~15 service files (one-line wrap each) +
~10 repository files (delete `SaveChangesAsync`) + interfaces. **~30 files, but almost all edits are
one-liners or deletions.** Zero repository *method* signatures change.

**DRY score:** service methods **+1 line each (35 methods, derived § 2a — behavior-derived, corrects
the prior ~21-then-25 name-regex estimate; still the same line every time)**. Repository methods **0**
added lines, **0** signature changes. Net line count is *negative*: 6 pass-through implementations
deleted (~24 lines), 35 explicit `SaveChangesAsync` call sites deleted (~35 lines), against 35 added
wrapper lines + ~20 lines of `UnitOfWork` (+ ~5 lines for the `AsyncLocal` ambient-join, shipped now
per Revision 2). ✔ REQ-UOW-10.

**New-developer legibility:** **good.** `_uow.ExecuteAsync(async sp => { … })` is the boundary and it
is at the top of the method the developer is already reading. The one thing a newcomer must be told
is that the save is implicit at the end of the lambda — which is the deliberate trade for deleting 35
save call sites. Naming the method `ExecuteAsync` on `IUnitOfWork` with the XML doc above puts the
answer one hover away.

**Leaks:** the singleton captive-dependency risk is **eliminated by construction** for anything
inside the lambda, because the scope is created *by the primitive*, not injected. A singleton
injecting `IUnitOfWork` is safe (`IUnitOfWork` is itself a singleton over `IServiceScopeFactory`).
Remaining audit item: singletons that still inject repositories/services directly must be found and
converted to inject `IUnitOfWork`.

**Interceptors:** `AddDbContextFactory` preserves the `(sp, options)` overload. ✔ REQ-UOW-14.
`TransactionLogInterceptor.cs:31` reads `ChangeTracker.Entries()` at `SavingChanges` — under C it
sees exactly one operation's entities, which is what the transaction log was always meant to record.
✔ REQ-UOW-15. (Today it silently accumulates.)

**BaseRepository / families / pass-throughs:** all 6 pass-throughs deleted (**DRY win**).
`BaseRepository<T>` survives intact, minus its `SaveChangesAsync`. The two repository families are
**not** merged by this change — that remains a separate, optional follow-up, and this design does not
require it.

**Testability:** `TestDbContextFactory` keeps its SQLite-temp-file + `CollationInterceptor` shape;
tests either (a) build a small `ServiceCollection` and use the real `UnitOfWork` — highest fidelity,
recommended for the REQ-UOW-03/04 regression tests — or (b) construct a repository over a
per-test context directly for narrow repository tests. Both tracking workarounds
(`CatalogRepositoryTests.cs:66`, `ArtistRepositoryTests.cs:366`) become deletable. ✔ REQ-UOW-17.

### 6b. Save-skip mechanism for the failure-tuple path (Revision 8, resolves spec-review finding B3)

**The gap this closes.** § 9's invariants and the requirements.md failure-mode table claimed "no
partial state survives" — but as originally specified, `ExecuteAsync` saved unconditionally whenever
`body` returned without throwing. This codebase signals business failure by **returning** a tuple
`(bool success, string message, T? entity)`, never by throwing
(`code-style-reference.md § Service Return Patterns`). So a service that mutates an entity and then
returns a failure tuple on a later branch got that mutation **committed** — the "no partial state"
claim was true only for the exception path (REQ-UOW-06), never for the far more common failure-tuple
path. This is live in the audited flows: `ArtistResolutionService.CommitAsync` mutates
(`:112`, `:132`) inside branches that return success, but the method also has failure-returning
branches (`:98`, `:102`, `:123`) reached after the surrounding `switch` could in principle have taken
a mutating branch on a different call — and `SongResolutionService.CommitAsync` explicitly returns
`(false, ...)` (`:214`, `:238`) *after* an inner service call (`_songService.UpdateSongAsync`) has
already run its own nested unit of work. Once Wave 5 deletes the explicit
`_artistRepository.SaveChangesAsync(ct)` / `_songRepository.SaveChangesAsync(ct)` calls in favor of
the implicit end-of-`body` save, an unconditional save would commit any mutation that happened to run
before a failure tuple was returned.

**Return-shape survey (Grep + Read verified against current `develop` HEAD, all 35 methods in § 2a's
mutating-method table).** Three distinct shapes exist among the methods `IUnitOfWork.ExecuteAsync`
will wrap:

| Shape | Count | Examples | Success signal available? |
|---|---|---|---|
| `ValueTuple` with `bool` as the first element — `(bool success, string message[, T? entity])` | **31** | `ArtistService.CreateArtistAsync/UpdateArtistAsync/DeleteArtistsAsync`, `SongService.*`, `PersonService.*`, `VenueService.*`, `EventService.*` (all 5), `QueueServiceNew.*` (all 6), `CatalogService.*`, `SongKaraokeUrlService.AddUrlAsync/RemoveUrlAsync`, `ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`, `QueueService.AddPersonToQueueAsync` | Yes — structural (§ below) |
| Named record type with a `bool Success` property, not a `ValueTuple` | **1** | `BackupService.CreateFullBackupAsync` → `Task<BackupResult>`, `BackupResult` defined as `public record BackupResult(bool Success, string Message, string? FilePath, long FileSizeBytes)` (`Domain/ServicesInterfaces/IBackupService.cs:5`) | Only if the type opts in (§ below) |
| Bare `Task` — no return value, no signal of any kind | **3** | `QueueService.RecordParticipationAsync` (`Task`, not `Task<T>`), `QueueService.SetActiveEventAsync` (`Task`), `SongKaraokeUrlService.RecordPlayAsync` (`Task`) | No — never can be |

31 + 1 + 3 = 35, matching § 2a's total exactly. No mutating method returns a bare entity
(`Task<Song>`) or a bare `bool`/`int` with no tuple wrapper — the codebase's tuple convention is in
fact near-universal; `BackupResult` and the three void-returning methods are the only exceptions.

**Mechanism chosen: structural `ITuple` detection + opt-in marker interface + a dedicated no-signal
overload for bare-`Task` methods + a fail-closed throw for anything else — no reflection, no naming
heuristics, no new required parameter, and no silent guess in either direction (commit or skip).**

1. **ValueTuple convention (31 of 35 methods) — detected structurally, not by reflection.** Every
   C# `ValueTuple<...>`, at every arity, implements `System.Runtime.CompilerServices.ITuple`
   (a BCL interface exposing `Length` and an indexer). `ResultSignalsSuccess` pattern-matches
   `result is ITuple { Length: > 0 } t && t[0] is bool b` and reads `b`. This is a real type check
   against a documented BCL contract — not string-matching a tuple element name (element names like
   `success` are compile-time-only sugar and are erased at runtime; nothing here depends on them).
   It costs one boxing allocation for value-type `TResult`s, which is negligible next to the
   `SaveChangesAsync` round-trip it guards.
2. **Named result types opt in via `IUnitOfWorkOutcome` (1 of 35 methods today: `BackupResult`).**
   A tiny marker interface (`bool Success { get; }`) that a result record implements explicitly.
   **Under fail-closed (Revision 9) this is no longer optional:** the one-line edit to
   `BackupResult`'s declaration (`: IUnitOfWorkOutcome` appended) is a **blocking prerequisite** of
   wrapping `BackupService.CreateFullBackupAsync` in Wave 5, not a later or optional step — without
   it, the moment `BackupService` is wrapped, every call throws `InvalidOperationException` instead
   of running. Tracked as a same-wave sub-step in § 10, not performed by this design document
   itself. Chosen over reflecting for a property
   named `"Success"` by convention: an explicit interface is a compile-time-checked contract: if a
   future result type is renamed or reshaped, the compiler catches a mismatch immediately, whereas a
   property-name convention fails silently (exactly the "convention that usually detects failure" this
   design explicitly rejects, item 3 below).
3. **No-signal fallback (3 of 35 methods today: the two `Task`-returning `QueueService` methods and
   `SongKaraokeUrlService.RecordPlayAsync`) — a dedicated overload, not a runtime guess.** `IUnitOfWork`
   gains `ExecuteAsync<TRepo>(Func<TRepo, Task> body, ct)` (and its `IServiceProvider` escape-hatch
   twin) alongside the existing `Task<TResult>`-returning overloads. This overload has nothing to
   inspect — `body` returns `Task`, not `Task<TResult>` — so it **always saves** when `body` completes
   without throwing. The choice of always-save (not always-skip) is deliberate: the two failure modes
   are not symmetric. Silently discarding a real write is a correctness bug with no local signal a
   test would easily catch (the entity was mutated in memory; only a re-read from the database would
   reveal the write never landed). An unnecessary save on a method that always succeeds today is a
   no-op — SQLite writes nothing new if the change tracker has no dirty entries beyond what the
   overload's own `body` produced, and this project has no case where a `Task`-returning mutating
   method is expected to leave state unchanged. The overload's XML doc states this outcome explicitly
   (§ 6, `ExecuteAsync<TRepo>`) so it is impossible to reach this fallback by accident: a service
   method with no signal to give literally cannot call the signal-inspecting overload — the compiler
   picks the overload matching the delegate's return type.
4. **Fail-closed refusal for any other `TResult` reaching the value-returning overload (Revision 9,
   supersedes Revision 8's unconditional-save fallback for this case) — a `throw`, not a guess.** If
   a `TResult` passed to `ExecuteAsync<TRepo, TResult>` is neither an `ITuple` with a leading `bool`
   nor an `IUnitOfWorkOutcome` implementer, `ResultSignalsSuccess` throws
   `InvalidOperationException` naming the type and the two valid fixes, before any
   `SaveChangesAsync` is attempted. **Why the fallback direction changed:** Revision 8 reasoned that
   defaulting to save was "the safe direction" because it "matches this codebase's convention that
   reaching the end of body without throwing already means success." That reasoning is **rejected as
   of Revision 9** — it borrows the exception-signaling idiom's logic and applies it to the
   value-return idiom. This codebase signals business failure by **returned value**
   (`code-style-reference.md § Service Return Patterns`), not by exception; "no throw ⇒ success" is
   true only for the exception-signaling idiom, and applying it here is the same category error that
   produced BUG-068's sibling defect — an unmarked named result type (e.g. `BackupResult` before it
   implements `IUnitOfWorkOutcome`) would silently commit on a returned business failure. Fail-closed
   makes that gap loud at development time (a thrown exception on the very first call) instead of
   silent in production (a committed mutation nobody asked for).

**Rejected alternatives:**
- **Save unconditionally on an unrecognised `TResult` (Revision 8's original choice for this
  branch).** Superseded by Revision 9 (item 4 above) — see the rationale there. Kept here for the
  record: this was not a bug in Revision 8, it was a deliberate choice that a later, more careful
  reading of this codebase's failure-signaling convention showed to be the wrong direction.
- **Explicit `Func<TResult, bool> isSuccess` predicate parameter on every call.** Considered per the
  brief's option list. Rejected as the *default* API: it reintroduces ceremony proportional to call
  count (one extra lambda argument at all ~30 tuple call sites) for a signal the tuple shape already
  carries structurally — directly against REQ-UOW-10 ("at most one line of code per service method").
  Not needed as an *escape hatch* either: the two-tier fallback (ITuple → `IUnitOfWorkOutcome` →
  always-save) is exhaustive over the actual 35-method survey; there is no third shape in this
  codebase today that would need a bespoke predicate. If one appears later, adding an explicit-predicate
  overload remains open — nothing in this design forecloses it — but it is not built speculatively now.
- **Reflection over a property literally named `"Success"` (case-insensitive), with no interface.**
  Rejected: this is precisely the "convention that usually detects failure" the brief calls
  unacceptable. It would silently misfire on a future type with an unrelated bool property named
  `Success` (e.g. a DTO flag), and it gives no compile-time signal when a result type's shape changes.
  `IUnitOfWorkOutcome` gets the same coverage with a compiler-checked contract instead of a string.
- **Detect failure only (assume success is default, look for `IsFailure`/exception-like marker).**
  Rejected: symmetric with detecting success; adds no clarity, and "assume success unless proven
  otherwise" is exactly the unconditional-save behavior already found unsafe.

**Exhaustiveness — the undetectable case is explicit and refused, not guessed (item 3 of the brief;
revised Revision 9).** Every one of the 35 audited methods falls into exactly one of the three rows
above, and each row has a defined, non-ambiguous outcome:
- ValueTuple with leading `bool` → that `bool` decides (skip on `false`).
- `IUnitOfWorkOutcome` implementer → `.Success` decides (skip on `false`).
- Bare `Task` (no `TResult` at all) → the dedicated no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task>,
  ct)` overload is the only one whose signature fits; the compiler selects it, and it always saves —
  unchanged from Revision 8.
- Any other `TResult` reaching the value-returning `ExecuteAsync<TRepo, TResult>` overload that is
  neither an `ITuple` nor an `IUnitOfWorkOutcome` implementer (e.g. a bare `Task<Song>`, should one
  ever be added) falls through both `if`s in `ResultSignalsSuccess` and hits the fail-closed `throw`
  — by explicit code, not by absence of a check, and before any `SaveChangesAsync` is attempted.
  There is no ambiguous path: skipping the save happens only inside the two recognised-failure
  branches, saving happens only inside the two recognised-success branches or the dedicated
  no-signal overload, and everything else throws instead of guessing either way. Because the
  compiler selects the no-signal overload for any `Func<TRepo, Task>` body, no legitimate call site
  — genuinely signal-less or otherwise — can reach the throw; it exists solely to catch a named
  result type that forgot to implement `IUnitOfWorkOutcome`.

---

## 7. Side-by-side

| | **A — scope in one place** | **B — factory + explicit context** | **C — scope per service call** |
|---|---|---|---|
| Fixes BUG-068 structurally | ✔ | ✔ | ✔ |
| Fixes the 5 unguarded repos | ✔ | ✔ | ✔ |
| Lines added per service method | 0 | 1–2 | **1** |
| Lines/params added per repository method | 0 | **1 param × 80 methods (derived, § 2a)** | **0** |
| Repository interface churn | none | 160 signature edits (derived, § 2a) | delete `SaveChangesAsync` only |
| Files touched | ~20 (all UI) | ~40 | ~30 (mostly one-liners/deletions) |
| Repository interface method signatures touched | 0 | 80 × 2 = 160 (derived, § 2a) | 0 (deletion of `SaveChangesAsync` only, not a signature edit to surviving methods) |
| Service methods needing a wrap | 0 | 35 (derived, § 2a) | 35 (derived, § 2a) |
| Boundary visible where writes happen | ✘ (ambient) | ✔✔ | ✔ |
| 6 pass-through saves deleted | ✘ | ✔ | ✔ |
| Captive-dependency risk | **high, invisible** | none | low (audit singletons once) |
| `ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`, `QueueService.AddPersonToQueueAsync` (3 confirmed nested chains, § 6a) | works as-is | **gets worse** | one save each, cleanest |
| Test workarounds deletable | ✘ | ✔ | ✔ |
| Hand-rolled machinery | 1 small type | none | 1 small type + `AsyncLocal` ambient-join (ships now, § 6a — not optional) |

## 8. Key Decisions

### Decision: cancel the T10 re-run #6 gate on the stopgap deletion (Wave 3b) — **APPROVED by Helder (2026-08-04)**
**Chosen approach:** the hard constraint that Wave 3b (deleting the `1a114c1` `SongRepository.UpdateAsync`
stopgap, REQ-UOW-18) must not land before Helder's on-device T10 re-run #6 completes is withdrawn.
Wave 3b becomes an ordinary step of Wave 3, gated on nothing external.
**Rationale:** there is no reason to device-test the stopgap when this unit-of-work work deletes and
replaces it outright — verifying code that is about to be removed is wasted effort.
**Reversibility:** Reversible — reinstating a gate is a documentation-only change.

### Decision: adopt a scope-per-operation unit of work (Candidate C) — **APPROVED by Helder**
**Chosen approach:** `AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` + a single
`IUnitOfWork` primitive built on `IServiceScopeFactory`, exposing the typed `ExecuteAsync<TRepo,
TResult>` / `ExecuteReadAsync<TRepo, TResult>` pair as the primary API and the `IServiceProvider`
overloads as an escape hatch (Revision 7); services wrap their body in one line; repositories keep
constructor injection and lose `SaveChangesAsync`.
**Alternatives considered:** A (boundary stays ambient inside Services, violates REQ-UOW-12, and
carries an invisible captive-dependency failure mode through the two `AddSingleton` Shell types);
B (best legibility but 160 repository interface/implementation signature edits, derived § 2a, and it
makes `ArtistResolutionService.CommitAsync` strictly worse — fails constraint 3).
**Reversibility:** Reversible with effort — it is a mechanical wrap/unwrap, no schema or data impact.
**Rationale:** C is the only candidate that satisfies all four of Helder's constraints
simultaneously: correct end-state (not a hotfix), visible boundary, one-line-per-method DRY cost with
a *net negative* line count, and built-ins (`IServiceScopeFactory` + `AddDbContextFactory`) doing the
work. It also turns two existing conventions — "one save commits both atomically" and "the
transaction log records this operation" — from comments into structural guarantees.

### Decision: implicit save at the end of `ExecuteAsync` (Revision 5) — **APPROVED by Helder**
**Chosen approach:** `ExecuteAsync` calls `SaveChangesAsync` automatically after `body` returns
successfully. No explicit `uow.SaveAsync()` call appears in service code.
**Rejected alternative:** an explicit `uow.SaveAsync()` call inside every lambda — restores a marginal
amount of local visibility at the cost of re-adding the ~35 lines the implicit-save design removes,
and reintroduces a class of bug (forgetting the call) that the whole point of `IUnitOfWork` is to
eliminate.
**Reversibility:** Reversible — confined to `UnitOfWork`'s two `ExecuteAsync` bodies.
**Rationale:** the save is the *definition* of "unit of work completed successfully"; making it
automatic is what turns "one SaveChangesAsync per unit of work" from a convention services must
remember into a structural guarantee (REQ-UOW-10, REQ-UOW-12).

### Decision: two methods, `ExecuteAsync` (saves) and `ExecuteReadAsync` (never saves) (Revision 6) — **APPROVED by Helder**
**Chosen approach:** `IUnitOfWork` exposes both `ExecuteAsync<TRepo, TResult>` and
`ExecuteReadAsync<TRepo, TResult>` (plus their `IServiceProvider` escape-hatch counterparts). The
method name carries the read/write intent; a reviewer does not need to read the lambda body to know
whether a call site can mutate data.
**Alternatives considered:** a single `ExecuteAsync` used uniformly for both reads and writes, with
"no mutation happened" left as an unenforced convention for read paths — rejected because it silently
performs a wasted `SaveChangesAsync` no-op on every read call and offers no signal at the call site.
**Reversibility:** Reversible — `ExecuteReadAsync` is additive; nothing depends on its absence.
**Rationale:** resolves Open Question 4 from the prior draft. Uniformity of "everything goes through
`IUnitOfWork`" is preserved without forcing every read-only service method to carry a phantom save.

### Decision: typed overload is the primary API; the `sp => …` overload is an escape hatch only (Revision 7) — **APPROVED by Helder**
**Chosen approach:** `ExecuteAsync<TRepo, TResult>(repo => …)` / `ExecuteReadAsync<TRepo, TResult>(repo
=> …)` are what the ~30-of-35 single-repository service methods (§ 2a) call. The
`Func<IServiceProvider, Task<TResult>>` overloads are retained **only** for the genuinely
multi-repository flows (`CreateSongWithUrlsAsync`, `GetOrCreateDefaultEventAsync`, `ReorderQueueAsync`)
and the three nested-service chains found in § 6a.
**Alternatives considered:** the `sp => …` overload as the sole/primary API (former "Variant C2"
framing, where the typed overload was optional) — rejected because resolving a dependency from an
`IServiceProvider` bag is a service-locator pattern, and standard DI guidance treats service location
as a last resort, not a default. Presenting it as equally idiomatic to the typed overload would have
made the last-resort pattern the common case.
**Reversibility:** Reversible — both overloads coexist; nothing prevents converting call sites either
direction later.
**Rationale:** resolves Open Question 3 from the prior draft. Names the dependency in the method
signature for the majority case; confines the service-locator pattern to the minority of call sites
that genuinely need it, and the design says so explicitly rather than leaving it implicit.

### Decision: adopt the `AsyncLocal` ambient-scope join now, in Wave 3 — **APPROVED by Helder (Revision 2, supersedes prior deferral)**
**Chosen approach:** `UnitOfWork` carries an `AsyncLocal<IServiceProvider?>` flag; `ExecuteAsync`/
`ExecuteReadAsync` join an already-open unit of work instead of nesting a second scope. Ships as part
of the Wave 3 `UnitOfWork` implementation, not deferred to a later change.
**Rejected alternative (the prior draft's recommendation):** ship without the join and add it only "if
a second nesting case appears," backed by a hard review rule against service-to-service calls in the
interim. **Superseded** because § 6a's full audit found the second (and third, and a 3-level compound
fourth) case already exists on current `develop` HEAD — the deferral's own trigger condition was
already true when the prior draft was written. Shipping without the join would leave
`QueueService.AddPersonToQueueAsync` and `SongResolutionService.CommitAsync` reproducing BUG-068's
two-context hazard on day one of the new pattern.
**Reversibility:** Reversible with effort — removing the join reverts the three nested flows to
Candidate B's harder problem, so removal is not free once other code depends on the joined-scope
behavior.
**Rationale:** resolves Open Question 2 from the prior draft. The audit changed the facts the decision
was based on; the decision changes with them.

### Decision: reject `AddPooledDbContextFactory`
**Chosen approach:** plain `AddDbContextFactory`. **Reversibility:** Easily reversible.
**Rationale:** pooling amortises construction under server request rates; a single-user mobile app
gains nothing measurable and inherits pooled-state reset semantics as a footgun.

### Decision: reject interceptor-based masking and `ChangeTracker.Clear()`
Already rejected by Helder as production-hotfix-shaped. Recorded here so the rejection is not
re-litigated: both hide the symptom while leaving the session-lifetime context in place.

### Prerequisite (not part of this change's waves): merge the two repository families — **decided by Helder, supersedes the prior "do not merge" decision**
**Decision:** `Infra/Repository/*` and `Infra/Repositories/*` are two repository families that exist
only because of an accident across prior sessions (duplicate `IEventRepository` in both
`Domain/RepositoryInterface/` and `Domain/Interfaces/`, confirmed in § 2a's derived-count run —
`Domain/Interfaces/IEventRepository.cs` and `Domain/RepositoryInterface/IEventRepository.cs` both
exist today). Helder has decided the two families are to be **merged into one before any wave in
§ 10 runs.**
**This document does not perform the merge.** It is listed here as a hard prerequisite, to be executed
and completed as its own tracked task before Wave 0 begins. None of Waves 0—7 below assume which
family survives; whichever survives is the one every wave operates against.
**Rationale for reversing the prior "do not merge in this change" decision:** the prior decision
treated the merge as optional scope creep. Helder's view (recorded here, not re-litigated further):
running seven-plus waves of unit-of-work migration across two duplicate, drifting repository families
is itself the riskier path — the merge is now sequenced *before* the migration, as a precondition, not
folded into it as extra scope.

### Decision: `ExecuteAsync` skips the save when the result signals business failure (Revision 8) — **APPROVED by Helder 2026-08-04**
**Chosen approach:** `ExecuteAsync` inspects `body`'s returned value for this codebase's universal
failure-tuple convention and skips `SaveChangesAsync` when it signals failure (§ 6b). Detection is
structural (`ITuple`) for the 31-of-35 `ValueTuple`-returning methods, an opt-in
`IUnitOfWorkOutcome` marker interface for the 1 named-record method (`BackupResult`), and a dedicated
no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task> body, ct)` overload that **always** saves for the
3 bare-`Task` methods. Service code is unchanged — it keeps returning its ordinary tuple; nothing
about `UpdateSongAsync`'s body in § 6's worked example changes under this revision.
**Alternatives considered:** an explicit `Func<TResult, bool> isSuccess` predicate parameter on every
call (rejected — reintroduces per-call-site ceremony the tuple shape already carries structurally,
against REQ-UOW-10; not needed given the 35-method survey is fully covered by the two-tier fallback);
reflection over a property named `"Success"` by convention with no interface (rejected — exactly the
"convention that usually detects failure" ruled unacceptable; `IUnitOfWorkOutcome` gets the same
coverage with a compiler-checked contract).
**Reversibility:** Reversible — confined to `UnitOfWork.ExecuteAsync`'s two `Task<TResult>` overloads
and the new `ResultSignalsSuccess` helper; the no-signal overload is additive.
**Rationale:** resolves spec-review finding B3. `§ 9`'s "no partial state survives" claim was true
only for the exception path (REQ-UOW-06) before this revision — a mutation followed by a returned
failure tuple was silently committed, because the codebase's universal failure idiom is the tuple, not
an exception (`code-style-reference.md § Service Return Patterns`). The UoW must understand that idiom
or the implicit save Revision 5 chose is unsafe on the majority of this codebase's write paths. This
is live today in `ArtistResolutionService.CommitAsync` (`:112`, `:132`) and
`SongResolutionService.CommitAsync` (mutates via nested calls, then returns `(false, …)` on later
branches, `:214`/`:238`) — the two flows named in the finding.

### Decision: fail-closed on an unrecognised `TResult` reaching the value-returning overload (Revision 9) — **APPROVED by Helder 2026-08-04**
**Chosen approach:** in `ResultSignalsSuccess`, branch 3 ("no recognised signal") no longer defaults
to save unconditionally. It throws `InvalidOperationException` naming the two valid fixes (implement
`IUnitOfWorkOutcome`, or use the no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task>, ct)` overload).
The dedicated no-signal overload itself is unaffected — it still always saves for the 3 genuinely
signal-less methods (`QueueService.RecordParticipationAsync`, `QueueService.SetActiveEventAsync`,
`SongKaraokeUrlService.RecordPlayAsync`), because the compiler selects that overload on the
`Func<TRepo, Task>` delegate type — no legitimate call site can reach the throw.
**Alternatives considered:** keep Revision 8's unconditional-save fallback (rejected — see § 6b item
4 rationale); an explicit `isSuccess` predicate parameter (rejected earlier, in Revision 8, for the
same REQ-UOW-10 ceremony reason, and nothing about fail-closed changes that).
**Reversibility:** Reversible — confined to `ResultSignalsSuccess`'s branch 3.
**Rationale:** Revision 8's "reaching the end without throwing means success" argument belongs to the
exception-signaling idiom. This codebase signals business failure by **returned value**
(`code-style-reference.md § Service Return Patterns`), never by throwing for expected business
failures. Applying exception-path reasoning to the value-return path is the same category error that
produced BUG-068's sibling defect: a named result type that forgets to implement
`IUnitOfWorkOutcome` would silently commit on a returned failure. Fail-closed converts that into a
loud, immediate `InvalidOperationException` on the first call — a development-time failure instead of
a silent production one — and makes `BackupResult : IUnitOfWorkOutcome` a mandatory, same-wave,
blocking prerequisite of wrapping `BackupService` in Wave 5 (§ 10) rather than an optional or later
step.

### Decision: no `CreateExecutionStrategy` / retry policy
**Rationale:** SQLite is local; there are no transient network faults to retry. Adding a strategy
would also constrain the manual transaction in `QueueRepository.ReorderAsync`.

## 9. Invariants & postconditions

- After any service write call, zero `AppDbContext` instances created by that call remain undisposed.
- At most one `SaveChangesAsync` executes per unit of work (excepting the documented
  REQ-UOW-08 flows, and excepting nested calls that join the ambient scope rather than opening a new
  one, § 6a).
- No `ChangeTracker` entry survives a unit of work.
- No singleton holds a repository, a service that writes, or an `AppDbContext`.
- **Save-skip on failure (Revision 8, § 6b):** `SaveChangesAsync` executes only when `body`'s
  returned result signals success per § 6b's two recognised shapes (`ITuple` with leading `bool`, or
  `IUnitOfWorkOutcome.Success`) — a mutation followed by a returned failure tuple does **not**
  persist. For the 3 genuinely signal-less methods, the dedicated no-signal overload (delegate type
  `Func<TRepo, Task>`, nothing to inspect) always saves; this is the documented default (§ 6b
  "no-signal fallback"), never a silent guess, and is reachable only through that overload. This
  closes the exception-only reading of REQ-UOW-06: "no partial state survives" now holds for both the
  throw path and the far more common failure-tuple path.
- **Fail-closed on an unrecognised `TResult` (Revision 9, § 6b):** on the value-returning
  `ExecuteAsync<TRepo, TResult>` overload, a `TResult` that is neither an `ITuple` with a leading
  `bool` nor an `IUnitOfWorkOutcome` implementer is never assumed to have succeeded — `ExecuteAsync`
  throws `InvalidOperationException` before any save is attempted. No unit of work may commit a
  mutation whose result type it could not interpret.

## 10. Migration plan (DRY Onion: Domain → Infra → Services → UI)

**Prerequisite (outside these waves):** the `Infra/Repository/*` / `Infra/Repositories/*` repository-family
merge (§ 8, Prerequisite decision) completes first. Every wave below assumes one merged family.

Sequential-only files (`workflow.md § Sequential-only file registry`): **`MauiProgram.cs`**,
**`AppDbContext.cs`**, and each spec `tasks.md` — never concurrent writers. `ServiceCollectionExtensions.cs`
should be treated as sequential-only for this change for the same reason.

| Wave | Layer | Work | Parallel? |
|---|---|---|---|
| 0 | Tests (RED) | Write the REQ-UOW-03/04 BUG-068 regression tests, plus new atomicity tests for `SongResolutionService.CommitAsync` and `QueueService.AddPersonToQueueAsync` (Revision 2b), against **current `develop` HEAD, unchanged code**. Run them and confirm each FAILS for its exact stated reason (below), not merely "fails" — a test that goes red for the wrong reason is not evidence (`bug-tracking.md`: Critical ⇒ mandatory failing-test-first, no exceptions). This wave produces no production-code change. **Expected failure per test:** REQ-UOW-03 (BUG-068) — the create→read→update sequence on `SongRepository` throws `InvalidOperationException: ... already being tracked` on the second save, because the shared session-lifetime `AppDbContext` still tracks the entity from the first save. REQ-UOW-04 — the same exception, once per repository family parameterisation, for the same reason. REQ-UOW-22 (`SongResolutionService.CommitAsync`, 3-level nested chain) — **two** assertions are exercised and must be shown failing independently: (a) the happy-path Given/When/Then may already PASS the "no InvalidOperationException" assertion on current HEAD, because each nested call currently saves eagerly per inner call rather than sharing a context — so this assertion alone is not evidence of RED; (b) the added fault-injection Given/When/Then (`requirements.md` REQ-UOW-22) is the assertion expected to FAIL today, because nothing rolls back the outer `SongService` write when the inner `ArtistService.CreateArtistAsync` call throws — each nested call already committed its own `SaveChangesAsync` independently, so partial state (a `Song` row with no matching `Artist`) survives the fault, which is the opposite of "all-or-nothing". REQ-UOW-23 (`QueueService.AddPersonToQueueAsync`) — same shape: the plain happy-path assertion may already pass today; the fault-injection Given/When/Then is the one expected to fail, because the nested `PersonService.CreatePersonAsync` save already committed independently of the outer flow, so a `Person` row survives a fault that should have rolled back the whole unit of work. | no — must complete and be observed RED for the stated reason before Wave 1 |
| 1 | Docs | Amend `code-style-reference.md § DI Registration Conventions` (REQ-UOW-19, `amend:` + changelog). Land before any code so subagents read the corrected rule. | no |
| 2 | Domain/Contracts | Introduce `IUnitOfWork` (both the typed primary API and the `IServiceProvider` escape hatch, § 6/§ 8); remove `SaveChangesAsync` from every repository **interface**. | no (single file set) |
| 3 | Infra | `UnitOfWork` implementation, **including the `AsyncLocal` ambient-scope join** (§ 6a — ships now, not deferred). Delete the 6 pass-through implementations. | partly — one agent per repository family |
| 3b | Infra | Delete the `1a114c1` stopgap guard in `SongRepository.UpdateAsync` (REQ-UOW-18), as an ordinary step of Wave 3 — no external gate (Helder cancelled the T10 re-run #6 gate 2026-08-04; § 8). | partly — one agent per repository family, same as Wave 3 |
| 4 | Composition | `MauiProgram.cs`: `AddDbContext` → `AddDbContextFactory(…, Scoped)`; register `IUnitOfWork`; remove the duplicate `IAppInfo` (REQ-UOW-21). Verify `App.xaml.cs:35,:54` scopes still resolve. | **no — sequential-only** |
| 5 | Services | Wrap each of the **35** service methods (behavior-derived, § 2a — includes `BackupService.CreateFullBackupAsync`, `EventService.StartEventAsync`/`PauseEventAsync`/`ResumeEventAsync`/`FinishEventAsync`, `QueueService.SetActiveEventAsync`, and `QueueServiceNew.EnqueueSingerAsync`/`RegisterParticipationAsync`/`StopPerformanceAsync`/`MarkAbsentAsync` — all missed by the prior name-regex count) in `ExecuteAsync`/`ExecuteReadAsync`; delete the corresponding `SaveChangesAsync` call sites; re-shape `ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`, and `QueueService.AddPersonToQueueAsync` to use the ambient join (REQ-UOW-09, REQ-UOW-22, REQ-UOW-23). `QueueService.RecordParticipationAsync` (the public method) is where the wrap goes for the private `GetOrCreateDefaultEventAsync` — see note below. A wave that only covers the prior 25-method list leaves 10 write paths (including `BackupService`, an entire file missing from the prior scope) still using the session-lifetime context, reproducing BUG-068 on those paths. **Save-skip wiring (Revision 8, § 6b) — `BackupResult : IUnitOfWorkOutcome` is a blocking, same-wave prerequisite (Revision 9), not a later or optional step:** `BackupService.CreateFullBackupAsync`'s wrap MUST NOT be committed before `: IUnitOfWorkOutcome` is appended to `BackupResult`'s declaration (`Domain/ServicesInterfaces/IBackupService.cs:5`) — under fail-closed (§ 6b, § 8 Revision 9), wrapping `BackupService` with an unmarked `BackupResult` makes every call throw `InvalidOperationException` immediately, so the marker-interface edit and the service wrap land together, in this order, within Wave 5, never split across sessions or deferred; `QueueService.RecordParticipationAsync`/`SetActiveEventAsync` and `SongKaraokeUrlService.RecordPlayAsync` wrap via the no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task> body, ct)` overload (always saves, § 6b); every other method wraps via the typed `Task<TResult>` overload and gets save-skip for free from the `ITuple` structural check — no per-method opt-in needed. | yes — one agent per service, no file overlap; the `BackupResult` marker edit + `BackupService` wrap are one atomic sub-task, not splittable |
| 6 | UI | Audit singletons for captive dependencies (`AppShellViewModel`, `AppShell`, `MauiProgram.cs:109-110`); convert any that inject repositories/services. | yes |
| 7 | Tests (GREEN + cleanup) | Confirm the Wave-0 regression tests now PASS. `TestDbContextFactory` alignment; delete the workarounds at `CatalogRepositoryTests.cs:66` and `ArtistRepositoryTests.cs:366`; fix the stale `GetByIdAsync` "Tracked query" comment (REQ-UOW-20). | partly |

**`QueueService.GetOrCreateDefaultEventAsync` is `private`, reachable only via
`RecordParticipationAsync`.** It cannot independently open a unit of work as a standalone wrap target
 — the wrap in Wave 5 goes on the **public** `RecordParticipationAsync`, and
`GetOrCreateDefaultEventAsync` runs inside that same `ExecuteAsync` lambda as a plain private helper
call, sharing the one scope and the one implicit save (REQ-UOW-08).

**Branch note:** the stopgap lives on `feat/inline-artist-create` (`1a114c1`). Wave 3b — which
deletes it — has no external gate; it lands as an ordinary part of Wave 3 (the T10 re-run #6 gate
was cancelled by Helder 2026-08-04, § 8).

**Testing tier (`testing.md`):** Level **A** — `UnitOfWork`, and every service method re-shaped in
Wave 5, are business-logic/state-mutation paths requiring full Red→Green→Refactor (Wave 0 is the Red;
Wave 7 confirms Green). Repository edits in Wave 3 are Level **B**. Wave 4 registration edits are
Level **C**, covered by the existing BUG-021 DI regression tests plus one new composition test for
REQ-UOW-01.

## 11. Open questions for Helder

All five prior open questions are resolved (§ 8 decisions record the answers and rationale):

1. ~~Implicit save~~ — resolved: implicit, no explicit `uow.SaveAsync()` (Revision 5).
2. ~~Ambient-scope join~~ — resolved: adopted now, in Wave 3, not deferred (Revision 2).
3. ~~C vs C2~~ — resolved: typed overload is primary; `sp => …` is an escape hatch only (Revision 7).
4. ~~Read paths~~ — resolved: separate `ExecuteReadAsync` that never saves (Revision 6).
5. ~~Repository-family merge~~ — resolved: in scope, but as a **prerequisite** completed before Wave 0
   of this change, not as a follow-up and not folded into these waves (§ 8, Prerequisite decision).
6. ~~Spec-review finding B3 (unconditional save vs. this codebase's failure-tuple convention)~~ —
   resolved: `ExecuteAsync` skips the save when the result signals failure, via structural `ITuple`
   detection + `IUnitOfWorkOutcome` opt-in + an always-saves no-signal overload for bare-`Task`
   methods (Revision 8, § 6b). No longer OPEN — pending architect decision.
7. ~~Fallback direction for an unrecognised `TResult` on the value-returning overload~~ — resolved
   2026-08-04: fail-closed (throw `InvalidOperationException`), not fail-open (save unconditionally),
   superseding Revision 8's original choice for this one branch (Revision 9, § 6b / § 8).

No open questions remain for this design. The next step is task-log/tasks.md breakdown against the
wave table in § 10.
