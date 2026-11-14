# Tasks: a Deezer pick fills Lyrics

Spec: `requirements.md`, `design.md`. Log: `task-log.md`.
Branch `feature/deezer-lyrics` (worktree `.claude/worktrees/deezer-lyrics`). Tasks run in order in that
one worktree. DRY Onion order: Contracts/Domain → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec
- [x] L0 Research (Deezer API, LRCLIB API), code map, requirements, design, backlog item.

## Phase 1 — Services
- [ ] L1 **Duration through the suggestion DTOs.** (REQ-LYR-01)
  - Files owned: `Contracts/DTOs/MusicSearchResultDto.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`, `Services/DeezerProvider.cs`, `Services/SongSuggestionService.cs`, new tests `MyVocaList.Tests/Unit/Services/DeezerProviderDurationTests.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionServiceDurationTests.cs`.
  - Demo: a `search/track` row with `"duration": 215` yields a remote `SongSuggestionDto` with `DurationSeconds == 215`.
- [ ] L2 **Lyrics provider and service.** After L1. (REQ-LYR-02, 03, 05, 06, 07, 08, 15)
  - Files owned: new `Contracts/DTOs/LyricsQuery.cs`, `Services/ILyricsProvider.cs`, new `Services/LrclibLyricsProvider.cs`, new `Services/ISongLyricsService.cs`, new `Services/SongLyricsService.cs`, `Domain/Constants/SearchConstants.cs` (+ the lyrics limit constant), `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, new tests `LrclibLyricsProviderTests.cs`, `SongLyricsServiceTests.cs`, one added test in `Unit/DependencyInjection/AppServicesRegistrationTests.cs`.
  - Demo: `FindLyricsAsync(new LyricsQuery("Imagine", "John Lennon"))` against a stub returning LRCLIB JSON yields the plain lyrics; a 404 stub yields null.

## Phase 2 — UI
- [ ] L3 **`SongFormViewModel` + placeholder.** After L2. (REQ-LYR-01, 02, 04, 09, 10, 11, 12, 13, 14)
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (one attribute), new tests `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelLyricsTests.cs`, constructor argument in the three existing `SongFormViewModel*Tests.cs` files (planned exception, `design.md § 6`).
  - Demo: add mode, pick a Deezer row for a well-known song → Lyrics fills within 5 s; pick with lyrics already typed → typed text stays.

## Close-out
- [ ] L3c Merge to `develop`, full test run, Android build, traceability matrix, status.
- [ ] L4 **Device check (Helder)** — checklist in `task-log.md § Device check`.
