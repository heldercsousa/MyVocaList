# Requirements: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

> **Status: APPROVED 2026-11-16 under Helder's instruction of the same day** ("You are authorized in
> advance to proceed with all next needed steps. Be autonomous."). Helder's own decisions are marked
> "(Helder)". Every decision Claude took for him is listed in `design.md § 1.2` so he can overrule it.
> The on-device check stays his. Ids run REQ-SLD-01 to 31; 30 and 31 were added by the spec review and
> sit beside the criteria they belong to.

Predecessor specs (shipped, not edited): Song form title-first
`../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/` (REQ-SFT-NN), pictures
`../2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/` (REQ-PIC-NN),
lyrics `../2026-11-14-song-form-a-deezer-pick-fills-lyrics-when-a-lyrics-match-exists/` (REQ-LYR-NN).

## Goal

Four changes that came out of the 2026-11-16 discussion about how Deezer data is kept (background in
`../2026-11-16-songs-list-searches-deezer-one-tap-saves-a-thin-song-row-lyrics-copy-with-edit-mark/README.md`):

1. The Songs list finds a song by its title **or its artist**, the way the Song form's Title search does.
2. Deezer results show **one row per song**: live, remix and album repeats of the same song are collapsed.
3. The **artist of a Deezer song cannot be changed**. Only a song that exists locally only keeps an editable artist.
4. A Deezer song stored without lyrics gets its lyrics **the first time it is opened**; a copy is kept on
   the device, and the app records where the stored lyrics came from.

**Facts that shape the feature**
- A Deezer track search answer lists the same song several times (album, compilation, live). Each row
  carries the Deezer artist id, a short title without the version label, and the version label.
- A stored song has no album title and no duration, so a lyrics lookup for a stored song can send the
  title and the artist name only.
- Only audio differs between versions of a song. MyVocaList stores no audio, so Deezer versions carry
  no value for it; the version concern lives in the YouTube links (Helder, 2026-11-11 and 2026-11-16).

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Deezer song** | A stored song whose external provider and external id are both set, or, in the Song form in add mode, the form state after a Deezer pick while the pick is still held. |
| **Local-only song** | A stored song with no external identity, or the add-mode form with no Deezer pick held. |
| **Deezer pick** | As in REQ-LYR: picking a Title option that fills the form instead of opening a stored song. |
| **Song group** | The rows of one Deezer answer that have the same Deezer artist id and the same short title, compared character by character. |
| **Version label** | Deezer's `title_version` of a row ("Live", "Remix 2010"); blank for the plain recording. |
| **Library search** | The local search the Song form's Title field uses (REQ-SFT-02, REQ-SFT-33): full-text over title and artist when the query has a word of 3 or more characters, otherwise the collated title prefix. |
| **Lyrics origin** | Where the stored lyrics of a song came from: *not recorded*, *lyrics source* (unchanged copy from LRCLIB), *user* (typed or changed by the user), *none found* (the lyrics source answered that it has no match). |
| **Lyrics lookup, lyrics source, lyrics match, blank** | As in REQ-LYR. |
| **Songs list** | The Songs page showing the whole library. The same page showing one artist's catalog is "catalog mode". |

## User stories

1. As an admin, I type an artist's name in the Songs list and see that artist's songs.
2. As an admin, I see each Deezer song once, not once per album and per live recording.
3. As an admin, I cannot change the artist of a song that came from Deezer by accident; I can still fix
   the artist of a song I typed myself.
4. As an admin, I open a Deezer song that has no lyrics and get them without doing anything, and they
   are still there at an event without internet.
5. As the product owner, I know which stored lyrics were written or corrected by the user, for a later
   feature that offers them back to the lyrics source.

## Acceptance criteria

### A. Songs list search

- **REQ-SLD-01: The Songs list searches title and artist.**
  Given the Songs list with a search text that has a word of 3 or more characters, When the list
  loads, Then it SHALL show the songs whose title or artist name matches the text in the full-text
  index, so a song is found by its artist name even when its title does not match. Given a shorter
  non-blank text, the list SHALL show the songs whose title starts with the text (collated), as today;
  a short text does not search the artist.
- **REQ-SLD-02: Order of search results.**
  WHEN the search text has a word of 3 or more characters, the results SHALL be ordered by the
  full-text rank the Song form's Title search uses (title weighted 10, artist 1), then by title
  length, then by song id. Otherwise (shorter text, or blank text) the list SHALL be ordered by title
  as today.
- **REQ-SLD-03: Paging is kept.**
  Search results SHALL load in pages of the application page size, and scrolling loads the next page,
  as today. With an unchanged library, every matching song is reachable by scrolling and no song
  appears twice (the song id in the order makes the pages disjoint). A song saved or deleted between
  two page loads may shift a row; this is accepted, as today.
- **REQ-SLD-04: Empty states are kept.**
  WHEN a non-blank search text returns no song, the list SHALL show the existing "No results found"
  state. A blank search text on an empty library shows the existing "no songs" state.
- **REQ-SLD-05: Catalog mode is unchanged.**
  WHILE the Songs page shows one artist's catalog, its search SHALL behave as today.

### B. One Deezer row per song

- **REQ-SLD-06: A song group is shown as one row.**
  WHEN a Deezer answer holds several rows of one song group, the Song form's Title search SHALL show
  one row for the group. Rows are grouped only on the Deezer artist id and the short title exactly as
  Deezer sent them; the application SHALL NOT change case, remove accents or otherwise rewrite the
  values to group them: "Creep" and "creep" are two groups. The short title of a row is Deezer's
  `title_short`, or its `title` when `title_short` is blank.
- **REQ-SLD-07: Which row of a group is kept.**
  The kept row SHALL be the first row of the group, in Deezer's order, whose version label is blank.
  IF every row of the group has a version label, THEN the kept row is the first row of the group.
- **REQ-SLD-08: A Deezer pick carries no version.**
  WHEN the user makes a Deezer pick, the Version field SHALL stay as the user left it (blank on a fresh
  form), and the Deezer option row SHALL show no version label (its other parts are unchanged).
  The Version field itself stays, for songs and versions the user types.
- **REQ-SLD-09: Grouping comes before the library check.**
  The check that hides Deezer rows already in the library (REQ-SFT-05) SHALL run on the kept rows, with
  a blank version. A song stored from any row of a group with a blank version is therefore shown as
  the library song, not as a Deezer row. A library song stored only with a non-blank version ("Live")
  does not hide the group; the group is shown as a Deezer row and can be saved beside it with a blank
  version.
- **REQ-SLD-10: Enough rows to group.**
  The Deezer track search SHALL group all rows of one Deezer answer, up to a named limit of 25 rows,
  and then show up to the existing maximum of remote rows (REQ-SFT-04). The request to Deezer is
  unchanged (REQ-SFT-35): the rows of Deezer's default answer are used, 25 as observed. The Deezer
  artist search is unchanged.
- **REQ-SLD-11: A row without a Deezer artist id is its own group.**
  IF a Deezer row has no artist id (absent or blank), THEN it SHALL be shown as its own row and never
  merged.

### C. Fixed artist for Deezer songs

- **REQ-SLD-12: The artist of a Deezer song is shown as plain text.** (Helder)
  WHILE the Song form shows a Deezer song, in add mode and in edit mode, the Artist SHALL be shown as
  plain Title Large text. A tap on it does nothing; there is no editor, no edit icon, no X, no artist
  search and no error state.
- **REQ-SLD-13: The artist of a local-only song stays editable.** (Helder)
  WHILE the Song form shows a local-only song, the Artist field SHALL behave as today (REQ-PIC-23 to
  28 and 31): a tap opens the editor and the artist can be changed.
- **REQ-SLD-14: Releasing the Deezer pick releases the artist.**
  In add mode the form shows a Deezer song exactly while a Save at that moment would store the
  external identity of the pick. WHEN that stops being true — the user taps X on Title (REQ-SFT-14),
  or changes the Title text away from the picked title (REQ-PIC-27) — the Artist field SHALL return to
  the editable state of REQ-SLD-13 and keep its text. WHEN the form holds the pick again (a new Deezer
  pick), the artist is plain text again.
- **REQ-SLD-15: The Service refuses an artist change on a Deezer song.**
  IF an update of a Deezer song carries anything but the stored artist id as its artist (another
  artist id, a typed artist name, or a new artist name), THEN the song service SHALL change nothing
  and return the failure "The artist of a song from the music database cannot be changed.", pointing
  at the Artist field. Artists are compared by id only, never by name text. The rule holds whatever
  the screen sends.
- **REQ-SLD-30: Saving a Deezer song in edit mode sends the stored artist.**
  WHEN the user saves a Deezer song in edit mode, the form SHALL send the stored artist id. No artist
  resolution runs and no near-duplicate artist sheet is shown.
- **REQ-SLD-16: Save decisions are not edits.**
  The near-duplicate artist sheet at Save in add mode (REQ-SFT-19) is unchanged: choosing an existing
  artist there is the resolution of the Deezer artist name, not a change of a stored artist. The song
  is stored as a Deezer song with the chosen artist, and the artist picture rule is unchanged.
- **REQ-SLD-17: Title stays editable.**
  The Title of a Deezer song stays editable in edit mode as today (REQ-SFT-27), so a correction never
  requires deleting and re-creating a song.
- **REQ-SLD-18: A song saved as a new version or a new song is local-only.**
  A song stored through "Save as new version" or "Save as new song" has no external identity
  (existing behaviour); it is a local-only song and REQ-SLD-13 applies to it. Its lyrics origin is
  still derived from its lyrics by REQ-SLD-26.

### D. Lyrics on first open

- **REQ-SLD-19: Opening a Deezer song without lyrics starts one lyrics lookup.** (Helder)
  Given a stored Deezer song whose lyrics are blank and whose lyrics origin is *not recorded*, When the
  Song form has finished loading it in edit mode (from the Songs list or from catalog mode), Then the
  system SHALL start one lyrics lookup with the stored title and the stored artist name. The stored
  title is used as it is, also when the user corrected it earlier.
- **REQ-SLD-20: A match is stored at once.** (Helder: "keep a copy")
  WHEN that lookup returns a lyrics match, the song service SHALL store the lyrics on the song with the
  origin *lyrics source*, without waiting for Save. The copy is then available without internet. Once
  the service holds the answer of the lyrics source, it stores it (or the "no match" of REQ-SLD-22)
  even if the form was closed or Save was pressed meanwhile; the service stores only when the song's
  lyrics are still blank and its origin still *not recorded*.
- **REQ-SLD-21: A match fills the open form.**
  WHEN the match arrives and the Lyrics field of the open form is still blank, the field SHALL show the
  lyrics. IF the user typed in the field meanwhile, THEN the field keeps the typed text (as REQ-LYR-04).
- **REQ-SLD-22: A definite "no match" is recorded.**
  IF the lyrics source answers that it has no usable lyrics (no record, instrumental, blank lyrics,
  or lyrics over the limit of REQ-LYR-05), THEN the song service SHALL record the origin *none found*
  and no lookup SHALL start again for that song when it is opened later.
- **REQ-SLD-23: An unanswered lookup is tried again later.**
  IF the lookup ends without an answer from the lyrics source (offline, timeout, failure, the form
  closed first), THEN nothing is recorded and the next opening of the song starts a lookup again.
- **REQ-SLD-24: Scope of the trigger.**
  No lookup on open SHALL start for a local-only song, for a song with non-blank lyrics, or for a song
  whose origin is *user* or *none found*.
- **REQ-SLD-25: Looking-for-lyrics hint.**
  WHILE the lookup on open runs, the Lyrics placeholder SHALL read "Looking for lyrics…" as in
  REQ-LYR-09, and the field stays editable. Offline and failures show no message (as REQ-LYR-07).
  A song that starts no lookup (REQ-SLD-24) shows no hint.
- **REQ-SLD-31: Save waits for the lookup on open.**
  WHEN Save starts while the lookup on open runs, the lookup SHALL be asked to stop and Save SHALL wait
  until it has ended before writing. IF the lookup stored a match before it ended and the Lyrics field
  is blank, THEN the field takes the match first (REQ-SLD-21) and Save stores it unchanged. Outcomes:
  typed lyrics "B" at Save → stored lyrics "B", origin *user*, whatever the lookup returned; blank
  field and a stored match "A" → lyrics "A", origin *lyrics source*; blank field and no answer →
  blank lyrics, origin *not recorded*.
- **REQ-SLD-26: Lyrics origin on Save, add mode.**
  WHEN a new song is saved, its lyrics origin SHALL be:
  1. *lyrics source* when the saved lyrics are non-blank and equal the trimmed lyrics the pick lookup
     of the held pick returned (REQ-LYR-02);
  2. else *user* when the saved lyrics are non-blank;
  3. else *none found* when the pick lookup of the held pick got a definite "no match";
  4. else *not recorded* (blank lyrics and no answer: offline, failure, a lookup still running at
     Save and therefore cancelled by REQ-LYR-10, or no pick at all).
  The rules are tried in this order. A pick lookup whose pick was released by X on Title or Artist
  (REQ-LYR-11) or replaced by a newer pick (REQ-LYR-12) counts as no lookup. A changed Title text
  does not drop it: lyrics that still equal what the lyrics source returned keep that origin. The origin does not depend on whether the song is stored as a
  Deezer song or as a local-only song.
- **REQ-SLD-27: Lyrics origin on Save, edit mode.**
  WHEN a stored song is saved and the saved lyrics differ from the lyrics stored at that moment, the
  origin SHALL become *user*, also when the user emptied the field (an emptied field is the user's
  choice and starts no new lookup). WHEN the lyrics are unchanged, the origin is unchanged.

  | Origin before | Event | Origin after |
  |---------------|-------|--------------|
  | not recorded | lookup on open: match | lyrics source |
  | not recorded | lookup on open: definite no match | none found |
  | not recorded | lookup on open: no answer | not recorded |
  | any | Save with lyrics different from the stored ones | user |
  | any | Save with unchanged lyrics | unchanged |

  No event returns a song to *not recorded*.
- **REQ-SLD-28: Songs stored before this change.**
  Songs stored before this change SHALL have the origin *not recorded*. Those that are Deezer songs
  with blank lyrics therefore get a lookup on their first open (REQ-SLD-19); those with lyrics keep
  them and are never treated as *user* lyrics.
- **REQ-SLD-29: The lookup on open keeps the rules of REQ-LYR.**
  The 5,000 ms bound (REQ-LYR-06), the offline rule (REQ-LYR-07), the 10,000-character limit
  (REQ-LYR-05) and the client identification (REQ-LYR-15) apply. With title and artist only, one
  request is sent (REQ-LYR-08 has nothing to drop).

## Validation rules

| Field | Rule | Where |
|-------|------|-------|
| Artist of a Deezer song | cannot differ from the stored artist on update | song service (REQ-SLD-15) |
| Lyrics origin | one of: not recorded, lyrics source, user, none found | domain (REQ-SLD-22, 26, 27) |
| `Song.Lyrics` | unchanged: optional, ≤ 10,000 characters | existing |
| Looked-up lyrics | unchanged: non-blank after trim, ≤ 10,000 characters, else no match | lyrics service (REQ-LYR-03, 05) |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Full-text index not present (first run before it is built) | The Songs list search falls back to the collated title prefix. The form search keeps its current behaviour |
| Deezer answer with only labelled versions of a song | One row, the first one, shown without the label (REQ-SLD-07, 08) |
| Update request that changes the artist of a Deezer song | Refused, nothing stored (REQ-SLD-15) |
| Offline, timeout or lyrics-source failure on first open | No message, nothing recorded, tried again on the next open (REQ-SLD-23) |
| Lyrics source has no match | Origin *none found*, no later lookup (REQ-SLD-22) |
| Lyrics source has lyrics over 10,000 characters | Treated as no match: origin *none found*, permanently (REQ-SLD-22) |
| The user corrected the title before the first open and the lyrics source knows no such title | Origin *none found*, permanently; the user types or pastes the lyrics. Accepted consequence of "no retry" |
| Save pressed while the lookup on open runs | Save waits for the lookup to end; outcomes in REQ-SLD-31 |
| The same song opened twice in quick succession | The second lookup finds the lyrics already stored, or stores nothing because the song is no longer blank and *not recorded* (REQ-SLD-20) |
| Deezer pick whose artist is resolved through the near-duplicate sheet | Song stored as a Deezer song with the chosen artist (REQ-SLD-16) |
| User types lyrics while the lookup runs | Field keeps the typed text; Save stores it with origin *user* (REQ-SLD-21, 27) |
| The form is closed while the lookup runs | A match that already arrived is stored (REQ-SLD-20); otherwise nothing is recorded |

## Out of scope

- Searching Deezer from the Songs list, a one-tap save, and a starter set of songs at first run
  (undecided; see the sibling item named under Goal).
- Sending lyrics back to the lyrics source (post-MVP item `../2026-11-16-send-corrected-lyrics-back-to-lrclib/`).
- A lookup on open for local-only songs; a "search lyrics again" button; a retry after *none found*.
- Storing the album title or the duration on the song; synced lyrics.
- Changing the search of catalog mode, of the Artists list, or the search delay of the lists.
- Blocking the deletion of a song. A song that nothing references can be deleted as today; blocking
  comes with the event-queue work (Helder, 2026-11-16).
- Removing an artist that is left without songs after a deletion.
- Showing the lyrics origin on screen.

## Supersedes (shipped specs are not edited)

| Shipped requirement | Change |
|---------------------|--------|
| REQ-SFT-04 note (remote row shows "artist · version · Deezer" so "Creep" and "Creep (Live)" stay apart) | **Replaced** by REQ-SLD-06 to 08: one row per song group, no version label. |
| REQ-SFT-12 (a Deezer pick fills Version with the normalised `title_version`) | **Replaced for Version** by REQ-SLD-08. Title, Artist and the pending identity are unchanged. |
| REQ-SFT-05 (library check on title, artist, version) | **Kept**, applied after grouping with a blank version (REQ-SLD-09). |
| REQ-SFT-37 ("No manual-edit flag … SHALL NOT exist") | **Narrowed**: a lyrics origin is stored for the Lyrics field only (REQ-SLD-26 to 28). No flag exists for any other field. |
| REQ-PIC-23, 26, 28, 31 (Artist text state with edit icon; a tap on Artist opens the editor and releases the pick; the artist of a stored song can be changed) | **Limited to local-only songs** by REQ-SLD-12, 13, 15. After a Deezer pick the Artist cannot be tapped or cleared, so releasing a picked artist by a tap or by X on Artist no longer happens while the pick is held. |
| REQ-PIC-27 (changing the Title text after a pick saves a song with no external identity) | **Kept, and extended** by REQ-SLD-14: the Artist becomes editable at that moment. |
| REQ-SFT-14 (X on Title: "the other field keeps its value and state"; X resets a Version the pick filled) | **Extended** by REQ-SLD-14: the Artist keeps its value; its state changes from plain text to the editable field. The Version reset no longer applies, because a pick fills no Version (REQ-SLD-08). |
| REQ-SFT-46 (one write path for a song) | **Extended**: the lookup on open writes lyrics and lyrics origin only, inside the song service, through the same unit of work. Lyrics are not part of any identity, duplicate or catalog rule, so no other check of the write path applies. |
| REQ-LYR-13 ("Nothing is stored before Save") | **Kept for the pick lookup in add mode.** The lookup on open stores a match at once (REQ-SLD-20). |
| REQ-LYR-14 (edit mode starts no lookup) and REQ-LYR "Out of scope" (no lyrics for songs already stored) | **Replaced** for Deezer songs with blank lyrics by REQ-SLD-19, 24, 28. |
