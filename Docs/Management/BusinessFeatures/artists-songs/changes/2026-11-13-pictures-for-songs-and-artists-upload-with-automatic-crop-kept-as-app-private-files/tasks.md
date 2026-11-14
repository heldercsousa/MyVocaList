# Tasks: pictures and tap-to-edit names in the Song and Artist forms

Spec: `requirements.md`, `design.md`. Log: `task-log.md`.
Branch `feature/form-pictures` (worktree `.claude/worktrees/form-pictures`); parallel tasks use
`feature/form-pictures-ui` (worktree `.claude/worktrees/form-pictures-ui`), merged into
`feature/form-pictures` at the end of each wave. DRY Onion order: Domain → Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Items

| Key | Backlog item | Tasks |
|-----|--------------|-------|
| PIC | `artists-songs/changes/2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/` (this folder) | T0, T1, T3, T9, T10 |
| TTE | `DevCycleCraft/autocomplete-component/changes/2026-11-13-new-component-taptoeditfield-a-defined-value-shows-as-text-a-tap-opens-its-editor/` | T2a |
| IPF | `DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/` | T2b |
| SONG | `artists-songs/changes/2026-11-13-song-form-picture-at-the-top-title-and-artist-as-tap-to-edit-text-artist-editable-in-edit-mode/` | T4, T5 |
| ARTIST | `artists-songs/changes/2026-11-13-artist-form-picture-at-the-top-and-name-as-tap-to-edit-text/` and `artists-songs/changes/2026-11-11-artistformpage-name-as-the-autocomplete-field-split-from-sft-6-3/` | T6, T7 |
| LIST | `artists-songs/changes/2026-11-13-artists-list-rows-show-the-artist-picture-as-a-leading-avatar/` | T8 |

## Phase 0 — Spec
- [x] T0 Research (DevExpress `ImageEdit`, .NET MAUI 10 `MediaPicker`, `Microsoft.Maui.Graphics`), code map, requirements, design, backlog items.

## Wave 1 (parallel, two worktrees)
- [x] T1 **Image infrastructure.** `ImageReferenceRules`, `ImageSize`, `ImageOwnerKind`, `IImageStore` + `ImageStore`, `IImageProcessor` + `PlatformImageProcessor`, `IImageMaintenanceService` + service, `GetLocalImageReferencesAsync` on both repositories, `IPhotoPicker` + `MauiPhotoPicker`, `AppImagePaths`, `ImageReferenceConverter`, DI registration, start-up sweep, Android permission, iOS usage text. (REQ-PIC-01..11) — worktree `form-pictures`
- [x] T2a **`TapToEditField`** + `TapToEditFieldLogic` + `Headline.Medium` style. (REQ-PIC-23..26, 30) — worktree `form-pictures-ui`
- [x] T2b **`ImagePickerField`**. After T2a, same worktree. (REQ-PIC-13..16)

## Wave 2 (parallel, two worktrees; after Wave 1 is merged)
- [x] T3 **Services.** `SongFormSaveRequest.ImageChanged`, `SongService` (local references, edit-mode picture, edit-mode artist collision, artist image on an artist created in edit mode), `IArtistService.GetArtistByIdAsync` / `SetArtistImageAsync`, `ArtistSuggestionService` local pictures. Depends on T1. (REQ-PIC-02, 21, 22, 32, 34, 36) — worktree `form-pictures`
- [~] T8 **Lists.** `ArtistsPage.xaml` leading avatar + trailing checkbox; `SongsPage.xaml` through the converter; `ListItemLeadingAvatar` added to the governed list. Depends on T1. (REQ-PIC-38, 39) — worktree `form-pictures-ui`

## Wave 3 (parallel, two worktrees; after Wave 2 is merged)
- [ ] T4 **`SongFormViewModel`**: form picture, pick / remove, save request, releasing picks, option pictures. Depends on T1, T3. (REQ-PIC-17..22, 27, 28, 31, 39) — worktree `form-pictures`
- [ ] T5 **`SongFormPage.xaml`**: wrappers and picture field. After T4, same worktree. Depends on T2a, T2b. (REQ-PIC-12, 23..26)
- [ ] T6 **`ArtistFormViewModel`**: picture, Deezer pick keeps the picture, option avatars, save. Depends on T1, T3. (REQ-PIC-34..37) — worktree `form-pictures-ui`
- [ ] T7 **`ArtistFormPage.xaml`**: Name as the autocomplete field in the wrapper, picture field; both new components added to the governed list. After T6, same worktree. (REQ-PIC-12, 33)

## Close-out
- [ ] T9 Merge, full test run, Android build, verifier, traceability matrix, statuses, merge to `develop`.
- [ ] T10 **Device check (Helder)** — checklist in `task-log.md § Device check`.
