# Tasks: Deezer artwork in the Song form options and the Songs list

Spec: `requirements.md`, `design.md`. Briefs per task: `plan.md`. Log: `task-log.md`.
Branch `feature/deezer-artwork`, worktree `.claude/worktrees/deezer-artwork`. Tasks run **one at a
time, in order** (one worktree, one writer). DRY Onion order: Domain → Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec
- [x] T0 Evaluation, MD3 evidence (`findings.md`), requirements, design, component records, plan; spec and plan reviews.

## Phase 1 — Domain and Infra
- [ ] T1 `ImageUrl` on `Song` and `Artist`, `ImageUrlRules`, EF configuration, migration `AddImageUrlToSongsAndArtists`. (REQ-ART-03, 05)
- [ ] T2 Read models and projections: `SongListItemDto` (+`Version`, `ImageUrl`, `SupportingText`), `SongSearchHit`, `ArtistListItem`; `SongRepository`, `CatalogRepository`, `ArtistRepository`. Depends on T1. (REQ-ART-06, 10, 19, 20)

## Phase 2 — Services
- [ ] T3 `DeezerProvider` + `MusicSearchResultDto`: map images and album. Depends on T1. (REQ-ART-01, 02, 03, 04)
- [ ] T4 Suggestion DTOs and services carry the images and album. Depends on T2, T3. (REQ-ART-06, 24)
- [ ] T5 `SongFormSaveRequest` + `SongService.SaveFromFormAsync` image rules. Depends on T1. (REQ-ART-15, 16, 17, 18)

## Phase 3 — Shared components (governed; each its own task and record)
- [ ] T6 `ListItemLeadingImage.PlaceholderIcon` + new `ListItemLeadingAvatar`. Record: `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/`. (REQ-ART-22)
- [ ] T7 `AutocompleteSuggestion` leading/trailing properties + `AutocompleteDXField` option row. Depends on T6. Record: `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/`. (REQ-ART-06, 08, 09)

## Phase 4 — Pages (successors of T6/T7)
- [ ] T8 `SongFormViewModel`: option content, pending images, save request. Depends on T4, T5, T7. (REQ-ART-06, 07, 08, 09, 10, 11, 12, 13, 14)
- [ ] T9 `SongsPage.xaml`: leading image, supporting text, 72 dp row. Depends on T2, T6. (REQ-ART-19, 20, 21)

## Phase 5 — Close-out
- [ ] T10 Full test run, Android build, verifier, traceability matrix, governed-list update, follow-up items registered, merge to `develop`.
- [ ] T11 **Device check (Helder)** — checklist in `task-log.md § Device check`. (REQ-ART-21, 22, 23 and the component Gate 3 rows)
