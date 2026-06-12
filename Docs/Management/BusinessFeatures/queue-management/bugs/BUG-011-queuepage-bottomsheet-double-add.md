# BUG-011 — QueuePage BottomSheet double-add on navigation

**Filed:** 2026-06-11
**Feature area:** Queue Management
**Severity:** High — causes 4103ms UI freeze (245 skipped frames / Davey!) on every navigation to QueuePage after first visit
**Status:** 💡 Pending

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
