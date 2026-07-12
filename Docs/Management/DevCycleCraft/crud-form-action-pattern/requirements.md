# CRUD Form Action Pattern — Requirements

> BACKLOG ref: `Docs/Management/BACKLOG.md` row 168. Design: `design.md`.

## Story

As a user filling out the Song form, I want Save to live in the top app bar and Cancel to disappear (redundant with the back button), so the form matches the MD3 full-screen pattern instead of showing two competing action buttons in the body.

## Acceptance Criteria

- **AC-1:** `SongFormPage` shows a `ToolbarItem` bound to the existing `SaveCommand` in the native Shell top app bar's trailing slot.
- **AC-2:** The `ToolbarItem`'s enabled state matches `SaveCommand.CanExecute` — disabled exactly when the current inline Save button would have been disabled.
- **AC-3:** Tapping the `ToolbarItem` performs the same save operation as the current inline Save button (same command, same side effects, same navigation-away-on-success behavior).
- **AC-4:** The in-body Cancel button is removed from `SongFormPage.xaml`. No Cancel affordance remains in the form body.
- **AC-5:** The in-body Save button and its wrapping `HorizontalStackLayout` are removed from `SongFormPage.xaml` — no duplicate Save control remains.
- **AC-6:** The native Shell back button remains the sole dismiss/discard action; its behavior (discarding unsaved changes) is unchanged from before this change.
- **AC-7:** `ArtistFormPage.xaml`, `PersonFormPage.xaml`, `VenueFormPage.xaml` are unmodified by this change.
- **AC-8:** `.claude/library/crud-pages.md`'s Form Page section documents the ToolbarItem-Save / no-Cancel pattern as the general law for full-screen CRUD forms, replacing the previously documented inline-button variants, AND notes that Artist/Person/Venue forms are currently non-compliant pending their bottom-sheet conversion decisions (BACKLOG rows 43–45).
- **AC-9:** `.claude/library/m3-components.md` cross-references the updated pattern.

## Validation

- **AC-1–AC-3, AC-6:** manual E2E on SongFormPage (emulator/device) — Level C (XAML-only, no ViewModel logic change) per `testing.md`, so no mandatory automated test; manual smoke test documented in task-log.
- **AC-4, AC-5, AC-7:** verified by reading the committed XAML diff.
- **AC-8, AC-9:** verified by reading the committed rules-file diff.

## Out of scope

See `design.md § Out of scope`.
