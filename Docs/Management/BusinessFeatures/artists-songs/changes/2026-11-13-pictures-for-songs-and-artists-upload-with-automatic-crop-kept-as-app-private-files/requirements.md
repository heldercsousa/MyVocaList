# Requirements: pictures and tap-to-edit names in the Song and Artist forms

> **Status: APPROVED 2026-11-13 under Helder's instruction of the same day** ("autonomously, with my
> authorization in advance to all needed steps … You decide"). Every decision taken for Helder is
> listed in `design.md § 1` so he can overrule it. The on-device check (`tasks.md` T10) stays his.

This spec covers six backlog items (see `tasks.md § Items`). Predecessor spec: the Deezer artwork item
`../2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/` (REQ-ART-NN).

## Goal

1. A song and an artist can have a picture chosen by the user, not only one that came from Deezer.
2. The Song form and the Artist form show that picture at the top, below the first field.
3. A defined Title, Artist or Name reads as text, larger than the rest of the form, and opens for
   editing when tapped.
4. The Artist of a stored song can be changed.
5. The Artists list shows the artist picture on each row, as the Songs list shows the song picture.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Picture** | The image of a song or an artist. |
| **Image reference** | The one value stored per song or artist (`ImageUrl` column): either an **image URL** (absolute `https`, at most 500 characters, as in REQ-ART-03) or a **local reference**. |
| **Local reference** | `local:<folder>/<name>`, where `<folder>` is `songs` or `artists` and `<name>` is 32 lowercase hexadecimal characters. It names two files of an uploaded picture. |
| **Uploaded picture** | A picture the user picked from the device. It is stored as two square JPEG files: the **thumbnail** (256 x 256 px) and the **large** file (512 x 512 px). |
| **Images folder** | `images/` inside the app's private data directory. Other apps and the device's file explorer cannot read it. |
| **Form picture** | What the picture field of a form shows: the picture the form would store on Save, or the placeholder when there is none. |
| **Placeholder** | `music_note_outlined` for a song, `account_circle_outlined` for an artist (unchanged from REQ-ART). |
| **Text state** | A field whose value is defined and is not being edited shows the value as text, with no input box. |
| **Editor state** | The field shows its input box (the autocomplete field). |
| **Option**, **pick**, **picked state**, **X** | As in the Song form title-first spec (REQ-SFT). |

## User stories

1. As an admin, I add my own picture to a song or an artist that Deezer does not know, so my lists
   are easy to scan.
2. As an admin, I replace a wrong picture, in a new record or a stored one.
3. As an admin, I see the name of the song and its artist as a heading once they are set, and I tap
   one to correct it.
4. As an admin, I fix the artist of a stored song without deleting the song.
5. As an admin, I do not want pictures to make the database or its backups grow.

## Acceptance criteria

### A. Image references

- **REQ-PIC-01: One stored value.** The picture of a song or an artist SHALL be stored as one image
  reference in the existing `ImageUrl` column. Image bytes are never written to the database. No
  column is added.
- **REQ-PIC-02: Valid references only.** A value that is neither an image URL nor a well-formed
  local reference SHALL be stored as null.
  Given `local:songs/../../x`, Then it is stored as null.
- **REQ-PIC-03: Display source by size.** For a reference and a size (thumbnail or large) the system
  SHALL give the source to display:
  - local reference → the path of the thumbnail file (`<images folder>/<folder>/<name>_t.jpg`) or of
    the large file (`…_l.jpg`);
  - image URL holding the Deezer size segment `/250x250-` → for the large size the same URL with
    `/500x500-`; for the thumbnail the URL unchanged;
  - any other image URL → the URL unchanged for both sizes;
  - null or invalid → null.
  No extra Deezer API request is made (REQ-ART-04 stays).
- **REQ-PIC-04: Deezer pictures stay remote.** A picture that came from Deezer SHALL stay an image
  URL, loaded from the network and kept by the platform image cache (REQ-ART-23). It is not copied
  into the images folder.

### B. Uploading a picture

- **REQ-PIC-05: Pick from the device.** The picture field SHALL let the user pick one photo from the
  device's photo library. Cancelling the pick changes nothing.
- **REQ-PIC-06: Automatic centred square crop.** The system SHALL crop the picked photo to the
  largest centred square, with no user step.
  Given a 4000 x 3000 px photo, Then both files hold the centre 3000 x 3000 px area, scaled down.
- **REQ-PIC-07: Two sizes, compressed.** The system SHALL write a thumbnail of 256 x 256 px and a
  large file of 512 x 512 px, both JPEG, quality 75 and 80. A photo whose shorter side is below the
  target is not enlarged: the file's side is that shorter side.
- **REQ-PIC-08: Orientation.** A photo taken with the phone turned SHALL be stored upright (the EXIF
  orientation is applied before the crop).
- **REQ-PIC-09: App-private files.** Both files SHALL be written inside the images folder, under
  `songs/` or `artists/`, with a new random name per upload.
- **REQ-PIC-10: Failure.** IF the photo cannot be read or decoded, or is larger than 25 MB, THEN no
  file is kept, the form picture is unchanged and the form shows one message ("Could not use this
  picture."). The form stays usable.
- **REQ-PIC-11: Orphan files are removed.** A file in the images folder that no stored song or
  artist references, and that is older than 24 hours, SHALL be deleted at app start. (Covers a
  replaced picture, a removed picture, a deleted record and a form closed without Save.) Files
  younger than 24 hours are never deleted by this sweep.

### C. Picture field (both forms)

- **REQ-PIC-12: Position.** The Song form SHALL show the picture field directly below Title. The
  Artist form SHALL show it directly below Name.
- **REQ-PIC-13: Shape and size.** The song picture is a 120 x 120 dp rounded square; the artist
  picture is a 120 x 120 dp circle. The form picture uses the large size (REQ-PIC-03).
- **REQ-PIC-14: Placeholder is the default.** WHEN there is no form picture, the field shows the
  placeholder at the same size and shape. This holds for a new record and for a stored record
  without a picture. WHILE a picture loads or WHEN it fails to load, the placeholder shows.
- **REQ-PIC-15: Actions.** With no form picture the field shows one button, "Add picture". With a
  form picture it shows "Replace picture" and "Remove picture". Tapping the picture itself does the
  same as the first button. Both work in add mode and in edit mode.
- **REQ-PIC-16: Size hint.** WHILE there is no form picture, the field shows below the picture the
  text "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre."
  The hint is hidden when there is a form picture.
- **REQ-PIC-17: Nothing is stored before Save.** Picking, replacing or removing a picture changes
  the form picture only. The record changes on Save.

### D. Song form: which picture is the form picture

- **REQ-PIC-18: Order.** The song's form picture is the first that exists of: (1) the picture
  uploaded in this form; (2) add mode: the pending song image of a Deezer pick (REQ-ART-11); edit
  mode: the stored picture of the song. The artist picture is never the form picture of a song.
- **REQ-PIC-19: An upload outlives picks.** A Deezer pick or X on Title made after an upload SHALL
  keep the uploaded picture as the form picture.
- **REQ-PIC-20: Remove.** "Remove picture" SHALL leave the form with no picture: the upload and the
  pending Deezer song image are dropped; in edit mode the stored picture is marked for removal. A
  later Deezer pick or upload sets a picture again.
- **REQ-PIC-21: Save stores the form picture.**
  - Add mode: the stored song's reference is the form picture's reference (null when none).
    "Save as new version" and "Save as new song" keep it (REQ-ART-15).
  - Edit mode: WHEN the user uploaded or removed a picture, the stored reference becomes the form
    picture's reference (null after Remove). Otherwise the stored reference is unchanged.
    (Replaces REQ-ART-17.)
- **REQ-PIC-22: A failed Save keeps the form picture.** As REQ-ART-18: no stored reference changes
  and the form keeps its picture for the next Save.

### E. Tap-to-edit text (Title, Artist, Name)

- **REQ-PIC-23: Text state.** WHILE a field's value is not blank, the field is not being edited and
  the field shows no error, the field SHALL show the value as text and hide its input box.
  - Title (Song form) and Name (Artist form): type style Headline Medium (28 sp).
  - Artist (Song form): type style Title Large (22 sp).
  - At most 3 lines, then an ellipsis. A trailing `edit_outlined` icon marks the text as editable.
- **REQ-PIC-24: Editor state.** WHILE the value is blank, or the field is being edited, or the field
  shows an error, the field SHALL show its input box as today.
- **REQ-PIC-25: Entering the text state.** The field enters the text state:
  - on page load with a defined value (edit mode, or add mode opened with a value);
  - when its input box loses focus with a non-blank value and no error;
  - when an option is picked.
- **REQ-PIC-26: Tap opens the editor.** WHEN the user taps the text, the field SHALL show its input
  box with the keyboard open and the cursor in it. This holds every time, including for a value in
  the picked state: the tap releases the picked state and **keeps the text** (unlike X, which
  empties it).
- **REQ-PIC-27: Releasing a picked Title (add mode).** After a Deezer pick, WHEN the Title is opened
  by a tap and saved:
  - with the text unchanged → the song keeps the Deezer identity (REQ-SFT-12);
  - with the text changed → the song is saved with no external identity. The pending song image is
    kept in both cases.
- **REQ-PIC-28: Releasing a picked Artist.** WHEN the Artist is opened by a tap, the local artist id
  is released and the Artist is resolved by its text on Save (REQ-SFT-18: exact match is reused
  silently, near-duplicates raise the sheet, otherwise the artist is created). With the text
  unchanged the same artist is used. The pending artist image of a Deezer pick is sent on Save only
  when the Artist text still equals the picked artist's name exactly.
- **REQ-PIC-29: Search rules are unchanged.** Title searches in add mode only (REQ-SFT-27). Artist
  searches local artists only (REQ-SFT-16). Name searches in add mode only.
- **REQ-PIC-30: Reusable.** The text-state behaviour SHALL be one shared component usable around any
  form editor. Its customisable parts: the text, the type style, the maximum lines, the edit icon
  (shown or hidden), and whether a tap releases the picked state.

### F. Song form: Artist in edit mode

- **REQ-PIC-31: The Artist of a stored song can be changed.** In edit mode the Artist opens by a tap
  (REQ-PIC-26) and accepts a pick or typed text. On Save the song is linked to the picked artist, or
  to the artist resolved from the text (REQ-PIC-28). A typed artist with no match is created.
  (Replaces the edit-mode lock of REQ-SFT-28. The song row is updated in place.)
- **REQ-PIC-32: No duplicate by changing the artist.** IF the change would give two songs the same
  artist, title and version, THEN Save is refused with a message on the form and nothing changes.

### G. Artist form

- **REQ-PIC-33: Name is the autocomplete field.** Name SHALL be the shared autocomplete field: in
  add mode it lists local artists first, then Deezer artists; a local pick opens that artist's edit
  form; a Deezer pick fills Name in the picked state. In edit mode it is plain editable text.
  The field does not stop typing at 60 characters; a longer Name is refused by the existing length
  rule and the counter (amended 2026-11-14).
  (Delivers the item `../2026-11-11-artistformpage-name-as-the-autocomplete-field-split-from-sft-6-3/`.)
- **REQ-PIC-34: Name options show the avatar.** Each Name option SHALL show a leading avatar
  (40 x 40 dp circle) with the artist's picture, or the placeholder, for local and Deezer rows.
- **REQ-PIC-35: The artist's form picture.** The first that exists of: (1) the picture uploaded in
  this form; (2) add mode: the picture of the picked Deezer artist; edit mode: the stored picture.
  REQ-PIC-19 and REQ-PIC-20 apply the same way (X on Name drops the Deezer picture, not an upload).
- **REQ-PIC-36: Save stores the artist picture.** Add mode: the new artist's reference is the form
  picture's reference. Edit mode: changed only when the user uploaded or removed a picture.
  A failed Save stores nothing.
- **REQ-PIC-37: Name as tap-to-edit text.** Name follows REQ-PIC-23 to 26. Releasing a picked Deezer
  Name by a tap keeps the text. The Deezer picture is the form picture only while the Name equals
  the picked name, so the form always shows what Save would store. The Artist form stores no
  external identity (REQ-SFT-49). (Amended 2026-11-14.)

### H. Lists and options

- **REQ-PIC-38: Artists list rows show the picture.** Each Artists list row SHALL show a leading
  avatar (40 x 40 dp circle) with the artist's picture or the placeholder, with and without search
  text and in the selected state. The checkbox moves to the trailing slot, after the catalog
  button, as on the Songs list. (Delivers the item
  `../2026-11-13-artists-list-rows-show-the-artist-picture-as-a-leading-avatar/`.)
- **REQ-PIC-39: Uploaded pictures show everywhere.** Songs list rows, Artists list rows and the
  options of Title, Artist and Name SHALL show an uploaded picture through its thumbnail file, by
  the same row-image rule as today (REQ-ART-06, 09, 19).
- **REQ-PIC-40: A Deezer pick persists the artist picture.** Song form: unchanged (REQ-ART-16)
  in add mode. A save in edit mode that creates an artist, or moves the song to an artist without
  a picture, stores the sent artist picture on that artist; an unchanged artist is never changed.
  The Song form sends no artist picture in edit mode (amended 2026-11-14).
  Artist form: REQ-PIC-36. No other path stores a Deezer picture.

## Validation rules

| Field | Rule | Where |
|-------|------|-------|
| `Song.ImageUrl`, `Artist.ImageUrl` | null, an image URL, or a local reference; anything else → null | Service write paths (REQ-PIC-02) |
| Picked photo | readable image, at most 25 MB | image store (REQ-PIC-10) |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Pick cancelled | Nothing changes (REQ-PIC-05) |
| Photo unreadable, not an image, over 25 MB | Message, form picture unchanged, no file kept (REQ-PIC-10) |
| A local reference whose files are missing (restore on another device) | Placeholder (REQ-PIC-14) |
| Save fails | Stored references unchanged, form picture kept (REQ-PIC-22, 36) |
| Form closed without Save after an upload | The files become orphans and are removed later (REQ-PIC-11) |

## Out of scope

- Manual crop (pan, zoom) with `dxe:ImageEdit`; taking a photo with the camera.
- Copying Deezer pictures into local files; backfill for old records (item
  `../2026-11-13-backfill-deezer-images-for-songs-and-artists-saved-before-the-artwork-item/`).
- Uploaded pictures inside backups (item `Docs/Management/BusinessFeatures/backup-restore/changes/2026-11-13-backups-include-uploaded-pictures/`).
- Remote artist search in the Song form Artist field (REQ-SFT-16 stays).
- Row actions rework of the Artists and Songs lists (overflow-menu items) and BUG-086.
- Version, Featured artists, Lyrics and YouTube URLs of the Song form: unchanged.

## Supersedes (shipped specs are not edited)

| Shipped requirement | Change |
|---------------------|--------|
| REQ-ART-05 ("URL only") | **Extended** by REQ-PIC-01/02: the column also holds a local reference. Bytes still never enter the database. |
| REQ-ART-17 (edit mode never changes the song image) | **Replaced** by REQ-PIC-21. |
| REQ-ART-11/13 (pending images) | **Extended** by REQ-PIC-18/19/20 (an upload outlives picks). |
| REQ-ART-24 (local artist suggestions carry no image) | **Replaced** by REQ-PIC-34. |
| REQ-SFT-14 (only X leaves the picked state) | **Extended** by REQ-PIC-26/27/28 (a tap on the text releases it and keeps the text). |
| REQ-SFT-28 (Artist locked in edit mode) | **Replaced** by REQ-PIC-31. |
