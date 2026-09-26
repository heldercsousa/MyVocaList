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

**Status:** Red — tests written

### Changed files:
- `MyVocaList/UI/Models/IMenuOptionSource.cs` (new) — `IMenuOptionSource` + `MenuOptionSource` adapter, complete (contract only).
- `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (new) — `FieldMode`, `PickRoute` enums + 7 `internal static` signatures, each body `throw new NotImplementedException();`.
- `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (new) — 24 test methods / 39 test cases (Theories expand to multiple cases), one `// [AC]` per test.

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
Committed with `--no-verify`: deliberate TDD Red (BUG-074) — the repo pre-commit hook runs the full suite and blocks a commit containing 39 intentionally-failing tests; this is the documented intentional-checkpoint bypass, not a quality bypass.

### Checkpoint

- T3 Tester wave complete: Red confirmed, task-log written. Next: commit (`--no-verify`) and push, then stop (Builder is a separate dispatch).
- Context manifest for a fresh agent resuming: `plan.md` § "Task T3" (Wave 1b steps 5-7, the exact rule table) · `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (skeleton to fill) · `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (tests, do not edit) · `MyVocaList/UI/Models/IMenuOptionSource.cs` (already complete, no action needed).

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
