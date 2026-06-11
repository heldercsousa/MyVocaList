# BUG-006 — Double-tap on Search Song link crashes app

**Feature:** Artists & Songs
**Severity:** High — reproducible crash on any device
**Status:** Spec written — ready for implementation
**Reported:** 2026-06-11

---

## Symptom

In `SongFormPage`, tapping the "Search music database" list item twice in quick succession before `SongPickerPage` finishes loading causes the app to crash.

---

## Root Cause

`NavigateToSongPickerCommand` is an `AsyncRelayCommand` constructed with no `CanExecute` guard and no `AllowConcurrentExecutions` = false override:

```csharp
// SongFormViewModel.cs — constructor, line 85
NavigateToSongPickerCommand = new AsyncRelayCommand(NavigateToSongPickerAsync);
```

`AsyncRelayCommand` from CommunityToolkit.Mvvm defaults to allowing concurrent executions. Two rapid taps each call `NavigateToSongPickerAsync` independently before the first navigation settles. Each invocation calls `_messenger.Register<SongPickedMessage>(this, ...)` and then `Shell.Current.GoToAsync(Routes.SongPicker)`.

Two concrete failure modes follow from this:

1. **Duplicate messenger registration crash:** The second tap calls `_messenger.Register<SongPickedMessage>(this, ...)` while the first registration is still active on the same token (`this`). `WeakReferenceMessenger` throws an `InvalidOperationException` ("A recipient has already been registered for the SongPickedMessage message") on the second registration attempt.

2. **Shell navigation stack corruption (secondary):** Even if the messenger exception is swallowed, two concurrent `GoToAsync(Routes.SongPicker)` calls push two copies of `SongPickerPage` onto the navigation stack. Back-navigation from the picker then lands on a second picker instance rather than `SongFormPage`, disorienting the user or triggering a further exception when the orphaned page attempts to send a message.

The same vulnerability exists on `NavigateToYouTubeSearchCommand` (line 86) — it is constructed identically with the same messenger-register pattern.

---

## Crash Type

`InvalidOperationException` from `IMessenger.Register` (WeakReferenceMessenger) thrown on the UI thread during the second rapid navigation invocation. Results in an unhandled exception caught by `GlobalExceptionHandler`, which logs Fatal and may terminate the app depending on Android/iOS behaviour.

---

## Affected Code

| File | Line(s) | Issue |
|------|---------|-------|
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | 85 | `NavigateToSongPickerCommand` constructed with no re-entry guard |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | 86 | `NavigateToYouTubeSearchCommand` — same pattern, same risk |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | 250–261 | `NavigateToSongPickerAsync` — registers messenger then navigates; no IsExecuting check |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | 264–274 | `NavigateToYouTubeSearchAsync` — same pattern |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | 70 | `TapGestureRecognizer` bound to `NavigateToSongPickerCommand` — no visual disabled state |

---

## Out of Scope

- Changing the navigation architecture (Shell vs modal)
- Modifying `SongPickerPage` or `SongPickerViewModel`
- Fixing any other commands in `SongFormViewModel` beyond the two navigation commands

---

## Fix Approach

Minimal — two changes, no architectural modification:

### Option A — `AllowConcurrentExecutions = false` (recommended)

Pass `false` as the `allowConcurrentExecutions` parameter when constructing both commands. `AsyncRelayCommand` will automatically set `IsRunning = true` during execution and suppress re-entry through its built-in `CanExecute` mechanism:

```csharp
// Before
NavigateToSongPickerCommand    = new AsyncRelayCommand(NavigateToSongPickerAsync);
NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync);

// After
NavigateToSongPickerCommand    = new AsyncRelayCommand(NavigateToSongPickerAsync,
    allowConcurrentExecutions: false);
NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync,
    allowConcurrentExecutions: false);
```

No changes to the XAML or to the async methods are required. `AsyncRelayCommand.IsRunning` becomes `true` from first tap until `GoToAsync` returns; the second tap's `CanExecute` returns `false` and the tap is silently ignored.

### Option B — `IsNavigating` bool flag (more explicit, more code)

Add `[ObservableProperty] private bool _isNavigating;` and gate both methods:

```csharp
private async Task NavigateToSongPickerAsync()
{
    if (_isNavigating) return;
    _isNavigating = true;
    try { ... }
    finally { _isNavigating = false; }
}
```

Option A is preferred — it is idiomatic CommunityToolkit.Mvvm, requires fewer lines, and the disabled state propagates automatically to any bound UI element.

---

## Acceptance Criteria

### AC-BUG006-01 — Single tap navigates normally

**Given** `SongFormPage` is displayed
**When** the user taps "Search music database" once
**Then** `SongPickerPage` opens and the navigation stack contains exactly one `SongPickerPage` instance

### AC-BUG006-02 — Double-tap does not crash

**Given** `SongFormPage` is displayed
**When** the user taps "Search music database" twice within 300 ms
**Then** the app does not crash, exactly one `SongPickerPage` is pushed, and the second tap is silently discarded

### AC-BUG006-03 — Back navigation returns to SongFormPage

**Given** `SongPickerPage` was opened by any number of rapid taps
**When** the user presses Back
**Then** the app returns to `SongFormPage` (not to a second picker instance or an empty page)

### AC-BUG006-04 — NavigateToYouTubeSearchCommand has the same fix

**Given** `SongFormPage` is displayed with an API key configured
**When** the user taps "Search YouTube (API)" twice within 300 ms
**Then** the app does not crash and exactly one `YouTubeSearchPage` is pushed

---

## Implementation Notes

- No migration, no repository change, no new file — single ViewModel file edit.
- Verify with `dotnet build` only; no automated test can cover Shell navigation in unit tests (see `testing.md § Anti-Patterns`).
- Manual E2E verification on Android emulator: tap the list item with two rapid fingers (or use a tap macro) and confirm the app stays alive with one page pushed.
- After the fix, confirm `NavigateToSongPickerCommand.IsRunning` is correctly exposed if the XAML needs to show a loading indicator — it is available at no extra cost via `AsyncRelayCommand.IsRunning`.
