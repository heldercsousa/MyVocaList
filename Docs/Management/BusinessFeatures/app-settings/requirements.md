# Requirements — App Settings

> Retroactive spec written 2026-05-30. The Settings page was implemented during the YouTube Karaoke URLs feature without a prior SDD cycle. This spec captures the delivered behavior and two known gaps that require fixes.

---

## User Stories

**US-01 — View stored API key**
As an admin, I want to open the Settings page and see whether a YouTube API key is already stored, so that I know whether YouTube search is available without re-entering the key.

**US-02 — Enter or paste an API key**
As an admin, I want to type or paste a YouTube Data API v3 key into the Settings page, so that YouTube track search becomes available in the Song form.

**US-03 — Mask and unmask the key**
As an admin, I want to toggle between showing and hiding the API key text, so that I can verify what I typed without leaving the key visible on screen.

**US-04 — Test the API key**
As an admin, I want to test the stored key against the YouTube API before saving, so that I know it is valid before relying on it.

**US-05 — Save the API key**
As an admin, I want to save the API key to secure storage, so that it persists across app restarts.

**US-06 — Clear the API key**
As an admin, I want to remove the stored API key, so that YouTube search is disabled and the key is no longer accessible on this device.

**US-07 — Reach Settings from the flyout menu**
As an admin, I want the "Preferences" flyout menu item to open the Settings page, so that I can access API key configuration without having to open a song form first.

**US-08 — Search strip reflects saved key immediately**
As an admin, after saving an API key in Settings and returning to an open Song form, I want the YouTube search strip to appear immediately, so that I do not have to close and reopen the song to use the new key.

---

## Acceptance Criteria

### AC-SETTINGS-01 — Stored key pre-fills the input on open
```
Given a YouTube API key has been previously saved to secure storage
When the Settings page appears (initial open or navigation return)
Then the API key input field displays the stored key value
And the key is masked (password characters) by default
```

### AC-SETTINGS-02 — Empty input when no key is stored
```
Given no YouTube API key is in secure storage
When the Settings page appears
Then the API key input field is empty
```

### AC-SETTINGS-03 — Toggle reveals the key
```
Given the key input contains text and is currently masked
When the admin taps Show
Then the key is displayed as plain text
And the button label changes to Hide
```

### AC-SETTINGS-04 — Toggle re-masks the key
```
Given the key input is currently unmasked
When the admin taps Hide
Then the key is masked again
And the button label changes to Show
```

### AC-SETTINGS-05 — Test with valid key shows success
```
Given the API key input contains a non-empty string
When the admin taps Test
Then an activity indicator is shown while the test is in progress
And when the test completes with a valid key, the status label reads "Key valid — YouTube search is ready."
And the activity indicator is hidden
```

### AC-SETTINGS-06 — Test with invalid key shows failure
```
Given the API key input contains a non-empty string
When the admin taps Test and the API returns an invalid-key response
Then the status label reads "Invalid key — check and retry."
And the activity indicator is hidden
```

### AC-SETTINGS-07 — Test with network error shows connection message
```
Given the API key input contains a non-empty string
When the admin taps Test and no network response is received (exception thrown)
Then the status label reads "Test failed. Check your connection."
And the activity indicator is hidden
```

### AC-SETTINGS-08 — Test button disabled while a test is running
```
WHILE a test is in progress (IsTestingKey = true),
the system SHALL disable the Test button so it cannot be tapped again.
```

### AC-SETTINGS-09 — Save persists the trimmed key
```
Given the API key input contains a non-empty value (possibly with leading/trailing spaces)
When the admin taps Save
Then the trimmed key is written to secure storage under the key "youtube_api_key"
And a success snackbar "API key saved" is shown
```

### AC-SETTINGS-10 — Save with empty input clears storage
```
Given the API key input is empty (or whitespace only)
When the admin taps Save
Then secure storage entry "youtube_api_key" is removed
And a snackbar "API key removed" is shown
```

### AC-SETTINGS-11 — Clear removes key and resets UI
```
Given any state of the API key input
When the admin taps Clear
Then secure storage entry "youtube_api_key" is removed
And the API key input is cleared
And the status label is hidden
And a snackbar "API key removed" is shown
```

### AC-SETTINGS-12 — Flyout "Preferences" item navigates to Settings
```
Given the admin is on any page with the flyout menu available
When the admin opens the flyout and taps "Preferences"
Then the Settings page opens via PushAsync
And the flyout closes
```

### AC-SETTINGS-13 — YouTube search strip appears after key saved on return
```
Given the Song form page is open and HasYouTubeApiKey was false (no key stored)
When the admin navigates to Settings, saves a valid API key, and returns to the Song form
Then the YouTube search strip is visible without closing and reopening the song
```

---

## Validation Rules

- **No client-side format validation** for the API key text. The API key field accepts any non-empty string. Validity is determined only by the Test operation (live call to YouTube Data API v3).
- **Trimming**: Leading and trailing whitespace is stripped before Save and before Test.
- **Empty key on Save**: treated as an implicit Clear (removes the stored entry).

---

## Out of Scope

- General app preferences (theme, language, font size, notification settings).
- Cloud sync or backup of the API key.
- Multiple API keys or key rotation.
- Key expiry tracking or quota monitoring.
- Any API key other than YouTube Data API v3.
- In-app account management or sign-in flows.
- `PreferencesPage` as a multi-section preferences hub — the stub is deleted; Settings is the sole system-settings surface for now.

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **YouTube Data API v3 key** | A Google-issued API key granting quota for YouTube Data API v3 requests. Required for in-app YouTube track search. |
| **Secure storage** | Platform-native encrypted key-value store: Android Keystore on Android, iOS Keychain on iOS. Accessed via `ISecureStorageWrapper`. |
| **API key status** | The UI string shown after a Test operation indicating valid, invalid, or connection-error outcome. Cleared when the user clears the key. |
| **IsApiKeyMasked** | ViewModel boolean that controls whether the key input renders as password characters. Toggled by Show/Hide button. Default: true. |
| **HasYouTubeApiKey** | ViewModel boolean on `SongFormViewModel` that controls visibility of the YouTube search strip in the Song form. Set by reading secure storage. |
| **Preferences flyout item** | The "Preferences" entry in the System group of the flyout navigation menu. Previously pointed to the `PreferencesPage` stub; after the navigation fix it points to `SettingsPage`. |
| **PreferencesPage stub** | The placeholder `ContentPage` that displayed "This page is under construction." Deleted as part of the navigation consolidation fix. |
