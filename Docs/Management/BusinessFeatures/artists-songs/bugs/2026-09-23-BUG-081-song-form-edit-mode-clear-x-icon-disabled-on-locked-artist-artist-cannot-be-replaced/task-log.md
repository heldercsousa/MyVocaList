---
## Task: Fix BUG-081 — song form clear (X) icon disabled on locked artist in edit mode
**Plan:** (bug fix — no plan.md; commit message + this task-log are the spec, per `.claude/rules/bug-tracking.md`)
**Status:** To Review
**Started:** 2026-09-24
**Completed:** 2026-09-24

### Root cause

`SongFormPage.xaml`'s `artistEdit` (`dxe:AutoCompleteEdit`) bound `IsReadOnly="{Binding IsArtistLocked}"`,
and edit-mode hydration sets `IsArtistLocked = true` (`SongFormViewModel.cs:477`). DevExpress's
`IsReadOnly` disables the whole editor's interactive surface, not just typed text — confirmed via
Context7 (DX MAUI 25.2.x docs, `/websites/devexpress_maui`): the analogous `HTML Edit` control
documents two distinct properties for exactly this split — `AllowUserInput=false` disables only the
text area while leaving the toolbar (icons) usable, whereas `IsReadOnly=true` "disables editing in
both the text input area **and** the toolbar." `AutoCompleteEdit` has no `AllowUserInput`-equivalent
split property (confirmed via the full `AutoCompleteEdit` member list query and a
`search_maui_demo_app_code` search for `IsReadOnly` + `ClearIconVisibility`/`Clear`, no results) — so
binding `IsReadOnly` to the lock necessarily also disabled the `ClearIconClicked` affordance, which is
the only unlock control per REQ-ACREATE-15.

This was introduced by `cb78f3e4` (2026-07-22, "fix: SongFormPage — BUG-054b/057/058 Artist
AutoCompleteEdit wiring"), which switched the lock mechanism from `IsEnabled` (BUG-054b: also killed
the clear icon) to `IsReadOnly` + `ClearIconVisibility="Auto"` — the commit's own task-log explicitly
deferred manual E2E confirmation of this exact combination ("manual E2E deferred to T10 re-run"), so
the icon-still-disabled regression went unverified until Helder's 2026-09-23 on-device check
registered it as BUG-081.

The DX constraint also matches the spec's own wording: REQ-ACREATE-15 says the restore-prior-selection
blur branch "applies only when the user re-typed over an existing lock **without clearing**" — i.e.
the spec always anticipated the locked field being typed over directly (and relying on VM-side
blur-restore / `suppressCreateRow` gating, not `IsReadOnly`, to keep the lock meaningful). Making the
field literally non-editable was an over-restriction that happened to also disable the icon.

### Fix

Removed the `IsReadOnly="{Binding IsArtistLocked}"` binding from `artistEdit` in `SongFormPage.xaml`.
`ClearIconVisibility="Auto"` is kept. No other binding to `IsArtistLocked` existed in the XAML (grep
confirmed). Lock semantics remain enforced entirely in the ViewModel/code-behind, unchanged by this
fix:
- `SongFormPage.xaml.cs` `OnArtistItemsRequested` still uses `ViewModel.IsArtistLocked` to suppress the
  "create new artist" suggestion row while locked.
- `SongFormViewModel.OnArtistBlurredWithoutSelection` still restores the prior artist name on blur
  when `SelectedArtistId` is still set (i.e. the user typed over the lock but never selected/cleared).
- `SongFormViewModel.ClearArtist` (wired to `ClearIconClicked`) still fully unlocks and clears the
  selection — now reachable again because the icon is enabled.

No ViewModel code changed — this is a pure XAML/DX-rendering fix, so no new VM-seam test was required
(no VM behavior changed). All pre-existing REQ-ACREATE-15 VM tests
(`SongFormViewModelTests.cs` — `ClearArtist_WhenLocked_UnlocksAndClearsSelection`,
`ClearArtist_ClosesDropdown`, `ArtistBlurredWithoutSelection_AfterDeliberateClear_DoesNotRestorePriorArtist`)
remain green, untouched.

### AC traceability

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|--------------|
| REQ-ACREATE-15 | Tapping the clear (X) icon on a locked field unlocks it and clears the selection; the icon must be reachable/enabled in the locked (edit-mode) state | `SongFormPage.xaml` (`IsReadOnly` binding removed from `artistEdit`); `SongFormViewModel.ClearArtist` (unchanged) | Existing `ClearArtist_WhenLocked_UnlocksAndClearsSelection` (VM-seam, unchanged, still green) + manual E2E below (icon enablement is a DX rendering concern, not testable at the VM seam) |

### Manual E2E steps for Helder

1. Open an existing song for edit (one with an artist already assigned).
2. Confirm the Artist field shows the persisted artist name.
3. Confirm the clear (X) icon at the right of the Artist field is now visibly enabled (not greyed out)
   and tappable.
4. Tap the X icon — confirm the field empties and becomes a normal searchable field (placeholder
   "Search artists..." shows).
5. Type a different artist name, pick a suggestion (or inline-create a new one), Save.
6. Reopen the song — confirm the newly picked artist is now shown (REQ-ACREATE-16 persistence, unaffected
   by this fix but exercises the same path).
7. Separately, confirm BUG-069 (dropdown auto-opens on edit-load / re-opens after pick) still reproduces
   unchanged — this fix does not touch dropdown-open behavior (`IsDropDownOpen` binding untouched).

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`

### Build notes
Build: passed (0 errors, 8 pre-existing warnings unrelated to this change) — `dotnet build` on
`net10.0-android` (and full multi-target build via `dotnet test`'s restore/build).
Tests: 634 passed, 0 failed, 0 skipped, Total 634 (`dotnet test`, single run — no BUG-076 flake
observed this run).
Files written and re-read: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (re-read after edit — the
`IsReadOnly` line is gone, `ClearIconVisibility="Auto"` and all other bindings intact).

### Design concern (not fixed here — out of scope)

`IsArtistLocked` is now purely a VM/code-behind signal (suggestion suppression + blur-restore trigger)
with no visual "locked" affordance in the UI beyond the pre-filled text — a user has no visual cue that
retyping over the field will be reverted on blur unless they clear it first via X. This matches
REQ-ACREATE-15's literal wording but may be worth a follow-up UX pass (e.g. a subtle lock icon/affix)
if Helder finds it confusing on device. Not fixed here — no AC currently requires a visual lock
indicator, and adding one would be scope creep on a Major bug fix.
