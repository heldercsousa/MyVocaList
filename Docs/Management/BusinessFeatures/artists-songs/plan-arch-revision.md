# Plan: Artists & Songs — Architectural Revision (v2)

> **Updated 2026-05-15** after clarifications:
> - `Song.ArtistId` = copyright/original artist FK (nullable — songs are independent)
> - `Catalog` (not ArtistSong) = join table for artist's performance repertoire (many-to-many, optional)
> - Songs page added to top-level menu
> - Tap on artist row = selection only; trailing button = navigate to Catalog
> - `TrailingContent` slot on existing ListItem is flexible enough (no new component property needed)
> - Migration: DELETE all rows before structural changes (app not in production)
> - SongFormPage has NO artist picker; ArtistId set via context or API only
> - Lyrics added to Song entity; ILyricsProvider is a placeholder interface only

## Context

The current spec treats Songs as entities subordinate to a single Artist (mandatory FK `ArtistId`).
This was approved before the "cover song" reality was understood: in karaoke, the same song title
frequently appears under many artists (covers), and songs should be manageable independently from
the artist catalog. Additionally, Songs is missing a top-level menu entry, lyrics storage, and
the navigation model on the Artists list is wrong (tapping navigated instead of selecting).

This plan defines the spec changes required and the resulting implementation phases.

---

## Changes Summary

| # | Area | Change |
|---|------|--------|
| 1 | Navigation fix | Tap = select only; trailing icon button = navigate to Songs |
| 2 | Data model | Song→Artist becomes many-to-many (`ArtistSong` join table) |
| 3 | Navigation | Songs added as top-level menu item (independent of Artists) |
| 4 | Song entity | Add `Lyrics TEXT` column (nullable) |
| 5 | Lyrics API | Placeholder `ILyricsProvider` interface; actual API as future spike |

---

## SDD Path

Per `workflow.md` Rule 1 — this change is **architectural** (schema change + cross-layer refactor
touching all 5 layers). Required artifacts: updated `requirements.md`, `design.md`, `tasks.md` +
Helder sign-off before implementation.

The existing spec at `Docs/specs/artists-songs/` will be revised in-place (not a new spec folder).
All three files receive a `> **Spec updated [date]:**` header block.

---

## Phase 0 — Spec Revision (main agent, no code)

Update `Docs/specs/artists-songs/requirements.md`:
- Revise song data model: remove `ArtistId` FK; add `Artists` (many-to-many via `ArtistSong` join)
- Add `Lyrics` optional field to Song
- Revise US-8: songs browse is top-level AND filterable by artist (two entry points)
- Revise US-9: song creation links one or more artists; artist is not mandatory at creation time
- Add US-13: Admin can browse all songs from the main menu (independent of artist)
- Add US-14: From artist list, admin taps a trailing button to see songs by that artist
- Add US-15: Admin can view/edit song lyrics manually in SongFormPage
- Add US-16 (deferred): Admin can fetch lyrics from an external API (interface only, no implementation)
- Update "Out of scope" accordingly

Update `Docs/specs/artists-songs/design.md`:
- New entity: `ArtistSong { ArtistId (FK), SongId (FK) }` join table (composite PK)
- Song entity: remove `ArtistId`; add `Lyrics TEXT?`
- New unique constraint: `(SongId, ArtistId)` on ArtistSong; remove `(ArtistId, TitleNormalized)` from Songs
- `ISongRepository`: add `GetPagedAsync(pageNumber, pageSize, query)` (all songs, no artist filter); keep `GetPagedByArtistAsync`; add `GetArtistsBySongAsync(songId)`; add `LinkArtistAsync`, `UnlinkArtistAsync`
- `ISongService`: matching changes; add `GetSongsForArtistAsync`, `GetArtistsForSongAsync`
- `SongListItemDto`: replace `ArtistId/ArtistName` with `IReadOnlyList<ArtistRefDto> Artists`
- New `ArtistRefDto(int Id, string Name)` for lightweight artist reference in song lists
- AppShell: add `songs` FlyoutItem route; register in AppShellViewModel menu groups (Catalog group alongside Artists)
- Routes.cs: add `Songs` top-level route (currently registered as a pushed route only)
- `SongsPage`/`SongsViewModel`: add optional `ArtistId`/`ArtistName` query params; when present = filtered mode (title shows artist name); when absent = all-songs mode (title = "Songs")
- `ArtistsPage` list item: TrailingContent becomes `HorizontalStackLayout` with a `DXButton` (icon-only, `arrow_forward_outlined`) + `CheckEdit` (existing checkbox). Tap on row = toggle selection. Button tap = `ViewSongsCommand(artist)` → navigate to Songs filtered by artist.
- `ArtistFormPage`/`SongFormPage`: SongFormPage gains a multi-artist picker (chips or secondary list)
- Add `ILyricsProvider` interface in Services layer (placeholder); no implementation registered in DI yet
- `SongFormPage`: add multi-line `Lyrics` editor field (optional, below FeaturedArtists)

Update `Docs/specs/artists-songs/tasks.md`:
- Mark Phase 1–7 as done (they remain complete for the original implementation)
- Add Phase 9: Spec revision (this phase)
- Add Phase 10: Domain refactor (Song entity + ArtistSong + Lyrics)
- Add Phase 11: Infra migration + repository updates
- Add Phase 12: Services refactor
- Add Phase 13: UI changes (AppShell, ArtistsPage trailing button, SongsPage dual-mode, SongFormPage lyrics + multi-artist)
- Add Phase 14: Tests update
- Mark Phase 8 (smoke test) as superseded — Phase 14 will be the new final gate

---

## Phase 10 — Domain Layer

Files to change:
- `MyVocaList.Domain/Entities/Song.cs` — remove `ArtistId`, remove `Artist` nav prop; add `Lyrics?`; add `Artists` nav (ICollection<Artist>)
- `MyVocaList.Domain/Entities/ArtistSong.cs` — new join entity `{ int ArtistId, int SongId, Artist Artist, Song Song }`
- `MyVocaList.Domain/Entities/Artist.cs` — `Songs` nav stays but through `ArtistSong`
- `MyVocaList.Domain/Interfaces/ISongRepository.cs` — per design.md changes above
- `MyVocaList.Domain/Interfaces/ISongService.cs` — per design.md changes above
- `MyVocaList.Contracts/DTOs/List/SongListItemDto.cs` — replace ArtistId/ArtistName with `IReadOnlyList<ArtistRefDto>`
- `MyVocaList.Contracts/DTOs/List/ArtistRefDto.cs` — new file

---

## Phase 11 — Infrastructure

Files to change:
- New migration `AddArtistSongJoinAndLyrics` (run `dotnet ef migrations add`)
  - Drop unique index on `(ArtistId, TitleNormalized)` from Songs
  - Drop `ArtistId` column from Songs
  - Add `Lyrics TEXT NULL` to Songs
  - Create `ArtistSong` table with composite PK `(ArtistId, SongId)`, FKs to both tables, cascade delete on both sides
- `MyVocaList.Infra/EntityEFConfig/SongConfiguration.cs` — remove ArtistId FK config; add Lyrics; add ArtistSong relationship
- `MyVocaList.Infra/EntityEFConfig/ArtistSongConfiguration.cs` — new file
- `MyVocaList.Infra/Repository/SongRepository.cs` — implement updated ISongRepository

---

## Phase 12 — Services

Files to change:
- `MyVocaList.Services/SongService.cs` — update to work with join table; `CreateSongAsync` accepts `IEnumerable<int> artistIds`; add `LinkArtistAsync`, `GetArtistsForSongAsync`
- `MyVocaList.Services/ILyricsProvider.cs` — new placeholder interface (`Task<string?> FetchLyricsAsync(string title, string artistName, CancellationToken ct)`)
- `MyVocaList.MauiProgram.cs` — no new registration for ILyricsProvider (deferred)

---

## Phase 13 — UI

Files to change:
- `MyVocaList/AppShell.xaml` — add `songs` FlyoutItem
- `MyVocaList/AppShell.xaml.cs` — register `Routes.Songs` as FlyoutItem route (remove from pushed route only)
- `MyVocaList/UI/ViewModels/AppShellViewModel.cs` — add Songs entry in Catalog menu group
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — update ListItem TrailingContent: add `DXButton` with `arrow_forward_outlined` icon beside checkbox
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — add `ViewSongsCommand(ArtistListItemDto artist)` → navigate to `Routes.Songs?artistId=…&artistName=…`; ensure tap = selection only (remove any existing tap-navigate logic)
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — no structural change; AppBar title binding already dynamic
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` — make `ArtistId` optional; when 0 = all-songs mode; `AppBarTitle` = ArtistName or "Songs"
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — add multi-line Lyrics editor; add multi-artist chip list
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — add `Lyrics` observable property; add `LinkedArtists` collection with add/remove

---

## Phase 14 — Tests

- Update `SongServiceTests` for new create/link pattern
- Update `SongRepositoryTests` for join table queries
- Add `ArtistSongRepositoryTests` for link/unlink operations
- Remove tests that depended on mandatory ArtistId

---

## Open Questions (resolve before Phase 10 dispatch)

1. **Lyrics API** (US-16): Which provider? Suggest deferring to a spike after Phase 14.
2. **Multi-artist in SongFormPage**: Should the artist picker be a BottomSheet search (same as API enrichment flow) or an inline chip list? Recommendation: chip list with inline search for UX simplicity.
3. **Existing data migration**: The current DB has Songs with `ArtistId`. The EF migration must backfill `ArtistSong` rows from existing `Songs.ArtistId` before dropping the column. This must be a raw SQL step in the migration `Up()`.

---

## Verification (after Phase 14)

1. `dotnet build` — 0 errors
2. `dotnet test` — 0 failures
3. Emulator smoke test:
   - Songs menu entry visible in flyout
   - Tapping artist row toggles selection, does not navigate
   - Trailing button on artist row navigates to songs filtered by that artist
   - Songs page opened from menu shows all songs
   - Adding a song allows linking multiple artists
   - Lyrics field visible and editable in SongFormPage
