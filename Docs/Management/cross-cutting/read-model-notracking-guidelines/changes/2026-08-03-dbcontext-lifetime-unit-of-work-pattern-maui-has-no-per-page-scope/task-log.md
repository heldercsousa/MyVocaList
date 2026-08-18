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
