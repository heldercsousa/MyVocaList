# Requirements — Scope all service reads through `IUnitOfWork`

> **Parent spec:** `../2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/`
> (requirements.md / design.md). That change migrated every **write** to `IUnitOfWork.ExecuteAsync`.
> Reads were never in its scope. This change closes that half.
>
> **Defect this change fixes:** `../../bugs/2026-08-24-BUG-078-service-read-paths-still-use-the-captive-appdbcontext-one-of-them-tracks/README.md` (Major).
>
> **Requirement numbering continues the parent series.** Highest allocated before this change:
> `REQ-UOW-35`. This change allocates **REQ-UOW-36 … REQ-UOW-52**.
>
> > **Spec updated [2026-08-25]:** approved by Helder with one amendment — the minimum query length
> > that triggers a search was left implicit and inconsistent across services. **REQ-UOW-51** (local
> > DB reads, ≥ 2 characters) and **REQ-UOW-52** (remote HTTP provider fetches, ≥ 3 characters) were
> > added, and the guard table under REQ-UOW-41 now records the two guards that change. This item has
> > not shipped, so the spec is edited in place per `workflow.md § SDD Invariant`.

---

## Problem statement

`AppDbContext` is registered Scoped (`MauiProgram.cs:61-68`) and .NET MAUI creates a DI scope **per
Window**, not per page. A single-window app therefore has exactly one scope for the whole session, so
every constructor-injected repository holds an **app-lifetime captive `AppDbContext`**.

Every service **read** still runs on that captive context. Three consequences follow, and only the
third is user-visible today:

1. **Thread-safety.** `DbContext` is not thread-safe for concurrent operations. Because all CRUD list
   ViewModels read through the same instance, `CrudListViewModelBase.cs:16`'s static `DbLoadGate`
   semaphore exists purely to serialise them app-wide. Its justification is fully intact while reads
   are unscoped, which is why parent Phase 4.7 (`DbLoadGate` removal, REQ-UOW-29) cannot close.
2. **Change-tracker growth.** `ArtistRepository.cs:80` `GetByIdAsync` is the **only** `.AsTracking()`
   read in `Infra/Repository/` (tree-wide census, count = 1). Each artist delete-confirmation attaches
   an entity to a context that is never disposed.
3. **Stale reads (the user-visible defect).** Writes now commit through a *different*, freshly-scoped
   context. A rename committed via `IUnitOfWork` is not reflected in the captive context's already-
   tracked copy, so `ArtistService.GetDeleteConfirmationAsync` can display the artist's **old** name
   after a successful rename. It fails silently — no exception, which is why no test caught it.

### What this change is *not* about

Two intuitive but wrong readings are explicitly rejected (see `design.md § 2`):

- **Not tracking.** `QueryTrackingBehavior.NoTracking` is set globally
  (`Infra/AppDbContext.cs:37`, `:54`, `MauiProgram.cs:72`). Scoping a read buys nothing on tracking.
- **Not transactions.** `ExecuteReadAsync` opens no transaction and never publishes an ambient scope
  (REQ-UOW-34). "Transaction safety" is not the justification for any requirement below.

The justification is **`DbContext` lifetime**: a fresh scoped context per read (thread-safety, the
precondition for removing `DbLoadGate`), and read-your-own-writes when a read is nested inside a write.

---

## User stories

- **US-1 (developer).** As a developer, I want a single, uniform rule — *every service read goes
  through `IUnitOfWork.ExecuteReadAsync`* — so that I never have to reason per-method about whether a
  given read is on the captive context.
- **US-2 (user).** As a user, after I rename an artist and then delete it, I want the confirmation
  dialog to show the **new** name.
- **US-3 (developer).** As a developer, I want `DbLoadGate` gone so that CRUD list pages no longer
  serialise their loads app-wide — but only once both limbs of REQ-UOW-29 are satisfied.
- **US-4 (developer, forward-looking).** As a developer, I want `ArtistSuggestionService` and
  `SongSuggestionService` to already follow the HTTP-outside-the-unit-of-work rule *before* the
  autocomplete feature wires them into `ArtistFormPage` / `SongFormPage`, so that when a user first
  reaches these lookups a network round-trip never holds a database scope open. These two services
  have **no production consumer today and are not registered in DI** — see
  `§ The two suggestion services are pre-built, not dead code`. The user-facing responsiveness this
  story protects is therefore future, not current; the requirement is that the *pattern* is correct at
  the moment the feature lands, not that a user can observe it now.

---

## The two suggestion services are pre-built, not dead code

**Decided by Helder — recorded here so it is not re-opened in review.**

`SongSuggestionService` and `ArtistSuggestionService` are **not registered in `MauiProgram.cs` and have
no production consumer today.** That is **deliberate**, not an oversight, and it does **not** make them
dead code: they are pre-built for an imminent feature. The artist-name autocomplete on
`ArtistFormPage` and the song-title autocomplete on `SongFormPage` will search these suggestion
services (remote metadata providers) **in addition to** the existing local-database search. Today only
artist name has autocomplete, and it is local-DB only.

Three consequences bind this change:

1. **Both services DO get `IUnitOfWork` + the `ExecuteReadAsync` wrap now** (REQ-UOW-38/43), so they
   already conform to the universal rule at the moment the autocomplete feature wires them up. A
   service converted later, under feature pressure, is a service converted wrongly.
2. **DI registration is out of scope for this change** and belongs to the future autocomplete feature.
   `MauiProgram.cs` SHALL NOT appear in any wave's `Files owned` — it is a sequential-only file
   (`workflow.md § Sequential-only file registry`), and registering these services is not this
   change's job.
3. **No AC may assert container resolution of these two services.** Such an assertion is unsatisfiable
   against the real container by construction. Their conversion is verified by unit tests that
   construct them directly (see REQ-UOW-38 and REQ-UOW-49's carve-out).

---

## Acceptance criteria

### The universal rule

- **REQ-UOW-36** — Every read method on a service in `Services/` that reaches a repository SHALL
  execute that repository access inside an `IUnitOfWork.ExecuteReadAsync` lambda. The census in `§
  Scope` enumerates the complete in-scope set; a method not listed there is either already compliant
  (`§ Explicitly out of scope`) or performs no repository access.
  *Test:* a source-level assertion (Python file walk over `Services/*.cs`, not `grep`) that for each
  method named in the `§ Scope` census, the repository call it makes lies textually inside an
  `ExecuteReadAsync(` lambda body; the assertion is recorded in the task-log with the walk's output.

- **REQ-UOW-37** — Inside every read lambda added by this change, ALL repository access SHALL be
  resolved from the lambda's own `IServiceProvider` parameter (`sp.GetRequiredService<IXRepository>()`).
  A lambda body SHALL NOT reference the enclosing service's constructor-injected `_`-prefixed
  repository or service field. This is REQ-UOW-28 applied to the new call sites; a lambda that reads
  through `_field` resolves the captive context and leaves the defect in place while *appearing*
  converted.
  *Test:* a Python file-walk check over `Services/*.cs` asserting that no `ExecuteReadAsync` lambda
  body contains a `_`-prefixed identifier that is a repository- or data-service-typed field of the
  enclosing class; result pasted into the task-log. Plus a per-method code-review checklist item.

- **REQ-UOW-38** — A service that gains a read lambda but has no `IUnitOfWork` today SHALL acquire one
  by constructor injection. This applies to `SongSuggestionService` and `ArtistSuggestionService` only.

  **Parameter position is specified, not left to convention** — REQ-UOW-49's permitted `CreateSut` edit
  is a *positional* argument insertion, so an unstated position makes that carve-out ambiguous. The
  `IUnitOfWork` parameter SHALL be placed **immediately after the last repository parameter and before
  the `providers` parameter**. This matches the existing codebase: `CatalogService`
  (`CatalogService.cs:17`) is `(ICatalogRepository, IUnitOfWork, ILogger)` — uow 2nd of 3, directly
  after its only repository; `ArtistService` (`ArtistService.cs:22-27`) is
  `(IArtistRepository, ISongRepository, ICatalogRepository, IUnitOfWork, ILogger)` — uow 4th, again
  directly after the last repository. Applied here (line numbers verified 2026-08-25):

  | Service | Current signature | Resulting `IUnitOfWork` position |
  |---|---|---|
  | `ArtistSuggestionService` (`:23-27`) | `(IArtistRepository, IEnumerable<IMusicMetadataProvider>, ISimilarityScorer, ILogger<…>)` | **2nd of 5** — after `artistRepository`, before `providers` |
  | `SongSuggestionService` (`:25-30`) | `(ISongRepository, IArtistRepository, IEnumerable<IMusicMetadataProvider>, ISimilarityScorer, ILogger<…>)` | **3rd of 6** — after `artistRepository`, before `providers` |
  **No DI-registration work is in scope.** Neither service is registered in `MauiProgram.cs` today and
  neither is registered by this change; that belongs to the future autocomplete feature
  (`§ The two suggestion services are pre-built, not dead code`). `MauiProgram.cs` SHALL NOT be edited
  by this change and SHALL NOT appear in any wave's `Files owned`. Consequently this AC carries **no
  container-resolution test** — such a test is unsatisfiable against the real container by
  construction, and its absence here is deliberate.
  *Test:* the existing `SongSuggestionServiceTests` / `ArtistSuggestionServiceTests` suites stay green
  after each `CreateSut` helper is given a `PassthroughUnitOfWork.Over(...)` argument (the single
  permitted test edit — REQ-UOW-49), proving the new constructor parameter is wired and every lambda
  body resolves its repository from `sp`. Plus the REQ-UOW-36/37 file walk, which covers both files.

### Entity-returning reads (highest risk — these are what can hand out a tracked instance)

- **REQ-UOW-39** — The following entity-returning service reads SHALL be wrapped per REQ-UOW-36/37:
  `PersonService.GetPersonByIdAsync:177`, `PersonService.GetPersonByNameAsync:181`,
  `PersonService.SearchPersonsAsync:185`, `PersonService.SearchPersonsStartsWithAsync:194`,
  `SongService.GetSongByIdAsync:268`, `BackupService.GetHistoryAsync:169`.
  Each SHALL return the same values as before the change for the same database state.
  *Test:* one integration test per method against a real SQLite temp-file database (`testing.md §
  Project anti-patterns` — never the in-memory provider), asserting the returned entity/collection
  matches a seeded row set; plus an assertion that two successive calls to the same method obtain two
  **distinct** `AppDbContext` instances.
  **Observation mechanism (shared with REQ-UOW-42) — the parent's REQ-UOW-05 technique:** the context
  identity is captured *from inside the unit of work*, by resolving `AppDbContext` from the lambda's
  own `sp` (`sp.GetRequiredService<AppDbContext>()`) and recording the reference, then asserting
  `Assert.NotSame` pairwise across the captured references. There is no way to observe scope identity
  from outside the lambda, so tests use a probe registered on the `UnitOfWorkTestHost` container, or a
  counting/recording `IUnitOfWork` decorator that forwards to the real implementation and stores each
  body's resolved `AppDbContext`. Overlap is produced by awaiting a `TaskCompletionSource` inside each
  body — **never** `Task.WhenAll`, which does not guarantee interleaving (parent `plan.md`, Global
  Constraints).

### Projection / DTO reads

- **REQ-UOW-40** — The following projection- or scalar-returning service reads SHALL be wrapped per
  REQ-UOW-36/37, returning identical values for identical database state:
  `VenueService.GetPagedVenuesForListAsync:189`, `PersonService.GetPagedPersonsForListAsync:207`,
  `SongService.GetPagedSongsForListAsync:305`, `SongService.ExistsByTitleForArtistAsync:272`,
  `CatalogService.GetPagedCatalogForArtistAsync:25`, `ArtistService.GetPagedArtistsForListAsync:149`,
  `ArtistService.SearchArtistsByNameAsync:161`, `SongKaraokeUrlService.GetUrlsForSongAsync:38`,
  `SongKaraokeUrlService.GetSuggestedUrlAsync:106`, `BackupService.HasRecentBackupAsync:175`.
  *Test:* one integration test per method against real SQLite asserting the returned DTO/scalar equals
  the pre-change expected value for a seeded fixture. Existing tests for these methods MUST remain
  green unmodified (`testing.md § Builder Must Not Modify Tests`).

- **REQ-UOW-41** — Argument-validation and short-circuit logic that today runs **before** the
  repository call (e.g. `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber)` in
  `GetPagedArtistsForListAsync`, and the `normalized.Length < 2` / `IsNullOrWhiteSpace` early returns
  in `SearchArtistsByNameAsync`, `ArtistSuggestionService.GetLocalAsync`,
  `SongSuggestionService.GetLocalAsync`) SHALL remain **outside** the `ExecuteReadAsync` lambda, so
  that no DI scope is created for a call that never touches the database and so that the exception
  type and message observed by callers are unchanged.

  **The set is closed — these are ALL the in-scope methods with pre-repository validation or a
  short-circuit** (line numbers verified by direct file inspection, 2026-08-25):

  | Method | Guard | Line(s) |
  |---|---|---|
  | `ArtistService.GetPagedArtistsForListAsync` | `ThrowIfNegativeOrZero(pageNumber)` / `(pageSize)` | `:153-154` |
  | `ArtistService.SearchArtistsByNameAsync` | `IsNullOrWhiteSpace(normalized)` → `return []` | `:165-166` |
  | `ArtistService.GetDeleteConfirmationAsync` | `idList.Count == 1` branch guard | `:175` |
  | `PersonService.SearchPersonsAsync` | `searchTerm.Length < 2` → `return []` | `:188-189` |
  | `PersonService.SearchPersonsStartsWithAsync` | `searchTerm.Length < 2` → `return []` | `:197-198` |
  | `SongService.GetPagedSongsForListAsync` | `ThrowIfNegativeOrZero(pageNumber)` / `(pageSize)` | `:308-309` |
  | `CatalogService.GetPagedCatalogForArtistAsync` | `ThrowIfNegativeOrZero(pageNumber)` / `(pageSize)` | `:28-29` |
  | `VenueService.GetPagedVenuesForListAsync` | `ThrowIfNegativeOrZero(pageNumber, nameof(...))` / `(pageSize, …)` | `:194-195` |
  | `ArtistSuggestionService.GetLocalAsync` | `trimmed.Length < 2` → `return []` | `:39-40` |
  | `SongSuggestionService.GetLocalAsync` | `IsNullOrWhiteSpace(term)` → `return []` | `:42-43` |

  > **Two of these guards change value under REQ-UOW-51** (`ArtistService.SearchArtistsByNameAsync`
  > and `SongSuggestionService.GetLocalAsync`, both `IsNullOrWhiteSpace` → `Length < 2`). REQ-UOW-41
  > governs their **position** (outside the lambda); REQ-UOW-51 governs their **threshold**. The two
  > are independent and both hold.

  Note `ArtistSuggestionService.GetLocalAsync`'s guard is preceded by the normalization call
  `var trimmed = term.NormalizeSearchQuery();` at `:38`. Normalization is not itself a guard, but it
  feeds one and stays outside the lambda with it; the lambda begins at the first repository call
  (`:42`).

  Note `VenueService`'s two guards pass `nameof(...)` and the others do not; the resulting
  `ParamName`/message differ, which is exactly why the test asserts against *today's* observed
  exception rather than a hand-written expectation.

  *Test:* one unit test per row above asserting the same exception type, `ParamName` and message as
  pre-change code for an invalid argument (or the same empty result for a short-circuit), plus an
  assertion (via a counting `IUnitOfWork` test double) that `ExecuteReadAsync` was invoked **zero**
  times on that path. A method not in this table has no guard and needs no such test.

### Paged list reads are mandatory scope

- **REQ-UOW-42** — The **five** paged-list service reads (`GetPagedVenuesForListAsync`,
  `GetPagedPersonsForListAsync`, `GetPagedSongsForListAsync`, `GetPagedArtistsForListAsync`,
  `GetPagedCatalogForArtistAsync`) SHALL be scoped. These are exactly the reads `DbLoadGate`
  serialises; leaving any of them unscoped means limb (a) of REQ-UOW-29 is unmet and the gate cannot
  be removed. Scoping them is **not** optional or deferrable to a later phase.

  > **Not a sixth.** `SongSuggestionService.GetLocalAsync:40` also calls
  > `_songRepository.GetPagedAsync`, but it is **not** a `DbLoadGate` consumer — it is reached from a
  > suggestion lookup, not from a `CrudListViewModelBase` page load, and no `CrudListViewModelBase`
  > subclass fetches through it. It is scoped under REQ-UOW-43, not under this AC, and it is not part
  > of limb (a)'s gate-removal precondition.

  *Test:* a concurrency test that starts two paged-list loads across two different services
  simultaneously **with `DbLoadGate` removed** and asserts (i) two distinct `AppDbContext` instances
  were used — captured by the REQ-UOW-39 observation mechanism, with overlap forced by a
  `TaskCompletionSource` awaited inside each body rather than `Task.WhenAll` — and (ii) no
  `InvalidOperationException` mentioning "A second operation was started on this context" is thrown.

### Network and file I/O stay outside the unit of work

- **REQ-UOW-43** — No `ExecuteReadAsync` lambda SHALL contain a call to `IMusicMetadataProvider` (or
  any other HTTP-backed collaborator). In `SongSuggestionService` and `ArtistSuggestionService`, only
  the repository segments go inside a lambda; `FetchFromProvidersAsync` (`SongSuggestionService.cs:78`)
  and the provider fetch reached from `ArtistSuggestionService.GetRemoteAsync` (`:57`) run **outside**
  it. Holding a `DbContext` scope open across a network round-trip is prohibited.
  Concretely, the wrapped segments are: `SongSuggestionService.GetLocalAsync:40`,
  `SongSuggestionService.DedupAsync:113` (its `GetByTitlesCollatedAsync` call),
  `SongSuggestionService.ResolveLocalArtistIdsAsync:153` (its `GetByNamesCollatedAsync` call),
  `ArtistSuggestionService.GetLocalAsync:36`, and the `GetByNamesCollatedAsync` call at
  `ArtistSuggestionService.cs:79` inside `GetRemoteAsync`.
  *Test:* (1) a Python file-walk assertion that no `ExecuteReadAsync` lambda body in `Services/*.cs`
  contains the identifiers `_providers`, `IMusicMetadataProvider`, or `FetchFromProviders`; (2) a
  behavioural test using a fake `IMusicMetadataProvider` that blocks on a signal, asserting that while
  the provider call is in flight the `IUnitOfWork` test double reports **zero** open read scopes.

- **REQ-UOW-44** — `BackupService.ExportBundleAsync:86` SHALL wrap only its
  `_repo.GetLatestSnapshotAsync` call (`:90`) in `ExecuteReadAsync`; the subsequent `File.Exists`
  (`:91`), `ZipFile.Open` (`:97`) and the entry copies SHALL remain outside the lambda, for the same
  reason as REQ-UOW-43 — long-running non-database I/O must not hold a scope.

  **The wrap goes INSIDE the existing `try` block** (`:88-89`), not around it. The method's whole body
  already sits in a `try/catch` that maps *any* exception to `(false, "Export failed…")`. Placing the
  `ExecuteReadAsync` call inside that `try` preserves the current failure mapping exactly: a read that
  throws still returns the same failure tuple it returns today, and no exception newly escapes the
  service boundary (`code-style-reference.md § Service Return Patterns`). Hoisting the read above the
  `try` would change observable failure behaviour and is prohibited.

  *Test:* a code-review checklist item; a unit test with a fake `IBackupRepository` and a fake
  `IUnitOfWork` asserting the read scope is closed before any file-system call occurs; **and** a unit
  test in which the repository read throws, asserting the returned tuple is still
  `(false, <the existing "Export failed…" message>)` and that no exception propagates.

### BUG-078 — the defect site

- **REQ-UOW-45** — `ArtistService.GetDeleteConfirmationAsync:172` SHALL obtain the artist through
  `ExecuteReadAsync`, resolving `IArtistRepository` from the lambda's `sp`. After this change, a
  rename committed through `IUnitOfWork` SHALL be visible to a subsequent
  `GetDeleteConfirmationAsync` call for the same artist id.
  ```
  Given an Artist row persisted with Name = "Old Name"
  And GetDeleteConfirmationAsync([id]) has already been called once (priming the read path)
  When the artist is renamed to "New Name" through ArtistService.UpdateArtistAsync
      (which commits via IUnitOfWork.ExecuteAsync)
  And GetDeleteConfirmationAsync([id]) is called again
  Then the returned string is "Delete 'New Name'?"
  And it is not "Delete 'Old Name'?"
  ```
  *Test:* **the BUG-078 regression test** (Major severity ⇒ mandatory per `.claude/rules/bug-tracking.md`).
  An integration test against a real SQLite temp-file database implementing exactly the Given/When/Then
  above. It **MUST be seen to FAIL on pre-change code** (it returns `"Delete 'Old Name'?"` because the
  captive context serves its tracked copy) **and PASS after**. Both runs' output are pasted into
  `task-log.md`; a task-log entry without both is invalid.

  **Required harness — `UnitOfWorkTestHost`, not `TestDbContextFactory`.** The Red depends on the
  captive context being reproduced, and only `UnitOfWorkTestHost` reproduces it: it composes the real
  DI graph over a SQLite temp file with a **single long-lived scope**, exposing
  `Services => Scope.ServiceProvider` (`MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs:22`) —
  the MAUI single-window scope this whole change models. A test written against a per-call
  `TestDbContextFactory` gets a fresh context for every operation, so nothing is ever stale, the
  assertion passes against unfixed code, and **the mandatory Red is unobtainable**. The test therefore
  SHALL resolve `IArtistService` once from a single `UnitOfWorkTestHost` instance and **reuse that same
  host across all three steps** — prime → rename → re-read. Creating a second host, a second scope, or
  re-resolving from the root provider between steps invalidates the test.

  > **The Red is only obtainable while `.AsTracking()` is still present.** The staleness exists
  > *because* `ArtistRepository.cs:80` calls `.AsTracking()`; remove it first and this test passes
  > against unfixed `ArtistService` code, destroying the mandatory Red. The Red MUST therefore be
  > captured before REQ-UOW-46's removal. See `design.md § 7`, which encodes this as a hard ordering
  > constraint.

- **REQ-UOW-46** — `Infra/Repository/ArtistRepository.GetByIdAsync` (`:79-80`) SHALL NOT call
  `.AsTracking()`. The call is removed; the method becomes an ordinary read under the global
  `NoTracking` default. Rationale — including the honest case for keeping it and the decisive
  ambient-scope failure mode that rules it out — is `design.md § 5b`. `GetByIdAsync` SHALL remain
  correct for its existing write-path callers.

  **Two stale comments SHALL be rewritten in the same commit as the removal.** Both name the call
  being deleted; leaving either is a review failure, not a nit (same defect class this spec already
  polices elsewhere). Line numbers verified by direct file inspection, 2026-08-25:

  1. **`Infra/AppDbContext.cs:36`** currently reads
     `// Edit queries use explicit .AsTracking() to enable change detection`. After this removal there
     is **zero** `.AsTracking()` anywhere in the repository layer, so that sentence describes code that
     no longer exists. It SHALL be rewritten to state the actual policy — reads never track; write
     paths set entity state explicitly through `ExecuteAsync` lambdas rather than relying on a tracked
     read (`design.md § 5b` reason 2). The neighbouring lines `:34-35` (the BUG-018 `NoTracking`
     rationale and the defence-in-depth `.AsNoTracking()` note) remain correct and SHALL be left alone.
     **This is a comment-only edit** — `ChangeTracker.QueryTrackingBehavior` at `:37` is untouched
     (`§ Explicitly out of scope`). REQ-UOW-46's own `Infra/Repository/` file walk is scoped to the
     repository folder and does **not** reach `AppDbContext.cs`, which is why this rewrite is named
     explicitly and `Infra/AppDbContext.cs` is added to Wave 3's `Files owned` (`design.md § 7`).
  2. **`MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs:41-44`** — see the removal
     guard immediately below.

  **Removal guard — named tests that must stay green, unmodified:**
  - the existing artist update/delete tests;
  - **`MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`**,
    `Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` (`[Fact]` at `:45`). Its comment at
    **`:41-44`** states in terms that the artist family never reproduced BUG-068 *precisely because*
    `ArtistRepository.cs:79-80` calls `.AsTracking()`, so EF identity resolution returns the
    already-tracked instance. This test therefore **documents a dependence on the very call being
    removed.** It must still pass without `.AsTracking()` (it should: the write path sets entity state
    explicitly — `design.md § 5b` reason 2), and its `:41-44` comment SHALL be rewritten **in the same
    commit as the removal** so it no longer cites a call that no longer exists. Leaving the stale
    comment is a review failure, not a nit.

  If any guard test turns red, that is evidence a write path *was* relying on the read's tracking —
  stop and log `blocked: spec gap`; do not restore `.AsTracking()` and do not edit the test
  (`testing.md § Builder Must Not Modify Tests`). The `:41-44` comment rewrite is the sole permitted
  edit to `Bug068RegressionTests.cs`, and it touches no assertion.

  *Test:* a repository test against real SQLite asserting `GetByIdAsync` returns the row; plus a
  source-level assertion (Python file walk) that `Infra/Repository/` contains **zero** `.AsTracking()`
  occurrences after the change; plus the guard tests above green, unmodified.

### `DbLoadGate` removal (parent Phase 4.7)

- **REQ-UOW-47** — The static `DbLoadGate` (`CrudListViewModelBase.cs:16`, waits at `:128`/`:207`,
  releases at `:179`/`:241`) SHALL be removed **only** after BOTH limbs of REQ-UOW-29 are satisfied
  and evidenced:
  - **(a)** every in-scope consumer of this census is converted and committed (REQ-UOW-36 … 45 green);
  - **(b)** the `page-load-frozen` regression suite is confirmed green **with the gate absent**.
    `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` contains **five** `[Fact]` methods
    (at `:10`, `:40`, `:78`, `:103`, `:120`). Exactly **two** of them carry the off-context assertion
    `Assert.NotSame(uiContext, contextDuringFetch)` — at **`:37`** (in
    `InitializeAsync_WithSynchronizationContext_ExecutesFetchOffContext`) and **`:75`** (in
    `LoadMoreCommand_WithSynchronizationContext_ExecutesFetchOffContext`). Those two assert `Task.Run`
    off-context behaviour and **not** the semaphore.
    **Limb (b) requires the WHOLE five-test file green, not merely the two assertions.** The other
    three tests exercise shimmer state and search debounce, which run through the same
    `LoadFirstPageAsync` / `LoadMoreAsync` paths the gate is being cut out of; a break in any of them
    is a real regression from the deletion. The two off-context assertions are the *sharpest* guard,
    not the *whole* guard. All five are *expected* to stay green, but this expectation SHALL be
    verified by actually running the suite after deletion — never asserted from reasoning.
  **A third stale comment SHALL be rewritten in the deletion commit** (verified by direct file
  inspection, 2026-08-25). Deleting the gate leaves **three** references to the symbol behind, not two:

  1. `CrudListViewModelBase.cs`'s comment block at `:12-15` (the gate rationale — step 4 of
     `design.md § 6`);
  2. `CrudListViewModelBase.cs:304`, "Do NOT hold `DbLoadGate` here" (REQ-UOW-48's table);
  3. **`MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs:215`** — inside
     `PumpSynchronizationContext.Post`'s `catch (InvalidOperationException)` block, the comment reads
     *"…a swallowed post would strand the static `DbLoadGate` and hang every later test"*. That
     sentence is the stated justification for the `ThreadPool.QueueUserWorkItem` fallback at `:217`.
     Once the gate is deleted it names a symbol that no longer exists. It SHALL be rewritten to justify
     the fallback on its surviving ground — a swallowed post strands the SUT's `finally` blocks and
     hangs later tests — **comment text only: no assertion, no `[Fact]`, no setup or teardown change,
     and the `ThreadPool` fallback itself stays.** This makes `CrudListViewModelBaseTests.cs` the
     **fourth** permitted test-file edit under REQ-UOW-49's carve-out; without that row REQ-UOW-47 and
     REQ-UOW-49 would contradict each other.

  *Test:* `dotnet test --filter CrudListViewModelBaseTests` executed on the branch **after** the gate's
  deletion, with the full pass output pasted into `task-log.md` alongside the pre-deletion run. Limb
  (a) is evidenced by the REQ-UOW-36 file-walk output. **Plus a tree-wide source-level assertion
  (Python file walk over the whole repository, not `grep`) that the identifier `DbLoadGate` occurs
  **zero** times** — parent REQ-UOW-29 requires "a source-level check that `DbLoadGate` no longer
  exists" *in addition to* the regression-suite evidence, and this AC previously carried only the
  latter limb. The walk covers comments and string literals as well as code, so it mechanically catches
  all three stale references above, including the `CrudListViewModelBaseTests.cs:215` one.

- **REQ-UOW-48** — The `Task.Run(...)` offloads in `CrudListViewModelBase.LoadFirstPageAsync` and
  `LoadMoreAsync` SHALL **survive** the removal of `DbLoadGate`. They are a separate mitigation for the
  `Microsoft.Data.Sqlite` sync-async freeze, with a separate revert trigger (`INFRA_MSSQL`,
  `constraints-registry.md § EF Core / SQLite`). The two rationales sit in one comment block; the
  comment SHALL be edited so the surviving `SQLITE-WORKAROUND` rationale remains legible after the
  gate's rationale is deleted.

  **`CrudListViewModelBase.cs` contains FOUR `Task.Run` calls; exactly two are protected by this AC:**

  | Line | Enclosing method | Disposition |
  |---|---|---|
  | `:141` | `LoadFirstPageAsync` | **PROTECTED — must survive.** |
  | `:216` | `LoadMoreAsync` | **PROTECTED — must survive.** |
  | `:254` | fire-and-forget `_ = Task.Run(...)` (search-debounce path) | Not covered by this AC; unrelated to the gate. Do not touch. |
  | `:306` | `ExecuteConfirmActionAsync` (`await Task.Run(action)`) | Not covered by this AC. Its neighbouring comment at `:304` says "Do NOT hold `DbLoadGate` here" and **must be updated** by REQ-UOW-47's deletion, since it names a thing that will no longer exist — but the `Task.Run` itself stays. |

  Only `:141` and `:216` are the `page-load-frozen` mitigation. The other two are untouched by this
  change; naming all four here prevents both an over-deletion and a mistaken "there are only two".

  *Test:* the two `CrudListViewModelBaseTests` off-context assertions at `:37` and `:75` green after
  the gate's removal — they fail if the `:141` or `:216` `Task.Run` is deleted — with the whole
  five-test file green per REQ-UOW-47 limb (b). Plus a source-level assertion (Python file walk) that
  `Task.Run` still appears in both `LoadFirstPageAsync` and `LoadMoreAsync`.

### No behavioural regression

- **REQ-UOW-49** — No public service-interface signature in `Domain/ServicesInterfaces/` SHALL change,
  and no ViewModel or page SHALL be edited as part of this change other than
  `CrudListViewModelBase.cs` (for REQ-UOW-47/48). Existing tests SHALL pass **unmodified**, subject to
  the closed carve-out below.

  **Carve-out — exactly four permitted edits under `MyVocaList.Tests/`, and no others.** REQ-UOW-38
  adds an `IUnitOfWork` constructor parameter to `SongSuggestionService` and `ArtistSuggestionService`.
  That is a **compile break** in the two suites that construct them directly, so "unmodified" is
  literally unachievable for those files and pretending otherwise would put REQ-UOW-38 and REQ-UOW-49
  in contradiction. The remaining two rows are **stale-comment rewrites forced by REQ-UOW-46 and
  REQ-UOW-47**: each names a symbol those ACs delete, and leaving it is the exact defect REQ-UOW-46
  calls "a review failure, not a nit". Neither touches an assertion:

  | File | Permitted edit | Nothing else |
  |---|---|---|
  | `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs` | In the `CreateSut` helper (`:20`), add a `PassthroughUnitOfWork.Over(...)` argument for the new constructor parameter, passing the existing `_songRepoMock` / `_artistRepoMock` fields through. | No `[Fact]` body, no `Assert`, no `Setup`/`Verify`, no field may change. |
  | `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs` | In the `CreateSut` helper (`:22`), the same edit, passing `_repoMock` through. | Same. |
  | `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` | Rewrite the stale `.AsTracking()` comment at `:41-44` (REQ-UOW-46). | Comment text only — no assertion, no `[Fact]`. |
| `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` | Rewrite the `DbLoadGate` reference in the comment at `:215` — inside `PumpSynchronizationContext.Post`'s `catch (InvalidOperationException)` block — so it no longer names the deleted gate (REQ-UOW-47). | Comment text only — no assertion, no `[Fact]`, no setup/teardown change, and the `ThreadPool.QueueUserWorkItem` fallback at `:217` stays. Committed in the Wave 8 deletion commit. |

  `PassthroughUnitOfWork.Over(...)` already exists at
  `MyVocaList.Tests/Infrastructure/UnitOfWorkMocks.cs:42` (class declared `:13`) and already implements
  `ExecuteReadAsync` (`:25-27`) by running the body immediately against a stub `IServiceProvider`, so
  the lambda's `sp.GetRequiredService<IXRepository>()` resolves to the very mock the test already
  asserts on. **No assertion may be weakened, deleted, or relaxed** anywhere
  (`testing.md § Builder Must Not Modify Tests`). If a suite cannot be made green by the `CreateSut`
  edit alone, log `blocked: spec gap` — do not touch a test body.

  *Test:* full `dotnet test` green, plus a `git diff --stat` over `MyVocaList.Tests/` recorded in the
  task-log showing **only** new test files added by this change plus the four rows above; any fifth
  modified pre-existing test file is a violation. Additionally, a reviewed `git diff` of those four
  files confirming no `Assert`/`Verify`/`Setup` line changed.

### The rule must outlive the landing commit

- **REQ-UOW-50** — The repository shall carry an **xUnit architecture test** that enforces REQ-UOW-36,
  REQ-UOW-37 and REQ-UOW-43 as a permanent, executing gate rather than as one-off task-log evidence.
  The test reads the `Services/*.cs` source files from disk and asserts that:
  - **(a)** no repository- or data-service-typed `_`-prefixed field of the enclosing class is
    dereferenced anywhere **outside** an `ExecuteAsync` / `ExecuteReadAsync` lambda body (REQ-UOW-36 +
    the positive half of REQ-UOW-37);
  - **(b)** no such `_`-prefixed field is dereferenced **inside** a lambda body either — inside,
    collaborators come from `sp` (the prohibition half of REQ-UOW-37);
  - **(c)** no `ExecuteReadAsync` lambda body contains the identifiers `_providers`,
    `IMusicMetadataProvider`, or `FetchFromProviders` (REQ-UOW-43).

  **This AC is writable in a form that passes vacuously, and R1 is rated High — so the test's own
  construction is specified, not left to the implementor.** All four sub-requirements below are
  mandatory; a test missing any of them does not satisfy this AC.

  **(i) "Repository- or data-service-typed field" is defined mechanically.** The term is otherwise
  undefined, and a wrong classification would make the test fail against correct, in-spec code — e.g.
  `BackupService._logWriter` (`ITransactionLogWriter`) is dereferenced at `BackupService.cs:95`
  (`_logWriter.CurrentSessionLogPath`) **outside** any lambda, in the very method REQ-UOW-44 wraps, and
  that is correct and must stay. The governed field set is therefore exactly:

  - **every field whose declared type name matches `I*Repository`** (regex `^I[A-Za-z0-9]*Repository$`),
    which is what makes the rule survive a repository added later; **plus**
  - **this explicitly enumerated list of data-service-typed fields** — services that themselves reach a
    repository, so their identity likewise determines which `AppDbContext` is used (census taken over
    `Services/*.cs`, 2026-08-25): `IArtistService`, `ISongService`, `IArtistResolutionService`,
    `ISongResolutionService`, `ISongKaraokeUrlService`.

  Everything else is **out** of the governed set and may be referenced from the enclosing instance
  anywhere, inside a lambda or outside it (`design.md § 3`): `IUnitOfWork`, `ILogger<…>`,
  `ISimilarityScorer`, `IMusicMetadataProvider` / `_providers` (governed instead by (c), which forbids
  it *inside* a read lambda), `ITransactionLogWriter`, `INotificationService`, `IHttpClientFactory`,
  `HttpClient`, MAUI essentials wrappers (`IPreferences`, `IFileSystem`, `IAppInfo`, `IDeviceInfo`,
  `ISecureStorageWrapper`), `IConfiguration`, `ResourceManager`, `SemaphoreSlim`, and all primitive
  fields. The enumerated data-service list is a **closed list carried in a named constant in the test
  file**; extending it is a deliberate edit, and adding a new data service without adding it here is a
  gap the reviewer must catch.

  **(ii) A non-zero-file guard is mandatory.** The test enumerates `Services/*.cs` from disk, so if the
  directory is not found — a changed output layout, a moved project, a CI working directory — the
  enumeration yields an empty set, every assertion holds over it, and the test passes **permanently
  while guarding nothing**. That is the exact vacuous-pass failure mode this AC exists to prevent. The
  test SHALL assert `Assert.NotEmpty(serviceFiles)` **and** a floor on the count —
  `Assert.True(serviceFiles.Count >= 25, …)` (there are 30 `.cs` files under `Services/` as of
  2026-08-25; the floor is set below that so ordinary additions and deletions do not churn it, while a
  collapse to a near-empty set still fails loudly).

  **(iii) Comments and string literals SHALL be stripped before matching, and field checks SHALL be
  anchored on `_field` followed by `.`.** Checks (a)–(c) and REQ-UOW-43's walk are identifier-based, and
  the codebase already contains explanatory prose naming governed fields — verified 2026-08-25:
  `ArtistResolutionService.cs:108` (`"…A surviving _artistService reference here would silently defeat
  the ambient join."`, a comment **inside** the `ExecuteAsync` lambda body), `SongResolutionService.cs:183`
  (the same shape, naming `_artistResolution`, also inside the lambda body), and
  `SongResolutionService.cs:288` (an XML doc comment on the private helper `ResolveOrCreateArtistIdAsync`,
  naming `_artistResolution`). A naive identifier match false-positives on all three — in the two files
  this AC promises "are already compliant and simply pass". The test SHALL therefore (1) strip `//`,
  `/* */` and `///` comment content and the contents of string/verbatim/interpolated literals before any
  matching, and (2) match a field dereference as the identifier immediately followed by `.` (e.g.
  `_artistRepository.`), not as a bare identifier occurrence. Stripping is required for (a), (b) and (c)
  alike, and for the REQ-UOW-36/37/43 landing-commit file walks, which use the same matcher.

  > **Note the one exception to stripping:** REQ-UOW-47's tree-wide `DbLoadGate` check deliberately does
  > **not** strip comments — there its whole purpose is to catch stale comments. These are two different
  > walks with two different matchers; do not share one implementation between them.

  **(iv) Path resolution SHALL reuse the existing proven helper, not reinvent one.**
  `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkCompositionTests.cs` already solves this:
  `LocateSource(string relativePath)` (declared `:150`) walks up from `AppContext.BaseDirectory` until
  the relative path exists and **throws `FileNotFoundException`** if it never does (`:159`) — so a
  missing tree fails loudly instead of yielding an empty set. The architecture test SHALL locate
  `Services/` by that same helper (extract it to a shared test-infrastructure location, or call it —
  either is acceptable; a hand-rolled relative path such as `../../../../Services` is not). This is
  belt-and-braces with (ii): the throw is the primary guard, `Assert.NotEmpty` the backstop.

  Rationale: R1 is rated **High** likelihood — an implementor referencing `_repository` inside a new
  lambda produces code that compiles, passes every behavioural test, and leaves the defect in place
  while *looking* fixed. A Python file walk pasted into a task-log catches that once, on the day it is
  run; it does not catch the next service added six months from now. Only a test in the suite does.
  The file walks required by REQ-UOW-36/37/43 remain as the landing-commit evidence; this AC makes the
  same checks permanent.

  Files in `§ Explicitly out of scope` (`SongResolutionService`, `ArtistResolutionService` are already
  compliant and simply pass) and any future deliberate exception are handled by an **explicit,
  commented allow-list inside the test** — never by loosening the assertion. An empty allow-list is
  the intended end state.

  *Test:* the architecture test itself. It SHALL be seen to **fail** against a deliberately
  reintroduced `_field` dereference inside a lambda (demonstrate on a scratch edit, revert, paste both
  outputs into `task-log.md`) — a guard never seen to fail is not known to guard anything.

- **REQ-UOW-51** — Every **local database** search/suggestion entry point SHALL short-circuit and
  return an empty result when the normalized query is shorter than **2 characters**. The guard stays
  outside the `ExecuteReadAsync` lambda (REQ-UOW-41), so a sub-threshold query creates no DI scope
  and issues no SQL.

  **Rationale (community practice, researched 2026-08-25).** Autocomplete convention splits by data
  source: an in-memory/local index can match usefully at 1–2 characters, while a remote API is
  conventionally held to 3. A single character over a local table matches an unbounded fraction of
  rows and returns a list with no discriminating value, so 1 is rejected; 3 is unnecessarily strict
  for a fast local SQLite query on an indexed collated column. **2 is the local threshold.** This is
  also the value already in force in `PersonService` (`:188-189`, `:197-198`) and
  `ArtistSuggestionService` (`:39-40`), so the AC *standardises on the majority existing behaviour*
  rather than introducing a new one.

  **The set is closed — exactly two call sites change; the other four already comply:**

  | Method | Today | After | Change? |
  |---|---|---|---|
  | `ArtistService.SearchArtistsByNameAsync` (`:165-166`) | `IsNullOrWhiteSpace(normalized)` | `normalized.Length < 2` | **CHANGES** |
  | `SongSuggestionService.GetLocalAsync` (`:42-43`) | `IsNullOrWhiteSpace(term)` | `trimmed.Length < 2` | **CHANGES** |
  | `PersonService.SearchPersonsAsync` (`:188-189`) | `Length < 2` | unchanged | no |
  | `PersonService.SearchPersonsStartsWithAsync` (`:197-198`) | `Length < 2` | unchanged | no |
  | `ArtistSuggestionService.GetLocalAsync` (`:39-40`) | `trimmed.Length < 2` | unchanged | no |
  | `SongService` / `VenueService` / `CatalogService` paged lists | not a search entry point | n/a | no |

  `SongSuggestionService.GetLocalAsync` has no normalization call today, unlike
  `ArtistSuggestionService.GetLocalAsync` (`:38`). REQ-UOW-51 requires the threshold to be measured
  on the **trimmed** term, so the implementor adds the same `NormalizeSearchQuery()` call (or a
  `Trim()`, matching whatever `ArtistSuggestionService` does) ahead of the guard — this is the only
  behavioural addition the AC permits, and it stays outside the lambda.

  The threshold SHALL be expressed as a **single named constant** shared by all call sites (e.g.
  `SearchConstants.MinimumLocalQueryLength = 2`), not as a repeated `2` literal — a magic number
  repeated across five services is how the current inconsistency arose.

  *Test:* for each of the two changing call sites, a unit test asserting that a 1-character query
  returns an empty result **and** that the repository mock was never called (`Verify(..., Times.Never)`),
  plus a 2-character query that does reach the repository. The existing `PersonService` and
  `ArtistSuggestionService` guard tests MUST stay green **unmodified**.

- **REQ-UOW-52** — Every **remote HTTP provider** fetch SHALL short-circuit before any network call
  when the normalized query is shorter than **3 characters**. Concretely:
  `SongSuggestionService.FetchFromProvidersAsync` (`:78`) and the provider fetch reached from
  `ArtistSuggestionService.GetRemoteAsync` (`:57`).

  **Rationale (community practice, researched 2026-08-25).** 3 is the dominant published default for
  API-backed autocomplete (NCI Design System default 3; the widely-cited jQuery UI/Kendo guidance to
  raise `minLength` above 1 whenever a query "could match a lot of items"; the common `minLengthTerm: 3`
  for remote sources). The asymmetry with REQ-UOW-51 is deliberate and is the whole point: a local
  query costs a millisecond of SQLite, a remote one costs a network round trip, third-party rate-limit
  budget, and battery. Two characters against a global music-metadata catalogue returns noise at real
  cost.

  Expressed as a second named constant (e.g. `SearchConstants.MinimumRemoteQueryLength = 3`) beside
  the first, so the asymmetry is visible in one place rather than inferred from two call sites.

  The guard SHALL sit **outside** the `ExecuteReadAsync` lambdas exactly as REQ-UOW-41 requires, and
  its addition SHALL NOT move any provider call inside one (REQ-UOW-43 is unchanged and still binding).

  *Test:* a 2-character query drives the local path only — asserted with a fake
  `IMusicMetadataProvider` that records invocations and MUST record zero; a 3-character query reaches
  it. This composes with, and does not replace, REQ-UOW-43's blocking-fake test.

  **Debounce is explicitly NOT in scope.** Community practice pairs a remote minimum length with a
  ~200–250 ms input debounce, but debounce is a **UI-timing** concern belonging to the
  `ArtistFormPage`/`SongFormPage` autocomplete feature that will consume these services (see
  `§ The two suggestion services are pre-built, not dead code`). Recorded here as a **forward
  constraint on that feature**, not as work for this change: the existing
  `CrudListViewModelBase.cs:254` debounce path stays untouched (REQ-UOW-48's do-not-touch list).

---

## Scope — the verified census

Every row below is addressed by an AC above. The census was produced by direct file inspection and
verified by a Python file walk (Bash `grep`/`rg` is proxied through a lossy wrapper that has produced
false zeroes and is not admissible evidence here).

| Service | Method (line) | Class | AC |
|---|---|---|---|
| `PersonService` | `GetPersonByIdAsync:177` | entity | REQ-UOW-39 |
| `PersonService` | `GetPersonByNameAsync:181` | entity | REQ-UOW-39 |
| `PersonService` | `SearchPersonsAsync:185` | entity | REQ-UOW-39 |
| `PersonService` | `SearchPersonsStartsWithAsync:194` | entity | REQ-UOW-39 |
| `SongService` | `GetSongByIdAsync:268` | entity | REQ-UOW-39 |
| `BackupService` | `GetHistoryAsync:169` | entity | REQ-UOW-39 |
| `VenueService` | `GetPagedVenuesForListAsync:189` | projection, paged | REQ-UOW-40, 42 |
| `PersonService` | `GetPagedPersonsForListAsync:207` | projection, paged | REQ-UOW-40, 42 |
| `SongService` | `GetPagedSongsForListAsync:305` | projection, paged | REQ-UOW-40, 42 |
| `SongService` | `ExistsByTitleForArtistAsync:272` | scalar (`bool`) | REQ-UOW-40 |
| `CatalogService` | `GetPagedCatalogForArtistAsync:25` | projection, paged | REQ-UOW-40, 42 |
| `ArtistService` | `GetPagedArtistsForListAsync:149` | projection, paged | REQ-UOW-40, 41, 42 |
| `ArtistService` | `SearchArtistsByNameAsync:161` | projection | REQ-UOW-40, 41 |
| `SongKaraokeUrlService` | `GetUrlsForSongAsync:38` | projection | REQ-UOW-40 |
| `SongKaraokeUrlService` | `GetSuggestedUrlAsync:106` | projection | REQ-UOW-40 |
| `BackupService` | `ExportBundleAsync:86` (`GetLatestSnapshotAsync` only) | projection + file I/O | REQ-UOW-44 |
| `BackupService` | `HasRecentBackupAsync:175` | projection | REQ-UOW-40 |
| `ArtistService` | `GetDeleteConfirmationAsync:172` → `ArtistRepository.cs:80` | **BUG-078 defect site** | REQ-UOW-45, 46 |
| `SongSuggestionService` | `GetLocalAsync:40` | repo, no UoW today | REQ-UOW-38, 41, 43 |
| `SongSuggestionService` | `DedupAsync:113` | repo, no UoW today | REQ-UOW-38, 43 |
| `SongSuggestionService` | `ResolveLocalArtistIdsAsync:153` | repo, no UoW today | REQ-UOW-38, 43 |
| `SongSuggestionService` | `FetchFromProvidersAsync:78` | **HTTP — stays outside** | REQ-UOW-43 |
| `ArtistSuggestionService` | `GetLocalAsync:36` | repo, no UoW today | REQ-UOW-38, 41, 43 |
| `ArtistSuggestionService` | `GetRemoteAsync:57` (repo call `:79` only) | repo + HTTP split | REQ-UOW-38, 43 |
| `CrudListViewModelBase` | `DbLoadGate:16,128,179,207,241` | gate removal | REQ-UOW-47, 48 |

---

## Explicitly out of scope

- **`SongResolutionService` and `ArtistResolutionService`** — already use `ExecuteReadAsync` correctly
  (`ArtistResolutionService.cs:36` is the canonical shape this change copies). **Do not touch.**
- **All service write methods** — migrated by parent Phase 4.1–4.5. **Do not touch.**
- **`MauiProgram.cs` / all DI registration.** No service is registered or re-registered by this change.
  In particular `SongSuggestionService` and `ArtistSuggestionService` stay unregistered — deliberately,
  see `§ The two suggestion services are pre-built, not dead code`. `MauiProgram.cs` is a
  sequential-only file (`workflow.md`) and SHALL NOT appear in any wave's `Files owned`.
- **`QueueService` / Event & Queue entity code — *historical exclusion, now moot*.** The parent spec
  excluded it by Helder's decision D12. The whole Event/Queue feature has since been **deleted**
  (commit `c7ad5bd4`, "chore: delete the Event/Queue feature, keeping only its infra definitions"), so
  `QueueService` no longer exists and there is nothing to exclude. Retained here only so a reader
  comparing this list against the parent's does not think the exclusion was silently dropped. It is
  **not** a live scope boundary and no wave should reason about it.
- **Repository-layer internals** other than the single `.AsTracking()` decision of REQ-UOW-46. No
  repository method signature changes; no `AppDbContext` parameter is added to any repository method
  (REQ-UOW-10 caps ceremony at one line per service method, zero per repository method).
- **Removing `AppDbContext`'s global `NoTracking`** or altering `MauiProgram.cs:72`. Tracking policy is
  already correct and is not this change's lever (see `design.md § 2`).
- **Introducing transactions on the read path.** `ExecuteReadAsync` opens none and publishes no ambient
  scope (REQ-UOW-34); this change does not alter that.
- **The `INFRA_MSSQL` SQLite replacement** and the `Task.Run` offloads' eventual removal — a different
  revert trigger entirely (REQ-UOW-48 keeps them).
- **Any UI/XAML change.** No page or ViewModel other than `CrudListViewModelBase.cs` is edited.

---

## Resolved classifications (formerly "open questions")

All three items previously listed here as open are **closed**. Nothing in this spec is pending a
decision at implementation time.

1. **`BackupService.GetHistoryAsync` returns a mapped entity — classification confirmed "entity".**
   `BackupHistory` is a real EF-mapped entity, verified 2026-08-25 in three places:
   `Infra/AppDbContext.cs:24` (`public DbSet<BackupHistory> BackupHistories { get; set; }`),
   `Infra/AppDbContext.cs:77` (`ApplyConfiguration(new BackupHistoryConfiguration())`) with
   `Infra/EntityEFConfig/BackupHistoryConfiguration.cs`, and migration
   `Infra/Migrations/20260531044743_AddBackupHistory.cs`. The census row stands as written; REQ-UOW-39
   applies. No implementation-time verification is required.
2. **Two scopes per `SongSuggestionService.GetRemoteAsync` is a decision, not a question.** Recorded as
   stated rationale in `design.md § 4a`. **The two services differ and must not be conflated:** the two
   scopes belong to `SongSuggestionService.GetRemoteAsync` alone — one in `DedupAsync`
   (`GetByTitlesCollatedAsync`) and one in `ResolveLocalArtistIdsAsync` (`GetByNamesCollatedAsync`).
   `ArtistSuggestionService.GetRemoteAsync` has exactly **one** repository call
   (`GetByNamesCollatedAsync` at `ArtistSuggestionService.cs:79`, verified 2026-08-25) and therefore
   opens exactly **one** scope.
3. **One shared lambda for `ArtistSuggestionService.GetLocalAsync`'s two repository calls is a
   decision, not a question.** Recorded as stated rationale in `design.md § 4b`.
