# Task log: AutocompleteDXField (pick-lock via X)

> Plan: `plan.md` (this folder). One section per task, pre-created so parallel worktrees edit different
> hunks (plan § 0.4 item 0.3). Implementors write only their own section. The orchestrator owns
> `tasks.md`, `findings.md`, `MyVocaList.sln` and the LEDGER.
>
> **Approval:** the spec was approved by Helder 2026-09-25; the whole pipeline (merges and every Helder
> gate except the on-device stops T1b and T7) was **pre-authorised by Helder 2026-09-25**.

## Baseline

- 2026-09-25, `develop` @ `7561a0d6`: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` →
  **Passed 635, Failed 0, Skipped 0** (Total 635). Used for the "+N tests" checks after each merge.
- Hook health: `.claude/settings.json` parses as valid JSON. `.worktrees/` is git-ignored.

### Checkpoint

- Step 0 in progress (orchestrator).

## T1 — Spike

**T1a: `Check build`** (implementor handback, 2026-09-25). Recorded on develop by the orchestrator per plan T1c
item 1. Full facts: `findings.md` § Environment.
- Branch `spike/acf-t1-lock-mechanism` @ `73184803` (local, not pushed), worktree `.worktrees/acf-t1-spike` (kept for T1b).
- `RequestDelay` type: **int**. Context7 confirmed every listed member (`IsReadOnly`, `IsDropDownOpen` and `SelectionChanged`
  were confirmed via assembly metadata; the last two are declared on `ItemsEditBase`). devexpress-maui MCP: returned empty.
- Build 0 errors. Emulator smoke not run (no device attached).
- Changed files (spike branch only): `MyVocaList/UI/Pages/Spike/AcfSpikePage.xaml(.cs)` (new), `MyVocaList/AppShell.xaml`,
  `MyVocaList/Navigation/NavigationConfig.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`.
- Next: **T1b, Helder on device (HARD STOP)**, run sheet in `findings.md`.

### Checkpoint

- 2026-09-25: T1a done; awaiting T1b (Helder). The spike worktree must NOT be deleted before T1c.

## T2

**Status:** To Review

**Summary:** Added `SearchConstants.AutocompleteDebounceMilliseconds = 300` constant (REQ-ACF-14) via test-first TDD (Red → Green → Build).

### Changed files

- `Domain/Constants/SearchConstants.cs` — appended constant with XML doc after `MaxSearchResults`
- `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` — new test file

### Verification evidence

**Red (Step 2):** Test failed with CS0103 (SearchConstants does not exist)
- `dotnet test ... --filter "FullyQualifiedName~SearchConstantsTests"` → build error before constant was added

**Green (Step 4):** Test passed after constant was added
- T2 test alone: `Passed: 1, Failed: 0`
- Full suite: `Passed: 636, Failed: 0` (baseline 635 + 1 new test)

**Build (Step 5):** 0 errors, 2 warnings (DevExpress evaluation license)
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android --no-restore` → ok

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|------------------------|------------|
| REQ-ACF-14 | One shared 300 ms debounce delay for every autocomplete field | `SearchConstants.AutocompleteDebounceMilliseconds` | `SearchConstantsTests.AutocompleteDebounceMilliseconds_Is300` |

### Post-merge verification (orchestrator, develop)

- Merged `--no-ff` as `78b2b735` (2026-09-25). Pre-authorised by Helder 2026-09-25.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors (DX1000/DX1001 licence warnings only).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → **Passed 636, Failed 0** (baseline 635 + 1). No BUG-076 re-run needed.
- Verifier (Sonnet) on the merge diff: **PASS**, 0 findings. (A first Verifier dispatch was cut off by a usage limit and re-run.)
- Push: the implementor's `git push` hung on a GitHub credential GUI prompt; merged locally instead. Worktree removed; branch deleted.
- Deviation: the Red step raised CS0103 rather than the plan's CS0117 (the test file had no `using` for the namespace until
  the constant existed); still a compile-time Red for the right reason.

## T3

**Status:** To Review

### Changed files:
- `MyVocaList/UI/Models/IMenuOptionSource.cs` (new, Tester) — `IMenuOptionSource` + `MenuOptionSource` adapter, complete (contract only).
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (Tester: skeleton; **Builder: implemented all 7 rules**, verbatim from `plan.md` § Task T3 Wave 1b Step 5 / `design.md` § 4).
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (new, Tester) — 24 test methods / 39 test cases (Theories expand to multiple cases), one `// [AC]` per test. **Not modified by the Builder.**

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-ACF-12b | Read-only field is always `ReadOnly` | `AutocompleteDXFieldLogic.Mode` | `Mode_ReadOnly_ReturnsReadOnly_WhateverLockAndText` |
| REQ-ACF-02 | Locked field with text is `Locked` | `AutocompleteDXFieldLogic.Mode` | `Mode_LockedWithText_ReturnsLocked` |
| REQ-ACF-05a | Locked field with empty/whitespace text falls back to `Editable` | `AutocompleteDXFieldLogic.Mode` | `Mode_LockedWithEmptyOrWhitespaceText_ReturnsEditable` |
| REQ-ACF-03 | Unlocked field with text is `Editable` | `AutocompleteDXFieldLogic.Mode` | `Mode_NotLockedWithText_ReturnsEditable` |
| REQ-ACF-12c | No error → supporting text shown as-is | `AutocompleteDXFieldLogic.BottomSupportingText` | `BottomSupportingText_NoError_ReturnsSupportingText` |
| REQ-ACF-12c | Error → supporting text suppressed | `AutocompleteDXFieldLogic.BottomSupportingText` | `BottomSupportingText_HasError_ReturnsEmpty` |
| REQ-ACF-12c | No error, null supporting text → empty, not null | `AutocompleteDXFieldLogic.BottomSupportingText` | `BottomSupportingText_NoErrorNullText_ReturnsEmpty` |
| REQ-ACF-07 | Clear icon shown when Editable with text | `AutocompleteDXFieldLogic.ShowClearIcon` | `ShowClearIcon_EditableWithText_ReturnsTrue` |
| REQ-ACF-07 | Clear icon shown when Locked with text | `AutocompleteDXFieldLogic.ShowClearIcon` | `ShowClearIcon_LockedWithText_ReturnsTrue` |
| REQ-ACF-07 | Clear icon hidden with no text, Editable | `AutocompleteDXFieldLogic.ShowClearIcon` | `ShowClearIcon_EmptyText_ReturnsFalse` |
| REQ-ACF-12b | Clear icon never shown in ReadOnly | `AutocompleteDXFieldLogic.ShowClearIcon` | `ShowClearIcon_ReadOnlyWithText_ReturnsFalse` |
| REQ-ACF-13 | Trimmed query ≥ minimum length requests options | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_EditableSearchOnTrimmedTwoChars_ReturnsTrue` |
| REQ-ACF-13 | Trimmed query below minimum length never requests options | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_BelowTrimmedThreshold_ReturnsFalse` |
| REQ-ACF-04 | Locked field never requests options | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_Locked_ReturnsFalse` |
| REQ-ACF-12a | Search disabled → no options requested | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_SearchDisabled_ReturnsFalse` |
| REQ-ACF-12b | Read-only field never requests options | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_ReadOnly_ReturnsFalse` |
| REQ-ACF-11 | No option source → no options requested | `AutocompleteDXFieldLogic.ShouldRequestOptions` | `ShouldRequestOptions_NullSource_ReturnsFalse` |
| REQ-ACF-11 | Selecting an `AutocompleteSuggestion` routes to `Select` | `AutocompleteDXFieldLogic.RoutePick` | `RoutePick_AutocompleteSuggestion_ReturnsSelect` |
| REQ-ACF-11 (b) | Null or non-suggestion value routes to `None` | `AutocompleteDXFieldLogic.RoutePick` | `RoutePick_NullOrNonSuggestion_ReturnsNone` |
| REQ-ACF-01 | `Select` route locks the field | `AutocompleteDXFieldLogic.LockAfterPick` | `LockAfterPick_Select_ReturnsTrue` |
| REQ-ACF-01 | `None` route does not lock the field | `AutocompleteDXFieldLogic.LockAfterPick` | `LockAfterPick_None_ReturnsFalse` |
| REQ-ACF-17 | No pick, editable field → blur forwarded | `AutocompleteDXFieldLogic.ShouldForwardBlur` | `ShouldForwardBlur_NoPickEditable_ReturnsTrue` |
| REQ-ACF-11 | Selected item present → blur not forwarded | `AutocompleteDXFieldLogic.ShouldForwardBlur` | `ShouldForwardBlur_WithSelectedItem_ReturnsFalse` |
| REQ-ACF-11 | Locked field never forwards blur | `AutocompleteDXFieldLogic.ShouldForwardBlur` | `ShouldForwardBlur_Locked_ReturnsFalse` |
| REQ-ACF-12b | Read-only field never forwards blur | `AutocompleteDXFieldLogic.ShouldForwardBlur` | `ShouldForwardBlur_ReadOnly_ReturnsFalse` |

### Test names (24 methods, 39 cases with Theory expansion)
`Mode_ReadOnly_ReturnsReadOnly_WhateverLockAndText` (4 cases) · `Mode_LockedWithText_ReturnsLocked` · `Mode_LockedWithEmptyOrWhitespaceText_ReturnsEditable` (3 cases) · `Mode_NotLockedWithText_ReturnsEditable` · `BottomSupportingText_NoError_ReturnsSupportingText` · `BottomSupportingText_HasError_ReturnsEmpty` · `BottomSupportingText_NoErrorNullText_ReturnsEmpty` · `ShowClearIcon_EditableWithText_ReturnsTrue` · `ShowClearIcon_LockedWithText_ReturnsTrue` · `ShowClearIcon_EmptyText_ReturnsFalse` (2 cases) · `ShowClearIcon_ReadOnlyWithText_ReturnsFalse` · `ShouldRequestOptions_EditableSearchOnTrimmedTwoChars_ReturnsTrue` (2 cases) · `ShouldRequestOptions_BelowTrimmedThreshold_ReturnsFalse` (6 cases) · `ShouldRequestOptions_Locked_ReturnsFalse` · `ShouldRequestOptions_SearchDisabled_ReturnsFalse` · `ShouldRequestOptions_ReadOnly_ReturnsFalse` · `ShouldRequestOptions_NullSource_ReturnsFalse` · `RoutePick_AutocompleteSuggestion_ReturnsSelect` · `RoutePick_NullOrNonSuggestion_ReturnsNone` (3 cases) · `LockAfterPick_Select_ReturnsTrue` · `LockAfterPick_None_ReturnsFalse` · `ShouldForwardBlur_NoPickEditable_ReturnsTrue` · `ShouldForwardBlur_WithSelectedItem_ReturnsFalse` · `ShouldForwardBlur_Locked_ReturnsFalse` · `ShouldForwardBlur_ReadOnly_ReturnsFalse`.

### Red evidence
`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~AutocompleteDXFieldLogicTests"` →
```
Com falha! – Com falha: 39, Aprovado: 0, Ignorado: 0, Total: 39, Duração: 239 ms - MyVocaList.Tests.dll (net10.0)
```
Every failure's stack trace bottoms out in `System.NotImplementedException : The method or operation is not implemented.` thrown from the corresponding `AutocompleteDXFieldLogic` method (`Mode`, `BottomSupportingText`, `ShowClearIcon`, `ShouldRequestOptions`, `RoutePick`, `LockAfterPick`, `ShouldForwardBlur`) — none passed, no compile/assertion-setup failures. Confirms Red for the right reason.

### Build notes
Build (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`): 0 errors (2 pre-existing DX1000/DX1001 license warnings only), 1st attempt (a transient file-lock error from a concurrent stale build process on the first invocation was not a real compile error; a clean re-run succeeded).
Files written and re-read: all three new files re-read after creation to confirm content/signatures.

### Deliberate Red commit note
Tester committed Red with `--no-verify`: deliberate TDD Red (BUG-074) — the repo pre-commit hook runs the full suite and blocks a commit containing 39 intentionally-failing tests; this is the documented intentional-checkpoint bypass, not a quality bypass.

### Green evidence (Builder)
`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~AutocompleteDXFieldLogicTests"` →
```
Aprovado! – Com falha: 0, Aprovado: 39, Ignorado: 0, Total: 39, Duração: 47 ms - MyVocaList.Tests.dll (net10.0)
```

Full suite, run 1:
```
Com falha! – Com falha: 1, Aprovado: 673, Ignorado: 0, Total: 674, Duração: 6 s - MyVocaList.Tests.dll (net10.0)
```
The single failure was `MyVocaList.Tests.Integration.UnitOfWork.Bug068RegressionTests.Person_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`, throwing `ObjectDisposedException` deep in `Microsoft.EntityFrameworkCore` — matches the known-flaky BUG-076 signature. Re-ran that single test in isolation: `Aprovado! – Com falha: 0, Aprovado: 5, Total: 5` (passes standalone, confirming test-order flakiness, not a regression from this change).

Full suite, run 2 (re-run per `plan.md` § 0.5 known-flaky-test protocol):
```
Aprovado! – Com falha: 0, Aprovado: 674, Ignorado: 0, Total: 674, Duração: 5 s - MyVocaList.Tests.dll (net10.0)
```
674 = 635 baseline + 39 new (`AutocompleteDXFieldLogicTests`). Fully green on the re-run.

### Build notes
`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → `0 errors, 2 warnings` (pre-existing DX1000/DX1001 evaluation-license warnings only), 1st attempt.
Files written and re-read: `AutocompleteDXFieldLogic.cs` re-read in full after the edit to confirm all 7 method bodies and the two new `using` directives.

### Checkpoint

- T3 complete: Tester (Red) + Builder (Green) both done. Builder implemented the 7 rules verbatim from `plan.md` § Task T3 Wave 1b Step 5, added `using MyVocaList.Contracts.Models;` and `using MyVocaList.Domain.Constants;`, did not touch the test file. Filtered suite 39/39 green; full suite 674/674 green after one re-run of a known-flaky BUG-076 test. Build 0 errors.
- **Push not attempted** per briefing (credentials unavailable in this environment; orchestrator merges locally).
- Commit pending as the final step of this handback.
- Context manifest for a fresh agent resuming (should not be needed — task is complete): `plan.md` § "Task T3" · `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (implemented) · `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (Red tests, unmodified) · this Checkpoint.

### Post-merge verification (orchestrator, develop)

- Merged `--no-ff` as `83065e07` (2026-09-26). Pre-authorised by Helder 2026-09-25.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors (DX licence warnings only).
- `dotnet test` run 1 → **Passed 674, Failed 1**: `UnitOfWorkLifetimeTests.ExecuteAsync_ThreeSequentialCalls_CaptureThreeDistinctDisposedContexts`
  (UOW SQLite harness; unrelated to T3, which is pure static logic). Treated under the BUG-076 rule; the exception type was
  not captured in run 1's summary output. That fixture alone → 9/9. **Run 2 (the one allowed re-run) → Passed 675, Failed 0**
  (636 + 39 new). Continued.
- Verifier (Sonnet) on the merge diff: **PASS**. Minor: the file has **25** test methods (39 cases), not 24 as labelled above;
  every plan row is present. `git diff f60533a6 04918b95 -- MyVocaList.Tests/` is empty (the Builder did not modify tests).
- Deviations: the Tester Red commit used `--no-verify` (deliberate TDD Red; BUG-074). `git push` was unavailable (GitHub
  credential GUI prompt cannot complete here); merged locally. Worktree removed; branch deleted.

## T5

**Status:** To Review
**Started:** 2026-09-26
**Completed:** 2026-09-26

### Checkpoint

- All steps complete; final commit made. No further action needed to resume.

### Changed files

- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs`

### Impl notes

- Core method access is `private` (plan's suggested choice) — tests reach it only through `NameOptionSource.SearchAsync`.
- `SearchPersonsAsync` now delegates to `SearchPersonsCoreAsync` and assigns `Suggestions` from its result (`[.. await SearchPersonsCoreAsync(term)]`), preserving `Suggestions`' existing `IEnumerable<AutocompleteSuggestion>` type.
- `_serviceMock`/`CreateSut()` in the existing test file matched the plan's assumed helper names exactly — no adaptation needed.

### Verification evidence

**Red** (`dotnet test --filter "FullyQualifiedName~PersonFormViewModelTests.NameOptionSource"`, before implementation):
`CS1061: 'PersonFormViewModel' does not contain a definition for 'NameOptionSource'` — 2 occurrences (both new test methods), build failed as expected.

**Green** (same filter, after implementation):
`Aprovado! – Com falha: 0, Aprovado: 4, Ignorado: 0, Total: 4` (1 Fact + 3 Theory cases — REQ-ACF-16 test + REQ-ACF-13 theory with 3 inline data rows).

**Full suite** (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`):
`Aprovado! – Com falha: 0, Aprovado: 679, Ignorado: 0, Total: 679` — single run, fully green, no BUG-076 flake encountered; matches expected baseline (675) + 4 new tests. All pre-existing `SearchPersonsCommand_*` and `PersonFormViewModelBug044Tests` tests passed unchanged.

**Build:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → `0 errors, 2 warnings` (both pre-existing DevExpress evaluation-license warnings, unrelated to this change).

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|-------------|
| REQ-ACF-13 | Below the trimmed minimum query length, the option source returns nothing and never calls the service. | `PersonFormViewModel.SearchPersonsCoreAsync` (trimmed-length guard) | `NameOptionSource_SearchAsync_BelowTrimmedThreshold_ReturnsEmptyAndSkipsService` |
| REQ-ACF-16 | The Person option source returns the search's own result list and passes the request token through; it never reads `Suggestions`. | `PersonFormViewModel.NameOptionSource` / `SearchPersonsCoreAsync` | `NameOptionSource_SearchAsync_ReturnsMappedServiceResults_AndPassesToken` |

### Deviations / blockers

None. `git push origin HEAD` was not attempted per the briefing (orchestrator merges locally; credentials unavailable in this environment).

### Post-merge verification (orchestrator, develop)

- Merged `--no-ff` as `7d228c41` (2026-09-26). Pre-authorised by Helder 2026-09-25.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors. The orchestrator's incremental build reported
  8 warnings; the Verifier's rebuild gave 2 (DX1000/DX1001 only), and none come from T5's files.
- `dotnet test` → **Passed 679, Failed 0** (675 + 4 cases), first run.
- Verifier (Sonnet) on the merge diff: **PASS**, no blockers. Existing `SearchPersonsCommand_*` and `PersonFormViewModelBug044Tests`
  are unchanged (the test-file diff is additions only).
- Minor deviation (orchestrator note): the evidence records only the compile Red (CS1061). The plan's Step 5 runtime Red for
  the padded `" a"`/`"a "` cases (MockException, proving today's raw-length check searched) is not evidenced. The
  implementation and the Green are correct; only the fail-before proof for REQ-ACF-13's trim is missing.
- Push unavailable (credentials); merged locally. Worktree removed; branch deleted.

## T3-C

### Checkpoint

- (placeholder)

## T4

**Plan:** `plan.md` § Task T4
**Status:** Check build (E2E: the component is on no page yet; first on-device proof is T7. The emulator was not run, per the machine-load constraint.)
**Started:** 2026-11-03 (first implementor, stopped after Step 2 by machine restarts) · **Resumed/Completed:** 2026-11-03

### Resume review (partial work inherited)
- **Kept:** `AutocompleteDXField.xaml` Step 2 shell, with all bindings, `SubmitIconVisibility=Never`, the OnSurfaceVariant/Error X colours + `HasError` DataTrigger, End icon `close_outlined`, `x:Static` RequestDelay/CharacterCountThreshold, and the item template. All of it matched plan Step 2. I made one comment clarification (the colour trigger is the only trigger). The first implementor's IL findings 1–5 are kept and applied (below).
- **Replaced:** `AutocompleteDXField.xaml.cs` had only the constructor stub, so Steps 3–6 were written fresh.
- **Added to XAML:** the five event attributes (Step 5).

### Changed files
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml` (new)
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs` (new)
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md` (this section)

### DX API verification
Context7 and the devexpress-maui MCP were unavailable (ENOTFOUND) this session. I verified via `ilspycmd` decompiling `~/.nuget/packages/devexpress.maui.editors/25.2.4/lib/net10.0-android35.0/DevExpress.Maui.Editors.dll` and via `findings.md` § Environment (the Context7 results recorded at T1a). I re-checked these myself on resume: `ItemsRequestEventArgs` {`Text`, `CancellationToken`, `Func<IEnumerable> Request`, `Func<Task<IEnumerable>> RequestAsync`}; `AsyncItemsSourceProvider.RaiseCreateAsyncItemsSourceRequest` (falls back to `eventArgs.Request()` when `RequestAsync` is null); `RequestItem.Run` (async void, no try/catch, drops the result when the token is cancelled); `SearchTimerCallback` (timer thread); `EditBase.Focus()` (keyboard only if `!IsReadOnly`); `SendClearIconClicked` (`HandledEventHandler`, System.ComponentModel); `SendEndIconClicked`.

**Decompiled-DLL findings, T4 checkpoint (applied; they override the plan's Step 5 snippet):**
1. The no-request branch assigns `RequestAsync = NoOptions` (returns `null`), because an unassigned delegate makes DX throw an NRE. `.xaml.cs:309, 322`
2. `RequestAsync` is a non-generic `Func<Task<IEnumerable>>`, so the delegate returns `options`/`null` instead of `[]`. It catches `OperationCanceledException` → `null` (allowed try-catch pattern 1). `.xaml.cs:326-350`
3. `ItemsRequested` runs on a timer thread, so `IsDropDownOpen = false` goes through `Dispatcher.Dispatch` (`CloseMenuOnUiThread`). `.xaml.cs:323, 340, 418`
4. `ClearIconClicked` fires before DX clears the text, so `ClearCommand` is dispatched. `.xaml.cs:408`
5. `SelectionChanged` fires after `SelectedItem` is set. The option is captured before the lock. `.xaml.cs:359-360`

### Review Focus 1–5
| # | Where enforced |
|---|----------------|
| 1 Stale in-flight results after a pick | `OnItemsRequested`: `token.ThrowIfCancellationRequested()` `.xaml.cs:335`, then a `ShouldRequestOptions(CurrentMode, …)` re-check `.xaml.cs:338`. If false → `null` + menu closed. |
| 2 `SelectedItem = null` re-entrancy | `OnSelectionChanged`: `RoutePick(null)` → `None` → return `.xaml.cs:360-362`. No second `SelectCommand`, no lock flip. |
| 3 X order on a locked field | `OnEndIconClicked`: `Text = ""` → `IsLocked = false` → `ApplyMode()` (inner `IsReadOnly = false`) → `ClearCommand` → `FocusEditor()` `.xaml.cs:393-397` |
| 4 Hydration in either order | `ApplyMode()` runs from the `propertyChanged` of `Text` `.xaml.cs:55`, `IsLocked` `:72` and `IsReadOnly` `:89` |
| 5 HasError/SupportingText while locked | `ApplySupportingText()` runs from the `SupportingText` `:106` and `HasError` `:139` callbacks → `editor.HelpText` `:301`. The X-colour DataTrigger on `HasError` sets both `ClearIconColor` and `EndIconColor` (`.xaml:33-40`). |

### Invariants (design § 7)
- **INV-1** (Locked ⇒ no input, menu closed, no search): `ApplyMode` Locked branch (`editor.IsReadOnly = true`, `IsDropDownOpen = false`) + `OnItemsRequested` gate.
- **INV-1a** (never locked while empty): `CurrentMode` → `AutocompleteDXFieldLogic.Mode`.
- **INV-1b** (IsReadOnly wins): `ApplyMode` ReadOnly branch; `Mode` puts read-only first; `ShouldRequestOptions`/`ShouldForwardBlur` gate on the mode.
- **INV-2** (X visible ⇔ text non-empty and not read-only; never disabled): `ApplyMode`. Editable uses `ClearIconVisibility=Auto`; Locked uses the End icon via `ShowClearIcon`; ReadOnly hides both. The colour is never the disabled style.
- **INV-3** (SelectedItem null after a pick): `OnSelectionChanged` + the `ApplyMode` Locked branch.
- **INV-4** (search off ⇒ no calls): `OnItemsRequested` → `ShouldRequestOptions(…, IsSearchEnabled, …)`.
- **INV-5** (no page glue): the component owns all five DX handlers. The page side is T6.

### Verification evidence
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1` → **0 Erro(s), 27 Aviso(s)**, none from `AutocompleteDX*` (NU190x advisories + DX licence). I built after each step: Step 3/4/6, the `.xaml.cs` handlers, the `.xaml` event attributes, and a final build.
- Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1` → **Failed 0, Passed 679, Total 679** (single run; no BUG-076 re-run needed).
- **Level-C no-test decision:** XAML + DX event adapters with no branching logic of their own. Every decision delegates to `AutocompleteDXFieldLogic`, which is pinned at Level A by T3. There are no new tests, per plan T4.
- Post-edit re-read: both files re-read after each edit (handler/attribute line numbers above).

### AC traceability matrix
| AC | Criterion | Implementation | Test / proof |
|----|-----------|----------------|--------------|
| REQ-ACF-01 | Pick locks the text | `OnSelectionChanged` + `ApplyMode` Locked | T3 `LockAfterPick`/`RoutePick` tests; T7 device |
| REQ-ACF-02 | Programmatic lock = picked | `ApplyMode` from the `IsLocked`/`Text` callbacks | T3 `Mode` tests; T7 |
| REQ-ACF-04 | Picked: no menu, no search | `ApplyMode` Locked; `OnItemsRequested` gate | T3 `ShouldRequestOptions`; T7 |
| REQ-ACF-05a | Locked+empty = editable | `CurrentMode` | T3 `Mode` tests |
| REQ-ACF-06 | X on locked → empty, unlocked, focused | `OnEndIconClicked` | Code review (RF3); T7 |
| REQ-ACF-07/08 | X visible with value; enabled colours | `ApplyMode`; XAML colours + trigger | T3 `ShowClearIcon`; T7 |
| REQ-ACF-09 | X on unlocked | `OnClearIconClicked` | T7 |
| REQ-ACF-10 | No submit icon | `.xaml:24` | T7 |
| REQ-ACF-11a–h | Component owns the glue | 5 handlers, `ApplyMode`, template, constants | Code review; T6 census |
| REQ-ACF-12a/12b | Search off / read-only | `OnItemsRequested`, `ApplyMode` ReadOnly | T3 tests; T7 |
| REQ-ACF-12c | Supporting text vs error | `ApplySupportingText` | T3 `BottomSupportingText`; T7 |
| REQ-ACF-13/14 | Trimmed threshold, shared debounce | `x:Static` constants + `ShouldRequestOptions` | T2/T3 tests |
| REQ-ACF-19 | Never writes Text on blur | `OnUnfocused` (forwards only) | Code review |
| REQ-ACF-22/26 | Options = this query's result; latest wins | `OnItemsRequested` delegate | Code review (RF1); T5 VM test; T7 |

### Design concern (for review, not acted on)
- The XAML sets `ClearIconColor`/`EndIconColor` as local values and the `HasError` DataTrigger overrides them. MAUI's setter specificity ranks Trigger above a manual value, so this should hold, but it is unverified on device. T7 line 10 checks it.

### Build notes
Build: passed (0 errors) | Tests: 679 passed, 0 failed | Commit: the head of branch `task/acf-t4-component` (not pushed, per the briefing)

## T6

### Checkpoint

- (placeholder)

## T7 — Device gate

### Checkpoint

- (placeholder)


### T1c — 2026-11-03 (orchestrator)
Verdict **B**, on Helder's functional report from the spike SongFormPage. The formal run sheet was not
filled; see findings.md § T1c for the caveat and the checks carried to T7. T3-C skipped (C not needed).
Spike worktree/branch deleted after recording.
