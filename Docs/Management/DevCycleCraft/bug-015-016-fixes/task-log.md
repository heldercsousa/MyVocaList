---
## Task: BUG-016 — Fix SongsPage FAB crash (route collision)
**Plan:** Docs/Management/DevCycleCraft/bug-015-016-fixes/plan.md
**Status:** To Review
**Started:** 06/27/2026
**Completed:** 06/27/2026

### Changed files:
- `MyVocaList/Navigation/Routes.cs` — added `QueueSongPicker = "queue-song-picker"` constant
- `MyVocaList/AppShell.xaml` — renamed QueueSongPickerPage FlyoutItem route from "song-picker" to "queue-song-picker"
- `MyVocaList/UI/ViewModels/QueueManagementViewModel.cs` — updated `SelectSongAsync` GoToAsync call to use `Routes.QueueSongPicker` instead of hardcoded "song-picker"
- `MyVocaList.Tests/Unit/Infrastructure/RouteCollisionTests.cs` — regression test (Critical severity — MANDATORY)

### Verification evidence
- Build: PASS — 0 errors, 13 warnings (pre-existing; SQLitePCLRaw vulnerability warnings + DX1001 license notice)
- Tests: PASS — 358 tests, 0 failures (RouteCollisionTests.Routes_QueueSongPicker_IsDistinctFromSongPicker confirmed PASS)
- Post-edit re-read: confirmed — all 4 changed files reviewed
- Spec compliance: confirmed — BUG-016 route collision resolved; QueueSongPickerPage route "queue-song-picker" is now distinct from SongPickerPage route "song-picker"

### Red phase evidence
Build error before fix (Red):
```
RouteCollisionTests.cs(11,51): error CS0117: "Routes" does not contain a definition for "QueueSongPicker"
```
Test passed after fix (Green):
```
Aprovado! – Com falha: 0, Aprovado: 1, Total: 1
```

### AC traceability
| AC ID | Criterion | Implementation | Test method |
|-------|-----------|----------------|-------------|
| BUG-016 | QueueSongPickerPage route distinct from SongPickerPage route | Routes.cs `QueueSongPicker = "queue-song-picker"` constant; AppShell.xaml route renamed; QueueManagementViewModel GoToAsync updated | RouteCollisionTests.Routes_QueueSongPicker_IsDistinctFromSongPicker |
