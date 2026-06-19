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

**Regression test:** `MyVocaList.Tests/Unit/ViewModels/QueueManagementViewModelTests.cs` — 3 tests tagged `// [AC] AC-5.3`. Seen Red (VM members absent before the change) → Green after. Full suite 357 passed.

## Resolution (2026-06-19)

**Misattribution corrected:** the title names `QueuePage`, but that page is a 712-byte placeholder (dead leftover). The real, Shell-cached root view is **`QueueManagementPage`**, and the offending control was its inline `dx:BottomSheet x:Name="finishEventSheet"` with `IsModal="True"`, declared directly in the page's root `<Grid>`. A DevExpress modal BottomSheet re-parents itself to a window-level overlay when shown; the original Grid still lists it as a child, so when Shell reattaches the cached page tree on a 2nd navigation the sheet collides → the logged warning + Davey.

**Fix:** removed the inline modal BottomSheet and routed the finish-event confirmation through the app's safe reusable `ConfirmSheet` wrapper (driven by a `FinishConfirmSheetState` TwoWay property on `QueueManagementViewModel`). The "Finish Event" button now opens the confirmation (`RequestFinishEventCommand`); only the confirmed path transitions STARTED → FINISHED. This eliminates the re-parent-on-cache collision by construction **and** closes a spec gap — **AC-5.3** (a finish confirmation: "End event and archive queue?") was previously unwired, so finishing archived the queue with no prompt.

**Files changed:** `QueueManagementPage.xaml`, `QueueManagementPage.xaml.cs`, `QueueManagementViewModel.cs`, + new `MyVocaList.Tests/Unit/ViewModels/QueueManagementViewModelTests.cs` (AC-5.3 regression).

**Verification:** `dotnet build` 0 errors; new ViewModel test passes; `QueueRepositoryTests` 5/5 green in isolation. (Intermittent full-suite `QueueRepositoryTests` FK-constraint failures are the pre-existing flaky parallel-SQLite race — unrelated, tracked in BACKLOG.) ⏳ Helder: emulator E2E for AC-BUG011-1/2/3.

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
