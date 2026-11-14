# Task log: a Deezer pick fills Lyrics

### Checkpoint
- 2026-11-14 — Phase 0 done: spec written. Next: spec review, then L1 in worktree `deezer-lyrics`.
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

## Device check (Helder) — L4

1. Songs → add → type a well-known title → pick a Deezer row: Lyrics shows "Looking for lyrics…" and then the lyrics.
2. Pick a Deezer row for an obscure or instrumental track: Lyrics stays empty, no message.
3. Type lyrics first, then pick a Deezer row: the typed lyrics stay.
4. Pick, wait for the lyrics, tap X on Title: Lyrics is emptied. Repeat, edit one word, tap X: Lyrics stays.
5. Airplane mode, pick from library only: no lyrics lookup, form works.
