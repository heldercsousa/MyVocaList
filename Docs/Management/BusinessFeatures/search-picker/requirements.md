# Search Page Component — Requirements

**Feature:** Search Page Component  
**Status:** Spec  
**Date:** 2026-06-03  
**Spec author:** Helder Carvalho de Sousa  

---

## Context

Three non-MD3 inline search strips exist in the app (`ArtistFormPage` lines 63–107, `SongFormPage` lines 57–107, `SongFormPage` lines 172–275). MD3 has no inline form search pattern. The correct pattern is a standalone search destination page navigated to from the form.

---

## Domain Vocabulary

| Term | Definition |
|---|---|
| **Picker page** | A full-screen `ContentPage` whose sole purpose is to search an external data source and return one selected result to the caller. Not a CRUD page. |
| **Trigger row** | A tappable `ListItem` row inside a form page that navigates to a picker page. Replaces the non-MD3 inline search strip. |
| **Music database** | The external music metadata API queried via `IMusicMetadataService`. Not the local SQLite DB. |
| **Karaoke URL** | A YouTube video link stored against a song entity, used during a karaoke session. Represented by `SongKaraokeUrlDto`. |
| **WeakReferenceMessenger** | CommunityToolkit.Mvvm publish/subscribe bus used to return a selected result from a picker page to its caller ViewModel. |
| **Loading state** | The visual state of a picker page while an API call is in flight. Rendered via `dx:ShimmerView`. |
| **Empty state** | The visual state shown when a search returns zero results. Rendered via the existing `EmptyState` component. |

---

## User Stories

**US-1 — Artist name lookup**  
As a host adding or editing an artist, I want to search the music database for an artist so I can pre-fill the artist name field without typing it manually.

**US-2 — Song lookup**  
As a host adding or editing a song, I want to search the music database for a song so I can pre-fill the title and artist fields.

**US-3 — YouTube video search**  
As a host adding a karaoke video to a song, I want to search YouTube for a karaoke video and add it to the song's URL list.

**US-4 — Search loading feedback**  
As a user initiating any search, I want immediate visual feedback that the search is in progress so I know the app is working and I do not tap search again.

**US-5 — No results feedback**  
As a user whose search returned no results, I want a clear empty state message so I understand no match was found and can refine my query.

**US-6 — Cancel search**  
As a user who changed their mind, I want to navigate back from any search page without selecting a result, leaving the calling form unchanged.

---

## Acceptance Criteria

### ArtistPickerPage

**AC-ART-01** — Given the user taps the trigger row on ArtistFormPage, when the navigation completes, then the ArtistPickerPage is shown with the SearchAppBar visible.

**AC-ART-02** — Given the user has entered a non-empty query and tapped the search action, when the API call is in progress, then the `dx:ShimmerView` loading skeleton is visible and the search field remains editable.

**AC-ART-03** — Given the API call completes with at least one result, when the results list is rendered, then each result is a `ListItem` with the artist name as the headline (single line, no leading, no trailing).

**AC-ART-04** — Given results are displayed and the user taps a result, when `SelectResultCommand` executes, then the page pops and the calling ViewModel's `ArtistName` field is set to the selected artist's name.

**AC-ART-05** — Given the API call completes with zero results, when the results area is rendered, then the `EmptyState` component is shown with an appropriate message, and the loading skeleton is hidden.

**AC-ART-06** — Given the user is on ArtistPickerPage, when they tap the back button or use the back gesture, then the page pops and the calling ViewModel's `ArtistName` field is unchanged.

### SongPickerPage

**AC-SONG-01** — Given the user taps the trigger row on SongFormPage, when the navigation completes, then the SongPickerPage is shown with the SearchAppBar visible.

**AC-SONG-02** — Given the user has entered a non-empty query and tapped the search action, when the API call is in progress, then the `dx:ShimmerView` loading skeleton is visible and the search field remains editable.

**AC-SONG-03** — Given the API call completes with at least one result, when the results list is rendered, then each result is a `ListItem` with the song title as the headline and the artist name as the supporting text.

**AC-SONG-04** — Given results are displayed and the user taps a result, when `SelectResultCommand` executes, then the page pops and the calling ViewModel's `SongTitle` and `ArtistSearchText` fields are set to the selected song's title and artist name respectively.

**AC-SONG-05** — Given the API call completes with zero results, when the results area is rendered, then the `EmptyState` component is shown and the loading skeleton is hidden.

**AC-SONG-06** — Given the user is on SongPickerPage, when they navigate back without selecting, then the calling ViewModel's `SongTitle` and `ArtistSearchText` fields are unchanged.

### YouTubeSearchPage

**AC-YT-01** — Given `HasYouTubeApiKey` is true on SongFormPage, when the YouTube section is rendered, then the "Search YouTube" trigger row is visible.

**AC-YT-02** — Given `HasYouTubeApiKey` is false on SongFormPage, when the YouTube section is rendered, then the "Search YouTube" trigger row is hidden and the no-API-key nudge is shown (unchanged from current behaviour).

**AC-YT-03** — Given the user taps the "Search YouTube" trigger row, when the navigation completes, then the YouTubeSearchPage is shown with the SearchAppBar visible.

**AC-YT-04** — Given the user has entered a non-empty query and tapped the search action, when the API call is in progress, then the `dx:ShimmerView` loading skeleton is visible and the search field remains editable.

**AC-YT-05** — Given the API call completes with at least one result, when the results list is rendered, then each result is a `ListItem` with a 48×48 video thumbnail in the leading slot, the video title as the headline, and the channel name + formatted duration as the supporting text.

**AC-YT-06** — Given results are displayed and the user taps a result, when `SelectResultCommand` executes, then the page pops and the selected video is added to the song's `KaraokeUrls` list (same behaviour as the removed `AddFromSearchCommand`).

**AC-YT-07** — Given the API call completes with zero results, when the results area is rendered, then the `EmptyState` component is shown and the loading skeleton is hidden.

**AC-YT-08** — Given the user is on YouTubeSearchPage, when they navigate back without selecting, then the `KaraokeUrls` list on SongFormPage is unchanged.

### Loading UX (all three pages)

**AC-LOAD-01** — Given a search is submitted, when the command handler executes, then `IsLoading` is set to `true` synchronously (before any `await`) so the loading skeleton appears within one rendered frame.

**AC-LOAD-02** — Given a new search is submitted while `IsLoading` is true, when the new search starts, then the prior API call's `CancellationToken` is cancelled, the results collection is cleared, and the loading skeleton continues to show.

**AC-LOAD-03** — Given loading is active, when it ends (success, empty result, or error), then `IsLoading` is set to `false` in a `finally` block — no code path may leave `IsLoading = true` after the API call ends.

**AC-LOAD-04** — Given the API call throws an exception (network failure, HTTP error), when the exception is caught, then `IsLoading` is set to `false`, the `EmptyState` is shown with a generic error message ("Search failed. Please try again."), the exception is logged via `ILogger`, and the search field remains usable for a retry.

**AC-LOAD-05** — Given the page is first opened before any search has been submitted, when the page renders, then neither the loading skeleton nor the empty state is visible (both are hidden until after the first search).

### Form page triggers

**AC-TRIGGER-01** — Given ArtistFormPage is rendered, when the form is shown, then the existing API search strip Border (lines 63–107) is absent and a `ListItem` trigger row occupies its position, with a search icon leading, "Search music database" headline, and chevron-right trailing.

**AC-TRIGGER-02** — Given SongFormPage is rendered, when the music-DB form section is shown, then the existing API search strip Border (lines 57–107) is absent and a `ListItem` trigger row occupies its position.

**AC-TRIGGER-03** — Given SongFormPage is rendered and `HasYouTubeApiKey` is true, when the YouTube section is shown, then the "Search YouTube" `ListItem` trigger row is at the top of the YouTube Border, above the Paste URL section.

**AC-TRIGGER-04** — Given SongFormPage is rendered, the Paste URL section is present and functional regardless of `HasYouTubeApiKey` value.

---

## Validation Rules

- An empty query must not trigger a search (the search action button is disabled or a no-op when `SearchText` is empty or whitespace).
- A second search submission while one is in flight must cancel the prior request (`CancellationToken`).
- `IsLoading` must never be `true` when the page is idle or showing results/empty state.

---

## Out of Scope

- MD3 Card component for KaraokeUrls display (separate BACKLOG entry)
- Multiple video types per song with type labels and usage stats (separate BACKLOG entry)
- YouTube video preview / launch player from SongFormPage (separate BACKLOG entry)
- Shared `SearchPageBase` / `SearchContentView` abstraction — 3 separate concrete pages for now
- Pagination of search results
- Search history / recent searches
- Auto-search on text change (all searches require explicit submission via the search action button)
