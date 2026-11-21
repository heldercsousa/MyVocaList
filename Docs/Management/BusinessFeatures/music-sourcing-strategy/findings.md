# Findings — Deezer catalog, YouTube without an API key, lyrics coverage (2026-11-21)

Research session of 2026-11-21, answering Helder's points 1–1.7. Nothing here is a decision. Each claim is marked **V** (verified on the official page or by calling the live API) or **I** (inferred, or from a third party).

Business model given by Helder for this analysis: **free at first; a paid version with cloud features may follow if the community engages.**

Related items:
- `estrategia-gemini-2026-11-14.md` (this folder) holds the original Gemini ideas.
- `../artists-songs/changes/2026-11-16-songs-list-searches-deezer-one-tap-saves-a-thin-song-row-lyrics-copy-with-edit-mark/` is the paused Songs-list Deezer trial.
- `../artists-songs/changes/2026-11-21-deezer-browse-popular-now-cards-artist-profile-with-discography-and-top-hits-read-only-deezer-song-view/` holds the post-MVP browse ideas (1.1–1.5).

## 1. Deezer API

### Access
- The public read endpoints answer with no key or token: `/search`, `/track`, `/artist`, `/album`, `/chart`, `/editorial`. **V** (live calls)
- The Terms still require an application through account creation, approved at Deezer's discretion (§II.a–b). **V** — https://developers.deezer.com/termsofuse
- **New developer access has been disabled.** A Deezer Community Manager wrote on 2025-10-19 that abuse "led us to disable the possibility of creating new accesses". Users still had no access in 2026-03. **V** (staff post) — https://en.deezercommunity.com/features-feedback-44/api-auth-impossible-80857
- Deezer says it is "working on new ways of ensuring that our services are used properly". The keyless endpoints could be locked down. **V** quote, risk **I**.

### Limits
- Deezer publishes no number: "no limitation on data… but there is a query quota". **V** — https://support.deezer.com/hc/en-gb/articles/360011538897
- Third-party libraries use about 50 requests per 5 seconds. **I**
- Search reaches only a few hundred results per query. `search?q=love` reported `total: 249`, and `index=990` came back empty. **V** (live API)
- Narrow queries with `artist:"…" track:"…"`.

### Terms of Use that matter here
https://developers.deezer.com/termsofuse — **V**

| § | Rule | Effect on MyVocaList |
|---|------|----------------------|
| III.1 | Private, personal, non-commercial use only; money-making use needs a separate written agreement | Fits the free phase. The paid phase needs an agreement or another catalog source |
| III.4 + FAQ | No Content database and no "local copy of any Content". The FAQ allows names stored locally but says "images are not allowed to be stored" | The thin song row (title/artist names + Deezer id + image **URL**) is consistent with the FAQ. **Image files must never be saved** |
| Logo guideline | "Each application using Deezer API/SDKs must … include a clearly visible Deezer Logo" — https://developers.deezer.com/guidelines/logo | A Deezer logo must appear where Deezer data is shown |
| III.3 | Audio only through the Deezer Widget | Not relevant; the app does not play Deezer audio |
| III.2 | No combining Content with "visual or audiovisual images" | **I:** probably rules out an in-app YouTube embed shown next to Deezer data. Opening the YouTube app externally is less exposed |

### Charts and "popular songs" (point 1.1)
- `/chart/0` and `/editorial` take no country parameter. Called from here, they returned Brazil's chart with Portuguese genre names. **V** behaviour; IP-based region **I**.
- `/chart/{genreId}/tracks|albums|artists|playlists` gives popular-by-genre lists. **V/I**
- A first-run "popular now" screen is feasible with no user data.

### Artist data (points 1.5.2, 1.5.3)
- `/artist/{id}` returns `picture_small/medium/big/xl` (56/250/500/1000 px), `nb_album`, `nb_fan` and `tracklist`. It has no genre field. **V**
- `/artist/{id}/top` (top hits), `/artist/{id}/albums` (discography), `/album/{id}/tracks`, `/artist/{id}/related`, and album `cover_small…cover_xl` + `genres`. **I** (the endpoint docs now sit behind a login wall)
- All of it is free, subject to the same quota.

### Paid-phase alternatives
- **MusicBrainz + Cover Art Archive:** CC0 data, commercial use allowed **I**. It allows about 1 request per second per IP and needs a User-Agent with contact details **V** — https://musicbrainz.org/doc/MusicBrainz_API/Rate_Limiting. It has no charts.
- **Spotify Web API:** quotas are pooled per account since 2026-07-23 **V** — https://developer.spotify.com/blog/2026-07-23-web-api-quota-updates. Extended quota requires an established business; commercial use and storage are restricted. **I**
- **Apple iTunes Search:** about 20 calls/min, no key. Content may only promote the store, with a badge/link and credit. **V** — https://performance-partners.apple.com/search-api
- **Last.fm:** non-commercial only, 100 MB storage cap, attribution plus link-back required. **V** — https://www.last.fm/api/tos

**Architecture note for the paid phase:** keep Deezer behind the existing service interfaces, and store a source + external id pair, not Deezer-only fields. That way the catalog source can be swapped for MusicBrainz, or kept under a signed agreement, without a data migration.

## 2. YouTube without an API key (point 1.7)
Sources: Developer Policies https://developers.google.com/youtube/terms/developer-policies · Required Minimum Functionality https://developers.google.com/youtube/terms/required-minimum-functionality · IFrame API https://developers.google.com/youtube/iframe_api_reference · quota https://developers.google.com/youtube/v3/determine_quota_cost

| Gemini claim | Result |
|---|---|
| Store only `videoId` | True. Copies of the video may not be stored (Policies III.E.1). **V** |
| Thumbnails `i.ytimg.com/vi/{id}/mqdefault.jpg` need no key | True in practice; the URL pattern is undocumented. **I** |
| oEmbed needs no key | True in practice; no official Google page. **I** |
| IFrame player needs no key | True (the official sample has no key). Very high request volumes "might" need credentials. **V** |
| `search.list` costs 100 of 10 000 units | **Outdated.** It now has its own bucket: 100 calls/day at 1 unit each. Too few for an event app either way. **V** |
| No overlays; 200×200 minimum | True: no "overlays, frames, or other visual elements in front of any part" of the player. **V** |
| Referer header needed | True. Error 153 = request without a Referer or app identity. The Referer must be an HTTPS URL whose domain is the app id. **V** |

**How MAUI meets the Referer rule:**
- Use `HtmlWebViewSource { Html = <iframe player page>, BaseUrl = "https://<application id>" }` — https://learn.microsoft.com/dotnet/api/microsoft.maui.controls.htmlwebviewsource.baseurl
- This should map to Android `loadDataWithBaseURL` and iOS `LoadHtmlString(baseURL)`. **I** — test on a device.
- iOS also needs `playsinline=1`. **V**
- On Android, the embed may additionally check app identity through the Media Integrity API. **V**

**Compliant search without a key:**
- (a) Open `youtube.com/results?search_query=<title artist karaoke>`, which the app already does.
- (b) The user shares or pastes the link back; the app extracts the id from `youtu.be/{id}`, `watch?v={id}` or `/shorts/{id}`.
- Scraping, InnerTube or YoutubeExplode-style libraries **violate** Policies I.14 and III.E.6 and the YouTube Terms. **V**

**Second screen:**
- HDMI/AirPlay with the player WebView on the external display (Android `Presentation`, iOS external screen) needs no key. Attribution and no-overlay rules still apply; no background play (III.I.9). **I**
- Chromecast from an in-app WebView is unverified. The safe fallback is to open the video in the YouTube app and cast from there.

## 3. Lyrics and karaoke coverage (point 1.7)

### LRCLIB
https://lrclib.net/docs.md — **V**
- **Endpoints:** `GET /api/get?track_name&artist_name[&album_name&duration]`, `/api/get/{id}`, `/api/search` (max 20 results), `/api/v2/search` (beta).
- **Access:** no auth for reading. About 45 req/min anonymously, about 90 with a descriptive User-Agent, about 180 with a registered `Lrclib-App-Id`. `Retry-After` must be honored.
- **Sync level:** `syncedLyrics` is line-level only. Word timing exists in a YAML `lyricsfile` field flagged `hasWordSync`; it is not A2 Enhanced LRC.
- **Brazilian coverage spot check:** "Evidências" and "Pipoco" each had several line-synced entries; only one had word sync. Line sync is common, word sync is rare. **I**
- **Licensing:** the code is MIT; the docs say nothing about rights to the lyrics, which look user-contributed. **I**

### Legal risk of displaying lyrics
- Lyrics are works owned by music publishers. Showing them needs a display license, which licensed apps get through LyricFind or Musixmatch (paid per display). **I**
- US publishers (NMPA) have sued unlicensed lyrics sites. **I** — trade press: https://themusicnetwork.com/news/feature-online-lyric-licensing-part-one
- Brazilian law (Lei 9.610/98) protects lyrics too.
- **LRCLIB grants no rights.** Showing full LRCLIB lyrics in a published app is infringement exposure, lower for a free app but not zero.
- **This applies to what the app already does** (lyrics copied from LRCLIB on first open). It is a pre-MVP decision for Helder; see `../../cross-cutting/store-presubmission-compliance/README.md`.

### "100% karaoke coverage" with self-made word-synced lyrics
- Forced alignment (WhisperX, stable-ts, MMS) works best with known lyrics text plus the audio, on a desktop or server.
- whisper.cpp runs on phones, but word timings on sung vocals over music are unproven. **I**
- It needs a **lawfully owned audio file**. Pulling audio from YouTube is forbidden (III.I.7, III.E.1).
- MyVocaList has no Windows target, so this means a server pipeline. Not realistic before the MVP. "100% coverage" cannot be promised, because of both alignment quality and lyrics licensing.

## 4. Triage — points 1 to 1.7

| Point | Verdict | When |
|---|---|---|
| 1 Songs list shows Deezer titles directly | Allowed in the free phase (logo, no image files). Already a paused trial (2026-11-16) | Trial stays parked; post-MVP |
| 1.1 First-run popular songs | Feasible: `/chart/0/tracks`, region by IP, `/chart/{genreId}` for genres | Post-MVP |
| 1.2 Songs from past events | Local data only, no terms risk; needs the queue/event history first | Post-MVP, after the queue pages |
| 1.3 Card-based browse home (songs + a few artists) | UX design work; wait for the UI-library decision | Post-MVP |
| 1.4 / 1.4.1 / 1.4.2 Read-only song view for Deezer songs, only lyrics editable; local songs stay editable | Builds on the existing "Deezer artist frozen" spec | Post-MVP |
| 1.5 Artist profile, discography, top hits, tap song → song view, album expand | Feasible on Deezer (`/top`, `/albums`, `/album/{id}/tracks`). Start with albums expanding inline; add an album page only if needed | Post-MVP |
| 1.6 Store only the Deezer id + changed lyrics | **Already rejected 2026-11-16** (sorting, paging, local search and duplicate checks need the title on the device). The Deezer FAQ allows names locally, so the decision stands. Never store image files | Closed |
| 1.7 YouTube embed without a key | Feasible and compliant (`BaseUrl` Referer, link share-back, id only). Watch Deezer §III.2 if shown next to Deezer data | Post-MVP |
| 1.7 Enhanced lyrics, 100% coverage | Server pipeline + licensed audio + lyrics license | Post-MVP, long term |
| Compliance of what already ships | Deezer logo, no stored image files, lyrics display risk | **Pre-MVP**, in `store-presubmission-compliance` |
