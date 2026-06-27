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
