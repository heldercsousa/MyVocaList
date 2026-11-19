# Tasks: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md` (REQ-SLD-01 to 31), `design.md`. Plan: `plan.md` (signatures, test names, decisions in § 8). Log: `task-log.md`.
Branch `feature/songs-list-deezer` (worktree `.claude/worktrees/songs-list-deezer`). Tasks run in order in that
one worktree. DRY Onion order: Contracts/Domain → Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec and plan
- [x] T0 Discussion with Helder, code maps, requirements, design, spec review, Deezer API check, plan.

## Phase 1 — Contracts and Domain
- [x] T1 **Lyrics origin types and rules.** (REQ-SLD-12, 13, 19, 24, 26, 27) Level A.
  - Files owned: new `Contracts/Enums/LyricsOrigin.cs`, `Contracts/DTOs/LyricsLookupResult.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs`; `Domain/Resolution/SongFormSaveRequest.cs`; new tests `Unit/Domain/LyricsOriginRulesTests.cs`, `Unit/Domain/SongIdentityRulesTests.cs`.
  - Demo: `LyricsOriginRules.OnCreate("A", LyricsLookupResult.Found("A"))` is `LyricsSource`; `OnUpdate("A", LyricsSource, "")` is `User`.
- [x] T2 **Deezer artist id on the search DTO and the fetch-count constant.** (REQ-SLD-06, 10, types only) Level C, no test.
  - Files owned: `Contracts/DTOs/MusicSearchResultDto.cs`, `Domain/Constants/SearchConstants.cs`.
  - Demo: solution builds; full test run unchanged.

## Phase 2 — Infra
- [x] T3 **`Song.LyricsOrigin`, mapping, migration `AddSongLyricsOrigin`.** After T1. (REQ-SLD-28) Level B. Sequential-only files; hand-off to the main agent for the migration (plan § 9 item 6).
  - Files owned: `Domain/Entity/Song.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/Migrations/*AddSongLyricsOrigin*`, `Infra/Migrations/AppDbContextModelSnapshot.cs`, new test `Integration/Migrations/LyricsOriginMigrationTests.cs`.
  - Demo: a song stored before the migration reads `LyricsOrigin == null`; all seven full-text triggers still exist.
- [x] T4 **Paged list search in `SongRepository.GetPagedAsync`.** (REQ-SLD-01, 02, 03, 04) Level B.
  - Files owned: `Infra/Repository/SongRepository.cs`, new test `Integration/Repositories/SongRepositoryListSearchTests.cs`.
  - Demo: query "guns" returns the songs of Guns N' Roses; 45 matches load as three disjoint pages.

## Phase 3 — Services
- [x] T5 **`LookupLyricsAsync` on the lyrics service.** After T1. (REQ-SLD-20, 22, 23, 29) Level A.
  - Files owned: `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`, new test `Unit/Services/SongLyricsServiceLookupTests.cs`.
  - Demo: a 404 from the provider gives `NoMatch`; offline gives `NoAnswer`; the 16 existing `SongLyricsServiceTests` pass unchanged.
- [x] T6 **`DeezerProvider`: artist id and up to 25 track rows.** After T2. (REQ-SLD-06, 10, 11) Level B. No `limit` is sent (plan § 8 item 1).
  - Files owned: `Services/DeezerProvider.cs`, new test `Unit/Services/DeezerProviderArtistIdTests.cs`.
  - Demo: a row with `"artist":{"id":5288900}` carries `ExternalArtistId == "5288900"`; the request URL is unchanged.
- [x] T7 **One row per song group in `SongSuggestionService`.** After T6. (REQ-SLD-06 to 11) Level A.
  - Files owned: `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (doc comment), new test `Unit/Services/SongSuggestionServiceGroupingTests.cs`; the three existing tests named in plan § 7 (superseded ACs).
  - Demo: three Deezer rows of "Creep" by one artist id give one remote row with a blank version.
- [x] T8a **Service refuses an artist change on a Deezer song.** After T1. (REQ-SLD-13, 15, 16, 17, 18, 30) Level A.
  - Files owned: `Services/SongService.cs`, new test `Unit/Services/SongServiceArtistFreezeTests.cs`.
  - Demo: updating a Deezer song with another artist id fails with "The artist of a song from the music database cannot be changed."; the stored id saves with no artist resolution call.
- [x] T8b **Lyrics origin on create and update.** After T3, T8a. (REQ-SLD-18, 26, 27) Level A.
  - Files owned: `Services/SongService.cs`, new test `Unit/Services/SongServiceLyricsOriginTests.cs`.
  - Demo: saving changed lyrics in edit mode stores origin `User`; unchanged lyrics keep the origin.
- [x] T9a **`EnsureLyricsAsync`.** After T5, T8b. (REQ-SLD-19, 20, 22, 23, 24, 29) Level A.
  - Files owned: `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs` (lyrics service resolved inside the method, plan § 8 item 5 as revised), new test `Unit/Services/SongServiceEnsureLyricsTests.cs`.
  - Demo: a match is stored with origin `LyricsSource`; a definite no-match stores `NoneFound`; the HTTP lookup runs outside any unit-of-work scope.
- [x] T9b **`EnsureLyricsAsync` on the real stack.** After T9a. (REQ-SLD-19, 20, 24) Level A. (plan § 9 item 2)
  - Files owned: new test `Integration/Services/SongServiceEnsureLyricsRealStackTests.cs`.
  - Demo: lyrics and origin are read back from a fresh context through the real unit of work.

## Phase 4 — UI
- [x] T10 **`SongFormViewModel`: frozen artist, no Version on a pick.** After T7, T8a. (REQ-SLD-08, 12, 13, 14, 17, 30) Level A.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelFrozenArtistTests.cs`; the existing tests named in plan § 7 and § 8 item 4 (superseded ACs).
  - Demo: after a Deezer pick `IsArtistFrozen` is true; changing the Title text or X on Title makes it false and keeps the artist text.
- [x] T11a **`SongFormViewModel`: lyrics on open.** After T9a, T10. (REQ-SLD-19, 21, 23, 24, 25) Level A. (plan § 9 items 3, 4, 8)
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs`.
  - Demo: opening a Deezer song without lyrics starts one lookup, shows the hint and fills the blank field.
- [x] T11b **`SongFormViewModel`: Save waits, pick outcome in the request.** After T11a. (REQ-SLD-26, 31) Level A. (plan § 9 items 3, 5)
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs`; mock re-point in `Unit/ViewModels/SongFormViewModelLyricsTests.cs` (planned exception, plan § 8 item 3).
  - Demo: Save pressed during the lookup waits for it; the save request carries the pick lookup outcome.
- [x] T12 **Page: plain artist text when frozen.** After T11b. (REQ-SLD-08, 12, 13, 23) Level C, device check. One XAML file, then build.
  - Files owned: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`.
  - Demo: a Deezer song shows the artist as plain Title Large text; a local-only song keeps the tap-to-edit field.

## Close-out
- [x] T13 Full test run, Android build, pending-model-changes check, traceability matrix, review, merge to `develop`, status.
- [x] T15 **Song form: loading indicator and wrapped load.** (REQ-SLD-32, 33, 34) Level A for the ViewModel, device check for the shimmer. Added 2026-11-16 on Helder's decision.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, new test `Unit/ViewModels/SongFormViewModelLoadingTests.cs`.
  - Demo: opening a stored song shows a shimmer until it has loaded; a failed load shows "This song could not be loaded." and goes back.
- [x] T16 **Songs list: a short text matches inside title or artist.** (REQ-SLD-35) Level B. Added 2026-11-17 after the device check.
  - Files owned: `Infra/Repository/SongRepository.cs`, the repository test file of the paged list search.
  - Demo: "es" finds "Yesterday"; "u2" finds the songs of U2; title-prefix matches come first.
- [x] T17 **Service: a Deezer song's title cannot be changed.** (REQ-SLD-37) Level A. Added 2026-11-17.
  - Files owned: `Services/SongService.cs`, new test `Unit/Services/SongServiceTitleFreezeTests.cs`; the existing tests of REQ-SLD-17.
  - Demo: an update of a Deezer song with another title is refused on the Title field; a local-only song is renamed.
- [x] T18 **Song form: frozen Deezer title, pick released for good.** After T17. (REQ-SLD-36, 38, 39) Level A for the ViewModel, device check for the page. Added 2026-11-17.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, new test `Unit/ViewModels/SongFormViewModelFrozenTitleTests.cs`; the existing tests of the replaced part of REQ-SLD-14 / REQ-PIC-27.
  - Demo: a stored Deezer song shows its title as plain text; in add mode, changing the title after a pick and typing it back saves a local-only song.
- [x] T14 **Device check (Helder)** — checklist in `task-log.md § Device check`. Passed 2026-11-19 (round 2 and the open round-1 points).
