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
ServiceCollectionExtensions.cs  25 × AddScoped (all repos + all services — corrected 2026-08-04,
                                4th-pass spec review non-blocking #6: verified by grep count against
                                `Extensions/ServiceCollectionExtensions.cs`, not 23 as previously stated)
                                → 27 de-facto singletons (25 + the 2 in MauiProgram.cs), all sharing
                                the one context

Repositories: ctor-inject AppDbContext directly. IDbContextFactory used nowhere.
Saves:        THREE categories, not two (corrected below, BL-1) — 6 pass-through SaveChangesAsync
              repository methods + 6 SaveChangesAsync calls embedded inside repository MUTATOR
              methods + 35 service-level call sites (§ 2a "35 methods"). No choke point.
Families:     Infra/Repository/*  (BaseRepository<T> descendants + 5 standalone)
              Infra/Repositories/* (EventRepository, QueueRepository)
Correct today: App.xaml.cs:35, :54 — the only two manual scopes, both correct.
```

**Correction (BL-1, third-pass spec review):** the prior draft's "6 pass-through SaveChangesAsync +
~21 call sites in services. No choke point." understated the save landscape by omitting a third
category entirely: repository **mutator** methods (not pass-throughs — methods that also do real
work, like `AddAsync`/`UpdateAsync`) that call `SaveChangesAsync` **inside themselves**, verified via
`grep -rn "SaveChangesAsync" Infra/` against current `develop` HEAD and cross-checked against every
non-`ExecuteDeleteAsync` mutator in both repository families:

| Site | Method | Family |
|---|---|---|
| `Infra/Repository/EventRepository.cs:37` | `SetActiveEventAsync` | `Infra/Repository/*` |
| `Infra/Repositories/QueueRepository.cs:56` | `AddAsync` | `Infra/Repositories/*` |
| `Infra/Repositories/QueueRepository.cs:67` | `UpdateAsync` | `Infra/Repositories/*` |
| `Infra/Repositories/QueueRepository.cs:93` | `ReorderAsync` (inside its own `BeginTransactionAsync`/`CommitAsync`) | `Infra/Repositories/*` |
| `Infra/Repositories/EventRepository.cs:66` | `AddAsync` | `Infra/Repositories/*` |
| `Infra/Repositories/EventRepository.cs:77` | `UpdateAsync` | `Infra/Repositories/*` |

**Six sites, verified — not seven.** No further embedded-save sites exist in either family; every
other mutator (`BaseRepository<T>.AddAsync`/`UpdateAsync`/`DeleteAsync`/`DeleteRangeAsync`, and every
`ExecuteDeleteAsync`-based delete, which bypasses the change tracker and needs no save) leaves the save
to its caller, matching the 6 documented pass-throughs. Both repository `DeleteAsync` methods in the
`Infra/Repositories/*` family use `ExecuteDeleteAsync`, which needs no `SaveChangesAsync` at all.

> **Superseded by D12 (non-blocking #1/#2, 4th-pass spec review):** every "Wave 3" reference in this
> BL-1 analysis below predates both D11's Phase restructuring AND D12's Queue/Event exclusion. All 6
> embedded-mutator saves this analysis discusses live in `EventRepository` (either family) or
> `QueueRepository` (D12 item 4, verified) — **entirely excluded from this spec's Phases 0–4+**, not
> merely renamed to a Phase. This BL-1 analysis is retained as-is because it correctly identifies the
> live risk (§ 8 D12 item 3's "LIVE RISK" paragraph restates it), but no phase of *this* spec performs
> the removal it describes — it is owned by
> `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`.

**Consequences this correction requires fixing, all addressed below (superseded by D12 — see note
above; retained for the risk analysis, not as an active task list for this spec):**
- REQ-UOW-11 (`requirements.md`) previously named only the 6 pass-throughs; it is amended to require
  removal of all 6 embedded-mutator saves above as well — **out of scope under D12**, owned by the
  deferred item, not by any phase of this spec.
- The Revision 8/9 save-skip guarantee (§ 6b) was **void** for every `QueueServiceNew` method touching
  `QueueRepository`, and for `EventService`/`QueueService` methods touching either `EventRepository` —
  the write was committed **inside the repository**, before the service's failure tuple was even
  constructed, so `IUnitOfWork.ExecuteAsync`'s save-skip logic never had a chance to run. Fixed by
  deleting these embedded saves in Wave 3, so the write only happens once, at the `IUnitOfWork`
  boundary, where save-skip can see it.
- § 9's "at most one `SaveChangesAsync` executes per unit of work" invariant was violated by
  construction until these 6 sites are removed — a service call spanning, say,
  `QueueRepository.AddAsync` followed by the (pre-D10-fix) `IUnitOfWork` implicit save was really two
  saves, one inside the repository and one at the boundary.
- REQ-UOW-08 names `EventRepository.SetActiveEventAsync` and `QueueRepository.ReorderAsync` as
  "one-save flows" — true only after Wave 3 removes their embedded saves; before that, nothing in the
  original draft removed them, so the "one save" claim was aspirational, not yet true of the code.
- **`QueueServiceNew.EnqueueSingerAsync` (`QueueServiceNew.cs:86`) has NO service-level save today** —
  it calls `_queueRepository.AddAsync` and relies entirely on that method's embedded save
  (`QueueRepository.cs:56`). Removing the embedded save in Wave 3 without adding the `IUnitOfWork` wrap
  in the *same* migration silently stops persisting every enqueue — this is flagged as a CRITICAL
  CAUTION in the Wave 5 row (§ 10) precisely because the failure mode is silent (no exception, no test
  failure unless a test re-reads the row after the call).

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

> **Later correction (§ 8, Decision D12, 2026-08-04):** the 35-method/80-method totals above are
> **all-inclusive** figures computed before Helder excluded Queue/Event entity code from this spec's
> scope. The **in-scope** subset actually migrated by this spec's phases (§ 10) is **21 methods**
> (16 single-repository / 5 multi-repository) — see § 8 D12 items 1–2 for the file-by-file
> re-derivation. The 35/80/26/9 numbers in this section and § 6 remain correct as all-inclusive totals
> and are not edited in place; treat them as superseded-in-scope, not wrong, per D12.

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

Keep `AddDbContext` (Scoped) and all 27 `AddScoped` registrations (corrected count, non-blocking #6).
Introduce a single well-named
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

> **Superseded-in-scope note (non-blocking #2, 4th-pass spec review):** the `GetOrCreateDefaultEventAsync`/
> `SetActiveEventAsync`/`QueueRepository.ReorderAsync` bullets below are Queue/Event flows, excluded
> from this spec's phases by D12 (§ 8) — retained as Candidate B worked examples, not an active task
> list. See § 8 D12 for the authoritative scope statement.

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
// all 27 AddScoped registrations: UNCHANGED (corrected count, non-blocking #6)
```

```csharp
// ── The one new type (~30 lines with the ambient-join, written once, never repeated) ────
// Revision 10 (§ 8, APPROVED by Helder 2026-08-04): a SINGLE value-returning form and a SINGLE
// no-signal form. There is no typed TRepo overload and no "escape hatch" — see § 8 Revision 10 for
// why Revision 7's typed-overload-as-primary-API design is superseded.
public interface IUnitOfWork
{
    /// <summary>Runs <paramref name="body"/> inside a fresh (or ambiently-joined, § 6a) DI scope
    /// owning one AppDbContext, resolving whatever repositories/services the body needs from the
    /// supplied <see cref="IServiceProvider"/>. Disposes the scope on exit.
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
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

    /// <summary>No-signal form (Revision 8, § 6b). Use for service methods that return bare
    /// <see cref="Task"/> — the only IN-SCOPE example is <c>SongKaraokeUrlService.RecordPlayAsync</c>
    /// (corrected 2026-08-04, non-blocking #4, 4th-pass spec review: <c>QueueService.RecordParticipationAsync</c>
    /// and <c>QueueService.SetActiveEventAsync</c> are EXCLUDED Queue/Event methods, D12 — do not wrap
    /// them under this spec; an implementor copying this doc comment verbatim would be wrapping
    /// out-of-scope code) — which
    /// have no failure tuple to inspect. <b>Always saves</b> when <paramref name="body"/> returns
    /// without throwing; this is the safe default for a method with no success/failure signal at all
    /// (§ 6b "no-signal fallback").</summary>
    Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);

    /// <summary>Same as <see cref="ExecuteAsync{TResult}"/> but never saves (Revision 6). Use
    /// for read-only service methods so the method name itself carries the intent — a reviewer
    /// reading the call site does not need to open the body to know whether it writes.</summary>
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
}

public sealed class UnitOfWork(IServiceScopeFactory scopeFactory) : IUnitOfWork
{
    // AsyncLocal flag joins an already-open unit of work instead of nesting a second scope —
    // ships now per Revision 2, not deferred. Holds across the 3-level chain found in § 6a
    // (SongResolutionService.CommitAsync → ArtistResolutionService.CommitAsync → ArtistService.CreateArtistAsync).
    //
    // Revision 12 (§ 8, supersedes Revision 11 — Helder's decision 2026-08-04): ONLY a write
    // publishes an ambient scope. A read never does. This closes 4th-pass finding BL-E (a write
    // nested in a read joined a scope that never saves, silently discarding the mutation) without
    // a guard, a flag, or an exception — the write simply opens its own scope and saves.
    //   write -> read : the read JOINS the write's scope (the lookup-before-persist case; the
    //                   outer write still saves normally).
    //   read  -> write: the write opens its OWN scope and saves. No silent loss, no throw.
    private static readonly AsyncLocal<IServiceProvider?> _ambientScope = new();

    public async Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
    {
        // Only a write ever publishes an ambient scope (Revision 12), so joining one is always
        // joining another write — it will save.
        if (_ambientScope.Value is { } joined)
            return await body(joined);   // join, don't nest — both are writes

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
    // (bare Task): the only IN-SCOPE example is RecordPlayAsync (SongKaraokeUrlService).
    // RecordParticipationAsync/SetActiveEventAsync are EXCLUDED QueueService methods (D12) — not
    // wrapped by this spec (corrected 2026-08-04, non-blocking #4).
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
        // A read JOINS an ambient write scope when there is one — this is the common
        // lookup-before-persist case, and the outer write still saves normally.
        if (_ambientScope.Value is { } joined) return await body(joined);

        // Standalone read: open a scope but do NOT publish it as ambient (Revision 12). A read
        // never saves, so anything nested inside it must not be lured into joining it.
        await using var scope = scopeFactory.CreateAsyncScope();
        return await body(scope.ServiceProvider);
        // no SaveChangesAsync — read-only, per Revision 6.
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
>
> **Second bug fix (Revision 12, § 8 — Helder's decision 2026-08-04, supersedes Revision 11):** the fix
> above stopped the second-scope problem but introduced a worse, silent one: once a nested
> `ExecuteAsync` correctly joins the read scope instead of opening a second one, it takes the **join
> branch**, which returns `await body(joined)` with **no `SaveChangesAsync` call at all** — because the
> read scope that opened the ambient context never saves, by design (Revision 6). A write nested inside
> a read was therefore silently discarded: no exception, no failing test, the same failure shape as
> BUG-071.
>
> Revision 11 fixed this with a read/write flag on the ambient scope plus a fail-closed throw. **That
> mechanism is withdrawn.** Helder's judgement (2026-08-04): the guard defends a scenario with no call
> site in this codebase, and the concern is absent from ordinary web-stack practice because there the
> framework owns the scope (scope = HTTP request) — nobody hand-rolls ambient joining, so the question
> never arises. It arises here only because MAUI has no per-page scope and this design creates one
> manually.
>
> **Revision 12 removes the failure instead of guarding it: only a write ever publishes an ambient
> scope; a read never does.** A read nested in a write still joins the write's scope — the common
> lookup-before-persist case, and the outer write saves normally. A write nested in a read finds no
> ambient scope, opens its own, and saves. No flag, no throw, no silent loss, and less code than either
> earlier revision. The cost is that two `AppDbContext` instances can be alive during a
> read-containing-a-write — harmless, since the reads are `NoTracking` and the write owns its own
> context (§ 9's one-context invariant is restated as applying to writes).

**Inputs / Outputs / Preconditions — `IUnitOfWork.ExecuteAsync<TResult>` (the only value-returning form, Revision 10):**
- **Inputs:** `body` — a delegate receiving the scope's `IServiceProvider`, returning `Task<TResult>`.
  The body resolves whatever repositories or services it needs from that `IServiceProvider` — one or
  several, there is no distinction; `ct` — optional cancellation token, defaults to `default`.
- **Outputs:** `TResult` returned by `body` on success. On failure inside `body`, whatever the body
  itself returns per `code-style-reference.md § Service Return Patterns` (a `(false, message)` tuple)
  — `ExecuteAsync` itself does not catch or translate exceptions; an exception from `body` propagates
  after the `using` disposes the scope (no partial state survives, REQ-UOW-06). **Fail-closed on an
  unrecognised `TResult` (Revision 9, § 6b):** if `TResult` is neither a `ValueTuple` with a leading
  `bool` nor an `IUnitOfWorkOutcome` implementer, `ExecuteAsync` throws `InvalidOperationException`
  before any save is attempted, naming the two valid fixes (implement `IUnitOfWorkOutcome`, or use
  the no-signal `ExecuteAsync(Func<IServiceProvider, Task>, ct)` overload).
- **Preconditions:** every repository/service type the body resolves must be registered in the DI
  container (unchanged `AddScoped`/`AddSingleton`, § 2a "Reviewer-finding correction"). Caller must
  not hold a reference to any resolved instance or the `AppDbContext` behind it beyond the lifetime of
  `body` — doing so recreates a captive dependency (§ 9 Invariants). **The body MUST resolve
  repositories from the supplied `IServiceProvider`, never from the service's own constructor-injected
  fields — see the load-bearing rule below (§ 8, BL-2).**
- **Postconditions:** exactly one `SaveChangesAsync` executes (unless `body` itself calls a nested
  `ExecuteAsync`, which joins the ambient scope and does not trigger a second save); the scope and its
  `AppDbContext` are disposed before `ExecuteAsync` returns.

**Inputs / Outputs / Preconditions — `IUnitOfWork.ExecuteReadAsync<TResult>`:**
- **Inputs / Preconditions:** identical to `ExecuteAsync`.
- **Outputs:** `TResult` returned by `body`. **No `SaveChangesAsync` is ever called** — calling
  `ExecuteReadAsync` with a body that mutates tracked entities is a misuse of the API (the mutation is
  silently lost when the scope disposes); this is intentional per Revision 6 and is the tradeoff for
  the method name carrying the read/write intent.

```csharp
// ── Services/SongService.cs ─────────────────────────────────────────────────
public Task<(bool success, string message)> UpdateSongAsync(int id, string title, ..., CancellationToken ct = default)
    => _uow.ExecuteAsync<(bool, string)>(async sp =>      // ← the ONLY added line
    {
        var repo = sp.GetRequiredService<ISongRepository>();   // resolved from the LAMBDA's sp — never _songRepository (§ 8, BL-2)
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

**API shape — decided (Revision 10, § 8, APPROVED by Helder 2026-08-04, supersedes Revision 7): one
value-returning form, one no-signal form — no typed overload, no escape hatch.** Revision 7 justified
a typed `ExecuteAsync<TRepo, TResult>(repo => …)` as the "primary API" on the claim that "~30 of the
35 methods [§ 2a] are single-repository," with the `sp => …`/`IServiceProvider` form kept only as a
rare "escape hatch" for genuinely multi-repository flows. **That claim is false.** A full re-derivation
against current `develop` HEAD (§ 2a's method list, counting every distinct repository/service type a
method's body actually touches — including read-only validation lookups, since those also determine
whether a single `TRepo` type parameter could cover the body) finds **26 single-repository and 9
multi-repository methods**, not "~30 single, ~5 multi":

| Multi-repository method | Types touched | Notes |
|---|---|---|
| `QueueServiceNew.EnqueueSingerAsync` | `IEventRepository`, `IPersonRepository`, `ISongRepository`, `IQueueRepository` (4) | Reads three repos to validate, writes one — a typed single-`TRepo` overload cannot express this. |
| `QueueService.RecordParticipationAsync` | `IEventRepository`, `IVenueRepository`, `IEventParticipationRepository` (3) | Via the private `GetOrCreateDefaultEventAsync` helper it calls (`design.md § 10`); the public method itself reaches all three. |
| `SongService.CreateSongWithUrlsAsync` | `IArtistRepository`, `ISongRepository`, `ISongKaraokeUrlRepository` (3) | |
| `ArtistResolutionService.CommitAsync` | `IArtistRepository`, `IArtistService` (2, nested service call) | |
| `ArtistService.DeleteArtistsAsync` | `IArtistRepository`, `ICatalogRepository` (2) | Reads `ICatalogRepository.CountByArtistAsync` to validate before deleting. |
| `QueueService.AddPersonToQueueAsync` | `IPersonService` (nested service call) | Nested cross-service chain, § 6a. |
| `QueueServiceNew.UpdateSongSelectionAsync` | `IQueueRepository`, `ISongRepository` (2) | |
| `SongResolutionService.CommitAsync` | `ISongRepository`, `ISongService` (2, and a further-nested 3-level chain, § 6a) | |
| `SongService.CreateSongAsync` | `IArtistRepository`, `ISongRepository` (2) | Reads `IArtistRepository.GetByIdAsync` to validate the artist exists before writing the song. |

The remaining 26 methods touch exactly one repository/service type each. Even so, **the multi-repository
case is common, not exceptional** — 9 of 35 (roughly a quarter) is far from the rare "escape hatch"
Revision 7's framing implied, and the earlier service-locator objection to the `sp => …` form was
premised on that form being the minority case. **Decision:** drop the typed overload entirely. There
is one value-returning form, `ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, ct)`,
and one no-signal form, `ExecuteAsync(Func<IServiceProvider, Task> body, ct)`, plus the existing
`ExecuteReadAsync<TResult>`. A single uniform shape — always resolve from the lambda's
`IServiceProvider` — is simpler than two APIs plus a rule about which one to use for a given method,
and it removes an entire class of Wave 5 per-method judgment calls ("is this method single- or
multi-repository today, and will it stay that way"). Implicit save, the `ExecuteReadAsync`-never-saves
rule, `ITuple`/`IUnitOfWorkOutcome` detection, and the fail-closed throw on an unrecognised `TResult`
are all unchanged by this revision — see § 8 "Decision: drop the typed overload (Revision 10)" for the
full decision record.

```csharp
public interface IUnitOfWork
{
    // The only value-returning form. Body resolves whatever it needs from IServiceProvider —
    // one repository or several, no distinction.
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);

    // The only no-signal form, for bodies with nothing to inspect (bare Task).
    Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);
}
```

> **Superseded-in-scope note (non-blocking #2, 4th-pass spec review):** every `GetOrCreateDefaultEventAsync`/
> `SetActiveEventAsync`/`QueueRepository.ReorderAsync` bullet below predates D12 (2026-08-04), which
> excludes all Queue/Event entity code from this spec's phases. These flows are retained here as
> worked examples of Candidate C's mechanism (the analysis itself is still correct) but are **not**
> implemented by any phase of this spec — they are carried by
> `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`. See § 8
> D12 and `requirements.md` REQ-UOW-08's correction for the authoritative scope statement; do not treat
> the bullets below as an active task list.

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
  everything inside the lambda shares one scope. The inner service is resolved from `sp` and, when it
  also calls `ExecuteAsync`, the primitive honours an
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

**Restated for the in-scope 21-method set (non-blocking #5, 4th-pass spec review — this table was never
recomputed after D12 excluded Queue/Event code).** Of the 14 excluded methods: `EventService`'s 5 and
`QueueServiceNew`'s 6 are all `ValueTuple`-shaped (11); `QueueService`'s 3 split into 1 `ValueTuple`
(`AddPersonToQueueAsync`) and 2 bare `Task` (`RecordParticipationAsync`, `SetActiveEventAsync`) — 12
`ValueTuple` + 2 bare `Task` + 0 `IUnitOfWorkOutcome` excluded, summing to 14. Subtracting from the
all-inclusive 31/1/3:

| Shape | All-inclusive (35) | Excluded (14, D12) | **In-scope (21)** |
|---|---|---|---|
| `ValueTuple` with leading `bool` | 31 | 12 | **19** |
| `IUnitOfWorkOutcome` (`BackupResult`) | 1 | 0 | **1** |
| Bare `Task` (no signal) | 3 | 2 | **1** (`SongKaraokeUrlService.RecordPlayAsync` only) |
| **Total** | **35** | **14** | **21** |

19 + 1 + 1 = 21, matching § 8 D12's corrected in-scope total. This confirms REQ-UOW-26's correction
("the only in-scope no-signal method is `SongKaraokeUrlService.RecordPlayAsync`") is arithmetically
consistent with the full survey, not an isolated claim.

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
   itself. **Also required in the same sub-step (non-blocking #8, 4th-pass spec review, verified
   against source):** `BackupService` is hand-constructed via a factory lambda at `MauiProgram.cs:72`
   (`AddScoped<IBackupService>(sp => new BackupService(sp.GetRequiredService<IBackupRepository>(), ...))`),
   not a plain `AddScoped<IBackupService, BackupService>()`. Wrapping `CreateFullBackupAsync` in
   `IUnitOfWork` does not itself require touching this lambda (the wrap happens inside
   `BackupService`'s method body, resolving `IUnitOfWork` via ordinary constructor injection like any
   other service), but no Phase 4+ task currently names this lambda, and any change to
   `BackupService`'s constructor shape would require editing it. Flagged so the Phase 4+ `BackupService`
   sub-task explicitly checks whether its constructor changes and, if so, claims `MauiProgram.cs`
   (sequential-only, `workflow.md § Sequential-only file registry`) for that one line. Chosen over
   reflecting for a property
   named `"Success"` by convention: an explicit interface is a compile-time-checked contract: if a
   future result type is renamed or reshaped, the compiler catches a mismatch immediately, whereas a
   property-name convention fails silently (exactly the "convention that usually detects failure" this
   design explicitly rejects, item 3 below).
3. **No-signal fallback (3 of 35 methods today: the two `Task`-returning `QueueService` methods and
   `SongKaraokeUrlService.RecordPlayAsync`) — a dedicated overload, not a runtime guess.** `IUnitOfWork`
   gains `ExecuteAsync(Func<IServiceProvider, Task> body, ct)` alongside the existing
   `Task<TResult>`-returning `ExecuteAsync<TResult>` form (Revision 10 -- there is only ever one of
   each, no typed/escape-hatch pair). This overload has nothing to
   inspect — `body` returns `Task`, not `Task<TResult>` — so it **always saves** when `body` completes
   without throwing. The choice of always-save (not always-skip) is deliberate: the two failure modes
   are not symmetric. Silently discarding a real write is a correctness bug with no local signal a
   test would easily catch (the entity was mutated in memory; only a re-read from the database would
   reveal the write never landed). An unnecessary save on a method that always succeeds today is a
   no-op — SQLite writes nothing new if the change tracker has no dirty entries beyond what the
   overload's own `body` produced, and this project has no case where a `Task`-returning mutating
   method is expected to leave state unchanged. The overload's XML doc states this outcome explicitly
   (§ 6, `ExecuteAsync`) so it is impossible to reach this fallback by accident: a service
   method with no signal to give literally cannot call the signal-inspecting overload — the compiler
   picks the overload matching the delegate's return type.
4. **Fail-closed refusal for any other `TResult` reaching the value-returning overload (Revision 9,
   supersedes Revision 8's unconditional-save fallback for this case) — a `throw`, not a guess.** If
   a `TResult` passed to `ExecuteAsync<TResult>` is neither an `ITuple` with a leading `bool`
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

**Convention constraint this mechanism depends on (NB-2, third-pass spec review).** The `ITuple`
detection in item 1 above reads `t[0]` and interprets it as the success flag. This is only correct
because this codebase's Service Return Pattern convention (`code-style-reference.md § Service Return
Patterns`) fixes the **leading** `bool` of a mutating method's tuple to mean success. Nothing in the
C# type system enforces that positionally — a hypothetical future tuple shaped
`(bool hasMore, string message)` for a paging-style mutator, or any tuple whose first element is a
`bool` that means something other than "the mutation succeeded," would be silently misread by
`ResultSignalsSuccess`: it would skip the save when `hasMore` happens to be `false` and save when it
happens to be `true`, with no error, because a `bool`-shaped first element is exactly what the
structural check requires and always finds one. **The fail-closed throw (Revision 9, item 4 above)
does not protect against this** — fail-closed only catches the case where no `bool` is found at all;
a `bool` in the wrong semantic position is indistinguishable from the right one at the type level. This
is therefore a **documentation-enforced convention, not a compiler-enforced one**: any new mutating
service method wrapped in `IUnitOfWork.ExecuteAsync<TResult>` MUST keep "leading `bool` = success" as
its tuple's meaning, and any reviewer approving a Wave 5 (or later) diff must check this by reading the
tuple's semantics, not just its shape. Recorded here so it is not rediscovered as a bug later.

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
- Bare `Task` (no `TResult` at all) → the dedicated no-signal `ExecuteAsync(Func<IServiceProvider,
  Task>, ct)` overload is the only one whose signature fits; the compiler selects it, and it always saves —
  unchanged from Revision 8.
- Any other `TResult` reaching the value-returning `ExecuteAsync<TResult>` overload that is
  neither an `ITuple` nor an `IUnitOfWorkOutcome` implementer (e.g. a bare `Task<Song>`, should one
  ever be added) falls through both `if`s in `ResultSignalsSuccess` and hits the fail-closed `throw`
  — by explicit code, not by absence of a check, and before any `SaveChangesAsync` is attempted.
  There is no ambiguous path: skipping the save happens only inside the two recognised-failure
  branches, saving happens only inside the two recognised-success branches or the dedicated
  no-signal overload, and everything else throws instead of guessing either way. Because the
  compiler selects the no-signal overload for any `Func<IServiceProvider, Task>` body, no legitimate call site
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

> **Terminology note (non-blocking #1, 4th-pass spec review): "Wave N" below predates D11.** D11
> (2026-08-04) restructured § 10's flat Wave 0–7 sequence into six named Phases (0, 1, 2/PILOT,
> 3/VERIFY, 4+/Spread, LAST). Every "Wave N" reference inside a Decision block dated **before**
> 2026-08-04 (all of them — D11/D12/D13 are the only 2026-08-04 decisions and they already use Phase
> language) is left as originally written rather than rewritten, consistent with this document's own
> "a shipped/approved decision is immutable record" convention (see Revision 7's "kept for history, not
> deleted," § 8 above). Read every pre-D11 "Wave 3" as **Phase 1** (primitive) or **Phase 2** (pilot
> service wraps, since D11 folded the old Waves 1–4 into Phase 1 and moved the pilot-service wraps
> specifically into Phase 2) depending on context, "Wave 5" as **Phase 2** (for the four pilot services
> — `SongService`, `ArtistService`, `ArtistResolutionService`, `SongResolutionService`) or **Phase 4+**
> (for every other service, including `BackupService`), "Wave 6" as **Phase 4+**, "Wave 0" as
> **Phase 0**, and "Wave 7" as **LAST**. § 10's phase table (below § 8) is the authoritative,
> current-terminology source; this note exists so a reader of § 8's history is not misled into thinking
> a still-current Wave numbering exists elsewhere in the codebase or task-log.

### Decision: cancel the T10 re-run #6 gate on the stopgap deletion (Wave 3b) — **APPROVED by Helder (2026-08-04)**
**Chosen approach:** the hard constraint that Wave 3b (deleting the `1a114c1` `SongRepository.UpdateAsync`
stopgap, REQ-UOW-18) must not land before Helder's on-device T10 re-run #6 completes is withdrawn.
Wave 3b becomes an ordinary step of Wave 3, gated on nothing external.
**Rationale:** there is no reason to device-test the stopgap when this unit-of-work work deletes and
replaces it outright — verifying code that is about to be removed is wasted effort.
**Reversibility:** Reversible — reinstating a gate is a documentation-only change.

### Decision: adopt a scope-per-operation unit of work (Candidate C) — **APPROVED by Helder**
**Chosen approach:** `AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` + a single
`IUnitOfWork` primitive built on `IServiceScopeFactory`, exposing `ExecuteAsync<TResult>` /
`ExecuteReadAsync<TResult>` / `ExecuteAsync` (no-signal), each resolving from the lambda's
`IServiceProvider` (Revision 10, supersedes the Revision 7 typed/escape-hatch pair — see below);
services wrap their body in one line; repositories keep constructor injection and lose
`SaveChangesAsync`.
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
**Chosen approach:** `IUnitOfWork` exposes both `ExecuteAsync<TResult>` and `ExecuteReadAsync<TResult>`
(each resolving from the body's `IServiceProvider`, Revision 10). The method name carries the
read/write intent; a reviewer does not need to read the lambda body to know whether a call site can
mutate data.
**Alternatives considered:** a single `ExecuteAsync` used uniformly for both reads and writes, with
"no mutation happened" left as an unenforced convention for read paths — rejected because it silently
performs a wasted `SaveChangesAsync` no-op on every read call and offers no signal at the call site.
**Reversibility:** Reversible — `ExecuteReadAsync` is additive; nothing depends on its absence.
**Rationale:** resolves Open Question 4 from the prior draft. Uniformity of "everything goes through
`IUnitOfWork`" is preserved without forcing every read-only service method to carry a phantom save.

### Decision: typed overload is the primary API; the `sp => …` overload is an escape hatch only (Revision 7) — **SUPERSEDED by Revision 10 (below), APPROVED by Helder at the time**
> **Superseded 2026-08-04.** This decision is kept for history, not deleted — `CLAUDE.md § Docs/
> Folder Layout` treats a shipped/approved decision as immutable record, and the third-pass spec
> review (finding D10) showed this decision's premise was factually wrong, not merely reconsidered.
> See "Decision: drop the typed overload (Revision 10)" immediately below for the correction and the
> re-derived numbers.
**Chosen approach (superseded):** `ExecuteAsync<TRepo, TResult>(repo => …)` / `ExecuteReadAsync<TRepo,
TResult>(repo => …)` are what the "~30-of-35 single-repository" service methods (§ 2a) call. The
`Func<IServiceProvider, Task<TResult>>` overloads are retained **only** for the genuinely
multi-repository flows (`CreateSongWithUrlsAsync`, `GetOrCreateDefaultEventAsync`, `ReorderQueueAsync`)
and the three nested-service chains found in § 6a.
**Alternatives considered:** the `sp => …` overload as the sole/primary API (former "Variant C2"
framing, where the typed overload was optional) — rejected because resolving a dependency from an
`IServiceProvider` bag is a service-locator pattern, and standard DI guidance treats service location
as a last resort, not a default. Presenting it as equally idiomatic to the typed overload would have
made the last-resort pattern the common case.
**Reversibility:** Reversible — both overloads coexist; nothing prevents converting call sites either
direction later. *(Exercised: Revision 10 below reverses this decision.)*
**Rationale (at the time, now known incorrect):** resolves Open Question 3 from the prior draft. Names
the dependency in the method signature for the majority case; confines the service-locator pattern to
the minority of call sites that genuinely need it, and the design says so explicitly rather than
leaving it implicit. **Why this was wrong:** the "~30 of 35 single-repository" premise was never
verified against source before Revision 7 was approved. § 6 (D10 correction) re-derives the number by
actually counting distinct repository/service types touched per method body — including read-only
validation lookups, since those also determine whether a single `TRepo` type parameter could cover the
body — and finds **26 single-repository, 9 multi-repository**, not "~30 single, ~5 multi". Nine of
thirty-five is common enough that the "escape hatch" framing was never accurate, and the
service-locator objection to `sp => …` was premised on that form being rare.

### Decision: drop the typed overload entirely — one value-returning form, one no-signal form (Revision 10) — **APPROVED by Helder 2026-08-04, supersedes Revision 7**
**Chosen approach:** `IUnitOfWork` exposes exactly `ExecuteAsync<TResult>(Func<IServiceProvider,
Task<TResult>>, ct)`, `ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>>, ct)`, and
`ExecuteAsync(Func<IServiceProvider, Task>, ct)`. There is no `TRepo`-typed overload and no
"escape hatch" framing anywhere in the API or its docs — every call site resolves what it needs from
the lambda's `IServiceProvider`, whether that is one repository or several.
**Alternatives considered:** keep Revision 7's typed-overload-as-primary-API shape and simply correct
the "~30 of 35" prose to "26 of 35" — rejected because 9-of-35 multi-repository is not a rare case a
one-line "escape hatch" doc comment can honestly describe, and a two-API surface (plus the judgment
call of which one a given method needs, which can change as a method's implementation evolves) is
strictly more ceremony than one uniform shape for a benefit (naming the dependency in the signature)
that only 26 of 35 call sites would ever realize.
**Reversibility:** Reversible with effort — reintroducing a typed overload is additive and would not
require touching the 35 call sites again, but doing so would resurrect the same "which overload does
this method need today" judgment call this revision removes.
**Rationale:** the multi-repository case is common, not exceptional (9 of 35, § 6 D10 correction), so
a single uniform shape is simpler than two APIs plus a rule about which to use. The earlier
service-locator objection to `sp => …` was premised on that form being rare, which the corrected count
shows it is not. This also collapses an entire class of Wave 5 per-method judgment calls (§ 10) — no
implementer needs to first classify a method as single- or multi-repository before choosing which
`ExecuteAsync` overload to call.

### Decision: the lambda body MUST resolve repositories from its own `IServiceProvider`, never from the service's constructor-injected fields (§ 8, BL-2) — **load-bearing rule, APPROVED by Helder 2026-08-04**
**Chosen approach:** inside every `ExecuteAsync`/`ExecuteReadAsync` body, ALL repository (and
data-writing service) access MUST go through instances resolved from the lambda's own
`IServiceProvider` parameter. The service's constructor-injected repository fields (e.g.
`SongService._songRepository`) MUST NOT be referenced inside the lambda — not even by accident,
because the field and the lambda-resolved instance both type-check and both compile.
**Why this is load-bearing, not stylistic:** the constructor-injected fields are resolved once, at
service construction time, from the **window-scope** `IServiceProvider` (the same
`AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` registration also hands out a scoped
`AppDbContext` to ordinary scoped resolution — § 1's "Reviewer-finding correction" — and MAUI's
services are themselves constructed once per window, § 1). A repository field resolved that way holds
the **session-lifetime** `AppDbContext`, exactly the object this entire change exists to stop using.
If a Wave 5 implementer adds the `ExecuteAsync` wrap around a method body but leaves that body calling
`_songRepository.UpdateAsync(...)` instead of `sp.GetRequiredService<ISongRepository>().UpdateAsync(...)`,
the code compiles clean, the diff looks correct, and existing tests may even pass — because the
*call shape* is identical either way, only the resolved instance's `AppDbContext` differs. The wrap
becomes a no-op against BUG-068: the body still mutates through the window-scope context, and
`IUnitOfWork` dutifully calls `SaveChangesAsync` on a *different*, freshly-scoped `AppDbContext` that
never saw the mutation. BUG-068 would silently remain, hidden behind a code change that looks like the
fix.
**Enforceable check (AC):** REQ-UOW-28 (`requirements.md`, corrected 4th-pass spec review BL-G) — a
per-method review/grep check that no `ExecuteAsync`/`ExecuteReadAsync` lambda body in `Services/*.cs`
references **any** `_`-prefixed constructor-injected field of the enclosing service — repository-typed
or service-typed alike. The narrower "repository field only" version of this check would pass the
pilot's deepest chain (`SongResolutionService.CommitAsync`'s lambda driven through `_songService`/
`_artistResolution`, § 6a) unexamined even if it silently defeated the pattern; the wave-5 checklist
item and code-review checklist entry both point to the corrected AC.
**Should the injected repository fields be removed from services entirely once every method is
wrapped?** Yes, in principle, once every mutating AND every read method of a service is wrapped in
`ExecuteAsync`/`ExecuteReadAsync` — an unused constructor-injected field is itself a code smell and a
standing invitation to reintroduce this exact bug in a future edit. This design does not mandate that
removal as part of Wave 5, because a handful of read-only service methods may reasonably continue to
use a directly-injected repository outside any unit of work (nothing in this design requires every
*read* method to route through `ExecuteReadAsync` — only writes are in scope, per the Out of Scope
section of `requirements.md`). **Recommendation, not a Wave 5 requirement:** once a service's every
method is wrapped, remove that service's now-dead repository constructor parameters and fields as a
same-PR cleanup step; if any read method still uses the field directly, leave it and note why in the
task-log. Flagged here so Wave 5 implementers make the call deliberately per service, not by omission.
**Reversibility:** Reversible — the rule constrains lambda bodies only; nothing about the API shape
depends on it.

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
no-signal `ExecuteAsync(Func<IServiceProvider, Task> body, ct)` overload that **always** saves for the
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
`IUnitOfWorkOutcome`, or use the no-signal `ExecuteAsync(Func<IServiceProvider, Task>, ct)` overload).
The dedicated no-signal overload itself is unaffected — it still always saves for the 3 genuinely
signal-less methods (`QueueService.RecordParticipationAsync`, `QueueService.SetActiveEventAsync`,
`SongKaraokeUrlService.RecordPlayAsync`), because the compiler selects that overload on the
`Func<IServiceProvider, Task>` delegate type — no legitimate call site can reach the throw.
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

### Decision: `ExecuteUpdateAsync`/`ExecuteDeleteAsync` are exempt from the save-skip/atomicity guarantees (fixes 4th-pass finding BL-C)
**The problem, verified against source.** `IUnitOfWork.ExecuteAsync`'s save-skip mechanism (§ 6b) and
REQ-UOW-24/25/26's atomicity claims assume every mutation inside `body` is staged on the change tracker
until `SaveChangesAsync` runs. Five in-scope repository methods violate that assumption: they call
`ExecuteUpdateAsync`/`ExecuteDeleteAsync`, which run immediate SQL against the database the instant they
are awaited, bypassing the change tracker entirely — `SongKaraokeUrlRepository.IncrementPlayCountAsync`
(`:56-64`, `ExecuteUpdateAsync`), `SongKaraokeUrlRepository.RemoveAsync` (`:48-53`, `ExecuteDeleteAsync`),
`ArtistRepository.DeleteAsync` (`:148-154`), `SongRepository.DeleteAsync` (`:136-142`),
`CatalogRepository.RemoveAsync` (`:70-75`) — all `ExecuteDeleteAsync`. Two of the five back **pilot**
(Phase 2) methods: `ArtistService.DeleteArtistsAsync` and `SongService.DeleteSongsAsync`.
**Chosen approach:** these five methods are documented as an explicit carve-out (`requirements.md`
REQ-UOW-33) — `IUnitOfWork` still wraps them (for API-surface consistency and because some also read
tracked state before deleting), but no save-skip/atomicity test may be written against the
`ExecuteUpdateAsync`/`ExecuteDeleteAsync` call itself. REQ-UOW-26 is corrected to note this exemption
applies to its only exemplar, `SongKaraokeUrlService.RecordPlayAsync`.
**Alternatives considered:** rewrite the five methods to load-then-mutate-then-`SaveChangesAsync`
instead of using `ExecuteUpdateAsync`/`ExecuteDeleteAsync` — rejected as out of scope for this spec
(REQ-UOW-13 prefers EF Core built-ins including `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, and rewriting
five working bulk operations to fit a save-boundary abstraction is scope creep unrelated to BUG-068).
**Reversibility:** Reversible — the carve-out is a documentation/testing-scope correction; nothing about
`UnitOfWork`'s implementation depends on it.
**Rationale:** an immediate-SQL bulk operation has no "partial state" for a unit of work to roll back —
asserting REQ-UOW-24/25/26 against it would be asserting something structurally false, not something
this design could ever make true without abandoning `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (which
REQ-UOW-13 explicitly favors).

### Decision: `IBaseRepository<T>.SaveChangesAsync()` is NOT removed by this spec — deferred entirely (fixes BL-A2/4th-pass finding BL-B) — **REQUIRES HELDER'S CONFIRMATION**
**The problem, verified against source.** `Domain/RepositoryInterface/IBaseRepository.cs:18` declares
`Task SaveChangesAsync();`. `IVenueRepository` and `IPersonRepository` both extend
`IBaseRepository<T>` (`IVenueRepository.cs:8`, `IPersonRepository.cs:6`) — both are IN-SCOPE entities.
But the *same* `IBaseRepository<T>.SaveChangesAsync()` member is also the only save entry point the
EXCLUDED `Services/QueueService.cs` calls on its constructor-injected in-scope-typed repository fields:
`_participationRepository.SaveChangesAsync()` (`QueueService.cs:97`), `_venueRepository.SaveChangesAsync()`
(`:134`), `_eventRepository.SaveChangesAsync()` (`:145`) — all verified by direct read. `IEventRepository`
and `IEventParticipationRepository` also extend `IBaseRepository<T>` (excluded types). Removing
`SaveChangesAsync()` from `IBaseRepository<T>` — as Phase 1's original "remove `SaveChangesAsync` from
every repository interface" instruction implied — makes `QueueService.cs` fail to compile, which is a
de facto modification of an excluded file, forbidden by REQ-UOW-31.
**Chosen approach:** `IBaseRepository<T>.SaveChangesAsync()` is **not removed** by any phase of this
spec. It is only removed from the five **standalone** repository interfaces that do not extend
`IBaseRepository<T>` and have no excluded consumer — verified by grep, none of `ISongRepository`,
`IArtistRepository`, `ICatalogRepository`, `ISongKaraokeUrlRepository`, `IBackupRepository` declares
`: IBaseRepository<T>` (`Domain/RepositoryInterface/*.cs`). Phase 1's repository-interface edit is
scoped to exactly these five. Removal of `IBaseRepository<T>.SaveChangesAsync()` (and, by extension,
of `BaseRepository<T>`'s own pass-through implementation, `BaseRepository.cs:76-79`, one of REQ-UOW-11's
six named sites) is deferred to
`changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`, which owns
removing the excluded call sites first.
**Consequence for REQ-UOW-11 (`requirements.md`):** the "six pass-through implementations" obligation is
narrowed from six to **five** for this spec — `BaseRepository.cs:76-79` is excluded from this spec's
REQ-UOW-11 scope and moved to the deferred item alongside the six embedded-mutator saves already
deferred there (D12 item 4). `PersonRepository`/`VenueRepository` keep a technically-reachable
`SaveChangesAsync()` member (inherited from `IBaseRepository<T>`) after this spec ships; no in-scope
service calls it once Phase 2/4+ wraps `PersonService`/`VenueService`, but the interface member itself
is not deleted. This is a **narrower structural guarantee than REQ-UOW-11's original wording
("no in-scope repository implementation declares... SaveChangesAsync")** promised — flagged here as a
requirement correction, not silently absorbed.
**Alternatives considered:** (b) a named, bounded REQ-UOW-31 exemption permitting this spec to touch
`QueueService.cs`'s three call sites only, deleting `.SaveChangesAsync()` from those three lines while
changing nothing else in the file. Rejected as the primary choice — it still counts as "modifying"
an excluded file under REQ-UOW-31's plain reading, and D12's exclusion was Helder's verbatim
instruction to "forget about" Queue/Event code entirely, which a surgical three-line exemption
undercuts even if narrow.
**Reversibility:** Reversible — once the deferred item removes the three `QueueService.cs` call sites,
`IBaseRepository<T>.SaveChangesAsync()` and `BaseRepository.cs`'s pass-through can be deleted in a
follow-up commit against this spec's own repositories.
**Why this needs Helder's confirmation:** it narrows an already-approved requirement (REQ-UOW-11) after
approval, on a fact (the shared generic interface) that the third- and fourth-pass reviews both missed.
This is a correction to a load-bearing AC, not a cosmetic edit.

### Decision: no `CreateExecutionStrategy` / retry policy
**Rationale:** SQLite is local; there are no transient network faults to retry. Adding a strategy
would also constrain the manual transaction in `QueueRepository.ReorderAsync`.

### Decision: pilot-first phase order — prove the pattern on one service group before spreading app-wide (D11) — **APPROVED by Helder 2026-08-04**

**Chosen approach:** § 10's wave table is restructured into six phases, replacing the flat Wave 0–7
sequence with an explicit prove-then-spread order:
- **Phase 0 — RED tests** (unchanged content of former Wave 0): write and confirm-red the
  REQ-UOW-03/04 BUG-068 regression tests plus the REQ-UOW-22/09 atomicity tests, against current
  `develop` HEAD.
- **Phase 1 — Registration + primitive:** `AddDbContext` → `AddDbContextFactory(…, Scoped)`;
  introduce `IUnitOfWork`/`IUnitOfWorkOutcome`; implement `UnitOfWork`, including the `AsyncLocal`
  ambient-scope join (former Waves 1, 2, 3, 4 collapsed into one phase — none of them touches a
  service method body, so there is no reason to gate them behind separate phases).
- **Phase 2 — PILOT:** wrap only `SongService`, `ArtistService`, `ArtistResolutionService`, and
  `SongResolutionService` in `ExecuteAsync`/`ExecuteReadAsync`; delete their repositories'
  `SaveChangesAsync` call sites; **also delete the `1a114c1` stopgap guard in
  `SongRepository.UpdateAsync`** (REQ-UOW-18) here, not earlier — the stopgap and the pilot wrap touch
  the same method, and validating the real fix and removing the old workaround together is one
  reviewable change instead of two.
- **Phase 3 — VERIFY (HARD GATE):** full test suite green, **and** Helder's on-device confirmation
  that the pilot screens (Song/Artist CRUD) no longer reproduce BUG-068/BUG-071. **No Phase 4 task may
  be dispatched, claimed, or started until this gate is recorded passed in the task-log** — the same
  hard-gate discipline as any other Helder approval gate in this project (`workflow.md`).
- **Phase 4+ — Spread:** wrap the remaining in-scope service methods (§ 8 D12 — the pilot's **9**
  methods, re-derived from source, BL-A, subtracted from the corrected 21-method in-scope total leaves
  **12** methods across `CatalogService`, `PersonService`, `VenueService`, `SongKaraokeUrlService`,
  `BackupService`); convert the in-scope ViewModels (former Wave 6 —
  `PersonPickerViewModel`, `QueueSongPickerViewModel`, and `QueueManagementViewModel`'s
  `IPersonRepository`/`ISongRepository` usage only — its `IEventService`/`IQueueServiceNew` fields stay
  untouched, § 8 D12 item 6 correction, REQUIRES HELDER'S CONFIRMATION — plus the singleton
  audit); remove `DbLoadGate` only once every in-scope consumer has been converted AND the
  `page-load-frozen` regression suite is confirmed green without it (REQ-UOW-29, BL-H — removing it
  earlier would prematurely stop serializing loads for ViewModels the pilot has not yet touched, or
  would reintroduce the separate `page-load-frozen` sync-async freeze this gate also happens to guard
  against).
- **LAST — Guidelines:** amend `code-style-reference.md § DI Registration Conventions` (REQ-UOW-19)
  and align `TestDbContextFactory`/delete the two tracking-workaround tests (former Wave 1 and Wave 7
  respectively). These explicitly land **after** Phase 4+, not interleaved with it — a guideline
  documents an established pattern; documenting it before the pattern has spread past a 4-service
  pilot would describe something not yet true of the codebase.

**Alternatives considered:** the original flat Wave 0–7 order (rejected — it wraps all 35 service
methods across every layer before any of them has been proven correct end-to-end on a real device, so
a design flaw discovered at the old Wave 5 would already have touched every service).
**Reversibility:** Reversible — this re-sequences already-approved work items; it changes no API shape
or requirement.
**Rationale:** proving the pattern on one screen (Song/Artist CRUD, the screens BUG-068/BUG-071 were
found on) before spreading it to the remaining in-scope service methods and ViewModels prevents
propagating a wrong approach app-wide. If Phase 3's on-device check surfaces a problem with the API
shape or the ambient-join mechanism, only 4 services need rework, not the full in-scope set.

### Decision: exclude Queue and Event entity code from this rollout (D12) — **APPROVED by Helder 2026-08-04**, verbatim: *"forget about any code related to Queue and Event entities. They're candidates to be completely refactored later, when we will already have the new approach already stablished in the guides."*

**Chosen approach:** `EventService`, `QueueService`, and `QueueServiceNew` — and the repositories they
exclusively own (`EventRepository` in both families, `QueueRepository`, `EventParticipationRepository`)
— are out of scope for every phase of this spec. Their unit-of-work migration is tracked separately:
`changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`.

**Re-derived scope (corrected against source — the prior "35 methods" and "26 single / 9 multi"
figures in § 2a/§ 6 were computed before this exclusion existed and are all-inclusive totals, not
in-scope totals):**

1. **In-scope method count.** § 2a's 35-method table itemises `EventService` (5 methods:
   `CreateEventAsync`, `StartEventAsync`, `PauseEventAsync`, `ResumeEventAsync`, `FinishEventAsync` —
   verified by direct read of `Services/EventService.cs`), `QueueService` (3 methods:
   `AddPersonToQueueAsync`, `RecordParticipationAsync`, `SetActiveEventAsync` — verified against
   `Services/QueueService.cs`), and `QueueServiceNew` (6 methods: `EnqueueSingerAsync`,
   `RegisterParticipationAsync`, `StopPerformanceAsync`, `MarkAbsentAsync`, `UpdateSongSelectionAsync`,
   `ReorderQueueAsync` — verified against `Services/QueueServiceNew.cs`). 5 + 3 + 6 = 14, matching the
   count used when framing the deferral. **Previously stated as 35 (undifferentiated); re-derived as
   21 in-scope** (35 − 14) from `Services/EventService.cs`, `Services/QueueService.cs`,
   `Services/QueueServiceNew.cs` (source-verified above) plus § 2a's existing per-file breakdown for
   the other nine files (`ArtistResolutionService` 1, `ArtistService` 3, `BackupService` 1,
   `CatalogService` 2, `PersonService` 3, `SongKaraokeUrlService` 3, `SongResolutionService` 1,
   `SongService` 4, `VenueService` 3 — sums to 21).

2. **Single- vs multi-repository split, in-scope methods only.** § 6's 9-row multi-repository table
   contains `QueueServiceNew.EnqueueSingerAsync`, `QueueService.RecordParticipationAsync`,
   `QueueService.AddPersonToQueueAsync`, and `QueueServiceNew.UpdateSongSelectionAsync` — 4 of the 9,
   all now excluded. **Previously stated as 26 single / 9 multi (all-inclusive); re-derived as 16
   single / 5 multi, 21 total, for the in-scope set** (9 − 4 = 5 in-scope multi:
   `SongService.CreateSongWithUrlsAsync`, `ArtistResolutionService.CommitAsync`,
   `ArtistService.DeleteArtistsAsync`, `SongResolutionService.CommitAsync`,
   `SongService.CreateSongAsync`; 21 − 5 = 16 in-scope single. Arithmetic check: the 10 excluded
   single-repository methods (26 total single − 16 in-scope single) plus the 4 excluded multi total
   14, matching item 1).

3. **Boundary-crossing analysis (load-bearing).** § 6a's cross-service call-site audit covered
   `I*Service` fields only. **Corrected 2026-08-04 (4th-pass spec review, BL-D):** re-derived by
   grepping over `Services/*.cs` for repository-typed fields as well, and over `MyVocaList/UI/ViewModels/*.cs`
   for both service- and repository-typed fields — the original "in-scope → excluded: none found" claim
   was reported without running these two additional greps and is **false**. The corrected table:

   | Direction | Caller (post-spec state) | Callee (post-spec state) | What happens | Verdict |
   |---|---|---|---|---|
   | Excluded → in-scope | `QueueService.AddPersonToQueueAsync` (**unwrapped** — stays on the window-scope `AppDbContext` via its own `_personService` field, D12) | `PersonService.CreatePersonAsync` / `GetPersonByNameAsync` (**wrapped** once Phase 4+ lands) | `QueueService`'s call is not inside an `ExecuteAsync` lambda, so `_ambientScope.Value` is `null` when `CreatePersonAsync` runs; `CreatePersonAsync`'s own `ExecuteAsync` opens a **fresh** scope+context, saves, and disposes it before returning, independent of `QueueService`'s window-scope context. `QueueService` only reads `person.FullName` afterward (`Services/QueueService.cs:64`) and does not inject `IPersonRepository` at all, so no tracked-entity leak crosses back into the window-scope context from this call site. | **Not a live risk at this call site** — the wrapped side's short-lived context absorbs the mutation cleanly. |
   | In-scope service → excluded repository, at the REPOSITORY layer (**newly found, BL-D**) | `Services/VenueService.cs:16,25,29` — constructor-injects `IEventRepository _eventRepository` | *(unused — verified: `_eventRepository` appears only in the field declaration and constructor assignment; no method body references it)* | Dead injection, not a live data-flow risk today, but it is a live compile-time coupling: `VenueService` (in-scope, wrapped in Phase 4+) constructor-injects an excluded-family repository type. If a future edit to `VenueService` starts using `_eventRepository` inside an `ExecuteAsync` lambda, it would resolve `IEventRepository` from the per-operation `sp` (per REQ-UOW-28) rather than the window-scope field — safe *if* the load-bearing rule is followed, but the dependency itself contradicts D12's "forget about Queue/Event code" instruction at the composition level. | **Not a runtime risk (dead field), but the prior "none found" claim was false** — recorded here so a future `VenueService` edit doesn't assume no Queue/Event coupling exists. Recommend removing the unused `_eventRepository` field as part of Phase 4+'s `VenueService` wrap (not mandated by any REQ-UOW-NN; a cleanup note, not a blocking gate). |
   | Excluded service → in-scope REPOSITORY, unwrapped save (**newly found, BL-D, more serious**) | `QueueService.GetOrCreateDefaultEventAsync` (private helper of `QueueService`, excluded) | `_venueRepository.AddAsync` + `_venueRepository.SaveChangesAsync()` (`QueueService.cs:133-134`) — `IVenueRepository` is an IN-SCOPE repository type | This is a repository-layer crossing the § 6a service-field-only audit could not see: an EXCLUDED service directly mutates and saves through an IN-SCOPE repository's window-scope `AppDbContext`, using the pass-through `SaveChangesAsync()` this spec cannot remove from `IBaseRepository<T>` (see the "`IBaseRepository<T>.SaveChangesAsync()` is NOT removed" decision above — this call site is *why* that member must survive). Once `VenueService`'s own methods are wrapped in Phase 4+, `VenueService`'s writes go through a fresh per-operation context, but `QueueService`'s direct write via `_venueRepository` still lands on the **window-scope** `AppDbContext`, since `QueueService` is never wrapped. | **Not a new correctness regression** — this is exactly the pre-existing window-scope-context write pattern for excluded code, already covered by the D12 item 3 "LIVE RISK" paragraph below. Recorded here because it is the concrete evidence for that paragraph's general claim, not merely a repetition of it. |
   | In-scope → excluded | *(none found among service-to-service fields, § 6a's original scope)* | — | The original service-field grep result stands for `I*Service`-typed fields specifically. Extending to repository-typed fields (`IQueueRepository`/`IEventRepository`/`IEventParticipationRepository` injected directly by an in-scope `Services/*.cs` file) still finds no matches — only `VenueService`'s unused `IEventRepository` field, already listed above. | **No live in-scope → excluded service-layer crossing exists**, but the repository-layer coupling above was missed by the narrower original claim. |

   **The real LIVE RISK is structural, not call-site-local, and it is carried by the deferred item:**
   `AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)` (§ 1 "Reviewer-finding correction")
   registers `AppDbContext` itself as an ordinary scoped service, so **every** constructor-injected
   repository field anywhere in the app — including `EventService`'s `_eventRepository`,
   `QueueService`'s three repository fields, and `QueueServiceNew`'s four — still resolves the **same
   single window-scope `AppDbContext` instance** this change exists to stop using, for as long as
   Queue/Event code exists (indefinitely, per this deferral). `requirements.md`'s Problem statement
   already documents that an `InvalidOperationException` can put a `DbContext` into an unrecoverable
   state for the remainder of its lifetime. Because Queue/Event code keeps the BUG-068/BUG-071 defect
   by design (that is the point of deferring it), a Queue/Event throw poisons the **same shared context
   instance** that any not-yet-migrated in-scope code (during Phases 2/3, before Phase 4+ completes) is
   *also* still resolving via plain constructor injection. Finishing Phase 4+ does not remove this risk
   — it persists for as long as Queue/Event repositories remain constructor-injectable off the shared
   scope, i.e. permanently, until the deferred item lands. **Recorded as a LIVE RISK owned by
   `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/README.md`**,
   not resolved by this spec.

4. **BL-1 re-verified under the exclusion.** `requirements.md` REQ-UOW-11 names 6 embedded
   `SaveChangesAsync` calls "inside repository *mutator* methods": `Infra/Repository/EventRepository.cs:37`,
   `Infra/Repositories/QueueRepository.cs:56,67,93`, `Infra/Repositories/EventRepository.cs:66,77`. A
   source grep (`SaveChangesAsync` across `Infra/Repositories/*.cs` and `Infra/Repository/*.cs`)
   confirms **all 6** are in `EventRepository` (either family) or `QueueRepository` — **zero** are in
   an in-scope repository. **This confirms BL-1's claim is TRUE, and — new under D12 — all 6 embedded
   saves are now out of scope for this spec's phases.** BL-1 is **not deleted**: it is restated as a
   live risk owned by the deferred item. `REQ-UOW-11` is corrected (`requirements.md`) to scope its
   "6 embedded saves" clause to the deferred item, leaving only the 6 pass-through implementations (all
   in in-scope repositories) as this spec's REQ-UOW-11 obligation.

5. **REQ-UOW-04 / REQ-UOW-08 / REQ-UOW-23 / REQ-UOW-26 scope corrections:** see `requirements.md` —
   each named a Queue/Event repository, service, or method as in scope; each is corrected there.

6. **`PersonPickerViewModel` — IN SCOPE.** Its only dependency is `IPersonRepository`
   (`MyVocaList/UI/ViewModels/PersonPickerViewModel.cs:10,13` — verified by direct read: constructor is
   `PersonPickerViewModel(IPersonRepository personRepository, ILogger<PersonPickerViewModel> logger)`,
   no Queue/Event type anywhere in the file). `Person` is not a Queue/Event entity — D12 excludes
   Queue/Event **entities**, not every screen whose name contains "Queue". `PersonPickerViewModel`
   stays in the Wave 6/Phase 4+ ViewModel conversion as originally scoped. The same reasoning applies
   to `QueueSongPickerViewModel` (injects `ISongRepository`) — named "Queue…" but does not inject a
   Queue/Event repository, so it remains in scope despite its name.
   **`QueueManagementViewModel` — corrected 2026-08-04 (4th-pass spec review, BL-D): NOT convertible to
   `IUnitOfWork` as a whole, despite being listed as IN SCOPE above.** Verified by direct read
   (`MyVocaList/UI/ViewModels/QueueManagementViewModel.cs:11-14`, constructor `:22-28`): its full
   dependency set is `IEventService _eventService` (:11), `IQueueServiceNew _queueService` (:12),
   `IPersonRepository _personRepository` (:13), `ISongRepository _songRepository` (:14) — **not** just
   `IPersonRepository`/`ISongRepository` as the original D12 item 6 claim stated. `IEventService` and
   `IQueueServiceNew` are both excluded types (D12, Out of scope). This directly contradicts § 10
   Phase 4+'s instruction to "convert all three [`QueueSongPickerViewModel`, `QueueManagementViewModel`,
   `PersonPickerViewModel`]... to inject `IUnitOfWork`" — a `QueueManagementViewModel` fully converted to
   inject only `IUnitOfWork` would have nowhere to resolve `IEventService`/`IQueueServiceNew` from
   without those excluded services being wrapped, which D12 forbids.
   **Resolution:** `QueueManagementViewModel` is only **partially** convertible. Its `IPersonRepository`/
   `ISongRepository` fields (if used for direct repository access outside `IEventService`/
   `IQueueServiceNew` calls) may be replaced by resolving those repositories through `IUnitOfWork` at
   the call sites that use them; its `IEventService`/`IQueueServiceNew` fields MUST remain as direct
   constructor injections — converting them is Queue/Event work, out of scope under D12. Phase 4+'s
   task for `QueueManagementViewModel` is corrected from "convert to inject `IUnitOfWork`" to "convert
   its `IPersonRepository`/`ISongRepository` usage to `IUnitOfWork`-wrapped calls; leave
   `IEventService`/`IQueueServiceNew` untouched." **REQUIRES HELDER'S CONFIRMATION** — this changes the
   Phase 4+ task scope for one named ViewModel after D12 was approved on an incomplete read of its
   constructor.

**Alternatives considered:** deferring only the duplicate `EventRepository` families while still
migrating `QueueService`/`QueueServiceNew`/`EventService` themselves — rejected; Helder's instruction
excludes the **entities**, and every one of the 14 excluded methods is a service-layer mutator over
those entities, not just the repository layer.
**Reversibility:** Reversible — nothing in Phases 0–4+ depends on Queue/Event code; the deferred item
can be picked up independently once its own spec exists.
**Rationale:** verbatim Helder instruction; Queue and Event are already flagged for a full refactor, so
investing unit-of-work migration effort in code that will be rebuilt is waste. The corrected scope
numbers above show the remaining in-scope surface (21 methods, 16 single / 5 multi) is meaningfully
smaller than the original 35/26/9 figures — the pilot-first ordering (D11) is even more tractable at
this size.

### Decision: defer the final `IUnitOfWork` API shape decision to the pilot (D13) — **APPROVED by Helder 2026-08-04**, verbatim: *"I supose that a pilot proven pattern is the correct call."*

**Chosen approach:** the Revision 10 API shape recorded above (a single value-returning
`ExecuteAsync<TResult>`, a single no-signal `ExecuteAsync`, no typed `TRepo` overload) is kept as
**PROVISIONAL** working text for Phase 1/Phase 2. It is not re-opened or re-litigated before the
pilot — Phase 2 implements against it as written. **Phase 3's VERIFY checklist gains a new item:**
decide the final API shape (keep the uniform `sp => …` shape as final, or introduce a typed overload
after all) from the 4 pilot services' **real** call sites — not the hypothetical § 6 audit — and apply
that decision, if it differs from the provisional shape, before Phase 4+ spread begins, so the
remaining 17 in-scope methods are written against the final shape once, not migrated twice.
**Alternatives considered:** treat Revision 10 as final today, with no pilot checkpoint (rejected —
Helder's own framing explicitly wants the pilot's real usage, not the audit's projected usage, to be
the deciding evidence); re-open the full Revision 7/Revision 10 debate now (rejected — nothing new is
known before the pilot runs; re-litigating now would not be evidence-based).
**Reversibility:** Reversible — the Phase 3 checklist item either confirms the provisional shape
(no-op) or changes it before only 17 methods, not 35, have been written against it.
**Recommendation, not a Phase 2 requirement (4th-pass spec review):** consider adding
`BackupService.CreateFullBackupAsync` (+ `BackupResult : IUnitOfWorkOutcome`) to Phase 2 alongside the
four pilot services, so D13's API-shape decision is made from real call sites covering all three
return shapes the survey found (§ 6b: `ValueTuple`, `IUnitOfWorkOutcome`, bare `Task`) rather than two
of three. **Trade-off:** `BackupService.CreateFullBackupAsync` is single-repository and low-risk, so
including it would strengthen D13's evidence at low cost — but it also grows the pilot beyond the
Song/Artist CRUD surface Helder's on-device Phase 3 check is scoped to (BUG-068/BUG-071 were found on
Song/Artist screens specifically), diluting the pilot's "prove it on the screens with the known bug"
focus (D11's own rationale). Not adopted by default; left as an option for Helder to accept or decline
before Phase 2 starts.

**Rationale:** the Revision 10 numbers (26/9, now corrected to 16/5 under D12) were derived from a
static audit of method bodies, not from writing the actual `ExecuteAsync` call sites. A 4-method pilot
across `SongService`/`ArtistService`/`ArtistResolutionService`/`SongResolutionService` — which includes
2 of the 5 in-scope multi-repository methods (`CreateSongWithUrlsAsync`, `CreateSongAsync`) and the
deepest nested chain in the whole spec (`SongResolutionService.CommitAsync` →
`ArtistResolutionService.CommitAsync` → `ArtistService.CreateArtistAsync`) — exercises enough of the
API's edge cases that its real call sites are better evidence than the audit for whether the uniform
shape actually reads well in practice.

## 9. Invariants & postconditions

- After any service write call, zero `AppDbContext` instances created by that call remain undisposed.
- At most one `SaveChangesAsync` executes per unit of work (excepting the REQ-UOW-08 flows, which are
  OUT OF SCOPE/deferred under D12 and not implemented by this spec — corrected 2026-08-04, non-blocking
  #3: this invariant should not read as if REQ-UOW-08 is an active exception this spec's code
  implements; it is retained only because the deferred item's future implementation will need the same
  exception documented — and excepting nested calls that join the ambient scope rather than opening a new
  one, § 6a).
- No `ChangeTracker` entry survives a unit of work.
- No singleton holds a repository, a service that writes, or an `AppDbContext`.
- **Save-skip on failure (Revision 8, § 6b):** `SaveChangesAsync` executes only when `body`'s
  returned result signals success per § 6b's two recognised shapes (`ITuple` with leading `bool`, or
  `IUnitOfWorkOutcome.Success`) — a mutation followed by a returned failure tuple does **not**
  persist. For the 3 genuinely signal-less methods, the dedicated no-signal overload (delegate type
  `Func<IServiceProvider, Task>`, nothing to inspect) always saves; this is the documented default (§ 6b
  "no-signal fallback"), never a silent guess, and is reachable only through that overload. This
  closes the exception-only reading of REQ-UOW-06: "no partial state survives" now holds for both the
  throw path and the far more common failure-tuple path.
- **Fail-closed on an unrecognised `TResult` (Revision 9, § 6b):** on the value-returning
  `ExecuteAsync<TResult>` overload, a `TResult` that is neither an `ITuple` with a leading
  `bool` nor an `IUnitOfWorkOutcome` implementer is never assumed to have succeeded — `ExecuteAsync`
  throws `InvalidOperationException` before any save is attempted. No unit of work may commit a
  mutation whose result type it could not interpret.
- **Only a write publishes an ambient scope (Revision 12, § 6 — Helder's decision 2026-08-04):**
  `ExecuteReadAsync` never sets `_ambientScope`. Therefore any ambient scope a write joins is another
  write's, and always saves. A read nested inside a write joins it (lookup-before-persist); a write
  nested inside a read opens its own scope and saves. The Revision 11 read/write flag and fail-closed
  throw are **withdrawn** — the failure mode is removed structurally rather than guarded.
  Corollary: the "at most one `AppDbContext` per unit of work" invariant applies to **write** units of
  work; a standalone read may hold its own context concurrently with one.

## 10. Migration plan (Phase order per D11, § 8; DRY Onion within each phase: Domain → Infra → Services → UI)

**Prerequisite (outside these phases):** the `Infra/Repository/*` / `Infra/Repositories/*` repository-family
merge (§ 8, Prerequisite decision) completes first. Every phase below assumes one merged family.

**Scope exclusion (D12, § 8):** `EventService`, `QueueService`, `QueueServiceNew`, and the repositories
they exclusively own (`EventRepository` in both families, `QueueRepository`,
`EventParticipationRepository`) are excluded from every phase below. The **21** in-scope methods (§ 8
D12 item 1) are the ones referenced as "the service methods"/"the in-scope set" in this table.

Sequential-only files (`workflow.md § Sequential-only file registry`): **`MauiProgram.cs`**,
**`AppDbContext.cs`**, and each spec `tasks.md` — never concurrent writers. `ServiceCollectionExtensions.cs`
should be treated as sequential-only for this change for the same reason.

| Phase | Layer | Work | Parallel? |
|---|---|---|---|
| 0 (RED tests) | Tests | Write the REQ-UOW-03/04 BUG-068 regression tests, plus new atomicity tests for `SongResolutionService.CommitAsync` (REQ-UOW-22), against **current `develop` HEAD, unchanged code**. Run them and confirm each FAILS for its exact stated reason (below), not merely "fails" — a test that goes red for the wrong reason is not evidence (`bug-tracking.md`: Critical ⇒ mandatory failing-test-first, no exceptions). This phase produces no production-code change. **Expected failure per test:** REQ-UOW-03 (BUG-068) — the create→read→update sequence on `SongRepository` throws `InvalidOperationException: ... already being tracked` on the second save, because the shared session-lifetime `AppDbContext` still tracks the entity from the first save. REQ-UOW-04 — the same exception, once per in-scope repository family parameterisation, for the same reason. REQ-UOW-22 (`SongResolutionService.CommitAsync`, 3-level nested chain) — **two** assertions are exercised and must be shown failing independently: (a) the happy-path Given/When/Then may already PASS the "no InvalidOperationException" assertion on current HEAD, because each nested call currently saves eagerly per inner call rather than sharing a context — so this assertion alone is not evidence of RED; (b) the added fault-injection Given/When/Then (`requirements.md` REQ-UOW-22) is the assertion expected to FAIL today, because nothing rolls back the outer `SongService` write when the inner `ArtistService.CreateArtistAsync` call throws — each nested call already committed its own `SaveChangesAsync` independently, so partial state (a `Song` row with no matching `Artist`) survives the fault, which is the opposite of "all-or-nothing". **REQ-UOW-23 (`QueueService.AddPersonToQueueAsync`) is out of scope under D12 and is NOT part of this phase** — it is carried by the deferred item, not tested here. | no — must complete and be observed RED for the stated reason before Phase 1 |
| 1 (Registration + primitive) | Domain/Contracts, Infra, Composition | Introduce `IUnitOfWork` — the single `ExecuteAsync<TResult>`/`ExecuteReadAsync<TResult>`/`ExecuteAsync` (no-signal) surface, **PROVISIONAL per D13**, § 6/§ 8 Revision 10; no typed overload — plus `IUnitOfWorkOutcome`; remove `SaveChangesAsync` from the **five standalone** repository interfaces that do not extend `IBaseRepository<T>` (`ISongRepository`, `IArtistRepository`, `ICatalogRepository`, `ISongKaraokeUrlRepository`, `IBackupRepository`) — **`IBaseRepository<T>.SaveChangesAsync()` itself is NOT removed** (§ 8, "IBaseRepository<T>.SaveChangesAsync() is NOT removed" decision — it is shared with the excluded `QueueService.cs`/`IEventRepository`/`IEventParticipationRepository` and removing it would force a de facto edit of an excluded file, REQ-UOW-31). Implement `UnitOfWork`, **including the `AsyncLocal` ambient-scope join** (§ 6a — ships now, not deferred). `MauiProgram.cs`: `AddDbContext` → `AddDbContextFactory(…, Scoped)`; register `IUnitOfWork`; remove the duplicate `IAppInfo` (REQ-UOW-21). Verify `App.xaml.cs:35,:54` scopes still resolve. **Do not touch any repository's pass-through/embedded `SaveChangesAsync` implementations yet** — those are deleted per-repository in Phase 2/4+ as each repository's owning service is actually wrapped, not wholesale here. | no — sequential-only (`MauiProgram.cs`, `AppDbContext.cs`) |
| 2 (PILOT) | Services + Infra | Wrap **only** `SongService`, `ArtistService`, `ArtistResolutionService`, `SongResolutionService` (**9** methods total across these four, re-derived by direct read of `Services/SongService.cs`, `Services/ArtistService.cs`, `Services/ArtistResolutionService.cs`, `Services/SongResolutionService.cs` — previously miscounted as "5": `CreateSongAsync`/`UpdateSongAsync`/`CreateSongWithUrlsAsync`/`DeleteSongsAsync` (4), `CreateArtistAsync`/`UpdateArtistAsync`/`DeleteArtistsAsync` (3), `CommitAsync` ×2 (2) — 4+3+2=9) in `ExecuteAsync`/`ExecuteReadAsync`, using the PROVISIONAL API shape from Phase 1; delete `SongRepository`'s and `ArtistRepository`'s pass-through `SaveChangesAsync` implementations; re-shape `ArtistResolutionService.CommitAsync` and `SongResolutionService.CommitAsync` to use the ambient join (REQ-UOW-09, REQ-UOW-22 — the deepest nested chain in the spec, `SongResolutionService.CommitAsync` → `ArtistResolutionService.CommitAsync` → `ArtistService.CreateArtistAsync`, is exercised here). **Also delete the `1a114c1` stopgap guard in `SongRepository.UpdateAsync` here** (REQ-UOW-18, no external gate — Helder cancelled the T10 re-run #6 gate 2026-08-04, § 8) — the stopgap and the pilot wrap touch the same method, so removing the old workaround and landing the real fix is one reviewable change. **Load-bearing rule (§ 8, BL-2) applies from this first phase on:** every lambda body resolves repositories from its own `IServiceProvider`, never from `_songRepository`/`_artistRepository` constructor fields (REQ-UOW-28). | partly — one agent per service, no file overlap |
| 3 (VERIFY — HARD GATE) | Tests + Helder | Confirm the Phase 0 regression tests now PASS for the pilot's scope (REQ-UOW-03, REQ-UOW-04's `ArtistRepository`/`SongRepository` rows, REQ-UOW-22). Run the full automated suite green. **Helder performs an on-device confirmation** that the pilot screens (Song/Artist CRUD) no longer reproduce BUG-068/BUG-071, starting from a **cold app start with no Queue/Event screen visited during the session** (Queue/Event code keeps the pre-existing shared-context defect by design, D12 LIVE RISK — visiting a Queue/Event screen first could poison the shared window-scope context for reasons unrelated to the pilot, making the pilot appear to fail for a defect it did not cause). **Decide the final `IUnitOfWork` API shape (D13, § 8) from the pilot's real call sites** — confirm the provisional shape or replace it; record the decision and rationale in the task-log. **HARD GATE (REQ-UOW-30):** no Phase 4+ task may be dispatched, claimed, or started until this phase's pass (tests + on-device + API-shape decision) is recorded in the task-log. | no — gate, not parallelizable |
| 4+ (Spread) | Services | Wrap the remaining **12** in-scope service methods (21 in-scope total minus the pilot's 9 — `BackupService.CreateFullBackupAsync` (1), `CatalogService.AddSongToCatalogAsync`/`RemoveSongFromCatalogAsync` (2), `PersonService.CreatePersonAsync`/`UpdatePersonAsync`/`DeletePersonsAsync` (3), `SongKaraokeUrlService.AddUrlAsync`/`RemoveUrlAsync`/`RecordPlayAsync` (3), `VenueService.CreateVenueAsync`/`UpdateVenueAsync`/`DeleteVenuesAsync` (3) — 1+2+3+3+3=12; `SongService.DeleteSongsAsync` is already wrapped in Phase 2 as part of the pilot's 9, not a Phase 4+ item) in `ExecuteAsync`/`ExecuteReadAsync`, using the shape Phase 3 finalised; delete the corresponding pass-through `SaveChangesAsync` implementations (`CatalogRepository`, `SongKaraokeUrlRepository`, `BackupRepository`, and the `BaseRepository<T>` pass-through for `PersonRepository`/`VenueRepository`). **Save-skip wiring (Revision 8, § 6b) — `BackupResult : IUnitOfWorkOutcome` is a blocking, same-phase prerequisite (Revision 9), not a later or optional step:** `BackupService.CreateFullBackupAsync`'s wrap MUST NOT be committed before `: IUnitOfWorkOutcome` is appended to `BackupResult`'s declaration (`Domain/ServicesInterfaces/IBackupService.cs:5`) — under fail-closed, wrapping `BackupService` with an unmarked `BackupResult` makes every call throw `InvalidOperationException` immediately. `SongKaraokeUrlService.RecordPlayAsync` is the **only** in-scope no-signal method (bare `Task`, § 8 D12 item 5 / `requirements.md` REQ-UOW-26 correction) and wraps via the single no-signal `ExecuteAsync(Func<IServiceProvider, Task> body, ct)` overload; every other in-scope method wraps via the single value-returning `ExecuteAsync<TResult>` overload and gets save-skip for free from the `ITuple` structural check. **Load-bearing rule (§ 8, BL-2) — MANDATORY for every method in this phase:** the lambda body MUST resolve repositories from its own `IServiceProvider` parameter, never from the service's constructor-injected field (REQ-UOW-28). Then: audit **every ViewModel or other UI type that constructor-injects a repository or a data-writing service, regardless of the ViewModel's own DI lifetime** (widened from "singletons only" — BL-4). `AddDbContextFactory(..., ServiceLifetime.Scoped)` still registers `AppDbContext` itself as scoped (§ 1 "Reviewer-finding correction"), so a **transient** ViewModel resolving a repository still resolves the window-lifetime context exactly like a singleton would. Confirmed transient ViewModels injecting repositories directly, all registered `AddTransient` in `MauiProgram.cs`: `MyVocaList/UI/ViewModels/QueueSongPickerViewModel.cs` (`ISongRepository`), `MyVocaList/UI/ViewModels/QueueManagementViewModel.cs` (`IPersonRepository`, `ISongRepository`, **and also `IEventService`/`IQueueServiceNew` — both excluded types, § 8 D12 item 6 corrected**), `MyVocaList/UI/ViewModels/PersonPickerViewModel.cs` (`IPersonRepository`) — none of the three injects a Queue/Event **repository** (§ 8 D12 item 6: only the ViewModel names contain "Queue"), so all three remain IN SCOPE for their repository-typed fields. Convert `QueueSongPickerViewModel` and `PersonPickerViewModel` fully to inject `IUnitOfWork` (plus the pre-existing singleton audit: `AppShellViewModel`, `AppShell`, `MauiProgram.cs:109-110`). **`QueueManagementViewModel` converts only its `IPersonRepository`/`ISongRepository` usage to `IUnitOfWork`-wrapped calls — its `IEventService`/`IQueueServiceNew` fields remain untouched, direct constructor injections, per § 8 D12 item 6's correction (REQUIRES HELDER'S CONFIRMATION).** **Remove the now-obsolete `DbLoadGate` (NB-1) only after (a) every in-scope consumer above is converted AND (b) the `page-load-frozen` regression suite (`DevCycleCraft/page-load-frozen/`) is confirmed green without the gate present** — removing it on condition (a) alone would prematurely stop serializing loads for ViewModels not yet converted; the gate's comment (`CrudListViewModelBase.cs:12-15`) also carries a SEPARATE `SQLITE-WORKAROUND` rationale (the `Microsoft.Data.Sqlite` sync-async freeze) with its own revert trigger (`INFRA_MSSQL`), independent of this spec's unit-of-work migration — removing the gate without confirming (b) would reintroduce that separate bug class (REQ-UOW-29, corrected BL-H). | yes — one agent per service/ViewModel, no file overlap; the `BackupResult` marker edit + `BackupService` wrap are one atomic sub-task, not splittable |
| LAST (Guidelines) | Docs + Tests | Amend `code-style-reference.md § DI Registration Conventions` (REQ-UOW-19, `amend:` + changelog) — this lands **after** Phase 4+, not before, per D11: a guideline documents an established pattern, and the pattern is not established until it has spread past the pilot. `TestDbContextFactory` alignment; delete the tracking-conflict workarounds at `CatalogRepositoryTests.cs:66` and `ArtistRepositoryTests.cs:366`; fix the stale `GetByIdAsync` "Tracked query" comment (REQ-UOW-20). Confirm the REQ-UOW-24/25/26/27 save-skip tests (authored in Phase 1/2 as the primitive and pilot land) stay GREEN after Phase 4+'s service rewrites. | partly |

**`QueueService.GetOrCreateDefaultEventAsync`/`RecordParticipationAsync` are out of scope under D12
(§ 8)** — both are `QueueService` members, one of the three excluded files. REQ-UOW-08, which
originally described wrapping this pair, is corrected in `requirements.md` to mark itself deferred;
this note is retained only as a pointer, not an instruction for any phase above.

**Branch note (NB-4, third-pass spec review — merge-ordering statement was missing):** the stopgap
lives on `feat/inline-artist-create` (`1a114c1`), which **has not merged into `develop`** as of this
revision (verified: `git log develop --oneline | grep 1a114c1` returns nothing; the branch exists only
as a remote ref). Two cases, both handled without re-gating Phase 2:
- **If `feat/inline-artist-create` merges into `develop` before Phase 2 runs:** the stopgap is present
  on `develop`'s `SongRepository.UpdateAsync`, and Phase 2 deletes it as originally described.
- **If `feat/inline-artist-create` has NOT merged by the time Phase 2 runs (the current state):**
  `develop`'s `SongRepository.UpdateAsync` does not contain the stopgap at all — there is nothing for
  Phase 2 to delete. Phase 2 becomes a no-op check ("confirm the stopgap is absent; if present, delete
  it") rather than a guaranteed deletion, and REQ-UOW-18 is satisfied vacuously in that case. This is
  not a gate on Phase 2's timing — Phase 2 runs unconditionally either way (the T10 re-run #6 gate
  remains cancelled by Helder 2026-08-04, § 8); it only changes whether the step does anything.
  Whichever case is true when Phase 2 executes, record which one in the task-log so REQ-UOW-18's
  verification evidence is unambiguous either way.

**Test-composition note (non-blocking #9, 4th-pass spec review, verified against source):**
`MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs:22` calls
`services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=:memory:"))` inside its own
independent `ServiceCollection`, separate from production `MauiProgram.cs`. **Correction to the
4th-pass finding:** this test is NOT broken by Phase 1 — it builds its own isolated composition and
does not depend on `MauiProgram.cs`'s registration choice, and no service constructor signature changes
until Phase 2/4+ wrap individual methods (Phase 1 touches no service constructor). It should still be
revisited at the LAST (Guidelines) phase for consistency with the new pattern (e.g. switching its own
`AddDbContext` call to `AddDbContextFactory` so the test's composition matches production), but "Phase 1
breaks it" is not supported by source — no phase task is added for this file on that basis; a LAST-phase
alignment note is sufficient.

**Testing tier (`testing.md`):** Level **A** — `UnitOfWork`, and every in-scope service method
re-shaped in Phase 2/Phase 4+, are business-logic/state-mutation paths requiring full
Red→Green→Refactor (Phase 0 is the Red; Phase 3 confirms Green for the pilot, LAST confirms Green for
the rest). Repository edits in Phase 1/Phase 4+ are Level **B**. Phase 1's registration edits are
Level **C**, covered by the existing BUG-021 DI regression tests plus one new composition test for
REQ-UOW-01.

## 11. Open questions for Helder

All five prior open questions are resolved (§ 8 decisions record the answers and rationale):

1. ~~Implicit save~~ — resolved: implicit, no explicit `uow.SaveAsync()` (Revision 5).
2. ~~Ambient-scope join~~ — resolved: adopted now, in Wave 3, not deferred (Revision 2).
3. ~~C vs C2~~ — **corrected 2026-08-04 (non-blocking #12, 4th-pass spec review): this entry recorded
   the superseded Revision 7 answer.** Revision 7's "typed overload is primary; `sp => …` is an escape
   hatch only" was superseded by Revision 10 (§ 8, APPROVED by Helder 2026-08-04): the typed overload is
   dropped entirely — there is one uniform value-returning `ExecuteAsync<TResult>(Func<IServiceProvider,
   Task<TResult>>, ct)` form and one no-signal form, both resolving from the lambda's `IServiceProvider`.
   See "Decision: drop the typed overload entirely" (§ 8, Revision 10) for the corrected rationale.
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

No open questions remain from the original seven. Three further decisions were made after this design
was first approved — D11 (pilot-first phase order), D12 (Queue/Event exclusion), D13 (API shape
deferred to the pilot) — recorded in § 8 and reflected in the § 10 phase table above. No open
questions remain for this design as of Revision 12. The next step is task-log/tasks.md breakdown
against the phase table in § 10.
