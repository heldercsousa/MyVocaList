# Requirements — DbContext lifetime & unit-of-work pattern

> Change folder for `cross-cutting/read-model-notracking-guidelines`. The parent spec established
> `NoTracking` as the read-model default; this change establishes the **write-side** lifetime and
> unit-of-work boundary that `NoTracking` alone does not provide.
>
> Status: **Candidate C is chosen** (`design.md § 8`); the API decisions are APPROVED by Helder
> (Revision 8, 2026-08-04, adds the failure-tuple save-skip mechanism resolving spec-review finding
> B3, `design.md § 6b`; Revision 9, 2026-08-04, makes the unrecognised-`TResult` fallback on the
> value-returning overload fail-closed — throw, not save unconditionally). The acceptance criteria
> below reflect the approved, candidate-C-specific design.

---

## Problem statement

`MauiProgram.cs:61-68` registers `AppDbContext` with `AddDbContext<AppDbContext>(...)`, whose default
lifetime is Scoped. .NET MAUI creates a DI scope **per Window**, not per page or per navigation
(`/dotnet/maui@10.0.51 docs/design/Scoping.md`). A single-window Android/iOS app therefore has exactly
one scope for the whole session, so the root provider hands out **one `AppDbContext` for the entire
app session**.

`.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` suppresses tracking of *query results*
only. Entities left tracked by a prior `Add`/`Update` + `SaveChangesAsync` stay in the change tracker
for the rest of the session. A later read of the same row followed by `DbSet.Update` throws
`InvalidOperationException: another instance with the same key value {'Id'} is already being tracked`.
That is **BUG-068 (Critical)**.

EF Core documents two further consequences of an app-lifetime context
(`/dotnet/entityframework.docs`, *DbContext Lifetime, Configuration, and Initialization*):

- `DbContext` **is not thread-safe**; instances must never be accessed concurrently. The documented
  remedies are (a) register scoped and create a scope per unit of work with `IServiceScopeFactory`,
  or (b) register transient.
- An `InvalidOperationException` thrown by EF Core **can put the context into an unrecoverable
  state** — such exceptions signal a program error and are not designed to be recovered from. In this
  app that means a single BUG-068 throw can poison the shared context for the remainder of the
  session.

## Domain vocabulary

| Term | Definition |
|------|------------|
| **Unit of work** | A bounded span of code within which exactly one `AppDbContext` instance exists, ending in at most one `SaveChangesAsync`, after which the context is disposed. |
| **Unit-of-work boundary** | The single syntactic construct in the code that opens and closes a unit of work. It must be visible at the call site, not ambient. |
| **Captive dependency** | A shorter-lived service (scoped/transient) held by a longer-lived one (singleton), which silently extends its lifetime. |
| **Ambient context** | A `DbContext` reachable by a component without that component or its caller declaring a unit of work. The current state of the app. |
| **Pass-through save** | A repository method whose whole body is `_db.SaveChangesAsync(ct)`. Six exist today. |
| **Stopgap guard** | The `ChangeTracker.Entries<Song>()` detach loop added to `SongRepository.UpdateAsync` in commit `1a114c1` on `feat/inline-artist-create`. |

## User stories

- **US-1 — As Helder (Architect),** I want one documented unit-of-work pattern used by every service,
  so that a data-write path cannot accidentally reuse a session-lifetime `DbContext`.
- **US-2 — As a new developer,** I want to see where a unit of work begins and ends by reading the
  service method, so that I do not have to know a framework convention to reason about persistence.
- **US-3 — As a new developer,** I want the pattern expressed once rather than repeated per method,
  so that the codebase does not grow boilerplate proportional to its method count.
- **US-4 — As Helder,** I want the five unguarded repositories carrying the same latent defect as
  BUG-068 fixed by the pattern itself, not by five separate hand-written guards.
- **US-5 — As a test author,** I want the existing tracking-conflict workarounds in integration tests
  deleted, so that tests stop encoding a bug as if it were a rule.

## Acceptance criteria

Format: EARS for invariants, Given/When/Then for behavioral scenarios.

### Lifetime & structural correctness

- **REQ-UOW-01** — The system SHALL NOT resolve `AppDbContext` from the root/window `IServiceProvider`
  at any point in application code. *Test:* a DI-composition test asserts that either no
  `ServiceDescriptor` for `AppDbContext` exists with `ServiceLifetime.Scoped` registered against the
  root provider, or that resolving it outside an explicitly created scope throws.
- **REQ-UOW-02** — Every `AppDbContext` instance SHALL be disposed at the end of the unit of work
  that created it. *Test:* an integration test creating N units of work asserts N distinct
  `DbContext` instances (compared by reference) and that each is disposed on exit.
- **REQ-UOW-03** — **BUG-068 must be structurally impossible.**
  ```
  Given a Song has been created and saved through the application's normal write path
  When the same Song row is subsequently read and updated through the normal write path
  Then no InvalidOperationException mentioning "already being tracked" is thrown
  And the update is persisted
  ```
  *Test:* an integration regression test named for BUG-068 that fails on the pre-change code.
- **REQ-UOW-04** — **The currently-unguarded repositories are covered by the same guarantee.** The
  REQ-UOW-03 create→read→update sequence SHALL also hold for `ArtistRepository`, `QueueRepository`,
  every `BaseRepository<T>` descendant (`PersonRepository`, `VenueRepository`,
  `EventParticipationRepository`), and **the surviving merged `EventRepository`** — the
  `Infra/Repository/*` / `Infra/Repositories/*` family merge (`design.md § 8` Prerequisite decision)
  is a hard prerequisite completing before Wave 0, so only one `EventRepository` exists by the time
  this requirement is tested. *Test:* one parameterised integration test per repository family, each
  failing on pre-change code.
- **REQ-UOW-05** — WHEN two units of work run concurrently, the system SHALL NOT share a
  `DbContext` instance between them. *Test:* a test issuing two overlapping service calls asserts two
  distinct context instances and no `InvalidOperationException`.
- **REQ-UOW-06** — IF a unit of work throws, THEN the failure SHALL NOT affect any subsequent unit of
  work. *Test:* a test forces an EF failure inside one unit of work, then asserts the next write
  succeeds.

### Atomicity of existing multi-write flows (no behavioral regression)

- **REQ-UOW-07** — `SongService.CreateSongWithUrlsAsync` SHALL persist the `Song` and its
  `SongKaraokeUrl` rows in **one** `SaveChangesAsync`, preserving the existing "N3: one
  SaveChangesAsync commits both atomically" guarantee and the existing reliance on EF FK fixup via the
  navigation property. *Test:* existing tests plus an assertion that a forced failure on the URL rows
  leaves no `Song` row persisted.
- **REQ-UOW-08** — `QueueService.GetOrCreateDefaultEventAsync`,
  `Infra/Repository/EventRepository.SetActiveEventAsync`, and `QueueRepository.ReorderAsync` SHALL
  each complete within a single unit of work, with their existing all-or-nothing semantics unchanged.
  **`GetOrCreateDefaultEventAsync` is `private`** — reachable only via the public
  `QueueService.RecordParticipationAsync`. The unit-of-work wrap goes on `RecordParticipationAsync`;
  `GetOrCreateDefaultEventAsync` runs as a plain private helper call inside that same unit of work
  (`design.md § 10`).
- **REQ-UOW-09** — `ArtistResolutionService.CommitAsync` SHALL produce the same observable outcome as
  today (an artist exists with the external identity attached) **without** a save→mutate→save sequence
  spanning two contexts. Whether this becomes one save or remains two saves inside one unit of work is
  a design choice; the observable outcome and the returned `artistId` must not change.
  ```
  Given an artist candidate with an external provider and id and choice = CreateNew
  When CommitAsync is called
  Then exactly one Artist row exists with that name, ExternalProvider and ExternalId set
  And the returned artistId matches that row
  ```

- **REQ-UOW-22** — `SongResolutionService.CommitAsync` SHALL produce the same observable outcome as
  today across its three nested cross-service calls (`_songService.CreateSongAsync` /
  `CreateSongWithUrlsAsync` / `UpdateSongAsync`, and the further-nested
  `_artistResolution.CommitAsync` → `_artistService.CreateArtistAsync` chain, `design.md § 6a`)
  **without** any nested call opening a second `AppDbContext`. This is the 3-level compound case found
  in the full call-site audit and is the deepest atomicity requirement in this spec.
  ```
  Given a song candidate whose artist does not yet exist and whose song does not yet exist
  When SongResolutionService.CommitAsync is called
  Then exactly one Song row and exactly one Artist row exist as expected
  And no InvalidOperationException mentioning "already being tracked" is thrown
  And all writes across the nested chain persist atomically (all present, or none)

  Given the same song candidate as above
  And a fault is injected so that the innermost repository call
      (ArtistService.CreateArtistAsync -> IArtistRepository.AddAsync/SaveChangesAsync) throws after
      the outer SongService write has already executed but before the unit of work completes
      (design.md § 6a 3-level chain)
  When SongResolutionService.CommitAsync is called
  Then no Song row and no Artist row exist afterward (all-or-nothing)
  And the thrown exception propagates to the caller per REQ-UOW-06
  ```
- **REQ-UOW-23** — `QueueService.AddPersonToQueueAsync` SHALL produce the same observable outcome as
  today across its nested call to `_personService.CreatePersonAsync` (`design.md § 6a`) **without**
  the nested call opening a second `AppDbContext`.
  ```
  Given a queue add-request for a person who does not yet exist
  When AddPersonToQueueAsync is called
  Then exactly one Person row exists and is queued as expected
  And no InvalidOperationException mentioning "already being tracked" is thrown

  Given the same add-request as above
  And a fault is injected so that the nested PersonService.CreatePersonAsync repository write
      (IPersonRepository.AddAsync/SaveChangesAsync) throws before the unit of work completes
  When AddPersonToQueueAsync is called
  Then no Person row exists afterward (all-or-nothing)
  And the thrown exception propagates to the caller per REQ-UOW-06
  ```

### Save-skip on failure-tuple results (Revision 8 — resolves spec-review finding B3)

- **REQ-UOW-24** — WHEN a service method's body mutates an entity and then returns a `ValueTuple`
  whose first element is `bool` set to `false` (or a named result type implementing
  `IUnitOfWorkOutcome` with `Success == false`), THEN `IUnitOfWork.ExecuteAsync` SHALL NOT call
  `SaveChangesAsync` — the mutation SHALL NOT be persisted.
  ```
  Given SongService.UpdateSongAsync loads a Song, sets song.Title to a new value,
      calls repo.UpdateAsync(song, ct), and then returns (false, "A song with this title
      already exists for this artist") because a later validation check fails
  When UpdateSongAsync is called through IUnitOfWork.ExecuteAsync
  Then the returned tuple is (false, "A song with this title already exists for this artist")
  And re-reading the Song row from the database shows the original Title, not the mutated one
  ```
  *Test:* an integration test that runs a body performing a real repository mutation followed by a
  `(false, …)` return, then asserts via a fresh read that the row is unchanged. Cover at least one
  `ValueTuple`-shaped method (e.g. `SongService.UpdateSongAsync`) and the `IUnitOfWorkOutcome` shape
  (`BackupService.CreateFullBackupAsync` returning a `BackupResult` with `Success == false`).
- **REQ-UOW-25** — WHEN a service method's body mutates an entity and returns a `ValueTuple` whose
  first element is `bool` set to `true` (or `IUnitOfWorkOutcome.Success == true`), THEN
  `IUnitOfWork.ExecuteAsync` SHALL call `SaveChangesAsync` exactly once and the mutation SHALL be
  persisted. *Test:* the positive counterpart of REQ-UOW-24 — same shapes, success path, asserts the
  mutated value is present on re-read.
- **REQ-UOW-26** — WHEN a service method's body has no success/failure signal to inspect (its
  delegate is `Func<TRepo, Task>`, not `Func<TRepo, Task<TResult>>` — e.g.
  `QueueService.RecordParticipationAsync`, `QueueService.SetActiveEventAsync`,
  `SongKaraokeUrlService.RecordPlayAsync`), THEN `IUnitOfWork.ExecuteAsync<TRepo>(Func<TRepo, Task>
  body, ct)` SHALL call `SaveChangesAsync` unconditionally whenever `body` completes without
  throwing. This is the documented fallback (`design.md § 6b` "no-signal fallback"), reachable only
  through this dedicated overload — the compiler selects it for any `Func<TRepo, Task>` body, so no
  call site can reach it by accident. *Test:* an integration test running
  `QueueService.RecordParticipationAsync` (or an equivalent bare-`Task` mutating call) asserts the
  mutation is persisted on a normal return, and a second test asserts an exception thrown inside the
  body still leaves no partial state (REQ-UOW-06 applies identically to the no-signal overload).
- **REQ-UOW-27** (Revision 9, 2026-08-04 — resolves the fail-open/fail-closed refinement of finding
  B3) — WHEN a service method's body returns, via `IUnitOfWork.ExecuteAsync<TRepo, TResult>` (the
  value-returning overload), a `TResult` that is neither a `ValueTuple` with a leading `bool` element
  nor a type implementing `IUnitOfWorkOutcome`, THEN `ExecuteAsync` SHALL throw
  `InvalidOperationException` before any `SaveChangesAsync` is attempted, and the exception message
  SHALL name both valid fixes (implement `IUnitOfWorkOutcome`, or use the no-signal
  `ExecuteAsync<TRepo>(Func<TRepo, Task>, ct)` overload per REQ-UOW-26). This SHALL NOT save the
  mutation under any circumstance — the prior "always saves" fallback (design.md Revision 8) is
  superseded for this branch.
  ```
  Given a bespoke named result type MyResult that implements neither a ValueTuple-with-leading-bool
      shape nor IUnitOfWorkOutcome
  And a body that mutates a tracked entity and then returns a MyResult instance
  When the body is run through IUnitOfWork.ExecuteAsync<TRepo, MyResult>
  Then an InvalidOperationException is thrown, naming MyResult and the two valid fixes
  And re-reading the mutated row from the database shows no change (no save was attempted)
  ```
  *Test:* a unit/integration test calling `ExecuteAsync<TRepo, TResult>` with a body that returns a
  bespoke named type implementing neither recognised shape asserts `InvalidOperationException` is
  thrown and that no row was written as a result of any mutation performed inside `body` before the
  return. A second test confirms `BackupService.CreateFullBackupAsync`'s wrap (Wave 5) does NOT throw
  once `BackupResult : IUnitOfWorkOutcome` is in place — the positive counterpart proving the fix
  closes the gap without breaking the one existing named-result case.

### DRY & comprehensibility

- **REQ-UOW-10** — The unit-of-work boundary SHALL be expressed in **at most one line of code per
  service method** and **zero lines per repository method**. A design requiring an added
  `AppDbContext` parameter on repository methods, or two or more lines of ceremony per service method,
  fails this criterion. *Test:* reviewer-checked diff statistic recorded in the task-log.
- **REQ-UOW-11** — The six pass-through `SaveChangesAsync` implementations
  (`BaseRepository.cs:76-79`, `ArtistRepository.cs:157-158`, `CatalogRepository.cs:78-79`,
  `SongKaraokeUrlRepository.cs:67-68`, `SongRepository.cs:145-146`, `BackupRepository.cs:46-49`)
  SHALL be reduced to at most one save entry point. *Test:* a source-level assertion (grep in the
  review checklist) that no repository implementation declares `SaveChangesAsync`.
- **REQ-UOW-12** — WHEN a new developer reads any service method that writes data, the method SHALL
  name its unit of work explicitly — no write may occur through an implicitly-obtained context.
- **REQ-UOW-13** — The design SHALL use .NET/EF Core built-ins (`AddDbContextFactory` /
  `IDbContextFactory<T>`, `IServiceScopeFactory`, `ExecuteUpdateAsync` / `ExecuteDeleteAsync`,
  `CreateExecutionStrategy`, interceptors, `IServiceCollection` extension composition) in preference to
  hand-rolled infrastructure. `AddPooledDbContextFactory` is explicitly rejected (`design.md § 8`
  Decision: reject `AddPooledDbContextFactory`) — pooling amortises construction cost under
  server-grade request rates, which does not apply to a single-user mobile app, and it inherits a
  reset-semantics footgun for no measured gain. Any hand-written type introduced must be justified in
  `design.md` under Key Decisions.

#### Inputs / Outputs / Preconditions — `IUnitOfWork` primitive (REQ-UOW-13 scope)

- **Inputs:** a repository type parameter `TRepo` (typed API) or an `IServiceProvider`-consuming
  delegate (escape-hatch API, `design.md § 8` Decision: typed overload preferred); a
  `Func<..., Task<TResult>>` body; an optional `CancellationToken`.
- **Outputs:** `TResult` on success; on business failure, whatever tuple shape `body` returns per
  `code-style-reference.md § Service Return Patterns`; an exception from `body` propagates after the
  scope disposes (REQ-UOW-06). **Save-skip (Revision 8, `design.md § 6b`):** `SaveChangesAsync` runs
  only when the returned `TResult` signals success — a `ValueTuple` with a leading `bool` that is
  `false`, or an `IUnitOfWorkOutcome.Success` of `false`, skips the save entirely; a `TResult` with no
  recognised signal (reachable only via the dedicated no-signal overload) always saves.
- **Preconditions:** the resolved repository/service type must be registered in DI (unchanged
  `AddScoped` registrations, `design.md § 2a` "Reviewer-finding correction"); the caller must not
  retain the resolved instance or its underlying `AppDbContext` past the body's return.
- Full API surface (typed `ExecuteAsync`/`ExecuteReadAsync` + escape-hatch overloads + no-signal
  overloads, save-skip semantics, ambient-scope join): `design.md § 6`, `design.md § 6b`.

### Interceptors & existing infrastructure

- **REQ-UOW-14** — `CollationInterceptor` and `TransactionLogInterceptor` SHALL remain registered on
  every `AppDbContext` instance produced by the new pattern, resolved from the provider exactly as
  today. *Test:* an integration test asserts a collated query still works and a transaction-log entry
  is still written for a save.
- **REQ-UOW-15** — `TransactionLogInterceptor` reads `ChangeTracker.Entries()` during
  `SavingChanges`. WHEN a unit of work saves, the interceptor SHALL observe **only** the entities
  mutated in that unit of work. *Test:* two sequential writes; the second log entry must not repeat
  the first's entities. (On the current code this criterion is already silently violated.)

### Testability

- **REQ-UOW-16** — `MyVocaList.Tests/Infrastructure/TestDbContextFactory.cs` SHALL expose the same
  unit-of-work primitive the application uses, over a real SQLite temp file with
  `CollationInterceptor` — never the in-memory provider (`testing.md § Project anti-patterns`).
- **REQ-UOW-17** — The tracking-conflict workarounds at `CatalogRepositoryTests.cs:66` ("Detach
  tracked entities so EF doesn't raise identity conflict") and `ArtistRepositoryTests.cs:366` SHALL be
  deleted, and the tests SHALL still pass. Their presence after this change is a defect.

### Stopgap removal

- **REQ-UOW-18** — The hand-rolled `ChangeTracker.Entries<Song>()` detach guard added to
  `SongRepository.UpdateAsync` in commit `1a114c1` (branch `feat/inline-artist-create`) is an
  explicitly acknowledged **STOPGAP**. This work SHALL delete it, as part of Wave 3b
  (`design.md § 10`), an ordinary step of the repository work with no external gate. *(withdrawn
  2026-08-04: the sub-clause gating this deletion on Helder's on-device T10 re-run #6 is cancelled —
  see `design.md § 8` decision "cancel the T10 re-run #6 gate". There is no reason to device-test the
  stopgap when this work deletes and replaces it.)*

### Guideline amendments (documentation deliverables)

- **REQ-UOW-19** — `code-style-reference.md § DI Registration Conventions` currently reads
  "`AddScoped` — Repositories, Services, IDatabaseInit (per-lifetime scope)". This is an ASP.NET-shaped
  rule that is **wrong for MAUI** and is the direct cause of all 25 `AddScoped` registrations. It
  SHALL be replaced with a MAUI-correct rule stating that MAUI scopes are per-Window (effectively
  app-lifetime on mobile) and naming the chosen unit-of-work pattern. The change follows
  `CLAUDE.md § Amending These Rules` (`amend:` prefix + changelog entry).
- **REQ-UOW-20** — The stale comment on `SongRepository.GetByIdAsync` describing a "Tracked query"
  SHALL be corrected or removed — it contradicts the global `NoTracking` default.
- **REQ-UOW-21** — The duplicate `IAppInfo` registration (`MauiProgram.cs:86` and `:157`) SHALL be
  removed, leaving one.

## Validation rules

- No service method may call more than one `SaveChangesAsync` per unit of work **unless** the flow is
  listed in REQ-UOW-08/09 and the reason is documented inline.
- No repository may hold a `DbContext` beyond the unit of work that produced it.
- No singleton may capture a repository or service that owns a `DbContext` (captive dependency).
  `AppShellViewModel` and `AppShell` (`MauiProgram.cs:109-110`) are the known singletons to audit.
- No unit of work may commit a mutation whose body returned a failure signal (REQ-UOW-24). The
  dedicated no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task>, ct)` overload always commits when its
  body completes without throwing (REQ-UOW-26) — this is the one exception to "commit only on
  success," documented, not silent, and reachable only through that overload. On the
  value-returning overload, a `TResult` the unit of work cannot interpret is never committed either —
  it throws instead (REQ-UOW-27, fail-closed).

## Out of scope

- Changing SQLite/EF Core provider, schema, or migrations.
- The Blazor Hybrid + MudBlazor UI migration.
- Introducing MediatR or FluentValidation.
- Performing the repository-family merge itself. Merging `Infra/Repository/*` and
  `Infra/Repositories/*` into one family is a **hard prerequisite** that Helder has decided must
  complete before Wave 0 of this change (`design.md § 8` Prerequisite decision) — it is out of
  scope for *this* spec's waves in the sense that this spec does not perform it, not in the sense
  that it is optional or deferred indefinitely.
- Retry/`CreateExecutionStrategy` policies for transient faults — SQLite is local; evaluated in
  `design.md` and deliberately not required.
- Multi-window support.

## Failure-mode analysis

| Failure | Required behavior |
|---------|-------------------|
| Save fails mid unit of work (EF throws) | Unit of work is disposed; no partial state survives into the next unit of work (REQ-UOW-06). Service returns `(false, message)` per `code-style-reference.md § Service Return Patterns` — no exception escapes the service boundary. |
| Body mutates an entity, then returns a failure tuple/outcome (no exception thrown) | **(Revision 8, REQ-UOW-24)** `ExecuteAsync` detects the failure signal and skips `SaveChangesAsync` — the mutation is never persisted. This is the non-exception failure path, distinct from the row above, and is exactly as mandatory: "no partial state survives" holds for both. |
| Body mutates an entity, then returns a success tuple/outcome | **(Revision 8, REQ-UOW-25)** `ExecuteAsync` detects the success signal and saves exactly once — the mutation is persisted. |
| Body has no success/failure signal at all (bare `Task`, no `TResult`) | **(Revision 8, REQ-UOW-26)** The no-signal `ExecuteAsync<TRepo>(Func<TRepo, Task>, ct)` overload always saves when `body` completes without throwing — this is the explicit, documented default, not an unrecognised/ambiguous case; an exception thrown inside `body` still leaves no partial state (REQ-UOW-06 applies identically). |
| `TResult` on the value-returning overload matches neither the `ValueTuple` shape nor `IUnitOfWorkOutcome` | **(Revision 9, REQ-UOW-27)** `ExecuteAsync` throws `InvalidOperationException` before any save is attempted, naming the two valid fixes (implement `IUnitOfWorkOutcome`, or switch to the no-signal overload). Supersedes Revision 8's "always saves" fallback for this branch — that reasoning applied exception-path logic ("no throw ⇒ success") to this codebase's value-return failure idiom, the same category error behind BUG-068's sibling defect. |
| Two units of work run concurrently on the same row | Last-write-wins as today; no shared-context corruption (REQ-UOW-05). Optimistic concurrency is out of scope. |
| A ViewModel is disposed mid-operation | The unit of work is owned by the service call, not the ViewModel — it completes or faults independently of the UI. |
| Migration path (`App.xaml.cs:54`) | The two existing manual scopes (`App.xaml.cs:35`, `:54`) are already correct usage and must continue to work under the new registration. |
