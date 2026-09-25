# Requirements: shared exposed dropdown menu field (pick-lock via X, centralised behaviour)

> **Status: DRAFT for Helder's review (2026-09-24).** Drafted by a subagent from Helder's on-device
> requirements recorded in `README.md` (same folder). Items marked `[OPEN — Helder decides]` are not
> decided and block the tasks that depend on them.
>
> Dated change spec under `autocomplete-component`, per the SDD Invariant. The shipped specs it
> extends or narrows are immutable and are **not** edited (see § Supersedes).
>
> Design: `design.md` (same folder). Tasks: `tasks.md` (same folder).

## Goal

One reusable field component owns all autocomplete behaviour for the app. Once the user picks an
option, its text is read-only until the user taps X. X looks enabled. Debounce and minimum-query
length come from shared constants. `SongFormPage`'s Artist field is the first and only consumer
this item migrates.

## Domain vocabulary

Every agent uses these terms exactly. No synonyms.

| Term | Definition |
|------|------------|
| **Field** | The shared component this item builds: an MD3 *editable exposed dropdown menu* (a text field with a menu of options under it). Its code name is in `design.md` § Component name. |
| **Option** | One row in the field's menu: a real match returned by the consumer's search. The `AutocompleteSuggestion` record (`Contracts/Models/AutocompleteSuggestion.cs`). |
| **Create-new row** | The synthetic last row, "Add «typed text» as a new artist", offered when the consumer opts in (REQ-ACREATE-02). An `AutocompleteSuggestion` with `IsCreateNew = true`. |
| **Pick** | The user taps an option or the create-new row, and the consumer ViewModel accepts it (the created artist counts only after the create succeeds). |
| **Picked state / locked** | The field holds a picked value. The consumer ViewModel's lock flag is `true` (`SongFormViewModel.IsArtistLocked`). Programmatic hydration that sets the lock flag counts as picked (edit-mode load, picker-result resolve). |
| **Unlocked / searchable state** | The lock flag is `false`. The text is editable, and typing searches. |
| **Clear icon (X)** | The trailing icon in the text field that removes the value. In MD3 it is the text field's *trailing icon*. In DevExpress it is the built-in `ClearIcon`, or the substitute that `design.md` chooses. |
| **Submit icon** | The DevExpress `AutoCompleteEdit` default trailing magnifier icon (`SubmitIcon`/`SubmitIconVisibility`). This is the "trailing search icon" Helder reported. |
| **Debounce delay** | The pause after the last keystroke before the field requests options (DX `AsyncItemsSourceProvider.RequestDelay`). |
| **Minimum query length** | The number of typed characters below which the field requests no options (DX `CharacterCountThreshold`). |
| **Blur** | The field loses focus (`Unfocused`). |

## User stories

- **US-1:** As the admin registering or editing a song, once I have picked an artist I want the
  artist text to be protected from accidental edits, so that a stray tap or keystroke cannot
  silently change the song's artist.
- **US-2:** As the admin, I want one obvious control (X) to change a picked artist, and I want it
  to look tappable, so that I know how to change it.
- **US-3:** As the admin, I want every autocomplete field in the app to behave the same way, so
  that I learn the pattern once.
- **US-4:** As a developer, I want autocomplete glue to live in one component and not in each page's
  code-behind, so that a fix lands once and pages only bind.

## Acceptance criteria

### A. Pick-lock (US-1)

- **REQ-ACF-01: Pick locks the text (add mode).**
  Given a new song and the Artist field is unlocked,
  When the user picks an existing-artist option,
  Then the field shows the artist's name, the lock flag is `true`, and the text cannot be changed
  by keyboard input: after any keystroke or paste, the field's text still equals the picked name.
- **REQ-ACF-02: Inline create locks the text.**
  Given the Artist field is unlocked and shows the create-new row,
  When the user picks the create-new row and `IArtistService.CreateArtistAsync` succeeds,
  Then the field enters the picked state as in REQ-ACF-01, showing the created artist's name.
  On failure, REQ-ACREATE-05 applies unchanged: the field stays unlocked, the typed text is kept,
  and the service message is shown.
- **REQ-ACF-03: Hydration counts as picked (edit mode).**
  Given a saved song with an artist,
  When `SongFormPage` opens it for editing,
  Then the Artist field shows the stored artist name in the picked state (REQ-ACF-01 read-only
  rule applies), and no search runs (REQ-ACREATE-14 carry-over).
  The same applies when an artist is locked by the song-picker result path
  (`SongFormViewModel.ResolveAndLockArtistAsync`, exact-match branch).
- **REQ-ACF-04: While picked, no menu and no search.**
  WHILE the field is in the picked state, the system SHALL NOT request options, SHALL NOT call the
  consumer's search, and SHALL keep the menu closed (`IsDropDownOpen == false`). This keeps BUG-069
  fixed (REQ-ACF-25): the menu SHALL NOT reopen after a pick, and SHALL NOT open on edit-mode page load.
- **REQ-ACF-05: X is the only user unlock.**
  None of the following user actions SHALL move the field from picked to unlocked, or change its
  text. Each is a test case in T7:
  (a) tapping the text;
  (b) long-pressing the text (selection handles, paste menu);
  (c) typing on the soft keyboard, including IME composition;
  (d) pasting;
  (e) blur by tapping another field;
  (f) navigating back from a pushed page and the form reappearing (`OnAppearing` re-runs
  `InitializeArtistField`, `SongFormPage.xaml.cs:37`);
  (g) scrolling the form.
  Programmatic re-locks by the ViewModel (hydration, picker result) are not user unlocks. The
  song-picker path is Q6.
- **REQ-ACF-05a: Locked-and-empty is not a state.**
  IF the lock flag is `true` and the field text is empty or whitespace, THEN the component SHALL
  treat the field as unlocked: text editable, options requested, X hidden per REQ-ACF-07. This state
  exists today: edit-mode load sets `IsArtistLocked = true` even when `song.ArtistId == 0`
  (`SongFormViewModel.cs:469-477`). Without this rule the field would be read-only with no X, a
  dead end. **Enforced in the component** (`ExposedDropdownMenuLogic.EffectiveLocked`, design § 4),
  so it holds for every consumer. The VM line is not changed by this item.
- **REQ-ACF-06: After X, the field is empty, searchable and focused.**
  Given the field is in the picked state,
  When the user taps X,
  Then the field text is empty, the field is unlocked, the field has focus, the soft keyboard is
  shown (Android), the placeholder "Search artists..." is visible, and the next typed characters
  search as usual.
  *Proposed, needs Helder's confirmation (Q7):* focusing after X is chosen because the only reason
  to tap X is to type a different value. Rejected alternative: leave focus unchanged, which costs an
  extra tap every time.

### B. X icon (US-2)

- **REQ-ACF-07: X is visible whenever there is a value.**
  WHILE the field text is non-empty (picked or unlocked), the system SHALL show X. WHILE the text
  is empty, the system SHALL NOT show X.
- **REQ-ACF-08: X is styled enabled, per MD3.**
  WHILE X is visible and the field is not in error, the X colour SHALL be the `OnSurfaceVariant`
  token (MD3 `outlined-text-field.trailing-icon.color`). WHILE the field is in error, the X colour
  SHALL be the `Error` token (MD3 `error-trailing-icon-color`). X SHALL never render in the
  disabled style (`OnSurface` at 38 % opacity) while it is tappable.
  On-device check: Helder confirms that X in the picked state is visually distinct from a disabled
  icon.
- **REQ-ACF-09: X clears the selection in the ViewModel (REQ-ACREATE-15 semantics kept).**
  When the user taps X, the consumer ViewModel's clear command runs. For `SongFormViewModel.ClearArtist`
  this means: `SelectedArtistId = null`, `SelectedArtistName = null`, `ArtistSearchText = ""`,
  `IsArtistLocked = false`, `IsArtistDropDownOpen = false`, `ArtistSuggestions` empty, and the error
  cleared. A later blur SHALL NOT restore the previous artist (existing test
  `ArtistBlurredWithoutSelection_AfterDeliberateClear_DoesNotRestorePriorArtist` stays green unchanged).

### C. Trailing search (submit) icon

- **REQ-ACF-10: No submit icon.**
  *Proposed, needs Helder's confirmation (Q8). The README said "evaluate removing it".*
  The field SHALL NOT show the DevExpress submit (magnifier) icon in any state. Justification:
  options are requested automatically as the user types (debounce), so a submit action adds nothing,
  and its presence next to X made X read as a second inert icon (README point 2).

### D. Centralisation (US-3, US-4)

- **REQ-ACF-11: One component owns the glue.**
  The field component SHALL own all of the following, and no consumer page's code-behind SHALL
  contain any of them:
  (a) the options-request glue (DX `ItemsRequested` handler);
  (b) appending the create-new row, which is opt-in per consumer;
  (c) create-new-row suppression when locked, or when the query equals the picked name (BUG-054a);
  (d) routing a pick to the consumer's select command or create command;
  (e) forwarding blur-without-pick to the consumer's blur command;
  (f) forwarding X to the consumer's clear command;
  (g) resetting DX `SelectedItem` to `null` after every routed pick and when the field enters the
  picked state (BUG-061);
  (h) menu-open control (`IsDropDownOpen`);
  (i) debounce delay and minimum query length (REQ-ACF-13/14).
  Verification: after migration, `SongFormPage.xaml.cs` contains no handler named
  `OnArtistItemsRequested`, `OnArtistSelectionChanged`, `OnArtistUnfocused` or
  `OnArtistClearIconClicked`, and no reference to `artistEdit.SelectedItem`.
- **REQ-ACF-12: Business rules stay in ViewModels/Services.**
  The field component SHALL NOT decide whether a value is valid, SHALL NOT create entities, and
  SHALL NOT compose validation messages. It forwards to consumer commands only. Lock state is owned
  by the consumer ViewModel and bound into the component, not computed by it.

### E. Shared constants

- **REQ-ACF-13: Minimum query length is the shared local constant.**
  The field's minimum query length SHALL equal `SearchConstants.MinimumLocalQueryLength` (currently
  2, `Domain/Constants/SearchConstants.cs:12`), read from that constant and not written as a literal.
  Given the unlocked Artist field, When the user types 1 character, Then no options are requested and
  the menu stays closed. When the user types a 2nd character, Then options are requested once the
  debounce delay has passed.
  This fixes today's divergence, `CharacterCountThreshold="1"` (`SongFormPage.xaml:42`), whose
  1-character request always returns no matches anyway, because `ArtistService.SearchArtistsByNameAsync`
  rejects queries shorter than the constant (`Services/ArtistService.cs:179`).
  The threshold is measured on the **trimmed** text: leading or trailing whitespace SHALL NOT count
  towards it. For example, " a" (space + 1 letter) SHALL NOT produce a create-new row offering "a".
  DX `CharacterCountThreshold` counts raw characters, so the component also gates the create-new row
  on trimmed length (design § 4).
  **Consequence (needs Helder's ruling, see Open question Q2):** a 1-character typed name no longer
  gets the create-new row.
- **REQ-ACF-14: Debounce delay is a shared constant.**
  The field's debounce delay SHALL be read from one named constant,
  `SearchConstants.AutocompleteDebounceMilliseconds = 300` (new, `Domain/Constants/SearchConstants.cs`),
  replacing the literal `RequestDelay="300"` (`SongFormPage.xaml:41`). No consumer sets its own value.
  Placing a UI-timing constant in the Domain layer is proposed and needs Helder's confirmation (Q10);
  see design § 8.
  This item owns debounce because Helder reversed decision D8's debounce exclusion on 2026-09-23
  (`cross-cutting/read-model-notracking-guidelines/changes/2026-08-24-scope-all-service-reads-through-iunitofwork/design.md` § 2c).

### F. Validation copy (BUG-070) `[OPEN — Helder decides]`

- **REQ-ACF-15: Blur-with-unmatched-text message.**
  Given the Artist field is unlocked and holds typed text that is not a picked artist,
  When the field blurs,
  Then the field shows an error whose copy tells the user both ways forward (pick an existing
  artist, or add the typed name as new). The copy replaces "Search and select an artist from the
  list" in both places it appears today: blur (`SongFormViewModel.cs:378`) and the Save guard
  (`SongFormViewModel.cs:570`).
  **Candidate wordings. Helder picks one, or supplies another:**
  1. "Pick an artist from the list, or choose Add to create it"
  2. "No artist selected. Pick one from the list or add it as new"
  3. "Choose a suggestion, or tap “Add «name» as a new artist”"
  The "Artist is required" message for an empty field (`SongFormViewModel.cs:569`) is unchanged.
  **Also open:** whether the error border should show on blur while the create-new row is still
  available (BUG-070 second half, INLINE-AC `task-log.md`). Default if Helder does not rule: keep
  the error on blur (current trigger) and change only the copy.

### G. Regression protection (carry-overs)

Each row is a shipped behaviour that SHALL still hold after migration. "Test" means the named
existing test stays green **unchanged**, unless the row says otherwise.

| ID | Shipped behaviour | Verified by |
|----|-------------------|-------------|
| **REQ-ACF-16** (REQ-ACREATE-02/10, **conditional on Q2**) | When unlocked and the **trimmed** typed text is at least `MinimumLocalQueryLength`, the create-new row is the last row, with a divider and a leading ➕, whether or not matches exist. If Helder rejects Q2, the threshold stays at 1 for this field and this row reverts to "≥1 non-whitespace char". | Component logic unit test (new, Level A) + on-device E2E |
| **REQ-ACF-17** (REQ-ACREATE-03) | Blur never clears typed text. | `ArtistBlurredWithoutSelection_NoPriorSelection_RetainsText…` |
| **REQ-ACF-18** (REQ-ACREATE-04/05) | Create-new success locks the created artist. Failure keeps the text and shows the service message, with no dialog. | `CreateArtistInline_Success_LocksCreatedArtistAndClearsError` + existing failure test |
| **REQ-ACF-19** (REQ-ACREATE-15) | X unlocks and clears. A later blur does not restore the previous artist. | `ClearArtist_WhenLocked_UnlocksAndClearsSelection`, `ClearArtist_ClosesDropdown`, `ArtistBlurredWithoutSelection_AfterDeliberateClear_DoesNotRestorePriorArtist` |
| **REQ-ACF-20** (REQ-ACREATE-16) | Replacing the artist of a saved song via X, then a pick, then Save persists the new `ArtistId`. | Existing Service/VM/Repository seam tests + on-device E2E |
| **REQ-ACF-21** (BUG-054a) | No create-new row while locked, or when the query equals the picked name. | Component logic unit test (new) |
| **REQ-ACF-22** (BUG-056) | Options come directly from the search's return value, never from reading `ArtistSuggestions` after the call. | Component logic unit test (new) + code review |
| **REQ-ACF-23** (BUG-061) | No option stays highlighted after a pick, or on edit-mode load. | On-device E2E (DX rendering; not VM-testable) |
| **REQ-ACF-24** (BUG-065/066) | Programmatic text changes (lock, hydrate, clear, restore) never trigger a search and always close the menu. | `SelectArtist_ExistingSuggestion_ClosesDropdown`, `InitializeArtistField_WithArtistId_ClosesDropdown`, `ClearArtist_ClosesDropdown`, `ResolveAndLockArtistAsync_*_ClosesDropdown` |
| **REQ-ACF-25** (BUG-069, must stay fixed) | The menu does not reopen after a pick and does not open on edit-mode load. BUG-069 stopped reproducing after the BUG-081 merge (`2d70ac73`), confirmed by Helder on device 2026-09-24 (`Docs/Management/LEDGER.md`, INLINE-AC row, "Update 2026-09-24"; likely a side effect of dropping `IsReadOnly`). Any mechanism that re-introduces `IsReadOnly` must re-prove this (T1). | REQ-ACF-04 + T1 criterion B5 + on-device E2E |
| **REQ-ACF-26** (REQ-ACREATE-13/BUG-051) | Latest query wins. | Existing out-of-order completion test |
| **REQ-ACF-27** (REQ-DXAC-06/12) | No DX client-side filtering. The Outlined form-field style (`MaterialStyles.xaml:74`) still applies. | Code review + on-device E2E |

## Validation rules

Unchanged. Artist-name rules stay in `IArtistService` (`CreateArtistAsync` / `ValidateNameInput`,
non-empty after trim, ≤ 60 chars). The field component validates nothing (REQ-ACF-12).

## Failure modes

- **Search throws or returns empty:** the menu shows the create-new row only, when opted in and the
  text is long enough; otherwise it stays closed. Search exceptions follow the existing VM behaviour
  (no new catch).
- **X tapped while a create is in flight:** the clear command runs, and the create completion then
  locks the created artist (the existing command-guard behaviour). Proposed to accept this (rare,
  reversible with X), pending Helder's confirmation (Q9). Not a new requirement.
- **Lock mechanism fails on a platform** (the text becomes editable in the picked state): this
  violates REQ-ACF-01. The on-device gate (tasks.md T7) catches it. iOS is not device-tested in this
  item (see Out of scope).

## Supersedes (shipped specs are not edited)

| Shipped REQ | File | Effect of this item |
|-------------|------|---------------------|
| **REQ-ACREATE-15** (type-over allowance) | `BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/requirements.md` | **Narrowed.** Its "restore-prior-selection branch applies … when the user re-typed over an existing lock without clearing" is no longer reachable by the user, because typing over a lock is now impossible (REQ-ACF-01/05). The X-unlock-and-clear semantics are kept (REQ-ACF-09). The VM's restore branch (`SongFormViewModel.cs:381-387`) stays as a defensive no-op path; it is not deleted. |
| **REQ-ACREATE-02 / -10** ("≥1 non-whitespace char") | same file | **Narrowed**, pending Q2: the create-new row appears from `MinimumLocalQueryLength` (2) characters, not 1. |
| **REQ-DXAC-07** ("target ≈300 ms; DX built-in delay acceptable") | `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/requirements.md` | **Made concrete:** the value comes from the shared constant (REQ-ACF-14). Behaviour is unchanged. |
| **DX-AC design "No new custom component" / wrapper promotion trigger** | same folder, `design.md` § Wrapper promotion trigger | **Triggered early by Helder's direction** (README point 4), with one consumer at first. |
| **BUG-081 fix rationale** ("making the field literally non-editable was an over-restriction") | `BusinessFeatures/artists-songs/bugs/2026-09-23-BUG-081-…/task-log.md` | **Reversed by Helder, 2026-09-24:** the field must be non-editable while picked, and X must stay live. The BUG-081 outcome (X usable in edit mode) is kept. |

## Out of scope

- `ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage`: expected to be deleted. No investment.
- Migrating any consumer other than `SongFormPage`'s Artist field. Follow-ups, from the census in
  `design.md`:
  - `PersonFormPage` Full Name (`PersonFormPage.xaml:19`). Note that its pick navigates to the
    existing person (dedup), so pick-lock may not apply (Q3).
  - `ArtistFormPage` duplicate suggestions (`ArtistFormPage.xaml:46-51`, BindableLayout list, not an
    autocomplete today).
  - The future queue-entry form (Venue/Artist/Singer), per the DX-AC design § Wrapper promotion trigger.
- The list-page search debounce literal `Task.Delay(400)` (`CrudListViewModelBase.cs:238`). That is list
  search, not autocomplete; it is a candidate follow-up so it can share a constant.
- Deleting or wiring `ArtistSuggestionService`/`SongSuggestionService` (unregistered on purpose;
  pre-built for future form autocomplete).
- Deleting the frozen `UI/Components/AutocompleteField/` family (REQ-DXAC-11).
- iOS device verification (Helder verifies on Android). The design must not rely on an
  Android-only API.
- Moving the artist validation messages into `IArtistService` (the Form Validation Standard in
  `.claude/library/dialogs-validation.md` § Form Validation Standard puts messages in services, but
  these two strings live in the VM today). This is a pre-existing deviation, noted and not fixed here.

## Open questions for Helder

- **Q1 (BUG-070 copy):** pick one wording in REQ-ACF-15, and rule on whether the error border shows
  while the create-new row is available.
- **Q2 (threshold vs inline create):** accept that 1-character artist names can no longer be
  inline-created from the Song form (they can still be created on the Artist form)? Recommended: yes,
  consistent with REQ-UOW-51.
- **Q3 (app-wide default):** the README says pick-lock is the default for every autocomplete field.
  `PersonFormPage`'s Full Name is a free-text field whose pick navigates away (dedup). Confirm that
  pick-lock is on by default but can be switched off per consumer (design exposes it as a property),
  or rule otherwise.
- **Q4 (component name):** `design.md` proposes `ExposedDropdownMenu` in a new `Components/Menus/`
  folder. Two caveats: (a) the evidence is Material Components Android `docs/components/Menu.md`, not
  m3.material.io, which CLAUDE.md names as the source (m3.material.io renders client-side and could
  not be fetched as text on 2026-09-24); (b) `Menus/` files what is visually a text field under
  menus. Confirm, or rename (e.g. file it under a `TextFields/` folder).
- **Q5 (spike outcome):** if B fails T1, mechanism C is **not** an automatic fallback. It is a
  visible compromise (the keyboard can open, and the field briefly takes focus) that Helder must
  accept after seeing the T1 measurements. If C is not accepted, or also fails, choose among the
  escalation options in `design.md` § Decision: lock mechanism.
- **Q6 (song-picker path vs "X is the only way"):** `SongFormViewModel.ResolveAndLockArtistAsync`
  (`:516-538`) re-locks a picked artist to a *different* artist when a song is chosen in the song
  picker, and its no-match branch unlocks the field (`:537`), all without X. This conflicts with
  REQ-ACF-05. Is the picker allowed to replace a locked artist, should it require X first, or is it
  moot because the picker pages are expected to be deleted? Not decided in this spec.
- **Q7:** confirm that X focuses the field and opens the keyboard (REQ-ACF-06).
- **Q8:** confirm removing the magnifier outright (REQ-ACF-10). The README said "evaluate".
- **Q9:** confirm accepting the race where X tapped during an in-flight create ends locked to the
  created artist (§ Failure modes).
- **Q10:** confirm placing the debounce constant in `Domain/Constants/SearchConstants.cs`, a Domain
  file holding a UI-timing value (design § 8).

## Traceability seed (filled at implementation)

| AC | Implementation location | Test |
|----|-------------------------|------|
| REQ-ACF-01…06 | component (`design.md` § Lock mechanism) + `SongFormViewModel` lock members | component logic tests (Level A) + T7 on-device |
| REQ-ACF-07/08/10 | component XAML | T7 on-device (DX rendering) |
| REQ-ACF-09, -17…-20, -24, -26 | `SongFormViewModel` (unchanged members) | existing VM tests, unchanged |
| REQ-ACF-11/12 | component + `SongFormPage.xaml(.cs)` | code review checklist (T6) |
| REQ-ACF-13/14 | `SearchConstants` + component | constants test + component logic test |
| REQ-ACF-15 | `SongFormViewModel` messages | VM tests updated with the spec (T5) |
| REQ-ACF-16, -21, -22 | component logic class | component logic tests |
| REQ-ACF-23, -25, -27 | component | T7 on-device |
