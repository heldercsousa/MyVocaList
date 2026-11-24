# UI library migration — DevExpress → Syncfusion — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Task status lives in `tasks.md` (Rule 4); this file holds the steps.

**Goal:** Replace every DevExpress control with Syncfusion (or stock MAUI where Syncfusion has no equivalent) before the MVP launch, keeping every user-facing page working at every commit.

**Architecture:** Approach B. Each DX original gets an Sf copy (suffix `Sf` / `SfPage`) that reuses the same ViewModel. Sf pages live on Debug-only preview routes until a one-line cut-over points the user-facing route at them. ViewModels stay unchanged until Phase 5; a migration-only converter bridges the DevExpress sheet state meanwhile.

**Tech Stack:** .NET MAUI (net10.0-android mandatory), `Syncfusion.Maui.Toolkit` (MIT), the Syncfusion Community-License suite, CommunityToolkit.Maui 14.0.0 (`IconTintColorBehavior`), Python 3 `unittest` for the check script. Exact Syncfusion package ids and versions come out of T0.1b and are pinned in `Directory.Packages.props`.

**Spec:** `requirements.md` and `design.md` in this folder (approved by Helder 2026-11-22). Evidence: `findings.md` § 7. POC reference only: branch `task/spike-syncfusion`.

## Global Constraints

- DX originals are never edited until Phase 5, except the cut-over line(s) and AppShell in Phase 4 (design § Decision: AppShell in place).
- Sf file names match `^[A-Za-z0-9]+(Sf|SfPage)\.(xaml|xaml\.cs|cs)$`; an Sf file never references DevExpress (REQ-UIMIG-11).
- No commit adds a DevExpress line, except the two bridge files `MyVocaList/UI/Migration/DxSheetStateConverter.cs` and `Tests/Unit/Migration/DxSheetStateConverterTests.cs` (REQ-UIMIG-12).
- Every Sf page: `SafeAreaEdges="All"` with text input, else `"Container"`; no `DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync`; same ViewModel as its DX original; code-behind only forwards events to ViewModel commands.
- Every Sf component exposes the DX original's BindableProperty names and types, unless design § Control mapping lists a deviation.
- Colors only from `MaterialColors.xaml` tokens (dark "Karaoke Neon" palette); no hard-coded colors.
- `GlobalUsings.cs` keeps `global using DevExpress.*` until T5.4: Sf code names Syncfusion and MAUI types fully qualified or through a `using` alias where a name collides (`BottomSheetState`, `Popup`, `CheckBox`).
- Sequential-only files, applied by the orchestrator after each wave: `MauiProgram.cs`, `AppShell.xaml(.cs)`, `GlobalUsings.cs`, `Directory.Packages.props`, `MyVocaList.csproj`, `Tests/MyVocaList.Tests.csproj`, `App.xaml`, `MyVocaList/Navigation/Routes.cs`, `MyVocaList/Navigation/NavigationConfig.cs`, `MyVocaList/UI/Migration/PreviewRoutes.cs`, `.claude/library/component-safety-gate.md`, `tasks.md`. A wave task lists the lines it needs there in its task log.
- Sf page DI registrations in `MauiProgram.cs` are not `#if DEBUG`: unreachable in Release until cut over, which REQ-UIMIG-09 allows (it concerns routes and the preview entry).
- XAML: edit one file → build → fix → next. Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` (0 errors). Tests: `dotnet test Tests/MyVocaList.Tests.csproj` (all pass).
- Parity screenshots go to `artifacts/parity/<page>/` at the repo root, which is gitignored (added in T0.2), so no `.sln` registration applies. The task log lists them by name; the orchestrator sends them to Helder.
- `SYNCFUSION_LICENSE_KEY` never appears in a tracked file. No `--no-verify`.
- A control whose primary and fallback both fail, or a mapping row marked "escalate", stops the task: `blocked: spec gap` to Helder. Never pick another library alone.
- Commits: `feat:` / `refactor:` with trailer `Item: ui-component-library-licensing`; cut-overs `feat: cut over <Page> to Syncfusion`.
- Briefings: at most 5 files written and about 2 hours each (Rule 2).

## Review Focus

1. **A bug fix on a DX original that needs a new DX line** during coexistence: the check script blocks it. Expected: the fixer logs it in `.claude/exception-registry.md` and asks Helder, never bypasses the hook. Pinned by T0.2 test `test_block_message_names_exception_registry`.
2. **Menu page opened through `NavigationService.PushAsync`** (not the flyout) after a cut-over: it must open the Sf page too. Pinned by Recipe X step X2 and by T1.5's check of both `preferences` and `settings`.
3. **Contextual route does not resolve** (`venues-sf/venue-form`): the Sf list must still push a working (DX) form, never crash. Pinned by T0.6b step 4 and T2.9's P11 check.
4. **Back with a sheet open while rows are selected** on an Sf list: closes the sheet only, keeps the selection (P10 order). Pinned by T2.9's extra check.
5. **Debug build with no license key on Helder's device**: the app runs; a trial notice is acceptable (REQ-UIMIG-18). Pinned by T0.3 step 5.

---

## Recipes (shared by many tasks)

### Recipe C — port a component to its Sf copy (implementor subagent)

For a component `X` at `MyVocaList/UI/Components/<Folder>/X.xaml(.cs)`:
- [ ] **C1** Copy to `<Folder>/XSf.xaml(.cs)`; rename the class to `XSf`, keep the namespace.
- [ ] **C2** Replace each DX part with its design § Control mapping primary (fallback only after the primary is shown failing, logged in the task log). Keep every BindableProperty name, type and default; keep public events and commands.
- [ ] **C3** Build; fix; repeat (≤ 3 tries, then stop and report).
- [ ] **C4** `git add` then `python .claude/scripts/migration/check_dx.py --staged` → exit 0.
- [ ] **C5** Commit `feat: <X>Sf — Syncfusion copy of <X>`.

Components have no unit tests (Level C, pure UI; the no-test decision goes in the task log). Parity is proven through the first Sf page that consumes them.

### Recipe P — port a page to its Sf copy

**Subagent half** (implementor; may run in a wave):
- [ ] **P1** Copy `MyVocaList/UI/Pages/<Folder>/XPage.xaml(.cs)` to `XSfPage.xaml(.cs)`; same ViewModel injected the same way; replace DX components by their `…Sf` copies and DX controls by the mapping.
- [ ] **P2** Build; fix (≤ 3 tries). `check_dx.py --staged` → 0. Commit `feat: <X>SfPage — Syncfusion copy of <X>Page`.
- [ ] **P3** Write in the task log the lines the orchestrator applies: DI (`builder.Services.AddTransient<XSfPage>();` beside the DX page in `MauiProgram.cs`) and `PreviewRoutes` entries (T0.6a interface). The subagent ends here.

**Orchestrator half** (one page at a time, after the wave merges; one emulator):
- [ ] **P4** Apply P3; build; clean `obj/bin` of `net10.0-android`; deploy Debug to AVD `pixel_5_-_api_36_0_1`.
- [ ] **P5** Run every applicable parity state (design § Parity checklist) on the DX original and the Sf copy; save `dx-<state>.png` / `sf-<state>.png` under `artifacts/parity/<page>/`; list them in the task log. Any visible difference is listed for Helder (requirements § Out of Scope, first bullet). Covers REQ-UIMIG-01…07 for the page.
- [ ] **P5b** `[AC] REQ-UIMIG-08`: from the flyout and from every pushing page, the user-facing route still opens the DX original; the Sf copy opens only from "Syncfusion preview".
- [ ] **P6** Commit the registration lines plus the task-log entry: `feat: register <X>SfPage on its preview route`. A parity failure goes back to a new implementor briefing (never the finished one).

### Recipe X — cut over a page (orchestrator, after Helder's explicit go)

Precondition: P5 passed; Helder's go recorded in the task log; `SYNCFUSION_LICENSE_KEY` set. On the first cut-over only: a Debug deploy with the key shows no Syncfusion trial notice — `[AC] REQ-UIMIG-16`.
- [ ] **X1** Flyout page: in `AppShell.xaml` change the `ShellContent` `ContentTemplate` from `XPage` to `XSfPage` (both for `SettingsPage`), and in `NavigationConfig.PageTypes` change `typeof(XPage)` to `typeof(XSfPage)`. Pushed page: in `AppShell.xaml.cs` change `Routing.RegisterRoute(Routes.X, typeof(XPage))` to `typeof(XSfPage)`. Remove the page's entry from `PreviewRoutes`.
- [ ] **X2** Build, test, deploy; open the page from the flyout **and** through its other path (`NavigationService.PushAsync` for menu pages; the pushing page for pushed pages). Both show the Sf page.
- [ ] **X3** `rtk proxy git diff --cached` shows only the X1 lines; commit `feat: cut over <X> to Syncfusion`. A revert must restore the DX page (REQ-UIMIG-10).

---

## Phase 0 — Foundation

### Task T0.1a / T0.1b / T0.1c: Control verification (time-boxed, about 2 h each, throwaway) [P]

**Files:** results go into the task's `task-log.md` entry only; the orchestrator applies all three results to `design.md` § Control mapping, § Sheet-state bridge and § Theme mapping in one commit after the wave (one writer). Scratch branches `task/spike-sf-controls-a|b|c` from `develop`, never merged.

| Task | Rows verified | Also produces |
|---|---|---|
| T0.1a Toolkit | `SfBottomSheet` (state property + enum values; half-expanded support), `SfShimmer`, `SfButton`, `SfTextInputLayout` + `Entry` / `Editor` / password, `SfChipGroup` choice + filter, `SfPopup` | Toolkit dark theme dictionary type and its color keys; whether the Toolkit has a plain `net10.0` asset (T0.5's unit test) |
| T0.1b Community suite | `SfComboBox`, `SfAutocomplete` (async items through a custom filter), `SfCheckBox` | exact NuGet ids + versions; host-builder calls; license registration call; plain `net10.0` assets |
| T0.1c Shell + stock | Shell contextual route `venues-sf/venue-form`; `DXImage`→`Image`, `DXBorder`→`Border`, `DXScrollView`→`ScrollView`, `{dx:ThemeColor}`→`{StaticResource}` | contextual-route verdict |

- [ ] **Step 1:** Look up each row: Context7 (`/syncfusion/maui-toolkit`) for the Toolkit; the official Syncfusion .NET MAUI docs for the suite; Microsoft Learn for Shell contextual routes. Note each source URL.
- [ ] **Step 2:** On the scratch branch, one screen per control on the emulator with dark tokens, screenshot to `artifacts/parity/t0.1/`.
- [ ] **Step 3 (T0.1c):** register `Routing.RegisterRoute("venues-sf/venue-form", typeof(VenueFormPage))` on the scratch branch, navigate `venue-form` from a page on route `venues-sf`, record which page opened.
- [ ] **Step 4:** Write the results in the task log (V or failed, source, screenshot name). Any "escalate" outcome → stop, report to Helder before Phase 1.
- [ ] **Step 5 (orchestrator, after the wave):** apply the three results to the design sections; commit on `develop`: `docs: Syncfusion control verification results`.

### Task T0.2: Check script `check_dx.py`, pre-commit wiring, artifacts ignore [P]

**Files:**
- Create: `.claude/scripts/migration/check_dx.py`
- Create: `.claude/scripts/migration/tests/test_check_dx.py`
- Modify: `.claude/githooks/pre-commit` (new block after the build+test gate)
- Modify: `.gitignore` (add `artifacts/`)

**Interfaces (in `check_dx.py`):**
- `SF_FILE = re.compile(r"^[A-Za-z0-9]+(Sf|SfPage)\.(xaml|xaml\.cs|cs)$")`
- `BRIDGE_FILES = ("MyVocaList/UI/Migration/DxSheetStateConverter.cs", "Tests/Unit/Migration/DxSheetStateConverterTests.cs")`
- `class Violation(NamedTuple): path: str; line: int; rule: str` — rules `"sf-file-references-devexpress"`, `"added-devexpress-line"`, `"release-references-devexpress"`, `"license-key-in-tracked-file"`.
- `def dx_prefixes(text: str) -> set[str]` — XAML prefixes declared with `http://schemas.devexpress.com/maui` or `clr-namespace:DevExpress…`.
- `def has_dx_reference(line: str, prefixes: set[str]) -> bool` — `DevExpress`, `schemas.devexpress.com`, or a declared prefix `p` used as `<p:`, `</p:`, `{p:`, `(p:`, `"p:` or ` p:`.
- `def check_staged(added: dict[str, list[tuple[int, str]]], contents: dict[str, str]) -> list[Violation]` — `added`: path → added lines from `git diff --cached -U0`; `contents`: staged content (`git show :path`) for prefix resolution and the Sf-file scan.
- `def check_release(files: dict[str, str], excluded: list[str]) -> list[Violation]`
- `def main(argv: list[str]) -> int` — `--staged` | `--release`; 0 clean, 1 violations (prints `path:line: rule`), 2 usage or git error. On a blocked added line, prints `Blocked: new DevExpress line. Log an exception in .claude/exception-registry.md and ask Helder; never bypass the hook.`

- [ ] **Step 1: Write the failing tests** (`unittest.TestCase` `CheckDxTests`; the AC tag is the docstring's first line):
  - `test_sf_file_with_dx_namespace_fails` — `[AC] REQ-UIMIG-11`: contents `{"MyVocaList/UI/X/FooSf.xaml": 'xmlns:dx="http://schemas.devexpress.com/maui"\n<dx:DXButton/>'}` → `sf-file-references-devexpress` at line 1.
  - `test_sf_file_name_pattern_is_case_sensitive` — `[AC] REQ-UIMIG-11`: `Foosf.xaml` is not an Sf file; `VenuesSfPage.xaml.cs` is.
  - `test_closing_tag_and_markup_extension_forms_are_detected` — `[AC] REQ-UIMIG-11`: `</dx:DXButton>` and `{x:Static dx:Foo.Bar}` with `dx` declared → flagged.
  - `test_added_dx_line_in_dx_original_fails` — `[AC] REQ-UIMIG-12`: added `(12, "using DevExpress.Maui.Core;")` in `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` → `added-devexpress-line`.
  - `test_bridge_files_are_exempt` — `[AC] REQ-UIMIG-12`: the same line in each of `BRIDGE_FILES` → no violation.
  - `test_removed_lines_are_ignored` — `[AC] REQ-UIMIG-12`: `added` empty, contents still DX → no `added-devexpress-line`.
  - `test_word_containing_dx_without_declared_prefix_passes` — `[AC] REQ-UIMIG-12`: added `<local:dxLike Text="dx:none"/>` with no DX prefix declared → no violation.
  - `test_block_message_names_exception_registry` — Review Focus 1: `main` output on a blocked line contains `exception-registry.md`.
  - `test_release_flags_package_reference` — `[AC] REQ-UIMIG-14`: `Directory.Packages.props` with `<PackageVersion Include="DevExpress.Maui.Core"` → `release-references-devexpress`.
  - `test_release_skips_excluded_paths` — `[AC] REQ-UIMIG-14`: a DX file under an excluded glob → no violation.
  - `test_license_key_literal_is_flagged` — `[AC] REQ-UIMIG-17`: `RegisterLicense("` + 40 letters + `")` → `license-key-in-tracked-file`; `RegisterLicense(SyncfusionLicense.Key)` → none.
  - `test_exit_codes` — 0 clean, 1 violations, 2 with no argument.
- [ ] **Step 2:** `cd .claude/scripts/migration/tests && python -m unittest test_check_dx -v` → FAIL (module not found).
- [ ] **Step 3:** Implement. `--release` reads tracked `*.cs`, `*.xaml`, `*.csproj`, `Directory.Packages.props` via `git ls-files`, excluding the globs of `<Compile Remove>` / `<MauiXaml Remove>` in `MyVocaList/MyVocaList.csproj`. Key pattern: `RegisterLicense\(\s*"[^"]{20,}"\s*\)` or `SYNCFUSION_LICENSE_KEY\s*=\s*\S{20,}`.
- [ ] **Step 4:** Re-run → all PASS.
- [ ] **Step 5:** Hook: in `.claude/githooks/pre-commit`, when staged files match `\.(cs|xaml|csproj|props)$`, run `python .claude/scripts/migration/check_dx.py --staged`; when `.claude/scripts/migration/` files are staged, run the tests as the backlog block does. Verify: stage a scratch `FooSf.xaml` with `xmlns:dx=…` → commit refused; unstage and delete it.
- [ ] **Step 6:** Add `artifacts/` to `.gitignore`. Commit `feat: check_dx.py — block DevExpress in Sf copies and new DX lines`.

### Task T0.3: Packages, license key and Syncfusion host registration [SEQUENTIAL — after T0.1b]

**Files:**
- Modify: `Directory.Packages.props` (Syncfusion block beside the DevExpress block L68-76)
- Modify: `MyVocaList/MyVocaList.csproj` (PackageReferences in the main group L114-141; new target)
- Modify: `MyVocaList/MauiProgram.cs` (builder chain L34-41)
- Modify: `.claude/library/dev-env-setup.md`, `Docs/DevEnv/devenv-setup.md` (new "Syncfusion license key" section beside § 2 DevExpress)

**Interfaces:**
- Consumes: T0.1b package ids, versions, host calls and registration call.
- Produces: `internal static class SyncfusionLicense { public const string Key = "…"; }` generated at `$(IntermediateOutputPath)SyncfusionLicense.g.cs`; Syncfusion controls usable in any XAML.

- [ ] **Step 1:** Add `PackageVersion` entries (exact versions, no floating) and the matching `PackageReference`s.
- [ ] **Step 2:** Add target `GenerateSyncfusionLicense` (`BeforeTargets="CoreCompile"`): `WriteLinesToFile` with `WriteOnlyWhenDifferent="true"`, then `<Compile Include>` of the generated file; plus `<Error Condition="'$(Configuration)' == 'Release' and '$(SYNCFUSION_LICENSE_KEY)' == ''" Text="SYNCFUSION_LICENSE_KEY is not set — required for Release builds." />`.
- [ ] **Step 3:** In `CreateMauiApp`, before `MauiApp.CreateBuilder()` (L34): the T0.1b registration call with `SyncfusionLicense.Key`; the T0.1b host calls after `.UseMauiCommunityToolkit()` (L37). DevExpress calls L38-41 stay.
- [ ] **Step 4: Build checks** — `[AC] REQ-UIMIG-18`, `[AC] REQ-UIMIG-17`: variable unset, `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release` → fails with `SYNCFUSION_LICENSE_KEY is not set`; same with `-c Debug` → 0 errors; `git status --short` shows no generated file; `check_dx.py --release` reports no `license-key-in-tracked-file`.
- [ ] **Step 5:** Deploy Debug without the key to the emulator: the app starts and every page opens (Review Focus 5). Record in the task log.
- [ ] **Step 6:** `dotnet test Tests/MyVocaList.Tests.csproj` → pass. Docs: the variable, where Helder gets the key, how to set it for Release. Commit `feat: Syncfusion packages and build-time license key`.

### Task T0.4a: Theme dictionary and Sf styles [SEQUENTIAL — after T0.3]

**Files:**
- Create: `MyVocaList/Resources/Styles/SyncfusionTheme.xaml` (+ `.xaml.cs` only if the theme type requires it)
- Create: `MyVocaList/UI/Behaviors/IconTint.cs`
- Modify: `MyVocaList/App.xaml` (MergedDictionaries L13-16, after `MaterialStyles.xaml`)
- Modify: `MyVocaList/Resources/Styles/MaterialStyles.xaml` (new Sf section at the end)

**Interfaces:**
- Consumes: T0.1a theme type and color keys.
- `public static class IconTint` with attached property `Color` (`BindableProperty.CreateAttached("Color", typeof(Color), typeof(IconTint), null, propertyChanged: OnColorChanged)`), settable from a `Style` setter. `static void OnColorChanged(BindableObject b, object oldValue, object newValue)`: if `b` is not a `View`, return; find the existing `IconTintColorBehavior` in `view.Behaviors`; if `newValue` is null, remove it; otherwise create and add one when missing, then set `TintColor = (Color)newValue`.
- Style keys (m3.material.io names): `SfIconButton` (icon tinted `OnSurfaceVariant` through `IconTint.Color`), `SfTextButton`, `SfFilledButton`, `SfFlyoutMenuButton` (same look as the DX `FlyoutMenuButton`, `MaterialStyles.xaml:95`), and implicit styles for `SfShimmer`, `SfBottomSheet`, `SfChipGroup`, `SfPopup` using `Surface*`, `OnSurface*`, `Primary`, `Outline` tokens.

- [ ] **Step 1:** `SyncfusionTheme.xaml` merges the Toolkit dark theme dictionary and overrides its color keys with `{StaticResource <token>}`.
- [ ] **Step 2:** `IconTint` and the styles. DX style keys stay untouched.
- [ ] **Step 3:** Build; on the T0.1a scratch branch, shimmer, sheet, chips and an icon button render in the dark tokens. No unit test (Level C) — noted in the task log.
- [ ] **Step 4:** Commit `feat: Syncfusion theme and styles mapped to Material tokens`.

### Task T0.4b: `OutlinedTextFieldSf` [SEQUENTIAL — after T0.4a]

**Files:** Create `MyVocaList/UI/Components/TextFields/OutlinedTextFieldSf.xaml(.cs)`.

**Interfaces:** `public partial class OutlinedTextFieldSf : ContentView` — a stock `Border` (MD3 outlined text field: `Outline` stroke, `Primary` when focused, corner 4) around an `Entry`; BindableProperties `Text` (string, TwoWay), `Placeholder` (string), `IsPassword` (bool), `Keyboard` (Keyboard), `ReturnCommand` (ICommand), `ClearButtonVisibility` (ClearButtonVisibility); event `Completed`. Used where `SfTextInputLayout` failed (search bar) or T0.1a marks it failed.

- [ ] **Step 1:** Recipe C steps C3–C5 (new component, no DX original). Check on the emulator: keyboard opens, text binds, placeholder in `OnSurfaceVariant`, no underline (REQ-UIMIG-05 basis).

### Task T0.5: Sheet-state bridge [P — after T0.3; not in a wave with T0.4a]

**Files:**
- Create: `MyVocaList/UI/Migration/DxSheetStateConverter.cs`
- Test: `Tests/Unit/Migration/DxSheetStateConverterTests.cs`
- Orchestrator: `App.xaml` resources `<migration:DxSheetStateConverter x:Key="DxSheetStateToSf" />` and `<migration:DxSheetStateToIsOpenConverter x:Key="DxSheetStateToSfIsOpen" />`; `Tests/MyVocaList.Tests.csproj` Toolkit reference if T0.1a says it is needed

**Interfaces:** `public sealed class DxSheetStateConverter : IValueConverter` in `MyVocaList.UI.Migration`; `Convert` DX `BottomSheetState` → Sf state; `ConvertBack` Sf → DX, per design § Sheet-state bridge (Sf enum names from T0.1a; `using` aliases for the two same-named enums). *Added 2026-11-23 from T0.1a:* in the same file, `public sealed class DxSheetStateToIsOpenConverter : IValueConverter` (key `DxSheetStateToSfIsOpen`, bound to `SfBottomSheet.IsOpen`): `Hidden` → `false`, other → `true`; `ConvertBack` `false` → `Hidden`, `true` → `Binding.DoNothing`. Sf `Collapsed` converts back to DX `Hidden`.

- [ ] **Step 1: Failing tests**, each tagged `// [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it`: `Convert_Hidden_IsClosed`, `Convert_HalfExpanded_IsHalfExpanded`, `Convert_FullExpanded_IsFullExpanded`, `ConvertBack_ClosedByUser_IsHidden`, `ConvertBack_DraggedToFull_IsFullExpanded`, `ConvertBack_DraggedToHalf_IsHalfExpanded`, plus (2026-11-23) `ConvertBack_Collapsed_IsHidden`, `IsOpen_Hidden_IsFalse`, `IsOpen_HalfExpanded_IsTrue`, `IsOpenBack_False_IsHidden`.
- [ ] **Step 2:** Run → FAIL. If T0.1a found no plain `net10.0` Toolkit asset, the test project cannot name the Sf enum: replace these tests by an emulator check of the six transitions, record the decision in the task log, and go to Step 3.
- [ ] **Step 3:** Implement. Unknown input → `Binding.DoNothing`.
- [ ] **Step 4:** Run → PASS; full suite passes. `check_dx.py --staged` → 0 (bridge files exempt). Commit `feat: DxSheetStateConverter — migration bridge`.

### Task T0.6a: Preview page and route registry [SEQUENTIAL — after T0.3]

**Files:**
- Create: `MyVocaList/UI/Migration/PreviewRoutes.cs` (whole file inside `#if DEBUG`)
- Create: `MyVocaList/UI/Migration/SyncfusionPreviewPage.xaml(.cs)` (`.cs` inside `#if DEBUG`)
- Modify: `MyVocaList/MyVocaList.csproj` (`<MauiXaml Remove="UI\Migration\SyncfusionPreviewPage.xaml" Condition="'$(Configuration)' == 'Release'" />`, so every commit still builds in Release)

**Interfaces:** `public static class PreviewRoutes { public static IReadOnlyList<(string Title, string Route, Type Page)> Entries { get; } public static void Register(); }` — `Register` calls `Routing.RegisterRoute` for `syncfusion-preview` (→ `SyncfusionPreviewPage`) and each entry (`<route>-sf`, plus contextual `<list>-sf/<form>` entries if T0.1c says they resolve). `SyncfusionPreviewPage` (`SafeAreaEdges="Container"`) lists `Entries`; a tap calls `Shell.Current.GoToAsync(route)` from code-behind (navigation only, no logic).

- [ ] **Step 1:** Implement; build; commit `feat: Syncfusion preview page and route registry (Debug)`.

### Task T0.6b: Preview wiring [SEQUENTIAL — after T0.6a]

**Files:**
- Modify: `MyVocaList/AppShell.xaml.cs` (after L32: `#if DEBUG` `PreviewRoutes.Register();` `#endif`)
- Modify: `MyVocaList/Navigation/NavigationConfig.cs` (`BuildMenuGroups`, System group: `#if DEBUG` item "Syncfusion preview", route `syncfusion-preview`)

- [ ] **Step 1:** Implement; Debug deploy: the flyout shows "Syncfusion preview" (empty list).
- [ ] **Step 2: Release check** — `[AC] REQ-UIMIG-09`: with a dummy key set in the shell only, `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release`; a `python -c` search of `MyVocaList/bin/Release/net10.0-android/**/MyVocaList.dll` bytes (UTF-8 and UTF-16) for `syncfusion-preview` and `venues-sf` → not found. Record command and output in the task log.
- [ ] **Step 3:** Commit `feat: Debug-only Syncfusion preview entry`.
- [ ] **Step 4:** Review Focus 3: if T0.1c found contextual routes not resolving, `Register` adds no contextual entries; the preview page opens forms directly on `<form>-sf`, and Sf lists push DX forms — checked with Venues in T2.9.

---

## Phase 1 — Simple pages (Toolkit + stock controls; their cut-overs need the license key)

### Tasks T1.1–T1.4: Sf copies [P wave of 4, Recipe P]

| Task | Page (`UI/Pages/…`) | Replace | Parity states |
|---|---|---|---|
| T1.1 | `About/AboutPage` → `AboutSfPage` | 1 `dxe:ChoiceChipGroup` → `SfChipGroup` (choice) | P10, P14 |
| T1.2 | `BackupRestore/BackupRestorePage` → `BackupRestoreSfPage` | 3 `dx:DXButton` → Sf button styles | P10, P14 + each button's action |
| T1.3 | `Settings/SettingsPage` → `SettingsSfPage` | 4 `dx:DXButton`; 1 `dxe:PasswordEdit` → `SfTextInputLayout` password, or `OutlinedTextFieldSf IsPassword` if T0.1a marks it failed | P10, P12 (save), P14 |
| T1.4 | `Venues/VenueMapPickerPage` → `VenueMapPickerSfPage` | 1 `dxe:TextEdit` → `SfTextInputLayout` + `Entry`, or `OutlinedTextFieldSf` if failed; 1 `dx:DXButton` | P10, P12 (pick + confirm), P14 |

`VenueMapPickerSfPage` is pushed from the venue form, so its preview entry opens it directly on `venue-map-picker-sf` with no query (empty map state).

### Tasks T1.5–T1.8: Cut-overs [SEQUENTIAL, Recipe X, each after Helder's go]

T1.5 Settings changes both `ShellContent`s (`preferences`, `settings`) and the one `PageTypes` entry; X2 opens both routes (Review Focus 2). T1.6 About, T1.7 BackupRestore (flyout + `PageTypes`). T1.8 VenueMapPicker is a pushed route (AppShell.xaml.cs L27).

---

## Phase 2 — List components and list pages

### Task T2.0: List control check (time-boxed 2 h, throwaway)

Design § Decision: List control. On branch `task/spike-sflistview`, a minimal list on the Venues Sf copy using `SfListView`, checking REQ-UIMIG-03 (selected look), REQ-UIMIG-06 (tap and long-press reach the VM commands) and `CrudListView`'s load-more. Output: verdict in design § Decision: List control; implicit list style added to the T0.4a section if `SfListView` wins; **Helder confirms before T2.8a**.

### Tasks T2.1–T2.7: Components [Recipe C; waves in this order]

| Wave | Task | DX original (`UI/Components/…`) → Sf copy | Note |
|---|---|---|---|
| 2a [P] | T2.1 | `Lists/ListItemCheckbox` → `ListItemCheckboxSf` | `CheckEdit` → `SfCheckBox` (fallback stock `CheckBox`) |
| 2a [P] | T2.2a | `Lists/ListItemLeadingAvatar`, `Lists/ListItemLeadingImage` → `…Sf` | `DXImage`, `DXBorder` |
| 2a [P] | T2.3 | `States/EmptyState` → `EmptyStateSf`; `Toolbars/FloatingToolbar` → `FloatingToolbarSf` | |
| 2a [P] | T2.4 | `Menus/Menu` → `MenuSf` | shares `IMenuHost`, `MenuItemSelection`, `MenuItemShape`, `MenuResolution` (not copied) |
| 2b [P] | T2.2b | `Lists/ListItemLeadingMonogram` → `ListItemLeadingMonogramSf` | |
| 2b [P] | T2.5 | `AppBars/SearchBar` → `SearchBarSf`; `AppBars/SmallAppBar` → `SmallAppBarSf` | search field = `OutlinedTextFieldSf` (POC: `SfTextInputLayout` failed there; accepted deviation, design § Control mapping); `AppBarBase` shared |
| 2b [P] | T2.6 | `Menus/OverflowMenuButton` → `OverflowMenuButtonSf` | |
| 2c [P] | T2.7 | `AppBars/SearchAppBar` → `SearchAppBarSf`; `Lists/ListItemOverflowButton` → `ListItemOverflowButtonSf` | consume T2.5 / T2.6 |

### Task T2.8a: `CrudListViewSf` — list body [SEQUENTIAL — after T2.0 confirmed and T2.1–T2.7]

**Files:** Create `MyVocaList/UI/Components/CrudListViewSf.xaml(.cs)`. **Consumes:** Phase 2 components, T0.4a styles, T2.0 verdict. **Produces:** `CrudListViewSf : ContentView, IMenuHostSf` (ruling 2026-11-23: `IMenuHost.HostedMenu` is typed `Menu?`) with the same BindableProperties and `DataTemplate` slots as `CrudListView` (`MyVocaList/UI/Components/CrudListView.xaml(.cs)`).
- Recipe C: list = T2.0 verdict (`SfListView`, or stock `CollectionView` + a `DataTemplateSelector` choosing the selected/unselected row template + code-behind forwarding of tap and long-press to the VM commands, as `CrudListView.OnItemTap` / `OnItemLongPress`); `ShimmerView` → `SfShimmer` with token colors and no DX shimmer anywhere in the tree (REQ-UIMIG-04); `EmptyStateSf`; background below the last row = the page `Surface` token (REQ-UIMIG-02). Sheets and floating toolbar are stubs bound to nothing until T2.8b.

### Task T2.8b: `CrudListViewSf` — sheets, toolbar, menu [SEQUENTIAL — after T2.8a]

Same files. Both bottom sheets → `SfBottomSheet` with `State="{Binding …SheetState, Converter={StaticResource DxSheetStateToSf}, Mode=TwoWay}"`; `FloatingToolbarSf`, FAB with `SfIconButton`, `MenuSf` through `IMenuHostSf`. Recipe C C3–C5.

### Task T2.9: `VenuesSfPage` [SEQUENTIAL — first consumer; proves `CrudListViewSf` parity; about 2 h]

Recipe P with states P1–P11 + P14. Extra checks: Review Focus 4 (back with sheet open and rows selected → sheet closes, selection stays); Review Focus 3 (tap a row on `venues-sf` opens a working form). POC branch files are reference only.

### Tasks T2.10–T2.13: Remaining list pages [P wave of 4 — after T2.9 passes]

Recipe P: T2.10 `People/PeoplePage` → `PeopleSfPage` (P9 applies); T2.11 `Songs/SongsPage` → `SongsSfPage`; T2.12 `Artists/ArtistsPage` → `ArtistsSfPage`; T2.13 `Songs/YouTubeSearchPage` → `YouTubeSearchSfPage` (`SearchAppBarSf`; `FilterChipGroup` → `SfChipGroup` filter type; its list per the T2.0 verdict). When a second page binds to `CrudListViewSf` or any other Sf component, the orchestrator adds that component to `.claude/library/component-safety-gate.md` in the merge commit.

### Tasks T2.14–T2.18: Cut-overs [SEQUENTIAL, Recipe X, each after Helder's go]

Venues, People, Songs, Artists (flyout + `PageTypes`); YouTubeSearch (pushed route, AppShell.xaml.cs L32).

---

## Phase 3 — Form components and form pages (needs the license key)

Precondition: `SYNCFUSION_LICENSE_KEY` set by Helder. Text fields in forms use `SfTextInputLayout` where T0.1a marked it V, else `OutlinedTextFieldSf`.

| Wave | Task | Work | Note |
|---|---|---|---|
| 3a [P] | T3.1 | `Media/ImagePickerField` → `ImagePickerFieldSf` | Recipe C |
| 3a [P] | T3.2 | `TextFields/TapToEditField` → `TapToEditFieldSf` | Recipe C; `TapToEditFieldLogic.cs` shared, not copied |
| 3a [P] | T3.3 | `TextFields/AutocompleteDXField` → `AutocompleteFieldSf` | `SfAutocomplete` with a custom filter calling the same suggestion services; `AutocompleteDXFieldLogic.cs` shared. Escalate if T0.1b marked async items failed |
| 3b [P] | T3.4–T3.7 | `Venues/VenueFormPage`, `People/PersonFormPage`, `Artists/ArtistFormPage`, `Songs/SongFormPage` → `…SfPage` | Recipe P; P12, plus P13 where autocomplete; `PersonForm`/`SongForm` sheets use `SfBottomSheet` directly (no bridge); contextual routes per T0.1c |
| 3c | T3.8 | `Feedback/FeedbackPage` → `FeedbackSfPage` | `ComboBoxEdit` → `SfComboBox`; `MultilineEdit` → `SfTextInputLayout` + `Editor` or the fallback |
| — | T3.9–T3.13 | Cut-overs (pushed routes, AppShell.xaml.cs L26, L28-31) | Recipe X, each after Helder's go |

---

## Phase 4 — Shell

- **T4.1a [P]** `Sheets/WhatsNewBottomSheet`, `Sheets/UpdateAvailableBottomSheet` → `…Sf` (Recipe C). **T4.1b [P]** `Sheets/UpdateRequiredBottomSheet` → `…Sf`. Preview entries open each sheet over the preview page.
- **T4.2 [SEQUENTIAL]** `AppShell.xaml(.cs)` in place: `dx:DXScrollView` (L38) → `ScrollView`; `dx:DXButton` (L49-53) → `SfButton` with style `SfFlyoutMenuButton`; sheet constructions (L56-91) → the `…Sf` sheets; remove `xmlns:dx` (L6). Flyout parity: every menu item, grouping, selected highlight, Exit; P10, P14. Commit `refactor: AppShell on Syncfusion` — the one in-place exception, revertible in one commit.

---

## Phase 5 — Removal (**Helder approves the start explicitly; hard to reverse**)

Every briefing: at most 5 files written (deletions listed by path), build 0 errors, tests pass, one commit. Before each deletion, a grep gate for I-4 — `rtk proxy grep -rn "<TypeName>" MyVocaList Tests` shows no reference outside the files being deleted — is recorded in the task log.

- **T5.1a–T5.1d** Delete DX originals that are cut over: T5.1a the 4 Phase-1 pages; T5.1b the 5 Phase-2 pages; T5.1c the 5 Phase-3 pages; T5.1d the DX components (`Lists/`, `States/`, `Toolbars/`, `Menus/`, `AppBars/`, `Media/`, `TextFields/AutocompleteDXField`, `TextFields/TapToEditField`, `Sheets/` DX sheets, `CrudListView`), plus the unused `Sheets/ConfirmSheet` and `Lists/ListItemLeadingIcon`. Shared non-DX files (`AppBarBase`, `ListItem`, `IMenuHost` and the `Menu*` support types, the `*Logic.cs` files) stay.
- **T5.1e** Delete `UI/Components/AutocompleteField/**` and its csproj entries (L172-176); remove the Release exclusion of `UI/Pages/Samples/**` (L159-163) — the folder no longer exists on `develop`, so only the csproj entry goes.
- **T5.2 (one atomic commit)** The ViewModel type change must compile and keep the sheets working in one commit (I-1), so this briefing writes 6 files plus one test file. Log that exception to the 5-file rule in `.claude/exception-registry.md` first.
  - Builder: add `public enum SheetState { Hidden, HalfExpanded, FullExpanded }` (`MyVocaList/UI/ViewModels/SheetState.cs`); replace `BottomSheetState` in `CrudListViewModelBase.cs`, `ICrudListViewModel.cs`, `PersonsViewModel.cs`, `VenuesViewModel.cs`; `git mv MyVocaList/UI/Migration/DxSheetStateConverter.cs MyVocaList/UI/Converters/SheetStateConverter.cs`, now mapping `SheetState` ↔ the Sf state with the same table; the `App.xaml` key becomes `SheetStateToSf`, and `CrudListViewSf.xaml` uses it.
  - Tester (same commit): in `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` the type `BottomSheetState` becomes `SheetState` — `[AC] REQ-UIMIG-13`; `git mv` the converter tests to `Tests/Unit/Converters/SheetStateConverterTests.cs`, changing only the input type and class name, so the six REQ-UIMIG-07 tests survive. Diff review: no assertion, setup value or case changed.
  - Files written: `SheetState.cs`, `CrudListViewModelBase.cs`, `ICrudListViewModel.cs`, `PersonsViewModel.cs`, `VenuesViewModel.cs`, `SheetStateConverter.cs`, `App.xaml`, `CrudListViewSf.xaml`, plus the two test files.
  - **Authorized by Helder 2026-11-22 (REQ-UIMIG-13):** for `--release` to pass, the same test file's `using DevExpress.Maui.Controls;` (L1), the DevExpress package reference in `Tests/MyVocaList.Tests.csproj` (L10, edited in T5.4), and DX words in test comments (`LoadingHoldShimmerTests.cs` L74, L91; `SongFormViewModelTests.cs` L1261; `ObservableRangeCollectionTests.cs` L8) change too, along with the type-only retarget of the converter tests above. The Tester makes these edits.
- **T5.3** Remove `UseDevExpress*` (`MauiProgram.cs` L38-41) and `WarmUpDevExpressAsync` (`App.xaml.cs` L16, L87-121); rewrite the DX comments in `SongFormViewModel.cs` (L63-64, L270), `AutocompleteDXFieldLogic.cs` (L37, L100), `TapToEditFieldLogic.cs` (L5), `ICrudListViewModel.cs` (L38), `PersonsViewModel.cs` (L33), `VenuesViewModel.cs` (L32), `UI/Collections/ObservableRangeCollection.cs` (L33) and `Platforms/Android/PopupBackHandler.cs` (L9). Delete `PopupBackHandler.cs` instead if the I-4 grep shows no consumer left once `Menu` is gone. `Domain/Constants/SearchConstants.cs` L35 only cites a spec id (`REQ-DXAC-07`) and stays. Comment-only edits in more than 5 files: log that exception first.
- **T5.4** Remove `global using DevExpress.*` (`GlobalUsings.cs` L4-8), DX styles in `MaterialStyles.xaml` (including `FlyoutMenuButton`), DX packages (`MyVocaList.csproj` L121-125, `Directory.Packages.props` L68-76), the DevExpress section of `Docs/DevEnv/devenv-setup.md` § 2. Then `python .claude/scripts/migration/check_dx.py --release` → exit 0 — `[AC] REQ-UIMIG-14`.
- **T5.5a–T5.5c** Drop the `Sf` suffix — `[AC] REQ-UIMIG-15`. One rename per commit (`git mv` of the pair, then every consumer: XAML references, DI line, `AppShell.xaml(.cs)`, `NavigationConfig.cs`); after each commit the build passes and every route in `Routes.cs` and `AppShell.xaml` opens its page. A rename touches more than 5 files by nature (mechanical, no logic): log one exception for the rename series first. Groups: T5.5a components (`ListItemCheckboxSf`, `ListItemLeading{Avatar,Image,Monogram}Sf`, `EmptyStateSf`, `FloatingToolbarSf`, `MenuSf`, `OverflowMenuButtonSf`, `ListItemOverflowButtonSf`, `SearchBarSf`, `SmallAppBarSf`, `SearchAppBarSf`, `CrudListViewSf`, `OutlinedTextFieldSf`); T5.5b pages (`AboutSfPage`, `BackupRestoreSfPage`, `SettingsSfPage`, `VenueMapPickerSfPage`, `VenuesSfPage`, `PeopleSfPage`, `SongsSfPage`, `ArtistsSfPage`, `YouTubeSearchSfPage`); T5.5c forms and sheets (`ImagePickerFieldSf`, `TapToEditFieldSf`, `AutocompleteFieldSf`, the five form `…SfPage`s, the three `…BottomSheetSf`). `OutlinedTextFieldSf` becomes `OutlinedTextField`.
- **T5.6** Remove `PreviewRoutes`, `SyncfusionPreviewPage`, the `UI/Migration` folder, the preview menu item and the `--staged` hook block (keep `--release` for the launch build); delete `constraints-reference.md` § DevExpress / UI (rules file — Helder reviews). Close the item.

After the item: the iOS and Windows trials are separate, non-blocking backlog items (requirements § Out of Scope).
