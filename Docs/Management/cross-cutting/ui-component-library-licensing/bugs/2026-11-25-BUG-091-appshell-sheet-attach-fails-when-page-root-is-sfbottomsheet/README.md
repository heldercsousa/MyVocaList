---
id: BUG-091
title: AppShell sheet attach fails when page root is SfBottomSheet
status: ✅ Done
severity: Major
target: 2026-11-25
parent: ui-component-library-licensing
goal: WhatsNew/UpdateAvailable/UpdateRequired sheets never show because AttachSheetToCurrentPage requires a Layout root
closed: 2026-11
kind: bug
---

# AppShell sheet attach fails when page root is SfBottomSheet

WhatsNew/UpdateAvailable/UpdateRequired sheets never show because AttachSheetToCurrentPage requires a Layout root

## Root cause
`AppShell.AttachSheetToCurrentPage` attached only when `CurrentPage.Content is Layout`. After the Venues cut-over the page root is an `SfBottomSheet`, so the attach silently did nothing and the app-level sheets (including the blocking UpdateRequired gate) never showed.

## Fix
View-only, `MyVocaList/AppShell.xaml.cs`: when the page root is not a Layout it is wrapped in a Grid and the sheet overlays it; a sheet attached to a previous page is detached first; an `ILogger<AppShell>` warning is logged when the page has no content.

## Manual E2E evidence (Android emulator-5554, 2026-11-25, temp trigger hack reverted, uncommitted)
- Migration: no CHECK error in logcat; `__EFMigrationsHistory` latest = PersonIdentityUniqueIndexRestore.
- WhatsNew shows over Venues; "Got it" closes it.
- UpdateAvailable shows; "Update Now" opens the browser (returning to the app finds the sheet closed); "Later" closes it.
- UpdateRequired shows; hardware Back and a downward swipe do not dismiss it.
- Person null round-trip ("ZZ Test"): March/2 saved and reopened with values; cleared both with X, saved, reopened: both empty (DB row has NULL birthday); test Person deleted.
- Feedback: counter goes "5 / 1000" then "10 / 1000" while typing; Send never pressed.

## Observations (not fixed)
- WhatsNew title "What's New in 9.9.9" rendered dark on dark in the screenshot (low contrast); may be mid-animation, needs a look.
- Singers list still showed "2 Mar" for the test Person after Cancel from the form (stale list, DB was correct).
