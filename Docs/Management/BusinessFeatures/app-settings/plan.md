# Plan — App Settings

> Two gap fixes. The SettingsPage itself is already implemented. This plan covers the two known gaps identified in `design.md`.

---

## Phase 1 — Navigation Consolidation

- [ ] **Consolidate Preferences → Settings navigation** [SEQUENTIAL]
  - **Produces:** Updated `NavigationConfig.cs` (route mapped to `SettingsPage`); updated `AppShell.xaml` (template swapped, `xmlns:prefs` removed); deleted `PreferencesPage.xaml` + `PreferencesPage.xaml.cs`; updated `MauiProgram.cs` (removed `PreferencesPage` transient registration); updated `MyVocaList.sln` (deleted file entries removed)
  - **Consumes:** `design.md § Gap Fix 1 — Navigation Consolidation` (read before starting)
  - **Risk:** Medium — deleting a registered page and changing a shell route template. Build will fail if any reference to `PreferencesPage` or the `prefs:` namespace is left behind.
  - **Files owned:**
    - `MyVocaList/Navigation/NavigationConfig.cs`
    - `MyVocaList/AppShell.xaml`
    - `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml`
    - `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml.cs`
    - `MyVocaList/MauiProgram.cs`
    - `MyVocaList.sln`
  - **Demo:** Admin opens the flyout menu, taps "Preferences", and the Settings page opens (showing the YouTube Integration section). The "under construction" screen is no longer reachable.
  - **Review lane:** Standard

---

## Phase 2 — Stale HasYouTubeApiKey Fix

- [ ] **Refresh HasYouTubeApiKey on SongFormPage appearance** [SEQUENTIAL — after Phase 1 committed]
  - **Produces:** New `RefreshApiKeyFlagAsync()` method on `SongFormViewModel`; updated `SongFormPage.xaml.cs` `OnAppearing` calling `RefreshApiKeyFlagAsync`
  - **Consumes:** `design.md § Gap Fix 2 — Stale HasYouTubeApiKey` (read before starting)
  - **Risk:** Low — additive change; no existing behavior is altered. `OnAppearing` already calls `titleEdit.Focus()` which remains unchanged.
  - **Files owned:**
    - `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
    - `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`
  - **Demo:** Admin opens an existing song (no API key stored), navigates to Settings via the flyout, saves a valid API key, taps Back — the Song form reappears with the YouTube search strip now visible, without closing and reopening the song.
  - **Review lane:** Standard

---

## Verification

### Build
```bash
dotnet build MyVocaList.sln
```
Expected: 0 errors, 0 warnings about missing types or unresolved references.

### Tests
```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```
Expected: 0 failures. No new tests are required for these tasks (pure UI/wiring changes); existing tests must remain green.

### Manual smoke test — Phase 1
1. Launch the app on the Android emulator.
2. Open the flyout menu → tap "Preferences".
3. Confirm the Settings page opens (YouTube Integration section visible).
4. Confirm no "under construction" text appears anywhere.
5. Confirm the `PreferencesPage` files are absent from the solution.

### Manual smoke test — Phase 2
1. Open any song (API key not stored).
2. Confirm the YouTube search strip is hidden.
3. Navigate to Settings via the flyout → enter any non-empty string → tap Save.
4. Tap Back.
5. Confirm the YouTube search strip is now visible on the Song form — without closing and reopening the song.
