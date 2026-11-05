# Tasks: AutocompleteDXField, the shared autocomplete field (pick-lock via X)

> **DRAFT, revised 2026-09-24 after Helder's rulings.** Not dispatchable until Helder approves the
> revised `requirements.md` + `design.md`. T1 needs no further answers.
> **Scope:** component + `PersonFormPage` migration (first consumer). `SongFormPage` is **not**
> migrated here (see § Handed over).
> Every code task runs in a git worktree on a task branch based on `develop` (`workflow.md` Rule 2).
> Docs (this file, task-log, findings) land on `develop`.
> DRY Onion order: Domain (T2) → UI logic (T3) → component (T4) → VM (T5) → page (T6) → device gate (T7).
>
> **Exit steps for every task** (`workflow.md` Rule 2 exit checklist): build with 0 errors → test →
> re-read the edited files → **`.sln` registration of every file created, moved or deleted under
> `Docs/` (e.g. `findings.md`, `task-log.md` in this folder), in the same commit** → task-log entry →
> commit. Code files are project items, not `.sln` SolutionItems.

## Phase 0: Spike (gates the lock mechanism)

- [x] **T1 [SPIKE]: On-device lock-mechanism spike, B then C** — verdict B 2026-11-03 (`findings.md` § T1c) [SEQUENTIAL]
  - **Where:** a **throwaway test page**, never committed: one `dxe:AutoCompleteEdit` fed by a static
    in-memory list of 5 options, plus a "lock programmatically" button that sets text + lock, to
    simulate edit-mode hydration. Criterion B5 (BUG-069) is also checked on a **local, uncommitted edit
    of `SongFormPage`** (edit a saved song), because that page is where BUG-069 occurred. Both are
    discarded after the spike.
    *Why not the Person form:* its pick navigates away (`PersonFormViewModel.cs:295-313`), so the locked
    state is never on screen long enough to test.
  - **Time-box:** 2 h hard stop. No production file is committed.
  - **Question:** on DX `AutoCompleteEdit` 25.2.4 (Android, Helder's device), which mechanism gives
    "text not editable, X tappable and styled enabled", without bringing BUG-069 back?
  - **Recording:** every "frames" criterion is measured on a 60 fps screen recording of the device.
  - **B procedure:** while locked, `IsReadOnly=True`, `ClearIconVisibility=Never`, `EndIcon=close_outlined`,
    `IsEndIconVisible=True`, `EndIconColor={StaticResource OnSurfaceVariant}`, and an `EndIconClicked`
    handler that logs, then unlocks.
  - **B passes iff all of B1–B6 hold:**
    - B1: after a programmatic lock, tapping the text shows no soft keyboard and no cursor in any frame.
    - B2 (right after a menu pick, **keyboard still up**): 10 keystrokes plus 1 long-press paste leave
      the text byte-equal to the picked value, and 0 frames show a typed or pasted character.
    - B3: the End icon renders in `OnSurfaceVariant`, not dimmed or disabled (screenshot beside an
      enabled `TextEdit` clear icon). **Also:** the End-icon X (locked) and DX's built-in clear icon
      (unlocked) render identically in glyph, size, position and colour (side-by-side screenshots).
      If they differ, record it: the design then uses the End icon as X in both states.
    - B4: tapping the End icon fires `EndIconClicked` (log line) on 5 of 5 taps.
    - B5 (**BUG-069 must stay fixed**, REQ-ACF-25): under B, no menu opens on a programmatic lock or
      on `SongFormPage` edit-load, and no menu reopens after a pick, on 5 of 5 attempts each.
    - B6: after the handler sets `IsReadOnly=False` and focuses the editor, the keyboard opens and
      typing requests options.
  - **Read-only check (REQ-ACF-12b; whatever B/C's outcome):** with `IsReadOnly=True`,
    `ClearIconVisibility=Never`, `IsEndIconVisible=False` and text set:
    - R1: the text is visible in the normal (not disabled) text colour, and **no X** is shown in any frame.
    - R2: tap, long-press and 10 keystrokes leave the text byte-equal, with no keyboard and no cursor in any frame.
    - R3: no menu opens on load or on tap (5 of 5); this is the BUG-069 exposure again, because `IsReadOnly` is back.
    If R1–R3 fail, report it to Helder: the song-form item's read-only Title depends on it.
  - **Supporting-text check (REQ-ACF-12c):** S1: DX `HelpText="Online search unavailable"` renders
    under the field in the unlocked, locked and read-only states (screenshots). S2: with `HasError=True`
    and the component rule applied (`HelpText=""`), only `ErrorText` shows, in one line, with no layout
    jump beyond DX's own bottom-text line. Record whether DX itself hides `HelpText` while `HasError`.
  - **C procedure (only if B fails):** `IsReadOnly=False`, built-in clear icon; while locked, revert
    `TextChanged` when `Reason==UserInput`, skip `ItemsRequested`, `Unfocus()` on `Focused`.
  - **C is measured against C1–C5.** Passing them does **not** select C: it is a visible compromise
    that Helder accepts or rejects after seeing the numbers (Q5, ruled "as recommended").
    - C1: 10 keystrokes on a locked field leave the text byte-equal to the locked value, with 0 frames
      showing a typed character.
    - C2: a long-press paste, a long-press "select all" followed by a keystroke, and an IME composition
      (Gboard swipe-typing one word; one accented character via long-press on a key) each leave the
      text byte-equal, with 0 frames showing the change.
    - C3: 0 `ItemsRequested` searches while locked (log) and 0 frames with the menu visible.
    - C4: X clears on 5 of 5 taps and is never swallowed by the `Unfocus`.
    - C5: frames the soft keyboard is visible after a tap on the locked text (measured; target 0;
      Helder sets the threshold).
  - **Also record:** that `SubmitIconVisibility=Never` removes the magnifier; `SelectedItem` highlight
    after a pick under each mechanism.
  - **Output:** `findings.md` in this folder: evidence per criterion, the verdict (B / C-measured /
    neither), and the decision-table row applied. Register `findings.md` in `MyVocaList.sln`.
  - **Failure path:** B fails → `blocked: spike inconclusive`. Escalate to Helder with the C
    measurements and the options in `design.md` § Decision: lock mechanism. Do **not** pick one.
  - **Produces:** `findings.md` · **Consumes:** nothing · **Risk:** High (decides the design)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/findings.md`, `MyVocaList.sln`
  - **Demo:** Helder sees, on his device, a picked value that cannot be typed over, with an X that clears it.
  - **Review lane:** Architectural
  > **Spec updated 2026-09-25:** (plan § 0.3 items 3, 4) T1 is split into T1a (implementor builds the
  > throwaway page), T1b (Helder runs it on device) and T1c (orchestrator records the verdict). Local
  > `spike:` commits are allowed on the throwaway branch `spike/acf-t1-lock-mechanism` so Helder deploys
  > a known state; the branch is never pushed or merged, and is deleted at T1c. `findings.md` and the
  > `MyVocaList.sln` registration are owned by the **orchestrator** on `develop` (Step 0 skeleton, T1c
  > results), not by the T1 implementor, to keep the `.sln` single-writer.

## Phase 1: Domain

- [x] **T2: Add `SearchConstants.AutocompleteDebounceMilliseconds = 300`** [P] *(merged to develop 2026-09-25, `78b2b735`)*
  - Test first (Level B): assert the value is 300, in the style of `SongSuggestionThresholdTests.cs:55`. Red, then Green.
  - The value is ruled (Q10). The location stays `SearchConstants` unless Helder rules otherwise before dispatch.
  - **Produces:** the constant + XML doc (cites REQ-ACF-14, REQ-DXAC-07) · **Consumes:** nothing
  - **Risk:** Low (additive const)
  - **Files owned:** `Domain/Constants/SearchConstants.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs` (or new `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs`)
  - **Demo:** `dotnet test` shows the new constant test green.
  - **Review lane:** Standard

## Phase 2: UI logic (Level A, Red first)

- [x] **T3: `IMenuOptionSource` + `AutocompleteDXFieldLogic` with tests** [SEQUENTIAL, after the T1 verdict] *(mechanism-independent part merged to develop 2026-09-26, `83065e07`; see note below)*
  - Tester writes `AutocompleteDXFieldLogicTests` first: every rule in `design.md` § 4, each test tagged
    `// [AC] REQ-ACF-NN`: 01 (`LockAfterPick`), 04, 05a (locked + empty/whitespace → not effectively
    locked), 11b, 11c/17, 12a (search off → no request), 12b (read-only wins over locked and search; no X, no request, no blur forwarding), 12c (`BottomSupportingText`: error wins), 13 (" a" and "a" → no request; "ab" → request),
    19, 22, 24; plus 01 via `TextToRestore` **only if Helder accepted C**. Confirm Red, implement, Green.
  - **Produces:** `IMenuOptionSource`, `MenuOptionSource`, `AutocompleteDXFieldLogic`, `PickRoute`
  - **Consumes:** T1 verdict; `AutocompleteSuggestion`; `SearchConstants`
  - **Risk:** High (Level A logic: every component rule, including lock and read-only precedence)
  - **Files owned:** `MyVocaList/UI/Models/IMenuOptionSource.cs`, `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`, `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
  - **Demo:** `dotnet test` shows the new logic tests green; the full suite stays green.
  - **Review lane:** Standard
  > **Spec updated 2026-09-25:** (plan § 0.3 items 1, 2) T3 is split. **T3** (mechanism-independent:
  > `IMenuOptionSource`, `MenuOptionSource`, `FieldMode`, `PickRoute` and every `design.md` § 4 function
  > except `TextToRestore`) runs in Wave 1 before the T1 verdict. **T3-C** (`TextToRestore`) runs only if
  > Helder accepts mechanism C, otherwise it is marked `[CANCELLED: T1 verdict B]`. AC tags: REQ-ACF-19
  > and -24 are proven by code review and on device (and by T3-C unit tests under C); REQ-ACF-22 is
  > proven at the VM seam (T5), per `design.md` § 4 and § 10, which are more specific than the list above.

- [CANCELLED: T1 verdict B — mechanism C not needed] **T3-C: `TextToRestore` (only if Helder accepted mechanism C)** [SEQUENTIAL, after the T1 verdict + T3 merged]
  - See `plan.md` § Task T3-C. Skipped entirely under B.
  - **Files owned:** `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`, `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
  - **Review lane:** Standard

## Phase 3: Component

- [x] **T4: `AutocompleteDXField` component (XAML + code-behind)** [SEQUENTIAL, after T2 + T3] — merged `0d74b4a6` 2026-11-03, Verifier PASS
  - Build the `ContentView` per `design.md` § 3.1 (including `IsSearchEnabled` and `FocusEditor()`;
    no create or opt-out surface), § 5 (the mechanism chosen after T1, driven by `Mode`), `IsReadOnly` (REQ-ACF-12b: inner `IsReadOnly=true`, no clear/end icon),
    § 5.2 (X colour tokens + `Error` trigger), § 5.3 (`SubmitIconVisibility=Never`) and § 6 (X
    handling: empty text, unlock, clear command, focus). Option template = Headline + optional
    SupportingText. Threshold and delay via `x:Static` `SearchConstants`. Incremental: XAML → build →
    code-behind → build. Level C (record the no-test decision in the task-log).
  - **Produces:** `AutocompleteDXField` · **Consumes:** T2 constant, T3 types
  - **Risk:** Medium (DX event wiring; not unit-testable)
  - **Files owned:** `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`, `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
  - **Demo:** the solution builds with 0 errors. The component is not yet placed on any page.
  - **Review lane:** Elevated (new shared component; MD3 review of the icon tokens)

## Phase 4: ViewModel

- [x] **T5: `PersonFormViewModel`: `SearchPersonsCoreAsync` + `NameOptionSource`** [SEQUENTIAL, after T3] *(merged to develop 2026-09-26, `7d228c41`)*
  - Tester first: (a) `NameOptionSource.SearchAsync("jo", ct)` returns exactly the list mapped from
    `IPersonService.SearchPersonsStartsWithAsync`, and the token passed to `SearchAsync` reaches the
    service call (`// [AC] REQ-ACF-16`); (b) below the **trimmed** threshold (" a", "a") it returns
    empty and the service is not called (`// [AC] REQ-ACF-13`). Confirm Red, then refactor
    `SearchPersonsAsync` (`PersonFormViewModel.cs:280-293`) to delegate to the new core method, which
    trims before the threshold check (today it checks raw `term.Length`, `:282`) and passes `ct` to
    `SearchPersonsStartsWithAsync` (`Services/PersonService.cs:213`), then Green. The only behaviour
    change for the existing command is that padded input such as "a " no longer searches.
    Existing `PersonFormViewModelTests` and `PersonFormViewModelBug044Tests` stay **unchanged** and green
    (REQ-ACF-20/21/28).
  - **Produces:** `SearchPersonsCoreAsync`, `NameOptionSource` · **Consumes:** T3 `MenuOptionSource`
  - **Risk:** Low (additive; the existing command is kept)
  - **Files owned:** `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs`
  - **Demo:** `dotnet test` green, with the two new tests passing.
  - **Review lane:** Standard

## Phase 5: Page migration

- [x] **T6: Migrate `PersonFormPage` Full Name onto `AutocompleteDXField`** [SEQUENTIAL, after T4 + T5]
  - One task, two files, so that the task commits only a green build (`workflow.md` Rule 3).
    Steps (incremental XAML rule):
    1. Edit `PersonFormPage.xaml`: replace `:19-43` with `<textfields:AutocompleteDXField x:Name="nameField" …/>`,
       binding `Text`=`PersonName` (TwoWay), `HasError`/`ErrorText`, `OptionSource`=`NameOptionSource`,
       `SelectCommand`=`SuggestionSelectedCommand`, `BlurWithoutPickCommand`=`ValidateNameCommand`,
       `LabelText`="Full Name", `PlaceholderText`="First and last name". `IsLocked` stays unbound. Add
       `xmlns:textfields`.
    2. Build. The only expected errors are in `PersonFormPage.xaml.cs` (the old handlers'
       `nameField.SelectedItem` uses; `ContentView` has no `SelectedItem`). Any other error: fix it
       before step 3.
    3. Edit `PersonFormPage.xaml.cs`: delete the glue at `:33-61` (`OnNameItemsRequested`,
       `OnNameSelectionChanged`, `OnNameUnfocused`); change `:30` to `nameField.FocusEditor()`. Keep the
       birthday/email blur handlers.
    4. Build with 0 errors; full `dotnet test` green.
    5. REQ-ACF-11 checklist, using a Python substring scan (not grep): no `OnNameItemsRequested`,
       `OnNameSelectionChanged`, `OnNameUnfocused` or `SelectedItem` in either file; no `RequestDelay`
       or `CharacterCountThreshold` literal in `PersonFormPage.xaml`.
  - **Produces:** the migrated Person form · **Consumes:** T4, T5
  - **Risk:** Medium (first real consumer)
  - **Files owned:** `MyVocaList/UI/Pages/People/PersonFormPage.xaml`, `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`
  - **Demo:** on the New Singer form, typing "jo" lists matching people; picking one opens that person's edit form.
  - **Review lane:** Standard

## Phase 6: Device gate

- [ ] **T7: On-device E2E gate (Helder, Android, Person form)** [SEQUENTIAL, after T6]
  - Checklist, recorded in the task-log with pass/fail per line:
    1. New Singer: the Full Name field has focus on open (REQ-ACF-18).
    2. Typing 1 char, or a space plus 1 char → no menu; 2 chars → up to 5 people after about 300 ms (REQ-ACF-13, -14, -15).
    3. Pick a person → that person's edit form replaces the page (no stacked form), with the name
       editable (REQ-ACF-03, -15, -20); no menu opens on the edit form (REQ-ACF-25).
    4. After a pick, the name is never read-only, not even before navigation completes, and no menu reopens (REQ-ACF-29, -24, -25). *(Run 1 wording, before Q3 was reversed: "informational: the picked name cannot be edited".)*
    5. X is visible whenever there is text, looks enabled, and there is no magnifier icon (REQ-ACF-07, -08, -10).
    6. Type a name, tap X → empty, focus kept, keyboard up (REQ-ACF-09).
    7. Type an invalid name and blur → the existing name validation shows; the text is kept (REQ-ACF-17, -19).
    8. Character counter and error clearing while typing behave as before (REQ-ACF-28).
    9. No row stays highlighted after a pick; the field style matches the other Outlined fields (REQ-ACF-23, -27).
    10. Supporting text renders (REQ-ACF-12c). The Person form doesn't use it, so this is checked with a
        temporary, uncommitted `SupportingText="Test"` binding: the text shows under the field, and an
        invalid name blur shows only the error.
  - Picked-state behaviour that the Person form cannot show (REQ-ACF-05 paths (a)–(g), REQ-ACF-06 on a
    locked field) is proven in T1 and re-verified on device by the song-form item.
  - **Produces:** task-log evidence (register `task-log.md` in `MyVocaList.sln` if it is new); the
    item's status moves per Helder's verdict
  - **Consumes:** T6 build on device · **Risk:** High (the only check of DX rendering on a real page)
  - **Files owned:** `…/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`, `MyVocaList.sln`
  - **Demo:** Helder completes lines 1–10 on device without a failure.
  - **Review lane:** Architectural

> **Spec updated 2026-11-05: T7 run 1 result.** PASS on lines 2, 3, 5, 6, 7. Line 4 (informational)
> is moot under REQ-ACF-29, and line 8 is accepted as designed. **FAIL on lines 1, 9 and 10** (10 is a
> consequence of 1, and the T7-prep supporting-text build was never deployed). Details:
> `task-log.md` § T7. All failures are Minor. T8 fixes them, then T7 is re-run.
> **Re-run checklist changes:**
> - Line 4 becomes "after a pick, the name is never read-only, even before navigation" (REQ-ACF-29).
> - Line 8 is confirmed against the app-wide validation standard.
> - Line 10 also checks that the "First and last name" placeholder shows on load (create mode) and
>   that the T7-prep "Test" supporting text renders.

- [ ] **T8: T7 run 1 fixes + pick-lock opt-out** `[SEQUENTIAL, after T7 run 1]` (added 2026-11-05)
  - **Behaviour:**
    - REQ-ACF-29: `IsPickLockEnabled` (default `true`) in the component and the logic class, and the
      Person form sets it to `false`.
    - REQ-ACF-18 (T7 line 1, which also fixes line 10): `FocusEditor()` defers until the inner editor is ready.
    - REQ-ACF-24/25 (T7 line 9): programmatic text closes the menu and resets `SelectedItem`, and a stale
      in-flight request is dropped after a pick. This follows the Song form Artist pattern (`design.md`
      § 3.1, note 2026-11-05).
  - **Steps (TDD, one writer, branch `task/acf-t8-t7-fixes` @ `.worktrees/acf-t8`):**
    1. Red: add the Level A tests to `AutocompleteDXFieldLogicTests`, tagged `// [AC] REQ-ACF-29`:
       - `Mode(false, true, "Ana Silva", isPickLockEnabled: false)` → `Editable`;
       - `Mode(true, true, "x", isPickLockEnabled: false)` → `ReadOnly`;
       - `LockAfterPick(Select, false)` → `false`;
       - `LockAfterPick(None, false)` → `false`.
       Run them: they fail on compile or assert.
    2. Green: `AutocompleteDXFieldLogic.Mode` / `LockAfterPick` gain `bool isPickLockEnabled = true` (`design.md` § 4).
       The existing tests stay unchanged and green.
    3. Component: the `IsPickLockEnabled` BindableProperty (§ 3.1; propertyChanged → `ApplyMode`). `CurrentMode`
       and `OnSelectionChanged` pass it, and `OnSelectionChanged` writes `IsLocked` only when `LockAfterPick` is true.
       Update the XML doc of `IsLocked`. Build.
    4. Component: the `TextChanged` handler. `Reason != UserInput` → close the menu. Only
       `Reason == ProgrammaticChange` also sets `SelectedItem = null`; `ItemSelected` never resets it, so the
       pick routed by `OnSelectionChanged` is never lost (REQ-ACF-24). First confirm the DX 25.2 member names with Context7; on a mismatch, stop and escalate. Build.
    5. Component: the pick counter, and stale-result discard in `OnItemsRequested` (REQ-ACF-26). Build.
    6. Component: the `FocusEditor()` deferral (REQ-ACF-18). Build.
    7. `PersonFormPage.xaml`: `IsPickLockEnabled="False"`. Build, then run the full `dotnet test`.
  - **Tests (Level A):** step 1. Steps 3–7 are UI glue (Level C: build + code review + the T7 re-run).
    Record that no-test decision in the task-log. The existing tests stay unchanged (the parameters
    default to `true`).
  - **Files owned:** `AutocompleteDXFieldLogic.cs`, `AutocompleteDXField.xaml(.cs)`,
    `AutocompleteDXFieldLogicTests.cs`, `PersonFormPage.xaml`, and its own `## T8` section of `task-log.md`
  - **Governance:** one consumer (PersonFormPage), not yet governed (census 2026-11-05, including the
    sft-* worktrees). If SFT binds SongFormPage before T8 merges, the four gates apply.
  - **Demo:** T7 re-run, lines 1–10 PASS on device. **Review lane:** Elevated

## Handed over to the song-form title-first item (not tasks here)

`BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/`
owns:
- `SongFormViewModel` artist flow onto the component: `ArtistOptionSource`, lock hydration, and
  deleting `SongFormPage.xaml.cs:38,41-113` and `SongFormPage.xaml:28-79`.
- Title field on the component (pick-lock on; `IsSearchEnabled=false` in edit mode, REQ-ACF-12a;
  `IsReadOnly=true` for an API-sourced song's Title in edit mode, REQ-ACF-12b; Version shown via
  `SupportingText`).
- Removing the create-new row path and the BUG-070 message (create on Save).
- Adding `AutocompleteDXField` to the governed table when the Song form binds (second consumer).
- On-device proof of REQ-ACF-05 (a)–(g) and REQ-ACF-06 on a real locked field.

## Follow-ups (register separately if Helder wants them)

- A shared constant for the list-search debounce (`CrudListViewModelBase.cs:238`).
- `ArtistFormPage` duplicate suggestions onto the component (candidate).
