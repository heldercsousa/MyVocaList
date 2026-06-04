# YouTube Integration — Conflict Analysis & Action Plan

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

### Recommended resolution: Replace Task 3c, keep the door open for in-app search later

**Rationale:**

1. `YouTubeSearchPage` (Task 3c) is currently `🔴 Blocked` — it hasn't started. The cost of
   changing direction now is zero.

2. The API key gate (AC-YT-01/02) is an admission that the current design fails a significant
   portion of users. "Hide the feature if no key" is not a solution — it is feature denial.

3. Share from YouTube delivers the same outcome (URL added to song) with zero adoption friction.
   The UX of switching to YouTube to search is familiar and arguably better (YouTube's own search
   is far more powerful than any in-app wrapper).

4. `IYouTubeService` was designed for in-app search. Under the Share model, it becomes unnecessary
   for the Phase 1 karaoke URL use case. It may have a future role if in-app search is reintroduced
   via a paid/backend model — but that is post-MVP.

### Action items (not implementation — spec changes only)

| # | Action | Where |
|---|--------|-------|
| 1 | Mark Task 3c (`YouTubeSearchPage`) as `[SUSPENDED — superseded by Share from YouTube]` | `search-picker/tasks.md` |
| 2 | Update Search Picker `requirements.md`: remove AC-YT-01 through AC-YT-08 | `search-picker/requirements.md` |
| 3 | Update Search Picker `design.md`: remove YouTubeSearchPage section | `search-picker/design.md` |
| 4 | Add `YouTubeSearchPage` suspension note to BACKLOG (Search Picker row) | `BACKLOG.md` |
| 5 | Add new BACKLOG entry: **YouTube Share Intent** (Phase 1: host share-to-add, Android) | `BACKLOG.md` |
| 6 | Create `BusinessFeatures/youtube-share/` folder | filesystem |
| 7 | Move + rename this file to `BusinessFeatures/youtube-share/findings.md` | filesystem |
| 8 | Register `findings.md` in `MyVocaList.sln` under a new `youtube-share` Solution Folder | `MyVocaList.sln` |

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
| `goofy-munching-widget.md` | Ephemeral session work queue (Phase 16C context — done) | Delete | — |
| `happy-knitting-storm.md` | Backup & Restore plan (feature ✅ Done; `plan.md` exists) | Delete | — |
| `reflective-fluttering-hinton.md` | App Settings plan (feature ✅ Done; `plan.md` exists) | Delete | — |
| `tidy-discovering-summit.md` | About Page evaluation (feature ✅ Done) | Delete | — |
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
