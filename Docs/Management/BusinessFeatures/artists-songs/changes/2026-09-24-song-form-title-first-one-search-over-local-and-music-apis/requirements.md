# Requirements: Song form title-first, one search over local and music APIs

> **Amendment 2026-11-04 (Q5/Q7: REQ-SFT-04/05/06/08/12/14/47 amended, REQ-SFT-53 new) APPROVED by
> Helder on 2026-11-04, including the author proposals Q17–Q21.** Dated notes mark every change; the
> tasks gated by plan § 0.3 D21 are now dispatchable.

> **Spec APPROVED 2026-09-26** under Helder's pre-authorisation of 2026-09-25 ("approve as you
> recommend … be automatic"). Every open question takes its recorded recommendation, and all seven
> decision rounds in README.md apply. Helder did not read the full spec; the on-device E2E gate
> remains his.

> **Status: DRAFT for Helder's review (2026-09-24).** Written from Helder's five rounds of decisions
> (`README.md`, same folder) and the read-only evaluation `findings.md` (same folder).
> Items marked `[OPEN — Helder decides]` or `[Proposed — confirm]` are not decided (none of the latter remain after the 2026-11-04 approval).
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
| **Online-search note** | One of two texts shown in the Field's own supporting text (`AutocompleteDXField.SupportingText`, REQ-ACF-12c: the line under the field, replaced by the error text while the field has an error): the **offline note** or the **unavailable note** (REQ-SFT-06). Not an option row. *(Amended 2026-11-04, Q7: was the single text "Online search unavailable".)* |
| **Remote search status** | The outcome of one remote query: **Ok** (Deezer answered, including with zero results), **Offline** (the device reported no internet, so Deezer was not called, REQ-SFT-53) or **Unavailable** (Deezer was called and failed or timed out, REQ-SFT-06/08). |
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
- **US-7:** As the admin editing a song, I want to correct its Title in place, so that I never have to
  delete and re-create a song, and lose its history, to fix a typo.

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
  - Remote option: headline = title; supporting text = "artist · version · Deezer" (the " · version"
    part is omitted when the Deezer version is empty). Title and version are the Deezer values as
    defined in REQ-SFT-12.
  > **Spec updated 2026-11-04 (Q5 = B; consequence of Q5, not a separate decision):** the remote option's supporting text gains the version, because
  > its headline is now Deezer's `title_short`, so "Creep" and "Creep (Live)" would otherwise be two
  > identical-looking rows. Example: Deezer "Creep (Live)" by Radiohead → headline "Creep", supporting
  > text "Radiohead · Live · Deezer".
  Given the library holds "Don't Cry" (Version "") and "Don't Cry" (Version "Live") by Guns N' Roses,
  When the user types "don't cry",
  Then two local options appear, with supporting texts "Guns N' Roses · In your library" and
  "Guns N' Roses · Live · In your library".
- **REQ-SFT-05: One row per song.** IF a Deezer result already exists locally, THEN it SHALL appear
  once, as a local option, never as a remote option. "Exists locally" means, in this order:
  (a) a stored song has the same external identity; else
  (b) a stored song is a collated match on title **and** artist name **and** version, where the
  Deezer side uses the REQ-SFT-12 values (title = `title_short`, version = the normalised
  `title_version`, "" when Deezer has none).
  A song found by (a) or (b) that is not already among the local options replaces the remote row
  in place of it, as a local option.
  > **Spec updated 2026-11-04 (Q5 = B; consequence of Q5, not a separate decision):** (b) compared Deezer's full title against stored songs with
  > Version "". Now that a pick stores `title_short` + version, (b) compares the same three values a
  > pick would save, so a stored "Creep" / "Live" by Radiohead is found for Deezer's "Creep (Live)".
- **REQ-SFT-06: Offline or API error — two notes** `[Amended 2026-11-04 — Helder, Q7]`. The local options
  SHALL always show. The Field's supporting text depends on the remote search status:
  1. **Offline** (REQ-SFT-53: the device reports no internet; Deezer is **not** called) → the
     **offline note**: *"You're offline — showing songs from your library only."* Logged at
     Information (an expected device state, not an app failure).
  2. **Unavailable** — Deezer was called and failed: no network reached it, HTTP error, timeout per
     REQ-SFT-08, unreadable response, or an HTTP 200 response carrying an `error` object (e.g. over
     quota) → the **unavailable note**: *"Online search isn't available right now — showing your
     library only."* Logged at Warning.
  3. **Ok** (including zero results) → no note.
  The note shown is the status of the **latest** query that reached the remote step (≥ 3 trimmed
  characters, REQ-SFT-03); a shorter query leaves the current note as it is. Emptying Title (or X on
  Title) clears the note. No option row, snackbar or dialog.
  Given the device is in airplane mode, When the user types "creep", Then local options appear with
  no 3,000 ms wait, no Deezer request is sent, and the supporting text reads the offline note.
  Given Deezer answers HTTP 500, When the user types "creep", Then the local options appear and the
  supporting text reads the unavailable note.
  > **Spec updated 2026-11-04 (Q7):** was one note, "Online search unavailable", for every failure.
  > Helder asked for two distinct messages so the user can tell "my phone is offline" from "the music
  > service is down".
- **REQ-SFT-07: Local options do not wait for Deezer.** WHEN a query is requested, the local options
  SHALL be shown as soon as the local search returns, without waiting for Deezer; remote options are
  appended when Deezer answers, if the query is still current. This needs one capability from the
  shared Field (append options to the current menu, `design.md` § 3.4). If the dependency item
  rejects it, the fallback is: the Field shows local and remote together once Deezer answers or the
  REQ-SFT-08 bound expires, so local options wait at most 3,000 ms (Q16).
  > **Spec updated 2026-09-26:** Q16 RESOLVED. The shared Field does **not** gain an append capability in this pipeline (orchestrator decision under Helder's pre-authorisation of 2026-09-25, applying the Q16 recommendation). The fallback is the behaviour: the Field shows local and remote options together once Deezer answers or the REQ-SFT-08 bound expires, so local options wait at most 3,000 ms. Same for the Artist form (REQ-SFT-47). Plan: `plan.md` § 0.3 D1.
  > **Spec updated 2026-11-11:** Helder confirmed the fallback: no loading indicator while Deezer answers; options are
  > shown when the response arrives. See `design.md` § 3.4 note of the same date.
- **REQ-SFT-08: Remote wait bound.** A Deezer call that has not completed within 3,000 ms
  `[Confirmed — Helder 2026-11-04]` SHALL be cancelled and treated as a failure: status Unavailable,
  the unavailable note (REQ-SFT-06 case 2). When the device is offline the call is never made
  (REQ-SFT-53), so this bound does not apply and local options do not wait.
- **REQ-SFT-09: Latest query wins.** Options from an earlier, slower query SHALL never replace options
  of a later query (carry-over of REQ-ACREATE-13 / REQ-ACF-26).
- **REQ-SFT-53: Offline detection skips Deezer** `[New 2026-11-04 — Helder, Q7: offline skips Deezer, read through an interface; access-level mapping (Q19) approved by Helder 2026-11-04]`. Before each remote
  query (≥ 3 trimmed characters), the search Service SHALL ask the device's network state through an
  interface it owns (`INetworkStatus`, `design.md` § 3.6). IF the device reports **no internet**,
  THEN the Service SHALL NOT call Deezer and SHALL return status Offline at once (REQ-SFT-06 case 1).
  Otherwise it calls Deezer as in REQ-SFT-06/08.
  - "No internet" means the platform reports network access **None** or **Local** (connected to a
    network with no internet route). **Unknown** and **ConstrainedInternet** (e.g. a captive portal)
    are **not** treated as offline: Deezer is tried, and a failure shows the unavailable note. This
    avoids a false "You're offline" when the platform cannot tell. `[Approved — Helder, 2026-11-04 — (Q19)]`
  - The state is read per query, never cached, so going back online is picked up by the next query.
  - **No MAUI type in Services or Domain:** Services see only `INetworkStatus`; the MAUI
    implementation lives in the app project (constitutional layering, `code-principles.md`).
  - Applies to the Artist form's Name search too (REQ-SFT-47).

### C. Picks

- **REQ-SFT-10: Local pick opens the song.** Given add mode, When the user picks a local option,
  Then the add form is replaced by that song's edit form (Back from it returns to the page the add
  form was opened from, not to the add form).
- **REQ-SFT-11: Unsaved data on a local pick** `[Withdrawn — Helder 2026-11-11]`. The discard
  `dx:BottomSheet` ("Open '<title>'? What you entered here will be discarded.") is removed: the 2026-11-11
  device test showed it never appeared, and Helder chose to drop it. A local pick ALWAYS runs REQ-SFT-10
  with no sheet, whatever the other fields hold (their input is replaced by the opened song). The
  `DiscardSheetTitle` follow-up is void.
- **REQ-SFT-12: New Deezer pick fills the form.** Given add mode, When the user picks a remote option,
  Then:
  - **Title** = Deezer's `title_short` (the title without its version suffix); if `title_short` is
    missing or empty, Deezer's full `title`.
  - **Version** = Deezer's `title_version`, normalised: trim, then remove the outer brackets **only when** the value starts with `(` or `[` and that bracket's **matching** closer is the value's last character; trim again; otherwise keep it as is ("matching" counts nesting depth of the same bracket type only; the other type is ignored, so `"(Live [2011])"` → `"Live [2011]"`) (`"(Live)"` → `"Live"`,
    `"[Remastered 2011]"` → `"Remastered 2011"`, `"Live at Wembley"` unchanged, `"(Live) (2011)"` unchanged
    because the first `(` closes before the end, `"(Live]"` unchanged, missing or empty → `""`). `[Approved — Helder, 2026-11-04 — the bracket stripping (Q17)]` Version is **overwritten** by the
    pick, including with `""` (the pick defines the song's identity). `[Approved — Helder, 2026-11-04 — overwrite
    with "" (Q20)]`
  - **Artist** = the provider's artist name; the pending external identity = `("Deezer", <track id>)`.
  - Title and Artist are both in the picked state. Version stays an ordinary editable field.
    Featured Artists and Lyrics are unchanged.
  - **Nothing is written to the database** at pick time. On Save, Title and Version are persisted
    like typed values (REQ-SFT-22/26/29 rules apply unchanged); the song's identity is
    (artist, title, version).
  Given Deezer returns "Creep (Live)" (`title_short` "Creep", `title_version` "(Live)") by Radiohead,
  When the user picks it, Then Title = "Creep", Version = "Live", Artist = "Radiohead", and Save
  stores Title "Creep" and Version "Live".
  > **Spec updated 2026-11-04 (Q5 = B, Helder):** was "Title = the provider's title exactly as
  > returned; Version unchanged". Rationale (Helder): artist + title + version is the song's identity
  > (a unique-key candidate), and the same three make a precise YouTube query. Verified against
  > Deezer's live public API 2026-11-04: `/search` and `/track/{id}` both return `title`,
  > `title_short` and `title_version`, and `title_version` carries the parentheses, e.g.
  > `"(Live)"`, `"(Acoustic)"` (hence the normalisation). **Lyrics:** neither response carries any
  > lyrics text, only the content-rating flags `explicit_lyrics` and `explicit_content_lyrics`, so
  > Lyrics stays user input (lyrics providers remain out of scope).
- **REQ-SFT-13: Artist of a Deezer pick.** IF the provider's artist name is a collated match for a
  local artist, THEN Artist is picked as that local artist. Otherwise Artist is picked with the
  provider's name and no local id, and the artist is created on Save (REQ-SFT-18).
- **REQ-SFT-14: X after a Deezer pick.** When the user taps X on Title **or** on Artist after a remote
  pick, the pending external identity SHALL be cleared, because the record is no longer the picked
  Deezer song `[Confirmed for Artist — Helder 2026-11-04]`. X on Title also empties and unlocks Title; X on
  Artist also empties and unlocks Artist. The other field keeps its value and state.
  **Version filled by the pick** `[Approved — Helder, 2026-11-04 — (Q21)]`: X on **Title** also resets Version
  to `""` if it still holds the value the pick wrote (unchanged since the pick); a Version the user edited
  after the pick is kept. X on Artist leaves Version as is. Consequence for REQ-SFT-11: after X on Title, a
  pick-filled Version is gone, so it never triggers the discard sheet; a Version the user typed or edited
  is user input there as before.

### D. No pick

- **REQ-SFT-15: Typed text is the title.** When the user leaves Title without picking, the typed text
  stays and is the title. Save applies the existing title rules only (required; ≤ 100 characters [limit raised to 250 by change 2026-11-11-song-title-limit-raised-from-100-to-250-characters],
  `SongService.ValidateTitleInput`). There is no other handling of unpicked title text.

### E. Artist

- **REQ-SFT-16: Artist field.** Artist SHALL be the Field over **local artists only**, in the picked
  state after a pick, with no "Add … as a new artist" row and no remote search.
- **REQ-SFT-17: Blur keeps text, no error.** When Artist loses focus holding unpicked text, the text
  stays and **no** error is shown. ("Search and select an artist from the list" is removed.)
- **REQ-SFT-18: Artist with no local id on Save.** Given Artist is non-empty and has **no local id**
  (typed and not picked, **or** filled by a Deezer pick with no local match, REQ-SFT-13), When the
  user saves, Then the Service decides, in this order:
  1. a collated match exists → that artist is used, silently, with no prompt (Helder, sixth round);
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
  > **Spec updated 2026-11-05:** an artist refused while it is created inside the song write returns `Failed(Artist, message)` and saves no song (ruling D2); a song-level refusal after the artist was created keeps field None.
- **REQ-SFT-52: Never two artists with the same name** `[HARD RULE — Helder, sixth round]`. The system
  SHALL NOT create or rename an artist so that two artists share a name under the DB collation
  (`NOCASE_NOACCENT`). Enforced in the Services, on every artist create and rename path
  (`SongService.SaveFromFormAsync` → `CreateSongWithUrlsAsync`/`UpdateSongAsync`, and
  `ArtistService.CreateArtistAsync`/`UpdateArtistAsync`), with the unique collated index
  `IX_Artists_Name` as the backstop.
  - An exact collated match is reused silently (REQ-SFT-18.1); a near-duplicate asks (REQ-SFT-19).
  - Concurrent writes are not designed for: the database is local to one device and one user (Helder,
    seventh round). `IX_Artists_Name` stays as the safety net only.
  - Given "Metallica" is stored, When any path tries to create "metallica" or "Métallica", Then no
    second artist exists afterwards.
  - **User guidance:** the app does not model homonyms. Two different artists with the same name are
    told apart by the user in the name itself, e.g. "Carlinhos Sanfoneiro - Grajaú".
  - The Artist form already enforces this: create `Services/ArtistService.cs:66-67`, rename
    `:102-103` (collated query `Infra/Repository/ArtistRepository.cs:98,104`), DB index
    `Infra/EntityEFConfig/ArtistConfiguration.cs:17-21,39-41`. No change is needed on that path.
- **REQ-SFT-21: Artist required.** Save with an empty or whitespace Artist shows "Artist is required"
  and saves nothing (unchanged, AC-10.4).
  > **Spec updated 2026-11-04 (3.1):** at the Service, a blank or whitespace `newArtistName` counts as not supplied, so `CreateSongWithUrlsAsync(null, "  ", …)` returns "Artist is required" (consistent with this REQ; Verifier-confirmed).

### F. Save-time song duplicate guard (simplified resolution sheet)

- **REQ-SFT-22: Exact duplicate.** Given the artist is an existing artist, When Save finds a stored
  song that is a collated match on (artist, title, version), Then a `dx:BottomSheet` offers
  **Open existing** and **Save as new version**. Open existing → REQ-SFT-10 navigation (no extra
  prompt; the user chose it). Save as new version → requires a non-empty Version (AC-1.2 validation
  and message unchanged).
- **REQ-SFT-23: Possible duplicates** `[Confirmed — Helder 2026-11-04]`. Given no exact duplicate, When Save finds
  similar titles for the same artist (existing fuzzy step, score ≥ 0.82), Then the sheet lists them
  (tap a row → open that song, REQ-SFT-10) and offers **Save as new song**, which saves with no
  Version requirement.
  > **Spec updated 2026-11-09 (BUG-085, approved by Helder):** the "existing fuzzy step" candidate pool is no longer a forward first-word prefix match (a typed title longer than the stored one, e.g. "Patiencee" vs stored "Patience", found nothing). Candidates for the artist are retrieved from the song-title trigram index (OR of the typed title's 3-character windows, as REQ-SFT-34 does for artists), scoped to the same artist, then scored; the ≥ 0.82 threshold is unchanged. Given "Patience" is stored for an artist, When the user saves "Patiencee" for that artist, Then the Possible duplicates sheet lists "Patience" (and the reverse still lists "Patiencee").
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
  > **Spec updated 2026-11-05:** the refusal applies only on the no-decision path. With an explicit `SaveAsNewVersion` or `SaveAsNewSong` the identity is nulled before the write and the save succeeds (ruling D1; see REQ-SFT-39).

### G. Edit mode

- **REQ-SFT-27: Title is editable in edit mode** (Helder, seventh round; reverses the sixth-round
  ruling). WHILE in edit mode, Title SHALL be an Editable plain text field for **every** song: the Field
  with `IsSearchEnabled = false` (no options, no menu) and `IsReadOnly = false`.
  - **Rationale:** a song carries history that must be preserved: YouTube links, catalog rows, and each
    singer's performance history from karaoke events, which is the basis for future AI song suggestions.
    Songs are never deleted and re-created as a workaround.
  - **API-sourced song:** editing its Title changes only the local title. `ExternalId` and
    `ExternalProvider` are kept unchanged (the song is still that Deezer track, with a corrected local name).
  - The edited title is checked for uniqueness with Version (REQ-SFT-29) and with the title rules
    (REQ-SFT-15). Given Radiohead has "Creep" (Version ""), When the user edits another Radiohead song
    with Version "" to Title "creep" and saves, Then the save fails with the existing duplicate message.
- **REQ-SFT-28: Artist in edit mode.** The stored artist loads in the picked state with no search
  (REQ-ACF-03). After X, REQ-SFT-16…21 apply unchanged, including create-on-Save. The changed artist
  is persisted (REQ-ACREATE-16 kept).
- **REQ-SFT-29: Edit-save uniqueness includes Version** (absorbed bug). Given Radiohead has "Creep"
  (Version "") and "Creep" (Version "Live"), When the user edits the "Live" song's Version to
  "Acoustic" and saves, Then the save succeeds (today it fails: the update check ignores Version,
  `SongService.cs:155`). Changing that Version to "" fails with the existing duplicate message.

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
- **REQ-SFT-36: MusicBrainz** `[Decided — drop, Helder 2026-11-04]`. The title search and
  the Artist-form search SHALL NOT query MusicBrainz; Deezer is the only remote provider. Rationale in `design.md` § Decision: MusicBrainz.

### I. Tracking-field simplification (approved, README third round item 4)

- **REQ-SFT-37: No manual-edit flag.** `Song.HasManualEdits` and `Artist.HasManualEdits` SHALL NOT
  exist in the model, the database, DTOs or service signatures.
- **REQ-SFT-38: No merge sheet.** No merge sheet, field-diff list or per-field accept SHALL exist.
- **REQ-SFT-39: Unique external identity index.** The database SHALL reject a second song with the
  same non-null `(ExternalProvider, ExternalId)`. Existing duplicates are resolved by the migration:
  the song with the lowest `Id` keeps the identity; the others keep their data and lose only the
  identity (set to null). No song row is deleted.
  > **Spec updated 2026-11-05:** a row written from an explicit `SaveAsNewVersion` / `SaveAsNewSong` decision carries a NULL external identity, even for a new artist and even when the identity is held by another song; the save succeeds (ruling D1).
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
  > **Spec updated 2026-11-05:** per Helder's approval, the ViewModel holds the artist and song decisions for one Save attempt and re-sends them on every re-invocation, clears both on an Artist or Title edit, and a fresh user Save tap starts with no decisions (confirmed by Helder 2026-11-05). Made a first-class criterion: REQ-SFT-54.
- **REQ-SFT-54: Save decision lifecycle in the ViewModel** `[New 2026-11-05 — Helder, approved with 5.3]`. Within one Save attempt the ViewModel SHALL hold the artist decision and the song decision the user has chosen and send both on every re-invocation of the Save method (REQ-SFT-51). It SHALL clear both when the user edits the Artist or the Title field. A fresh user tap on Save SHALL start with no decisions, so a dismissed sheet is asked again.
  > **Spec updated 2026-11-05:** the 5.3 tests that cover this are currently tagged REQ-SFT-51; re-tag to REQ-SFT-54 the next time those tests are touched (the Builder may not edit them).
- **REQ-SFT-46: One write path.** Every song create and update from the Song form, including the
  artist it creates, SHALL go through `ISongService.CreateSongWithUrlsAsync` or
  `ISongService.UpdateSongAsync`, so CATALOG-PROP's propagation cannot be bypassed.
  > **Spec updated 2026-11-05:** the public Create/Update methods are thin wrappers over private cores that hold their bodies; `SaveFromFormAsync` calls those cores, so they remain the only write bodies. The cores also return the failing form field (ruling D2). No public signature changed.

### L. Artist form name search (confirmed by Helder, fifth round)

- **REQ-SFT-47: Name is a Field.** On the Artist form in **add mode**, Name SHALL be the Field over
  local artists and Deezer artists, with the REQ-SFT-03/06/07/08/09 thresholds and failure rules.
  Local options come first (marker "In your library"), then Deezer options (supporting text
  "Deezer"). In edit mode Name is the Field with `IsSearchEnabled = false`, Editable (rename unchanged).
  > **Spec updated 2026-11-04 (Q7):** the two notes and offline detection (REQ-SFT-06/53) apply here.
  > Wording on this form `[Approved — Helder, 2026-11-04 — (Q18)]`: offline → *"You're offline — showing artists
  > from your library only."*; unavailable → *"Online search isn't available right now — showing
  > your library only."* (the unavailable text is the Song form's, verbatim).
  > **Spec updated 2026-09-26:** the Artist form's **local** name search uses the artist-name trigram index (`IArtistRepository.SearchNameCandidatesAsync`, FTS5 `ArtistSearch`, REQ-SFT-19/34), ranked for display by `ISimilarityScorer.RankScore` (ordering only, `design.md` § 6.4), at most 5 — not the collated name-prefix query. Without this, "metalica" could not list the stored "Metallica" and REQ-SFT-48's single-row rule could not hold for a typo. Orchestrator decision under Helder's pre-authorisation (plan § 0.3 D20).
  Given "Metallica" is stored, When the user types "metalica" in Name on New Artist,
  Then "Metallica" is the first local option ("In your library"), and a Deezer artist named "Metallica" is not shown as a separate Deezer option (REQ-SFT-48).
- **REQ-SFT-48: Local artist pick opens it.** Picking a local artist opens that artist's edit form,
  replacing the add form (dedup; carry-over of REQ-FORMUX-09's intent). If a Deezer artist's name is
  a collated match for a local artist, it is shown once, as the local option.
- **REQ-SFT-49: Deezer artist pick fills the name only.** Picking a Deezer artist fills Name with the
  provider's name and puts it in the picked state; X empties and unlocks it. Nothing is written
  before Save; Save uses the existing create rules (non-empty, ≤ 60, no collated duplicate).
  **Storing the Deezer artist id** `[Decided — not stored, Helder 2026-11-04]`: recommended **not stored**. Reason: no
  flow reads `Artist.ExternalId` (REQ-SFT-40, README third round item 4), song-side dedup uses the
  collated name, and a write-only column would revive the machinery just removed. Revisit only when
  an artist re-import or sync feature needs it.
- **REQ-SFT-50: No typed-name duplicate on the Artist form.** Unpicked typed text is saved as today
  (`ArtistService.CreateArtistAsync`, collated duplicate rejected with the existing message).

## Validation rules

| Field | Rule | Source (unchanged unless noted) |
|-------|------|------|
| Title | required; ≤ 100 chars after trim (→ 250, see change 2026-11-11-song-title-limit-raised-from-100-to-250-characters) | `SongService.ValidateTitleInput` |
| Version | optional; ≤ 60; required for "Save as new version"; a Deezer pick fills it (REQ-SFT-12) | `SongService.ValidateVersionInput` |
| Artist | required; typed name ≤ 60, non-empty after trim, no collated duplicate when created | `IArtistService` |
| Song identity | unique `(ArtistId, Title, Version)` collated, on create **and** update | DB index + REQ-SFT-29 (update: changed) |
| External identity | unique `(ExternalProvider, ExternalId)` when both non-null | REQ-SFT-26/39 (new) |

## Failure modes

| Situation | Required behaviour |
|-----------|--------------------|
| Device reports no internet (network access None/Local) | Deezer not called; local options at once + the offline note (REQ-SFT-06 case 1, REQ-SFT-53); Save unaffected |
| Deezer unreachable, HTTP error, timeout, bad JSON, HTTP 200 with an `error` object (quota) | Local options + the unavailable note (REQ-SFT-06 case 2); Save unaffected |
| Deezer `title_version` longer than 60 characters after normalisation | The pick fills it as is; Save shows the existing Version length message (`ValidateVersionInput`) and the user shortens it (REQ-SFT-12) |
| Deezer returns 0 results | Local options only; no note |
| Artist create fails inside Save | Nothing saved; message on Artist (REQ-SFT-20) |
| Song write fails after the artist was staged | Unit of work rolls back both; the existing save-error path shows the message |
| Near-duplicate sheet or resolution sheet dismissed | No save; form unchanged |
| Navigation to the edit form fails (song deleted meanwhile) | Edit form loads empty and logs a warning, as today (`SongFormViewModel.cs:440-444`) |
| Local search index missing (migration not applied) | Local search returns no FTS results and logs an Error; the prefix path (REQ-SFT-33) still works |

## Out of scope

- **CATALOG-PROP** (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`): catalog rows on
  song writes. This item only guarantees REQ-SFT-46.
- Showing an artist's songs or albums from Deezer (future idea; the Song-form Artist field searches
  local artists only, REQ-SFT-16, Helder sixth round).
- Modelling homonym artists (REQ-SFT-52: the user distinguishes them in the name).
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

- **Answered by Helder 2026-11-04 (G4 round).** Guiding principle he stated: *keep it simple — the MVP
  validates the basic business features and whether the community finds them valuable.*
  - **Q1 → drop MusicBrainz** (REQ-SFT-36): Deezer is the only remote provider; the provider is deleted.
  - **Q2 → confirmed** (REQ-SFT-11): the discard sheet is shown. Helder's long-term note (not in scope): a later
    stage may move records to a central DB with local and cloud copies and restrict edits.
  - **Q4 → confirmed** (REQ-SFT-23): "Save as new song", no Version required.
  - **Q5 → B** (clarified 2026-11-04): a Deezer pick fills Title = `title_short` and Version =
    `title_version`, and Version is persisted. Rationale (Helder): artist + title + version is the song's
    identity (unique-key candidate) and the same three make a precise YouTube query. Lyrics: Deezer's public
    API is believed not to return lyrics text — to be verified when the REQs are amended.
  - **Q6 → confirmed** (REQ-SFT-14): X on Artist also drops the pending external identity.
  - **Q7 → 3,000 ms confirmed** (REQ-SFT-08), **plus a new ask (2026-11-04): two distinct messages** in the
    field's supporting text, replacing the single "Online search unavailable":
    1. Device offline → *"You're offline — showing songs from your library only."* — and the Deezer call is
       skipped entirely (local results shown at once, no 3,000 ms wait).
    2. Deezer timeout (> 3,000 ms) or any Deezer error → *"Online search isn't available right now —
       showing your library only."*
    Connectivity is read through an interface (no MAUI types in Services). REQ-SFT-06/08 and the Deezer-pick
    REQs (Q5) are amended in the next spec step, then spec-reviewer, then Helder's approval.
  - **Amendment written and APPROVED 2026-11-04 (Helder: "Step 1: Approved" — Q17–Q21 resolved as recommended):** Q5 → REQ-SFT-04/05/12; Q7 →
    REQ-SFT-06/08, new REQ-SFT-53, REQ-SFT-47 note; `design.md` § 3.3/3.4/3.6/4; plan D21. Two
    sub-questions it raised:
    - **Q17 (Version normalisation):** Deezer's `title_version` comes with parentheses (`"(Live)"`).
      Recommended: strip one enclosing pair of `()` or `[]` and trim, so a pick stores `Live` like a
      typed Version, and REQ-SFT-05(b) finds a stored "Live". Alternative: store it raw.
    - **Q18 (Artist form wording):** recommended *"You're offline — showing artists from your library
      only."* for the offline note, with the unavailable note verbatim from the Song form.
    - **Q19 (what counts as offline):** recommended: only network access None/Local; Unknown and
      ConstrainedInternet try Deezer (a failure then shows the unavailable note). Alternative: anything
      but Internet is offline (risks a false "You're offline" on captive portals or unknown states).
    - **Q20 (pick overwrites Version with ""):** recommended yes, because the pick defines the song's
      identity (artist, title, version). Alternative: overwrite only when Deezer has a version (keeps a
      typed "Acoustic" for a version-less Deezer track, at the risk of a mismatched identity).
    - **Q21 (X on Title after a Deezer pick):** recommended: also reset a still-unchanged pick-filled
      Version (REQ-SFT-14). Alternative: keep it (it then counts as user input for REQ-SFT-11).
    - Featuring info is **not** lost: Deezer keeps "(feat. …)" inside `title_short` (e.g. "Old Town Road
      (feat. Billy Ray Cyrus)" with `title_version` "(Remix)"; payloads in `findings.md` § Deezer title
      fields, 2026-11-04). Moving it to Featured Artists is out of scope.
  - **Q8 → confirmed:** ranking uses `TokenSortRatio` via `ISimilarityScorer.RankScore` (merged in 2.2).
  - **Q10 → not stored** (REQ-SFT-49): only the name is saved.
  - **Q11 → confirmed:** port 4 of the 5 `NestedUnitOfWorkTests` to the new save path; delete the 5th
    (artist external identity, REQ-UOW-09).
- **Q16 (REQ-SFT-07): RESOLVED 2026-09-26: fallback accepted; no append capability (see the note under REQ-SFT-07).** Original question: if the shared Field cannot append options, accept the fallback (local options
  wait up to 3,000 ms for Deezer)?

Resolved: Q3 (collated match reused silently), Q13 (Title editable in edit mode for every song; seventh round, reversing the sixth),
Q14 (Song-form Artist searches local only), Q15 (Artist-form local pick opens the artist), all
sixth round; Q9 (YouTube search out of scope, fifth round); Q12 (the "In your library" marker lives in
the option supporting text, and "Online search unavailable" is the Field's `SupportingText`; no
non-selectable row — the text itself was replaced by the two Q7 notes on 2026-11-04, REQ-SFT-06).

## Traceability seed (filled at implementation)

| AC | Implementation location | Test |
|----|-------------------------|------|
| REQ-SFT-02…09 | `SongSuggestionService.SearchTitleLocalAsync`/`SearchTitleRemoteAsync`, `SongRepository.SearchTitleCandidatesAsync`, `SongFormViewModel.TitleOptionSource` | `SongSuggestionServiceTests` (A), `SongRepositorySearchTests` (B, real SQLite), VM tests |
| REQ-SFT-10…14 | `SongFormViewModel` pick handlers | `SongFormViewModelTests` (A) |
| REQ-SFT-15…21, 25, 28, 51 | `SongService.SaveFromFormAsync` (rules), `SongService` create/update (writes), `SongFormViewModel` (outcome → sheet mapping) | `SongServiceSaveFromFormTests` (A), UoW integration (A), VM mapping (A) |
| REQ-SFT-19 (retrieval) | `ArtistRepository.SearchNameCandidatesAsync`, `ArtistResolutionService` | repo test (B, migrated SQLite: "Metalica" → "Metallica") + service (A) |
| REQ-SFT-22…24, 42 | `SongResolutionService.ResolveAsync`, `SongRepository.FindIdByTitleVersionForArtistAsync` | `SongResolutionServiceTests` (A), repo test (B) |
| REQ-SFT-26, 39 | `SongService`, migration M2 | service (A), migration test (B) |
| REQ-SFT-27 | `SongFormViewModel.IsTitleSearchEnabled` (= `!IsEditMode`), `SongService.UpdateSongAsync` | VM (A), service (A: identity kept on title edit; duplicate title rejected) + E2E |
| REQ-SFT-52 | `SongService.SaveFromFormAsync`, `ArtistService.CreateArtistAsync`/`UpdateArtistAsync` | `SongServiceSaveFromFormTests` (A: collated reuse, no second artist), existing `ArtistServiceTests` duplicate cases |
| REQ-SFT-29 | `SongService.UpdateSongAsync` | regression test Red→Green (A) |
| REQ-SFT-30…34 | FTS5 migration M3, repository, scorer | repo tests (B, migrated SQLite) + service ranking tests (A) |
| REQ-SFT-35/36 | `DeezerProvider`, DI | provider test with fake handler (B) |
| REQ-SFT-37/38/40/43 | entities, configs, migration M1, deleted members | build + migration test (B) + census |
| REQ-SFT-41 | `SongService.SaveFromFormAsync` | regression test Red→Green (A) |
| REQ-SFT-44/45 | deletions | census + build (C, recorded) |
| REQ-SFT-46 | `SongService` | architecture/code review + VM tests assert only these two write calls |
| REQ-SFT-47…50 | `ArtistFormViewModel`, `ArtistSuggestionService` | VM + service tests (A) |
| REQ-SFT-12 (Q5 mapping) | `DeezerProvider` (title_short, normalised title_version), `SongFormViewModel.ApplyRemotePick` | provider tests (B, fake handler), VM (A) |
| REQ-SFT-53 | `INetworkStatus` / `MauiNetworkStatus`, `SongSuggestionService`, `ArtistSuggestionService` | service tests with `Mock<INetworkStatus>` (A); `MauiNetworkStatus.IsInternetReachable` theory (B); airplane-mode check in the device gate (7.2) |
| REQ-SFT-54 | `SongFormViewModel` (`ClearSaveDecisions`, `OnSongTitleChanged`, `OnArtistSearchTextChanged`, `RunSaveAsync`) | `SongFormViewModelTests` 5.3 decision-lifecycle tests (A; currently tagged REQ-SFT-51); device check: AutoCompleteEdit must not re-write Text (7.2) |
