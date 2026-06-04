# YouTube Integration — Conflict Analysis & Action Plan

## Update: Direct API Usage Research (2026-06-04)

**Question:** Can a consumer app call YouTube Data API v3 directly using a single
developer-owned key, without requiring each user to obtain their own key?

**Answer: Yes — this is the standard industry model.**

### Key findings

- **ToS allows a single developer key for multi-user apps.** The YouTube API Terms of Service
  bind the *API Client* (the app) to the *API Project* (the Google Cloud project). There is no
  rule requiring per-user keys. The developer holds one key; all users of that app share the
  quota from that one project. This is exactly how every consumer app that embeds YouTube search
  works (Spotify, music playlist apps, karaoke apps, etc.).

- **The "key per user" assumption in the original spec was wrong.** The earlier concern was based
  on misreading the quota model. The correct model: Helder creates one Google Cloud project,
  enables YouTube Data API v3, generates one API key, and all MyVocaList users share the 10,000
  units/day quota from that project.

- **Key security:** Embedding an API key directly in a mobile app binary is risky (can be
  extracted via reverse engineering). The recommended pattern for distributed mobile apps is a
  **thin backend proxy** — the app calls Helder's server, the server holds the key and forwards
  to YouTube. This adds server infrastructure but solves both security and quota management.

- **Quota math for MyVocaList:** 10,000 units/day ÷ 100 units/search = 100 searches/day across
  ALL users sharing the key. For a personal/single-KJ install this is workable. For any
  meaningful distribution (multiple KJs, singer self-service search), the free tier is
  exhausted quickly.

- **Quota increase = paid tier, and it is expensive.** Beyond the free 10,000 units/day,
  additional quota requires entering Google's paid quota programme. Pricing is not published
  transparently but community reports place significant usage in the range of hundreds to
  thousands of USD/month at scale. This makes a centrally-funded YouTube search feature
  only viable under a paid subscription model where app revenue explicitly covers API costs.
  There is no cheap "just request more quota" path — the extension form leads to billing.

- **Conclusion on central-key viability:** Technically ToS-compliant, but economically
  only sustainable if MyVocaList charges a subscription fee that covers the YouTube API
  bill. Free-tier apps at any meaningful scale will hit the 100-searches/day wall and
  cannot afford the overage costs without passing them on to users.

### Implication for YouTubeSearchPage (Task 3c)

The API-key-per-user block is removed — that assumption was wrong architecturally.
But a new economic block replaces it:

1. SongPickerPage (3b) must ship first — sequential dependency unchanged
2. Backend proxy decision — needed for key security in a distributed app
3. **Monetisation decision required:** in-app YouTube search via a central key is only
   sustainable under a paid subscription. Shipping it free means either hitting quota daily
   or absorbing unpredictable API costs. This decision belongs to Helder before Task 3c unblocks.
4. **Alternative path:** `Share from YouTube` (oEmbed, zero quota, zero cost) may be the
   right permanent solution rather than a stepping stone — especially pre-monetisation.

The `YouTubeSearchPage` and the `Share from YouTube` feature remain complementary:
`YouTubeSearchPage` = in-app convenience; Share = zero-infrastructure fallback.

---

> **Session goal:** (1) Register this research in its dedicated folder with a proper name.
> (2) Compare the "Share from YouTube" approach with the existing `YouTubeSearchPage` task
> in the Search Picker spec and resolve the conflict.

---

## What Already Exists — YouTubeSearchPage (Search Picker, Task 3c)

The Search Picker spec (`Docs/Management/BusinessFeatures/search-picker/`) defines a full
in-app YouTube search page as its third picker:

| Aspect | Detail |
|--------|--------|
| **File** | `YouTubeSearchPage.xaml` + `YouTubeSearchViewModel.cs` |
| **Route** | `youtube-search` |
| **Status** | `🔴 Blocked` (on SongPickerPage / Search Picker Phase 3b) |
| **How it works** | User types a query inside MyVocaList → calls `IYouTubeService.SearchAsync` → YouTube Data API v3 `search.list` → results list → user picks → URL added to song |
| **Key gate** | `HasYouTubeApiKey` — trigger row hidden entirely if no API key configured |
| **Quota cost** | 100 units per `search.list` call; default quota = 100 searches/day total |

**ACs that gate on the API key:**
- AC-YT-01: "Search YouTube" trigger row visible only when `HasYouTubeApiKey` is `true`
- AC-YT-02: if `HasYouTubeApiKey` is `false`, trigger is hidden and a no-API-key nudge is shown

The spec already acknowledges the API key problem — it hides the feature entirely for users without one.

---

## What the Research Proposes — Share from YouTube

Instead of building search inside the app, MyVocaList becomes an **Android share target**.
The user searches on YouTube themselves, shares the URL to MyVocaList, and the app handles
metadata resolution and confirmation.

| Aspect | Detail |
|--------|--------|
| **API key required** | None |
| **Quota** | None — YouTube oEmbed is a free public endpoint |
| **Works for** | 100% of users, zero setup |
| **Metadata** | `GET https://www.youtube.com/oembed?url=VIDEO_URL&format=json` → title, thumbnail |
| **Implementation** | Android `IntentFilter` on `MainActivity`; iOS Share Extension (deferred) |
| **Confirm card** | Inline editable pre-filled with parsed artist + song; one tap to save |

---

## The Conflict — Head-to-Head

Both features solve the same job: **"add a YouTube karaoke URL to a song"**.

| | YouTubeSearchPage | Share from YouTube |
|---|---|---|
| User stays in MyVocaList | ✓ | ✗ (switches to YouTube app) |
| Requires API key | ✓ (mandatory) | ✗ (zero config) |
| Works for all users | ✗ (hidden without key) | ✓ |
| Search quality | YouTube API results | YouTube's own search (better) |
| Quota risk | 100 searches/day shared | None |
| iOS support | Yes (same as Android) | Phase 2 only (no MAUI template) |
| Implementation complexity | Medium (already specced) | Low (one IntentFilter + oEmbed call) |
| Coexist possible? | Yes — different entry points | Yes |

**Important distinction:** These are NOT the same UX. oEmbed does NOT search — it only resolves
metadata for a known URL. The Share approach requires the user to leave MyVocaList. The
YouTubeSearchPage keeps the user in-app. They are complementary, not identical.

---

## Decision

### Recommended resolution: Keep Task 3c blocked; Ship Share from YouTube first

Task 3c is NOT removed or suspended. It remains in the spec, blocked. The two features serve
different UX modes and can coexist. The API key friction problem for Task 3c is real but
solvable in the future via an automated agent flow — e.g. a browser extension agent (Claude Code
browser extension is a candidate) that walks the user through Google Cloud project creation,
API key generation, and injection into the app on their behalf. This is a reasonable near-future
capability given the AI agent tooling trajectory.

**Task 3c remains blocked on:** SongPickerPage (current dependency) AND the future API-key
automation solution. When that solution exists, Task 3c unblocks and the two flows coexist:
- Share from YouTube → zero-config, works for everyone
- YouTubeSearchPage → in-app convenience for users who have automated the API key setup

### Action items (not implementation — spec changes only)

| # | Action | Where |
|---|--------|-------|
| 1 | Update BACKLOG: add secondary blocker note to YouTubeSearchPage row | `BACKLOG.md` |
| 2 | Add new BACKLOG entry: **YouTube Share Intent** (Phase 1: host share-to-add, Android) | `BACKLOG.md` |
| 3 | Create `BusinessFeatures/youtube-share/` folder | filesystem |
| 4 | Move + rename this file to `BusinessFeatures/youtube-share/findings.md` | filesystem |
| 5 | Register `findings.md` in `MyVocaList.sln` under a new `youtube-share` Solution Folder | `MyVocaList.sln` |

### What happens to `IYouTubeService` and `HasYouTubeApiKey`?

- `IYouTubeService` — retain the interface; it still exists in the codebase for the Settings page
  API key test flow. Do not delete. Mark as "future in-app search candidate" in its XML doc.
- `HasYouTubeApiKey` in `SongFormViewModel` — still used to show/hide the Paste URL section
  and key nudge. Retain. The "Search YouTube" trigger row it was gating is now gone.
- `app-settings` spec and `SettingsPage` — unaffected; YouTube API key management stays for users
  who already configured a key and may want to use it for other future features.

---

## Cleanup Plan for `Docs/Management/` Orphan Files

Several plan-mode-generated files with random names are sitting directly in `Docs/Management/`.
All should be moved to their feature folder or deleted:

| File | Content | Action | Destination |
|------|---------|--------|-------------|
| `adaptive-doodling-knuth.md` | Plan: Fix URL Remove Undo Pattern in SongFormViewModel | Move | `BusinessFeatures/artists-songs/bugs/url-undo-fix-plan.md` |
| `gentle-splashing-wave.md` | Plan: UI Architecture Decision (ui-2nd-refactor) | Move | `BusinessFeatures/UI-2nd-refactor/ui-arch-decision-plan.md` |
| `goofy-munching-widget.md` | Session work-queue plan (Phase 16C + search-picker context) | Move | `BusinessFeatures/artists-songs/session-plan-phase16c-work-queue.md` |
| `happy-knitting-storm.md` | Backup & Restore implementation plan draft | Move | `BusinessFeatures/backup-restore/plan-draft.md` |
| `reflective-fluttering-hinton.md` | App Settings orchestration plan | Move | `BusinessFeatures/app-settings/orchestration-plan.md` |
| `tidy-discovering-summit.md` | About Page evaluation | Move | `BusinessFeatures/about-page/evaluation.md` |
| `agile-weaving-stroustrup.md` | This file — YouTube integration research | Move + rename | `BusinessFeatures/youtube-share/findings.md` |

`.sln` changes required:
- Remove entries for `adaptive-doodling-knuth.md` and `happy-knitting-storm.md` (the only two registered)
- Add entries for the two moved files at their new paths
- Add new `youtube-share` Solution Folder with `findings.md` registered

---

## New BACKLOG Entry (to be written)

**Table: Business Features**

| Target | Feature | Status | Notes |
|--------|---------|--------|-------|
| 2026-06 | ↳ YouTube Share Intent | 💡 Pending | Share-from-YouTube replaces in-app search (no API key). Phase 1: host Android share target + oEmbed metadata + confirm card. Research: `Docs/Management/BusinessFeatures/youtube-share/findings.md` |

**Update to existing Search Picker row:**

Add note: `YouTubeSearchPage (Task 3c) suspended — superseded by YouTube Share Intent feature.`
