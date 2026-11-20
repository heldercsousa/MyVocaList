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
