# Task log - song writes propagate to the artist catalog

---
## Task: C1 - Repository queries
**Status:** Done, task review clean (2026-11-20)
**Completed:** 2026-11-20

### Changed files:
- `Domain/RepositoryInterface/ICatalogRepository.cs`
- `Infra/Repository/CatalogRepository.cs`
- `Domain/RepositoryInterface/ISongRepository.cs`
- `Infra/Repository/SongRepository.cs`
- `Tests/Integration/Repositories/CatalogRepositoryTests.cs`
- `Tests/Integration/Repositories/SongRepositoryTests.cs`
- `Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (decorator forwards the new interface method)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`
- `MyVocaList.sln`

### Test evidence
RED: tests written first; build failed with CS1061 (IsDerivedAsync, RemoveCoversAsync, CountByArtistAsync missing).
GREEN: two classes 43 passed; full `dotnet test Tests/MyVocaList.Tests.csproj` 1858 passed, 0 failed.
`DeleteAsync_Song_RemovesDerivedAndCoverCatalogRows` (REQ-CATPROP-05) pins an existing DB cascade and passed without production code.

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-05 | existing FK cascade | SongRepositoryTests.DeleteAsync_Song_RemovesDerivedAndCoverCatalogRows |
| REQ-CATPROP-06 | CatalogRepository.IsDerivedAsync | IsDerivedAsync_AuthorRow/CoverRow/MissingRow |
| REQ-CATPROP-07 | CatalogRepository.RemoveCoversAsync | RemoveCoversAsync_MixedList/UnknownIds/DuplicateIds/OtherArtistsCover |
| REQ-CATPROP-10 | SongRepository.CountByArtistAsync | CountByArtistAsync_ReturnsOwnedSongsOnly |

---
## Task: C2 - Backfill migration (REQ-CATPROP-09, 11)
**Status:** Done, task review clean (2026-11-20)

### Changed files:
- `Infra/Migrations/20261120052950_BackfillDerivedCatalogRows.cs` (+ `.Designer.cs`; snapshot unchanged)
- `Tests/Integration/Migrations/CatalogBackfillMigrationTests.cs`
- `Tests/Integration/Repositories/ArtistRepositoryTests.cs`
- `Tests/Integration/Migrations/RemoveSongVersionMigrationTests.cs` (one test pinned to its own migration)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`

### Test evidence
RED: `BackfillSql` const added with an empty `Up`; 3 of 5 new tests failed (SongWithoutDerivedRow, CoverRowsUntouched, SecondApply: Actual empty). ManualAuthorRow and PerformersOnly are guards that pass by design (INSERT OR IGNORE; repository unchanged).
GREEN: 5 passed; full `dotnet test Tests/MyVocaList.Tests.csproj` 1863 passed, 0 failed.
`RemoveSongVersionMigrationTests.Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty` migrated to latest and asserted Catalog unchanged; it now correctly sees backfilled rows, so it is pinned to `NewMigrationId(db)` (intent REQ-RMV-03 unchanged). Recorded in design.md section 9 (commit 90aed3be).

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-09 | BackfillDerivedCatalogRows.BackfillSql | CatalogBackfillMigrationTests (4 tests) |
| REQ-CATPROP-11 | existing PerformersOnly filter | ArtistRepositoryTests.PerformersOnly_AuthorWithOnlyDerivedRows_IsReturned |

---
## Task: C3a - Test setup: register ICatalogRepository in SongService test scopes (no AC; setup only)
**Status:** Done, task review clean (2026-11-20)

### Changed files:
- `Tests/Integration/Services/SongSaveTrigramDuplicateTests.cs` (1 call; new `_catalogRepo` field)
- `Tests/Integration/UnitOfWork/SongServiceReadScopeTests.cs` (2 calls; inline `new Mock<ICatalogRepository>().Object`)
- `Tests/Unit/Services/SongServiceArtistFreezeTests.cs` (1 call; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceEnsureLyricsTests.cs` (2 calls; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceLyricsOriginTests.cs` (1 call; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceSaveFromFormImageTests.cs` (1 call; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceSaveFromFormPictureTests.cs` (1 call; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceSaveFromFormTests.cs` (1 call; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceTests.cs` (2 calls; new `_catalogRepoMock` field)
- `Tests/Unit/Services/SongServiceTitleFreezeTests.cs` (1 call; new `_catalogRepoMock` field)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`

Not changed (their `Over` does not feed a `SongService`): `SongResolutionServiceTests.cs`, the line-100 `Over` in `SongSaveTrigramDuplicateTests.cs` (feeds `SongResolutionService`), and the `Over(_artistRepoMock, _artistServiceMock)` at `SongServiceSaveFromFormTests.cs` (feeds `ArtistResolutionService`).

### Test evidence
Full `dotnet test Tests/MyVocaList.Tests.csproj`: 1863 passed, 0 failed (no assertion or other setup changed; `UnitOfWorkMocks.cs` untouched).

---
## Task: C3b - SongService propagation (REQ-CATPROP-01 to 04)
**Status:** Done, task review clean (2026-11-20)

### Changed files:
- `Services/SongService.cs` (CreateSongAsync, CreateSongWithUrlsCoreAsync, UpdateSongCoreAsync)
- `Tests/Integration/Services/SongServiceCatalogPropagationTests.cs` (new, 12 tests, real SQLite + real unit of work)
- `Tests/Integration/UnitOfWork/VenueCatalogServiceReadScopeTests.cs` (setup only: dropped the now-duplicate AddSongToCatalogAsync; assertions unchanged)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/design.md` (section 9 note for the line above)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`

### Test evidence
RED: 12 new tests, 8 failed (4 create tests, MovesRow, NewInlineArtist move, OldDerivedRowMissing, SaveThrows). 4 pass by design on the old code (SameAuthor, BAlreadyCovers, ValidationFails, ArtistChoiceNeeded are guards).
GREEN: 12/12; full `dotnet test Tests/MyVocaList.Tests.csproj` 1875 passed, 0 failed (includes UnitOfWorkReadScopeTests).

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-01 | SongService create paths stage `new Catalog { Song = song, ArtistId }` | CreateSongAsync_WritesDerivedRow, SaveFromForm_AddMode_WritesDerivedRow, SaveFromForm_AddMode_NewInlineArtist_WritesDerivedRowWithRealId, CreateSongWithUrls_WritesDerivedRow |
| REQ-CATPROP-02 | move only when author changed | Update_SameAuthor_CatalogUnchanged |
| REQ-CATPROP-03 | UpdateSongCoreAsync Remove(old) + Exists/Add(new) | Update_AuthorAtoB_MovesRow, _BAlreadyCovers_NoDuplicate, Update_AuthorToNewInlineArtist_MovesRowWithRealId, Update_AuthorChange_OldDerivedRowMissing_StillWritesNew |
| REQ-CATPROP-04 | all staged in the UoW transaction | Create_ValidationFails_NoCatalogRow, Update_ArtistChoiceNeeded_CatalogUnchanged, Update_AuthorAtoB_SaveThrows_CatalogUnchanged |

---
## Task: C4 - CatalogService guard and bulk remove (REQ-CATPROP-06, 07)
**Status:** Done, task review clean (2026-11-20)

### Changed files:
- `Domain/ServicesInterfaces/ICatalogService.cs` (new `RemoveSongsFromCatalogAsync`)
- `Services/CatalogService.cs` (single remove: exists, derived, remove; bulk remove with message table)
- `Tests/Unit/Services/CatalogServiceTests.cs` (replaced "AlwaysReturnsSuccess"; 3 single + 2 + 7-case theory bulk tests)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`

### Test evidence
RED: with the interface member added and no implementation, the test project did not compile (CS0535 on CatalogService); the old always-success body also could not satisfy the new single-remove tests.
GREEN: CatalogServiceTests 15/15; full `dotnet test Tests/MyVocaList.Tests.csproj` 1886 passed, 0 failed.

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-06 | CatalogService.RemoveSongFromCatalogAsync | RemoveSongFromCatalogAsync_DerivedRow_RefusesAndKeepsRow, _CoverRow_RemovesAndSucceeds, _MissingRow_ReturnsNotInCatalog |
| REQ-CATPROP-07 | CatalogService.RemoveSongsFromCatalogAsync + BuildBulkRemoveMessage | RemoveSongsFromCatalogAsync_EmptyList_..., _UnknownArtist_..., _Counts_ProduceExactMessage (7 cases) |

---
## Task: C5 - ArtistService delete check (REQ-CATPROP-10, service part)
**Status:** Done, task review clean (2026-11-20)

### Changed files:
- `Services/ArtistService.cs` (delete check via `ISongRepository.CountByArtistAsync`, AC-6.2 text; `ICatalogRepository` field/parameter removed)
- `Tests/Unit/Services/ArtistServiceTests.cs` (two refusal tests rewritten, catalog-only test added)
- `Tests/Unit/Services/ArtistServicePictureTests.cs`, `Tests/Integration/UnitOfWork/PagedListConcurrencyTests.cs`, `Tests/Integration/UnitOfWork/ArtistServiceReadScopeTests.cs` (constructor calls only)
- `Tests/Integration/Services/ArtistServiceDeleteIntegrationTests.cs` (new)

### Test evidence
RED: 4 failed (Delete_OwnerOfSongs_Refused, Delete_TwoOwners_NamesFirstInSelectionOrder, Delete_ArtistWithOnlyCatalogRows_Succeeds, Delete_CoverOnlyArtist_RowsGoneSongsKept).
GREEN: full `dotnet test Tests/MyVocaList.Tests.csproj` 1888 passed, 0 failed (one unrelated timing flake, LoadingHoldShimmerTests, passed on rerun). UnitOfWorkReadScopeTests 4/4.
Governed constructor parameters after removal: 22 (23 before), counted by the C5 reviewer with the architecture test's own rule (`^I\w*Repository$` plus the five data services, per file, under `Services/`). The floor is >= 15, and the test passes 4/4.

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-10 | ArtistService.DeleteArtistsAsync | Delete_OwnerOfSongs_Refused, Delete_TwoOwners_NamesFirstInSelectionOrder, Delete_ArtistWithOnlyCatalogRows_Succeeds, Delete_CoverOnlyArtist_RowsGoneSongsKept |

---
## Task C6: ViewModels (REQ-CATPROP-08, REQ-CATPROP-10 text)
**Status:** Done, task review clean (2026-11-20)
**Completed:** 2026-11-20

### Changed files:
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` (catalog-mode `BuildDeleteConfirmMessage` and `ExecuteDeleteAsync` via `RemoveSongsFromCatalogAsync`; dead `RemoveFromCatalogCommand` / `RemoveFromCatalogAsync` removed, no XAML or test referenced them)
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` (`BuildDeleteConfirmMessage` text only)
- `Tests/Unit/ViewModels/SongsViewModelCatalogDeleteTests.cs` (new)
- `Tests/Unit/ViewModels/ArtistsViewModelDeleteConfirmTests.cs` (new)

### Test evidence
RED: 6 of 7 new tests failed (texts unchanged, bulk remove never called); the songs-mode regression test passed as expected.
GREEN: new classes 7/7; full `dotnet test Tests/MyVocaList.Tests.csproj` 1895 passed, 0 failed. Android build (controller, net10.0-android Debug): 0 errors.

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-08 | SongsViewModel.BuildDeleteConfirmMessage / ExecuteDeleteAsync | CatalogMode_OneSong_ConfirmTextNamesSongAndCatalog, CatalogMode_ThreeSongs_ConfirmTextCountsAll, CatalogMode_Confirm_CallsBulkRemoveNeverDeleteSongs_AndShowsSuccess, CatalogMode_RemoveFails_ShowsErrorStillReloads_NeverDeletesSongs, SongsMode_Confirm_StillDeletesSongs |
| REQ-CATPROP-10 (text) | ArtistsViewModel.BuildDeleteConfirmMessage | OneArtist_ConfirmTextMentionsCatalogEntries, TwoArtists_ConfirmTextMentionsCatalogEntries |

---
## Task C7: Merge, verification, traceability
**Status:** Done (2026-11-20)
**Completed:** 2026-11-20

### Changed files:
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/tasks.md`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/README.md` (status, via backlog_gen)

### Test evidence
- `feature/catalog-prop` merged into `develop` (--no-ff).
- On `develop`: `dotnet test Tests` 1911 passed, 0 failed. Android build (net10.0-android Debug): 0 errors.
- Developer query database: `dotnet ef database update -p Infra -s Infra --connection "Data Source=<absolute path>/.claude/MyVocaList.db"` applied `RemoveSongVersionAndFeaturedArtists` and `BackfillDerivedCatalogRows`. The app project cannot be the EF startup project (no Android deps.json), and a relative path resolves against `Infra/`.

### Traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| REQ-CATPROP-01 | Creating a song stores its derived catalog row | SongService create paths (CreateSongAsync, SaveFromFormAsync add mode, CreateSongWithUrlsAsync) | CreateSongAsync_WritesDerivedRow, SaveFromForm_AddMode_WritesDerivedRow, SaveFromForm_AddMode_NewInlineArtist_WritesDerivedRowWithRealId, CreateSongWithUrls_WritesDerivedRow |
| REQ-CATPROP-02 | An update that keeps the author leaves the catalog unchanged | SongService.UpdateSongCoreAsync | Update_SameAuthor_CatalogUnchanged |
| REQ-CATPROP-03 | An author change moves the derived row, without duplicates | SongService.UpdateSongCoreAsync | Update_AuthorAtoB_MovesRow, Update_AuthorAtoB_BAlreadyCovers_NoDuplicate, Update_AuthorToNewInlineArtist_MovesRowWithRealId, Update_AuthorChange_OldDerivedRowMissing_StillWritesNew |
| REQ-CATPROP-04 | A refused or failed save leaves the catalog unchanged | SongService (single unit of work) | Create_ValidationFails_NoCatalogRow, Update_ArtistChoiceNeeded_CatalogUnchanged, Update_AuthorAtoB_SaveThrows_CatalogUnchanged |
| REQ-CATPROP-05 | Deleting a song removes all its catalog rows | Existing FK cascade (Catalog → Songs) | DeleteAsync_Song_RemovesDerivedAndCoverCatalogRows |
| REQ-CATPROP-06 | A derived row cannot be removed from the catalog | CatalogRepository.IsDerivedAsync, CatalogService.RemoveSongFromCatalogAsync | IsDerivedAsync_AuthorRow_ReturnsTrue, IsDerivedAsync_CoverRow_ReturnsFalse, IsDerivedAsync_MissingRow_ReturnsFalse, CatalogServiceTests (derived refused / cover removed / missing row) |
| REQ-CATPROP-07 | Bulk remove removes covers, keeps derived rows, reports counts | CatalogRepository.RemoveCoversAsync, CatalogService.RemoveSongsFromCatalogAsync | RemoveCoversAsync_MixedList_RemovesCoversKeepsDerived, RemoveCoversAsync_UnknownIds_CountsNeither, RemoveCoversAsync_DuplicateIds_CountedOnce, RemoveCoversAsync_OtherArtistsCoverOfSameSong_Untouched, CatalogServiceTests (empty list, unknown artist, message theory) |
| REQ-CATPROP-08 | Catalog-mode Delete removes from the catalog, never deletes songs | SongsViewModel.BuildDeleteConfirmMessage / ExecuteDeleteAsync | CatalogMode_OneSong_ConfirmTextNamesSongAndCatalog, CatalogMode_ThreeSongs_ConfirmTextCountsAll, CatalogMode_Confirm_CallsBulkRemoveNeverDeleteSongs_AndShowsSuccess, CatalogMode_RemoveFails_ShowsErrorStillReloads_NeverDeletesSongs, SongsMode_Confirm_StillDeletesSongs |
| REQ-CATPROP-09 | Backfill adds every missing derived row, touches nothing else, is idempotent | Migration 20261120052950_BackfillDerivedCatalogRows | CatalogBackfillMigrationTests (4 tests) |
| REQ-CATPROP-10 | Artist delete blocked only by owned songs; catalog rows go with the artist | SongRepository.CountByArtistAsync, ArtistService.DeleteArtistsAsync, ArtistsViewModel.BuildDeleteConfirmMessage | CountByArtistAsync_ReturnsOwnedSongsOnly, Delete_OwnerOfSongs_Refused, Delete_TwoOwners_NamesFirstInSelectionOrder, Delete_ArtistWithOnlyCatalogRows_Succeeds, Delete_CoverOnlyArtist_RowsGoneSongsKept, OneArtist_ConfirmTextMentionsCatalogEntries, TwoArtists_ConfirmTextMentionsCatalogEntries |
| REQ-CATPROP-11 | An author with only derived rows is listed under Performers | ArtistRepository (Performers filter, unchanged) | PerformersOnly_AuthorWithOnlyDerivedRows_IsReturned |

---
## Task C8: Device check (Helder)
**Status:** Done (2026-11-20)

### Evidence (reported by Helder)
- Tests all good on device; nothing pending.
- A song picked from Deezer and saved locally appears in its author's catalog.
- That song cannot be removed from the author's catalog (ownership refusal).
- The backfill migration ran fine at app startup.
