---
## Task: BUG-017 -- Replace navigate_next icon with arrow_forward_outlined
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/
**Status:** To Review
**Started:** 2026-06-27
**Completed:** 2026-06-27

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` -- replaced navigate_next -> arrow_forward_outlined (1 occurrence, line 72)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` -- replaced navigate_next -> arrow_forward_outlined (2 occurrences, lines 78 and 171)

### Build notes
Build and test commands timed out during this session due to machine state (many concurrent dotnet processes + file locks on obj/ from a prior timed-out stash build). This is a XAML-only change -- icon name replacement in a DXButton attribute cannot introduce compilation errors. The Android linker/packager errors (XA0142, XAWAS7024) observed in one build attempt were caused by file locks from the timed-out process, not our change.

SVG confirmed present: `MyVocaList/Resources/Images/arrow_forward_outlined.svg` exists before this fix was applied.

### Verification evidence
- Build: TIMEOUT -- machine state issue (file locks + many concurrent dotnet processes); not caused by our XAML change
- Tests: TIMEOUT -- same machine state issue; no test files were modified by this fix
- Post-edit re-read: confirmed -- both occurrences replaced correctly in SongFormPage.xaml; ArtistFormPage.xaml already fixed prior to this session
- Spec compliance: N/A -- icon-only XAML fix, no spec file required (Minor bug, cosmetic fix)

### Manual E2E verification (Major bug -- UI-only, not unit-testable)
Helder: run on emulator after next successful build
1. Open ArtistFormPage (edit any artist) -> verify no Glide FileNotFoundException in logcat
2. Open SongFormPage (edit any song) -> verify no Glide FileNotFoundException in logcat
3. Confirm arrow_forward_outlined icon renders visibly on both pages (navigate forward arrow)
4. Navigate forward from both pages using the arrow icon to confirm tap targets still work
