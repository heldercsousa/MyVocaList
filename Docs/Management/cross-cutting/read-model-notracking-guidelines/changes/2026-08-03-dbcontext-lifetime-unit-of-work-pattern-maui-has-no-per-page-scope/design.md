# Design — DbContext lifetime & unit-of-work pattern

> **Design proposal — Helder decides.** Three candidates are presented with the same worked example
> so they can be compared side by side. A recommendation is given at the end; it is a recommendation,
> not a settled choice. No production code has been written for this change.
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

**Service methods that would need a UoW wrap** (public mutating methods — `Create*`/`Update*`/`Delete*`/
`Remove*`/`Add*`/`Record*`/`Commit*`/`Reorder*` — found across `Services/*.cs`):

```
$ grep -nE '^\s*public async Task(<\([^)]*bool[^)]*\)>)? \w*(Create|Update|Delete|Remove|Add|Record|Commit|Reorder|Complete|Reset|Toggle|Merge|Save)\w*Async\(' Services/*.cs
```

25 methods across 11 files: `ArtistResolutionService` (1), `ArtistService` (3), `CatalogService` (2),
`EventService` (1), `PersonService` (3), `QueueService` (2), `QueueServiceNew` (2),
`SongKaraokeUrlService` (2 mutating + `RecordPlayAsync`, counted = 3), `SongResolutionService` (1),
`SongService` (4), `VenueService` (3).

**Correction to prior draft:** the earlier "~120 signature edits" figure for Candidate B and the
"~60+ method signatures" figure in § 5 were estimates. Recomputed from the 80-method total above:
Candidate B adds one `AppDbContext` parameter to every one of the **80** repository interface
methods (interface + implementation = **160** edits, not counting the ~10 repository class files
touched once each) plus a `db`-threading change to each of the **25** service methods — the
side-by-side table in § 7 below uses these corrected numbers.

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

**DRY score:** service methods **+1 to +2 lines each × 25 methods (derived, § 2a) = ~35-50 lines**;
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
    /// owning one AppDbContext. Saves once, implicitly, after <paramref name="body"/> returns
    /// successfully (Revision 5 — no explicit uow.SaveAsync() call in service code). Disposes the
    /// context on exit.</summary>
    Task<TResult> ExecuteAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>Same as <see cref="ExecuteAsync{TRepo,TResult}"/> but never saves (Revision 6). Use
    /// for read-only service methods so the method name itself carries the intent — a reviewer
    /// reading the call site does not need to open the body to know whether it writes.</summary>
    Task<TResult> ExecuteReadAsync<TRepo, TResult>(Func<TRepo, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>Escape hatch (Revision 7). Use ONLY for genuinely multi-repository flows or flows
    /// that must call into another service (§ 6a's three nested chains) — resolving from
    /// <see cref="IServiceProvider"/> is a service-locator pattern and is a last resort, not the
    /// default. Saves once, implicitly, same as the typed overload.</summary>
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

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
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync(ct);   // implicit save
            return result;
        }
        finally { _ambientScope.Value = null; }
    }

    public async Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        if (_ambientScope.Value is { } joined) return await body(joined);
        await using var scope = scopeFactory.CreateAsyncScope();
        return await body(scope.ServiceProvider);   // no SaveChangesAsync — read-only, per Revision 6
    }
}
```

**Inputs / Outputs / Preconditions — `IUnitOfWork.ExecuteAsync<TRepo, TResult>` (primary API):**
- **Inputs:** `body` — a delegate receiving the resolved `TRepo` instance, returning `Task<TResult>`;
  `ct` — optional cancellation token, defaults to `default`.
- **Outputs:** `TResult` returned by `body` on success. On failure inside `body`, whatever the body
  itself returns per `code-style-reference.md § Service Return Patterns` (a `(false, message)` tuple)
  — `ExecuteAsync` itself does not catch or translate exceptions; an exception from `body` propagates
  after the `using` disposes the scope (no partial state survives, REQ-UOW-06).
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
majority (~20 of the 25 methods in § 2a) — the dependency is named in the signature, not pulled from
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

**DRY score:** service methods **+1 line each (25 methods, derived § 2a — corrects the prior ~21
estimate; still the same line every time)**. Repository methods **0** added lines, **0** signature
changes. Net line count is *negative*: 6 pass-through implementations deleted (~24 lines), 25
explicit `SaveChangesAsync` call sites deleted (~25 lines), against 25 added wrapper lines + ~20
lines of `UnitOfWork` (+ ~5 lines for the `AsyncLocal` ambient-join, shipped now per Revision 2).
✔ REQ-UOW-10.

**New-developer legibility:** **good.** `_uow.ExecuteAsync(async sp => { … })` is the boundary and it
is at the top of the method the developer is already reading. The one thing a newcomer must be told
is that the save is implicit at the end of the lambda — which is the deliberate trade for deleting 25
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

---

## 7. Side-by-side

| | **A — scope in one place** | **B — factory + explicit context** | **C — scope per service call** |
|---|---|---|---|
| Fixes BUG-068 structurally | ✔ | ✔ | ✔ |
| Fixes the 5 unguarded repos | ✔ | ✔ | ✔ |
| Lines added per service method | 0 | 1–2 | **1** |
| Lines/params added per repository method | 0 | **1 param × ~60 methods** | **0** |
| Repository interface churn | none | ~120 signature edits | delete `SaveChangesAsync` only |
| Files touched | ~20 (all UI) | ~40 | ~30 (mostly one-liners/deletions) |
| Repository interface method signatures touched | 0 | 80 × 2 = 160 (derived, § 2a) | 0 (deletion of `SaveChangesAsync` only, not a signature edit to surviving methods) |
| Service methods needing a wrap | 0 | 25 (derived, § 2a) | 25 (derived, § 2a) |
| Boundary visible where writes happen | ✘ (ambient) | ✔✔ | ✔ |
| 6 pass-through saves deleted | ✘ | ✔ | ✔ |
| Captive-dependency risk | **high, invisible** | none | low (audit singletons once) |
| `ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`, `QueueService.AddPersonToQueueAsync` (3 confirmed nested chains, § 6a) | works as-is | **gets worse** | one save each, cleanest |
| Test workarounds deletable | ✘ | ✔ | ✔ |
| Hand-rolled machinery | 1 small type | none | 1 small type + `AsyncLocal` ambient-join (ships now, § 6a — not optional) |

## 8. Key Decisions

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
amount of local visibility at the cost of re-adding the ~25 lines the implicit-save design removes,
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
=> …)` are what the ~20-of-25 single-repository service methods (§ 2a) call. The
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

## 10. Migration plan (DRY Onion: Domain → Infra → Services → UI)

**Prerequisite (outside these waves):** the `Infra/Repository/*` / `Infra/Repositories/*` repository-family
merge (§ 8, Prerequisite decision) completes first. Every wave below assumes one merged family.

Sequential-only files (`workflow.md § Sequential-only file registry`): **`MauiProgram.cs`**,
**`AppDbContext.cs`**, and each spec `tasks.md` — never concurrent writers. `ServiceCollectionExtensions.cs`
should be treated as sequential-only for this change for the same reason.

| Wave | Layer | Work | Parallel? |
|---|---|---|---|
| 0 | Tests (RED) | Write the REQ-UOW-03/04 BUG-068 regression tests, plus new atomicity tests for `SongResolutionService.CommitAsync` and `QueueService.AddPersonToQueueAsync` (Revision 2b), against **current `develop` HEAD, unchanged code**. Run them and confirm they FAIL for the expected reason (`bug-tracking.md`: Critical ⇒ mandatory failing-test-first, no exceptions). This wave produces no production-code change. | no — must complete and be observed RED before Wave 1 |
| 1 | Docs | Amend `code-style-reference.md § DI Registration Conventions` (REQ-UOW-19, `amend:` + changelog). Land before any code so subagents read the corrected rule. | no |
| 2 | Domain/Contracts | Introduce `IUnitOfWork` (both the typed primary API and the `IServiceProvider` escape hatch, § 6/§ 8); remove `SaveChangesAsync` from every repository **interface**. | no (single file set) |
| 3 | Infra | `UnitOfWork` implementation, **including the `AsyncLocal` ambient-scope join** (§ 6a — ships now, not deferred). Delete the 6 pass-through implementations. | partly — one agent per repository family |
| 3b | Infra (gated) | Delete the `1a114c1` stopgap guard in `SongRepository.UpdateAsync` (REQ-UOW-18). **Hard constraint: this sub-wave MUST NOT land before Helder's on-device T10 re-run #6 completes.** Sequenced as its own commit, separable from the rest of Wave 3, so the rest of the migration is not blocked waiting on the re-run. | no — gated on an external event, not on other waves |
| 4 | Composition | `MauiProgram.cs`: `AddDbContext` → `AddDbContextFactory(…, Scoped)`; register `IUnitOfWork`; remove the duplicate `IAppInfo` (REQ-UOW-21). Verify `App.xaml.cs:35,:54` scopes still resolve. | **no — sequential-only** |
| 5 | Services | Wrap each of the 25 service methods (derived, § 2a) in `ExecuteAsync`/`ExecuteReadAsync`; delete the corresponding `SaveChangesAsync` call sites; re-shape `ArtistResolutionService.CommitAsync`, `SongResolutionService.CommitAsync`, and `QueueService.AddPersonToQueueAsync` to use the ambient join (REQ-UOW-09, REQ-UOW-22, REQ-UOW-23). `QueueService.RecordParticipationAsync` (the public method) is where the wrap goes for the private `GetOrCreateDefaultEventAsync` — see note below. | yes — one agent per service, no file overlap |
| 6 | UI | Audit singletons for captive dependencies (`AppShellViewModel`, `AppShell`, `MauiProgram.cs:109-110`); convert any that inject repositories/services. | yes |
| 7 | Tests (GREEN + cleanup) | Confirm the Wave-0 regression tests now PASS. `TestDbContextFactory` alignment; delete the workarounds at `CatalogRepositoryTests.cs:66` and `ArtistRepositoryTests.cs:366`; fix the stale `GetByIdAsync` "Tracked query" comment (REQ-UOW-20). | partly |

**`QueueService.GetOrCreateDefaultEventAsync` is `private`, reachable only via
`RecordParticipationAsync`.** It cannot independently open a unit of work as a standalone wrap target
 — the wrap in Wave 5 goes on the **public** `RecordParticipationAsync`, and
`GetOrCreateDefaultEventAsync` runs inside that same `ExecuteAsync` lambda as a plain private helper
call, sharing the one scope and the one implicit save (REQ-UOW-08).

**Branch note / hard constraint:** the stopgap lives on `feat/inline-artist-create` (`1a114c1`). Wave
3b — which deletes it — **must not land before Helder's on-device T10 re-run #6 completes.** This is
a hard constraint on sequencing, not a soft preference: landing early blocks the re-run.

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

No open questions remain for this design. The next step is task-log/tasks.md breakdown against the
wave table in § 10.
