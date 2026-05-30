# YouTube Karaoke — Session Handoff

**Date:** 2026-05-30
**Branch:** develop
**Last commit:** docs: mark all YouTube Karaoke test tasks complete — 192 tests passing

---

## Status

All Phase 1–5 tasks committed and pushed. Review run — **1 Blocker + 5 Warnings** found. Session cleared before fixing. Fix these in the next session in the order listed.

---

## Fixes Required (in priority order)

### 🔴 B-1 — AC-1.5: Remove URL has no undo (BLOCKER)

**File:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `RemoveUrlAsync`
**Problem:** Removes URL immediately and shows `ShowSuccessAsync("URL removed")`. Spec requires undo snackbar.
**Root cause:** `ISnackbarComponent` only has `ShowSuccessAsync(string)` and `ShowErrorAsync(string)` — no undo callback.
**Fix:** Extend `ISnackbarComponent` with an undo overload, then implement optimistic remove + undo re-add in `RemoveUrlAsync`.

Option A (preferred): Add `Task ShowWithUndoAsync(string message, string undoLabel, Func<Task> onUndo)` to `ISnackbarComponent` + `SnackbarComponent`. DevExpress `DXSnackbar` supports action buttons — use `ActionText` and `ActionCommand`.

Option B (simpler): Show a `dx:BottomSheet` with "Remove?" + Cancel/Confirm before deleting.

**Files to touch:** `MyVocaList/UI/Components/SnackbarComponent.cs`, `SongFormViewModel.cs`
> `SnackbarComponent.cs` is a sequential-only file — do not edit in parallel with other hotspot files.

---

### 🟡 W-3 — AC-2.1: Search input not pre-filled (easy win)

**File:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
**Problem:** `YoutubeSearchQuery` starts as `string.Empty`. Spec says pre-fill with `"{Artist} {Title} karaoke"`.
**Fix:** In `OnSongIdChanged` (or `LoadKaraokeUrlsAsync`), after loading, set:
```csharp
if (string.IsNullOrWhiteSpace(YoutubeSearchQuery))
    YoutubeSearchQuery = $"{ArtistName} {SongTitle} karaoke".Trim();
```
`ArtistName` and `SongTitle` are `[ObservableProperty]` already set via `[QueryProperty]` before `OnSongIdChanged` fires.

---

### 🟡 W-4 — AC-2.3: No thumbnail in search results

**File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
**Problem:** `YouTubeSearchResultDto.ThumbnailUrl` is populated but not rendered.
**Fix:** Add `Image Source="{Binding ThumbnailUrl}"` (small, ~48×48, left of the VerticalStackLayout) inside the search results `DataTemplate`. Wrap in a `Grid ColumnDefinitions="48,*,Auto"`.

---

### 🟡 W-2 — AC-1.4: Suggested badge shows "★" only, not "★ SUGGESTED"

**File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
**Problem:** Badge `Label` text is `"★"`. Spec says "★ SUGGESTED".
**Fix:** Change `Text="★"` to `Text="★ SUGGESTED"` in the `IsSuggested` badge Border. Adjust `Padding` if needed.

---

### 🟡 W-5 — AC-2.4: + button doesn't toggle to ✓ after adding from search

**Files:** `SongFormViewModel.cs`, `SongFormPage.xaml`
**Problem:** After `AddFromSearchCommand` adds a result, the "+" button stays unchanged.
**Fix:**
1. Add `ObservableRangeCollection<string> _addedSearchVideoIds` or `HashSet<string>` in VM (exposed as observable if binding needed, or use a converter with the `KaraokeUrls` collection).
2. In `AddFromSearchAsync`, after success: add `result.VideoId` to the set.
3. In XAML, bind the `+` button's `Content` to a converter or use a `DataTrigger` checking if `VideoId` is in `KaraokeUrls`.

Simpler approach: disable the `+` button when `VideoId` is already in `KaraokeUrls` using a converter — avoids the separate tracking collection.

---

### 🟡 W-1 — AC-1.2: No YouTube icon in section header

**File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
**Problem:** Section header has two `Label` elements but no YouTube icon.
**Fix:** Add `Image Source="youtube_icon"` (resource must exist — check `Resources/Images/`). If asset doesn't exist, use a red `Label Text="▶"` as placeholder and log asset gap.

---

## Constraint to add after fixes

Add to `.claude/rules/constraints-registry.md`:
> **BindableLayout vs DXCollectionView in ScrollView forms:** For small inline lists embedded in a `ScrollView`-based form page, use `BindableLayout.ItemsSource` on a `VerticalStackLayout` — not `DXCollectionView`. `DXCollectionView` inside a `ScrollView` requires workarounds (`IsScrollable="False"` + fixed height). Established by `SongFormPage.xaml` (ApiResults section pre-existing, YouTube URLs section 2026-05-30).

---

## Session Start Reading Order (next session)

1. This file (`handoff.md`)
2. `MyVocaList/UI/Components/SnackbarComponent.cs` — understand current interface before extending
3. `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — for W-3 and W-5
4. `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — for W-1, W-2, W-4
5. `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/requirements.md` — AC reference
