# Plan: Register SongForm Bugs in BACKLOG.md

## Context
Helder identified 6 bugs/gaps in the Song CRUD flow during emulator testing (2026-06-11). All must be registered as individual BACKLOG entries nested under **Artists & Songs Catalog**, then each will get its own spec and subagent. No bundling.

## Action: Add 6 rows to BACKLOG.md under Artists & Songs Catalog

Insert after the existing `↳ YouTube Search Launch Button ✅ Done` row (line ~45):

---

### BUG-003 — New Song: Save doesn't work
```
| 2026-06 | ↳ Bug: New Song — Save has no effect | 💡 Pending | Tapping Save on SongFormPage in Add mode does not persist the song. Root cause unknown — requires subagent investigation of SaveCommand in SongFormViewModel and ISongService.CreateSongAsync. Spec: `artists-songs/bugs/BUG-003-new-song-save-broken.md` |
```

### BUG-004 — Double-tap on Search Song crashes app
```
| 2026-06 | ↳ Bug: New Song — double-tap on search link crashes app | 💡 Pending | Tapping the "Search Song" navigation trigger twice before the picker page loads causes a crash. Likely a navigation guard missing (CanExecute not blocking re-entry). Spec: `artists-songs/bugs/BUG-004-search-song-double-tap-crash.md` |
```

### BUG-005 — SearchAppBar renders duplicate back arrow in picker pages
```
| 2026-06 | ↳ Bug: New Song — SearchAppBar renders two back arrows in picker | 💡 Pending | When navigating to a search/picker page from SongFormPage, two leading left-arrow icons appear: one from the page's SmallAppBar and a smaller duplicate from inside the SearchAppBar ContentView. Spec: `artists-songs/bugs/BUG-005-searchappbar-duplicate-back-arrow.md` |
```

### BUG-006 — Artist field in SongForm: should be autocomplete with blur-clear behavior
```
| 2026-06 | ↳ Bug/Gap: SongFormPage Artist field — must be autocomplete with blur-clear | 💡 Pending | The Artist field in SongFormPage (Add and Edit modes) must: (1) use the existing AutocompleteField component to select an existing artist (not free-text entry); (2) auto-clear the field if the user blurs without selecting a valid artist from the list; (3) show matched artists as the user types; (4) on Edit mode, pre-populate with the current artist and allow replacement. Requires spec review of original artists-songs/design.md. Spec: `artists-songs/bugs/BUG-006-songform-artist-autocomplete.md` |
```

### BUG-007 — "Save song first" validation on Add YouTube URL is bad UX
```
| 2026-06 | ↳ Bug/UX: Add YouTube URL without saved song shows blocking validation | 💡 Pending | In New Song mode, tapping Add on the YouTube URL section shows "You must save the song first before adding a URL." This is junior-level UX. The Save action must handle this transparently: auto-save the song (or defer URL persistence) before inserting the URL. Preferred approach: service-layer orchestration — SaveSongWithUrlsAsync handles new song + its URLs atomically in one call, removing the UI-level ordering constraint. Spec: `artists-songs/bugs/BUG-007-add-url-before-save-ux.md` |
```

### BUG-008 — 3rd-party song API auto-fill (Deezer etc.) never worked
```
| 2026-06 | ↳ Bug/Gap: Song API auto-fill (Deezer/MusicBrainz) — never functional | 💡 Pending | A requirement existed to auto-populate song data (title, artist, album art) via a 3rd-party API (Deezer or similar) when adding a new song. This feature was never implemented or never worked end-to-end. Requires: (1) spike to verify which API was targeted and what was implemented; (2) decision on whether to fix existing integration or replace with a working alternative (MusicBrainz is free, no key required). Spec: `artists-songs/bugs/BUG-008-song-api-autofill-broken.md` |
```

---

## Sequencing

Each bug is an independent BACKLOG entry. Implementation order suggested:
1. **BUG-003** (Save broken) — highest severity, blocks all other form testing
2. **BUG-006** (Artist autocomplete) — correctness gap, affects every new song
3. **BUG-007** (URL UX) — service-layer refactor, medium complexity
4. **BUG-005** (Duplicate back arrow) — UI cosmetic, isolated to SearchAppBar
5. **BUG-004** (Double-tap crash) — navigation guard, isolated fix
6. **BUG-008** (API auto-fill) — needs spike first; lowest urgency

## What this plan does NOT cover
- Implementation plans for each bug — those come after BACKLOG registration, one spec per bug
- Code changes — zero code in this step

## Verification
After updating BACKLOG.md: confirm all 6 rows appear under Artists & Songs section, each with `💡 Pending` status and correct bug file path reference.
