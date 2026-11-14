# Tasks: a Deezer pick fills Lyrics

Spec: `requirements.md`, `design.md`. Log: `task-log.md`.
Branch `feature/deezer-lyrics` (worktree `.claude/worktrees/deezer-lyrics`). Tasks run in order in that
one worktree. DRY Onion order: Contracts/Domain → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec
- [x] L0 Research (Deezer API, LRCLIB API), code map, requirements, design, backlog item.

## Phase 1 — Services
- [x] L1 **Duration through the suggestion DTOs.** (REQ-LYR-01) Size note: six files, two of them one-parameter record changes.
  - Files owned: `Contracts/DTOs/MusicSearchResultDto.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`, `Services/DeezerProvider.cs`, `Services/SongSuggestionService.cs`, new tests `MyVocaList.Tests/Unit/Services/DeezerProviderDurationTests.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionServiceDurationTests.cs`.
  - Demo: a `search/track` row with `"duration": 215` yields a remote `SongSuggestionDto` with `DurationSeconds == 215`.
- [~] L2a **LRCLIB provider.** After L1. (REQ-LYR-03, 15)
  - Files owned: new `Contracts/DTOs/LyricsQuery.cs`, `Services/ILyricsProvider.cs`, new `Services/LrclibLyricsProvider.cs`, new test `MyVocaList.Tests/Unit/Services/LrclibLyricsProviderTests.cs`.
  - Demo: a stub returning LRCLIB JSON yields the plain lyrics; a 404 stub yields null; a 500 stub throws.
- [ ] L2b **Lyrics service and DI.** After L2a. (REQ-LYR-02, 03, 05, 06, 07, 08, 15)
  - Files owned: new `Services/ISongLyricsService.cs`, new `Services/SongLyricsService.cs`, `Domain/Constants/SearchConstants.cs` (two constants), `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, new test `MyVocaList.Tests/Unit/Services/SongLyricsServiceTests.cs`, one added test in `Unit/DependencyInjection/AppServicesRegistrationTests.cs`.
  - Demo: `FindLyricsAsync(new LyricsQuery("Imagine", "John Lennon"))` with a provider stub yields the trimmed lyrics; offline yields null with no provider call.

## Phase 2 — UI
- [ ] L3 **`SongFormViewModel` + placeholder.** After L2b. (REQ-LYR-01, 02, 04, 09, 10, 11, 12, 13, 14) Size note: two production files; four existing test files change by one constructor argument each.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (one attribute), new tests `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelLyricsTests.cs`, constructor argument in `SongFormViewModelTests.cs`, `SongFormViewModelImageTests.cs`, `SongFormViewModelPictureTests.cs` and `Integration/Services/SongSuggestionTitleSearchRealStackTests.cs` (planned exception, `design.md § 6`).
  - Demo: add mode, pick a Deezer row for a well-known song → Lyrics fills within 5 s; pick with lyrics already typed → typed text stays.

## Close-out
- [ ] L3c Merge to `develop`, full test run, Android build, traceability matrix, status.
- [ ] L4 **Device check (Helder)** — checklist in `task-log.md § Device check`.
