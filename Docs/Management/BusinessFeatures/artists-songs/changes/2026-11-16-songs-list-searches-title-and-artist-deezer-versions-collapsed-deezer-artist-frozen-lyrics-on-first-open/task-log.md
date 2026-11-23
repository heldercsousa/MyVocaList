# Task log: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md`, `design.md`. Plan: `plan.md`. Tasks: `tasks.md`.
Branch `feature/songs-list-deezer`, worktree `.claude/worktrees/songs-list-deezer`.

### Checkpoint

- **Updated:** 2026-11-19, item closed.
- **Phase:** Done. T14 device check passed 2026-11-19 (round 2 below, plus the four open round-1 points). Item ✅ Done; worktree `songs-list-deezer` removed.
- **Load fix merged** (Helder approved the test waits, 2026-11-17): 53678a5f moves the edit-mode load off the UI thread; waits changed in 6 song-form test files, no assertion changed; `SongFormViewModelImageTests.Save_EditModeAfterDeezerPick_SendsNullImages` had no wait at all and so saved during a pending load. Merge ccbaa4af, full suite 1,856 passed.
- **Context manifest (≤ 8 files):** `tasks.md`, `requirements.md`, `design.md`, `spec-changelog.md`, this file.

## Device check (T14, Helder)

### Round 1 (2026-11-17), Helder's results

Search by artist, Deezer pick, fixed artist in add mode and the airplane-mode copy: ok. Short search text showed "No results found" (fixed by REQ-SLD-35). A Deezer title could be changed into another song's name (fixed by REQ-SLD-36 to 39). Emptied lyrics stay empty: by design (REQ-SLD-27, origin *user*). No lookup on some songs without lyrics: most likely their stored origin is *none found* or *user* (no code fault found; open below). No shimmer seen: the load runs on the UI thread (parked fix).

### Round 2 — build from `develop` at or after 78da0df9

**Result (2026-11-19, Helder): "All 1 to 10 are ok! All tested and all worked."** Covers the six points below (Venues was closed with its own item) and the four open round-1 points: look of the fixed artist, typing lyrics while the lookup runs, leaving during the lookup, Save ignored while loading.

- [ ] **Short search.** Type 1 or 2 letters from the middle of a title, or of an artist name ("u2"): matching songs appear; titles that start with the letters come first.
- [ ] **Deezer title fixed in edit mode.** Open a stored Deezer song: title and artist are plain text, nothing to tap; Save works.
- [ ] **Hand-typed song.** Its title and artist are still tap-to-edit and can be changed.
- [ ] **C25 closed.** Add mode: pick a Deezer song, change one letter of the title, change it back: the artist stays editable; Save stores a hand-typed song (no Deezer mark).
- [ ] **X on Title after a pick** still clears and lets you search again.
- [ ] **Look of the frozen title.** Same size, colour and position as the title's text state.
- [ ] **Venues** (separate item): same name at two addresses saves; another name at an existing address saves; same name and same address is refused with the message on Name.

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

## T11a, T11b, T12, T13 (2026-11-16) — To Review

### Changed files

| Task | Commit | Files |
|------|--------|-------|
| T11a | 5915c90a | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs` |
| T11b | 3d6a0391 | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs`; mock re-point in `Unit/ViewModels/SongFormViewModelLyricsTests.cs` (C22) |
| T12 | 860f857d | `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `SongFormPage.xaml.cs` |
| T13 (review fix) | efa30c1b | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, tests added to `SongFormViewModelFrozenArtistTests.cs` and `SongFormViewModelLyricsOnOpenTests.cs` |
| T13 (merge) | 1631fca9 | merge of `feature/songs-list-deezer` into `develop` |

### Verification evidence

- T11a: `SongFormViewModel` filter 220 passed; full suite 1,811 passed, 0 failed. 4 tests red on assertion first, 3 guards.
- T11b: filter 229 passed; full suite 1,820 passed, 0 failed (two runs). 3 red first, 5 guards.
- T12: Android build `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors, 34 warnings (NuGet advisories) after the XAML edit; 0 errors after the code-behind edit. Full suite 1,820 passed. Level C: no automated test, device check.
- Review fix: `EditMode_BeforeTheSongHasLoaded_ArtistIsFrozen` red first (`Expected: True, Actual: False`); `LookupOnOpen_AnswerForAnotherSong_IsNotApplied` shown able to fail by removing the guard and restoring it. Full suite 1,823 passed.
- Final head efa30c1b: Android build 0 errors, 34 warnings (run by the main agent).
- Merged `develop` (1631fca9): full suite 1,823 passed, 0 failed (run by the main agent).

### Independent verification (fresh verifier, branch head 860f857d)

Verdict: **pass with findings**, no blocking or major finding. Every REQ-SLD criterion has an implementation path and a test or a plan note; the suite passed in three runs (1,820 each); the existing tests changed are exactly those in the table above plus the C22 re-point (no assertion, name or `[AC]` line changed); no governed component, native dialog, text normalisation or sequential-only file (beyond the migration) in the diff; search text is parameterised and escaped; no path found that loses or overwrites typed lyrics; the C24 claim (the lyrics service holds nothing its scope disposes) holds.

| Finding (all minor) | Outcome |
|---------------------|---------|
| The "still shows that song" guard of REQ-SLD-21 had no test | Fixed: test added (efa30c1b) |
| Opening a Deezer song showed the editable artist until the load finished | Fixed: edit mode is frozen until the song has loaded (efa30c1b) |
| `OnDisappearing` cancelling the lookup has no test | Device check line |
| C25 gap (add mode: change the Title, edit the artist, type the picked title back) | Open, for Helder |
| Save that fails validation after cancelling the lookup on open does not restart it | As specified (REQ-SLD-31); lyrics arrive on the next open |
| Empty-title rows with one artist id would merge | Left; Deezer sends no empty titles |
| Blank-query list order has no id tie-breaker; `%` and `_` in a short search act as wildcards | Both older than this item; not changed |

### Open points for Helder

1. **C25** (see above) — close it by changing the identity rule of REQ-PIC-27/28, or accept it.
2. **Load failure in edit mode.** If loading the song throws (not "not found"), the artist stays frozen on that form and the exception is unobserved, as the load already was before this item. Wrap the load in try/finally, or leave it.
3. **Intermittent test.** The pre-commit hook failed once on the T2 commit and once on the T11b commit and passed on an unchanged retry; the failing test name was not captured. Eleven later full runs were green. A guess, not shown: new ViewModel tests that poll a background task with a 5 s cap depend on thread-pool timing.
4. **Attribution.** Commits 646efce9 and 3d5aff4a are signed "Claude Fable 5.2" although that implementor reported running on Haiku (briefing error). Rewriting them would need a history rewrite of merged commits; not done.
5. Decisions C1 to C28 in `design.md § 1.2`, in particular C14, C16, C17, C22, C23, C24 (revised), C27, C28.

### Deviations from strict TDD (T11a to T13)

- Tests of existing behaviour passed before the change and are recorded as guards. `Save_BlankFieldAndStoredMatch_SendsTheMatch` does not prove ordering by itself; the wait test does.
- `EditMode_LocalOnlySongAfterLoad_ArtistBecomesEditableAndNotifies` has no captured red run.

## Traceability matrix (REQ-SLD)

Implementation locations are files and members; tests are classes (method names in `plan.md § 6`).

| AC | Criterion (short) | Implementation | Tests |
|----|-------------------|----------------|-------|
| 01 | List searches title and artist | `SongRepository.GetPagedAsync` (full-text branch, prefix branch, missing-table fallback) | `SongRepositoryListSearchTests` |
| 02 | Order of results | same, `ListSearchSql` order | `SongRepositoryListSearchTests` |
| 03 | Paging kept, disjoint | same, LIMIT/OFFSET, count with the same joins | `SongRepositoryListSearchTests` |
| 04 | Empty states kept | same (empty result); existing `SongsViewModel` | `SongRepositoryListSearchTests`; device check |
| 05 | Catalog mode unchanged | nothing edited | none (level C) |
| 06 | One row per song group | `SongSuggestionService.CollapseSongGroups`; `DeezerProvider` (artist id) | `SongSuggestionServiceGroupingTests`, `DeezerProviderArtistIdTests` |
| 07 | Which row is kept | `CollapseSongGroups` | `SongSuggestionServiceGroupingTests` |
| 08 | A pick carries no version | `CollapseSongGroups` (blank version); `SongFormViewModel.ApplyRemotePick` | `SongSuggestionServiceGroupingTests`, `SongFormViewModelFrozenArtistTests`; device check |
| 09 | Grouping before the library check | `SongSuggestionService` remote search order | `SongSuggestionServiceGroupingTests` |
| 10 | Up to 25 rows grouped, URL unchanged | `DeezerProvider.SearchSongsAsync`, `SearchConstants.RemoteSongFetchCount` | `DeezerProviderArtistIdTests`, `SongSuggestionServiceGroupingTests` |
| 11 | No artist id, own group | `CollapseSongGroups`; `DeezerProvider` | same two classes |
| 12 | Deezer artist is plain text | `SongIdentityRules.IsDeezerSong`; `SongFormViewModel.IsArtistFrozen`; `SongFormPage.xaml` | `SongIdentityRulesTests`, `SongFormViewModelFrozenArtistTests`; device check |
| 13 | Local-only artist editable | same; `SongService` (no refusal) | `SongIdentityRulesTests`, `SongServiceArtistFreezeTests`, `SongFormViewModelFrozenArtistTests`; device check |
| 14 | Releasing the pick releases the artist | `SongFormViewModel.HoldsDeezerPick`, `NotifyArtistFrozenChanged` | `SongFormViewModelFrozenArtistTests` |
| 15 | Service refuses an artist change | `SongService.SaveFromFormAsync` (edit), `UpdateSongCoreAsync` | `SongServiceArtistFreezeTests` |
| 16 | Save decisions are not edits | `SongService` add path (unchanged) | `SongServiceArtistFreezeTests` |
| 17 | Title stays editable | `SongService` (no title rule); `SongFormViewModel` | `SongServiceArtistFreezeTests`, `SongFormViewModelFrozenArtistTests` |
| 18 | Saved as new is local-only | `SongService` add path | `SongServiceArtistFreezeTests`, `SongServiceLyricsOriginTests` |
| 19 | Open starts one lookup | `LyricsOriginRules.NeedsLookupOnOpen`; `SongService.EnsureLyricsAsync`; `SongFormViewModel.LoadSongForEditAsync` | `LyricsOriginRulesTests`, `SongServiceEnsureLyricsTests`, `SongFormViewModelLyricsOnOpenTests` |
| 20 | A match is stored at once | `SongService.EnsureLyricsAsync`; `SongLyricsService.LookupLyricsAsync` | `SongServiceEnsureLyricsTests`, `SongServiceEnsureLyricsRealStackTests`, `SongLyricsServiceLookupTests` |
| 21 | A match fills the open form | `SongFormViewModel.ApplyLyricsOnOpenAnswer` | `SongFormViewModelLyricsOnOpenTests` |
| 22 | Definite no match recorded | `SongLyricsService.LookupLyricsAsync`; `EnsureLyricsAsync`; column mapping | `SongLyricsServiceLookupTests`, `SongServiceEnsureLyricsTests`, real-stack tests, `LyricsOriginMigrationTests` |
| 23 | No answer, tried again later | same; `SongFormViewModel.CancelLyricsLookupOnOpen`; page `OnDisappearing` | `SongLyricsServiceLookupTests`, `SongServiceEnsureLyricsTests`, `SongFormViewModelLyricsOnOpenTests`; device check |
| 24 | Scope of the trigger | `NeedsLookupOnOpen`; `EnsureLyricsAsync` preconditions | `LyricsOriginRulesTests`, `SongServiceEnsureLyricsTests`, real-stack tests, `SongFormViewModelLyricsOnOpenTests` |
| 25 | Looking-for-lyrics hint | `SongFormViewModel` (hint state) | `SongFormViewModelLyricsOnOpenTests` |
| 26 | Origin on Save, add mode | `LyricsOriginRules.OnCreate`; `SongService` create paths; `SongFormViewModel.BuildSaveRequest` | `LyricsOriginRulesTests`, `SongServiceLyricsOriginTests`, `SongFormViewModelLyricsOnOpenTests` |
| 27 | Origin on Save, edit mode | `LyricsOriginRules.OnUpdate`; `SongService.UpdateSongCoreAsync` | `LyricsOriginRulesTests`, `SongServiceLyricsOriginTests` |
| 28 | Songs stored before | migration `AddSongLyricsOrigin` | `LyricsOriginMigrationTests` |
| 29 | Lookup keeps the REQ-LYR rules | `SongLyricsService.LookupLyricsAsync` | `SongLyricsServiceLookupTests`; existing `SongLyricsServiceTests` unchanged |
| 30 | Frozen edit sends the stored artist | `SongFormViewModel.BuildSaveRequest`, `InitializeArtistField`; `SongService.SaveFromFormAsync` | `SongFormViewModelFrozenArtistTests`, `SongServiceArtistFreezeTests` |
| 31 | Save waits for the lookup on open | `SongFormViewModel.SaveAsync` (wait step) | `SongFormViewModelLyricsOnOpenTests`; stored origins by `SongServiceLyricsOriginTests` |
| 32 | Loading indicator in edit mode | `SongFormViewModel.IsSongLoading`; `SongFormPage.xaml` (`dx:ShimmerView`) | `SongFormViewModelLoadingTests`; device check |
| 33 | Save does nothing while the song loads | `SongFormViewModel.SaveAsync` | `SongFormViewModelLoadingTests` |
| 34 | A failed load closes the form with a message | `SongFormViewModel.LoadSongForEditAsync`, `FailSongLoadAsync` | `SongFormViewModelLoadingTests` |

## T15 — loading indicator and wrapped load (2026-11-16) — To Review

Helder's decision after the first merge: "wrap the load. Also, a loading indicator without freezing UI". Spec: REQ-SLD-32 to 34, `design.md § 1.3` (C29 to C32).

### Changed files

| Part | Commit | Files |
|------|--------|-------|
| ViewModel | e4a77167 | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test `Unit/ViewModels/SongFormViewModelLoadingTests.cs` |
| Page | 896c95c3 | `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (fields wrapped in `dx:ShimmerView`; the large diff is re-indentation) |
| Merge | 1be5e035 | into `develop` |

### Verification evidence

- Five of the six new tests were red on their assertion first (written as one batch, not one at a time); `AddMode_IsSongLoadingIsFalse` is a guard.
- `SongFormViewModel` filter 238 passed; full suite 1,829 passed, 0 failed, before and after the XAML change.
- Android build: 0 errors, 34 warnings (NuGet advisories).
- Merged `develop` (1be5e035): full suite 1,829 passed, 0 failed (run by the main agent).
- Not verified by an independent verifier (small follow-up); the shimmer's look is a device check.

### Notes

- The message and the navigation use the members the form already uses after a successful Save (`ShowErrorAsync`, `GoToAsync("..")`).
- `IsSongLoading` stays true until the failure message and the navigation back have run, so Save is ignored for that whole time.
- Not handled: a second song id set on the same form instance before the first load ends would let the first load clear the flag early. The form receives its song id once per instance; left as is.
- After a load that throws, the artist stays frozen on that instance; the form has navigated back by then.

## Intermittent pre-commit failure (2026-11-16)

Twenty consecutive full runs on `develop` with result files: 20 of 20 passed (1,823 each). The two failures of the day both happened inside the pre-commit hook and passed on an unchanged retry; the failing test, if it was a test, was never named. Possible cause, not shown: the hook builds and tests while other processes use the same folder. **Pending Helder:** let the pre-commit hook save its build and test output to a file so the next failure names itself.

## T16, T17, T18 (2026-11-17) — To Review

After Helder's first device round. Spec: REQ-SLD-35 to 40, `design.md § 1.4` (C33 to C39), `spec-changelog.md`.

### Changed files

| Task | Commit | Files |
|------|--------|-------|
| T17 | 8e912d3a | `Services/SongService.cs`, new `Unit/Services/SongServiceTitleFreezeTests.cs`; title inputs in `SongServiceArtistFreezeTests.cs`, `SongServiceSaveFromFormTests.cs`, `SongServiceTests.cs` |
| T16 | a188e68b | `Infra/Repository/SongRepository.cs` (`GetPagedPrefixAsync`), `Integration/Repositories/SongRepositoryListSearchTests.cs` |
| T18 | 956c0fc1 | `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, new `Unit/ViewModels/SongFormViewModelFrozenTitleTests.cs`; `SongFormViewModelTests.cs`, `SongFormViewModelFrozenArtistTests.cs` |
| Merge | 78da0df9 | into `develop` |

### Verification evidence

- T17: 3 of 8 new tests red on the rule first; suite 1,836 passed. T16: 5 new tests red first; suite 1,841 passed. T18: compile red, then 3 of 9 red on the rule; suite 1,855 passed. Android build 0 errors after each.
- Merged `develop` 78da0df9: full suite 1,855 passed, 0 failed (main agent).
- Not independently verified; the page is a device check.

### Existing tests changed (each encodes a replaced requirement)

| File | Test | Change | Authority |
|------|------|--------|-----------|
| `SongServiceArtistFreezeTests.cs` | `SaveFromForm_DeezerSongTitleChanged_Saved` | removed; replaced by the refusal test | REQ-SLD-17 → 37 |
| `SongServiceArtistFreezeTests.cs` | `SaveFromForm_DeezerSongStoredArtistId_SavesWithoutArtistResolution` | input title = stored title | REQ-SLD-37 |
| `SongServiceSaveFromFormTests.cs` | `SaveFromForm_EditMode_RunsNoSongGuard_CallsUpdateWithNullIdentity` | input title = stored title | REQ-SLD-37 |
| `SongServiceTests.cs` | `UpdateSongAsync_TitleEditedOnApiSourcedSong_KeepsExternalIdentity` | now `..._TitleEditedOnDeezerSong_RefusedAndIdentityKept` | REQ-SFT-27 → SLD-37 |
| `SongServiceTests.cs` | `UpdateSongAsync_WithNullExternalIdentity_DoesNotOverwriteExistingIdentity` | input title = stored title | REQ-SLD-37 (C39) |
| `SongRepositoryListSearchTests.cs` | `GetPagedAsync_ShortQuery_MatchesTitlePrefixOnly` | replaced by `..._ShortQueryMatchesArtistNameAnywhere_...` | REQ-SLD-01 → 35 |
| `SongFormViewModelFrozenArtistTests.cs` | `EditMode_DeezerSong_TitleEditKeepsTheArtistFrozen` | renamed; title assertion = loaded title | REQ-SLD-17 → 38 |
| `SongFormViewModelTests.cs` | `EditMode_SaveEditedTitle_SendsNewTitleAndKeepsIdentity` | theory → fact (local-only case); Deezer case moved to a REQ-SLD-38 test | REQ-SLD-17 → 38 |

### Decisions during implementation

- C35 revised: no LIKE escape (the app's `like` override takes two arguments); `%`/`_` act as wildcards, as before. C39: the title rule covers every external identity, like the artist rule.
- Titles are compared after `Trim()`, ordinal. Artist refusal is checked before title refusal.
- `ApplyRemotePick` clears the picked title before writing the Title, so the pick never releases itself.

### Traceability (additions)

| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| 35 | Short text contains, title or artist, prefix first | `SongRepository.GetPagedPrefixAsync` | `SongRepositoryListSearchTests` (6 new) |
| 36 | Deezer title plain text in edit mode | `IsTitleFrozen`; page `Label` | `SongFormViewModelFrozenTitleTests`; device |
| 37 | Service refuses a title change | `SongService.SaveFromFormAsync`, `UpdateSongCoreAsync` | `SongServiceTitleFreezeTests` |
| 38 | Edit save sends the loaded title | `SongFormViewModel.RunSaveAsync` | `SongFormViewModelFrozenTitleTests`, `SongFormViewModelTests` |
| 39 | Changed title drops the pick for good | `OnSongTitleChanged` → `ReleaseDeezerPickOnTitleChange` | `SongFormViewModelFrozenTitleTests` |
| 40 | Old changed titles kept | no code | — |

### Intermittent test, named

`SongFormViewModelTests.TitleOptionSource_SetsNoteThroughUiThreadHelper` (expected 1, actual 2) failed in 2 of 4 full runs of the venue implementor and passes alone. Not edited; it is the likely cause of the earlier unnamed pre-commit failures.

