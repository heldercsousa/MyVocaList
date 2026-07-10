# Artist & Song Form UX Redesign — Requirements (dated change spec)

> Feature folder: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`
> Parent feature: Artists & Songs Catalog
> Status: 📋 Spec (Helder-approved design 2026-07-10, encoded here)
> Created: 2026-07-10
>
> **Dated change spec.** The shipped specs `Docs/Management/BusinessFeatures/artists-songs/requirements.md`
> and `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/requirements.md` are immutable
> history — they are NOT rewritten. This spec supersedes specific ACs in them (see § Supersession) and the
> originals receive dated supersession notes as a task of this feature (see `tasks.md` Phase 0).

## Open assumptions (Helder to confirm at review)

1. **REQ-FORMUX-09 — ArtistForm local-pick navigates to Edit Artist.** The approved design said "pick local → selects (existing duplicate-guard rules apply)"; this spec encodes navigate-to-Edit-Artist because it is the only actionable outcome on a create form and preserves the original AC-3.3 intent.
2. **REQ-FORMUX-08 — manual edit after a remote pick clears the pending external identity (ArtistForm create path).** Supersedes AC-4.7 for the create path only (see § Supersession); chosen for symmetry with the SongForm rule that typing clears only the selection identity. AC-4.7 remains in force for edit mode.
3. **SongForm Title divergence (REQ-FORMUX-31).** Manual edit after a remote *title* pick keeps the pending external identity and lets the existing `HasManualEdits` mechanism (AC-11.4) govern — a deliberate divergence from the ArtistForm rule above, because the song save runs the resolution/merge engine that `HasManualEdits` protects.

## Purpose

Fix the form-entry UX that currently makes song registration impossible (BUG-027) and replaces the
confusing "Search music database" strip with in-field autocomplete:

1. **Autocomplete (local DB + remote music APIs)** on ArtistForm Name, SongForm Artist, and SongForm Title.
2. **Blur-clear removal** — typed text is never destroyed by focus loss (kills BUG-027 friction).
3. **Similar-match warn-before-save** — inline hint + confirm BottomSheet instead of silent duplicates or silent rejection.
4. **Search-strip removal** from both forms and **deletion of ArtistPickerPage / SongPickerPage** (superseded entry points).
5. **ArtistForm external-id persistence fix** — a remote pick's `ExternalId`/`ExternalProvider` is actually saved.

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Local suggestion** | An autocomplete row sourced from the local SQLite DB (existing Artist or Song record). |
| **Remote suggestion** | An autocomplete row sourced from an `IMusicMetadataProvider` (Deezer, MusicBrainz), deduplicated against local suggestions. |
| **Section marker** | The visual divider/header ("From music database") under which remote suggestions render in the suggestion list. |
| **Exact match** | A local record whose name/title equals the typed term under DB collation (`NOCASE_NOACCENT` via `EF.Functions.Collate`) — never via C#-side normalization. |
| **Similar match** | A fetched suggestion (local or remote) whose `ISimilarityScorer` score against the typed term is ≥ `SimilarityConstants.DefaultThreshold` (0.82) and which is not an exact match. |
| **Confirm sheet** | A ConfirmSheet-style `dx:BottomSheet` presenting similar-match candidate rows (tap to pick) plus a "Create '<typed>'" primary action. Never a native dialog. |
| **Suggestion service** | `IArtistSuggestionService` / `ISongSuggestionService` in the Services layer — owns all lookup, dedup, and similarity classification logic (business logic in Services only). |
| **Marked-for-create** | An artist identity captured in the form (name + optional external identity) that does not exist locally yet and will be created inside the same atomic save as the song. |
| **Loading-hint row** | A single non-selectable row in the suggestion list indicating the remote lookup is in flight. |

## User Stories & Acceptance Criteria

### US-1 — Artist form Name autocomplete (local + remote)

As an admin registering an artist, I want name suggestions from both my catalog and the music database
while typing, so I avoid duplicates and get accurate names without a separate search screen.

- **REQ-FORMUX-01** WHEN the user has typed ≥ 2 characters in the ArtistForm Name field and the component debounce (300 ms) elapses, the system SHALL query local suggestions and render up to 5 local rows immediately, without waiting for any remote call.
- **REQ-FORMUX-02** WHEN local suggestions have been requested for a term, the system SHALL schedule the remote lookup 400 ms after the local query is dispatched (≈ 700 ms after the last keystroke); remote results SHALL render appended under the "From music database" section marker, limited to 5 rows.
- **REQ-FORMUX-03** The system SHALL exclude from remote suggestions any result that duplicates a local suggestion, deciding in this order: (a) equal `(ExternalProvider, ExternalId)`; (b) collation-equal name (DB collation query — no C#-side normalization, HARD RULE); (c) `ISimilarityScorer` score ≥ `SimilarityConstants.DefaultThreshold` against a local suggestion's name.
- **REQ-FORMUX-04** WHILE the remote lookup is in flight, the suggestion list SHALL show a loading-hint row below the local rows; the row SHALL disappear when remote results render or the lookup fails/completes empty.
- **REQ-FORMUX-05** IF all remote providers fail or the device is offline, THEN the system SHALL keep the suggestion list local-only with no error UI and no save friction; the failure SHALL be logged.
- **REQ-FORMUX-06** WHEN the user taps a remote suggestion, the system SHALL fill the Name field with the remote name and record the pending `ExternalId` + `ExternalProvider` for save.
- **REQ-FORMUX-07** WHEN the artist is created after a remote pick (name unchanged since the pick), the system SHALL persist `ExternalId` and `ExternalProvider` on the created Artist row. (Fixes the current gap: `ArtistFormViewModel` stashes `SelectedExternalId`/`SelectedProvider` but the save path never passes them to `ArtistService`.)
- **REQ-FORMUX-08** WHEN the user manually edits the Name text after a remote pick, the system SHALL clear the pending external identity (the record being typed is no longer the picked remote entity) while keeping the typed text intact.
- **REQ-FORMUX-09** WHEN the user taps a local suggestion on the ArtistForm, the system SHALL navigate to the Edit Artist form pre-populated with that record (the artist already exists — creating it again is blocked by the uniqueness rule regardless).
- **REQ-FORMUX-32** The Edit Artist form SHALL behave identically to the create form for US-1 and US-2 — except REQ-FORMUX-08, which applies to the create path only; in edit mode AC-4.7 governs manual edits after a pick. In particular, the similar-match warn and confirm sheet (REQ-FORMUX-10…14) SHALL apply when renaming an artist (self excluded from candidates). AC-4.7 `HasManualEdits` tracking remains in force in edit mode (see § Supersession).

### US-2 — Similar-match warn before save (ArtistForm Name + SongForm Artist)

As an admin, I want to be warned when the name I typed is similar to an existing record before a new
record is created, so accidental near-duplicates need explicit confirmation.

- **REQ-FORMUX-10** WHILE the typed name has ≥ 1 similar match among the already-fetched suggestions (local or remote), the system SHALL show an inline warning hint below the entry listing the similar names (e.g. "Similar: X, Y — tap to pick"; exact wording is implementer-discretion, but it MUST be English and MUST list the candidate names), fed exclusively from the cached suggestion results — no refetch. On the ArtistForm this repurposes the existing (currently never-populated) `DuplicateSuggestions` inline block.
- **REQ-FORMUX-11** WHEN the user taps a candidate in the inline hint, the system SHALL apply the pick semantics of the hosting form (ArtistForm: REQ-FORMUX-09 for local / REQ-FORMUX-06 for remote; SongForm Artist: attach per REQ-FORMUX-17).
- **REQ-FORMUX-12** WHEN the user saves and the typed name has ≥ 1 similar match but no exact match, the system SHALL open the confirm sheet listing the candidates (tap to pick) with a "Create '<typed>'" primary action; the save SHALL NOT complete until the user chooses. The hardware Back button SHALL dismiss the sheet without saving. Pick semantics on the sheet are form-specific and deliberately asymmetric: on the **ArtistForm**, picking a remote candidate fills the form (name + pending external identity), closes the sheet, and the user MUST tap Save again (no save continues automatically); on the **SongForm**, picking a candidate attaches it as the song's artist and the save continues (REQ-FORMUX-19).
- **REQ-FORMUX-13** WHEN the user taps "Create '<typed>'" on the confirm sheet, the system SHALL proceed with creation using the typed name.
- **REQ-FORMUX-14** WHEN the typed name has no exact and no similar match, saving SHALL proceed directly with no sheet and no hint.

### US-3 — Song form Artist entry (blur-clear removal + save resolution)

As an admin registering a song, I want to type an artist name freely — picking an existing artist when
offered, or having a new one created for me on save — without the form ever erasing what I typed.

- **REQ-FORMUX-15** *(BUG-027 regression — Critical)* GIVEN any text typed in the SongForm Artist field, WHEN the field loses focus (with or without a matching suggestion), THEN the typed text SHALL remain unchanged. The blur-clear behavior is deleted. A regression test SHALL be written first and seen to FAIL against the current behavior (Red) before the fix (Green).
- **REQ-FORMUX-16** WHEN the user types after an artist selection exists (local pick, remote pick, or edit-mode pre-population), the system SHALL clear only the selection identity (local artist id and/or pending external identity) — never the text. The `IsArtistLocked` behavior is retired; the Artist field SHALL always be editable.
- **REQ-FORMUX-17** The SongForm Artist entry SHALL provide the same local + remote autocomplete behavior as REQ-FORMUX-01…05, with a tap on a suggestion selecting that artist (local: attach id; remote: record name + external identity as marked-for-create).
- **REQ-FORMUX-18** WHEN the user saves with no selected artist and the typed name has an exact local match, the system SHALL attach that artist automatically with no prompt.
- **REQ-FORMUX-19** WHEN the user saves with no selected artist and the typed name has ≥ 1 similar (non-exact) match, the system SHALL apply the confirm sheet flow (REQ-FORMUX-12); picking a candidate attaches it as the song's artist.
- **REQ-FORMUX-20** WHEN the user saves with no selected artist and no exact or similar match, the system SHALL create the artist transparently inside the same atomic save as the song (single transaction — a failure rolls back both); a marked-for-create artist carrying external identity SHALL be created with that identity persisted.
- **REQ-FORMUX-21** WHEN the user saves with an empty or whitespace-only Artist field, the form SHALL show "Artist is required" and SHALL NOT save (unchanged from AC-10.4).
- **REQ-FORMUX-33** The Edit Song form SHALL behave identically to the create form for US-3 and US-4 — in particular, when the user changes the Artist text in edit mode, the save-resolution ladder (REQ-FORMUX-18…20) SHALL apply identically.

### US-4 — Song form Title autocomplete (local + remote) with autofill

As an admin, I want title suggestions from my catalog and the music database, and picking a database
song should fill the form for me — but nothing is stored until I save.

- **REQ-FORMUX-22** The SongForm Title entry SHALL provide the same local + remote autocomplete behavior as REQ-FORMUX-01…05, with local suggestions sourced from registered songs (title + artist name shown as supporting text).
- **REQ-FORMUX-23** WHEN the user taps a remote title suggestion, the system SHALL autofill Title, Artist (the local artist if one is collation-equal / external-id-equal, otherwise the remote artist as marked-for-create), and the song's pending external identity — and SHALL NOT persist anything before Save.
- **REQ-FORMUX-24** WHEN the user saves after a remote title pick, the system SHALL run the existing Song Import & Entity Resolution flow unchanged (resolution sheet / merge sheet per `song-import-resolution/requirements.md` US-1…US-5 — those ACs remain fully in force).
- **REQ-FORMUX-25** WHEN the user taps a local title suggestion, the system SHALL fill the Title text only (uniqueness per artist is still enforced at save by the existing rules).
- **REQ-FORMUX-31** WHEN the user manually edits the Title (or any other autofilled field) after a remote title pick, the system SHALL retain the pending external identity and let the existing `HasManualEdits` mechanism (AC-11.4) govern on save — a deliberate divergence from the ArtistForm rule REQ-FORMUX-08 (see § Open assumptions, item 3).

### US-5 — Search-strip removal and picker page deletion

As an admin, I want one obvious way to get music-database data — the autocomplete — with no redundant
"Search music database" entry points.

- **REQ-FORMUX-26** The "Search music database" `ListItem` row SHALL be removed from ArtistFormPage and from SongFormPage.
- **REQ-FORMUX-27** `ArtistPickerPage` and `SongPickerPage` SHALL be deleted entirely: pages, code-behind, ViewModels, Shell routes, `Routes` constants, DI registrations, and their picked-result messenger messages. The solution SHALL build with 0 errors and no dangling references afterward.
- **REQ-FORMUX-28** `YouTubeSearchPage`, the SongForm YouTube strip, and `QueueSongPickerPage` (+ its ViewModel/route/message) SHALL remain untouched by this feature.

### US-6 — AutocompleteField remote-row presentation (governed component)

As an admin, I can tell at a glance which suggestions come from my catalog and which come from the
music database, and I can see when the database is still being searched.

- **REQ-FORMUX-29** `AutocompleteField` SHALL render remote suggestions visually distinct from local ones (a "From music database" section header, or per-row supporting text carrying the provider origin) and SHALL support a loading-hint row.
- **REQ-FORMUX-30** The `AutocompleteField` change SHALL be purely additive: with the new capabilities unused, existing consumers (PersonFormPage) SHALL render and behave exactly as before. The change follows component-change governance (dedicated task, MD3 review, consumer map, per-consumer risk, Helder approval) — see `tasks.md` Phase 2.

## Validation Rules

Existing field rules are unchanged and remain authoritative in the original specs:

| Field | Rule | Source |
|-------|------|--------|
| Artist Name | required · 2–200 chars · unique (collation) · counter thresholds | `artists-songs/requirements.md § Validation Rules` |
| Song Title | required · 1–200 chars · unique per `(ArtistId, Title, Version)` | `artists-songs/requirements.md` + `song-import-resolution/requirements.md` |
| Song Artist | required (REQ-FORMUX-21) | this spec |
| Lyrics / FeaturedArtists | unchanged | `artists-songs/requirements.md` |

Timing and matching thresholds introduced by this spec:

| Threshold | Value | Source of truth |
|-----------|-------|-----------------|
| Autocomplete component debounce | 300 ms (`AutocompleteField.DebounceDelay` default) | existing component |
| Remote lookup stagger after local dispatch | 400 ms (≈ 700 ms after last keystroke) | `design.md § Remote staging` |
| Minimum chars to trigger suggestions | 2 | matches existing AC-3.1 |
| Max suggestions per source (local / remote) | 5 each | matches existing AC-3.1 / AC-4.5 |
| Similar-match threshold | `SimilarityConstants.DefaultThreshold` = 0.82 | `Domain/Resolution/SimilarityConstants.cs` |

## Supersession

This change spec supersedes the following shipped acceptance criteria. Phase 0 of `tasks.md` adds a
dated note to each original file:
`> **Spec updated 2026-07-10:** superseded by changes/2026-07-10-form-ux-redesign — <one line>`.

| Superseded item | Original file | Disposition |
|-----------------|--------------|-------------|
| **AC-10.3** — Artist field autocomplete searches local artists only; "user must select an artist from the results" | `artists-songs/requirements.md` | Superseded by REQ-FORMUX-15…20: free text always allowed; local + remote sources; no-match → transparent create on save. |
| **AC-10.2** (partial) — form shows "an API search strip" | `artists-songs/requirements.md` | Search strip element removed (REQ-FORMUX-26); the rest of AC-10.2 stands. |
| **AC-11.1 / AC-11.2 / AC-11.2a** — API search strip below Title; artist pre-fill locked (read-only) after API result | `artists-songs/requirements.md` | Superseded by REQ-FORMUX-22…24 (title autocomplete + autofill) and REQ-FORMUX-16 (lock retired). AC-11.3–11.5 (`HasManualEdits`, merge warning) remain in force. |
| **AC-4.1 / AC-4.5 / AC-4.6** (partial) — Artist form API search strip + result list + tap-to-import | `artists-songs/requirements.md` | Delivery mechanism replaced by remote autocomplete rows (REQ-FORMUX-02, 06, 07). AC-4.2 provider order (MusicBrainz first, Deezer fallback on empty/error) and AC-4.4 semantics remain in force, adapted to the autocomplete surface; AC-4.3's blocking error message is replaced by silent local-only degradation (REQ-FORMUX-05). AC-4.7: see its own row below. |
| **AC-4.7** (create path only) — manual edit after API import marks `HasManualEdits = true` on save | `artists-songs/requirements.md` | Superseded **for the ArtistForm create path only** by REQ-FORMUX-08: a manual name edit after a remote pick clears the pending external identity, so the created record is a plain manual record (no identity, no `HasManualEdits`). AC-4.7 remains in force for **edit mode** (REQ-FORMUX-32). Open assumption — see § Open assumptions, item 2. |
| **AC-B8 (BUG-008)** — Artist field autocomplete-only, "auto-clears on blur without a valid selection", locks when API-imported | `artists-songs/song-import-resolution/requirements.md` | Superseded by REQ-FORMUX-15/16: blur-clear deleted, lock retired. Pre-populate-in-Edit-mode remains in force. |

Bug dispositions (BACKLOG rows updated at close-out, `tasks.md` Phase 6):

| Bug | Disposition |
|-----|-------------|
| **BUG-027** (Critical) | Fixed by this feature. REQ-FORMUX-15 (blur-clear regression test, Red-first mandatory) + REQ-FORMUX-18…21 (validation + save resolution). Cross-reference: `Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-001 step 7. |
| **BUG-029** (Critical, deferred) | Obsolete — the crashing search-strip element is deleted (REQ-FORMUX-26/27). Close as superseded; no fix needed. |
| **BUG-030** (spec gap, deferred) | Answered — the element's purpose duplicated the autocomplete goal; it is removed from both forms (REQ-FORMUX-26). |
| **BUG-031/032** (spec gap, deferred) | Answered and implemented — local + remote autocomplete on Artist Name and Song Title is defined by US-1/US-3/US-4. |

## Out of Scope

- Bottom-sheet/modal conversion of simple forms (own BACKLOG row: *Form presentation — bottom-sheet/modal conversion*, 2026-07-10).
- MD3 Save/Cancel app-bar action pattern for forms (own BACKLOG row: *CRUD Form Action Pattern*, Dev Cycle Craft).
- Artist name *editing* from the Song form — the song form picks or creates artists; it never renames one.
- Any change to lyric-entry behavior (assessment only — see `design.md § Lyrics entry assessment`).
- Lyrics API provider implementation (`ILyricsProvider` stays a placeholder).
- Changes to `YouTubeSearchPage`, the SongForm YouTube strip, or `QueueSongPickerPage`.
- Changes to the Song Import & Entity Resolution engine itself (resolution/merge sheets, `Song.Version` logic) — it is consumed unchanged.
- Re-sync / refresh of existing records from providers.
- Person form autocomplete changes (PersonFormPage keeps current behavior; component change is additive only).

## Failure modes

| Situation | Required behavior |
|-----------|-------------------|
| All remote providers fail / offline | Local-only suggestions, silent, logged (REQ-FORMUX-05); save fully functional. |
| Remote lookup returns after the user typed again | Stale results discarded (cancellation) — never rendered for the old term. |
| Atomic save fails after transparent artist create | Transaction rolls back both artist and song; error surfaces via the existing form error path (tuple return, snackbar/inline — never an exception escaping the service). |
| Confirm sheet dismissed (hardware back / scrim) | No save occurs; form state unchanged. |
| Suggestion tap while remote lookup in flight | In-flight lookup cancelled; pick applies immediately. |
