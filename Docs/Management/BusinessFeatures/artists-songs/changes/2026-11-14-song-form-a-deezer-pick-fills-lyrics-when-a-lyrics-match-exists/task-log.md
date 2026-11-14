# Task log: a Deezer pick fills Lyrics

### Checkpoint
- 2026-11-14 — L1, L2a, L2b done (77f66917). L3 dispatched in worktree `deezer-lyrics`. Disk C: is nearly full: worktree tasks run `dotnet test` only; the Android build runs once at close-out.
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

## Device check (Helder) — L4

1. Songs → add → type a well-known title → pick a Deezer row: Lyrics shows "Looking for lyrics…" and then the lyrics.
2. Pick a Deezer row for an obscure or instrumental track: Lyrics stays empty, no message.
3. Type lyrics first, then pick a Deezer row: the typed lyrics stay.
4. Pick, wait for the lyrics, tap X on Title: Lyrics is emptied. Repeat, edit one word, tap X: Lyrics stays.
5. Airplane mode, pick from library only: no lyrics lookup, form works.
