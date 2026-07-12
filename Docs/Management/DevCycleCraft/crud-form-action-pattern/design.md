# CRUD Form Action Pattern — MD3 Save/Cancel placement for full-screen forms

> BACKLOG ref: `Docs/Management/BACKLOG.md` row 168 (registered 2026-07-10 by Helder).

## Problem

Full-screen CRUD forms currently show an in-body, right-aligned `HorizontalStackLayout` with `Cancel` (OutlinedButton) + `Save` (FilledButton). This looks off-pattern: Cancel duplicates the back-navigation affordance once a form occupies the whole screen, and Save would read better as a top-app-bar action.

## MD3 Research (mandatory compliance check)

Official MD3 guidance (via WebSearch — `m3.material.io` full-screen-dialog/app-bar guidelines; WebFetch failed, page is JS-rendered) establishes:

- MD3's full-screen-dialog pattern normally pairs an **"X" close icon** (not a back arrow) with an explicit confirming action in the trailing app-bar slot, when the view uses explicit (non-autosave) Save. The back/up arrow is reserved for continuously-autosaved views.
- The confirming action belongs in the **top app bar's trailing slot**, using a descriptive verb ("Save"), and should be disabled until required fields are valid.

MyVocaList's forms are **pushed Shell pages**, not modal full-screen dialogs — the "X vs back arrow" distinction doesn't map cleanly. Decision below deliberately diverges from strict MD3 iconography for this reason (documented, not silently ignored).

## Current-state findings (Explore agent, verified against source)

All four CRUD form pages (`ArtistFormPage`, `PersonFormPage`, `SongFormPage`, `VenueFormPage`) are identical in structure:
- Native Shell top app bar (`Title="{Binding PageTitle}"`), no `Shell.TitleView`/`SmallAppBar`, no existing `ToolbarItem`.
- Inline `HorizontalStackLayout(HorizontalOptions="End", Spacing="8")` with Cancel (`OutlinedButton`) + Save (`FilledButton`), both bound to `CancelCommand`/`SaveCommand`.
- No `IsEnabled` binding on Save in XAML — enable/disable relies solely on `SaveCommand`'s `CanExecute`.
- `SmallAppBar`/`AppBarBase` expose no `IsEnabled`/`ActionNEnabled` bindable property today — irrelevant here since this design does not touch `SmallAppBar`.

## Sequencing override (Helder, 2026-07-12)

BACKLOG row 46 ("Form & Autocomplete UX Overhaul" umbrella, registered 2026-07-11 — one day after row 168) sequences the Song-form AppBar-save change to run **last**, after Venue (row 43), Artist (row 44), and Singer/Person (row 45) bottom-sheet conversions all ship. As of this spec, none of the three predecessor conversions have shipped (all `💡 Pending`).

**Helder explicitly authorized running this spec now, out of that order** (2026-07-12, in response to the spec-reviewer flagging the conflict). This is a deliberate override of row 46's stated dependency, not an oversight — recorded here so a future reader of BACKLOG row 46 or this design doc understands why Song's AppBar-save pattern shipped before its documented predecessors.

## Decision

1. **Scope: `SongFormPage.xaml` only.** `ArtistFormPage` and `VenueFormPage` are registered (BACKLOG rows 43–44) for a separate bottom-sheet/modal conversion; sheets keep in-body Save/Cancel per `crud-pages.md`, so applying this pattern to them now would likely be reverted once they convert. `PersonFormPage`'s conversion is marked a **candidate** in BACKLOG row 45 ("Maybe — evaluate whether the Person form benefits from a sheet"), not a settled decision — if Person form is ultimately *not* converted, it remains full-screen and this pattern (per row 168's "apply to ALL CRUD forms that remain full-screen") will need to be re-applied to it as a follow-up task once that evaluation lands. This spec does not preempt that evaluation.
2. **Mechanism: native Shell `ToolbarItem`**, not `SmallAppBar`/`Shell.TitleView`. Technical driver: `SmallAppBar`/`AppBarBase` expose no `IsEnabled`/`ActionNEnabled` bindable property today, so reusing SmallAppBar's Action-slot pattern for a disabled-until-valid Save would require adding a new BindableProperty to a governed component — itself a change requiring `component-change-governance.md`'s four gates. `ToolbarItem` bound directly to the existing `SaveCommand` avoids that new BindableProperty entirely (inherits `CanExecute`-driven enabled state with zero new binding infrastructure), so no governed-component work is needed as a consequence — not because avoiding governance was the goal in itself.
3. **Leading icon: unchanged.** Native Shell back button stays. No custom "X"/close icon, no discard-confirmation handling. (Diverges from strict MD3 full-screen-dialog iconography — accepted because these are pushed pages with a real navigation stack, not modal dialogs; revisit if/when a form becomes a true modal.)
4. **Cancel button: removed** from the form body. Back button becomes the sole dismiss action.
5. **Inline button block:** the entire `HorizontalStackLayout` (Cancel + Save) is deleted from `SongFormPage.xaml`. Save moves fully to the `ToolbarItem`; nothing remains inline.
6. **Rules update:** `.claude/library/crud-pages.md`'s Form Page section is updated to make "ToolbarItem-Save, no in-body Cancel, back button as sole dismiss" the **general law for any full-screen CRUD form** (present and future), replacing the two currently-documented inline-button variants. The updated section must note that `ArtistFormPage`, `PersonFormPage`, `VenueFormPage` are **currently non-compliant** with this law, pending their bottom-sheet conversion decisions (BACKLOG rows 43–45), so a future reader isn't confused by the rule/code mismatch. `.claude/library/m3-components.md` gets a cross-reference note pointing to this pattern.
7. **Double-tap / in-flight save:** no new race is introduced — the `ToolbarItem` binds the same `SaveCommand` the inline button used, so whatever `CanExecute`/async guard already prevents double-submission on the inline button applies unchanged to the `ToolbarItem`.

## Out of scope

- Artist/Person/Venue forms — untouched until the bottom-sheet conversion decision lands for each.
- Any change to `SmallAppBar`/`AppBarBase` (no governed-component work here).
- Adding an `IsEnabled`/disabled-until-valid pattern beyond what `SaveCommand.CanExecute` already provides — the `ToolbarItem` inherits existing behavior; no new validation logic.
- The separate BACKLOG row 167 (AppBar/SearchAppBar interaction redesign) — unrelated, different pages (CRUD list pages, not forms).

## Testing impact

No new ViewModel logic introduced (same `SaveCommand`/`CancelCommand` reused; `CancelCommand` becomes unused by this page's XAML but is not deleted from the ViewModel unless verified unused elsewhere). XAML-only change — Level C per `testing.md` (no mandatory test). Manual E2E smoke test on SongFormPage: Save via ToolbarItem persists correctly; back button discards unsaved changes as before.

> **Correction (plan-reviewer, 2026-07-12):** the original wording here ("ToolbarItem disabled when required fields are invalid") is stale — plan research confirmed `SaveCommand` (`SongFormViewModel.cs:123`) has no `CanExecute` predicate at all, so there is no disabled-state behavior to verify. Removed from the manual E2E checklist.

## Files touched

- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (implementation)
- `.claude/library/crud-pages.md` (rule update)
- `.claude/library/m3-components.md` (cross-reference note)
- `Docs/Management/BACKLOG.md` (status update, row 168)
