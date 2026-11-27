---
id: share-to-song-form-youtube-video-titles-and-thumbnails-artist-photo-adoption-and-song-image-fallback
title: Share-to-song form, YouTube video titles and thumbnails, artist photo adoption and song image fallback
status: 💡 Pending
target: 2026-11-27
parent: music-sourcing-strategy
goal: A shared YouTube link opens a prefilled song form; video rows show YouTube title and thumbnail; the Wikidata photo becomes the artist picture; songs without a picture show the artist or album picture
kind: change
---

# Share-to-song form, YouTube video titles and thumbnails, artist photo adoption and song image fallback

A shared YouTube link opens a prefilled song form; video rows show YouTube title and thumbnail; the Wikidata photo becomes the artist picture; songs without a picture show the artist or album picture


Requested by Helder on 2026-11-27 after the on-device test of APK 8270f468. Approved as part 3 of the plan: components, then screen bugs, then this change.

## Requirements

| ID | Requirement | Acceptance |
|----|-------------|------------|
| REQ-MSRC-11 | A YouTube link shared to the app opens the Song form. | The form is in edit mode when a stored song already holds that video id. It is in add mode otherwise. The video is already in the YouTube URL list (unsaved until Save). The "ready to attach" snackbar is not shown. |
| REQ-MSRC-12 | Add mode from a share is prefilled from the video. | The video title comes from YouTube oEmbed (keyless). It is cleaned of karaoke noise (e.g. "Karaokê", "(Karaoke Version)", "UPGRADED", the channel name) and used to query MusicBrainz. The best recording fills Title and Artist in the picked state. If nothing matches, Title gets the cleaned text and the fields stay editable. Lyrics are fetched with the existing `ILyricsProvider`. |
| REQ-MSRC-13 | An artist that does not exist locally is created on Save with its MusicBrainz id. | The profile data (photo, see REQ-MSRC-15) is fetched at that moment. |
| REQ-MSRC-14 | Each YouTube URL row shows the video title and thumbnail, not the raw video id. | Both come from oEmbed and are stored on the row when it is added. Existing rows are filled lazily when the form opens. Offline or on failure the row falls back to the video id. |
| REQ-MSRC-15 | The licensed Wikidata/Commons photo becomes the artist picture when the artist has none of its own. | It is shown in the form's circle, with the credit kept below. A picture the user chose is never replaced. |
| REQ-MSRC-16 | A song with no picture of its own shows a fallback in lists and the form placeholder. | The album cover is used first (Cover Art Archive for the picked release, when known). The artist picture is used next. The music note icon is used last. |
| REQ-MSRC-17 | MusicBrainz title suggestions are distinct. | One row per (title, artist) pair, keeping the best-scored recording. Done on the fetched page, display-only, with no extra request. |
| REQ-MSRC-18 | Every autocomplete suggestion row shows a picture. | The song's own picture comes first. The album cover comes next, as the Cover Art Archive `release/{mbid}/front-250` URL built from the recording's release with no extra API call and loaded lazily by the image. The artist picture comes next. The current note/avatar icon is used last. |

## Constraints
- Song rows are edited in place and never recreated.
- Keyless sources only: YouTube oEmbed, MusicBrainz at 1 request per second with the User-Agent from local config, Wikidata/Commons and Cover Art Archive.
- No C#-side normalization for DB search.

## Open points
- Cleaning the title is heuristic, so a share can still need manual correction. The tests pin a sample of real karaoke titles.
- REQ-MSRC-12 lyrics depend on the provider's coverage.

## Tasks
- [ ] S1 oEmbed client + title/thumbnail on `SongKaraokeUrl` (migration, in place) — REQ-MSRC-14
- [ ] S2 Share → Song form routing, replacing the snackbar — REQ-MSRC-11
- [ ] S3 Prefill from the video title (cleaning + MusicBrainz + lyrics) — REQ-MSRC-12, -13
- [x] S4 Artist photo adoption — REQ-MSRC-15
- [ ] S5 Song image fallback (Cover Art Archive, then the artist) — REQ-MSRC-16, -18
- [x] S6 Distinct MusicBrainz title suggestions — REQ-MSRC-17
