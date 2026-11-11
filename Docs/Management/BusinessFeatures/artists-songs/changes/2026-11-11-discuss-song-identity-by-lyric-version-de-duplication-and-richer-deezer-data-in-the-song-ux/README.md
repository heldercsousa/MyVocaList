---
id: discuss-song-identity-by-lyric-version-de-duplication-and-richer-deezer-data-in-the-song-ux
title: "Discuss: song identity by lyric, version de-duplication and richer Deezer data in the song UX"
status: 💡 Pending
target: 2026-11-11
section: BusinessFeatures
parent: artists-songs
goal: "Decide whether versions of one song (live, remaster, featured artist) should count as one record keyed on lyric, hide near-duplicate Deezer versions, and use Deezer cover art in lists and forms. Gate: discussion with Helder first; nothing designed or built before it."
kind: change
---

# Discuss: song identity by lyric, version de-duplication and richer Deezer data in the song UX

Decide whether versions of one song (live, remaster, featured artist) should count as one record keyed on lyric, hide near-duplicate Deezer versions, and use Deezer cover art in lists and forms. Gate: discussion with Helder first; nothing designed or built before it.


## Origin
Helder, 2026-11-11, device-testing SFT 5.5 (build with the Title-search fix). Recorded here for a later discussion and decision. No design, no code, no spec change before that discussion.

## What was observed on device
- Typing "commando" in New Song > Title lists the local song "Commandi" (Ramones) first, then Deezer rows: the search tolerates a typo. That is the local fuzzy (trigram) match, a baseline of the SFT item, not a Deezer feature.
- Typing "commando ramones" returns rows whose title OR artist matches either word, which Helder found useful. It also returned many versions of one song, all by Ramones, differing only in the version text: "1999 Remaster", plain, "Live at Apollo Centre, Glasgow, Scotland, 12/19/1977", "40th Anniversary Mix", "Sundragon Rough Mix".

## Helder's position and ideas (to discuss)
1. **Versions are noise for MyVocaList.** The lyric is what a singer needs; a live, remastered or remixed version of the same song has the same lyric. Two songs with the same title, artist and lyric need not both exist, whatever their version or featured artists. There is no advantage in persisting 2+ versions with the same lyric.
2. **Hide variant rows in Deezer results.** Find a way to drop Deezer results that are only variations of a song already listed ("Live at...", "Remaster", "Mix", etc.), since they add clutter and no distinction for the user. Unknown whether this can be done reliably or cheaply; Deezer exposes `title_short` and `title_version` (e.g. `title_version: "(Ao Vivo)"`) which may help, but this is unverified.
3. **Smarter song identity.** A later, smarter song form could treat the title as the main title only, ignoring featured artists, live/not-live and version, and include the **lyric** in the uniqueness key, so that same lyric means same song.
4. **Lyric as a full-screen edit.** The lyric entry might move to its own full-screen edit, leaving the song form cleaner.
5. **Several YouTube URLs per song.** A song already allows N YouTube URLs, so different performances or versions of one song can be attached as URLs on a single record instead of being separate songs.
6. **Related existing item:** "Evaluate removing the Song form Version and Featured artists fields" (same folder `changes/2026-11-11-evaluate-removing-...`). This discussion probably decides it.

## More Deezer data that could enrich the UX
Deezer returns, per track, `album.cover_small` (56x56), `cover_medium` (250), `cover_big` (500), `cover_xl` (1000), and `md5_image`; per artist, `picture_small|medium|big|xl`. Ideas from Helder:
- small/medium cover in list items (Songs list, search results, local and Deezer rows);
- medium/big cover in the song form in add and edit modes.
Storage options (local bytes vs URL vs on-demand) are evaluated in the sibling item `changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/`.

## Questions for the discussion
1. What is a song's identity: (artist, main title), (artist, main title, lyric), or something else? What happens to existing records with versions, and to the singer-performance history that hangs off song rows (never recreate rows; see the song-rows-carry-history rule)?
2. How is "main title" derived from a Deezer title: use `title_short` and drop `title_version`, strip "(feat. ...)", "- Live at ...", "- Remastered ..."? What are the false-positive risks (e.g. a real subtitle, "Part II", a different song with a parenthetical)?
3. Is the lyric available at all from Deezer? If not, where does it come from, and can a unique key include a field that is often empty when the song is created?
4. De-duplicating Deezer rows: collapse by (artist, main title) and keep one row, or keep versions but group them? What does a user lose if a wanted version is hidden?
5. Does this change REQ-SFT-35 (plain Deezer query, `Take(5)`) or the duplicate-guard rules in the SFT spec, and does it need a migration?
6. Sequencing against: song-writes-propagate-to-artist-catalog (waits on SFT), the 250-character title limit change (on hold), and the artwork item.
