# Findings — song title search alternatives (2026-11-28)

Trigger: Helder's device test. "Don't Cry" returned no Guns N' Roses row from MusicBrainz; "don't cry gun" and "Don't crie" returned nothing useful.
Legend: **V** = verified (live-tested from this machine, or quoted from the cited URL). **U** = unverified. Latencies are single samples.

## Live tests (keyless), top 5 results

| Source | Q1 "Don't Cry" | Q2 "don't cry gun" | Q3 "Don't crie" | Popularity | ms |
|---|---|---|---|---|---|
| iTunes Search | HIT #1 V | HIT #1 V | MISS (Big Girls / Boys Don't Cry) V | no field; order looks popularity-based U | 121–428 |
| Deezer | HIT #1–3 V | HIT #1 V | MISS ("Don't Creep") V | `rank` field V | 456–775 |
| MusicBrainz, plain query | MISS V | MISS V | MISS V | none V | 1245–1634 |
| MusicBrainz `recording:(don't cry) AND artist:gun*` | — | HIT 5/5 GNR V | — | none V | 812 |
| MusicBrainz fuzzy `recording:(don't crie~1)` | — | — | MISS (drops "don't") V | none | ~1000 |
| MusicBrainz fuzzy + artist | — | — | not obtained: HTTP 503 twice V | | |
| LRCLIB (keyless lyrics DB) | MISS V | HIT #1, misspelled artist V | empty V | none | 351–643 |
| TheAudioDB (test key `2`) | "Not found" V | | | | |
| Last.fm without key | HTTP 400 V | | | | |
| Spotify without token | HTTP 401 V | | | | |

No tested source tolerates the typo "Don't crie" (V).

## Per source

**iTunes Search** — https://performance-partners.apple.com/search-api
- No key (V). Rate: "approximately 20 calls per minute (subject to change)" (V); "Large websites should set up caching logic" (V).
- Previews must be "streamed only, and not downloaded, saved, cached, or synchronized with video" (V). Promo content is "not for entertainment purposes" and may not be used "to promote any other goods or services" (V). Preview attribution: "provided courtesy of iTunes" (V).
- Not stated: whether title/artist strings may be stored; commercial use of metadata (U). Gray area for a karaoke app and a later paid tier.

**Deezer** — https://developers.deezer.com/termsofuse
- Search works without a key (V). New developer access reported closed (staff post, see `findings.md` §1).
- Terms (V): "not authorised either to make any local copy of any Content"; "not authorised to extract Content in order to make a separate database"; "shall not receive, generate or derive … any revenue"; "shall not combine any Content with visual or audiovisual images". **Excluded by its terms.**

**Spotify** — https://developer.spotify.com/documentation/web-api/concepts/quota-modes, https://developer.spotify.com/terms
- Token required (V). Dev mode: Premium owner, "Up to 5 authenticated Spotify users"; extended mode only for organisations with "at least 250k MAUs" (V). Since Feb 2026 search max 10 and `popularity` removed (V). "Do not locally cache any Spotify Content…" (V). **Excluded.**

**MusicBrainz** — https://musicbrainz.org/doc/About/Data_License
- No key; User-Agent with contact info mandatory (V). "1 request per second" per IP, excess gets 503 (V; 503 seen in tests).
- Core data CC0, "effectively placing the data into the Public Domain" (V): storing, paid tier, YouTube pairing all fine.
- No popularity data, many duplicate recordings, weak typo handling (V).

**Last.fm** — needs a key (V); ToS page returned HTTP 402; non-commercial only per a search snippet (U).
**Discogs** — terms page HTTP 403; 60 req/min authenticated and "Restricted Data" non-commercial per snippets (U). Release-oriented, poor fit.
**Genius / Musixmatch** — docs blocked to the fetch tool; token required, commercial licence needed (U). Not tested.
**TheAudioDB** — track search needs artist + title, so no title-first type-ahead (V).
**Apple MusicKit / Apple Music API** — Developer enrolment token (U). Not tested.
**YouTube Data API** — `search.list` = 100 units; default allocation allows ~100 searches/day (V), https://developers.google.com/youtube/v3/determine_quota_cost. Unusable for type-ahead.

## Recommendation
1. **iTunes Search** for remote type-ahead: no key, fast, Q1 and Q2 hit at #1 (V). Debounce, respect ~20 calls/min, never store previews or artwork files. Get a legal reading before a paid tier (terms are written for store promotion).
2. **MusicBrainz** stays for CC0 data after the pick (MBID, discography, cover art) — legally clean, too weak for type-ahead.
3. Deezer and Spotify are excluded by their own terms (V); YouTube Data API by quota (V).
4. Typo tolerance must be local: fuzzy re-ranking over the remote candidates plus the user's own songs (design idea, U).

## Open items
- Check in a browser: Last.fm ToS, Discogs terms, Genius/Musixmatch docs, Apple Music API terms and fees.
- Apple page does not say whether title/artist strings may be stored or whether commercial use is allowed.
