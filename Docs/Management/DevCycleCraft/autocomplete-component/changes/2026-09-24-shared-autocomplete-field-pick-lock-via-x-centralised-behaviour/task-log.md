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

## T3

### Checkpoint

- (placeholder)

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
