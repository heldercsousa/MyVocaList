# Task log: a Deezer pick fills Lyrics

### Checkpoint
- 2026-11-14 — Complete. On `develop`; the Deezer pick filling Lyrics was confirmed on the Android emulator (entry "Emulator check").
- Context manifest: `requirements.md`, `design.md`, `tasks.md` (this folder).

## 2026-11-14 — L0 Research and spec (Done)

- Deezer public API: no lyrics text (`explicit_lyrics` flag only); the internal lyrics endpoint needs a
  logged-in user's cookie. Source: Deezer API reference and third-party client docs, checked 2026-11-14.
- LRCLIB (`https://lrclib.net/docs.md`, read 2026-11-14): `GET /api/get` needs `track_name` and
  `artist_name`; `album_name` and `duration` (seconds, 1–3600) are optional and improve the match;
  404 with `TrackNotFound` when nothing matches; `plainLyrics`, `syncedLyrics`, `instrumental` in the
  record; no API key; an identifying `User-Agent` is required; about 90 requests/min per IP when identified.
- Code map: `ApplyRemotePick` in `SongFormViewModel` is the single entry point of a Deezer pick; it is
  synchronous and does no network call today. `ILyricsProvider` exists as an unused placeholder.
  `DeezerTrack` does not read `duration` today.
- The tasks file doubles as the plan (files owned + demo per task); no separate `plan.md`.

## 2026-11-14 — L1 Duration through the suggestion DTOs (Done)

### Changed files
- `Contracts/DTOs/MusicSearchResultDto.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (trailing `int? DurationSeconds = null`)
- `Services/DeezerProvider.cs` (`DeezerTrack.Duration`, `NormaliseDuration`: null when absent or ≤ 0)
- `Services/SongSuggestionService.cs` (remote rows copy `DurationSeconds`; `MapLocal` unchanged)
- New tests: `Unit/Services/DeezerProviderDurationTests.cs` (4), `Unit/Services/SongSuggestionServiceDurationTests.cs` (3)

### Verification evidence
- Commit `5fd8ad0c` on `feature/deezer-lyrics`. Red: new tests failed to compile on the missing member. Android build 0 errors; `dotnet test` 1284 passed, 0 failed; no existing test edited.
- Orchestrator re-check: diff holds only the six owned files; Deezer + suggestion test subset 126 passed.

### Traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-LYR-01 | duration 215 → `DurationSeconds == 215` | `DeezerProvider.SearchSongsAsync` | `DeezerProviderDurationTests.SearchSongsAsync_DurationPresent_MapsToDurationSeconds` |
| REQ-LYR-01 | missing / 0 / negative → null | `DeezerProvider.NormaliseDuration` | `…_DurationMissing_…`, `…_DurationZero_…`, `…_DurationNegative_…` |
| REQ-LYR-01 | remote row carries the duration; local row null | `SongSuggestionService.ResolveLocalArtistsAsync`, `MapLocal` | `SongSuggestionServiceDurationTests` (3 tests) |

## 2026-11-14 — L2a LRCLIB provider (Done)

### Changed files
- New `Contracts/DTOs/LyricsQuery.cs`; `Services/ILyricsProvider.cs` (signature replaced; no other reference existed); new `Services/LrclibLyricsProvider.cs`
- New tests `Unit/Services/LrclibLyricsProviderTests.cs` (17)

### Verification evidence
- Commit `d55981cf` on `feature/deezer-lyrics`. Red: 17 failed on a `NotImplementedException` scaffold; green: 17 passed. Android build 0 errors; `dotnet test` 1301 passed, 0 failed.
- Orchestrator re-check: diff holds only the four files; provider source read against `design.md § 2.4`; provider tests 17 passed.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-LYR-01 | title, artist, album, duration sent, escaped; album/duration omitted when blank or outside 1–3600 | `LrclibLyricsProvider.BuildUrl` | `FetchLyricsAsync_FullQuery_…`, `…_NullAlbumAndNullDuration_…`, `…_BlankAlbum_…`, `…_DurationOutsideOneToOneHour_…`, `…_DurationAtBounds_…` |
| REQ-LYR-02 | match returns the text | `FetchLyricsAsync` | `…_200WithPlainLyrics_ReturnsTextUnchanged` |
| REQ-LYR-03 | instrumental, null, blank, 404 → null | `FetchLyricsAsync` | `…_200Instrumental_…`, `…_200NullPlainLyrics_…`, `…_200WhitespacePlainLyrics_…`, `…_404TrackNotFound_…` |
| REQ-LYR-07 | 500 / 429 / bad body / cancel surface to the service | `FetchLyricsAsync` | `…_500_…`, `…_429_…`, `…_200UnreadableBody_…`, `…_CancelledToken_…` |

### Decisions made in the task (kept)
- `instrumental` is read as `bool?` (a missing member counts as not instrumental). A 404 is not logged. The Warning log carries the request URL, as `DeezerProvider` does.

## 2026-11-14 — L2b Lyrics service and DI (Done)

### Changed files
- New `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`; `Domain/Constants/SearchConstants.cs` (`LyricsLookupTimeoutMilliseconds`, `LyricsMaxLength`); `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (LRCLIB typed client, `ILyricsProvider`, `ISongLyricsService`)
- New tests `Unit/Services/SongLyricsServiceTests.cs` (21); one test added to `Unit/DependencyInjection/AppServicesRegistrationTests.cs`

### Verification evidence
- Commit `77f66917` on `feature/deezer-lyrics`. Red: 21 failed on a `NotImplementedException` stub. The implementor was stopped by a full disk (C: at 0 bytes) before the full run and the commit; the orchestrator freed space (build output of this session's worktrees), read the service against `design.md § 2.3`, ran the full suite (1323 passed, 0 failed) and committed.
- The Android-target build was not run in this worktree (disk space); it runs once at close-out (L3c).

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-LYR-02 | match returned trimmed | `SongLyricsService.AcceptLyrics` | `FindLyricsAsync_MatchWithSurroundingWhiteSpace_ReturnsTrimmedText` |
| REQ-LYR-03 | no match / blank → null | `AcceptLyrics` | `…_BothAttemptsNoMatch_…`, `…_WhiteSpaceOnlyAnswer_…` |
| REQ-LYR-05 | 10,000 kept, 10,001 → null | `AcceptLyrics` | `…_TextOfExactlyMaxLength_IsKept`, `…_TextOneOverMaxLength_ReturnsNull` |
| REQ-LYR-06 | 5,000 ms bound over both attempts | linked token + `CancelAfter` | `…_ProviderNeverAnswers_ReturnsNullAfterBound`, `…_BoundCoversBothAttempts_…` |
| REQ-LYR-07 | offline no call; failure → null; caller cancel rethrown | guards and catch chain | `…_Offline_…`, `…_ProviderThrowsHttpRequestException_…`, `…_CallerCancelled…` (2) |
| REQ-LYR-08 | one retry without album and duration, only after a no-match | `HasAlbumOrDuration` | `…_BareQueryNoMatch_…`, `…_NoMatchWithAlbumOrDuration_…` (3), `…_BlankAlbumAndNoDuration_…`, `…_FirstAttemptReturnsText_…` |
| REQ-LYR-15 | LRCLIB client has base address and `User-Agent` | `AddAppServices` | `AddAppServices_ResolvingLyricsProvider_HasConfiguredHttpClientAndSongLyricsService` |

### Decisions made in the task (kept)
- A blank album counts as no album. A blank-only first answer is not retried. An over-limit answer is logged at Information without its content.

## 2026-11-14 — L3 `SongFormViewModel` + placeholder (Done)

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (constructor gains `ISongLyricsService`; `StartLyricsLookup`, `RunLyricsLookupAsync`, `ApplyLyricsAnswer`, `CancelLyricsLookup`, `DropUntouchedPickFilledLyrics`, `LyricsLookupTask`, `IsLyricsLoading`, `LyricsPlaceholder`; hooks in `ApplyRemotePick`, `ClearTitle`, `ClearArtist`, `SaveAsync`)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (`Placeholder="{Binding LyricsPlaceholder}"`)
- New tests `Unit/ViewModels/SongFormViewModelLyricsTests.cs` (21)
- Planned exception (constructor argument only): `SongFormViewModelTests.cs`, `SongFormViewModelImageTests.cs`, `SongFormViewModelPictureTests.cs`, `Integration/Services/SongSuggestionTitleSearchRealStackTests.cs`

### Verification evidence
- Commit `1b5fadc6` on `feature/deezer-lyrics`. Red: 14 of 21 failed because no lookup started (the 7 green ones are no-op cases). `dotnet test` 1344 passed, 0 failed. `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing` unedited and green.
- Orchestrator re-check: diff read line by line against `design.md § 2.5`; the four existing test files changed by the constructor argument only.

### Traceability
| AC | Criterion | Implementation | Tests (`SongFormViewModelLyricsTests`) |
|----|-----------|----------------|-------|
| REQ-LYR-01 | every pick starts one lookup with title, artist, album, duration | `ApplyRemotePick` → `StartLyricsLookup` (before the artist branch) | `PickWithoutLocalArtist_StartsOneLookupWithThePicksFields`, `PickWithLocalArtist_StartsOneLookupToo` |
| REQ-LYR-02 | match fills blank Lyrics | `ApplyLyricsAnswer` | `Match_FillsEmptyLyrics` |
| REQ-LYR-03 | no match leaves it blank | `ApplyLyricsAnswer` | `NoMatch_LeavesLyricsBlank` |
| REQ-LYR-04 | typed lyrics never replaced; emptied field filled | `StartLyricsLookup`, `ApplyLyricsAnswer` | `NonBlankLyricsAtPickTime_StartsNoLookup`, `TypedWhileLookupPending_…`, `TypedThenEmptiedWhileLookupPending_…` |
| REQ-LYR-09 | placeholder texts | `LyricsPlaceholder` | `LyricsPlaceholder_ReadsLookingForLyricsWhilePending_IdleOtherwise`, `LyricsPlaceholder_RaisesPropertyChanged…` |
| REQ-LYR-10 | new pick, X, Save cancel; late answer ignored | `CancelLyricsLookup`, guard in `ApplyLyricsAnswer` | `SecondPick_CancelsFirstLookup_…`, `ClearTitle_CancelsTheLookup_…`, `ClearArtist_CancelsTheLookup_…`, `Save_CancelsTheLookup_…` |
| REQ-LYR-11 | X removes untouched pick-filled lyrics | `DropUntouchedPickFilledLyrics` | `ClearTitle_AfterFill_…` (2), `ClearArtist_AfterFill_…` (2) |
| REQ-LYR-12 | second pick replaces untouched fill | `StartLyricsLookup` | `SecondPick_AfterFill_…`, `SecondPick_AfterFillAndEdit_…` |
| REQ-LYR-13 | nothing stored before Save | lookup writes `Lyrics` only | `Lookup_CallsNoSaveMethodOfSongService_DuringOrAfter` |
| REQ-LYR-14 | local pick starts no lookup | by construction | `LocalPick_NavigatesAndStartsNoLookup` |

### Decisions made in the task (kept)
- A lookup fill is a plain write of `Lyrics`: the field has no dirty flag or validation, so the fill behaves like typing.
- `CancelLyricsLookup()` runs before the `IsBusy` guard of `SaveAsync`, so every Save tap cancels.

## 2026-11-14 — L3c Close-out (Done; device check pending)

- `develop` merged into `feature/deezer-lyrics`; Android build `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors, 34 warnings (package advisories and DevExpress evaluation notices); full suite 1344 passed, 0 failed.
- Merged into `develop` as `fd6aab7e` and pushed.
- All fifteen requirements have at least one test (matrices in the L1, L2a, L2b and L3 entries above); REQ-LYR-15 is covered by the DI test of L2b.
- Not verified here: the behaviour on a device against the live LRCLIB service. That is L4 (Helder), checklist below. The item stays In Progress until then.

## 2026-11-14 — Emulator check (Android, Pixel 5 API 36) (Done)

- New song → typed "Bohemian Rhapsody" → picked the first Deezer option (Queen · A Night At The Opera): Title, Artist and picture filled, and the Lyrics field filled with "Is this the real life? Is this just fantasy? ...". Build from `develop` content (venue branch tip `a4369a4c`).
- Not checked on the emulator: a pick with no lyrics match (unit-tested: the field stays empty).

## Device check (Helder) — L4

1. Songs → add → type a well-known title → pick a Deezer row: Lyrics shows "Looking for lyrics…" and then the lyrics.
2. Pick a Deezer row for an obscure or instrumental track: Lyrics stays empty, no message.
3. Type lyrics first, then pick a Deezer row: the typed lyrics stay.
4. Pick, wait for the lyrics, tap X on Title: Lyrics is emptied. Repeat, edit one word, tap X: Lyrics stays.
5. Airplane mode, pick from library only: no lyrics lookup, form works.
