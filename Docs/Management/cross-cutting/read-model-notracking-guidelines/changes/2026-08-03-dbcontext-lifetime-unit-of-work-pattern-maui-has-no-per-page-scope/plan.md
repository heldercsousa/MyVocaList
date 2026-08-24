# DbContext Lifetime & Unit-of-Work Pattern — Implementation Plan

> **Status:** APPROVED by Helder 2026-08-04. Execution authorised for **Phases 0, 1 and 2 only** —
> stop at the Phase 3 gate and hand back. Phase 4+ is not to be started.

> **For agentic workers:** REQUIRED SUB-SKILL: use `superpowers:subagent-driven-development` to
> implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **Revision 2 (post plan-review).** A fresh-context plan-reviewer returned FAIL with 6 blocking
> findings. All six are fixed here; the changelog is in *§ Plan-review resolutions* at the end.

**Goal:** Make BUG-068/BUG-071 structurally impossible by giving every in-scope write its own
short-lived `AppDbContext`, expressed as one line per service method.

**Architecture:** Candidate C (`design.md § 6`). `AddDbContextFactory<AppDbContext>(…, ServiceLifetime.Scoped)`
replaces `AddDbContext`; a single `IUnitOfWork` primitive over `IServiceScopeFactory` creates one DI
scope (and one explicit transaction) per unit of work; service methods wrap their body in
`_uow.ExecuteAsync(async sp => { … })` and resolve repositories from the lambda's `IServiceProvider`.
Repository constructors and signatures do not change.

**Tech Stack:** .NET MAUI 10 (`net10.0-android`/`net10.0-ios`), C# 13, EF Core 10 + SQLite,
CommunityToolkit.Mvvm, Serilog. Tests: `MyVocaList.Tests` on plain `net10.0` — xUnit + Moq + FsCheck,
plain `Assert.*` (no FluentAssertions).

---

## Context

`MauiProgram.cs:61-68` registers `AppDbContext` with `AddDbContext`, whose default lifetime is Scoped.
MAUI creates a DI scope **per Window**, not per page — so a single-window mobile app has exactly one
scope, and therefore **one `AppDbContext` for the whole app session**. `NoTracking` suppresses tracking
of query *results* only; entities left tracked by a prior `Add`/`Update` + save stay in the change
tracker for the rest of the session. A later read-then-`Update` of the same row throws
`InvalidOperationException: another instance with the same key value {'Id'} is already being tracked`.

That is **BUG-068/BUG-071 (Critical)**, found on the Song/Artist CRUD screens during T10 re-run #5. It
was fixed at the repository seam by a hand-rolled `ChangeTracker` detach loop (`1a114c1`), explicitly
recorded as a **stopgap**. This work deletes the stopgap and replaces it with the real fix.

Two further consequences of an app-lifetime context, both documented by EF Core: `DbContext` is not
thread-safe, and an `InvalidOperationException` can put a context into an **unrecoverable** state — so
one BUG-068 throw poisons the shared context for the remainder of the session.

**Intended outcome:** 21 in-scope service methods each own their context; the 5 standalone pass-through
`SaveChangesAsync` implementations disappear; the DI guideline that caused this (`AddScoped` for
repositories/services, an ASP.NET-shaped rule wrong for MAUI) is amended.

---

## Verification findings — corrections to the spec

Per the instruction to treat the spec's quantitative claims as claims to verify, every load-bearing
number was re-derived from `develop` HEAD (`5c03a4dc`) by read-only agents. **Confirmed:** the pilot's
9 methods, Phase 4+'s 12, the 5 standalone interfaces, the 27 `AddScoped` registrations, the duplicate
`IAppInfo` at `MauiProgram.cs:86`/`:157`, `RecordPlayAsync` returning bare `Task`, `BackupResult` at
`IBackupService.cs:5`, the `ExecuteDeleteAsync` delete paths, `DbLoadGate` at `CrudListViewModelBase.cs:16`,
and the 26 `TODO [BUG-071 / UOW]` markers (none in any file this plan touches).

| # | Finding | Effect on the plan |
|---|---|---|
| **F1** | **Phase 1 as specified does not compile.** § 10 Phase 1 removes `SaveChangesAsync` from the 5 standalone repository *interfaces* while deferring service wraps to Phase 2/4+. Eleven live call sites reach it through those interfaces: `SongService.cs:96,146,214`, `ArtistService.cs:69,96`, `ArtistResolutionService.cs:113,133`, `CatalogService.cs:40`, `SongKaraokeUrlService.cs:61,74,82`, `BackupService.cs:61` — plus test callers (F7). | **Interface-member removal moves out of Phase 1** into the task that wraps the member's *last remaining caller*: `IArtistRepository`→2.3, `ISongRepository`→2.4, `ICatalogRepository`→4.1, `ISongKaraokeUrlRepository`→4.3, `IBackupRepository`→4.5. REQ-UOW-11 fully satisfied by end of Phase 4+. |
| **F2** | ~~REQ-UOW-04's Person/Venue rows cannot be green at Phase 3, so skip-mark them.~~ **SUPERSEDED at execution 2026-08-04 by F8 — the whole problem was imaginary.** All three families already pass; nothing needs skipping. | Skip-marking dropped entirely. Phase 3.1's gate becomes **`Skipped: 0`**, not "exactly 2", and Tasks 4.2/4.4 lose their unskip steps. |
| **F8** | **REQ-UOW-04's premise is false — verified at execution.** The AC assumes `ArtistRepository`, `PersonRepository` and `VenueRepository` carry the same latent defect as `SongRepository`. They do not, and cannot be made to fail on this path: `ArtistRepository.GetByIdAsync` (`:79-80`) calls `.AsTracking()`, and `PersonRepository`/`VenueRepository` inherit `BaseRepository<T>.GetByIdAsync` (`:24-29`) which uses `FindAsync` — **both identity-resolve and hand back the already-tracked instance**. Only `SongRepository.GetByIdAsync` (`:53-54`) is a bare `FirstOrDefaultAsync` under the global `NoTracking` default, returning a second detached instance that then collides at `Update`. Finding F6 noticed this asymmetry and filed it as a footnote; it actually falsifies an AC. | **Helder's decision 2026-08-04: Option A.** REQ-UOW-04 is satisfied **vacuously** for Artist/Person/Venue (same treatment as F3/F5/F6). The three tests are kept as **passing characterization tests** — they lock the behavior in through the refactor and would catch a future edit that removed `.AsTracking()` from `ArtistRepository`, which is exactly how `SongRepository` reached its current state. Each carries a comment naming its own family's reason. |
| **F3** | **The stopgap is not on `develop`.** `1a114c1` is not an ancestor of HEAD; `SongRepository.UpdateAsync` (`:128-133`) is a plain `_db.Songs.Update(song)` with no `ChangeTracker` reference anywhere in the file. | REQ-UOW-18 satisfied **vacuously** (`§ 10` NB-4 case 2). Task 2.4 re-checks and records. |
| **F4** | **No service-over-real-DB integration test exists.** All 12 integration classes test *repositories*; every `Unit/Services/*Tests.cs` is Moq-over-repository. Only `AppServicesRegistrationTests.cs:23` composes `AddAppServices()`. This is why the unit suite missed BUG-068. | Phase 0's first task builds a real DI-composition harness. New infrastructure, not a tweak. |
| **F5** | ~~`ArtistRepositoryTests.cs:257`/`:358` are deletable workarounds.~~ **WITHDRAWN — this finding of mine was wrong** (caught by plan-review). `:257` is the arrange step that *creates* the detached condition `UpdateAsync_DetachedInstance_Updates` exists to exercise (BUG-018's AC) — deleting it silently deletes the AC. `:358` sits under a deliberately-overridden `QueryTrackingBehavior.TrackAll` (`:352`); deleting it leaves the seeded `Artist` tracked and **fails** the test's own `Assert.Empty(...ChangeTracker.Entries<Artist>())` at `:364`. | **REQ-UOW-17 is partially vacuous.** Exactly one genuine workaround exists: `CatalogRepositoryTests.cs:67` (comment `:66`). The spec's `ArtistRepositoryTests.cs:366` reference resolves to a comment line and to no workaround at all. Recorded like F3/F6. |
| **F6** | **REQ-UOW-20's target does not exist.** Repo-wide search for "Tracked query"/"tracked query" across all `.cs` files: **zero** hits (only this spec's own `requirements.md:512` / `design.md:1684`). | Vacuous; recorded at L.4. Real asymmetry worth a note: `ArtistRepository.GetByIdAsync:80` calls `.AsTracking()`; `SongRepository.GetByIdAsync:52-54` does not. |
| **F7** | **~9 existing test files break on these edits** and no phase of the spec accounts for them: every `Unit/Services/*Tests.cs` constructs its service directly with Moq'd repositories (so a new `IUnitOfWork` ctor parameter breaks construction), and several `.Setup(r => r.SaveChangesAsync(...))` on interfaces this plan deletes the member from (`ArtistResolutionServiceTests.cs:206,234`; `SongServiceTests.cs:522,524,559`; `SongKaraokeUrlServiceTests.cs:81,168`; `CatalogServiceTests.cs:19`; `BackupServiceTests.cs:71`). Two **integration** tests also call it directly (`CatalogRepositoryTests.cs:64,71`; `BackupRepositoryTests.cs:40,58,75,91`) and need `_db.SaveChangesAsync()` instead — a different edit. | New **Task 1.4** produces a shared `UnitOfWorkMocks` helper; every service-wrap task names its affected test files in `Files owned`. |

### Sequencing: the merge does **not** block Phase 0

Resolved by the most-recent-commit rule: `c5822933` *"repository-family merge no longer blocks Phase 0
— moves to Phase 3.5"*, `9f2396b2` *"sequencing DECIDED — option (a), after the pilot"*, and
`LEDGER.md:14` *"NOTHING BLOCKS PHASE 0"* supersede the earlier prerequisite position. Verified safe:
the four pilot services touch only `Infra/Repository/` (singular) and `Domain/Entity/` (singular).

**Phases 0–3 are executable now.** The blocker sits one step later: **Phase 3.5** is `💡 Pending` with
a `README.md` only — no `requirements.md`, no `design.md`, no plan. **Phase 4+ and LAST cannot start
until that item is specced, planned and shipped.**

---

## Global Constraints

Every task's requirements implicitly include this section.

- **Worktree mandatory**, base branch **`develop`** — never `main`, never edit `develop` directly
  (hook-enforced). Verify `git merge-base --is-ancestor develop HEAD` after creating any worktree.
- **Docs land on `develop`** — `task-log.md`, `tasks.md`, `plan.md`, LEDGER updates committed by the
  orchestrator to `develop`, never stranded on a task branch.
- **NEVER touch these files (REQ-UOW-31), in any Phase 0–4+ commit:** `Services/EventService.cs`,
  `Services/QueueService.cs`, `Services/QueueServiceNew.cs`, `Infra/Repository/EventRepository.cs`,
  `Infra/Repositories/EventRepository.cs`, `Infra/Repositories/QueueRepository.cs`, and the
  `EventParticipationRepository` interface/implementation. `git grep -n "TODO \[BUG-071 / UOW\]"` = 26
  hits, none in a file this plan edits.
- **Sequential-only files:** `MauiProgram.cs`, `AppDbContext.cs`, `Extensions/ServiceCollectionExtensions.cs`,
  `GlobalUsings.cs`, `Directory.Build.props`, `tasks.md`.
- **The load-bearing rule (REQ-UOW-28) — MANDATORY in every wrapped method:** inside an
  `ExecuteAsync`/`ExecuteReadAsync` lambda, resolve **every** repository *and nested service* from the
  lambda's own `sp`. No `_`-prefixed constructor-injected field of the enclosing service may appear
  inside a lambda body — repository-typed *or* service-typed. Fields that matter today:
  `SongResolutionService._songRepository/._artistResolution/._songService`;
  `ArtistResolutionService._artistRepository/._artistService`;
  `SongService._songRepository/._artistRepository/._urlRepository/._urlService`;
  `ArtistService._artistRepository/._songRepository/._catalogRepository`.
- **Leading `bool` = success** (REQ-UOW-24). `ResultSignalsSuccess` reads `t[0]`; a tuple whose first
  `bool` means anything else is silently misread. Documentation-enforced — reviewers check tuple
  *semantics*, not just shape.
- **No parallel fan-out inside a unit of work** — never `Task.WhenAll` over two `ExecuteAsync` calls.
- **Testing tier:** Level **A** for `UnitOfWork` and every wrapped service method; **B** for repository
  edits; **C** for registration edits.
- **Repository tests use real SQLite temp files**, never the in-memory provider; never mock `DbContext`.
- **Test command:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` (targets `net10.0`; the
  Android TFM is not required and fails locally with `XARLP7024`).
- Commit after every task (`/sln-commit`); English only; business logic in Services.

---

## File Structure

**New production files**

| File | Responsibility |
|---|---|
| `Domain/UnitOfWork/IUnitOfWork.cs` | Three-member contract: `ExecuteAsync<TResult>`, `ExecuteAsync` (no-signal), `ExecuteReadAsync<TResult>`. No EF dependency. |
| `Domain/UnitOfWork/IUnitOfWorkOutcome.cs` | `bool Success { get; }` opt-in marker for named result types. |
| `Infra/UnitOfWork/UnitOfWork.cs` | The single implementation: scope creation, explicit transaction, `AsyncLocal` ambient join, `ResultSignalsSuccess`, fail-closed throw. ~70 lines. The only hand-rolled machinery in the design. |

**New test files**

| File | Responsibility |
|---|---|
| `MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs` | Real `ServiceProvider` over a SQLite temp file with **both** interceptors, mirroring production. `CreateLegacy()` (current `AddDbContext`, used to observe RED) and `Create()` (post-Phase-1). |
| `MyVocaList.Tests/Infrastructure/UnitOfWorkMocks.cs` | `Mock<IUnitOfWork>` passthrough for the Moq-based unit tests (Task 1.4). |
| `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` | REQ-UOW-03, REQ-UOW-04. |
| `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` | REQ-UOW-09, REQ-UOW-22, REQ-UOW-34, REQ-UOW-24 nested-precedence. |
| `MyVocaList.Tests/Integration/UnitOfWork/SaveSkipTests.cs` | REQ-UOW-24/25/26/27/33. |
| `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkLifetimeTests.cs` | REQ-UOW-02/05/06/14/15. |
| `MyVocaList.Tests/Unit/DependencyInjection/UnitOfWorkCompositionTests.cs` | REQ-UOW-01/21. |

**Modified production files** — the wrap is identical every time, so it is described once here rather
than enumerated per file. In each wrapped method the body moves inside
`_uow.ExecuteAsync<T>(async sp => { … }, ct)`, every `_xRepository` reference becomes
`sp.GetRequiredService<IXRepository>()` resolved at the top of the lambda, and the trailing
`await _xRepository.SaveChangesAsync(ct);` is deleted. Representative paths: `Services/SongService.cs` (4),
`Services/ArtistService.cs` (3), `Services/ArtistResolutionService.cs` (1),
`Services/SongResolutionService.cs` (1); then Phase 4+ across
`Services/{Catalog,Person,Venue,SongKaraokeUrl,Backup}Service.cs`. Registration edits are confined to
`MyVocaList/MauiProgram.cs`.

---

## Phase 0 — RED tests (no production code)

> **Exit condition:** every *regression* test in Tasks 0.2–0.4b has been **seen** to fail, and its
> failure reason read and matched against the expected reason stated in the task. A test that goes red
> for the wrong reason is not evidence (`bug-tracking.md`: Critical ⇒ mandatory failing-test-first).
> **Task 0.1 is harness infrastructure and is expected to PASS** — it is exempt from this condition.

### Task 0.1: Real-DI test harness

**Files owned:** Create `MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs`; Create
`MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkTestHostTests.cs`
**Consumes:** nothing (first task).
**Produces:** `UnitOfWorkTestHost` — `static UnitOfWorkTestHost CreateLegacy(Action<IServiceCollection>? customize = null)`,
`IServiceProvider Services { get; }`, `IServiceScope Scope { get; }`, `T Resolve<T>() where T : notnull`,
`AppDbContext Db { get; }`, `ValueTask DisposeAsync()`. Task 1.3 adds
`static UnitOfWorkTestHost Create(Action<IServiceCollection>? customize = null)` with the identical signature shape.
**Demo:** a test proves two different services resolved from the host share one `AppDbContext` instance.

- [ ] **Step 1: Write the harness.** Composition modelled on `AppServicesRegistrationTests.cs:23` and
  the SQLite/interceptor setup on `TestDbContextFactory.cs`, but over a temp **file** (not `:memory:`)
  and with **both** production interceptors. `ITransactionLogWriter` is **not** registered by
  `AddAppServices()` — the harness must supply it, or REQ-UOW-14/15 cannot be tested (finding B3).

```csharp
public sealed class UnitOfWorkTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _root;
    private readonly string _dbPath;

    public IServiceScope Scope { get; }
    public IServiceProvider Services => Scope.ServiceProvider;
    public AppDbContext Db => Resolve<AppDbContext>();
    public RecordingTransactionLogWriter Log { get; }
    public T Resolve<T>() where T : notnull => Services.GetRequiredService<T>();

    private UnitOfWorkTestHost(ServiceProvider root, string dbPath, RecordingTransactionLogWriter log)
    {
        _root = root; _dbPath = dbPath; Log = log; Scope = root.CreateScope();
    }

    /// <summary>Current production composition: one scoped AppDbContext for the whole session.</summary>
    public static UnitOfWorkTestHost CreateLegacy(Action<IServiceCollection>? customize = null)
    {
        var (services, dbPath, log) = BaseCollection();
        services.AddDbContext<AppDbContext>((sp, o) => Configure(sp, o, dbPath));
        return Build(services, dbPath, log, customize);
    }

    private static (ServiceCollection, string, RecordingTransactionLogWriter) BaseCollection()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"uow_test_{Guid.NewGuid():N}.db");
        var log = new RecordingTransactionLogWriter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CollationInterceptor>();
        services.AddSingleton<ITransactionLogWriter>(log);
        services.AddSingleton<TransactionLogInterceptor>();
        services.AddAppServices();
        return (services, dbPath, log);
    }

    private static void Configure(IServiceProvider sp, DbContextOptionsBuilder o, string dbPath) => o
        .UseSqlite($"Data Source={dbPath}")
        .AddInterceptors(
            sp.GetRequiredService<CollationInterceptor>(),
            sp.GetRequiredService<TransactionLogInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    private static UnitOfWorkTestHost Build(
        ServiceCollection services, string dbPath, RecordingTransactionLogWriter log,
        Action<IServiceCollection>? customize)
    {
        customize?.Invoke(services);   // last-wins override point, used by fault injection (Task 0.4)
        var host = new UnitOfWorkTestHost(services.BuildServiceProvider(), dbPath, log);
        host.Db.Database.EnsureCreated();
        return host;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.Database.EnsureDeletedAsync();
        Scope.Dispose();
        await _root.DisposeAsync();
        try { File.Delete(_dbPath); } catch (IOException) { /* temp file, best effort */ }
    }
}

/// <summary>Captures transaction-log entries in memory so REQ-UOW-14/15 can assert on them.</summary>
public sealed class RecordingTransactionLogWriter : ITransactionLogWriter
{
    public List<string> Entries { get; } = [];
    // implement each ITransactionLogWriter member by appending to Entries; no file I/O.
}
```

- [ ] **Step 2: Write the smoke test.** It must extract the context *from the services*, not from the
  host, or it proves nothing:

```csharp
[Fact]
public async Task LegacyHost_TwoDifferentServices_ShareOneAppDbContextInstance()
{
    await using var host = UnitOfWorkTestHost.CreateLegacy();

    // Both services resolve AppDbContext through their repositories; under AddDbContext(Scoped)
    // in a single scope, that is one and the same instance — the precondition for BUG-068.
    var artists = host.Resolve<IArtistService>();
    var songs = host.Resolve<ISongService>();
    Assert.NotNull(artists);
    Assert.NotNull(songs);

    var (ok, _, artist) = await artists.CreateArtistAsync("Shared Context Probe");
    Assert.True(ok);
    // The entity created through ArtistService is still tracked by the context the host resolves —
    // i.e. one context spans both service calls.
    Assert.Contains(host.Db.ChangeTracker.Entries<Artist>(), e => e.Entity.Id == artist!.Id);
}
```

- [ ] **Step 3: Run.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter FullyQualifiedName~UnitOfWorkTestHostTests`
  Expected: **PASS**. If the `Assert.Contains` fails, the harness is not reproducing the production
  lifetime and every downstream RED test would be invalid — stop and fix the harness.
- [ ] **Step 4: Commit.** `test(uow): real-DI integration harness over SQLite temp file (Phase 0)`

### Task 0.2: REQ-UOW-03 — the BUG-068 regression test

**Files owned:** Create `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
**Consumes:** `UnitOfWorkTestHost.CreateLegacy()` (0.1). **Demo:** the tracking conflict is reproduced
on demand, from the service layer, with the exact production exception.

- [ ] **Step 1: Write the failing test**

```csharp
// [AC] REQ-UOW-03: create -> read -> update through the normal write path must not throw
// "already being tracked", and the update must persist.
[Fact]
public async Task Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict()
{
    await using var host = UnitOfWorkTestHost.CreateLegacy();
    var artists = host.Resolve<IArtistService>();
    var songs = host.Resolve<ISongService>();

    var (artistOk, _, artist) = await artists.CreateArtistAsync("Tracking Artist");
    Assert.True(artistOk);

    var (createOk, _, song) = await songs.CreateSongAsync(artist!.Id, "Original Title");
    Assert.True(createOk);

    var (updateOk, message) = await songs.UpdateSongAsync(
        song!.Id, "Updated Title", featuredArtists: null, lyrics: null, hasManualEdits: false);

    Assert.True(updateOk, message);
    var reread = await songs.GetSongByIdAsync(song.Id);
    Assert.Equal("Updated Title", reread!.Title);
}
```

- [ ] **Step 2: Run it and read the failure**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter FullyQualifiedName~Bug068RegressionTests`
Expected: **FAIL** with `InvalidOperationException` whose message contains
`another instance with the same key value`, thrown from `_db.Songs.Update(song)`
(`SongRepository.cs:131`) because the shared session-lifetime context still tracks the `Song` from
`CreateSongAsync`'s save. **Paste the exact exception text into the task-log.** Any other failure
reason (null `artist`, a validation tuple) means the test is wrong — fix the test, do not proceed.

- [ ] **Step 3: Commit.** `test(uow): RED — BUG-068 tracking conflict on create->read->update (REQ-UOW-03)`

### Task 0.3: REQ-UOW-04 — the same guarantee for the other in-scope repositories

**Files owned:** Modify `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
**Consumes:** 0.1, 0.2. **Demo:** the defect is shown to be repository-family-wide, not Song-specific.

- [ ] **Step 1: Write three separate `[Fact]` methods** — one per in-scope repository family. They are
  *not* a `[Theory]`: the three services have different signatures, so the arrange/act differs.

| Test | Create call | Update call | Assert |
|---|---|---|---|
| `Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` | `IArtistService.CreateArtistAsync("Repo Probe Artist")` | `UpdateArtistAsync(id, "Renamed Artist")` | `(true, _)`; re-read via `SearchArtistsByNameAsync("Renamed Artist")` shows the new name |
| `Person_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` | `IPersonService.CreatePersonAsync("Repo Probe Person")` | `UpdatePersonAsync(id, "Renamed Person")` | `(true, _)`; re-read shows the new name |
| `Venue_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` | `IVenueService.CreateVenueAsync("Repo Probe Venue")` | `UpdateVenueAsync(id, "Renamed Venue")` | `(true, _)`; re-read shows the new name |

Each carries its own `// [AC] REQ-UOW-04: <repository family>` first line. Each uses
`await using var host = UnitOfWorkTestHost.CreateLegacy();` and resolves its service via `host.Resolve<T>()`.
Note `CreateVenueAsync`/`UpdateVenueAsync` take no `CancellationToken` (`VenueService.cs:57,77`).

- [ ] **Step 2: Run all three and read each failure.** Expected: **FAIL**, same
  `InvalidOperationException … already being tracked`, once per repository. Record all three exception
  texts in the task-log — this is the RED evidence, and it is captured **before** any skip is applied.
- [ ] **Step 3: ~~Skip-mark~~ CANCELLED by F8.** All three tests pass on unchanged code and stay
  active as characterization tests. No `[Fact(Skip = …)]` anywhere. Each carries a second comment line
  naming its OWN family's reason (`.AsTracking()` for Artist; `BaseRepository.FindAsync` for
  Person/Venue) so a later reader does not mistake a passing test for a missing one.
- [ ] **Step 4: Run the full suite.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`
  Expected: pre-existing suite still passes; **0 skipped**; `Bug068RegressionTests` red on
  REQ-UOW-03 only — the three REQ-UOW-04 tests PASS (F8).
- [ ] **Step 5: Commit.** `test(uow): RED — REQ-UOW-04 across Artist/Person/Venue repositories`

### Task 0.4: REQ-UOW-22 — atomicity of the 3-level nested chain

**Files owned:** Create `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`
**Consumes:** `UnitOfWorkTestHost.CreateLegacy(customize)` (0.1). **Demo:** partial state provably
survives a mid-chain fault today.

Chain under test: `SongResolutionService.CommitAsync` (`:151-248`) → `_songService.CreateSongAsync`
(`:166`) / `CreateSongWithUrlsAsync` (`:184`) / `UpdateSongAsync` (`:208`,`:232`), and →
`_artistResolution.CommitAsync` (`:260`) → `_artistService.CreateArtistAsync` (`ArtistResolutionService.cs:121`).

- [ ] **Step 1: Write the happy-path test** — `CommitAsync` with a novel artist and novel song ⇒
  exactly one `Artist` row, exactly one `Song` row, no tracking exception. **Expect PASS on current
  HEAD** — each nested call saves eagerly today, so this assertion alone is **not** RED evidence.
  State that explicitly in the task-log so the RED claim rests only on Step 2.
- [ ] **Step 2: Write the fault-injection test — this is the RED one.** Use the `customize` hook from
  Task 0.1 to replace `IArtistRepository` with a decorator over the real one that throws
  `InvalidOperationException("injected")` from `AddAsync`, i.e. at the innermost point of the chain,
  after the outer `SongService` write has already run:

```csharp
await using var host = UnitOfWorkTestHost.CreateLegacy(services =>
    services.AddScoped<IArtistRepository>(sp =>
        new ThrowOnAddArtistRepository(
            ActivatorUtilities.CreateInstance<ArtistRepository>(sp))));   // last registration wins
```
  Assert: the exception propagates, **and** a fresh read finds **no `Song` row and no `Artist` row**
  (all-or-nothing, REQ-UOW-22's second Given/When/Then). Define `ThrowOnAddArtistRepository` inside the
  test file as a private sealed decorator forwarding every `IArtistRepository` member to the inner
  instance except `AddAsync`, which throws.
- [ ] **Step 3: Write the nested-failure-tuple test** (REQ-UOW-24's nested-call precedence clause):
  drive the same chain so the innermost call returns a `(false, …)` **tuple** rather than throwing, and
  assert the failure reaches `SongResolutionService.CommitAsync`'s own returned tuple. On HEAD this
  will show the outer write **persisted anyway** — record that as the second RED.
- [ ] **Step 4: Run and read the failures.** Expected: Step 2's test **FAILS** because a `Song` row
  survives the fault (each nested call already committed its own `SaveChangesAsync`), which is the
  opposite of all-or-nothing; Step 3's test **FAILS** because the mutation persisted despite the
  failure tuple. Record both assertion outputs.
- [ ] **Step 5: Commit.** `test(uow): RED — REQ-UOW-22/24 nested chain is not atomic (partial state survives)`

### Task 0.4b: REQ-UOW-09 — `ArtistResolutionService.CommitAsync` observable outcome

**Files owned:** Modify `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`
**Consumes:** 0.1. **Demo:** the CreateNew branch's contract is pinned before it is re-shaped.

`design.md § 8` D11 assigns Phase 0 "the REQ-UOW-03/04 BUG-068 regression tests **plus the
REQ-UOW-22/09 atomicity tests**". REQ-UOW-09 needs its own test — Task 2.3 re-shapes exactly this
method, and its guarantee is that the *observable outcome does not change*.

- [ ] **Step 1: Write the test** for REQ-UOW-09's Given/When/Then: an `ArtistCandidate` with an
  external provider and id, `choice = ResolutionChoice.CreateNew` ⇒ **exactly one** `Artist` row with
  that name, `ExternalProvider` and `ExternalId` set, and the returned `artistId` matching that row.
- [ ] **Step 2: Run.** **Expect PASS on HEAD** — this is a characterization test protecting an existing
  guarantee through the Task 2.3 rewrite, not a defect reproduction. Record it as such, so the Phase 0
  exit condition is not misread as "everything must be red".
- [ ] **Step 3: Commit.** `test(uow): pin REQ-UOW-09 observable outcome before the CommitAsync re-shape`

### Task 0.5: Phase 0 gate

- [ ] Record in `task-log.md`: every test written, expected-vs-actual failure reason, and the two
  skip-marked tests with their justification. Confirm **no production file changed** —
  `git diff develop --name-only` lists test files only.
- [ ] **Carry the skip-removal obligation forward** *(finding F2, reviewer condition 1)*: add to the
  UOW item's `README.md` `gate:` line. **Superseded by F8: there are no skipped tests, so
  there is no carry-forward obligation.** Record instead that REQ-UOW-04 is vacuously satisfied for
  Artist/Person/Venue and why.
- [ ] Register the O3 hazard (`BackupService.RestoreFromBundleAsync:137`) as a bug now, per proactive
  triage: `python .claude/scripts/backlog/backlog_gen.py register --kind bug --severity Major …`
- [ ] `backlog_gen.py status <ID> "🟡 In Progress"`, then `regen`.

---

## Phase 1 — Registration + primitive

> **Amended per F1:** this phase adds `IUnitOfWork` and swaps the registration. It removes **no**
> interface member. It changes no service method body, so the Phase 0 tests stay red.

### Task 1.1: The contracts

**Files owned:** Create `Domain/UnitOfWork/IUnitOfWork.cs`, `Domain/UnitOfWork/IUnitOfWorkOutcome.cs`
**Consumes:** nothing. **Produces:** the two interfaces below. **Demo:** solution builds with 0 errors.

- [ ] **Step 1: Write `IUnitOfWorkOutcome`**

```csharp
namespace MyVocaList.Domain.UnitOfWork;

/// <summary>Opt-in marker for named result types that carry a success signal but are not a
/// ValueTuple. Mandatory (not optional) for any named result type passed to
/// <see cref="IUnitOfWork.ExecuteAsync{TResult}"/> — an unmarked type throws (fail-closed).</summary>
public interface IUnitOfWorkOutcome
{
    bool Success { get; }
}
```

- [ ] **Step 2: Write `IUnitOfWork`** — the three members exactly as `design.md § 6` Revision 10
  specifies, carrying that section's XML docs (save-skip, fail-closed, read-never-saves), plus a
  `<remarks>` recording the shape as **PROVISIONAL per D13**, decided at Phase 3.

```csharp
public interface IUnitOfWork
{
    Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
    Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);
    Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
}
```

- [ ] **Step 3: Build.** `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` — 0 errors.
- [ ] **Step 4: Commit.** `feat(uow): IUnitOfWork + IUnitOfWorkOutcome contracts (provisional shape, D13)`

### Task 1.2a: `UnitOfWork` implementation — lifetime, concurrency, interceptors

**Files owned:** Create `Infra/UnitOfWork/UnitOfWork.cs`; Create
`MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkLifetimeTests.cs`
**Consumes:** `IUnitOfWork`/`IUnitOfWorkOutcome` (1.1), `UnitOfWorkTestHost` (0.1).
**Produces:** `UnitOfWork` + its private `static bool ResultSignalsSuccess<TResult>(TResult)`.
**Demo:** N units of work ⇒ N distinct disposed contexts; interceptors still fire.

*(Split from a single oversized task per reviewer sizing finding — 1.2a is the mechanism, 1.2b is the
signal semantics.)*

- [ ] **Step 1: Write the failing tests,** each with its `// [AC] REQ-UOW-NN:` first line:
  - REQ-UOW-02 — three sequential `ExecuteAsync` calls capture three distinct `AppDbContext` references
    from inside their bodies (`Assert.NotSame` pairwise); each throws `ObjectDisposedException` on use
    after its unit of work returned.
  - REQ-UOW-05 — two overlapping units of work (started, interleaved by awaiting a `TaskCompletionSource`
    inside each body, **not** `Task.WhenAll` — see Global Constraints) see distinct contexts and no
    `InvalidOperationException`.
  - REQ-UOW-06 — a body that throws leaves the next unit of work able to write successfully.
  - REQ-UOW-14 — a `NOCASE_NOACCENT`-collated query still returns the accent-insensitive match inside a
    unit of work, and `host.Log.Entries` gains an entry for a save.
  - REQ-UOW-15 — after two sequential writes, the second `host.Log.Entries` entry does **not** contain
    the first write's entity — the interceptor sees only its own unit of work.
- [ ] **Step 2: Run.** All fail — `UnitOfWork` does not exist.
- [ ] **Step 3: Implement `UnitOfWork`** exactly as the `design.md § 6` code block specifies —
  `AsyncLocal<IServiceProvider?>` published by `ExecuteAsync` **only** (never `ExecuteReadAsync`,
  Revision 12), `CreateAsyncScope()`, `BeginTransactionAsync` immediately after scope creation, commit
  on success signal / rollback on failure signal / rollback-and-rethrow on exception,
  `_ambientScope.Value = null` in `finally`. **Copy that block — it is complete and reviewed; do not
  re-derive it.** `ResultSignalsSuccess` may be a `throw new NotImplementedException()` stub at this
  step; Task 1.2b implements it.
- [ ] **Step 4: Run.** Green. **Step 5: Commit.**
  `feat(uow): UnitOfWork — scope-per-operation with explicit transaction and ambient join`

### Task 1.2b: `ResultSignalsSuccess` — save-skip, fail-closed, transactional rollback

**Files owned:** Modify `Infra/UnitOfWork/UnitOfWork.cs`; Create
`MyVocaList.Tests/Integration/UnitOfWork/SaveSkipTests.cs`; Modify
`MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (REQ-UOW-34 only)
**Consumes:** 1.2a. **Demo:** a mutation followed by a failure tuple provably does not persist.

- [ ] **Step 1: Write the failing tests:**
  - REQ-UOW-24 (tuple) — body mutates a real `Song` via the repository, returns `(false, "…")` ⇒ fresh
    read shows the **original** title.
  - REQ-UOW-24 (`IUnitOfWorkOutcome`) — same, with a test-local
    `sealed record ProbeOutcome(bool Success, string Message) : IUnitOfWorkOutcome` returning
    `Success == false`. *(The spec's exemplar is `BackupResult`, but `IBackupRepository` is registered
    in `MauiProgram.cs`, not `AddAppServices()`; the real-`BackupResult` counterpart is Task 4.5's
    obligation. This synthetic type covers the shape now — reviewer non-blocking #1.)*
  - REQ-UOW-25 — the success counterpart of both shapes: mutation **is** persisted.
  - REQ-UOW-26 — the no-signal `Func<IServiceProvider, Task>` overload always saves on non-throwing return.
  - REQ-UOW-27 — a body returning `sealed record UnmarkedResult(int Value)` throws
    `InvalidOperationException` whose message names `UnmarkedResult`, `IUnitOfWorkOutcome`, **and** the
    no-signal overload; and a mutation performed before the return did **not** persist.
  - REQ-UOW-33 (`ExecuteDeleteAsync`) — body calls `ArtistRepository.DeleteAsync` then returns
    `(false, …)` ⇒ the `Artist` row **still exists** (rolled back by the explicit transaction). This is
    the test that proves the withdrawn carve-out was a design gap.
  - REQ-UOW-33 (`ExecuteUpdateAsync`) — same via `SongKaraokeUrlRepository.IncrementPlayCountAsync`:
    play count unchanged after a fault.
  - REQ-UOW-34 (both directions) — write→read joins one context (assert `Assert.Same` on the two
    captured contexts); read→write opens its own and **persists**, with no throw. The second is the
    BL-E regression test: it fails against Revision 10 (write silently lost) and Revision 11 (throws).
- [ ] **Step 2: Run.** All fail on the `NotImplementedException` stub.
- [ ] **Step 3: Implement `ResultSignalsSuccess`** per `design.md § 6`: `ITuple { Length: > 0 }` with
  `t[0] is bool` → that bool; else `IUnitOfWorkOutcome` → `.Success`; else fail-closed `throw`.
- [ ] **Step 4: Run.** Green. **Step 5: Commit.**
  `feat(uow): save-skip signal detection, fail-closed on unrecognised TResult (REQ-UOW-24..27/33/34)`

### Task 1.3: Registration swap

**Files owned:** Modify `MyVocaList/MauiProgram.cs` (**sequential-only — claim it**); Create
`MyVocaList.Tests/Unit/DependencyInjection/UnitOfWorkCompositionTests.cs`; Modify
`MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs`
**Consumes:** 1.1, 1.2a/b. **Produces:** `UnitOfWorkTestHost.Create(Action<IServiceCollection>? customize = null)`.
**Demo:** the app composes with the factory registration and both `App.xaml.cs` scopes still resolve.

- [ ] **Step 1: Write the failing composition test** (REQ-UOW-01, REQ-UOW-21): `IDbContextFactory<AppDbContext>`
  and `IUnitOfWork` are each registered **exactly once**; `IAppInfo` has exactly one `ServiceDescriptor`;
  and `AppDbContext` still resolves from a child scope (the `App.xaml.cs:54` path).
- [ ] **Step 2: Run.** FAIL — no factory, no `IUnitOfWork`, two `IAppInfo` descriptors.
- [ ] **Step 3: Edit `MauiProgram.cs`.** Replace `:61-68`'s `AddDbContext<AppDbContext>` with
  `AddDbContextFactory<AppDbContext>(…, lifetime: ServiceLifetime.Scoped)`, keeping the `(sp, options)`
  overload and both interceptors byte-for-byte (REQ-UOW-14). Add
  `builder.Services.AddSingleton<IUnitOfWork, UnitOfWork>();`. Delete the **second** duplicate
  `IAppInfo` registration at `:157`, keeping `:86`.
- [ ] **Step 4: Add `UnitOfWorkTestHost.Create(...)`** — identical to `CreateLegacy` except it calls
  `AddDbContextFactory<AppDbContext>((sp, o) => Configure(sp, o, dbPath), ServiceLifetime.Scoped)` and
  `services.AddSingleton<IUnitOfWork, UnitOfWork>()`. Switch the Task 1.2a/1.2b tests to `Create()`.
  **Leave the Phase 0 tests on `CreateLegacy()` for now** — Task 2.1 migrates them, deliberately, so
  the RED evidence and the composition change are not confounded (finding B1).
- [ ] **Step 5: Verify the two pre-existing manual scopes** still resolve — `App.xaml.cs:35`
  (`IBackupService`) and `:54` (`AppDbContext`). The `:54` resolution is the one at risk; assert it in
  the composition test.
- [ ] **Step 6: Run the full suite.** Composition tests green; **Phase 0's REQ-UOW-03/04/22/24 tests
  still RED** — confirm by reading the output rather than assuming. Record that
  `AppServicesRegistrationTests.cs:23` builds its own isolated composition and is unaffected.
- [ ] **Step 7: Commit.** `feat(uow): AddDbContextFactory(Scoped) + IUnitOfWork registration; drop duplicate IAppInfo`

### Task 1.4: `UnitOfWorkMocks` — unblock the existing Moq-based unit tests

**Files owned:** Create `MyVocaList.Tests/Infrastructure/UnitOfWorkMocks.cs`
**Consumes:** 1.1. **Produces:** the helper below. **Demo:** an existing service test constructed with
`UnitOfWorkMocks.Passthrough(...)` behaves exactly as before the wrap.

*(New task, finding B2/F7. Every service-wrap task from 2.1 onward depends on this; without it, ~9
existing test files cannot compile and Phase 3's gate is unreachable.)*

- [ ] **Step 1: Write the helper.** It must run the lambda immediately against a stub `IServiceProvider`
  backed by the caller's mocks, so a wrapped method under unit test behaves as the unwrapped one did:

```csharp
public static class UnitOfWorkMocks
{
    /// <summary>An IUnitOfWork whose Execute* methods invoke the body straight away, resolving
    /// services from the supplied instances. No scope, no transaction, no save — the unit tests
    /// assert on the mocked repositories, exactly as they did before the wrap.</summary>
    public static IUnitOfWork Passthrough(params object[] resolvables)
    {
        var sp = new StubServiceProvider(resolvables);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteAsync(It.IsAny<Func<IServiceProvider, Task>>(), It.IsAny<CancellationToken>()))
           .Returns((Func<IServiceProvider, Task> body, CancellationToken _) => body(sp));
        // The generic overloads need one Setup per TResult used by the tests; expose a helper:
        return uow.Object;
    }

    /// <summary>Registers the generic ExecuteAsync/ExecuteReadAsync passthrough for one TResult.</summary>
    public static Mock<IUnitOfWork> WithResult<TResult>(this Mock<IUnitOfWork> uow, IServiceProvider sp) { /* … */ return uow; }

    private sealed class StubServiceProvider(object[] instances) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => instances.FirstOrDefault(serviceType.IsInstanceOfType)
               ?? throw new InvalidOperationException(
                   $"UnitOfWorkMocks.Passthrough was not given an instance of {serviceType.Name}.");
    }
}
```
  The `Mock<IUnitOfWork>` generic-overload setup is the fiddly part: `It.IsAny<Func<IServiceProvider, Task<TResult>>>()`
  must be registered per `TResult` the test uses. Prefer a hand-written
  `sealed class PassthroughUnitOfWork(IServiceProvider sp) : IUnitOfWork` over Moq for the generic
  members — three short method bodies, no per-`TResult` setup, and it reads better at the call site.
  **Decide in favour of the hand-written class** unless a test needs to assert on `ExecuteAsync` calls.
- [ ] **Step 2: Write one test** proving `PassthroughUnitOfWork` invokes the body and returns its value.
- [ ] **Step 3: Run** — green. **Step 4: Commit.** `test(uow): PassthroughUnitOfWork helper for Moq-based service tests`

---

## Phase 2 — PILOT (9 methods)

Four services, **9 mutating methods** — verified against source:
`SongService.CreateSongAsync/UpdateSongAsync/CreateSongWithUrlsAsync/DeleteSongsAsync` (4),
`ArtistService.CreateArtistAsync/UpdateArtistAsync/DeleteArtistsAsync` (3),
`ArtistResolutionService.CommitAsync` (1), `SongResolutionService.CommitAsync` (1).

**Ordering (corrected per reviewer):** `SongService` injects `IArtistRepository`, not `IArtistService`,
and `ArtistService` injects no service — so **2.1 and 2.2 are independent and may run in parallel**
(different files, no shared writer). The genuine constraints are **2.3 after 2.1** (via
`ArtistResolutionService.cs:121` → `_artistService.CreateArtistAsync`) and **2.4 after 2.2 and 2.3**
(via `SongResolutionService.cs:166,184,208,232,260`).

The wrap is the same edit every time. Applied to `UpdateSongAsync` (`SongService.cs:101-148`):

```csharp
public Task<(bool success, string message)> UpdateSongAsync(
    int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits,
    string? externalId = null, string? externalProvider = null, string? version = null,
    CancellationToken ct = default)
    => _uow.ExecuteAsync<(bool, string)>(async sp =>          // ← the ONLY added line
    {
        var repo = sp.GetRequiredService<ISongRepository>(); // NEVER _songRepository (REQ-UOW-28)
        // … the existing body verbatim, with _songRepository -> repo …
        await repo.UpdateAsync(song, ct);
        return (true, $"Song updated to '{title}'");          // save happens once, in UnitOfWork
    }, ct);                                                   // the SaveChangesAsync line is DELETED
```

### Task 2.1: `ArtistService` — 3 methods, and migrate the Phase 0 tests

**Files owned:** Modify `Services/ArtistService.cs`, `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`,
`MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`,
`MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`
**Consumes:** `IUnitOfWork` (1.1), `PassthroughUnitOfWork` (1.4), `UnitOfWorkTestHost.Create()` (1.3).
**Demo:** the Artist REQ-UOW-04 test goes RED→GREEN.

- [ ] **Step 1: Migrate the Phase 0 integration tests from `CreateLegacy()` to `Create()`** (finding B1).
  `AddAppServices()` does not register `IUnitOfWork`; the moment a service constructor requires it,
  every `CreateLegacy()`-hosted test throws a DI resolution failure rather than passing. Record in the
  task-log that the RED evidence was captured under the legacy composition **before** this switch, so
  the RED→GREEN transition is not confounded by the composition change.
- [ ] **Step 2:** Add `IUnitOfWork _uow` to `ArtistService`'s constructor. Keep the three existing
  repository fields — the unwrapped read methods still use them.
- [ ] **Step 3:** Wrap `CreateArtistAsync` (`:46-71`), `UpdateArtistAsync` (`:74-98`),
  `DeleteArtistsAsync` (`:101-123`). Delete `SaveChangesAsync` at `:69` and `:96`. `DeleteArtistsAsync`
  has no save line — it is `ExecuteDeleteAsync`-based and is one of the 5 in-scope multi-repository
  methods, so its `_catalogRepository.CountByArtistAsync` validation must resolve from `sp` too.
- [ ] **Step 4: Do NOT remove `IArtistRepository.SaveChangesAsync`** — `ArtistResolutionService.cs:113,133`
  still calls it. The member and `ArtistRepository.cs:157-158` stay until Task 2.3.
- [ ] **Step 5:** Update `ArtistServiceTests.cs` to construct `ArtistService` with
  `new PassthroughUnitOfWork(sp)` over the existing repository mocks. Do not weaken any assertion.
- [ ] **Step 6: Run.** REQ-UOW-03 (Song) still red — ArtistService's wrap does not fix it. Full suite
  otherwise green, 0 skipped. **Note and verify an intermediate state:** between this task and
  Task 2.3, the still-unwrapped `ArtistResolutionService.CommitAsync` calls the now-wrapped
  `CreateArtistAsync` (`:121`) and then does `_artistRepository.UpdateAsync(created, ct)` (`:132`) on
  the window-scope context, with an entity created in a different, now-disposed context. EF's `Update`
  on a detached instance handles this and `ArtistResolutionServiceTests` should stay green — **confirm
  by running, do not assume**, since Rule 3 requires green after every task.
- [ ] **Step 7: Commit.** `refactor(uow): wrap ArtistService's 3 mutating methods (Phase 2 pilot)`

### Task 2.2: `SongService` — 3 create/update methods

**Files owned:** Modify `Services/SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
**Consumes:** 1.1, 1.4. **Demo:** REQ-UOW-03 goes RED→GREEN.
*(Split from a single 4-method task per reviewer sizing finding; `DeleteSongsAsync` is Task 2.2b.)*

- [ ] **Step 1:** Add `IUnitOfWork _uow` to the constructor; update `SongServiceTests.cs` construction
  to pass `new PassthroughUnitOfWork(sp)`.
- [ ] **Step 2: Wrap `UpdateSongAsync`** (`:101-148`) exactly as the template above. Delete
  `SaveChangesAsync` at `:146`.
- [ ] **Step 3: Wrap `CreateSongAsync`** (`:64-98`) — multi-repository: it validates the artist via
  `IArtistRepository.GetByIdAsync`, so resolve **both** `ISongRepository` and `IArtistRepository` from
  `sp` at the top of the lambda. Delete `SaveChangesAsync` at `:96`.
- [ ] **Step 4: Wrap `CreateSongWithUrlsAsync`** (`:151-216`) — three repositories
  (`IArtistRepository`, `ISongRepository`, `ISongKaraokeUrlRepository`), all from `sp`. Delete
  `SaveChangesAsync` at `:214`. **Preserve REQ-UOW-07:** one save commits the `Song` and its
  `SongKaraokeUrl` rows via EF FK fixup through the navigation property.
- [ ] **Step 5: Add the REQ-UOW-07 assertion** to `Bug068RegressionTests.cs`: inject a fault on the URL
  rows (same decorator technique as Task 0.4) and assert **no `Song` row** persists.
- [ ] **Step 6: Do NOT remove `ISongRepository.SaveChangesAsync`** — `SongServiceTests` and Task 2.4's
  caller sweep own that.
- [ ] **Step 7: Run.** REQ-UOW-03 PASSES — this is the pilot's headline RED->GREEN. Full suite green, 0 skipped.
- [ ] **Step 8: Commit.** `refactor(uow): wrap SongService create/update methods (Phase 2 pilot)`

### Task 2.2b: `SongService.DeleteSongsAsync`

**Files owned:** Modify `Services/SongService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
**Consumes:** 2.2. **Demo:** a delete followed by a failure tuple leaves the row present.

- [ ] Wrap `DeleteSongsAsync` (`:233-244`). It has no `SaveChangesAsync` line — `SongRepository.DeleteAsync`
  (`:136-142`) is `ExecuteDeleteAsync`, one of REQ-UOW-33's five bulk-op paths, now covered by the
  explicit transaction.
- [ ] Add the REQ-UOW-33 integration assertion for this method to `SaveSkipTests.cs` if not already
  covered by the `ArtistService.DeleteArtistsAsync` case from Task 1.2b.
- [ ] Run; commit: `refactor(uow): wrap SongService.DeleteSongsAsync (ExecuteDeleteAsync under transaction)`

### Task 2.3: `ArtistResolutionService` + retire `IArtistRepository.SaveChangesAsync`

**Files owned:** Modify `Services/ArtistResolutionService.cs`, `Domain/RepositoryInterface/IArtistRepository.cs`,
`Infra/Repository/ArtistRepository.cs`, `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`
**Consumes:** 2.1. **Demo:** REQ-UOW-09's pinned outcome (Task 0.4b) still passes after the re-shape.

- [x] **Step 1:** Wrap `CommitAsync` (`:86-142`). First ambient-join site: the lambda resolves
  `IArtistService` from `sp` and calls `CreateArtistAsync` (`:121`), whose own `ExecuteAsync` **joins**
  the ambient scope instead of opening a second one. The save→mutate→save at `:112-113`/`:132-133`
  becomes **two saves inside one unit of work** — REQ-UOW-09's explicitly sanctioned second branch —
  via `IUnitOfWork.FlushAsync` (REQ-UOW-35).
  > **Spec updated [2026-08-18]:** this step originally read "collapses to a single implicit save".
  > That was wrong. `CommitAsync` returns `created.Id` from *inside* the lambda, before `ExecuteAsync`'s
  > deferred save runs, so a single save returns `artistId = 0` and breaks REQ-UOW-09's own guarantee;
  > and `UpdateAsync` throws on the still-`Added` entity's temporary key. The CreateNew branch calls
  > `FlushAsync` once, immediately after `CreateArtistAsync` succeeds. Still **one** context and
  > **one** transaction: a later failure tuple or exception rolls the flushed rows back. REQ-UOW-11 is
  > unaffected — the flush is on `IUnitOfWork`, not on a repository — so Step 5's retirement of
  > `IArtistRepository.SaveChangesAsync` proceeds as planned.
- [x] **Step 2: REQ-UOW-28 is at its sharpest here** — `_artistService` **and** `_artistRepository` must
  both disappear from inside the lambda. A surviving `_artistService` reference silently defeats the join.
- [x] **Step 3: Wrap `ResolveAsync` (`:28-83`) in `ExecuteReadAsync`** — decided, not optional: the
  method name then carries the read/write intent (Revision 6's stated purpose), and REQ-UOW-34's
  write→read join is exercised by real code rather than only by a synthetic test.
- [x] **Step 4:** Remove the two `.Setup(r => r.SaveChangesAsync(...))` calls at
  `ArtistResolutionServiceTests.cs:206,234` and construct with `PassthroughUnitOfWork`.
- [x] **Step 5: Grep for remaining callers** of `IArtistRepository.SaveChangesAsync` repo-wide. If a
  caller survives in an **excluded** file, do **not** remove the member — record the deferral. Otherwise
  delete `IArtistRepository.cs:55` and `ArtistRepository.cs:157-158`.
- [x] **Step 6: Run.** Task 0.4b's REQ-UOW-09 test still passes. Full suite green.
- [x] **Step 7: Commit.** `refactor(uow): wrap ArtistResolutionService; retire IArtistRepository.SaveChangesAsync`

### Task 2.4: `SongResolutionService` — the 3-level join

**Files owned:** Modify `Services/SongResolutionService.cs`,
`MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`,
`MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`
**Consumes:** 2.2, 2.2b, 2.3. **Demo:** Task 0.4's fault-injection test goes RED→GREEN.

- [ ] **Step 1:** Wrap `CommitAsync` (`:151-248`). **Deepest chain in the spec:**
  `SongResolutionService.CommitAsync` → `ArtistResolutionService.CommitAsync` (`:260`) →
  `ArtistService.CreateArtistAsync`. The `AsyncLocal` join must hold across all three levels — one
  scope, one context, one save. Resolve `ISongService`, `IArtistResolutionService` and `ISongRepository`
  from `sp`; `_songService` / `_artistResolution` / `_songRepository` must not appear inside the lambda.
- [ ] **Step 2:** Wrap `ResolveAsync` (`:36-148`) in `ExecuteReadAsync` — it calls
  `_artistResolution.ResolveAsync` at `:39`,`:254`, now also an `ExecuteReadAsync`, so the read-join
  path is exercised.
- [ ] **Step 3: Run Task 0.4's tests.** The fault-injection test now **PASSES**: no `Song` row and no
  `Artist` row survive. The nested-failure-tuple test (Task 0.4 Step 3) also passes: save-skip is
  decided once, by the outermost `ExecuteAsync`. These two are the REQ-UOW-22 and REQ-UOW-24-nested greens.
- [ ] **Step 4: Commit** the service wrap before touching the interface — two reviewable changes.
- [ ] **Step 5: Commit.** `refactor(uow): wrap SongResolutionService.CommitAsync (3-level ambient join)`

### Task 2.4b: Retire `ISongRepository.SaveChangesAsync` + stopgap check

**Files owned:** Modify `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`,
`MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
**Consumes:** 2.4. **Demo:** no interface in the pilot's scope declares a save entry point.

- [ ] **Step 1: REQ-UOW-18 stopgap check** *(F3)*: re-run `git merge-base --is-ancestor 1a114c1 HEAD; echo $?`.
  If `1` (current state), record **"stopgap absent on develop; REQ-UOW-18 satisfied vacuously (NB-4 case 2)"**
  and delete nothing. If `0`, delete the `ChangeTracker.Entries<Song>()` loop from
  `SongRepository.UpdateAsync` and record the deletion diff. Either way the task proceeds — not a gate.
- [ ] **Step 2:** Grep repo-wide for `ISongRepository.SaveChangesAsync` callers. Expected remaining:
  `SongServiceTests.cs:522,559` (mock setups — delete them). If a production caller survives in an
  excluded file, defer and record. Otherwise delete `ISongRepository.cs:65` and `SongRepository.cs:144-146`.
- [ ] **Step 3: Run** the full suite green. **Step 4: Commit.**
  `refactor(uow): retire ISongRepository.SaveChangesAsync; confirm stopgap absent (REQ-UOW-18)`

---

## Phase 3 — VERIFY (HARD GATE)

> **Nothing in Phase 3.5, 4+ or LAST may be dispatched, claimed, or started until all five items below
> are recorded passed in `task-log.md` (REQ-UOW-30).** This is Helder's gate, not the orchestrator's.

- [ ] **3.1 — Full automated suite green.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`.
  Record total/passed/failed/skipped. **The run must report `Skipped: 0`** — the skip-mark step was
  cancelled by F8, so ANY skipped test is an unexplained regression and does not pass this gate.
  Confirm the Phase 0 RED tests (REQ-UOW-03, REQ-UOW-22, REQ-UOW-24-nested) are now GREEN and record
  before/after. Phase 0 baseline for comparison: **Failed 3, Passed 525, Skipped 0, Total 528.**
- [ ] **3.2 — Review-checklist commands**, output pasted into the task-log:
  - `git diff develop --name-only` contains **none** of the seven excluded files (REQ-UOW-31).
  - REQ-UOW-28: `git grep -nE '_(song|artist|catalog|url|person|venue)\w*(Repository|Service|Resolution)' -- Services/*.cs`
    then **read** each of the 9 wrapped methods and confirm no hit falls inside a lambda body. Grep
    narrows the reading list; it does not replace the reading.
  - REQ-UOW-01: `git grep -n "Create\(Async\)\?Scope()" -- Services/ MyVocaList/UI/ViewModels/ Infra/`
    returns only `Infra/UnitOfWork/UnitOfWork.cs`. (`App.xaml.cs:35,:54` call it on `IServiceProvider`
    and are the two documented pre-existing exceptions.)
  - REQ-UOW-11: `git grep -n "SaveChangesAsync" -- Domain/RepositoryInterface/` shows
    `ISongRepository`/`IArtistRepository` gone; 3 of 5 remain, owned by Phase 4+.
  - REQ-UOW-10: `git diff develop --stat -- Services/` plus, per wrapped method,
    `git diff develop -- Services/<file>.cs | grep -c '^+'` minus the re-indentation — record added
    lines per method and confirm ≤ 1 line of *boundary ceremony* each.
- [ ] **3.3 — Helder's on-device confirmation. HARD GATE.** Song/Artist CRUD no longer reproduces
  BUG-068/BUG-071, **from a cold app start, with no Queue or Event screen visited during the session**.
  Queue/Event code keeps the shared-context defect by design (D12 LIVE RISK) — visiting one of those
  screens first can poison the window-scope context for reasons the pilot did not cause, making the
  pilot look broken when it is not. Script: launch cold → Songs list → create a song with a new inline
  artist → reopen it → change the title → save → reopen and confirm → change the artist → save →
  confirm → delete → confirm. Repeat on Artists. **Does not pass without Helder's explicit confirmation.**
- [ ] **3.4 — D13 API-shape decision (REQ-UOW-32).** From the 9 pilot call sites actually written — not
  the § 6 audit — record **"keep provisional"** or **"replace"** with a one-line rationale. If
  "replace", every Phase 4+ task is rewritten against the replacement shape before dispatch, and the 9
  pilot call sites are migrated too.
- [ ] **3.5 — Record the gate.** One `task-log.md` entry naming all four outcomes. Phase 4+'s first
  dispatch entry must reference it by date; its absence is a process defect caught at review.

---

## Phase 3.5 — RESOLVED 2026-08-24: dissolved by the Event/Queue deletion

> **Both halves of this phase ceased to exist on 2026-08-20** (`32e7a85e`, branch
> `chore/demolish-event-queue`). `Infra/Repositories/` — the plural family this phase existed to
> delete — is gone from the tree; `Infra/Repository/` (singular) is now the only repository folder,
> holding 8 files. The Queue/Event UoW migration is likewise moot: the services, their repositories
> and every `TODO [BUG-071 / UOW]` marker were deleted with the feature, and only the Event infra
> definitions (entities, EF configs, `DbSet`s, migrations) were kept — none of which carry a save.
>
> The two traps this phase warned about are therefore both discharged, not deferred: the duplicate
> `Event` entity namespaces collapsed to one, and the "services with zero `SaveChangesAsync` of their
> own" hazard cannot fire because those services no longer exist.
>
> **Consequence: Phase 4+ is UNBLOCKED.** Its gate read *"startable only after Phase 3 passes and
> Phase 3.5 lands"*; Phase 3 passed (Helder's emulator gate, `cdec7af5`) and Phase 3.5 is now
> discharged. The owning item's folder
> (`changes/2026-08-04-merge-duplicate-repository-families-…`) was removed in the same docs purge
> and is not to be recreated.
>
> **Phase 4+ scope shrinks with it:** task 4.6a named `QueueSongPickerViewModel` and
> `QueueManagementViewModel`, both deleted — only `PersonPickerViewModel` survives from that task.
> Task 4.2's rationale for retaining `IBaseRepository<T>.SaveChangesAsync()` also lapses: its cited
> surviving callers were all in `Services/QueueService.cs`, which is gone. Re-check before dispatch
> rather than assuming either way.

**Historical record of what this phase would have done, kept for context:**

**Not planned here, and not startable.** The owning item —
`changes/2026-08-04-merge-duplicate-repository-families-into-one-infra-repository-infra-repositories/`
— is `status: 💡 Pending` with a `README.md` only: no `requirements.md`, no `design.md`, no plan. Its
gate reads *"Runs AFTER the pilot proves the pattern (Helder 2026-08-04, option a); merged with the
deferred Queue/Event unit-of-work item."*

Direction is recorded: delete `Infra/Repositories/` (plural, 2 files, 6 embedded saves), keep
`Infra/Repository/` (singular, 10 files, stage-only). Two known traps from `LEDGER.md:14`: `Event`
exists in **both** entity namespaces with different shapes; and `QueueServiceNew`/`EventService` have
**zero** `SaveChangesAsync` calls of their own, relying entirely on the embedded repository saves — so
deleting those saves without giving both services a commit boundary silently stops persisting every
enqueue, with no exception and no failing test.

**Required before Phase 4+ starts:** that item needs its own brainstorm → spec → spec-review → plan →
plan-review → implementation cycle. This plan does not attempt it.

---

## Phase 4+ — Spread (12 methods) — GATED

> ~~Startable only after Phase 3 passes **and** Phase 3.5 lands.~~ **UNBLOCKED 2026-08-24** —
> Phase 3 passed and Phase 3.5 was dissolved by the Event/Queue deletion (see above).
> Re-verify each task's file list against the tree before dispatch; several coordinates are stale. Written against the **provisional** API
> shape; if Phase 3.4 decided "replace", rewrite against the final shape before dispatch. Each task
> below also owns its service's `Unit/Services/*Tests.cs` construction fix (finding F7).

**The 12 methods** (21 in-scope − the pilot's 9): `BackupService.CreateFullBackupAsync` (1),
`CatalogService.AddSongToCatalogAsync`/`RemoveSongFromCatalogAsync` (2),
`PersonService.CreatePersonAsync`/`UpdatePersonAsync`/`DeletePersonsAsync` (3),
`SongKaraokeUrlService.AddUrlAsync`/`RemoveUrlAsync`/`RecordPlayAsync` (3),
`VenueService.CreateVenueAsync`/`UpdateVenueAsync`/`DeleteVenuesAsync` (3).

- [ ] **4.1 — `CatalogService` (2).** Files: `Services/CatalogService.cs`,
  `Domain/RepositoryInterface/ICatalogRepository.cs`, `Infra/Repository/CatalogRepository.cs`,
  `Unit/Services/CatalogServiceTests.cs` (`:19` mock setup), `Integration/Repositories/CatalogRepositoryTests.cs`
  (`:64,:71` call `_repo.SaveChangesAsync()` directly — substitute `_db.SaveChangesAsync()`; this is a
  *different* edit from the service-test fix). Retire `ICatalogRepository.cs:20` +
  `CatalogRepository.cs:78-79`. `RemoveSongFromCatalogAsync` is an `ExecuteDeleteAsync` path (REQ-UOW-33).
- [ ] **4.2 — `PersonService` (3).** Files: `Services/PersonService.cs`, `Unit/Services/PersonServiceTests.cs`,
  `Integration/UnitOfWork/Bug068RegressionTests.cs` (no `Skip` to remove — F8). **`IBaseRepository<T>.SaveChangesAsync()`
  is NOT removed** — approved decision (`design.md § 8`), unconditional: `PersonRepository`/`VenueRepository`
  retain a technically-reachable inherited member after this spec ships. Its surviving callers are in the
  **excluded** `Services/QueueService.cs` (`:109`, `:151`, `:162`), so removing it would be a compile-level
  edit of an excluded file — a REQ-UOW-31 violation. This task deletes only `PersonService`'s own three
  call sites (`:161`, `:254`, `:274`).
- [ ] **4.3 — `SongKaraokeUrlService` (3).** Files: `Services/SongKaraokeUrlService.cs`,
  `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs`, `Infra/Repository/SongKaraokeUrlRepository.cs`,
  `Unit/Services/SongKaraokeUrlServiceTests.cs` (`:81,:168`), `Unit/Services/SongServiceTests.cs` (`:524`).
  Retire `ISongKaraokeUrlRepository.cs:22` + `SongKaraokeUrlRepository.cs:67-68`. `RecordPlayAsync` is the
  **only** in-scope no-signal method — wraps via `ExecuteAsync(Func<IServiceProvider, Task>, ct)`
  (REQ-UOW-26). `RemoveUrlAsync`/`RecordPlayAsync` are two of REQ-UOW-33's five bulk-op paths.
- [ ] **4.4 — `VenueService` (3).** Files: `Services/VenueService.cs`, `Unit/Services/VenueServiceTests.cs`,
  `Integration/UnitOfWork/Bug068RegressionTests.cs` (no `Skip` to remove — F8). Also delete the dead
  `IEventRepository _eventRepository` field (`VenueService.cs:16-18`, ctor param `:27`, assignment `:31`)
  — verified unused, and it carries its own `TODO [BUG-071 / UOW] — … Delete, do not migrate` marker.
  This edits `VenueService.cs` only, not any excluded file. It changes the constructor arity, so
  `VenueServiceTests.cs` construction must be updated in the same task.
- [ ] **4.5 — `BackupService.CreateFullBackupAsync` (1). Atomic sub-task, not splittable.** Append
  `: IUnitOfWorkOutcome` to `BackupResult` (`Domain/ServicesInterfaces/IBackupService.cs:5`) **in the
  same commit** as the wrap — under fail-closed, wrapping with an unmarked `BackupResult` makes every
  call throw immediately. Add the two tests the spec assigns here and Task 1.2b could not: the real
  `BackupResult` failure case (REQ-UOW-24's `IUnitOfWorkOutcome` exemplar) and the **positive**
  counterpart proving the wrap does **not** throw once the marker is in place (REQ-UOW-27). Note
  `IBackupRepository` is registered in `MauiProgram.cs:71`, not `AddAppServices()` — the harness needs
  it added via the `customize` hook. `BackupService` is hand-constructed by a factory lambda at
  `MauiProgram.cs:72-77`; if its constructor gains `IUnitOfWork`, that lambda must be edited too —
  claim `MauiProgram.cs` (sequential-only) for that one line. Retire `IBackupRepository.cs:17` +
  `BackupRepository.cs:46-48`, and fix `BackupServiceTests.cs:71` and
  `Integration/Repositories/BackupRepositoryTests.cs:40,58,75,91`.
- [ ] **4.6a — ViewModel conversion.** Convert `QueueSongPickerViewModel` (`ISongRepository`) and
  `PersonPickerViewModel` (`IPersonRepository`) **fully** to inject `IUnitOfWork`. Convert
  `QueueManagementViewModel`'s `IPersonRepository`/`ISongRepository` usage **only** — its
  `IEventService`/`IQueueServiceNew` fields (`:16-17`, ctor `:27-33`) stay as direct constructor
  injections (D12 item 6, approved).
- [ ] **4.6b — Captive-dependency audit, with a defined outcome.** Enumerate every UI type that
  constructor-injects a repository or a data-writing service, regardless of its own DI lifetime — a
  transient ViewModel is exactly as captive as a singleton, because `AddDbContextFactory(…, Scoped)`
  still registers `AppDbContext` as scoped. Command:
  `git grep -nE 'I\w+(Repository|Service) _\w+' -- MyVocaList/UI/ViewModels/ MyVocaList/UI/Components/`.
  **Outcome required, not just a list:** each hit is either (a) converted to `IUnitOfWork`, (b) recorded
  as safe with a one-line reason (read-only, no `AppDbContext` behind it), or (c) registered as a
  follow-up bug. Start with `AppShellViewModel`/`AppShell` (`MauiProgram.cs:109-110`, singletons).
- [ ] **4.7 — `DbLoadGate` removal, conditional.** Remove the static `SemaphoreSlim`
  (`CrudListViewModelBase.cs:16`; used `:128`, `:179`, `:207`, `:241`; comment `:304`; test comment
  `CrudListViewModelBaseTests.cs:215`) **only when both hold**: (a) every in-scope consumer above is
  converted, **and** (b) the `page-load-frozen` regression suite is confirmed green without the gate.
  The gate's comment carries **two independent rationales** — the MAUI-no-per-page-scope one this spec
  fixes, and a separate `SQLITE-WORKAROUND` for the `Microsoft.Data.Sqlite` sync-async freeze whose
  revert trigger is `INFRA_MSSQL`, not this work. If (b) cannot be confirmed, **defer the removal** to
  the item closing out `page-load-frozen` and record which case applied (REQ-UOW-29 then satisfied
  vacuously, mirroring REQ-UOW-18).

---

## LAST — Guidelines (after Phase 4+, never interleaved)

- [ ] **L.1 — Amend `code-style-reference.md § DI Registration Conventions`** (REQ-UOW-19). The current
  rule — *"`AddScoped` — Repositories, Services, IDatabaseInit (per-lifetime scope)"* — is an
  ASP.NET-shaped rule wrong for MAUI, and the direct cause of all 27 `AddScoped` registrations. Replace
  with a MAUI-correct rule stating that MAUI scopes are per-Window (effectively app-lifetime on mobile)
  and naming `IUnitOfWork` as the write boundary. Follows `CLAUDE.md § Amending These Rules`: `amend:`
  prefix, rationale in the body, changelog entry with old rule / new rule / effective date.
- [ ] **L.2 — Delete the one genuine tracking-conflict workaround** (REQ-UOW-17, **corrected per F5**):
  `CatalogRepositoryTests.cs:67` (comment `:66`), in `AddAsync_DuplicateEntry_ThrowsDbUpdateException`.
  **Do NOT touch `ArtistRepositoryTests.cs:257` or `:358`** — both are load-bearing arrange steps, not
  workarounds; `:358`'s deletion would fail that test's own `Assert.Empty` at `:364`. Record REQ-UOW-17
  as **partially vacuous** with this evidence, the same treatment F3/F6 give REQ-UOW-18/20.
- [ ] **L.3 — `TestDbContextFactory` alignment (REQ-UOW-16), with defined outcome.** REQ-UOW-16 requires
  that file to expose the same unit-of-work primitive the app uses. Concretely: add
  `public static UnitOfWorkTestHost CreateHost()` to `TestDbContextFactory` delegating to
  `UnitOfWorkTestHost.Create()`, keep `Create()` returning a bare `AppDbContext` for the ~12 repository
  integration classes that legitimately want one, and add an XML doc on `Create()` stating that new
  *service*-level tests must use `CreateHost()`. Files: `MyVocaList.Tests/Infrastructure/TestDbContextFactory.cs`.
- [ ] **L.4 — REQ-UOW-20**: record as vacuously satisfied (F6 — zero "Tracked query" comments repo-wide).
  Optionally align `SongRepository.GetByIdAsync:52-54` with `ArtistRepository.GetByIdAsync:80`, which
  calls `.AsTracking()` explicitly.
- [ ] **L.5 — Align `AppServicesRegistrationTests.cs:23`** to `AddDbContextFactory` so its composition
  matches production. *(Not urgent — verified Phase 1 does not break it: isolated `ServiceCollection`,
  and no service constructor changes until Phase 2.)*
- [ ] **L.6 — Confirm REQ-UOW-24/25/26/27/33/34 stay GREEN** after all Phase 4+ rewrites.
- [ ] **L.7 — Closeout**: `backlog_gen.py status <ID> "✅ Done" --closed 2026-08`, `regen`, LEDGER row,
  Rebuild Test per `spec-writing-guide.md`.

---

## AC traceability

Every REQ-UOW-01 … REQ-UOW-34 must appear in the task-log matrix with a test, a vacuous-satisfaction
record, or an out-of-scope marker. Assignments not obvious from the task list above:

| AC | Where |
|---|---|
| REQ-UOW-07 | Task 2.2 Step 5 (fault on URL rows leaves no `Song`) |
| REQ-UOW-08, REQ-UOW-23 | **Out of scope** (D12) — carried by the deferred Queue/Event item |
| REQ-UOW-09 | Task 0.4b (pinned), Task 2.3 (preserved) |
| REQ-UOW-10 | Task 3.2 diff statistic |
| REQ-UOW-12 | **Satisfied by construction** — every wrapped method names its unit of work at the top of its own body. Reviewer-checked per diff, recorded once at Phase 3; no separate test. |
| REQ-UOW-13 | **Satisfied by construction** — the only hand-written type is `UnitOfWork` itself, justified in `design.md § 8`; `AddPooledDbContextFactory` explicitly rejected. Recorded once at Phase 3; no separate test. |
| REQ-UOW-16 | Task L.3 |
| REQ-UOW-17 | Task L.2 — **partially vacuous** (F5) |
| REQ-UOW-18 | Task 2.4b — **vacuous** unless `1a114c1` merged (F3) |
| REQ-UOW-19 | Task L.1 |
| REQ-UOW-20 | Task L.4 — **vacuous** (F6) |
| REQ-UOW-21 | Task 1.3 |
| REQ-UOW-29 | Task 4.7 — conditional; vacuous if `page-load-frozen` cannot be confirmed |
| REQ-UOW-30, 31, 32 | Phase 3 gate items 3.5, 3.2, 3.4 — process checks at review, not runtime tests |

---

## Verification

**Per task:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` — 0 failures, 0 build errors, with
the RED/GREEN transition read and recorded, not assumed.

**Per phase:** the review-checklist commands in Phase 3.2.

**End-to-end, on device (Helder, Phase 3.3 and again after Phase 4+):** cold app start with no
Queue/Event screen visited → Song/Artist CRUD create/edit/re-edit/delete cycle → confirm no
`InvalidOperationException`, no silent non-persistence, and that every edit survives a re-read.

---

## Plan-review resolutions

A fresh-context plan-reviewer returned **FAIL** with 6 blocking findings. All six are fixed:

| # | Finding | Fix |
|---|---|---|
| B1 | Phase 0 tests pinned to `CreateLegacy()`, never migrated ⇒ Phase 2 could not go green (`AddAppServices()` does not register `IUnitOfWork`). | Task 2.1 Step 1 migrates them to `Create()`, with an explicit task-log note that RED was captured under the legacy composition first. |
| B2 | ~9 existing test files break (Moq'd repositories + ctor arity); the only mention was an unnamed helper. | New **Task 1.4** produces `PassthroughUnitOfWork`; every wrap task now names its test files in `Files owned`, and the two *integration* repository tests get their distinct `_db.SaveChangesAsync()` fix called out. |
| B3 | Harness omitted `TransactionLogInterceptor`/`ITransactionLogWriter` ⇒ REQ-UOW-14/15 untestable. | Task 0.1 registers both, plus `RecordingTransactionLogWriter` so the ACs can assert on entries. |
| B4 | My F5 was wrong — `ArtistRepositoryTests.cs:257`/`:358` are load-bearing arrange steps; deleting `:358` fails the test's own `Assert.Empty`. | F5 withdrawn and rewritten; L.2 targets only `CatalogRepositoryTests.cs:67`; REQ-UOW-17 recorded partially vacuous. |
| B5 | REQ-UOW-09 had no test despite D11 assigning it to Phase 0. | New **Task 0.4b**. |
| B6 | Task 4.2 made `IBaseRepository<T>.SaveChangesAsync()` removal conditional; the spec decides it unconditionally, and removal would break excluded files. | Rewritten as a flat statement; stale `QueueService.cs:134` corrected to `:109/:151/:162`. |

Non-blocking items also applied: 2.1/2.2 declared parallel (the "forced by the call graph" claim was
overstated — only 2.3-after-2.1 and 2.4-after-2.2/2.3 are real); Tasks 1.2, 2.2, 2.4 split for sizing;
`Consumes`/`Demo`/`Files owned` added; the "same shape as Task N" / "same pattern" / audit-with-no-outcome
/ deferred-`ExecuteReadAsync`-decision / `TestDbContextFactory` placeholders spelled out; Phase 3.1
gate tightened to `Skipped: 2` exactly; skip-removal carried into both items' `README.md` gates;
REQ-UOW-12/13 given traceability entries; REQ-UOW-24's `IUnitOfWorkOutcome` shape covered synthetically
in 1.2b and for real in 4.5; REQ-UOW-27's positive counterpart assigned to 4.5; REQ-UOW-24's
nested-tuple-failure direction added to Task 0.4 Step 3; Task 0.1's smoke test made non-vacuous; the
Phase 0 exit condition reworded to exempt harness tasks; the O3 hazard registered as a bug in Task 0.5.

**Not adopted:** the reviewer's suggestion to add `Review lane:` fields — this project's review lane is
set by `subagent-driven-development` (fresh reviewer subagent per task), not per-task in the plan.

**Residual risk for Helder:** the reviewer noted that plan-introduced blockers (B1, B2) and
spec-inherited ones (B4, B5) were roughly even, i.e. the spec is closer to quiescent than the plan was.
This revision has not been re-reviewed. A second plan-review pass before Phase 0 dispatch is
recommended, not assumed.
