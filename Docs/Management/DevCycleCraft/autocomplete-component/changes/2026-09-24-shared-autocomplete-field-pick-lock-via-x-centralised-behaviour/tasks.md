# Tasks: shared exposed dropdown menu field (pick-lock via X)

> **DRAFT. Not dispatchable until Helder approves `requirements.md` + `design.md` and answers the
> open questions (requirements § Open questions, Q1–Q10).** T1 needs no answers. T3 needs Q2/Q3
> settled or their defaults recorded. T5 needs Q1.
> Every code task runs in a git worktree on a task branch based on `develop`
> (`workflow.md` Rule 2). Docs (this file, task-log, findings) land on `develop`.
> DRY Onion order: Domain (T2) → UI logic (T3) → component (T4) → VM (T5) → page (T6) → device gate (T7).
>
> **Exit steps for every task** (`workflow.md` Rule 2 exit checklist): build with 0 errors → test →
> re-read the files after editing → **`.sln` registration of every file created, moved or deleted under
> `Docs/` (e.g. `findings.md`, `task-log.md` in this folder), in the same commit** → task-log entry →
> commit. Code files are project items, not `.sln` SolutionItems.

## Phase 0: Spike (gates the lock mechanism)

- [ ] **T1 [SPIKE]: On-device lock-mechanism spike, B then C** [SEQUENTIAL]
  - **Time-box:** 2 h hard stop. Throwaway code only: a scratch page or a local, uncommitted edit to
    `SongFormPage.xaml`, discarded afterwards. No production file is committed.
  - **Question:** on DX `AutoCompleteEdit` 25.2.4 (Android, Helder's device), which mechanism gives
    "text not editable, X tappable and styled enabled", without bringing BUG-069 back?
  - **Recording:** every "frames" criterion is measured on a 60 fps screen recording of the device.
  - **B procedure:** while locked, `IsReadOnly=True`, `ClearIconVisibility=Never`, `EndIcon=close_outlined`,
    `IsEndIconVisible=True`, `EndIconColor={StaticResource OnSurfaceVariant}`, and an `EndIconClicked`
    handler that logs, then unlocks.
  - **B passes iff all of B1–B6 hold:**
    - B1: tapping the text of a locked field (edit-mode load) shows no soft keyboard and no cursor in any frame.
    - B2 (add-mode path): right after picking an option from the menu, **with the keyboard still up**,
      10 keystrokes plus 1 long-press paste leave the text byte-equal to the picked name, and 0 frames
      show a typed or pasted character.
    - B3: the End icon renders in `OnSurfaceVariant`, not a dimmed or disabled colour (screenshot beside
      an enabled `TextEdit` clear icon).
    - B4: tapping the End icon fires `EndIconClicked` (log line) on 5 of 5 taps.
    - B5 (**BUG-069 must stay fixed**, REQ-ACF-25): under B, no menu opens on edit-mode load and no
      menu reopens after a pick, over 5 of 5 attempts each.
    - B6: after the handler sets `IsReadOnly=False` and calls `Focus()`, the keyboard opens and typing
      requests options.
  - **C procedure (only if B fails):** `IsReadOnly=False`, built-in clear icon; while locked, revert
    `TextChanged` when `Reason==UserInput`, skip `ItemsRequested`, `Unfocus()` on `Focused`.
  - **C is measured against C1–C5.** Passing them does **not** select C: C is a visible compromise that
    Helder must explicitly accept under Q5 after seeing these numbers (design § Decision: lock mechanism).
    - C1: after 10 keystrokes on a locked field, the text is byte-equal to the locked name, and 0 frames
      show a typed character.
    - C2: a long-press paste, a long-press "select all" followed by a keystroke, and an IME composition
      (Gboard swipe-typing one word, and one accented character via long-press on a key) each leave the
      text byte-equal to the locked name, with 0 frames showing the change.
    - C3: 0 `ItemsRequested` searches while locked (log) and 0 frames with the menu visible.
    - C4: tapping X clears on 5 of 5 taps and is never swallowed by the `Unfocus`.
    - C5: report the number of frames the soft keyboard is visible after a tap on the locked text
      (measured; the threshold is Helder's to set under Q5; the target is 0).
  - **Also record:** that `SubmitIconVisibility=Never` removes the magnifier; DX `SelectedItem`
    highlight after a pick under each mechanism.
  - **Output:** `findings.md` in this folder: evidence per criterion, verdict (B / C-measured / neither),
    and the decision-table row applied. Register `findings.md` in `MyVocaList.sln`.
  - **Failure path:** B fails → `blocked: spike inconclusive`. Escalate Q5 to Helder with the C
    measurements and the options in `design.md`. Do **not** pick one.
  - **Produces:** `findings.md` · **Consumes:** nothing · **Risk:** High (decides the design)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/findings.md`, `MyVocaList.sln`
  - **Demo:** Helder sees, on his device, a picked artist that cannot be typed over, with an X that clears it.
  - **Review lane:** Architectural

## Phase 1: Domain

- [ ] **T2: Add `SearchConstants.AutocompleteDebounceMilliseconds = 300`** [P, needs Q10]
  - Test first (Level B): assert the value is 300, in the style of `SongSuggestionThresholdTests.cs:55`. Red, then Green.
  - **Produces:** the constant + XML doc (cites REQ-ACF-14, REQ-DXAC-07) · **Consumes:** nothing
  - **Risk:** Low (additive const)
  - **Files owned:** `Domain/Constants/SearchConstants.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs` (or new `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs`)
  - **Demo:** `dotnet test` shows the new constant test green.
  - **Review lane:** Standard

## Phase 2: UI logic (Level A, Red first)

- [ ] **T3: `IMenuOptionSource` + `ExposedDropdownMenuLogic` with tests** [SEQUENTIAL, after the T1 verdict]
  - Tester writes `ExposedDropdownMenuLogicTests` first: every rule in `design.md` § 4, each test tagged
    `// [AC] REQ-ACF-NN`: 04, 05a (locked + empty/whitespace text → not effectively locked), 11d, 11e,
    13/16 (trimmed-length gate: " a" → no create-new row; "ab" → row), 21, 22; plus 01 via
    `TextToRestore` **only if C was accepted**. Confirm Red, then implement, then Green.
  - **Produces:** `IMenuOptionSource`, `MenuOptionSource`, `ExposedDropdownMenuLogic`, `PickRoute`
  - **Consumes:** T1 verdict; `AutocompleteSuggestion` (existing); `SearchConstants`
  - **Risk:** Medium (encodes BUG-054a/056 rules that are currently in page code)
  - **Files owned:** `MyVocaList/UI/Models/IMenuOptionSource.cs`, `MyVocaList/UI/Components/Menus/ExposedDropdownMenuLogic.cs`, `MyVocaList.Tests/Unit/Components/ExposedDropdownMenuLogicTests.cs`
  - **Demo:** `dotnet test` shows the new logic tests green; the full suite stays green.
  - **Review lane:** Standard

## Phase 3: Component

- [ ] **T4: `ExposedDropdownMenu` component (XAML + code-behind)** [SEQUENTIAL, after T2 + T3]
  - Build the `ContentView` per `design.md` § 3.1, § 5 (the mechanism chosen after T1; always driven by
    `EffectiveLocked`), § 5.2 (X colour tokens + `Error` trigger) and § 5.3 (`SubmitIconVisibility=Never`,
    subject to Q8). Move the option `ItemTemplate` from `SongFormPage.xaml:45-78`. Threshold and delay via
    `x:Static` `SearchConstants`. Incremental: XAML → build → code-behind → build. Level C (no mandatory
    test; record the no-test decision in the task-log). The logic is already covered by T3.
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
  - **Demo:** `dotnet test` green; blurring an unmatched artist (on device, after T6) shows the new copy.
  - **Review lane:** Standard

## Phase 5: Page migration

- [ ] **T6: Migrate `SongFormPage` onto `ExposedDropdownMenu`** [SEQUENTIAL, after T4 + T5]
  - One task, two files, so that the task commits only a green build (`workflow.md` Rule 3).
    Steps inside the task (incremental XAML rule):
    1. Edit `SongFormPage.xaml`: replace `:28-79` with `<menus:ExposedDropdownMenu …/>`, which only binds
       (design § 3.2 table), and add the `xmlns:menus` declaration.
    2. Build. The only expected errors are the `artistEdit` references in `SongFormPage.xaml.cs`
       (`:38`, `:82`, `:99`, `:105`). Any other error: fix it before step 3.
    3. Edit `SongFormPage.xaml.cs`: delete `:38` (the BUG-061 reset, now owned by the component) and
       `:41-113` (the four handlers and their comments). Keep the `CompleteHydration`/`InitializeArtistField`
       calls and all BottomSheet sync code.
    4. Build with 0 errors; full `dotnet test` green.
    5. REQ-ACF-11 checklist, using a Python substring scan (not grep): no `OnArtistItemsRequested`,
       `OnArtistSelectionChanged`, `OnArtistUnfocused`, `OnArtistClearIconClicked`, or `artistEdit` in
       either file.
  - **Produces:** the migrated page · **Consumes:** T4, T5
  - **Risk:** Medium (the page's only field-level XAML change)
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Demo:** the app builds and the Song form opens; the Artist field searches, picks and locks.
  - **Review lane:** Standard

## Phase 6: Device gate

- [ ] **T7: On-device E2E gate (Helder, Android)** [SEQUENTIAL, after T6]
  - Checklist, recorded in the task-log with pass/fail per line:
    1. New song: typing 1 char, or a space plus 1 char → no menu; 2 chars → options + create-new row last (REQ-ACF-13, -16).
    2. Pick an option → the name shows; the menu does not reopen (REQ-ACF-01, -04, -25).
    3. REQ-ACF-05 paths on a locked field, each leaving the text unchanged: (a) tap the text; (b) long-press;
       (c) type 10 keys, including one IME composition; (d) paste; (e) tap another field (blur);
       (f) navigate to another page and back so the form reappears; (g) scroll the form.
    4. X is visible and looks enabled; no magnifier icon (REQ-ACF-07, -08, -10).
    5. Tap X → the field is empty, focused, the keyboard is up, the placeholder shows; type → options (REQ-ACF-06, -09).
    6. Create-new row → the artist is created and locked; a duplicate name → service error, text kept (REQ-ACF-02, -18).
    7. Type an unmatched name and blur → the REQ-ACF-15 copy; text kept (REQ-ACF-15, -17).
    8. Edit a saved song → the artist is locked, no menu opens on load, no highlighted row (REQ-ACF-03, -23, -25).
    9. Song-picker path: choose a song whose artist exists → the Artist field is locked to it, no menu (REQ-ACF-03). Record, without judging, what happens when a locked artist is replaced this way (Q6).
    10. Edit mode: X → pick a different artist → Save → reopen → the new artist shows (REQ-ACF-20).
    11. Field style matches the other Outlined fields (REQ-ACF-27).
  - **Produces:** task-log evidence (register `task-log.md` in `MyVocaList.sln` if it is new); the item's status moves per Helder's verdict
  - **Consumes:** T6 build on device · **Risk:** High (the only check of DX rendering)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`, `MyVocaList.sln`
  - **Demo:** Helder completes lines 1–11 on device without a failure.
  - **Review lane:** Architectural

## Follow-ups (not in this item; register separately if Helder wants them)

- Migrate `PersonFormPage` Full Name (second consumer → governance starts; add to `component-safety-gate.md`).
- Register BUG-070 as a generated row, if Helder wants it tracked separately from this item. BUG-069
  needs nothing: it is fixed, and it never had a row.
- A shared constant for list-search debounce (`CrudListViewModelBase.cs:238`).
- Resolve Q6 (song-picker re-lock), unless the picker pages are deleted first.
