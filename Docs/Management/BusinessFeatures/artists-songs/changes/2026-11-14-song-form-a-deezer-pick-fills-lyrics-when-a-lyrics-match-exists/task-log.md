# Task log: a Deezer pick fills Lyrics

### Checkpoint
- 2026-11-14 — L1 done (5fd8ad0c). L2a dispatched in worktree `deezer-lyrics`. Next: L2b, L3.
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

## Device check (Helder) — L4

1. Songs → add → type a well-known title → pick a Deezer row: Lyrics shows "Looking for lyrics…" and then the lyrics.
2. Pick a Deezer row for an obscure or instrumental track: Lyrics stays empty, no message.
3. Type lyrics first, then pick a Deezer row: the typed lyrics stay.
4. Pick, wait for the lyrics, tap X on Title: Lyrics is emptied. Repeat, edit one word, tap X: Lyrics stays.
5. Airplane mode, pick from library only: no lyrics lookup, form works.
