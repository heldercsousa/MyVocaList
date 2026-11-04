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

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier **PASS** (BUG-067 detach block unchanged; effective version `version ?? song.Version ?? ""` correct for REQ-SFT-29; 12 Moq edits purely mechanical). W1: REQ-SFT-29 cites the old `SongService.cs:155` (stale spec line ref, ignored). W2: unified duplicate message recorded in `design.md` § 3.2 note.
- Conflict with merged 3.3 (only `SongResolutionServiceTests.cs`: 3.2a's edits sat in 3 tests 3.3 deleted) resolved on the branch by taking develop's version (0 `UpdateSongAsync` refs remain) — `9f8e083`; worktree build 0 errors, 674/674.
- Merged `eab8b6b2`. Post-merge develop: build `0 Erro(s)`; test run 1: `Com falha: 1, Aprovado: 673, Total: 674` (failing test name NOT captured — output filter dropped it); merged tree identical to the branch tip (`rev-parse ^{tree}` equal) that passed 674/674; re-run once: `Com falha: 0, Aprovado: 674, Total: 674`. Treated as flaky (third intermittent failure this session — flagged to Helder).

## 3.3

### Checkpoint

_(not started)_

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier **PASS** (0 blockers/warnings). Red reason `Expected ExactLocalMatch / Actual NoMatch` judged valid (same cause as the plan's id assert). Census `NestedUnitOfWorkTests`: 7 hits, all `//`/`///` provenance comments, no code reference — meets the zero-code-reference intent. Info: three unused constructor fields → folded into 3.3b (tasks.md note).
- Merged `c59c61d1`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 666, Total: 666` (678 − 12).

## 3.3b

**Status:** To Review
**Plan:** `plan.md` § Task 3.3b · **Branch:** `task/sft-3.3b-shrink-resolution` · **Model:** Haiku · **Level:** C (mechanical, no new test; no-test decision recorded below) · **Review lane:** Standard
**Started:** 2026-11-04 · **Completed:** 2026-11-04

Context7 unavailable this session (per briefing); no new library API used (mechanical removal and construction-site edits), so no lookup was needed.

### Inherited (uncommitted edits, kept)
- `Domain/Resolution/ResolutionResults.cs`: `FieldDiff` deleted; `SongResolution(ResolutionKind Kind, int? ExactMatchSongId, IReadOnlyList<SongMatch> FuzzyCandidates)`.
- `Services/SongResolutionService.cs`: six `SongResolution` constructions reduced to 3 args.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`: four constructions reduced to 3 args.
- This section (previous draft).

### Withdrawn change (undone, reason recorded)
The previous implementor also removed the `SongResolutionService` constructor parameters and fields `_songRepository`, `_artistResolution`, `_songService`, and changed `SongResolutionServiceTests.CreateSut()` to 3 args. This is reverted: the constructor is restored exactly as on develop, and `SongResolutionServiceTests.cs` is restored to the develop version (`git checkout develop -- …`; the file no longer differs).
Reason: the permanent architecture gate `UnitOfWorkReadScopeTests.NoGovernedField_IsDereferencedAnywhere_InsideOrOutsideALambda` (REQ-UOW-36/37) requires governed constructor parameters on services that call `ExecuteReadAsync`. With the shrunk constructor the gate failed (`SongResolutionService.cs — calls ExecuteAsync/ExecuteReadAsync but zero governed constructor parameters were detected`). The dead fields stay under READ-SCOPE open item F4 (Helder). No architecture test was edited; no assertion, `Times`, or setup value changed.

### Changed files
- `Domain/Resolution/ResolutionResults.cs` (`FieldDiff` deleted; `SongResolution` 3-arg record)
- `Services/SongResolutionService.cs` (6 `SongResolution` constructions → 3 args; constructor unchanged vs develop)
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (4 `SongResolution` constructions → 3 args)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section)

Not changed vs develop: `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` (restored).

### Construction sites edited
- `Services/SongResolutionService.cs`: ExactExternalMatch, NoMatch (artist unresolved), ExactLocalMatch, FuzzyCandidates, NoMatch (final).
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`: 4 positional constructions → 3 args.

### Level C no-test decision
Mechanical removal of the unused `FieldDiff` member and its positional arguments from `SongResolution` construction sites; no new behaviour, no new AC. No test added or altered; existing suite covers the record through its consumers.

### Superseded tests
None. No test deleted, rewritten, or renamed.

### Build / test evidence (verbatim)
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → `ok dotnet build: 6 projects, 0 errors, 33 warnings (00:02:38.08)`
- Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` → `Aprovado! – Com falha: 0, Aprovado: 674, Ignorado: 0, Total: 674, Duração: 6 s - MyVocaList.Tests.dll (net10.0)`
- Count: 674 on this base; delta vs. base 0 (the briefing's 671 figure predates 3.4 merging; the count on this base is 674 and is unchanged by this task).
- Census `['FieldDiff','TargetHasManualEdits']` (Python `os.walk`, skipping `Docs`, `.git`, `bin`, `obj`, `.worktrees`): `hits: 0`.

### Verification
- `rtk proxy git diff develop -- Services/SongResolutionService.cs` shows only `SongResolution` construction hunks (no constructor change).
- Re-read after each edit: service constructor lines 13–25 match develop; `SongResolutionServiceTests.cs` matches develop.

### Checkpoint
_(none — section final)_

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier **CONDITIONAL PASS** (0 blockers). Only warning: unrelated flaky `MyVocaList.Tests.Integration.Repositories.SongRepositoryTests.AddAsync_ValidSong_PersistedAndReturnedById` — `DbUpdateException: An error occurred while saving the entity changes` (inner exception not captured) on run 1, green on run 2. Info: five `new SongResolution` sites, not six (cosmetic).
- Merged `5191b255`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671`.

## 3.4

### Checkpoint

_(not started)_

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier **PASS** (0 blockers/warnings; `ResolveAsync` byte-unchanged; censuses: `ResolutionChoice` 0, `CommitAsync(` only `UnitOfWork.cs:53,88`). Info: test file lost its UTF-8 BOM (cosmetic); `_artistRepository`/`_artistService` now dead → READ-SCOPE F4 (Helder).
- Merged `23428828`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671` (674 − 3).

## 3.2b

### Checkpoint

- 2026-11-04 (orchestrator) — **Pre-dispatch G3 checks (plan § 0.2.2):** (1) LEDGER: no live branch (BUG-080 merged+closed; CATALOG-PROP deferred, no branch). (2) `git worktree list`: main only. (3) `git branch -a`: only `feat/uow-phase-4-1-catalog`, `feat/uow-phase-4-venue` (merged 2026-08-24). **Clear.** Worktree `.worktrees/sft-3.2b`, branch `task/sft-3.2b-remove-hasmanualedits`, from develop after 3.3b's merge.

- 2026-11-04 (implementor) — **Status: To Review.** Inherited nothing (clean worktree). Context7 not needed (no external API). Not pushed (per briefing).
  - `ISongService`: new overload without `hasManualEdits`; old overload kept `[Obsolete("... Removed in 3.2c.")]`. `SongService`: old overload forwards to the new one (flag ignored); `song.HasManualEdits = ...` writer removed; 3.2a logic and BUG-067 `OriginalArtist = null` untouched. `SongFormViewModel` call site drops the `true` argument. No CS0121 ambiguity.
  - Edited `SongServiceTests.cs` (only the `hasManualEdits` argument dropped; no assertion/Times/return changed): UpdateSongAsync_ValidTitle_ReturnsSuccess, _SongNotFound_ReturnsFalse, _DuplicateTitleExcludingSelf_ReturnsFalse, _VersionChangedToNewVersionOfExistingTitle_Succeeds, _VersionChangedToExistingVersion_FailsDuplicate, _TitleEditedOnApiSourcedSong_KeepsExternalIdentity, _EditedTitleCollidesWithArtistTitleVersion_Rejected, _ExternalIdentityHeldByAnotherSong_Fails, _NewArtistName_CreatesArtistInSameUnitOfWork, _NewArtistCreationFails_ReturnsMessageAndDoesNotWrite, _BothArtistIdAndNewArtistName_Rejected, _WithLyrics_ReturnsSuccess, _WithExternalIdentity_PersistsProviderAndId, _WithNullExternalIdentity_DoesNotOverwriteExistingIdentity, _WithChangedArtistId_PersistsNewArtist, _NullArtistId_KeepsExistingArtist, _UnknownArtistId_ReturnsFalseAndDoesNotWrite, _ChangedArtistWithDuplicateTitle_ReturnsFalse, _WithVersion_PersistsVersion, _NullVersion_KeepsExistingVersion, _VersionTooLong_ReturnsFalse.
  - Edited `SongFormViewModelTests.cs` (mock setups/verifies, `It.IsAny<bool>()` / `true` dropped; Times unchanged): SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion, SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService, SaveAsync_EditMode_AfterHydration_PreservesArtistLink.
  - **Superseded-test row (D6):** `SongFormViewModelTests.LoadSongForEdit_ApiImportedWithoutManualEdits_LocksArtistField` (BUG-024/BUG-008 setup, `HasManualEdits = false`) -> renamed `LoadSongForEdit_ApiImportedSong_LocksArtistField`, `HasManualEdits` removed from the setup, first comment line `// [AC] REQ-SFT-28: the stored artist loads in the picked state`; assertion (`IsArtistLocked`) unchanged.
  - **Verification:** build `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` exit 0, 0 errors. Full suite: `Aprovado! - Com falha: 0, Aprovado: 671, Ignorado: 0, Total: 671` (baseline 671, delta 0). Census (os.walk, excl. obj/bin/Migrations): `hasManualEdits` only in ISongService.cs (4), SongService.cs (2), SongServiceUpdateIntegrationTests.cs (2), Bug068RegressionTests.cs (1). CS0618 for `UpdateSongAsync`: only SongServiceUpdateIntegrationTests.cs (52, 80) and Bug068RegressionTests.cs (31); other CS0618 hits are pre-existing `CreateSongWithUrlsAsync`.

  ### Changed files
  - `Domain/ServicesInterfaces/ISongService.cs`
  - `Services/SongService.cs`
  - `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
  - `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
  - `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md`

### Verifier + merge (orchestrator, 2026-11-04)
- Verifier **PASS** (every `UpdateSongAsync` Moq expression binds to the new overload; no EOL churn — `--stat` identical with `--ignore-cr-at-eol`; BUG-067 detach untouched). Commit `5c52a167`.
- Merged `712affbd`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671`.

## 3.2c

### Checkpoint

- 2026-11-04 (orchestrator) — **G3 checks:** LEDGER no live branch; `git worktree list` main only (+ `sft-3.2b` being removed, merged); `git branch -a` only the two merged 2026-08-24 UoW branches. **Clear.** Worktree `.worktrees/sft-3.2c`, branch `task/sft-3.2c-delete-obsolete-update`.

### Implementor entry (task 3.2c)

**Plan:** `plan.md` § Task 3.2c
**Status:** To Review
**Branch:** `task/sft-3.2c-delete-obsolete-update` (worktree `.worktrees/sft-3.2c`)
**Started:** 2026-11-04
**Completed:** 2026-11-04

**Inherited:** nothing. Fresh worktree at `e2f5ffe8` (develop ancestor check passed); no uncommitted edits existed when started.
**Tooling:** no new library API was used, so no Context7 lookup was required. Briefing says Context7 is available.

**Census before** (Python `os.walk` substring `hasManualEdits`; skipped bin, obj, .worktrees, Docs, .claude, .git, Migrations): 4 files, 10 hits.
- `Domain/ServicesInterfaces/ISongService.cs`: 53, 60, 68, 70 (obsolete overload + XML doc)
- `Services/SongService.cs`: 112, 114 (obsolete forwarder)
- `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs`: 53, 81
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`: 32
Hits outside the four files: none (matched the expected set, so no STOP).

**Census after:** `hasManualEdits` zero hits (outside `Migrations/`).

### Changed files:
- `Domain/ServicesInterfaces/ISongService.cs`: deleted the `[Obsolete]` `UpdateSongAsync(..., bool hasManualEdits, ...)` overload with its XML doc (-23 lines).
- `Services/SongService.cs`: deleted the `[Obsolete]` forwarder with its `<inheritdoc />` (-10 lines).
- `MyVocaList.Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs`: lines 53 and 81 drop the `hasManualEdits: true,` argument. No assertion or expected value changed.
- `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`: line 32 drops `hasManualEdits: false`. Line 129 `CreateSongWithUrlsAsync` call inserts `null` as the `newArtistName` argument, so it binds to the new overload (`artist!.Id, null, "Atomic URL Song", string.Empty, null, null, null, null, [...]`). Assertions unchanged.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md`: this section only.

**Superseded tests:** none. No test was deleted or rewritten; only argument lists at the four call sites changed.

**Level C no-test decision:** the task is a call-site argument removal plus deletion of an `[Obsolete]` forwarder. It owns no new acceptance criterion. The existing integration tests (`SongServiceUpdateIntegrationTests` x2, `Bug068RegressionTests`) keep exercising the surviving overloads, with unchanged assertions. No new test added.

**Moq note (3.2a lesson):** no Moq `UpdateSongAsync` expression exists in these four files; the census confirmed no other hit.

**Design concern:** none. **Spec gap:** none. **Spec note requested:** none.

### Verification evidence (verbatim)

Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1`, run alone:
```
    34 Aviso(s)
    0 Erro(s)

Tempo Decorrido 00:03:53.64
[exited with code 0]
```
The only CS0618 in that build is `MyVocaList/UI/ViewModels/SongFormViewModel.cs(554,43)` (expected, owned by 5.3b). The harness moved this build to background after 180 s; it ran alone and completed with exit 0.

Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` (single run; `SongRepositoryTests` flake did not occur):
```
Aprovado!  – Com falha:     0, Aprovado:   671, Ignorado:     0, Total:   671, Duração: 7 s - MyVocaList.Tests.dll (net10.0)
```
Delta vs baseline 671: 0.

**Pre-commit hook run (first commit attempt, blocked; --no-verify NOT used):** `Com falha: 1, Aprovado: 670, Total: 671`. Failing test: `MyVocaList.Tests.Integration.Repositories.BackupRepositoryTests.GetLatestSnapshotAsync_MixedTypes_ReturnsOnlySnapshot [FAIL]`. The full-suite run immediately before this one (above) had 0 failures.
**Focused re-run** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1 --no-build --filter "FullyQualifiedName~BackupRepositoryTests"`: `Aprovado: 4, Falha: 0, Total: 4`.
**Full-suite re-run after the hook failure** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1`: `Aprovado: 671, Falha: 0, Total: 671`. The failure did not reproduce on the re-runs. It is not the BUG-076 flake named in the briefing (`ObjectDisposedException`) and was not investigated further (out of scope for 3.2c).


Remaining CS0618 in the test build (expected only): `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs(200,32)` and `(219,33)`, both `CreateSongWithUrlsAsync(int, ...)`. `Bug068RegressionTests.cs` and `SongServiceUpdateIntegrationTests.cs` no longer warn.

**Files verified by re-read after edit:** the four changed files were re-checked via `git diff` (4 files, 4 insertions, 37 deletions; the test diffs are one-line changes each).


### Build notes + Verifier + merge (orchestrator, 2026-11-04)
- Commit `936fd20e` (Haiku). Verifier **CONDITIONAL PASS** (0 blockers): diff = obsolete overload + XML doc deleted; 2+1 call sites lost only the flag; `Bug068RegressionTests` `CreateSongWithUrlsAsync` moved to the new overload with correct alignment; census `hasManualEdits` → 0. W1 (this block + SHA) applied here.
- **SQLite flake evidence (W2):** Verifier run 1 `Com falha: 1, Aprovado: 670` — `SongRepositoryResolutionTests.DuplicateTitleVersion_ThrowsDbUpdateException`: `System.InvalidOperationException : Collection was modified; enumeration operation may not execute.` thrown inside Microsoft.Data.Sqlite pool clearing, from `DisposeAsync` → `EnsureDeletedAsync` (`SongRepositoryResolutionTests.cs:26`); the test body passed. Run 2: 671/0. Implementor's pre-commit hook run also failed once on `BackupRepositoryTests.GetLatestSnapshotAsync_MixedTypes_ReturnsOnlySnapshot` (green on retry). Hypothesis (unconfirmed): concurrent pool clears across parallel xUnit test classes. Escalated to Helder (new bug vs BUG-076).
- Merged `a9094bf5`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671`.

## 3.5a

**Plan:** `plan.md` § 3.5a · **Status:** To Review · **Started:** 2026-11-03 (inherited edits) · **Completed:** 2026-11-04
**Branch:** `task/sft-3.5a-song-readmodel` @ `.worktrees/sft-3.5a` (base develop verified)

### Inherited work
The previous implementor left correct uncommitted edits to `SongListItemDto.cs`, `SongRepository.cs`, `CatalogRepository.cs` and `SongSuggestionServiceTests.cs` (positional constructions). Reviewed against plan § 3.5a and kept unchanged. Its blocked note (unowned test-file compile failure) was resolved by the orchestrator extending Files owned.

### Owned-files extension (orchestrator decision, sequencing)
Target-typed `new(...)` constructions evaded the plan's census pattern (`SongListItemDto(`). The orchestrator added the following two test files to 3.5a's Files owned. Only the trailing `, false` (removed `HasManualEdits` argument) was dropped at each site; no assertion or expected value changed:
- `MyVocaList.Tests/Unit/Services/CatalogServiceTests.cs:69` — `GetPagedByArtistAsync` list setup (`new(1, "Bohemian Rhapsody", 1, "Queen", null, null)`)
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs:498` — `GetPagedSongsForListAsync_ReturnsFromRepository` list setup
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs:517` — `GetPagedSongsForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall` list setup

### Level C no-test decision
Task is DTO/projection plumbing (remove a DTO member and its projection assignment). Level C per `testing.md`: no mandatory new test. Existing tests were updated to the new 6-argument constructor; their assertions are unchanged.

### Changed files:
- `Contracts/DTOs/List/SongListItemDto.cs`
- `Infra/Repository/SongRepository.cs`
- `Infra/Repository/CatalogRepository.cs`
- `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs`
- `MyVocaList.Tests/Unit/Services/CatalogServiceTests.cs`
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md`

### Verification evidence
- Census (Python `os.walk`, bin/obj/.worktrees/Docs/.claude/.git/Migrations excluded): `HasManualEdits` count in `SongListItemDto.cs` = 0, `SongRepository.cs` = 0, `CatalogRepository.cs` = 0.
- Census for 7-argument `SongListItemDto` constructions: none remain; the remaining `false` arguments in the scan belong to other record types (`ArtistListItemDto`, `ArtistListItem`, `SongFormViewModelTests` local record).
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` exit code 0, no `error` lines in output (summary line not captured in the harness output file; exit code 0 used as the pass signal).
- Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` (run once, no re-run needed):
  `Aprovado!  � Com falha:     0, Aprovado:   671, Ignorado:     0, Total:   671, Dura��o: 7 s - MyVocaList.Tests.dll (net10.0)`
  Passed 671, Failed 0, Total 671.
- Context7 unavailable this session; no new library API used (plumbing only).
- Emulator smoke deferred to Helder's device gate.

### Build notes + Verifier + merge (orchestrator, 2026-11-04)
- Commit `9425579d` (finishing Haiku implementor; first implementor stopped on 3 target-typed `new(` sites in unowned files → owned files extended). Build `0 Erro(s)`; 671/671.
- Verifier **CONDITIONAL PASS** (0 blockers; W1 SHA/Build notes → this block).
- Merged `39dbc58d`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671`. Combined census `HasManualEdits`: only `Artist.cs:9`, `Song.cs:14`, `ArtistConfiguration.cs:33`, `SongConfiguration.cs:48`, `ArtistService.cs:107` (3.6a/3.6b).

## 3.5b

**Status:** To Review
**Started:** 2026-11-04
**Completed:** 2026-11-04
**Lane:** Standard · Level C · Branch `task/sft-3.5b-artist-readmodel`

### Context
- Context7 unavailable this session; no new library API is introduced (positional record shrink only, existing EF projections). Verified via existing codebase patterns.
- Inherited state: clean worktree at start (no prior edits).

### Census (plan § 0.7 `os.walk` scan, before edits)
Terms `['ArtistListItem(','ArtistListItemDto(','HasManualEdits']`. Owned-file hits: `ArtistListItemDto.cs:3,7`, `ArtistListItem.cs:10,12`, `ArtistRepository.cs:47,51,74`, `SongFormViewModelTests.cs:428,452,1054,1056,1097`, `ArtistSuggestionServiceTests.cs:50`. Plan line numbers for `SongFormViewModelTests.cs` (424, 448, 491, 1177, 1179, 1220) had drifted; located by content. No mapper uses `ArtistListItemDto` (no target-typed `new(` hit).
Out-of-scope hits (untouched): `Domain/Entity/Artist.cs:9`, `Domain/Entity/Song.cs:14`, `Infra/EntityEFConfig/ArtistConfiguration.cs:33`, `Infra/EntityEFConfig/SongConfiguration.cs:48` (3.6b); `Services/ArtistService.cs:107` (3.6a); `Infra/Repository/CatalogRepository.cs:48`, `Infra/Repository/SongRepository.cs:46`, `Contracts/DTOs/List/SongListItemDto.cs:10` (3.5a).

### Changed files:
- `Domain/ReadModels/ArtistListItem.cs`
- `Contracts/DTOs/List/ArtistListItemDto.cs`
- `Infra/Repository/ArtistRepository.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
- `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section only)

### Edits (post-edit re-read done for each)
- `ArtistListItem(int Id, string Name, string ExternalProvider, int CatalogCount)`; `<param name="HasManualEdits">` XML doc removed.
- `ArtistListItemDto(int Id, string Name, string? ExternalProvider, int CatalogCount)`.
- `ArtistRepository.GetPagedAsync` projection and `SearchByNameAsync` projection: `HasManualEdits` argument removed; SQL still one statement (scalar `CatalogEntries.Count()` retained).
- `SongFormViewModelTests.cs`: 5 positional constructions, `false` argument dropped (`:428, :452, :1054, :1056, :1097`). Assertions and expected values unchanged.
- `ArtistSuggestionServiceTests.cs:50`: `false` argument dropped.

### Test decision
Level C (DTO/read-model plumbing, projection simplification). No new test. Existing suite covers the projections and the VM/Service paths that consume them. No AC is newly implemented here; REQ-SFT-37 is verified by the census and by the absence of the field from the read models. Superseded tests: none (no assertions changed).

### Build notes
Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → 0 errors, 34 warnings (1st attempt, `ok dotnet build: 6 projects, 0 errors, 34 warnings`). Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` → `Aprovado! – Com falha: 0, Aprovado: 671, Ignorado: 0, Total: 671` (baseline 671, delta 0). No flaky failures this run.
Emulator smoke: deferred to Helder's device gate (not run; no `-t:Run`, no emulator).
Census after edits: zero `HasManualEdits` / old-arity `ArtistListItem(` hits in owned files; remaining hits are only the out-of-scope files listed above.
Commit SHA: see commit (recorded in handback).
Files written and re-read: `ArtistListItem.cs` (7-12), `ArtistListItemDto.cs` (3-8), `ArtistRepository.cs` (43-74), `SongFormViewModelTests.cs` (428, 452, 1054, 1056, 1097), `ArtistSuggestionServiceTests.cs` (50).

### Spec notes requested
None required. No `> **Spec updated**` note needed: the change is within REQ-SFT-37 and plan § 3.5b as written.

### 3.5b — Verifier + merge (orchestrator, 2026-11-04)
- 3.5b commit `6cf6e34a`. Verifier **CONDITIONAL PASS** (0 blockers; W1 SHA → recorded here; W2 the implementor wrote "Context7 unavailable" — the briefing said available; no library API was needed, so no impact; noted for Helder). Merged `408929e7`. Post-merge develop: `0 Erro(s)`; `Aprovado! – Com falha: 0, Aprovado: 671, Total: 671`.

## 3.6a

### Checkpoint

ITF: SFT-3.6a — Services/ArtistService.cs — root cause: `ArtistService.UpdateArtistAsync` still writes the retired flag (`artist.HasManualEdits = true;`, :107), the last writer before M1 drops the column (REQ-SFT-37) — expected 1 line(s).
- ITF commit `8db66028` (`Lane: ITF (1 files, 1 lines)`); the first pre-commit-hook test run failed one unnamed test (output not captured), the second passed unchanged (5th intermittent failure this session). Verifier **PASS**. Merged `29de1054`; post-merge develop `0 Erro(s)`, 671/671.

## 3.6b

### Checkpoint

- 2026-11-04 (orchestrator) — **Pre-dispatch G3 / § 0.2.2 checks (migration task):** (1) LEDGER: no live branch (CATALOG-PROP spec-only, deferred; BUG-080 merged `8f5d716c` with **no** migration). (2) `git worktree list`: main + `sft-3.6a` (merged, being removed). (3) `git branch -a --no-merged develop`: only `remotes/origin/worktree-agent-aaae95a5d8a6c56ec` (old agent branch, matches no G3 pattern). **Clear.** Snapshot rule: worktree created from current develop `29de1054`+, so the scaffold is generated against the merged snapshot. Worktree `.worktrees/sft-3.6b`, branch `task/sft-3.6b-m1-drop-hasmanualedits`. Part A dispatched.

- 2026-11-04 (Part A implementor, Sonnet) — **Part A Step 1-2 done (Red).** Context7 not queried; EF Core APIs (`IMigrator`, `GetMigrations`, `SqlQueryRaw`) verified via the existing codebase pattern `MyVocaList.Tests/Integration/Migrations/VenuesSeedMigrationDownTests.cs`. Inherited: clean worktree, nothing half-done. Tests: `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (3 tests; temp DB files deleted in `DisposeAsync`). The guard test `M1_TableRebuild_KeepsUniqueCollatedIndexes` inserts with/without the `HasManualEdits` column depending on the schema (the pre-M1 NOT NULL column otherwise breaks the guard).
- Red output (verbatim, trimmed to the result lines): `M1_DropsHasManualEditsFromSongsAndArtists [FAIL]` — `Assert.DoesNotContain() Failure: Item found in collection ... Found: "HasManualEdits"`; `M1_ExistingRowsSurvive [FAIL]` — `System.InvalidOperationException : Migration 'DropHasManualEdits' not found (or has no predecessor).`; `M1_TableRebuild_KeepsUniqueCollatedIndexes` PASSED (guard, as the plan allowed). Totals: `Com falha! – Com falha: 2, Aprovado: 1, Ignorado: 0, Total: 3`.
- Next: Step 3 (delete four model lines, build, leave uncommitted) -> handback "model edited, awaiting scaffold".

### Part B (Green) - 2026-11-04, Sonnet implementor

Context7 not queried (no new EF API used; migration is orchestrator-scaffolded). Inherited: four model edits + scaffolded migration/Designer/snapshot, uncommitted; reviewed against the plan, all correct, nothing changed beyond this section.

**Step 3b - generated migration (verbatim).**
`Up`: `migrationBuilder.DropColumn(name: "HasManualEdits", table: "Songs");` then `migrationBuilder.DropColumn(name: "HasManualEdits", table: "Artists");`
`Down`: `migrationBuilder.AddColumn<bool>(name: "HasManualEdits", table: "Songs", type: "INTEGER", nullable: false, defaultValue: false);` then the same for `table: "Artists"`.
No other operation. Snapshot diff: only the two `b.Property<bool>("HasManualEdits").HasColumnType("INTEGER");` blocks removed (Artists ~line 161, Songs ~line 374).

### Changed files
- `Domain/Entity/Song.cs`
- `Domain/Entity/Artist.cs`
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/EntityEFConfig/ArtistConfiguration.cs`
- `Infra/Migrations/20261104150926_DropHasManualEdits.cs` (new)
- `Infra/Migrations/20261104150926_DropHasManualEdits.Designer.cs` (new)
- `Infra/Migrations/AppDbContextModelSnapshot.cs`
- `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (Red commit 439f95ad, unchanged in Part B)
- this task-log section

### Verification evidence
- Filtered `SongMigrationTests`: `Aprovado! - Com falha: 0, Aprovado: 3, Ignorado: 0, Total: 3`.
- Full suite run 1: `Com falha: 1, Aprovado: 673, Total: 674` - failing `PersonServiceReadScopeTests.SearchPersonsAsync_TooShortTerm_ShortCircuitsWithoutExecutingRead` (PersonServiceReadScopeTests.cs:84), unrelated; message not captured (stack top only). Re-runs 2 and 3: `Com falha: 0, Aprovado: 674, Total: 674` (baseline 671, delta +3).
- Build (`MyVocaList.csproj -f net10.0-android -m:1`): `0 Erro(s)`, 28 warnings.
- `dotnet ef migrations has-pending-model-changes`: "No changes have been made to the model since the last migration."
- Census `HasManualEdits` (.cs/.xaml, excl. bin/obj/.worktrees/Docs/.claude/.git/Migrations): ZERO HITS.

### AC matrix
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-SFT-37 | Songs/Artists no longer have HasManualEdits | DropHasManualEdits migration + model edits | `M1_DropsHasManualEditsFromSongsAndArtists` |
| REQ-SFT-37 | Existing rows survive the drop | same | `M1_ExistingRowsSurvive` |
| REQ-SFT-52 | Unique collated indexes preserved after table rebuild | same | `M1_TableRebuild_KeepsUniqueCollatedIndexes` (guard) |

### Verifier + merge (orchestrator, 2026-11-04)
- Red `439f95ad`, Green `6c290e27`; scaffold `20261104150926_DropHasManualEdits` by the orchestrator (generated files not opened by the orchestrator; Part B verified Up = 2 x DropColumn, Down = 2 x AddColumn<bool> default false). Part B's first full run had 1 intermittent failure (`PersonServiceReadScopeTests.SearchPersonsAsync_TooShortTerm_ShortCircuitsWithoutExecutingRead`), green on 2 re-runs (6th intermittent failure this session).
- Verifier **PASS** (0 warnings; the guard's conditional insert does not weaken REQ-SFT-52).
- Merged `a6a34647`. Post-merge develop: `0 Erro(s)`; `Aprovado! - Com falha: 0, Aprovado: 674, Total: 674`; `has-pending-model-changes`: "No changes have been made to the model since the last migration." `database update` on `.claude/MyVocaList.db`: relative path failed (SQLite Error 14), absolute path succeeded (`Done.`); DB committed in `e7dbf9a2`; plan section 0.5 note added.

## 3.7

### Checkpoint

- 2026-11-04 (orchestrator) — **Pre-dispatch G3 / § 0.2.2 checks (migration task):** (1) LEDGER: no live branch (CATALOG-PROP spec-only, deferred; BUG-080 merged with no migration). (2) `git worktree list`: main only. (3) `git branch -a --no-merged develop`: only `remotes/origin/worktree-agent-aaae95a5d8a6c56ec` (old agent branch, no G3 pattern). **Clear.** Snapshot rule: worktree from develop `5789f115` (includes M1 `20261104150926_DropHasManualEdits`). Worktree `.worktrees/sft-3.7`, branch `task/sft-3.7-m2-external-identity-unique`. Part A dispatched.

### Part A (Sonnet implementor, 2026-11-04)
**Status:** Part A Red done; model edit pending commit by scaffold flow. Context7 not needed (raw-SQL tests reuse the `SongMigrationTests` helpers). Inherited: clean worktree.

Red (`dotnet test ... -m:1 --filter "FullyQualifiedName~SongMigrationTests"`): `Failed: 3, Passed: 4, Total: 7`.
- `M2_UniqueIndex_RejectsSecondSongWithSameIdentity` FAIL: `Assert.ThrowsAny() Failure: No exception was thrown`
- `M2_IndexName_IsComposite` FAIL: `Assert.True() Failure Expected: True Actual: False`
- `M2_DuplicateIdentities_LowestIdKeepsIdentity_OthersClearedNotDeleted` FAIL: songs 2 and 3 still `Deezer|3135556` (expected `null|null`)
- `M2_NullIdentities_AllowMany` PASS (guard: holds before and after M2); three `M1_*` PASS.

### Changed files:
- `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs`
- `Infra/EntityEFConfig/SongConfiguration.cs` (model edit; committed in the Part B Green commit)
- this task-log section

### Part B (Sonnet implementor, 2026-11-04)
**Status:** To Review. Context7 not needed (no new API; raw `migrationBuilder.Sql` only). Inherited: Red `67327f7e`; uncommitted `SongConfiguration.cs` + orchestrator scaffold (migration, Designer, snapshot), reviewed and kept.

**Step 3b - generated migration check: OK** (only the index swap; snapshot diff only swaps the index). Generated, verbatim:
```csharp
// Up
migrationBuilder.DropIndex(name: "IX_Songs_ExternalId", table: "Songs");
migrationBuilder.CreateIndex(name: "IX_Songs_ExternalProvider_ExternalId", table: "Songs",
    columns: new[] { "ExternalProvider", "ExternalId" }, unique: true);
// Down
migrationBuilder.DropIndex(name: "IX_Songs_ExternalProvider_ExternalId", table: "Songs");
migrationBuilder.CreateIndex(name: "IX_Songs_ExternalId", table: "Songs", column: "ExternalId");
```
**Step 4 - inserted in `Up` between DropIndex and CreateIndex:**
```csharp
migrationBuilder.Sql(
    "UPDATE Songs SET ExternalId = NULL, ExternalProvider = NULL " +
    "WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
    "AND Id NOT IN (SELECT MIN(Id) FROM Songs WHERE ExternalId IS NOT NULL AND ExternalProvider IS NOT NULL " +
    "GROUP BY ExternalProvider, ExternalId);");
```
`Down` got a comment only (cleared identities are not restored).

### Changed files (Part B)
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/Migrations/20261104154053_SongExternalIdentityUnique.cs`
- `Infra/Migrations/20261104154053_SongExternalIdentityUnique.Designer.cs`
- `Infra/Migrations/AppDbContextModelSnapshot.cs`
- `MyVocaList.Tests/Integration/Migrations/SongMigrationTests.cs` (Red commit `67327f7e`, unchanged in Green)
- this task-log section

### Verification evidence (Part B)
- `SongMigrationTests` filtered: `Aprovado! - Com falha: 0, Aprovado: 7, Total: 7`.
- Full suite `-m:1`: `Aprovado! - Com falha: 0, Aprovado: 678, Total: 678` (baseline 674, delta +4).
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1`: `Build succeeded` (0 errors; NU1903 warnings only).
- `dotnet ef migrations has-pending-model-changes`: "No changes have been made to the model since the last migration."

### AC matrix (Part B)
| AC | Implementation | Test |
|----|----------------|------|
| REQ-SFT-39 unique (ExternalProvider, ExternalId) | `SongConfiguration` composite unique index; migration | `M2_UniqueIndex_RejectsSecondSongWithSameIdentity` |
| REQ-SFT-39 index name | default `IX_Songs_ExternalProvider_ExternalId` | `M2_IndexName_IsComposite` |
| REQ-SFT-39 lowest Id keeps identity, others cleared not deleted | `Up` dedupe SQL | `M2_DuplicateIdentities_LowestIdKeepsIdentity_OthersClearedNotDeleted` |
| REQ-SFT-39 null identities unaffected | unique index ignores NULLs | `M2_NullIdentities_AllowMany` |

### Verifier + merge (orchestrator, 2026-11-04)
- Red `67327f7e`, Green `6c3c4790`; scaffold `20261104154053_SongExternalIdentityUnique` by the orchestrator (generated files not opened by it; Part B confirmed Up = DropIndex + CreateIndex(unique) only, then inserted the design section 7 dedupe SQL between them).
- Verifier **PASS** (0 warnings). Edge cases reasoned: a row with ExternalId set and ExternalProvider null is untouched and allowed (SQLite NULLs distinct in unique indexes); provider case variants are distinct identities under BINARY collation, consistently for both the GROUP BY dedupe and the index.
- Merged `4e81916b`. Post-merge develop: `0 Erro(s)`; `Aprovado! - Com falha: 0, Aprovado: 678, Total: 678`; `has-pending-model-changes`: no changes; `database update` (absolute path) on `.claude/MyVocaList.db`: `Done.` (DB committed with this record).

## 3.8

### Checkpoint

- 2026-11-04 (orchestrator) — **Pre-dispatch:** `findings.md` § G5 Verdict = `G5: PASS` (confirmed). **G3 / § 0.2.2:** LEDGER no live branch; `git worktree list` main only; `git branch -a --no-merged develop` only the old `remotes/origin/worktree-agent-aaae95a5d8a6c56ec`. **Clear.** Snapshot rule: worktree from develop `75966b9c` (includes M1 + M2). Worktree `.worktrees/sft-3.8`, branch `task/sft-3.8-m3-fts5`. Part A (Sonnet) dispatched; Part B will be Opus (Architectural lane).
**Part A (Sonnet) — 2026-11-04.** Context7 available this session; `UseSqlReturningClause` verified via Context7 `/dotnet/entityframework.docs` (query "UseSqlReturningClause(false) ToTable ...", docs `sqlite/misc.md` + EF Core 8 what's-new): `modelBuilder.Entity<T>().ToTable(tb => tb.UseSqlReturningClause(false))`; Infra pins `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 (Directory.Packages.props). Inherited nothing (clean worktree).

### Part A Red output (commit: see git log, Lane: RED)
Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1 --filter "FullyQualifiedName~SongMigrationTests"` -> Failed 9, Passed 8, Total 17.
- FAIL `FullMigrate_AllSevenFtsTriggersExist` — Assert.Contains() Failure: Not found: "SongSearch_ai"
- FAIL `FullMigrate_CreatesSongSearchAndArtistSearch` — Assert.Contains() Failure: Not found: "SongSearch"
- FAIL `M3_Backfill_OneRowPerSongAndArtist` — SqliteException: SQLite Error 1: 'no such table: SongSearch'.
- FAIL `SongInsertViaEf_AddsSongSearchRow` — SqliteException: no such table: SongSearch (EF insert itself passed; fails on the FTS assertion)
- FAIL `SongTitleUpdateViaEf_ReplacesSongSearchRow` — SqliteException: no such table: SongSearch
- FAIL `SongExecuteDelete_RemovesSongSearchRow` — SqliteException: no such table: SongSearch
- FAIL `ArtistInsertViaEf_AddsArtistSearchRow` — SqliteException: no such table: ArtistSearch
- FAIL `ArtistRenameViaEf_UpdatesSongSearchAndArtistSearch` — SqliteException: no such table: SongSearch
- FAIL `ArtistDelete_CascadeRemovesSongSearchRows` — SqliteException: no such table: SongSearch
- PASS (vacuous until M3 exists) `M3_Down_DropsTablesAndTriggers` — "latest" == SongExternalIdentityUnique, so nothing to drop; becomes meaningful once M3 exists.
- PASS (unchanged) M1_DropsHasManualEditsFromSongsAndArtists, M1_ExistingRowsSurvive, M1_TableRebuild_KeepsUniqueCollatedIndexes, M2_DuplicateIdentities..., M2_UniqueIndex_RejectsSecondSongWithSameIdentity, M2_NullIdentities_AllowMany, M2_IndexName_IsComposite.

### Part B (Opus) — 2026-11-04
**Status:** To Review. Context7 available this session (Part A verified `UseSqlReturningClause` against EF Core 10 docs; no new API used in Part B — `migrationBuilder.Sql` only). Inherited (uncommitted, kept as-is, correct per plan): both configurations' `ToTable(..., tb => tb.UseSqlReturningClause(false))` (Part A Step 3) and the orchestrator's scaffold (migration + Designer + snapshot).

**Step 3b — generated migration (verbatim, before editing):**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{

}

protected override void Down(MigrationBuilder migrationBuilder)
{

}
```
Snapshot diff vs. `75966b9c`: annotation-only — `+ b.HasAnnotation("Sqlite:UseSqlReturningClause", false);` inside the `Artist` and the `Song` entity blocks (4 lines added incl. blank lines, 0 removed). No table/column/index operation → not stale; proceeded.

**Step 4 — final `Up` (11 `Sql` calls, one statement each, DDL verbatim from `design.md` § 6.2):**
1. `CREATE VIRTUAL TABLE SongSearch USING fts5(title, artist, tokenize='trigram remove_diacritics 1')`
2. `INSERT INTO SongSearch(rowid, title, artist) SELECT s.Id, s.Title, a.Name FROM Songs s JOIN Artists a ON a.Id = s.ArtistId`
3. `CREATE TRIGGER SongSearch_ai AFTER INSERT ON Songs …`
4. `CREATE TRIGGER SongSearch_au AFTER UPDATE OF Title, ArtistId ON Songs …` (delete + insert)
5. `CREATE TRIGGER SongSearch_ad AFTER DELETE ON Songs …`
6. `CREATE TRIGGER SongSearch_artist_au AFTER UPDATE OF Name ON Artists …` (UPDATE SongSearch.artist for the artist's songs)
7. `CREATE VIRTUAL TABLE ArtistSearch USING fts5(name, tokenize='trigram remove_diacritics 1')`
8. `INSERT INTO ArtistSearch(rowid, name) SELECT Id, Name FROM Artists`
9. `CREATE TRIGGER ArtistSearch_ai AFTER INSERT ON Artists …`
10. `CREATE TRIGGER ArtistSearch_au AFTER UPDATE OF Name ON Artists …` (delete + insert)
11. `CREATE TRIGGER ArtistSearch_ad AFTER DELETE ON Artists …`

`Down` (9 calls): `DROP TRIGGER IF EXISTS` SongSearch_ai, SongSearch_au, SongSearch_ad, SongSearch_artist_au, ArtistSearch_ai, ArtistSearch_au, ArtistSearch_ad; then `DROP TABLE IF EXISTS SongSearch`, `DROP TABLE IF EXISTS ArtistSearch`. Header comment carries the rebuild constraint sentence. No annotation operations existed to keep (generated Up/Down were empty).

**DDL review against the real schema (no defect found):** `Songs.Id/Title/ArtistId` and `Artists.Id/Name` match the snapshot; `Songs.ArtistId` is required (`SongConfiguration.cs`, `IsRequired()`) so the backfill's inner `JOIN` drops no song; artist delete cascades via FK (`ArtistConfiguration.cs`, `OnDelete(Cascade)`) and SQLite FK actions fire `SongSearch_ad` (NOT proven by `ArtistDelete_CascadeRemovesSongSearchRows` — that test deletes EF-tracked songs (EF issues the DELETEs); the DB-level `ON DELETE CASCADE` path was probed correct by the Verifier but has no regression test (follow-up in tasks.md 3.8) [corrected by the orchestrator 2026-11-04]); artist rename is covered by `SongSearch_artist_au` (proven by `ArtistRenameViaEf_UpdatesSongSearchAndArtistSearch`). Note (not a defect, no action): `SongSearch_au` is `OF Title, ArtistId`, so an `UPDATE` that sets `Id` would desync — no code path updates a PK.

### Verification evidence
- Filtered `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1 --filter "FullyQualifiedName~SongMigrationTests"` → `Aprovado!  – Com falha:     0, Aprovado:    17, Ignorado:     0, Total:    17, Duração: 4 s` (`M3_Down_DropsTablesAndTriggers` now meaningful: latest = `AddSearchIndexes`, rolled back to `SongExternalIdentityUnique`).
- Full `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` → `Aprovado!  – Com falha:     0, Aprovado:   688, Ignorado:     0, Total:   688, Duração: 7 s` (baseline 678 → 688, +10). All EF write tests pass on both the trigger-free `EnsureCreated` schema and migrated DBs; no flaky failure observed (single run).
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → `6 projects, 0 errors, 28 warnings (00:02:31.58)`.
- `dotnet ef migrations has-pending-model-changes -p Infra/MyVocaList.Infra.csproj -s Infra/MyVocaList.Infra.csproj` → `No changes have been made to the model since the last migration.`
- Post-edit re-read: migration `.cs` compared line-by-line with `design.md` § 6.2 (lines 438-464).

### AC matrix
| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-SFT-34 | all seven FTS triggers exist after a full migrate | `AddSearchIndexes.Up` calls 3-6, 9-11 | `FullMigrate_AllSevenFtsTriggersExist` |
| REQ-SFT-34 | both FTS tables exist | `Up` calls 1, 7 | `FullMigrate_CreatesSongSearchAndArtistSearch` |
| REQ-SFT-34 / INV-SFT-3 | backfill one row per song/artist, artist name in `SongSearch.artist` | `Up` calls 2, 8 | `M3_Backfill_OneRowPerSongAndArtist` |
| REQ-SFT-34 | EF song insert succeeds (RETURNING off) and indexes | `SongSearch_ai`; `SongConfiguration` | `SongInsertViaEf_AddsSongSearchRow` |
| REQ-SFT-34 | EF title update replaces the row | `SongSearch_au` | `SongTitleUpdateViaEf_ReplacesSongSearchRow` |
| REQ-SFT-34 | `ExecuteDeleteAsync` removes the row | `SongSearch_ad` | `SongExecuteDelete_RemovesSongSearchRow` |
| REQ-SFT-34 | EF artist insert succeeds and indexes | `ArtistSearch_ai`; `ArtistConfiguration` | `ArtistInsertViaEf_AddsArtistSearchRow` |
| REQ-SFT-34 | artist rename updates both tables | `SongSearch_artist_au`, `ArtistSearch_au` | `ArtistRenameViaEf_UpdatesSongSearchAndArtistSearch` |
| REQ-SFT-34 | artist delete (cascade) removes song + artist rows | `SongSearch_ad` (via FK cascade), `ArtistSearch_ad` | `ArtistDelete_CascadeRemovesSongSearchRows` |
| REQ-SFT-34 | rollback drops tables and triggers | `AddSearchIndexes.Down` | `M3_Down_DropsTablesAndTriggers` |

REQ-SFT-19 and REQ-SFT-30…33 are enabled by this schema but exercised by the 3.9 / 3.9b query tests, not here. No superseded tests (none deleted or edited).

### Changed files:
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/EntityEFConfig/ArtistConfiguration.cs`
- `Infra/Migrations/20261104161213_AddSearchIndexes.cs` (created)
- `Infra/Migrations/20261104161213_AddSearchIndexes.Designer.cs` (created, generated, untouched)
- `Infra/Migrations/AppDbContextModelSnapshot.cs` (generated, untouched)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section)

### Verifier + merge (orchestrator, 2026-11-04)
- Red `c820a6a9`, Green `b0aeef2d`; scaffold `20261104161213_AddSearchIndexes` by the orchestrator (generated Up/Down empty; snapshot gained only the two `Sqlite:UseSqlReturningClause` annotations). The commit hook hit one intermittent failure (`SongRepositoryResolutionTests.GetFuzzyCandidatePoolAsync_RespectsArtistScopeAndTake`), green on re-runs (7th this session).
- Verifier (Opus) **CONDITIONAL PASS**, 0 blockers: all 11 statements match design section 6.2 character for character; trigger set correct against the real schema (ArtistId change and DB-level FK cascade probed correct in SQLite). W1: DB-level cascade has no regression test (follow-up queued). W2: the over-claim above corrected. Info: EF1002 analyzer warnings in the test helpers (constant/integer interpolation only).
- Merged `eca9f708`. Post-merge develop: `0 Erro(s)`; `Aprovado! - Com falha: 0, Aprovado: 688, Total: 688`; `has-pending-model-changes`: no changes; `database update` (absolute path) on `.claude/MyVocaList.db`: `Done.`
- Orchestrator post-merge step: FTS-trigger rule added to `.claude/library/constraints-reference.md` § EF Core / SQLite.

## 3.9

**Status:** To Review

### Orchestrator decision 2026-11-04 (Option A, recorded in tasks.md on develop 55d906e9)
Also owned: `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (only 3 forwarding members in `ThrowOnAddSongRepository`). `SongRepository` gets optional `ILogger<SongRepository>? logger = null`, stored as `logger ?? NullLogger<SongRepository>.Instance`.

### Red output (dotnet test --filter SongRepositorySearchTests, compile errors, excerpt)
- CS1061 `SongRepository` has no `SearchTitleCandidatesAsync` (lines 71, 79, 87, 97, 107, 124, 132, 151, 152)
- CS1061 `SongRepository` has no `FindByExternalIdsAsync` (line 167)
- CS1061 `SongRepository` has no `FindByTitleAndArtistNamesAsync` (lines 177, 188, 190)
- CS0103 `FtsQueryBuilder` does not exist (lines 52, 60, 65)

### Superseded tests
None (all new; nothing deleted or rewritten).

### Deviations
- `FtsQueryBuilder` and `BuildTrigramMatch` are `public` (plan: `internal`). The Red test class (`MyVocaList.Tests`) calls them directly and `Infra` has no `InternalsVisibleTo`; the csproj is not owned. 3.9b still consumes it in-assembly.
- `FindByTitleAndArtistNamesAsync` runs one collated indexed query per triple (loop, results de-duplicated by SongId) rather than one combined statement; the batch is a result page.

### Changed files:
- `Domain/ReadModels/SongSearchHit.cs` (new)
- `Domain/RepositoryInterface/ISongRepository.cs`
- `Infra/Repository/SongRepository.cs` (3 methods, optional `ILogger<SongRepository>? logger = null`)
- `Infra/Repository/FtsQueryBuilder.cs` (new)
- `MyVocaList.Tests/Integration/Repositories/SongRepositorySearchTests.cs` (new, Red commit de57f010)
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (3 forwarding members in `ThrowOnAddSongRepository` only)
- `Docs/.../task-log.md` (this section)

### Verification evidence
- Focused: `Aprovado! Com falha: 0, Aprovado: 26, Total: 26` (SongRepositorySearchTests)
- Full suite: `Com falha: 0, Aprovado: 714, Total: 714` (baseline 688, delta +26)
- Android build: `ok dotnet build: 6 projects, 0 errors, 28 warnings`

### AC matrix
| AC | Implementation | Test |
|---|---|---|
| REQ-SFT-30 | FtsQueryBuilder.BuildTrigramMatch, SearchTitleCandidatesAsync | BuildTrigramMatch_DontCry_ReturnsQuotedWindowsOred, SearchTitleCandidatesAsync_DontCry/DonCry_IncludesDontCry |
| REQ-SFT-31 | FTS trigram remove_diacritics | SearchTitleCandidatesAsync_NaoSeiAnyCase_MatchesAccentedTitle |
| REQ-SFT-32 | bm25 over title+artist columns | SearchTitleCandidatesAsync_GunsCry_IncludesDontCryViaArtist |
| REQ-SFT-33 | collated title prefix + missing-index catch | ..._ShortQuery_UsesCollatedTitlePrefix, BuildTrigramMatch_NoWordOfThreeChars_ReturnsNull, ..._IndexMissing_ReturnsEmptyAndPrefixStillWorks |
| REQ-SFT-02 | quote doubling + parameters | BuildTrigramMatch_QuoteInWord_IsDoubled, ..._FtsSyntaxCharacters_DoNotThrow |
| REQ-SFT-04 | LIMIT @take / Take | SearchTitleCandidatesAsync_RespectsTake |
| REQ-SFT-05(a) | FindByExternalIdsAsync | FindByExternalIdsAsync_ReturnsHoldersOnly |
| REQ-SFT-05(b) | FindByTitleAndArtistNamesAsync | ..._CollatedTripleEmptyVersion_Returns, ..._VersionMustMatch |

### Requested spec notes
None required; optionally note `FtsQueryBuilder` is public (see Deviations).

## 3.9b

**Status:** To Review
**Red SHA:** 61deac4d (CS1061 x11, see below) | **Green SHA:** see handback / `git log` (commit `feat(artist-repo): FTS5 trigram artist-name candidates`)

### Red (verbatim, CS1061)
`ArtistRepositorySearchTests.cs(42,32): error CS1061: 'ArtistRepository' does not contain a definition for 'SearchNameCandidatesAsync'` (same at lines 50, 58, 66, 81, 89, 105, 106, 125, 126; localized pt-BR in raw output).

### Implementation notes
- `IArtistRepository.SearchNameCandidatesAsync(name, take, ct)`; `ArtistRepository` ctor gains optional `ILogger<ArtistRepository>? logger = null` (NullLogger default; existing `new ArtistRepository(_db)` sites compile).
- FTS path: `SqlQueryRaw<int>` ids from `ArtistSearch MATCH @m ORDER BY bm25(ArtistSearch), length(name) LIMIT @take`, then one `Artists` query (AsNoTracking) re-ordered by id position. Prefix path (no word >= 3 chars): collated `LIKE` ordered by name length.
- Index-missing catch is specific to `ArtistSearch` (3.9 verifier follow-up W3): `SqliteException` containing both "no such table" and "ArtistSearch".
- Test defect found in Green (my own Red): the rename test needed `ChangeTracker.Clear()` + `Update` because the test context is NoTracking by default and seeding leaves entities tracked. Test fixed in the Green commit; no spec change; assertions unchanged.
- IArtistRepository implementer census (Python os.walk): only `ArtistRepository`; all other hits are Moq mocks, `GetRequiredService`, or an `AddScoped` factory. No forwarding members needed; no files outside the owned set touched.

### Changed files:
- `Domain/RepositoryInterface/IArtistRepository.cs`
- `Infra/Repository/ArtistRepository.cs`
- `MyVocaList.Tests/Integration/Repositories/ArtistRepositorySearchTests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section)

### Verification evidence
Focused: Aprovado, Falha 0, Aprovado 14, Total 14. Full suite: Falha 0, Aprovado 751, Total 751 (baseline 737, delta +14). Android build: 0 Erro(s).

### AC matrix
| AC | Test |
|---|---|
| REQ-SFT-19 | Metalica, Beatles, Unaccented, ShortName, FtsSyntaxCharacters (x7), RespectsTake, IndexMissing |
| REQ-SFT-34 | AfterRename_ReflectsNewName |

## 3.9c

### Checkpoint

_(not started)_

## 3.10

### Checkpoint

_(not started)_

## 4.1a

**Status:** To Review. Branch task/sft-4.1a-deezer-plain-query. Red a5c9cc7c (Lane: RED), Green in the following commit.
Inherited: clean worktree, nothing half-done.

### Red output (verbatim CS lines)
DeezerProviderTests.cs(129,35), (145,31), (164,37): error CS1061: MusicSearchResultDto has no definition for "Version"
DeezerProviderTests.cs(94,43): error CS0246: type "MusicProviderException" not found

### Changed files:
- `Services/IMusicMetadataProvider.cs` (contract doc: failures throw; artistHint ignored)
- `Services/MusicProviderException.cs` (new)
- `Services/DeezerProvider.cs` (plain query, failures throw, title_short/title_version mapping, version normaliser)
- `Contracts/DTOs/MusicSearchResultDto.cs` (trailing `string? Version = null`)
- `MyVocaList.Tests/Unit/Services/DeezerProviderTests.cs` (new, 18 cases)
- this task-log section

### Verification evidence
Focused: Aprovado! Com falha: 0, Aprovado: 18, Total: 18.
Full suite: Failed 0, Passed 706, Total 706 (baseline 688, +18). First full run showed one FAIL, Bug068RegressionTests.Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict (BUG-076 flaky family); re-run passed 706/706.
Android build (net10.0-android, -m:1): no error lines.

### AC matrix
REQ-SFT-35 SendsPlainQueryWithoutQualifiers; REQ-SFT-47 SendsPlainArtistQuery; REQ-SFT-06 Http500_Throws (HttpRequestException), MalformedJson_Throws (JsonException, unwrapped), Http200WithErrorObject (MusicProviderException), EmptyData_ReturnsEmpty; REQ-SFT-09 CallerCancelled; REQ-SFT-12 MapsIdTitleShortVersionArtistName, TitleShortMissing_FallsBackToTitle (2 cases), NormalisesVersion (8 cases). All in DeezerProviderTests.cs.

### Superseded tests
None.

### Notes
Version normaliser also trims the stripped result and supports (), [], {} pairs; brace support is an implementation choice within REQ-SFT-12 (requested spec note optional). Downstream: 4.2 must catch the new exceptions (callers previously got empty on failure).

ITF: SFT-4.1a — Services/DeezerProvider.cs — root cause: NormaliseVersion also strips `{}` pairs, beyond REQ-SFT-12 / design § 3.4 (only `(` and `[`) — Verifier blocker 1 — expected 1 line(s).

## 4.1b

**Status:** To Review
**Started:** 2026-11-04 **Completed:** 2026-11-04
**Plan:** plan.md § Task 4.1b · Level C · Review lane Standard · Model Haiku

Inherited nothing (clean worktree on `task/sft-4.1b-drop-musicbrainz`, base develop `15038cf2`, ancestor verified).
Level C (deletion + DTO record): no new tests (plan lists none); no-test decision recorded. No AC rows, so no traceability matrix.
Deleted tests: none. Superseded tests: none. `Spec updated 2026-11-04 (D21)` note already present in `tasks.md` (4.1b entry); no spec write-back by this task.
Not pushed; `tasks.md` not ticked (orchestrator).

### Census
- Pre-delete (os.walk, `*.cs`/`*.csproj`/`*.xaml`, excluding bin/obj/.git/.worktrees/Docs), whole-word `\bMusicBrainzProvider\b`: 5 hits, all in owned files (`MyVocaList/Extensions/ServiceCollectionExtensions.cs:34,48`; `Services/MusicBrainzProvider.cs:7,10,14`).
- Substring `MusicBrainz` also matches string-name constants and tests (`MusicBrainzProviderName` in `Services/ArtistSuggestionService.cs:18,152`, `Services/SongSuggestionService.cs:19,98`; `"MusicBrainz"` provider-name literals in `MyVocaList.Tests/Unit/Services/*`). These reference the provider **name string**, not the type, and compile without the type, so no file outside the owned list was touched. Flagged for Helder: the string-name branches in the two suggestion services and their tests are dead-code candidates for 4.2/4.3/6.1 to retire.
- Post-delete whole-word census: **ZERO HITS**.
- `Services/*.cs` count: 29 before, 28 after (floor ≥ 25 met).

### Changed files:
- `Services/MusicBrainzProvider.cs` (deleted; `git rm`, staged)
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (removed `AddHttpClient<MusicBrainzProvider>` block and the `IMusicMetadataProvider` forward to `MusicBrainzProvider`; Deezer lines and BUG-082 comment kept)
- `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs` (new: `public sealed record ArtistRemoteSearchResult(IReadOnlyList<ArtistSuggestionDto> Remote, RemoteSearchStatus Status)`, XML doc with param tags)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` (this section)

### Verification evidence
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → `ok dotnet build: 6 projects, 0 errors, 34 warnings (00:05:19.98)` (attempt 1, passed).
- Focused: `dotnet test ... --filter "FullyQualifiedName~AppServicesRegistrationTests|FullyQualifiedName~ServicesDirectory"` → `Aprovado! – Com falha: 0, Aprovado: 5, Ignorado: 0, Total: 5`.
- Full suite: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1 --no-build` → `Aprovado! – Com falha: 0, Aprovado: 737, Ignorado: 0, Total: 737`. Delta vs baseline 737/737: **0**.
- Post-edit re-read: `ArtistRemoteSearchResult.cs` (lines 1-8) and `ServiceCollectionExtensions.cs` (lines 33-45) confirmed.

### Build notes
Build: passed (0 errors) | Tests: 737 passed, 0 failed (delta 0) | Commit SHA: 36305a69 (refactor commit; pre-commit hook build+tests green)
Files written and re-read: `Contracts/DTOs/Suggestions/ArtistRemoteSearchResult.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`

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


## 3.3
**Status:** To Review. **Branch:** task/sft-3.3-song-resolution. Context7 not used (no new library API; `FindIdByTitleVersionForArtistAsync` already in repo, 3.1/2.x). Inherited: nothing.

Red (`--no-verify`, `Lane: RED`), verbatim: `Assert.Equal() Failure: Values differ / Expected: ExactLocalMatch / Actual: NoMatch` (SongResolutionServiceTests.cs:183; today the unmocked `ExistsByTitleVersionForArtistAsync` returns false so the Kind assert trips before the id assert).

Green: `ResolveAsync` exact-local path now one `FindIdByTitleVersionForArtistAsync(artistId, title, version, null, ct)`; `CommitAsync`, `ApplyUpdate`, `ComputeFieldDiffs`, `ResolveOrCreateArtistIdAsync`, `MergeableFields` deleted; interface keeps only `ResolveAsync`; `SongResolution` constructed with `FieldDiffs: []`, `TargetHasManualEdits: false` (ResolutionResults.cs untouched, 3.3b). Constructor left unchanged (fields now unused; removal is a DI/test-wide change out of scope).

### Superseded tests
| Test (original file:line) | Action | Superseded AC -> replacing REQ |
|---|---|---|
| SongResolutionServiceTests:100 ExternalIdHitWithManualEdits_PopulatesFieldDiffs | deleted | AC-4.2 -> REQ-SFT-38 |
| :271 ExternalMatch_FieldDiffsRestrictedToMergeableSet | deleted | AC-4.2 -> REQ-SFT-38 |
| :312, :327 CommitAsync_CreateNewVersion_* | deleted | CommitAsync removed; AC-1.2 covered by SongFormViewModelTests:265 |
| :343, :389 CommitAsync_UpdateExisting_* | deleted | AC-4.1/4.2 -> REQ-SFT-24 |
| :435 CommitAsync_AttachExternalId_* | deleted | AC-2.2 -> REQ-SFT-05 |
| :481 CommitAsync_CreateNew_ResolvesArtistThenCreatesSong | deleted | AC-2.5 -> REQ-SFT-18 (3.1 tests) |
| :70 ExternalIdHit, :140 ExactLocalHit (HasManualEdits initialisers, FieldDiffs/TargetHasManualEdits asserts) | edited | REQ-SFT-37 |
| :140 ExactLocalHit mock, :47/:208/:238/:172 mocks | edited mechanically: `ExistsByTitleVersionForArtistAsync` -> `FindIdByTitleVersionForArtistAsync` (null / 5) | REQ-SFT-42 |
| NestedUnitOfWorkTests.cs (whole file, 5 tests :22,:66,:105,:146,:195) | file deleted | :22/:146 ported in 3.1 (REQ-UOW-22 re-homed); :66/:105 rewritten in 3.1 (REQ-SFT-18); :195 REQ-UOW-09 removed / AC-3.4 -> REQ-SFT-40 |

### AC matrix
| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-SFT-42 | accent-only title difference -> exact match with stored id | SongResolutionService.ResolveAsync step 2 | ResolveAsync_AccentOnlyTitleDifference_ReturnsExactLocalMatchWithId |

### Changed files:
- `Domain/ServicesInterfaces/ISongResolutionService.cs`
- `Services/SongResolutionService.cs`
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/NestedUnitOfWorkTests.cs` (deleted)
- this task-log section

### Verification evidence
Build (android, -m:1): 0 Erro(s). Full suite: `Failed: 0, Passed: 665, Total: 665` (baseline 677 + 1 new - 8 deleted SongResolution tests - 5 deleted Nested tests = 665). Census `ApplyUpdate` / `CommitAsync(` / `NestedUnitOfWorkTests`: ApplyUpdate zero; CommitAsync( only in IArtistResolutionService.cs, ArtistResolutionService.cs, ArtistResolutionServiceTests.cs, UnitOfWork.cs; `NestedUnitOfWorkTests` has NOT-zero hits, all provenance COMMENTS in files not owned by 3.3 (Bug068RegressionTests.cs:112, SongServiceNewArtistUnitOfWorkTests.cs:12,23,41,60,77,99); no code references. SongFormViewModel.cs `CommitAsync(`: zero.

## 3.4
**Status:** To Review. Branch `task/sft-3.4-artist-resolution`. Level C (deletions), no new test.
Context7 not needed (pure deletion; no API lookups). Inherited: clean worktree, nothing half-done.

Census before: `ResolutionChoice` hit only the 4 owned files (enum, interface, service, ArtistResolutionServiceTests).
Green: `CommitAsync` removed from `IArtistResolutionService` and `ArtistResolutionService`; `ResolutionChoice` enum deleted from `ResolutionEnums.cs` (file kept, `ResolutionKind` remains). Constructor left unchanged (`_artistRepository`, `_artistService` fields now unused; removal is a DI/test-wide change out of scope).

### Superseded tests
| Test (original file:line) | Action | Superseded AC -> replacing REQ |
|---|---|---|
| ArtistResolutionServiceTests:200 CommitAsync_CreateNew_SetsExternalIdentityOnCreatedArtist | deleted | AC-3.4 -> REQ-SFT-40 |
| :225 CommitAsync_AttachExternalId_SetsExternalIdentityIfAbsent | deleted | AC-3.2 -> REQ-SFT-40 |
| :249 CommitAsync_AttachExternalId_DoesNotOverwriteExistingIdentity | deleted | AC-3.2 -> REQ-SFT-40 |

No-new-test decision: Level C deletion; nothing new to assert.

### Changed files:
- `Domain/ServicesInterfaces/IArtistResolutionService.cs`
- `Services/ArtistResolutionService.cs`
- `Domain/Resolution/ResolutionEnums.cs`
- `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs`
- this task-log section

### Verification evidence
Build (android, -m:1): exit 0, 0 errors. Full suite: `Failed: 0, Passed: 663, Total: 663` (baseline 666, delta -3 = deleted CommitAsync tests).
Census `ResolutionChoice`: ZERO HITS. Census `CommitAsync(`: only `Infra/UnitOfWork/UnitOfWork.cs:53,88` (plus unrelated doc text `.claude/skills/efcore-patterns/SKILL.md:376`).

## 4.1c

**Plan:** `plan.md` § Task 4.1c (W5) · **Branch:** `task/sft-4.1c-network-status` (worktree `.worktrees/sft-4.1c`, base `develop` d0a06dd4, ancestor verified) · **Status:** To Review

### Context7 lookup (MAUI, pinned to csproj: Microsoft.Maui.Controls 10.0.41 via Directory.Packages.props)
- Library: `/dotnet/maui` (Context7 versions available 10.0.20, 10.0.51; closest to pin used).
- Confirmed: `IConnectivity { NetworkAccess NetworkAccess {get;} ... }`, `Connectivity.Current` (IConnectivity), `Connectivity.NetworkAccess`, namespace `Microsoft.Maui.Networking`; `NetworkAccess` = Unknown(0), None(1), Local(2).
- Not in the returned snippet: `ConstrainedInternet`, `Internet`. Design § 3.6 cites them from PublicAPI.Shipped.txt; the Green Android build compiled them, so they exist in the pinned MAUI.

### Commits
- Red: `94fb324d` · `Lane: RED` · `--no-verify` (test file + this section only).
- Green: this commit (SHA reported in the handback; the log cannot contain its own SHA).

### Red (test-only commit, `Lane: RED`)
Test: `MauiNetworkStatusTests.IsInternetReachable_MapsEachNetworkAccessValue` (5 InlineData cases), AC REQ-SFT-53.
Output (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1 --filter "FullyQualifiedName~MauiNetworkStatusTests"`, exit 1), compiler line verbatim:
```
C:\Users\helde\source\repos\MyVocaList\.worktrees\sft-4.1c\MyVocaList.Tests\Unit\Services\MauiNetworkStatusTests.cs(21,22): error CS0103: O nome "MauiNetworkStatus" não existe no contexto atual [C:\Users\helde\source\repos\MyVocaList\.worktrees\sft-4.1c\MyVocaList.Tests\MyVocaList.Tests.csproj]
```

### Green
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → `Compilação com êxito.` / `0 Erro(s)` / `Tempo Decorrido 00:07:18.17`.
- Focused: `MauiNetworkStatusTests` → `Aprovado!  – Com falha: 0, Aprovado: 5, Ignorado: 0, Total: 5`.
- Full suite: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` → `Aprovado!  – Com falha: 0, Aprovado: 693, Ignorado: 0, Total: 693` (baseline 688; delta +5, the new theory cases). Exit 0.

### Changed files
- `Domain/ServicesInterfaces/INetworkStatus.cs` (new) — `bool HasInternetAccess { get; }`, no MAUI types.
- `Contracts/DTOs/Suggestions/RemoteSearchStatus.cs` (new) — `enum RemoteSearchStatus { Ok, Offline, Unavailable }`.
- `MyVocaList/UI/Services/MauiNetworkStatus.cs` (new) — `MauiNetworkStatus(IConnectivity)`, `HasInternetAccess`, static pure `IsInternetReachable(NetworkAccess)`.
- `MyVocaList/MauiProgram.cs` — two lines (110-111) beside `INavigationService`: `AddSingleton<IConnectivity>(_ => Connectivity.Current)` and `AddSingleton<INetworkStatus, MauiNetworkStatus>`. Not in `AddAppServices`.
- `MyVocaList.Tests/Unit/Services/MauiNetworkStatusTests.cs` (new) — Level B theory, 5 cases.
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/task-log.md` — `## 4.1c` section only.

### AC traceability matrix (REQ-SFT-53)
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-SFT-53 | None/Local → no internet (Deezer not called); Unknown/ConstrainedInternet/Internet → attempt | `MauiNetworkStatus.IsInternetReachable` | `MauiNetworkStatusTests.IsInternetReachable_MapsEachNetworkAccessValue` (5 cases) |

Consumer wiring (`INetworkStatus` into the suggestion services) is task 4.1b/4.2 scope, not this task.

### No-test decisions (Level C)
- `INetworkStatus` (interface), `RemoteSearchStatus` (enum), `MauiNetworkStatus.HasInternetAccess` (one-line delegation to `IConnectivity`), and the two `MauiProgram.cs` registrations: Level C, no mandatory test. Behaviour is exercised through the pure mapping; DI wiring is verified by the Android build and the full suite.

### Verification evidence
- Context7 lookup: recorded above.
- Post-edit re-read: `INetworkStatus.cs` (16 lines), `RemoteSearchStatus.cs` (17 lines), `MauiNetworkStatus.cs` (19 lines) verified non-empty and correct; `MauiProgram.cs` lines 107-111 re-read: registrations present.
- No SafeAreaEdges / XAML touched. No tasks.md tick. No push.

### Deviations / notes for review
- Design § 3.6 places `MauiNetworkStatus` in namespace `MyVocaList.UI.Services` (beside `INavigationService`); implemented that way. The brief named no namespace, so this is the spec's placement.
- `ConstrainedInternet` / `Internet` not visible in the Context7 snippet; compiled successfully, so the pinned MAUI defines them.
- Inherited state: none (fresh branch from develop; the Red was written in this session).
- Requested spec notes: none required by this task.

## Wave 2026-11-04 (3.9 + 4.1a + 4.1c) — Verifier + merge (orchestrator)
- **4.1c:** Red `94fb324d`, Green `0b18fd0c`. Verifier **PASS** (0 blockers; W1 Green SHA missing from the section — recorded here). Merged `a190b2a0`. Post-merge develop: `dotnet build: 6 projects, 0 errors`; `Aprovado! – Com falha: 0, Aprovado: 693, Total: 693`.
- **3.9:** spec-gap note `26303575` (unowned `ISongRepository` decorator + no logger in `SongRepository`; orchestrator chose Option A, tasks.md `55d906e9`), Red `de57f010`, Green `bb924232`. Verifier (Opus) **CONDITIONAL PASS**, 0 blockers; W1–W5 recorded in tasks.md 3.9. Merged `f3770a83`. Post-merge develop: `0 errors`; `Aprovado! – Com falha: 0, Aprovado: 719, Total: 719`.
- **4.1a:** Red `a5c9cc7c`, Green `17c4aaed`. Verifier **CONDITIONAL PASS**: blocker 1 = `{}` stripping beyond REQ-SFT-12 → fixed by orchestrator ITF `de13e317` (1 file, 1 line; DeezerProvider 18/18; the first hook run had one intermittent failure, full suite 706/706 on re-run). Verifier build line: `Compilação com êxito. 0 Erro(s)`. Merged `bc81f9e4` (task-log conflict = both sides appended; resolved keeping both, ITF line moved into § 4.1a). Post-merge develop: pre-commit hook build + tests green; `Aprovado! – Com falha: 0, Aprovado: 737, Total: 737`.
- **4.1b:** code `36305a69`, task-log `ca789189`. Verifier **PASS** (shape matches plan:1159; Deezer DI + BUG-082 forward intact; W1 dead MusicBrainz name branches → owned by 4.2/4.3/6.1, now explicit in tasks.md). Merged `1aee99f6`. Post-merge develop: `0 errors`; `Aprovado! – Com falha: 0, Aprovado: 737, Total: 737`.
- **3.9b:** Red `61deac4d`, Green `6fcff60b`. Verifier **PASS** (Green edited the self-written Red rename test: `ChangeTracker.Clear()` + `Update` in setup only, assertions unchanged — the NoTracking default meant the rename was never saved; judged acceptable). Merged `02fc76b7`. Post-merge develop: `0 errors`; first full run `Com falha: 1, Aprovado: 750` (intermittent, name not captured), re-run `Aprovado! – Com falha: 0, Aprovado: 751, Total: 751`.
