# Requirements: a Deezer pick fills Lyrics

> **Status: APPROVED 2026-11-14 under Helder's instruction of the same day** ("you're authorized in
> advance for all phases … consider any plan you create as already approved … use your recommended
> options"). Every decision taken for Helder is listed in `design.md § 1` so he can overrule it.
> The on-device check (`tasks.md` L4) stays his.

Predecessor specs (shipped, not edited): Song form title-first
`../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/` (REQ-SFT-NN), Deezer artwork
`../2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/` (REQ-ART-NN),
pictures `../2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/` (REQ-PIC-NN).

## Goal

When the admin picks a Deezer song in the Song form Title field, the Lyrics field is filled with that
song's lyrics when a lyrics match exists. With no match the field stays empty, as today.

**Fact that shapes the feature:** the Deezer public API returns no lyrics text (verified on the live
API 2026-11-04 and again in the API reference 2026-11-14; only an `explicit_lyrics` flag exists). The
lyrics therefore come from a second source, looked up with the data of the Deezer pick.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Deezer pick** | Picking a Title option that fills the form instead of opening a stored song (REQ-SFT-12): every pick handled by `ApplyRemotePick`, whether or not the picked artist already exists in the library. |
| **Lyrics lookup** | One search for the lyrics of a Deezer pick: at most two HTTP requests to the lyrics source. |
| **Lyrics source** | LRCLIB (`https://lrclib.net`), a free lyrics database with a public API and no API key. |
| **Lyrics match** | A lyrics-source record for the picked title and artist with non-blank plain lyrics of at most 10,000 characters. |
| **Pick-filled lyrics** | The exact text a lyrics lookup wrote into the Lyrics field. |
| **Blank** | Null, empty, or white space only. |
| **X** | The clear icon of the Title or Artist field (REQ-SFT-14). |

## User stories

1. As an admin, I pick a song from Deezer and get its lyrics filled in, so I do not paste them by hand.
2. As an admin, I keep lyrics I typed or pasted: a lookup never replaces them.
3. As an admin, I am not blocked or interrupted when no lyrics exist or the network fails.

## Acceptance criteria

- **REQ-LYR-01: A Deezer pick starts one lyrics lookup.**
  Given the Song form in add mode with a blank Lyrics field, When the user makes a Deezer pick, Then
  the system SHALL start one lyrics lookup with the picked title, the picked artist name, the album
  title (when the pick has one) and the track duration in seconds (when the pick has one).
- **REQ-LYR-02: A match fills Lyrics.**
  Given a lyrics lookup that returns a lyrics match, When the answer arrives and the Lyrics field is
  still blank and the pick is still the current pick, Then the Lyrics field SHALL hold the matched
  plain lyrics, trimmed.
- **REQ-LYR-03: No match leaves Lyrics empty.**
  IF the lyrics source has no record, or the record is instrumental, or its plain lyrics are blank,
  THEN the Lyrics field SHALL stay blank and the form SHALL show no message.
- **REQ-LYR-04: Typed lyrics are never replaced.**
  - Given a non-blank Lyrics field at pick time, Then no lyrics lookup is started.
  - Given the user types in Lyrics while a lookup runs, When the answer arrives, Then the Lyrics field
    SHALL keep the typed text.
  - The test is the field's value when the answer arrives: a field the user typed in and emptied again
    is blank and is filled.
- **REQ-LYR-05: Lyrics over the limit are not used.**
  IF the matched plain lyrics, trimmed, are longer than 10,000 characters, THEN the Lyrics field SHALL
  stay blank. (A cut text would be stored as if complete.)
- **REQ-LYR-06: Bounded lookup.**
  A lyrics lookup SHALL end within 5,000 ms of its start. WHEN the bound is reached, the lookup ends
  with no match.
- **REQ-LYR-07: Offline and failures are silent.**
  - WHILE the device has no internet access, a Deezer pick SHALL start no HTTP request to the lyrics source.
  - IF the lyrics source fails (connection error, timeout, HTTP status other than 200 and 404,
    unreadable body), THEN the lookup ends with no match, the failure is logged at Warning, the form
    SHALL show no message and stays usable. No exception leaves the service.
- **REQ-LYR-08: Second attempt without album and duration.**
  IF the first request, sent with album title or duration, answers 404, THEN the system SHALL send one
  second request with the title and the artist name only. A first request sent with title and artist
  only is never repeated.
- **REQ-LYR-09: Looking-for-lyrics hint.**
  WHILE a lyrics lookup runs, the Lyrics field placeholder SHALL read "Looking for lyrics…". Otherwise
  it reads "Song lyrics (optional)" as today. The field stays editable during the lookup.
- **REQ-LYR-10: A new pick or X cancels the lookup.**
  WHEN the user makes another Deezer pick, taps X on Title, taps X on Artist, or Save starts, the
  running lookup SHALL be cancelled and its answer SHALL never change the form. A cancelled lookup is
  not restarted, also when Save does not complete (validation error, resolution sheet).
- **REQ-LYR-11: X removes untouched pick-filled lyrics.**
  WHEN the user taps X on Title or X on Artist and the Lyrics field still equals the pick-filled
  lyrics, the Lyrics field SHALL become blank. Lyrics the user edited after the fill are kept.
  (Same rule as Version in REQ-SFT-14.)
- **REQ-LYR-12: A second Deezer pick replaces untouched pick-filled lyrics.**
  WHEN the user makes a Deezer pick while the Lyrics field still equals the pick-filled lyrics of the
  previous pick, the Lyrics field SHALL become blank and a new lookup starts for the new pick.
- **REQ-LYR-13: Nothing is stored before Save.**
  A lyrics lookup changes the Lyrics field only. The database changes on Save, through the existing
  Save path, which stores the Lyrics field as it does today.
- **REQ-LYR-14: Scope of the trigger.**
  A local pick (REQ-SFT-10), edit mode (REQ-SFT-27: no Title search) and typing a title without a pick
  SHALL start no lyrics lookup.
- **REQ-LYR-15: Client identification.**
  Every request to the lyrics source SHALL carry the `User-Agent`
  `MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)` (LRCLIB requires an identified client).

## Validation rules

| Field | Rule | Where |
|-------|------|-------|
| Looked-up lyrics | non-blank after trim; length ≤ 10,000 characters; else treated as no match | lyrics service (REQ-LYR-03, 05) |
| `Song.Lyrics` | unchanged: optional, ≤ 10,000 characters | existing |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Offline at pick time | No request; Lyrics stays blank (REQ-LYR-07) |
| 404 on both attempts | Lyrics stays blank, no message (REQ-LYR-03, 08) |
| HTTP 429 or 5xx, connection error, bad JSON | No match, Warning log, no message (REQ-LYR-07) |
| Answer after 5,000 ms | No match (REQ-LYR-06) |
| Answer after the user typed lyrics, picked again, tapped X or saved | Ignored (REQ-LYR-04, 10) |

## Out of scope

- Lyrics for songs already stored (no backfill), for local picks and for edit mode.
- Synced (time-stamped) lyrics; choosing among several lyrics records; a "search lyrics" button.
- Any lyrics source other than LRCLIB; a lyrics-source setting.
- Storing the track duration or the album title on the song.
- A message when no lyrics are found.

## Supersedes (shipped specs are not edited)

| Shipped requirement | Change |
|---------------------|--------|
| REQ-ART-12 ("a Deezer pick SHALL leave … Lyrics … unchanged") | **Replaced for Lyrics** by REQ-LYR-01/02/04. Featured artists and karaoke URLs stay untouched; "writes nothing to the database" stays (REQ-LYR-13). |
| REQ-SFT-12 note ("Lyrics stays user input; lyrics providers remain out of scope") | **Replaced** by this spec. |
| REQ-SFT-14 (X after a Deezer pick) | **Extended** by REQ-LYR-10/11. |
