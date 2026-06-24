# Handoff — Artists & Songs Phase 16C Gate

**Created:** 2026-06-20
**Status:** Phases 1–16B complete. Awaiting Helder emulator smoke tests before 16C.2–16C.5.

---

## Exact resume point (next session, after Helder's smoke tests)

**Both smoke tests must be green before this session proceeds.**

Check Helder's confirmation in conversation, then run steps in order:

1. `dotnet build` — confirm 0 errors (Phase 16C.2)
2. `/project:review` (Phase 16C.3)
3. Update `Docs/Changelog/changelog.md` (Phase 16C.4)
4. `/project:commit` (Phase 16C.5)
5. Update `Docs/Management/BACKLOG.md`:
   - Artists & Songs → ✅ Done
   - Song Import & Entity Resolution → ✅ Done
6. Spec cleanup (see below)

---

## Pending smoke tests (Helder gates)

### Gate 1 — Song Import Wave 5.2 (5 steps)
`Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md § Wave 5`

1. API search (Song Picker) → select result → form populates; save → ExternalProvider/ExternalId persisted.
2. Manually enter song with title nearly matching existing one → resolution BottomSheet surfaces fuzzy candidate; choose **Update existing** vs **Save as new version** (version required).
3. Edit song (set HasManualEdits), re-import same from API → merge BottomSheet shows per-field diff; accept some → only those change.
4. New Song: add YouTube URL before saving → no "save first" error; song + URL save atomically.
5. Save failure shows error snackbar. Double-tap "Search music database" does not crash. Picker pages show single back arrow. Artist field is autocomplete-only, clears on blur-without-selection, locks on API origin.

### Gate 2 — Phase 16C.1 (10 steps)
`Docs/Management/BusinessFeatures/artists-songs/tasks.md § Phase 16C`

1. Single "Artists" menu item navigates to ArtistsPage; filter chips (Authors/Performers) work.
2. Register artist; verify no songs required.
3. Register song from global Songs page; verify title uniqueness.
4. API search strip works on ArtistFormPage and SongFormPage.
5. Add song to artist's Catalog via trailing button → Catalog page → FAB picker.
6. Remove song from Catalog; verify song still exists in global list.
7. Delete artist; verify songs not deleted; Catalog entries gone.
8. Delete song; verify it disappears from all Catalogs.
9. Edit song; verify Lyrics field visible and saveable.
10. Verify Songs menu item in flyout; search back arrow shows correctly.

---

## Spec cleanup (do after 16C.3 review, before 16C.5 commit)

These divergences were found in Phase 2 reconciliation (2026-06-20):

| File | What to fix |
|------|-------------|
| `requirements.md` AC-1.16 | Change "top tab bar with three tabs" → "FilterChipGroup with two chips (Authors/Performers); deselecting both = All" |
| `requirements.md` Overview (Artist Roles) | Remove two-entry menu table; replace with "Single 'Artists' menu entry; role filter exposed via chips on the page" |
| `design.md` Page Structure (ArtistsPage) | Change "Top tab bar (DXTabView...)" → `dxe:FilterChipGroup` |
| `design.md` AppShell code block | Replace the three-item Catalog group with single "Artists" entry (matches Phase 16A.2 outcome) |
| `design.md` Song entity | Add `Version string?` property (Song Import Wave 1.1) |
| `design.md` SongConfiguration | Update unique index to `IX_Songs_ArtistId_Title_Version` (3-col, Song Import Wave 2.2) |

The ISongService/ISongRepository/IArtistRepository extension methods (Song Import fuzzy/resolution additions) are tracked in `song-import-resolution/design.md` — no change needed in the parent design.md unless a cross-reference note is wanted.

---

## Build state at handoff

**Verified 2026-06-20 (Debug, `develop`):** 0 errors · 118 warnings (all pre-existing nullable CS8618/CS8620/CS8600 in test files — not regressions). 354 tests passing as of Song Import merge (2026-06-19).

---

## Files to check at session start (Rule 7)

1. This file (resume point above)
2. `Docs/Management/BACKLOG.md` (Artists & Songs + Song Import rows)
3. `Docs/Management/BusinessFeatures/artists-songs/tasks.md` (Phase 16C checkboxes)
4. `Docs/Management/BusinessFeatures/artists-songs/song-import-resolution/tasks.md` (Wave 5.2)
