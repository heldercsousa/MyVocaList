# Task log - song writes propagate to the artist catalog

---
## Task: C1 - Repository queries
**Status:** To Review
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
**Status:** To Review (Elevated lane: migration)

### Changed files:
- `Infra/Migrations/20261120052950_BackfillDerivedCatalogRows.cs` (+ `.Designer.cs`; snapshot unchanged)
- `Tests/Integration/Migrations/CatalogBackfillMigrationTests.cs`
- `Tests/Integration/Repositories/ArtistRepositoryTests.cs`
- `Tests/Integration/Migrations/RemoveSongVersionMigrationTests.cs` (one test pinned to its own migration)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/task-log.md`

### Test evidence
RED: `BackfillSql` const added with an empty `Up`; 3 of 5 new tests failed (SongWithoutDerivedRow, CoverRowsUntouched, SecondApply: Actual empty). ManualAuthorRow and PerformersOnly are guards that pass by design (INSERT OR IGNORE; repository unchanged).
GREEN: 5 passed; full `dotnet test Tests/MyVocaList.Tests.csproj` 1863 passed, 0 failed.
`RemoveSongVersionMigrationTests.Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty` migrated to latest and asserted Catalog unchanged; it now correctly sees backfilled rows, so it is pinned to `NewMigrationId(db)` (intent REQ-RMV-03 unchanged). Not in design.md section 9 list; flagged for review.

| AC | Implementation | Test |
|----|----------------|------|
| REQ-CATPROP-09 | BackfillDerivedCatalogRows.BackfillSql | CatalogBackfillMigrationTests (4 tests) |
| REQ-CATPROP-11 | existing PerformersOnly filter | ArtistRepositoryTests.PerformersOnly_AuthorWithOnlyDerivedRows_IsReturned |

---
## Task: C3a - Test setup: register ICatalogRepository in SongService test scopes (no AC; setup only)
**Status:** To Review (Standard lane: test setup)

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
