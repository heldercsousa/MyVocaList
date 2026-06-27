---
## Task: BUG-015 — Fix ArtistsPage trailing button RelativeSource binding
**Status:** To Review
**Started:** 2026-06-27
**Completed:** 2026-06-27

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — replaced RelativeSource AncestorType binding with x:Reference binding on trailing button Command in both ItemTemplate and SelectedItemTemplate

### Build notes
MyVocaList.dll (C# + XAML compilation) built successfully. A concurrent build process held a file lock on an Android packaging artifact (`lib_Microsoft.CSharp.dll.so`), causing the Android packaging step to fail with XAWAS7024. This is unrelated to the XAML change. Single-process build result: PASS (see Verification evidence).

### Verification evidence
- Build: PASS — 0 errors, 16 warnings (all pre-existing: DevExpress trial + NuGet vulnerability advisories)
- Tests: SKIPPED (no .cs files changed)
- Post-edit re-read: confirmed — both trailing button bindings updated in ItemTemplate and SelectedItemTemplate
- Spec compliance: confirmed — BUG-015 fix approach Option A applied; SafeAreaEdges="Container" present; no DX components replaced; English only

### E2E verification (manual, emulator)
1. Launch app on Android emulator
2. Navigate to Artists page
3. Tap the trailing icon button (queue_music_outlined) on any artist row
4. Verify: navigates to SongsPage filtered for that artist (catalog mode)
5. Expected: Songs list shows only songs by the selected artist

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
