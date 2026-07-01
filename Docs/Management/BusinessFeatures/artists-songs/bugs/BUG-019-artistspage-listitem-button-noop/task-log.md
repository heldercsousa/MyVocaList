---
## Task: BUG-019 — ArtistsPage list item trailing button noop + artist name invisible
**Status:** To Review
**Started:** 2026-06-30
**Completed:** 2026-06-30

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
