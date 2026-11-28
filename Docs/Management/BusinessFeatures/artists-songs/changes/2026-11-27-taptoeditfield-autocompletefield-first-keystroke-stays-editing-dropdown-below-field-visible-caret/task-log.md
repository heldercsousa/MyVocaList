# Task-log: TapToEditField / AutocompleteField fixes

## Task: Governed-component change (a) first keystroke stays editing, (b) dropdown below field, (c) visible caret
**Item:** taptoeditfield-autocompletefield-first-keystroke-stays-editing-dropdown-below-field-visible-caret (child of `artists-songs`)
**Status:** To Review
**Started / Completed:** 2026-11-27

**Approval (gate 4):** Helder approved in chat on 2026-11-27 ("aprova"), after the consumer map and risk table below.
**MD3 review (gate 1):** text field caret uses the primary color; the exposed dropdown menu anchors below the field (m3.material.io text fields / menus).

### Consumer map (grep of `<local:TapToEditField` / `<local:AutocompleteField`)
| Consumer | Risk | Verification |
|---|---|---|
| ArtistFormPage.xaml:24/29 (Name) | editing stuck after pick/blur; blur validation stops | type, pick -> text state; blur without pick -> error |
| SongFormPage.xaml:206/212 (Title) | same; dropdown clipped by scroll | typing in a blank field stays editing; pick; tap text releases pick |
| SongFormPage.xaml:264/270 (Artist) | same + ReleasesPickOnEdit/IsLocked; dropdown vs Lyrics | pick/edit/clear cycle and dropdown position |
| PersonFormPage.xaml:117 (Name, standalone Autocomplete) | dropdown position + caret | caret visible, dropdown below, pick/clear/blur |

### Changes
- (a) `TapToEditFieldLogic.ShouldEndEditingAfterBlur(isEditorFocused, isDropDownOpen)` = `!focused && !open`; `TapToEditField.ScheduleBlurCheck` passes `_autocomplete?.IsDropDownOpen`; the blur check re-runs when `IsDropDownOpen` turns false. Test written first (compile-red on the 2-arg signature), then green.
- (b) `AutocompleteField.xaml`: `DropDownPlacement="Bottom"` (`Syncfusion.Maui.Core.DropDownPlacement`, from the 35.2.12 nupkg XML).
- (c) `AutocompleteField.xaml.cs` `ApplyCaretColor()` (called from `ApplyOutline`): Android 10+ tints the inner `EditText.TextCursorDrawable` with the `Primary` token (SfAutocomplete has no cursor color property).

### Changed files:
- `MyVocaList/UI/Components/TextFields/TapToEditFieldLogic.cs`
- `MyVocaList/UI/Components/TextFields/TapToEditField.xaml.cs`
- `MyVocaList/UI/Components/TextFields/AutocompleteField.xaml`
- `MyVocaList/UI/Components/TextFields/AutocompleteField.xaml.cs`
- `Tests/Unit/Components/TapToEditFieldLogicTests.cs`

### Build notes
Build: passed (0 errors) | Tests (Components): 109 passed, 0 failed.
E2E: see the line added below after the emulator check.

### E2E (emulator-5554, clean build + Install, Song form "New Song")
- (a) PASS: tapping Title and typing "b", "boh", "Fran" keeps the editor state (field stays in edit mode, X icon shown); no snap to text.
- (c) PASS: caret visible in Title after the first character (primary/pink cursor).
- (b) NOT RESOLVED: with `DropDownPlacement="Bottom"` the suggestions still open anchored at the field's content area, covering the lower part of the Title field and its typed text (menu top at y~407 inside the field 372-527). `SfAutocomplete` has no offset property (nupkg XML 35.2.12: only `DropDownPlacement`, `DropdownWidth`, header/footer). Needs a decision from Helder (e.g. wrap/anchor change or accept).
- Pick of a Library suggestion opens that song's edit form (existing behavior). Not exercised: Artist, Person, Artist form fields.
