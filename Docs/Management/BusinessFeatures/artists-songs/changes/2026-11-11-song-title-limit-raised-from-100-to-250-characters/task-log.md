# Task-log — Song title limit raised from 100 to 250 characters

---
## Task: tasks 2-5 (tests, service, EF config + migration, verification)
**Status:** To Review
**Started:** 2026-11-11
**Completed:** 2026-11-11

### Changed files:
- `Services/SongService.cs` (`MaxTitleLength => 250`)
- `Infra/EntityEFConfig/SongConfiguration.cs` (`Song.Title` `HasMaxLength(250)`)
- `Infra/Migrations/20261111144142_SongTitleMaxLength250.cs` (new; empty Up/Down, metadata only)
- `Infra/Migrations/20261111144142_SongTitleMaxLength250.Designer.cs` (new)
- `Infra/Migrations/AppDbContextModelSnapshot.cs` (only `Song.Title` 100 -> 250)
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
- `MyVocaList.Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs`
- `MyVocaList.Tests/Integration/Repositories/SongRepositorySearchTests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-song-title-limit-raised-from-100-to-250-characters/tasks.md`, `task-log.md`

### Migration inspection
`SongTitleMaxLength250` has empty `Up`/`Down` (SQLite does not enforce TEXT length). No table rebuild, no DROP/CREATE,
FTS5 trigram index and triggers untouched. Snapshot diff: one line, `Song.Title` `HasMaxLength(100)` -> `250`.

### AC traceability matrix
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| AC-STL-01 | 250 saves, 251 rejected with 250 message | `SongService.MaxTitleLength` | `SongServiceTests.ValidateTitleInput_MaxLength250_ReturnsTrue`, `ValidateTitleInput_251Chars_ReturnsFalseWith250Message`, `CreateSongAsync_TitleTooLong_ReturnsFalse`; `SongFormViewModelTests` (message strings); `SongServiceNewArtistUnitOfWorkTests` (251 injection) |
| AC-STL-02 | counter 250/250 no error, 251 error | `GetCharacterCounterInfo` uses `MaxTitleLength` | `GetCharacterCounterInfo_AtMaxLength250_IsNotError`, `GetCharacterCounterInfo_OverMaxLength_IsError` |
| AC-STL-03 | snapshot `HasMaxLength(250)`, migration applies | `SongConfiguration`, migration | snapshot diff; `MigrateAsync` in `SongRepositorySearchTests` applies all migrations |
| AC-STL-04 | 250-char title found by exact and trigram search | unchanged repo code | `SongRepositorySearchTests.LongTitle250Chars_FoundByExactAndTrigramSearch` (passes before and after; SQLite does not enforce length, it guards REQ-STL-04) |

Build: solution build 0 errors (8 projects, 199 warnings); MyVocaList.Tests 959 passed, 0 failed. A standalone `-f net10.0-android` project build errored with XAWAS7024 (Android task) plus NuGet audit errors on System.Security.Cryptography.Pkcs 10.0.10 — environmental, unrelated to this change; not investigated.

Red confirmed before the service change: 3 failures with "Maximum 100" / "250/100" / valid-250 rejected.

### Not done
Helder device check (250 saves, 251 errors) — not claimed.
