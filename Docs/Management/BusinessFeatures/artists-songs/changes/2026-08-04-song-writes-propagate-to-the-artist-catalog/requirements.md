# Requirements: song writes propagate to the artist catalog (CATALOG-PROP)

> **Status: APPROVED by Helder 2026-11-20** (after spec-reviewer, PASS WITH FIXES, fixes applied). Written from Helder's
> decisions D1–D4 (2026-08-24) and D5–D6 (2026-11-19), all recorded in this folder's `README.md`.

Parent (shipped, not edited): Artists & Songs Catalog `../../` (`requirements.md`, `design.md`).
Related shipped specs: UoW / READ-SCOPE (REQ-UOW-NN), Song form title-first
`../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/` (REQ-SFT-NN).

## Goal

Creating a song also records its author as a performer of that song in the artist catalog. Changing a
song's author moves that record to the new author. The user cannot remove it by hand. Songs saved
before this change get the record once, through a migration.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Author** | The artist in `Song.ArtistId`: exactly one per song, required (D1). |
| **Catalog row** | One `Catalog(ArtistId, SongId)` record: "this artist performs this song". The catalog is the performer relation only (D1). |
| **Derived row** | A catalog row whose `ArtistId` equals the song's `ArtistId`. It is **computed** from that equality, never stored as a flag (D4). |
| **Cover row** | A catalog row whose `ArtistId` differs from the song's `ArtistId`: the artist performs a song somebody else wrote. User-managed. |
| **Catalog mode** | The Songs page opened for one artist from the Artists list (`SongsViewModel.IsCatalogMode`): it lists that artist's catalog. |
| **Invariant** | For every song, its derived row exists (D1). |

## User stories

1. As an admin, I add a song and its author's catalog already lists it, so I never record it twice.
2. As an admin, I correct a song's author and the catalogs follow: the old author no longer lists it,
   the new one does.
3. As an admin, I remove a cover from an artist's catalog without deleting the song itself.
4. As an admin, I cannot break the catalog by removing an author's own song from it.

## Acceptance criteria

### Song writes

- **REQ-CATPROP-01: Creating a song writes its derived row.**
  WHEN a song is created through any `SongService` create path (`CreateSongAsync`,
  `CreateSongWithUrlsAsync`, `SaveFromFormAsync` in add mode), the system SHALL store exactly one
  catalog row `(Song.ArtistId, Song.Id)`, in the same transaction as the song. This also holds when the
  author is created in the same save (REQ-SFT new-artist path).
- **REQ-CATPROP-02: An update that keeps the author leaves the catalog unchanged.**
  WHEN a song is updated and its `ArtistId` does not change, the song's catalog rows SHALL be exactly the
  rows it had before the update.
- **REQ-CATPROP-03: Changing the author moves the derived row.**
  WHEN a song is updated and its author changes from A to B (an existing artist, or one created in the
  same save), then in the same transaction as the update:
  - the row `(A, song)` SHALL be removed;
  - the row `(B, song)` SHALL exist exactly once. If B already had a cover row for the song, that row
    becomes the derived row and no second row is written;
  - every other catalog row (other songs of A, other performers of the song) SHALL be unchanged.
- **REQ-CATPROP-04: A failed save changes no catalog row.**
  IF a song create or update does not complete (validation failure, `ArtistChoiceNeeded`, an exception,
  a rolled-back transaction), THEN the catalog SHALL hold exactly the rows it held before the call.
- **REQ-CATPROP-05: Deleting a song removes all its catalog rows.**
  WHEN songs are deleted (`DeleteSongsAsync`), every catalog row of those songs, derived and cover,
  SHALL be gone. (Existing DB cascade; no test covered it, so a new one pins it.)

### Manual catalog removal

- **REQ-CATPROP-06: Single remove refuses a derived row.**
  For `CatalogService.RemoveSongFromCatalogAsync(artistId, songId)`:
  - on a derived row, it SHALL return `(false, "<Artist> wrote this song, so it stays in their catalog.")`
    and the row SHALL remain;
  - on a cover row, it SHALL remove the row and return `(true, …)`;
  - on a row that does not exist, it SHALL return `(false, "Song is not in this catalog.")`. This
    replaces today's always-true.

  No UI calls this method once REQ-CATPROP-08 ships (the catalog page uses the bulk remove). It is kept
  as the single-row Services guard of D2, for the future catalog picker and any other caller.
- **REQ-CATPROP-07: Bulk remove removes covers and skips derived rows.**
  `CatalogService.RemoveSongsFromCatalogAsync(artistId, songIds)` SHALL remove every listed cover row of
  the artist, leave every listed derived row in place, and return the counts removed (r) and kept (k),
  all in one transaction. Song ids with no row for the artist count as neither. `success` is true
  exactly when r > 0. The message SHALL be, with `<A>` the artist's name:

  | Case | success | Message |
  |------|---------|---------|
  | Empty id list | false | `No songs selected.` |
  | Unknown artist | false | `Artist not found.` |
  | r = 0, k = 0 | false | `Nothing to remove from the catalog.` |
  | r = 0, k = 1 | false | `<A> wrote this song, so it stays in their catalog.` |
  | r = 0, k > 1 | false | `<A> wrote these songs, so they stay in their catalog.` |
  | r ≥ 1, k = 0 | true | `Removed <r> song(s) from the catalog.`, with `1 song` / `<r> songs` |
  | r ≥ 1, k = 1 | true | the line above + ` 1 kept: <A> wrote it.` |
  | r ≥ 1, k > 1 | true | the line above + ` <k> kept: <A> wrote them.` |

- **REQ-CATPROP-08: Delete in catalog mode removes catalog rows, never songs.**
  In catalog mode, both the selection-mode Delete and the item-actions Delete SHALL call the bulk remove
  of REQ-CATPROP-07, and SHALL NOT call `DeleteSongsAsync`.
  - The confirmation SHALL read `Remove '<title>' from <A>'s catalog?` (one song) or
    `Remove <n> songs from <A>'s catalog?` (several). n counts every selected song, including songs
    that will be kept.
  - After the call, the VM SHALL show the service message as a success snackbar when `success` is
    true, and as an error snackbar when it is false. It then SHALL reload the list. The selection is
    cleared on both paths (by the existing base-class delete flow).
  - Outside catalog mode, Delete deletes songs as today.
  - The UI does nothing else to protect derived rows: they stay selectable, and the service refusal
    plus the kept count are the whole of D2's "lock" in the UI.

### Existing data

- **REQ-CATPROP-09: Migration backfills derived rows once.**
  After the database is migrated to this version:
  - every song SHALL have its derived row;
  - a song whose author already had a manually added row SHALL still have exactly one row for that pair;
  - no other row is added, changed or removed;
  - applying the migration to a database that already satisfies the invariant SHALL insert nothing.

### Artist delete

- **REQ-CATPROP-10: Only owned songs block an artist delete; catalog rows never do.**
  For `ArtistService.DeleteArtistsAsync`:
  - IF any selected artist owns one or more songs (`Song.ArtistId`), THEN the delete SHALL be refused
    as shipped AC-6.2 states (`Cannot delete — {Name} owns N song(s). Delete their songs first.`), and
    nothing is deleted;
  - when several selected artists own songs, the message names the first of them in selection order;
  - otherwise the delete SHALL succeed even if the artists have catalog rows, and their cover rows
    SHALL be gone with them. No song is deleted.
  - The confirmation sheet SHALL read as parent AC-6.3: `Delete '<Name>'? Their catalog entries will
    also be removed.` (one artist) or `Delete <n> artists? Their catalog entries will also be removed.`
    (several). Today it reads `Delete '<Name>'?` / `Delete <n> artists?`, which is short of AC-6.3.

  Today the check counts catalog rows (`ICatalogRepository.CountByArtistAsync`). That differs from
  AC-6.2: an author with no catalog rows is deleted and their songs cascade away, while a cover-only
  artist is wrongly blocked. Once REQ-CATPROP-01/09 hold, every author has catalog rows, so the old
  check would also block correctly for the wrong reason. The check SHALL count owned songs.

### Consequence stated as expected (D4)

- **REQ-CATPROP-11: Authors count as performers.**
  An artist with at least one song and no cover rows SHALL appear under the Artists list `Performers`
  filter chip. Their catalog count SHALL include their own songs. This is the intended effect of the
  invariant, not a defect.

## Validation rules

| Rule | Where |
|------|-------|
| `(ArtistId, SongId)` is unique (existing composite PK); writes never insert a duplicate | `CatalogRepository`, migration (REQ-CATPROP-03, 09) |
| A derived row is never removed by a catalog operation, only by deleting the song or changing its author | `CatalogService` (REQ-CATPROP-06, 07) |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Song save fails after the catalog row was staged | Transaction rolls back, no catalog change (REQ-CATPROP-04) |
| New author already performs the song as a cover | That row becomes the derived row, no duplicate (REQ-CATPROP-03) |
| User removes their author's own song in catalog mode | Kept, snackbar explains (REQ-CATPROP-08) |
| Mixed selection of covers and own songs | The confirmation counts all of them; covers are removed, own songs kept, one snackbar with both counts (REQ-CATPROP-07/08) |
| Confirmation button reads "Delete" while the text says "Remove" | Accepted; the label lives in the governed base class (Out of scope) |
| An older backup is restored | Startup `MigrateAsync` applies the backfill to it (REQ-CATPROP-09) |

## Out of scope

- The catalog picker (`SongsViewModel.AddToCatalogAsync`, Phase 15 stub): adding covers by hand.
- The catalog as a grouped grid and catalogs filled from karaoke participations. These are queue work:
  `../../../queue-management/changes/2026-11-19-catalog-as-an-auto-filled-grouped-grid-filled-from-karaoke-participations/`.
  Nothing here rules out a second source of catalog rows.
- The rule that an artist referenced by queue events can never be deleted (Helder, 2026-11-19). It
  belongs to the queue work and will add a second refusal reason to REQ-CATPROP-10 then.
- Relabelling the confirmation sheet's action button (it reads "Delete", fixed in the governed
  `CrudListViewModelBase`). If wanted, it is a separate governed-component task.
- A provenance column on `Catalog` (D4: not needed).

## Supersedes (shipped specs are not edited)

| Shipped behaviour | Change |
|-------------------|--------|
| Parent AC-6.2 / AC-6.3 (artist who owns songs is blocked; otherwise a confirmation names the catalog entries that go too) | **Kept as the rule.** The code does not match them today: it checks catalog rows and its texts differ. REQ-CATPROP-10 brings the check and both texts in line |
| Catalog-mode Delete deletes the songs (`SongsViewModel.ExecuteDeleteAsync`), against parent AC-9.6 | **Replaced** by REQ-CATPROP-08, which implements AC-9.6 for cover rows |
| `RemoveSongFromCatalogAsync` always returns success | **Replaced** by REQ-CATPROP-06 |
