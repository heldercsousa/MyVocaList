---
## Task: Remove dead search code after confirming nothing uses it (B12, B11, B1, B2)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-remove-dead-search-code-after-confirming-nothing-uses-it/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Grep proof (recorded before removal, `*.cs` and `*.xaml`, excluding obj/bin)
- `SearchArtistsCommand`, `ArtistSuggestions`, `_searchGeneration`, `SearchArtistsAsync`: referenced only in `MyVocaList/UI/ViewModels/SongFormViewModel.cs` and `Tests/Unit/ViewModels/SongFormViewModelTests.cs`. **No `.xaml` file references them** (the Artist fields bind `ArtistOptionSource`, `SongFormPage.xaml:123`, `SongFormSfPage.xaml:277`).
- Live path confirmed: `ArtistOptionSource` → `SearchArtistOptionsAsync` → `SearchArtistsCoreAsync` (kept).
- `YouTubeSearchViewModel.SearchAsync` cancel-previous: unreachable under `AllowConcurrentExecutions = false`.

### Changes
- B12 (SongFormViewModel): removed `SearchArtistsCommand` (+ init), `SearchArtistsAsync`, `_searchGeneration`, the `ArtistSuggestions` observable and all its dead writes. `SearchArtistsCoreAsync` now returns only (no observable state).
- B1: `SearchArtistOptionsAsync` forwards its `CancellationToken` to `SearchArtistsCoreAsync` (new optional `ct` parameter) and on to `SearchArtistsByNameAsync` (already accepts `ct`).
- B2: literal `maxResults: 5` → `SearchConstants.MaxSearchResults`.
- B11 (YouTubeSearchViewModel): removed the dead per-search `Cancel`/`Dispose`/re-create; the page `Dispose` still cancels the source.
- Tidy: `OnArtistBlurredWithoutSelection` rewritten as an early return + restore (same behaviour as before: no selection keeps text; a prior selection restores its name and closes the drop-down).

### Test changes (dead-code-only, allowed by the brief; listed per item)
- Removed `SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins` (BUG-051 test: drove the removed command and observable).
- Removed the `Assert.Empty(sut.ArtistSuggestions)` line in `RemotePick_ClosesDropdowns` (asserted the removed observable).
- Kept `SearchArtistsCoreAsync_ReturnsCurrentQueryResultsDirectly` (BUG-056; tests the live return path).
- No other test edited.

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`
- `Tests/Unit/ViewModels/SongFormViewModelTests.cs`
- `MyVocaList.sln` (registration)
- `<this folder>/task-log.md` (new)

### Verification evidence
- `dotnet test --filter SongFormViewModel|YouTubeSearch`: 252 passed, 0 failed.

### Build notes
Build: see Android build in commit | Tests: 252 passed (filtered) | Commit SHA: see git log
Files written and re-read: `SongFormViewModel.cs` L380-400 (search core), L440-456 (blur handler); `YouTubeSearchViewModel.cs` L51-60.

### Round 2 (coordinator request): B1 cancellation and the live out-of-order path
- `SearchArtistsCoreAsync_CancelledToken_CancelsTheServiceRead` (B1). Red: with the token replaced by `CancellationToken.None` in the service read, the test FAILED (TimeoutException: service read never cancelled). Restored.
- `ArtistOptionSearch_OutOfOrderCompletion_LatestQueryWins` (REQ-ACREATE-13 on the live `ArtistOptionSource` path, replacing the removed BUG-051 test). The superseded read honours its token; the newer answer is shown, the late older answer is dropped (cancelled). Green (SongForm suite passes). Red not separately demonstrated for this test.
