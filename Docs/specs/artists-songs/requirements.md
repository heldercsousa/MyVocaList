# Artists & Songs — Requirements

> **Status:** Spec approved — pending implementation
> **Last updated:** 2026-04-12

## Overview

Artists and Songs form the music catalog of MyVocaList. An artist is a musical act (solo performer, band, or ensemble) — e.g. Black Sabbath, Deep Purple, Zeca Pagodinho. Each artist owns a list of songs. A song belongs to one primary artist and optionally credits additional artists via free text.

The catalog serves two purposes:

1. **Universal:** Every karaoke performance — regardless of mode (Bandokê or mechanical/YouTube) — can optionally reference a registered song. This enables singer/song history tracking for future analytics.
2. **Bandokê-specific enrichment (future):** Rich fields (key, BPM, lyrics, chord chart) will be added in a later spec when the Bandokê queue flow is designed. This spec establishes the catalog foundation only.

Registration can be done manually or assisted by a free music metadata API (MusicBrainz primary, Deezer fallback). The API is an enrichment tool — it populates the form, and the admin reviews and saves.

**UI language:** "Artist(s)" and "Song(s)" throughout all labels, titles, snackbars, and empty states.
**Code identifiers:** `Artist`, `Song`, `ArtistService`, `SongService`, `IArtistRepository`, `ISongRepository`.

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
- AC-1.12: Each list row shall show the artist `Name` as headline and the song count as supporting text (e.g. "12 songs").
- AC-1.13: Each list row shall have a leading music icon and a trailing checkbox that reflects selection state.
- AC-1.14: When the user taps an artist row (with nothing selected), the app shall navigate to that artist's Songs page.

---

### US-2: Register an Artist

**As an** admin
**I want to** register a new artist manually or by importing from the music API
**So that** songs can be linked to that artist

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
- AC-3.2: Each suggestion row shall show the artist's `Name` and their song count.
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
- AC-6.2: When the user taps Delete and the selected artist(s) have no songs, the app shall show a confirmation BottomSheet: "Delete N artist(s)?"
- AC-6.3: When the user taps Delete and any selected artist has songs, the confirmation message shall read: "Delete N artist(s)? This will also delete all associated songs."
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
- AC-7.4: When a row is selected, tapping it again shall deselect it. Navigation to the Songs page only happens via a tap when `SelectedCount` is already zero before the tap.
- AC-7.5: The FloatingToolbar `Select All` button shall select all loaded items when not all are selected, and deselect all when all are selected.

---

### US-8: Browse and Search Songs (scoped to artist)

**As an** admin
**I want to** see all songs for a given artist and search by title
**So that** I can quickly find and manage a specific song

#### Acceptance Criteria

- AC-8.1: When the Songs page opens (navigated from an artist row tap), the app shall load the first page of songs for that artist (20 items) sorted by `Title` ascending.
- AC-8.2: The `SmallAppBar` title shall show the artist's name.
- AC-8.3: The list shall show a shimmer skeleton while loading.
- AC-8.4: While the list is empty and no search is active, the app shall show a "No song registered" empty state.
- AC-8.5: Search shall match against `Title` (case- and accent-insensitive via DB collation).
- AC-8.6: Each list row shall show `Title` as headline and `FeaturedArtists` as supporting text (if present).
- AC-8.7: Tapping a song row shall navigate to the Edit Song form for that song.
- AC-8.8: All paging, pull-to-refresh, and load-more behaviors shall mirror the Artists page.

---

### US-9: Register a Song

**As an** admin
**I want to** register a new song under an artist, manually or via API
**So that** the song is available for performance tracking

#### Acceptance Criteria

- AC-9.1: When the user taps the FAB on the Songs page, the app shall navigate to the New Song form page with the artist pre-set and displayed as a read-only label.
- AC-9.2: The form shall show a `Title` field (required), a `Featured Artists` field (optional), an API search strip, a `Cancel` button, and a `Save` button.
- AC-9.3: When the user submits with an empty or whitespace-only title, the form shall show "Title is required."
- AC-9.4: When the user submits a title shorter than 1 character, the form shall show "Title too short."
- AC-9.5: When the user submits a title longer than 200 characters, the form shall show "Title too long. Maximum 200 characters."
- AC-9.6: When a song with the same title already exists for this artist, the form shall show "This artist already has a song with this title."
- AC-9.7: When all fields are valid, the app shall save the song, navigate back to the Songs list, and show "{Title} registered successfully!".
- AC-9.8: A character counter shall appear on the Title field when length exceeds 180 characters.

---

### US-10: API Enrichment — Song

**As an** admin
**I want to** search the music catalog API and import song data into the form
**So that** I can register songs quickly and accurately

#### Acceptance Criteria

- AC-10.1: The song form shall show an API search strip below the Title field.
- AC-10.2: The API search shall use the current artist name as a hint to scope results (MusicBrainz supports artist-scoped track queries).
- AC-10.3: When the user taps an API result, the app shall populate `Title` and `FeaturedArtists` from the API data.
- AC-10.4: Error and fallback behavior shall mirror AC-4.3 and AC-4.4.
- AC-10.5: `HasManualEdits` tracking shall mirror AC-4.7.
- AC-10.6: If the song has `HasManualEdits = true` and the admin triggers an API import, a warning BottomSheet shall appear before overwriting (mirrors AC-5.7).

---

### US-11: Edit a Song

**As an** admin
**I want to** edit a song's title or featured artists
**So that** its information stays accurate

#### Acceptance Criteria

- AC-11.1: When exactly one song is selected on the Songs page, the FloatingToolbar Edit button shall be active.
- AC-11.2: When the user taps Edit, the app shall navigate to the Edit Song form pre-populated with the current data.
- AC-11.3: The Edit Song form shall apply the same validation rules as US-9.
- AC-11.4: On successful save, the app shall navigate back, reload the list, and show "{Title} updated successfully!".

---

### US-12: Delete Songs

**As an** admin
**I want to** delete one or more songs
**So that** incorrectly registered songs are removed

#### Acceptance Criteria

- AC-12.1: When one or more songs are selected, the FloatingToolbar Delete button shall be active.
- AC-12.2: The app shall show a confirmation BottomSheet: "Delete N song(s)?"
- AC-12.3: When deletion succeeds, the snackbar shall read "N song(s) successfully removed!".
- AC-12.4: After deletion, the selection shall be cleared and the list shall reload.

---

## Data Model

### Artist

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | |
| `Name` | `string` | NOT NULL, maxLen=250 (DB) / 200 (input) | Trimmed before save |
| `ExternalId` | `string?` | nullable, maxLen=100 | Provider's own ID (e.g. MusicBrainz MBID) |
| `ExternalProvider` | `string?` | nullable, maxLen=50 | `"MusicBrainz"`, `"Deezer"`, or null (manual) |
| `HasManualEdits` | `bool` | NOT NULL, default `false` | True if any field was manually changed after an API import |

**Database indexes:**

| Index | Fields | Type | Notes |
|-------|--------|------|-------|
| `IX_Artists_Name` | `Name` | Unique | No two artists with the same name |
| `IX_Artists_ExternalId` | `ExternalId` | Unique, nullable | Multiple NULLs allowed (SQLite behavior) |

### Song

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | |
| `Title` | `string` | NOT NULL, maxLen=250 (DB) / 200 (input) | Trimmed before save |
| `ArtistId` | `int` | FK → `Artist.Id`, NOT NULL | Primary artist |
| `FeaturedArtists` | `string?` | nullable, maxLen=200 | Free text: "feat. Ivete Sangalo" |
| `ExternalId` | `string?` | nullable, maxLen=100 | Provider's own ID |
| `ExternalProvider` | `string?` | nullable, maxLen=50 | `"MusicBrainz"`, `"Deezer"`, or null |
| `HasManualEdits` | `bool` | NOT NULL, default `false` | |

**Database indexes:**

| Index | Fields | Type | Notes |
|-------|--------|------|-------|
| `IX_Songs_ArtistId` | `ArtistId` | Standard | FK join performance |
| `IX_Songs_ArtistId_Title` | `ArtistId, Title` | Composite unique | Same artist cannot have two songs with the same title |
| `IX_Songs_ExternalId` | `ExternalId` | Unique, nullable | |

**Note on collation:** No normalized columns. All case- and accent-insensitive searches use `EF.Functions.Like` + `EF.Functions.Collate` on both operands, relying on the `CollationInterceptor` applied globally. This is portable to MSSQL (remove explicit `Collate` calls; DB-level collation cascades). `Person.FullNameNormalized` is acknowledged technical debt — to be removed in a future cleanup task.

**Cascade behavior:** Deleting an `Artist` cascades to all their `Song` records. The service surfaces the song count in the confirmation message before the admin confirms.

**Future link:** `EventParticipation` will gain a nullable `SongId` FK in the queue/event redesign spec. The data model is designed to receive it with no structural changes to `Artist` or `Song`.

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
| Title | required | "Title is required" |
| Title | minLen = 1 | "Title too short." |
| Title | maxLen = 200 (input) | "Title too long. Maximum 200 characters." |
| Title | unique per artist (excluding self on edit) | "This artist already has a song with this title." |
| FeaturedArtists | optional; maxLen = 200 | "Featured artists text too long. Maximum 200 characters." |

---

## Out of Scope

- Bandokê-specific song fields (key, BPM, lyrics, chord chart, arrangement notes) — future spec
- Song-to-performance link (`EventParticipation.SongId`) — future queue/event redesign spec
- Artist photo / image
- Multiple primary artists per song — primary artist FK + `FeaturedArtists` free text covers v1
- Song catalog browsing across all artists (cross-artist search) — Songs page is always scoped to one artist in v1
- Lyrics display during performance — future spec
- Re-sync with API (refresh existing records from provider) — future spec
- Soft delete / archive
- Year of formation / genre / biography — outside karaoke queue management scope
