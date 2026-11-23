# Remove the Song Version and Featured artists fields — Task log

---
## Task: T1 Domain + Infra + repository tests (Wave 1)
**Plan:** `tasks.md` (T1)
**Status:** blocked: spec gap
**Started:** 2026-11-20
**Completed:** — (T1 not ticked; see the blocker below)

### Blocker: spec gap (wave ordering vs. Contracts DTO)
**Location:** `design.md` § Waves (Wave 1 = Domain + Infra) and § Integration contracts (`SongListItemDto` removes `FeaturedArtists`, `Version`); `tasks.md` T3 owns the DTO.
**Gap description:** `SongListItemDto` (Contracts) is constructed by two Infra repositories (`SongRepository`, `CatalogRepository`). Removing `Song.Version` / `Song.FeaturedArtists` forces the Infra call sites to change, but the DTO change belongs to Wave 2 (T3). Infra compiles only with a placeholder for the two removed positional arguments.
**Options:**
- Option A: move the `SongListItemDto` constructor change (Contracts file only) into T1 — Infra then uses the clean post-change constructor; Services and UI consumers of the DTO still fail to compile until T3/T5 (expected for the wave).
- Option B: keep the Wave 2 DTO change in T3 and keep the `null` placeholders in Infra as a bridge until T3 — Infra and Domain build now, and the placeholders are removed in T3.
**Recommendation:** Option B because it keeps the wave split as designed and the placeholders are behaviour-neutral (`FeaturedArtists` and `Version` were removed fields, so `null` is the only correct value for them).
**Blocking:** Yes for ticking T1 (the Infra call sites carry placeholders that T3 must remove). The committed Domain + Infra change itself is consistent and builds.

### Implementation notes (T1)
- `Domain/RepositoryInterface/ISongRepository.cs`: the `ExistsByTitleForArtistAsync(artistId, title[, excludeId], ct)` overload set already existed without Version, so the Version overloads were removed rather than renamed. `FindIdByTitleVersionForArtistAsync` renamed to `FindIdByTitleForArtistAsync(artistId, title, excludeId, ct)`. `FindByTitleAndArtistNamesAsync` takes `IReadOnlyCollection<(string Title, string ArtistName)>` and keeps its shipped return type `IReadOnlyList<SongSearchHit>`.
- Design-vs-code mismatch (not changed): design.md shows `FindByTitleAndArtistNamesAsync` returning `IReadOnlyDictionary<(string Title, string ArtistName), int>`; the shipped code returns `IReadOnlyList<SongSearchHit>`. Per design.md § Integration contracts ("Exact existing signatures are authoritative"), the shipped shape was kept. Logged as a design concern for Helder.
- `Infra/Repository/SongRepository.cs` and `CatalogRepository.cs`: `SongListItemDto` construction passes `null` for the two removed positional arguments (`FeaturedArtists`, `Version`) until the DTO change lands (see Blocker). Runtime value is correct: both were removed fields.
- `Domain/Resolution/SongFormSaveRequest.cs`: `SongDecision.SaveAsNewVersion` removed; `SaveAsNewSong` kept.
- Existing SongSearch/projection SQL no longer selects `Version` or `FeaturedArtists`.
- Remaining mention outside T1 scope: `Domain/ServicesInterfaces/ISongService.cs:96` doc comment names FeaturedArtists and Version (T3 owner).

### Test changes (superseded ACs removed; identity-only ACs added)
| Test (file) | Action | Reason (AC) |
|-------------|--------|-------------|
| `SongRepositoryResolutionTests.ExistsByTitleVersionForArtistAsync_SameTitleDifferentVersion_ReturnsFalse` | deleted | AC-5.3 (version concept removed) |
| `SongRepositoryResolutionTests.DuplicateTitleVersion_ThrowsDbUpdateException` | renamed to `DuplicateTitleForArtist_ThrowsDbUpdateException` (2-col index) | AC-5.5 → INV-RMV-1 / REQ-RMV-02 |
| `SongRepositoryResolutionTests.ExistsByTitleVersionForArtistAsync_*` (3 others) | rewritten as `ExistsByTitleForArtistAsync_*` | AC-5.1, 5.2, 5.4 → REQ-RMV-06 |
| `SongRepositoryTests.FindIdByTitleVersionForArtistAsync_DifferentVersion_ReturnsNull` | deleted | REQ-SFT-29 (version part) |
| `SongRepositoryTests.FindIdByTitleVersionForArtistAsync_*` (2 others) | renamed to `FindIdByTitleForArtistAsync_*`, Version argument removed | REQ-SFT-42 unchanged |
| `SongRepositoryTests.FindIdByTitleForArtistAsync_CaseOnlyDifference_ReturnsStoredId` | added | REQ-RMV-06 |
| `SongRepositorySearchTests.FindByTitleAndArtistNamesAsync_CollatedTripleEmptyVersion_Returns` | renamed `..._CollatedPair_Returns`, Version assertion removed | REQ-SFT-05 (Version part superseded) |
| `SongRepositorySearchTests.FindByTitleAndArtistNamesAsync_VersionMustMatch` | replaced by `..._TitleWithExtraText_MatchesOnlyItsOwnRow` | REQ-SFT-05 version part; REQ-RMV-06 |
| `SongRepositorySearchTests` seed | "Don't Cry" + "Live" (same identity) changed to "Don't Cry (Live)" | a second same-identity row is now rejected by the unique index (INV-RMV-1) |
| `SongListItemProjectionTests.GetPagedAsync_SongWithVersion_CarriesVersion` | deleted | REQ-ART-19 version part superseded (REQ-RMV-09) |
| `SongListItemProjectionTests.GetPagedByArtistAsync_SongWithImageAndVersion_*` | renamed `..._SongWithImage_CarriesImage`, version assertion removed | REQ-ART-19 image part unchanged |
| `PersistedStringTrimmingTests.SongFeaturedArtists_WhitespaceOnly_PersistedAsNull` | deleted | REQ-RMV-10 (field removed; REQ-TRIM-07 generic text unchanged) |
| `SongRepositoryArtistTrigramTests`, `SongSearchHitImageTests`, `SongRepositoryListSearchTests`, `LocalImageReferencesRepositoryTests` | Version arguments/properties removed from helpers; no assertion change | mechanical |

REQ-RMV-09 (Songs-list supporting text = artist only) is NOT covered in T1: `SupportingText` lives in the Contracts DTO (T3 owns it; the test is `SongListItemDtoTests`).

### Traceability (T1 scope)
| REQ / INV | Covered in T1 by | Status |
|-----------|------------------|--------|
| REQ-RMV-02 (entity, configuration, unique index) | `SongRepositoryResolutionTests.DuplicateTitleForArtist_ThrowsDbUpdateException` (EnsureCreated, passes); the model snapshot is T2 | partial |
| REQ-RMV-06 (identity-only duplicate lookups, case and accent) | `SongRepositoryResolutionTests.ExistsByTitleForArtistAsync_AccentAndCaseInsensitive_Matches`, `..._ExcludeId_ExcludesSelf`, `..._DifferentArtist_ReturnsFalse`; `SongRepositoryTests.FindIdByTitleForArtistAsync_AccentOnlyDifference_ReturnsStoredId`, `..._CaseOnlyDifference_ReturnsStoredId`, `..._ExcludeSelf_ReturnsNull` (all pass); `SongRepositorySearchTests.FindByTitleAndArtistNamesAsync_CollatedPair_Returns`, `..._TitleWithExtraText_MatchesOnlyItsOwnRow` (MigrateAsync, blocked on T2) | covered, two tests pending T2 |
| REQ-RMV-10 (persisted fields only) | `PersistedStringTrimmingTests` (FeaturedArtists test deleted; remaining tests pass) | covered |
| INV-RMV-1 | `SongRepositoryResolutionTests.DuplicateTitleForArtist_ThrowsDbUpdateException` | covered |
| REQ-RMV-03, 04, 05; INV-RMV-2 | T2 (migration) | not in T1 |
| REQ-RMV-09 | T3 (`SongListItemDtoTests`) | not in T1 |

### Changed files:
- `Domain/Entity/Song.cs`
- `Domain/ReadModels/SongSearchHit.cs`
- `Domain/RepositoryInterface/ISongRepository.cs`
- `Domain/Resolution/Candidates.cs`
- `Domain/Resolution/ResolutionResults.cs`
- `Domain/Resolution/SongFormSaveOutcome.cs`
- `Domain/Resolution/SongFormSaveRequest.cs`
- `Infra/EntityEFConfig/SongConfiguration.cs`
- `Infra/Repository/CatalogRepository.cs`
- `Infra/Repository/SongRepository.cs`
- `Tests/Integration/Repositories/LocalImageReferencesRepositoryTests.cs`
- `Tests/Integration/Repositories/PersistedStringTrimmingTests.cs`
- `Tests/Integration/Repositories/SongListItemProjectionTests.cs`
- `Tests/Integration/Repositories/SongRepositoryArtistTrigramTests.cs`
- `Tests/Integration/Repositories/SongRepositoryListSearchTests.cs`
- `Tests/Integration/Repositories/SongRepositoryResolutionTests.cs`
- `Tests/Integration/Repositories/SongRepositorySearchTests.cs`
- `Tests/Integration/Repositories/SongRepositoryTests.cs`
- `Tests/Integration/Repositories/SongSearchHitImageTests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/task-log.md` (this file)

### Build notes
Build (Infra project, which builds Domain + Contracts + Extensions): **passed, 0 errors, 0 warnings**.
Full solution / `Tests` project: **not built** — Services and the MAUI head still reference the removed members (expected for Waves 2-3).
Tests (isolated harness, not committed: Domain + Contracts + Infra + Extensions + `Tests/Integration/Repositories/*.cs` + `Tests/Infrastructure/TestDbContextFactory.cs`): **193 total, 113 passed, 80 failed**. All 80 failures are `PendingModelChangesWarning` in `MigrateAsync`-based fixtures (`SongRepositorySearchTests`, `SongRepositoryListSearchTests`, `SongRepositoryArtistTrigramTests`, `SongListItemProjectionTests`, `SongSearchHitImageTests`, `LocalImageReferencesRepositoryTests`, `ArtistRepositorySearchTests`, `ArtistListItemProjectionTests`). Cause: the EF model no longer has the columns but no migration removes them yet (T2). Every EnsureCreated-based test in the rewritten set passes.
Commit SHA: none. `git commit` was blocked by the pre-commit hook (`dotnet test` against `MyVocaList.Tests/MyVocaList.Tests.csproj`, a path that does not exist; the real project is `Tests/MyVocaList.Tests.csproj`, which cannot build in Wave 1). The 21 files are staged, not committed; `--no-verify` was NOT used. Needs Helder's decision (see Blocker 2).

### Blocker 2: pre-commit gate (commit not created)
**Gap description:** the pre-commit hook runs the test project in a state that cannot pass by design (Services/UI still reference removed members; migration T2 pending) and points at a non-existent csproj path.
**Options:** A: authorise `git commit --no-verify` for this Wave 1 checkpoint. B: fix the hook path and gate tests per wave. C: hold T1 until T2 and Wave 2 land so the hook can pass.
**Recommendation:** Option A for this wave only, because the hook path is wrong regardless and the solution cannot be green until Wave 3.
**Blocking:** Yes for the commit.
Files written and re-read: all Domain and Infra files listed above (Grep verifies no `Version`/`FeaturedArtists`/`SaveAsNewVersion`/`ExistsByTitleVersion`/`FindIdByTitleVersion` left in `Domain/` and `Infra/` outside `Migrations/`).

---
## Task: T2 Migration `RemoveSongVersionAndFeaturedArtists` + snapshot + migration tests (Wave 1)
**Plan:** `tasks.md` (T2)
**Status:** To Review
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `Infra/Migrations/20261120031836_RemoveSongVersionAndFeaturedArtists.cs` (new, hand-edited Up; Down generated by `dotnet ef`)
- `Infra/Migrations/20261120031836_RemoveSongVersionAndFeaturedArtists.Designer.cs` (new, `dotnet ef migrations add`)
- `Infra/Migrations/AppDbContextModelSnapshot.cs` (`dotnet ef`; Version and FeaturedArtists removed, `IX_Songs_ArtistId_Title` added)
- `Tests/Integration/Migrations/RemoveSongVersionMigrationTests.cs` (new)
- `Tests/Integration/Migrations/SongMigrationTests.cs` (pinned to the last pre-change migration `20261116152258_AddSongLyricsOrigin` where the test seeds or asserts `Version`; `M1_ExistingRowsSurvive` and `M2_DuplicateIdentities` migrate to their own targets, not past the change)
- `Tests/Integration/Migrations/ImageUrlMigrationTests.cs` (seed insert without `Version`, which no longer exists at the latest schema)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/design.md` (one `Spec updated` note under § Migration)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/tasks.md` (T2 ticked)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/task-log.md` (this entry)

### Implementation decisions and deviations (T2)
1. **Native DROP COLUMN instead of EF `DropColumn`.** EF's SQLite rebuild renames `__ef_temp_Songs` and the FTS triggers that reference `Songs` fail with `error in trigger SongSearch_artist_au: no such table: main.Songs` (observed). Both columns are dropped with `ALTER TABLE "Songs" DROP COLUMN` (same approach as the `AddSongLyricsOrigin` Down). Triggers and rows are untouched, so no trigger recreation is needed. The design text says "EF performs the table rebuild": that path does not work with the triggers present.
2. **`IX_Songs_ArtistId_Title_Version` is dropped first**, before the renames. A rename can collide under the 3-column index (test `Rename_NeverFails_WhenRenamedTitleWouldCollideUnderTheOldThreeColumnIndex`), which would violate REQ-RMV-04 (e).
3. **Pass B runs three times** (a fixed repetition, since SQL has no loop). One application resolves all collisions except literal ` #<Id>` titles that themselves chain. A deeper chain fails the `CREATE UNIQUE INDEX` loudly; it does not corrupt data. Helder to confirm the bound (Blocking: No; assumption).
4. **Whitespace for "empty" Version is `char(32, 9, 10, 13)`**, not plain `TRIM(Version)`, so tab and newline-only Versions count as empty (REQ-RMV-04 a says "whitespace-only").
5. Pass B reads "colliding with a pre-existing row" as: the row with the higher Id is the one renamed (design SQL, lower Id keeps the title). Test `Rename_RowCollidingWithPreExistingRow_GetsIdSuffix` pins the case where the migrated row is higher-Id than the pre-existing one.

### Test changes (T2)
| Test | Action | Reason |
|------|--------|--------|
| `SongMigrationTests.M1_ExistingRowsSurvive` | migrate to `DropHasManualEdits` instead of latest | asserts `Version`; latest drops it |
| `SongMigrationTests.M1_TableRebuild_KeepsUniqueCollatedIndexes`, `M2_UniqueIndex_RejectsSecondSongWithSameIdentity`, `M2_NullIdentities_AllowMany`, `SongExecuteDelete_RemovesSongSearchRow` | pinned to `LastBeforeRemoveVersion` | seed or assert `Version` (historical schema) |
| `SongMigrationTests.M2_DuplicateIdentities_...` | migrate to `SongExternalIdentityUnique` instead of latest | asserts `Version` column in the output |
| `ImageUrlMigrationTests.ImageUrl_RollbackToBeforeMigration_...` | seed insert without `Version` | column absent at latest |
| `LyricsOriginMigrationTests` | no change | migrates to its own targets and reads through the entity |
| New `RemoveSongVersionMigrationTests` (13 tests) | added, Red first (failed on `PendingModelChangesWarning` before the migration existed) | REQ-RMV-03, 04 (a, b, c, d, e), 05; INV-RMV-1, INV-RMV-2; no-collision DB |

### Traceability (T2 scope)
| REQ / INV | Test | Status |
|-----------|------|--------|
| REQ-RMV-02 (2-column collated unique index, 3-column gone) | `Up_UniqueTwoColumnIdentityIndex_UsesCollatedTitle`, `Up_ThreeColumnIndexIsGone` | covered |
| REQ-RMV-03 (columns dropped, indexes and FTS triggers kept, FK check, row count) | `Up_DropsVersionAndFeaturedArtistsColumns`, `Up_KeepsExternalIdentityIndexAndSearchTriggers`, `Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty` | covered |
| REQ-RMV-04 (a) | `Rename_EmptyVersionLowestIdKeepsTitle_WhitespaceVersionCountsAsEmpty`, `Rename_NoEmptyVersion_LowestIdKeepsTitle` | covered |
| REQ-RMV-04 (b) | `Rename_SuffixUsesTrimmedVersion` | covered |
| REQ-RMV-04 (c) | `Rename_RowCollidingWithPreExistingRow_GetsIdSuffix` | covered |
| REQ-RMV-04 (d) | `Rename_TruncatesTitleSoSuffixStaysIntactAtMost250` | covered |
| REQ-RMV-04 (e) | `Rename_NeverFails_WhenRenamedTitleWouldCollideUnderTheOldThreeColumnIndex` | covered |
| REQ-RMV-05 (Down) | `Down_RestoresEmptyColumnsAndThreeColumnIndex_DoesNotUndoRenames`, `Down_IsRegisteredAsTheNewMigration` | covered |
| INV-RMV-1 | `Up_UniqueTwoColumnIdentityIndex_UsesCollatedTitle` + rename tests (no identity left) | covered |
| INV-RMV-2 | `Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty` | covered |
| No-collision DB | `Rename_NoCollisionDatabase_TitlesUnchanged` | covered |

### Build notes
Harness (isolated, not committed): `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\aabd8c83-a176-4748-8135-0270645bdd12\scratchpad\repo-harness\RepoHarness.csproj` (Domain + Contracts + Infra + Extensions + `Tests/Integration/Repositories/*.cs` + `Tests/Integration/Migrations/*.cs` + `Tests/Infrastructure/TestDbContextFactory.cs`).
- Migration tests only: **45 total, 45 passed, 0 failed** (Red before the migration: 37 failed, 8 passed; Red failures were `PendingModelChangesWarning`).
- Full harness: **238 total, 233 passed, 5 failed**. Repositories alone: 193 total, 188 passed, 5 failed.
- The 80 `PendingModelChangesWarning` failures from T1 are **0** now.
- Remaining 5 failures, all `SongRepositoryListSearchTests.GetPagedAsync_*`: `The required column 'FeaturedArtists' was not present in the results of a 'FromSql' operation`. The mapped type is `SongListItemDto` (Contracts), which still has `FeaturedArtists`. That DTO is T3 (Wave 2); not a T2 defect.
- `dotnet ef migrations add RemoveSongVersionAndFeaturedArtists -p Infra -s Infra`: succeeded (tools 9.0.6 vs runtime 10.0.0 warning only). No database was touched.
- Commit: **none** (staged only, per the commit policy; `--no-verify` not used).

Files written and re-read: migration `Up` (post-edit read, lines 14–60), snapshot (`IX_Songs_ArtistId_Title` at line 404, no `Version` or `FeaturedArtists`), `design.md` note, `tasks.md` T2 line, the test edits (grep-verified).

---
## Task: T3 DTOs + SongService/SongResolutionService + service tests (Wave 2)
**Plan:** `tasks.md` (T3)
**Status:** To Review
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `Contracts/DTOs/List/SongListItemDto.cs` (FeaturedArtists, Version removed; SupportingText = artist only, REQ-RMV-09)
- `Contracts/DTOs/MusicSearchResultDto.cs` (FeaturedArtists, Version removed)
- `Domain/ServicesInterfaces/ISongService.cs` (ValidateVersionInput removed; version/featuredArtists parameters removed; doc comment at former line 96 fixed)
- `Services/SongService.cs` (MaxVersionLength, ValidateVersionInput, SaveAsNewVersion path removed; identity-only duplicate check on create/edit/import write paths; single message `DuplicateTitleMessage`)
- `Services/SongResolutionService.cs` (exact local match via `FindIdByTitleForArtistAsync`; Version removed from SongMatch)
- `Infra/Repository/SongRepository.cs` (3 placeholder `null` arguments removed; the one allowed Infra edit)
- `Infra/Repository/CatalogRepository.cs` (1 placeholder `null` argument removed)
- `Tests/Unit/Contracts/SongListItemDtoTests.cs` (rewritten: REQ-RMV-09)
- `Tests/Unit/Services/SongServiceTests.cs`
- `Tests/Unit/Services/SongResolutionServiceTests.cs`
- `Tests/Unit/Services/SongServiceSaveFromFormTests.cs`
- `Tests/Unit/Services/SongServiceSaveFromFormImageTests.cs`
- `Tests/Unit/Services/SongServiceSaveFromFormPictureTests.cs`
- `Tests/Unit/Services/SongServiceArtistFreezeTests.cs`
- `Tests/Unit/Services/SongServiceTitleFreezeTests.cs`
- `Tests/Unit/Services/SongServiceLyricsOriginTests.cs`
- `Tests/Unit/Services/SongServiceEnsureLyricsTests.cs`
- `Tests/Unit/Services/ArtistSuggestionServiceImageTests.cs` (positional MusicSearchResultDto calls; outside the Grep list but affected by the DTO change)
- `Tests/Unit/Services/ArtistSuggestionThresholdTests.cs` (same reason)
- `Tests/Integration/Services/SongServiceUpdateIntegrationTests.cs` (+2 real-SQLite REQ-RMV-06 tests)
- `Tests/Integration/Services/SongSaveTrigramDuplicateTests.cs`
- `Tests/Integration/Services/SongServiceEnsureLyricsRealStackTests.cs`
- `Tests/Integration/Services/SongServiceSaveFromFormImageRealStackTests.cs`
- `Tests/Integration/Services/SongServiceSaveFromFormPictureRealStackTests.cs`
- `Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs` (repository decorator aligned to the new ISongRepository)
- `Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/task-log.md` (this entry)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/tasks.md` (T3 ticked)

### Test changes (superseded ACs removed or rewritten)
| Test (file) | Action | Reason (AC) |
|-------------|--------|-------------|
| `SongServiceTests.ValidateVersionInput_*` (5) | deleted | REQ-RMV-10 (version validation removed) |
| `SongServiceTests.UpdateSongAsync_VersionChangedTo*` (2) | deleted | REQ-SFT-29 version part; replaced by identity tests |
| `SongServiceTests.UpdateSongAsync_EditedTitleCollidesWithArtistTitleVersion_Rejected` | replaced by `UpdateSongAsync_RenameIntoIdentityOfAnotherSong_FailsWithDuplicateMessage` | REQ-SFT-27 version part; REQ-RMV-06 |
| `SongServiceTests.UpdateSongAsync_WithVersion_*`, `..._NullVersion_*`, `..._VersionTooLong_*` (BUG-024 version) | deleted | REQ-RMV-10 |
| `SongServiceTests.CreateSongWithUrlsAsync_DuplicateTitleVersion_*` | renamed `..._DuplicateIdentity_*`, asserts the single message | REQ-SFT-29 version part; REQ-RMV-06/10 |
| `SongServiceTests.GetSongByIdAsync_ExistingId_ReturnsSongWithAllFields` | Version and FeaturedArtists assertions removed | REQ-RMV-10 |
| `SongServiceSaveFromFormTests.SaveFromForm_VersionOver60_*` | deleted | REQ-RMV-10 (Version field removed) |
| `SongServiceSaveFromFormTests.SaveFromForm_SaveAsNewVersion*` (3) | deleted | REQ-RMV-07 (SaveAsNewVersion removed) |
| `SongServiceSaveFromFormTests.SaveFromForm_SaveAsNewVersionIdentityHeld_*` | deleted; the SaveAsNewSong counterpart kept | REQ-SFT-39 version part |
| `SongServiceSaveFromFormTests` (new) `SaveFromForm_SaveAsNewSongOnExactIdentity_RefusedAndNothingWritten` | added | REQ-RMV-06 / REQ-RMV-07 |
| `SongServiceSaveFromFormTests.SaveFromForm_WriteReturnsFailureTuple_*`, `..._NewArtistThenSongRefused_*` | rewritten to the identity check and the single message | REQ-RMV-06/10 |
| `SongServiceArtistFreezeTests` / `SongServiceTitleFreezeTests` `SaveAsNewVersion*` | `..._SaveAsNewVersionOrNewSong_*` reduced to SaveAsNewSong; the SaveAsNewVersion title-freeze test deleted (its SaveAsNewSong twin kept) | REQ-SLD-18 / REQ-SLD-37 version part |
| `SongServiceSaveFromFormImageTests` `InlineData(SaveAsNewVersion)` | removed | REQ-ART-15 version part (SaveAsNewSong case kept) |
| `SongResolutionServiceTests` | Version arguments removed; `ResolveAsync_ExactLocalLookup_UsesArtistAndTitleOnly` and `ResolveAsync_CaseOnlyTitleDifference_ReturnsExactLocalMatchWithId` added | REQ-RMV-06 |
| `SongListItemDtoTests` | rewritten (3 tests) | REQ-ART-20 superseded; REQ-RMV-09 |
| `SongServiceUpdateIntegrationTests` (new) `UpdateSongAsync_RenameIntoCaseAndAccentVariantOfAnotherSong_*`, `CreateSongAsync_CaseAndAccentVariantOfExistingTitle_*` | added (real SQLite) | REQ-RMV-06 (case and accent, edit and create) |

### Traceability (T3 scope)
| REQ | Implementation | Test(s) | Status |
|-----|----------------|---------|--------|
| REQ-RMV-06 (identity-only duplicate: create, edit, import) | `SongService.CreateSongAsync`, `CreateSongWithUrlsCoreAsync`, `UpdateSongCoreAsync` (`ExistsByTitleForArtistAsync` / `FindIdByTitleForArtistAsync`); `SongResolutionService.ResolveAsync` | `SongServiceTests.CreateSongAsync_DuplicateIdentity_ReturnsSingleDuplicateMessage`, `..._RenameIntoIdentityOfAnotherSong_*`, `..._CaseOnlyRenameOfOwnTitle_*`; `SongResolutionServiceTests.ResolveAsync_ExactLocalLookup_UsesArtistAndTitleOnly`, `..._CaseOnlyTitleDifference_*`; `SongServiceUpdateIntegrationTests` (2 real-SQLite tests, case and accent) | covered |
| REQ-RMV-07 (exact match: only Open existing / Cancel; no Version; SaveAsNewVersion gone; near-duplicate unchanged) | `SongService.RunSongGuardAsync` (exact → `SongChoiceNeeded`, `IsExactSongMatch`); `SongDecision.SaveAsNewVersion` gone (T1) | `SongServiceSaveFromFormTests.SaveFromForm_ExactDuplicateSong_ReturnsSongChoiceNeededExact`, `..._FuzzyDuplicateSong_ReturnsSongChoiceNeededNotExact`, `..._SaveAsNewSongOnExactIdentity_RefusedAndNothingWritten` | covered (sheet UI is T5/T6) |
| REQ-RMV-09 (supporting text = artist only) | `SongListItemDto.SupportingText` | `SongListItemDtoTests` (3) | covered; the list projection test is T1 (`SongListItemProjectionTests`) |
| REQ-RMV-10 (persisted fields only; single duplicate message; no version or featured validation) | `SongService` write paths; `DuplicateTitleMessage` | `SongServiceTests.CreateSongAsync_DuplicateIdentity_*`, `SongServiceSaveFromFormTests.SaveFromForm_WriteReturnsFailureTuple_*`, `PersistedStringTrimmingTests` (T1) | covered |
| REQ-RMV-11 (Deezer pick, artwork, lyrics unchanged) | no change in behaviour | existing `SongServiceLyricsOriginTests`, `SongServiceEnsureLyricsTests`, `SongServiceSaveFromFormPictureTests`, `SongServiceSaveFromFormImageTests`, `SongServiceTitleFreezeTests`, `SongServiceArtistFreezeTests` (kept, Version removed from fixtures) | covered by unchanged tests |
| REQ-RMV-08 (suggestions) | not in T3 | T4 | n/a |

### Build notes
- `dotnet build Services/MyVocaList.Services.csproj`: **exit 0, 0 errors** (final run, after all T3 edits; T4's in-progress `SongSuggestionService` / `DeezerProvider` were present in the worktree during the run).
- Isolated harness (scratchpad `t3-harness/T3Harness.csproj`, not committed): references Domain, Contracts, Infra, Extensions, Services; compiles the T3 test files, `Tests/Infrastructure/*`, `Tests/Integration/Repositories/*`, `Tests/Integration/Migrations/*`, and `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (pure DI, no MAUI types, needed by `UnitOfWorkTestHost`). **Build exit 0, 0 errors. Tests: 456 total, 456 passed, 0 failed.** This includes the 5 previously failing `SongRepositoryListSearchTests.GetPagedAsync_*` (now pass; their cause was the removed DTO columns).
- Full `Tests` project and `MyVocaList` head: **not built**. The head still references the removed members in the ViewModels and XAML (Wave 3, T5/T6, expected). The Tests project also contains the ViewModel tests (T5).
- Red-green note: the production changes landed before the test rewrites in this session, so the Red phase was not observed separately. Each new test was checked against the implementation by running it (see the counts above). Stated here rather than claimed as TDD Red.
- Commit: none (per policy; files staged only, `--no-verify` not used).

### Design concerns (for Helder)
- **Write-time guard for SaveAsNewSong on an exact identity:** REQ-RMV-06 says the save is not performed. A request carrying `SongDecision.SaveAsNewSong` for an exact identity is therefore refused by the write-time check (`Failed` with the single message), not offered as a choice. The sheet (T5/T6) offers only Open existing and Cancel for an exact match, so this only guards against a stale or forged decision. Behaviour is implemented as specified; flagging because the decision table in design.md does not say what the write path returns.
- **Dropped parameters of `CreateSongAsync` / `UpdateSongAsync`:** the removed positional arguments shift the remaining ones. The rewrite of every call site was checked by hand (silent shifts such as `(…, null, "x")` are the risk). Recommend a reviewer spot-check of the `SongServiceTests` update rewrites.

Files written and re-read: `SongListItemDto.cs`, `MusicSearchResultDto.cs`, `ISongService.cs` (interface and doc-comment regions), `SongService.cs` (create, update, save and guard regions), `SongResolutionService.cs` (lookup and SongMatch), `SongRepository.cs` / `CatalogRepository.cs` (three and one construction sites), `SongServiceTests.cs` (helper, deleted and new blocks), `SongResolutionServiceTests.cs` (helper), `SongListItemDtoTests.cs`. Grep (Tool) after the edits: no `Version`, `FeaturedArtists`, `SaveAsNewVersion`, `ExistsByTitleVersion`, `FindIdByTitleVersion`, `ValidateVersionInput` left in Tests (excluding ViewModels, T4 and T1/T2 files) or in the Services / Contracts / Domain production code.

---
## Task: T4 SongSuggestionService + DeezerProvider + suggestion tests (Wave 2)
**Plan:** `tasks.md` (T4)
**Status:** To Review
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (Version parameter and its doc removed)
- `Services/SongSuggestionService.cs` (CollapseSongGroups keeps the first row of each group; MapLocal and ResolveLocalArtistsAsync no longer carry Version; library check is a (Title, ArtistName) pair)
- `Services/DeezerProvider.cs` (title_version JSON member, NormaliseVersion and the Version argument removed; MusicSearchResultDto calls use named arguments)
- `Tests/Unit/Services/DeezerProviderTests.cs`
- `Tests/Unit/Services/SongSuggestionServiceTests.cs`
- `Tests/Unit/Services/SongSuggestionServiceGroupingTests.cs`
- `Tests/Unit/Services/SongSuggestionServiceDurationTests.cs`
- `Tests/Unit/Services/SongSuggestionServiceImageTests.cs`
- `Tests/Unit/Services/SongSuggestionThresholdTests.cs`
- `Tests/Integration/Services/SongSuggestionTitleSearchRealStackTests.cs`
- `tasks.md` (T4 ticked), `task-log.md` (this entry)

### Traceability (REQ-RMV-08)
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-RMV-08 (title_version ignored) | Deezer title_version does not change the row | `Services/DeezerProvider.cs` SearchSongsAsync (no version mapping) | `DeezerProviderTests.SearchSongsAsync_TitleVersionIgnored_TitleUnchanged` |
| REQ-RMV-08 (no version collapse step) | Rows differing only by version collapse by Deezer artist id + short title only | `Services/SongSuggestionService.cs` CollapseSongGroups | `SongSuggestionServiceGroupingTests.SearchTitleRemoteAsync_LabelledThenPlain_KeepsTheFirstRow`, `..._SeveralRowsOfOneSong_KeepsTheFirst`, `SongSuggestionTitleSearchRealStackTests.SearchTitleRemoteAsync_EmptyLibrary_ReturnsDeezerOptionsWithOkStatus` |
| REQ-RMV-08 (REQ-SLD-06 grouping stays) | Same artist id and title = one row; different artist = two rows | unchanged grouping | `..._AlbumRepeatsOfOneSong_OneRow`, `..._SameTitleTwoArtists_TwoRows`, `..._TitlesDifferingOnlyInCase_TwoRows` (existing, green) |
| REQ-RMV-08 (library check on song identity) | Library check uses (Artist, Title) only | `SongSuggestionService.DedupAgainstLibraryAsync` | `SongSuggestionServiceGroupingTests.SearchTitleRemoteAsync_LibraryCheckUsesArtistAndTitle`, `..._LibrarySongMatchesArtistAndTitle_HidesTheGroup`, `SongSuggestionServiceTests.SearchTitleRemoteAsync_CollatedTitleArtistMatch_Promoted` |
| REQ-SFT-04 (reduced) | Local option carries artist name and ids | `SongSuggestionService.MapLocal` | `SongSuggestionServiceTests.SearchTitleLocalAsync_MapsArtistAndIds` |

Superseded tests rewritten or removed (per `supersession.md`, SLD-REQ / SLD-DES / SFT-REQ rows for Version): `DeezerProviderTests.SearchSongsAsync_NormalisesVersion` (theory) removed; `SearchSongsAsync_MapsIdTitleShortVersionArtistName` renamed to `..._MapsIdTitleShortAndArtistName`; `SearchTitleRemoteAsync_KeptRow_HasBlankVersion` and `SearchTitleRemoteAsync_LibrarySongOnlyAsLive_GroupStaysRemote` removed (REQ-SLD-08 and the Live clause of REQ-SLD-09); `SearchTitleRemoteAsync_LiveThenPlain_KeepsThePlainRow` replaced by `..._LabelledThenPlain_KeepsTheFirstRow` (REQ-SLD-07 reduced); `..._OnlyLabelledRows_KeepsTheFirst` replaced by `..._SeveralRowsOfOneSong_KeepsTheFirst`; `..._LibraryCheckUsesBlankVersion` and `..._LibrarySongWithBlankVersion_HidesTheGroup` renamed to the artist-and-title forms; `SearchTitleLocalAsync_MapsVersionAndArtist` renamed to `..._MapsArtistAndIds`; `..._CollatedTitleArtistEmptyVersionMatch_Promoted` renamed to `..._CollatedTitleArtistMatch_Promoted`. Test helpers: `Triple(...)` became `Pair(title, artist)`; `Hit(...)` and the SongSearchHit helpers lost the version argument (SongSearchHit no longer has Version, T1). Integration: the `r.Version == ""` assertion became `r.ArtistName == "Banda X"` (the labelled row is no longer collapsed).

### Build notes
Build (full `Tests` project): **not green, outside T4 scope**. Its only errors are in `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (T5/T6 scope): CS0117 `SaveField`/`SongDecision` members, CS1061 on `ISongService`, `SongSuggestionDto` `.Version` (lines 594-595), `Song.Version`/`Song.FeaturedArtists` (929-930), CS1744 named `ImageUrl` (1129). The test project references the MAUI head, so it cannot compile until T5/T6 land. No error was reported in any T4 file.
Isolated verification: a temporary scratch project (Services, Domain, Contracts, Infra, Extensions references; my suggestion and Deezer unit tests plus `Tests/Infrastructure/UnitOfWorkMocks.cs` and a copy of the global usings without UI namespaces) built with 0 errors and ran: **87 total, 87 passed, 0 failed**. The scratch project was deleted afterwards. Not run: `SongSuggestionTitleSearchRealStackTests` (its file depends on MAUI-side namespaces such as CommunityToolkit and Navigation); its change is one assertion, verified by re-reading only.
Commit: **none** (staged policy; no `--no-verify`).

### Process note
The production edits (SongSuggestionService, DeezerProvider, SongSuggestionDto) were written before the test rewrites, so the Red state of the rewritten tests was not observed on this run. The test rewrites were verified Green against the final production code only.

### Spec decisions
- No spec text change needed. Implementation detail: `DeezerProvider` builds `MusicSearchResultDto` with named arguments and omits FeaturedArtists and Version, so the calls match the T3 DTO shape. Not a spec change.

### Spec gap: none found
`supersession.md` ambiguity 4 (grouping key) was resolved as the task brief states: REQ-SLD-06 artist-id grouping stays; only the library check uses (Artist, Title).

Files written and re-read: `SongSuggestionDto.cs` (signature lines 19-27), `SongSuggestionService.cs` (CollapseSongGroups 138-171, MapLocal 173-176, dedup pair 233-234, ResolveLocalArtists 272), `DeezerProvider.cs` (track and artist selects, record list), `DeezerProviderTests.cs` (120-160), `SongSuggestionServiceGroupingTests.cs` (helpers 57-69, rewritten block), `SongSuggestionServiceImageTests.cs` (117-153), `SongSuggestionServiceTests.cs` (helpers, local mapping, collated match), integration assertion (line 67-68).

---
## Task: T5 + T6 SongFormViewModel + SongFormPage + VM tests (Wave 3)
**Plan:** `tasks.md` (T5, T6)
**Status:** Check build
**Started:** 2026-11-20
**Completed:** 2026-11-20

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (Version and FeaturedArtists members, ValidateVersion command and validator, OnSongVersionChanged, ConfirmSaveAsNewVersionCommand and its flow, SaveField.Version and SaveAsNewVersion branches, Version in the suggestion label and in BuildSaveRequest removed; ConfirmSaveAsNewSongCommand kept for near-duplicates)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (Version and Featured Artists TextEdits, Version text in the candidate row, "Version label (required)" entry and "Save as new version" button removed; Save as new song kept with its existing `IsExactDuplicate` inverse binding)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (OnVersionUnfocused removed; VersionEntryRequired comment text updated). Done in T5 scope because the head could not compile without it.
- `Tests/Unit/ViewModels/SongFormViewModelTests.cs` (Version blur-validation block, clear-title Version test, FailedVersion test, ConfirmSaveAsNewVersion test deleted; Save-as-new-song test reduced to near-duplicate; exact-match sheet-state test, member-absence test added; hydration/save-edit tests rewritten without Featured/Version; helpers lost the version argument)
- `Tests/Unit/ViewModels/SongFormViewModelFrozenArtistTests.cs` (DeezerPick_LeavesTheVersion theory deleted; helper lost version)
- `Tests/Unit/ViewModels/SongFormViewModelFrozenTitleTests.cs` (helper lost version; Version initializer and ValidateVersionInput setup removed)
- `Tests/Unit/ViewModels/SongFormViewModelImageTests.cs` (Featured clause removed from the Deezer-pick test, renamed; supporting-text expectations without version; helpers lost version; CreateSongAsync/UpdateSongAsync verifies at the current arity)
- `Tests/Unit/ViewModels/SongFormViewModelPictureTests.cs` (helper lost version; positional DTO in the thumbnail test lost the empty version slot)
- `Tests/Unit/ViewModels/SongFormViewModelLyricsTests.cs` (helper lost version; ValidateVersionInput setup removed)
- `Tests/Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs` (helper lost version slot; Version initializer and setup removed)
- `Tests/Unit/ViewModels/SongFormViewModelLoadingTests.cs` (Version initializer and setup removed)
- `Tests/Unit/ViewModels/SongFormViewModelLoadOffUiThreadTests.cs` (Version initializer removed)
- `Tests/Unit/ViewModels/LoadingHoldTests.cs` (Version initializer and setup removed)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/task-log.md` (this entry)
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/tasks.md` (T5, T6, T7 ticked)

### Tests deleted (superseded ACs)
| Test (file) | Reason (AC) |
|-------------|-------------|
| `SongFormViewModelTests` Version blur-validation block (`ValidateVersionCommand_*`, `OnSongVersionChanged_*`, 4 tests) | REQ-RMV-01 (Version field removed) |
| `ClearTitle_AfterRemotePick_KeepsVersionEditedAfterPick` | REQ-SFT-14 version part; REQ-RMV-01 |
| `SaveCommand_FailedVersion_ShowsMessageOnVersionField` | REQ-SFT-22 (SaveField.Version removed) |
| `ConfirmSaveAsNewVersionCommand_ReInvokesSaveWithSaveAsNewVersionDecision` | REQ-RMV-07 |
| `TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart`, `TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart` | REQ-ART-07 version part; REQ-RMV-01 |
| `SongFormViewModelFrozenArtistTests.DeezerPick_LeavesTheVersionAsTheUserLeftIt` (theory) | REQ-SLD-08 version clause; REQ-RMV-11 |

### Tests rewritten (superseded clauses only; behaviour kept)
- `TitleOptionSource_LocalHit_*`, `TitleOptionSource_RemoteHit_*`, `TitleOption_RemoteRow_*`, `TitleOption_LocalRow_*`, `TitleOption_AlbumEqualToTitleIgnoringCase_*`, `TitleOption_LocalRowCarryingAlbum_*`: supporting text without the version part (REQ-RMV-01, REQ-ART-07).
- `ConfirmSaveAsNewSongCommand_ReInvokesSaveWithSaveAsNewSongDecisionWithoutVersion` renamed to `..._NearDuplicate_ReInvokesSaveWithSaveAsNewSongDecision`; asserts `IsExactDuplicate` is false first (REQ-RMV-07).
- `Decisions_PersistAcrossReinvocations_WithinOneSaveAttempt`: near-duplicate choice, `ConfirmSaveAsNewSongCommand` (was SaveAsNewVersion).
- `LoadSongForEdit_ExistingSong_HydratesFeaturedArtistsLyricsAndVersion` renamed `..._HydratesTitleAndLyrics`; `SaveAsync_EditMode_SendsSongIdAndHydratedFieldsAndEditedVersion` renamed `..._SendsSongIdAndHydratedTitleAndLyrics` (REQ-RMV-10).
- `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing` renamed `..._LeavesLyricsAndKaraokeUrlsAndWritesNothing` (REQ-RMV-11).

### Tests added
- `SaveCommand_ExactDuplicate_SheetStateIsExactWithTheMatchAsOpenExistingOnly` (REQ-RMV-07)
- `SongFormViewModel_HasNoVersionFeaturedArtistsOrSaveAsNewVersionMembers` (REQ-RMV-01, reflection over the VM members)
- `ConfirmSaveAsNewSongCommand_NearDuplicate_ReInvokesSaveWithSaveAsNewSongDecision` (REQ-RMV-07, near-duplicate keeps Save as new song; the existing test above, renamed)

### Traceability (Wave 3 scope; REQ-RMV rows from earlier waves listed for completeness)
| AC | Criterion | Implementation location | Test method | Status |
|----|-----------|-------------------------|-------------|--------|
| REQ-RMV-01 | No Version and no Featured artists field on the Song form; remaining order kept | `SongFormViewModel.cs` (members removed); `SongFormPage.xaml` (two TextEdits removed; Title, Artist, Lyrics, YouTube order unchanged) | `SongFormViewModelTests.SongFormViewModel_HasNoVersionFeaturedArtistsOrSaveAsNewVersionMembers`; `TitleOptionSource_LocalHit_SupportingTextIsArtistAndLibraryMarker` | Build verified; on-device check pending (Helder) |
| REQ-RMV-02 | Model, config and snapshot define neither column; index `IX_Songs_ArtistId_Title` | T1 (`Song.cs`, `SongConfiguration.cs`, `AppDbContextModelSnapshot.cs`) | T1 repository and migration tests | covered in T1 |
| REQ-RMV-03 | Migration drops both columns, keeps index, FK check, row count | T2 `Infra/Migrations/20261120031836_RemoveSongVersionAndFeaturedArtists.cs` | `Tests/Integration/Migrations/RemoveSongVersionMigrationTests.cs` | covered in T2 |
| REQ-RMV-04 | Title renames on collision (rules a–e) | T2 migration Up | `RemoveSongVersionMigrationTests` (one test per rule) | covered in T2 |
| REQ-RMV-05 | FeaturedArtists discarded; Down re-adds shape, lossy | T2 migration Down | `RemoveSongVersionMigrationTests.Down_*` | covered in T2 |
| REQ-RMV-06 | Identity-only duplicate detection on create, edit, import | T3 `SongService.cs`, `SongResolutionService.cs`, `SongRepository.cs` | `SongServiceTests.CreateSongAsync_DuplicateIdentity_*`, `SongServiceUpdateIntegrationTests` (real SQLite), `SongResolutionServiceTests.ResolveAsync_ExactLocalLookup_*` | covered in T3 |
| REQ-RMV-07 | Exact match: sheet offers Open existing and Cancel only; near-duplicate keeps Save as new song; SaveAsNewVersion gone | `SongFormViewModel.cs` (`ApplyOutcomeAsync` sets `IsExactDuplicate`; `ConfirmSaveAsNewVersion` removed); `SongFormPage.xaml` (Save as new version block removed; Save as new song shown only when `IsExactDuplicate` is false); T3 `SongService.RunSongGuardAsync` | `SongFormViewModelTests.SaveCommand_ExactDuplicate_SheetStateIsExactWithTheMatchAsOpenExistingOnly`; `ConfirmSaveAsNewSongCommand_NearDuplicate_ReInvokesSaveWithSaveAsNewSongDecision`; `Decisions_PersistAcrossReinvocations_WithinOneSaveAttempt`; T3 `SongServiceSaveFromFormTests.SaveFromForm_ExactDuplicateSong_*` | VM and service covered; sheet rendering pending on-device check |
| REQ-RMV-08 | `title_version` ignored; no version collapse; REQ-SLD-06 grouping kept | T4 `DeezerProvider.cs`, `SongSuggestionService.cs` | `DeezerProviderTests.SearchSongsAsync_TitleVersionIgnored_TitleUnchanged`; `SongSuggestionServiceGroupingTests.*` | covered in T4 |
| REQ-RMV-09 | Songs list supporting text is the artist name only | T3 `SongListItemDto.cs` | `SongListItemDtoTests` (3) | covered in T3 |
| REQ-RMV-10 | Persisted fields only; no Version or Featured validation; single duplicate message; title limit unchanged | T3 `SongService.cs`; VM `RunSaveAsync` / `BuildSaveRequest` (only title validated) | `SongFormViewModelTests.LoadSongForEdit_ExistingSong_HydratesTitleAndLyrics`; `SaveAsync_EditMode_SendsSongIdAndHydratedTitleAndLyrics`; `SaveCommand_TypedArtist_SendsTrimmedNameWithoutArtistId`; T3 `SongServiceTests.CreateSongAsync_DuplicateIdentity_ReturnsSingleDuplicateMessage` | covered |
| REQ-RMV-11 | Deezer pick: artwork, Lyrics, Lyrics origin unchanged | No VM behaviour change (VM only lost members) | `SongFormViewModelImageTests.SelectTitle_RemoteOption_LeavesLyricsAndKaraokeUrlsAndWritesNothing`; `SongFormViewModelFrozenArtistTests.Save_PickWithOnlyOneIdentityPart_*`; `SongFormViewModelPictureTests` (artwork); T3 lyrics-origin tests unchanged | covered by unchanged and renamed tests |
| INV-RMV-1 / INV-RMV-2 | One row per identity; no song row deleted or recreated | T2 migration and T1 index | T2 migration tests | covered in T2 |

### Grep sweep (T7)
Fresh Grep over `*.cs` and `*.xaml` in the worktree for `\bVersion\b`, `SongVersion`, `FeaturedArtists`, `Featured`, `SaveAsNewVersion`, `ValidateVersionInput`, `title_version`, `ExistsByTitleVersion`, `FindIdByTitleVersion`, `VersionEntryRequired`, `VersionHasError`, `ConfirmSaveAsNewVersion`, `SaveField.Version`, `OnVersionUnfocused`, `MaxVersionLength`, `NormaliseVersion`, `ValidateVersion`.
- Production (MyVocaList, Domain, Infra, Services, Contracts): only migration history (`AddSongVersion`, `Add/Remove...` Designer and snapshot history files, the new `RemoveSongVersionAndFeaturedArtists` migration, which is the removal), app-release `Version` (`ReleaseEntry.Version`, `BuildInfo`, `VersionCheckService`, `WhatsNewService`, `FeedbackService`, AboutViewModel, About and sheet XAML/CS, `MauiProgram.cs`), and comments.
- Tests: migration tests that seed or assert the pre-removal schema (`RemoveSongVersionMigrationTests`, `SongMigrationTests`, `LyricsOriginMigrationTests`, `SongRepositoryTests` comment); Deezer JSON `title_version` in `DeezerProviderTests` (REQ-RMV-08); the new member-absence test; comments in the VM and tests.
- No real consumer left; no fix needed outside Wave 3.

### Build notes
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: **0 errors** (2 DX1000/DX1001 evaluation-licence warnings, pre-existing). Built after the VM edit, after the code-behind edit and after the page XAML edit (the page was edited in one pass, then built).
- `dotnet build Tests/MyVocaList.Tests.csproj`: **0 errors** (after the test edits).
- `dotnet test Tests/MyVocaList.Tests.csproj -v q --nologo`: **1849 total, 1849 passed, 0 failed, 0 skipped** (second run; the first run had 1 failure, `SongFormViewModelPictureTests.TitleOption_LocalReference_CarriesTheResolvedThumbnail`, caused by a positional `SongSuggestionDto` call whose empty version slot had not been removed; fixed, not a pre-existing failure).

### Spec gap / design notes
- Clear-title (X) behaviour: `ClearTitle` never touched the Version field in the shipped VM (it resets the pick identity, lyrics pick, title, pending image), so nothing needed removing there. Stated here because the briefing expected a reset.
- The exact-match rule "Save as new song is not offered" is enforced by the existing `IsExactDuplicate` binding in XAML, and by the T3 write-time refusal of `SaveAsNewSong` on an exact identity. The VM has no extra guard (no logic moved into the VM).
- No new spec gap found. No spec text change needed in this wave.

### E2E
E2E: emulator not run in this session (no Android emulator launched here). Manual verification required: open the Song form in add and edit mode, confirm there is no Version and no Featured artists field, and check the duplicate sheet on an exact match (Open existing, Cancel only) and on a near-duplicate (Save as new song present). Status is `Check build` per the implementor protocol until that check is done.
