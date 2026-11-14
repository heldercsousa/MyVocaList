---
id: BUG-087
title: Song form and Artist form freeze the app on open (TapToEditField)
status: 🟡 In Progress
severity: Critical
target: 2026-11-14
parent: artists-songs
goal: Opening the Song form or the Artist form no longer hangs the main thread; the forms render and work.
kind: bug
---

# Song form and Artist form freeze the app on open (TapToEditField)

Opening the Song form or the Artist form no longer hangs the main thread; the forms render and work.

## Symptom (reported by Helder, 2026-11-14, release build from `e2768f83`)

Tapping the add button on the Songs page shows the Android "isn't responding" dialog; "Wait" closes the app. The Artist form uses the same component.

## Root cause

`TapToEditField` declares `[ContentProperty(nameof(Editor))]` so that a page can write the editor as the child element. The same attribute also applies to the component's own XAML: its root `Grid` was assigned to `Editor`, not to `Content`. `OnEditorChanged` then placed that `Grid` inside `editorHost`, which is a child of the same `Grid`. The visual tree had a cycle and the main thread looped.

## Fix

Commit `8937ae4b` (`fix/bug-087-form-freeze`, merged to `develop` in `5c062615`): `TapToEditField.xaml` sets its layout through an explicit `<ContentView.Content>` element. One file, four lines.

### Changed files
- `MyVocaList/UI/Components/TextFields/TapToEditField.xaml`

### Verification evidence
- Reproduced on the emulator (Pixel 5, API 36, signed Release APK of `e2768f83`): `ANR in com.myvocalist ... Input dispatching timed out` after the tap on the Songs add button; the app main thread was running managed code.
- After the fix (signed Release APK of `8937ae4b`, same steps): the Song form opens; the Artist form opens; no ANR line in logcat.
- Suite: 1277 passed, 0 failed (pre-commit gate).
- Regression test: none automated. The defect is in XAML loading of a view, which the plain `net10.0` test head cannot run (UI-only). Manual check: open the Song form and the Artist form from their lists.

## Why it was not caught

The feature was verified by build and unit tests only; no form was opened on a device or emulator before the release build was uploaded. The emulator `pixel_5_-_api_36_0_1` is available for this check.

## Related findings from the same report (not defects of this bug)
- **Update refused by Android:** the release APK is signed with the release key; a build deployed from Visual Studio is signed with the debug key. Android refuses an update between different signatures (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`), so the app must be uninstalled once when changing from a debug build to a release build.
- **No venues after a fresh install:** the 500-venue seed (`20260211055225_venuesSeedForTest`) is compiled only in Debug (`#if DEBUG`), so a Release build has no seeded venues.
- **Slow first screen:** the Release build took 7.5 to 10 seconds to show the first page on the emulator, with skipped frames; not investigated here.
- **Two menu icons** on the Venues app bar in the Release build (seen in the emulator screenshot); not investigated here.
