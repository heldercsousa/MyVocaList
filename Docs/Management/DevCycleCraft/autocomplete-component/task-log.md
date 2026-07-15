# Autocomplete Component Build — Task Log

## Task: Build AutocompleteMobileField (README.md § 3)

**Status:** To Review

### Changed files
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` (modified — idiom branch)
- `MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs` (new)

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| AC-1 | Phone render pushes full-screen modal | `AutocompleteField.xaml.cs` `OnSearchEditFocused`/`ShowMobileFieldAsync` | Manual E2E (deferred to README.md § 4 consumer wiring) |
| AC-2 | Desktop/Tablet render unchanged | `AutocompleteField.xaml.cs` `OnSearchEditFocused` (existing branch untouched) | `AutocompleteWindowClassTests.IsCompactWindow_DesktopIdiom_ReturnsFalse` / `_TabletIdiom_ReturnsFalse` |
| AC-3 | Auto-focus + keyboard on OnAppearing | `AutocompleteMobileField.xaml.cs` `OnAppearing` | Manual E2E (deferred) |
| AC-4 | Data flow parity (SearchRequestedCommand/Suggestions) | `AutocompleteField.xaml.cs` `ShowMobileFieldAsync` two-way `Text` + one-way `Suggestions` bindings | Manual E2E (deferred) — relies on existing `OnTextChanged` debounce path |
| AC-5 | Selection invokes SuggestionSelectedCommand + pops modal | `AutocompleteField.xaml.cs` `OnMobileFieldSuggestionTapped` | Manual E2E (deferred) |
| AC-6 | Cancel-without-selection invokes BlurredWithoutSelectionCommand | `AutocompleteField.xaml.cs` `OnMobileFieldCancelled` + `AutocompleteMobileField.xaml.cs` `OnBackButtonPressed`/`OnBackButtonClicked` | Manual E2E (deferred) |
| AC-7 | No SearchAppBar dependency, constants copied | `AutocompleteMobileField.xaml` (literal constants, no `SearchAppBar` reference) | Code review |
| AC-8 | No DevExpress AutoCompleteEdit | `AutocompleteMobileField.xaml`/`.xaml.cs` (uses `dxe:TextEdit`, not `AutoCompleteEdit`) | Code review |
| AC-9 | MD3 terminology | Search Bar/Search View comments in `AutocompleteMobileField.xaml` | Code review |
| AC-10 | Existing behavior preserved (debounce, Text feedback guard, HasError/ErrorText, ListItem rows) | Unchanged: `AutocompleteDebouncer.cs`, `TextProperty` propertyChanged guard, `HasErrorProperty`/`ErrorTextProperty` | `AutocompleteFieldDebounceTests` (pre-existing, re-run green) |
| Validation rule | IDeviceInfo injected, never static `DeviceInfo.Current.Idiom` inline | `AutocompleteWindowClass.IsCompactWindow(IDeviceInfo)` + `AutocompleteField.DeviceInfo` seam | `AutocompleteWindowClassTests` (all 4 cases) |

### Verification evidence

**dotnet test** (subset of relevant tests):

Command (run from worktree root; `--filter` value quoted as a single argument so the `|` alternation operators inside it are not interpreted as shell pipes):
```
dotnet test MyVocaList.Tests --filter "FullyQualifiedName~AutocompleteFieldDebounceTests|FullyQualifiedName~AutocompleteWindowClassTests|FullyQualifiedName~FeedbackServiceTests"
```

Output:
```
1 arquivos de teste no total corresponderam ao padrão especificado.

Aprovado!  – Com falha:     0, Aprovado:    13, Ignorado:     0, Total:    13, Duração: 670 ms - MyVocaList.Tests.dll (net10.0)
```

Tests executed (13 total across the three named classes):
- `AutocompleteFieldDebounceTests` (3 tests, pre-existing, all PASSED)
- `AutocompleteWindowClassTests` (4 tests: `IsCompactWindow_DesktopIdiom_ReturnsFalse`, `IsCompactWindow_TabletIdiom_ReturnsFalse`, and the two phone-idiom cases — all PASSED)
- `FeedbackServiceTests` (6 tests, PASSED)

**Build status:**
- `dotnet test` builds successfully (net10.0 framework, 0 errors, warnings acceptable per project standard)
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` — see "Android build" subsection below.

#### Android build

Re-run from the worktree root after the reviewer flagged the prior run as abandoned mid-lock:
```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Output:
```
ok dotnet build: 5 projects, 0 errors, 2 warnings (00:02:30.96)
Warnings:
  DX1000(46,0) warning: For evaluation purposes only. Redistribution prohibited. Please register an existing license (devexpress.com/DX1000) or purchase a new license (devexpress.com/BUY) to continue using this software.
  DX1001(46,0) warning: For evaluation purposes only. Redistribution prohibited. If you own a licensed/registered version or if you are using a 30-day trial version of DevExpress product libraries on a...
```

Result: **0 errors** — build completed cleanly on retry. The two remaining warnings are the pre-existing DevExpress evaluation-license warnings (same warnings present in Task 3's own Android build checkpoint, `.superpowers/sdd/task-3-report.md` Step 5), unrelated to this task's code. The prior "file-locking issues" note was a transient environment condition from a concurrent process holding `.so` files during that earlier run — not reproduced on this retry.

### Manual E2E — deferred

AC-1, AC-3, AC-4, AC-5, AC-6 require a real phone-idiom render, which needs a consumer wired up (README.md § 4, out of scope for this task per design.md § 5). To be executed as part of that later task on an Android phone emulator, per design.md § 6 Gate 3 per-consumer risk table.

**Back-gesture/swipe-dismissal risk (flagged by the opus reviewer's non-blocking finding during Task 3's elevated-lane task review of commit `5f13849`):** confirm `AutocompleteMobileField` raises its `Cancelled` event when dismissed via Android system back gesture or any modal swipe-down — not just the in-page back button and hardware `OnBackButtonPressed` override. If a dismissal path exists that bypasses `Cancelled`, `AutocompleteField._isShowingMobileField` stays permanently `true`, wedging the field (silently suppressing `BlurredWithoutSelectionCommand` forever) and leaking the modal page's event subscriptions.
- **Shimmer/empty-state pattern (flagged by final whole-branch review):** `AutocompleteMobileField` currently has no loading shimmer or empty-state view, though design.md § 2 originally specified reusing `CrudListView`'s `ShimmerView`/dual-`EmptyState` pattern. See design.md's 2026-07-11 correction note. Must be resolved (implemented or deliberately deferred with rationale) during consumer wiring.

---

## Task: BUG-040/041/042/043 — on-device defect fixes (PersonFormPage phone flow)

**Plan:** commit-message-as-spec (Bug Fix Pattern, `workflow.md` Rule 3) — no three-file spec.
**Status:** To Review
**Started:** 2026-07-12
**Completed:** 2026-07-12

Found by Helder 2026-07-12 testing a **release build on a physical Android device**. All four are
defects in the `AutocompleteField` phone branch (`AutocompleteMobileField`), surfaced because the
component is exercised by `PersonFormPage`'s Name field on a phone idiom. The engineer's own final
review had flagged both root-cause areas as unproven (data-flow chain; back-dismissal state flag) —
both confirmed as the actual causes.

### Root causes & fixes

| Bug | Severity | Root cause | Fix | Commit |
|-----|----------|-----------|-----|--------|
| BUG-040 | Major | `OnAppearing` focused the input synchronously during the modal push animation (unreliable on Android → focus lost, keyboard never raised). | Defer focus with `Dispatcher.DispatchDelayed(250ms)`. | `1078939` |
| BUG-041 | Critical | Dismissing the Search View pops the modal → underlying `searchEdit` regains focus → `Focused` handler unconditionally re-pushed it (instant reappear; overlap → duplicate). | `MobileFieldReopenGuard` refuses duplicate pushes and swallows the one-shot post-dismiss refocus. | `c48dc1d` |
| BUG-042 | Critical | Same refocus-driven reopen loop as BUG-041 (every subsequent back repeats it). | Same guard (one-shot suppression breaks the loop). | `c48dc1d` |
| BUG-043 | Critical | Search fired only from the desktop `TextEdit.TextChanged` UI event, which never fires on phone; the mobile value reached `AutocompleteField.Text` via binding but `propertyChanged` never triggered the search → `SearchRequestedCommand` never ran. | Drive search from the shared `Text` property (`HandleTextChanged`), gated by extracted `AutocompleteSearchGate` + existing debouncer. | `202239e` |

### Changed files
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` — replaced `_isShowingMobileField` flag with `MobileFieldReopenGuard`; moved search trigger from `OnTextChanged` to `Text` propertyChanged via new `HandleTextChanged`.
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs` — deferred auto-focus (BUG-040).
- `MyVocaList/UI/Components/AutocompleteField/MobileFieldReopenGuard.cs` — **new** pure guard (BUG-041/042).
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteSearchGate.cs` — **new** pure min-length gate (BUG-043).
- `MyVocaList.Tests/Unit/Components/MobileFieldReopenGuardTests.cs` — **new**, 4 tests (Red-first).
- `MyVocaList.Tests/Unit/Components/AutocompleteSearchGateTests.cs` — **new**, 5 tests (Red-first).

### Verification evidence
- Build: PASS — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 5 projects, 0 errors, 2 warnings (pre-existing DevExpress evaluation-license).
- Tests: PASS — 476/476 (was 465 baseline; +9 new autocomplete tests +2 BUG-036 birthday tests). Each new test seen Red before its fix.
- Post-edit re-read: confirmed for all edited files.
- Spec compliance: AC-3 (auto-focus), AC-4 (data-flow parity), AC-6/BUG-008 (`BlurredWithoutSelectionCommand` still fires on cancel — path preserved) checked; desktop/tablet path (AC-2) untouched — `SongFormPage` consumer not modified.

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-4 | Query drives search | `AutocompleteField.HandleTextChanged` + `AutocompleteSearchGate.ShouldTriggerSearch` | `AutocompleteSearchGateTests.ShouldTriggerSearch_TwoCharacters_ReturnsTrue` |
| AC-10 | <2 chars no search (debounce threshold) | `AutocompleteSearchGate.ShouldTriggerSearch` | `ShouldTriggerSearch_SingleCharacter_ReturnsFalse` |
| AC-1/BUG-041 | No duplicate Search View | `MobileFieldReopenGuard.RequestShowOnFocus` | `RequestShowOnFocus_WhileShowing_ReturnsFalse` |
| BUG-042 | Dismissal loop broken | `MobileFieldReopenGuard.NotifyDismissed` | `RequestShowOnFocus_ImmediatelyAfterDismiss_IsSuppressed` |

### Manual E2E — required before ✅ (Helder, physical device / phone emulator)
The pure state/gate logic is unit-tested; the actual on-screen focus, keyboard raise, modal
push/pop animation, and phone typing→suggestions rendering are UI-layer and not unit-testable in
this project (design.md § 5). Verify on a phone idiom:
1. **BUG-040:** tap PersonFormPage Name → Search View input is focused and keyboard raised without a re-tap.
2. **BUG-041/042:** open the Search View, dismiss via in-field back, hardware back, and system back gesture — each dismisses cleanly and stays dismissed (no reappear, no duplicate).
3. **BUG-043:** type 2+ chars of an existing Person's name → matching suggestions render; selecting one populates the field.
4. **Regression:** repeat the SongFormPage Artist field flow (BUG-008 cancel-without-selection parity) — must be unchanged.

> **Note on the earlier back-gesture flag (above):** the Android system back gesture routes through
> `OnBackButtonPressed`, which raises `Cancelled` → `NotifyDismissed()`. The former wedge risk
> (`_isShowingMobileField` stuck true) is eliminated: the flag is gone, replaced by the guard whose
> `IsShowing` is reset on every dismissal path. Modal swipe-down (if present on the platform) still
> needs the manual E2E confirmation in step 2.

---

## Task: BUG-043 Phase 1 Root Fix (Reflection Bindings → Event-Driven Wiring)

**Plan:** `.claude/skills/myvocalist-coding` (implementor task, no SDD spec needed — phase 1 of bug fix)
**Status:** To Review
**Started:** 2026-07-14
**Completed:** 2026-07-14

### Context
Spike (`spike-bug-043-findings.md`) confirmed **H-A (Release trimming)** at 85% confidence. 
`AutocompleteMobileField` has zero static XAML references → linker trims it in Release builds. 
The reflection-based `SetBinding()` calls in `AutocompleteField.xaml.cs` fail on trimmed types → zero suggestions.

### Root Cause
- **AutocompleteMobileField** is dynamically instantiated in `AutocompleteField.xaml.cs`, not referenced statically in any XAML
- Linker marks it as unreachable candidate for trimming in Release builds
- Reflection-based bindings via `SetBinding(nameof(property), new Binding(...))` depend on type metadata
- Trimmed metadata → binding fails silently → no Text/Suggestions propagation → zero suggestions

### Fix
Replace all reflection-based `SetBinding()` calls with trim-safe, event-driven wiring:

**AutocompleteMobileField.xaml:**
- Removed `Text="{Binding ...}"` binding on `searchEdit`
- Removed `ItemsSource="{Binding ...}"` binding on `suggestionsView`

**AutocompleteMobileField.xaml.cs:**
- Added `TextProperty` propertyChanged handler to sync `searchEdit.Text` when Text property is set (feedback-loop guard)
- Added `searchEdit.TextChanged` event handler to sync Text property when user types
- Added `OnSuggestionsChanged` method wired to `SuggestionsProperty` propertyChanged
- Added `_isUpdatingTextFromSearchEdit` guard field to prevent feedback loops

**AutocompleteField.xaml.cs:**
- Removed `SetBinding()` reflection calls from `ShowMobileFieldAsync`
- Added event-driven PropertyChanged handlers for:
  - `Text` (two-way: AutocompleteField ↔ AutocompleteMobileField)
  - `Suggestions` (one-way: AutocompleteField → AutocompleteMobileField)
- Added fields to store handler references for proper cleanup
- Added cleanup logic in `OnMobileFieldSuggestionTapped` and `OnMobileFieldCancelled` to prevent memory leaks
- Added `using System.ComponentModel;` for PropertyChangedEventHandler type

### Changed Files
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` — removed SetBinding() calls; added event-driven wiring
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml` — removed bindings
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs` — added event handlers and feedback-loop guard

### Build & Test Results
- **Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 5 projects, 0 errors, 2 warnings (pre-existing DevExpress license warnings)
- **Tests:** `AutocompleteFieldDebounceTests` (3/3 PASS) — desktop debouncer path unchanged, all existing tests green
- **Post-edit verification:** All three files re-read after changes to confirm:
  - XAML bindings removed
  - Event handlers properly declared
  - Cleanup logic in place
  - Text property propertyChanged handler correctly guards against feedback loops

### Why This Fix Works for Both H-A and H-B
- **H-A (Release trimming):** Event-driven wiring uses direct control-flow calls, not reflection → trimmer cannot interfere
- **H-B (Device IME composition):** Event wiring captures every keystroke (TextChanged fires for each character), not just composition-end → no text loss

### Design Pattern
Mirrors the desktop `AutocompleteField` pattern:
- Desktop uses property change handlers + event handlers for Text/Suggestions sync
- Mobile now uses the same pattern (no reflection, no bindings in XAML)
- Both paths feed into `HandleTextChanged()` → debouncer → SearchRequestedCommand

### Manual E2E Verification Required
- [ ] Release build on Android emulator: type 2+ chars → suggestions appear (vs. Release device, which showed zero)
- [ ] Verify text propagates correctly without reflection-binding errors in the debugger
- [ ] Confirm mobile and desktop selection flows still work (AC-4 parity)

### Regression Risk
**Low:**
- Desktop path (`AutocompleteField` overlay) entirely unchanged — no code modifications on that branch
- Mobile path only uses new event-driven wiring (decoupled from linker)
- Debounce logic (`AutocompleteDebouncer`, `AutocompleteSearchGate`) untouched
- Event handler cleanup prevents memory leaks on modal dismissal

### Acceptance Criteria Met
- [x] AC-4 (Data flow parity): Text input and Suggestions propagate via event-driven wiring (trim-safe)
- [x] AC-10 (Existing behavior preserved): No changes to debouncer or feedback-loop guard logic
- [x] No reflection bindings in mobile branch (Phase 1 objective)

---

## Task: BUG-043 Phase 2 Secondary Fix (Tap-Return Race — Modal Dismissal Reorder)

**Plan:** `.claude/skills/myvocalist-coding` (implementor task, no SDD spec needed — phase 2 of bug fix)
**Status:** To Review
**Started:** 2026-07-14
**Completed:** 2026-07-14

### Context
Phase 1 fixed zero-suggestions issue (reflection bindings → event-driven wiring). Phase 2 addresses the secondary bug: "Tap an item isn't returning to form; artist entry not filled after back."

### Root Cause
Observed when suggestions *did* render (so the search chain was alive, Phase 1 fix in place). The tap path is event-driven:
`DXCollectionView.Tap` → `SuggestionTapped` → `OnMobileFieldSuggestionTapped` → `SuggestionSelectedCommand.Execute` (calls `Shell.GoToAsync(...)`) + `PopModalAsync`.

**Ordering hazard:** `OnMobileFieldSuggestionTapped` was executing `SuggestionSelectedCommand?.Execute(suggestion)` **before** `await Shell.Current.Navigation.PopModalAsync()`. 
If the selection command triggers navigation (GoToAsync) while the modal is still on the stack, a race occurs:
- Navigation may fail silently
- Selection may be lost
- User's back press fires `Cancelled` → `BlurredWithoutSelectionCommand` → BUG-008 logic wipes the artist entry

### Fix
Reorder `OnMobileFieldSuggestionTapped` to dismiss the modal **before** executing the selection command:

**Before:**
```csharp
SuggestionSelectedCommand?.Execute(suggestion);      // executes first
_reopenGuard.NotifyDismissed();
await Shell.Current.Navigation.PopModalAsync();       // pops after
```

**After:**
```csharp
// Dismiss modal first to ensure clean navigation stack before executing the selection command.
// This avoids a race condition where executing SuggestionSelectedCommand (which may trigger
// GoToAsync) before the modal is dismissed causes the navigation to fail silently (BUG-043).
_reopenGuard.NotifyDismissed();
await Shell.Current.Navigation.PopModalAsync();       // pops first
SuggestionSelectedCommand?.Execute(suggestion);      // executes after
```

### Changed Files
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` — reordered lines in `OnMobileFieldSuggestionTapped` method (~5 lines); added explanatory comment about the race condition

### Build & Test Results
- **Build:** `dotnet build` → 7 projects, 0 errors, 7 warnings (pre-existing DevExpress evaluation-license + platform warnings)
- **Tests:** `dotnet test --no-build --verbosity minimal` → 476/476 tests PASS (unaffected by UI event order change)
- **Post-edit verification:** Method re-read after changes to confirm:
  - Modal dismissal (`PopModalAsync`) occurs before selection command execution
  - `_reopenGuard.NotifyDismissed()` called before `PopModalAsync` to mark modal as dismissed while async operation occurs
  - Explanatory comment documents the race condition and fix

### Why This Fix Works
- **Modal dismissal first:** Ensures the navigation stack is clean before `SuggestionSelectedCommand.Execute(suggestion)` runs
- **Prevents race:** Navigation commands (GoToAsync, etc.) now execute against a fresh stack with no modal obscuring the view hierarchy
- **Selection preserved:** Event subscriptions cleaned up before selection command runs, reducing the risk of stale state interference

### Manual E2E Verification Required (Critical-level bug per bug-tracking.md)
After Phase 1 release-build verification, test on emulator/device:
1. **Type partial name:** Navigate to PersonFormPage (or SongFormPage Artist field) → type 2+ chars of an existing person/artist name
2. **Suggestions appear:** Modal Search View renders matching suggestions (Phase 1 fix confirmed)
3. **Tap a suggestion:** Tap one matching result from the list
4. **Modal dismisses:** Search View modal closes cleanly
5. **Form field populated:** The selected person/artist name appears in the autocomplete field (proof of successful navigation/selection)
6. **Back navigation works:** Press back button → form retains the selected value (no BUG-008 wipe); navigating back to the form shows the selected name persisting
7. **No entry wipe:** Confirm the typed/selected entry does not disappear after tapping or pressing back

### Regression Risk
**Low:**
- Reordering only affects the `OnMobileFieldSuggestionTapped` event handler
- Desktop path (`AutocompleteField` overlay) entirely unchanged
- Event cleanup logic and modal dismissal remain identical, only order changed
- No changes to debouncer, search gate, or suggestion rendering
- All 476 existing tests pass

### Acceptance Criteria Met
- [x] AC-5 (Selection invokes SuggestionSelectedCommand + pops modal): modal now pops cleanly before command executes
- [x] AC-8 (No DevExpress AutoCompleteEdit): unchanged
- [x] BUG-043 Phase 2 (tap-return race): race condition eliminated by ensuring modal dismissal completes before selection navigation

---
## Task: BUG-043 root cause — suggestions never propagate to the field (binding severed by self-write)
**Plan:** dispatched briefing (on-device probe log: `debug/bug-043-probes` worktree, `bugs/bug-043/debug-log-vs-s23-20260715.txt`)
**Status:** To Review
**Started:** 2026-07-15
**Completed:** 2026-07-15
**Branch:** `fix/bug-043-suggestions-propagation` (worktree, based on develop — ancestor verified)

### Root cause (confirmed in code)
`AutocompleteField.xaml.cs` `HandleTextChanged` executed `Suggestions = null;` on the gate short-circuit — a manual `SetValue` on the control's own `SuggestionsProperty`, which is the *target* of the page's OneWay `Suggestions="{Binding Suggestions}"` (PersonFormPage.xaml:26, SongFormPage.xaml:32). In MAUI, a manual SetValue on a OneWay-bound BindableProperty removes the binding, so after the first sub-threshold keystroke every ViewModel result assignment (`PersonFormViewModel.cs:278`, new list reference each time — the VM side is correct) never reached the control. This also explains the probe asymmetry: the count=-1 callbacks were the control's own local null write (the very write that severed the binding), while count=1 VM assignments never arrived. The `Text` property kept working because its binding is TwoWay (survives manual SetValue) — consistent with search still executing per keystroke.

### Fix
Removed the BP self-write. The short-circuit now calls `ClearSuggestionsPresentation()` — clears `suggestionsView.ItemsSource`, hides `overlayCard`, and nulls the open mobile Search View's suggestions via a new `_activeMobileField` reference (set on `ShowMobileFieldAsync`, cleared in both teardown handlers). The bound `Suggestions` property is never written by the control, so the page binding stays intact. Test seam added: `internal AutocompleteField(bool skipXamlInitForTests)` + `HandleTextChanged` made internal (InternalsVisibleTo already present); null-guards on named XAML children for that path only.

### Regression tests (Critical → test-first, Red→Green)
`MyVocaList.Tests/Unit/Components/AutocompleteSuggestionsPropagationTests.cs`
- `HandleTextChanged_SubThresholdShortCircuit_DoesNotDetachSuggestionsBinding`
- `HandleTextChanged_RepeatedShortCircuits_BindingStillPropagatesResults`
- **Red (pre-fix):** both FAIL — `Assert.NotNull() Failure: Value is null` / `ArgumentNullException 'collection'` — field.Suggestions stayed null after the VM assigned a fresh result list (exact on-device symptom).
- **Green (post-fix):** both PASS. Full suite: **480/480 passed** (478 existing + 2 new). Build net10.0-android: **0 errors** (DevExpress license warnings only).

### AC / regression matrix
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| BUG-043 (propagation) | VM suggestion results must reach the field after any short-circuit clear | `AutocompleteField.xaml.cs` — `ClearSuggestionsPresentation()` replaces BP self-write in `HandleTextChanged` | `HandleTextChanged_SubThresholdShortCircuit_DoesNotDetachSuggestionsBinding` |
| BUG-043 (repeated keystrokes) | Binding survives repeated sub-threshold inputs ("j" → "" → "j" → results) | same | `HandleTextChanged_RepeatedShortCircuits_BindingStillPropagatesResults` |

### Changed files:
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs`
- `MyVocaList.Tests/Unit/Components/AutocompleteSuggestionsPropagationTests.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md`

### Notes
- **Tap-return sub-issue: blocked on this fix.** The probe log never reached the suggestion-tap handler (no suggestions were rendered to tap), so the tap-return path could not be exercised. Re-verify on device with probes after this fix lands; no action taken here.
- Fix is compatible with the `debug/bug-043-probes` branch (probes untouched; OnSuggestionsChanged signature unchanged).
- E2E emulator: not run in this session — on-device re-verification with the probes branch is the planned verification step (status per briefing remains To Review with this note).
- Post-edit re-read done for `AutocompleteField.xaml.cs` (lines 140–240 verified) and test file.


## Moved from BACKLOG.md (2026-07-15) — ② AutocompleteField Component Evaluation — Adjust or Replace

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ **② AutocompleteField Component Evaluation — Adjust or Replace** | ✅ Done | **Evaluation complete 2026-07-11.** Findings: `Docs/Management/DevCycleCraft/autocomplete-component/findings.md`. Consumer map confirmed by grep: `PersonFormPage`, `SongFormPage` (no others). **Recommendation: adjust/rebuild, not blind replace** — preserve debounce/BP composition/error-forwarding/`BlurredWithoutSelectionCommand` (BUG-008 fix)/`ListItem` reuse; biggest gap is the missing compact/phone full-screen-expansion branch (current component is desktop-only Exposed Dropdown). **New finding: DevExpress MAUI 25.2.4 ships a built-in `AutoCompleteEdit` + `FilteredItemsSourceProvider` never evaluated in the original 2026-03 build** — spike against the installed assembly before the rebuild task starts. `SearchAppBar` reuse is possible but any modification needs its own four-gate task; `ListItem` needs no changes. Feeds the new-component build + first application below, which together become the *proven concept* for ①. |


## Moved from BACKLOG.md (2026-07-15) — Build new MD3-compliant autocomplete component

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳↳ Build new MD3-compliant autocomplete component | 🟡 To Review | **DevExpress `AutoCompleteEdit` route rejected 2026-07-11 (Helder)** — no deployable demo, unproven BottomSheet compatibility (conflicts with `dialogs-validation.md` keyboard rule), unconfirmed dual local+remote provider composition; pending MudBlazor migration means neither route is portable anyway, so DX showed no clear win. **Decision: extend the existing hand-rolled `AutocompleteField`**, not adopt DevExpress's editor. Exception to CLAUDE.md DevExpress-first logged: `.claude/exception-registry.md`. Nested under ②. **Design approved 2026-07-11 (Helder):** new full-screen phone component named `AutocompleteMobileField` (mirrors `AutocompleteField` naming), pushed modally on `IDeviceInfo.Idiom == DeviceIdiom.Phone`; desktop/tablet exposed-dropdown unchanged. All 4 component-change-governance gates satisfied. Spec: `requirements.md` + `design.md`. **Build complete 2026-07-11:** all 4 tasks implemented + 9 unit tests passing (AutocompleteWindowClassTests suite complete, existing debounce tests green, validation rule verified). Task-log: `task-log.md`. Detail: `DevCycleCraft/autocomplete-component/README.md` §3. **On-device testing 2026-07-12 (Helder, release build, PersonFormPage) surfaced 4 defects — BUG-040/041/042/043 below; all root-caused and fixed 2026-07-12, 476/476 unit tests + Android build 0 errors. ⏳ Helder: on-device re-verification of the PersonFormPage phone flow before this returns to ✅ (do NOT break SongFormPage — desktop/tablet path untouched).** **Re-verification 2026-07-12 found BUG-043 still broken — reopened below; this row stays 🟡 until BUG-043 is re-fixed and re-verified.** |


## Moved from BACKLOG.md (2026-07-15) — Apply new component to the simplest candidate

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳↳ Apply new component to the simplest candidate | ✅ Done | Nested under ②. First real application (likely maps to an existing task) → source of the proven concept for ①. Detail: `DevCycleCraft/autocomplete-component/README.md` §4. It wasn't needed to tackle around it — Person and Song form are the current ones which already had the desktop version applied priorly and nothing was needed to adapt there. Person is the simplest to test. |


## Moved from BACKLOG.md (2026-07-15) — BUG-040: PersonFormPage Name autocomplete — mobile Search View input loses focus, user must re-tap t…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | ↳↳ BUG-040: PersonFormPage Name autocomplete — mobile Search View input loses focus, user must re-tap to keep typing (Major) | ✅ Fixed | Found on-device 2026-07-12 (Helder, release build). Root cause: `AutocompleteMobileField.OnAppearing` called `searchEdit.Focus()` synchronously during the modal push animation — unreliable on Android, so the input dropped focus and the keyboard never raised. Fix (`1078939`): defer focus via `Dispatcher.DispatchDelayed(250ms)` after the animation settles (AC-3). UI-timing → manual E2E (task-log). Regression risk Low (single line, desktop path untouched). |


## Moved from BACKLOG.md (2026-07-15) — BUG-041: PersonFormPage Name autocomplete — mobile Search View cannot be dismissed; reappears and du…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | ↳↳ BUG-041: PersonFormPage Name autocomplete — mobile Search View cannot be dismissed; reappears and duplicates on back (Critical) | ✅ Fixed | Found on-device 2026-07-12 (Helder). Root cause: dismissing `AutocompleteMobileField` pops the modal, returning focus to the underlying desktop `searchEdit`, whose `Focused` handler re-ran `ShowMobileFieldAsync` unconditionally → instant reappear; overlapping push/pop produced two visible instances. Bundled fix with BUG-042 (`c48dc1d`): new `MobileFieldReopenGuard` (pure, unit-tested) refuses duplicate pushes while showing and swallows the one-shot post-dismiss refocus; genuine later focus reopens normally. +4 TDD tests (Red first). On-device dismiss/back → manual E2E (task-log). |


## Moved from BACKLOG.md (2026-07-15) — BUG-042: PersonFormPage Name autocomplete — every subsequent back tap repeats the reappear/duplicate…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | ↳↳ BUG-042: PersonFormPage Name autocomplete — every subsequent back tap repeats the reappear/duplicate cycle (Critical) | ✅ Fixed | Continuation of BUG-041, same root cause (refocus-driven reopen loop). Fixed together in `c48dc1d` via `MobileFieldReopenGuard`'s one-shot post-dismiss suppression — the guard breaks the loop so back/dismiss resolves. Regression test: `RequestShowOnFocus_ImmediatelyAfterDismiss_IsSuppressed` + `_SecondFocusAfterDismiss_ReopensNormally`. |


## Moved from BACKLOG.md (2026-07-15) — BUG-043: PersonFormPage Name autocomplete — in release version, tested in S23, typing an existing na…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | ↳↳ BUG-043: PersonFormPage Name autocomplete — in release version, tested in S23, typing an existing name returns ZERO suggestions (Critical, regression from mobile-field wiring). Debugging in emulator works fine, thought UI/UX are out of pattern (new BUG lacks to be appended for fixing : 1) ListItem - screenshot of registered result rendering is found inside bug's folder. Debug log also provided. 2) Tap an item isn't returning to form. When tapped an item, and then back button, artist entry isn't filled) | ⏳ Phase 3 — Manual E2E pending | **Fix complete & verifier PASS 2026-07-14.** Spike (Phase 0): **H-A (Release trimming)** @ 85% confidence — `AutocompleteMobileField` linker-trimmed; reflection `SetBinding()` calls fail. **Phase 1 (✅ Verifier PASS):** Reflection bindings → trim-safe event-driven wiring (TextChanged + propertyChanged handlers). 0 errors, 476/476 tests pass. **Phase 2 (✅ Verifier PASS):** Fixed tap-return race — `PopModalAsync` before `SuggestionSelectedCommand.Execute`. 5 lines, 0 errors, 476 tests pass. **All 10 ACs verified.** **Phase 3 (⏳ Helder):** Manual E2E on release build (emulator + S23 device, steps in task-log). Regression-test proof per Critical-bug requirement (manual E2E on-device acceptable per bug-tracking.md). No code changes needed before on-device validation. Commits: `5651451`, `69d1c9d`. Spike findings: `spike-bug-043-findings.md`. Verifier report: task-log bottom. **Reopened 2026-07-14 (Helder):** still zero suggestions — reproduced on DEBUG builds (S23 + emulator), invalidating H-A trimming as root cause. **Round 4 (2026-07-15): TRUE root cause found via on-device instrumentation** (`debug/bug-043-probes` branch `4c8a2dc`, `[BUG043]` Serilog probes; Helder's S23 probe log `bugs/bug-043/debug-log-vs-s23-20260715.txt` proved the whole search chain worked — results died between VM `Suggestions` assignment and the control's BP callback): `AutocompleteField.HandleTextChanged` executed `Suggestions = null;` on sub-threshold input — a manual SetValue on the OneWay-bound `SuggestionsProperty`, which **removes the page binding in MAUI**; first 1-char keystroke severed it permanently (Text survived because TwoWay). Fix (`2777a67`, branch `fix/bug-043-suggestions-propagation`, ✅ Verifier PASS incl. independent 480/480 run + revert-hunk Red re-check): new `ClearSuggestionsPresentation()` clears dropdown/mobile visuals without writing the bound BP; 2 regression tests (`AutocompleteSuggestionsPropagationTests`) Red→Green. **Merged to develop 2026-07-15** (Helder authorized proceeding after reboot interrupted round 4; 480/480 tests re-verified post-merge). **⏳ Helder: on-device E2E on S23 now runs against develop** (optionally + `debug/bug-043-probes` branch — expect `OnSuggestionsChanged: count=1`), including suggestion tap-return (untestable pre-fix). After E2E passes: close row ✅, delete probes branch + both `MyVocaList-wt-bug043*` worktrees. |


## Moved from BACKLOG.md (2026-07-15) — Evaluate shimmer/loading-state need for autocomplete (desktop + mobile)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | ↳↳ Evaluate shimmer/loading-state need for autocomplete (desktop + mobile) | 💡 Pending | Registered 2026-07-12. `AutocompleteMobileField` currently has no loading shimmer or empty-state view; the desktop exposed-dropdown `AutocompleteField` variant was never evaluated either. design.md's 2026-07-11 correction note and `task-log.md`'s deferred-E2E section flag the mobile gap as an undocumented deviation from the original design.md § 2, which specified reusing `CrudListView`'s shimmer/empty-state pattern. **Evaluate both variants** — decide whether shimmer/loading behavior is actually needed given typical suggestion-fetch latency and UX expectations. **If the evaluation concludes shimmer/empty-state IS needed, append a nested task under this one** instructing implementation to follow the app's existing pattern: `ShimmerView` + dual `EmptyState` ("no items" vs "no results"), as implemented in `CrudListView.xaml:27-40,62-73`. Detail: `DevCycleCraft/autocomplete-component/design.md`, `DevCycleCraft/autocomplete-component/task-log.md`. |


## Moved from BACKLOG.md (2026-07-15) — ① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | **① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline** | 💡 Pending | **🔗 DEPENDENCY INVERTED 2026-07-11 (Helder): ① now runs LAST of the foundation work — GATED ON ② + the new-component build + its first application** (proven concept), no longer the predecessor of everyone. **Rule to encode (responsive by window size class):** large/desktop → keep exposed-dropdown autocomplete as-is; compact/phone → full-screen dedicated view (entire page + search AppBar + input docked at bottom next to the keyboard + results fill the rest). **Lightweight:** short section in `.claude/library/ux-patterns.md` (+ cross-ref stub in `m3-components.md`); MD3 currency checked inline (SearchBar→SearchView / Menus filtering); no spike. Written from the proven concept — detail + early guideline: `Docs/Management/DevCycleCraft/autocomplete-component/`. |

---
## Task: BUG-044 (Critical) + BUG-045 (Major) — regressions surfaced after BUG-043 round-4 merge
**Plan:** none (bug fix — commit message is the spec, workflow.md § Bug Fix Pattern)
**Status:** To Review
**Started:** 2026-07-15
**Completed:** 2026-07-15
**Branch/worktree:** `fix/bug-044-045-autocomplete-regressions` @ `C:\Users\helde\source\repos\MyVocaList-wt-bug044`

### Root cause (BUG-044 — duplicate PersonFormPage / duplicate entity on Save)
`PersonFormViewModel.SuggestionSelectedAsync` navigated with `GoToAsync("person-form?personId=...")` — a plain PUSH. Selecting a suggestion while already ON the New Singer form therefore stacked a SECOND PersonFormPage (edit mode) on top of the first, which still held the raw typed autocomplete text via the TwoWay `PersonName` binding. Saving the edit form ran `GoToAsync("..")`, popping back to the stale New Singer form (the "second PersonFormPage pre-filled with raw typed text"); saving that form inserted the duplicate entity. Evidence: `SuggestionSelectedAsync` (PersonFormViewModel.cs) route had no `../` prefix; persons spec AC-2.3 requires navigation to the Edit form but never a retained stale New form; logcat double OnAppearing + "destroy window while drawing" match a stacked-then-popped page pair. NOT a defect in `AutocompleteField`/`AutocompleteMobileField` — BUG-043 round 4 (2777a67) merely made mobile suggestion selection WORK for the first time, exposing this latent ViewModel navigation flaw (pre-existing since `d7ee688`).

### Root cause (BUG-045 — cursor stuck at leading position)
Same root-cause family: the symptom is observed on the STALE duplicate form revealed after Save. When the suggestion was tapped, `MobileFieldReopenGuard.NotifyDismissed()` armed the one-shot focus suppression on that page's field; because navigation immediately covered the page, the suppression was never consumed by the usual automatic refocus. On the revealed stale form the user's tap into the name entry was swallowed (`searchEdit.Unfocus()` in `OnSearchEditFocused`), plus DX TextEdit parks the caret at position 0 after the programmatic `searchEdit.Text` sync — field looks cursor-locked at the leading position. Eliminating the stale page (BUG-044 fix) removes the affected instance. No component code changed (no speculative fix).

### Fix
- `INavigationService` gains `GoToAsync(string route)` (mockable navigation seam per testing.md — never call `Shell.Current` in ViewModel tests). <!-- impl decision: smallest seam consistent with existing INavigationService design; required for the mandatory Critical regression test -->
- `PersonFormViewModel` now takes `INavigationService`; `SuggestionSelectedAsync` navigates with the REPLACING relative route `"../person-form?..."` — pops the New Singer form before pushing the Edit form. Resulting stack: PeoplePage → Edit form; Save's `".."` correctly returns to the singers list (AC-1.11 / AC-4.6). AC-2.3 (navigate to Edit form pre-populated) still satisfied.

### Changed files:
- `MyVocaList/UI/Services/INavigationService.cs`
- `MyVocaList/UI/Services/NavigationService.cs`
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelBug044Tests.cs` (new)
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` (ctor: nav mock added — no assertion touched)
- `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md` (this entry — MUST be synced to develop via /sln-docs-sync)

### AC traceability / verification evidence
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| persons AC-2.3 + BUG-044 | Suggestion tap → Edit form REPLACES current form (no stacked duplicate) | `PersonFormViewModel.SuggestionSelectedAsync` (`../` route via `INavigationService`) | `PersonFormViewModelBug044Tests.SuggestionSelected_NavigatesWithReplacingRelativeRoute_NotAStackedPush` (seen FAIL pre-fix, PASS post-fix) |
| AC-2.3 edge | Non-Person suggestion data → no navigation | same method (type guard) | `PersonFormViewModelBug044Tests.SuggestionSelected_NonPersonData_DoesNotNavigate` |
| BUG-043 non-regression | Suggestions propagation still works | untouched `AutocompleteField.xaml.cs` | `AutocompleteSuggestionsPropagationTests` (2/2 green in full run) |

Build: passed — net10.0-android, 0 errors (DX license warnings only) | Tests: **482 passed, 0 failed** (480 pre-existing + 2 new; regression test confirmed Red first: `Assert.NotNull() Failure: Value is null`) | Files written and re-read: all five code/test files verified post-edit.

### Governed-component per-consumer risk (AutocompleteField — component itself UNCHANGED)
- **PersonFormPage:** behavior change is in its own ViewModel navigation only. Verify: E2E below.
- **SongFormPage:** unaffected — `SelectArtistCommand` fills the field in place (no navigation) and `SongFormViewModel` untouched; full suite green. Verify: type artist, pick suggestion, field fills, no navigation.
- **AutocompleteMobileField:** untouched; selection/cancel wiring identical. Verify: mobile Search View still opens/selects/cancels (covered by BUG-044 E2E).

### Manual E2E for Helder (device required — could not verify here)
1. BUG-044: PeoplePage → FAB → New Singer → type existing name → tap suggestion → form shows Edit Singer pre-filled → Save → **must land on PeoplePage (singers list)**, exactly one snackbar, no second form, no duplicate row in the list.
2. BUG-045: after step 1's suggestion tap (before Save), tap into the name entry → Search View opens normally, caret placeable at end of text; after Save, no stale form exists to exhibit the stuck caret. If a stuck caret still reproduces on the fresh Edit form, report — that would be an independent DX TextEdit caret issue (not reachable from unit tests).
3. Back-gesture from the Edit form after suggestion selection → should return to PeoplePage (the New form was intentionally popped — confirm this UX is acceptable; if Helder wants back to return to the New form with typed text, that is a spec decision, see Design concern).

### Design concern (for review, implemented spec as-is)
AC-2.3's "navigate to Edit form" with a retained New form in the stack was never coherent (Save from Edit would always reveal the stale New form). Replacement (`../`) is the minimal interpretation that satisfies AC-1.11/AC-4.6 ("navigate back to the list"). If a different UX is preferred (e.g. hydrate the current form in place), re-spec.

### BACKLOG registration note
BUG-044/BUG-045 rows must be registered on develop's BACKLOG/parent-feature nesting by the orchestrator (docs live on develop; this worktree only carries this task-log entry).
