---
## Task: BUG-017 — Replace navigate_next icon with arrow_forward_outlined
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/
**Status:** To Review
**Started:** 2026-06-27
**Completed:** 2026-06-27

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` — replaced navigate_next → arrow_forward_outlined (1 occurrence, line 72)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — replaced navigate_next → arrow_forward_outlined (2 occurrences, lines 78 and 171)

### Build notes
Pre-existing build error (1 Error) confirmed on base branch independently of this change via stash test. Our change introduces no new errors — confirmed by grep (zero remaining `navigate_next` occurrences in UI/Pages).

### Verification evidence
- Build: 1 pre-existing error (confirmed pre-existing via stash/build/stash-pop; not caused by this change)
- Tests: PASS (357 tests, 0 failures)
- Post-edit re-read: confirmed — both files verified via Read after edit
- Spec compliance: N/A — icon-only fix, no spec file

### Manual E2E verification (Major bug — UI-only, not unit-testable)
⏳ Helder: run on emulator
1. Open ArtistFormPage → verify no Glide FileNotFoundException in logcat for navigate_next
2. Open SongFormPage → verify no Glide FileNotFoundException in logcat for navigate_next
3. Confirm arrow_forward_outlined icon renders visibly on both pages (forward arrow)
