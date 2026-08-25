# Tasks — App Settings

> **Closed 2026-05-31.** Both phases were implemented and reviewed **PASS** (see `task-log.md`);
> the item shipped as `✅ Done`, closed 2026-06. The boxes below were left unchecked at the time —
> corrected 2026-08-25 during the ORPHAN-APPSET audit. `PreferencesPage/` is absent from the tree,
> confirming Phase 1 landed. **No work remains here.**

> Two gap fixes. The SettingsPage itself is already implemented and requires no code changes.
> Read `design.md` before implementing either task.

---

## Phase 1 — Navigation Consolidation

- [x] **Consolidate Preferences → Settings navigation** [SEQUENTIAL]
  - **Produces:** Updated `NavigationConfig.cs`; updated `AppShell.xaml`; deleted `PreferencesPage.xaml` + `PreferencesPage.xaml.cs`; updated `MauiProgram.cs`; updated `MyVocaList.sln`
  - **Consumes:** `design.md § Gap Fix 1 — Navigation Consolidation` (read before starting)
  - **Risk:** Medium — deleting a registered page and changing a shell route template. Build will fail if any reference to `PreferencesPage` or the `prefs:` namespace is left behind.
  - **Files owned:**
    - `MyVocaList/Navigation/NavigationConfig.cs`
    - `MyVocaList/AppShell.xaml`
    - `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml`
    - `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml.cs`
    - `MyVocaList/MauiProgram.cs`
    - `MyVocaList.sln`
  - **Demo:** Admin opens the flyout menu, taps "Preferences", and the Settings page opens (showing the YouTube Integration section). The PreferencesPage "under construction" screen is no longer reachable.
  - **Review lane:** Standard

---

## Phase 2 — Stale HasYouTubeApiKey Fix

- [x] **Refresh HasYouTubeApiKey on SongFormPage appearance** [SEQUENTIAL — after Phase 1 committed]
  - **Produces:** New `RefreshApiKeyFlagAsync()` method on `SongFormViewModel`; updated `SongFormPage.xaml.cs` `OnAppearing`
  - **Consumes:** `design.md § Gap Fix 2 — Stale HasYouTubeApiKey` (read before starting)
  - **Risk:** Low — additive change; no existing behavior is altered. `OnAppearing` already calls `titleEdit.Focus()` which remains unchanged.
  - **Files owned:**
    - `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
    - `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Demo:** Admin opens an existing song (no API key stored), navigates to Settings via the flyout, saves a valid API key, taps Back — the Song form reappears with the YouTube search strip now visible, without closing and reopening the song.
  - **Review lane:** Standard

---

## Subagent exit checklist reminder

Before marking any task done and committing:
1. Run `dotnet build` — 0 errors required.
2. Run `dotnet test` — 0 failures required (no new tests expected for these tasks; verify existing tests still pass).
3. Confirm `MyVocaList.sln` entries are correct for any added/removed files.
4. Confirm no reference to `PreferencesPage` or the `prefs:` XAML namespace survives after Phase 1.
