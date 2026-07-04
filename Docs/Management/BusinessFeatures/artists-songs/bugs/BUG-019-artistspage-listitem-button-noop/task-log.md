---
## Task: BUG-019 — ArtistsPage list item trailing button noop + artist name invisible
**Status:** Regression found — emulator smoke test 2026-07-03
**Started:** 2026-06-30
**Completed:** 2026-06-30

### Regression — 2026-07-03 emulator retest
Helder's smoke test (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-009 and Phase 16C.1 error "16C.1 - 1)") found the **artist name is now visible** (this fix holds), but tapping the trailing `queue_music_outlined` button still does **not** navigate to the artist's Catalog page — the noop symptom this bug was meant to close has returned. Tracked as **BUG-028** (see `BACKLOG.md` Artists & Songs Catalog row) pending root-cause investigation — do not reopen this file's original fix; register the new investigation under BUG-028.

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — updated x:DataType in both DataTemplates from `dto:ArtistListItemDto` to `domain:ArtistListItem`; added `xmlns:domain` namespace declaration for `MyVocaList.Domain.ReadModels`

### Build notes
Build: PASS — 0 errors after XAML type correction.

### Verification evidence
- Build: PASS
- Tests: PASS
- Post-edit re-read: confirmed
- Spec compliance: N/A — bug fix, no spec file

### Manual E2E verification (required — UI-only Major bug)
1. Deploy to emulator
2. Navigate to Artists page
3. Verify artist names are visible in list items (previously blank due to failed compiled binding cast)
4. Tap the queue_music icon on any artist row → verify navigates to SongsPage filtered by that artist (previously noop due to CommandParameter resolving to null)
⏳ Helder: perform this emulator smoke test
