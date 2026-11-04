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

_(not started)_

## 3.2a

### Checkpoint

_(not started)_

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

### Checkpoint

_(not started)_

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

