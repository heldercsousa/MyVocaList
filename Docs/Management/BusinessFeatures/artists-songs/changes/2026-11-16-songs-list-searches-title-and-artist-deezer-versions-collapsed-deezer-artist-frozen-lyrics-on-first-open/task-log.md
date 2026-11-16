# Task log: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md`, `design.md`. Plan: `plan.md`. Tasks: `tasks.md`.
Branch `feature/songs-list-deezer`, worktree `.claude/worktrees/songs-list-deezer`.

### Checkpoint

- **Updated:** 2026-11-16, after T10.
- **Phase:** 4 — UI. T11a and T11b dispatched to one implementor (sequential).
- **Done:** T0 to T10. Task branch at 6bea2b54, pushed. Full suite 1,801 passed, 0 failed.
- **Next:** T12 (page), T13 (close-out: Android build, review, merge), then Helder's device check (T14).
- **Context manifest (≤ 8 files):** `plan.md`, `tasks.md`, `requirements.md`, `design.md`, this file.

## T0 — Spec and plan (2026-11-16) — Done

- Discussion with Helder on how Deezer data is kept. Id-only storage rejected; four certain pieces specified here; the Deezer section in the Songs list is a separate undecided trial; sending lyrics to LRCLIB is post-MVP.
- Helder's instruction of 2026-11-16: "You are authorized in advance to proceed with all next needed steps. Be autonomous." The spec and the plan are approved under it. The device check stays his.
- Spec review (fresh reviewer): pass with fixes; fixes applied (REQ-SLD-30, 31 added; decisions C14 to C19).
- Plan review (fresh reviewer): pass with fixes; fixes applied (`plan.md § 9`).
- Deezer API check: official reference behind a login wall, not read. A recorded real answer in the test suite shows 25 rows by default and `artist.id` on each row. No page-size parameter is sent (C20).

### For Helder to confirm or overrule

Decisions taken for him are C1 to C27 in `design.md § 1.2`. The ones most worth a look:

| # | Decision |
|---|----------|
| C14 | Add mode: changing the Title text after a Deezer pick makes the artist editable again (that song is saved as local-only by the existing rule). |
| C16 | Save waits for the first-open lyrics lookup to end before writing. |
| C17 | "None found" is permanent, also when the user had corrected the title or the lyrics were over the limit. |
| C22 | Mocks in `SongFormViewModelLyricsTests` are re-pointed to the new lookup method; no assertion changes. |
| C23 | The existing test of the Version reset on X is removed with the code it tests. |
| C25 | **Known gap:** add mode — change the Title, edit the artist, type the picked title back — saves a Deezer song with the edited artist. Not closed. |
| C27 | A Deezer option row keeps its album part; only the version label is gone. |

## T1, T2, T4, T5, T6 (2026-11-16) — To Review

### Changed files

| Task | Commit | Files |
|------|--------|-------|
| T1 | 9c87cbde | `Contracts/Enums/LyricsOrigin.cs`, `Contracts/DTOs/LyricsLookupResult.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs`, `Domain/Resolution/SongFormSaveRequest.cs`, tests `Unit/Domain/LyricsOriginRulesTests.cs`, `Unit/Domain/SongIdentityRulesTests.cs` |
| T2 | 646efce9 | `Contracts/DTOs/MusicSearchResultDto.cs`, `Domain/Constants/SearchConstants.cs` (level C, no test) |
| T6 | 3d5aff4a | `Services/DeezerProvider.cs`, test `Unit/Services/DeezerProviderArtistIdTests.cs` |
| T4 | a0258799 | `Infra/Repository/SongRepository.cs`, test `Integration/Repositories/SongRepositoryListSearchTests.cs` |
| T5 | 0c3abbb3 | `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`, test `Unit/Services/SongLyricsServiceLookupTests.cs` |

### Verification evidence

- T1: rule tests 46 cases passed; full suite 1,706 passed, 0 failed (run by the main agent).
- T2 + T6: `DeezerProvider` filter 41 passed; full suite 1,712 passed, 0 failed. No existing test edited.
- T4: list-search filter 118 passed; full suite 1,720 passed, 0 failed. `SqlQueryRaw<SongListItemDto>` binds the record directly.
- T5: `SongLyricsService` filter 31 passed (the 16 existing tests unchanged); full suite 1,729 passed, 0 failed.

### Deviations from strict TDD (honest record)

- T1: the implementor lost its shell before running anything. Tests were written as one batch and first failed at compile level. The main agent reviewed the diff, ran the tests and committed.
- T5: the nine tests were first red against a throwing stub, not on their assertions.
- T4 and T6: tests that guard existing behaviour passed before the change and are recorded as guards; the others were red on their assertions first.

### Notes

- T2/T6 commits are signed "Claude Fable 5.2" although that implementor reported running on Haiku (briefing error by the main agent). Later briefings let each agent sign with its own model.
- The pre-commit hook failed once on the T2 commit and passed on an unchanged retry; no cause found.
- T5: the "blank title or artist gives no answer" row has no dedicated new test (the plan lists nine); the existing `FindLyricsAsync_BlankTitleOrArtist` test covers the wrapper.

## T3, T7, T8a, T8b, T9a, T9b, T10 (2026-11-16) — To Review

### Changed files

| Task | Commit | Files |
|------|--------|-------|
| T7 | 61014727 | `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (doc), new test `Unit/Services/SongSuggestionServiceGroupingTests.cs`; existing tests per plan § 7: two removed in `SongSuggestionServiceTests.cs`, one assertion changed in `SongSuggestionTitleSearchRealStackTests.cs` |
| T8a | ac367a8d | `Services/SongService.cs`, new test `Unit/Services/SongServiceArtistFreezeTests.cs` |
| T3 | 738229ee | `Domain/Entity/Song.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`, `Infra/Migrations/20261116152258_AddSongLyricsOrigin.cs` (+ designer, snapshot), new test `Integration/Migrations/LyricsOriginMigrationTests.cs` |
| T8b | 64e06519 | `Services/SongService.cs`, new test `Unit/Services/SongServiceLyricsOriginTests.cs` |
| T9a | 56be4b42 | `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, new test `Unit/Services/SongServiceEnsureLyricsTests.cs` |
| T9b | 49cdc1a7 | new test `Integration/Services/SongServiceEnsureLyricsRealStackTests.cs` |
| T10 | 6bea2b54 | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelFrozenArtistTests.cs`; existing tests per plan § 7 and § 8 item 4: two removed and one assertion removed in `SongFormViewModelTests.cs` |

### Verification evidence

- T7: `SongSuggestion` filter 57 passed; full suite 1,739 passed, 0 failed. 8 tests red on assertion first, 4 guards.
- T8a: `SongService` filter 174 passed (read-scope tests unchanged); full suite 1,751 passed. 6 red first, 6 guards.
- T3: before the migration 105 tests failed on "pending model changes"; after it the full suite is 1,756 passed, 0 failed (run by the main agent).
- T8b: `SongService` filter 185 passed; full suite 1,767 passed. 7 red first, 4 guards.
- T9a: `SongService` filter 201 passed; full suite 1,783 passed, 0 failed.
- T9b: three real-stack tests pass against T9a's code; each was shown able to fail by inverting its key assertion and restoring it. Full suite 1,786 passed.
- T10: `SongFormViewModel` filter 210 passed; full suite 1,801 passed, 0 failed.

### Decisions and events during implementation

- **C24 revised (T9a).** The optional constructor parameter for the lyrics service made the container build that service for every `SongService`; 32 existing tests failed (their hosts do not register the network-status reader). Changed to resolving the lyrics service inside `EnsureLyricsAsync` from the unit-of-work scope. No existing test or test host was edited. The reflection DI test was dropped; T9b covers the wiring.
- **C28 (T10).** The form sends the Deezer provider and id as a pair. One extra test covers the half-identity case.
- **T3 migration (main agent).** Generated with `dotnet ef migrations add AddSongLyricsOrigin -p Infra/MyVocaList.Infra.csproj`. The generated `Down` was replaced by the native `ALTER TABLE ... DROP COLUMN` statement (plan T3), a hand edit of a generated code file by the main agent as the plan assigns. `dotnet ef migrations has-pending-changes` does not exist in the installed EF tools (9.0.6); the green migrate-based tests are the evidence that the model and the migration agree.
- **T7, flagged for review.** Two Deezer rows with an empty title and the same artist id would merge (the plan names only a null title as its own group). Deezer does not send empty titles; left as is.
- **T7.** "Blank version" is tested with `IsNullOrWhiteSpace`. The remote-search log reports the row count before the 25-row limit.
- **T10.** Guards were run in batches of two or three; each red test was run on its own.

### Existing tests changed (all named in plan § 7 / § 8, none beyond)

| File | Test | Change | Authority |
|------|------|--------|-----------|
| `SongSuggestionServiceTests.cs` | `SearchTitleRemoteAsync_RemoteRowCarriesVersion` | removed | REQ-SFT-12 (Version) replaced by REQ-SLD-08 |
| `SongSuggestionServiceTests.cs` | `SearchTitleRemoteAsync_CollatedTitleArtistVersionMatch_Promoted` | removed | REQ-SFT-05 applied with a blank version (REQ-SLD-09) |
| `SongSuggestionTitleSearchRealStackTests.cs` | `SearchTitleRemoteAsync_EmptyLibrary_ReturnsDeezerOptionsWithOkStatus` | one assertion: version "Ao Vivo" → "" | REQ-SFT-04 note replaced by REQ-SLD-06 to 08 |
| `SongFormViewModelTests.cs` | `SelectTitle_RemoteOption_SetsVersionFromDeezer_OverwritingTypedVersion` | removed | REQ-SFT-12 (Version) replaced by REQ-SLD-08 |
| `SongFormViewModelTests.cs` | `ClearArtist_AfterRemotePick_ClearsPendingIdentity_KeepsTitle` | one assertion removed (Version "Live") | same |
| `SongFormViewModelTests.cs` | `ClearTitle_AfterRemotePick_ResetsUnchangedPickFilledVersion` | removed | C23; REQ-SFT-14 Version reset no longer applies |

