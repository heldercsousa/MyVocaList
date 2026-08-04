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
