# Task log: Song form title-first, one search over local and music APIs

> Plan: `plan.md` (approved 2026-09-26; pipeline pre-authorised by Helder 2026-09-25). One section per task. Implementors write only their own section (in their worktree copy); the orchestrator syncs it to `develop` at merge. Checkpoint format: `.claude/library/session-ops.md § Checkpoint Ping & Context Manifest`.

## Baseline

### Checkpoint

Baseline on `develop` at `c81d993a` (2026-09-26, before Step 0): `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → `Aprovado!  – Com falha:     0, Aprovado:   679, Ignorado:     0, Total:   679`. G3 check at Step 0: LEDGER CATALOG-PROP = spec only, no branch; no BUG-080 row/branch; `git worktree list` = main + `.worktrees/acf-t1-spike` only; `git branch -a` hits for catalog/venue = `feat/uow-phase-4-1-catalog`, `feat/uow-phase-4-venue`, both already merged into develop → **G3 clear**.

## G5 — FTS5 spike

### Checkpoint

_(not started)_

## 1.1

**Status:** To Review
**Worktree:** `.worktrees/sft-1.1` (branch `task/sft-1.1-song-picker-entry`)

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — removed the "Search music database" `ListItem` row
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — removed `NavigateToSongPickerCommand`, `OnSongPicked`, `ResolveAndLockArtistAsync`, the `SongPickedMessage` registration, and the `NavigateToSongPickerAsync` method
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — removed the two tests covering the removed picker-resolve path (`ResolveAndLockArtistAsync_ExactMatch_ClosesDropdown`, `ResolveAndLockArtistAsync_NoMatch_ClosesDropdown`)

Verified no remaining references to `NavigateToSongPickerCommand`, `OnSongPicked`, or `ResolveAndLockArtistAsync` anywhere in `MyVocaList/` or `MyVocaList.Tests/`.

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-44 | Song form has no Song-picker entry row or navigation command | `SongFormPage.xaml`, `SongFormViewModel.cs` | Demo-verified (no automated UI test for XAML absence); covering VM tests for the removed path deleted |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors, 27 warnings (pre-existing NU1902/NU1903 advisories, unrelated).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 677/677 passed.
- Commit: `ec16fda3`.
- gates pre-authorised by Helder 2026-09-25

## 1.2

**Status:** To Review
**Worktree:** `.worktrees/sft-1.2` (branch `task/sft-1.2-artist-picker-entry`)

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` — removed the "Search music database" `ListItem` row
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` — removed `NavigateToArtistPickerCommand`, `NavigateToArtistPickerAsync`, the `ArtistPickedMessage` registration

Census: no Artist-form VM test file referenced the removed command/message — no test file changes required. `ArtistPickerViewModel.cs` and `ArtistPickerViewModelTests.cs` left untouched (unused but not deleted, per Helder's instruction).

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| (task 1.2, no dedicated REQ-SFT ID cited in tasks.md) | Artist form has no Artist-picker entry row or navigation command | `ArtistFormPage.xaml`, `ArtistFormViewModel.cs` | Demo-verified (no automated UI test for XAML absence) |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors, 27 warnings (pre-existing NU1902/NU1903 advisories, unrelated).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 679/679 passed.
- Commit: `a54d7257`.
- gates pre-authorised by Helder 2026-09-25

## 1.3

**Status:** To Review
**Worktree:** `.worktrees/sft-w2` (branch `task/sft-w2-picker-teardown`)

### Changed files:
- `MyVocaList/Navigation/Routes.cs` — removed `ArtistPicker`/`SongPicker` route constants
- `MyVocaList/AppShell.xaml.cs` — removed both `Routing.RegisterRoute` lines
- `MyVocaList/MauiProgram.cs` — removed `ArtistPickerViewModel`/`SongPickerViewModel`/`ArtistPickerPage`/`SongPickerPage` DI registrations

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-44 | Neither picker has a route, Shell registration, or DI entry | `Routes.cs`, `AppShell.xaml.cs`, `MauiProgram.cs` | Demo-verified (no automated test for route/DI absence); census below |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors, 33 warnings (pre-existing NU1902/NU1903 advisories, unrelated).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 683/683 passed.
- Census `Routes\.(SongPicker|ArtistPicker)` — zero hits in `.cs`/`.xaml` (only doc files under `Docs/` reference the retired names).
- Commit: `11cb7d19`.
- gates pre-authorised by Helder 2026-09-25

## 1.4

**Status:** To Review
**Worktree:** `.worktrees/sft-w2` (branch `task/sft-w2-picker-teardown`)

### Changed files (deleted):
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs`
- `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`
- `Contracts/Messages/SongPickedMessage.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs` (12 tests)

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-44 | SongPickerPage, VM, message and tests fully removed | deletions above | Python `os.walk` census (below); full suite |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 671/671 passed (683 − 12 deleted).
- Census: Python `os.walk` over all `.cs`/`.xaml` for `SongPicker|SongPickedMessage` → **0 hits**.
- Commit: `c2367091`.
- gates pre-authorised by Helder 2026-09-25

## 1.5

**Status:** To Review
**Worktree:** `.worktrees/sft-w2` (branch `task/sft-w2-picker-teardown`)

### Changed files (deleted):
- `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`
- `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs`
- `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`
- `Contracts/Messages/ArtistPickedMessage.cs`
- `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs` (11 tests)

`YouTubeSearchPage`/VM/route/row left untouched, as instructed.

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-44 | ArtistPickerPage, VM, message and tests fully removed | deletions above | Python `os.walk` census (below); full suite |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 660/660 passed (671 − 11 deleted).
- Census: Python `os.walk` over all `.cs`/`.xaml` for `ArtistPicker|ArtistPickedMessage` → **0 hits**.
- Commit: `dba12eb7`.
- gates pre-authorised by Helder 2026-09-25

## 1.6

**Status:** To Review
**Worktree:** `.worktrees/sft-w2` (branch `task/sft-w2-picker-teardown`)

### Changed files:
- `Services/IMusicMetadataService.cs` — deleted
- `Services/MusicMetadataService.cs` — deleted
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` — removed `AddScoped<IMusicMetadataService, MusicMetadataService>()`

Note: the `ServiceCollectionExtensions.cs` edit was made in the same work session but initially left staged uncommitted; it was committed separately as a documented 1.6 follow-up (`fc25520d`) once the gap was found on session resume. No behavior difference — build/tests were already green against the combined change before either commit.

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-44 | `IMusicMetadataService`/impl and DI entry fully removed, zero remaining consumers | deletions + DI edit above | Pre-deletion census (below); `AppServicesRegistrationTests`, `UnitOfWorkReadScopeTests.ServicesDirectory_IsFoundAndNonEmpty` |

### Verification evidence
- **Pre-deletion census** (`IMusicMetadataService|MusicMetadataService` over all `.cs`/`.xaml`): 3 hits, all in the 3 owned files — no external consumer found.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors (both commits).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 660/660 passed, including `AppServicesRegistrationTests` and `UnitOfWorkReadScopeTests`.
- **Post-deletion census** — 0 hits. `Services/` directory count: 32 (≥ 25 required).
- Commits: `305f493c` (interface/impl deletion), `fc25520d` (DI line, follow-up).
- gates pre-authorised by Helder 2026-09-25

## 1.7

**Status:** To Review
**Worktree:** `.worktrees/sft-w2` (branch `task/sft-w2-picker-teardown`)
**Model:** Sonnet (single agent, Red first, per plan.md).

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — removed `CreateArtistInlineCommand` (registration + property + `CreateArtistInlineAsync` method); `OnArtistBlurredWithoutSelection` no longer sets `ArtistHasError`/`ArtistErrorText` when text is unmatched (REQ-SFT-17)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — removed the create-sentinel row append in `OnArtistItemsRequested` and the `IsCreateNew` routing branch in `OnArtistSelectionChanged` (always routes to `SelectArtistCommand` now)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — rewrote `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError` → `…RetainsTextAndSetsNoError`; deleted `ArtistBlurredWithoutSelection_NoPriorSelection_SetsErrorText`, `CreateArtistInline_Success_LocksCreatedArtistAndClearsError`, `CreateArtistInline_Failure_MapsErrorAndRetainsText`

### Superseded-test mapping
| Test | Action | Superseded AC → replacing REQ |
|---|---|---|
| `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsError` | rewritten → `…RetainsTextAndSetsNoError` | REQ-ACREATE-03 error clause → REQ-SFT-17 |
| `ArtistBlurredWithoutSelection_NoPriorSelection_SetsErrorText` | deleted | REQ-ACREATE-03 error clause → REQ-SFT-17 |
| `CreateArtistInline_Success_LocksCreatedArtistAndClearsError` | deleted | REQ-ACREATE-04/08 → REQ-SFT-45/18/20 |
| `CreateArtistInline_Failure_MapsErrorAndRetainsText` | deleted | REQ-ACREATE-05 → REQ-SFT-45/18/20 |
| `ArtistBlurredWithoutSelection_EmptyText_NoErrorNoLock`, `…WithPriorSelection_RestoresName` | kept unchanged | REQ-ACF-17/24 carry-over |

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-17 | Blur with unpicked text keeps the text and sets no error | `SongFormViewModel.OnArtistBlurredWithoutSelection` | `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsNoError` |
| REQ-SFT-45/18/20 | No inline artist-create path remains in the Song form | removal of `CreateArtistInlineCommand` + sentinel glue | Census (`CreateArtistInline`) → 0 hits in production code |

### TDD evidence (Red → Green)
- **Red:** rewritten test run alone — failed with `Assert.False() Failure: Expected False, Actual True` (old `ArtistHasError` assignment still present) — confirmed failing for the right reason. Committed test-only, `Lane: RED`, commit `9401ee6e`.
- **Green:** removed the error assignment, deleted inline-create members/glue/superseded tests; filtered test passes; full suite green. Commit `bec7378f`.

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors (Red commit stage and Green stage both built 0 errors; two transient unrelated-error build outputs during the session were concurrent-build noise, not reproducible on a clean rebuild).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 657/657 passed (660 − 3 net: 1 rewritten, 2 deleted). One run showed 2 unrelated Integration/Repository failures (`PersonRepositoryTests.GetPagedAsync_WithQuery_FiltersResults`, `ArtistRepositoryTests.DeleteAsync_ExistingArtist_Removed`) that passed clean on immediate rerun — known-flaky SQLite integration tests per briefing, not caused by this change.
- Census `CreateArtistInline` → 0 hits in production/test code. Census `"Search and select an artist from the list"` → 1 remaining hit, in `SaveAsync`'s existing guard (`SongFormViewModel.cs`), which is explicitly retained per the task's documented interim risk (Save still requires a picked artist until 5.3) — not a regression of this task's scope.
- Commits: `9401ee6e` (RED), `bec7378f` (Green).
- gates pre-authorised by Helder 2026-09-25

## 2.1

**Status:** To Review
**Worktree:** `.worktrees/sft-2.1` (branch `task/sft-2.1-song-lookups`)

Branch `task/sft-2.1-song-lookups`, worktree `.worktrees/sft-2.1`, base develop `286b7712`. Step: Red committed, about to implement Green (ISongRepository + SongRepository). Next command: implement the two methods then `dotnet test --filter SongRepositoryTests`.
Context manifest: `Domain/RepositoryInterface/ISongRepository.cs` (interface to extend), `Infra/Repository/SongRepository.cs` (implementation), `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs` (Red tests, `// ── Lookups (SFT) ──` region), `design.md` §3.1 (verbatim signatures), `plan.md` lines 459-494 (task spec).

### Red

Filter `SongRepositoryTests`. Build failed with the expected compile errors (both new members missing):

```
SongRepositoryTests.cs(400,33): error CS1061: 'SongRepository' does not contain a definition for 'FindIdByTitleVersionForArtistAsync' ...
SongRepositoryTests.cs(415,33): error CS1061: 'SongRepository' does not contain a definition for 'FindIdByTitleVersionForArtistAsync' ...
SongRepositoryTests.cs(432,34): error CS1061: 'SongRepository' does not contain a definition for 'ExistsByExternalIdentityAsync' ...
SongRepositoryTests.cs(449,34): error CS1061: 'SongRepository' does not contain a definition for 'ExistsByExternalIdentityAsync' ...
SongRepositoryTests.cs(459,34): error CS1061: 'SongRepository' does not contain a definition for 'ExistsByExternalIdentityAsync' ...
```

(Original compiler output was localized pt-BR; error code/meaning unchanged.)

### Changed files:
- `Domain/RepositoryInterface/ISongRepository.cs` — added `FindIdByTitleVersionForArtistAsync`, `ExistsByExternalIdentityAsync`
- `Infra/Repository/SongRepository.cs` — implementations using `EF.Functions.Collate` (DB collation, no C#-side normalization)
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` — test-double decorator updated to delegate the two new interface members to `_inner`
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryTests.cs` (Red, prior commit `ad534180`)

### Deviation note
`NestedUnitOfWorkTests.cs`'s in-file fake repository required the two new interface members to compile. The fix delegates both to `_inner` (pass-through) — a mechanical compile fix, not a behavior or assertion change. No assertion was weakened or removed.

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|-----------------|--------------|
| REQ-SFT-42 | Collated (artist, title, version) match ignores accent difference | `SongRepository.FindIdByTitleVersionForArtistAsync` | `SongRepositoryTests` (3 tests, see below) |
| REQ-SFT-29 | Version mismatch is not a match; excluding the edited row yields no match | `SongRepository.FindIdByTitleVersionForArtistAsync` | `SongRepositoryTests` (2 tests) |
| REQ-SFT-26 | External identity uniqueness check, with exclude | `SongRepository.ExistsByExternalIdentityAsync` | `SongRepositoryTests` (3 tests) |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` — 0 errors, 27 warnings (pre-existing NU1902/NU1903 advisories, unrelated).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` — 684 passed, 1 failed (full suite). The one failure (`SongKaraokeUrlRepositoryTests.GetBySongIdAsync_AfterAdd_ReturnsUrl`, `ObjectDisposedException` on SQLite handle) is an unrelated flaky SQLite-disposal race, not in scope for this task.
- `dotnet test --filter "FullyQualifiedName~SongRepositoryTests"` — 27/27 passed (all tests touched by this task, including the 8 `[AC]`-tagged new tests).
- Commit: `45d0e4a8` (Green, in worktree `.worktrees/sft-2.1`); Red at `ad534180`.
- gates pre-authorised by Helder 2026-09-25

## 2.2

**Status:** To Review
**Worktree:** `.worktrees/sft-2.2` (branch `task/sft-2.2-rankscore`, base develop `1db92388`)

Context7 was not needed (FuzzySharp `Fuzz.TokenSortRatio` already used by `Score` via `Fuzz.TokenSetRatio`; same package 2.0.2). No inherited partial work.

### Red (commit `5257d208`, `Lane: RED`, tests only)

Filter `SimilarityScorerTests` + `SearchConstantsTests`; build failed (compiler output localized pt-BR, codes verbatim):

```
SearchConstantsTests.cs(15,47): error CS0117: "SearchConstants" nao contem uma definicao para "RemoteSearchTimeoutMilliseconds"
SimilarityScorerTests.cs(105,26): error CS1061: 'SimilarityScorer' nao contem uma definicao para "RankScore" ...
(same CS1061 at lines 105, 112, 119, 126, 135 and others)
```

### Green

Filter `SimilarityScorerTests|SearchConstantsTests`: `Failed: 0, Passed: 23, Total: 23` (pt-BR "Aprovado!"). Existing `Score` tests unchanged and green.

### Changed files:
- `Domain/ServicesInterfaces/ISimilarityScorer.cs` - added `RankScore` (XML doc: ordering only, cites exception design.md § 6.4)
- `Infra/Similarity/SimilarityScorer.cs` - `RankScore` = `Fuzz.TokenSortRatio(Normalize(q), Normalize(c)) / 100.0`; null/empty -> 0.0
- `Domain/Constants/SearchConstants.cs` - `RemoteSearchTimeoutMilliseconds = 3000`
- `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs` - 6 new tests (Red commit)
- `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` - 1 new test (Red commit)
- task-log.md (this section)

### AC matrix
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|----------------|-------------|
| REQ-SFT-30 | typo/truncated queries rank closest title first; range [0,1] | `SimilarityScorer.RankScore` | `RankScore_DontCry_RanksDontCryAboveCry`, `RankScore_DonCry_RanksDontCryAboveCry`, `RankScore_DontCry_RanksDontCryAboveBoysDontCry`, `RankScore_Range_IsBetweenZeroAndOne` |
| REQ-SFT-31 | accents/case ignored | `Normalize` in `RankScore` | `RankScore_AccentAndCaseVariant_ScoresOne` |
| REQ-SFT-32 | artist words in query rank title+artist | `SimilarityScorer.RankScore` | `RankScore_GunsCry_TitleArtistOfDontCryAboveCryByCyndiLauper` |
| REQ-SFT-08 | remote bound 3,000 ms | `SearchConstants.RemoteSearchTimeoutMilliseconds` | `RemoteSearchTimeoutMilliseconds_Is3000` |

### Superseded tests
None.

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1`: no `error` lines (0 errors; exit 0).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` (full): `Aprovado! - Com falha: 0, Aprovado: 668, Ignorado: 0, Total: 668` (Failed 0; 7 new tests included).
- Files re-read after edit: ISimilarityScorer.cs, SimilarityScorer.cs, SearchConstants.cs (tail), both test files.

## 3.1

### Checkpoint

- 2026-11-04 (orchestrator) — **Pre-dispatch G3 checks (plan § 0.2.2), recorded before dispatching 3.1-Tester:**
  1. `LEDGER.md` rows with a non-empty branch/worktree: none live. CATALOG-PROP = "— (spec only, no branch yet)", deferred by Helder 2026-11-04 (spec after SFT). BUG-080 = "— (no branch)"; its fix (dispatched in parallel with this task) adds **no** migration and edits only `20260211055225_venuesSeedForTest.cs` + a new test, so it is not a migration branch in the § 0.2.2 sense and shares no file with 3.1.
  2. `git worktree list`: only the main tree `C:/Users/helde/source/repos/MyVocaList dae05832 [develop]`.
  3. `git branch -a`: no local or remote branch name contains `catalog`, `catalog-prop`, `bug-080` or `venue`.
  - **Result: clear to dispatch.** Worktree `.worktrees/sft-3.1`, branch `task/sft-3.1-new-artist-uow`, from develop `dae05832`. Role: Tester (W2), then a fresh Builder (Opus).

- 2026-11-04 (Tester W2) - Step 1 + Step 2 done; Red committed. Build is a compile failure (expected). Now: STOP, hand back to orchestrator for the Builder (W3, Opus).

### Tester notes (W2)

- Context7 available; not needed (only existing test mechanics reused). Skills: myvocalist-coding.
- Edited `SongServiceTests.cs`: the 3 existing `CreateSongWithUrlsAsync(artistId, ...)` calls (lines ~604, 624, 642 -> `artistId, null, ...`) moved to the new overload; +3 new unit tests. NOTE: the plan says "10 existing calls"; only 3 exist in this file (the others are in `SongFormViewModelTests.cs` / `Bug068RegressionTests.cs`, not owned; they keep compiling against the `[Obsolete]` old overload).
- Created `SongServiceNewArtistUnitOfWorkTests.cs` (6 tests, own copy of the `ThrowOnAddSongRepository` decorator).
- Superseded tests (NOT deleted; `NestedUnitOfWorkTests.cs` stays until 3.3): `:66` `CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow` (REQ-UOW-22) -> `CreateSongWithUrlsAsync_ThrowAfterArtistStaged_RollsBackBoth` (REQ-SFT-18); `:105` `..._LeavesPartialArtistRow` failure-tuple (REQ-UOW-24) -> `CreateSongWithUrlsAsync_FailureTupleAfterArtistStaged_WritesNothing` (REQ-SFT-18); `:22` -> `..._NewArtistName_PersistsOneArtistAndOneSong`; `:146` -> `..._UsesOneAppDbContextAcrossTwoLevels`.
- Flag: the "failure tuple after artist staged" test uses the prescribed 101-char title; if the Builder validates the title BEFORE creating the artist (natural), that test passes without exercising rollback of a staged artist (weak but spec-prescribed).

### Red output (verbatim, `dotnet test ... -m:1 --filter "FullyQualifiedName~SongServiceTests|FullyQualifiedName~SongServiceNewArtistUnitOfWorkTests"`; compiler is pt-BR localised)

Compile failure. The expected CS1501 does NOT appear: the old overload is the only candidate with optional `ct`, so the 9-arg calls bind to it and fail with CS1503/CS9174/CS8130 instead.

```
SongServiceNewArtistUnitOfWorkTests.cs(20,13): error CS1503: Argumento 1: nao e possivel converter de "<nulo>" para "int"
SongServiceNewArtistUnitOfWorkTests.cs(20,76): error CS1503: Argumento 8: nao e possivel converter de "string" para "System.Collections.Generic.IEnumerable<string>"
SongServiceNewArtistUnitOfWorkTests.cs(20,94): error CS9174: Nao e possivel inicializar o tipo 'CancellationToken' com uma expressao de colecao porque o tipo nao e construivel.
SongServiceNewArtistUnitOfWorkTests.cs(134,14): error CS8130: Nao e possivel inferir o tipo da variavel de desconstrucao digitada implicitamente 'holderOk'.
SongServiceTests.cs(604,14): error CS8130: Nao e possivel inferir o tipo da variavel de desconstrucao digitada implicitamente 'success'.
SongServiceTests.cs(606,13): error CS9174: Nao e possivel inicializar o tipo 'CancellationToken' com uma expressao de colecao porque o tipo nao e construivel.
SongServiceTests.cs(659,13): error CS1503: Argumento 1: nao e possivel converter de "<nulo>" para "int"
SongServiceTests.cs(696,66): error CS1503: Argumento 8: nao e possivel converter de "string" para "System.Collections.Generic.IEnumerable<string>"
```
(Accents transliterated; full set: CS1503, CS8130, CS8183, CS9174 across both files.)

### Builder (W3) — Status: blocked: spec gap

- 2026-11-04 (Builder W3, Opus) — branch `task/sft-3.1-new-artist-uow`, worktree `.worktrees/sft-3.1`. Step 3 implemented (new overload + `[Obsolete]` forwarder in `ISongService.cs` / `SongService.cs`), **left UNCOMMITTED in the worktree** (no Green commit: one Red test cannot pass for a reason inside the test). Context7 available; not needed (no new library API; `IUnitOfWork.FlushAsync` + ambient join verified from `Infra/UnitOfWork/UnitOfWork.cs` and the `ArtistResolutionService.CommitAsync` precedent).
- Build (attempt 1): `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → `6 projects, 0 errors, 35 warnings`.
- Focused tests (`SongServiceTests|SongServiceNewArtistUnitOfWorkTests`): `Com falha: 1, Aprovado: 46, Total: 47`. Failing: `CreateSongWithUrlsAsync_NewArtist_UsesOneAppDbContextAcrossTwoLevels` — `Assert.Single() Failure: The collection contained 2 items`.
- Also run (`--no-build`): `UnitOfWorkReadScopeTests|NestedUnitOfWorkTests|Bug068RegressionTests|SongFormViewModelTests` → `Com falha: 0, Aprovado: 65, Total: 65`. Full suite NOT run (stopped at the block).
- New CS0618 warnings (old-overload call sites only): `MyVocaList/UI/ViewModels/SongFormViewModel.cs(568)`, `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs(128)`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs(180, 199)`.

#### Spec gap: two-level context test records the window-scope SongService's constructor repositories
**Location:** `SongServiceNewArtistUnitOfWorkTests.cs:81-106` (port of `NestedUnitOfWorkTests.cs:146`).
**Gap description:** the helper `CreateWithNewArtist` calls `host.Resolve<ISongService>()` AFTER `observed.Clear()`, so constructing `SongService` resolves its constructor-injected `ISongRepository`/`IArtistRepository` from the host scope and records the host's `AppDbContext`; the unit of work's own context is the second distinct instance. The original test resolved the service BEFORE `observed.Clear()` (`NestedUnitOfWorkTests.cs:164` then `:177`). No production change can fix this without removing `SongService`'s constructor fields (constructor signature is pinned by `SongServiceTests.CreateSut` and by the architecture floor of governed constructor parameters).
**Options:**
- Option A: Tester resolves `var service = host.Resolve<ISongService>();` before `observed.Clear()` and calls `service.CreateSongWithUrlsAsync(null, "Two Level Join Artist", …)` directly — the test then measures only the unit of work's contexts, as the original did.
- Option B: Builder alters `SongService`'s constructor — breaks every test's `CreateSut` and the architecture floors; out of scope.
**Recommendation:** Option A, because it restores the original test's observation window and is a test-only change.
**Blocking:** Yes — Builder must not edit tests.

#### Note for the Verifier — `FailureTupleAfterArtistStaged_WritesNothing`
With this implementation the title is validated (top of the lambda) BEFORE the artist is created, so the 101-char title fails before any artist is staged: the test passes but does NOT exercise a staged-artist rollback. The rollback of a flushed artist is exercised by `ThrowAfterArtistStaged_RollsBackBoth` (exception path) and `NewArtistNameCollatedDuplicate_FailsAndWritesNothing` covers the nested failure-tuple path.

#### Implementation decisions (for review)
- XOR check (`artistId.HasValue == !IsNullOrWhiteSpace(newArtistName)` → `"Artist is required"`) runs OUTSIDE the lambda (REQ-UOW-41: no DI scope for pure input validation); a blank `newArtistName` counts as not supplied.
- Order inside the lambda: title validation → external-identity check (only when both `externalId` and `externalProvider` are non-blank; `ExistsByExternalIdentityAsync(provider, externalId, null, ct)`) → new artist via `IArtistService` from `sp` + `_uow.FlushAsync` (or existing artist lookup "Artist not found") → title/version duplicate check against the effective artist → stage song → stage URLs.
- `_uow.FlushAsync` is called on the constructor field (it holds no `AppDbContext`; same rationale as `ArtistResolutionService.CommitAsync`).

### Changed files (uncommitted)
- `Domain/ServicesInterfaces/ISongService.cs`
- `Services/SongService.cs`
- this task-log section

- Context manifest: `Services/SongService.cs` (impl) · `Domain/ServicesInterfaces/ISongService.cs` (surface) · `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` · `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` · `Infra/UnitOfWork/UnitOfWork.cs` (ambient join/flush) · `plan.md` § Task 3.1.

### Tester correction (2026-11-04)

- **What was wrong:** in `CreateSongWithUrlsAsync_NewArtist_UsesOneAppDbContextAcrossTwoLevels`, `observed.Clear()` ran before `CreateWithNewArtist(...)`, whose helper resolves `ISongService` after the clear. Constructing `SongService` from the host scope recorded the host's `AppDbContext`, so `Assert.Single` saw 2 contexts. The source test (`NestedUnitOfWorkTests.cs:164` then `:177`) resolves the service first and clears afterwards.
- **Fix:** test-only. The test now resolves `var service = host.Resolve<ISongService>();` before `observed.Clear();` and calls `service.CreateSongWithUrlsAsync(null, "Two Level Join Artist", "Two Level Join Song", string.Empty, null, null, null, null, [])` directly, matching the helper's argument list. Comment added on the resolve line.
- No assertion changed; AC REQ-SFT-18 unchanged. No production file touched (`ISongService.cs` / `SongService.cs` remain uncommitted, untouched).
- Test output: `Aprovado!  – Com falha:     0, Aprovado:     6, Ignorado:     0, Total:     6, Duração: 12 s - MyVocaList.Tests.dll (net10.0)`

### Builder (finishing) - Green (2026-11-04)

Inherited: the previous Builder's uncommitted `ISongService.cs` / `SongService.cs`; reviewed against plan 3.1 Step 3 and design 3.2, kept unchanged (no deviations). Context7 not needed (no new API surface; existing `IUnitOfWork` / `IArtistService` patterns).

### Changed files
- `Domain/ServicesInterfaces/ISongService.cs` (new `int? artistId, string? newArtistName` overload; old overload `[Obsolete]`)
- `Services/SongService.cs` (new overload implementation; old overload forwards with `newArtistName: null`)
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (Red 3dc296b1 + Tester fix 4d4125be, already committed)
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (Red 3dc296b1, already committed)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md`

### Verification evidence
- Build: `ok dotnet build: 6 projects, 0 errors, 35 warnings (00:03:51.10)`
- Focused (SongServiceTests + SongServiceNewArtistUnitOfWorkTests): `Aprovado!  - Com falha:     0, Aprovado:    47, Ignorado:     0, Total:    47`
- Full suite: `Aprovado!  - Com falha:     0, Aprovado:   677, Ignorado:     0, Total:   677` (baseline 668, delta +9; no flaky re-run needed)

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| REQ-SFT-18 | Typed new artist + song + URLs persisted in one unit of work | `SongService.CreateSongWithUrlsAsync(int?, string?, ...)` (artist via `IArtistService` from `sp`, `_uow.FlushAsync`) | `CreateSongWithUrlsAsync_NewArtistName_PersistsOneArtistAndOneSong`, `_ThrowAfterArtistStaged_RollsBackBoth`, `_FailureTupleAfterArtistStaged_WritesNothing`, `_NewArtist_UsesOneAppDbContextAcrossTwoLevels` |
| REQ-SFT-21 | Exactly one of artistId / newArtistName, else "Artist is required" | guard before `_uow.ExecuteAsync` | `CreateSongWithUrlsAsync_NeitherArtistIdNorNewName_FailsArtistRequired`, `_BothArtistIdAndNewName_FailsArtistRequired` |
| REQ-SFT-26 | External identity held by another song rejected before any artist is created | `ExistsByExternalIdentityAsync` check ahead of the artist branch | `CreateSongWithUrlsAsync_ExternalIdentityHeldByAnotherSong_FailsWithMessage`, `_NewArtistAndIdentityHeld_CreatesNoArtist` |
| REQ-SFT-52 | Collated duplicate artist name refused, nothing written | `IArtistService.CreateArtistAsync` failure tuple returned as-is | `CreateSongWithUrlsAsync_NewArtistNameCollatedDuplicate_FailsAndWritesNothing` |

Note on `FailureTupleAfterArtistStaged_WritesNothing`: the title is validated before the artist is created, so this test does not exercise staged-artist rollback. That rollback is covered by `ThrowAfterArtistStaged_RollsBackBoth` and `NewArtistNameCollatedDuplicate_FailsAndWritesNothing`.

### CS0618 warnings (old overload `int artistId`; allowed at call sites only)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs(568)` (production caller; migrated by a later task, overload removed in 5.3b)
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs(128)`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs(180, 199)`

### Spec notes requested
- REQ-SFT-21: a blank/whitespace `newArtistName` is treated as not supplied.

### Verifier Verdict — 2026-11-04 (recorded on develop by the orchestrator)
**Result:** CONDITIONAL PASS (Opus verifier; build 0 errors; 677/677 re-run)
- [PASS] Signatures match design.md § 3.2; `[Obsolete]` forwarder with `newArtistName: null`
- [PASS] XOR guard outside the lambda; all repos/services from `sp`; REQ-SFT-26 before artist create; `FlushAsync` before the song is staged
- [PASS] `4d4125be` test-only reorder, assertions/`[AC]` unchanged; the Green commit touches no test
- [PASS] REQ-SFT-18/21/26/52 each covered
- [PASS] All old-overload callers bind to the old overload (incl. `SongResolutionService.cs:216`)

**W1 (applied):** CS0618 list completed — add `Services/SongResolutionService.cs(216)` (it binds to the old overload; a clean build shows the warning, the Builder's incremental build did not). Full list: `SongFormViewModel.cs(568)`, `SongResolutionService.cs(216)`, `Bug068RegressionTests.cs(128)`, `SongFormViewModelTests.cs(180, 199)`.
**W2 (applied, matrix note):** `CreateSongWithUrlsAsync_FailureTupleAfterArtistStaged_WritesNothing` covers "a title failure writes nothing"; it does not stage an artist (title validation runs first). A post-flush failure *tuple* is unreachable on the new-artist path (a just-created artist has no songs, so the duplicate check cannot fire; invalid URLs are skipped), so REQ-SFT-18's rollback is fully exercised by `ThrowAfterArtistStaged_RollsBackBoth` (exception path, real SQLite). A rename is a Tester task, deferred (not blocking).
**I1 (noted):** forwarding means the old overload now also enforces REQ-SFT-26 (new failure "This music-database song is already in your library" for the VM and `SongResolutionService` CreateNewVersion callers), consistent with REQ-SFT-26; "behaviour otherwise unchanged" is not literally true.
**Merge:** `4748326b` (`--no-ff`) 2026-11-04. Post-merge on develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 677, Total: 677`.

## 3.2a

### Checkpoint (final)

- 2026-11-04 (orchestrator) — **Pre-dispatch G3 checks (plan § 0.2.2):**
  1. `LEDGER.md` rows with a live branch: BUG-080 (`fix/bug-080-venue-seed-debug-only`) — touches only `Infra/Migrations/20260211055225_venuesSeedForTest.cs` (edit, no new migration, no snapshot), `MyVocaList.Tests/Integration/Migrations/VenuesSeedMigrationDownTests.cs`, its README (`git diff --name-only develop...` verified); no `SongService.cs`/`ISongService.cs`; not a migration-adding branch. CATALOG-PROP: deferred, no branch.
  2. `git worktree list`: main (develop `4748326b`), `.worktrees/bug-080`, `.worktrees/sft-3.1` (merged; being removed).
  3. `git branch -a` matches: `feat/uow-phase-4-1-catalog` and `feat/uow-phase-4-venue` — both merged into develop 2026-08-24 (UoW phases 4.1/4.4, `merge-base --is-ancestor` true), not live; `fix/bug-080-…` as in 1.
  - **Result: clear to dispatch.** Worktree `.worktrees/sft-3.2a`, branch `task/sft-3.2a-update-version-uniqueness`, from develop after 3.1's merge.

**Status:** To Review (E2E: not applicable - Service-only change, no emulator run per briefing)
**Started/Completed:** 2026-11-04
Context7 available; no API lookup needed (patterns copied from 3.1's `CreateSongWithUrlsAsync`). Inherited: nothing (clean worktree).

### Red evidence
Red A (`289f25e5`, `Lane: RED`): `dotnet test --filter SongServiceTests.UpdateSongAsync` -> `Com falha: 6, Aprovado: 12, Total: 18`. Failing: `VersionChangedToExistingVersion_FailsDuplicate`, `EditedTitleCollidesWithArtistTitleVersion_Rejected`, `ChangedArtistWithDuplicateTitle_ReturnsFalse`, `DuplicateTitleExcludingSelf_ReturnsFalse`, `ExternalIdentityHeldByAnotherSong_Fails` - all `Assert.False() Failure / Expected: False / Actual: True` (REQ-SFT-29 regression Red); `VersionChangedToNewVersionOfExistingTitle_Succeeds` - message `A song with this title already exists for this artist` (title-only check).
Red B (`553f4cd7`, `Lane: RED`): compile error `CS1739: A melhor sobrecarga de "UpdateSongAsync" nao tem um parametro chamado "newArtistName"` (SongServiceTests.cs 389, 410, 428) + `CS8130`.
Note: `UpdateSongAsync_TitleEditedOnApiSourcedSong_KeepsExternalIdentity` (REQ-SFT-27) passed from the start (characterization of null-keeps-existing).

### Green
Full suite: `Aprovado! - Com falha: 0, Aprovado: 685, Total: 685` (baseline 677, +8 new tests; Bug068RegressionTests and UnitOfWorkReadScopeTests included). Android build: `0 errors`.

### Mechanical mock changes (SongServiceTests.cs)
Duplicate-title mocks in `UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse` and `UpdateSongAsync_ChangedArtistWithDuplicateTitle_ReturnsFalse` now set up `FindIdByTitleVersionForArtistAsync` (returns 2 / 3) instead of `ExistsByTitleForArtistAsync`.

### Spec gap (Blocking: No - documented assumption, review please)
**Location:** plan.md Task 3.2a "no call site breaks".
**Gap:** adding `newArtistName` before `ct` shifts the positional arg of Moq expression-tree `Setup/Verify(s => s.UpdateSongAsync(..., It.IsAny<CancellationToken>()))`; 12 calls in `SongResolutionServiceTests.cs` (6) and `SongFormViewModelTests.cs` (6) stopped compiling (CS1503). Both files are outside Files owned.
**Option A (taken):** insert `It.IsAny<string?>(),` before the final arg in those 12 expressions (mechanical, no assertion changed). **Option B:** block and have the orchestrator edit them.
**Recommendation:** A. Orchestrator should confirm the two files are not being edited by 3.3 (3.3 owns SongResolutionService tests - merge conflict likely, trivial to resolve).

### Decisions / requested spec notes
- Duplicate message on update is now `A song with this title and version already exists for this artist` (same as create); the old title-only text no longer exists on this path.
- Both `artistId` and `newArtistName` supplied -> `(false, "Artist is required")` outside the UoW (no new string invented).
- Request: `> **Spec updated 2026-11-04:**` note in design.md 3.2 for `newArtistName` on `UpdateSongAsync` + the two points above.

### AC matrix
| AC | Implementation | Test |
|---|---|---|
| REQ-SFT-26 | SongService.UpdateSongAsync `ExistsByExternalIdentityAsync(..., id)` before artist creation | UpdateSongAsync_ExternalIdentityHeldByAnotherSong_Fails |
| REQ-SFT-27 | collated uniqueness; null/null keeps identity | UpdateSongAsync_TitleEditedOnApiSourcedSong_KeepsExternalIdentity, UpdateSongAsync_EditedTitleCollidesWithArtistTitleVersion_Rejected |
| REQ-SFT-28 | sp-resolved IArtistService.CreateArtistAsync + FlushAsync | UpdateSongAsync_NewArtistName_CreatesArtistInSameUnitOfWork, ..._NewArtistCreationFails_ReturnsMessageAndDoesNotWrite, ..._BothArtistIdAndNewArtistName_Rejected |
| REQ-SFT-29 | FindIdByTitleVersionForArtistAsync(effectiveArtist, title, effectiveVersion, id) | UpdateSongAsync_VersionChangedToNewVersionOfExistingTitle_Succeeds, ..._VersionChangedToExistingVersion_FailsDuplicate |

### Changed files:
- `Domain/ServicesInterfaces/ISongService.cs`
- `Services/SongService.cs`
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` (off-list, mechanical, see spec gap)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (off-list, mechanical)
- task-log.md (section 3.2a)

BUG-067 navigation-detach block (`song.OriginalArtist = null`) untouched.

## 3.3

### Checkpoint

_(not started)_

## 3.3b

### Checkpoint

_(not started)_

## 3.4

### Checkpoint

_(not started)_

## 3.2b

### Checkpoint

_(not started)_

## 3.2c

### Checkpoint

_(not started)_

## 3.5a

### Checkpoint

_(not started)_

## 3.5b

### Checkpoint

_(not started)_

## 3.6a

### Checkpoint

_(not started)_

## 3.6b

### Checkpoint

_(not started)_

## 3.7

### Checkpoint

_(not started)_

## 3.8

### Checkpoint

_(not started)_

## 3.9

### Checkpoint

_(not started)_

## 3.9b

### Checkpoint

_(not started)_

## 3.9c

### Checkpoint

_(not started)_

## 3.10

### Checkpoint

_(not started)_

## 4.1a

### Checkpoint

_(not started)_

## 4.1b

### Checkpoint

_(not started)_

## 4.2

### Checkpoint

_(not started)_

## 4.3

### Checkpoint

_(not started)_

## 5.1

**Status:** To Review
**Started:** 2026-11-04 **Completed:** 2026-11-04
**Plan:** plan.md § Task 5.1 · Level A · Review lane Elevated

Context7 available; no new external API used (INavigationService is an existing project interface; pure deletion + one command). Inherited nothing (clean worktree).
Emulator smoke deferred to Helder's device gate (agents never run an emulator).

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (INavigationService ctor dependency (last param); `OpenExistingSongCommand`; removed merge/update-existing state, commands, `HandleCommitResultAsync`, `SelectedResolutionTargetId`, `_pendingResolution`, nested `MergeFieldRow` (was in the same file, no separate file))
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (merge sheet deleted; row button "Open existing" bound to `OpenExistingSongCommand`; "Save as new version" label)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (**outside owned list, required**: removed merge-sheet sync/handlers that referenced the deleted `IsMergeSheetVisible` and `mergeSheet`; otherwise the page does not compile)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (CreateSut passes `INavigationService` mock; new REQ-SFT-22 test; superseded test deleted)
- `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs` (`AddSingleton(Mock.Of<INavigationService>())` in the SongFormViewModel graph test + using)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section)

### Red output (verbatim; compile-error Red, commit fffe4a66, Lane: RED)
```
SongFormViewModelTests.cs(26,20): error CS1729: 'SongFormViewModel' does not contain a constructor that takes 9 arguments
SongFormViewModelTests.cs(47,19): error CS1061: 'SongFormViewModel' does not contain a definition for 'OpenExistingSongCommand' and no accessible extension method 'OpenExistingSongCommand' accepting a first argument of type 'SongFormViewModel' could be found
```

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1`: 0 errors (NU1903 warnings only), 1 attempt.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1`: `Aprovado! - Com falha: 0, Aprovado: 668, Ignorado: 0, Total: 668` (baseline 668; delta 0 = +1 new REQ-SFT-22 test, -1 superseded).
- Step 3 census (Python scan) of `SongFormViewModel.cs`, `SongFormPage.xaml`, `SongFormViewModelTests.cs` for `CommitAsync(`, `ResolutionChoice`, `FieldDiff`, `MergeFieldRow`, `TargetHasManualEdits`, `ConfirmUpdateExisting`, `IsMergeSheetVisible`: `hits 0`.

### AC matrix
| AC | Implementation | Test |
|----|----------------|------|
| REQ-SFT-22 Open existing navigates to `../song-form?songId=N`, sheet closes | `SongFormViewModel.OpenExistingSongAsync` / `OpenExistingSongCommand`; XAML buttons | `OpenExistingSong_FromResolutionSheet_NavigatesToEditFormReplacingAddForm` |
| REQ-SFT-24 no overwrite path | `CommitAsync`, update-existing and merge flow removed from VM (census 0) | census |
| REQ-SFT-38 no merge sheet | merge sheet, `MergeFieldRow`, rows/commands removed (VM, XAML, code-behind) | census; superseded test removed |

### Superseded tests
| Where (develop) | Test | Superseded AC | Replacing REQ |
|---|---|---|---|
| `SongFormViewModelTests.cs:602` (plan cites :707; line drift) | `ConfirmUpdateExisting_TargetHasManualEdits_PopulatesMergeRows` (deleted) | AC-4.2 | REQ-SFT-38 |
| none found | tests of `ConfirmUpdateExistingCommand` / `SelectResolutionCandidateCommand`: none existed besides the above (census) | AC-1.1 "Update existing" | REQ-SFT-22/24 (covered by the new Open existing test) |

### Deviations / notes
- `SongFormPage.xaml.cs` edited though not in the owned list (mechanical removal, see above). Not a spec gap that blocks; flagging for review.
- Candidate-row button relabelled "Open existing" (was "Select") and bound to `OpenExistingSongCommand`, so tapping a fuzzy-candidate row also opens that song (REQ-SFT-23 direction); the Save flow itself is replaced in 5.3.
- Requested spec note: tasks.md 5.1 Files owned should list `SongFormPage.xaml.cs`; the `MergeFieldRow` file did not exist separately (nested class).

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier: **PASS** — 0 blockers, 0 warnings; census re-run (Python) zero hits; deviations (a) `SongFormPage.xaml.cs` edit and (b) single build for VM+XAML+code-behind judged acceptable; worktree 668/668.
- Merged into develop `f687e72d` (`--no-ff`). Post-merge on develop: build `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 668, Total: 668`.
- Open: on-device check of the resolution sheet → Open existing navigation (Helder's 7.2 gate).

## 5.2

### Checkpoint

_(not started)_

## 5.3

### Checkpoint

_(not started)_

## 5.4

### Checkpoint

_(not started)_

## 5.3b

### Checkpoint

_(not started)_

## 5.5

### Checkpoint

_(not started)_

## 5.6

### Checkpoint

_(not started)_

## 6.1

### Checkpoint

_(not started)_

## 6.2

### Checkpoint

_(not started)_

## 6.3

### Checkpoint

_(not started)_

## 7.1

### Checkpoint

_(not started)_

## 7.2 — Device gate

### Checkpoint

_(not started)_

