# Requirements: AutocompleteDXField, the shared autocomplete field (pick-lock via X, centralised behaviour)

> **Status: DRAFT, revised 2026-09-24 after Helder's two rounds of rulings** (recorded verbatim in
> § Helder rulings below). Drafted by a subagent from Helder's on-device requirements in `README.md`
> (same folder).
>
> Dated change spec under `autocomplete-component`, per the SDD Invariant. The shipped specs it
> extends or supersedes are immutable and are **not** edited (see § Supersedes).
>
> Design: `design.md`. Tasks: `tasks.md` (same folder).
> **Scope after the rulings:** this item builds the component and migrates **`PersonFormPage`**
> (first consumer). **`SongFormPage` is not migrated here**: the song-form title-first item
> (`BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/`)
> builds the new Song form, with its Title and Artist fields, on this component.

## Goal

One reusable field component owns all autocomplete behaviour for the app. Once the user picks an
option, its text is read-only until the user taps X. X looks enabled. Debounce and minimum-query
length come from shared constants. There is no "create new" row. The Person form's Full Name is the
first consumer.

## Domain vocabulary

Every agent uses these terms exactly. No synonyms.

| Term | Definition |
|------|------------|
| **Field** | The shared component this item builds, `AutocompleteDXField`. It implements the MD3/Material *exposed dropdown menu* pattern (a text field with a menu of options under it). Code name and folder: `design.md` § 2. |
| **Option** | One row in the field's menu: a match returned by the consumer's search. The `AutocompleteSuggestion` record (`Contracts/Models/AutocompleteSuggestion.cs`). |
| **Pick** | The user taps an option. |
| **Picked state / locked** | The field holds a picked value, so its text is read-only. Entered by a pick, or when the consumer sets `IsLocked = true` programmatically (e.g. edit-mode hydration). Left only by X. |
| **Unlocked state** | Not locked. The text is editable. If search is enabled, typing searches. |
| **Read-only** | The page-controlled state (`IsReadOnly`) in which the text is shown but cannot be changed, and there is no X and no search. Overrides locked and search enabled. |
| **Search enabled** | The page-controlled switch (`IsSearchEnabled`) that turns option requests and the menu on or off, while keeping the field as a text field. |
| **Clear icon (X)** | The trailing icon that removes the value (MD3 text-field *trailing icon*). In DevExpress: the built-in `ClearIcon`, or the substitute chosen in `design.md` § 5. |
| **Submit icon** | The DevExpress `AutoCompleteEdit` default trailing magnifier icon (`SubmitIcon`/`SubmitIconVisibility`). |
| **Debounce delay** | The pause after the last keystroke before the field requests options (DX `AsyncItemsSourceProvider.RequestDelay`). |
| **Minimum query length** | The trimmed character count below which the field requests no options. |
| **Blur** | The field loses focus (`Unfocused`). |

## User stories

- **US-1:** As the admin, once I have picked a value from a field's suggestions, I want its text
  protected from accidental edits, so that a stray tap or keystroke cannot silently change it.
- **US-2:** As the admin, I want one obvious control (X) to change a picked value, and I want it to
  look tappable.
- **US-3:** As the admin, I want every autocomplete field in the app to behave the same way.
- **US-4:** As a developer, I want autocomplete glue in one component and not in each page's
  code-behind, so that a fix lands once and pages only bind.
- **US-5:** As the admin registering a singer, I type a full name and see existing people with that
  name (dedup) in the new field, with no change to what picking one does.

## Acceptance criteria

### A. Pick-lock (US-1): on for every instance, no opt-out

- **REQ-ACF-01: A pick locks the text.**
  Given an unlocked field with search enabled,
  When the user picks an option,
  Then the component routes the pick to the consumer's select command, enters the picked state
  (`IsLocked = true`, written back to the consumer through the TwoWay binding), and the text cannot
  be changed by input: after any keystroke or paste, the text still equals the value shown at pick time.
  The system SHALL NOT offer a property that turns pick-lock off (Helder ruling Q3).
- **REQ-ACF-02: Programmatic lock counts as picked.**
  WHEN a consumer sets `IsLocked = true` while the text is non-empty (e.g. edit-mode hydration),
  the system SHALL apply the picked state exactly as after a user pick (REQ-ACF-01 read-only rule,
  REQ-ACF-04), and SHALL NOT issue a search for the programmatic text (BUG-065/066 carry-over).
- **REQ-ACF-03: A field that was never picked stays editable.**
  Given a field whose `IsLocked` is `false` and that no pick has locked (e.g. the Person form in
  edit mode showing an existing name),
  Then the text is editable. Pick-lock applies only after a pick or a programmatic lock.
- **REQ-ACF-04: While picked, no menu and no search.**
  WHILE the field is in the picked state, the system SHALL NOT request options, SHALL NOT call the
  consumer's search, and SHALL keep the menu closed (`IsDropDownOpen == false`). This keeps BUG-069
  fixed (REQ-ACF-25): no menu reopens after a pick, and none opens on an edit-mode page load.
- **REQ-ACF-05: X is the only user unlock.**
  None of the following user actions SHALL move the field from picked to unlocked, or change its
  text. Each is a test case in the spike (T1), and is re-verified on device by the song-form item (the Person
  form cannot hold a picked state on screen, because its pick navigates away):
  (a) tapping the text; (b) long-pressing the text (selection handles, paste menu); (c) typing on
  the soft keyboard, including IME composition; (d) pasting; (e) blur by tapping another field;
  (f) navigating away and back so the page reappears; (g) scrolling the page.
  Programmatic changes by the consumer (setting `IsLocked` or `Text`) are not user unlocks.
- **REQ-ACF-05a: Locked-and-empty is not a state.**
  IF `IsLocked` is `true` and the text is empty or whitespace, THEN the component SHALL treat the
  field as unlocked: text editable, X hidden per REQ-ACF-07, options requested if search is enabled.
  Reason: `SongFormViewModel.cs:469-477` sets the lock even when `song.ArtistId == 0`, and a future
  consumer could do the same. Without this rule the field would be read-only with no X. Enforced in
  the component (`AutocompleteDXFieldLogic.Mode`, design § 4) for every consumer.
- **REQ-ACF-06: After X, the field is empty, unlocked and focused (Helder ruling Q7).**
  Given the field is in the picked state,
  When the user taps X,
  Then the text is empty, `IsLocked` is `false`, the consumer's clear command (if bound) has run,
  the field has focus, the soft keyboard is shown (Android), the placeholder is visible, and the next
  typed characters search if search is enabled.

### B. X icon (US-2)

- **REQ-ACF-07: X is visible whenever there is a value.**
  WHILE the text is non-empty (picked or unlocked), the system SHALL show X. WHILE the text is
  empty, it SHALL NOT.
- **REQ-ACF-08: X is styled enabled, per MD3.**
  WHILE X is visible and the field is not in error, X SHALL use the `OnSurfaceVariant` token (MD3
  `outlined-text-field.trailing-icon.color`). WHILE the field is in error, it SHALL use `Error` (MD3
  `error-trailing-icon-color`). X SHALL never render in the disabled style (`OnSurface` at 38 %
  opacity) while it is tappable. On-device check: Helder confirms that X in the picked state looks
  distinct from a disabled icon.
- **REQ-ACF-09: X on an unlocked field.**
  WHEN the user taps X on an unlocked field with text, the system SHALL empty the text, run the
  consumer's clear command if bound, and keep focus in the field.

### C. Submit icon (Helder ruling Q8)

- **REQ-ACF-10: No submit icon.**
  The field SHALL NOT show the DevExpress submit (magnifier) icon in any state. Options are
  requested as the user types, so a submit action adds nothing.

### D. Centralisation (US-3, US-4)

- **REQ-ACF-11: One component owns the glue.**
  The component SHALL own all of the following, and no consumer page's code-behind SHALL contain
  any of them:
  (a) the options-request glue (DX `ItemsRequested` handler);
  (b) routing a pick to the consumer's select command;
  (c) forwarding blur-without-pick to the consumer's blur command;
  (d) X handling and forwarding to the consumer's clear command;
  (e) resetting DX `SelectedItem` to `null` after every routed pick and on entering the picked state (BUG-061);
  (f) menu-open control (`IsDropDownOpen`);
  (g) debounce delay and minimum query length (REQ-ACF-13/14);
  (h) the option row template (`Headline` + optional `SupportingText`).
  The system SHALL have no create-new row and no create concept (Helder ruling Q1). Creating
  entities from unpicked text is the consumer's job, on Save (song-form item).
  Verification for the Person form: after migration, `PersonFormPage.xaml.cs` contains no
  `OnNameItemsRequested`, `OnNameSelectionChanged` or `OnNameUnfocused`, and no
  `nameField.SelectedItem`.
- **REQ-ACF-12: Business rules stay in ViewModels/Services.**
  The component SHALL NOT validate, create entities, navigate or compose messages. It forwards to
  consumer commands only.
- **REQ-ACF-12a: The page can turn searching off.**
  WHILE `IsSearchEnabled` is `false`, the system SHALL request no options, call no search, and keep
  the menu closed. The field SHALL otherwise behave as a text field: pick-lock state, X, error
  display and blur forwarding are unchanged. Default `true`.
  Need: the song-form item turns the Title search off in edit mode (its README, third round,
  decision 3). Given a field with `IsSearchEnabled = false`, When the user types 5 characters,
  Then 0 search calls reach the consumer and the menu never opens.
- **REQ-ACF-12b: The page can make the field read-only.**
  WHILE `IsReadOnly` is `true`, the system SHALL show the text; accept no typing, paste or edit;
  show **no** X; request no options, call no search, and keep the menu closed; and SHALL NOT forward
  blur. **Read-only wins:** it overrides `IsLocked` and `IsSearchEnabled` (a read-only field behaves
  identically whatever their values). When `IsReadOnly` returns to `false`, the field resumes the state
  given by `IsLocked`/`IsSearchEnabled`. Default `false`. The field is rendered in the normal (not
  disabled) style, because the value is meaningful content, not an unavailable control.
  Need: the song-form item's edit mode shows an API-sourced song's Title read-only, with no way to
  change it (its README, third round, decision 3). Added now, before a second consumer binds, so that
  it does not require a governed-component change later.
  Given a field with `IsReadOnly = true` and text "Don't Cry",
  When the user taps, long-presses or types in it,
  Then the text stays "Don't Cry", no X is visible in any frame, 0 search calls are made, and no menu opens.

### E. Shared constants (Helder rulings Q2, Q10)

- **REQ-ACF-13: Minimum query length is the shared local constant, on trimmed text.**
  The field SHALL request options only when the **trimmed** text length is at least
  `SearchConstants.MinimumLocalQueryLength` (2, `Domain/Constants/SearchConstants.cs:12`), read from
  the constant, never a literal. Given an unlocked field, When the user types 1 character, or a space
  plus 1 character, Then no options are requested and the menu stays closed. When the trimmed text
  reaches 2 characters, Then options are requested once the debounce delay has passed.
- **REQ-ACF-14: One shared debounce delay.**
  The debounce delay SHALL be `SearchConstants.AutocompleteDebounceMilliseconds = 300` (new), used by
  every instance. No consumer sets its own value. It replaces the literal `RequestDelay="300"` at
  `PersonFormPage.xaml:28` in this item (`SongFormPage.xaml:41` goes away with the song-form item).
  This item owns debounce because Helder reversed decision D8's debounce exclusion on 2026-09-23.

### F. Person form migration (US-5, first consumer)

- **REQ-ACF-15: Person Full Name uses the component, with unchanged behaviour.**
  Given the New Singer form,
  When the user types a trimmed name of at least 2 characters,
  Then after the debounce delay the menu lists up to `SearchConstants.MaxSearchResults` (5) people
  whose name starts with the text (`IPersonService.SearchPersonsStartsWithAsync`), each showing the
  full name and its display identifier.
  When the user picks one, Then the existing selection flow runs unchanged: a replacing navigation to
  that person's edit form (BUG-044, `PersonFormViewModel.cs:295-313`). The field locks for the
  moment before navigation (REQ-ACF-01); the edit form then opens unlocked (REQ-ACF-03).
- **REQ-ACF-16: Person options come from the search's return value.**
  The Person form's option source SHALL return the search's result list directly and SHALL NOT read
  `PersonFormViewModel.Suggestions` after the call (the BUG-056 rule, which the Person glue violates
  today at `PersonFormPage.xaml.cs:44-45`).
- **REQ-ACF-17: Person blur validation is unchanged.**
  WHEN the Full Name field blurs without a pick, the system SHALL run `ValidateNameCommand`
  (the dirty-field rules of the Form Validation Standard), exactly as today (`PersonFormPage.xaml.cs:57-61`).
- **REQ-ACF-18: Create-mode autofocus is kept.**
  WHEN the New Singer form appears (not edit mode), the Full Name field SHALL receive focus
  (`PersonFormPage.xaml.cs:28-30`).

### G. Regression protection (carry-overs)

| ID | Shipped behaviour | Verified by |
|----|-------------------|-------------|
| **REQ-ACF-19** (REQ-DXAC-03) | Typed text is never cleared or replaced by the control on blur, menu dismiss or focus change. | Code review (the component writes `Text` only on X, and on the C-mechanism revert while locked) + T7 on-device |
| **REQ-ACF-20** (BUG-044) | Picking a person replaces the page (no stacked form). | `PersonFormViewModelBug044Tests.SuggestionSelected_NavigatesWithReplacingRelativeRoute_NotAStackedPush` (unchanged) |
| **REQ-ACF-21** (REQ-DXAC-02 gate) | Person search below 2 characters returns nothing. | `PersonFormViewModelTests.SearchPersonsCommand_ShortTerm_SetsEmptySuggestions` (unchanged) |
| **REQ-ACF-22** (BUG-056) | Options come from the search's return value. | T5 VM test (REQ-ACF-16) + code review of the component's `ItemsRequested` glue |
| **REQ-ACF-23** (BUG-061) | No option stays highlighted after a pick. | T7 on-device |
| **REQ-ACF-24** (BUG-065/066) | Programmatic text changes never trigger a search and always close the menu. | T1 (programmatic lock button) + T7 on-device + code review. DX fires no `ItemsRequested` for programmatic text (BUG-065/066 IL evidence), so there is no component logic to unit-test. |
| **REQ-ACF-25** (BUG-069, must stay fixed) | No menu reopens after a pick, and none opens on an edit-mode load. BUG-069 stopped reproducing after the BUG-081 merge (`2d70ac73`), confirmed by Helder on device 2026-09-24 (`Docs/Management/LEDGER.md`, INLINE-AC row, "Update 2026-09-24"; likely a side effect of dropping `IsReadOnly`). Any mechanism that brings `IsReadOnly` back must re-prove it. | T1 criterion B5 + T7 |
| **REQ-ACF-26** (REQ-DXAC-07) | Latest query wins: a stale request never replaces newer options (DX cancellation token honoured after the await). | Code review + T7 |
| **REQ-ACF-27** (REQ-DXAC-06/12) | No DX client-side filtering. The Outlined form-field style (`MaterialStyles.xaml:74`) applies. | Code review + T7 |
| **REQ-ACF-28** (Person name rules) | Character counter, keystroke-while-in-error re-validation and hydration rules on `PersonName` are unchanged. | Existing `PersonFormViewModelTests` name tests (unchanged) |

**Handed over to the song-form title-first item** (not verified here, because `SongFormPage` is not
migrated by this item): REQ-ACREATE-13/14/15/16 behaviours, BUG-054a (moot: no create row), the
BUG-061 edit-load reset (`SongFormPage.xaml.cs:38`), the artist/title lock hydration and the
`SongFormViewModel` artist-flow migration (`ArtistOptionSource`, deleting `SongFormPage.xaml.cs:41-113`).

## Validation rules

Unchanged. Person name rules stay in `IPersonService` (`Validate…Input`). The component validates nothing.

## Failure modes

- **Search throws or returns empty:** the menu stays closed. Search exceptions follow the existing
  VM behaviour (no new catch).
- **Lock mechanism fails on a platform** (the text becomes editable in the picked state): this
  violates REQ-ACF-01. The spike (T1) and device gate (T7) catch it. iOS is not device-tested here.
- **Consumer never binds `IsLocked`** (Person form): the component still locks on a pick and
  unlocks on X by itself (TwoWay property with a local value), so pick-lock holds with no VM code.
- **Person form stays locked on screen after a pick:** if `SuggestionSelectedAsync` returns early
  (`PersonFormViewModel.cs:297`, suggestion data is not a `Person`) or the navigation fails, the Full
  Name field remains in the picked state on the New Singer form. Harmless: X unlocks it (REQ-ACF-06).

## Supersedes (shipped specs are not edited)

| Shipped REQ | File | Effect |
|-------------|------|--------|
| **REQ-ACREATE-02, -03 (create-row part), -10** | `BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/requirements.md` | **Superseded** (Helder ruling Q1): no create-new row anywhere. Unpicked artist text is created on Save, owned by the song-form title-first item. REQ-ACREATE-03's retain-typed-text rule survives as REQ-ACF-19. |
| **REQ-ACREATE-15** (type-over allowance) | same file | **Narrowed:** typing over a picked value is impossible (REQ-ACF-01/05). The X semantics for the Song form are re-specified by the song-form item. |
| **REQ-DXAC-07** (≈300 ms) | `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/requirements.md` | Made concrete by the shared constant (REQ-ACF-14). |
| **DX-AC "No new custom component" / wrapper promotion trigger** | same folder, `design.md` | Triggered early by Helder's direction (README point 4). |
| **BUG-081 fix rationale** ("literally non-editable was an over-restriction") | `BusinessFeatures/artists-songs/bugs/2026-09-23-BUG-081-…/task-log.md` | Reversed by Helder 2026-09-24; the fix's outcome (a live X) is kept. |

## Out of scope

- Migrating `SongFormPage` (owned by the song-form title-first item).
- `ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage` (to be replaced or deleted).
- `ArtistFormPage` duplicate suggestions (`ArtistFormPage.xaml:46-51`): a follow-up candidate.
- The list-page search debounce literal `Task.Delay(400)` (`CrudListViewModelBase.cs:238`): a follow-up candidate.
- Deleting or wiring `ArtistSuggestionService`/`SongSuggestionService` (unregistered on purpose).
- The frozen `UI/Components/AutocompleteField/` family: left as is, per Helder (REQ-DXAC-11 stands).
- iOS device verification.

## Open items

- **Q5 (spike):** as ruled, mechanism C is built only if Helder accepts it after seeing the T1 measurements.
- **Q10 (location only):** Domain `SearchConstants` vs a UI-layer constant for the debounce value.
  The design keeps `SearchConstants` unless Helder rules otherwise.
- **Component name and frozen family:** resolved (see the note below the rulings).

### Helder rulings — 2026-09-24

- **Q2 RESOLVED: threshold 2, applied to the create-new row too.** Helder observed on device that the
  Song form's artist search only returns matches from 2 characters (`SearchConstants.MinimumLocalQueryLength`),
  but the editor requests items from 1 (`CharacterCountThreshold="1"`). Typing a single character
  that matches an existing artist therefore shows **only** the "Add … as a new artist" row, which
  invites duplicate artists. REQ-ACF-16 is no longer conditional: the component requests items and
  offers the create-new row only from trimmed length ≥ `MinimumLocalQueryLength`.
- **Q10 RESOLVED (partly): debounce stays 300 ms, one shared constant for every autocomplete field.**
  Its location (Domain `SearchConstants` vs a UI-layer constant) stays with the design's proposal
  unless Helder rules otherwise.

### Helder rulings — 2026-09-24 (second round)

- **Q1 RESOLVED: no "create new" row in the autocomplete at all.** The ➕ "Add … as a new artist"
  row is removed. An artist typed but not picked is created **on Save**, keeping the existing
  duplicate validation. This makes the BUG-070 message question moot: there is no row to point the
  user at, and the "Search and select an artist from the list" error is removed rather than reworded.
  **Supersedes** shipped inline-create REQ-ACREATE-02/03/10 (create-row behaviour); the
  create-on-save rule belongs to the song-form title-first item. REQ-ACF-15, REQ-ACF-16 and the
  create-row parts of REQ-ACF-21/22 must be rewritten or removed accordingly.
- **Q3 RESOLVED: pick-lock ON everywhere, with no per-page switch.** Change it when a real need
  appears. The Person form gets it too. Drop the opt-out property from the design (YAGNI).
- **Q4 RESOLVED: location is `MyVocaList/UI/Components/`**, following its existing subfolder
  convention (`AppBars/`, `Lists/`, `Sheets/`, `States/`, `Toolbars/`). The frozen
  `Components/AutocompleteField/` family sits there too; the design must say what happens to it.
- **Q5 RESOLVED: as recommended.** Decide C only after seeing the T1 spike measurements.
- **Q6 RESOLVED: moot.** The song picker is replaced by the title-first search.
- **Q7 RESOLVED: yes.** X focuses the field and opens the keyboard.
- **Q8 RESOLVED: yes.** Remove the magnifier.
- **Q9 RESOLVED: as recommended (accept),** and now largely moot, since there is no inline create.

> **Q4 RESOLVED (name), Helder 2026-09-24 (added below the rulings; the ruling text above is unchanged):**
> the component is named `AutocompleteDXField`, at `MyVocaList/UI/Components/TextFields/`. The frozen
> `Components/AutocompleteField/` family is left as is. The MD3-naming exception is proposed in
> `design.md` § Constitutional exception.

> **Numbering note (added after the rulings; the rulings text above is unchanged):** the rulings use
> the previous numbering. Old REQ-ACF-15 (BUG-070 copy), old -16 (create-new row) and old -21
> (BUG-054a create-row suppression) are removed. Old -16's trimmed-threshold rule survives as the
> current REQ-ACF-13. Old -22 (BUG-056) is the current REQ-ACF-22. The current REQ-ACF-15…18 are the
> Person form ACs, and the current -21 is the Person 2-character gate.

## Traceability seed (filled at implementation)

| AC | Implementation location | Test |
|----|-------------------------|------|
| REQ-ACF-01…06, -12a, -12b | component (`design.md` § 4, § 5) | component logic tests (Level A) + T1 spike + T7 on-device |
| REQ-ACF-07/08/10 | component XAML | T7 on-device (DX rendering) |
| REQ-ACF-11/12 | component + `PersonFormPage.xaml(.cs)` | code review checklist (T6) |
| REQ-ACF-13/14 | `SearchConstants` + component | constants test (T2) + component logic test (T3) |
| REQ-ACF-15…18 | `PersonFormViewModel` + `PersonFormPage` | VM tests (T5) + T7 |
| REQ-ACF-19, -24 | component code-behind | code review + T1/T7 on-device |
| REQ-ACF-22 | `PersonFormViewModel.NameOptionSource` + component glue | T5 VM test + code review |
| REQ-ACF-20, -21, -28 | `PersonFormViewModel` (unchanged members) | existing VM tests, unchanged |
| REQ-ACF-23, -25, -26, -27 | component | T1 + T7 on-device, code review |
