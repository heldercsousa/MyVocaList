# Task log — UI component library licensing

## 2026-11-21 — Syncfusion POC (spike, throwaway)

Helder's directive (2026-11-21): do the POC. Write new code only; existing code stays unchanged. Duplicate existing code under different names so the two can be compared side by side. Work in a worktree, and keep every step's status here so the job can resume after an abrupt stop.

- **Branch:** `task/spike-syncfusion` (from `develop`), never merged to `develop`.
- **Worktree:** `C:\Users\helde\source\repos\MyVocaList\.claude\worktrees\spike-syncfusion`
- **Naming rule:** each duplicate carries the `Sf` suffix (`CrudListViewSf`, `VenuesSfPage`, …). The originals are never edited.
- **Allowed additive edits to shared files** (on the spike branch only):
  - package entries in `Directory.Packages.props` / the `.csproj`;
  - one Syncfusion registration call in `MauiProgram.cs`;
  - one DI line per new page;
  - one Shell route / flyout entry, so the copy can be reached.

### Checkpoint

**Resume rule:** read this block, then `git -C <worktree> log --oneline -10`, then continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| S0 | Checkpoint log on develop + worktree created | done | worktree at `efd4cdf3` (develop tip 2026-11-21) |
| S1 | Context7: Syncfusion MAUI packages (Toolkit vs commercial suite), net10 versions, license-key behaviour | done | Context7 `/syncfusion/maui-toolkit`. **Syncfusion Toolkit for .NET MAUI** (`Syncfusion.Maui.Toolkit`, latest 1.0.12 on nuget.org, LICENSE.txt in package — MIT per GitHub repo) has BottomSheet, Buttons, Chips, Shimmer, TextInputLayout, Popup, Expander, NavigationDrawer, TabView, PullToRefresh, SegmentedControl, Picker, Cards. Registered with `.ConfigureSyncfusionToolkit()`; **no license key**. It has **no ListView** → the POC uses stock MAUI `CollectionView` for the list. Commercial `SfListView` (needs a key) is not used. Context7 has no entry for the commercial suite. |
| S2 | Duplicate `CrudListView` → `CrudListViewSf` (Syncfusion controls) | done | `cb570587`. New `CrudListViewSf` + `SearchBarSf` copies. Verified 2026-11-21: `git diff --numstat` shows 0 deleted lines; originals untouched |
| S3 | Duplicate Venues page → `VenuesSfPage` using `CrudListViewSf`; route + DI | done | `1f696102`. `VenuesViewModel` reused unchanged. Hidden route `venues-sf`; DEBUG-only launch hook in `AppShell.xaml.cs` opens it first |
| S4 | Build net10.0-android, 0 errors | done | 0 errors, 10 warnings, none from Syncfusion. Tests pass (1 flaky repository test passed on retry) |
| S5 | Emulator run: screenshots of the DX vs Sf Venues pages (list, shimmer, selection, search, empty state, sheets) | done (partial) | `1a9e9974`, `spike-screenshots/`. List + delete sheet OK. **Failed on Sf:** (a) `SfTextInputLayout`+`Entry` search — no EditText in the native tree, no keyboard; (b) row tap / long-press never reach the VM (cause unconfirmed). Shimmer not captured; back-closes-sheet not checked |
| S5b | Diagnose + fix (a) and (b) in the Sf copies only (≤3 attempts each); re-capture search / empty / selection; check back button closing the sheet | done | `21aa8bba` (pushed). (a) `SfTextInputLayout` never sized its `Entry` → replaced by a plain MAUI `Entry` (works). (b) XAML command binding never executed → forwarded from code-behind (tap opens the form; long-press selects). Back button matches DX. Shimmer still not captured. 0 deleted lines vs base re-checked |
| S6 | Spike results written into `findings.md` § Spike results | done | `findings.md` § 7. Orchestrator screenshot review added a white strip behind the FAB, and noted the Toolkit has no Autocomplete/ComboBox |
| S7 | Commit spike branch + push; docs commit on develop | done | spike branch pushed at `21aa8bba`; this docs commit on develop |

**POC closed 2026-11-21.** Emulator left running. The worktree is kept, so Helder can deploy the branch and compare: the DEBUG build opens "Venues (Syncfusion POC)" first, and the original Venues is in the flyout.

**Context manifest** (≤ 8 files; spike branch files are listed in `findings.md` § 7):
- `Docs/Management/cross-cutting/ui-component-library-licensing/README.md`
- `Docs/Management/cross-cutting/ui-component-library-licensing/findings.md`
- this file

## 2026-11-22 — Migration implementation (plan.md, tasks.md)

Plan approved by Helder 2026-11-22 ("plan approved. Start implementation"); autonomy grant the same day (any step inside this project folder, push included). Orchestrator ledger: `.superpowers/sdd/plan/progress.md` (gitignored).

### Checkpoint

**Resume rule:** read this block, then the ledger, then `git log --oneline -10`; continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| M0 | Item 🟡; wave 1 claimed `[~]` | done | `794c9f10` |
| M1 | Wave 1: T0.1a, T0.1b, T0.1c (scratch worktrees `spike-sf-controls-a/b/c`), T0.2 (`check-dx-script`) | done | all four reported; results below |
| M2 | Review T0.2, merge to develop; apply T0.1 results to design.md | done | T0.2 merged `0ae29cea`; T0.1 results applied to design.md (this commit) |
| M3 | T0.3 packages + license target | done | merged `db9b6d90` |
| M4 | Wave: T0.4a theme + T0.6a preview registry | pending | |

**Context manifest:** `plan.md`, `tasks.md`, `design.md`, `requirements.md`, this file, `.superpowers/sdd/plan/progress.md`.

### T0.2 — `check_dx.py` + pre-commit wiring — Done (2026-11-23)

Implementor `ad894e4e`, fix round 1 `e6eb35ee` (review: 3 Important fixed — diff parser `+++` lines, MSBuild `;` lists, missing parser tests; re-review all ADDRESSED). Merged `0ae29cea`.

### Changed files
- `.claude/scripts/migration/check_dx.py` (new)
- `.claude/scripts/migration/tests/test_check_dx.py` (new, 16 tests)
- `.claude/githooks/pre-commit` (DevExpress guard block after the build+test gate)
- `.gitignore` unchanged: `artifacts/` was already ignored (L76)

### Verification
- `python -m unittest test_check_dx -v` → 16/16 OK; also green inside the commit hook.
- Hook: an edit adding no DX line passes; a scratch `FooSf.xaml` with `xmlns:dx` is refused (`sf-file-references-devexpress`, exit 1).
- `--release` on develop reports 265 lines today (expected until Phase 5).

| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-UIMIG-11 | Sf files never reference DevExpress | `check_staged` Sf scan | `test_sf_file_with_dx_namespace_fails`, `test_sf_file_name_pattern_is_case_sensitive`, `test_closing_tag_and_markup_extension_forms_are_detected` |
| REQ-UIMIG-12 | no commit adds a DX line (bridge files exempt) | `check_staged` added-line scan, `parse_added` | `test_added_dx_line_in_dx_original_fails`, `test_bridge_files_are_exempt`, `test_removed_lines_are_ignored`, `test_word_containing_dx_without_declared_prefix_passes`, parser tests |
| REQ-UIMIG-14 | release check finds no DX reference | `check_release`, `release_exclusions` | `test_release_flags_package_reference`, `test_release_skips_excluded_paths`, exclusion-list test |
| REQ-UIMIG-17 | no license key in tracked files | key pattern | `test_license_key_literal_is_flagged`, release key test |

Deferred minors (orchestrator ledger): the hook trigger uses `ACM` (a rename plus an edit skips the guard); the license scan only runs on source commits; non-raw test strings give a SyntaxWarning on Python 3.12+; git-quoted paths are not handled. **Note for other sessions:** while DX and Sf coexist, editing an existing DX line counts as adding one, so it is blocked. Log an exception in `.claude/exception-registry.md` and ask Helder (Review Focus 1).

### T0.1a / T0.1b / T0.1c — Control verification — Done (2026-11-23)

Scratch branches `task/spike-sf-controls-a|b|c` (never merged). Results applied to `design.md` § Control mapping, § Sheet-state bridge, § Theme mapping, § License key and § Page structure and navigation. Screenshots: `artifacts/parity/t0.1/a-*.png`, `b-*.png`, `c-*.png` (gitignored). Full reports: orchestrator workspace `.superpowers/sdd/plan/task-T0.1{a,b,c}-report.md`.

- **T0.1a (Toolkit):** every row V; nothing escalated. The sheet needs `IsOpen` as well as `State`; dragging it down ends in `Collapsed`; Android Back does not close it. The dark theme dictionary only partly themes the controls. Context7 was wrong in 4 API names, so the nupkg XML is the source of truth.
- **T0.1b (Community suite):** `SfComboBox`, `SfAutocomplete` (async `IAutocompleteFilterBehavior`) and `SfCheckBox` are V on the emulator. Packages 35.2.12 need MAUI ≥ 10.0.110. With no key, a Debug build showed no visible trial notice.
- **T0.1c (Shell + stock):** the contextual route **does not resolve**, so the design fallback applies. `Image`, `Border`, `ScrollView` and `StaticResource` are V.
- **Orchestrator decision (2026-11-23):** the sheet bridge gets a second converter for `IsOpen` (design § Sheet-state bridge, plan T0.5). No unit test applies to T0.1, because it is a spike.

### T0.3 — Packages, license key, host registration — Done (2026-11-23)

Implementor `d0d71f0b` (MAUI 10.0.41 → 10.0.110) + `8a4d7875`; review spec ✅ quality Approved (3 Minor, deferred). Merged `db9b6d90`.

### Changed files
- `Directory.Packages.props` (MAUI bump; Syncfusion pins)
- `MyVocaList/MyVocaList.csproj` (PackageReferences; `GenerateSyncfusionLicense` target; Release `<Error>`)
- `MyVocaList/MauiProgram.cs` (license registration when the key is non-empty; `ConfigureSyncfusionCore()`, `ConfigureSyncfusionToolkit()`)
- `Docs/DevEnv/devenv-setup.md`, `.claude/library/dev-env-setup.md` (Syncfusion license key section)

### Verification
- After the bump: build 0 errors; `dotnet test` 2039 passed, 0 failed; the app starts and the Venues, Singers, Songs, Artists, Settings and About pages open (`artifacts/parity/t0.3/`).
- With the variable unset: `-c Release` fails with `SYNCFUSION_LICENSE_KEY is not set — required for Release builds.`; `-c Debug` gives 0 errors; no generated file is tracked; `check_dx.py --release` reports no `license-key-in-tracked-file`.

| AC | Criterion | Evidence |
|---|---|---|
| REQ-UIMIG-16 | no trial notice with the key | pending: needs Helder's key, checked at the first cut-over |
| REQ-UIMIG-17 | key never tracked | generated under `obj/`; `check_dx.py --release` clean for keys |
| REQ-UIMIG-18 | Release fails without the key; Debug runs | build commands above |

Deferred minors: a `"` or `;` in a key breaks the generated file (the build fails loudly); `dotnet test -c Release` needs the key; `Microsoft.Maui.Essentials` is still pinned at 10.0.0.

### T0.6a — Preview page and route registry (Debug) — Done (2026-11-23)

Implementor `4352e78a`; review spec ✅ quality Approved (4 Minor, deferred). Merged into develop.

### Changed files
- `MyVocaList/UI/Migration/PreviewRoutes.cs` (new, `#if DEBUG`; `Entries` empty; `Register()` registers `syncfusion-preview` + entries; no contextual entries, per T0.1c)
- `MyVocaList/UI/Migration/SyncfusionPreviewPage.xaml(.cs)` (new; `.cs` `#if DEBUG`; `SafeAreaEdges="Container"`; empty-state label)
- `MyVocaList/MyVocaList.csproj` (Release `MauiXaml Remove` for the page)

### Verification
Debug build: 0 errors. Release build with a dummy key in the shell only: 0 errors. No unit test, because this is Level C plumbing. REQ-UIMIG-08/09 are checked once T0.6b wires the entry.

### T0.5 — Sheet-state bridge — Done (2026-11-23)

Implementor `fdb0b7ce`; fix round 1 `d354d5a5` (tests for the `Binding.DoNothing` branches); fix round 2 `fcb96e29` (an undefined DX state now does nothing in the `IsOpen` converter, per design). Each round was re-reviewed: ADDRESSED. Merged into develop.

### Changed files
- `MyVocaList/UI/Migration/DxSheetStateConverter.cs` (new: `DxSheetStateConverter`, `DxSheetStateToIsOpenConverter`)
- `Tests/Unit/Migration/DxSheetStateConverterTests.cs` (new, 18 cases)
- Pending, orchestrator ruling: the `App.xaml` resources (`xmlns:migration="clr-namespace:MyVocaList.UI.Migration"`; keys `DxSheetStateToSf`, `DxSheetStateToSfIsOpen`). T0.4a owned `App.xaml` while this task ran, so the lines are added by the next task that edits `App.xaml` from develop after T0.4a merges (T0.4b).

### Verification
- RED: CS0246, the type was missing.
- GREEN: `dotnet test --filter DxSheetStateConverterTests` passes 18/18. The full suite passes in the pre-commit hook (2049+).

| AC | Criterion | Implementation | Test |
|---|---|---|---|
| REQ-UIMIG-07 | with a bottom sheet open, back closes it (state bridge part) | both converters | `Convert_Hidden_IsClosed`, `Convert_HalfExpanded_IsHalfExpanded`, `Convert_FullExpanded_IsFullExpanded`, `ConvertBack_ClosedByUser_IsHidden`, `ConvertBack_DraggedToFull_IsFullExpanded`, `ConvertBack_DraggedToHalf_IsHalfExpanded`, `ConvertBack_Collapsed_IsHidden`, `IsOpen_Hidden_IsFalse`, `IsOpen_HalfExpanded_IsTrue`, `IsOpenBack_False_IsHidden`, plus the DoNothing branch cases |

**Observed flake (not caused by this task):** `SongServiceSaveFromFormImageRealStackTests.SaveFromFormAsync_ExistingArtistWithImage_KeepsStoredArtistImage` sometimes throws `ObjectDisposedException` (SQLitePCL) in a parallel full run. It passes alone and on retry.

### T0.6b — Debug-only preview entry — Done (2026-11-23)

Implementor `ecfc485a`; review: spec ✅, quality Approved (2 Minor). Merged into develop.

### Changed files
- `MyVocaList/AppShell.xaml.cs`: `#if DEBUG` `PreviewRoutes.Register();`
- `MyVocaList/Navigation/NavigationConfig.cs`: a `#if DEBUG` "Syncfusion preview" item in the System group. It navigates by route through its own Command. It also has a Debug-only `PageTypes` entry, because the existing `NavigationConfigTests` needs one per menu route; that entry is never used for navigation (orchestrator ruling, since tests are not changed).

### Verification
- Debug on the emulator: the flyout shows "Syncfusion preview"; the page opens with "No Syncfusion pages to preview yet."; Back returns (`artifacts/parity/t0.6b/`).
- `dotnet test`: 2039 passed.

| AC | Criterion | Evidence |
|---|---|---|
| REQ-UIMIG-08 | user-facing routes still open the DX pages | no user-facing route changed; checked again per page in Recipe P step P5b |
| REQ-UIMIG-09 | Release has no preview route or entry | Release build (dummy key in the shell only): `syncfusion-preview` and `venues-sf` are absent from all 6 `MyVocaList.dll` (UTF-8 and UTF-16-LE); the positive control finds `syncfusion-preview` in the Debug dll |

Observed flake (not caused by this task): `SongFormViewModelFrozenArtistTests` threw "Collection was modified" once and passed on rerun.
