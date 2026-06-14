# Song Import & Entity Resolution — Task Log

> One entry per task. See `tasks.md` for sequencing, `plan.md` for step detail.

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
