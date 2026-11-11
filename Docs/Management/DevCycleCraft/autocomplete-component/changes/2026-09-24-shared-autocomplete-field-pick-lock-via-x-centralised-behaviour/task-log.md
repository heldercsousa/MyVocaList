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

**Status:** To Review (E2E: not run - no emulator per briefing; device gate is T7)
**Started/Completed:** 2026-11-04

Replaced the Full Name `dxe:AutoCompleteEdit` in `PersonFormPage.xaml` with `textfields:AutocompleteDXField` (bindings per plan T6; `IsLocked` unbound; character counter untouched). Added `xmlns:textfields`. Code-behind: deleted `OnNameItemsRequested`, `OnNameSelectionChanged`, `OnNameUnfocused`; focus call is now `nameField.FocusEditor()`; birthday/email blur handlers kept. Line numbers in the plan matched the file (XAML 19-43, cs 33-61, focus at 30).
Context7 / devexpress MCP not needed (no new DX API used beyond the T4 component).

**Level C - no-test decision:** page swap with no new logic; behaviour is covered by existing PersonFormViewModel tests and the T7 device gate.

### Observation (not acted on)
`PersonFormPage.xaml` has `SafeAreaEdges="All"`, not `"Container"` as CLAUDE.md prescribes. Pre-existing and left untouched (out of T6 scope); flagging for the orchestrator.

### Verification evidence
- After XAML edit only, build errors were exactly 2 x CS1061 `AutocompleteDXField` has no `SelectedItem` at `PersonFormPage.xaml.cs(52,23)` and `(59,23)` (expected).
- Final build `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -m:1`: exit 0, 0 errors (only pre-existing NU1903 warnings).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -m:1`: `Aprovado! - Com falha: 0, Aprovado: 657, Ignorado: 0, Total: 657` (T4 log recorded 679 on its branch; this worktree's base differs, no tests were added/removed by T6).
- REQ-ACF-11 Python substring scan: `PASS`.

### Changed files:
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`

## T7 — Device gate

### Checkpoint

- 2026-11-05: run 1 recorded. Spec amended (REQ-ACF-29, design § 3.1/§ 4 notes, T8 added). Next: T8
  on `task/acf-t8-t7-fixes`, then the T7 re-run with a T7-prep build.
- 2026-11-05 (later):
  - T8 `2b4bc57b`: branch build 0 errors and 813/813 tests; verifier PASS. Merged into develop as `97a329e1`.
  - **Post-merge build/test NOT yet verified.** The run was killed by Claude Code's low-memory
    reaper, not a failure. Do not tick T8 or remove `.worktrees/acf-t8` until develop is green.
  - T7-prep: worktree `.worktrees/acf-t7-prep` (`spike/acf-t7-supporting-text`). The edit
    `SupportingText="Test"` is applied but uncommitted, because its build was also reaped.
  - Next: run the two builds one at a time (develop post-merge, then T7-prep), then the device re-run.
- 2026-11-05 (closure): **T8 post-merge verification satisfied by evidence, not by a fresh run.**
  - The parallel SFT session's 5.3 Verifier built `03df3a34` for Android (0 errors) and ran the
    full suite there (874/874). That commit already contains the T8 merge `97a329e1`.
  - After the 5.3 merge (`e1721279`), develop ran 874/874.
  - `git diff 03df3a34 develop -- *.cs *.xaml *.csproj` is empty, so develop's code equals the verified tree.
  - T8 ticked. Worktree `acf-t8` removed and branch `task/acf-t8-t7-fixes` deleted.
  - Governance re-census: `AutocompleteDXField` is still consumed only by `PersonFormPage.xaml`, so
    it is not yet governed.
  - T7-prep fast-forwarded to develop `a2cee866` (edit kept); its Android build is running alone.
  - Manifest: `tasks.md` § T7/T8, this file § T7/T8, `requirements.md` REQ-ACF-18/24/26/29.

### Run 1 — 2026-11-05 (Helder, Android, built from develop; the T7-prep build was never created)

| Line | Result | Helder's note | Diagnosis (static, Explore subagent) |
|------|--------|---------------|--------------------------------------|
| 1 | **FAIL** | Full Name not focused on load | HYP: `FocusEditor()` (component) calls `editor.Focus()` from `OnAppearing` before the wrapped DX editor's handler is ready. The old unwrapped `nameField.Focus()` worked. |
| 2 | PASS | | |
| 3 | PASS | | |
| 4 | info | "when page just loaded, entry is enabled for editing" | Consistent with REQ-ACF-03. Moot under REQ-ACF-29 (Person never locks). |
| 5 | PASS | | |
| 6 | PASS | | |
| 7 | PASS | String without a space shows "Enter first and last name."; text kept | |
| 8 | PASS (accepted) | No counter; errors stay until fixed | By design: the counter shows above 180 chars (`PersonService.cs:22`); reward-early re-validation (`dialogs-validation.md` § Validation timing) is app-wide and unchanged since cf096dc8^. Helder accepted it on 2026-11-05. |
| 9 | **FAIL** | Picked item reappears in the menu after a pick, and on an edit-mode load | The component closes the menu on a pick but not on programmatic `Text` (hydration, post-pick DX text write). Possible in-flight request race. The Song form Artist pattern (close the menu on a programmatic set, reset `SelectedItem`) is the reference. |
| 10 | **FAIL** | No placeholder in insert mode | `PlaceholderText` is forwarded. DX shows the placeholder only when focused with `LabelText` set, as on Birthday/Email, so this is a consequence of line 1. Supporting text not tested (no T7-prep build). |

Severity: Minor for all (each has a workaround). Fixes in T8, no bug folders. Helder ruling of
2026-11-05: Person Full Name never locks (REQ-ACF-29).


### T1c — 2026-11-03 (orchestrator)
Verdict **B**, on Helder's functional report from the spike SongFormPage. The formal run sheet was not
filled; see findings.md § T1c for the caveat and the checks carried to T7. T3-C skipped (C not needed).
Spike worktree/branch deleted after recording.

### 2026-11-03 — Helder decision: keep the pick-lock
Helder tested `sft-g5-spike` (which predates T4 and uses the old per-page autocomplete) and saw no lock. Once told that the lock lives only in `AutocompleteDXField`, he chose: "keep the lock once it already lives in the component". No spec change. The lock is verified on device in T7 (Person form, after T6).

## T8 — T7 run 1 fixes + pick-lock opt-out

**Status:** To Review (device re-run of T7 pending)
**Started / Completed:** 2026-11-05

### Checkpoint
Done: steps 1-7 (all). Build 0 errors, tests 813/813. Next: commit + push. Manifest: none needed (work complete).

### Changed files:
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md` (this section)

### Verification evidence
- Red: 4 new REQ-ACF-29 tests failed to compile (CS1739, no `isPickLockEnabled` parameter). Green: `AutocompleteDXFieldLogicTests` 43/43.
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` passed, 0 errors (28 NU190x package-vulnerability warnings, pre-existing).
- Tests: full `dotnet test` 813 passed, 0 failed (single run, no BUG-076 flake observed).
- Context7 (DevExpress .NET MAUI): `AutoCompleteEditBase.TextChanged` is `EventHandler<AutoCompleteEditTextChangedEventArgs>` with `Reason`; `AutoCompleteEditTextChangeReason` = UserInput, ProgrammaticChange, ItemSelected. Matches the spec. (Docs list the NuGet as 26.1.5; the project's pinned version was not cross-checked against it.)
- Observed `TextChanged` vs `SelectionChanged` event order: unknown (not determinable without a device); the T7 re-run will show it.

### AC traceability (REQ-ACF-29)
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ACF-29 | Opt-out: locked flag with text stays Editable | `AutocompleteDXFieldLogic.Mode(..., isPickLockEnabled)` | `Mode_PickLockDisabled_LockedWithText_ReturnsEditable` |
| REQ-ACF-29 | Read-only still wins | same | `Mode_PickLockDisabled_ReadOnly_ReturnsReadOnly` |
| REQ-ACF-29 | A pick does not lock | `LockAfterPick(route, isPickLockEnabled)`; `OnSelectionChanged` writes `IsLocked` only when true | `LockAfterPick_PickLockDisabled_Select_ReturnsFalse` |
| REQ-ACF-29 | No pick, no lock | same | `LockAfterPick_PickLockDisabled_None_ReturnsFalse` |

### Level C no-test decision (steps 3-7)
UI glue with no extractable logic: the `IsPickLockEnabled` BindableProperty, `OnTextChanged` (REQ-ACF-24), pick counter and stale-result discard (REQ-ACF-26), `FocusEditor()` deferral (REQ-ACF-18), and `PersonFormPage.xaml` `IsPickLockEnabled="False"`. Verified by build and by the T7 device re-run.

### Notes
- `FocusEditor()` retry subscribes to `HandlerChanged` and `Loaded` (one-shot, both unsubscribed on the first ready signal; a handler-removal `HandlerChanged` is ignored).
- Stale-result discard returns an empty array (not null), per the spec.

### Run 2 — 2026-11-06 (Helder, Android; lines 1–9 from develop, line 10 from `.worktrees/acf-t7-prep` @ `624ff33b`)

| Line | develop | T7-prep | Note |
|------|---------|---------|------|
| 1 | PASS | PASS | Autofocus fixed (T8). |
| 2 | **FAIL** | **FAIL** | "  a" (2 spaces + 1 char) searches and shows results (T7-prep: the "not found" row shows for 1 or 2 spaces + 1 char). Helder: the search string must be trimmed at both ends and inner runs of spaces collapsed to one, using the existing Extensions-project helper that is used on persist/search. REQ-ACF-13. |
| 3–7 | PASS | PASS | |
| 8 | PASS | PASS | The error stays until fixed. Helder confirms this is the app-wide pattern, and every entry (autocomplete included) must follow it. |
| 9 | PASS with issue | same | After a pick, the entry stays focused (keyboard up, highlighted) with the cursor at position 0, so the user must move the cursor to edit. Plus extra issue F1 (menu reappears on an edit load and after a pick), which is still failing REQ-ACF-25. |
| 10 | — | PASS | Placeholder on load, "Test" supporting text, and error replaces it. |

Extra issues, per Helder's ruling, are recorded under Person as
`BusinessFeatures/persons/changes/2026-11-06-person-form-follow-ups-from-acf-t7-run-2/` (F1 menu
reappears, F2 duplicate singer, F3 birthday field), to be tackled in a later session.
Still open in this item: line 2 (search-text normalisation) and the line 9 cursor/focus issue.

## T9 — T7 run 2 fixes (search-text normalisation + pick drops focus)

**Plan:** `tasks.md § T9` · **Status:** To Review · **Started:** 2026-11-06 · **Completed:** 2026-11-06 · **Branch:** `task/acf-t9-t7-run2-fixes` @ `.worktrees/acf-t9` (base `develop` `fd5901c4`)

### Final entry (step 5 complete)

### Changed files:
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md` (`## T9` section only)

`tasks.md` is not in this task's files owned; its T9 box is left for the orchestrator (sequential-only file).

### Root cause, REQ-ACF-13c (step 1, decompiled DX 25.2.4 Editors.dll)

Decompiled from `~/.nuget/packages/devexpress.maui.editors/25.2.4/lib/net10.0-android35.0/DevExpress.Maui.Editors.dll` with ilspycmd (scratchpad only).

Proven in managed code:
1. `AsyncItemsSourceProvider.OnEditorTextChanged` gates on the **raw** `Editor.Text.Length >= CharacterCountThreshold` (2). For `" a"` (raw length 2) DX starts its debounce timer and raises `ItemsRequested`. The component is the only gate, so the normalised check must run there. The threshold itself is unchanged (no DX semantics change).
2. The below-threshold branch only runs when the **raw** length is short. For a raw-long, normalised-short text (`" a"`, `"a "`) DX never takes its own "clear + close" path (`ItemsSource = null; IsLoadingInProgress = false; IsDropDownOpen = false`). The component's no-request branch is therefore the only thing that clears the list and closes the menu.
3. The no-request branch does `e.RequestAsync = NoOptions` (returns `null`). `AsyncItemsSourceProvider.RequestItem.Run` then sets `editor.ItemsSource = null` **after** the debounce, on the UI thread. `CloseMenuOnUiThread()` is dispatched **before** that, from the timer thread. So the close runs before DX clears the list, and the list is cleared up to one debounce interval after the keystroke.
4. `IsDropDownOpen = false` is a MAUI property write. `AutoCompleteEditBaseHandler.UpdateIsDropDownOpen` → `DismissDropDown` runs only on a value change (`UpdateValue("IsDropDownOpen")`). A close while the property is already `false` does nothing to a native popup.
5. Native popup opening and the "no results" row are decided in the Java DX binding (`DXEditors`), which is not decompilable here (same limit as T4 finding 3). Not proven, see "Unproven" below.

Working theory (matches both observed symptoms): the native popup reacts to the keystroke with the previous query's items (stale items, develop build); when the debounce expires the list is replaced with an empty `null` source and the menu is not reliably dismissed (no-results row, T7-prep build). The managed close is queued too early and does not clear the list.

**Fix chosen (component-only, no XAML, no threshold change):** on a `UserInput` text change where `ShouldRequestOptions` is false, clear the list (`ItemsSource = null`), set `IsLoadingInProgress = false` and close the menu synchronously, which is the same sequence DX uses for raw-short text. `OnItemsRequested` keeps the normalised gate as the second guard.

**Unproven, verified only on device (T7 run 3, line 2):** that the native popup stays closed for `" a"`. This is an assumption to review at `To Review`, `Blocking: No`.

### Pick drops focus, REQ-ACF-30 (step 5)

- DX 25.2.4 cursor member: `AutoCompleteEdit.CursorPosition` (public `int`, decompiled `AutoCompleteEdit.cs:167`). Context7 check: see the result line below.
- Context7 (`/websites/devexpress_maui`, DX MAUI 25.2 docs): confirmed `AutoCompleteEdit.CursorPosition` (int) and `SelectionStart`/`SelectionLength`, matching the decompile. No mismatch, so no escalation.

### Red evidence (step 2)

- `dotnet test --filter AutocompleteDXFieldLogicTests` before Green: compile failure, CS0117 `SearchQuery` not defined, CS1739 `ShouldForwardBlur` has no `isPickBlur` parameter. The failure is on exactly the new symbols.

### Green (step 3, logic)

- `AutocompleteDXFieldLogic.SearchQuery` added (`NormalizeSearchQuery`); `ShouldRequestOptions` now uses `SearchQuery(query).Length`; `ShouldForwardBlur` gained `isPickBlur = false`.
- `AutocompleteDXFieldLogicTests`: 52 passed, 0 failed (existing tests unchanged).

### AC traceability matrix (T9)

| AC | Criterion (short) | Implementation location | Test method |
|----|-------------------|-------------------------|-------------|
| REQ-ACF-13 (a)(b) | Search text trimmed, inner runs collapsed, null → "" | `AutocompleteDXFieldLogic.SearchQuery` | `SearchQuery_TrimsAndCollapsesWhitespace` (3 cases) |
| REQ-ACF-13 (a)(b) | Normalised-short text never requests (`"  a"`, `"a  "`, `"a\t"`) | `ShouldRequestOptions` (uses `SearchQuery`) | `ShouldRequestOptions_NormalisedQueryBelowMinimum_ReturnsFalse` (3 cases), existing `ShouldRequestOptions_BelowTrimmedThreshold_ReturnsFalse` (unchanged) |
| REQ-ACF-13 (a)(b) | Padded two-char query requests (`" an "`) | `ShouldRequestOptions` | `ShouldRequestOptions_PaddedTwoCharsNormalised_ReturnsTrue` |
| REQ-ACF-13 (c) | Below-threshold request shows nothing | `OnItemsRequested` (gate on `query`, `SearchAsync(query)`), `OnTextChanged` UserInput → `ClearMenuOnKeystroke` | Level C: code review + T7 run 3 line 2 (device) |
| REQ-ACF-30 (a)(b) | Pick: cursor to end, unfocus | `OnSelectionChanged` end (`CursorPosition`, `Unfocus()`) | Level C: code review + T7 run 3 line 9 (device) |
| REQ-ACF-30 (c) | Pick-caused blur not forwarded | `ShouldForwardBlur(..., isPickBlur)`, `OnUnfocused` | `ShouldForwardBlur_PickBlur_ReturnsFalse` |
| REQ-ACF-30 (c) | A real blur after pick-then-edit is forwarded | `OnTextChanged` UserInput and `OnFocused` clear `_pickBlurPending` | `ShouldForwardBlur_NotPickBlur_ReturnsTrue` (logic); flag lifecycle Level C |

### Level C no-test decisions (recorded per testing.md)

- `OnItemsRequested`, `OnTextChanged`, `OnSelectionChanged`, `OnUnfocused`, `OnFocused`, `ClearMenuOnKeystroke`, `Focused="OnFocused"` in XAML: UI glue with DX/native objects. No unit test. Verified by build (both TFMs' Android build) and T7 run 3 on device.

### Root cause status and assumption (for review at To Review)

- The managed-side mechanics are proven by the decompile (above). The native popup decision is in the DX Java binding and is not proven here.
- **Assumption, `Blocking: No`:** clearing the list and closing the menu synchronously at the keystroke (for normalised-short text) keeps the menu closed for `" a"`. Verification: T7 run 3, line 2 on device.

### Build notes

- Build (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`): step 4 build succeeded, 0 errors; step 5 build (final code) exit 0, 0 compiler errors.
- Tests: `dotnet test MyVocaList.Tests` 893 passed, 0 failed (includes 52 `AutocompleteDXFieldLogicTests` cases).
- Note: there were two Android builds (after step 4 and after step 5), not one. The first was used to check the NormalizeSearchQuery reference compiles from the UI project (it does, transitively; no ProjectReference added).
- Context7 (`/websites/devexpress_maui`): `AutoCompleteEdit.CursorPosition`, `SelectionStart`, `SelectionLength` confirmed for DX MAUI 25.2. No mismatch.
- No Android emulator run in this session (E2E gate: device check is T7 run 3). Status `To Review` is set on that basis.
- Spec drift: none. The design already specifies the rules implemented; the keystroke close (`ClearMenuOnKeystroke`) is the design's "close the menu" with a list clear, documented here as an implementation choice inside the REQ-ACF-13c fix.

### Checkpoint — T9 merged (2026-11-06)
- Verifier PASS on `88bdae85` (no must-fix). Merged into develop `--no-ff` as `e3c661a6`.
- Post-merge `dotnet test MyVocaList.Tests` on develop: 911/911 passed.
- Next: T7 run 3, lines 2 and 9 only (Helder, Android). Open assumption: the native popup stays closed for " a" (REQ-ACF-13c).

### T7 run 3 (Helder, Android, develop `cbb126f7`, 2026-11-09) — lines 2 and 9 only

| Line | Result | Helder's observation |
|------|--------|----------------------|
| 2 | **FAIL** | The "not found" row appeared in both cases: " a" / "  a" / "a  " (expected: no menu) and "  an  " (expected: results). |
| 9 | **Partial** | PASS: field not highlighted, edit mode loads correctly, tapping a filled field puts the cursor exactly where tapped. FAIL: the keyboard did not close, and after tapping an item the search seems to run again: singers whose names match the picked name are shown. |

- The line 9 failure overlaps F1 (`Docs/Management/BusinessFeatures/persons/changes/2026-11-06-person-form-follow-ups-from-acf-t7-run-2/README.md`: menu reappears on edit load and after a pick).
- The T9 line-2 fix rested on an unproven assumption about the DX native popup (§ T9). Decompiled code alone has not explained the device behaviour, so the next step is on-device evidence (diagnostic logs), not another guess.

### Diagnostic run — emulator (2026-11-09)

Throwaway logging build: branch `spike/acf-t7-run3-diag` (`d1e3f9e6`, local only), Debug APK on the AVD
`pixel_5_-_api_36_0_1` (API 36), driven by the orchestrator through adb. Each failure seen on Helder's
phone reproduced on the emulator. Root causes proven from the `ACF-DIAG` log lines:

- **RC-A (line 2, "Not found" for " a"):** the gate rejects correctly (`OnItemsRequested raw=" a" query="a" gate=False`),
  and we return `null` and close. 10–60 ms **after** the delegate completes, DX sets `IsDropDownOpen=True` itself,
  with `ItemsSource=null`, which renders "Not found". The T9 keystroke clear (`ItemsSource=null`) made this worse. Our close runs
  before DX's open, so it has no effect. DX opens the drop-down even when the editor is not focused.
- **RC-B (line 9 / F1, search runs again after a pick):** the component sets no `DisplayMember`, so on
  `ItemSelected` DX writes the item's `ToString()` (`"AutocompleteSuggestion { Headline = … }"`) into `Text`,
  and the VM's `PersonName` receives it. `SelectCommand` then writes "Helder Sousa", and DX reports that change as
  **`Reason=UserInput`**, so `AsyncItemsSourceProvider` issues a new request 300 ms later
  (`OnItemsRequested raw="Helder Sousa" … editorFocused=False`). The request is not stale, because it started after the pick,
  so the pick counter does not catch it. It returns 1 item and DX opens the menu while the field is unfocused. The
  Song form never hit this: it sets `DisplayMember="Headline"` and locks after a pick.
- **RC-C (line 9, keyboard stays up):** `Unfocus()` works (`IsFocused=False`, blur not forwarded), but the
  IME stays visible (`dumpsys input_method` → `mInputShown=true`). Unfocus does not hide the soft keyboard.
- **F1 edit-load half:** NOT reproduced. Opening the edit form from the Singers list (Edit selected)
  logs only the hydration (`PersonName set … hydrating=true`) and no request, menu or keyboard. Helder's "edit mode"
  observation is the same page after an in-place pick: its title becomes "Edit Singer", and the instance hash is unchanged.
- Correct behaviour confirmed: "  He  " → query "He" → 1 result shown.

## T10 — Root-cause fixes RC-A/B/C

**Plan:** `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md` § T10
**Status:** To Review
**Started:** 2026-11-09
**Completed:** 2026-11-09
**Branch:** `task/acf-t10-root-cause-fixes` @ `.worktrees/acf-t10` (base `develop`, ancestry verified)

### Step 1 — DevExpress mechanism for RC-A (decision)
- Public API (DevExpress.Maui.Editors 25.2.4 XML doc): `ItemsRequestEventArgs` exposes only `Text`, `CancellationToken`, `Request`, `RequestAsync`. No cancel or suppress-open flag.
- `AutoCompleteEdit` public events: `TextChanged`, `Submitted`, `SelectionChanged`, `Focused`/`Unfocused`. No drop-down opening event.
- `IsDropDownOpen` is a documented bindable property. `DropDownStateChanged` exists in the DLL but is not public.
- **Decision:** no supported way to veto DX's own open. Mechanism used: a rejected request sets `_lastRequestRejected`. The editor's `PropertyChanged` for `IsDropDownOpen` closes the menu again, dispatched after DX's write. This is the design § 3.1 RC-A rule, not the "close before DX" guard that did not work.
- Caveat: decompiled IL was not inspected (no decompiler on the machine). The DLL scan used metadata strings and XML docs only.

### Step 1 — mechanism for RC-C (decision)
- MS Learn (MAUI 10): `Microsoft.Maui.SoftInputExtensions.HideSoftInputAsync(this ITextInput, CancellationToken)`, plus `IsSoftInputShowing`.
- Applied when the DX editor is `ITextInput`. Otherwise the Android `InputMethodManager.HideSoftInputFromWindow` fallback under `#if ANDROID`.
- Whether the DX `AutoCompleteEdit` implements `ITextInput` was not confirmed. The fallback runs if it does not. Verify on the emulator (`mInputShown=false`).

### Changes
- **RC-A:** `RejectRequest(e)` replaces the no-request branch. The post-await gate rejection sets the flag. `OnEditorPropertyChanged` closes the menu when `IsDropDownOpen` turns true while the flag is set. `ClearMenuOnKeystroke` and its UserInput call are removed (T9 path).
- **RC-B:** `DisplayMember="Headline"` on the inner `AutoCompleteEdit` (matches the Song form). `_textAtPick` is set from `editor.Text` after `SelectCommand`. `OnItemsRequested` rejects a request where `IsPickEcho(query, _textAtPick)`.
- **RC-B detail (impl decision):** DX reports the `SelectCommand` write as `UserInput`, so the UserInput branch must not clear the pick text on that echo. It keeps `_textAtPick` and `_pickBlurPending` when `IsPickEcho(editor.Text, _textAtPick)`. Any other UserInput change clears both.
- **RC-C:** `HideSoftKeyboardAsync()` runs after `Unfocus()` (`_ = ...`, discarded).
- **Logic:** new pure `AutocompleteDXFieldLogic.IsPickEcho(string? query, string? textAtPick)`. Normalised equality through `SearchQuery`, ordinal, no case folding.
- Spec note added to `design.md` § 3.1 (one blockquote, RC-C bullet).

### Edge (documented, not a defect)
- Trailing whitespace typed after a pick (`"Helder Sousa "`) normalises to the pick text, so it is treated as an echo and does not search. The query is identical after normalisation, so no result is lost.

### AC traceability (Level A logic)
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-ACF-25 | echo query equals pick text, normalised | `AutocompleteDXFieldLogic.IsPickEcho` | `IsPickEcho_QueryMatchesTextAtPick_ReturnsTrue` (3 cases) |
| REQ-ACF-25 | no pick → no echo | `IsPickEcho` | `IsPickEcho_NoPick_ReturnsFalse` |
| REQ-ACF-25 | edited query differs → no echo | `IsPickEcho` | `IsPickEcho_QueryDiffersFromTextAtPick_ReturnsFalse` |

Red evidence: the three tests and the method were written in the same pass before the first run, so a separate Red run was not observed. The first run passed (57/57 in the logic test filter). This is a process gap, recorded here.

### Level C (UI glue, no unit test)
- RC-A (`RejectRequest`, `OnEditorPropertyChanged`), RC-B (`DisplayMember`, `_textAtPick` glue), RC-C (`HideSoftKeyboardAsync`). Verification: emulator adb script (" a", "  a", "a  ", "  He  ", pick → no menu, `mInputShown=false`), then Helder's phone (T7 run 4).

### Changed files
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/design.md` (one `Spec updated 2026-11-09` note only; outside Files owned, permitted write-back)

### Verification evidence
- Logic filter `AutocompleteDXFieldLogicTests`: 57 passed, 0 failed.
- Full `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`: 916 passed, 0 failed.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug`: exit 0, 0 errors, warnings only (NU1903 and pre-existing).
- APK: `MyVocaList/bin/Debug/net10.0-android/com.myvocalist-Signed.apk` (worktree `.worktrees/acf-t10`).
- Emulator run: not performed by the implementor. Orchestrator to run.

### Build notes
Build: passed (0 errors) | Tests: 916 passed, 0 failed | Commit SHA: recorded in git log on `task/acf-t10-root-cause-fixes`
Files written and re-read: `AutocompleteDXField.xaml.cs` (grep of all edited symbols), `AutocompleteDXField.xaml` (DisplayMember line 24), `AutocompleteDXFieldLogic.cs` (IsPickEcho lines 79–80), `AutocompleteDXFieldLogicTests.cs` (IsPickEcho tests), `design.md` (note line 153).

### Checkpoint — T10 emulator check + merge (2026-11-09)
- Emulator (AVD `pixel_5_-_api_36_0_1`, Debug APK of `814c0630`, adb-driven by the orchestrator; screenshots
  checked visually because `uiautomator` does not see the DX popup):
  - " a", "  a", "a  " showed no menu.
  - "  He  " showed 1 result.
  - A pick reopened no menu; `mInputShown=false`; the field was not highlighted.
  - After a pick, clearing the field and typing "he" showed results again.
- The test run was interrupted once by an external reinstall of `com.myvocalist` (15:40:11, `installPackageLI`).
  It was re-run from scratch.
- Verifier PASS (no must-fix). Merged `--no-ff` as `3440d982`. `dotnet test` on develop: 926/926.
- Next: T7 run 4 on Helder's phone (Release APK from develop), checking lines 2 and 9 only.


## Ledger detail moved from LEDGER.md (2026-11-10) — AC-FIELD

> Verbatim text of the former ledger row, moved so the ledger stays one short line per item (`/sln-ledger § Row-length rule`). Nothing was dropped.

- **Feature / Bug:** Shared autocomplete field: pick-lock via X, centralised behaviour and constants (+ BUG-070 copy)
- **Phase / Step:** T1 verdict **B**; T2–T6 merged, all Verifier PASS (T6 `91f87884`, 2026-11-04); T3-C cancelled. Helder chose to keep the pick-lock (2026-11-03). Only T7 remains.
- **Branch:** develop
- **Worktree:** —
- **Status:** blocked: awaiting Helder T7 on device
- **Last commit:** 91f87884
- **Next action:** **STOP T7:** Helder runs the on-device E2E gate on the Person form (focus on open, "jo" lists people, pick opens that person's edit form, field locked until X, blur validation). Pre-existing: `PersonFormPage.xaml` has `SafeAreaEdges="All"` not `"Container"` — awaiting Helder whether to fix separately.

### T7 run 4 (Helder, phone, Release APK from develop, 2026-11-10)

| # | Check | Result |
|---|-------|--------|
| 1 | Short text shows no menu | PASS for "a". New observation: "aa" and "aaa" (valid queries with no match) show DX's "Not found" row. |
| 2 | Padded / multi-space query | PASS: "helder      s" finds "Helder Sousa". "H e l" finds nothing; Helder confirms this pattern is out of scope. |
| 3 | Pick → no menu reopens, keyboard closes, no highlight | PASS |
| 4 | Search works again after a pick | PASS |

- Open question raised by Helder: is the "Not found" row useful at all? An empty result already says nothing was found.

## T11 — No "no results" row (REQ-ACF-31)

### Governance (component-change-governance, four gates)

1. **Dedicated task:** T11. MD3 review: the M3 menu spec has no "no results" item; a menu shows only selectable items, so closing on an empty result is consistent with m3.material.io/components/menus.
2. **Consumer map** (grep `AutocompleteDXField` in `MyVocaList/**/*.xaml`, 2026-11-10): `MyVocaList/UI/Pages/People/PersonFormPage.xaml:20` (`nameField`). Only consumer. `SongFormPage` does not use the component.
3. **Risk:**

   | Consumer | What could break | Verification |
   |---|---|---|
   | PersonFormPage `nameField` | a query with matches also closes (guard flag stuck) | emulator: "aa" → no menu, then "He" → menu opens |

4. **Helder approval:** 2026-11-10, "Go with T11 automatically, from spec into development. 'Not Found' must be removed from everywhere, no matter the context."

### Implementation (2026-11-10)

- **Red:** three tests `ShowsMenu_NullOptions_ReturnsFalse`, `ShowsMenu_EmptyOptions_ReturnsFalse`, `ShowsMenu_OneOption_ReturnsTrue` added to `AutocompleteDXFieldLogicTests.cs`, each tagged `// [AC] REQ-ACF-31`. Run failed to compile (CS0117, `ShowsMenu` missing), as expected.
- **Green:** `internal static bool ShowsMenu(IReadOnlyList<AutocompleteSuggestion>? options) => options is { Count: > 0 };` added to `AutocompleteDXFieldLogic.cs` with XML doc. `AutocompleteDXFieldLogicTests`: 60 passed, 0 failed.
- **Glue:** `AutocompleteDXField.xaml.cs` `OnItemsRequested` RequestAsync delegate, after the Review Focus 1 re-check and before `return options;`: if `!ShowsMenu(options)` → `_lastRequestRejected = true; CloseMenuOnUiThread(); return options;`. `_lastRequestRejected = false;` reset at request start unchanged. Re-read at lines 405-422: edit present, placement correct.
- **No other files touched.** SongFormPage not modified. No existing tests edited. No normalisation added.

### Changed files:
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md` (T11 box ticked)

### Verification evidence
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`: exit 0, **953 passed, 0 failed, 0 skipped** (includes the 3 new REQ-ACF-31 tests).
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug`: exit 0, **0 errors**, 27 warnings (all NU1903 package-vulnerability advisories, none from this change).
- **E2E emulator: not run in this session.** Demo per tasks.md is "emulator run clean, then T7 run 5 on Helder's phone." Manual verification required: "aa" → no menu; "He" → menu opens (PersonFormPage `nameField`).

### AC traceability (REQ-ACF-31)

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-ACF-31 | An empty or null search result never shows the DX "Not found" row; the menu stays closed via the rejected-request guard | `AutocompleteDXFieldLogic.ShowsMenu`; `AutocompleteDXField.xaml.cs` `OnItemsRequested` RequestAsync | `ShowsMenu_NullOptions_ReturnsFalse`, `ShowsMenu_EmptyOptions_ReturnsFalse`, `ShowsMenu_OneOption_ReturnsTrue` |

Glue (code-behind `RequestAsync` → RC-A guard) is not unit-testable; covered by the emulator check above.

### Commit
**Status:** To Review. Committed as `bfe4e6a6` on `task/acf-t11-no-results-row` with `Item: shared-autocomplete-field-pick-lock-via-x-centralised-behaviour` (the commit-msg hook rejects `Item: ACF`: not a registered backlog id). Not pushed, not merged.

### T11 merge + emulator check (2026-11-10)

- Verifier: PASS. Merged `--no-ff` into develop as `25ccbdbc`. Android Debug build from develop: 0 errors.
- Emulator (AVD `pixel_5_-_api_36_0_1`, New Singer, one singer "Helder Sousa" in the DB), visual screenshots:

  | Input | Expected | Result |
  |---|---|---|
  | "aa" (no match) | no menu, no "Not found" | PASS |
  | "aaa" (no match) | no menu, no "Not found" | PASS |
  | "He" | menu with "Helder Sousa" | PASS |
  | "He" → "Hex" (menu open, then no match) | menu closes | PASS |

- Next: T7 run 5 on Helder's phone (line 1: "aa" / "aaa" show nothing).

## T7 run 5 (Helder, phone, 2026-11-11) — line 1 FAIL (flash)

- "aa" / "aaa": "Not found" animates in and out for a fraction of a second on every no-match search. T11 closes the menu only after DX has opened it.

### Root cause (decompiled DX 25.2.4 `DevExpress.Maui.Editors.dll`, ilspycmd, scratchpad only)

1. `AsyncItemsSourceProvider.RequestItem.Run` sets `editor.IsLoadingInProgress = true` **before** awaiting the request, then `ItemsSource = result`, then `IsLoadingInProgress = false`. The handler maps `IsLoadingInProgress` to the native `LoadingInProcess`; the native control shows the drop-down while loading, so a no-match search renders "Not found" for the duration of the search. A rejected request completes synchronously (same frame), which is why REQ-ACF-13c (" a") passes on the phone.
2. `AutoCompleteEditBaseHandler.OnControlDropDownStateChanged` syncs the native state to `IsDropDownOpen` only through `Locker.DoLockedActionIfNotLocked`, and `UpdateIsLoadingInProcess` locks that locker. The MAUI property can therefore stay out of sync with the native popup, and `IsDropDownOpen = false` is then a no-op (finding 4 of the T9 decompile).
3. Emulator evidence (screen recording, frame brightness under the field; scratchpad `flash.sh`): after a "He" search with results, "aaa" and "zq" show "Not found" from the request onwards and it **stays open** (≥ 2.5 s, 12-13 frames). A first "aa" in a fresh form showed nothing. State-dependent, consistent with 2.

**Conclusion:** no close-after-open fix can be flash-free. The drop-down must never be opened by DX's loading path. Candidate: the component runs the search itself (own debounce + cancellation) and opens the list only when there are options (`FilteredItemsSourceProvider` itself opens with `IsDropDownOpen = true`). Spike `spike/acf-t12-own-search` @ `.worktrees/acf-t12-spike` proves it on the emulator before any spec change.

## T12 — Component-owned search (REQ-ACF-31 amended)

**Status:** To Review
**Plan:** `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md` § T12
**Started:** 2026-11-11
**Completed:** 2026-11-11
**Branch:** `task/acf-t12-own-search` (worktree `.worktrees/acf-t12`), from develop f2d6bf68. Spike base: `c5f986e1` (code files only).

### Changed files
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/tasks.md` (T12 ticked)
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/design.md` (one `Spec updated 2026-11-11` note, § 3.1 T12 block)
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/task-log.md` (this section)

### What changed vs the spike (c5f986e1)
- `RestartSearch`: a qualifying query no longer clears `ItemsSource` or closes the menu; it cancels the pending search and starts `RunSearchAsync`. Only a rejected query (pick echo, `ShouldRequestOptions` false, empty or below-threshold text) clears `ItemsSource`, closes the menu and cancels the pending search.
- Blur: `OnUnfocused` calls `CancelSearch()` first. Pick: `OnSelectionChanged` calls `CancelSearch()` after `_pickCount++`. Programmatic text change: already cancelled in `OnTextChanged` (spike).
- `RunSearchAsync`: comment states the UI-context continuation rule (no `ConfigureAwait(false)`), and a `finally` disposes its own `CancellationTokenSource` and clears `_searchCts` if it is still the latest. `CancelSearch` only cancels and forgets the reference, so no disposed CTS is ever cancelled.
- Dead code: confirmed absent (`OnItemsRequested`, `NoOptions`, `RejectRequest`, `_lastRequestRejected`, `OnEditorPropertyChanged` and its subscription, `CloseMenuOnUiThread`, T11 empty-result branch). `ShowsMenu` and its T11 tests kept.
- XAML: removed the now-unused `xmlns:constants` declaration (spike had removed its only use). Spike's `LoadingProgressMode="Manual"` and removed `AsyncItemsSourceProvider` kept.
- Comments: spike labels ("spike B'") removed from production comments; RC-A references gone.

### Build notes
Build: passed (0 errors, 27 warnings; `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug`) | Tests: 953 passed, 0 failed (`dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`) | Commit SHA: b5504214
- The two SongRepositoryArtistTrigramTests SQLite failures the spike saw did not reproduce in this run (953/953 passed). No develop comparison needed; not investigated further.
- Emulator: not run (orchestrator). Demo per tasks.md: `flash.sh` (aa/aaa/zq/Hex/" a" lit=0; He lit>0; He→Hel no blink; pick → list stays closed), then T7 run 6.
- Two background builds were started during this task; the first build's `tail` pipe hid its exit code, so the result above comes from a clean rerun (`build1.log`, `dotnet` EXIT=0).

### Level C justification (no new test for the glue)
- The change is event glue (DX editor events, `CancellationTokenSource` lifetime, UI-thread continuation). No new pure logic was added; decisions stay in `AutocompleteDXFieldLogic` (`ShowsMenu`, `ShouldRequestOptions`, `IsPickEcho`, `SearchQuery`), which already have Level A tests from T9/T11.
- Per `testing.md § TDD Level Guidance`, glue with no pure logic is Level C. The DX editor, its events and the UI thread cannot be exercised in a unit test without mocking DX, and mocking DX would only test the mock.
- Behaviour is verified on the emulator instead (orchestrator): REQ-ACF-31 frame count, plus manual checks for blur-while-pending, pick-while-pending (REQ-ACF-26) and pick-drops-focus (REQ-ACF-30).

### AC traceability (T12)
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| REQ-ACF-31 (amended) | No transient frame, no loading indicator, no blink while typing; rejected query closes | `AutocompleteDXField.xaml.cs` `RestartSearch`, `RunSearchAsync`; `AutocompleteDXField.xaml` `LoadingProgressMode="Manual"` | Emulator: `flash.sh` 0 frames (orchestrator); T7 run 6 pending. Logic: `ShowsMenu` tests (T11) |
| REQ-ACF-13c | Below-threshold / rejected query leaves no menu | `RestartSearch` rejected branch; `ShouldRequestOptions` | Logic tests (T9/T11); emulator " a" lit=0 |
| REQ-ACF-26 | A pick while a search is pending keeps the menu closed | `OnSelectionChanged` `_pickCount++` + `CancelSearch()`; `RunSearchAsync` pick-count check | Level C glue; emulator check (orchestrator) |
| REQ-ACF-14 | Shared debounce constant applied by the component | `RunSearchAsync` `Task.Delay(SearchConstants.AutocompleteDebounceMilliseconds)` | Level C glue; constant value covered by existing constants usage |

Covers REQ-ACF-04, -12a/b, -22, -24/-25, -30 by unchanged code paths (no edits to their logic); regression pass deferred to the orchestrator's emulator run (T7 run 6, lines 2-4 and 9).

### T12 review + emulator check (orchestrator, 2026-11-11)

- **Verifier:** conditional PASS, no blockers. Warning 5 (`_ = RunSearchAsync(...)` catches only cancellation): no change. `code-style-reference.md § Exception Handling` forbids a try-catch for what `GlobalExceptionHandler` already logs, and category 2 (`TaskScheduler.UnobservedTaskException`) logs a failed search as Error. Warning 8 (SHA) fixed above.
- **Emulator** (worktree build, New Singer, one singer "Helder Sousa"), `flash.sh` lit frames / max brightness, two runs:

  | Input | Run 1 | Run 2 | Expected |
  |---|---|---|---|
  | aa | 0 / 38 | 0 / 38 | 0 |
  | He | 17 / 232 | 15 / 232 | > 0 (list) |
  | aaa | 0 / 38 | 0 / 38 | 0 |
  | zq | 0 / 38 | 0 / 38 | 0 |
  | Hex | 0 / 38 | 0 / 38 | 0 |
  | " a" | 0 / 38 | 0 / 38 | 0 |
  | He → Hel (`blink.sh`, dark frames) | 0 of 255 | 0 of 134 | 0 (no blink) |

  REQ-ACF-31 pass criterion met. Develop baseline before T12: aaa/zq 12-13 frames of "Not found", stuck open.
