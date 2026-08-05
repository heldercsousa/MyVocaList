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
