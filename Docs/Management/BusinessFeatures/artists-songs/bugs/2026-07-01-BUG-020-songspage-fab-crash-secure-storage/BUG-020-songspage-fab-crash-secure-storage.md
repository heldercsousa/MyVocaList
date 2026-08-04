# BUG-020 — SongsPage: App Crashes When FAB (Add) Tapped (post BUG-016/BUG-017 fix)

**Severity:** Critical — app crash; user cannot create a new song
**Discovered:** 2026-07-01 — Helder, manual smoke test
**Reporter:** Helder
**Status:** Fixed — emulator-verified 2026-07-03 (TEST-010, `Docs/Management/EMULATOR_TEST_MASTER_LIST.md`): FAB opens SongFormPage, no SecureStorage exception, no crash.

---

## Symptom

Tapping the FAB button (Add) on `SongsPage` still crashes the app, even though BUG-016
(route collision on `"song-picker"`) and BUG-017 (missing `navigate_next` icon) were both
fixed and confirmed merged into `develop`. Navigation to `SongFormPage` is reached (unlike
BUG-016, where the crash happened before the page loaded), but the app terminates shortly
after.

## Expected

Tapping the FAB navigates to `SongFormPage` in add-mode and the page renders normally.

## Investigation (systematic-debugging)

Root causes ruled out first (confirmed absent in current `develop`):
- **Route collision** (BUG-016 class) — checked `Routes.cs`, `AppShell.xaml`,
  `AppShell.xaml.cs`: `"song-form"` is registered exactly once via
  `Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage))`, no `FlyoutItem` or other
  `Routing.RegisterRoute` call shares that string. No collision.
- **Missing icon SVG** (BUG-017 class) — checked every icon referenced by `SongsPage.xaml`
  and `SongFormPage.xaml` (`music_note_outlined`, `language_outlined`, `search_outlined`,
  `arrow_forward_outlined`, `add_outlined`) against `MyVocaList/Resources/Images/*.svg` — all
  present.
- **DI registration** — `SongFormPage` and `SongFormViewModel` are both registered
  `AddTransient` in `MauiProgram.cs`; every constructor dependency
  (`IArtistService`, `ISongService`, `ISongResolutionService`, `ISnackbarComponent`,
  `ILogger<SongFormViewModel>`, `ISongKaraokeUrlService`, `ISecureStorageWrapper`,
  `IMessenger`) is registered. No unresolvable dependency.
- **EF Core concurrent-tracking issue** (BUG-018 class) — `SongFormViewModel`'s "new song"
  path (`ExecuteNewSongSaveAsync` / `CommitNewSongAsync`) does not touch `AppDbContext`
  directly and only runs after Save is tapped, not on FAB tap / page appearance — ruled out
  as the FAB-tap-time crash cause.

**Pattern comparison (Phase 2):** every sibling "Add" flow (`ArtistFormPage`,
`VenueFormPage`, `PersonFormPage`) declares:
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    nameEdit.Focus();
}
```
a plain **synchronous `void`** override with no I/O. `SongFormPage.xaml.cs` is the only one
of the four Form pages declaring:
```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    titleEdit.Focus();
    if (BindingContext is SongFormViewModel vm)
    {
        await vm.RefreshApiKeyFlagAsync();
        vm.InitializeArtistField();
    }
}
```
an **`async void`** override that awaits `RefreshApiKeyFlagAsync()`, which calls
`ISecureStorageWrapper.GetAsync("youtube_api_key")` → `SecureStorage.GetAsync` (MAUI
platform API), with no try-catch anywhere in the call chain.

## Root Cause

`SecureStorage.GetAsync` can throw on Android (a well-documented MAUI/Xamarin.Essentials
issue — most commonly a corrupted or inaccessible Android Keystore alias, e.g. after an APK
reinstall, app-data reset, or signing-key change without clearing storage). Because
`SongFormPage.OnAppearing` is declared `async void`, any exception thrown inside the awaited
`RefreshApiKeyFlagAsync()` call is not caught by any `try/catch` in the call chain — it
escapes the method's `SynchronizationContext.Post` continuation and reaches
`AppDomain.CurrentDomain.UnhandledException`, which per `GlobalExceptionHandler` is logged as
**Fatal — app may terminate**.

This is unique to the Songs "Add" flow because `SongFormPage` is the only Form page whose
`OnAppearing` performs I/O (a SecureStorage read for the YouTube API key flag) inside an
`async void` handler; the other three Form pages call only synchronous, non-throwing
`Focus()` in `OnAppearing`.

## Fix

Wrap the `SecureStorage.GetAsync` call inside `SongFormViewModel.RefreshApiKeyFlagAsync()` in
a try-catch (per `code-principles.md § Exception Handling` — "Error recovery with logging,
not swallowed"). On failure, log the exception and treat it the same as "no API key
configured" (`HasYouTubeApiKey = false`) instead of letting the exception propagate through
the `async void` `OnAppearing` call chain.

```csharp
public async Task RefreshApiKeyFlagAsync()
{
    try
    {
        var key = await _secureStorage.GetAsync("youtube_api_key");
        HasYouTubeApiKey = !string.IsNullOrWhiteSpace(key);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to read YouTube API key from secure storage");
        HasYouTubeApiKey = false;
    }
}
```

**Files changed:**
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `RefreshApiKeyFlagAsync` wrapped in try-catch
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — regression test added

**Regression risk:** None — the fix only adds exception handling around an existing call;
the success path (`HasYouTubeApiKey` set from a non-throwing `GetAsync`) is unchanged.

## Regression Test (Critical — mandatory before close)

`SongFormViewModelTests.RefreshApiKeyFlagAsync_SecureStorageThrows_DoesNotThrowAndSetsFalse`
— mocks `ISecureStorageWrapper.GetAsync` to throw `InvalidOperationException`, asserts
`RefreshApiKeyFlagAsync()` does not throw and `HasYouTubeApiKey` is `false` afterward.
Confirmed **Red** (test failed — the un-caught exception propagated) before the fix, and
**Green** (all 361 tests pass) after.

**Not yet verified:** emulator smoke test (tap FAB → SongFormPage renders → fill title +
artist → Save → song appears in list) — no emulator was available in this session. This is a
static-analysis-derived fix; Helder should confirm on-device before considering the bug fully
closed, and if the crash persists, capture the `adb logcat` stack trace for the actual
exception type/stack (this fix targets the most probable cause given the code pattern, but
without a device stack trace the exact platform exception type could not be confirmed).
