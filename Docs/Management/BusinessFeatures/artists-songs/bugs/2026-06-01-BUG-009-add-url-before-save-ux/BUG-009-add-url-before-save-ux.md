# BUG-009 — Add YouTube URL Before Save Shows Blocking Validation

**Status:** Spec  
**Reported:** 2026-06-11  
**Area:** SongFormPage / SongFormViewModel / ISongService

---

## Summary

In **New Song mode** (no `SongId` yet), tapping **Add** next to the paste URL field shows:

> "Save the song first before adding URLs"

This is a blocking UX failure. The user typed a URL while filling in the form and is told to abandon the URL flow, save, reopen, then try again — three extra steps for something the app should handle transparently.

---

## Current Flow vs Desired Flow

### Current flow (broken)

1. User opens New Song page.
2. User fills in Artist + Title.
3. User pastes a YouTube URL and taps **Add**.
4. `AddFromPasteAsync` checks `!SongId.HasValue` → sets `PasteUrlError = "Save the song first before adding URLs"` and returns.
5. URL is discarded. User must manually tap Save, wait for navigation pop, reopen the song, and paste the URL again.

The same blockage exists in `NavigateToYouTubeSearchAsync` — `AddUrlFromPickerAsync` also calls `_karaokeUrlService.AddUrlAsync(SongId!.Value, ...)`, which would throw if `SongId` is null (the null-forgiving `!` suppresses the warning, masking the bug path).

### Desired flow

1. User opens New Song page.
2. User fills in Artist + Title.
3. User pastes a YouTube URL and taps **Add**.
4. URL is validated and held **in memory** (pending list in the ViewModel).
5. User taps **Save**.
6. `ISongService.CreateSongAsync` inserts the Song row and immediately inserts all pending URLs in a **single transaction** — the user sees no intermediate state.
7. Snackbar: "Song created". Navigation pops. Done.

Pending URLs must also be shown in the URL list during form fill, with a visual distinction (optional — no "★ SUGGESTED" badge, no play count) so the user gets confirmation that their URL was accepted.

---

## Root Cause

**Architectural:** `SongKaraokeUrl.SongId` is a non-nullable FK to `Song.Id`. A URL cannot be persisted until the `Song` row exists. The current code calls `_karaokeUrlService.AddUrlAsync(SongId.Value, ...)` immediately on paste — correct for edit mode, but impossible in new-song mode before save.

**Code location:** `SongFormViewModel.AddFromPasteAsync` lines 300–305 — explicit guard that blocks instead of buffering:

```csharp
if (!SongId.HasValue)
{
    PasteUrlError = "Save the song first before adding URLs";
    HasPasteUrlError = true;
    return;
}
```

The same issue exists silently in `NavigateToYouTubeSearchAsync → AddUrlFromPickerAsync` (null-forgiving `!` hides it).

---

## Fix Approach

### Principle: buffer in ViewModel, commit atomically in Service

The ViewModel holds a **pending URLs list** (in-memory only, no SongId required). On Save, a new `ISongService` method inserts the Song and all pending URLs in a single EF transaction.

### ViewModel changes (`SongFormViewModel.cs`)

1. Add `private readonly List<string> _pendingRawUrls = []` — raw URL strings for new-song mode.
2. In `AddFromPasteAsync`: when `!SongId.HasValue`, extract + validate the video ID via `_karaokeUrlService.ExtractVideoId(raw)`, check for duplicate in `_pendingRawUrls`, then add to `_pendingRawUrls` and add a "pending" `SongKaraokeUrlDto` (with `IsSuggested = false`, zero `PlayCount`) to `KaraokeUrls` for display. Clear `PasteUrlInput`.
3. In `NavigateToYouTubeSearchAsync` message handler: same — when `!SongId.HasValue`, buffer the `videoId` into `_pendingRawUrls` and append to `KaraokeUrls`.
4. In `RemoveUrlAsync`: when `!SongId.HasValue`, remove from `_pendingRawUrls` and `KaraokeUrls` directly (no DB call, no undo snackbar).
5. In `SaveAsync` new-song branch: call the new `CreateSongWithUrlsAsync` instead of `CreateSongAsync`. Pass `_pendingRawUrls` as the URL list. On success, navigate back (no pending URL re-add needed). On failure, keep `_pendingRawUrls` intact so URLs are not lost on a validation error.

### Service interface change (`ISongService`)

Add one method:

```csharp
/// <summary>
/// Creates a song and atomically inserts all pending YouTube URLs in a single transaction.
/// URL insertion failures are non-fatal — invalid/duplicate URLs are skipped and reported in the result.
/// </summary>
Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
    int artistId,
    string title,
    string? featuredArtists,
    string? lyrics,
    IReadOnlyList<string> pendingRawUrls,
    string? externalId = null,
    string? externalProvider = null,
    CancellationToken ct = default);
```

### Service implementation (`SongService.cs`)

`CreateSongWithUrlsAsync`:
1. Run the same validation as `CreateSongAsync` (title, artist exists, duplicate title).
2. Insert the `Song` row via `_songRepository.AddAsync` + `SaveChangesAsync` — Song now has a real `Id`.
3. For each raw URL in `pendingRawUrls`: call `_karaokeUrlService.AddUrlAsync(song.Id, rawUrl)`. Non-fatal: log and skip on failure (invalid URL or duplicate). The song is already saved — URL failures must not roll back the song.
4. Return `(true, message, song)`.

**Transaction scope:** `SaveChangesAsync` in step 2 commits the song. URL inserts in step 3 each call their own `SaveChangesAsync` via the existing `SongKaraokeUrlService.AddUrlAsync`. This is intentional: URL insert failures are non-fatal and must not cause a rollback of the song. A future enhancement could wrap all inserts in a single `DbContext.SaveChangesAsync` call for performance, but correctness does not require it for MVP.

### No change to `ISongKaraokeUrlService` or repository interfaces

`AddUrlAsync` already accepts a `songId` and is already used in edit mode. No repository changes needed.

---

## Affected Files

| File | Change |
|------|--------|
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | Add pending URL buffer; update `AddFromPasteAsync`, picker handler, `RemoveUrlAsync`, `SaveAsync` |
| `Domain/ServicesInterfaces/ISongService.cs` | Add `CreateSongWithUrlsAsync` signature |
| `Services/SongService.cs` | Implement `CreateSongWithUrlsAsync` |
| `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` | Tests for new method |

No migration needed. No new entity. No repository interface change.

---

## Acceptance Criteria

**AC-BUG009-01 — URL accepted before save**
> Given the user is on New Song page and has not yet tapped Save,
> When the user pastes a valid YouTube URL and taps Add,
> Then the URL appears in the URL list on the form, and no error is shown.

**AC-BUG009-02 — Pending URL persisted on Save**
> Given the user added one or more URLs in New Song mode,
> When the user taps Save with a valid Artist + Title,
> Then the song is created and all pending URLs are persisted to the database.

**AC-BUG009-03 — Pending URL survives a failed Save attempt**
> Given the user added a URL but left Artist blank,
> When the user taps Save (which fails validation),
> Then the URL is still shown in the URL list and is not lost.

**AC-BUG009-04 — Pending URL can be removed before Save**
> Given the user added a URL in New Song mode,
> When the user taps the remove (✕) button next to that URL,
> Then the URL is removed from the list without any database call or undo snackbar.

**AC-BUG009-05 — Invalid URL in new-song mode shows inline error**
> Given the user is on New Song page,
> When the user pastes an invalid or non-YouTube URL and taps Add,
> Then `PasteUrlError` is shown ("Not a valid YouTube URL.") and nothing is added to the list.

**AC-BUG009-06 — Duplicate URL in pending list is rejected**
> Given the user already added a URL in new-song mode,
> When the user pastes the same URL again and taps Add,
> Then `PasteUrlError` is shown ("This URL is already added.") and the list is unchanged.

**AC-BUG009-07 — Edit mode unchanged**
> Given an existing song (edit mode, `SongId` is set),
> When the user adds a URL,
> Then the existing direct-persist path is used (no pending buffer), behavior unchanged from before this fix.
