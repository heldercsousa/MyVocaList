---
## Task: Timing tests for the autocomplete search
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-timing-tests-for-the-autocomplete-search/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Governed-component note (AutocompleteFieldSf)
Consumer map (grep `AutocompleteFieldSf`): `SongFormSfPage.xaml`, `ArtistFormSfPage.xaml`, `PersonFormSfPage.xaml` (XAML, parameterless constructor, unchanged).
Risk: the only production change is an added internal constructor overload `AutocompleteFieldSf(bool skipXamlInitForTests)`; the public parameterless constructor delegates to it with `false`, so XAML consumers run the same code path. Verified by build and the Sf filter/characterization tests.

### Decision (test seam)
> **Spec updated 2026-11-25:** `AutocompleteFieldSf(bool skipXamlInitForTests)` added as internal test seam, mirroring the DX `AutocompleteField(skipXamlInitForTests)` precedent; XAML load fails in unit tests (`StaticResource not found for key TransparentBrush`).
<!-- impl decision: field search must be reachable from unit tests without the app's XAML resources -->

### Changed files:
- `MyVocaList/UI/Components/TextFields/AutocompleteFieldSf.xaml.cs` (test-seam constructor overload)
- `Tests/Unit/Components/AutocompleteFieldSfCharacterizationTests.cs` (new, 4 tests)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-timing-tests-for-the-autocomplete-search/task-log.md`

### Coverage gap (not pinned by item 1)
Pick routing (`OnSelectionChanging` → cancel pending search, apply text), blur/unload and the non-empty result path (`ApplyRowHeight` needs the app's row templates) need the XAML host or a Syncfusion selection-args fixture. Not covered; recorded, not faked.

### Verification evidence
- `dotnet test --filter AutocompleteFieldSfCharacterization|AutocompleteFilterSf`: 13 passed, 0 failed (Sf filter tests 9 + characterization 4).
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors (exit 0). Full suite: see Build notes.

### Build notes
Build: passed (0 errors) | Tests: 13 passed, 0 failed (filtered run) | Commit SHA: see git log
Files written and re-read: `AutocompleteFieldSf.xaml.cs` (constructor block L286-302), `AutocompleteFieldSfCharacterizationTests.cs` (4 tests), this task-log.
