# Artists & Songs — Requirements

> **Status:** Spec approved — implementation in progress (phases 1–7 complete)
> **Last updated:** 2026-04-12
> **Spec updated 2026-05-15:** Unified Artist model clarified: Artist serves dual roles (copyright
> owner + performer); Catalog join table introduced; Lyrics field added; navigation model revised.
> **Spec updated 2026-05-15b:** Role filtering added to US-1 (AC-1.16, AC-1.17); Artist Roles section
> updated to note list supports All/Authors/Performers filter.
> **Spec updated 2026-06-20:** Phase 2 reconciliation — FilterChipGroup (two chips) replaces top tab bar; single "Artists" menu entry replaces Authors/Performers split; AC-1.16 rewritten; Song.Version and 3-col unique index added to data model.

## Artist Roles

An Artist is any musical entity — a solo person or a band.

The Artist entity is **unified**: there is one Artist table and one set of fields. The role an
Artist plays is determined by usage context, not by a type column or flag.

- **Author role:** `Song.ArtistId` points to an Artist who is the copyright/original creator of
  that song. Every registered song must have exactly one Author Artist (`ArtistId int NOT NULL`).
- **Performer role:** An Artist who has `Catalog` entries performs songs live at events. The
  Catalog is the artist's performance repertoire and is entirely optional.
- **Both roles simultaneously:** An Artist can be both an Author (owns original songs) and a
  Performer (has a live catalog) at the same time — e.g., a band that wrote their own songs and
  performs them live.

The admin UI exposes Artists via a single "Artists" flyout menu entry. Role filtering is done on the page itself via filter chips (Authors / Performers). All Artist CRUD features (browse, register, edit, delete, Catalog management) are available from this single entry point.

---

## Overview

Artists and Songs form the music catalog of MyVocaList.

**Artist:** A musical act (solo performer, band, or ensemble) — e.g. Black Sabbath, Deep Purple,
Zeca Pagodinho. An artist may optionally build a **Catalog** of songs they perform. The Catalog is
the artist's performance repertoire — it is optional and may include songs originally created by
other artists (covers).

**Song:** A musical work with a mandatory original/copyright artist (`ArtistId` — e.g. "Paranoid"
→ Black Sabbath). Songs are registered at the global level and are not scoped to a single artist's
profile. A song may appear in multiple artists' Catalogs (covers).

**Catalog:** The many-to-many join between an Artist and the Songs they perform. Building a Catalog
is entirely optional — an artist can exist without any Catalog entries.

The catalog serves two purposes:

1. **Universal:** Every karaoke performance — regardless of mode (Bandokê or mechanical/YouTube) —
   can optionally reference a registered song. This enables singer/song history tracking for future
   analytics.
2. **Bandokê-specific enrichment (future):** Rich fields (key, BPM, chord chart) will be added in
   a later spec when the Bandokê queue flow is designed. This spec establishes the catalog
   foundation only.

Registration can be done manually or assisted by a free music metadata API (MusicBrainz primary,
Deezer fallback). The API is an enrichment tool — it populates the form, and the admin reviews and
saves.

**UI language:** "Artist(s)", "Song(s)", "Catalog" throughout all labels, titles, snackbars, and
empty states.
**Code identifiers:** `Artist`, `Song`, `Catalog`, `ArtistService`, `SongService`, `CatalogService`,
`IArtistRepository`, `ISongRepository`, `ICatalogRepository`.

---

## User Stories

### US-1: Browse and Search Artists

**As an** admin
**I want to** see all registered artists in a scrollable list and search by name
**So that** I can quickly find and manage a specific artist

#### Acceptance Criteria

- AC-1.1: When the Artists page opens, the app shall load the first page of artists (20 items) sorted by `Name` ascending.
- AC-1.2: The list shall show a shimmer skeleton while the first page is loading.
- AC-1.3: While the list is empty and no search is active, the app shall show a "No artist registered" empty state centered on screen.
- AC-1.4: When the user taps the search icon in the app bar, the `SmallAppBar` shall be replaced by the `SearchAppBar`.
- AC-1.5: While the `SearchAppBar` is active, the app shall debounce input by 400ms and reload the list on each change.
- AC-1.6: The search shall match against `Name` (case- and accent-insensitive via DB collation).
- AC-1.7: When a search returns no results, the app shall show a "No artist found" empty state.
- AC-1.8: When the user taps the back arrow in the `SearchAppBar`, the search shall be cleared and the `SmallAppBar` shall be restored.
- AC-1.9: When the user scrolls down, the app bar shall show an elevated state (surface tint).
- AC-1.10: When the list reaches the last item, the app shall automatically load the next page.
- AC-1.11: The user shall be able to pull-to-refresh to reload from the first page.
- AC-1.12: Each list row shall show the artist `Name` as headline and the Catalog size as supporting text (e.g. "12 songs in catalog" / "No catalog").
- AC-1.13: Each list row shall have a leading checkbox (MD3 multi-action rule — trailing slot occupied by catalog button, so checkbox moves left; person icon dropped) and a trailing catalog-navigation icon button.
- AC-1.14: Tapping a row shall toggle its selection state (selection is always on — no tap-to-navigate on the row itself).
- AC-1.15: Tapping the catalog-navigation icon button on an artist row shall navigate to that artist's Catalog page.
- AC-1.16: The page shall show a FilterChipGroup with two filter chips: "Authors" (artists with ≥1 song) and "Performers" (artists with ≥1 Catalog entry). When neither chip is selected, all artists are shown. When a chip is selected, only the matching role subset is shown.
- AC-1.17: When a role filter is active, the search and pagination shall apply within the filtered set.

---

### US-2: Register an Artist

**As an** admin
**I want to** register a new artist manually or by importing from the music API
**So that** the artist can be referenced in song registration and Catalog management

#### Acceptance Criteria

- AC-2.1: When the user taps the FAB on the Artists page, the app shall navigate to the New Artist form page.
- AC-2.2: The form shall show a `Name` field (required), an API search strip, a `Cancel` button, and a `Save` button.
- AC-2.3: When the user submits with an empty or whitespace-only name, the form shall show an inline error "Name is required" and shall not save.
- AC-2.4: When the user submits a name shorter than 2 characters, the form shall show "Name too short. Minimum 2 characters."
- AC-2.5: When the user submits a name longer than 200 characters, the form shall show "Name too long. Maximum 200 characters."
- AC-2.6: A character counter shall appear when the name length exceeds 180 characters, showing `current/200`. It turns warning color at 191+ and error color at 200.
- AC-2.7: When the name is valid and unique, the app shall save the artist, navigate back to the list, and show a success snackbar "{Name} registered successfully!".
- AC-2.8: Tapping `Cancel` shall navigate back without saving.

---

### US-3: Artist Name Suggestions (Duplicate Detection)

**As an** admin
**I want to** see existing artists with similar names while typing
**So that** I can avoid accidentally registering a duplicate

#### Acceptance Criteria

- AC-3.1: While the user types in the Name field (≥ 2 characters, 400ms debounce), the form shall show a suggestion list of up to 5 existing artists whose name matches the search term (case- and accent-insensitive).
- AC-3.2: Each suggestion row shall show the artist's `Name` and their Catalog size.
- AC-3.3: When the user taps a suggestion, the app shall navigate to the Edit Artist form pre-populated with that artist's data.
- AC-3.4: When the suggestion list is visible and the field is cleared below 2 chars, the suggestion list shall be hidden.
- AC-3.5: The suggestion list shall not block save — the admin can ignore all suggestions and proceed.
- AC-3.6: When no suggestions match, the suggestion list shall be hidden.

---

### US-4: API Enrichment — Artist

**As an** admin
**I want to** search the music catalog API and import artist data into the form
**So that** I can register artists quickly and accurately without typing everything manually

#### Acceptance Criteria

- AC-4.1: The form shall show an API search strip below the Name field, containing a search input (pre-filled with the current Name value) and a "Search" button.
- AC-4.2: When the user taps "Search", the app shall query MusicBrainz; if the result is empty or a network error occurs, it shall fall back to Deezer silently.
- AC-4.3: If both providers fail, the app shall show an inline message below the search strip: "Could not reach music catalog. Check your connection."
- AC-4.4: If no results are found, the app shall show: "No results found. You can register manually."
- AC-4.5: The app shall show up to 5 API results in a compact list below the search strip.
- AC-4.6: When the user taps an API result, the app shall populate the Name field with the artist name from the API and record `ExternalId` and `ExternalProvider` for saving.
- AC-4.7: After an API import, any subsequent manual edit to the Name field shall mark the record as `HasManualEdits = true` on save.

---

### US-5: Edit an Artist

**As an** admin
**I want to** edit an artist's name
**So that** their profile stays accurate over time

#### Acceptance Criteria

- AC-5.1: When exactly one artist is selected, the FloatingToolbar Edit button shall be active.
- AC-5.2: When the user taps Edit with exactly one artist selected, the app shall navigate to the Edit Artist form pre-populated with the current data.
- AC-5.3: The Edit Artist form shall apply the same name validation rules as AC-2.3–2.6.
- AC-5.4: On successful save, the app shall navigate back, reload the list, and show "{Name} updated successfully!".
- AC-5.5: On failure, the form shall show the error inline — no navigation.
- AC-5.6: The suggestion list (US-3) and API search strip (US-4) shall also be active on the edit form.
- AC-5.7: If the artist has `HasManualEdits = true` and the admin triggers an API import, a warning BottomSheet shall appear: "This artist has been manually edited. Importing will overwrite your changes." The admin must confirm before the import populates the fields.

---

### US-6: Delete Artists

**As an** admin
**I want to** delete one or more artists
**So that** incorrectly registered artists are removed

#### Acceptance Criteria

- AC-6.1: When one or more artists are selected, the FloatingToolbar Delete button shall be active.
- AC-6.2: When any selected artist owns one or more songs (`Song.ArtistId`), the app shall block deletion and show a snackbar: "Cannot delete — {Name} owns N song(s). Delete their songs first."
- AC-6.3: When no selected artist owns songs, the app shall show a confirmation BottomSheet: "Delete N artist(s)? Their Catalog entries will also be removed." (Songs are not deleted — only the Catalog links.)
- AC-6.4: The hardware Back button shall dismiss the confirmation sheet.
- AC-6.5: When deletion succeeds, the snackbar shall read "N artist(s) successfully removed!".
- AC-6.6: After deletion, the selection shall be cleared and the list shall reload.

---

### US-7: Select Artists (always-on selection)

**As an** admin
**I want to** select artists by tapping rows
**So that** I can batch-edit or batch-delete

#### Acceptance Criteria

- AC-7.1: Selection is always active (`SelectionMode.Multiple` hardcoded in XAML — no mode toggle).
- AC-7.2: Tapping a row shall toggle its selection state natively via `DXCollectionView`.
- AC-7.3: The app bar title shall show "Artists" when nothing is selected and "N selected" when N ≥ 1.
- AC-7.4: When a row is selected, tapping it again shall deselect it. Row taps never trigger navigation.
- AC-7.5: The FloatingToolbar `Select All` button shall select all loaded items when not all are selected, and deselect all when all are selected.

---

### US-8: Browse and Search Songs (global)

**As an** admin
**I want to** see all registered songs in a scrollable list and search by title
**So that** I can quickly find and manage any song regardless of which artist catalogued it

#### Acceptance Criteria

- AC-8.1: When the Songs page opens from the main menu, the app shall load the first page of all songs (20 items) sorted by `Title` ascending.
- AC-8.2: The `SmallAppBar` title shall show "Songs".
- AC-8.3: The list shall show a shimmer skeleton while loading.
- AC-8.4: While the list is empty and no search is active, the app shall show a "No song registered" empty state.
- AC-8.5: Search shall match against `Title` (case- and accent-insensitive via DB collation).
- AC-8.6: Each list row shall show `Title` as headline and the original artist name (if set) as supporting text.
- AC-8.7: All paging, pull-to-refresh, and load-more behaviors shall mirror the Artists page.

---

### US-9: Browse Songs in an Artist's Catalog

**As an** admin
**I want to** see which songs are in a specific artist's Catalog
**So that** I can manage that artist's performance repertoire

#### Acceptance Criteria

- AC-9.1: When the user taps the catalog-navigation icon button on an artist row, the app shall navigate to the Songs page filtered to that artist's Catalog.
- AC-9.2: The `SmallAppBar` title shall show the artist's name.
- AC-9.3: Each list row shall show the song `Title` as headline and `FeaturedArtists` as supporting text (if present).
- AC-9.4: An empty Catalog shall show a "No songs in catalog yet" empty state.
- AC-9.5: The admin can add existing songs to the Catalog via a FAB that opens a song picker.
- AC-9.6: The admin can remove a song from the Catalog (without deleting the song itself).

---

### US-10: Register a Song

**As an** admin
**I want to** register a new song independently
**So that** it is available for Catalog assignment and performance tracking

#### Acceptance Criteria

- AC-10.1: When the user taps the FAB on the Songs page (global mode), the app shall navigate to the New Song form page.
- AC-10.2: The form shall show an `Artist` autocomplete field (required), a `Title` field (required), a `Featured Artists` field (optional), a `Lyrics` field (optional), an API search strip, a `Cancel` button, and a `Save` button.
- AC-10.3: The `Artist` field shall be an autocomplete that searches registered artists by name (case- and accent-insensitive, ≥ 2 chars, 400ms debounce). The user must select an artist from the results.
- AC-10.4: When the user submits with no artist selected, the form shall show "Artist is required."
- AC-10.5: When the user submits with an empty or whitespace-only title, the form shall show "Title is required."
- AC-10.6: When the user submits a title shorter than 1 character, the form shall show "Title too short."
- AC-10.7: When the user submits a title longer than 200 characters, the form shall show "Title too long. Maximum 200 characters."
- AC-10.8: When a song with the same title already exists for the selected artist, the form shall show "This artist already has a song with this title."
- AC-10.9: When all fields are valid, the app shall save the song, navigate back to the Songs list, and show "{Title} registered successfully!".
- AC-10.10: A character counter shall appear on the Title field when length exceeds 180 characters.
- AC-10.11: The `Lyrics` field shall be a multi-line text editor. Its content is not validated beyond length (max 10 000 characters).

---

### US-11: API Enrichment — Song

**As an** admin
**I want to** search the music catalog API and import song data into the form
**So that** I can register songs quickly and accurately

#### Acceptance Criteria

- AC-11.1: The song form shall show an API search strip below the Title field.
- AC-11.2: When the user taps an API result, the app shall populate `Title`, `FeaturedArtists`, and `Artist` from the API data. The `Artist` field shall be matched to a registered artist by name (case-insensitive); if matched, the field shall be pre-filled and disabled (read-only). If not matched, the user may register the artist first and return.
- AC-11.2a: When the `Artist` field is pre-filled from an API result, it shall be locked (non-editable) to preserve the external attribution.
- AC-11.3: Error and fallback behavior shall mirror AC-4.3 and AC-4.4.
- AC-11.4: `HasManualEdits` tracking shall mirror AC-4.7.
- AC-11.5: If the song has `HasManualEdits = true` and the admin triggers an API import, a warning BottomSheet shall appear before overwriting (mirrors AC-5.7).

---

### US-12: Edit a Song

**As an** admin
**I want to** edit a song's title, featured artists, or lyrics
**So that** its information stays accurate

#### Acceptance Criteria

- AC-12.1: When exactly one song is selected on the Songs page, the FloatingToolbar Edit button shall be active.
- AC-12.2: When the user taps Edit, the app shall navigate to the Edit Song form pre-populated with the current data (including lyrics if present).
- AC-12.3: The Edit Song form shall apply the same validation rules as US-10.
- AC-12.4: On successful save, the app shall navigate back, reload the list, and show "{Title} updated successfully!".

---

### US-13: Delete Songs

**As an** admin
**I want to** delete one or more songs
**So that** incorrectly registered songs are removed

#### Acceptance Criteria

- AC-13.1: When one or more songs are selected, the FloatingToolbar Delete button shall be active.
- AC-13.2: The app shall show a confirmation BottomSheet: "Delete N song(s)? They will also be removed from all artist Catalogs."
- AC-13.3: When deletion succeeds, the snackbar shall read "N song(s) successfully removed!".
- AC-13.4: After deletion, the selection shall be cleared and the list shall reload.

---

### US-14: Song Lyrics (manual)

**As an** admin
**I want to** store and edit the lyrics for a song
**So that** lyrics are available for display during performance (future use)

#### Acceptance Criteria

- AC-14.1: The Song form (add and edit) shall include a multi-line `Lyrics` editor below `FeaturedArtists`.
- AC-14.2: The `Lyrics` field shall be optional.
- AC-14.3: Lyrics shall be stored as plain text (no formatting, no markup).
- AC-14.4: The maximum length of the Lyrics field is 10 000 characters.

---

### US-15 (Deferred): Lyrics API

**As an** admin
**I want to** fetch lyrics automatically from an external API
**So that** I don't have to paste them manually

> **Status: Deferred.** The `ILyricsProvider` interface is defined in this spec as a placeholder;
> no implementation is registered. The actual provider (Genius, Musixmatch, or other) will be
> decided in a separate spike task before implementation begins.

---

## Data Model

### Artist

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | |
| `Name` | `string` | NOT NULL, maxLen=250 (DB) / 200 (input) | Trimmed before save |
| `ExternalId` | `string?` | nullable, maxLen=100 | Provider's own ID (e.g. MusicBrainz MBID) |
| `ExternalProvider` | `string?` | nullable, maxLen=50 | `"MusicBrainz"`, `"Deezer"`, or null (manual) |
| `HasManualEdits` | `bool` | NOT NULL, default `false` | True if any field was manually changed after API import |

**Database indexes:**

| Index | Fields | Type |
|-------|--------|------|
| `IX_Artists_Name` | `Name` | Unique |
| `IX_Artists_ExternalId` | `ExternalId` | Unique, nullable |

### Song

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | |
| `Title` | `string` | NOT NULL, maxLen=250 (DB) / 200 (input) | Trimmed before save |
| `ArtistId` | `int` | **NOT NULL**, FK → `Artist.Id` | Original/copyright artist — mandatory. Never nullable. |
| `FeaturedArtists` | `string?` | nullable, maxLen=200 | Free text: "feat. Ivete Sangalo" |
| `Version` | `string?` | nullable | Version variant label (e.g. "live", "acoustic", "remix"). Added by Song Import. |
| `Lyrics` | `string?` | nullable, maxLen=10 000 | Plain text |
| `ExternalId` | `string?` | nullable, maxLen=100 | Provider's own ID |
| `ExternalProvider` | `string?` | nullable, maxLen=50 | `"MusicBrainz"`, `"Deezer"`, or null |
| `HasManualEdits` | `bool` | NOT NULL, default `false` | |

**Database indexes:**

| Index | Fields | Type | Notes |
|-------|--------|------|-------|
| `IX_Songs_ArtistId_Title_Version` | `ArtistId, Title, Version` | Composite unique | Same artist cannot have two songs with the same title and version (added by Song Import Wave 2.2) |
| `IX_Songs_ArtistId` | `ArtistId` | Standard | FK join performance |
| `IX_Songs_ExternalId` | `ExternalId` | Unique, nullable | |

### Catalog

Join table between Artist and Song representing the artist's performance repertoire.

| Field | Type | Constraints |
|-------|------|-------------|
| `ArtistId` | `int` | PK (composite), FK → `Artist.Id` CASCADE DELETE |
| `SongId` | `int` | PK (composite), FK → `Song.Id` CASCADE DELETE |

**Behavior:**
- Deleting an `Artist` removes all their `Catalog` entries. Songs are not deleted.
- Deleting a `Song` removes all `Catalog` entries for that song. Artists are not deleted.
- An artist may have an empty Catalog (no entries required).

---

## Validation Rules

### Artist

| Field | Rule | Error message |
|-------|------|---------------|
| Name | required | "Name is required" |
| Name | minLen = 2 | "Name too short. Minimum 2 characters." |
| Name | maxLen = 200 (input) | "Name too long. Maximum 200 characters." |
| Name | unique (excluding self on edit) | "An artist with this name is already registered." |

**Character counter thresholds (Name field):**

| Length | State |
|--------|-------|
| ≤ 180 | Hidden |
| 181–190 | Visible, neutral color |
| 191–199 | Visible, warning color |
| 200 | Visible, error color |

### Song

| Field | Rule | Error message |
|-------|------|---------------|
| Artist | required | "Artist is required" |
| Title | required | "Title is required" |
| Title | minLen = 1 | "Title too short." |
| Title | maxLen = 200 (input) | "Title too long. Maximum 200 characters." |
| Title | unique per artist (excluding self on edit) | "This artist already has a song with this title." |
| FeaturedArtists | optional; maxLen = 200 | "Featured artists text too long. Maximum 200 characters." |
| Lyrics | optional; maxLen = 10 000 | "Lyrics too long. Maximum 10 000 characters." |

---

## Out of Scope

- Bandokê-specific song fields (key, BPM, chord chart, arrangement notes) — future spec
- Song-to-performance link (`EventParticipation.SongId`) — future queue/event redesign spec
- Artist photo / image
- Lyrics display during performance — future spec
- Lyrics API implementation (ILyricsProvider is a placeholder only) — future spike
- Re-sync with API (refresh existing records from provider) — future spec
- Soft delete / archive
- Year of formation / genre / biography — outside karaoke queue management scope
- Cross-artist song deduplication — songs are uniquely identified by Title globally
- `ArtistMember` join table (linking Artist to Person records) — future spec
- Queue ↔ Catalog integration (singer picks from Performer's catalog when queued) — future spec
- YouTube / Mechanical mode integration (synced karaoke video as an alternative to live
  performance) — future spec
- AI-powered catalog import from file (TXT, XLS, PDF parsed by embedded AI agent) — future spec
- Author CRUD page (Authors are seeded for MVP dev/testing; full CRUD page deferred) — future spec

---

## Future Specs

These items are out of scope for this spec but have enough context to guide future spec writing.
Future sessions should use these stubs as a starting point rather than re-deriving decisions.

### ArtistMember

An Artist (solo or band) can be linked to one or more `Person` records via an `ArtistMember` join
table (`artistId`, `personId`). This allows a Performer who is also a registered singer to share
one identity record across both contexts. It also enables future peer-to-peer catalog sharing
between devices, keyed on Person identity rather than Artist name.

### Queue ↔ Catalog

When a singer is enqueued, the app will optionally filter available songs to the songs in the
active Performer's Catalog. The singer may still pick any registered song outside the Catalog
(flexibility for trial songs not yet added to the Performer's repertoire). The queue flow spec
will define how a "mode" (Bandokê / Mechanical) maps to a specific Performer.

### YouTube / Mechanical Mode

Songs can be performed via a YouTube karaoke video (lyrics synced in the video). The app needs an
integration to store a `YouTubeUrl` reference per song and project the video to an external
display. In this mode, no app-side `Lyrics` field is needed because the video includes them. The
existing `Lyrics` field on `Song` is for Bandokê (live instrumental) mode only.

### AI Catalog Import

A Performer can upload a file (TXT, XLS, DOC, or PDF) containing their song list. An embedded AI
agent parses the file, identifies Artist names and song titles, auto-creates any missing Author /
Song records, and adds them to the Performer's Catalog in batch. The admin reviews and confirms
before records are committed. This requires a file upload service, an AI parsing pipeline, and a
review/confirmation UI step.
