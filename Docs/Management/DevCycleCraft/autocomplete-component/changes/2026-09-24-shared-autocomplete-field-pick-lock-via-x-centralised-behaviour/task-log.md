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

### Checkpoint

- (placeholder)

## T2

### Checkpoint

- (placeholder)

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

## T5

### Checkpoint

- (placeholder)

## T3-C

### Checkpoint

- (placeholder)

## T4

### Checkpoint

- (placeholder)

## T6

### Checkpoint

- (placeholder)

## T7 — Device gate

### Checkpoint

- (placeholder)
