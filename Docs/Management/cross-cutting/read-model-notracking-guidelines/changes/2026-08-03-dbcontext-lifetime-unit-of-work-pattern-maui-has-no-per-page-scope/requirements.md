# Requirements — DbContext lifetime & unit-of-work pattern

> Change folder for `cross-cutting/read-model-notracking-guidelines`. The parent spec established
> `NoTracking` as the read-model default; this change establishes the **write-side** lifetime and
> unit-of-work boundary that `NoTracking` alone does not provide.
>
> Status: **design proposal awaiting Helder's decision.** Three candidates are compared in
> `design.md`; the acceptance criteria below are candidate-independent and hold for whichever
> candidate Helder selects.

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
  REQ-UOW-03 create→read→update sequence SHALL also hold for `ArtistRepository`,
  `Infra/Repositories/EventRepository`, `Infra/Repositories/QueueRepository`, and every
  `BaseRepository<T>` descendant (`PersonRepository`, `VenueRepository`,
  `EventParticipationRepository`, `Infra/Repository/EventRepository`). *Test:* one parameterised
  integration test per repository family, each failing on pre-change code.
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
  ```
- **REQ-UOW-23** — `QueueService.AddPersonToQueueAsync` SHALL produce the same observable outcome as
  today across its nested call to `_personService.CreatePersonAsync` (`design.md § 6a`) **without**
  the nested call opening a second `AppDbContext`.
  ```
  Given a queue add-request for a person who does not yet exist
  When AddPersonToQueueAsync is called
  Then exactly one Person row exists and is queued as expected
  And no InvalidOperationException mentioning "already being tracked" is thrown
  ```

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
  `IDbContextFactory<T>`, `IServiceScopeFactory`, `AddPooledDbContextFactory`, `ExecuteUpdateAsync` /
  `ExecuteDeleteAsync`, `CreateExecutionStrategy`, interceptors, `IServiceCollection` extension
  composition) in preference to hand-rolled infrastructure. Any hand-written type introduced must be
  justified in `design.md` under Key Decisions.

#### Inputs / Outputs / Preconditions — `IUnitOfWork` primitive (REQ-UOW-13 scope)

- **Inputs:** a repository type parameter `TRepo` (typed API) or an `IServiceProvider`-consuming
  delegate (escape-hatch API, `design.md § 8` Decision: typed overload preferred); a
  `Func<..., Task<TResult>>` body; an optional `CancellationToken`.
- **Outputs:** `TResult` on success; on business failure, whatever tuple shape `body` returns per
  `code-style-reference.md § Service Return Patterns`; an exception from `body` propagates after the
  scope disposes (REQ-UOW-06).
- **Preconditions:** the resolved repository/service type must be registered in DI (unchanged
  `AddScoped` registrations, `design.md § 2a` "Reviewer-finding correction"); the caller must not
  retain the resolved instance or its underlying `AppDbContext` past the body's return.
- Full API surface (typed `ExecuteAsync`/`ExecuteReadAsync` + escape-hatch overloads,
  implicit-save semantics, ambient-scope join): `design.md § 6`.

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
  explicitly acknowledged **STOPGAP**. This work SHALL delete it. **Hard constraint:** the deletion
  MUST NOT land before Helder's on-device T10 re-run #6 completes — it survives exactly until then,
  sequenced as its own gated sub-wave (`design.md § 10`, Wave 3b), not bundled into the rest of the
  `UnitOfWork` implementation work.

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
| Save fails mid unit of work | Unit of work is disposed; no partial state survives into the next unit of work (REQ-UOW-06). Service returns `(false, message)` per `code-style-reference.md § Service Return Patterns` — no exception escapes the service boundary. |
| Two units of work run concurrently on the same row | Last-write-wins as today; no shared-context corruption (REQ-UOW-05). Optimistic concurrency is out of scope. |
| A ViewModel is disposed mid-operation | The unit of work is owned by the service call, not the ViewModel — it completes or faults independently of the UI. |
| Migration path (`App.xaml.cs:54`) | The two existing manual scopes (`App.xaml.cs:35`, `:54`) are already correct usage and must continue to work under the new registration. |
