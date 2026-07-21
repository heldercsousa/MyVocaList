# Requirements — Inline "create new artist" on the Song form

**Feature:** Song form lets the user create a new artist inline when the typed name matches no existing artist, without leaving the form.
**Parent:** Artists & Songs Catalog → BUG-027 (closes its deferred third symptom, "no create-new fallback").
**Relationship:** delivers the "never clear typed text" + "create from no-match" slices of the deferred *Form UX Redesign* (BACKLOG 2026-07-10). The similar-match (fuzzy) warning from that redesign is explicitly **out of scope** here (see below).
**Design approval:** Helder approved the design 2026-07-21 (plan mode). Affordance = synthetic dropdown row; scope = minimal (no shared-boilerplate refactor).

## User story

As an admin registering a song, when I type an artist who is not yet in my catalog, I want to add that artist right from the Song form — so I don't lose my half-filled song and don't have to detour to the Artist form and back.

## Acceptance criteria

- **REQ-ACREATE-01** — While typing in the Song form Artist field, the existing local autocomplete continues to show matching artists unchanged (no regression to current search behavior).
- **REQ-ACREATE-02** — When the typed text (≥1 non-whitespace char) yields at least one result, a distinct **"Add «typed text» as a new artist"** row is appended as the **last** item of the suggestion list, visually separated (leading ➕, divider above) from real matches.
- **REQ-ACREATE-03** — When the typed text yields **no** local match, the typed text is **retained** (never cleared on blur) and the "Add «typed text»…" row is still offered. This supersedes the current BUG-008 clear-on-no-match behavior for the Song artist field and resolves the REQ-DXAC-03 escalation.
- **REQ-ACREATE-04** — Selecting the "Add «text»…" row calls `IArtistService.CreateArtistAsync(text)`. On success: the returned artist becomes the selected+locked artist (same locked state as picking an existing suggestion), the field shows the created name, and any prior error is cleared.
- **REQ-ACREATE-05** — On create failure (exact-duplicate or invalid name returned by the service), the failure message is surfaced via `ArtistHasError`/`ArtistErrorText`, the typed text is retained, and **no** artist is created. No native dialog is used.
- **REQ-ACREATE-06** — Creating an artist requires **no confirmation prompt** (low-stakes, reversible). Only the artist **name** is captured inline; no other artist fields are collected on the Song form.
- **REQ-ACREATE-07** — Name validation for the inline path is the same single source of truth used elsewhere: `IArtistService.ValidateNameInput` / `CreateArtistAsync` (empty check, max length 60). No validation rule is re-implemented in `SongFormViewModel`.
- **REQ-ACREATE-08** — After a successful inline create + select, saving the song persists the song with the newly created artist's `ArtistId` (the save-time "Artist is required" guard is satisfied).
- **REQ-ACREATE-09** — The existing SongFormViewModel and full test suites remain green (verified by task T6); new behavior is covered by new unit tests (see below). No change to `ArtistService` behavior.
- **REQ-ACREATE-10 (invariant)** — The create affordance appears for **any** non-whitespace typed text, whether or not local matches exist (this ties REQ-ACREATE-02 and -03 together and removes the transition-case ambiguity).
- **REQ-ACREATE-11 (fallback branch)** — If the T1 spike forces **Option B** (on-no-match `DXButton` instead of the synthetic row), REQ-ACREATE-02's affordance becomes that button and it appears on no-match blur; the observable create / lock / error / save behavior (REQ-ACREATE-04, -05, -08) is **unchanged** in either branch.

## Validation rules

- Inline-create name obeys `ArtistService` rules: non-empty after trim, ≤ 60 chars. Violations return a mapped error (REQ-ACREATE-05), no entity created.
- Exact-duplicate names are rejected by `CreateArtistAsync`'s existing `ExistsByNameAsync` guard; the user sees the "already exists" message and can instead pick the existing artist from the list.

## Out of scope

- **Fuzzy / near-duplicate warning** ("Beatles" vs "The Beatles") — no such detection exists anywhere today; parity with the Artist form is accepted for this increment. Tracked as a separate future item.
- Refactoring the duplicated dirty-tracking / character-counter boilerplate into `ViewModelBase` — separate tech-debt task (no-bundling rule).
- Fixing ArtistForm's dead `DuplicateSuggestions` stub — separate gap.
- Collecting any artist field beyond name inline; richer data stays on the Artist form.

## Traceability seed (filled at implementation)

| AC | Implementation location | Test method |
|----|-------------------------|-------------|
| REQ-ACREATE-01 … 09 | `SongFormViewModel` + `SongFormPage.xaml(.cs)` | `SongFormViewModelTests` (Level A) + on-device E2E |
