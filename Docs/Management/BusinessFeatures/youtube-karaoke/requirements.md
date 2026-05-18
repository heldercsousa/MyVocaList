# YouTube Karaoke — Requirements

> **Status:** Spec in progress — brainstorm complete 2026-05-17
> **Feature:** YouTube URL management per song + next-singer alert overlay during playback

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Karaoke URL** | A YouTube video URL associated with a song for use during mechanical karaoke playback |
| **Video ID** | The 11-character YouTube identifier (e.g. `dQw4w9WgXcQ`) — the canonical form stored in the DB |
| **Suggested URL** | The URL with the highest `PlayCount` for a given song; used as the default when queuing a performance |
| **Play count** | Number of times a specific URL was used in an actual performance — incremented by the queue flow |
| **Overlay** | The next-singer alert rendered on top of the YouTube app (Android only) |
| **Stage 1 alert** | First alert fired ~45 s before estimated song end — "next singer, prepare" |
| **Stage 2 alert** | Second alert fired ~15 s before estimated song end — "next singer, mic now" |
| **YouTube API key** | A user-supplied Google API key enabling in-app YouTube search; optional |

---

## Overview

MyVocaList supports a **YouTube (mechanical) karaoke mode** alongside the existing Bandokê (live instrumental) mode.

In YouTube mode, a registered song can have one or more YouTube URLs pointing to karaoke videos. When a singer is queued in YouTube mode, the admin launches the best-matched video directly from the app. While the video plays, the app alerts the admin (and through them, the next singer) when it is time to prepare — without requiring the admin to switch apps.

This spec covers:
1. **YouTube URL management** — adding, listing, removing, and playing URLs per song
2. **In-app YouTube search** — optional search using a user-supplied API key
3. **Next-singer alert system** — cross-platform 2-stage alert during playback
4. **Android overlay** — blinking label drawn over the YouTube app

---

## User Stories

### US-1: Manage YouTube URLs for a Song

**As an** admin
**I want to** associate one or more YouTube karaoke video URLs with a song
**So that** I can launch the correct video quickly during a performance without searching YouTube manually

#### Acceptance Criteria

- AC-1.1: The Song form (add and edit) shall include a "YouTube URLs" section below the Lyrics field.
- AC-1.2: The section header shall show a YouTube icon, the label "YouTube URLs", and the word "optional" — no add button in the header.
- AC-1.3: Each saved URL shall appear as a row showing: video ID (short form), play count, duration (if known), and an optional label.
- AC-1.4: The URL with the highest play count shall be marked "★ SUGGESTED" with a visually distinct tint. When play counts are equal, recency (`LastUsedAt` then `AddedAt`) breaks the tie.
- AC-1.5: Each saved URL row shall have a trailing remove button (✕). Tapping it removes the URL after a confirmation snackbar with Undo.
- AC-1.6: The section shall always show a search strip below the saved URLs (see US-2).
- AC-1.7: A song may have zero saved URLs — the section is always visible but empty is valid.
- AC-1.8: There is no maximum number of URLs per song.
- AC-1.9: The same video ID cannot be added twice to the same song — attempting to do so shows an inline message "This URL is already saved for this song."

---

### US-2: Add a YouTube URL (search or paste)

**As an** admin
**I want to** add a YouTube URL to a song either by searching inside the app or by pasting a URL
**So that** I do not have to leave the app to find videos

#### Acceptance Criteria

- AC-2.1: The search strip shall show a text input (pre-filled with "{Artist} {Title} karaoke") and a search button (▶).
- AC-2.2: When a YouTube API key is configured in Settings, tapping the search button shall query YouTube Data API v3 and show up to 5 results.
- AC-2.3: Each result row shall show: video thumbnail, title (truncated), channel name, and duration. A trailing `+` button adds the URL.
- AC-2.4: When a result is added, the row's trailing button changes to a checkmark (✓) and the URL appears in the saved list above — no navigation required.
- AC-2.5: When no API key is configured, the search button shall be replaced with a nudge message: "Add a YouTube API key in Settings to search without leaving the app." The paste field remains active.
- AC-2.6: A "Or paste a URL directly" field shall always be visible below the search results. It accepts any YouTube URL format (full, short, embed) and normalises it to the video ID on save.
- AC-2.7: Pasting an invalid URL (non-YouTube or malformed) shall show an inline error: "Not a valid YouTube URL."
- AC-2.8: When a URL is added via paste or search, the app shall attempt to auto-fill `DurationSeconds` via the YouTube oEmbed endpoint (free, no API key required). If the call fails, `DurationSeconds` remains null silently.
- AC-2.9: The admin may optionally type a short label for any saved URL (e.g. "HD version"). Max 100 characters. Edited inline on the saved URL row.

---

### US-3: Launch a YouTube Video

**As an** admin
**I want to** launch a song's karaoke video from the queue
**So that** the correct video plays without manually searching YouTube

#### Acceptance Criteria

- AC-3.1: From the queue page (future spec), tapping "Play" for a queued singer in YouTube mode shall open the suggested URL in the YouTube app (or browser if YouTube is not installed) using `Launcher.OpenAsync`.
- AC-3.2: If the song has no saved URLs, the "Play" button shall be replaced with a "No URL — add one" link that navigates to the Song edit form.
- AC-3.3: If the song has multiple URLs, a bottom sheet shall list them (suggested first) allowing the admin to pick one. The selected URL is opened and its `PlayCount` is incremented.
- AC-3.4: `PlayCount` is incremented only when the admin confirms the launch — not on URL management actions (add/remove).
- AC-3.5: `LastUsedAt` is updated to the current timestamp on each confirmed launch.

---

### US-4: Next-Singer Alert — Universal (both platforms)

**As an** admin
**I want to** be alerted when the next singer should prepare, while a YouTube video is playing
**So that** I can cue them in time and reduce dead air between performances

#### Acceptance Criteria

- AC-4.1: When a video is launched (US-3), if `DurationSeconds` is known, the app shall schedule two local notifications:
  - Stage 1: at `DurationSeconds - 45` seconds after launch
  - Stage 2: at `DurationSeconds - 15` seconds after launch
- AC-4.2: If `DurationSeconds` is null, no notifications are scheduled. The admin may still use the overlay manually on Android.
- AC-4.3: Stage 1 notification content: title = "Next up — {SingerName}", body = "{SongTitle} · preparing in ~45s".
- AC-4.4: Stage 2 notification content: title = "⚡ {SingerName} — mic now!", body = "{SongTitle} · ~15s remaining". Stage 2 shall use a stronger haptic pattern than Stage 1.
- AC-4.5: Both notifications shall include a "Done" action that marks the current performance as complete and advances the queue — without requiring the admin to open the app.
- AC-4.6: When the admin taps "Done" (from notification or app), both scheduled notifications shall be cancelled.
- AC-4.7: The app shall request local notification permission on first use of this feature, explaining: "MyVocaList uses notifications to alert you when the next singer should prepare."
- AC-4.8: If notification permission is denied, a snackbar shall inform: "Alerts disabled. Enable notifications in Settings to use singer cues."

---

### US-5: Android Overlay — Blinking Label

**As an** admin on Android
**I want to** see a blinking label over the YouTube app showing the next singer
**So that** I have persistent visual awareness without switching apps

#### Acceptance Criteria

- AC-5.1: On Android, when a video is launched, the app shall check for `SYSTEM_ALERT_WINDOW` permission.
- AC-5.2: If permission is not granted, the app shall show a one-time bottom sheet: "Allow MyVocaList to appear on top of other apps to show next-singer alerts while YouTube plays. This is optional — notifications will still work." With "Allow" (opens Settings) and "Skip" actions.
- AC-5.3: If permission is granted, a blinking label shall be drawn over the YouTube app showing the next singer's name.
- AC-5.4: The label shall animate using `FadeTo` only — no timer-driven opacity mutations, no `while` loops with `Task.Delay`.
  - Stage 1 (from launch to T-15s): slow pulse — `FadeTo(0, 1200) → FadeTo(1, 1200)` loop
  - Stage 2 (T-15s to end): fast blink — `FadeTo(0, 400) → FadeTo(1, 400)` loop; text turns red
- AC-5.5: The label shall show: "NEXT" prefix + singer name + song title (Stage 1); "⚡ {name} — mic now!" (Stage 2).
- AC-5.6: The label shall be positioned top-right by default. The admin may drag it to any corner; the chosen position is persisted per session.
- AC-5.7: The label shall have no background panel — text shadow only for legibility on any video background (`text-shadow` equivalent via MAUI platform shadow).
- AC-5.8: The overlay is implemented as a foreground service with a `WindowManager` overlay view. It must not block touch events on the underlying video.
- AC-5.9: Tapping the label shall bring MyVocaList to the foreground.
- AC-5.10: The overlay shall be dismissed automatically when the admin taps "Done" or when the app returns to the foreground.
- AC-5.11: This feature is Android-only. On iOS, no overlay is shown; the notification-based alerts (US-4) are the sole mechanism.

---

### US-6: YouTube API Key — Settings

**As an** admin
**I want to** optionally configure a YouTube Data API v3 key
**So that** I can search for videos inside the app without switching to YouTube

#### Acceptance Criteria

- AC-6.1: The app Settings page shall include a "YouTube Integration" section with an API key input field.
- AC-6.2: The API key shall be stored in the platform's secure storage (Android Keystore / iOS Keychain via `SecureStorage`).
- AC-6.3: The field shall show the key masked (••••••••) with a reveal toggle.
- AC-6.4: A "Test" button shall fire a minimal API call to verify the key is valid, showing "Key valid ✓" or "Invalid key — check and retry."
- AC-6.5: Clearing the key field and saving shall remove the key from secure storage and revert the search strip to paste-only mode.
- AC-6.6: The Settings page shall include a help link explaining how to obtain a free YouTube Data API v3 key and the free quota (10,000 units/day, 100 units per search = ~100 searches/day).

---

## Data Model

### SongKaraokeUrl

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `VideoId` | `string` | PK (composite with SongId), NOT NULL, maxLen=11 | YouTube video ID only — not the full URL |
| `SongId` | `int` | PK (composite with VideoId), FK → `Song.Id`, NOT NULL, CASCADE DELETE | |
| `PlayCount` | `int` | NOT NULL, default 0 | Incremented on each confirmed launch |
| `DurationSeconds` | `int?` | nullable | Auto-filled via oEmbed on add; null = unknown |
| `LastUsedAt` | `datetime?` | nullable | Updated on each confirmed launch |
| `AddedAt` | `datetime` | NOT NULL, default = now | |
| `Label` | `string?` | nullable, maxLen=100 | Admin-assigned nickname |

**Composite unique index:** `(SongId, VideoId)` — same video cannot be added twice to the same song.

**Suggested URL query:**
```sql
SELECT * FROM SongKaraokeUrls
WHERE SongId = @id
ORDER BY PlayCount DESC, LastUsedAt DESC, AddedAt DESC
LIMIT 1
```

### Song (additions to existing entity)

No new columns on `Song` itself. The relationship is expressed entirely through `SongKaraokeUrl`.

---

## Validation Rules

### SongKaraokeUrl

| Field | Rule | Error |
|-------|------|-------|
| VideoId | Must be exactly 11 chars, alphanumeric + `-` + `_` | "Not a valid YouTube URL." |
| VideoId | Unique per song | "This URL is already saved for this song." |
| Label | optional; maxLen = 100 | "Label too long. Maximum 100 characters." |

### URL normalisation (on paste)

The app accepts any of these formats and extracts the video ID:
- `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
- `https://youtu.be/dQw4w9WgXcQ`
- `https://www.youtube.com/embed/dQw4w9WgXcQ`
- `https://youtube.com/shorts/dQw4w9WgXcQ`

Any other format → validation error "Not a valid YouTube URL."

---

## Out of Scope

- Embedded YouTube playback inside the app (future spec — WebView IFrame Player)
- Live Activities / Dynamic Island (iOS 16.1+ — future spec)
- Overlay position memory across sessions (position resets to top-right on each launch)
- Song duration auto-detection without `DurationSeconds` (e.g. audio analysis)
- YouTube playlist support (single video IDs only)
- Offline / downloaded video support
- Multiple concurrent performances
- Analytics dashboard for play counts
- AI-powered overlay positioning (future spec — stored `OverlayZone` per video)
- Visual theme redesign — captured separately in BACKLOG.md

---

## Future Specs

### Embedded Playback
Once YouTube ToS and iOS WebView autoplay restrictions are resolved, `SongKaraokeUrl` already has `VideoId` and `DurationSeconds` — no schema change needed.

### AI Overlay Positioning
A one-time Claude Vision API call per video ID identifies the safe corner for the overlay. Result stored in a new `OverlayZone` column on `SongKaraokeUrl`. Cached forever; ~$0.001 per video.

### Live Activities (iOS)
ActivityKit-based Live Activity showing next singer in the Dynamic Island and Lock Screen. Same data source as the notification system (US-4).
