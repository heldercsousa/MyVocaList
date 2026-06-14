# Song Import & Entity Resolution — Tasks

> Source of truth for sequencing. Markers: `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`.
> Full step detail + code in `plan.md`. Read `requirements.md` + `design.md` first. DRY Onion order; worktrees mandatory for any 2+ parallel wave.

## Wave 0 — Spike
- [x] **0.1 [SPIKE] Validate fuzzy library on net10.0-android** — DONE 2026-06-13: FuzzySharp 2.0.2, Android-safe, threshold 0.82. See `findings.md`.

## Wave 1 — Domain `[P]`
- [x] **1.1 Add Song.Version** — `Version` property added to `Song.cs` with XML doc; Domain builds clean.
- [x] **1.2 Resolution contracts** — `Domain/Resolution/` folder created: `ResolutionEnums.cs` (ResolutionKind, ResolutionChoice), `Candidates.cs` (ArtistCandidate, SongCandidate), `ResolutionResults.cs` (FieldDiff, SongMatch, SongResolution, ArtistResolution); Domain builds clean.
- [x] **1.3 Service + scorer interfaces** — `ISimilarityScorer.cs`, `IArtistResolutionService.cs`, `ISongResolutionService.cs` created; `ISongService.cs` extended (CreateSongWithUrlsAsync + UpdateSongAsync externalId params); `ISongRepository.cs` extended (ExistsByTitleVersionForArtistAsync ×2 + GetFuzzyCandidatePoolAsync); `IArtistRepository.cs` extended (GetFuzzyCandidatePoolAsync); Domain builds 0 errors.

## Wave 2 — Infra `[SEQUENTIAL]` (waits W1) — DONE on feature/song-import-resolution
> Implemented in a parallel session, adopted via stash 9208f48 (extracted file-by-file onto this branch). Infra builds 0 errors. Repo/scorer TESTS run after Wave 3 (Services must compile first — ISongService gained members in Wave 1).
- [x] **2.1 SongConfiguration: Version + 3-col unique index** — Version col `.IsRequired().HasMaxLength(60).UseCollation(Default)`; `IX_Songs_ArtistId_Title_Version` unique.
- [x] **2.2 Migration AddSongVersion** — `Infra/Migrations/20260613082518_AddSongVersion.cs`: drops old 2-col index, adds Version (default ""), creates 3-col unique index.
- [x] **2.3 SongRepository query methods** — `ExistsByTitleVersionForArtistAsync` ×2 (EF.Functions.Collate Title+Version), `GetFuzzyCandidatePoolAsync`. Integration tests authored (run post-W3).
- [x] **2.4 ArtistRepository fuzzy pool** — `GetFuzzyCandidatePoolAsync` (collation LIKE, Take, AsNoTracking). Test authored.
- [x] **2.5 SimilarityScorer + constants** — `Infra/Similarity/SimilarityScorer.cs` (NFD FormD + NonSpacingMark strip + ToLowerInvariant → FuzzySharp TokenSetRatio/100), `SimilarityConstants`. FuzzySharp 2.0.2 wired. ⚠️ scorer UNIT TEST still TODO — add in Wave 3 test pass.

## Wave 3 — Services `[SEQUENTIAL]` (waits W2; TDD Level A)
- [x] **3.1 ArtistResolutionService (TDD)** — Produces: `Services/ArtistResolutionService.cs` + 10 tests. Consumes: 2.4, 2.5. DONE 2026-06-14.
- [x] **3.2 SongResolutionService (TDD)** — Produces: `Services/SongResolutionService.cs` + 14 tests. Consumes: 3.1, 2.3, 2.5. DONE 2026-06-14.
- [x] **3.3 SongService.CreateSongWithUrlsAsync + update external-id (TDD)** — `SongService.cs` updated: `UpdateSongAsync` gains externalId/externalProvider params (M2); `CreateSongWithUrlsAsync` implemented with atomic single-SaveChangesAsync over shared context (N3, AC-6.1/6.2). SimilarityScorer unit tests added (`Similarity/SimilarityScorerTests.cs`). Pre-existing `QueueRepositoryTests` OriginalArtistId→ArtistId bug fixed. Wave 2 integration tests now execute: 304/304 passed.

## Wave 4 — UI `[SEQUENTIAL]` (waits W3). Invoke `myvocalist-coding`. One XAML file at a time.
- [x] **4.1 BottomSheetTitle style (BUG-004)** — Files: `MaterialStyles.xaml`.
- [x] **4.2 SongPickerViewModel + page/DI fix (BUG-010, BUG-006)** — Files: `SongPickerViewModel.cs`, `SongPickerPage.xaml.cs`, `MauiProgram.cs` (hotspot) + tests.
- [x] **4.3 Picker pages suppress Shell back chrome (BUG-007)** — Files: 4 picker `*.xaml` (2 already done; added to SongPickerPage.xaml + ArtistPickerPage.xaml).
- [x] **4.4 SongFormViewModel: save catch + buffered URLs + artist autocomplete (BUG-005/008/009)** — Files: `SongFormViewModel.cs`, `SongFormPage.xaml(.cs)`, `AutocompleteField.xaml.cs`, `SongFormViewModelTests.cs` (17 tests). DONE 2026-06-14.
- [x] **4.5 Resolution + merge BottomSheets wiring** — Files: `SongFormPage.xaml(.cs)`, `SongFormViewModel.cs`, `MauiProgram.cs`. DONE 2026-06-14.

## Wave 5 — Tests + gate `[SEQUENTIAL]`
- [ ] **5.1 Integration + AC traceability matrix** — full `dotnet test` green; matrix in `task-log.md`.
- [ ] **5.2 Emulator smoke (Helder gate)** — record in `task-log.md`; BACKLOG → ✅ Done on green.

## Hotspot / single-writer files (serialize)
`MauiProgram.cs` · `AppDbContext.cs` + snapshot · `*_AddSongVersion.cs` · `tasks.md` · picker `*.xaml` (edit one at a time).
