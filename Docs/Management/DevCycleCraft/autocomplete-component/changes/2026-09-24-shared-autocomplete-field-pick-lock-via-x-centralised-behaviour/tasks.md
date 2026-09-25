# Tasks: shared exposed dropdown menu field (pick-lock via X)

> **DRAFT. Not dispatchable until Helder approves `requirements.md` + `design.md` and answers
> Q1–Q5.** T1 needs no answers. T5 needs Q1. T3 needs Q2 and Q3 settled, or the defaults recorded.
> Every code task runs in a git worktree on a task branch based on `develop`
> (`workflow.md` Rule 2). Docs (this file, task-log) land on `develop`.
> DRY Onion order: Domain (T2) → UI logic (T3) → component (T4) → VM (T5) → page (T6, T7) → device gate (T8).

## Phase 0: Spike (gates the lock mechanism)

- [ ] **T1 [SPIKE]: On-device lock-mechanism spike, B then C** [SEQUENTIAL]
  - **Time-box:** 2 h hard stop. Throwaway code only: a scratch page or a local, uncommitted edit to
    `SongFormPage.xaml`, discarded afterwards. No production file is committed.
  - **Question:** on DX `AutoCompleteEdit` 25.2.4 (Android, Helder's device), which mechanism gives
    "text not editable, X tappable and styled enabled"?
  - **B procedure:** `IsReadOnly=True`, `ClearIconVisibility=Never`, `EndIcon=close_outlined`,
    `IsEndIconVisible=True`, `EndIconColor={StaticResource OnSurfaceVariant}`, `EndIconClicked` handler logging.
  - **B passes iff all hold:** (1) tapping the text shows no soft keyboard and no cursor; (2) the End
    icon renders in `OnSurfaceVariant`, not a dimmed/disabled colour (screenshot next to an enabled
    `TextEdit` clear icon); (3) tapping the End icon fires `EndIconClicked` (log line) on 5 of 5 taps;
    (4) after the handler sets `IsReadOnly=False` and calls `Focus()`, the keyboard opens and typing works.
  - **C procedure (only if B fails):** `IsReadOnly=False`, built-in clear icon; while locked, revert
    `TextChanged` when `Reason==UserInput`, skip `ItemsRequested`, `Unfocus()` on `Focused`.
  - **C passes iff all hold:** (1) after 10 keystrokes on a locked field the text equals the locked
    name and no typed character stays visible after the keystroke's frame (screen recording);
    (2) no `ItemsRequested` search runs while locked (log); (3) tapping X clears on 5 of 5 taps and is
    not swallowed by the `Unfocus`; (4) the keyboard stays visible no longer than one flash (Helder judges
    whether this is acceptable).
  - **Also record:** the `SubmitIconVisibility=Never` effect (the magnifier is gone); DX
    `SelectedItem`/menu behaviour after a pick under the chosen mechanism (BUG-069 reproduction).
  - **Output:** `findings.md` in this folder: evidence, verdict B/C/neither, and the
    decision-table row applied (`design.md` § Decision: lock mechanism).
  - **Failure path:** neither passes → `blocked: spike inconclusive`. Escalate Q5 to Helder with the
    three options in `design.md`. Do **not** pick one.
  - **Produces:** `findings.md` · **Consumes:** nothing · **Risk:** High (decides the design)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/findings.md` (+ its `.sln` entry)
  - **Demo:** Helder sees, on his device, a picked artist that cannot be typed over, with an X that clears it.
  - **Review lane:** Architectural

## Phase 1: Domain

- [ ] **T2: Add `SearchConstants.AutocompleteDebounceMilliseconds = 300`** [P]
  - Test first (Level B): assert the value is 300, in the style of `SongSuggestionThresholdTests.cs:55`. Red, then Green.
  - **Produces:** the constant + XML doc (cites REQ-ACF-14, REQ-DXAC-07) · **Consumes:** nothing
  - **Risk:** Low (additive const)
  - **Files owned:** `Domain/Constants/SearchConstants.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs` (or new `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs`)
  - **Demo:** `dotnet test` shows the new constant test green.
  - **Review lane:** Standard

## Phase 2: UI logic (Level A, Red first)

- [ ] **T3: `IMenuOptionSource` + `ExposedDropdownMenuLogic` with tests** [SEQUENTIAL, after T1 verdict]
  - Tester writes `ExposedDropdownMenuLogicTests` first: every rule in `design.md` § 4, each test tagged
    `// [AC] REQ-ACF-NN` (04, 11d, 11e, 16, 21, 22; plus 01 via `TextToRestore` **only if T1 chose C**).
    Confirm Red, then implement, then Green.
  - **Produces:** `IMenuOptionSource`, `MenuOptionSource`, `ExposedDropdownMenuLogic`, `PickRoute`
  - **Consumes:** T1 verdict; `AutocompleteSuggestion` (existing)
  - **Risk:** Medium (encodes BUG-054a/056 rules that are currently in page code)
  - **Files owned:** `MyVocaList/UI/Models/IMenuOptionSource.cs`, `MyVocaList/UI/Components/Menus/ExposedDropdownMenuLogic.cs`, `MyVocaList.Tests/Unit/Components/ExposedDropdownMenuLogicTests.cs`
  - **Demo:** `dotnet test` shows the new logic tests green; the full suite stays green.
  - **Review lane:** Standard

## Phase 3: Component

- [ ] **T4: `ExposedDropdownMenu` component (XAML + code-behind)** [SEQUENTIAL, after T2 + T3]
  - Build the `ContentView` per `design.md` § 3.1, § 5 (the mechanism chosen in T1), § 5.2 (X colour
    tokens + `Error` trigger) and § 5.3 (`SubmitIconVisibility=Never`). Move the option `ItemTemplate`
    from `SongFormPage.xaml:45-78`. Threshold and delay via `x:Static` `SearchConstants`.
    Incremental: XAML → build → code-behind → build. Level C (no mandatory test; record the no-test
    decision in the task-log). The logic is already covered by T3.
  - **Produces:** `ExposedDropdownMenu`
  - **Consumes:** T2 constant, T3 types
  - **Risk:** Medium (DX event wiring; not unit-testable)
  - **Files owned:** `MyVocaList/UI/Components/Menus/ExposedDropdownMenu.xaml`, `MyVocaList/UI/Components/Menus/ExposedDropdownMenu.xaml.cs`
  - **Demo:** the solution builds with 0 errors. The component is not yet placed on any page.
  - **Review lane:** Elevated (new shared component; MD3 review of the icon tokens)

## Phase 4: ViewModel

- [ ] **T5: `SongFormViewModel`: `ArtistOptionSource` + REQ-ACF-15 copy** [SEQUENTIAL, after T3; needs Q1]
  - Tester first: (a) `ArtistOptionSource.SearchAsync` returns exactly `SearchArtistsCoreAsync`'s list
    (`// [AC] REQ-ACF-22`); (b) update the message assertions in the existing blur/Save tests
    (`ArtistBlurredWithoutSelection_NoPriorSelection_SetsErrorText`, the Save "no artist" test and
    any sibling asserting the old string) to Helder's chosen copy (`// [AC] REQ-ACF-15`). This is a
    spec-driven test change made by the Tester; the Builder never edits tests. Confirm Red, implement,
    Green. All other artist tests stay unchanged (requirements § G).
  - **Produces:** `ArtistOptionSource` property; new copy at `SongFormViewModel.cs:378` and `:570`
  - **Consumes:** T3 `MenuOptionSource`
  - **Risk:** Medium (a user-visible string + the search contract)
  - **Files owned:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs`
  - **Demo:** `dotnet test` green; blurring an unmatched artist (on device, after T7) shows the new copy.
  - **Review lane:** Standard

## Phase 5: Page migration (one file at a time)

- [ ] **T6: `SongFormPage.xaml`: swap in `ExposedDropdownMenu`** [SEQUENTIAL, after T4 + T5]
  - Replace `SongFormPage.xaml:28-79` with `<menus:ExposedDropdownMenu …/>`, which only binds (design
    § 3.2 table). Add the `xmlns:menus` declaration. Build: the only expected errors are the
    `artistEdit` references in `SongFormPage.xaml.cs` (`:38`, `:82`, `:99`, `:105`). They are fixed
    in T7, which is the same worktree and the immediate next step. No other XAML file is touched.
  - **Produces:** the migrated page XAML · **Consumes:** T4, T5
  - **Risk:** Medium (the incremental-XAML rule applies; single file)
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
  - **Demo:** the XAML compiles; the remaining errors are exactly the `artistEdit` code-behind references.
  - **Review lane:** Standard

- [ ] **T7: `SongFormPage.xaml.cs`: delete autocomplete glue** [SEQUENTIAL, directly after T6]
  - Delete `:38` (the BUG-061 reset, now owned by the component) and `:41-113` (the four handlers and
    their comments). Keep `CompleteHydration`/`InitializeArtistField` calls and all BottomSheet sync
    code. Build with 0 errors; full `dotnet test` green. Run the REQ-ACF-11 verification checklist
    (no `OnArtist*` autocomplete handlers, no `artistEdit.SelectedItem`) with a Python substring scan,
    not grep.
  - **Produces:** the thinned code-behind · **Consumes:** T6
  - **Risk:** Low
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Demo:** the app builds and the Song form opens; the Artist field searches, picks and locks.
  - **Review lane:** Standard

## Phase 6: Device gate

- [ ] **T8: On-device E2E gate (Helder, Android)** [SEQUENTIAL, after T7]
  - Checklist, recorded in the task-log with pass/fail per line:
    1. New song: typing 1 char → no menu; 2 chars → options + create-new row last (REQ-ACF-13, -16).
    2. Pick an option → the name shows; typing/tapping cannot change it; the menu does not reopen (REQ-ACF-01, -04, -25).
    3. X is visible and looks enabled; no magnifier icon (REQ-ACF-07, -08, -10).
    4. Tap X → the field is empty, focused, the keyboard is up, the placeholder shows; type → options (REQ-ACF-06, -09).
    5. Create-new row → the artist is created and locked; a duplicate name → service error, text kept (REQ-ACF-02, -18).
    6. Type an unmatched name and blur → the REQ-ACF-15 copy; text kept (REQ-ACF-15, -17).
    7. Edit a saved song → the artist is locked, no menu opens on load, no highlighted row (REQ-ACF-03, -23, -25).
    8. Edit mode: X → pick a different artist → Save → reopen → the new artist shows (REQ-ACF-20).
    9. Field style matches the other Outlined fields (REQ-ACF-27).
  - **Produces:** task-log evidence; the item's status moves per Helder's verdict
  - **Consumes:** T7 build on device · **Risk:** High (the only check of DX rendering)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`
  - **Demo:** Helder completes lines 1–9 on device without a failure.
  - **Review lane:** Architectural

## Follow-ups (not in this item; register separately if Helder wants them)

- Migrate `PersonFormPage` Full Name (second consumer → governance starts; add to `component-safety-gate.md`).
- Register BUG-069 / BUG-070 as generated rows, if they are not closed by this item.
- A shared constant for list-search debounce (`CrudListViewModelBase.cs:238`).
