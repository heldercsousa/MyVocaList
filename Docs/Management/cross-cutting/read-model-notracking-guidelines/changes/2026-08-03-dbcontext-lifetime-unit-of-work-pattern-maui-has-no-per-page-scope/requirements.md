# Requirements — DbContext lifetime & unit-of-work pattern

> Change folder for `cross-cutting/read-model-notracking-guidelines`. The parent spec established
> `NoTracking` as the read-model default; this change establishes the **write-side** lifetime and
> unit-of-work boundary that `NoTracking` alone does not provide.
>
> Status: **Candidate C is chosen** (`design.md § 8`); the API decisions are APPROVED by Helder
> (Revision 8, 2026-08-04, adds the failure-tuple save-skip mechanism resolving spec-review finding
> B3, `design.md § 6b`; Revision 9, 2026-08-04, makes the unrecognised-`TResult` fallback on the
> value-returning overload fail-closed — throw, not save unconditionally; Revision 10, 2026-08-04,
> drops the typed overload). **Three further Helder-approved decisions (2026-08-04) are recorded as
> `design.md § 8` D11/D12/D13:** D11 restructures `design.md § 10` into a pilot-first phase order
> (SongService/ArtistService/ArtistResolutionService/SongResolutionService first, gated VERIFY, then
> spread); D12 excludes all Queue and Event entity code from this spec's scope (tracked separately at
> `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`), which
> **corrects the in-scope method count from 35 to 21** (16 single-repository / 5 multi-repository,
> `design.md § 8` D12); D13 keeps the API shape provisional until the pilot's real call sites decide it
> in Phase 3. The acceptance criteria below reflect the approved, candidate-C-specific design as
> corrected by D11/D12/D13.

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
- **REQ-UOW-04** — **The currently-unguarded IN-SCOPE repositories are covered by the same guarantee.**
  The REQ-UOW-03 create→read→update sequence SHALL also hold for `ArtistRepository` and the two
  `BaseRepository<T>` descendants that are not Queue/Event entities (`PersonRepository`,
  `VenueRepository`). *(Corrected 2026-08-04, `design.md § 8` D12: this requirement previously also
  named `QueueRepository`, `EventParticipationRepository`, and "the surviving merged `EventRepository`"
  — all three are Queue/Event entities excluded from this spec's scope under D12 and are carried by
  `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/` instead.
  The `Infra/Repository/*` / `Infra/Repositories/*` family merge remains a hard prerequisite completing
  before Phase 0 — `design.md § 8` Prerequisite decision — independent of which repositories this
  requirement covers.)* *Test:* one parameterised integration test per in-scope repository family,
  each failing on pre-change code.
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
- **REQ-UOW-08** — **OUT OF SCOPE, deferred (corrected 2026-08-04, `design.md § 8` D12).**
  `QueueService.GetOrCreateDefaultEventAsync`, `Infra/Repository/EventRepository.SetActiveEventAsync`,
  and `QueueRepository.ReorderAsync` are all Queue/Event entity code, excluded from this spec's scope
  by Helder's decision (D12). This requirement is not implemented or tested by any phase of this
  spec — it is carried forward to
  `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`, which must
  restate it (with its own acceptance criteria) before implementation. Retained here, marked
  out-of-scope rather than deleted, so the original intent is not lost.
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
- **REQ-UOW-23** — **OUT OF SCOPE, deferred (corrected 2026-08-04, `design.md § 8` D12).**
  `QueueService.AddPersonToQueueAsync` is a `QueueService` method, excluded from this spec's scope by
  Helder's decision (D12) along with the rest of `QueueService`/`EventService`/`QueueServiceNew`. It
  will **not** be wrapped in `IUnitOfWork` by any phase of this spec, and the Given/When/Then scenarios
  below are **not** implemented as tests here.
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
  **Boundary-crossing note (`design.md § 8` D12 item 3):** the nested call itself
  (`QueueService.AddPersonToQueueAsync` → `PersonService.CreatePersonAsync`) is analysed in
  `design.md § 8` D12's boundary-crossing table and found **not** to reproduce BUG-068/BUG-071 once
  `PersonService.CreatePersonAsync` is wrapped in Phase 4+ — the wrapped side opens and disposes its
  own short-lived context regardless of whether the unwrapped caller ever joins it. This requirement
  is retained here, marked out-of-scope, so the original acceptance criteria are not lost — it is
  restated by the deferred item if `QueueService` itself is ever migrated.

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
  delegate is `Func<IServiceProvider, Task>`, not `Func<IServiceProvider, Task<TResult>>`), THEN
  `IUnitOfWork.ExecuteAsync(Func<IServiceProvider, Task>
  body, ct)` SHALL call `SaveChangesAsync` unconditionally whenever `body` completes without
  throwing. This is the documented fallback (`design.md § 6b` "no-signal fallback"), reachable only
  through this dedicated overload — the compiler selects it for any `Func<IServiceProvider, Task>`
  body, so no call site can reach it by accident. Under Revision 10 (`design.md § 8`) this is the
  ONLY no-signal form — there is no `TRepo`-typed variant. **Corrected 2026-08-04 (`design.md § 8`
  D12):** this requirement originally named three no-signal methods —
  `QueueService.RecordParticipationAsync`, `QueueService.SetActiveEventAsync`,
  `SongKaraokeUrlService.RecordPlayAsync`. The first two are `QueueService` methods, excluded from this
  spec's scope under D12 (see REQ-UOW-08/REQ-UOW-23) — `QueueService.RecordParticipationAsync`'s
  three-repository span is no longer relevant to this spec's Phase 4+ work. **The only in-scope
  no-signal method is `SongKaraokeUrlService.RecordPlayAsync`.** *Test:* an integration test running
  `SongKaraokeUrlService.RecordPlayAsync` (or an equivalent bare-`Task` in-scope mutating call) asserts
  the mutation is persisted on a normal return, and a second test asserts an exception thrown inside
  the body still leaves no partial state (REQ-UOW-06 applies identically to the no-signal overload).
- **REQ-UOW-27** (Revision 9, 2026-08-04 — resolves the fail-open/fail-closed refinement of finding
  B3) — WHEN a service method's body returns, via `IUnitOfWork.ExecuteAsync<TResult>` (the
  value-returning overload — the only value-returning form under Revision 10, `design.md § 8`), a
  `TResult` that is neither a `ValueTuple` with a leading `bool` element nor a type implementing
  `IUnitOfWorkOutcome`, THEN `ExecuteAsync` SHALL throw
  `InvalidOperationException` before any `SaveChangesAsync` is attempted, and the exception message
  SHALL name both valid fixes (implement `IUnitOfWorkOutcome`, or use the no-signal
  `ExecuteAsync(Func<IServiceProvider, Task>, ct)` overload per REQ-UOW-26). This SHALL NOT save the
  mutation under any circumstance — the prior "always saves" fallback (design.md Revision 8) is
  superseded for this branch.
  ```
  Given a bespoke named result type MyResult that implements neither a ValueTuple-with-leading-bool
      shape nor IUnitOfWorkOutcome
  And a body that mutates a tracked entity and then returns a MyResult instance
  When the body is run through IUnitOfWork.ExecuteAsync<MyResult>
  Then an InvalidOperationException is thrown, naming MyResult and the two valid fixes
  And re-reading the mutated row from the database shows no change (no save was attempted)
  ```
  *Test:* a unit/integration test calling `ExecuteAsync<TResult>` with a body that returns a
  bespoke named type implementing neither recognised shape asserts `InvalidOperationException` is
  thrown and that no row was written as a result of any mutation performed inside `body` before the
  return. A second test confirms `BackupService.CreateFullBackupAsync`'s wrap (Wave 5) does NOT throw
  once `BackupResult : IUnitOfWorkOutcome` is in place — the positive counterpart proving the fix
  closes the gap without breaking the one existing named-result case.

### The load-bearing rule: lambda bodies resolve from their own `IServiceProvider` (BL-2, third-pass spec review)

- **REQ-UOW-28** — Inside every `IUnitOfWork.ExecuteAsync`/`ExecuteReadAsync` lambda body, ALL
  repository (and data-writing service) access SHALL be resolved from the lambda's own
  `IServiceProvider` parameter. A lambda body SHALL NOT reference the enclosing service's
  constructor-injected repository/service fields (e.g. `_songRepository`) — doing so silently
  defeats the entire unit-of-work pattern, because the injected field resolves the **window-scope**
  `AppDbContext` (the same object BUG-068 was caused by), while `IUnitOfWork` saves a *different*,
  freshly-scoped `AppDbContext` that never saw the mutation. This is not detectable by the compiler or
  by a test that only checks the return value — it requires reviewing what the lambda body actually
  references. *Test:* a per-method code-review checklist item (`design.md § 8` "the load-bearing
  rule") for every Wave 5 diff, plus a static/grep check in the review checklist confirming no
  `Services/*.cs` `ExecuteAsync`/`ExecuteReadAsync` lambda body references a `_`-prefixed
  constructor-injected repository field.
  ```
  Given SongService wraps UpdateSongAsync's body in _uow.ExecuteAsync<(bool, string)>(async sp => { ... })
  When the lambda body is reviewed
  Then every repository call inside the lambda resolves its repository via
      sp.GetRequiredService<ISongRepository>() inside that same lambda
  And no reference to the service's constructor-injected _songRepository field appears
      anywhere inside the lambda body
  ```

### DRY & comprehensibility

- **REQ-UOW-10** — The unit-of-work boundary SHALL be expressed in **at most one line of code per
  service method** and **zero lines per repository method**. A design requiring an added
  `AppDbContext` parameter on repository methods, or two or more lines of ceremony per service method,
  fails this criterion. *Test:* reviewer-checked diff statistic recorded in the task-log.
- **REQ-UOW-11** — The six pass-through `SaveChangesAsync` implementations
  (`BaseRepository.cs:76-79`, `ArtistRepository.cs:157-158`, `CatalogRepository.cs:78-79`,
  `SongKaraokeUrlRepository.cs:67-68`, `SongRepository.cs:145-146`, `BackupRepository.cs:46-49`)
  SHALL be reduced to at most one save entry point (the `IUnitOfWork` boundary). *Test:* a source-level
  assertion (grep in the review checklist) that no in-scope repository implementation declares or
  calls `SaveChangesAsync`. **Corrected 2026-08-04 (`design.md § 8` D12 item 4):** this requirement
  previously also named "the six `SaveChangesAsync` calls embedded inside repository *mutator*
  methods" (`design.md § 2a` BL-1) as a second category in scope for this spec:
  `Infra/Repository/EventRepository.cs:37` `SetActiveEventAsync`,
  `Infra/Repositories/QueueRepository.cs:56` `AddAsync`, `:67` `UpdateAsync`, `:93` `ReorderAsync`,
  `Infra/Repositories/EventRepository.cs:66` `AddAsync`, `:77` `UpdateAsync`. A source grep
  (`design.md § 8` D12 item 4) confirms **all six** of these are in `EventRepository` (either family)
  or `QueueRepository` — i.e. entirely Queue/Event entity code, excluded from this spec's scope under
  D12. BL-1's claim ("all 6 embedded saves are in `EventRepository`/`QueueRepository`") is verified
  **TRUE** and is **not deleted**; it is restated as a LIVE RISK owned by
  `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/README.md`,
  which must remove these six saves when it wraps `EventService`/`QueueService`/`QueueServiceNew`.
  This spec's REQ-UOW-11 obligation is limited to the six pass-throughs above.
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

- **Inputs:** an `IServiceProvider`-consuming delegate — `Func<IServiceProvider, Task<TResult>>` for
  the value-returning form, `Func<IServiceProvider, Task>` for the no-signal form (`design.md § 8`
  Decision: drop the typed overload, Revision 10 — there is no repository-typed `TRepo` overload; the
  body resolves whatever it needs, one repository or several, from the supplied `IServiceProvider`);
  an optional `CancellationToken`.
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
  stopgap when this work deletes and replaces it.)* **Merge-ordering note (NB-4, third-pass spec
  review):** `feat/inline-artist-create` has NOT merged into `develop` as of this revision. If it
  merges before Wave 3b runs, Wave 3b deletes the stopgap as described above; if it has not merged,
  `develop`'s `SongRepository.UpdateAsync` does not contain the stopgap and Wave 3b is a no-op
  confirmation instead — REQ-UOW-18 is satisfied vacuously in that case. Wave 3b runs unconditionally
  either way; only whether it deletes anything changes. Record which case applied in the task-log
  (`design.md § 10`).

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

### Obsolete concurrency workaround removal (NB-1, third-pass spec review)

- **REQ-UOW-29** — The static `DbLoadGate` (`MyVocaList/UI/ViewModels/CrudListViewModelBase.cs:12-14`,
  a `SemaphoreSlim(1, 1)`) SHALL be deleted, along with every acquire/release call site. Its own
  comment states its reason for existing: "all CRUD list ViewModels share one effectively-singleton
  AppDbContext (MAUI has no per-page scope), so at most one DB load may run at a time app-wide." This
  change removes that root cause (Phase 4+, `design.md § 10`); leaving the gate in place afterward
  would silently reintroduce app-wide load serialization, directly undermining REQ-UOW-05's guarantee
  that concurrent units of work do not share a context and therefore do not need to queue behind each
  other. **The gate SHALL NOT be removed before every in-scope consumer is converted (§ 10 Phase 4+)**
  — removing it earlier would stop serializing loads for ViewModels not yet migrated, which still share
  the window-scope context. *Test:* a test asserting two concurrent list-load calls (e.g. two different
  CRUD list ViewModels loading simultaneously) complete without serializing through a shared semaphore,
  plus a source-level check that `DbLoadGate` no longer exists.

### Phase-order, scope-exclusion, and API-shape-deferral requirements (D11/D12/D13, `design.md § 8`)

- **REQ-UOW-30** (D11) — No task belonging to `design.md § 10` Phase 4+ SHALL be dispatched, claimed,
  or started until Phase 3's VERIFY gate — full automated test suite green, Helder's on-device
  confirmation that the pilot screens (Song/Artist CRUD) no longer reproduce BUG-068/BUG-071, and the
  D13 API-shape decision (REQ-UOW-32) — is recorded passed in the feature's task-log. *Test:* not a
  runtime test — a process check at code review: the task-log entry recording the first Phase 4+ task's
  dispatch must reference a prior Phase 3 entry recording all three Phase 3 outcomes; its absence is a
  process defect caught at review.
- **REQ-UOW-31** (D12) — No commit belonging to `design.md § 10` Phase 0 through Phase 4+ of this spec
  SHALL modify `Services/EventService.cs`, `Services/QueueService.cs`, `Services/QueueServiceNew.cs`,
  `Infra/Repository/EventRepository.cs`, `Infra/Repositories/EventRepository.cs`,
  `Infra/Repositories/QueueRepository.cs`, or the `EventParticipationRepository`
  interface/implementation. *Test:* a source-level check (grep in the review checklist, mirroring
  REQ-UOW-11's pattern) confirming none of these files appear in any Phase 0–4+ commit's diff; their
  existing `SaveChangesAsync` call sites (pass-through and embedded) remain present and unchanged until
  the deferred item (`changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`)
  picks them up.
- **REQ-UOW-32** (D13) — Phase 3's VERIFY step SHALL include an explicit decision record — "keep
  provisional" or "replace" — for the `IUnitOfWork` API shape (`design.md § 6`/§ 8 Revision 10), made
  from the 4 pilot services' actual call sites, not the § 6 audit. IF the decision is "replace", THEN
  every Phase 4+ task SHALL be written against the replacement shape, not the provisional
  `Func<IServiceProvider, …>`-only shape. *Test:* the Phase 3 task-log entry names the decision
  (keep/replace) with a one-line rationale; if "replace", the first Phase 4+ commit's diff uses the
  replacement shape.

## Validation rules

- No service method may call more than one `SaveChangesAsync` per unit of work **unless** the flow is
  listed in REQ-UOW-09 (REQ-UOW-08 is out of scope, deferred — `design.md § 8` D12) and the reason is
  documented inline.
- No repository may hold a `DbContext` beyond the unit of work that produced it.
- No singleton or transient UI type may capture a repository or a data-writing service that owns a
  `DbContext` (captive dependency) — `AppDbContextFactory(..., ServiceLifetime.Scoped)` registers
  `AppDbContext` itself as scoped (§ 1 "Reviewer-finding correction"), so a **transient** ViewModel
  resolving a repository is exactly as captive as a singleton would be; the ViewModel's own DI
  lifetime does not protect it (`design.md § 8`, BL-1 widening). `AppShellViewModel` and `AppShell`
  (`MauiProgram.cs:109-110`, singletons) and the three transient ViewModels named in Phase 4+
  (`QueueSongPickerViewModel`, `QueueManagementViewModel`, `PersonPickerViewModel` — all IN SCOPE
  despite their names, `design.md § 8` D12 item 6) are the known offenders to audit and convert.
- No `ExecuteAsync`/`ExecuteReadAsync` lambda body may reference the enclosing service's
  constructor-injected repository/service fields — every repository the lambda uses SHALL be resolved
  from the lambda's own `IServiceProvider` (REQ-UOW-28, BL-2).
- No unit of work may commit a mutation whose body returned a failure signal (REQ-UOW-24). The
  dedicated no-signal `ExecuteAsync(Func<IServiceProvider, Task>, ct)` overload always commits when its
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
- **Queue and Event entity code (added 2026-08-04, `design.md § 8` D12, Helder verbatim: "forget
  about any code related to Queue and Event entities. They're candidates to be completely refactored
  later, when we will already have the new approach already established in the guides.")** —
  `EventService`, `QueueService`, `QueueServiceNew`, `EventRepository` (both families),
  `QueueRepository`, and `EventParticipationRepository` are excluded from every phase of this spec.
  Tracked separately at
  `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`. This
  corrects the in-scope service-method count from 35 to 21 (see REQ-UOW-04/08/11/23/26 corrections
  above and `design.md § 8` D12 items 1–2). `PersonPickerViewModel`, `QueueSongPickerViewModel`, and
  `QueueManagementViewModel` are **not** excluded by this bullet despite their names — none injects a
  Queue/Event repository (`design.md § 8` D12 item 6).

## Failure-mode analysis

| Failure | Required behavior |
|---------|-------------------|
| Save fails mid unit of work (EF throws) | Unit of work is disposed; no partial state survives into the next unit of work (REQ-UOW-06). Service returns `(false, message)` per `code-style-reference.md § Service Return Patterns` — no exception escapes the service boundary. |
| Body mutates an entity, then returns a failure tuple/outcome (no exception thrown) | **(Revision 8, REQ-UOW-24)** `ExecuteAsync` detects the failure signal and skips `SaveChangesAsync` — the mutation is never persisted. This is the non-exception failure path, distinct from the row above, and is exactly as mandatory: "no partial state survives" holds for both. |
| Body mutates an entity, then returns a success tuple/outcome | **(Revision 8, REQ-UOW-25)** `ExecuteAsync` detects the success signal and saves exactly once — the mutation is persisted. |
| Body has no success/failure signal at all (bare `Task`, no `TResult`) | **(Revision 8, REQ-UOW-26)** The no-signal `ExecuteAsync(Func<IServiceProvider, Task>, ct)` overload always saves when `body` completes without throwing — this is the explicit, documented default, not an unrecognised/ambiguous case; an exception thrown inside `body` still leaves no partial state (REQ-UOW-06 applies identically). |
| `TResult` on the value-returning overload matches neither the `ValueTuple` shape nor `IUnitOfWorkOutcome` | **(Revision 9, REQ-UOW-27)** `ExecuteAsync` throws `InvalidOperationException` before any save is attempted, naming the two valid fixes (implement `IUnitOfWorkOutcome`, or switch to the no-signal overload). Supersedes Revision 8's "always saves" fallback for this branch — that reasoning applied exception-path logic ("no throw ⇒ success") to this codebase's value-return failure idiom, the same category error behind BUG-068's sibling defect. |
| Two units of work run concurrently on the same row | Last-write-wins as today; no shared-context corruption (REQ-UOW-05). Optimistic concurrency is out of scope. |
| A ViewModel is disposed mid-operation | The unit of work is owned by the service call, not the ViewModel — it completes or faults independently of the UI. |
| Migration path (`App.xaml.cs:54`) | The two existing manual scopes (`App.xaml.cs:35`, `:54`) are already correct usage and must continue to work under the new registration. |
