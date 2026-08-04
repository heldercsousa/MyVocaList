# BACKLOG Archive — 2026-08

> Closed backlog rows completed in 2026-08, moved out of `Docs/Management/BACKLOG.md`. Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: make song creation possible again. Gate: fixed via the DX `AutoCompleteEdit` replacement plus the Song artist-field correctness work; all three symptoms verify clean. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-027-songformpage-artist-field-broken/`. |
| 2026-07-21 | BUG-050: Song form — selecting an artist suggestion does not lock the field (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: picking a suggestion must lock the Artist field. Root cause: `SelectArtist` never sets `IsArtistLocked=true` (one-line omission). Found in DX-AC T7. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-suggestion-not-locked/`. |
| 2026-07-21 | BUG-051: Song form — artist autocomplete returns stale results (searches prior keystroke) (Major) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: dropdown must reflect the current query. Root cause: shared `ArtistSuggestions` race, no per-request cancellation in `SearchArtistsAsync`. Found in DX-AC T7 (W2 realized). Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-051-autocomplete-stale-results/`. |
| 2026-07-21 | BUG-052: Song form — editing a saved song shows an empty Artist field (Major) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: edit mode must hydrate the saved artist. Likely compound with BUG-050 (song saved without ArtistId); reconfirm after BUG-050 and BUG-051. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-052-edit-shows-empty-artist-field/`. |
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **Workflow & Folder Layout Alignment** | ✅ Done | Goal: resolve SDD/skills/custom-rules conflicts and canonicalize the Docs/ layout. Pointer: `DevCycleCraft/workflow-folder-layout-alignment/`. |
| 2026-07-10 | **AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar** | ✅ Done | Goal: kill the bar-swap search toggle — persistent MD3 `SearchBar` hosted in `CrudListView`. Gate: Helder — confirm D-1 (SearchAppBar survives for 4 picker pages) + emulator smoke test before ✅. Pointer: `DevCycleCraft/appbar-searchbar-redesign/`. |
| 2026-07-11 | **Autocomplete Component — Evaluation, Rebuild & Rollout** | 🔵 Superseded (closed 2026-08) | Goal: make the app autocomplete MD3-compliant — evaluation, component build and rollout. Pointer: `DevCycleCraft/autocomplete-component/`. |
| 2026-07-09 | **Spec Evolution, Versioning & Feature-Folder Organization** | ✅ Done | Goal: shipped specs become immutable history; bugs/changes get dated nested folders (`bugs/`, `changes/`); BACKLOG becomes generated from folder frontmatter instead of hand-maintained. Pointer: `DevCycleCraft/spec-evolution-versioning/`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
