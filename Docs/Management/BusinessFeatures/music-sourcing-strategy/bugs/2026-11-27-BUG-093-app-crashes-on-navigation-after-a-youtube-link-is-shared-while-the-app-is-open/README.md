---
id: BUG-093
title: App crashes on navigation after a YouTube link is shared while the app is open
status: 💡 Pending
severity: Critical
target: 2026-11-27
parent: music-sourcing-strategy
goal: Sharing a link reuses the running app instance; navigating afterwards never crashes
kind: bug
---

# App crashes on navigation after a YouTube link is shared while the app is open

Sharing a link reuses the running app instance; navigating afterwards never crashes


## Evidence (Helder's phone, 2026-11-27 00:53 and 00:55, logcat -b crash)
`System.ArgumentNullException: Value cannot be null. (Parameter 'key')` at `ShellItemRendererBase.HandleFragmentUpdate` ← `ShellSection.OnPushAsync` ← `GoToAsync`, from an `AsyncRelayCommand`. Steps: the Songs list was open, a YouTube link was shared to the app, the snackbar showed, and the first navigation crashed. The app relaunched on the Venues list.

## Root cause (hypothesis, to be confirmed by the fix)
`MainActivity` uses `LaunchMode.SingleTop`. An ACTION_SEND from YouTube starts a second `MainActivity` instance in YouTube's task, so a second MAUI window/Shell renderer exists. Navigation then runs against a Shell whose fragment map has no entry for the section.

## Regression test
UI/platform only. A manual E2E is recorded in the task-log: share from YouTube with the Songs list open, then navigate to every flyout item.
