# Requirements — Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`

> Dated change spec (2026-07-19) under `autocomplete-component` per the SDD Invariant. Parent decision: `../../2026-07-19-dx-autocomplete-adoption-decision.md` (D-AC1). The original custom-component specs remain immutable history.

## Goal

Both autocomplete consumers (SongFormPage Artist field, PersonFormPage Full Name field) use the DevExpress built-in `AutoCompleteEdit` (dropdown-style, async suggestions). Unblocks Critical BUG-027 → Artists & Songs Catalog.

## User stories

- **US-1:** As the admin registering a song, I type an artist name and pick from live suggestions without the field ever losing my typed text, so song creation is possible again (BUG-027).
- **US-2:** As the admin registering a singer, I type a full name and see existing similar persons (dedup suggestions) inline, without navigating away.

## Acceptance criteria

- **REQ-DXAC-01:** SongFormPage Artist field is a `dxe:AutoCompleteEdit` bound to the existing `SongFormViewModel` members (`ArtistSearchText`, `ArtistSuggestions`, `SearchArtistsCommand`, `SelectArtistCommand`, `ArtistBlurredWithoutSelectionCommand`, `HasError`/`ErrorText`, `IsArtistLocked`). Typing past the existing gate shows Service suggestions (max 5).
- **REQ-DXAC-02:** PersonFormPage Full Name field is a `dxe:AutoCompleteEdit` bound to the existing `PersonFormViewModel` members (`PersonName`, `Suggestions`, `SearchPersonsCommand`, `SuggestionSelectedCommand`, `ValidateNameCommand`). Suggestions appear from 2 typed characters (existing min-length gate).
- **REQ-DXAC-03:** Typed text is never cleared or replaced by the control on blur, popup dismiss, focus change, or no-selection — under no circumstance does the user lose their entry (BUG-027 core criterion).
- **REQ-DXAC-04:** Tapping a suggestion executes the existing selection command with the tapped `AutocompleteSuggestion` (Song: sets `SelectedArtistId`/name and locks field per current behavior; Person: existing selection flow).
- **REQ-DXAC-05:** Leaving the field without a selection executes the existing blur/validation command; validation errors render via the DX editor's error properties (not a separate label), matching current messages.
- **REQ-DXAC-06:** DX client-side suggestion filtering is disabled — the suggestion list shows exactly what the Service returned (Service-side normalization per BUG-046 is the single filter; no double filtering).
- **REQ-DXAC-07:** Search calls are debounced: at most one Service call per settled typing pause (target ≈300 ms; DX built-in delay acceptable), and stale results do not overwrite newer ones.
- **REQ-DXAC-08:** All existing `SongFormViewModel`/`PersonFormViewModel` unit tests pass **unchanged** (contract-preservation proof).
- **REQ-DXAC-09:** The BUG-044/045/047 defect-family evaluation checklist (stacked navigation, cursor jump, stale popup) is executed on device on both pages; results recorded in `task-log.md`; any surviving defect gets a BUG row + regression coverage per `bug-tracking.md` severity rules. (Mandated first evaluation step of this spec — decision record 2026-07-19.)
- **REQ-DXAC-10:** On-device smoke test 16C.1 (song registration end-to-end) passes green.
- **REQ-DXAC-11:** The frozen custom component family (`UI/Components/AutocompleteField/` — 8 files) and its 6 test files are **excluded from compilation** but retained in the repo as reference for future guideline ① (Helder decision, this brainstorm). Solution builds with 0 errors afterward.
- **REQ-DXAC-12:** The DX editor visually matches the form-field convention (Outlined `dx:TextEdit` style in `MaterialStyles.xaml`): box mode, border/focus colors, background, text color.

## Out of scope

- "No match → add new" action row (separate BACKLOG row — creatable-autocomplete analysis).
- Shared wrapper component — deferred behind the **promotion trigger** in `design.md § Wrapper promotion trigger`.
- Queue-form consumers (Venue/Artist/Singer pickers) — future feature; design notes the direction only.
- Full-screen autocomplete UX (guideline ①) — retained as documented future enhancement.
- Deleting the frozen component files.

## Validation rules

Unchanged — all validation stays in existing ViewModel/Service logic; this change swaps the visual control only.
