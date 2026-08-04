# BUG-011 — QueuePage BottomSheet double-add on navigation

**Filed:** 2026-06-11
**Feature area:** Queue Management
**Severity:** High — causes 4103ms UI freeze (245 skipped frames / Davey!) on every navigation to QueuePage after first visit
**Status:** ✅ Fixed (2026-06-19, branch `fix/bug-011-queue-bottomsheet`, not yet merged)
**Recommended model:** `claude-sonnet-4-6` — XAML/code-behind fix, single-file guard pattern; no architectural decisions required

## Resolution (2026-06-19)

**Misattribution correction:** The title/affected-files name `QueuePage`, but that file is a dead 712-byte placeholder. The real wired page is **`QueueManagementPage`** (Shell root view, backed by `QueueManagementViewModel` + `IQueueServiceNew`). The fix was applied there, not in `QueuePage`.

**Root cause:** `QueueManagementPage.xaml` declared an inline `dx:BottomSheet x:Name="finishEventSheet"` with `IsModal="True"` as a direct child of the page root `<Grid>`. DevExpress re-parents modal sheets to a window overlay on show; on the 2nd navigation to the Shell-cached page the visual-tree reattach collided → `BottomSheet is already a child of ... Grid` + Davey jank.

**Fix:** Removed the inline sheet and routed the finish confirmation through the existing safe `ConfirmSheet` ContentView wrapper (driven by `FinishConfirmSheetState` TwoWay; the modal BottomSheet now lives inside the ContentView and only `Show()`s on an explicit non-Hidden state transition, so a fresh cached-page reattach never re-shows / re-parents). `ConfirmSheet` itself was reused unchanged (governed component).

**AC-5.3 now wired:** Previously the "Finish" button bound straight to `FinishEventCommand` with NO confirmation (irreversible archive without a prompt — violated AC-5.3 / Flow 4). Now "Finish" → `RequestFinishEventCommand` opens the confirmation; only Confirm (`FinishEventCommand`) transitions STARTED → FINISHED; Cancel/dismiss (`DismissFinishConfirmCommand`) leaves the event unchanged.

**Regression test:** `MyVocaList.Tests/Unit/ViewModels/QueueManagementViewModelTests.cs` — 3 tests tagged `// [AC] AC-5.3` (`RequestFinishEvent_StartedEvent_OpensConfirmationWithoutFinishing`, `FinishEvent_AfterConfirmation_FinishesEventAndClosesSheet`, `DismissFinishConfirm_AfterOpening_ClosesSheetWithoutFinishing`). Seen Red (VM members absent before the change) → Green after.

**Verification:** `dotnet build` 0 errors; the 3 new ViewModel tests pass; `QueueRepositoryTests` 5/5 green in isolation. Intermittent full-suite `QueueRepositoryTests` failures (`SQLite Error 19: FOREIGN KEY constraint failed`, count varies 3→1→0 across runs) are the pre-existing flaky parallel-SQLite race — unrelated to this change (green with the change stashed), tracked in BACKLOG. ⏳ Helder: emulator E2E for AC-BUG011-1/2/3.

### Emulator E2E — BLOCKED 2026-07-03
Helder's attempt (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-012) found **no reachable entry point into `QueueManagementPage`** from the current build — there is no way to navigate to it to exercise the BottomSheet fix at all. This is consistent with the in-flight **Queue Entry Point Redesign** work (`BACKLOG.md` Queue Management section, 🟡 In Progress): the old `QueuePage`/`EventsPage` entry points are being replaced and the new CRUD-list entry point is not yet wired. Re-run TEST-012 once the Queue Entry Point Redesign lands a working navigation path to `QueueManagementPage`.

**Files changed:** `QueueManagementPage.xaml`, `QueueManagementPage.xaml.cs`, `QueueManagementViewModel.cs`, + new `MyVocaList.Tests/Unit/ViewModels/QueueManagementViewModelTests.cs`. Committed as `e4d09bb` on branch `fix/bug-011-queue-bottomsheet` (not pushed, not merged).

## Symptom

When navigating to QueuePage a second time (Shell caches the page instance), Android logcat emits:

```
DevExpress.Maui.Controls.BottomSheet is already a child of Microsoft.Maui.Controls.Grid. Remove DevExpress.Maui.Controls.BottomSheet from Microsoft.Maui.Controls.Grid before adding to MyVocaList.UI.Pages.Queue.QueuePage.
```

Immediately followed by a 4103ms Davey burst (245 skipped frames). The page renders but the freeze is visible and severe.

## Root cause (hypothesis)

Shell navigation caches page instances after first visit. QueuePage likely adds one or more `dx:BottomSheet` controls to the page Grid in code-behind (or via a lifecycle method) without checking whether they are already attached. On second visit, the add is attempted again, triggering the DevExpress guard throw (which is caught internally) plus the resulting UI jank from the failed view-tree mutation.

## Affected files (to investigate)

- `MyVocaList/UI/Pages/Queue/QueuePage.xaml`
- `MyVocaList/UI/Pages/Queue/QueuePage.xaml.cs`

## Fix approach

1. Identify where the BottomSheet is being added dynamically (code-behind `OnAppearing`, constructor, or similar lifecycle hook).
2. Guard the add with a check: only add the BottomSheet if it does not already have a parent.
3. Alternatively, move the BottomSheet to XAML markup so it is part of the initial inflation and never re-added.
4. Verify fix: navigate to QueuePage, navigate away, navigate back — no Davey burst, no logcat warning.

## Acceptance criteria

- AC-BUG011-1: Navigating to QueuePage twice in one session produces no `BottomSheet is already a child` logcat warning.
- AC-BUG011-2: Second navigation to QueuePage causes no Davey burst (≤ 16ms per frame, no `Choreographer Skipped` warning in logcat).
- AC-BUG011-3: QueuePage functionality (BottomSheet open/close, queue interaction) is unaffected.

## Out of scope

- Other pages that use BottomSheet (unless the same pattern is confirmed)
- QueuePage performance beyond the double-add fix
