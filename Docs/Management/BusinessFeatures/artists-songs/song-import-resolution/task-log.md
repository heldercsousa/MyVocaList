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
