# YouTube Integration Strategy — MyVocaList Research

> **Core question:** How does MyVocaList integrate YouTube for song discovery without burdening
> users with API key setup or breaking their experience?

---

## 1. The Problem With In-App YouTube Search

| Constraint | Impact |
|------------|--------|
| `search.list` costs 100 units/call | Only 100 searches/day per project on free tier |
| API key acquisition is developer-only | 15-min Google Cloud setup — kills adoption |
| Asking users to get their own key | ToS-compliant but unusable UX |
| Singer-driven search phase | One busy event = 50–100+ searches = quota blown |

**Conclusion:** Building a traditional in-app YouTube search bar is the wrong direction.
It creates either a cost problem (Helder holds key + backend) or an adoption problem (users hold key).

---

## 2. The Better Design: "Share from YouTube"

Users already know how to use YouTube's search. The proposal leverages the **native OS share sheet**
instead of rebuilding search inside MyVocaList.

### Flow

```
YouTube app / browser
  └─ User finds karaoke video
  └─ Taps "Share" → selects MyVocaList
       ↓
MyVocaList receives the URL
  └─ Calls YouTube oEmbed (free, no API key) → gets title + thumbnail
  └─ Parses title → extracts artist + song name
  └─ Detects sharing user's role: Host or Singer
       ↓
Host flow:
  └─ Shows confirm card: [Thumbnail] "Ed Sheeran — Shape of You"
  └─ Artist auto-matched against MyVocaList DB (or create-new prompt)
  └─ One tap → saved to DB

Singer flow:
  └─ Checks if singer is enqueued in the active session
  └─ Offers: "Set as my next-round song" | "Save to my list"
  └─ If "next-round" → syncs to host DB automatically
  └─ If "save locally" → stored on singer's device for later
```

### Why this is the right design

- **Zero API key** — YouTube oEmbed is a free public endpoint, no auth
- **Zero quota** — oEmbed has no rate limits documented; it's a simple metadata lookup, not search
- **Better search** — YouTube's own search is far superior to any in-app search widget
- **Familiar UX** — users know the share sheet; no new mental model needed
- **Works for both phases** — host catalog building and singer self-service use the same mechanism

---

## 3. YouTube oEmbed — The Free Metadata Layer

**Endpoint (no API key, no auth):**
```
GET https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=VIDEO_ID&format=json
```

**Response includes:**
```json
{
  "title": "Ed Sheeran - Shape of You (Karaoke Version)",
  "author_name": "Sing King Karaoke",
  "thumbnail_url": "https://i.ytimg.com/vi/VIDEO_ID/hqdefault.jpg",
  "html": "<iframe ...>"
}
```

**What MyVocaList needs from this:** `title`, `thumbnail_url`. That's it.
No quota. No API key. No project setup. Works for any public YouTube video.

---

## 4. Title Parsing — Artist + Song Extraction

Karaoke video titles follow recognisable patterns (~80% coverage with simple heuristics):

| Title pattern | Artist | Song |
|--------------|--------|------|
| `"Ed Sheeran - Shape of You (Karaoke)"` | Ed Sheeran | Shape of You |
| `"Shape of You - Ed Sheeran \| Karaoke"` | Ed Sheeran | Shape of You |
| `"KARAOKE: Bohemian Rhapsody - Queen"` | Queen | Bohemian Rhapsody |
| `"Shape of You Karaoke Ed Sheeran"` | (manual) | (manual) |

**Parsing strategy:**
1. Strip common suffixes: `(Karaoke)`, `(Karaoke Version)`, `| Karaoke`, `- Karaoke`, `[Karaoke]`
2. Split on ` - ` or ` | ` to separate artist/song (order heuristic: shorter = artist)
3. Fuzzy-match extracted artist name against MyVocaList's existing artist DB
4. Pre-fill the confirm card with best guess; user corrects if wrong and taps confirm

The confirm step is the safety net — no parse has to be perfect.

---

## 5. Platform Implementation

### Android (straightforward)

Register MyVocaList as a share target via `IntentFilter` in `Platforms/Android/MainActivity.cs`:

```csharp
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "text/plain")]
```

In `OnCreate`, extract the shared URL:
```csharp
if (Intent?.Action == Intent.ActionSend && Intent.Type == "text/plain")
{
    string sharedUrl = Intent.GetStringExtra(Intent.ExtraText);
    // navigate to ShareIntentPage with the URL
}
```

**Status:** Well-documented, working solution confirmed in the MAUI community.
Known issue: navigation when the app is already open requires special handling (pass URL via
`MessagingCenter` or a static `PendingShareUrl` property read on `AppShell` resume).

### iOS (non-trivial)

iOS requires a **Share Extension** — a separate project bundled with the main app.
MAUI has no official Share Extension template; it requires platform-specific Xcode project setup.

**Status as of 2025–2026:** No official MAUI template. Community workarounds exist
(Vladislav Antonyuk's MAUI Extensions demo). Requires hands-on platform work.

**Recommendation:** Android-first. iOS Share Extension is a Phase 2 effort — acceptable given
MyVocaList's Android-primary deployment target (`net10.0-android` in `.csproj`).

---

## 6. Singer → Host Sync — The Dependency

The singer self-service share flow has one assumption: **the singer's device can communicate
with the host's device to write into the host's DB.**

This is a separate architectural decision not yet made for MyVocaList:

| Sync approach | Complexity | Infrastructure |
|--------------|-----------|----------------|
| Same local WiFi (mDNS / local HTTP) | Medium | None — peer-to-peer |
| Helder's relay server | High | Backend required |
| QR code handoff (no network) | Low | None — one-way, manual |
| Bluetooth (BLE) | High | None — but limited range/complexity |

**For Phase 1 (host-only share):** no sync needed — host shares to their own device.
**For Phase 2 (singer share → host DB):** this sync architecture must be decided first.
The share flow itself is independent of the sync mechanism; both can be designed separately.

---

## 7. Revised Feature Breakdown

### Phase 1 — Host Share-to-Add (no API key, no sync, Android)
- Register as Android share target
- Receive YouTube URL → oEmbed call → title parse → confirm card → save to DB
- Artist auto-match against existing DB; create-new flow if no match
- **Zero API key. Zero backend. Works offline after oEmbed call.**

### Phase 2 — Singer Share + Sync (requires sync architecture decision first)
- Singer shares → role detected → "next-round song" or "save locally"
- "Next-round" → write to host DB via chosen sync mechanism
- "Save locally" → device-local song list (no sync)

### Deferred indefinitely — In-app YouTube search bar
- Only viable if Helder runs a backend proxy funded by app monetization
- Not needed if Phase 1 + 2 above meet the use case

---

## 8. Open Questions Before Spec

1. **For Phase 2 singer sync:** What is the preferred device-to-device sync mechanism?
   Local WiFi is the simplest zero-infrastructure option for a live event context.
2. **Artist matching on confirm:** If the parsed artist doesn't exist in the DB yet, does the
   confirm card offer "create new artist" inline, or navigate to the full Artist form?
3. **iOS priority:** Is iOS share target a must-have for launch, or can it ship Android-only first?

---

## Sources

- [YouTube oEmbed — no API key needed (queen.raae.codes)](https://queen.raae.codes/2022-01-21-yt-oembed/)
- [Fetch YouTube metadata in 2 lines — Medium](https://100lvlmaster.medium.com/fetch-any-youtube-videos-metadata-in-2-lines-e97c961c9004)
- [Android Share Intent in MAUI — Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/967073/how-to-wire-up-android-share-intent-in-maui)
- [iOS Share Extension in MAUI — dotnet/maui Discussion #27199](https://github.com/dotnet/maui/discussions/27199)
- [YouTube Data API — Getting Started](https://developers.google.com/youtube/v3/getting-started)
- [YouTube API Limits 2026 — getphyllo.com](https://www.getphyllo.com/post/youtube-api-limits-how-to-calculate-api-usage-cost-and-fix-exceeded-api-quota)
