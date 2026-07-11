# Handoff — Artist & Song Form UX Redesign

**Status:** 🔵 Deferred — paused mid-implementation, to be resumed later.
**Parked on branch:** `feature/form-ux-redesign` (pushed to origin). This is the canonical
resume point — it contains **all** progress, including work that predates the branch (see below).
**Date parked:** 2026-07-10.

## How the work is distributed (read before resuming)

Implementation began before a dedicated feature branch existed, so early commits landed directly
on `develop` and were then continued on a checkpoint branch that has since been **renamed** to
`feature/form-ux-redesign`.

- `develop` (== `origin/develop`) carries commits `5a84503..187c9a8`: spec, plan, Phase 0, and
  Phase 1 Tasks 1–5.
- `feature/form-ux-redesign` = those same `develop` commits **plus** commit `5c510e5`
  (REQ-FORMUX-07, ArtistService external-identity persistence), which is the only code that never
  reached `develop`.

**To resume:** check out `feature/form-ux-redesign` and continue from the first unchecked task in
`tasks.md`. When the feature is complete it will be merged back into `develop` (the shared early
commits fast-forward cleanly).

## Progress (per `tasks.md`)

| Phase | State |
|-------|-------|
| Phase 0 — spec supersession notes | ✅ done |
| Phase 1 — Suggestion DTOs, repo collation lookups, ArtistSuggestionService, SongSuggestionService | ✅ done (Tasks 1–5) |
| Phase 1 — ArtistService external-identity fix (REQ-FORMUX-07) | ✅ done (commit `5c510e5`) |
| Phase 1 — DI registration for suggestion services | ⬜ **next task** |
| Phase 2 — `[COMPONENT]` AutocompleteField (governed; Helder pre-approved Gate 4) | ⬜ pending |
| Phase 3 — ViewModels (BUG-027 blur-clear removal + IsArtistLocked retirement; Artist/SongFormViewModel) | ⬜ pending |
| Phase 4 — UI/XAML (ArtistFormPage, SongFormPage) | ⬜ pending |
| Phase 5 — Delete ArtistPickerPage + SongPickerPage | ⬜ pending |
| Phase 6 — docs, guidelines, E2E emulator verification, close-out | ⬜ pending |

**~6 of 14 tasks complete.** Next task: **DI registration for suggestion services**
(`MyVocaList/Extensions/ServiceCollectionExtensions.cs`).

## Spec references
- Requirements / design / plan / tasks: this folder (`2026-07-10-form-ux-redesign/`).
- GAP-1 resolved (Option A) — see `plan.md § GAP-1`.
- Supersedes BUG-008, BUG-027, BUG-029/030/031/032 (see BACKLOG for the deferral notes).
