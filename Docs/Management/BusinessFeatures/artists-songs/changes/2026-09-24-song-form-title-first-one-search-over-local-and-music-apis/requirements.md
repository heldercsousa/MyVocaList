# Requirements: Song form title-first, one search over local and music APIs

> **Status: DRAFT for Helder's review (2026-09-24).** Written from Helder's five rounds of decisions
> (`README.md`, same folder) and the read-only evaluation `findings.md` (same folder).
> Items marked `[OPEN — Helder decides]` or `[Proposed — confirm]` are not decided.
>
> Dated change spec under `artists-songs`. The shipped specs it replaces or narrows are immutable and
> are **not** edited (§ Supersedes). Design: `design.md`. Tasks: `tasks.md`.
>
> Length note: this item is larger than the 2-page guideline because it folds four bugs, a
> tracking-field removal, two page deletions and the Artist-form name search into one redesign by
> Helder's decisions (README, third to fifth rounds).

## Goal

The Song form opens with **Title**. In add mode, Title is one search box over the local library and
Deezer. Picking a local song opens it for editing. Picking a Deezer song fills Title and Artist.
Typing without picking still works. A typed artist that is not in the library is created on Save.
The Artist form's Name gets the same kind of search over local and Deezer artists. The
API-edit-tracking machinery (`HasManualEdits`, merge sheet) and both picker pages are removed.

## Domain vocabulary

| Term | Definition |
|------|------------|
| **Field** | The shared autocomplete component `AutocompleteDXField` (MD3: editable exposed dropdown menu), specified by the dependency item `DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`. |
| **Option** | One row in a Field's menu. |
| **Local option** | An option for a song already stored in the library. Shows the "In your library" marker. |
| **Remote option** | An option for a Deezer track that is not in the library. Shows its source ("Deezer"). |
| **Online-search note** | The text "Online search unavailable" shown in the Field's own supporting text (`AutocompleteDXField.SupportingText`, REQ-ACF-12c: the line under the field, replaced by the error text while the field has an error). Not an option row. |
| **Option supporting text** | The second line of one option (`AutocompleteSuggestion.SupportingText`). Not the same thing as the Field's supporting text. |
| **Save outcome** | The result of `ISongService.SaveFromFormAsync`: `Saved`, `Failed`, `ArtistChoiceNeeded` or `SongChoiceNeeded` (`design.md` § 3.5). |
| **Field states** | As defined by the dependency item: **Editable**; **Locked** (after a pick; text cannot change; X unlocks); **Read-only** (`IsReadOnly`, page-set: text shown, no X, no search; wins over the others). `IsSearchEnabled = false` turns option requests off while the field stays a text field. |
| **Pick / X** | As defined by the dependency item (REQ-ACF-01…09). |
| **External identity** | The pair `(ExternalProvider, ExternalId)` stored on a `Song`, e.g. `("Deezer", "3135556")`. |
| **Pending external identity** | The external identity of a remote option picked in the current form, not yet saved. |
| **API-sourced song** | A stored song whose `ExternalId` is not null. |
| **Manual song** | A stored song whose `ExternalId` is null. |
| **Collated match** | Equality under the DB collation `NOCASE_NOACCENT` (`EF.Functions.Collate`), never C#-side. |
| **Near-duplicate artist** | An existing artist that is not a collated match for the typed name but scores ≥ `SimilarityConstants.DefaultThreshold` (0.82) against it with the existing `ISimilarityScorer`. |
| **Unit of work** | One `IUnitOfWork.ExecuteAsync` call: one `AppDbContext`, one transaction, all-or-nothing. |

## User stories

- **US-1:** As the admin adding a song, I want to type a title, an artist, or both into Title and see
  matching songs from my library and from Deezer, so that I find the song whatever I remember of it.
- **US-2:** As the admin, I want picking a song I already have to open it, so that I never create a
  duplicate by accident.
- **US-3:** As the admin, I want picking a Deezer song to fill Title and Artist for me, so that I
  don't have to type them.
- **US-4:** As the admin adding a local band's unrecorded song, I want to type the title and the
  artist freely and have the artist created when I save, so that I don't need the Artist form first.
- **US-5:** As the admin, I want to be asked before a near-duplicate artist is created, so that
  "Metalica" doesn't sit beside "Metallica".
- **US-6:** As the admin, I want typos such as "dont cry" or "don cry" to still find "Don't Cry".
- **US-7:** As the admin editing a song, I want Title and Artist protected in the way that fits the
  song's origin, so that an imported title isn't changed by mistake.

## Acceptance criteria

### A. Layout

- **REQ-SFT-01: Field order.** In add and edit mode the form shows, top to bottom: Title, Artist,
  Version, Featured Artists, Lyrics, then the YouTube section (unchanged). The "Search music
  database" row is absent (`SongFormPage.xaml:122-137` today).

### B. Title search (add mode only)

- **REQ-SFT-02: Title is the search box.** WHILE the form is in add mode, Title SHALL be the Field.
  The typed text is matched against song title **and** artist name, so "guns cry" can find
  "Don't Cry – Guns N' Roses".
- **REQ-SFT-03: Thresholds.** Local options are requested from `SearchConstants.MinimumLocalQueryLength`
  (2) trimmed characters; Deezer is queried only from `SearchConstants.MinimumRemoteQueryLength` (3).
  The debounce is the shared `SearchConstants.AutocompleteDebounceMilliseconds` (300). No literal
  values in the page or ViewModel.
- **REQ-SFT-04: Option content and order.** Local options come first (at most
  `SearchConstants.MaxSearchResults` = 5), then remote options (at most 5). The marker and the source
  are part of the **option supporting text**; the shared Field needs no new option property.
  - Local option: headline = title; supporting text = "artist · version · In your library" (the
    " · version" part is omitted when Version is empty).
  - Remote option: headline = title; supporting text = "artist · Deezer".
  Given the library holds "Don't Cry" (Version "") and "Don't Cry" (Version "Live") by Guns N' Roses,
  When the user types "don't cry",
  Then two local options appear, with supporting texts "Guns N' Roses · In your library" and
  "Guns N' Roses · Live · In your library".
- **REQ-SFT-05: One row per song.** IF a Deezer result already exists locally, THEN it SHALL appear
  once, as a local option, never as a remote option. "Exists locally" means, in this order:
  (a) a stored song has the same external identity; else
  (b) a stored song is a collated match on title **and** artist name, with Version "".
  A song found by (a) or (b) that is not already among the local options replaces the remote row
  in place of it, as a local option.
- **REQ-SFT-06: Offline or API error.** IF Deezer fails (no network, HTTP error, timeout per
  REQ-SFT-08, unreadable response, or an HTTP 200 response carrying an `error` object, e.g. over
  quota), THEN the local options SHALL still show and the Field's supporting text SHALL read
  "Online search unavailable" until the next query whose Deezer call succeeds, or until Title is
  emptied. No option row, snackbar or dialog. The failure is logged at Warning. Deezer returning zero
  results is not a failure and shows no note.
- **REQ-SFT-07: Local options do not wait for Deezer.** WHEN a query is requested, the local options
  SHALL be shown as soon as the local search returns, without waiting for Deezer; remote options are
  appended when Deezer answers, if the query is still current. This needs one capability from the
  shared Field (append options to the current menu, `design.md` § 3.4). If the dependency item
  rejects it, the fallback is: the Field shows local and remote together once Deezer answers or the
  REQ-SFT-08 bound expires, so local options wait at most 3,000 ms (Q16).
- **REQ-SFT-08: Remote wait bound.** A Deezer call that has not completed within 3,000 ms
  `[Proposed — confirm]` SHALL be cancelled and treated as a failure (REQ-SFT-06).
- **REQ-SFT-09: Latest query wins.** Options from an earlier, slower query SHALL never replace options
  of a later query (carry-over of REQ-ACREATE-13 / REQ-ACF-26).

### C. Picks

- **REQ-SFT-10: Local pick opens the song.** Given add mode, When the user picks a local option,
  Then the add form is replaced by that song's edit form (Back from it returns to the page the add
  form was opened from, not to the add form).
- **REQ-SFT-11: Unsaved data on a local pick** `[Proposed — confirm]`. IF, at the moment of a local
  pick, Artist, Version, Featured Artists, Lyrics or the buffered YouTube list holds user input,
  THEN a `dx:BottomSheet` SHALL ask "Open '<title>'? What you entered here will be discarded." with
  **Open** and **Keep editing**. Open → REQ-SFT-10. Keep editing (or Back / scrim) → the sheet closes,
  Title shows the text the user had typed, unlocked, and nothing else changes. With no such input,
  REQ-SFT-10 runs with no sheet.
- **REQ-SFT-12: New Deezer pick fills the form.** Given add mode, When the user picks a remote option,
  Then: Title = the provider's title exactly as returned (no trimming of "(Remastered 2011)" etc.);
  Artist = the provider's artist name; the pending external identity = `("Deezer", <track id>)`;
  Title and Artist are both in the picked state; Version, Featured Artists and Lyrics are unchanged;
  **nothing is written to the database**.
- **REQ-SFT-13: Artist of a Deezer pick.** IF the provider's artist name is a collated match for a
  local artist, THEN Artist is picked as that local artist. Otherwise Artist is picked with the
  provider's name and no local id, and the artist is created on Save (REQ-SFT-18).
- **REQ-SFT-14: X after a Deezer pick.** When the user taps X on Title **or** on Artist after a remote
  pick, the pending external identity SHALL be cleared, because the record is no longer the picked
  Deezer song `[Proposed — confirm for Artist]`. X on Title also empties and unlocks Title; X on
  Artist also empties and unlocks Artist. The other field keeps its value and state.

### D. No pick

- **REQ-SFT-15: Typed text is the title.** When the user leaves Title without picking, the typed text
  stays and is the title. Save applies the existing title rules only (required; ≤ 100 characters,
  `SongService.ValidateTitleInput`). There is no other handling of unpicked title text.

### E. Artist

- **REQ-SFT-16: Artist field.** Artist SHALL be the Field over **local artists only**, in the picked
  state after a pick, with no "Add … as a new artist" row and no remote search.
- **REQ-SFT-17: Blur keeps text, no error.** When Artist loses focus holding unpicked text, the text
  stays and **no** error is shown. ("Search and select an artist from the list" is removed.)
- **REQ-SFT-18: Artist with no local id on Save.** Given Artist is non-empty and has **no local id**
  (typed and not picked, **or** filled by a Deezer pick with no local match, REQ-SFT-13), When the
  user saves, Then the Service decides, in this order:
  1. a collated match exists → that artist is used, with no prompt `[Proposed — confirm]`;
  2. else a near-duplicate exists → REQ-SFT-19 (this applies to a Deezer-picked artist too);
  3. else a new artist with that name is created **in the same unit of work as the song**. If the
     song write fails, no artist row remains.
- **REQ-SFT-19: Near-duplicate guard.** IF step 2 of REQ-SFT-18 applies, THEN one `dx:BottomSheet`
  SHALL show "Similar artist found", one row "Use '<existing>'" per near-duplicate (at most 3,
  highest score first), and one action "Create '<typed>'". Use → the song is saved with that artist.
  Create → the save continues with a new artist. Back or scrim → nothing is saved and the form is
  unchanged. The sheet SHALL NOT appear in any other case. Near-duplicate candidates are retrieved
  from the artist-name trigram index (REQ-SFT-34), not by first-word prefix.
  Given "Metallica" is stored and no artist named "Metalica" exists,
  When the user types "Metalica" as the artist (not picked) and saves,
  Then the sheet offers "Use 'Metallica'" and "Create 'Metalica'", and nothing is saved yet.
  (Today this case creates "Metalica" silently: the pool is `LIKE 'Metalica%'`,
  `ArtistResolutionService.cs:183-195` via `DerivePrefixToken`, which cannot contain "Metallica".)
- **REQ-SFT-20: Created-artist validation.** The created artist obeys `IArtistService` rules
  (non-empty after trim, ≤ 60 characters, no collated duplicate). A violation shows the service
  message on Artist and saves nothing.
- **REQ-SFT-21: Artist required.** Save with an empty or whitespace Artist shows "Artist is required"
  and saves nothing (unchanged, AC-10.4).

### F. Save-time song duplicate guard (simplified resolution sheet)

- **REQ-SFT-22: Exact duplicate.** Given the artist is an existing artist, When Save finds a stored
  song that is a collated match on (artist, title, version), Then a `dx:BottomSheet` offers
  **Open existing** and **Save as new version**. Open existing → REQ-SFT-10 navigation (no extra
  prompt; the user chose it). Save as new version → requires a non-empty Version (AC-1.2 validation
  and message unchanged).
- **REQ-SFT-23: Possible duplicates** `[Proposed — confirm]`. Given no exact duplicate, When Save finds
  similar titles for the same artist (existing fuzzy step, score ≥ 0.82), Then the sheet lists them
  (tap a row → open that song, REQ-SFT-10) and offers **Save as new song**, which saves with no
  Version requirement.
- **REQ-SFT-24: No overwrite.** No path SHALL overwrite a stored song with values typed or picked in
  an add form. ("Update existing" is removed.)
- **REQ-SFT-25: A new artist does not skip the song guard.** WHEN the artist is being created by
  this Save, the song duplicate guard SHALL still run. Its artist-scoped checks cannot match, but the
  external-identity check can: Given "Don't Cry" is stored with Deezer id 3135556 under the artist
  "Guns N Roses", When the user picks that Deezer song, which fills a new artist "Guns N' Roses",
  and saves, Then the exact-duplicate sheet (REQ-SFT-22) offers Open existing, and no artist or song
  is created unless the user chooses to.
- **REQ-SFT-26: External identity is unique.** IF a save would give a second song the same external
  identity, THEN it SHALL fail with "This music-database song is already in your library" and save
  nothing. The database enforces the same rule (REQ-SFT-39).

### G. Edit mode

- **REQ-SFT-27: Title in edit mode.** WHILE in edit mode, Title SHALL NOT search (the Field with
  `IsSearchEnabled = false`). For an API-sourced song, Title SHALL be **Read-only** (`IsReadOnly = true`:
  no typing, no X); notes such as "live" go in Version. For a manual song, Title SHALL be Editable.
- **REQ-SFT-28: Artist in edit mode.** The stored artist loads in the picked state with no search
  (REQ-ACF-03). After X, REQ-SFT-16…21 apply unchanged, including create-on-Save. The changed artist
  is persisted (REQ-ACREATE-16 kept).
- **REQ-SFT-29: Edit-save uniqueness includes Version** (absorbed bug). Given a stored song "Creep"
  (Version "") by Radiohead, When the user edits another Radiohead song to Title "Creep", Version
  "Acoustic" and saves, Then the save succeeds. Saving it with Version "" fails with the existing
  duplicate message.

### H. Typo tolerance

- **REQ-SFT-30: Local typos.** Given a library holding "Don't Cry – Guns N' Roses", "Cry – Cyndi
  Lauper", "Boys Don't Cry – The Cure" and "Dont Look Back in Anger – Oasis", When the user types
  "dont cry", or "don cry", Then "Don't Cry – Guns N' Roses" is the **first** local option.
- **REQ-SFT-31: Local accents and case.** Given "Não Sei – Ana Carolina" is stored, When the user types
  "nao sei" or "NAO SEI", Then it is a local option.
- **REQ-SFT-32: Artist words find songs.** Given the library above, When the user types "guns cry",
  Then "Don't Cry – Guns N' Roses" is the first local option.
- **REQ-SFT-33: Short queries.** WHEN the trimmed query has no word of 3 or more characters (e.g.
  "ab", "a b"), the system SHALL return local songs whose title starts with the query (collated), as
  today.
- **REQ-SFT-34: Index stays in step.** The local song search and the artist near-duplicate search
  SHALL reflect every song create, update and delete (including bulk delete), every artist create,
  rename and delete, from the next query after the write commits.
- **REQ-SFT-35: Deezer query shape.** The system SHALL send Deezer the typed text as a plain query,
  with no `track:`/`artist:` qualifiers.
- **REQ-SFT-36: MusicBrainz** `[OPEN — Helder decides; recommendation: drop]`. The title search and
  the Artist-form search SHALL NOT query MusicBrainz; Deezer is the only remote provider. Rationale in `design.md` § Decision: MusicBrainz.

### I. Tracking-field simplification (approved, README third round item 4)

- **REQ-SFT-37: No manual-edit flag.** `Song.HasManualEdits` and `Artist.HasManualEdits` SHALL NOT
  exist in the model, the database, DTOs or service signatures.
- **REQ-SFT-38: No merge sheet.** No merge sheet, field-diff list or per-field accept SHALL exist.
- **REQ-SFT-39: Unique external identity index.** The database SHALL reject a second song with the
  same non-null `(ExternalProvider, ExternalId)`. Existing duplicates are resolved by the migration:
  the song with the lowest `Id` keeps the identity; the others keep their data and lose only the
  identity (set to null). No song row is deleted.
- **REQ-SFT-40: Artist external columns frozen.** `Artist.ExternalId/ExternalProvider` stay in the
  schema. No flow in this item reads them. Nothing writes them unless Q10 (REQ-SFT-49) decides so.

### J. Absorbed bugs (README third round item 6)

- **REQ-SFT-41: Artist lookup never uses the song's id.** WHEN the system looks up or resolves an
  artist for a song save, it SHALL NOT pass the song's external identity as the artist's
  (today `SongFormViewModel.cs:631-634`). Test: a stored artist whose `ExternalId` equals the song's
  Deezer track id is **not** selected by that id.
- **REQ-SFT-42: Accent-only exact match carries the id.** Given "Não Sei" by Ana Carolina is stored,
  When the user saves "Nao Sei" for Ana Carolina, Then the exact-duplicate sheet (REQ-SFT-22) shows
  "Não Sei" and Open existing opens that song (today: no candidate and a silent no-op,
  `SongResolutionService.cs:123-127`, `SongFormViewModel.cs:650-654,709`).
- **REQ-SFT-43: Merge-sheet empty write.** Removed together with the merge sheet (REQ-SFT-38); after
  this item no path compares an import to a stored song and writes it (REQ-SFT-24).
- **REQ-SFT-29** covers the fourth bug (edit-save uniqueness ignores Version).

### K. Removed surfaces (Helder ruling 2026-09-24: pickers never worked)

- **REQ-SFT-44: Picker pages removed.** `SongPickerPage` and `ArtistPickerPage` SHALL be deleted with
  their ViewModels, routes, messages, DI registrations and tests, and the "Search music database"
  rows on `SongFormPage` and `ArtistFormPage`. Third-party search exists only inside Fields.
  The solution builds with 0 errors and a census finds no reference (`design.md` § 9).
- **REQ-SFT-45: No inline artist create.** The inline "Add … as a new artist" path and its command
  SHALL be removed from the Song form.
- **REQ-SFT-51: Save decisions live in the Service.** Every Song-form Save SHALL call one Service
  method that applies REQ-SFT-18/19/22/23/25/26 and returns a save outcome: `Saved`, `Failed(message,
  field)`, `ArtistChoiceNeeded(typed name, candidates)` or `SongChoiceNeeded(exact?, candidates)`.
  The ViewModel only maps `ArtistChoiceNeeded`/`SongChoiceNeeded` to BottomSheets and calls the method
  again with the user's choice. Each rule is tested at the Service (Level A); VM tests check the
  mapping only.
- **REQ-SFT-46: One write path.** Every song create and update from the Song form, including the
  artist it creates, SHALL go through `ISongService.CreateSongWithUrlsAsync` or
  `ISongService.UpdateSongAsync`, so CATALOG-PROP's propagation cannot be bypassed.

### L. Artist form name search (confirmed by Helder, fifth round)

- **REQ-SFT-47: Name is a Field.** On the Artist form in **add mode**, Name SHALL be the Field over
  local artists and Deezer artists, with the REQ-SFT-03/06/07/08/09 thresholds and failure rules.
  Local options come first (marker "In your library"), then Deezer options (supporting text
  "Deezer"). In edit mode Name is the Field with `IsSearchEnabled = false`, Editable (rename unchanged).
- **REQ-SFT-48: Local artist pick opens it.** Picking a local artist opens that artist's edit form,
  replacing the add form (dedup; carry-over of REQ-FORMUX-09's intent). If a Deezer artist's name is
  a collated match for a local artist, it is shown once, as the local option.
- **REQ-SFT-49: Deezer artist pick fills the name only.** Picking a Deezer artist fills Name with the
  provider's name and puts it in the picked state; X empties and unlocks it. Nothing is written
  before Save; Save uses the existing create rules (non-empty, ≤ 60, no collated duplicate).
  **Storing the Deezer artist id** `[OPEN — Helder decides]`: recommended **not stored**. Reason: no
  flow reads `Artist.ExternalId` (REQ-SFT-40, README third round item 4), song-side dedup uses the
  collated name, and a write-only column would revive the machinery just removed. Revisit only when
  an artist re-import or sync feature needs it.
- **REQ-SFT-50: No typed-name duplicate on the Artist form.** Unpicked typed text is saved as today
  (`ArtistService.CreateArtistAsync`, collated duplicate rejected with the existing message).

## Validation rules

| Field | Rule | Source (unchanged unless noted) |
|-------|------|------|
| Title | required; ≤ 100 chars after trim | `SongService.ValidateTitleInput` |
| Version | optional; ≤ 60; required for "Save as new version" | `SongService.ValidateVersionInput` |
| Artist | required; typed name ≤ 60, non-empty after trim, no collated duplicate when created | `IArtistService` |
| Song identity | unique `(ArtistId, Title, Version)` collated, on create **and** update | DB index + REQ-SFT-29 (update: changed) |
| External identity | unique `(ExternalProvider, ExternalId)` when both non-null | REQ-SFT-26/39 (new) |

## Failure modes

| Situation | Required behaviour |
|-----------|--------------------|
| Deezer unreachable, HTTP error, timeout, bad JSON, HTTP 200 with an `error` object (quota) | Local options + Field supporting text "Online search unavailable" (REQ-SFT-06); Save unaffected |
| Deezer returns 0 results | Local options only; no note |
| Artist create fails inside Save | Nothing saved; message on Artist (REQ-SFT-20) |
| Song write fails after the artist was staged | Unit of work rolls back both; the existing save-error path shows the message |
| Near-duplicate sheet or resolution sheet dismissed | No save; form unchanged |
| Navigation to the edit form fails (song deleted meanwhile) | Edit form loads empty and logs a warning, as today (`SongFormViewModel.cs:440-444`) |
| Local search index missing (migration not applied) | Local search returns no FTS results and logs an Error; the prefix path (REQ-SFT-33) still works |

## Out of scope

- **CATALOG-PROP** (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`): catalog rows on
  song writes. This item only guarantees REQ-SFT-46.
- The YouTube section, `YouTubeSearchPage` and the "Search YouTube (API)" row stay exactly as they
  are, pending a separate discussion on the YouTube API-key requirement (Helder, fifth round).
- The shared Field's internals (lock mechanism, X styling, debounce plumbing): dependency item.
- `SongPickerPage`'s per-row "YouTube" button (`SongPickerViewModel.cs:100-106`, `SongPickerPage.xaml:66`)
  disappears with the picker (REQ-SFT-44). The YouTube page itself is untouched.
- Artist-form duplicate suggestions stub (`ArtistFormPage.xaml:46-51`), Person form.
- Re-syncing stored songs from Deezer; lyrics providers.
- Dropping `Artist.ExternalId/ExternalProvider` columns.

## Dependencies

1. **Provider DI defect, BUG-082** (`Docs/Management/BusinessFeatures/artists-songs/bugs/2026-09-25-BUG-082-music-api-providers-re-registered-in-di-bypassing-typed-httpclient/`). `ServiceCollectionExtensions.cs:44-45`
   re-registers `MusicBrainzProvider`/`DeezerProvider` as plain scoped services, bypassing the typed
   `HttpClient` (no `BaseAddress`), so every remote call fails (findings § 0.1). Fix: register the
   providers through their typed clients. No remote REQ here is testable on device until it lands.
2. **Shared Field `AutocompleteDXField`** (dependency item above): Editable / Locked / Read-only
   states, `IsSearchEnabled`, `SupportingText` (REQ-ACF-12c), shared constants. This item also asks
   for one capability, appending options to the open menu (REQ-SFT-07; `design.md` § 3.4).
4. **FTS5 on Android** is proven on desktop only. Spike gate G5 (`tasks.md`) runs on the device before
   the migration task; fallback named there.
3. **CATALOG-PROP**: coordinate on `SongService` (single writer; `tasks.md` § Coordination).

## Supersedes (shipped specs are not edited)

| Shipped REQ / AC | File | Effect |
|---|---|---|
| REQ-FORMUX-01…05 (Artist-form Name autocomplete: local 300 ms, remote +400 ms stagger, 3-tier dedup, loading-hint row, silent offline) | `changes/2026-07-10-form-ux-redesign/requirements.md` | **Replaced** by REQ-SFT-47/48 (Artist form) and REQ-SFT-03…09 (Song form): no 400 ms stagger (remote starts with local), no loading-hint row, collated-name/identity dedup only (tier (c) similarity removed), offline shows the note instead of staying silent |
| REQ-FORMUX-22 (title autocomplete, local+remote) | `changes/2026-07-10-form-ux-redesign/requirements.md` | **Replaced** by REQ-SFT-02…09 |
| REQ-FORMUX-23 (remote pick autofills) | same | **Replaced** by REQ-SFT-12/13 (artist created on Save, not "marked" at pick) |
| REQ-FORMUX-24 (run resolution/merge unchanged) | same | **Replaced** by REQ-SFT-22…26, 38 |
| REQ-FORMUX-25 (local title pick fills Title only) | same | **Replaced** by REQ-SFT-10/11 (opens the song) |
| REQ-FORMUX-31 (HasManualEdits governs title edits) | same | **Removed** (REQ-SFT-37) |
| REQ-FORMUX-26/27 (strip + picker deletion) | same | **Delivered** by REQ-SFT-44 |
| REQ-FORMUX-17…20 (Song Artist remote autocomplete, marked-for-create) | same | **Narrowed**: Artist is local-only (REQ-SFT-16); create on Save kept (REQ-SFT-18) |
| REQ-ACREATE-02, -03 (create row), -04…-08, -10, -11 | `changes/2026-07-21-inline-artist-create/requirements.md` | **Removed** by REQ-SFT-45; retain-text part of -03 kept (REQ-SFT-17) |
| REQ-ACREATE-15 (X unlocks) | same | **Kept**, and X also clears the pending external identity (REQ-SFT-14) |
| REQ-ACREATE-16 (artist change persisted) | same | **Kept**, extended to a created artist (REQ-SFT-28) |
| AC-4.1…4.4, INV-4, INV-5 (C#-normalisation clause) | `song-import-resolution/requirements.md` | **Removed** (REQ-SFT-37/38). INV-5 narrowed by the exception in `design.md` § 6.4 |
| AC-1.1 "Update existing"; AC-1.4 "update one of them" | same | **Narrowed** to Open existing (REQ-SFT-22/23/24) |
| AC-2.1…2.3 (import resolution routing) | same | **Replaced** by pick-time routing (REQ-SFT-05/10) |
| AC-3.1, AC-3.2 (attach identity), AC-3.4 (create with identity) | same | **Narrowed**: artist identity not used (REQ-SFT-40/41) |
| AC-B6, AC-B7, AC-B10 (picker bugs) | same | **Moot**: pickers deleted (REQ-SFT-44) |
| AC-4.7, AC-5.7, AC-11.4, AC-11.5 | `artists-songs/requirements.md` | **Removed** (REQ-SFT-37/38) |
| AC-11.1…11.3 (API strip, error mirror) | same | Already superseded; now fully replaced by REQ-SFT-02…14 |
| `IX_Songs_ExternalId` "Unique" (schema table, line 381) | same | **Replaced** by the composite unique index (REQ-SFT-39) |
| AC-ART-01…06, AC-SONG-01…06, AC-TRIGGER-01/02 | `search-picker/requirements.md` | **Removed** (REQ-SFT-44). AC-YT-*, AC-LOAD-* (YouTube page), AC-TRIGGER-03/04 kept |
| REQ-UOW-09 (subject: `ArtistResolutionService.CommitAsync` CreateNew outcome) | `cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-…/requirements.md` | **Removed**: the method is deleted; its test `NestedUnitOfWorkTests.cs:195` is deleted with it. The `FlushAsync` affordance it justified (REQ-UOW-35) keeps a production caller in the new save path |
| REQ-UOW-22 (subject: `SongResolutionService.CommitAsync` 3-level chain) | same | **Re-homed**: the method is deleted; the same all-or-nothing rule is required of the 2-level new-artist save path (REQ-SFT-18), and its tests are ported (`design.md` § 10.1) |
| REQ-ACF-03 "picker-result resolve" clause, Q6 | autocomplete dependency item | Moot (picker deleted) |

## Open questions for Helder

- **Q1 (REQ-SFT-36):** MusicBrainz: drop from all Field searches and delete the provider (recommended),
  or keep it behind Deezer?
- **Q2 (REQ-SFT-11):** confirm the discard prompt on a local pick when other fields hold input.
- **Q3 (REQ-SFT-18.1):** confirm that an artist with no local id that is a collated match is used silently.
- **Q4 (REQ-SFT-23):** confirm "Save as new song" (no Version needed) for fuzzy-only matches.
- **Q5:** Deezer returns `title_short` and `title_version` besides `title`. Keep Title = full `title`
  (current reading of your "tap X and retype" ruling), or fill Title = `title_short` and
  Version = `title_version`? Default: full title, Version untouched.
- **Q6 (REQ-SFT-14):** confirm that X on Artist also drops the pending external identity.
- **Q7 (REQ-SFT-08):** confirm the 3,000 ms remote bound.
- **Q8 (ranking key):** option (a) is chosen. Measured: the existing scorer's `TokenSetRatio` puts
  "Cry" (100) above "Don't Cry" (94) for "dont cry", which fails REQ-SFT-30. `design.md` § 6.3 uses
  FuzzySharp `TokenSortRatio` through the same scorer class and normalisation. Confirm.
- **Q10 (REQ-SFT-49):** store the Deezer artist id on the Artist form? Recommended: no.
- **Q11:** `NestedUnitOfWorkTests` exercises only the deleted `CommitAsync` methods. Port 4 of its 5
  tests to the new 2-level save path and delete the 5th (artist external identity, REQ-UOW-09)?
- **Q13 (REQ-SFT-27):** confirm edit mode: an API-sourced song's Title is Read-only, a manual song's
  Title is Editable.
- **Q14 (REQ-SFT-16):** confirm the Song-form Artist field searches local artists only (no Deezer).
- **Q15 (REQ-SFT-48):** confirm a local pick on the Artist form opens that artist's edit form.
- **Q16 (REQ-SFT-07):** if the shared Field cannot append options, accept the fallback (local options
  wait up to 3,000 ms for Deezer)?

Resolved: Q9 (YouTube search out of scope, fifth round); Q12 (the "In your library" marker lives in
the option supporting text, and "Online search unavailable" is the Field's `SupportingText`; no
non-selectable row).

## Traceability seed (filled at implementation)

| AC | Implementation location | Test |
|----|-------------------------|------|
| REQ-SFT-02…09 | `SongSuggestionService.SearchTitleLocalAsync`/`SearchTitleRemoteAsync`, `SongRepository.SearchTitleCandidatesAsync`, `SongFormViewModel.TitleOptionSource` | `SongSuggestionServiceTests` (A), `SongRepositorySearchTests` (B, real SQLite), VM tests |
| REQ-SFT-10…14 | `SongFormViewModel` pick handlers | `SongFormViewModelTests` (A) |
| REQ-SFT-15…21, 25, 28, 51 | `SongService.SaveFromFormAsync` (rules), `SongService` create/update (writes), `SongFormViewModel` (outcome → sheet mapping) | `SongServiceSaveFromFormTests` (A), UoW integration (A), VM mapping (A) |
| REQ-SFT-19 (retrieval) | `ArtistRepository.SearchNameCandidatesAsync`, `ArtistResolutionService` | repo test (B, migrated SQLite: "Metalica" → "Metallica") + service (A) |
| REQ-SFT-22…24, 42 | `SongResolutionService.ResolveAsync`, `SongRepository.FindIdByTitleVersionForArtistAsync` | `SongResolutionServiceTests` (A), repo test (B) |
| REQ-SFT-26, 39 | `SongService`, migration M2 | service (A), migration test (B) |
| REQ-SFT-27 | `SongFormViewModel.IsTitleReadOnly`, page | VM (A) + E2E |
| REQ-SFT-29 | `SongService.UpdateSongAsync` | regression test Red→Green (A) |
| REQ-SFT-30…34 | FTS5 migration M3, repository, scorer | repo tests (B, migrated SQLite) + service ranking tests (A) |
| REQ-SFT-35/36 | `DeezerProvider`, DI | provider test with fake handler (B) |
| REQ-SFT-37/38/40/43 | entities, configs, migration M1, deleted members | build + migration test (B) + census |
| REQ-SFT-41 | `SongService.SaveFromFormAsync` | regression test Red→Green (A) |
| REQ-SFT-44/45 | deletions | census + build (C, recorded) |
| REQ-SFT-46 | `SongService` | architecture/code review + VM tests assert only these two write calls |
| REQ-SFT-47…50 | `ArtistFormViewModel`, `ArtistSuggestionService` | VM + service tests (A) |
