# BUG-016 — SongsPage: App Crashes When FAB (Add) Tapped

**Severity:** Critical — app crash; user cannot create a new song  
**Discovered:** 2026-06-24 — emulator smoke test  
**Reporter:** Helder  
**Status:** Open

---

## Symptom

Tapping the FAB button (Add) on `SongsPage` crashes the app. The crash occurs on navigation, before `SongFormPage` becomes visible.

## Expected

Tapping the FAB navigates to `SongFormPage` in add-mode.

## Root Cause — Route Collision: `"song-picker"`

Two registrations exist for the route `"song-picker"` pointing to different pages:

1. **`AppShell.xaml`** — declares a `FlyoutItem` with `Route="song-picker"` pointing to `QueueSongPickerPage`. When MAUI processes Shell XAML, this route is registered in the Shell global route table.

2. **`AppShell.xaml.cs`** — calls `Routing.RegisterRoute(Routes.SongPicker, typeof(SongPickerPage))` where `Routes.SongPicker = "song-picker"`. This attempts to register `SongPickerPage` under the same route name.

MAUI Shell distinguishes "global routes" (Shell items) from "non-global routes" (Routing.RegisterRoute), but the collision causes undefined behavior or an `ArgumentException` during navigation. The exact crash point depends on when MAUI detects the ambiguity — it may throw when the Shell initializes or when `GoToAsync("song-form")` triggers the Shell route table resolution.

**Contributing factor:** commit `b73281f` (2026-06-23) added `QueueSongPickerPage` as a `FlyoutItem` in `AppShell.xaml` with `Route="song-picker"`, while the programmatic registration of `SongPickerPage` under the same route already existed in `AppShell.xaml.cs`. The routes served different purposes and should never have shared a name.

**Files to inspect:**
- `MyVocaList/UI/AppShell.xaml` — FlyoutItem Route="song-picker"
- `MyVocaList/UI/AppShell.xaml.cs` — Routing.RegisterRoute("song-picker", ...)
- `MyVocaList/Navigation/Routes.cs` — route constant definitions

## Fix Direction

Rename one of the two `"song-picker"` route registrations so they no longer collide:

- `QueueSongPickerPage` (the queue-flow picker): rename to `"queue-song-picker"` in AppShell.xaml and add a corresponding constant to `Routes.cs`.
- `SongPickerPage` (the music-DB artist/song search picker): keep as `"song-picker"` via `Routing.RegisterRoute`.

Update all `GoToAsync` call sites for `QueueSongPickerPage` to use the new route name. Confirm the `SongFormPage` FAB navigation resolves cleanly after the fix.

## Regression Test (Critical — mandatory before close)

Write a failing unit test that confirms `SongsViewModel.AddSongCommand` calls navigation with route `Routes.SongForm`, confirm Red before fix. After fix, confirm Green. Additionally run the emulator smoke test: tap FAB on SongsPage → SongFormPage loads → fill title + artist → Save → song appears in list.
