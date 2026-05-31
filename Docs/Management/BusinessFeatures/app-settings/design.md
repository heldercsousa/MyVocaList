# Design — App Settings

> Retroactive spec written 2026-05-30. Documents the as-built architecture and the two gap fixes required.

---

## Architecture

**Layers affected: MAUI UI only.**

- `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` + `.xaml.cs` — the settings surface (already built)
- `MyVocaList/UI/ViewModels/SettingsViewModel.cs` — ViewModel (already built)
- `MyVocaList/Navigation/NavigationConfig.cs` — routes `Routes.Preferences` to `SettingsPage` (fix required)
- `MyVocaList/AppShell.xaml` — `preferences` FlyoutItem target changes from `PreferencesPage` to `SettingsPage` (fix required)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — `OnAppearing` must refresh `HasYouTubeApiKey` (fix required)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — may need a new `RefreshApiKeyFlagAsync()` method (fix required)
- `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml` + `.xaml.cs` — deleted (fix required)

No Domain, Infra, or Services changes. No new EF Core migrations. No new interfaces.

---

## Page Structure

### SettingsPage

Single scrollable page with one section:

```
ScrollView
  VerticalStackLayout (Padding=24, Spacing=24)
    Label "YouTube Integration"           // Title.Medium style, section header
    VerticalStackLayout (Spacing=12)
      dxe:TextEdit                        // API key input; IsPassword bound to IsApiKeyMasked
      HorizontalStackLayout (Spacing=8)
        DXButton "Show"/"Hide"            // OutlinedButton; ToggleMaskCommand
        DXButton "Test"                   // OutlinedButton; TestApiKeyCommand; disabled while IsTestingKey
        DXButton "Save"                   // FilledButton; SaveApiKeyCommand
        DXButton "Clear"                  // TextButton; ClearApiKeyCommand
      ActivityIndicator                   // Visible+Running when IsTestingKey=true
      Label (ApiKeyStatus)                // Body.Small; visible when HasApiKeyStatus=true
    Label (helper text)                   // Body.Small, Opacity=0.6; quota and fallback hint
```

---

## Interaction Flows

### Flow 1 — View / Update Key

1. Admin navigates to Settings (via flyout "Preferences" or the nudge link in SongFormPage).
2. `SettingsPage.OnAppearing` calls `vm.InitializeAsync()`.
3. `InitializeAsync` reads `"youtube_api_key"` from secure storage and populates `ApiKeyInput`.
4. Key is displayed masked by default (`IsApiKeyMasked = true`).
5. Admin optionally taps Show to unmask, edits the key, taps Save.
6. Save trims the key; if non-empty → writes to secure storage → snackbar "API key saved".
7. If empty → removes from secure storage → snackbar "API key removed".

### Flow 2 — Test Key

1. Admin types/pastes a key in the input.
2. Admin taps Test.
3. `IsTestingKey = true` → activity indicator appears, Test button is disabled.
4. `IYouTubeSearchService.ValidateApiKeyAsync(key)` is called.
5. On success (valid=true): `ApiKeyStatus = "Key valid — YouTube search is ready."`, `HasApiKeyStatus = true`.
6. On success (valid=false): `ApiKeyStatus = "Invalid key — check and retry."`, `HasApiKeyStatus = true`.
7. On exception: `ApiKeyStatus = "Test failed. Check your connection."`, `HasApiKeyStatus = true`.
8. `IsTestingKey = false` → activity indicator hidden, Test button re-enabled.

### Flow 3 — Clear Key

1. Admin taps Clear.
2. `ISecureStorageWrapper.Remove("youtube_api_key")` is called.
3. `ApiKeyInput = ""`, `ApiKeyStatus = ""`, `HasApiKeyStatus = false`.
4. Snackbar "API key removed".

---

## Gap Fix 1 — Navigation Consolidation

### Problem
`NavigationConfig.PageTypes[Routes.Preferences]` currently maps to `PreferencesPage` (a stub). The flyout "Preferences" item therefore opens the stub, not Settings.

### Solution

**Step 1 — Update `NavigationConfig.cs`:**
```csharp
// Before
[Routes.Preferences] = typeof(PreferencesPage),

// After
[Routes.Preferences] = typeof(SettingsPage),
```

**Step 2 — Update `AppShell.xaml`:**

The `preferences` FlyoutItem currently uses `DataTemplate prefs:PreferencesPage`. Change it to use `settings:SettingsPage`:

```xml
<!-- Before -->
<FlyoutItem Route="preferences" FlyoutItemIsVisible="False">
    <ShellContent ContentTemplate="{DataTemplate prefs:PreferencesPage}" />
</FlyoutItem>

<!-- After -->
<FlyoutItem Route="preferences" FlyoutItemIsVisible="False">
    <ShellContent ContentTemplate="{DataTemplate settings:SettingsPage}" />
</FlyoutItem>
```

Remove the `xmlns:prefs` namespace declaration from the `Shell` element once `PreferencesPage` is deleted.

**Step 3 — Delete `PreferencesPage` files:**
- `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml`
- `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml.cs`
- Remove the `Preferences/` folder if empty after deletion.
- Remove `PreferencesPage` registration from `MauiProgram.cs` (Transient page DI entry).
- Update `MyVocaList.sln` to remove the deleted file entries.

> Note: `Routes.Preferences` and the `preferences` shell route are retained. The shell route `"preferences"` is what `AppShellViewModel.NavigateAsync` uses — no route string changes are needed.

---

## Gap Fix 2 — Stale HasYouTubeApiKey

### Problem
`SongFormViewModel.HasYouTubeApiKey` is set only inside `LoadKaraokeUrlsAsync()`, which is called only when `SongId` changes (i.e., when a song is loaded). `SongFormPage.OnAppearing` does not call `InitializeAsync` or any equivalent — it only calls `titleEdit.Focus()`. Therefore, if the admin adds a key in Settings and returns to the Song form, `HasYouTubeApiKey` remains false and the search strip stays hidden.

### Solution

Add a method to `SongFormViewModel` that refreshes the `HasYouTubeApiKey` flag without reloading the full karaoke URL list:

```csharp
public async Task RefreshApiKeyFlagAsync()
{
    var apiKey = await _secureStorage.GetAsync("youtube_api_key");
    HasYouTubeApiKey = !string.IsNullOrWhiteSpace(apiKey);
}
```

Call it from `SongFormPage.OnAppearing`:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    titleEdit.Focus();
    if (BindingContext is SongFormViewModel vm)
        await vm.RefreshApiKeyFlagAsync();
}
```

This reads one secure storage entry on each page appearance. The call is fast (< 50 ms on device) and does not re-query the database or the YouTube API.

---

## Interface Signatures

### New method on `SongFormViewModel`
```csharp
/// <summary>Re-reads secure storage to refresh the HasYouTubeApiKey flag.</summary>
/// <remarks>Called from OnAppearing to pick up keys saved in Settings since the song was loaded.</remarks>
public async Task RefreshApiKeyFlagAsync()
```

No interface changes required. `SettingsViewModel` and `IYouTubeSearchService` are unchanged.

---

## Invariants and Postconditions

- After Save with a non-empty key: `SecureStorage["youtube_api_key"]` equals the trimmed key.
- After Save with an empty key, and after Clear: `SecureStorage["youtube_api_key"]` does not exist.
- After Settings page appears: `ApiKeyInput` equals the value in `SecureStorage["youtube_api_key"]`, or empty if not set.
- After RefreshApiKeyFlagAsync: `SongFormViewModel.HasYouTubeApiKey` equals `!string.IsNullOrWhiteSpace(SecureStorage["youtube_api_key"])`.
- `IsApiKeyMasked` defaults to `true` on every navigation to Settings (not persisted across sessions).

---

## Key Decisions

### Decision: Reuse `preferences` shell route for SettingsPage
**Chosen approach:** Keep the `preferences` shell route and the `Routes.Preferences` constant. Change only the page type registered under that route.
**Alternatives considered:** Add a new `settings` shell route as the canonical target and redirect `preferences` to it. Rejected — the `settings` shell route already exists in `AppShell.xaml` but is unreachable from the flyout (it is only linked from the SongFormPage nudge). Changing `NavigationConfig` to map `Routes.Preferences → SettingsPage` is the minimal, zero-route-churn fix.
**Reversibility:** Easily reversible — change one line in `NavigationConfig.cs`.
**Rationale:** Minimises diff. The flyout item label "Preferences" remains unchanged; only the destination changes.

### Decision: Add RefreshApiKeyFlagAsync to SongFormViewModel
**Chosen approach:** Lightweight method reads only the secure storage key. Called from `OnAppearing`.
**Alternatives considered:** (a) WeakReferenceMessenger broadcast from SettingsViewModel on save — more complex, introduces cross-ViewModel coupling. (b) Reload the full `LoadKaraokeUrlsAsync` on every appearance — re-queries the DB and the karaoke URL list unnecessarily. (c) Shared reactive state (e.g., `IYouTubeApiKeyProvider`) — over-engineered for a single flag.
**Reversibility:** Easily reversible.
**Rationale:** Minimal code change; no new dependencies; reads at most one secure storage entry per page appearance.

### Decision: Delete PreferencesPage stub
**Chosen approach:** Remove both `.xaml` and `.xaml.cs` files and all registrations.
**Alternatives considered:** Keep the stub as a placeholder for future preferences. Rejected — an empty stub reachable from no navigation path adds confusion and dead code.
**Reversibility:** Easily reversible — recreate the page and re-register.
**Rationale:** Dead code. The stub has no functionality and was never the intended final destination.

---

## Security Notes

- The API key is stored via `ISecureStorageWrapper` which wraps `SecureStorage.SetAsync` / `GetAsync` / `Remove`.
- On Android, this uses the Android Keystore. On iOS, it uses the iOS Keychain.
- The key is never written to SQLite, `Preferences`, or any log output.
- `SettingsViewModel` logs errors via `ILogger<SettingsViewModel>` but never logs the key value itself.
