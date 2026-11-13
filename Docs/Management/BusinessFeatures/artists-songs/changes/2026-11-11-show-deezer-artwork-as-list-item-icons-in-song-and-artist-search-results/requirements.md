# Requirements: Deezer artwork in the Song form options and the Songs list

> **Status: APPROVED 2026-11-13 under Helder's instruction of the same day** ("take decisions by
> yourself … don't ask me anything … work in a worktree"). Every decision below was taken by the
> orchestrator and is listed in `design.md § 1` so Helder can overrule any of them afterwards.
> The on-device check (tasks.md T11) remains Helder's.

Parent item: `README.md` (same folder). Evidence for every Material Design 3 statement:
`findings.md` (same folder; verbatim quotes with URLs, captured 2026-11-13).

## Goal

Rows that represent a song or an artist show a picture, so the user tells them apart at a glance:

- the options of the Song form's Title and Artist fields (`AutocompleteDXField`);
- the rows of the Songs list (with and without search text, plain and catalog mode).

The pictures come from Deezer. Title options also show more text that separates one row from the next.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Song image** | The cover of the album a Deezer track belongs to (`album.cover_medium`, 250 x 250 px). Deezer has no picture per track. |
| **Artist image** | The Deezer picture of an artist (`artist.picture_medium`, 250 x 250 px). |
| **Image URL** | An absolute `https` URL of at most 500 characters holding a song image or artist image. Anything else (null, empty, whitespace, not absolute, not `https`, longer) is **no image**. The host is not checked. |
| **Row image** | What a song row shows: the song image; if there is none, the artist image; if there is none, the placeholder. |
| **Placeholder** | The icon shown in the image's place: `music_note_outlined` for a song row, `account_circle_outlined` for an artist row. |
| **Pending image** | An image URL the Song form holds between a Deezer pick and Save. Nothing is stored before Save. |
| **Option**, **Field**, **picked state**, **X** | As in the Song form title-first spec (`../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/requirements.md`). |

## User stories

1. As an admin adding a song, I see the cover next to each Title option, so I pick the right
   recording without reading every line.
2. As an admin adding a song, I see which album a Deezer option comes from, so two options with the
   same title, artist and version no longer look identical.
3. As an admin browsing or searching the Songs list, I see each song's cover (or its artist's
   picture) and the artist name, so I find a song faster.
4. As an admin, I keep working when the device is offline or a picture fails to load: a placeholder
   icon takes the picture's place and nothing else changes.

## Acceptance criteria

### A. Data from Deezer

- **REQ-ART-01: Track search carries images and album.** For each track of a Deezer track search,
  the provider result SHALL carry the song image URL (`album.cover_medium`), the artist image URL
  (`artist.picture_medium`) and the album title (`album.title`). A missing `album` or `artist`
  object, or a missing field, gives null for that value and does not fail the search.
- **REQ-ART-02: Artist search carries the image.** For each artist of a Deezer artist search, the
  provider result SHALL carry the artist image URL (`picture_medium`).
- **REQ-ART-03: Only image URLs are kept.** A value that is not an image URL (see vocabulary) SHALL
  be treated as no image (null) at the provider boundary.
  Given Deezer returns `cover_medium: ""` for a track, Then that result has no song image.
- **REQ-ART-04: No extra request.** Images SHALL come from the search response already made. No
  additional Deezer API call is made to obtain an image.

### B. Storage

- **REQ-ART-05: URL only.** `Song` and `Artist` each gain a nullable `ImageUrl` (at most 500
  characters). The system stores the URL only; image bytes are never written to the database.
  Existing rows have `ImageUrl` null after the migration.

### C. Title options (Song form, add mode)

- **REQ-ART-06: Every Title option has a leading image.** Each Title option SHALL show a leading
  image (56 x 56 dp, square) holding its row image.
  - Local option: the stored song's `ImageUrl`; else its artist's `ImageUrl`; else the placeholder.
  - Deezer option: the track's song image; else its artist image; else the placeholder.
- **REQ-ART-07: Supporting text separates rows.** The supporting text of a Title option SHALL be
  the non-empty parts of `artist · version · album`, joined by `" · "`.
  - `album` is present only on Deezer options, and is omitted when it equals the option's title
    ignoring case only (ordinal comparison; accents count as different). A single is often named
    after its song. This is display text, not search or de-duplication.
  - Given Deezer returns "Commando" by Ramones, version "1999 Remaster", album "Greatest Hits",
    Then the supporting text is "Ramones · 1999 Remaster · Greatest Hits".
  - Given a local song "Don't Cry" by Guns N' Roses, version "", Then the supporting text is
    "Guns N' Roses".
- **REQ-ART-08: The source is the trailing text.** A Title option SHALL show its source as trailing
  text: `Library` for a local option, `Deezer` for a Deezer option. The source is no longer part of
  the supporting text. (Replaces the marker placement of REQ-SFT-04; order and limits of REQ-SFT-04
  are unchanged.)

### D. Artist options (Song form)

- **REQ-ART-09: Every Artist option has a leading avatar.** Each Artist option SHALL show a leading
  avatar (40 x 40 dp, circle) holding the artist's `ImageUrl`, or the placeholder when there is none.
- **REQ-ART-10: Artist option supporting text shows the real catalog count.** The supporting text
  SHALL be the artist's catalog count text computed from the stored catalog entries.
  Given artist "Ramones" has 3 catalog entries, Then its option's supporting text is the text
  `ArtistListItem.CatalogCountText` gives for 3. (Today the search projects a count of 0, so every
  option reads "No catalog".)

### E. Picks and what they overwrite (Song form)

REQ-SFT-10, 12, 13 and 14 stay in force. These criteria add the image rules.

- **REQ-ART-11: A Deezer pick sets the pending images.** Given add mode, When the user picks a
  Deezer option, Then the pending song image is that track's song image (not the row-image
  fallback) and the pending artist image is that track's artist image. Either may be null.
  A later Deezer pick replaces both.
- **REQ-ART-12: A pick never touches the other fields.** A Deezer pick SHALL leave Featured
  artists, Lyrics and the karaoke URLs unchanged, and SHALL write nothing to the database.
- **REQ-ART-13: X clears the pending images.**
  - X on Title after a Deezer pick clears the pending song image and keeps the pending artist image
    (the Artist still holds the Deezer artist).
  - X on Artist after a Deezer pick clears both the pending song image and the pending artist image.
- **REQ-ART-14: A local pick is unchanged.** A local Title pick opens that song (REQ-SFT-10) and
  its form starts with no pending image. A local Artist pick sets the Artist only and changes no
  pending image. Neither changes a stored image.
  Given a Deezer pick, When the user taps X on Artist, picks a local artist and saves, Then the
  stored song has no `ImageUrl` and that artist's `ImageUrl` is unchanged (X on Artist cleared both
  pending images, REQ-ART-13).

### F. Save (Song form)

- **REQ-ART-15: A new song stores its image.** Given add mode and a pending song image, When Save
  succeeds, Then the stored song's `ImageUrl` is the pending song image. This holds also when the
  user chose "Save as new version" or "Save as new song" (the external identity is dropped there,
  the image is kept). With no pending song image the stored `ImageUrl` is null.
- **REQ-ART-16: The artist image fills an empty artist only.** Given a pending artist image, When
  Save succeeds:
  - an artist **created** by this Save gets the pending artist image as `ImageUrl`;
  - an **existing** artist that the Save uses because the Artist field holds the Deezer artist
    (picked as a local artist by REQ-SFT-13, or matched by name at Save, REQ-SFT-18 step 1) and
    whose `ImageUrl` is null gets the pending artist image;
  - an existing artist whose `ImageUrl` is not null is never changed;
  - an existing artist chosen by the user in the near-duplicate sheet (REQ-SFT-19) is never
    changed (it may be a different artist).
- **REQ-ART-17: Edit mode never changes the song image.** Saving a song in edit mode SHALL keep its
  stored `ImageUrl`, whatever was edited, including the Artist.
- **REQ-ART-18: A failed Save stores no image and keeps the pending images.** WHEN Save fails, is
  refused, or stops at a sheet (exact duplicate, possible duplicates, near-duplicate artist), no
  `ImageUrl` of any song or artist is changed, and the form keeps its pending images, so a later
  Save from the same form (including "Save as new version" and "Save as new song", or after
  closing a sheet with Back) still stores them. "Open existing" leaves the form; its pending images
  are discarded with it.

### G. Songs list

- **REQ-ART-19: Every Songs list row has a leading image.** Each row SHALL show a leading image
  (56 x 56 dp, square) holding the row image: the song's `ImageUrl`; else its artist's `ImageUrl`;
  else the placeholder. This holds in plain mode and catalog mode, with and without search text,
  and in the selected state.
- **REQ-ART-20: The supporting text names the artist.** The supporting text of a Songs list row
  SHALL be the non-empty parts of `artist · version · feat. <featured artists>`, joined by `" · "`.
  Given "Creep" by Radiohead, version "Live", featured artists empty, Then the supporting text is
  "Radiohead · Live". Given featured artists "Thom Yorke", Then "Radiohead · Live · feat. Thom Yorke".
  (Delivers the pending item `../2026-11-09-song-list-item-shows-artist-name-version-and-featured-artists/`.)
- **REQ-ART-21: Row height.** A Songs list row SHALL be at least 72 dp high with the image
  vertically centred (8 dp above and below the image; project decision, `design.md` D18).

### H. Loading, offline and failure

- **REQ-ART-22: The placeholder covers every gap.** WHILE an image is loading, and WHEN a row has no
  image URL, the device is offline or the load fails, the row SHALL show its placeholder in the
  image's place, at the same size and shape. No message, retry control or error is shown, and
  search, pick and Save are unaffected.
- **REQ-ART-23: Loaded images are reused.** An image shown once SHALL be served from the platform
  image cache on later displays while the cache entry is valid, so a row seen before can show its
  image offline.

### I. Artist form pipeline (Service level only)

- **REQ-ART-24: Remote artist suggestions carry the image.** `ArtistSuggestionDto` SHALL carry the
  artist image URL for Deezer rows (null for local rows until the Artist form item uses stored
  images). No Artist form UI changes in this item.

## Validation rules

| Field | Rule | Where |
|-------|------|-------|
| `Song.ImageUrl`, `Artist.ImageUrl` | null, or an absolute `https` URL of at most 500 characters; anything else is stored as null | provider boundary (REQ-ART-03) and the Song write path |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Deezer result without `album`, `artist` or an image field | That value is null; the row falls back per the row-image rule (REQ-ART-01/06) |
| Image URL does not load (offline, 404, timeout) | Placeholder stays (REQ-ART-22) |
| Image URL longer than 500 characters or not `https` | Treated as no image (REQ-ART-03) |
| Save fails | No image is stored (REQ-ART-18) |

## Out of scope (registered or left to existing items)

- **Images for songs and artists saved before this item, or typed without a Deezer pick.** They show
  the artist image when their artist has one, else the placeholder. A backfill from Deezer is a
  separate follow-up item (registered at close-out).
- **Artists list rows** (`ArtistsPage`): its row layout is being reworked by the row-actions items
  and BUG-086; the avatar there is a follow-up item (registered at close-out).
- **Artist form** UI (Name as the Field): owned by
  `../2026-11-11-artistformpage-name-as-the-autocomplete-field-split-from-sft-6-3/`. REQ-ART-24 only
  prepares its data.
- **Showing the cover inside the Song form**, version de-duplication of Deezer rows, lyrics: owned by
  the discussion item `../2026-11-11-discuss-song-identity-by-lyric-version-de-duplication-and-richer-deezer-data-in-the-song-ux/`.
- **Remote artist search in the Song form Artist field**: stays local only (REQ-SFT-16).
- **Person form** options: unchanged (no image, no trailing text).

## Dependencies

1. Component change `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/` (REQ-ART-06/09/19/22).
2. Component change `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/` (REQ-ART-06/08/09).

## Supersedes (shipped specs are not edited)

| Shipped requirement | Where | Change |
|---------------------|-------|--------|
| REQ-SFT-04 (source marker inside the supporting text: "artist · version · In your library / Deezer") | `../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/requirements.md` | **Replaced** for content by REQ-ART-07/08 (album added, source moves to the trailing text). Order and limits kept. |
| REQ-SFT-12 (what a Deezer pick fills) | same | **Extended** by REQ-ART-11/12 (pending images). |
| REQ-SFT-14 (X after a Deezer pick) | same | **Extended** by REQ-ART-13. |
