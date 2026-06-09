# YouTube Search Launch Button — Requirements

> **Status:** Spec ready for implementation
> **Feature:** Quick-launch buttons open YouTube app (or browser fallback) with pre-filled karaoke search query

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Launch Button** | A call-to-action button that opens the YouTube app with a pre-filled search query |
| **Search Query** | The text sent to YouTube: `karaoke <song title> <artist name>`, URL-encoded |
| **YouTube App** | The native YouTube mobile application (YouTube app on Android or iOS) |
| **Browser Fallback** | When YouTube app is not installed, open the search query in the platform's default web browser |
| **URL Encoding** | RFC 3986 percent-encoding of special characters (spaces → `%20`, etc.) |
| **App Intent** | On Android, an implicit Intent to launch YouTube; on iOS, a deep link to YouTube search |

---

## Overview

MyVocaList adds quick-access launch buttons to three song-related pages, allowing admins to search YouTube for karaoke versions without leaving the app. Each button constructs a search query from the song title and artist name, opens the YouTube app if installed, and falls back to a web browser if the app is not available.

The feature is **read-only** — no data is stored, no API calls are made, and no UI state changes persist beyond the button tap. This differs from the YouTube Karaoke URL management feature, which stores URLs in the database.

---

## User Stories

### US-1: Launch YouTube Search from Song Form

**As an** admin editing a song
**I want to** tap a button in the song form that opens YouTube with a karaoke search for that song
**So that** I can quickly find and preview karaoke videos without switching apps manually

#### Acceptance Criteria

- AC-1.1: SongFormPage shall include a "Search YouTube" button in the YouTube URLs section, positioned above the "Search YouTube" navigation row (or adjacent to it).
- AC-1.2: The button text shall be "🔍 Search YouTube" with an icon or text-based indicator.
- AC-1.3: Tapping the button shall construct a search query: `karaoke <title> <artist>` where title is the current value of `SongTitle` and artist is the current value of `ArtistName`.
- AC-1.4: The search query shall be URL-encoded per RFC 3986 (spaces → `%20`, special chars → percent-encoded).
- AC-1.5: The button shall call `Launcher.TryOpenAsync(new Uri("https://youtu.be/...?search_query=..."))` to open the YouTube app.
- AC-1.6: If `Launcher.TryOpenAsync` returns `false` (YouTube app not installed), the app shall fall back to `Browser.OpenAsync(new Uri("https://youtu.be/...?search_query=..."))` to open the search in the browser.
- AC-1.7: If both launcher and browser fail silently, a snackbar shall show "Could not open YouTube" (no exception is raised).
- AC-1.8: The button shall be disabled (`IsEnabled=false`) if either `SongTitle` or `ArtistName` is empty or null.
- AC-1.9: Tapping the button does not modify the song form's state, validation, or UI — the form remains unchanged and unsaved.

---

### US-2: Launch YouTube Search from Songs List

**As an** admin browsing the Songs CRUD list
**I want to** tap a button in each list item that opens YouTube with a karaoke search for that song
**So that** I can search for videos from the list view without opening the form

#### Acceptance Criteria

- AC-2.1: Each song list item on SongsPage shall display a trailing action button (icon or menu icon).
- AC-2.2: The trailing button shall include a "Search YouTube" action (either as the single action or in a context menu / overflow menu).
- AC-2.3: Tapping "Search YouTube" shall construct the same search query as US-1: `karaoke <title> <artist>` (URL-encoded).
- AC-2.4: The button shall use `Launcher.TryOpenAsync` with fallback to `Browser.OpenAsync` (same pattern as US-1).
- AC-2.5: A snackbar shows "Could not open YouTube" only if both methods fail silently.
- AC-2.6: Tapping the button does not modify the list, selection, or app state.

---

### US-3: Launch YouTube Search from Song Picker

**As an** admin using the Song Picker (when adding a song to an entity like a queue)
**I want to** tap a button in a song picker result to search for that song on YouTube
**So that** I can preview videos before committing to adding the song to my queue

#### Acceptance Criteria

- AC-3.1: Each song result in SongPickerPage shall include a trailing action button or context menu.
- AC-3.2: The action shall be labeled "Search YouTube" and construct the query: `karaoke <title> <artist>` (URL-encoded).
- AC-3.3: Tapping the button shall use `Launcher.TryOpenAsync` with fallback to `Browser.OpenAsync` (same pattern as US-1 and US-2).
- AC-3.4: A snackbar shows "Could not open YouTube" only if both methods fail silently.
- AC-3.5: Tapping the button does not close the picker, navigate away, or modify the selection. The picker remains open so the admin can continue browsing or select a song.

---

## Out of Scope

- Customizing the search prefix (prefix is always `karaoke `, not configurable by user)
- Capturing results from YouTube (no deep-link integration, no URL import from YouTube back to the app)
- Searching without a song title or artist (button is disabled if either is missing)
- Storing search history or bookmarks
- Embedding YouTube results inside the app (browser/app launch only)
- Filtering or ranking results
- Modifying the song form or picker's primary functionality

---

## Validation Rules

### Search Query Construction

| Input | Output | Notes |
|-------|--------|-------|
| Title: "Despacito", Artist: "Luis Fonsi" | `karaoke%20Despacito%20Luis%20Fonsi` | spaces encoded as %20 |
| Title: "Bad Guy", Artist: "Billie Eilish" | `karaoke%20Bad%20Guy%20Billie%20Eilish` | all spaces encoded |
| Title: "Uptown Funk!", Artist: "Mark Ronson" | `karaoke%20Uptown%20Funk!%20Mark%20Ronson` | `!` encoded as `%21` |
| Title: "(Empty or Null)", Artist: "Artist Name" | Button disabled | button shall not be clickable |
| Title: "Title", Artist: "(Empty or Null)" | Button disabled | button shall not be clickable |

### URL Encoding

- Use RFC 3986 percent-encoding for all query parameters
- Space (`U+0020`) → `%20`
- Special characters (!, &, =, ?, #, etc.) → percent-encoded to their UTF-8 byte sequences
- MAUI's `Uri.EscapeDataString()` or equivalent shall be used for encoding

---

## Demo Statement

1. Open the app and navigate to SongFormPage (add or edit a song)
2. Fill in Artist Name and Song Title fields
3. Tap "🔍 Search YouTube" button in the YouTube URLs section
4. Verify: YouTube app (or browser) opens with search results for `karaoke <title> <artist>`
5. Return to the app and verify the form is unchanged

---

## Success Criteria

- Button is visible and clickable on all three pages (SongFormPage, SongsPage, SongPickerPage)
- Button launches YouTube correctly with the pre-filled search query
- URL encoding is RFC-compliant
- Fallback to browser works when YouTube app is not installed
- Button is disabled when title or artist is missing
- No form/list state is modified by tapping the button

---

## Acceptance Criteria Traceability

All acceptance criteria use Given-When-Then (implicit) or EARS format as specified in code-principles.md.
