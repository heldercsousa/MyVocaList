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

## 5. Update 2026-11-22 — Helder's MVP re-scope and the terms re-checked

### Helder's decisions (2026-11-22)
- **The catalog is MVP, not post-MVP.** It is how a small band records which songs it can cover in a Bandokê event (live instrumental karaoke).
- **YouTube integration is MVP, not post-MVP.** It is the main source of karaoke videos. The app records a YouTube id against a song id, so that a video tracked once is found and replayed later. Search must happen inside the app; leaving the app and coming back is a bad experience. Benchmark: the Karaoke VS desktop app (Helder bought a one-year license), which searches YouTube in-app, filters by genre tags, plays a queue of picked videos and shows the YouTube logo throughout.
- **Compliance overrides earlier choices.** Nothing illegal may be done in the app. If storing only external ids is what compliance needs, it replaces the 2026-11-16 decision that rejected it. Ideally all third-party data loads at runtime. The duplicate-title problem (today solved by trimming extra words off autocomplete titles) still has to be handled.
- **Question from Helder:** may a Deezer song, artist or album page link to a separate lyrics page or a YouTube search page, so that Deezer data and YouTube video are never on screen together?
- **Wish:** lyrics shown to the singer, ideally synced to the instrumental, and if possible synced automatically by the app, as a differentiator.
- **Bandokê lyrics (Helder, 2026-11-22):** Bandokê needs no synced lyrics. It needs only the lyrics shown on a screen, for singers who do not know the song by heart.

### Deezer, re-checked (Terms "Last updated: 27 October 2026") — https://developers.deezer.com/termsofuse
- §III.4: "The Developer is not authorised either to make any local copy of any Content available through the Services". §I defines Content as "textual, visual, audio and any other content such as metadata". **V**
- The developers FAQ that allowed names to be stored locally now answers HTTP 404. **V** Without it, nothing written by Deezer allows storing titles or artist names.
- **This reverses the 2026-11-16 basis.** The app today saves Deezer titles and artist names in local rows; under §III.4 that is a gap, not "OK". See `../../cross-cutting/store-presubmission-compliance/README.md`.
- §III.2: "The Developer shall not combine any Content with visual or audiovisual images." **V** The ban has no scope: it does not say "same screen". **I:** a Deezer page that links to a YouTube page inside the same app may still count as combining. The separate-pages idea lowers the exposure but does not remove it. Only Deezer can confirm, and new developer access is closed (§1 above).
- §III.1 still limits use to "private, personal and non-commercial purpose(s)" and bans revenue "directly or indirectly", including subscription or advertising. **V**
- **Conclusion (I):** Deezer cannot be the MVP catalog source of an app that plays YouTube videos, stores song titles and may later charge.

### MusicBrainz as the catalog source (considerations only)

**Helder, 2026-11-22:** Deezer stays the source. Nothing moves to MusicBrainz now. A systematic comparison of the two (including the earlier idea of MusicBrainz as a fallback when Deezer is offline) is its own item: `changes/2026-11-22-deezer-vs-musicbrainz-systematic-comparison-with-pros-and-cons/`.

- **Core data is CC0** and may be stored forever and used commercially: "anyone can download and use the core data in any way they see fit". **V** — https://musicbrainz.org/doc/About/Data_License
- The core data includes **recordings** (title, artist credit, duration, ISRC, MBID), **artists**, **releases**, **release groups**, **works** ("Title, ISWC, disambiguation comment, MBID") and **relationships**. **V** — https://musicbrainz.org/doc/MusicBrainz_Database
- Tags and genres, ratings and derived statistics are CC BY-NC-SA, not CC0. **V**
- **Works solve the duplicate-title problem (I).** A *work* is the composition ("Evidências"); each recording, live version or cover links to it. A band's repertoire is naturally a list of works. Grouping by work replaces title trimming.
- **Public API:** at most 1 call per second per client, and a User-Agent with contact details. "Non-commercial use of this web service is free"; commercial use needs a plan. **V** — https://musicbrainz.org/doc/MusicBrainz_API
- **Plans** (https://metabrainz.org/supporters/account-type, **V**):
  - "Stealth Start-Up" costs $0 while "not publicly offering their services";
  - "Bronze" costs $100/month and up, "For popular mobile apps…";
  - "a pre-revenue start-up [that expects] revenue in the future" must sign up as commercial.

  A free app published in the store, with a paid tier planned, is therefore commercial. **I**
- **Way around the API fee (I):** the core data is CC0, so the app can ship, or self-host, its own subset built from the public database dumps (for example Brazilian and popular international works). No API call per user, and no fee. The cost is building and refreshing that subset.
- **Gaps:**
  - no regional "popular now" chart;
  - ListenBrainz gives top recordings per artist (`/1/popularity/top-recordings-for-artist/{mbid}`), but its data license was NOT FOUND, and it has no regional endpoint. **V**
  - Cover art (Cover Art Archive) is per-image licensed; show it by URL only. **I**

### YouTube, re-checked — https://developers.google.com/youtube/terms/developer-policies
- **Storage** (§III.E.4):
  - Authorized and Non-Authorized API Data may be kept "for no longer than 30 calendar days. After 30 calendar days, the API Client must either delete or refresh the stored data". **V**
  - Video ids are not on the list of longer-lived items. **V**
  - So a video id obtained from the API must be refreshed at least every 30 days. **I:** `videos.list` costs 1 unit for up to 50 ids from the 10 000-unit daily bucket, so refreshing is cheap.
  - Whether an id the user pastes from a youtube.com link counts as API Data: NOT FOUND.
- **In-app search** (https://developers.google.com/youtube/v3/determine_quota_cost):
  - "Projects that enable the YouTube Data API have a default quota allocation of 100 `search.list` calls". **V**
  - That is per project, so 100 searches a day **for all users together**.
  - More quota needs the "Audit and Quota Extension Form", which checks compliance with the Terms; no fee is mentioned. **V** — https://developers.google.com/youtube/v3/guides/quota_and_compliance_audits
- **Search results** must not be modified or replaced, and results from other sources must not be mixed in "and present[ed] as YouTube search results" (§III.C.5). **V**
  - Genre chips that add words to the query (for example "sertanejo karaoke") are not covered by that ban. **I**
  - Each page showing YouTube content must show YouTube branding (§III.F.2). **I** (read through a summary)
- **Audio:**
  - the policies guide lists "Offer users the ability to download or separate audio tracks" under "Do not". **V** — https://developers.google.com/youtube/terms/developer-policies-guide
  - The same guide accepts overlays for "playback controls (e.g. mute, full screen, play, pause, etc.)" that do not conflict with the player UI. **V**
- **Karaoke VS:** how it searches YouTube (official API with an audited quota, or scraping) could not be found out. **I** Its constant YouTube logo matches §III.F.2.

### Lyrics, re-thought
- **Mechanical karaoke:** the YouTube karaoke video already carries the synced lyrics, and it is the uploader's content played in the official player. The app does not need to show lyrics itself. **I**
- **Bandokê:**
  - Plain lyrics on a screen, no sync (Helder). The only open question is where the text comes from:
    - Musixmatch's free tier is reported to return only about 30% of the lyrics; full and synced lyrics need a paid commercial plan. **I** (third-party pages)
    - **User-supplied lyrics** (the band pastes its own lyric sheet, kept on the device, never shared by the app) is the pattern of setlist apps and carries the lowest exposure. **I**
- **Automatic sync:**
  - Needs a lawfully owned audio file. YouTube audio cannot be used (guide: no separating audio).
  - A legal path: the band uploads its own rehearsal recording, and a server aligns the band's lyrics to it (WhisperX or stable-ts forced alignment).
  - Post-MVP; it is a server feature. **I**
- **The other karaoke app** with a fixed background and words colored in time: probably a licensed catalog produced in-house (KaraFun-style), hence the limited song list. **I** — https://www.karafun.com/pt-br/subscribe.html

### Revised triage (supersedes §4 where they differ)

| Point | Before | Now |
|---|---|---|
| Catalog (band repertoire) | Post-MVP browse ideas | **MVP** (Helder). Deezer stays; the Deezer vs MusicBrainz comparison item informs any change |
| 1.6 store only the external id | Rejected 2026-11-16 | **Required for Deezer** by §III.4. Not needed for MusicBrainz (CC0) |
| 1.7 YouTube in-app search and player | Post-MVP | **MVP** (Helder). Needs the quota audit for in-app search; ids refreshed every 30 days; YouTube branding on every YouTube page |
| Deezer page → separate YouTube page | — | §III.2 has no scope; residual risk. Prefer not to use Deezer at all |
| Lyrics for mechanical karaoke | LRCLIB copy | Not needed: the video carries them |
| Lyrics for Bandokê | LRCLIB copy | Plain lyrics on a screen, no sync (Helder). Source to decide: user-supplied is the lowest risk; the LRCLIB copy stays a pre-MVP risk |
| Automatic lyric sync | Post-MVP, long term | Post-MVP; band's own recording only |

## 6. Update 2026-11-25 — no YouTube API key: compliance check and recommendation

Helder decided not to use a YouTube Data API key. Nothing here is a decision. Terms evidence is the 2026-11-22 re-check in §5 (Deezer Terms dated 27 October 2026; YouTube Developer Policies); not re-fetched today.

### 6.1 What breaks the terms today
| Where | What | Rule | Status |
|---|---|---|---|
| `Services/YouTubeSearchService.cs` (used by `SongFormViewModel` / `SongFormPage.xaml`) | Calls `youtube/v3/search` and `videos` with a user key | Needs a key; 100 `search.list`/day per project; audit for more | **Without a key the feature cannot work.** It must be removed or replaced (decision for Helder) |
| Song form | YouTube results (thumbnails from `img.youtube.com`) shown on the same screen as a Deezer-sourced title/artist | Deezer §III.2: no combining Content with audiovisual images | **Breaks Deezer §III.2** — the clearest illegal mix |
| Local song rows | Deezer titles and artist names saved in SQLite | Deezer §III.4: no local copy of Content incl. metadata | **Breaks §III.4** (already flagged in §5) |
| Stored video id next to a Deezer track id | Gemini's plan "videoId linked to the Deezer track id" | §III.2 (combining) + §III.4 (Deezer id is Content) | **Do not build** |
| Gemini claim "the 30-day refresh does not apply to the bare videoId" | — | YouTube §III.E.4 lists no exemption for ids | **Wrong** for API-obtained ids; moot without a key |
| Any future paid tier with Deezer | — | Deezer §III.1 non-commercial only | Blocks the paid phase |

YouTube side, without a key: playing via the official IFrame player (Referer = app id, no overlays, branding visible, no audio extraction) and storing an id the user pasted from a youtube.com link are compliant (§2). Scraping, InnerTube or YoutubeExplode are **not** (§2).

### 6.2 Workaround that removes the YouTube+Deezer mix
Replace Deezer, not YouTube: the mix disappears only when no Deezer Content is on a screen, in a row or in a link with YouTube. Splitting Deezer and YouTube onto separate pages leaves residual risk (§III.2 has no scope, §5). So:
1. **Catalog = MusicBrainz (CC0)** + the user's own entries. Store recording/work MBID + title + artist locally — allowed.
2. **YouTube without a key:** a "Search on YouTube" button opens `youtube.com/results?search_query=<title> <artist> karaoke` (YouTube app/browser); the user shares or pastes the link back (Android share target / iOS share extension or clipboard paste); the app extracts the 11-char id and stores it against the local song id. Playback in the IFrame player WebView.
3. Show title/thumbnail of a linked video through the player/oEmbed at runtime; never store YouTube titles or images.
4. Remove `YouTubeSearchService`, the key settings and the in-form results list. The in-app search wish (Karaoke VS benchmark) is not reachable without a key: that is the trade-off Helder accepts by dropping the key.

### 6.3 MusicBrainz (+ Cover Art Archive) vs Deezer
| Criterion | MusicBrainz + CAA | Deezer |
|---|---|---|
| License / storage | Core data CC0: store forever, commercial OK **V** | Non-commercial; no local copy of Content **V** |
| Combining with YouTube | No restriction on CC0 data **I** | §III.2 ban **V** |
| Access | No key; User-Agent with contact required; 1 req/s per client **V** | No key today, but new developer access closed since 2025-10 **V** |
| API cost | Free non-commercial; a published app planning a paid tier counts as commercial → plan (Bronze from $100/month) **V/I**; avoidable by shipping/self-hosting a subset from the CC0 dumps **I** | Free (non-commercial only) |
| Search quality | Good for exact title+artist (Lucene search); weaker fuzzy/typo tolerance; many duplicate releases; Brazilian popular coverage uneven **I** | Very good consumer search, typo-tolerant **I** |
| Duplicate titles | **Works** group versions of one composition **V** — a real win | No work concept |
| Artwork | Cover Art Archive by release MBID; not all releases have art; images are per-image licensed (not CC0) → display by URL only **I** | Covers + artist pictures; not storable **V** |
| Artist pictures | None in MusicBrainz **I** | Yes |
| Audio previews | **None** | 30-s previews (Widget only, §III.3) |
| Popularity / charts | None; ListenBrainz top recordings per artist, licence NOT FOUND, no regional chart **V** | `/chart`, regional by IP **V** |
| Genres / tags | CC BY-NC-SA (not CC0) → attribution + non-commercial **V** | Album genres |
| Attribution | Not required for CC0 core; courteous credit + required for tags **V** | Visible Deezer logo **V** |

What MusicBrainz lacks for MyVocaList: previews, artist photos, charts/"popular now", reliable artwork, consumer-grade fuzzy search, and commercially usable genres. None of these is required for the queue, the Bandokê repertoire or the YouTube link — they mattered only for the post-MVP browse ideas (1.1–1.5).

### 6.4 Recommendation (for Helder to decide)
1. **YouTube-only + MusicBrainz**, no Deezer, no YouTube key: catalog from MusicBrainz works/recordings (local CC0 rows), video via search-out + share-back + IFrame player.
2. Remove the key-based YouTube results list and its service; migrate existing Deezer rows to source-neutral local rows (keep the user's own data; re-match to MBIDs where possible; history stays in place per the never-recreate rule).
3. Start on the free MusicBrainz API (1 req/s, debounced search, User-Agent) during the free phase; before any paid tier, either buy a MetaBrainz plan or ship a CC0 subset.
4. Accept losing previews/charts/artist photos; cover art by CAA URL only, where present.
5. Spec first: a `changes/` item for "YouTube without a key" and an update of the Deezer vs MusicBrainz comparison item, before any code.
