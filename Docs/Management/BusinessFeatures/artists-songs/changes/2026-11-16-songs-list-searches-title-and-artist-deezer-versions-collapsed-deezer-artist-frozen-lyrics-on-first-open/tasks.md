# Tasks: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md` (REQ-SLD-01 to 31), `design.md`. Plan: `plan.md` (signatures, test names, decisions in § 8). Log: `task-log.md`.
Branch `feature/songs-list-deezer` (worktree `.claude/worktrees/songs-list-deezer`). Tasks run in order in that
one worktree. DRY Onion order: Contracts/Domain → Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec and plan
- [x] T0 Discussion with Helder, code maps, requirements, design, spec review, Deezer API check, plan.

## Phase 1 — Contracts and Domain
- [ ] T1 **Lyrics origin types and rules.** (REQ-SLD-12, 13, 19, 24, 26, 27) Level A.
  - Files owned: new `Contracts/Enums/LyricsOrigin.cs`, `Contracts/DTOs/LyricsLookupResult.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs`; `Domain/Resolution/SongFormSaveRequest.cs`; new tests `Unit/Domain/LyricsOriginRulesTests.cs`, `Unit/Domain/SongIdentityRulesTests.cs`.
  - Demo: `LyricsOriginRules.OnCreate("A", LyricsLookupResult.Found("A"))` is `LyricsSource`; `OnUpdate("A", LyricsSource, "")` is `User`.
- [ ] T2 **Deezer artist id on the search DTO and the fetch-count constant.** (REQ-SLD-06, 10, types only) Level C, no test.
  - Files owned: `Contracts/DTOs/MusicSearchResultDto.cs`, `Domain/Constants/SearchConstants.cs`.
  - Demo: solution builds; full test run unchanged.

## Phase 2 — Infra
- [ ] T3 **`Song.LyricsOrigin`, mapping, migration `AddSongLyricsOrigin`.** After T1. (REQ-SLD-28) Level B. Sequential-only files; the main agent generates the migration.
  - Files owned: `Domain/Entity/Song.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/Migrations/*AddSongLyricsOrigin*`, `Infra/Migrations/AppDbContextModelSnapshot.cs`, new test `Integration/Migrations/LyricsOriginMigrationTests.cs`.
  - Demo: a song stored before the migration reads `LyricsOrigin == null`; all seven full-text triggers still exist.
- [ ] T4 **Paged list search in `SongRepository.GetPagedAsync`.** (REQ-SLD-01, 02, 03, 04) Level B.
  - Files owned: `Infra/Repository/SongRepository.cs`, new test `Integration/Repositories/SongRepositoryListSearchTests.cs`.
  - Demo: query "guns" returns the songs of Guns N' Roses; 45 matches load as three disjoint pages.

## Phase 3 — Services
- [ ] T5 **`LookupLyricsAsync` on the lyrics service.** After T1. (REQ-SLD-20, 22, 23, 29) Level A.
  - Files owned: `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`, new test `Unit/Services/SongLyricsServiceLookupTests.cs`.
  - Demo: a 404 from the provider gives `NoMatch`; offline gives `NoAnswer`; the 16 existing `SongLyricsServiceTests` pass unchanged.
- [ ] T6 **`DeezerProvider`: artist id and up to 25 track rows.** After T2. (REQ-SLD-06, 10, 11) Level B. No `limit` is sent (plan § 8 item 1).
  - Files owned: `Services/DeezerProvider.cs`, new test `Unit/Services/DeezerProviderArtistIdTests.cs`.
  - Demo: a row with `"artist":{"id":5288900}` carries `ExternalArtistId == "5288900"`; the request URL is unchanged.
- [ ] T7 **One row per song group in `SongSuggestionService`.** After T6. (REQ-SLD-06 to 11) Level A.
  - Files owned: `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (doc comment), new test `Unit/Services/SongSuggestionServiceGroupingTests.cs`; the three existing tests named in plan § 7 (superseded ACs).
  - Demo: three Deezer rows of "Creep" by one artist id give one remote row with a blank version.
- [ ] T8a **Service refuses an artist change on a Deezer song.** After T1. (REQ-SLD-13, 15, 16, 17, 18, 30) Level A.
  - Files owned: `Services/SongService.cs`, new test `Unit/Services/SongServiceArtistFreezeTests.cs`.
  - Demo: updating a Deezer song with another artist id fails with "The artist of a song from the music database cannot be changed."; the stored id saves with no artist resolution call.
- [ ] T8b **Lyrics origin on create and update.** After T3, T8a. (REQ-SLD-18, 26, 27) Level A.
  - Files owned: `Services/SongService.cs`, new test `Unit/Services/SongServiceLyricsOriginTests.cs`.
  - Demo: saving changed lyrics in edit mode stores origin `User`; unchanged lyrics keep the origin.
- [ ] T9 **`EnsureLyricsAsync`.** After T5, T8b. (REQ-SLD-19, 20, 22, 23, 24, 29) Level A.
  - Files owned: `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs` (optional constructor parameter, plan § 8 item 5), new tests `Unit/Services/SongServiceEnsureLyricsTests.cs`, `Integration/Services/SongServiceEnsureLyricsRealStackTests.cs`, one added test in `Unit/DependencyInjection/AppServicesRegistrationTests.cs`.
  - Demo: a Deezer song without lyrics gets lyrics and origin `LyricsSource` stored and read back from a fresh context; the HTTP lookup runs outside any unit-of-work scope.

## Phase 4 — UI
- [ ] T10 **`SongFormViewModel`: frozen artist, no Version on a pick.** After T7, T8a. (REQ-SLD-08, 12, 13, 14, 17, 30) Level A.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelFrozenArtistTests.cs`; the existing tests named in plan § 7 and § 8 item 4 (superseded ACs).
  - Demo: after a Deezer pick `IsArtistFrozen` is true; changing the Title text or X on Title makes it false and keeps the artist text.
- [ ] T11 **`SongFormViewModel`: lyrics on open, Save waits, pick outcome in the request.** After T9, T10. (REQ-SLD-19, 21, 23, 24, 25, 26, 31) Level A.
  - Files owned: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs`; mock re-point in `Unit/ViewModels/SongFormViewModelLyricsTests.cs` (planned exception, plan § 8 item 3).
  - Demo: opening a Deezer song without lyrics starts one lookup and fills the blank field; Save pressed meanwhile waits for it.
- [ ] T12 **Page: plain artist text when frozen.** After T11. (REQ-SLD-08, 12, 13, 23) Level C, device check. One XAML file, then build.
  - Files owned: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`.
  - Demo: a Deezer song shows the artist as plain Title Large text; a local-only song keeps the tap-to-edit field.

## Close-out
- [ ] T13 Full test run, Android build, pending-model-changes check, traceability matrix, review, merge to `develop`, status.
- [ ] T14 **Device check (Helder)** — checklist in `task-log.md § Device check`.
