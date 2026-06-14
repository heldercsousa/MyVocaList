# Song Import & Entity Resolution — Task Log

> One entry per task. See `tasks.md` for sequencing, `plan.md` for step detail.

---
## Task: Wave 4A — BottomSheetTitle, SongPickerViewModel+DI (BUG-010/006), picker back-chrome (BUG-007)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 06/14/2026
**Completed:** 06/14/2026

### Changed files:
- `MyVocaList/Resources/Styles/MaterialStyles.xaml` — updated BottomSheetTitle: added `FontAttributes="None"`, padding corrected to `16,16,16,0` per BUG-004 spec (MD3 titleLarge)
- `MyVocaList/UI/ViewModels/SongPickerViewModel.cs` — created; mirrors ArtistPickerViewModel; injects IMusicMetadataService, IMessenger, INavigationService, ISnackbarComponent, ILogger; SearchCommand (AllowConcurrentExecutions=false), SelectResultCommand (sends SongPickedMessage), LaunchYouTubeSearchCommand, BackCommand; IDisposable CancellationTokenSource
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs` — fixed constructor: was injecting wrong type QueueSongPickerViewModel, now injects correct SongPickerViewModel (BUG-010)
- `MyVocaList/MauiProgram.cs` — added `builder.Services.AddTransient<SongPickerViewModel>()` beside ArtistPickerViewModel (BUG-010)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — NavigateToSongPickerCommand and NavigateToYouTubeSearchCommand now use AsyncRelayCommandOptions.None to prevent concurrent executions (BUG-006)
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` — added Shell.BackButtonBehavior IsVisible=False IsEnabled=False (BUG-007)
- `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` — added Shell.BackButtonBehavior IsVisible=False IsEnabled=False (BUG-007)
- `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs` — enabled (was commented out); 12 tests covering search, empty state, SelectResult message, BackCommand, IsShowEmptyState
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` — checked 4.1, 4.2, 4.3

### Notes
- PersonPickerPage.xaml and QueueSongPickerPage.xaml already had Shell.BackButtonBehavior — only 2 of 4 picker pages needed the BUG-007 fix.
- SongPickedMessage has two definitions: canonical in Contracts.Messages (MusicSearchResultDto Result) and a legacy class in QueueSongPickerViewModel.cs (SongId/QueueEntryId). Used a type alias `SongPickedMsg = MyVocaList.Contracts.Messages.SongPickedMessage` in SongPickerViewModel.cs to avoid ambiguity. The same alias is used in tests. The legacy class and SongFormViewModel's usage of .SongId/.QueueEntryId are pre-existing inconsistencies not in scope for this wave.
- AsyncRelayCommand in CommunityToolkit.Mvvm 8.x uses AsyncRelayCommandOptions enum, not a bool parameter — used AsyncRelayCommandOptions.None to disable concurrent executions.

### Build notes
- Attempt 1 failed: SongPickedMessage ambiguity (CS1729), AsyncRelayCommand named param not found (CS1739)
- Attempt 2 passed: 0 errors, 5 warnings (pre-existing: NU1608 + DX trial warnings)

### Verification evidence
- Build: PASS (0 errors, 5 pre-existing warnings) — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
- Tests: PASS (340 tests, 0 failures) — 328 pre-existing + 12 new SongPickerViewModelTests
- Post-edit re-read: confirmed
- Spec compliance: confirmed — BUG-004, BUG-006, BUG-007, BUG-010 specs checked

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-B4 (BUG-004) | BottomSheetTitle style exists with MD3 titleLarge | MaterialStyles.xaml | Build-time style resolution |
| AC-B6 (BUG-006) | Double-tap does not crash / concurrent navigation prevented | SongFormViewModel AsyncRelayCommandOptions.None | Manual E2E only (Shell nav) |
| AC-B10 (BUG-010) | SongPickerPage injects SongPickerViewModel | SongPickerPage.xaml.cs constructor | SearchCommand_OnSuccess_PopulatesResults |
| AC-2.4 | Successful search populates Results | SongPickerViewModel.SearchAsync | SearchCommand_OnSuccess_PopulatesResults |
| AC-2.5 | Empty result sets HasSearched=true, HasResults=false | SongPickerViewModel.SearchAsync | SearchCommand_OnEmptyResult_SetsHasSearchedNoResults |
| AC-2.7 | SelectResult sends SongPickedMessage | SongPickerViewModel.SelectResultAsync | SelectResultCommand_SendsSongPickedMessage |

---
## Task: Wave 4B — SongForm fixes (BUG-005/008/009) + Resolution/Merge BottomSheets (Tasks 4.4 + 4.5)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 06/14/2026
**Completed:** 06/14/2026

### Changed files:
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` — added `BlurredWithoutSelectionCommand` BindableProperty; `OnSearchEditUnfocused` now invokes it when no suggestion was tapped (BUG-008)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — full rewrite: BUG-005 save-catch; BUG-008 artist blur-clear + `InitializeArtistField()` + `IsArtistLocked`; BUG-009 `_pendingRawUrls` buffer + `RemoveUrlAsync` new-song mode; `SongPickedMessage` canonical alias; `ISongResolutionService` injected; `SaveAsync` now builds `SongCandidate` → `ResolveAsync` → direct create (NoMatch) or resolution sheet; `SelectResolutionCandidateCommand`; `ConfirmUpdateExistingCommand`; `ConfirmSaveAsNewVersionCommand` (AC-1.2 version gate); merge sheet state; `MergeFieldRow` class added; `VersionHasError/VersionErrorText/SongVersion` observable properties; `Version` entry field
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — added `BlurredWithoutSelectionCommand` binding; Version field; Resolution BottomSheet (candidate list, save-as-new-version branch, cancel); Merge BottomSheet (per-field diff rows with CheckEdit toggle, apply/cancel); `resolution:` XAML namespace for `SongMatch`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnAppearing` calls `vm.InitializeArtistField()` after `RefreshApiKeyFlagAsync` (BUG-008)
- `MyVocaList/MauiProgram.cs` — registered `IArtistResolutionService`/`ISongResolutionService` as Scoped
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — rewritten with 17 tests covering BUG-005 (save-catch, no-artist early exit, NoMatch path), AC-1.1/1.2 (resolution sheet shown, empty-version blocked), BUG-008 (blur-clear, restore-prior, InitializeArtistField), BUG-009 (buffer, duplicate, invalid, remove), AC-4.2 (merge row population); plus 3 original URL tests
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/design.md` — §6 updated with SaveAsync orchestration detail (living spec)
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` — 4.4 and 4.5 marked [x]

### Build notes
- 0 errors, 5 pre-existing warnings (NU1608 + DX trial) — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`

### Verification evidence
- Build: PASS (0 errors) — net10.0-android
- Tests: PASS (354 tests, 0 failures) — 354 total (17 new SongFormViewModelTests + 337 pre-existing)
- Post-edit re-read: confirmed for all 7 changed files
- Spec compliance: confirmed — BUG-005/008/009 specs, AC-1.1/1.2, AC-4.2/4.3, AC-6.1/6.2, AC-B5/B8 checked

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-1.1 | ExactLocalMatch → resolution sheet shown | SongFormViewModel.ExecuteNewSongSaveAsync | SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible |
| AC-1.2 | Save as new version with empty Version → blocked | SongFormViewModel.ConfirmSaveAsNewVersionAsync | ConfirmSaveAsNewVersion_EmptyVersion_SetsVersionError |
| AC-4.1 | NoMatch → CreateSongWithUrlsAsync called | SongFormViewModel.ExecuteNewSongSaveAsync | SaveAsync_NoMatch_CallsCreateSongWithUrls |
| AC-4.2 | HasManualEdits + FieldDiffs → merge sheet shown | SongFormViewModel.ConfirmUpdateExistingAsync | ConfirmUpdateExisting_TargetHasManualEdits_PopulatesMergeRows |
| AC-4.3 | Cancel merge → no write | SongFormViewModel.DismissMergeSheet | Structural (no CommitAsync call) |
| AC-4.4 | HasManualEdits set on manual edit | SongFormViewModel.ExecuteEditSaveAsync (hasManualEdits=true) | N/A — passed to UpdateSongAsync |
| AC-6.1 | URL buffered in new-song mode, no error | SongFormViewModel.AddFromPasteAsync + BufferUrlAsync | AddFromPasteAsync_NewSongMode_BuffersUrlNoError |
| AC-6.2 | Atomic save via CreateSongWithUrlsAsync | SongFormViewModel.CommitNewSongAsync | SaveAsync_NoMatch_CallsCreateSongWithUrls |
| AC-B5 | Exception in SaveAsync → error snackbar shown | SongFormViewModel.SaveAsync catch block | SaveAsync_ServiceThrows_ShowsErrorSnackbar |
| AC-B8 | Blur without selection → field cleared | AutocompleteField + SongFormViewModel.OnArtistBlurredWithoutSelection | ArtistBlurredWithoutSelection_NoPriorSelection_ClearsField |

---
## Task: Spec + plan authored
**Plan:** `song-import-resolution/plan.md`
**Status:** Review task done
**Started:** 06/13/2026
**Completed:** 06/13/2026

### Changed files:
- `requirements.md` — feature requirements (6 user stories, ACs, invariants)
- `design.md` — architecture, contracts, resolution algorithm, wave plan
- `plan.md` — bite-sized TDD implementation plan (Waves 0–5)
- `tasks.md` — structured task checklist
- `Docs/Management/BACKLOG.md` — new nested feature row; fuzzy-matching item subsumed
- `MyVocaList.sln` — registered solution folder (GUID ...0023)

### Verification evidence
- Spec-reviewer subagent: PASS with minor issues; M1/M2/M3 + N1–N6 applied.
- Build: N/A (docs only). Tests: N/A.

### Notes
Decisions locked with Helder 2026-06-13: (1) version variants first-class + confirm sheet; (2) exact-collation + bounded fuzzy matching; (3) never silently overwrite manual edits (field merge); (4) fold in blocking bugs 004/005/006/007/008/009/010.

---
## Task: Wave 1 — Domain contracts (Tasks 1.1, 1.2, 1.3)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 06/13/2026
**Completed:** 06/13/2026

### Changed files:
- `Domain/Entity/Song.cs` — added `Version` property (string, default `""`, with XML doc)
- `Domain/Resolution/ResolutionEnums.cs` — new file: `ResolutionKind` and `ResolutionChoice` enums
- `Domain/Resolution/Candidates.cs` — new file: `ArtistCandidate` and `SongCandidate` sealed records
- `Domain/Resolution/ResolutionResults.cs` — new file: `FieldDiff`, `SongMatch`, `SongResolution`, `ArtistResolution` sealed records
- `Domain/ServicesInterfaces/ISimilarityScorer.cs` — new file: `ISimilarityScorer` interface
- `Domain/ServicesInterfaces/IArtistResolutionService.cs` — new file: `IArtistResolutionService` interface
- `Domain/ServicesInterfaces/ISongResolutionService.cs` — new file: `ISongResolutionService` interface
- `Domain/ServicesInterfaces/ISongService.cs` — added `CreateSongWithUrlsAsync`; added `externalId`/`externalProvider` optional params to `UpdateSongAsync`
- `Domain/RepositoryInterface/ISongRepository.cs` — added `ExistsByTitleVersionForArtistAsync` (×2 overloads) and `GetFuzzyCandidatePoolAsync`
- `Domain/RepositoryInterface/IArtistRepository.cs` — added `GetFuzzyCandidatePoolAsync`
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` — Tasks 1.1, 1.2, 1.3 marked [x]

### Verification evidence
- Build: PASS — `dotnet build Domain/MyVocaList.Domain.csproj` → 0 errors, 1 pre-existing warning in Contracts (CS8612, unrelated).
- Tests: N/A — Wave 1 is pure contract definitions (Level C); no new business logic.
- Post-edit re-read: confirmed — all files match design.md §3 signatures verbatim.
- Spec compliance: confirmed — `design.md §3` interface signatures, record shapes, and enum values all match.

### Notes
- New `.cs` files live under existing project folders covered by the SDK glob; no `.csproj` item changes needed.
- `.sln` registration not required — new files are in `Domain/` (C# project), not `Docs/` or `.claude/`.
- `ISongService.UpdateSongAsync` and `ISongRepository` additions will break `SongService.cs` (implementation doesn't yet implement new members) — expected for Wave 1; will be resolved in Wave 3.

---
<!-- Implementation task entries appended below as waves execute. -->

---
## Task: Wave 2 — Infra (Tasks 2.1–2.5)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 06/13/2026
**Completed:** 06/13/2026

### Changed files:
- `Infra/EntityEFConfig/SongConfiguration.cs` — Version col collation + 3-col unique index `IX_Songs_ArtistId_Title_Version`
- `Infra/Migrations/20260613082518_AddSongVersion.cs` — drop old 2-col index; add Version TEXT NOT NULL DEFAULT ''; create 3-col unique index
- `Infra/Repository/SongRepository.cs` — `ExistsByTitleVersionForArtistAsync` ×2 + `GetFuzzyCandidatePoolAsync`
- `Infra/Repository/ArtistRepository.cs` — `GetFuzzyCandidatePoolAsync`
- `Infra/Similarity/SimilarityScorer.cs` — NFD + diacritic strip + TokenSetRatio impl
- `Infra/Similarity/SimilarityConstants.cs` — DefaultThreshold=0.82, PoolSize=50, PrefixTokenMaxLen=12
- `MyVocaList.Tests/Integration/Repositories/SongRepositoryResolutionTests.cs` — DuplicateTitleVersion, AccentInsensitive, FuzzyPool tests
- `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryResolutionTests.cs` — ArtistFuzzyPool tests

### Verification evidence
- Build: PASS (Infra 0 errors)
- Tests: Deferred to Wave 3 (Services project blocked compile; tests execute after 3A)
- Post-edit re-read: confirmed

---
## Task: Wave 3A — SongService atomic URL save + external-id; scorer unit tests; Wave 2 tests green (Task 3.3)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 06/14/2026
**Completed:** 06/14/2026

### Changed files:
- `Services/SongService.cs` — added `ISongKaraokeUrlRepository` + `ISongKaraokeUrlService` constructor params; `UpdateSongAsync` signature extended with `externalId`/`externalProvider` (M2); `CreateSongWithUrlsAsync` implemented (N3 atomic: song staged via `_songRepository.AddAsync`, URL entities staged via `_urlRepository.AddAsync` with `Song` nav set, single `_songRepository.SaveChangesAsync` commits both)
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` — updated `CreateSut()` to pass 5 constructor args; added 5 new tests: `UpdateSongAsync_WithExternalIdentity_PersistsProviderAndId`, `UpdateSongAsync_WithNullExternalIdentity_DoesNotOverwriteExistingIdentity`, `CreateSongWithUrlsAsync_ValidSongAndUrls_PersistsBoth`, `CreateSongWithUrlsAsync_DuplicateTitleVersion_ReturnsFalseAndPersistsNothing`, `CreateSongWithUrlsAsync_EmptyUrlList_CreatesSongOnly`
- `MyVocaList.Tests/Unit/Services/Similarity/SimilarityScorerTests.cs` — NEW: 11 tests covering identical→1.0, Björk/Biork≥0.80, NãoSei/NaoSei≥0.95, Queen/Madonna<0.30, empty/null→0.0, determinism
- `MyVocaList.Tests/Integration/Repositories/QueueRepositoryTests.cs` — fixed pre-existing bug: `OriginalArtistId` → `ArtistId` (3 occurrences; `Song.OriginalArtistId` never existed — it's `ArtistId`)
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` — Task 3.3 marked [x]

### Build notes
Services project: 0 errors, 0 new warnings (pre-existing warnings only).

### Verification evidence
- Build: PASS — `dotnet build Services\MyVocaList.Services.csproj` → 0 errors
- Tests: PASS — `dotnet test MyVocaList.Tests\MyVocaList.Tests.csproj` → **304 passed, 0 failed** (includes Wave 2 integration tests: SongRepositoryResolutionTests + ArtistRepositoryResolutionTests)
- Post-edit re-read: confirmed — `SongService.cs` matches design N3 (single SaveChangesAsync, Song nav property used for FK resolution)
- Spec compliance: confirmed — N3 (one ctx), M2 (null-guard preserves existing identity), AC-6.1/6.2 (atomic persist/rollback tested)

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-2.4 | UpdateSongAsync persists external identity when provided | `SongService.UpdateSongAsync` | `UpdateSongAsync_WithExternalIdentity_PersistsProviderAndId` |
| AC-6.1 | CreateSongWithUrlsAsync creates song and URLs atomically | `SongService.CreateSongWithUrlsAsync` | `CreateSongWithUrlsAsync_ValidSongAndUrls_PersistsBoth`, `CreateSongWithUrlsAsync_EmptyUrlList_CreatesSongOnly` |
| AC-6.2 | Failure rolls back both song and URLs | `SongService.CreateSongWithUrlsAsync` | `CreateSongWithUrlsAsync_DuplicateTitleVersion_ReturnsFalseAndPersistsNothing` |
| AC-5.5 | Duplicate (ArtistId, Title, Version) rejected | `SongRepository.ExistsByTitleVersionForArtistAsync` + DB unique index | `SongRepositoryResolutionTests.DuplicateTitleVersion_ThrowsDbUpdateException` (Wave 2, now executed) |

---
## Task: Wave 3B — Artist/Song resolution engine (Tasks 3.1, 3.2)
**Plan:** `song-import-resolution/plan.md`
**Status:** To Review
**Started:** 2026-06-14
**Completed:** 2026-06-14

### Changed files:
- `Domain/Resolution/SimilarityConstants.cs` — NEW: moved from Infra to Domain so Services layer can reference without depending on Infra. Values unchanged (DefaultThreshold=0.82, PoolSize=50, PrefixTokenMaxLen=12).
- `Domain/RepositoryInterface/IArtistRepository.cs` — added `GetByNameAsync(string name, CancellationToken ct)` for exact-name collation match used in ArtistResolutionService step 2.
- `Infra/Similarity/SimilarityConstants.cs` — replaced class body with `global using SimilarityConstants = MyVocaList.Domain.Resolution.SimilarityConstants;` (re-export; Infra callers still compile via alias).
- `Infra/Repository/ArtistRepository.cs` — implemented `GetByNameAsync` (AsNoTracking, EF.Functions.Collate exact match).
- `Services/ArtistResolutionService.cs` — NEW: full `IArtistResolutionService` implementation (external-id hit → exact-name hit → fuzzy pool → NoMatch; CommitAsync: CreateNew sets external identity; UpdateExisting/AttachExternalId sets identity if absent).
- `Services/SongResolutionService.cs` — NEW: full `ISongResolutionService` implementation (artist-first INV-1; external-id → exact-local → fuzzy → NoMatch; CommitAsync: CreateNewVersion rejects empty Version; UpdateExisting with HasManualEdits applies only acceptedFields; FieldDiffs restricted to {Title, FeaturedArtists, Lyrics, Version}).
- `MyVocaList.Tests/Unit/Services/ArtistResolutionServiceTests.cs` — NEW: 10 tests.
- `MyVocaList.Tests/Unit/Services/SongResolutionServiceTests.cs` — NEW: 14 tests.
- `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` — Tasks 3.1, 3.2 marked [x].

### Build notes
Services project: 0 errors. Full test suite: 328 passed, 0 failed (304 pre-existing + 24 new).

### Verification evidence
- Build: PASS — `dotnet build Services/MyVocaList.Services.csproj` → 0 errors
- Tests: PASS — `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → **328 passed, 0 failed** (↑24 from Wave 3A baseline of 304)
- Post-edit re-read: confirmed — ArtistResolutionService and SongResolutionService match design.md §4 algorithm verbatim.
- Spec compliance: confirmed — INV-1 enforced (artist resolved before song exact/fuzzy); FieldDiffs restricted to N4 mergeable set (ArtistId excluded); AC-1.2 CreateNewVersion empty-version rejection; AC-4.1/4.2 manual-edit guarded update.

### Design §4 clarification (living spec)
Added one detail to implementation not explicit in the spec: when artist resolves to Fuzzy/NoMatch in `SongResolutionService.ResolveAsync`, the exact-local and fuzzy song checks are skipped (no committed artistId to scope them), but the external-id song check still runs (it does not depend on artistId). The song resolution returns `NoMatch` in this case. This is documented in the code comment on the artist-first block and is consistent with INV-1 and design §4 step 1.

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-1.1 | Exact match detected and surfaced | `SongResolutionService.ResolveAsync` ExactLocalMatch branch | `ResolveAsync_ExactLocalHit_ReturnsExactLocalMatch` |
| AC-1.2 | CreateNewVersion requires non-empty Version | `SongResolutionService.CommitAsync` | `CommitAsync_CreateNewVersion_EmptyVersion_ReturnsFalse`, `CommitAsync_CreateNewVersion_WhitespaceVersion_ReturnsFalse` |
| AC-2.1 | External-id match → ExactExternalMatch | `SongResolutionService.ResolveAsync` | `ResolveAsync_ExternalIdHit_ReturnsExactExternalMatch` |
| AC-2.2 | No external match + exact local → ExactLocalMatch + AttachExternalId path | `SongResolutionService.ResolveAsync`, `CommitAsync` AttachExternalId | `ResolveAsync_ExactLocalHit_ReturnsExactLocalMatch`, `CommitAsync_AttachExternalId_SetsExternalIdentityOnly` |
| AC-2.3 | Fuzzy candidates surfaced / NoMatch | `SongResolutionService.ResolveAsync` | `ResolveAsync_FuzzyAboveThreshold_ReturnsFuzzyCandidates`, `ResolveAsync_NoMatchAtAll_ReturnsNoMatch` |
| AC-2.5 | Artist resolved first (INV-1) | `SongResolutionService.ResolveAsync` | `ResolveAsync_ArtistUnresolved_ReturnsNoMatchWithoutSongLookup` |
| AC-3.1 | Artist external-id match | `ArtistResolutionService.ResolveAsync` | `ResolveAsync_ExternalIdHit_ReturnsExactExternalMatch` |
| AC-3.2 | Artist exact-name match | `ArtistResolutionService.ResolveAsync` | `ResolveAsync_ExactNameHit_ReturnsExactLocalMatch` |
| AC-3.3 | Artist fuzzy candidates | `ArtistResolutionService.ResolveAsync` | `ResolveAsync_FuzzyAboveThreshold_ReturnsFuzzyCandidates`, `ResolveAsync_FuzzyBelowThreshold_ReturnsNoMatch` |
| AC-3.4 | Artist NoMatch → CreateNew sets external identity | `ArtistResolutionService.CommitAsync` | `CommitAsync_CreateNew_SetsExternalIdentityOnCreatedArtist` |
| AC-4.1 | No manual edits → overwrite non-empty API fields | `SongResolutionService.CommitAsync` UpdateExisting | `CommitAsync_UpdateExisting_NoManualEdits_OverwritesNonEmptyFields` |
| AC-4.2 | Manual edits → apply only acceptedFields | `SongResolutionService.CommitAsync` UpdateExisting | `CommitAsync_UpdateExisting_ManualEdits_AppliesOnlyAcceptedFields` |
