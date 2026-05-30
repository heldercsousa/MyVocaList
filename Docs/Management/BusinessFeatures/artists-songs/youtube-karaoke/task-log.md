# Task Log — YouTube Karaoke

---
## Task: Fix URL remove undo — commit-first pattern [AC-1.5]
**Plan:** Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/plan.md
**Status:** To Review
**Started:** 05/30/2026
**Completed:** 05/30/2026

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — replaced deferred-delete undo pattern with commit-first pattern in `RemoveUrlAsync`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — new test file with 3 TDD tests for `RemoveUrlAsync`
- `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/requirements.md` — updated AC-1.5 text to describe commit-first behavior; added design decision note on undo scope

### Build notes
Build completed with 0 errors (warnings only — pre-existing nullable analysis warnings unrelated to this fix).

### Verification evidence
- Build: PASS — 0 errors
- Tests: PASS — 195 tests total (3 new SongFormViewModelTests all green)
- Post-edit re-read: confirmed `RemoveUrlAsync` calls `_karaokeUrlService.RemoveUrlAsync` before `ShowWithUndoAsync`; undo callback calls `AddUrlAsync` and re-inserts the DTO
- Spec compliance: confirmed — AC-1.5 updated in requirements.md

### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-1.5 | DB delete happens before snackbar | `SongFormViewModel.RemoveUrlAsync` | `RemoveUrlAsync_ValidUrl_CommitsDeleteBeforeSnackbar` |
| AC-1.5 | Undo re-inserts URL in list | `SongFormViewModel.RemoveUrlAsync` | `RemoveUrlAsync_UndoTapped_ReAddsUrlToList` |
| AC-1.5 | Remove failure shows error, keeps URL | `SongFormViewModel.RemoveUrlAsync` | `RemoveUrlAsync_RemoveFails_ShowsErrorAndKeepsUrl` |
