---
## Task: BUG-017 -- Replace navigate_next icon with arrow_forward_outlined
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/
**Status:** Emulator-verified 2026-07-03 (TEST-008, `Docs/Management/EMULATOR_TEST_MASTER_LIST.md`) — no Glide FileNotFoundException, icon renders cleanly
**Started:** 2026-06-27
**Completed:** 2026-06-27

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` -- replaced navigate_next -> arrow_forward_outlined (1 occurrence, line 72)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` -- replaced navigate_next -> arrow_forward_outlined (2 occurrences, lines 78 and 171)

### Build notes
SVG confirmed present before any edits: `MyVocaList/Resources/Images/arrow_forward_outlined.svg`.
All 3 occurrences replaced (ArtistFormPage.xaml:72, SongFormPage.xaml:78 and 171).
Transient XAWAS7024 file-lock errors appeared during intermediate builds (`.NET Host` holding `lib_System.Diagnostics.Debug.dll.so`) — these were caused by a concurrent dotnet process, not by the XAML change. Final clean build passed (exit code 0).

### Verification evidence
- Build: PASS (exit code 0 on final clean build; prior failures were transient file locks unrelated to this change)
- Tests: PASS (357 tests, 0 failures)
- Post-edit re-read: confirmed — all 3 occurrences show `arrow_forward_outlined`
- Spec compliance: N/A — icon-only XAML fix, no spec file required (Minor bug, cosmetic fix)

### Manual E2E verification (Major bug -- UI-only, not unit-testable)
Helder: run on emulator after next successful build
1. Open ArtistFormPage (edit any artist) -> verify no Glide FileNotFoundException in logcat
2. Open SongFormPage (edit any song) -> verify no Glide FileNotFoundException in logcat
3. Confirm arrow_forward_outlined icon renders visibly on both pages (navigate forward arrow)
4. Navigate forward from both pages using the arrow icon to confirm tap targets still work
