# BUG-011 — QueueManagementPage BottomSheet double-add on navigation

## Context

**Why this change:** BUG-011 reports a `DevExpress.Maui.Controls.BottomSheet is already a child of Microsoft.Maui.Controls.Grid` log entry plus a 4103 ms Davey (245 skipped frames) on every navigation to the Queue page after the first visit. The page is cached by Shell as a root view, so its visual tree is reattached on each visit and a modal BottomSheet declared inline in that tree collides on re-parent.

**Investigation correction (important — the bug report is misaimed):**
- The bug names `QueuePage`. That file is a **712-byte placeholder** (dead leftover) on every branch. The real, wired page is **`QueueManagementPage`** — a `ShellContent` root view (`AppShell.xaml:109`), registered in DI (`MauiProgram.cs:169/188`), backed by `QueueManagementViewModel` (17.8 KB) and `IQueueServiceNew`/`QueueServiceNew`. The feature **is fully present in `develop`** (38 queue files) and reachable.
- The structural cause is in `QueueManagementPage.xaml`: a single BottomSheet `x:Name="finishEventSheet"`, `IsModal="True"`, declared as a direct child of the page's root `<Grid>`. A DevExpress modal BottomSheet re-parents itself to a window-level overlay when shown; the original Grid still lists it as a child, so when Shell reattaches the cached page tree on the 2nd visit the sheet collides → the logged error + view-tree-mutation Davey.
- **Repro caveat:** as committed on `develop`, `finishEventSheet` is **never shown** — it has no `State` binding and no `Show(...)` call; the "Finish Event" button binds straight to `FinishEventCommand`, bypassing the sheet. So the captured trace likely came from an earlier build where Finish opened the sheet. The inline-modal-in-cached-page anti-pattern is the cause regardless, and the sheet is currently orphaned.
- **Spec gap revealed:** `requirements.md` AC-5.3 and `design.md` Flow 4 require a confirmation dialog ("End event and archive queue?") before finishing. That confirmation is **currently missing** — Finish archives the queue (irreversible, locks to read-only) with no prompt.

**Intended outcome:** Eliminate the re-parent collision by construction and, in the same move, restore the spec-required finish confirmation by routing it through the app's established safe sheet pattern.

## Approach (recommended)

Converge on the app's existing safe convention — the reusable `ConfirmSheet` ContentView wrapper (kept out of the page's declared tree, attached on demand with a parent guard, driven by a `SheetState`/`Close()` toggle) — instead of an inline XAML modal sheet inside a cached page.

1. **Remove** the inline `<dx:BottomSheet x:Name="finishEventSheet" …>` block (and its Cancel/Confirm buttons) from `QueueManagementPage.xaml`.
2. **Wire the finish confirmation through `ConfirmSheet`** (the existing wrapper used by other sheets), mirroring how current consumers use it. The implementor must first read `ConfirmSheet.xaml` / `ConfirmSheet.xaml.cs` and an existing consumer to copy the exact API (message text, confirm/cancel labels, the `SheetState` TwoWay BindableProperty, and the `??=` + `AttachSheetToCurrentPage` parent-guard host pattern from `AppShell.xaml.cs`). Reuse, do not reinvent.
3. **Repoint `FinishEventCommand`**: the "Finish Event" button opens the confirmation sheet; only on Confirm does the event transition STARTED → FINISHED (archive + read-only). Cancel/swipe-dismiss closes with no change. This satisfies AC-5.3 / Flow 4.
4. Remove the now-unused `OnCancelFinish` / `OnConfirmFinish` code-behind handlers in `QueueManagementPage.xaml.cs` if they are only the inline sheet's wiring.

**Why not a minimal guard instead:** keeping the inline sheet and guarding re-add in `OnAppearing` fights DevExpress internals and has no precedent in this codebase. The wrapper pattern removes the failure mode structurally and matches convention.

### Files to modify (on a branch off `develop`)
- `MyVocaList/UI/Pages/Queue/QueueManagementPage.xaml` — remove inline `finishEventSheet`; host `ConfirmSheet` per established pattern.
- `MyVocaList/UI/Pages/Queue/QueueManagementPage.xaml.cs` — drop orphaned handlers; ensure confirm flow is sheet-driven.
- `MyVocaList/UI/ViewModels/QueueManagementViewModel.cs` — add `SheetState` (or equivalent) bound property; split Finish into "open confirm" vs "confirmed finish".

### Reuse (do not recreate)
- `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml(.cs)` — the safe sheet wrapper (`SheetState` TwoWay, `Show(state, host)` / `Close()`).
- `MyVocaList/AppShell.xaml.cs` — `??=` instantiation + `AttachSheetToCurrentPage` parent guard (`if (sheet.Parent is not null) return;`).

## Out of scope
- `QueuePage` / `QueuePage.xaml.cs` placeholder removal (dead leftover) — track separately.
- Dead-code cleanup of `QueueService` / `IQueueService` (superseded by `*New`, unregistered) — track separately.
- Any other page that declares an inline modal BottomSheet (none confirmed affected; flag if found).

## Verification (end-to-end, on emulator)
The working tree is on `main` (no Queue files); implement on a branch off `develop`.
1. **Build:** `dotnet build` — 0 errors.
2. **Tests:** `dotnet test` — existing 26 queue tests still pass; add a ViewModel test asserting Finish opens the confirmation (no immediate FINISHED) and only the confirmed path transitions to FINISHED (AC-5.3 regression coverage).
3. **Repro/E2E (Android emulator), with logcat open:**
   - Navigate to the Queue (root Shell view) → away to another flyout item → back. Repeat twice.
   - **AC-BUG011-1:** no `BottomSheet is already a child` warning in logcat.
   - **AC-BUG011-2:** no `Choreographer skipped frames` / Davey burst on the 2nd+ visit.
   - **AC-BUG011-3 / AC-5.3:** tap "Finish Event" → confirmation sheet appears; Cancel leaves the event STARTED; Confirm transitions to FINISHED and locks the queue read-only. Sheet open/close repeats cleanly across navigations.

## Process notes
- Per workflow Rule 2, all edits are done by an implementor subagent; main agent runs build/test/git only.
- Bug-fix ceremony: commit message as spec (Bug Fix Pattern), subject includes `BUG-011`; add the AC-5.3 regression test (High severity → regression test mandatory per `bug-tracking.md`).
- Update BACKLOG.md BUG-011 row → resolved when shipped; note the QueuePage-vs-QueueManagementPage misattribution in the bug file.
