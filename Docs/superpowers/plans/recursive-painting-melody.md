# Plan: Artists & Songs Spec Revision + Build Fix

## Context

During implementation of phases 10–11, the domain concepts were partially revised but the spec was
left with two errors and several gaps:

1. `requirements.md` header incorrectly says "Song.ArtistId made nullable" — it is and must remain
   `int NOT NULL` (every song has a copyright owner).
2. The concept of "Artist" was being split into Author + Performer, creating confusion. After
   discussion the correct model is: **one unified `Artist` entity** that can serve two roles
   (copyright owner of a song AND/OR live performer with a catalog). The menu exposes both roles as
   separate entries ("Authors", "Performers") pointing to the same CRUD page with a mode flag.

Separately, phases 12–15 were not executed, leaving the build broken with ~17 errors spread across
`SongService.cs`, `SongsViewModel.cs`, `SongFormViewModel.cs`, and the test files.

This plan covers two sequential deliverables:
A) Spec rewrite (requirements.md + design.md only — no code)
B) Build fix (phases 12–15 of tasks.md)

---

## Part A — Spec Rewrite (requirements.md + design.md)

### Key decisions to capture

| Topic | Decision |
|-------|----------|
| Entity name | `Artist` — unified, not split. Replaces both "Author" and "Performer" |
| `Song.ArtistId` | `int NOT NULL` — mandatory copyright owner |
| Artist as Author | When `Song.ArtistId` points to an Artist → that Artist is in the Author role |
| Artist as Performer | When an Artist has `Catalog` entries → that Artist is in the Performer role |
| Artist as both | Possible. All UI features appear regardless of menu entry used |
| Menu | Two entries: "Authors" (read/lookup artists with songs) + "Performers" (full CRUD with Catalog). Both navigate to the same `ArtistsPage` with a `mode` parameter |
| Artist deletion | Blocked if Artist owns songs (`Song.ArtistId`). Catalog entries cascade. |
| `ArtistMember(artistId, personId)` | Architecture documented as future. NOT implemented in MVP |
| Author CRUD in MVP | Minimal — seeded with ~10 sample authors via migration for dev/test. Full CRUD page deferred |
| Queue ↔ Catalog link | Documented as future spec (singer picks from Performer's catalog) |
| YouTube / Mechanical | Documented as future spec (YouTube integration for synced lyrics + video) |
| AI catalog import | Documented as future spec (AI agent parses TXT/XLS/PDF catalog files) |
| Lyrics field | Optional. Serves organic/Bandokê performances — projected on screen during live performance |

### Files to update

- `Docs/specs/artists-songs/requirements.md`
  - Fix header note (remove "ArtistId nullable" statement)
  - Add §Artist Roles section explaining Author vs Performer role on one entity
  - Update data model table to reflect `ArtistId int NOT NULL`
  - Update US-8 (Songs) and US-9 (Catalog) to reflect unified Artist
  - Add out-of-scope entries: ArtistMember, YouTube integration, AI catalog import, queue-catalog link
  - Add future-spec stubs for those three items
  - Correct Artist deletion AC (AC-6.x) — currently references "owns songs" correctly; verify

- `Docs/specs/artists-songs/design.md`
  - Remove the "Author" / "Performer" split framing from any section that implies two entity types
  - Clarify Artist entity roles in a §Roles section
  - Add note: `ArtistMember` join table (future — links Artist to Person)
  - Update navigation: menu entries "Authors" and "Performers" both use `ArtistsPage?mode=author` / `ArtistsPage?mode=performer`
  - Add §Future Architecture section documenting deferred items with enough detail to avoid re-deriving

### NOT changing in this pass

- `tasks.md` — phases 10–16 remain as-is; spec rewrite does not alter task sequence
- No code files touched in Part A

---

## Part B — Build Fix (phases 12–15)

Current broken files and required fixes (root cause: phases 12–15 not executed):

### 12.1 — SongService.cs
- `CreateSongAsync`: add `lyrics`, `externalId`, `externalProvider` params; assign to entity
- `UpdateSongAsync`: add `lyrics`, `hasManualEdits` params; assign to entity
- Add `ExistsByTitleForArtistAsync(string title, int artistId, int? excludeId, CT)`: normalize title, delegate to existing repository overloads
- `GetPagedSongsForListAsync`: remove `artistId` param; call `_songRepository.GetPagedAsync` (already exists)

### 12.4 — ArtistService.cs (line 112)
- `DeleteArtistsAsync`: replace `_songRepository.CountByArtistAsync` with `_catalogRepository.CountByArtistAsync`
  - Requires injecting `ICatalogRepository` into ArtistService constructor

### 14.4 — SongsViewModel.cs (lines 125, 172)
- Remove `ArtistId` from both `GetPagedSongsForListAsync` calls (5 args → 4 args)

### 14.8 — SongFormViewModel.cs (line 77)
- `UpdateSongAsync`: add `null` (lyrics) and `true` (hasManualEdits) to call site

### 15.2 — SongRepositoryTests.cs
- Replace all `GetPagedByArtistAsync` calls with `GetPagedAsync` (global); remove artist-filter assertions
- Remove `SearchByTitleAsync` tests (method removed from ISongRepository)
- Remove `CountByArtistAsync` / `CountByArtistsAsync` tests (moved to ICatalogRepository)

### 15.x — ArtistServiceTests.cs (lines 161, 178)
- Update mock setup: replace `_songRepoMock.Setup(r => r.CountByArtistAsync(...))` with `_catalogRepoMock.Setup(r => r.CountByArtistAsync(...))`
- Requires adding `Mock<ICatalogRepository> _catalogRepoMock` and injecting into `ArtistService`

### Execution order
1. Part A first (spec correct before code changes)
2. Part B as a single subagent task (all 5 files, one commit)
3. `dotnet build` → 0 errors
4. `dotnet test` → all pass
5. Check off tasks 12.1, 12.4, 14.4, 14.8, 15.2 in tasks.md

---

## Verification

- `dotnet build` → 0 errors
- `dotnet test` → all tests green (no regressions)
- `requirements.md` has no mention of "nullable ArtistId"
- `design.md` has §Artist Roles and §Future Architecture sections
