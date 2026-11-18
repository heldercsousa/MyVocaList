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

---
## Task: Device-check fix 1 — corner remove icon off-centre and read as an X
**Plan:** `Docs/Management/DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/changes/2026-11-18-imagepickerfield-bigger-centred-picture-tap-to-pick-corner-remove-button/README.md`
**Status:** To Review (device check pending)
**Started:** 2026-11-18
**Completed:** 2026-11-18

Level C (no test): pure visual change to one XAML button (icon, padding, icon size, alignment). No Service, ViewModel or Repository logic changed; no AC behaviour changed (REQ-IPF-03 already in the spec). Existing tests do not cover XAML layout.

### Changed files:
- `MyVocaList/UI/Components/Media/ImagePickerField.xaml`
- `Docs/Management/DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/changes/2026-11-18-imagepickerfield-bigger-centred-picture-tap-to-pick-corner-remove-button/task-log.md`

### Cause found
- Icon: `close_outlined` read as an "X". Changed to `delete_outlined`, the icon already used in `UI/Components/CrudListView.xaml`.
- Off-centre: Context7 (DevExpress MAUI docs, `DXContentPresenter.IconWidth` / `IconHeight`) gives the default icon size as 18x18 for non-ToolButton types. The docs do not state the default `Padding` value. The 40 dp button with default padding and an unset vertical content alignment left the icon off-centre. Likely cause from the docs; not verified on the emulator, so confirm on device.

### Fix (on `removeButton` only; `StandardIconButton` untouched)
- `Icon="delete_outlined"`
- `Padding="0"`
- `IconWidth="24"` / `IconHeight="24"` (explicit, instead of the 18 dp default)
- `HorizontalContentAlignment="Center"` (also inherited from the style; set locally) and `VerticalContentAlignment="Center"`
- `IconIndent` not changed: the button has no text.
- Comment above the button lists the centring overrides and says the icon is the trash can.

### Verification
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -nodeReuse:false`: 0 errors, 34 warnings (pre-existing NuGet advisories).
- Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`: 1869 passed, 0 failed (no flaky song-form failures this run).
- Re-read: removeButton block (ImagePickerField.xaml lines 56-80) confirmed after the edit.
- Manual: device check still required for centring and icon look (VenueFormPage, SongFormPage, ArtistFormPage circle).

### Spec note
- The device-check note at the end of the README is already on develop (`> **Spec updated 2026-11-18**`); no further spec change needed.
