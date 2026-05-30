# Plan: Fix URL Remove Undo Pattern in SongFormViewModel

## Context

Helder flagged that the undo behavior for URL removal in the song form feels off. Investigation confirms:

1. **The undo implementation has a critical bug that makes undo silently fail.**
2. The underlying design pattern is semantically inverted — delete should commit immediately; undo should reverse it.
3. The consistency concern (undo only on URLs, not on other CRUD) is valid but acceptable given different interaction context.

---

## Root Cause: The Bug

Current flow in `SongFormViewModel.RemoveUrlAsync` (lines 409–432):

```
1. Remove from UI list only (DB record still exists)
2. Await snackbar 4s
   - If UNDO tapped: calls AddUrlAsync("https://youtu.be/{videoId}")
     → AddUrlAsync finds a DUPLICATE (record was never deleted) → returns (false, "already saved")
     → URL never re-appears in the list
     → undone = true so no DB delete either
     → Result: URL in DB, not in UI → data inconsistency
   - If timer expires: calls RemoveUrlAsync → actual DB delete
```

**The undo button appears to work but silently does nothing. The URL stays in DB without showing in the list.**

---

## UX Assessment (IxD / Nielsen Heuristics)

| Heuristic | Current state | Issue |
|-----------|--------------|-------|
| **H1 — Visibility of system status** | UI shows URL gone; DB has it for 4s | UI lies during the snackbar window |
| **H3 — User control and freedom** | Undo offered but broken | Undo fails silently |
| **H4 — Consistency and standards** | Android Gmail pattern inverted | Standard: commit first, undo reverses |
| **H5 — Error prevention** | No confirmation gate | Acceptable — snackbar is the soft gate |

The intent (snackbar undo for a sub-item removal) is correct for this context. URLs require effort to re-find and re-add, so a recovery window is justified. The implementation just has the commit order backwards.

---

## Consistency Assessment

The concern "undo only in song form, not elsewhere" is valid but **acceptable** for this reason:

- Venue/Artist/Song deletes are **batch operations** from list pages. They navigate away; re-adding requires the same navigation. Snackbar undo is infeasible there.
- URL removal is an **inline action** within the form. The user never leaves the page. Snackbar undo is natural here.

These are structurally different interactions. No need to retrofit undo everywhere. Document this decision in the spec.

---

## Fix: Commit-First, Undo-Reverses (Standard Gmail Pattern)

```
1. Call RemoveUrlAsync immediately (commits DB delete)
2. Remove from UI list
3. Show snackbar: "URL removed [UNDO]" (4s)
4. If UNDO tapped:
   - Call AddUrlAsync to re-insert
   - Add returned DTO back to UI list
   - Show "Removed cancelled" snackbar (brief, no action)
5. If timer expires: nothing to do (already deleted)
```

No race conditions. No deferred commits. No navigation-during-4s risk.

---

## Files to Change

### `SongFormViewModel.cs`
`MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `RemoveUrlAsync` method (lines 409–432)

Replace the deferred-delete pattern with commit-first:

```csharp
private async Task RemoveUrlAsync(SongKaraokeUrlDto dto, CancellationToken ct = default)
{
    if (dto is null || !SongId.HasValue) return;

    var songId = SongId.Value;

    // Commit immediately
    var (success, message) = await _karaokeUrlService.RemoveUrlAsync(songId, dto.VideoId, ct);
    if (!success)
    {
        await _snackbarService.ShowErrorAsync(message);
        return;
    }

    _addedVideoIds.Remove(dto.VideoId);
    RunOnUiThread(() => KaraokeUrls.Remove(dto));

    // Undo reverses the committed delete
    await _snackbarService.ShowWithUndoAsync("URL removed", "UNDO", async () =>
    {
        var rawUrl = $"https://youtu.be/{dto.VideoId}";
        var (reAddSuccess, _, reAdded) = await _karaokeUrlService.AddUrlAsync(songId, rawUrl);
        if (reAddSuccess && reAdded is not null)
        {
            _addedVideoIds.Add(dto.VideoId);
            RunOnUiThread(() =>
            {
                KaraokeUrls.Add(reAdded);
                AddFromSearchCommand.NotifyCanExecuteChanged();
            });
        }
    });
}
```

### `ISnackbarComponent` / `SnackbarComponent.cs`
`MyVocaList/UI/Components/SnackbarComponent.cs` — no interface changes needed. `ShowWithUndoAsync` already works correctly for the undo action. No changes required here.

### Tests
`MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` (or new file if it doesn't exist)

New tests needed (TDD — write first):
- `RemoveUrlAsync_RemovesFromDbImmediately` — verify `RemoveUrlAsync` on service is called before snackbar
- `RemoveUrlAsync_UndoTapped_ReAddsUrl` — mock undo callback fires; verify `AddUrlAsync` called and URL re-appears
- `RemoveUrlAsync_UndoNotTapped_UrlStaysRemoved` — no `AddUrlAsync` call after timeout

### Spec update
`Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/requirements.md`
- AC-1.5: clarify "Tapping ✕ removes the URL immediately; a 4-second snackbar with Undo allows recovery."
- Add a note on the undo-consistency decision (inline vs. page-level deletes differ in context).

---

## What Does NOT Change

- `ISnackbarComponent` interface — no new methods
- `ShowWithUndoAsync` implementation — already correct; caller was the problem
- The rest of the CRUD (Venues, Artists, Songs list-level delete) — no undo there; different interaction model

---

## Verification

1. Run `dotnet test` — confirm existing tests pass; new tests written first (Red → Green)
2. Manual: tap ✕ on a URL → snackbar appears → wait 4s → confirm URL is gone in DB (via SQLite MCP)
3. Manual: tap ✕ → tap UNDO within 4s → confirm URL re-appears in UI and exists in DB
4. Manual: tap ✕ → navigate away during snackbar window → confirm URL is deleted (not orphaned)
