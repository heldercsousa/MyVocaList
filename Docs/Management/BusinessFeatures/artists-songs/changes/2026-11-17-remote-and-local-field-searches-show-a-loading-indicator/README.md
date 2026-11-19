---
id: remote-and-local-field-searches-show-a-loading-indicator
title: Remote and local field searches show a loading indicator
status: 💡 Pending
target: 2026-11-17
section: BusinessFeatures
parent: artists-songs
goal: Song title and artist fields show that a search is running, so the user waits instead of leaving the field; the local artist search runs off the UI thread.
gate: Governed-component task for AutocompleteDXField; Helder approves.
kind: change
---

# Remote and local field searches show a loading indicator

Song title and artist fields show that a search is running, so the user waits instead of leaving the field; the local artist search runs off the UI thread.

## Background (2026-11-17)

Helder: "I miss a loading indicator, what makes me often blur the field before the response comes. User must be aware that something is loading."

Facts found (read-only survey, 2026-11-17):
- `AutocompleteDXField` (`UI/Components/TextFields/AutocompleteDXField.xaml.cs` ~359-409, governed) runs its own debounce, then `OptionSource.SearchAsync`. It sets `LoadingProgressMode="Manual"` and never sets `IsLoadingInProgress`, on purpose: the DevExpress loading mode opened the drop-down with "Not found".
- Song Title search (`SongFormViewModel.SearchTitleAsync`): local search then Deezer through `SongSuggestionService`; only offline/unavailable notes are shown (`TitleSupportingText`).
- Artist search (`SearchArtistsCoreAsync`): calls `ArtistService.SearchArtistsByNameAsync` without `Task.Run`, so the local SQLite search runs on the UI thread (Microsoft.Data.Sqlite runs async calls synchronously).
- Existing loading UIs to reuse: YouTube search (`YouTubeSearchViewModel.IsLoading` + `ShimmerView`), map picker (`IsSearching` + `ActivityIndicator`).

## Scope for the spec

A visible "searching" state in the field while a search runs (MD3: linear progress indicator or supporting text, check m3.material.io), in every consumer of `AutocompleteDXField`; the artist search moved off the UI thread (app loading pattern: service call off the UI thread, flag, results applied on the UI thread). Needs the four gates of a governed-component change. Best started in a fresh session (Helder).
