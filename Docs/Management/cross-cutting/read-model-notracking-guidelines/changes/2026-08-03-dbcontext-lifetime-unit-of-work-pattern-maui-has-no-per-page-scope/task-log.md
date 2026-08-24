# Task Log — DbContext Lifetime & Unit-of-Work Pattern

---
## Task: 0.4 / 0.4b — Nested chain atomicity (REQ-UOW-22/24/09)
**Plan:** `plan.md`
**Status:** To Review
**Started:** 2026-08-04
**Completed:** 2026-08-04

### Changed files:
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (created)
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/task-log.md` (created)

### Spec deviation — call-order assumption in Task 0.4 Step 2 was wrong

`plan.md` Task 0.4 Step 2 says to inject the fault into `IArtistRepository.AddAsync`, describing
it as "the innermost point of the chain, after the outer SongService write has already run".
That ordering does not hold on HEAD for the `CreateNew` branch actually exercised:
`SongResolutionService.CommitAsync` resolves/creates the artist **first**
(`ResolveOrCreateArtistIdAsync` at `:162`, which commits the `Artist` row via
`ArtistResolutionService.CommitAsync` → `ArtistService.CreateArtistAsync`), and only **then**
calls `_songService.CreateSongAsync` (`:166`). So a throw from `IArtistRepository.AddAsync` fires
**before** any `Song` write is attempted — nothing has been persisted yet, and the test is
trivially green (0 Artist rows / 0 Song rows), which is **not** RED evidence.

Verified this empirically before correcting the test: with the fault on `IArtistRepository.AddAsync`
exactly as the plan describes, all 4 planned tests passed (see first run below) — that is the
"wrong reason" the Phase 0 exit condition warns about (`plan.md` line 154-156).

**Correction applied (within Task 0.4/0.4b's authorized scope — a test-construction detail, not a
redesign):** moved the fault injection to `ISongRepository.AddAsync` — the point that genuinely
runs *after* the Artist row has already been committed for this chain — and for the failure-tuple
variant, used `SongService.CreateSongAsync`'s own title-length validation (`ValidateTitleInput`,
returns `(false, message, null)` without calling the repository at all) as the "inner call returns
a failure tuple after an outer write already ran" case. Both reproduce the intended defect
(partial state survives) for the right reason. Both test method names spell out the actual
ordering they exercise (`...AfterArtistAlreadyCommitted...`) and each carries an inline comment
explaining the deviation.

> **Spec updated 2026-08-04:** `plan.md` Task 0.4 Step 2's fault-injection point
> (`IArtistRepository.AddAsync`) does not reproduce RED for the `CreateNew` branch's actual call
> order (artist is resolved/committed before the song write is attempted). The RED-producing
> injection point is `ISongRepository.AddAsync` (throw) / `SongService.CreateSongAsync`'s title
> validation (failure tuple) — both fire after the Artist row is already committed. Recorded here
> per the Living Spec Protocol; not applied in place to `plan.md` (implementor scope does not
> include editing `plan.md` — flagging for Helder/orchestrator review at `To Review`).

### Test results (verbatim)

**Run 1 — literal plan Step 2 (fault on `IArtistRepository.AddAsync`), before correction:**
All 4 tests in the file PASSED, including the fault-injection one — this is the "wrong reason"
finding above; that version of the file was replaced before committing (never committed).

**Run 2 — corrected file (`ISongRepository.AddAsync` fault + title-length failure tuple), final:**

```
Aprovado MyVocaList.Tests.Integration.UnitOfWork.NestedUnitOfWorkTests.CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet [2 s]
Aprovado MyVocaList.Tests.Integration.UnitOfWork.NestedUnitOfWorkTests.CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow [197 ms]

Com falha (FAILED) CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow [81 ms]
  Assert.Equal() Failure: Values differ
  Expected: 0
  Actual:   1

Com falha (FAILED) CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow [45 ms]
  Assert.Equal() Failure: Values differ
  Expected: 0
  Actual:   1

Total de testes: 4
     Aprovados: 2
     Com falha: 2
```

| Test | Expected on HEAD (per task briefing) | Actual | Match |
|---|---|---|---|
| `CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow` (0.4 Step 1, happy path) | PASS — not RED evidence by itself | PASS | Yes |
| `CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow` (0.4 Step 2, fault injection) | FAIL — partial state survives | FAIL — `Assert.Equal(0, artistCount)`: Expected 0, Actual 1 (Artist row survives; Song row correctly absent) | Yes, for the corrected injection point |
| `CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow` (0.4 Step 3, failure-tuple) | FAIL — outer write persisted anyway | FAIL — `Assert.Equal(0, artistCount)`: Expected 0, Actual 1 (Artist row persisted despite overall failure tuple) | Yes, for the corrected construction |
| `CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet` (0.4b, REQ-UOW-09) | PASS — characterization, not a defect reproduction | PASS | Yes |

### Candidate/choice shapes used

```csharp
new SongCandidate(
    "<title>", /* Version */ string.Empty, /* FeaturedArtists */ null, /* Lyrics */ null,
    new ArtistCandidate("<artist name>", /* ExternalProvider */ null, /* ExternalId */ null),
    /* ExternalProvider */ null, /* ExternalId */ null);

new ArtistCandidate("REQ-UOW-09 Probe Artist", "spotify", "ext-uow-09");

ResolutionChoice.CreateNew;
```

### Step 3 (failure-tuple path) constructibility

Constructible, but only by reframing which call is "outer" vs "inner" relative to the plan's
literal wording (see deviation note above): for the `CreateNew` branch, the **artist** write is
the one that already ran by the time any song-side failure can occur, not the reverse. Using a
too-long title (`SongService.MaxTitleLength == 100` → 101 `'x'` chars) makes
`SongService.CreateSongAsync` fail via `ValidateTitleInput` and return `(false, message, null)`
without ever calling the repository — a genuine "inner returns a failure tuple, no throw" path,
reached only after the Artist row is already committed.

### Build notes
Build: passed (0 errors) | Tests (whole suite, filtered to `NestedUnitOfWorkTests`): 2 passed
(expected GREEN), 2 failed (expected RED, evidence above) | `git diff develop --name-only` before
this task's own file was added: only prior tasks' test files (Bug068RegressionTests.cs,
UnitOfWorkTestHost.cs, UnitOfWorkTestHostTests.cs) — no production file touched by this task.
Files written and re-read: `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`.

---

## Phase 0 gate — PASSED 2026-08-04 (orchestrator-verified)

Verified by the orchestrator running the suite directly, not from a subagent's report.

### Suite state at end of Phase 0

`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` →
**Failed: 3, Passed: 525, Skipped: 0, Total: 528** (baseline before Phase 0 was 520/520).

### RED evidence — each failure read and matched to its stated reason

| AC | Test | Failure (verbatim) |
|----|------|--------------------|
| REQ-UOW-03 | `Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` | `InvalidOperationException: The instance of entity type 'Song' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked.` — `IdentityMap.ThrowIdentityConflict` → `InternalDbSet.Update` → `SongRepository.cs:131` → `SongService.cs:145` |
| REQ-UOW-22 | `CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow` | `Assert.Equal() Failure: Expected: 0, Actual: 1` — the Artist row survives a thrown fault in the Song write |
| REQ-UOW-24 (nested precedence) | `CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow` | `Assert.Equal() Failure: Expected: 0, Actual: 1` — same partial state via a returned `(false, …)` tuple instead of a throw |

Each failed for its stated reason. No test failed for an incidental reason.

### PASSING characterization tests (pin behavior through the refactor, not defect repros)

- `CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow` — happy path. Passes on HEAD
  because each nested call saves eagerly today; **this assertion alone is not RED evidence.**
- `CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet` — REQ-UOW-09,
  pinned before Task 2.3 re-shapes `ArtistResolutionService.CommitAsync`.
- `Artist_` / `Person_` / `Venue_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` — REQ-UOW-04,
  see the decision below.

### Decision: REQ-UOW-04 is VACUOUSLY satisfied for Artist/Person/Venue (Helder, 2026-08-04)

The AC assumed all four in-scope repository families carry the same latent defect. **They do not.**
Verified by reading the repositories:

| Family | `GetByIdAsync` | Why BUG-068 cannot occur |
|---|---|---|
| Artist | `ArtistRepository.cs:79-80` — explicit `.AsTracking()` | EF identity resolution returns the already-tracked instance |
| Person / Venue | inherited `BaseRepository<T>.GetByIdAsync` `:24-29` — `_dbSet.FindAsync(id)` | `FindAsync` checks the change tracker first — same instance |
| **Song** | `SongRepository.cs:53-54` — bare `FirstOrDefaultAsync`, no tracking override | inherits the global `NoTracking` default → **fresh detached instance** → collides at `Update` |

Helder chose **Option A**: keep the three tests as passing characterization tests, record the AC as
vacuously satisfied (same treatment as REQ-UOW-18/20 per findings F3/F6). Rationale: they lock the
behavior in through the refactor and would catch a future edit removing `.AsTracking()` from
`ArtistRepository` — which is exactly how `SongRepository` arrived at its current state.

**Consequence:** the plan's skip-mark step (finding F2) is cancelled outright. There are no skipped
tests, so Phase 3.1's gate is now **`Skipped: 0`**, and Tasks 4.2/4.4 lose their unskip steps.

### Plan correction — Task 0.4's fault-injection point was wrong

`plan.md` Task 0.4 Step 2 specified injecting the fault into `IArtistRepository.AddAsync`. In the
`CreateNew` branch actually exercised, `SongResolutionService.CommitAsync` resolves/creates the artist
**first** (`ResolveOrCreateArtistIdAsync` → `ArtistResolutionService.CommitAsync` →
`ArtistService.CreateArtistAsync`, already committed) and only then calls `_songService.CreateSongAsync`
— so that throw fires **before any Song write is attempted**. Verified empirically: all 4 tests went
green with the plan's injection point. Corrected to `ISongRepository.AddAsync` (throw path) and
`CreateSongAsync`'s own title-length validation (failure-tuple path), both of which genuinely run
after the Artist row is committed.

### REQ-UOW-31 / scope checks

- `git diff develop --name-only` — test files, docs and `.sln` only. **No production `.cs` modified.**
- Excluded-file grep (`EventService|QueueService|QueueServiceNew|EventRepository|QueueRepository|EventParticipationRepository`) over the diff — **no matches.**
- No file carrying a `TODO [BUG-071 / UOW]` marker was touched.

### Process note — hook bypass, authorised

All three RED-era commits (`1963af4b`, `756ed4a3`, `f62fae7a`) used `git commit --no-verify`. The
pre-commit hook runs `dotnet test` and aborts on any failing test, which makes the RED-first commit
that `bug-tracking.md` mandates for a Critical bug impossible to land. Helder authorised the bypass
for Phase 0 RED commits only, and asked for the underlying rules conflict to be tracked — registered
as **BUG-074** (`DevCycleCraft/hooks-redesign/bugs/2026-08-04-BUG-074-…`).

### Commits (worktree `feat/uow-pilot`)

`1974da98` harness · `1963af4b` REQ-UOW-03 RED · `756ed4a3` REQ-UOW-22/24 RED · `f62fae7a` REQ-UOW-04 characterization

**Phase 0 complete. Phase 1 may start.**

---
## Task: Close Revision-12 ambient-scope invariant test gap (`UnitOfWork.cs`)
**Plan:** `plan.md`
**Status:** To Review
**Started:** 2026-08-04
**Completed:** 2026-08-04

Review of Task 1.2a's delivery of `Infra/UnitOfWork/UnitOfWork.cs` confirmed the implementation is
correct but found the Revision 12 invariant — only a WRITE publishes the AsyncLocal ambient scope;
`ExecuteReadAsync` never does — had no direct test coverage. This task closes that gap with four
tests appended to `UnitOfWorkLifetimeTests.cs`. `UnitOfWork.cs` itself was **not modified**.

### Changed files
- `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkLifetimeTests.cs` (appended 4 tests)
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/task-log.md` (this entry)

### AC traceability

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-UOW-34 | Write nested in a read does NOT join ambient (none exists) — opens own scope, mutation is persisted | `Infra/UnitOfWork/UnitOfWork.cs` `ExecuteReadAsync`/`ExecuteAsync` | `ExecuteReadAsync_NestedWrite_OpensOwnScope_AndPersists` |
| REQ-UOW-34 | `ExecuteReadAsync` never publishes `_ambientScope` | `Infra/UnitOfWork/UnitOfWork.cs` `ExecuteReadAsync` | `ExecuteReadAsync_Standalone_DoesNotPublishAmbientScope` |
| REQ-UOW-34 | Read nested in a write JOINS the write's scope (same context); outer write still commits | `Infra/UnitOfWork/UnitOfWork.cs` `ExecuteReadAsync` join branch | `ExecuteAsync_NestedRead_JoinsSameContext_AndOuterWriteCommits` |
| REQ-UOW-22 | Write nested in a write JOINS the outer scope; failure anywhere in the joined chain rolls back the whole unit of work (all-or-nothing) | `Infra/UnitOfWork/UnitOfWork.cs` `ExecuteAsync` join branch + transaction rollback | `ExecuteAsync_NestedWrite_JoinsSameContext_OuterThrowRollsBackInnerToo` |

Test 1 deliberately asserts more than context identity: it reads the row back through a **fresh**
`ExecuteReadAsync` scope after the outer read returns, because a NotSame-only assertion would still
pass even if the nested write's `SaveChangesAsync` were silently skipped — the exact BUG-071-shaped
failure mode Revision 12 exists to prevent.

### Build / test evidence

- `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` — 0 errors (104 pre-existing warnings, unrelated).
- Filtered run (`--filter FullyQualifiedName~UnitOfWorkLifetimeTests`): **9 passed** (5 pre-existing + 4 new), 0 failed.
- Full suite: **Failed 3, Passed 539, Total 542** — up from the pre-task baseline **Failed 3, Passed 535, Total 538**. The 3 failures are the pre-existing, intentional Phase 0 RED tests (2 in `NestedUnitOfWorkTests`, 1 in `Bug068RegressionTests`) — unchanged, still failing for the same reason, unaffected by this task.

### `UnitOfWorkTestHost.DisposeAsync` teardown change — evidence still holds

The commit that delivered `UnitOfWork.cs` also changed `UnitOfWorkTestHost.DisposeAsync`
(`EnsureDeletedAsync` → `SqliteConnection.ClearAllPools()` + best-effort `File.Delete`), which
affects `CreateLegacy`'s teardown too. Re-ran the full suite after this task's changes: the same 3
RED tests still fail for their original reasons (confirmed by the unchanged failure count and
messages above) — the teardown change does not invalidate the pinned Phase 0 RED evidence.

### Constraints honored
- `UnitOfWork.cs` not modified.
- `UnitOfWorkTestHost.CreateLegacy()` not modified; only `Create()` used by the new tests.
- Value-returning `ExecuteAsync<TResult>` overload not used (still `NotImplementedException`, Task 1.2b scope).
- No file carrying a `TODO [BUG-071 / UOW]` marker touched; no existing test modified.

### Commit

`git commit --no-verify` — pre-commit hook runs `dotnet test` and aborts on the 3 known-RED Phase 0
tests (BUG-074, tracked). Helder authorised `--no-verify` for this situation.

---
## Task: 1.2b — `ResultSignalsSuccess`: save-skip, fail-closed, transactional rollback
**Plan:** `plan.md § Task 1.2b`
**Status:** To Review
**Started:** 2026-08-04
**Completed:** 2026-08-04

### Changed files:
- `Infra/UnitOfWork/UnitOfWork.cs` (only `ResultSignalsSuccess` — the stub body replaced; nothing else touched)
- `MyVocaList.Tests/Integration/UnitOfWork/SaveSkipTests.cs` (new)

### TDD evidence (Red → Green)

Red (stub in place): `--filter FullyQualifiedName~SaveSkipTests` → **Failed 11, Passed 1, Total 12**.
All 11 failures were `System.NotImplementedException: ResultSignalsSuccess is implemented by Task
1.2b` — i.e. failing for the right reason. The 1 pass was
`ExecuteAsync_NoSignalOverload_AlwaysSaves`, which is correct: the no-signal overload never consults
the signal, so REQ-UOW-26 is green by construction and only guards against a regression.

Green (after implementing): same filter → **Failed 0, Passed 12, Total 12**.

### Build notes
Build: passed (0 errors). Full suite AFTER: **Failed 3, Passed 551, Total 554** (before: 3/539/542).
The 3 failures are the pinned Phase 0 REDs, byte-identical in name and reason:
`NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`,
`NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`,
`Bug068RegressionTests.Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`.
**None of them turned green** — expected, since they run on `CreateLegacy()` (no `IUnitOfWork` in
that composition) and are Phase 2's obligation.

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| REQ-UOW-24 | Failure tuple (leading `bool false`) ⇒ no `SaveChangesAsync`, mutation not persisted | `UnitOfWork.cs` `ResultSignalsSuccess` branch 1 + `ExecuteAsync<TResult>` else-branch rollback | `ExecuteAsync_FailureTuple_DoesNotPersistMutation` |
| REQ-UOW-24 | Same for `IUnitOfWorkOutcome.Success == false` | `ResultSignalsSuccess` branch 2 | `ExecuteAsync_OutcomeWithSuccessFalse_DoesNotPersistMutation` |
| REQ-UOW-24 | Failure tuple after an UPDATE leaves the original value | same | `ExecuteAsync_FailureTupleAfterUpdate_LeavesOriginalValue` |
| REQ-UOW-25 | Success tuple ⇒ `SaveChangesAsync` + commit, mutation persisted | `ResultSignalsSuccess` branch 1 + commit-branch | `ExecuteAsync_SuccessTuple_CommitsMutation` |
| REQ-UOW-25 | Same for `IUnitOfWorkOutcome.Success == true` | `ResultSignalsSuccess` branch 2 | `ExecuteAsync_OutcomeWithSuccessTrue_CommitsMutation` |
| REQ-UOW-26 | No-signal overload saves unconditionally on non-throwing return | `ExecuteAsync(Func<IServiceProvider, Task>, ct)` | `ExecuteAsync_NoSignalOverload_AlwaysSaves` |
| REQ-UOW-27 | Unmarked named result ⇒ `InvalidOperationException` naming the type + both fixes, nothing persisted | `ResultSignalsSuccess` branch 3 (fail-closed throw) | `ExecuteAsync_UnmarkedNamedResult_ThrowsAndDoesNotPersist` |
| REQ-UOW-27 | Primitive (`int`) result ⇒ same fail-closed throw | same | `ExecuteAsync_PrimitiveResult_ThrowsAndDoesNotPersist` |
| REQ-UOW-27 | Tuple whose FIRST element is not `bool` ⇒ throw, not treated as success | `t[0] is bool` guard | `ExecuteAsync_TupleWithNonBoolFirstElement_ThrowsAndDoesNotPersist` |
| REQ-UOW-27 | Empty `ValueTuple` (`Length == 0`) ⇒ throw, not implicit success | `t.Length > 0` guard | `ExecuteAsync_EmptyTuple_ThrowsAndDoesNotPersist` |
| REQ-UOW-33 | Failure signal after `ExecuteDeleteAsync` ⇒ explicit transaction rolls the bulk delete back | `ExecuteAsync<TResult>` `transaction.RollbackAsync` | `ExecuteAsync_FailureTupleAfterExecuteDelete_RollsBackTheDelete` |
| REQ-UOW-33 | Failure signal after `ExecuteUpdateAsync` ⇒ play count unchanged | same | `ExecuteAsync_FailureTupleAfterExecuteUpdate_RollsBackTheUpdate` |

REQ-UOW-34 (both directions) is **already fully covered** by `UnitOfWorkLifetimeTests`
(`ExecuteReadAsync_NestedWrite_OpensOwnScope_AndPersists`,
`ExecuteAsync_NestedRead_JoinsSameContext_AndOuterWriteCommits`,
`ExecuteReadAsync_Standalone_DoesNotPublishAmbientScope`), delivered by the preceding commit
(`test(uow): cover Revision-12 ambient-scope invariant`). The plan's Task 1.2b line "Modify
`NestedUnitOfWorkTests.cs` (REQ-UOW-34 only)" is therefore **already satisfied elsewhere** — no
existing test was modified, per this task's constraints.

### Deviations from the plan text
- Plan Step 1 describes the REQ-UOW-24/25 probes as going through `SongService.UpdateSongAsync`.
  No service is wrapped in `IUnitOfWork` yet (that is Phase 2/3), so the probes call
  `IArtistRepository` / `ISongKaraokeUrlRepository` directly inside the `ExecuteAsync` body. The
  signal semantics under test are identical; the service wrap adds no new branch to
  `ResultSignalsSuccess`.
- `ProbeOutcome` / `UnmarkedResult` are test-local records, exactly as the plan anticipated for the
  `BackupResult` stand-in (reviewer non-blocking #1).

### Post-edit verification
Re-read `Infra/UnitOfWork/UnitOfWork.cs` lines 111–136 after the edit: the three branches are
present in order (`ITuple`/`Length > 0`/`t[0] is bool` → `IUnitOfWorkOutcome` → throw), and the
surrounding `ExecuteAsync` overloads are byte-unchanged.

### Constraints honored
- `UnitOfWorkTestHost.CreateLegacy()` not modified; `SaveSkipTests` uses `Create()` exclusively.
- No existing test modified; no file carrying a `TODO [BUG-071 / UOW]` marker touched.
- Task 1.3 (`MauiProgram.cs` registration swap) NOT started.

### Commit
`git commit --no-verify` — the pre-commit hook runs `dotnet test` and aborts on the 3 known-RED
Phase 0 tests (BUG-074, tracked). Helder authorised `--no-verify` for exactly this situation.

---
## Task: 1.3 — Registration swap (`AddDbContextFactory` + `IUnitOfWork`)
**Plan:** `plan.md § Task 1.3: Registration swap`
**Status:** To Review
**Started:** 2026-08-04
**Completed:** 2026-08-04

### Changed files:
- `MyVocaList/MauiProgram.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkCompositionTests.cs` (new)

### The change
`AddDbContext<AppDbContext>((sp, options) => …)` → `AddDbContextFactory<AppDbContext>((sp, options)
=> …, ServiceLifetime.Scoped)`. The configuration lambda is byte-identical: same
`Data Source={dbPath}`, same `AddInterceptors(CollationInterceptor, TransactionLogInterceptor)`,
same `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`. Only the method name and the
trailing `ServiceLifetime.Scoped` argument differ. Added
`builder.Services.AddSingleton<IUnitOfWork, MyVocaList.Infra.UnitOfWork.UnitOfWork>();` plus
`using MyVocaList.Domain.UnitOfWork;`. Removed the second `IAppInfo` registration (REQ-UOW-21),
keeping the one in the MAUI-platform-services block.

### Verified: `AppDbContext` stays directly resolvable
Context7 (`/dotnet/entityframework.docs`, EF Core 10.0.0 per `Directory.Packages.props`) — *"The
`AddDbContextFactory` method in EF Core 6.0 now also registers the `DbContext` type directly as a
scoped service."* Confirmed empirically:
`Composition_AppDbContext_StillResolvesDirectlyFromAScope` asserts exactly one `ServiceDescriptor`
for `typeof(AppDbContext)` with `Lifetime == Scoped`, resolves it, and resolves an
`IArtistRepository` (which constructor-injects it). Green. Repository constructors need no change.

### AC traceability matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|----------------|-------------|
| REQ-UOW-01 | `IDbContextFactory<AppDbContext>` and `IUnitOfWork` each registered exactly once | `MauiProgram.cs:61-74` | `Composition_RegistersFactoryAndUnitOfWork_ExactlyOnce` |
| REQ-UOW-01 | `AddDbContextFactory(…, Scoped)` keeps `AppDbContext` an ordinary scoped descriptor | `MauiProgram.cs:65` | `Composition_AppDbContext_StillResolvesDirectlyFromAScope` |
| REQ-UOW-01 / REQ-UOW-21 | Production registration shape; one `IAppInfo` registration | `MauiProgram.cs:65-74`, `:163` | `MauiProgram_RegistrationShape_MatchesTestHost` |
| REQ-UOW-02 | One `AppDbContext` per scope; distinct per scope; factory yields fresh instances | `MauiProgram.cs:65` | `Composition_EachScopeGetsADistinctAppDbContext_SameInstanceWithinAScope` |
| REQ-UOW-14 | Both interceptors survive the swap (collated query + transaction-log entry) | `MauiProgram.cs:67-71` | `Composition_InterceptorsSurviveTheSwap_CollatedQueryAndTransactionLog` |

### Spec/plan deviations (for review)
1. **The real composition root is unreachable from tests — plan Step 1 as written is unexecutable.**
   `MyVocaList.csproj` removes `MauiProgram.cs` from the plain `net10.0` TFM the test project
   consumes (`<Compile Remove="MauiProgram.cs" />`), and it reads `FileSystem.AppDataDirectory`,
   which has no off-device value. No test can call `MauiProgram.CreateMauiApp()`. Covered in two
   halves instead: behavioural assertions over `UnitOfWorkTestHost.Create()` (which mirrors the
   production shape line-for-line) plus a source-text drift guard reading `MauiProgram.cs`, so the
   two compositions cannot silently diverge. Production compilation is verified by building
   `-f net10.0-android` (0 errors).
2. **Test file location:** plan Step 1 names `Unit/DependencyInjection/UnitOfWorkCompositionTests.cs`;
   the dispatch briefing names `Integration/UnitOfWork/UnitOfWorkCompositionTests.cs`. Followed the
   briefing — the tests hit a real SQLite file, so `Integration/` is the correct classification.
3. **Plan Step 4 was already done** — `UnitOfWorkTestHost.Create()` exists from an earlier task and
   needed no edit. `CreateLegacy()` untouched, per briefing.
4. **`IUnitOfWork` lifetime:** Singleton, per plan Step 3 and `UnitOfWorkTestHost.Create()`.

### Post-edit verification
Re-read `MauiProgram.cs:58-74` (registration block) and `:158-164` (`IAppInfo` removal site) after
each edit; both changes present and syntactically intact. `MauiProgram.cs` compiles under
`net10.0-android`.

### Build notes
Build: `MyVocaList.Tests` 0 errors, 99 warnings (pre-existing). `MyVocaList -f net10.0-android`
0 errors, 2 warnings (DevExpress licensing, pre-existing).
Tests **before** this task: Failed 3 / Passed 551 / Total 554.
Tests **after**: Failed 3 / Passed 556 / Total 559 — all 5 new tests pass; the delta is +5 passed,
0 new failures.
The 3 failures are the SAME pinned Phase 0 REDs on `CreateLegacy()`, unchanged by the swap:
`Bug068RegressionTests.Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`,
`NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`,
`NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`.
None turned green — the RED evidence measures the tracking defect, not the registration, as
intended (finding B1 holds). Phase 2 fixes them.

### Constraints honored
`UnitOfWorkTestHost.CreateLegacy()`, `Infra/UnitOfWork/UnitOfWork.cs`, and every file carrying a
`TODO [BUG-071 / UOW]` marker are untouched. `MauiProgram.cs` edit is minimal — no reformatting or
reordering of unrelated registrations. Phase 2 NOT started.

### Commit
`git commit --no-verify` — the pre-commit hook runs `dotnet test` and aborts on the 3 known-RED
Phase 0 tests (BUG-074, tracked). Helder authorised `--no-verify` for exactly this situation.

---
## Task 2.1: `ArtistService` — wrap the 3 mutating methods + migrate the Phase 0 tests
**Plan:** `plan.md` § Task 2.1
**Status:** blocked: spec gap (implementation complete; suite not green — see the spec gap below)
**Started:** 2026-08-18
**Completed:** 2026-08-18 (in-scope steps only)
**Worktree/branch:** `C:\Users\helde\source\repos\myvocalist-uow` / `feat/uow-pilot`

### Changed files
- `Services/ArtistService.cs`
- `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`
- this `task-log.md`

### RED evidence captured BEFORE the composition switch (Step 1 requirement)
Run on the unmodified worktree, `CreateLegacy()` composition still in place:
`Failed 3, Passed 556, Skipped 0, Total 559`. The 3 failures were exactly the pinned Phase 0 REDs:
- `Bug068RegressionTests.Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`
- `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
  (`Expected: 0 / Actual: 1` — the Artist row survives the later Song-write fault)
- `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
  (`Expected: 0 / Actual: 1`)
So the RED→GREEN transition is not confounded by the `CreateLegacy()` → `Create()` switch made in
Step 1 of this task.

### Steps executed
- **Step 1** — `Bug068RegressionTests.cs` (4 sites) and `NestedUnitOfWorkTests.cs` (4 sites, one of
  them the `services =>` fault-injection overload) migrated from `UnitOfWorkTestHost.CreateLegacy()`
  to `UnitOfWorkTestHost.Create()`. `CreateLegacy()` itself untouched.
- **Step 2** — `IUnitOfWork _uow` added to `ArtistService`'s constructor (after
  `ICatalogRepository`, before `ILogger`). The three repository fields are kept: the unwrapped read
  methods (`GetPagedArtistsForListAsync`, `SearchArtistsByNameAsync`,
  `GetDeleteConfirmationAsync`) still use `_artistRepository`.
- **Step 3** — `CreateArtistAsync`, `UpdateArtistAsync`, `DeleteArtistsAsync` wrapped in
  `_uow.ExecuteAsync<T>(async sp => { ... }, ct)`. The two `await _artistRepository.SaveChangesAsync(ct);`
  lines deleted. `DeleteArtistsAsync` keeps `ArgumentNullException.ThrowIfNull(ids)` OUTSIDE the
  lambda so the guard still throws synchronously-on-await as before, then returns the
  `ExecuteAsync` task; its `ICatalogRepository.CountByArtistAsync` validation resolves from `sp`.
- **Step 4** — `IArtistRepository.SaveChangesAsync` NOT removed (Task 2.3 owns it).
- **Step 5** — `ArtistServiceTests.CreateSut()` now passes
  `PassthroughUnitOfWork.Over(_artistRepoMock, _songRepoMock, _catalogRepoMock)`. No assertion was
  weakened, deleted or commented out; no test file had any `SaveChangesAsync` setup/verify to begin
  with.
- **Step 6** — run; see below.

### REQ-UOW-28 compliance (the load-bearing rule) — per method
No `_`-prefixed constructor field appears anywhere inside a lambda body. Verified by reading each
wrapped method after the edit:
| Method | Resolved from `sp` at top of lambda | Constructor fields inside lambda |
|---|---|---|
| `CreateArtistAsync` | `IArtistRepository` | none |
| `UpdateArtistAsync` | `IArtistRepository` | none |
| `DeleteArtistsAsync` | `IArtistRepository`, `ICatalogRepository` | none |
Grep confirmation: `git grep -n "_artistRepository\|_songRepository\|_catalogRepository" -- Services/ArtistService.cs`
returns only the field declarations, the constructor assignments, and the four unwrapped read
methods — zero hits between an `ExecuteAsync(` and its closing `}, ct);`.

### AC traceability
| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-UOW-04 | Artist create→read→update does not throw a tracking conflict and persists | `ArtistService.CreateArtistAsync` / `UpdateArtistAsync` wrapped | `Bug068RegressionTests.Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` (GREEN under `Create()`) |
| REQ-UOW-10 | The trailing `SaveChangesAsync` disappears from the service; the save is the unit of work's | 2 deleted lines in `ArtistService` | whole `Bug068RegressionTests` Artist path + `ArtistServiceTests` |
| REQ-UOW-28 | Lambda bodies resolve from `sp`, never from constructor fields | 4 `sp.GetRequiredService<...>()` calls | structural — the per-method table above |
| REQ-UOW-33 | `ExecuteDeleteAsync`-based bulk delete runs under the explicit transaction | `DeleteArtistsAsync` wrap | `SaveSkipTests` (Task 1.2b) |

### Test results
- **Before (legacy composition, no production change):** `Failed 3, Passed 556, Skipped 0, Total 559`
- **After:** `Failed 7, Passed 552, Skipped 0, Total 559`

The 3 expected Phase 0 REDs are unchanged and still red, exactly as the plan predicts — the
`ArtistService` wrap does not fix the Song path (Task 2.2) nor the nested chain (Task 2.4).

`ArtistResolutionServiceTests`: **GREEN — confirmed by running, not assumed** (Step 6's intermediate
state holds: EF's `Update` on the detached instance created in the now-disposed inner scope works).

**4 NEW failures, all one root cause, all in files outside this task's `Files owned`:**
| Test | Failure |
|---|---|
| `AppServicesRegistrationTests.AddAppServices_ResolvingArtistResolutionService_Succeeds` | `Unable to resolve service for type 'IUnitOfWork' while attempting to activate 'ArtistService'` |
| `AppServicesRegistrationTests.AddAppServices_ResolvingSongResolutionService_Succeeds` | same |
| `AppServicesRegistrationTests.AddAppServices_ResolvingSongFormViewModelGraph_Succeeds` | same |
| `UnitOfWorkTestHostTests.LegacyHost_TwoDifferentServices_ShareOneAppDbContextInstance` | same (host built by `CreateLegacy()`, which deliberately omits `IUnitOfWork`) |

### Spec gap: `AddAppServices()` does not register `IUnitOfWork`, so any host built from it alone cannot activate a wrapped service
**Location:** `plan.md` § Task 2.1 Step 1 (finding B1) — it names only the two integration-test files.
**Gap description:** finding B1 is correct but its blast radius is larger than the plan states.
`IUnitOfWork` is registered in `MyVocaList/MauiProgram.cs:74` and in `UnitOfWorkTestHost.Create()`,
but NOT in `MyVocaList/Extensions/ServiceCollectionExtensions.AddAppServices()`. From the moment the
first service constructor requires `IUnitOfWork` — i.e. this task — every composition that calls
`AddAppServices()` without separately registering `IUnitOfWork` fails to activate it. Two such
compositions exist beyond the two files the plan lists, and neither is in this task's `Files owned`:
`MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` (3 tests) and
`MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkTestHostTests.cs` (1 test). The problem is
structural and grows with every subsequent Phase 2/4 task, not specific to `ArtistService`.
**Options:**
- **Option A** — register `IUnitOfWork` inside `AddAppServices()` and delete the duplicate line from
  `MauiProgram.cs:74` and from `UnitOfWorkTestHost.Create()`. Every composition then gets it for
  free and no later Phase 2/4 task hits this again. Consequence: `ServiceCollectionExtensions.cs` is
  a sequential-only file and `AddAppServices()` would then depend on `Infra.UnitOfWork.UnitOfWork`,
  which may cross a layer boundary the design deliberately kept in the composition root; also
  `CreateLegacy()` would silently gain a `IUnitOfWork` over an `AddDbContext`-registered context,
  which is not a composition that exists in production.
- **Option B** — leave `AddAppServices()` alone and fix the 4 tests at their own sites: add
  `services.AddSingleton<IUnitOfWork, UnitOfWork>()` (plus the `IDbContextFactory` it needs) to
  `AppServicesRegistrationTests`'s local `ServiceCollection`, and either migrate
  `UnitOfWorkTestHostTests` to `Create()` or retire it (its assertion — "the entity created through
  `ArtistService` is still tracked by `host.Db`" — is a characterization of exactly the lifetime
  behaviour this refactor is removing, so it is expected to die, not to be repaired). Consequence:
  4 files edited across 2 tasks' scopes; the same registration is then duplicated in 3 places.
**Recommendation:** Option A for the registration, plus the `UnitOfWorkTestHostTests` retirement
from Option B — the tracked-across-services assertion is a pinned description of BUG-068's cause and
cannot survive Phase 2 by design. Both need Helder's decision because they touch a sequential-only
file and delete a test.
**Blocking:** Yes for the *suite-green* gate. The in-scope implementation itself is complete and was
committed so the work is not lost; the 4 collateral failures are recorded here rather than papered
over. No file outside `Files owned` was touched.

### Post-edit verification
Re-read `Services/ArtistService.cs:1-140` after every edit — the `using
Microsoft.Extensions.DependencyInjection;` addition, the constructor change and all three wraps are
present and structurally intact (`git diff --stat`: 4 files, +102 / -74).
Build: `dotnet test --no-restore` compiled the solution with **0 errors** (2 build attempts: the
first surfaced `CS1061 GetRequiredService`, fixed by adding the
`Microsoft.Extensions.DependencyInjection` using).

### Commit
`git commit --no-verify` — the pre-commit hook runs `dotnet test` and aborts on the known-RED tests
(BUG-074). Helder authorised `--no-verify` for exactly this situation; the 4 collateral failures are
disclosed in the commit body as well as here.

---
## Task: Task 2.1 blocker resolution — `IUnitOfWork` registration site + retire superseded legacy-host test
**Plan:** `plan.md` (Phase 2)
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

Resolves the `blocked: spec gap` raised by Task 2.1. Helder authorised both decisions; implemented
exactly as decided, no re-litigation.

### Decision 1 — single registration site: `AddAppServices()`
`IUnitOfWork` is now registered inside `AddAppServices()` (`MyVocaList/Extensions/ServiceCollectionExtensions.cs`),
and the duplicate registrations were removed from `MyVocaList/MauiProgram.cs` and from
`UnitOfWorkTestHost.Create()`. Rationale: Task 2.1 made `IUnitOfWork` a constructor dependency of
`ArtistService`, which `AddAppServices()` registers — so any composition built from `AddAppServices()`
alone (all DI-composition tests) failed to activate `ArtistService`. Registering it beside the
services that need it makes the graph self-contained, and removing it from the test host means the
harness now genuinely exercises the production registration rather than a copy of it.

`MauiProgram.cs` is `<Compile Remove/>`-d from the `net10.0` TFM the test project builds, so the
source-text drift guard `UnitOfWorkCompositionTests.MauiProgram_RegistrationShape_MatchesTestHost`
was updated in the same commit: it now asserts `MauiProgram.cs` does **not** contain
`AddSingleton<IUnitOfWork,` and that `ServiceCollectionExtensions.cs` does — the inverse of the
previous assertion, preserving the same anti-drift guarantee against the new registration site.
A `LocateSource(relativePath)` helper replaced `LocateMauiProgram`'s hard-coded walk-up.

`UnitOfWorkTestHost.CreateLegacy()` was **not** touched — it is pinned Phase 0 RED evidence.

### Decision 2 — retire `UnitOfWorkTestHostTests.LegacyHost_TwoDifferentServices_ShareOneAppDbContextInstance`
Deleted (the file became empty and was removed; it had no `.sln` entry — `MyVocaList.Tests` is a
project, not a solution-items folder). This test was a **Phase-0 characterization of BUG-068's
cause**, not of desired behaviour: it asserted that an entity created through `ArtistService` is
still tracked by the host's session-scoped `AppDbContext`, i.e. that one context spans both service
calls. Phase 2 exists precisely to make that false. **Falsifying this assertion IS the pilot's
success signal**, so the test cannot survive by design — repairing it would mean re-asserting the
bug. This is the only test deleted; every other assertion stands exactly as written
(`testing.md § Builder Must Not Modify Tests`).

Files containing `TODO [BUG-071 / UOW]` (Queue/Event) were not touched.

### Changed files:
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
- `MyVocaList/MauiProgram.cs`
- `MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkCompositionTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkTestHostTests.cs` (deleted)

### Build notes
Build: passed (0 errors, 1 attempt). Tests before: `Failed 7, Passed 552, Skipped 0, Total 559`.
Tests after: **`Failed 3, Passed 555, Skipped 0, Total 558`** (one test retired). The 3 remaining
failures are exactly the intended Phase-2 REDs (BUG-074), unchanged:
- `Bug068RegressionTests.Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`
- `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
- `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`

Drift guard verified green in isolation: `--filter FullyQualifiedName~UnitOfWorkCompositionTests`
→ `Failed 0, Passed 5`.

### Post-edit verification
Every edited region re-read after the edit: the new `AddSingleton<IUnitOfWork, …>` block and the
`using MyVocaList.Domain.UnitOfWork;` in `ServiceCollectionExtensions.cs`; the replaced comment in
`MauiProgram.cs` (registration line gone, `AddDbContextFactory(…, ServiceLifetime.Scoped)` intact);
`UnitOfWorkTestHost.Create()` (registration gone, `CreateLegacy()` byte-identical).

### Commit
`git commit --no-verify` — the pre-commit hook runs `dotnet test` and aborts on the 3 intentional
REDs (BUG-074). Helder authorised `--no-verify` for exactly that; disclosed in the commit body.

---
## Task: 2.2 — `SongService` — 3 create/update methods
**Plan:** `plan.md § Task 2.2`
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

### Changed files:
- `Services/SongService.cs`
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
- `Docs/Management/.../task-log.md` (this file)

### What was done
- Added `IUnitOfWork _uow` to `SongService`'s constructor (kept all four repository/service fields —
  the unwrapped read methods and `DeleteSongsAsync` (Task 2.2b) still use them).
- Wrapped **`CreateSongAsync`**, **`UpdateSongAsync`**, **`CreateSongWithUrlsAsync`** in
  `_uow.ExecuteAsync<TResult>(async sp => { ... }, ct)`. Each is now an expression-bodied method:
  **exactly one added line of unit-of-work ceremony per method (REQ-UOW-10)**, zero lines in any
  repository.
- Deleted the trailing `await _songRepository.SaveChangesAsync(ct)` from all three.
  `ISongRepository.SaveChangesAsync` itself is **untouched** (Task 2.4b owns its retirement).
- `DeleteSongsAsync` **not touched** — Task 2.2b.

### REQ-UOW-28 (the load-bearing rule) — per-method verification
Mechanically checked by brace-matching each `_uow.ExecuteAsync<` lambda body and grepping it for
`_songRepository` / `_artistRepository` / `_urlRepository` / `_urlService`:

| Method | Resolved from `sp` | Constructor fields inside lambda |
|---|---|---|
| `CreateSongAsync` | `ISongRepository`, `IArtistRepository` | **none** |
| `UpdateSongAsync` | `ISongRepository` | **none** |
| `CreateSongWithUrlsAsync` | `ISongRepository`, `IArtistRepository`, `ISongKaraokeUrlRepository`, `ISongKaraokeUrlService` | **none** |

`_logger` is the only constructor field still referenced inside a lambda (`CreateSongWithUrlsAsync`'s
invalid-URL warning) — it is not a repository or a data-writing service, so REQ-UOW-28 does not apply
to it; this matches `ArtistService` (Task 2.1).

### REQ-UOW-07 (Step 5) — new test
`Bug068RegressionTests.CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow`. Uses the Task 0.4
decorator technique: `ThrowOnAddUrlRepository` wraps the real `SongKaraokeUrlRepository`, throws from
`AddAsync`, forwards every other member; injected via `UnitOfWorkTestHost.Create(customize)`. Asserts
**0 `Song` rows** and 0 `SongKaraokeUrl` rows survive the fault — i.e. the song write and the URL
writes really are one save inside one transaction. **PASSES.**

### Deviation — two `SaveChangesAsync` assertions re-expressed (NOT weakened; flagged for review)
`SongServiceTests.CreateSongWithUrlsAsync_ValidSongAndUrls_PersistsBoth` (:529) and
`..._EmptyUrlList_CreatesSongOnly` (:566) asserted
`_songRepoMock.Verify(r => r.SaveChangesAsync(...), Times.Once)`. REQ-UOW-10 **deliberately removes
that call from the service method**, so the assertion became structurally unsatisfiable — it encodes
the save's *old location*, not a guarantee the spec still makes at that location.

Rather than delete either assertion, both were **inverted to `Times.Never`** — the equally strict
positive encoding of REQ-UOW-10 ("zero save lines per service method") — and the atomic-single-commit
guarantee they used to carry (AC-6.2 / REQ-UOW-07) was **moved to a stronger observation**: the new
integration test above, which checks real rows in a real context instead of a mock call count.
`_urlRepoMock.Verify(SaveChangesAsync, Times.Never)` is unchanged. Net assertion strength increases.
**This is flagged deliberately for Helder's review** — it is the one place this task altered an
existing test's expectation.

### Build notes
Build: passed (0 errors). Test command: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-restore`.

| | Failed | Passed | Skipped | Total |
|---|---|---|---|---|
| Baseline (going in) | 3 | 555 | 0 | 558 |
| After | **2** | **557** | **0** | **559** |

Remaining failures — both intentional, both owned by **Task 2.4** (`SongResolutionService` still
unwrapped, so the 3-level chain is not yet atomic):
- `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
- `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`

The baseline's **third** failure was a pre-existing flaky `ObjectDisposedException:
'SQLitePCL.sqlite3'` during `EnsureCreated` that lands on a *different* integration test each run
(observed on `SongRepositoryTests.AddAsync_CatalogEntry_LinksArtistAndSong` and on
`UnitOfWorkCompositionTests.Composition_AppDbContext_StillResolvesDirectlyFromAScope`). Both pass in
isolation; it did not recur on the final run. Unrelated to this task — noted, not fixed.

### HEADLINE — REQ-UOW-03 RED -> GREEN
`Bug068RegressionTests.Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`
- **Before (HEAD `372d691d`, this task not applied):** FAILED — BUG-068 tracking conflict on the
  create -> read -> update sequence.
- **After:** **PASSED.** Verified in isolation: `--filter FullyQualifiedName~Bug068RegressionTests`
  -> `Failed: 0, Passed: 4`. The pilot's headline transition is achieved.

Files written and re-read: `Services/SongService.cs` (lambda bodies re-scanned mechanically for
REQ-UOW-28), `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`,
`MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`.

---
## Task: 2.2b — wrap `SongService.DeleteSongsAsync`
**Plan:** `plan.md § Task 2.2b`
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

### Changed files:
- `Services/SongService.cs`

### Implementation
`DeleteSongsAsync` now returns `_uow.ExecuteAsync<(bool success, string message)>(...)`, matching the
style established by `ArtistService.DeleteArtistsAsync` (Task 2.1) and the Task 2.2 `SongService`
methods. `ArgumentNullException.ThrowIfNull(ids)` stays outside the lambda (argument contract, not
unit-of-work work). `ISongRepository` is resolved via `sp.GetRequiredService<ISongRepository>()`
inside the lambda — **no `_`-prefixed constructor field appears in the lambda body** (REQ-UOW-28
verified by reading the method after the edit). The method has no `SaveChangesAsync`;
`SongRepository.DeleteAsync` is `ExecuteDeleteAsync`-based, so the value delivered is REQ-UOW-33:
the bulk delete now runs inside `ExecuteAsync`'s explicit transaction.

### No new `SaveSkipTests` case (deliberate)
`ExecuteAsync_FailureTupleAfterExecuteDelete_RollsBackTheDelete` (Task 1.2b) already asserts the
REQ-UOW-33 criterion at the level it lives: a failure signal returned after an `ExecuteDeleteAsync`
inside the lambda rolls the bulk delete back. That behaviour belongs to `UnitOfWork`, not to a
particular repository — a `SongRepository` copy of it would re-test the same transaction code with a
different `DELETE` statement and add no new failure mode. `DeleteSongsAsync` itself has no failure
return *after* its delete (the empty-id guard precedes it), so there is no service-level rollback
scenario to encode. Adding one would have meant writing a test with no AC of its own — declined per
`testing.md`. Existing unit coverage (`DeleteSongsAsync_ValidIds_ReturnsSuccess`, run through
`PassthroughUnitOfWork`) continues to pass unmodified.

### Build notes
Build: passed (0 errors) | Tests: `Failed: 2, Passed: 557, Skipped: 0, Total: 559` — identical to the
going-in baseline. The 2 failures are the two intentional REDs owned by Task 2.4
(`NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`,
`NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`).
No `ObjectDisposedException` flake on this run. Committed with `--no-verify` (pre-commit hook aborts
on those 2 intentional REDs, BUG-074 — authorised for exactly that).
Files written and re-read: `Services/SongService.cs`.

---
## Task 2.3: `ArtistResolutionService` + retire `IArtistRepository.SaveChangesAsync`
**Plan:** `plan.md § Task 2.3`
**Status:** blocked: spec gap
**Started:** 2026-08-18
**Completed:** —

### What was implemented (present in the worktree, deliberately NOT committed)
- `Services/ArtistResolutionService.cs` — `CommitAsync` wrapped in `_uow.ExecuteAsync<(bool, string, int)>`
  (Step 1), `ResolveAsync` wrapped in `_uow.ExecuteReadAsync<ArtistResolution>` (Step 3), `IUnitOfWork`
  added as a constructor dependency, both embedded `SaveChangesAsync` calls removed.
- `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs` — the two
  `.Setup(r => r.SaveChangesAsync(...))` stubs removed (Step 4), SUT constructed with
  `PassthroughUnitOfWork.Over(_repoMock, _artistServiceMock)`. **No assertion was weakened, removed or
  commented out.**
- WIP patch preserved at
  `%LOCALAPPDATA%/Temp/claude/C--Users-helde-source-repos-MyVocaList/f55efc67-60b9-45a1-b5b2-5dc48a150089/scratchpad/task-2.3-wip.patch`.

### REQ-UOW-28 — mechanically verified (not by eye)
Regex scan of `Services/ArtistResolutionService.cs` for `_artist(Repository|Service)` returns exactly
4 code hits — the two field declarations (`:12`, `:13`) and the two constructor assignments (`:25`,
`:26`) — plus one occurrence inside an explanatory comment (`:108`). **Neither `_artistRepository` nor
`_artistService` appears inside any lambda body.** Both are resolved from the lambda's own `sp`
(`sp.GetRequiredService<IArtistRepository>()`, `sp.GetRequiredService<IArtistService>()`).

### Step 6 result — the REQ-UOW-09 pinned outcome test FAILS after the re-shape
`NestedUnitOfWorkTests.CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet`
(Task 0.4b's characterization test) goes GREEN -> RED. Per the briefing this is "a real signal — report
it, do not adjust the test". The test was not touched.

### Spec gap: collapsing the save invalidates the CreateNew branch's generated key
**Location:** `requirements.md § REQ-UOW-09`; `plan.md § Task 2.3 Step 1`.
**Gap description:** Step 1 asserts the `save -> mutate -> save` pair "collapses to a single implicit
save". It cannot, as written: the CreateNew branch depends on the Artist's database-generated key
being materialised *before* the lambda returns, and a single deferred save materialises it *after*.
Two independent failures follow, both caused by this one fact:

1. `ArtistRepository.UpdateAsync` calls `DbSet.Update(artist)`, which forces state `Modified` on an
   entity that is still `Added` with a temporary key ->
   `InvalidOperationException: The property 'Artist.Id' has a temporary value while attempting to
   change the entity's state to 'Modified'.`
   (`ArtistRepository.cs:143` <- `ArtistResolutionService.cs:153`.)
2. `return (true, message, created.Id)` is evaluated inside the lambda, i.e. before
   `UnitOfWork.ExecuteAsync` saves — so the returned `artistId` is `0`. This breaks REQ-UOW-09's
   explicit guarantee that "the returned `artistId` must not change", and it is what makes
   `CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow` fail with
   `Failed to resolve or create artist` (`SongResolutionService` rejects the `0`).

Note that dropping the now-redundant `UpdateAsync` call (the entity is already tracked as `Added` in
the same context) would fix (1) but not (2), and would additionally break the existing unit assertion
`_repoMock.Verify(r => r.UpdateAsync(...), Times.Once)` — which I am not permitted to modify.

**Options:**
- Option A — take REQ-UOW-09's explicitly sanctioned second branch ("*or remains two saves inside one
  unit of work*"): keep an intermediate flush inside the single scope so the key materialises before
  the mutation. This requires **keeping** `IArtistRepository.SaveChangesAsync` (or an equivalent flush
  affordance on `IUnitOfWork`), which directly contradicts Step 5 / REQ-UOW-11's retirement.
  Consequence: REQ-UOW-09 and REQ-UOW-11 cannot both be satisfied for this method as currently specified.
- Option B — change `IArtistService.CreateArtistAsync`'s contract so the caller never needs the
  generated key inside the unit of work (e.g. `ArtistResolutionService` sets the external identity on
  the entity *before* `AddAsync`, and `SongResolutionService` consumes the `Artist` instance rather
  than an `int` id). Consequence: touches `IArtistService`/`ISongResolutionService` signatures and
  `SongResolutionService` — all outside this task's `Files owned`, and an interface change the
  subagent scope constraint forbids me making.
**Recommendation:** Option A, scoped narrowly: add a flush affordance to `IUnitOfWork` (so no
repository regains a save entry point and REQ-UOW-11 still holds), and have the CreateNew branch call
it once after `CreateArtistAsync`. This keeps REQ-UOW-09's observable outcome and the returned
`artistId` intact. It is an `IUnitOfWork` surface change — a D13 API-shape decision — so it is Helder's
to make, not mine.
**Blocking:** Yes — cannot proceed without resolution.

### Step 5 — grep result: retirement DEFERRED (not performed)
Repo-wide grep found **zero** remaining callers of `IArtistRepository.SaveChangesAsync` (production or
test) once the two mock setups are removed, so the member is mechanically retirable and no excluded
`TODO [BUG-071 / UOW]` file blocks it. It was nevertheless **not deleted**: Option A above needs
exactly that member (or its replacement) to exist, and deleting it now would pre-empt Helder's
decision on the gap. `Domain/RepositoryInterface/IArtistRepository.cs:55` and
`Infra/Repository/ArtistRepository.cs:157-158` are untouched.

### Verification evidence
| Run | Result |
|---|---|
| Baseline (`git stash`, HEAD `5a6d7ad`) | `Falha: 2, Aprovado: 557, Ignorado: 0, Total: 559` |
| With Task 2.3 changes | `Falha: 4, Aprovado: 555, Ignorado: 0, Total: 559` |

Baseline failures (both intentional REDs owned by Task 2.4, still red — correct):
- `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
- `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`

New failures introduced by this task (the two the gap is about):
- `NestedUnitOfWorkTests.CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet` (Step 6)
- `NestedUnitOfWorkTests.CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow`

`Skipped: 0` on every run. No `ObjectDisposedException` flake observed across the three runs.

### Why nothing was committed to `feat/uow-pilot`
Committing would leave the branch with 4 failures where the plan expects 2, and Task 2.4 consumes 2.3
— it would build on a known-broken base. The changes remain in the worktree working tree (and as a
saved patch) for inspection; no code file was committed. Files written and re-read:
`Services/ArtistResolutionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`.

---
## Task: Task 2.3 spec-gap resolution + completion - `IUnitOfWork.FlushAsync` (REQ-UOW-35) and `ArtistResolutionService` wrap
**Plan:** `plan.md` (Phase 2, Task 2.3)
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

Resolves the `blocked: spec gap` raised by the previous Task 2.3 attempt ("collapsing the save
invalidates the CreateNew branch's generated key"). Helder chose the recommended Option A: add a
flush affordance to `IUnitOfWork`. Implemented exactly as decided; no re-litigation.

### Decision implemented - REQ-UOW-35, `IUnitOfWork.FlushAsync`
`plan.md` Task 2.3 Step 1's claim that the save->mutate->save pair "collapses to a single implicit
save" was the error - **not** REQ-UOW-09, whose text explicitly sanctions *"or remains two saves
inside one unit of work"*. The CreateNew branch evaluates `return (true, message, created.Id)`
**inside** the `ExecuteAsync` lambda, so a single deferred save returns `artistId = 0`; and
`ArtistRepository.UpdateAsync` throws on the still-`Added` entity's temporary key. `FlushAsync` calls
`SaveChangesAsync` on the **ambient scope's** `AppDbContext` and stops - no `CommitAsync`, no
transaction disposal - so the flushed rows are still rolled back by a later failure tuple or
exception. The member sits on `IUnitOfWork`, not on a repository, so REQ-UOW-11 holds and
`IArtistRepository.SaveChangesAsync` **was** retired as Step 5 planned.

`_ambientScope` invariant preserved: `FlushAsync` only **reads** it. The assignment count in
`Infra/UnitOfWork/UnitOfWork.cs` is unchanged at 4 (2 write-path publications + 2 `finally` clears);
`ExecuteReadAsync` still never publishes and never saves.

### Step 5 grep - retirement PERFORMED
Re-ran the repo-wide grep myself before deleting. Only two hits remained, both being the member
itself (`Domain/RepositoryInterface/IArtistRepository.cs:55`, `Infra/Repository/ArtistRepository.cs:157`);
the four other `SaveChangesAsync` mentions under `Services/` are the already-deleted-call comments in
`ArtistResolutionService.cs` and `ArtistService.cs`. Zero callers, production or test. Both deleted.

### REQ-UOW-28 - mechanical confirmation (not by eye)
A Python scan stripped all `//` and `///` lines from `Services/ArtistResolutionService.cs` and located
every occurrence of the constructor fields. `_artistService` and `_artistRepository` each appear on
exactly **two** lines - the field declaration and the constructor assignment - and on **no** line
inside either lambda body (`ExecuteReadAsync<ArtistResolution>` / `ExecuteAsync<(bool, string, int)>`).
Both lambdas resolve `IArtistRepository`/`IArtistService` from their own `sp`. `_uow.FlushAsync(ct)`
is the one constructor field used inside a lambda and is deliberate: `IUnitOfWork` holds no
`AppDbContext` and flushes the **ambient** scope's context, so REQ-UOW-28's "each call gets its own
context" rationale does not apply to it (documented inline at the call site).

### Changed files:
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/requirements.md`
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/design.md`
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/plan.md`
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/task-log.md`
- `Domain/UnitOfWork/IUnitOfWork.cs`
- `Infra/UnitOfWork/UnitOfWork.cs`
- `Domain/RepositoryInterface/IArtistRepository.cs`
- `Infra/Repository/ArtistRepository.cs`
- `Services/ArtistResolutionService.cs`
- `MyVocaList.Tests/Infrastructure/UnitOfWorkMocks.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/SaveSkipTests.cs`
- `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-UOW-35 | Flush materialises the generated key inside the body; transaction still open | `Infra/UnitOfWork/UnitOfWork.cs` `FlushAsync` | `SaveSkipTests.FlushAsync_InsideSuccessfulUnitOfWork_MaterialisesGeneratedKeyAndPersists` |
| REQ-UOW-35 | Flush then failure tuple -> flushed row rolled back | `UnitOfWork.ExecuteAsync` failure branch (`RollbackAsync`) | `SaveSkipTests.FlushAsync_ThenFailureSignal_RollsFlushedRowBack` |
| REQ-UOW-35 | Flush then exception -> flushed row rolled back | `UnitOfWork.ExecuteAsync` `catch` branch | `SaveSkipTests.FlushAsync_ThenThrow_RollsFlushedRowBack` |
| REQ-UOW-35 | Flush outside a unit of work throws (fail-closed) | `UnitOfWork.FlushAsync` ambient-scope guard | `SaveSkipTests.FlushAsync_OutsideUnitOfWork_Throws` |
| REQ-UOW-09 | CreateNew produces exactly one Artist with external fields; returned artistId matches | `Services/ArtistResolutionService.cs` CreateNew branch + `FlushAsync` | `NestedUnitOfWorkTests.CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet` |
| REQ-UOW-11 | No repository is a save entry point | `IArtistRepository` / `ArtistRepository` - member deleted | Compile-time (zero callers; grep evidence above) |
| REQ-UOW-28 | Lambda bodies resolve from `sp`, never constructor fields | `Services/ArtistResolutionService.cs` both lambdas | Mechanical scan (above) + `ArtistResolutionServiceTests` via `PassthroughUnitOfWork` |
| REQ-UOW-34 | `ResolveAsync` is a read - never publishes an ambient scope | `Services/ArtistResolutionService.cs` `ExecuteReadAsync` wrap | `NestedUnitOfWorkTests` Revision-12 ambient-scope tests (pre-existing) |

### Verification evidence
| Run | Result |
|---|---|
| Baseline, HEAD `5a6d7ad` (recorded by previous attempt) | `Falha: 2, Aprovado: 557, Ignorado: 0, Total: 559` |
| Previous attempt's uncommitted WIP (the gap) | `Falha: 4, Aprovado: 555, Ignorado: 0, Total: 559` |
| **After this task** | **`Falha: 2, Aprovado: 561, Ignorado: 0, Total: 563`** |

Total rose by 4 - the four new REQ-UOW-35 integration tests. `Ignorado: 0`.

Remaining failures - exactly the two Task-2.4-owned intentional REDs, unchanged and still red (correct):
- `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow`
- `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow`

Both previously-broken tests are GREEN. Targeted re-run
(`--filter CreateNewWithExternalIdentity... | NovelArtistAndSong... | FlushAsync`):
`Aprovado! - Com falha: 0, Aprovado: 6, Ignorado: 0, Total: 6`.

No test assertion was weakened, skipped or deleted. `_repoMock.Verify(r => r.UpdateAsync(...), Times.Once)`
is untouched and passing. `UnitOfWorkTestHost.CreateLegacy()` untouched. No file carrying
`TODO [BUG-071 / UOW]` was opened. Build: 0 errors (1 attempt); warnings pre-existing only.
The known `ObjectDisposedException: 'SQLitePCL.sqlite3'` flake did **not** reproduce in either run.

### Post-edit verification
Every edit was applied by a script that asserts a unique anchor match before replacing (an unmatched
anchor aborts rather than silently no-ops), and each changed region was re-read afterwards:
`IUnitOfWork.cs` (new `FlushAsync` member + doc comment), `UnitOfWork.cs` (`FlushAsync` body; the
`_ambientScope.Value =` assignment count re-counted at 4, unchanged), `IArtistRepository.cs` and
`ArtistRepository.cs` (member gone, brace structure intact), `ArtistResolutionService.cs` (flush call
sited between `CreateArtistAsync`'s success check and the external-identity mutation),
`UnitOfWorkMocks.cs` (`PassthroughUnitOfWork.FlushAsync`), `SaveSkipTests.cs` (4 tests inserted above
the private helpers, inside the class).

### Commits
Two, in order, both with `--no-verify`: the pre-commit hook runs `dotnet test` and aborts on the two
known intentional REDs (BUG-074). Helder authorised `--no-verify` for exactly this; disclosed in each
commit body.

### Design concern (non-blocking, for review)
`ArtistResolutionService._artistRepository` is now referenced only by the constructor - every use
moved into a lambda and resolves from `sp`. The field (and its constructor parameter) is a candidate
for removal, but the constructor signature is a wider surface than this task owns, and Task 2.4 /
Phase 3's VERIFY gate is the right place to sweep such residue across all four pilot services at once.
Left in place deliberately; not a defect.

---

## Task: 2.4 - `SongResolutionService` - the 3-level join
**Plan:** `plan.md § Task 2.4`
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18
**Branch / worktree:** `feat/uow-pilot` @ `C:\Users\helde\source\repos\myvocalist-uow` (base HEAD `2a98c59`)

### Changed files:
- `Services/SongResolutionService.cs`
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs`

### What was done
**Step 1 - `CommitAsync` wrapped in `ExecuteAsync`.** The whole `switch (choice)` body moved inside
`_uow.ExecuteAsync<(bool success, string message, Song? song)>(async sp => { ... }, ct)`. `ISongRepository`,
`ISongService` and `IArtistResolutionService` are resolved from the lambda's own `sp`. The private helper
`ResolveOrCreateArtistIdAsync` runs inside that lambda, so it was changed from an instance method reading
`_artistResolution` to a `static` method **taking `IArtistResolutionService` as a parameter** - a field read
one call frame away from the lambda is the same defect as one written inline, and would have silently
defeated the join across levels 2-3 while still compiling and passing.

**Step 2 - `ResolveAsync` wrapped in `ExecuteReadAsync`.** `ISongRepository` and `IArtistResolutionService`
resolved from `sp`. The nested `IArtistResolutionService.ResolveAsync` is itself an `ExecuteReadAsync`
(Task 2.3), so the read-join path is now exercised by production code (REQ-UOW-34).

**No `FlushAsync` needed.** Unlike Task 2.3's Artist CreateNew branch, no Song path consumes a
database-generated key *inside* the lambda: `songService.CreateSongAsync` returns the entity but the value is
not read before the lambda returns, and both production consumers (`SongFormViewModel.cs:618,:699`) discard it
(`var (success, message, _) = ...`). The `UpdateExisting`/`AttachExternalId` reloads hit the same tracked
context. No repository save entry point was reintroduced; `ISongRepository.SaveChangesAsync` is untouched
(Task 2.4b owns it).

### Verification evidence

**Baseline at HEAD `2a98c59`** (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-restore`):
`Com falha: 2, Aprovado: 561, Ignorado: 0, Total: 563` - the 2 failures being exactly the two named tests.

**After** (same command): `Com falha: 0, Aprovado: 564, Ignorado: 0, Total: 564`.
(563 -> 564 = the one new test added below; **Failed 0, Skipped 0** as required.)

**RED -> GREEN, the two outstanding Phase 0 REDs:**

| Test | Before (`2a98c59`) | After |
|---|---|---|
| `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow` (REQ-UOW-22) | **FAIL** - `Assert.Equal() Failure: Expected 0, Actual 1` at line 90 (artist row survived the song-write fault) | **PASS** |
| `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow` (REQ-UOW-24 nested) | **FAIL** - `Assert.Equal() Failure: Expected 0, Actual 1` at line 129 (artist row survived the overall failure tuple) | **PASS** |

Both prove the outermost `ExecuteAsync` decides save-skip **once** for the whole nested chain: the artist row
flushed at level 2 is rolled back by level 1's transaction, whether the failure arrives as a throw or as a
leading-`false` tuple.

**3-level join verified directly, not inferred.** REQ-UOW-22 requires the outcome *"**without** any nested call
opening a second `AppDbContext`"* - a clause the two rollback tests establish only circumstantially. Added
`CommitAsync_NovelArtistAndSong_UsesOneAppDbContextAcrossAllThreeNestedLevels` (traced to REQ-UOW-22): scoped
recording factories for `ISongRepository`/`IArtistRepository` capture the `AppDbContext` of the scope each is
resolved from. `AppDbContext` is scoped, so a level that opened its own scope would re-run the factory there
and yield a different instance. Observed: >= 2 resolutions, **1 distinct instance** by reference.
*Falsification performed:* temporarily short-circuiting `UnitOfWork.ExecuteAsync`'s ambient-join branch made
this test **FAIL**; `Infra/UnitOfWork/UnitOfWork.cs` was then restored with `git checkout --` and `git status`
confirmed it unmodified. The test is therefore non-vacuous. `UnitOfWork.cs` is **not** in this commit.

**REQ-UOW-28 mechanical confirmation** (comment text stripped, then every occurrence of each of the three
fields located by word-boundary match - not read by eye):

| Field | Occurrences | Lines | Inside any lambda body? |
|---|---|---|---|
| `_songRepository` | 2 | declaration (`:17`), constructor assignment (`:32`) | **No** |
| `_artistResolution` | 2 | declaration (`:18`), constructor assignment (`:33`) | **No** |
| `_songService` | 2 | declaration (`:19`), constructor assignment (`:34`) | **No** |

Zero occurrences in any lambda body or in any helper reachable from one. `_uow`, `_scorer` and `_logger` are
the accepted exceptions (the unit of work itself, plus two stateless non-scoped collaborators), as established
in Task 2.3.

### AC traceability matrix

| AC ID | Criterion | Implementation | Test method |
|---|---|---|---|
| REQ-UOW-22 | 3-level chain is all-or-nothing under a mid-chain fault | `SongResolutionService.CommitAsync` `ExecuteAsync` wrap | `NestedUnitOfWorkTests.CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow` |
| REQ-UOW-22 | same observable happy-path outcome | idem | `NestedUnitOfWorkTests.CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow` |
| REQ-UOW-22 | no nested call opens a second `AppDbContext` | ambient join across levels 1-3 | `NestedUnitOfWorkTests.CommitAsync_NovelArtistAndSong_UsesOneAppDbContextAcrossAllThreeNestedLevels` (**new**) |
| REQ-UOW-24 | nested precedence - save-skip decided once, by the outermost `ExecuteAsync` | leading-`bool` tuple returned from the outer lambda | `NestedUnitOfWorkTests.CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow` |
| REQ-UOW-28 | every repository *and nested service* resolved from the lambda's own `sp` | `sp.GetRequiredService<...>()` x3 in `CommitAsync`, x2 in `ResolveAsync`; `ResolveOrCreateArtistIdAsync` now takes the service as a parameter | mechanical field audit above + `SongResolutionServiceTests` via `PassthroughUnitOfWork.Over(...)` |
| REQ-UOW-34 | read-only method uses `ExecuteReadAsync`; read joins an ambient write scope | `ResolveAsync` wrap | existing `SongResolutionServiceTests` `ResolveAsync_*` suite, assertions unchanged |

### Post-edit verification
Every edit was applied by a script asserting a unique anchor match before replacing (an unmatched anchor aborts
rather than silently no-ops), and each changed region was re-read afterwards: `SongResolutionService.cs`
(usings, field + constructor, `ResolveAsync` head/tail and re-indented body, `CommitAsync` head/tail and
re-indented body, `ResolveOrCreateArtistIdAsync` signature), `SongResolutionServiceTests.cs` (`CreateSut`
factory + `MyVocaList.Tests.Infrastructure` using), `NestedUnitOfWorkTests.cs` (new test inserted between the
failure-tuple test and the REQ-UOW-09 test, inside the class). An accidental UTF-8 BOM introduced on
`SongResolutionService.cs` by the first script was detected by a byte-level check against
`git show HEAD:...` and removed - the file's on-disk encoding is unchanged from HEAD.

No test assertion was weakened, skipped, commented out or deleted. `UnitOfWorkTestHost.CreateLegacy()`
untouched. `Infra/UnitOfWork/UnitOfWork.cs` untouched in the commit. `ISongRepository.SaveChangesAsync`
retained for Task 2.4b. No file carrying `TODO [BUG-071 / UOW]` was opened. Build: **0 errors**, first
attempt; all 105 warnings pre-existing. The known `ObjectDisposedException: 'SQLitePCL.sqlite3'` flake did
not reproduce in any of the four runs.

### Design concern (non-blocking, carried forward from Task 2.3)
`_songRepository`, `_artistResolution` and `_songService` are now referenced **only** by the declaration and
the constructor assignment - the same residue Task 2.3 left on `ArtistResolutionService._artistRepository`.
The fields and their constructor parameters are removal candidates, but the constructor signature is a wider
surface than this task owns. Phase 3's VERIFY gate remains the right place to sweep the residue across all
pilot services at once.

---
## Task: 2.4b - Retire `ISongRepository.SaveChangesAsync`; confirm REQ-UOW-18 stopgap absent
**Plan:** `plan.md` (Phase 2, final task)
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

### Step 1 - REQ-UOW-18 stopgap check
`git merge-base --is-ancestor 1a114c1 HEAD; echo $?` -> **exit 1**.
Verbatim finding: *"stopgap absent on develop; REQ-UOW-18 satisfied vacuously (NB-4 case 2)"*.
Nothing deleted for it. Plan finding F3 confirmed.

### Changed files:
- `Domain/RepositoryInterface/ISongRepository.cs` - removed `Task SaveChangesAsync(CancellationToken ct = default);` (no doc comment attached).
- `Infra/Repository/SongRepository.cs` - removed the `/// <inheritdoc />` + `public Task SaveChangesAsync(...) => _db.SaveChangesAsync(ct);` implementation (present at `:144-146`, contrary to the pre-task census which reported zero hits in this file).
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` - removed the two authorised `_songRepoMock.Verify(r => r.SaveChangesAsync(...), Times.Never)` lines (`:533`, `:571`) and the comment block that existed solely to introduce them; condensed the surviving REQ-UOW-10 note to three lines that still point at `Bug068RegressionTests.CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow`.
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` - **scope exception, see below**.

### Scope exception (documented, not silent)
`NestedUnitOfWorkTests.cs` was **not** in the task's `Files owned` and was **not** in the pre-task census, but
the first build failed with `CS1061` at `:267`: the file's private `ISongRepository` test **fake** carried a
mechanical delegating stub `public Task SaveChangesAsync(...) => _inner.SaveChangesAsync(ct);`. Removing an
interface member makes such a forwarder uncompilable, so the retirement is not expressible without deleting it.
The deleted lines are a pass-through forwarder, **not** an assertion - no `Verify`, no `Assert`, no `Setup`
behaviour was touched, and the file carries no `TODO [BUG-071 / UOW]` marker. Recorded here rather than
resolved silently; flag for review if the exception is judged too wide.

### Authorised test-assertion deletion (narrow)
Deleting the member is a **stronger** guarantee than a runtime `Times.Never` assertion: the call becomes
impossible at compile time rather than merely unobserved. This is a narrow exception to `testing.md`'s
Builder-must-not-modify-tests rule, granted by Helder for exactly these two lines, and it does not extend to
any other assertion in the file. Neither host test became empty (both retain `Assert.True/NotNull` plus
surviving `_urlRepoMock.Verify` assertions); no dead locals resulted.

### Explicitly untouched
- `SongServiceTests.cs:535` (now `:532`) `_urlRepoMock.Verify(r => r.SaveChangesAsync(...), Times.Never)` on
  `ISongKaraokeUrlRepository` - retired in **Phase 4.3**, left intact and verified present after the edit.
- `ISongKaraokeUrlRepository.SaveChangesAsync` + its implementation and its `SongKaraokeUrlService` callers.
- `IBaseRepository<T>.SaveChangesAsync` (plan Task 4.2, out of scope).
- `Infra/UnitOfWork/UnitOfWork.cs`, `UnitOfWorkTestHost.CreateLegacy()`.
- All `Services/SongService.cs` hits (comments about already-deleted calls).
- No file carrying `TODO [BUG-071 / UOW]` was opened.

### AC traceability matrix

| AC ID | Criterion | Implementation | Test method |
|---|---|---|---|
| REQ-UOW-11 | No repository exposes its own save; the single commit is owned by `IUnitOfWork` | `ISongRepository.SaveChangesAsync` + `SongRepository` implementation deleted - the call is now a **compile error**, not a runtime observation | Compile-time enforcement (stronger than a test); atomicity itself asserted by `Bug068RegressionTests.CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow` and `NestedUnitOfWorkTests.CommitAsync_*` |
| REQ-UOW-18 | The interim stopgap is removed before Phase 2 closes | Vacuous - `1a114c1` is not an ancestor of `HEAD` (exit 1), so the stopgap never landed on this line of development | N/A (git-ancestry check, recorded verbatim above) |

### Post-edit verification
Every edit applied by a script asserting a **unique** anchor match before replacing (an unmatched anchor
aborts rather than silently no-ops), preserving each file's existing CRLF/BOM byte profile; each changed
region was re-read afterwards via `sed`/`tail`. Confirmed after edit: zero `_songRepoMock` save references
remain in `SongServiceTests.cs`; the `_urlRepoMock` save assertion survives; both edited test bodies still
end in a non-empty assertion block. A repo-wide `SaveChangesAsync` sweep confirms every remaining hit is
either `_db.SaveChangesAsync` (DbContext, legitimate) or `ISongKaraokeUrlRepository` (Phase 4.3).

### Build notes
Build: **passed (0 errors)** on the second attempt - the first failed only on the `NestedUnitOfWorkTests`
fake described above; all warnings pre-existing.
Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-restore` ->
**`Com falha: 0, Aprovado: 564, Ignorado: 0, Total: 564`** (7 s), exactly matching the `3896d253` baseline of
`Failed 0, Passed 564, Skipped 0, Total 564`. No regression; no test adjusted to fit. The known
`ObjectDisposedException: 'SQLitePCL.sqlite3'` flake did not reproduce.

### Gate
Phase 3 **not** started - HARD GATE requiring Helder's on-device confirmation.

---
## Task: Phase 3.2 — review-checklist verification (independent verifier)
**Plan:** `plan.md` (Phase 3.2)
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

Verification target: `feat/uow-pilot` HEAD **`f6670092`**. Documentation-only task — no `.cs` file was
edited. Suite re-confirmed independently: **`Failed 0, Passed 564, Skipped 0, Total 564`**.

### Verdicts

**1. REQ-UOW-31 (excluded files untouched) — PASS.**
`git diff develop --name-only` = 29 files; none of the seven excluded files is present. `git grep -n
"BUG-071" -- '*.cs'` finds markers in 8 files under `Services/`, `Infra/`, `MyVocaList/UI/` — the 3
excluded services, the 3 excluded repositories, plus `MyVocaList/UI/ViewModels/QueueManagementViewModel.cs:11`
and `Services/VenueService.cs:16` (Phase 4.4 / 4.6a work). None of them appears in the diff.

> Verifier note: the marker sweep also hits `Domain/UnitOfWork/IUnitOfWork.cs` and
> `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkLifetimeTests.cs` when run repo-wide (10 files, not 8).
> Both are the pilot's own artifacts, not excluded files, so the REQ-UOW-31 verdict is unaffected —
> recorded only so a future re-run of the same command is not surprised by the count.

**2. REQ-UOW-28 (collaborators resolved from `sp`, never from a constructor field) — PASS.**
All 11 wrap sites confirmed (`git grep -n "_uow.Execute" -- Services/`): `SongService` 4, `ArtistService` 3,
`ArtistResolutionService` 2, `SongResolutionService` 2 — the 9 wrapped mutating methods plus the 2
`ResolveAsync` reads. Every lambda resolves its collaborators from `sp`. The remaining
`_artistService` / `_artistResolution` occurrences are comment text only
(`ArtistResolutionService.cs:108`; `SongResolutionService.cs:183`, `:288`).

Structural improvement worth recording: `ResolveOrCreateArtistIdAsync`, `ApplyUpdate`, `ComputeFieldDiffs`
and both `DerivePrefixToken` overloads are now `private static`. A static helper cannot read an instance
field, so the compiler now **enforces** what the review previously had to check by eye.

**3. REQ-UOW-01 (no ad-hoc scope creation outside the UoW) — PASS.**
`Create(Async)?Scope()` across `Services/`, `MyVocaList/UI/ViewModels/`, `Infra/` returns only
`Infra/UnitOfWork/UnitOfWork.cs:42`, `:80`, `:106`. The documented exceptions `App.xaml.cs:35`, `:54` are
unchanged. All other hits are test infrastructure, out of the requirement's scope.

**4. REQ-UOW-11 (repositories do not own the save) — PASS.**
`ISongRepository` has no occurrence; `IArtistRepository.cs:56` is a retirement comment.
**3 of 5 remain, all Phase 4+:** `IBackupRepository` (Task 4.5), `ICatalogRepository` (Task 4.1),
`ISongKaraokeUrlRepository` (Task 4.3). `IBaseRepository.SaveChangesAsync()` is the separately-approved
deliberately-retained member (`design.md § 8`) and is **not** one of the five.

**5. REQ-UOW-10 — NOT MET as literally written. ESCALATED to gate 3.4.** See open decision item F1 below.

**6. REQ-UOW-35 / Revision-12 ambient-scope invariant — PASS.**
`_ambientScope` has exactly two publishing assignments (`UnitOfWork.cs:43`, `:81`) — both write paths.
`:69` / `:95` are `finally` null-resets and are correct: the join branches return early, so publication
only ever happens from a null state. `ExecuteReadAsync` (`:98-109`) never publishes and never saves.
`FlushAsync` (`:118-134`) calls `SaveChangesAsync` with no `CommitAsync` and no transaction disposal, and
throws when no ambient scope exists (fail-closed).

**7. Behaviour changed vs merely relocated — PASS, no findings.**
Every removed line with no matching addition is either a `_field.Method(…)` → `local.Method(…)` rename or a
deleted `SaveChangesAsync` (3 in `SongService`, 2 in `ArtistService`, 2 in `ArtistResolutionService`). No
validation rule, branch, message string or ordering was altered. The deferred-key hazard was specifically
checked: UI call sites `ArtistFormViewModel.cs:156` and `SongFormViewModel.cs:554` both discard the returned
entity with `_` — no exposure.

### Open decision item for gate 3.4 — F1: REQ-UOW-10 line-count reading

REQ-UOW-10 states *"at most one line of code per service method; two or more lines of ceremony per service
method fails this criterion."* `design.md:857` scores the wrap as *"+1 line each… still the same line every
time"* — i.e. counting `_uow.ExecuteAsync(async sp => { … })` as **one logical line**. Under that reading
all 11 methods pass, and the `sp.GetRequiredService` lines are *substitutions* for constructor-field reads
(each paired with a removed `_field.` usage), not additions. Under a **literal physical-line count**, none
passes — every method adds 4–7 physical lines (3 wrapper lines + 1–4 `sp.GetRequiredService` lines).

**Root cause:** `design.md`'s "+1 line" accounting predates **Revision 10** (the `Func<IServiceProvider,…>`
shape) and **REQ-UOW-28**, which together *mandate* the `sp.GetRequiredService` lines. The DRY score was
never revised for them. `ArtistResolutionService.CommitAsync`'s `await _uow.FlushAsync(ct)` is
**substance (REQ-UOW-35), not ceremony**, and should not count against the budget.

Per-method count (verifier's table):

| Service | Method | Ceremony lines | `sp.GetRequiredService` lines | Other new executable lines |
|---|---|---|---|---|
| SongService | `CreateSongAsync` | 3 | 2 | 0 |
| SongService | `UpdateSongAsync` | 3 | 1 | 0 |
| SongService | `CreateSongWithUrlsAsync` | 3 | 4 | 0 |
| SongService | `DeleteSongsAsync` | 3 | 1 | 0 |
| ArtistService | `CreateArtistAsync` | 3 | 1 | 0 |
| ArtistService | `UpdateArtistAsync` | 3 | 1 | 0 |
| ArtistService | `DeleteArtistsAsync` | 3 | 2 | 0 |
| ArtistResolutionService | `ResolveAsync` | 3 | 1 | 0 |
| ArtistResolutionService | `CommitAsync` | 3 | 2 | 1 (`await _uow.FlushAsync(ct)`) |
| SongResolutionService | `ResolveAsync` | 3 | 2 | 0 |
| SongResolutionService | `CommitAsync` | 3 | 3 | 0 |

Plus **5 shared class-level lines per file** (2 usings, field, ctor param, ctor assignment) — per file, not
per method.

**Decision required from Helder at gate 3.4:** logical-line reading (all pass, REQ-UOW-10 text needs
clarifying) or physical-line reading (REQ-UOW-10 is not met and the target must be renegotiated).
**Follow-up, required either way:** `design.md`'s DRY-score paragraph (`:857`) must be corrected so a future
reader is not misled by the stale "+1 line each" accounting. `requirements.md` / `design.md` were
deliberately **not** edited in this task — the correction depends on the 3.4 outcome.

### Non-blocking findings

**F2 — dead constructor-only fields, broader than previously reported (10, not 4).**

| Service | Dead fields |
|---|---|
| `SongService` | `_artistRepository`, `_urlRepository`, `_urlService` |
| `ArtistService` | `_catalogRepository`, `_songRepository`, **`_logger`** |
| `ArtistResolutionService` | `_artistRepository` |
| `SongResolutionService` | `_songRepository`, `_artistResolution`, `_songService` |

`ArtistService._logger` is now **entirely** unused — the one most likely to trip an analyzer. All are
harmless today, but they inflate constructor arity and keep captive window-scope references alive. Each
removal changes constructor arity and therefore touches the matching `Unit/Services/*Tests.cs`; a sweep, if
done, belongs in **one dedicated task**, not folded into Phase 4.

**F3 — undocumented REQ-UOW-28 exception.** `_scorer` (`ISimilarityScorer`) is read from inside both
`ExecuteReadAsync` lambdas (`ArtistResolutionService.cs:80`, `SongResolutionService.cs:142`). Verified
benign: `Infra/Similarity/SimilarityScorer.cs` is a stateless pure function over `FuzzySharp` — no fields,
no `AppDbContext` — so it cannot defeat the join. But it is a **third** accepted exception (alongside `_uow`
and `_logger`) that no spec line or code comment acknowledges. One line in REQ-UOW-28's exception list
would close it.

### Tooling hazard (recorded for future sessions)

The `rtk` grep proxy returned **wrong results** during this verification: `grep -c '^+'` reported 0 for all
four service files, and an `_ambientScope` search came back as a mangled summary. Every
completeness-critical check was redone with `git grep` / Python. The same hazard bit the Task 2.4b caller
census. **Do not trust a single `grep` for byte-exact or completeness-critical work.**

### Gate

**Phase 3.3 remains OPEN — Helder's on-device gate. Phase 4+ must not start.**

### Changed files:
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/task-log.md` (this entry only)

### Build notes
Documentation-only task — no build or test run required; no source file touched. Suite state re-confirmed
from the Phase 3.2 verification run at `f6670092`: `Failed 0, Passed 564, Skipped 0, Total 564`.
No new file created, so no `.sln` registration is required.

---
## Task: Merge `develop` into `feat/uow-pilot` (takes on `feat/inline-artist-create`)
**Plan:** `plan.md` (Phase 2 / REQ-UOW-18)
**Status:** To Review
**Started:** 2026-08-18
**Completed:** 2026-08-18

`feat/inline-artist-create` (BUG-050/051/052/054-058/060/061/064/067, REQ-ACREATE-03) had never been
merged to any branch; it landed on `develop` at `71926980` and is taken on here.

### Conflicts resolved
1. `Services/SongService.cs` — BUG-067's `int? artistId` / `effectiveArtistId` / artist-existence check /
   uniqueness-against-effective-artist / `song.ArtistId = effectiveArtistId` re-expressed INSIDE the
   `_uow.ExecuteAsync` lambda, with `IArtistRepository` resolved from `sp` (REQ-UOW-28).
2. `Services/SongResolutionService.cs` — HEAD's wrapped body kept; develop's unwrapped duplicate
   discarded. Both `UpdateSongAsync` call sites switched to the NAMED `ct: ct` argument: the new
   `int? artistId` parameter sits before `ct`, so the pre-merge positional `ct` would have silently bound
   to `artistId`.
3. / 4. `SongServiceTests.cs`, `SongResolutionServiceTests.cs` — using/BOM only; both sides' tests kept,
   none weakened or deleted.
5. `task-log.md` — union, chronological.

### REQ-UOW-18 — stopgap DELETED (NB-4 case 1)
The `ChangeTracker.Entries<Song>()` guard is unreachable under the unit of work (fresh context per write),
so its deletion is behaviour-neutral. Proven by `SongServiceUpdateIntegrationTests` (both facts green after
deletion) plus the full suite.

### Defect the stopgap was masking (NEW — needs Helder's ruling)
`SongRepository.GetByIdAsync` eager-loads `OriginalArtist` (BUG-055). In a fresh unit-of-work context the
Song is untracked, so `DbSet.Update` attaches the graph and EF FK-fixup rewrites `ArtistId` back from the
stale navigation — BUG-067's artist change was silently discarded (verified by a throw-away diagnostic:
`state=Modified ArtistId=1 ArtistIdModified=True curr=1` after setting `ArtistId = 2`). Pre-merge this was
masked by the stopgap's `CurrentValues.SetValues` (scalars/FK only, no navigation graph). Fixed by
`song.OriginalArtist = null;` before the write in `SongService.UpdateSongAsync`. **Open question:** service
vs `SongRepository.UpdateAsync` as the right home for that detach.

### Changed files:
- `Services/SongService.cs`
- `Services/SongResolutionService.cs`
- `Infra/Repository/SongRepository.cs`
- `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (migrated to `UnitOfWorkTestHost`
  — its hand-built `SongService(...)` ctor and `ISongRepository.SaveChangesAsync` no longer exist; both ACs
  preserved verbatim)
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`, `.../SongResolutionServiceTests.cs`
- `Docs/.../requirements.md`, `.../task-log.md`

### Build notes
Build: passed (0 errors, 101 warnings). Tests: **Failed 0, Passed 590, Skipped 0, Total 590**
(pre-merge branch 564; new `develop` 585 with 2 pinned REDs — `NestedUnitOfWorkTests` are GREEN here).
BUG-076 flake (`ObjectDisposedException: 'SQLitePCL.sqlite3'`) hit `SaveSkipTests` on the first run and did
not reproduce on the two subsequent full runs. Android target not built (APK file lock, `XARDF7024`).
REQ-UOW-28 re-verified across all 11 lambda bodies (comment-stripped brace-scan) — 0 violations; all three
`SongResolutionService` private helpers are `static`, so a captured field is compiler-impossible.


---

## Gate 3.4 — Helder's decisions (2026-08-18)

Both items the Phase 3.2 verifier escalated are now resolved. Recorded here because Phase 3.5 and
Phase 4+ write new call sites against them.

### F1 — REQ-UOW-10 line-count reading: **RESOLVED — logical-line, as a guideline not an AC**

**Helder's decision (verbatim):** *"I suppose that's not something to lock once someday more than 1
line will be needed. Perhaps the prefered pattern is 1 logical-line reading, but more is allowed as
well. I am realy not sure about this rules need, except if clean code is the reason, or avoid commands
that isn't added into services methods."*

**Resolution.** REQ-UOW-10 is demoted from a binding acceptance criterion to a design guideline:

- The **preferred** shape is one logical line — `_uow.ExecuteAsync(async sp => { … })`.
- **More is allowed with reason.** Two reasons are pre-sanctioned, so neither needs re-litigating:
  1. `await _uow.FlushAsync(ct)` for deferred-key materialisation (REQ-UOW-35) — the Phase 3.2
     verifier correctly classified this as *substance, not ceremony*.
  2. `sp.GetRequiredService<T>()` lines — each is a **substitution** for a removed `_field.` read
     (mandated by REQ-UOW-28), not an addition.

**Why the rule existed, and why demotion is correct.** REQ-UOW-10 was a *design-selection* criterion,
not a coding rule: it made "minimal repeated code" (the parent item's stated goal) machine-checkable so
the choice between Candidates A/B/C was objective rather than a matter of taste — it is what made
Candidate B's rejection defensible (160 signature edits). That job ended when Candidate C shipped. Left
as a binding AC it now misfires on its own successor phases, penalising the very lines REQ-UOW-28 and
REQ-UOW-35 require. The anti-ceremony *intent* is retained as a guideline; the failing *test* is not.

**Consequential edit required (unchanged by this decision):** `design.md:857`'s "+1 line each … still
the same line every time" accounting predates Revision 10 and REQ-UOW-28 and is stale either way. It
must be corrected so a future reader is not misled.

### D13 — final `IUnitOfWork` API shape: **RESOLVED — provisional shape ratified as FINAL**

**Helder's decision:** confirm the provisional shape as final; no typed `TRepo` overload.

The surface below is now the settled contract. Phase 3.5 and Phase 4+ write against it once:

```csharp
Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
Task          ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default);
Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default);
Task          FlushAsync(CancellationToken ct = default);
```

`FlushAsync` was added mid-pilot under REQ-UOW-35 and is part of the ratified surface — it is not a
provisional addition awaiting separate approval.

**Follow-up:** the `PROVISIONAL shape (D13)` doc-comment on `Domain/UnitOfWork/IUnitOfWork.cs` must be
updated to record ratification, so the source stops advertising an open decision that is now closed.

### Evidence corrections carried into Phase 3.5

The 2026-08-04 notes were re-verified against current HEAD and several claims have drifted:

| Prior claim | Current truth |
|---|---|
| `TODO [BUG-071 / UOW]` markers ~14 | **26**, across 8 source files |
| `QueueService.cs:134` saves on `_venueRepository` | Call is real but at **`:151`** |
| `Infra/Repositories/QueueRepository.cs:56,67,93` | Saves at **`:58,71,99`** (TODOs at `:56,69,97`) |
| `Infra/Repositories/EventRepository.cs:66,77` | Saves at **`:68,81`** (TODOs at `:66,79`) |
| DI registration in `MauiProgram.cs` | **`MyVocaList/Extensions/ServiceCollectionExtensions.cs`** |

Line numbers from the 2026-08-04 notes must not be used as coordinates.

---

## 2026-08-24 — Phase 3.5 dissolved; Phase 4+ unblocked (orchestrator, docs-only)

**Status:** `Phase 3.5 closed — no code, no tests, docs reconciliation only.`

Triggered by Helder confirming his manual tests on the UOW work passed and asking whether any step
remains. Answer: yes — Phase 4+ — but its stated blocker had silently evaporated four days earlier.

### What changed in the tree (not by this entry — by the Event/Queue deletion, `32e7a85e`)

| Phase 3.5 obligation | Current truth on develop |
|---|---|
| Delete `Infra/Repositories/` (plural, 2 files, 6 embedded saves) | Folder does not exist. `Infra/Repository/` (singular) is the only one, 8 files. |
| Migrate Queue/Event services to UoW | Those services are deleted. Only Event *infra definitions* were kept (entities, EF configs, `DbSet`s, migrations) — none carry a save. |
| Trap: `Event` in two entity namespaces with different shapes | Collapsed to one. |
| Trap: `QueueServiceNew`/`EventService` have zero saves of their own | Cannot fire — neither service exists. |
| 26 `TODO [BUG-071 / UOW]` markers across 8 files | **Zero** remain in production code. The only surviving `BUG-071` mention is a comment in `MyVocaList.Tests/Integration/UnitOfWork/UnitOfWorkLifetimeTests.cs:179`, explaining what an assertion is guarding — correct as-is, do not delete. |

The `PROVISIONAL shape (D13)` doc-comment follow-up recorded in the previous entry is also already
discharged — no occurrence of `PROVISIONAL` survives in any `.cs` file.

### Consequence

Phase 4+'s gate (*"startable only after Phase 3 passes and Phase 3.5 lands"*) is fully satisfied:
Phase 3 passed at Helder's emulator gate (`cdec7af5`), Phase 3.5 is discharged rather than deferred.
**Phase 4+ is the only remaining UoW work and is now dispatchable**, subject to the caveat below.

### Caveat carried forward — plan coordinates are stale, re-verify before dispatch

`plan.md` Phase 4+ was written on 2026-08-04 and predates both the pilot and the deletion:

- **4.6a** named three ViewModels; `QueueSongPickerViewModel` and `QueueManagementViewModel` are
  deleted. Only `PersonPickerViewModel` survives.
- **4.2**'s justification for *retaining* `IBaseRepository<T>.SaveChangesAsync()` was that its only
  surviving callers sat in the excluded `Services/QueueService.cs`. That file is gone, so the
  justification lapses — the retain/remove decision must be re-taken on current evidence, not
  inherited.
- The line numbers throughout Phase 4+ are 2026-08-04 coordinates. As with the earlier evidence
  corrections in this log, **they must not be used as coordinates** — re-locate each edit site.

### Ordering note for the artist-catalog work

Phase **4.1 is `CatalogService`**, and it is a prerequisite for the separately-registered item
`BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`.
`CatalogService` today injects `ICatalogRepository` and calls `_catalogRepository.SaveChangesAsync(ct)`
directly — it has no `IUnitOfWork` dependency at all. `SongService`'s write methods are all wrapped in
`_uow.ExecuteAsync`, and REQ-UOW-28 requires everything used inside that lambda to be resolved from the
lambda's own `sp`. Propagating a song write into the catalog therefore cannot reuse `CatalogService`
as it stands without either violating REQ-UOW-28 or opening a second, uncoordinated commit boundary
inside a transaction that already has one. **4.1 first, then the propagation spec.**

### Changed files

- `plan.md` — Phase 3.5 heading rewritten to RESOLVED with the dissolution record; historical body
  retained beneath it; Phase 4+ gate struck through and marked UNBLOCKED.
- `README.md` — `gate:` frontmatter updated.
- `task-log.md` — this entry.

### Verification evidence

- `ls Infra/Repositories/` -> `No such file or directory`; `ls Infra/Repository/` -> 8 files.
- `grep -rn "BUG-071" --include=*.cs` -> 1 hit, the test comment named above.
- `grep -rn "PROVISIONAL" --include=*.cs` -> 0 hits.
- No production code touched by this entry; suite unaffected (575/575 green on develop at `2b6fc488`).

---

## Task 4.1 — `CatalogService` wrapped in `IUnitOfWork`

**Status:** To Review
**Branch:** `feat/uow-phase-4-1-catalog` (worktree `MyVocaList-uow-catalog`)

Wrapped `AddSongToCatalogAsync` and `RemoveSongFromCatalogAsync` in `_uow.ExecuteAsync`, following the
`SongService.CreateSongAsync` / `ArtistService.DeleteArtistsAsync` shape exactly: `ICatalogRepository`
resolved from the lambda's own `sp` (REQ-UOW-28), never the constructor field. `RemoveSongFromCatalogAsync`
calls `ICatalogRepository.RemoveAsync`, which is `ExecuteDeleteAsync`-based — the explicit transaction
`IUnitOfWork.ExecuteAsync` opens now brings it under the unit of work (REQ-UOW-33), matching
`ArtistService.DeleteArtistsAsync`'s comment for the same pattern.

`GetPagedCatalogForArtistAsync` was left as a direct `_catalogRepository` call, unwrapped — this matches
the already-migrated `SongService.GetPagedSongsForListAsync` / `ArtistService.GetPagedArtistsForListAsync`,
neither of which routes single-repository list reads through `ExecuteReadAsync`. That overload is reserved
for multi-repository read-joins (`ArtistResolutionService`, `SongResolutionService`), which this method is
not.

Retired `ICatalogRepository.SaveChangesAsync` / `CatalogRepository.SaveChangesAsync` (REQ-UOW-11) — the
single save is now owned by `IUnitOfWork` (REQ-UOW-10: one line of ceremony per service method, zero per
repository method).

### AC traceability

| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-UOW-10 | ≤1 line of UoW ceremony per service method, 0 per repository method | `CatalogService.cs` — both write methods are a single `_uow.ExecuteAsync(...)` expression body | Reviewer-checked diff, this entry |
| REQ-UOW-11 | Retire the standalone `SaveChangesAsync` pass-through | `ICatalogRepository.cs` / `CatalogRepository.cs` — member deleted | Compile-level: build green with no remaining references |
| REQ-UOW-28 | Lambda resolves collaborators from its own `sp`, never a ctor field | `CatalogService.cs` both lambdas — `sp.GetRequiredService<ICatalogRepository>()` | Reviewer-checked; existing `CatalogServiceTests` pass against `PassthroughUnitOfWork` |
| REQ-UOW-33 | `ExecuteDeleteAsync` path is transactional via `IUnitOfWork`'s explicit transaction | `RemoveSongFromCatalogAsync` → `ICatalogRepository.RemoveAsync` (`ExecuteDeleteAsync`) inside `_uow.ExecuteAsync` | Covered by the shared `IUnitOfWork` implementation's own REQ-UOW-33 tests (`Infra/UnitOfWork/UnitOfWork.cs`); no new test needed — this task only routes an existing `ExecuteDeleteAsync` call through the already-tested transactional boundary |
| REQ-UOW-24/26/27 | Not applicable here — no-signal/unrecognised-result branches are exercised by `IUnitOfWork`'s own tests, not per-service | `CatalogService`'s two wrapped methods both use the `ValueTuple`-with-leading-`bool` shape | n/a — construction, not new behaviour |

### Changed files

- `Services/CatalogService.cs` — constructor takes `IUnitOfWork uow`; `AddSongToCatalogAsync` and
  `RemoveSongFromCatalogAsync` wrapped in `_uow.ExecuteAsync`; own `SaveChangesAsync` call deleted.
- `Domain/RepositoryInterface/ICatalogRepository.cs` — `SaveChangesAsync` member removed.
- `Infra/Repository/CatalogRepository.cs` — `SaveChangesAsync` implementation removed.
- `MyVocaList.Tests/Unit/Services/CatalogServiceTests.cs` — `CreateSut` now passes
  `PassthroughUnitOfWork.Over(_catalogRepoMock)`; removed the now-nonexistent
  `_catalogRepoMock.Setup(r => r.SaveChangesAsync(...))` line.
- `MyVocaList.Tests/Integration/Repositories/CatalogRepositoryTests.cs` — all 9 call sites (not just the
  2 the plan's stale coordinates named) changed from `_repo.SaveChangesAsync()` to `_db.SaveChangesAsync()`.

### Verification evidence

- `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj --no-restore` → `0 errors, 107 warnings` (pre-existing warning baseline, none introduced by this change).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-build --no-restore`:
  - First run: 574/575 passed, 1 failure — `PersistedStringTrimmingTests.PersonEmail_WhitespaceOnly_PersistedAsNull`, `ObjectDisposedException` on `SQLitePCL.sqlite3` during `EnsureCreatedAsync` — matches the documented BUG-076 flake, unrelated to `CatalogService`/`CatalogRepository`.
  - Re-run: **575/575 passed**, 0 failures.
- Files re-read after edit to confirm persistence: `Services/CatalogService.cs`,
  `Domain/RepositoryInterface/ICatalogRepository.cs`, `Infra/Repository/CatalogRepository.cs`,
  `MyVocaList.Tests/Unit/Services/CatalogServiceTests.cs`.

### Out of scope, found during this task

None — no scope bleed into 4.2–4.7 or any other service.

---

## Task: UoW Phase 4.3 — wrap `SongKaraokeUrlService` in `IUnitOfWork`

**Plan:** `plan.md` § Phase 4+, Task 4.3 · **Status:** merged to develop (`fcc1509e`) · **Date:** 2026-08-24

Written to develop by the orchestrator: three implementors ran this wave in parallel worktrees and were
briefed NOT to touch `Docs/`, since a shared task-log is a single-writer file and would have collided.

### Changed files

- `Services/SongKaraokeUrlService.cs` — `AddUrlAsync` / `RemoveUrlAsync` / `RecordPlayAsync` wrapped.
- `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs` — `SaveChangesAsync` member retired.
- `Infra/Repository/SongKaraokeUrlRepository.cs` — its implementation retired.
- `MyVocaList.Tests/Unit/Services/SongKaraokeUrlServiceTests.cs` — `CreateSut` via `PassthroughUnitOfWork.Over(_repoMock)`.
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` — dropped one `Verify` against the retired member (`SongService` itself untouched).
- `MyVocaList.Tests/Integration/Repositories/SongKaraokeUrlRepositoryTests.cs` — direct repo saves redirected to `_db.SaveChangesAsync()`.
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` — the `ThrowOnAddUrlRepository` test decorator no longer implements the retired member.

### Notable decisions

- **`RecordPlayAsync` uses the no-signal overload** `ExecuteAsync(Func<IServiceProvider, Task>, ct)`
  (REQ-UOW-26) rather than the tuple-returning one — it has no failure mode, so there is no `bool` for
  the save-skip convention to read. It is the only in-scope method of this shape.
- **Validation stays outside the transaction.** `AddUrlAsync` rejects a malformed YouTube URL before
  entering `ExecuteAsync`, returning via `Task.FromResult`. A rejected input therefore never opens a
  transaction — correct, and worth preserving if this method is edited later.
- **Three `Moq` `Verify` calls against `SaveChangesAsync` were removed, not weakened.** Retiring the
  interface member makes those `Verify` expressions uncompilable; there is no way to keep them. The
  guarantee they encoded — that the URL repository never commits on its own — is asserted against a
  real context by `Bug068RegressionTests.CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow`, and the
  surviving comment in `SongServiceTests` was updated to point at it rather than left stale.

### Verification evidence

- Implementor: build 0 errors; `575/575` green.
- **Orchestrator re-verified independently** before merging: `dotnet build … --no-restore` -> 0 `error CS`;
  `dotnet test … --no-build --no-restore` -> `Com falha: 0, Aprovado: 575, Total: 575`.
- **BUG-076 fired twice** during the implementor's runs, on two *different* tests
  (`NestedUnitOfWorkTests…LeavesPartialArtistRow`, then `PersonRepositoryTests.InitializeAsync`), each
  time the known `ObjectDisposedException` on `SQLitePCL` during `EnsureCreated`, each time passing on
  re-run. Recorded, not "fixed" and not used to excuse a failure. This is now the clearest evidence yet
  that BUG-076 is schedule-sensitive and test-independent.

### File-collision note for the wave

This task edited `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`, which the Phase 4.2
briefing also named as owned. The overlap was survivable only because the edits are in different
members. **Both 4.2 and 4.4 name that same file** — it is effectively a shared fixture for the whole
Phase 4 rollout and should be treated as sequential-only for the remaining tasks.

---

## Task: UoW Phase 4.5 — wrap `BackupService.CreateFullBackupAsync` in `IUnitOfWork`

**Plan:** `plan.md` § Phase 4+, Task 4.5 · **Status:** merged to develop (`e7b0b1c…` see LEDGER) · **Date:** 2026-08-24

### Changed files

- `Domain/ServicesInterfaces/IBackupService.cs` — `BackupResult : IUnitOfWorkOutcome`.
- `Services/BackupService.cs` — `CreateFullBackupAsync` wrapped; `PruneOldSnapshotsAsync` converted to a static helper.
- `Domain/RepositoryInterface/IBackupRepository.cs` + `Infra/Repository/BackupRepository.cs` — `SaveChangesAsync` retired.
- `MyVocaList/MauiProgram.cs` — one line in the existing `BackupService` factory lambda.
- `MyVocaList.Tests/Unit/Services/BackupServiceTests.cs` — `CreateSut` via `PassthroughUnitOfWork`.
- `MyVocaList.Tests/Integration/Repositories/BackupRepositoryTests.cs` — 4 call sites to `_db.SaveChangesAsync()`.
- `MyVocaList.Tests/Integration/UnitOfWork/SaveSkipTests.cs` — the two new tests this task owns.

### The fail-closed trap, and why this task was unsplittable

`IUnitOfWork` decides whether to commit by inspecting the body's return value, and under fail-closed an
unrecognised return type throws. `BackupResult` did not implement `IUnitOfWorkOutcome`, so wrapping the
method *before* adding the marker would have made **every** call to `CreateFullBackupAsync` throw
immediately — with a green build, since the failure is a runtime type check. Marker and wrap landed in
one commit. Anyone splitting this later re-introduces the same trap.

### `PruneOldSnapshotsAsync` — a REQ-UOW-28 consequence worth naming

It previously read the constructor's `_repo` field, and it is called from *inside* the wrapped lambda.
Left as-is it would have been a textbook REQ-UOW-28 violation: the lambda would have committed through
the scope's context while pruning through the longer-lived one. It is now `static`, taking
`IBackupRepository` as a parameter, which makes the violation impossible to reintroduce by accident
rather than merely absent today. **This is the pattern to copy** wherever a private helper is reachable
from a wrapped body.

`_repo` is deliberately retained as a field: `ExportBundleAsync`, `RestoreFromBundleAsync`,
`GetHistoryAsync` and `HasRecentBackupAsync` are out of this task's scope and still use it.

### Tests added (both owned by this task per the spec)

- `BackupService_CreateFullBackupAsync_FailureResult_DoesNotPersistHistoryRow` — REQ-UOW-24's
  `IUnitOfWorkOutcome` exemplar against a **real** `BackupResult`, fault-injecting `ITransactionLogWriter`
  after the `AddAsync` mutation, proving a failure result skips the save.
- `BackupService_CreateFullBackupAsync_SuccessResult_DoesNotThrowAndPersistsHistoryRow` — REQ-UOW-27's
  positive counterpart, proving the wrap does not throw once the marker is present. Without this one the
  failure test would pass just as happily if the wrap were broken outright.

### Verification evidence

- Implementor: build 0 errors; `577/577` (baseline 575 + 2 new).
- **Orchestrator re-verified independently** before merging: 0 `error CS`;
  `Com falha: 0, Aprovado: 577, Total: 577`. Re-run again on develop after the merge: `577/577`.

### Out of scope, confirmed untouched

**BUG-073** — `RestoreFromBundleAsync` does `File.Copy(snapshotFile, _dbPath, overwrite: true)` on the
path `AppDbContext`'s connection string points at, with no dispose and no connection close. It is a
file-handle defect, not a unit-of-work one, is registered separately, and was deliberately excluded.

---

## Task: UoW Phase 4.2 — wrap `PersonService` in `IUnitOfWork`

**Plan:** `plan.md` § Phase 4+, Task 4.2 · **Status:** merged to develop · **Date:** 2026-08-24

### Changed files

- `Services/PersonService.cs` — `CreatePersonAsync` / `UpdatePersonAsync` / `DeletePersonsAsync` wrapped.
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` — `CreateSut` via `PassthroughUnitOfWork.Over(_repoMock)`.

`Bug068RegressionTests.cs` needed no edit — no `Skip` was present, and its
`Person_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` test turned out to be the thing that
caught the defect below.

### A real latent defect, exposed by the migration — the most important finding of this wave

This task did not merely wrap three methods. Wrapping `UpdatePersonAsync` **exposed a silent
persistence failure that already existed in the design and had no failing test**:

> Under a fresh per-unit-of-work context, `GetByIdAsync` (backed by `FindAsync`) returns a
> **detached** entity — nothing is tracked yet in that new context, and the harness runs
> `QueryTrackingBehavior.NoTracking`. Mutating that entity and relying on the unit of work's save is a
> **silent no-op**: no exception, no tracking conflict, no failing test. The update simply does not
> persist.

Fixed with an explicit `personRepository.UpdateAsync(person)` after mutation, mirroring
`ArtistService.UpdateArtistAsync`, which already had it. The implementor confirmed RED against
baseline (`git stash` of the fix) before going GREEN — no test was edited to make this pass.

**Why this matters beyond `PersonService`.** This is the mirror image of the BUG-067 hazard the pilot
found: there, a tracked graph caused EF fixup to *revert* a value; here, an untracked entity causes a
mutation to *vanish*. Both are invisible without an explicit `UpdateAsync`. Any service whose update
method follows read-mutate-save is suspect, and **each remaining Phase 4 task must return an explicit
verdict on it rather than assume**. This finding was written into the Phase 4.4 briefing, together
with the Phase 4.5 *helper* variant (a private method reading a constructor `_field` while called from
inside a wrapped lambda).

### `SaveChangesAsync` caller census (as of this task)

`QueueService.cs` — the plan's cited justification for retaining `IBaseRepository<T>.SaveChangesAsync()`
— is confirmed deleted, so that justification has lapsed. The member is nonetheless still genuinely
reachable: `VenueService` (3 calls, Phase 4.4, not yet migrated at the time of this census) plus the
then-pending 4.3/4.5 services. `Infra/UnitOfWork/UnitOfWork.cs`'s three `context.SaveChangesAsync`
calls are the pattern's own commit and are **not** repository callers — do not conflate them when
deciding on removal.

Correct action, unchanged from the plan's conclusion even though its reasoning lapsed: retain the
member; let the last Phase 4 task establish the final census and let Helder decide on removal.

### Verification evidence

- Implementor: build 0 errors; `575/575` on its branch (pre-4.5 baseline), no BUG-076 flake on the final run.
- **Orchestrator re-verified independently** after merging into develop: `577/577` green
  (575 + Phase 4.5's two new tests).

---

## Task: UoW Phase 4.4 — wrap `VenueService` in `IUnitOfWork` (last service in the rollout)

**Plan:** `plan.md` § Phase 4+, Task 4.4 · **Status:** merged to develop · **Date:** 2026-08-24

### Changed files

- `Services/VenueService.cs` — `CreateVenueAsync` / `UpdateVenueAsync` / `DeleteVenuesAsync` wrapped.
- `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs` — construction updated.

`Bug068RegressionTests.cs` needed no edit: its
`Venue_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` characterization test already used
`UnitOfWorkTestHost.Create()` and passed unchanged.

### Both traps checked explicitly — both verdicts negative

The briefing required a stated verdict rather than an assumption, since Phase 4.2's defect was invisible
without one.

- **Detached-entity trap: NOT present.** `UpdateVenueAsync` already called
  `await _venueRepository.UpdateAsync(venue)` before the wrap, so the mutation was never relying on
  tracking. `VenueRepository` does inherit the `FindAsync`-based `GetByIdAsync`, so the *structural*
  hazard is identical to `PersonService`'s — it was simply already neutralised here. The explicit call
  was carried through the wrap unchanged and now carries an explanatory comment, so a future edit that
  removes it will read as obviously wrong rather than harmless.
- **Helper variant: NOT present.** The only private helper, `BuildDeleteResultMessage`, operates purely
  on in-memory `List<(int, …)>` data and touches no repository or service field.

### The plan's dead-field instruction was already satisfied

Task 4.4 also called for deleting a dead `IEventRepository _eventRepository` field. It is **already
absent** — removed with the Event/Queue deletion on 2026-08-20. The constructor arity changed in this
task only from adding `IUnitOfWork`. Another instance of the plan's 2026-08-04 coordinates being stale.

### Verification evidence

- Implementor: build 0 errors; `577/577`.
- **Orchestrator re-verified independently**, and did not rely on the agent's grep: `rtk` silently
  rewrites `grep` and returned "0 matches" for patterns that demonstrably exist, so the file was
  re-inspected by direct read. Confirmed all three methods wrapped, every collaborator resolved from
  `sp`, and the explicit `UpdateAsync(venue)` present at `VenueService.cs:116`.

---

## Milestone: the service-layer rollout is COMPLETE — `SaveChangesAsync` census

With Phase 4.4 merged, **every service in the app writes through `IUnitOfWork`.** The orchestrator ran
an independent tree-wide census (a direct file walk, not `grep`, for the reason above):

| Site | Verdict |
|---|---|
| `Domain/RepositoryInterface/IBaseRepository.cs:18` (declaration) + `Infra/Repository/BaseRepository.cs:76-78` (implementation) | The member itself — **zero production callers remain** |
| `Infra/UnitOfWork/UnitOfWork.cs:52, :87, :132` | `DbContext.SaveChangesAsync` — **the pattern's own commit.** NOT a repository caller. Must never be conflated with the member above |
| `MyVocaList.Tests/Integration/Repositories/VenueRepositoryTests.cs` (10 sites) | Test-only, uses `_repo.SaveChangesAsync()` — the last file not yet on the `_db.SaveChangesAsync()` idiom |
| `PersonServiceTests.cs:348,:373`, `VenueServiceTests.cs:126,:174,:216` | Dead Moq `Setup` calls against the member |
| All other `_db.SaveChangesAsync()` sites in integration tests | `DbContext` directly — a different member, unaffected |

**Consequence.** `IBaseRepository<T>.SaveChangesAsync()` is now a dormant escape hatch: nothing calls
it, but while it exists any future code can commit outside a unit of work — precisely the BUG-068 /
BUG-071 defect class. Retiring it converts the rule from a convention into a compiler-enforced
constraint. Dispatched as its own task under Helder's directive that nothing may sit outside the
pattern. The plan had deferred this decision to "whichever Phase 4 task lands last"; this is that point.

---

## Task: Retire `IBaseRepository<T>.SaveChangesAsync()` — the pattern becomes compiler-enforced

**Status:** merged to develop · **Date:** 2026-08-24 · Dispatched under Helder's directive that the
entire app follow the unit-of-work pattern with nothing left outside it.

### Why

After Phases 4.1-4.5 the member had **zero production callers**, but it still existed. A dormant escape
hatch is not harmless: while the member is on the interface, any future code can commit outside a unit
of work and the compiler will not object — exactly the BUG-068 / BUG-071 defect class, re-openable at
any time by a well-meaning edit. Deleting it converts the rule from a convention that must be
remembered into a constraint that cannot be violated.

### The distinction that had to be got right

- **Removed:** `IBaseRepository<T>.SaveChangesAsync()` — declaration + `BaseRepository` implementation.
- **Untouched:** `Infra/UnitOfWork/UnitOfWork.cs:52, :87, :132`, which call
  `DbContext.SaveChangesAsync`. That **is** the pattern's own commit. Conflating the two and removing
  those would break every write in the application while still compiling.

This was called out explicitly in the briefing because the two read identically at a glance.

### Changed files

- `Domain/RepositoryInterface/IBaseRepository.cs` — member deleted.
- `Infra/Repository/BaseRepository.cs` — implementation deleted (a `// The subsequent SaveChangesAsync
  will persist…` comment remains and is correct; it refers to the unit of work's save).
- `MyVocaList.Tests/Integration/Repositories/VenueRepositoryTests.cs` — 10 sites moved from
  `_repo.SaveChangesAsync()` to `_db.SaveChangesAsync()`, the idiom every other repository test class
  already used. This was the last file on the old idiom.
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` (2) and `VenueServiceTests.cs` (4) — dead Moq
  setups for the deleted member.

### On those six deleted Moq lines — scaffolding, not assertions

Each was checked individually before deletion and **all six were `Setup(...)` calls, not `Verify(...)`**.
A `Setup` for a member that no longer exists is dead scaffolding whose removal changes no assertion; a
`Verify` would have been an encoded expectation and deleting one would have been silent spec deletion
(`testing.md`). The briefing required stopping and escalating if any turned out to be a `Verify`. None did.

### Verification evidence

- Build 0 errors. Suite `577/577`.
- **An intermittent failure was observed once and is recorded rather than dismissed.** The first run
  after the edits reported `Com falha: 1, Aprovado: 576`, with a stack terminating in
  `Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync`. Four consecutive
  full runs afterwards were green (`577/577` ×4), and the merged result on develop is green.
  **This is NOT BUG-076's signature** — BUG-076 is an `ObjectDisposedException` on `SQLitePCL` during
  `EnsureCreated`, whereas this is an EF Core update-batch failure. It should be treated as a **second,
  distinct intermittent** in the shared temp-file SQLite harness until proven otherwise, and added to
  BUG-076's README as a separate observed signature rather than folded into it. Recorded here so the
  observation is not lost; not investigated, as it is outside this task's scope.

### Orchestrator note on the two stalled agents

Both agents dispatched for this wave (this task and 4.6a/4.6b) ended by announcing they would "wait for
the background build notification" and stopped without completing their exit checklists. The edits for
**this** task were already complete and correct in the worktree but uncommitted and unverified; the
orchestrator verified, committed and merged them directly. The **4.6a/4.6b** worktree was clean — that
agent produced no changes at all — so the audit was re-dispatched as a read-only analysis, which is what
4.6b actually is. Worth noting as a dispatch-reliability observation for the agent-settings
recalibration item already in the backlog.

---

## Task: UoW Phase 4.6a + 4.6b — picker conversion + captive-dependency audit

**Status:** complete, **no code change required** · **Date:** 2026-08-24

### 4.6a — vacuous

All three ViewModels the plan named are deleted. The plan says only `QueueSongPickerViewModel` and
`QueueManagementViewModel` went with the Event/Queue removal and that `PersonPickerViewModel` survives —
**that note is stale**: `PersonPickerViewModel.cs` was deleted in the same commit (`c7ad5bd4`,
2026-08-20). Confirmed independently by the orchestrator via a direct directory listing of
`MyVocaList/UI/ViewModels/`, not by grep. Nothing to convert.

### 4.6b — the audit that makes "nothing outside the pattern" verifiable

17 ViewModel-level hits plus `AppShell`/`AppShellViewModel` checked directly. **Every one landed in
bucket (b) Safe. Zero conversions needed, zero follow-ups found.** The reasoning falls into two groups:

1. **Injects a DB-free service** — `IWhatsNewService`, `IVersionCheckService`, `IFeedbackService`,
   `IYouTubeSearchService`, `IMusicMetadataService`. Each was traced to its implementation and has zero
   `DbContext` / `Repository` / `IUnitOfWork` references; they are external-API clients or preference
   readers. This group includes the two **singletons** (`AppShellViewModel`, `AppShell`), which were the
   worst-case candidates precisely because a singleton capturing a scoped context is the BUG-068 shape.
   `AppShellViewModel`'s `IServiceProvider` is used only to resolve `Page` types for navigation.
2. **Injects a data-writing service that is already migrated** — `IArtistService`, `ISongService`,
   `IPersonService`, `IVenueService`, `ICatalogService`, `IBackupService`, `ISongKaraokeUrlService`,
   `ISongResolutionService`. All now follow the completed rollout shape: a repository field for reads,
   `IUnitOfWork` for every write. The captivity question therefore resolves inside the Services layer,
   which Phases 4.1-4.5 closed.

### Orchestrator's independent verification of the audit's central claim

An audit whose conclusion is "zero hits" cannot rest on a grep in this environment — `rtk` silently
rewrites `grep` and has already returned a false "0 matches" this session. The orchestrator re-ran the
census as a **direct file walk** over every `.cs` under `MyVocaList/`, matching `I\w+Repository`:

> **8 references total, and all 8 are DI registrations** — 6 in
> `Extensions/ServiceCollectionExtensions.cs` (`AddScoped<IVenueRepository, …>` and siblings) and 2 in
> `MauiProgram.cs` (the `IBackupRepository` registration and its use inside the `BackupService` factory
> lambda). **No ViewModel, page, or component constructor-injects a repository at all.**

That is the audit's conclusion arrived at independently, so it is now evidence rather than a claim.

### What this means for Helder's directive

*"The entire app must follow the UoW pattern; anything out of this must be migrated."* As of this task
that is **demonstrated, not assumed**: every service writes through `IUnitOfWork`; no UI type holds a
repository or a captive `AppDbContext`; and `IBaseRepository<T>.SaveChangesAsync()` no longer exists, so
committing outside a unit of work is not expressible in the codebase.

### Verification evidence

- Build 0 errors; `577/577` green.
- A full-solution build hit a transient Android packaging file-lock (`XAWAS7024`, a stray
  `MyVocaList.exe` holding the file). No `.cs`/`.xaml` was touched by this task, so it is an environment
  artifact, not a regression.

### Consequential edit required

`plan.md` Phase 4+ still claims `PersonPickerViewModel` survives. That line must be corrected so a
future reader does not go looking for a file that has not existed since 2026-08-20.
