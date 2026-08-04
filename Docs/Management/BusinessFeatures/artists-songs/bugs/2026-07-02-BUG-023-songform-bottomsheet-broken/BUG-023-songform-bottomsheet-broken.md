# BUG-023 — SongFormPage BottomSheet state bindings broken (Critical)

## Status
Fixed — 2026-07-03

## Emulator smoke test — BLOCKED 2026-07-03
Helder's emulator session (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-002) could not exercise this fix: song creation itself is blocked by **BUG-027** (SongFormPage Artist field has no working required-field validation/autocomplete, so no song can be saved to trigger the resolution flow at all). Re-run TEST-002 once BUG-027 is fixed.

## Root cause
Commit `e743601` ("fix errors", 2026-06-23) removed `IsExpanded="{Binding IsResolutionSheetVisible, Mode=TwoWay}"` and `IsExpanded="{Binding IsMergeSheetVisible, Mode=TwoWay}"` from `resolutionSheet` and `mergeSheet` in `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` without any replacement (likely because `IsExpanded` is not a valid `dx:BottomSheet` binding target in the DevExpress version in use — `dx:BottomSheet` has no bindable "open" property; opening/closing requires calling `Show(BottomSheetState, Page)` / `Close()` with a host `Page` reference, which cannot be expressed as a pure XAML two-way binding). `SongFormViewModel` continued to set `IsResolutionSheetVisible` / `IsMergeSheetVisible` correctly (confirmed unchanged and already covered by `SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible`), but nothing in the view observed those flags after the removal. Result: the resolution/merge BottomSheets never opened — the entire "Save as new version" / "Update existing" / "Merge fields" flow was unreachable whenever Save encountered a duplicate song title. Save appeared to silently do nothing.

## Fix
Restored two-way sync using the project's confirmed **BottomSheet State Management (Code-Behind Pattern)** (`.claude/library/dialogs-validation.md § BottomSheet State Management`), the same pattern used by `ConfirmSheet.xaml.cs`:

- `SongFormPage.xaml`: added `StateChanged="OnResolutionSheetStateChanged"` to `resolutionSheet` and `StateChanged="OnMergeSheetStateChanged"` to `mergeSheet`.
- `SongFormPage.xaml.cs`:
  - Subscribed to `SongFormViewModel.PropertyChanged` in the constructor.
  - On `IsResolutionSheetVisible` change: calls `resolutionSheet.Show(BottomSheetState.HalfExpanded, this)` when `true`, `resolutionSheet.Close()` when `false`.
  - On `IsMergeSheetVisible` change: same pattern for `mergeSheet`.
  - `OnResolutionSheetStateChanged` / `OnMergeSheetStateChanged` sync the sheet's `StateChanged` event (`BottomSheetState.Hidden`) back to the corresponding ViewModel flag, guarded by a re-entrancy flag (`_isSyncingResolutionSheet` / `_isSyncingMergeSheet`) to avoid a ViewModel → view → ViewModel loop.

`SongFormViewModel` was **not modified** — its flags were already being set correctly at every point in the resolution/merge flow (`SelectResolutionCandidateAsync`, `ConfirmSaveAsNewVersionAsync`, `DismissResolutionSheet`, `ConfirmMergeAsync`, `DismissMergeSheet`).

## Files changed
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — added `StateChanged` wiring on `resolutionSheet` and `mergeSheet`.
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — added `PropertyChanged` subscription + sheet open/close sync + `StateChanged` handlers.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — added `DismissResolutionSheetCommand_AfterExactLocalMatch_SetsIsResolutionSheetVisibleFalse` regression guard.

## Regression test note
This bug is a XAML-binding/wiring bug, not directly unit-testable — no unit test can observe whether a `dx:BottomSheet` visually opens. The pre-existing test `SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible` already proved the ViewModel half of the contract (the flag flips to `true`) and was unaffected by this bug — it was green both before and after the fix, so it does not prove the fix by itself.

Added `DismissResolutionSheetCommand_AfterExactLocalMatch_SetsIsResolutionSheetVisibleFalse` as an additional guard: it exercises the full open → dismiss round trip on the flag that the new code-behind wiring depends on (`DismissResolutionSheetCommand` is bound to the sheet's Cancel button and must still flip `IsResolutionSheetVisible` back to `false` for `resolutionSheet.Close()` to ever be invoked reactively). This test also passed immediately (Green before and after) since `SongFormViewModel` was never broken — per `bug-tracking.md`, this is documented as a guard test, not a Red-proving one.

**Full suite:** 436 tests passing (435 baseline + 1 new), 0 failures.

## Manual E2E verification required (Helder — emulator)
Unit tests cannot verify the actual XAML binding restoration. Run this on the emulator in the same session as the pending Phase 16C.1 / Wave 5.2 smoke tests:

1. Open Songs → Add Song.
2. Enter a title that exactly matches an existing song for the selected artist (triggers `ExactLocalMatch`).
3. Tap Save.
4. **Expected:** the resolution BottomSheet slides up from the bottom (half-expanded), showing the matching candidate(s) and a "Save as new version" option.
5. Tap the candidate's "Select" button (or enter a Version label and tap "Save As New Version") — confirm the sheet closes and the expected save action completes (snackbar confirmation).
6. Repeat steps 1–4, this time tap "Cancel" — confirm the sheet closes and the form returns to editable state with no data loss.
7. If the resolution flow can produce a merge scenario (target has manual edits + field diffs) — repeat for the Merge BottomSheet: confirm it opens, "Apply Selected Changes" and "Cancel" both close it correctly.

## Regression risk
Low — the fix is additive (new event wiring); `SongFormViewModel` was not touched, and the sheets' `AllowDismiss="False"` setting is unchanged so swipe-dismiss behavior is unaffected. The `_isSyncing*` re-entrancy guards prevent the closed-loop `PropertyChanged` → `StateChanged` → `PropertyChanged` scenario from causing a stack overflow or flicker.

## Commit
`fix: SongFormPage — BUG-023 restore BottomSheet state bindings` (branch `develop`, based on `74c5385`).

## Post-review hardening (2026-07-03)
Three review findings were applied to `SongFormPage.xaml.cs` in a follow-up commit — no ViewModel, XAML, or test changes were needed:

1. **Exception-safe re-entrancy guards (Medium):** `OnResolutionSheetStateChanged` / `OnMergeSheetStateChanged` now wrap the guarded `ViewModel.Is*SheetVisible = false` write in `try { ... } finally { _isSyncing* = false; }`. Previously, if the setter threw, the guard flag stayed `true` forever and the affected sheet could never open again.
2. **Guard the VM→View sync direction too (Medium):** `SyncResolutionSheetState()` / `SyncMergeSheetState()` now set `_isSyncingResolutionSheet` / `_isSyncingMergeSheet` (with the same `try/finally` reset) around the `Show()`/`Close()` calls, and the corresponding `StateChanged` handlers now check the guard at entry (`if (_isSyncingResolutionSheet) return;` / same for merge). Previously the loop was only avoided by coincidence of current Hidden-only semantics; the guard now makes the short-circuit deterministic in both directions.
3. **Unsubscribe `PropertyChanged` on teardown (Low):** added `OnDisappearing()` override that unsubscribes `ViewModel.PropertyChanged -= OnViewModelPropertyChanged`, mirroring the codebase's existing `OnDisappearing`-based teardown pattern (`QueueManagementPage.xaml.cs`). Prevents a leaked subscription keeping the page/ViewModel alive after navigation away.

Verification: `dotnet build` — 0 errors; `dotnet test` — 436/436 passing (no test changes required; behavior of the open/close flow is unchanged, only its exception-safety and determinism improved).
