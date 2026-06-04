# YouTube Integration Strategy — MyVocaList Research

> **Status:** Research complete. Ready for spec writing (Phase 1 — Host Share-to-Add).
> Phase 2 (singer sync) is deferred — blocked on sync architecture, not on this feature.

---

## Core Insight: "Share from YouTube" Eliminates the API Key Problem

YouTube Data API v3 `search.list` costs **100 units/call** against a **10,000 unit/day free quota**
= only 100 searches/day. Asking users to configure their own key is a developer workflow that
kills adoption. Building in-app search requires either a backend (cost) or user-configured keys (UX failure).

**The solution:** Don't build YouTube search inside MyVocaList at all.
Users already know YouTube. MyVocaList registers as a share target — user finds the video on YouTube,
shares it to MyVocaList, and the app handles the rest.

---

## YouTube oEmbed — Free Metadata, No Key

```
GET https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=VIDEO_ID&format=json
```

Returns: `title`, `thumbnail_url`, `author_name`. No API key. No quota. No Google Cloud project.
This is the only YouTube API call MyVocaList needs to make.

---

## Feature Design

### Receive & Confirm Flow (same for host and singer)

```
YouTube app / browser
  └─ User finds karaoke video → taps Share → selects MyVocaList
       ↓
MyVocaList receives URL
  └─ Calls oEmbed → gets title + thumbnail
  └─ Parses title → extracts artist + song name (heuristic, ~80% accuracy)
  └─ Detects role of sharing user: Host or Singer
       ↓
Confirm card (inline editable):
  ┌─────────────────────────────────┐
  │ [Thumbnail]  Shape of You       │  ← editable in place
  │              Ed Sheeran         │  ← editable, matched against DB
  │              [Confirm] [Cancel] │
  └─────────────────────────────────┘
  User can correct any field before confirming.
  No navigate-away. One tap to save if auto-parse was correct.
```

**Artist matching:** Fuzzy-match parsed artist name against existing artist DB.
- Match found → pre-select artist
- No match → offer "create new artist" inline on the same confirm card

### Host Path
- Tap Confirm → song saved to host's DB with artist link
- Immediately available in the song catalog for queue assignment

### Singer Path (Phase 2 — deferred)
- Detects if singer is enqueued in the active session
- Offers: **"Set as my next-round song"** | **"Save to my list"**
- "Next-round" → sync to host DB (sync mechanism TBD: local WiFi → Bluetooth/QR → cloud)
- "Save locally" → device-local list, no sync

### Multi-URL future extension
After Phase 1 ships, each song entry can accumulate multiple YouTube URLs
(different karaoke channels, different keys/arrangements) with per-URL usage stats across events.
The confirm card naturally extends to support this — each share adds a URL to the existing song entry
if the song is already recognised.

---

## Title Parsing Heuristics

| Title pattern | Extracted artist | Extracted song |
|--------------|-----------------|---------------|
| `"Ed Sheeran - Shape of You (Karaoke)"` | Ed Sheeran | Shape of You |
| `"Shape of You - Ed Sheeran \| Karaoke"` | Ed Sheeran | Shape of You |
| `"KARAOKE: Bohemian Rhapsody - Queen"` | Queen | Bohemian Rhapsody |
| `"Shape of You Karaoke Ed Sheeran"` | (user corrects) | (user corrects) |

Strategy:
1. Strip common karaoke suffixes/prefixes: `(Karaoke)`, `(Karaoke Version)`, `| Karaoke`, `- Karaoke`, `[Karaoke]`, `KARAOKE:`
2. Split on ` - ` or ` | `
3. Fuzzy-match each segment against existing artist DB to determine which segment is the artist
4. Present best guess; user corrects inline if wrong

---

## Platform Implementation

### Android (Phase 1)

One `IntentFilter` on `MainActivity` registers the app in the native share sheet:

```csharp
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "text/plain")]
```

Extract URL in `OnCreate`:
```csharp
if (Intent?.Action == Intent.ActionSend && Intent.Type == "text/plain")
{
    string sharedUrl = Intent.GetStringExtra(Intent.ExtraText);
    // navigate to ShareConfirmPage with URL
}
```

**Known MAUI issue:** When the app is already open, a new share intent may not navigate correctly.
Requires handling via a static pending-URL property read on `AppShell` resume, or `MessagingCenter`.

### iOS (after Android is in production)

Requires a **Share Extension** — a separate project bundled with the main app.
No official MAUI template exists as of 2026; requires platform-specific Xcode work.
Deferred until Android ships and is stable in production.

---

## Sync Architecture (Phase 2 — deferred, not a blocker)

Singer → host sync is independent of the share flow and will be addressed separately.
Planned progression:
1. **Local WiFi** — host runs a listener; singers POST to it; no internet required at venue
2. **Bluetooth / QR handoff** — fallback for venues without shared WiFi; evaluated against complexity
3. **Cloud relay** — if the app gains wider adoption; both devices connect to Helder's server

Each tier is additive — Phase 1 ships with zero sync, Phase 2 adds local WiFi, etc.

---

## BACKLOG Impact

| Feature | Phase | Status | Blocked on |
|---------|-------|--------|-----------|
| Host Share-to-Add (Android) | 1 | Ready to spec | Nothing |
| Singer self-service share + local save | 2 | Pending | Sync architecture |
| Singer → host sync (local WiFi) | 2 | Pending | Sync architecture decision |
| Multi-URL per song + usage stats | 3 | Pending | Phase 1 shipped |
| iOS Share Extension | — | Pending | Android in production |
| In-app YouTube search bar | Indefinitely deferred | — | Monetization decision |

---

## Sources

- [YouTube oEmbed — no API key needed](https://queen.raae.codes/2022-01-21-yt-oembed/)
- [Android Share Intent in MAUI — Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/967073/how-to-wire-up-android-share-intent-in-maui)
- [iOS Share Extension in MAUI — dotnet/maui Discussion #27199](https://github.com/dotnet/maui/discussions/27199)
- [YouTube API Limits 2026 — getphyllo.com](https://www.getphyllo.com/post/youtube-api-limits-how-to-calculate-api-usage-cost-and-fix-exceeded-api-quota)
- [search.list quota cost — Google Developers](https://developers.google.com/youtube/v3/docs/search/list)
