# Plan: YouTube Search Launch Button (3rd Karaoke URL Approach)

## Context
Songs in MyVocaList can have karaoke URLs added via (1) YouTubeSearchPage (requires API key) or (2) YouTube Share Intent (pending). A 3rd approach is needed: a "Search on YouTube" button that opens the YouTube app (if installed) or browser pre-filled with `karaoke <title> <artist>`. This removes friction for hosts who just want to find a video without the app requiring an API key or share flow. The button must appear in:
- `SongFormPage` (even when no URL is set)
- `SongsPage` CRUD list (per-song action)
- `SongPickerPage` (DB-only search results)

Queue management spec also needs updating: video playback via external YouTube launch must be moved out of "out-of-scope" and recognized as a supported MVP approach.

## Execution Plan

### BACKLOG.md update (orchestrator, before wave 1)
Add entry: `↳ YouTube Search Launch Button | 💡 Pending → 🟡 In Progress`

### Wave 1 — Parallel spec work (2 subagents)

**Agent A — Queue management spec update**
- Files: `Docs/Management/BusinessFeatures/queue-management/requirements.md`, `design.md`
- Task: Remove "Video playback integration or karaoke software bridging. MVP: Just tracks timing and song selection." from Out of Scope. Add a new story or note recognizing that the app supports launching YouTube externally to find/play karaoke videos via the song's stored URL or via search.

**Agent B — Song YouTube launch addendum spec + plan**
- Strategy: Do NOT modify existing youtube-karaoke spec (it's done/shipped). Create a new addendum spec at `Docs/Management/BusinessFeatures/artists-songs/youtube-search-launch/`.
- Files to create: `requirements.md`, `design.md`, `tasks.md`, `plan.md`
- Key spec content:
  - AC: button visible on SongFormPage regardless of URL state
  - AC: button visible per item in SongsPage list (contextual)
  - AC: button visible per result in SongPickerPage
  - AC: tapping opens YouTube app if installed (`vnd.youtube://results?search_query=karaoke+<title>+<artist>`), else browser (`https://www.youtube.com/results?search_query=karaoke+<title>+<artist>`)
  - AC: search query is URL-encoded
  - Technical: `Launcher.TryOpenAsync(Uri)` → bool; if false → `Browser.OpenAsync(Uri)`
  - Command lives in each ViewModel (it's a platform navigation action, not business logic)
  - No new service needed; no DB changes

### Wave 2 — Implementation (1 subagent, after Wave 1 complete)

**Agent C — Implement YouTube search launch button**
Files owned:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — add `OpenYouTubeSearchCommand`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — add button near YouTube URLs section
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` — add `OpenYouTubeSearchForSongCommand(SongListItemDto)`
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — add per-item trailing or swipe action
- `MyVocaList/UI/ViewModels/SongPickerViewModel.cs` — add `OpenYouTubeSearchCommand(MusicSearchResultDto)`
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` — add per-result action

Implementation pattern:
```csharp
[RelayCommand]
private async Task OpenYouTubeSearchAsync(string query)
{
    var encoded = Uri.EscapeDataString(query);
    var ytUri = new Uri($"vnd.youtube://results?search_query={encoded}");
    if (!await Launcher.TryOpenAsync(ytUri))
        await Browser.OpenAsync($"https://www.youtube.com/results?search_query={encoded}");
}
```
Query string: `$"karaoke {title} {artistName}"`

### Wave 3 — Code review (1 fresh subagent)
Review Agent C's output for correctness, MD3 compliance, DevExpress-first rule, SafeAreaEdges.

## Verification
- Build: `dotnet build` 0 errors
- Tests: `dotnet test` 0 failures (no new service logic = no new unit tests required; ViewModel command is UI action)
- Manual: tap button on SongFormPage, SongsPage item, SongPickerPage result → YouTube app or browser opens with correct search query
