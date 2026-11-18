# Task log: ImagePickerField — bigger centred picture, tap to pick, corner remove button

### Checkpoint
- Done: built, tested and committed by the orchestrator on 2026-11-18. Next: Helder device check.

## Task: ImagePickerField — bigger centred picture, tap to pick, corner remove button
**Plan:** `Docs/Management/DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/changes/2026-11-18-imagepickerfield-bigger-centred-picture-tap-to-pick-corner-remove-button/README.md`
**Status:** To Review (device check pending)
**Started:** 2026-11-18
**Completed:** 2026-11-18

Governed component change (`component-change-governance`): Helder pre-authorised the edit (see the README `## Approval`). Consumer map and per-consumer risk: see README `## Consumer map`.

### Changed files:
- `MyVocaList/UI/Components/Media/ImagePickerField.xaml`
- `MyVocaList/UI/Components/Media/ImagePickerField.xaml.cs`
- `Docs/Management/DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/changes/2026-11-18-imagepickerfield-bigger-centred-picture-tap-to-pick-corner-remove-button/task-log.md`
- `MyVocaList.sln` (solution item registration of this task-log)

### Implementation decisions
- Corner button style: reuses `StandardIconButton` (existing MD3 icon-button style, no new global key). Local overrides: `BackgroundColor` = `SecondaryContainer` (tonal), `IconColor` = `OnSecondaryContainer`, 40 dp size, `CornerRadius` 20, `Margin` 4 (inset), `Icon` = `close_outlined`.
- Side formula: `Math.Max(120, Math.Min(width - 2 * 40, 360))`; a width at or below 80 gives 120. The lower bound of 120 is a deviation from the bare formula that only matters for allocated widths below 200 dp; it keeps the picture from collapsing. The 120 dp value is a fallback constant (`MinimumSide`), not the displayed size once the field is laid out.
- Sizes are set on the picture host Grid, the frame and both image layers, not on the component.

### Verification
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -nodeReuse:false` by the orchestrator: 0 errors, 27 warnings (all pre-existing NuGet advisories and the DevExpress trial notice). The subagent's earlier builds hung on stale MSBuild/VBCSCompiler nodes; shutting those down fixed it.
- Tests: `dotnet test MyVocaList.Tests`: run 1 had 1868 passed and 1 failed; run 2 (`--no-build`) had 1869 of 1869 passed. The test project does not cover this UI component, so the run-1 failure is a flaky test unrelated to this change.
- No new tests: Level C (UI-only, no Service/ViewModel/Repository logic). No existing test references the removed buttons by name (grep: test files mention the button labels only in comments).

### Manual device check (per consumer, from the consumer map)
- **VenueFormPage** (`UI/Pages/Venues/VenueFormPage.xaml:46`): square fits inside the 24 dp page padding; add, replace and remove; map-pick picture import shows the busy state (remove button disabled, picture tap ignored); Save and Cancel.
- **SongFormPage** (`UI/Pages/Songs/SongFormPage.xaml:93`, inside `dx:ShimmerView`): add mode with a Deezer pick (corner button shows); edit mode; remove then Save; check the side is correct on first display (the width may arrive after the first layout).
- **ArtistFormPage** (`UI/Pages/Artists/ArtistFormPage.xaml:64`, `IsCircle`): add, replace and remove on the circle; check the corner button over the circle edge.
- All three: hint text shows only with no picture; screen reader says "Double tap to choose a picture" when empty and "Double tap to replace the picture" when filled.
