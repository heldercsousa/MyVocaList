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
> corrected by D11/D12/D13. **REQ-UOW-33 revised 2026-08-04:** `IUnitOfWork.ExecuteAsync` opens an
> explicit transaction, replacing the earlier "`ExecuteUpdateAsync`/`ExecuteDeleteAsync` are exempt
> from atomicity" carve-out (`design.md § 8` "Decision: `ExecuteAsync` opens an explicit transaction").
>
> **This spec is APPROVED by Helder as of 2026-08-04.** Every decision previously marked "REQUIRES
> HELDER'S CONFIRMATION" in this file and in `design.md` is now marked **APPROVED by Helder 2026-08-04**
> at its point of occurrence. This includes the `IBaseRepository<T>.SaveChangesAsync()` deferral
> decision (narrowing REQ-UOW-11), the `QueueManagementViewModel` Phase 4+ scope correction (§ 8 D12
> item 6), and the Revision 12 ambient-scope change (REQ-UOW-34). REQ-UOW-11's substance is unchanged
> by this approval pass — a separate question about it remains open with Helder; only its approval
> marker was updated.

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

- **REQ-UOW-01** — **Corrected 2026-08-04 (4th-pass spec review, BL-F).** The original wording ("SHALL
  NOT resolve `AppDbContext` from the root/window `IServiceProvider` at any point," tested by "either no
  `ServiceDescriptor` ... registered against the root provider, or resolving it outside a scope throws")
  is **false under the approved Candidate C design and untestable as written**: `AddDbContextFactory<AppDbContext>(…,
  ServiceLifetime.Scoped)` registers `AppDbContext` itself as an ordinary scoped `ServiceDescriptor`
  by design (§ 1 "Reviewer-finding correction" — repositories keep constructor-injecting it), so the
  first disjunct is false by construction; and MAUI does not enable `ValidateScopes` by default (§ 1,
  framework fact table), so resolving `AppDbContext` from the root/window provider does **not** throw —
  the second disjunct is also false. Both disjuncts of the original AC are false under the chosen
  design, making it neither true nor testable. Replaced with the structural guarantee Candidate C
  actually provides: **all scope creation for a unit of work SHALL go through `IUnitOfWork`; no other
  production code path SHALL call `IServiceScopeFactory.CreateScope()`/`CreateAsyncScope()` directly.**
  *Test:* (a) a DI-composition test asserts `IDbContextFactory<AppDbContext>` and `IUnitOfWork` are both
  registered exactly once; (b) a source-level grep check (review checklist, mirroring REQ-UOW-11's
  pattern) confirms no file under `Services/`, `MyVocaList/UI/ViewModels/`, or `Infra/` other than
  `UnitOfWork`'s own implementation calls `CreateScope()`/`CreateAsyncScope()` on an
  `IServiceScopeFactory`. This does not claim `AppDbContext` is unreachable from the root provider —
  it claims the *unit-of-work boundary* (not raw scope creation) is the single place a write's lifetime
  is managed, which is what REQ-UOW-12/REQ-UOW-28 actually depend on.
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
  The `Infra/Repository/*` / `Infra/Repositories/*` family merge is **no longer** a prerequisite
  completing before Phase 0 — **corrected 2026-08-04, superseded by Helder ("a, but not yet!")**: the
  merge now runs after Phase 3's VERIFY gate, combined with the deferred Queue/Event unit-of-work
  migration (`design.md § 8` superseded Prerequisite decision, `design.md § 10` Phase 3.5) —
  independent of which repositories this requirement covers.)* *Test:* one parameterised integration test per in-scope repository family,
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

  > **Spec updated [2026-08-18]:** the design choice is now made — **two saves inside one unit of
  > work** (REQ-UOW-09's explicitly sanctioned second branch), realised by the new flush affordance
  > **REQ-UOW-35**. Collapsing to a single implicit save is *not possible* for the CreateNew branch:
  > `CommitAsync` evaluates `return (true, message, created.Id)` **inside** the `ExecuteAsync` lambda,
  > i.e. before the deferred save runs, so a single save returns `artistId = 0` and breaks this very
  > requirement; and `ArtistRepository.UpdateAsync` forces state `Modified` on an entity still `Added`
  > with a temporary key (`InvalidOperationException: The property 'Artist.Id' has a temporary
  > value…`). The flush lives on `IUnitOfWork`, not on a repository, so REQ-UOW-11 still holds — no
  > repository regains a save entry point. (Helder's decision 2026-08-18; raised as a `blocked: spec
  > gap` by Task 2.3.)

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
  **Nested-call precedence (new, non-blocking #11, 4th-pass spec review):** WHEN a joined nested
  `ExecuteAsync` call (§ 6a ambient-scope join) returns a failure signal, THEN the failure SHALL
  propagate to the OUTER `ExecuteAsync`'s own `ResultSignalsSuccess` check via the outer body's own
  return value — the join branch (`if (_ambientScope.Value is { } joined) return await body(joined);`,
  `design.md § 6`) returns the inner result directly to its caller without independently inspecting it,
  so save-skip is decided exactly once, by the OUTERMOST `ExecuteAsync` in the chain, based on
  whatever tuple/outcome the outermost body ultimately returns. This is already the code's actual
  behavior (the join branch does no signal inspection of its own) — this clause makes it an explicit,
  tested requirement rather than an implicit consequence of the join branch's code shape. *Test:*
  `SongResolutionService.CommitAsync`'s 3-level chain (REQ-UOW-22) already covers this: assert that when
  the innermost `ArtistService.CreateArtistAsync` call fails, the failure propagates all the way to
  `SongResolutionService.CommitAsync`'s own returned tuple, and the outermost `ExecuteAsync` (opened by
  whichever caller is outermost) skips its save based on that final tuple — not on any intermediate
  nested call's result.
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
  no-signal method is `SongKaraokeUrlService.RecordPlayAsync`.**
  **Further correction (4th-pass spec review, BL-C, verified against source; superseded 2026-08-04 —
  see REQ-UOW-33 below):**
  `SongKaraokeUrlService.RecordPlayAsync` (`SongKaraokeUrlService.cs:79-83`) calls
  `_repo.IncrementPlayCountAsync`, whose implementation
  (`Infra/Repository/SongKaraokeUrlRepository.cs:56-64`) is `db.SongKaraokeUrls.Where(...).ExecuteUpdateAsync(...)`
  — an immediate-SQL EF Core bulk operation that runs **outside the change tracker** and, absent an
  explicit transaction, would commit the instant it is awaited, independent of any later
  `SaveChangesAsync` call. **This AC's exemplar is corrected:** `SongKaraokeUrlService.RecordPlayAsync`
  is retained as the only in-scope no-signal method for API-shape purposes (it is the only bare-`Task`
  in-scope mutator). Its "no partial state survives" guarantee is **no longer carved out** — REQ-UOW-33
  now wraps it (and the other four `ExecuteUpdateAsync`/`ExecuteDeleteAsync` methods) in
  `IUnitOfWork.ExecuteAsync`'s explicit transaction (`design.md § 6`/§ 8), so an exception thrown after
  `IncrementPlayCountAsync` returns but before the transaction commits rolls the increment back too.
  *Test:* an integration test running `SongKaraokeUrlService.RecordPlayAsync` asserts the mutation is
  persisted on a normal return, AND a second test (REQ-UOW-33) asserts that a fault injected after
  `IncrementPlayCountAsync` runs but before the unit of work completes leaves the play count
  unchanged — the exception-rollback test this AC's original text called for, now satisfiable.

- **REQ-UOW-33** (revised 2026-08-04, Helder's decision — supersedes the 4th-pass "carve-out" wording;
  design record: `design.md § 8` "Decision: `ExecuteAsync` opens an explicit transaction") — **The
  unit of work is transactional, including for `ExecuteUpdateAsync`/`ExecuteDeleteAsync`-based
  methods.** `IUnitOfWork.ExecuteAsync`'s two overloads open an explicit
  `Database.BeginTransactionAsync` immediately after creating the scope (`design.md § 6`). Of the 21
  in-scope methods, the 16 that mutate only tracked entities were already atomic via EF Core's
  automatic per-`SaveChangesAsync` transaction (the unit of work has exactly one `SaveChangesAsync`
  call); the explicit transaction exists specifically to bring the remaining 5 methods — which call
  `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, immediate-SQL EF Core bulk operations that do **not**
  implicitly start a transaction (verified against EF Core docs, Context7) — under the same
  commit/rollback boundary as everything else in the unit of work. WHEN the enclosing service method's
  body returns a failure signal, or throws, after an `ExecuteUpdateAsync`/`ExecuteDeleteAsync` call has
  already run inside it, THEN `IUnitOfWork.ExecuteAsync` SHALL roll back the transaction, undoing that
  bulk operation, exactly as it would a tracked-entity mutation. The following in-scope repository
  methods are the ones the explicit transaction was introduced for, verified by direct read against
  current `develop` HEAD:
  | Repository method | Mechanism | Line | Called by (in-scope service method) |
  |---|---|---|---|
  | `SongKaraokeUrlRepository.IncrementPlayCountAsync` | `ExecuteUpdateAsync` | `SongKaraokeUrlRepository.cs:56-64` | `SongKaraokeUrlService.RecordPlayAsync` |
  | `SongKaraokeUrlRepository.RemoveAsync` | `ExecuteDeleteAsync` | `SongKaraokeUrlRepository.cs:48-53` | `SongKaraokeUrlService.RemoveUrlAsync` |
  | `ArtistRepository.DeleteAsync` | `ExecuteDeleteAsync` | `ArtistRepository.cs:148-154` | `ArtistService.DeleteArtistsAsync` (pilot, Phase 2) |
  | `SongRepository.DeleteAsync` | `ExecuteDeleteAsync` | `SongRepository.cs:136-142` | `SongService.DeleteSongsAsync` (pilot, Phase 2) |
  | `CatalogRepository.RemoveAsync` | `ExecuteDeleteAsync` | `CatalogRepository.cs:70-75` | `CatalogService.RemoveSongFromCatalogAsync` |

  Wrapping any of these five service methods in `IUnitOfWork.ExecuteAsync` is REQUIRED (§ 10), both for
  API-surface consistency and because REQ-UOW-24/25/26's atomicity guarantees now apply to them too via
  the explicit transaction.
  ```
  Given ArtistService.DeleteArtistsAsync's body calls ArtistRepository.DeleteAsync
      (ExecuteDeleteAsync), then a later validation check in the same body fails and the method
      returns (false, "...")
  When DeleteArtistsAsync is called through IUnitOfWork.ExecuteAsync
  Then the returned tuple's success element is false
  And re-reading the Artist row from the database shows it still exists (the ExecuteDeleteAsync
      call was rolled back by the explicit transaction, not just left un-committed)
  ```
  *Test:* an integration test that runs a body performing a real `ExecuteDeleteAsync`/`ExecuteUpdateAsync`
  call followed by a `(false, …)` return or a thrown exception, then asserts via a fresh read that the
  row was NOT deleted/updated — this is the regression test that proves the old REQ-UOW-33 carve-out
  wording was a design gap, not a structural impossibility: it fails under the pre-2026-08-04 design
  (no transaction, `ExecuteDeleteAsync` commits immediately and unconditionally) and passes under the
  explicit-transaction design. Cover at least one `ExecuteDeleteAsync` case (`ArtistService.DeleteArtistsAsync`
  or `SongService.DeleteSongsAsync`, both pilot-phase) and the `ExecuteUpdateAsync` case
  (`SongKaraokeUrlService.RecordPlayAsync`).
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
  return. A second test confirms `BackupService.CreateFullBackupAsync`'s wrap (Phase 4+, corrected from
  stale "Wave 5", non-blocking #1) does NOT throw
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
  references. **Corrected 2026-08-04 (4th-pass spec review, BL-G):** the enforcement check below
  originally covered only `_`-prefixed REPOSITORY fields. This is a hole: the pilot's deepest chain
  (`SongResolutionService.CommitAsync`'s lambda calling `_songService.CreateSongAsync`/
  `CreateSongWithUrlsAsync`/`UpdateSongAsync`, and `_artistResolution.CommitAsync`, per § 6a; similarly
  `ArtistResolutionService.CommitAsync`'s lambda calling `_artistService.CreateArtistAsync`) is driven
  through constructor-injected **service** fields, not repository fields — a grep restricted to
  repository-typed fields would pass a lambda body that still references `_songService`/
  `_artistResolution`/`_artistService` directly instead of resolving the nested service from `sp`,
  silently defeating the pattern exactly as a stray `_songRepository` reference would. *Test:* a
  per-method code-review checklist item (`design.md § 8` "the load-bearing rule") for every Phase 2/4+
  diff, plus a static/grep check in the review checklist confirming no `Services/*.cs`
  `ExecuteAsync`/`ExecuteReadAsync` lambda body references **any** `_`-prefixed constructor-injected
  field of the enclosing service — repository-typed (`_songRepository`, `_artistRepository`, …) AND
  service-typed (`_songService`, `_artistService`, `_artistResolution`, `_urlService`, …) alike. The
  check enumerates the specific field names known to matter today:
  `SongResolutionService._songRepository`, `._artistResolution`, `._songService`;
  `ArtistResolutionService._artistRepository`, `._artistService`; `SongService._songRepository`,
  `._artistRepository`, `._urlRepository`, `._urlService`; `ArtistService._artistRepository`,
  `._songRepository`, `._catalogRepository`.
  ```
  Given SongService wraps UpdateSongAsync's body in _uow.ExecuteAsync<(bool, string)>(async sp => { ... })
  When the lambda body is reviewed
  Then every repository call inside the lambda resolves its repository via
      sp.GetRequiredService<ISongRepository>() inside that same lambda
  And no reference to the service's constructor-injected _songRepository field appears
      anywhere inside the lambda body

  Given SongResolutionService wraps CommitAsync's body in _uow.ExecuteAsync<(bool, string, Song?)>(async sp => { ... })
  And the body calls the nested ISongService.CreateSongAsync
  When the lambda body is reviewed
  Then the nested service is resolved via sp.GetRequiredService<ISongService>() inside that same lambda
  And no reference to the service's constructor-injected _songService or _artistResolution fields
      appears anywhere inside the lambda body
  ```

### DRY & comprehensibility

- **REQ-UOW-10** — The unit-of-work boundary SHALL be expressed in **at most one line of code per
  service method** and **zero lines per repository method**. A design requiring an added
  `AppDbContext` parameter on repository methods, or two or more lines of ceremony per service method,
  fails this criterion. *Test:* reviewer-checked diff statistic recorded in the task-log.
- **REQ-UOW-11** — The five pass-through `SaveChangesAsync` implementations on standalone repository
  interfaces that do not extend `IBaseRepository<T>`
  (`ArtistRepository.cs:157-158`, `CatalogRepository.cs:78-79`,
  `SongKaraokeUrlRepository.cs:67-68`, `SongRepository.cs:145-146`, `BackupRepository.cs:46-49`)
  SHALL be reduced to at most one save entry point (the `IUnitOfWork` boundary). *Test:* a source-level
  assertion (grep in the review checklist) that no in-scope repository interface among these five
  declares or calls `SaveChangesAsync`. **Corrected 2026-08-04 (4th-pass spec review, BL-B):**
  `BaseRepository.cs:76-79`'s pass-through implements `IBaseRepository<T>.SaveChangesAsync()`
  (`Domain/RepositoryInterface/IBaseRepository.cs:18`), a member also relied on by the EXCLUDED
  `Services/QueueService.cs` via its `IVenueRepository`/`IEventRepository`/`IEventParticipationRepository`
  fields (`QueueService.cs:97,134,145`, verified). Removing it would force a de facto edit of an
  excluded file (REQ-UOW-31). It is **out of scope for this spec** — moved to
  `changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/` alongside
  the six embedded-mutator saves (D12 item 4 below). This spec's REQ-UOW-11 obligation is limited to
  the five standalone-interface pass-throughs above; `PersonRepository`/`VenueRepository` retain a
  technically-reachable inherited `SaveChangesAsync()` after this spec ships (no in-scope service calls
  it, but the interface member is not deleted) — see `design.md § 8` "IBaseRepository<T>.SaveChangesAsync()
  is NOT removed" decision, **APPROVED by Helder 2026-08-04**.

  **Separately, corrected 2026-08-04 (`design.md § 8` D12 item 4):** this requirement
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
  explicitly acknowledged **STOPGAP**. This work SHALL delete it, as part of **Phase 2 (PILOT)**
  (corrected from stale "Wave 3b", non-blocking #1 — D11 restructured Waves into Phases and moved this
  deletion into Phase 2 specifically, `design.md § 10`), an ordinary step of the pilot work with no
  external gate. *(withdrawn 2026-08-04: the sub-clause gating this deletion on Helder's on-device T10
  re-run #6 is cancelled — see `design.md § 8` decision "cancel the T10 re-run #6 gate". There is no
  reason to device-test the stopgap when this work deletes and replaces it.)* **Merge-ordering note
  (NB-4, third-pass spec review):** `feat/inline-artist-create` has NOT merged into `develop` as of this
  revision. If it merges before Phase 2 runs, Phase 2 deletes the stopgap as described above; if it has
  not merged, `develop`'s `SongRepository.UpdateAsync` does not contain the stopgap and Phase 2's
  stopgap-deletion step is a no-op confirmation instead — REQ-UOW-18 is satisfied vacuously in that
  case. Phase 2 runs unconditionally either way; only whether it deletes anything changes. Record which
  case applied in the task-log (`design.md § 10`).

  > **Spec updated [2026-08-18]:** NB-4's *first* case applied — `feat/inline-artist-create` merged into
  > `develop` (`71926980`) and reached this branch by merge, so the stopgap WAS present and has now been
  > DELETED (`Infra/Repository/SongRepository.cs`, `UpdateAsync`). Deletion is behaviour-neutral under the
  > unit of work (a freshly-scoped context never has a tracked `Song`, so the guard was unreachable), proven
  > by `SongServiceUpdateIntegrationTests` green after deletion. Deletion also exposed a *separate* defect
  > the stopgap had masked: `GetByIdAsync`'s eager `Include(OriginalArtist)` (BUG-055) makes EF FK-fixup
  > rewrite `Song.ArtistId` back from the stale navigation on attach, silently discarding BUG-067's artist
  > change — fixed by detaching the navigation before the write in `SongService.UpdateSongAsync`. Open for
  > Helder: whether that detach belongs in the service or in `SongRepository.UpdateAsync`.

### Guideline amendments (documentation deliverables)

- **REQ-UOW-19** — `code-style-reference.md § DI Registration Conventions` currently reads
  "`AddScoped` — Repositories, Services, IDatabaseInit (per-lifetime scope)". This is an ASP.NET-shaped
  rule that is **wrong for MAUI** and is the direct cause of all 27 `AddScoped` registrations
  (corrected 2026-08-04, 4th-pass spec review non-blocking #6 — 25 in
  `Extensions/ServiceCollectionExtensions.cs` + 2 in `MauiProgram.cs:71-72`, verified by grep; previously
  stated as 25). It
  SHALL be replaced with a MAUI-correct rule stating that MAUI scopes are per-Window (effectively
  app-lifetime on mobile) and naming the chosen unit-of-work pattern. The change follows
  `CLAUDE.md § Amending These Rules` (`amend:` prefix + changelog entry).
- **REQ-UOW-20** — The stale comment on `SongRepository.GetByIdAsync` describing a "Tracked query"
  SHALL be corrected or removed — it contradicts the global `NoTracking` default.
- **REQ-UOW-21** — The duplicate `IAppInfo` registration (`MauiProgram.cs:86` and `:157`) SHALL be
  removed, leaving one.

### Obsolete concurrency workaround removal (NB-1, third-pass spec review)

- **REQ-UOW-29** — **Corrected 2026-08-04 (4th-pass spec review, BL-H).** The static `DbLoadGate`
  field declaration is at `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs:16`, not `:12-14` (the
  original line reference pointed at its preceding comment block, not the field itself); the comment
  spans lines 12–15. The full comment, quoted verbatim (the prior draft quoted only its first
  sentence), is:
  ```
  // Static: all CRUD list ViewModels share one effectively-singleton AppDbContext
  // (MAUI has no per-page scope), so at most one DB load may run at a time app-wide.
  // SQLITE-WORKAROUND: remove this gate when SQLite is replaced (INFRA_MSSQL) —
  // see constraints-registry.md § EF Core / SQLite and DevCycleCraft/page-load-frozen/plan.md.
  ```
  This comment carries **two independent rationales with two independent revert triggers**, not one:
  (1) the MAUI-no-per-page-scope rationale this spec's `IUnitOfWork` pattern resolves, and (2) a
  SEPARATE `SQLITE-WORKAROUND` rationale — the `page-load-frozen` `Microsoft.Data.Sqlite` sync-async
  freeze (`constraints-registry.md § EF Core / SQLite`, `DevCycleCraft/page-load-frozen/plan.md`) —
  whose stated revert trigger is `INFRA_MSSQL` (replacing SQLite), not "MAUI unit-of-work pattern
  ships." Removing `DbLoadGate` once every in-scope consumer is converted (Phase 4+) resolves rationale
  (1) but does **nothing** about rationale (2); if the `page-load-frozen` freeze is still live, removing
  the gate would reintroduce that separate, unrelated bug class. **The static `DbLoadGate` SHALL NOT be
  removed until BOTH:** (a) every in-scope consumer is converted (§ 10 Phase 4+, as originally stated),
  AND (b) the `page-load-frozen` regression tests (`DevCycleCraft/page-load-frozen/`) are confirmed
  green without the gate present — i.e. the sync-async freeze this gate also happens to prevent is
  independently verified fixed or no longer applicable, not merely assumed fixed because rationale (1)
  no longer applies. If (b) cannot be confirmed within this spec's Phase 4+, `DbLoadGate` removal is
  deferred to a follow-up item that owns closing out `page-load-frozen`, and this AC is satisfied
  vacuously (record which case applied in the task-log, mirroring REQ-UOW-18's NB-4 pattern). This
  change removes rationale (1)'s root cause (Phase 4+, `design.md § 10`); leaving the gate in place
  afterward for rationale (1) alone would silently reintroduce app-wide load serialization, directly
  undermining REQ-UOW-05's guarantee that concurrent units of work do not share a context and therefore
  do not need to queue behind each other — but rationale (2) is a separate, mandatory precondition on
  removal that the original AC omitted entirely. *Test:* a test asserting two concurrent list-load calls
  (e.g. two different CRUD list ViewModels loading simultaneously) complete without serializing through
  a shared semaphore AND without reproducing the `page-load-frozen` freeze, plus a source-level check
  that `DbLoadGate` no longer exists, plus confirmation in the task-log that the `page-load-frozen`
  regression suite is green before the gate is deleted.

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

- **REQ-UOW-34** (4th-pass finding BL-E; mechanism REVISED by Helder 2026-08-04 — `design.md § 6`
  Revision 12, superseding Revision 11) — A read-only unit of work SHALL NOT publish an ambient scope.
  `ExecuteReadAsync` never assigns `_ambientScope`; only `ExecuteAsync` does. This removes the silent
  write-loss defect structurally: because every ambient scope belongs to a write, any nested write that
  joins one is guaranteed to be saved. **No exception is thrown for nesting, in either direction** —
  the Revision 11 `IsReadOnly` flag and fail-closed throw are withdrawn as a guard against a scenario
  with no call site in this codebase.
  ```
  Given a body running inside IUnitOfWork.ExecuteAsync (a write)
  And that body calls IUnitOfWork.ExecuteReadAsync to look up data needed to populate the entity
  When the outer write body completes successfully
  Then the nested read joins the SAME scope and no second AppDbContext is created
  And the outer write is saved
  ```
  ```
  Given a body running inside IUnitOfWork.ExecuteReadAsync (a read)
  And that body calls IUnitOfWork.ExecuteAsync with a body that mutates an entity
  When the nested write executes
  Then it opens its OWN scope (no ambient scope is visible to it)
  And the mutation IS persisted — no exception is thrown and nothing is silently discarded
  ```
  *Test:* two integration tests, one per direction above. The first asserts a single `AppDbContext`
  instance and a persisted write; the second asserts the nested write is persisted and that no
  `InvalidOperationException` occurs. The second test is the regression test for BL-E — it fails
  against the Revision 10 design (write silently lost) and against Revision 11 (throws).

- **REQ-UOW-35** (added 2026-08-18, Helder's decision — the second branch of REQ-UOW-09) —
  `IUnitOfWork` SHALL expose a **flush** affordance (`FlushAsync`) that persists the pending changes
  of the **current** unit of work **without committing its transaction**. A flush makes
  database-generated keys (and any other store-generated value) materialised and readable *inside*
  the lambda body, which is what lets a body return a generated id it has just created. It is NOT a
  commit: the explicit transaction opened by `ExecuteAsync` stays open, so a later failure signal or
  a later exception still rolls the flushed rows back — atomicity is unchanged.

  The member lives on `IUnitOfWork` and **not** on any repository: REQ-UOW-11 retires the
  pass-through `SaveChangesAsync` members, and this requirement must not reintroduce a save entry
  point on a repository. `IArtistRepository.SaveChangesAsync` is therefore still retired.

  Calling flush **outside** a unit of work (no ambient scope) is invalid and SHALL throw
  `InvalidOperationException` — fail-closed, consistent with `ResultSignalsSuccess`'s treatment of an
  unrecognised result (REQ-UOW-27). Silently no-op'ing would let a caller believe its changes were
  persisted when no context exists to persist them.

  ```
  Given a body running inside IUnitOfWork.ExecuteAsync
  When the body calls FlushAsync after creating an entity
  Then the entity's database-generated key is populated and readable inside the body
  And the unit of work's transaction is still open (nothing is committed yet)
  ```
  ```
  Given a body running inside IUnitOfWork.ExecuteAsync that has already called FlushAsync
  When the body subsequently returns a failure tuple, or throws
  Then the transaction is rolled back
  And no flushed row survives in the database
  ```
  ```
  Given no unit of work is in progress (no ambient scope)
  When FlushAsync is called
  Then an InvalidOperationException is thrown
  ```
  *Test:* three integration tests, one per scenario above (flush-then-failure-signal rolls back,
  flush-then-exception rolls back, flush outside a unit of work throws), plus REQ-UOW-09's existing
  pinned outcome test which only passes once the flush exists.

## Validation rules

- No service method may call more than one `SaveChangesAsync` per unit of work **unless** the flow is
  listed in REQ-UOW-09 (REQ-UOW-08 is out of scope, deferred — `design.md § 8` D12) and the reason is
  documented inline.
- No repository may hold a `DbContext` beyond the unit of work that produced it.
- No singleton or transient UI type may capture a repository or a data-writing service that owns a
  `DbContext` (captive dependency) — `AddDbContextFactory<AppDbContext>(..., ServiceLifetime.Scoped)`
  (corrected 2026-08-04, 4th-pass spec review non-blocking #7 — `AppDbContextFactory` without the
  `Add` prefix is a real, unrelated class, `Infra/AppDbContextFactory.cs`) registers
  `AppDbContext` itself as scoped (§ 1 "Reviewer-finding correction"), so a **transient** ViewModel
  resolving a repository is exactly as captive as a singleton would be; the ViewModel's own DI
  lifetime does not protect it (`design.md § 8`, BL-1 widening). `AppShellViewModel` and `AppShell`
  (`MauiProgram.cs:109-110`, singletons) and the three transient ViewModels named in Phase 4+
  (`QueueSongPickerViewModel`, `QueueManagementViewModel`, `PersonPickerViewModel` — none injects a
  Queue/Event repository despite their names, `design.md § 8` D12 item 6) are the known offenders to
  audit and convert. **Corrected 2026-08-04 (4th-pass spec review, BL-D):** `QueueManagementViewModel`
  also constructor-injects `IEventService`/`IQueueServiceNew` (both excluded types) — it converts only
  its `IPersonRepository`/`ISongRepository` usage to `IUnitOfWork`; its `IEventService`/`IQueueServiceNew`
  fields are NOT converted (`design.md § 8` D12 item 6 correction, **APPROVED by Helder 2026-08-04**).
- **No parallel fan-out inside a unit of work (new, non-blocking #10, 4th-pass spec review).** A body
  passed to `ExecuteAsync`/`ExecuteReadAsync` SHALL NOT start two or more `ExecuteAsync`/`ExecuteReadAsync`
  calls concurrently (e.g. via `Task.WhenAll`) from within the same outer unit of work. The `AsyncLocal`
  ambient-scope join (`design.md § 6a`) is not thread-safe across concurrent branches of the same async
  flow — two parallel joins would share one non-thread-safe `AppDbContext` (§ 1, EF Core's documented
  concurrency constraint), reproducing the exact hazard this design exists to eliminate. Sequential
  nested calls (the audited chains in § 6a) are safe; parallel fan-out is not. No test is required for
  this rule at this time (no in-scope method fans out in parallel today, per § 6a's audit) — it is a
  documented constraint on future code, enforced by review.
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
  `Infra/Repositories/*` into one family is combined with the deferred Queue/Event unit-of-work
  migration and now runs **after Phase 3's VERIFY gate, before Phase 4+** — **corrected 2026-08-04,
  superseded by Helder ("a, but not yet!")** from the earlier "hard prerequisite before Phase 0"
  position (corrected from stale "Wave 0", non-blocking #1; `design.md § 8` superseded Prerequisite
  decision; `design.md § 10` Phase 3.5) — it is out of
  scope for *this* spec's phases in the sense that this spec does not perform it, not in the sense
  that it is optional or deferred indefinitely; only its position relative to Phase 0 changed.
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
