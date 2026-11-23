# Design — UI library migration: DevExpress → Syncfusion (coexisting)

Status: **approved by Helder on 2026-11-22** (revised after the spec-reviewer pass of 2026-11-21).
- Requirements: `Docs/Management/cross-cutting/ui-component-library-licensing/requirements.md`.
- Evidence: `findings.md` § 7.
- The POC branch `task/spike-syncfusion` is reference material only: it is never merged, and its files may be copied into properly tested tasks.

## Approaches

| | Approach | Trade-off | Verdict |
|---|---|---|---|
| A | Copy components; switch each page **in place** once its components exist | Fewer files, but every switch edits a working page; rollback reverts page edits | Rejected |
| **B** | Copy components **and pages**; the Sf page lives on a preview route; cut over by one line; delete the DX page later | Two page versions for a while, sharing ViewModels; working pages are never edited; rollback is one line | **Chosen by Helder 2026-11-21** |
| C | Runtime switch per page (setting or flag) | Feature flags are disallowed by the code principles | Rejected |
| D | One long branch migrating everything, merged at the end | No coexistence; `develop` diverges for weeks | Rejected |

## Architecture

Only the UI layer of the `MyVocaList` app project changes. Services, Domain, Infra and Contracts are untouched, and business logic stays in Services. ViewModels are untouched until Phase 5.

```
Phase 0  Foundation   rule amendment (precondition) · packages · license key · theme mapping · check script
                      · sheet-state bridge · preview entry + contextual routes · control verification
Phase 1  Simple pages About · BackupRestore · Settings · VenueMapPicker              (Toolkit + stock only)
Phase 2  List pages   list-item Sf set · SearchBarSf · SmallAppBarSf · MenuSf · OverflowMenuButtonSf
                      · ListItemOverflowButtonSf · EmptyStateSf · FloatingToolbarSf · CrudListViewSf
                      → Venues · People · Songs · Artists ; SearchAppBarSf → YouTubeSearch
Phase 3  Form pages   ImagePickerFieldSf · TapToEditFieldSf · AutocompleteFieldSf
                      → VenueForm · PersonForm · ArtistForm · SongForm · Feedback  (needs the license key)
Phase 4  Shell        WhatsNew / UpdateAvailable / UpdateRequired sheets Sf · AppShell (in place — see Decision)
Phase 5  Removal      delete DX originals + frozen AutocompleteField folder → ViewModels to SheetState
                      → remove packages + DX init + DX styles → drop the Sf suffix → remove preview entry
                      → delete migration-only files
After    Trials       iOS simulator/device trial · Windows build trial  (non-blocking; see § Resolved questions)
```

- **Order:** each phase needs the previous one (DRY onion: shared pieces before the pages that consume them).
- **Inside a phase:** components come first in level order, then pages. Independent tasks may run as a wave of up to 4 subagents, one writer per file.
- **Sequential-only files:** wave tasks never edit `MauiProgram.cs`, `AppShell.xaml(.cs)`, `MyVocaList/Navigation/Routes.cs` or `GlobalUsings.cs`. Each task lists the registration lines it needs (DI, preview route, contextual route) in its `task-log.md` entry. The orchestrator applies them serially after the wave merges, builds and commits.

### Control mapping

**V** = verified (POC 2026-11-21, or Phase 0 control verification T0.1a–c on 2026-11-23, emulator screenshots `artifacts/parity/t0.1/`), **U** = unverified. Every **U** row is checked before any task that needs it is dispatched.

*Phase 0 note (2026-11-23):* exact Toolkit API names come from `Syncfusion.Maui.Toolkit.xml` in the nupkg, not from Context7, which was wrong in 4 places (`StateChangedEventArgs.OldState/NewState`; no `ShimmerType.RectanglePersona`; `PopupButtonAppearanceMode.TwoButton`; `Entry.IsPassword`). Several guessed suite property names crash at launch (XAML parse); the suite's real names are listed below.

| DX control | Primary replacement | Fallback | Status |
|---|---|---|---|
| `DXCollectionView` | `SfListView` (Community suite) | stock `CollectionView` + `DataTemplateSelector` for the selected row + code-behind gesture forwarding | primary **V** (T2.0, 2026-11-23; `Syncfusion.Maui.ListView` 35.2.12, arrives with `Syncfusion.Maui.Inputs`; see § Decision: List control) · fallback V (renders; tap/long-press need forwarding; no selected template) |
| `BottomSheet` | `SfBottomSheet` (Toolkit) | — | V (T0.1a): `State` (`Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState`: `Collapsed, HalfExpanded, FullExpanded, Hidden`) **plus `IsOpen`**, since setting `State` alone does not show it; half-expanded supported; a drag down from full ends in `Collapsed`; **Android Back does not close it** (it pops the page), so the Sf page closes the sheet in `OnBackButtonPressed` through the ViewModel (checked in T2.9 P10) |
| `ShimmerView` | `SfShimmer` (Toolkit) with `ShimmerBackground`/`WaveColor`/`Fill` from tokens | stock skeleton `Border`s with an opacity animation | V (T0.1a) for a built-in `Type` (`Article`, `Feed`, …); a `CustomView` skeleton renders white, so custom row skeletons use a built-in `Type` or the fallback |
| `DXButton` | `SfButton` (Toolkit) | stock `Button` / `ImageButton` + CommunityToolkit `IconTintColorBehavior` | V (T0.1a) for text and filled buttons: XAML `Command` binding fires; a disabled button needs an explicit token style; outlined needs `StrokeThickness`; icon placement via `ImageAlignment`. **Icon buttons use the fallback** (T0.4a): `IconTintColorBehavior` throws `NotSupportedException` on an `SfButton`, and the app's icons are SVGs that need tinting, so style `SfIconButton` targets a stock `ImageButton` + `IconTint.Color` (accepted deviation: Syncfusion has no tintable SVG icon button). `SfFlyoutMenuButton` stays an `SfButton`: its `Content` DataTemplate holds an `Image` tinted through `IconTint.Color` plus a `Label`, both bound to the button's `ImageSource` and `Text` (T0.4a fix round 1) |
| `TextEdit` | `SfTextInputLayout` + `Entry` (Toolkit, outlined) | stock `Entry` inside a styled `Border` (MD3 outlined field) | primary V on a plain page (T0.1a: the POC sizing failure did not reproduce on 1.0.12; the hint needs `HintLabelStyle` with a token color and `Entry.TextColor` set explicitly) · **failed** in the POC search bar, so `SearchBarSf` uses the fallback (`OutlinedTextFieldSf`) unless the primary is shown working there · fallback V |
| `MultilineEdit` | `SfTextInputLayout` + `Editor` | stock `Editor` in a styled `Border` | V (T0.1a; same hint styling as `TextEdit`) |
| `PasswordEdit` | `SfTextInputLayout` with its password toggle | stock `Entry IsPassword` + toggle `ImageButton` | V (T0.1a): `EnablePasswordVisibilityToggle="True"` on the layout + `IsPassword="True"` on the `Entry` |
| `ComboBoxEdit` | `SfComboBox` (Community suite) | — (escalate) | V (T0.1b): `ItemsSource`, `SelectedItem`, `DisplayMemberPath`, `TextMemberPath`, `Placeholder`; dark styling through `TextColor`, `Background`, `DropDownBackground`, `SelectedDropDownItemBackground` and `SelectedDropDownItemTextStyle` (needed: the selected row otherwise shows dark text on `Primary`) |
| `AutoCompleteEdit` (+ async items provider) | `SfAutocomplete` (Community suite) with a custom filter calling the existing suggestion services | — (escalate) | V (T0.1b): `Syncfusion.Maui.Inputs.IAutocompleteFilterBehavior.GetMatchingItemsAsync(SfAutocomplete source, AutocompleteFilterInfo filterInfo)` returns `Task<object>` (awaited; `filterInfo.Text`), set through `SfAutocomplete.FilterBehavior`; also `MinimumPrefixCharacters`, `TextSearchMode` |
| `ChoiceChipGroup` / `FilterChipGroup` | `SfChipGroup` (Toolkit), choice / filter type | — (escalate) | V (T0.1a): `ChipType="Choice"` (`ChoiceMode` `Single`/`SingleOrNone`) or `"Filter"` (multi, check mark); react to `SelectionChanged` (the `ChipClicked` sender is the `SfChip`, not the group); set `ChipTextColor`, `SelectedChipBackground`, `SelectedChipTextColor` from tokens |
| `CheckEdit` | `SfCheckBox` (Community suite) | stock `CheckBox` | V (T0.1b): `IsChecked` (bindable, two-way), `CheckedColor`, `UncheckedColor`, `TickColor` (not `CheckMarkColor`) |
| `DXPopup` | `SfPopup` (Toolkit) | — (escalate) | V (T0.1a): `Show()`/`Dismiss()`/`IsOpen`, `ShowRelativeToView`, templates; Android Back closes it by itself, so `PopupBackHandler` is likely unneeded (confirm on `MenuSf`) |
| `DXImage` | stock `Image` | — | V (T0.1c; tint through `IconTintColorBehavior`) |
| `DXBorder` | stock `Border` | — | V (T0.1c) |
| `DXScrollView` | stock `ScrollView` | — | V (T0.1c; renders, not swipe-tested) |
| `{dx:ThemeColor Primary}` | `{StaticResource Primary}` | — | V (T0.1c): the only usage is `ListItemCheckbox.xaml:22`; `Primary` exists in `MaterialColors.xaml` |

**Rules:**
- "Zero DevExpress" allows stock MAUI and app-built controls as fallbacks (Syncfusion first, stock when Syncfusion has no equivalent).
- If both the primary and the fallback fail, or a row says "escalate", the work stops and goes to Helder. The builder never picks another library or approach alone.
- An accepted deviation from the DX original's bindable API is listed here.

### Component port list (by phase, leaf first)

| Phase | DX original → Sf copy | DX parts replaced |
|---|---|---|
| 1 | (no shared component; pages use the Sf button and text-field styles directly) | — |
| 2 | `ListItemCheckbox` → `ListItemCheckboxSf` | `CheckEdit`, `DXButton`, `{dx:ThemeColor}` |
| 2 | `ListItemLeadingAvatar` / `ListItemLeadingImage` / `ListItemLeadingMonogram` → `…Sf` | `DXImage`, `DXBorder` |
| 2 | `EmptyState` → `EmptyStateSf`, `FloatingToolbar` → `FloatingToolbarSf`, `Menu` → `MenuSf` (keeps `IMenuHost`) | `DXButton`, `DXBorder`, `DXPopup` |
| 2 | `SearchBar` → `SearchBarSf`, `SmallAppBar` → `SmallAppBarSf`, `SearchAppBar` → `SearchAppBarSf` | `TextEdit`, `DXBorder`, `DXButton` |
| 2 | `OverflowMenuButton` → `…Sf`, then `ListItemOverflowButton` → `…Sf` | `DXButton` |
| 2 | `CrudListView` → `CrudListViewSf` | `DXCollectionView`, `ShimmerView`, `BottomSheet` ×2, `DXBorder`, `DXButton` |
| 3 | `ImagePickerField` → `…Sf`, `TapToEditField` → `…Sf`, `AutocompleteDXField` → `AutocompleteFieldSf` | `DXButton`, `DXImage`, `EditBase`, `AutoCompleteEdit` |
| 4 | `WhatsNewBottomSheet`, `UpdateAvailableBottomSheet`, `UpdateRequiredBottomSheet` → `…Sf` | `BottomSheet`, `DXButton` |

**Not copied:**
- `ListItem` and `AppBarBase` contain no DevExpress.
- `ConfirmSheet` and `ListItemLeadingIcon` have no consumers and are deleted in Phase 5.
- `CurrentSingerCard` has no consumers and no DX, so it stays.

### Page port list

| Phase | Page | DX used directly |
|---|---|---|
| 1 | About · BackupRestore · Settings · VenueMapPicker | ChoiceChipGroup · DXButton · PasswordEdit · TextEdit |
| 2 | Venues · People · Songs · Artists · YouTubeSearch | BottomSheet · DXButton · FilterChipGroup · DXCollectionView · ShimmerView |
| 3 | VenueForm · PersonForm · ArtistForm · SongForm · Feedback | TextEdit · BottomSheet · ComboBoxEdit · ShimmerView · MultilineEdit · DXButton |
| 4 | AppShell | DXScrollView · DXButton |

## Interfaces

### Sheet-state bridge (Phase 0; deleted in Phase 5)

Today, `ICrudListViewModel.ItemActionsSheetState` and `ConfirmSheetState` are DevExpress `BottomSheetState`. Changing them now would mean editing the governed DX original `CrudListView`, which approach B forbids. So during coexistence:
- **ViewModels stay unchanged.**
- **One migration-only file** bridges the state for Sf copies: `MyVocaList/UI/Migration/DxSheetStateConverter.cs`. It is an `IValueConverter` between `BottomSheetState` and the Sf sheet state, registered under the resource key `DxSheetStateToSf`.
  - Sf XAML references only that key, never a DevExpress type, so REQ-UIMIG-11 holds.
  - This file and its unit test `Tests/Unit/Migration/DxSheetStateConverterTests.cs` are the only exceptions to REQ-UIMIG-12 (both deleted in Phase 5). *(Test file added 2026-11-22 during planning: the test must name the DX enum.)*

State table (confirmed 2026-11-23, T0.1a: property `State`, enum `Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState` with `Collapsed, HalfExpanded, FullExpanded, Hidden`; the sheet shows only when `IsOpen` is also true):

| ViewModel value (DX) | Sf sheet | Back to the ViewModel when the user acts |
|---|---|---|
| `Hidden` | closed (`IsOpen=false`, `State=Hidden`) | user swipes the sheet down (`Collapsed`) or backs it closed → `Hidden` |
| `HalfExpanded` | open, half-expanded | user drags it to full → `FullExpanded` |
| `FullExpanded` | open, full | user drags it to half → `HalfExpanded` |

<!-- impl decision (orchestrator, 2026-11-23, from the T0.1a result): one converter cannot drive both State and IsOpen -->
The bridge file holds **two** converters:
- `DxSheetStateConverter` (key `DxSheetStateToSf`, bound to `State`). `ConvertBack` maps Sf `Collapsed` to DX `Hidden`.
- `DxSheetStateToIsOpenConverter` (key `DxSheetStateToSfIsOpen`, bound to `IsOpen`). `Hidden` → `false`, any other value → `true`; `ConvertBack` maps `false` → `Hidden` and `true` → `Binding.DoNothing`.

Unknown input → `Binding.DoNothing`. Android Back does not close an `SfBottomSheet`, so the Sf page's `OnBackButtonPressed` closes it through the ViewModel (see § Control mapping).

**Phase 5:**
- An app-owned `public enum SheetState { Hidden, HalfExpanded, FullExpanded }` in `MyVocaList.UI.ViewModels` replaces `BottomSheetState` in `CrudListViewModelBase` and `ICrudListViewModel`.
- The converter is deleted.
- The type-only test change in REQ-UIMIG-13 is made by the Tester role. Authorized by Helder on 2026-11-22 (REQ-UIMIG-13, § Resolved questions 1).

`PersonFormPage` and `SongFormPage` use `BottomSheetState` in page code-behind only. Their Sf copies use `SfBottomSheet` directly, so the bridge is not needed there.

### License key (Phase 0)

- An MSBuild target in `MyVocaList.csproj`, run before `CoreCompile`, writes `$(IntermediateOutputPath)SyncfusionLicense.g.cs` (under `obj/`, so untracked).
  - It contains `internal static class SyncfusionLicense { public const string Key = "<value of $(SYNCFUSION_LICENSE_KEY)>"; }`.
  - It uses `WriteOnlyWhenDifferent`, so incremental builds recompile only when the variable changes.
- `<Error Condition="'$(Configuration)' == 'Release' and '$(SYNCFUSION_LICENSE_KEY)' == ''" Text="SYNCFUSION_LICENSE_KEY is not set — required for Release builds." />` (REQ-UIMIG-18).
- `MauiProgram.CreateMauiApp` first calls `Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicense.Key)`, then `.ConfigureSyncfusionToolkit()` and the Community-suite host call. *Confirmed 2026-11-23 (T0.1b, T0.3):*
  - Host calls: `ConfigureSyncfusionCore()` (`Syncfusion.Maui.Core.Hosting`) and `ConfigureSyncfusionToolkit()` (`Syncfusion.Maui.Toolkit.Hosting`). They coexist without collision.
  - `RegisterLicense` runs only when the key is non-empty.
  - Packages: `Syncfusion.Maui.Toolkit` 1.0.12 and `Syncfusion.Maui.Inputs` / `Buttons` / `Core` / `Licensing` 35.2.12, all with a plain `net10.0` asset.
  - The 35.x packages require `Microsoft.Maui.Controls` ≥ 10.0.110, so T0.3 bumped MAUI from 10.0.41 in its own commit, with the tests and the DX pages verified.
  - A Debug build without a key showed no trial popup, banner or watermark on the emulator.
  - The test project references the app project, so `dotnet test -c Release` also needs the key.
- `.claude/library/dev-env-setup.md` documents the variable.

### Theme mapping (Phase 0)

- A new `MyVocaList/Resources/Styles/SyncfusionTheme.xaml` merges the Toolkit's `SyncfusionThemeResourceDictionary` in dark mode and overrides its color keys with the tokens in `MaterialColors.xaml` (`Surface`, `OnSurface`, `Primary`, `SurfaceContainer*`, `Outline`, …).
  - *Confirmed 2026-11-23 (T0.1a):*
    - The type is `Syncfusion.Maui.Toolkit.Themes.SyncfusionThemeResourceDictionary`, set to dark with `VisualTheme="MaterialDark"` (enum `SfVisuals`). The merged dictionary is `DarkThemeColors`.
    - Its keys are Syncfusion names, not Material tokens:
      - `Primary{Background,Foreground,Border}` and `Secondary{…}`, each with the `Disabled`, `Hovered`, `Pressed`, `Selected` and `Focused` suffixes;
      - `Content{Background,BackgroundAlt1..5,Foreground,ForegroundAlt1,…}`;
      - `Error*`, `Disabled*`, `Overlay*`, `Outlined{Normal,Hover,Focused,Disabled,Error}` and `Filled`.
    - T0.4a dumps the full key list from `new DarkThemeColors().Keys` and maps each key to a token.
  - The dictionary themes controls only in part: the popup is dark, but the text-field hint, the disabled button and the shimmer custom view are not. So the per-control styles below are required, not optional.
  - *Found in T0.4a (2026-11-23):*
    - **Two override layers.** At load, the Toolkit resolves its common keys (`PrimaryBackground`, `ContentForeground`, …) into about 654 per-control `Sf*` keys (`SfChipGroup*`, `SfTextInputLayout*`, `SfBottomSheet*`, `SfShimmer*`, `SfPopup*`, `SfButton*`, `SfEffectsView*`). Overriding only the common keys does not recolor controls. `SyncfusionTheme.xaml` therefore overrides both layers, 107 common keys and 67 control keys, each mapped to a `MaterialColors.xaml` token.
    - **The theme keys are authoritative.** Controls bind those keys dynamically, so a `Style` setter on the same property loses. Use a style setter only where no theme key exists.
    - **XAML namespaces.** Syncfusion XAML in the app uses `clr-namespace:` prefixes, not the `http://schemas.syncfusion.com/maui/toolkit` URI. The URI does not resolve in the plain `net10.0` build that the pre-commit gate compiles (XC0000).
    - **Not themed through keys.** `SfShimmer.ShimmerBackground` cannot be set from XAML (XC0001), so the `SfShimmerNormalBackground` key covers it. State layers with alpha and the `*Disabled*` keys keep the Toolkit defaults, because there is no alpha token. A disabled `SfButton` uses a `VisualStateManager` state at opacity 0.38.
    - **Toolkit upgrade.** Dump the keys again (`new DarkThemeColors().Keys`) and diff them against `SyncfusionTheme.xaml`.
- Every Sf control that paints its own background (shimmer, bottom sheet, list, popup, chips) gets its token explicitly in `MaterialStyles.xaml`, as new keyed or implicit styles targeting the Syncfusion type.
- Existing DX style keys stay untouched until Phase 5.

### Icons

The 60 Material Symbols SVGs in `Resources/Images/` stay, referenced by name. Sf copies tint them with CommunityToolkit.Maui `IconTintColorBehavior` (already a dependency), set in the new Sf button styles. This replaces `DXButton.IconColor`.

### Check script (Phase 0)

`python .claude/scripts/migration/check_dx.py --staged | --release`

- **Sf copy file:** basename matches `^[A-Za-z0-9]+(Sf|SfPage)\.(xaml|xaml\.cs|cs)$` (case-sensitive).
- **DX reference:** a line matching `DevExpress` or `schemas\.devexpress\.com`, or an XAML prefix declared with that schema (`xmlns:<p>="http://schemas.devexpress.com/maui"`, `clr-namespace:DevExpress`). The script resolves the actually declared prefixes, so a word merely containing "dx" is not a false positive.
- **`--staged`** (pre-commit):
  - fails if any staged Sf copy file contains a DX reference (REQ-UIMIG-11);
  - fails if `git diff --cached -U0` adds a line with a DX reference in any file other than `MyVocaList/UI/Migration/DxSheetStateConverter.cs` and `Tests/Unit/Migration/DxSheetStateConverterTests.cs` (REQ-UIMIG-12). Removed lines are never counted, and comment lines count like code.
- **`--release`** (Phase 5 + MVP launch build): fails if any tracked `.cs`/`.xaml` compiled by the app or test projects, or `Directory.Packages.props` / any `.csproj`, contains a DX reference (REQ-UIMIG-14). Paths excluded from the build via `<Compile Remove>` are skipped.
- Also checks that no tracked file contains the value pattern of a Syncfusion key assignment (REQ-UIMIG-17).
- **Exit codes:** 0 = clean · 1 = violations (prints `path:line: rule`) · 2 = usage or git error.

## Page structure and navigation

- **Preview routes:** each Sf page gets a preview route `<route>-sf`, registered in `AppShell.xaml.cs` inside `#if DEBUG`.
- **Preview entry:** a Debug-only flyout item, "Syncfusion preview", opens `SyncfusionPreviewPage`. That page lists the Sf pages not yet cut over, each opening its preview route.
- **Previewing forms without a runtime flag:** forms are pushed by the shared ViewModels via fixed route names (e.g. `venue-form`). In Debug, the Sf form is also registered as a **contextual route** under the Sf list's preview route, e.g. `Routing.RegisterRoute("venues-sf/venue-form", typeof(VenueFormSfPage))`. Navigating to `venue-form` from inside `venues-sf` then resolves to the Sf form, while every user-facing route still opens the DX form.
  - Shell contextual-route resolution: **does not resolve** (T0.1c, 2026-11-23).
    - With the global `venue-form` registered, `GoToAsync("venue-form")` from a page on `venues-sf` (whether a `ShellContent` or a registered route) opened the global `VenueFormPage`.
    - The page registered at `venues-sf/venue-form` never opened.
    - **The fallback below applies**, and `PreviewRoutes` registers no contextual entries (plan T0.6b step 4).
  - **Fallback if it does not resolve:** the preview page opens each Sf form directly on its own `-sf` route with a fixed sample record, and Sf lists push DX forms until their forms are cut over.
- **Cut-over (REQ-UIMIG-10):**
  - flyout page: the `ShellContent` `ContentTemplate` in `AppShell.xaml` changes from `XPage` to `XSfPage`, and so does the page's entry in `NavigationConfig.PageTypes` (`NavigationService.PushAsync` resolves menu pages through it). `SettingsPage` sits on two routes (`preferences`, `settings`); its cut-over changes both `ShellContent`s and its one `PageTypes` entry. *(Added 2026-11-22 during planning.)*
  - pushed page: `Routing.RegisterRoute(Routes.X, typeof(XPage))` changes to `typeof(XSfPage)`;
  - the cut-over commit changes nothing else. The DX page is deleted in a later commit, after Helder's explicit go.
- **Preview entry location:** the flyout menu is built by `NavigationConfig.BuildMenuGroups`, so the Debug-only "Syncfusion preview" entry is added there inside `#if DEBUG`. *(Added 2026-11-22.)*
- **Phase 5:** renames each `XSf`/`XSfPage` back to `X`/`XPage`, removes the preview entry, `SyncfusionPreviewPage`, the preview and contextual routes, and the `UI/Migration` folder.

## Interaction flows

- Unchanged by design: every flow of the DX original (open, search, tap, long-press, select all, delete with confirm, overflow menu, back handling, form load/edit/validate/save/cancel, autocomplete pick) must work the same on the Sf copy.
- Where a control raises events instead of commands (row tap and long-press, POC § 7), the Sf code-behind forwards the event to the same ViewModel command, as `CrudListView.OnItemTap`/`OnItemLongPress` already do. Code-behind holds no logic.

## Parity checklist

Run per page, on the emulator, for both the DX original and the Sf copy. Every state the page has must pass. Screenshots `dx-<page>-<state>.png` / `sf-<page>-<state>.png` are referenced in the task log.

| # | State | Applies to |
|---|---|---|
| P1 | List loaded (first screen) | list pages |
| P2 | Loading skeleton: one shimmer, dark tokens | pages with a shimmer |
| P3 | Search typed, results filtered | pages with search |
| P4 | Empty: no items | list pages |
| P5 | Empty: no search results | pages with search |
| P6 | One row selected (long-press) | list pages |
| P7 | Select all | list pages |
| P8 | Delete confirm sheet open; cancel and confirm both | list pages |
| P9 | Overflow menu open on a row | Venues, People |
| P10 | Back order: closes sheet/menu → clears selection → exit sheet | all |
| P11 | Tap row opens the form | list pages |
| P12 | Form: load existing, edit, validation message, save, cancel | form pages |
| P13 | Autocomplete: type, suggestions, pick, clear | forms with autocomplete |
| P14 | Surface colors equal the DX original on each surface: page background, area below the last row, app bar, search bar, list row (normal + selected), skeleton, bottom sheet, menu, FAB, floating toolbar, empty state | all |

## Invariants & Postconditions

- **I-1** At every commit on `develop`, every user-facing route opens a working page, and the test suite passes.
- **I-2** No Sf copy file references DevExpress (script-enforced).
- **I-3** No commit adds a DevExpress reference, except in the bridge file (script-enforced).
- **I-4** A DX original is deleted only when its Sf copy is cut over and no other file references it.
- **I-5** At the end of Phase 5, ViewModels contain no UI-library types.
- **I-6** At the end of Phase 5: no `DevExpress.*` package, no `global using DevExpress.*`, no `UseDevExpress*`, no `WarmUpDevExpressAsync`, no `dx:` styles, no `UI/Migration` folder, no preview routes.

## Governance during coexistence

- A new Sf component with no consumer is not governed. It becomes governed when its **second** consumer binds (`component-change-governance.md` § Scope trigger) and is added to `component-safety-gate.md` in that commit.
- Each Sf component must reach parity with its DX original before a second Sf page uses it. Later changes go through the four gates.
- Deleting a DX original with zero consumers is not a governed change. Cut-overs are page work.

## Key Decisions

### Decision: Rule amendment before any Sf code
**Chosen approach:** Phase 0 starts with Helder's `amend:` commit to `CLAUDE.md` (wording in § Rule changes), human-reviewed.
**Done 2026-11-22:** Helder directed the change; the `amend:` commit landed on `develop` before Phase 0.
**Alternatives considered:** Amending at Phase 5. Rejected: until the amendment lands, Sf code would violate "DevExpress first".
**Reversibility:** Easily reversible.
**Rationale:** The constitution must allow Syncfusion before Syncfusion code exists.

### Decision: Copy pages too, cut over by route (approach B)
**Chosen approach:** Sf page copies on Debug preview routes, with a one-line cut-over per page.
**Alternatives considered:** A, C and D in § Approaches.
**Reversibility:** Easily reversible per page until its DX original is deleted.
**Rationale:** Helder's constraint "without breaking the working code".

### Decision: AppShell is migrated in place (the one exception to B)
**Chosen approach:** Phase 4 edits `AppShell.xaml(.cs)` in place: `DXScrollView` → `ScrollView`, `DXButton` → the Sf button style, `new …BottomSheet()` → `new …BottomSheetSf()`.
**Alternatives considered:** An `AppShellSf` copy chosen by `App.CreateWindow` needs a runtime switch to preview, which is a flag (rejected, like C).
**Reversibility:** Easily reversible (one commit).
**Rationale:** There is exactly one root shell, and its DX use is 2 controls plus 3 sheet constructions.

### Decision: Toolkit + Community-License suite
**Chosen approach:** The Toolkit for sheets, shimmer, buttons, chips, popup and effects; the Community suite for list, autocomplete, combo box and check box.
**Alternatives considered:** Toolkit + stock MAUI only, which means building our own autocomplete and dropdown.
**Reversibility:** Reversible with effort during coexistence. **Hard to reverse after Phase 5**: it depends on staying eligible for the Community License (under US$1M revenue, ≤ 5 developers, ≤ 10 employees). If eligibility is lost, the app needs a paid Syncfusion license or another migration.
**Rationale:** Ready-made controls closest to DX for the forms (Helder, 2026-11-21).

### Decision: Sheet-state bridge, ViewModels unchanged until Phase 5
**Chosen approach:** `DxSheetStateConverter` for Sf copies; the ViewModel type changes in Phase 5.
**Alternatives considered:** (1) Change the ViewModels to `SheetState` in Phase 0, which forces edits to the governed DX original `CrudListView`. (2) Duplicate the ViewModels, which doubles the logic.
**Reversibility:** Easily reversible.
**Rationale:** Keeps every DX original untouched and the ViewModels single.

### Decision: List control for `CrudListViewSf`
**Chosen approach:** A time-boxed check at the start of Phase 2: one implementor session of at most 2 hours (aligned with the 2-hour briefing limit, 2026-11-22), throwaway, on a spike branch. It tries `SfListView` against REQ-UIMIG-03 (selected look), REQ-UIMIG-06 (tap/long-press) and the load-more behavior of `CrudListView`.
- If it meets all three, use `SfListView`.
- Otherwise use the § Control mapping fallback (stock `CollectionView` + `DataTemplateSelector` + forwarding).
- The orchestrator records the outcome in this decision; Helder confirms before `CrudListViewSf` is dispatched.

**Outcome (T2.0, 2026-11-23): `SfListView` wins; the fallback is not needed.** Checked on the emulator against the real `VenuesViewModel` (`artifacts/parity/t2.0/`):
- **Selected look (REQ-UIMIG-03).** `SelectedItemTemplate` for the selected row; `SelectionMode` toggles with the VM's `IsSelectionMode`. `SelectedItems` is get-only, so `CrudListViewSf` syncs it both ways with the VM in code-behind (view-only plumbing; pattern in the T2.0 report).
- **Gestures (REQ-UIMIG-06).** Tap and long-press reach the VM through the `ItemTapped` / `ItemLongPress` events, forwarded from code-behind. The XAML `TapCommand`/`LongPressCommand` also fire, but they pass the event args, not the item.
- **Load-more.** `LoadMoreOption="AutoOnScroll"` with `LoadMoreCommand` bound to the VM paging (20 → 200 items). `LoadMoreOption` must switch to `None` when the VM has no more items, which T2.8a handles.
- **Not tested in T2.0, so checked in T2.9 parity:** end-of-data behavior, pull-to-refresh, back clearing the selection, search and the shimmer.

**Confirmation.** Helder's confirmation before T2.8a is covered by his 2026-11-22 autonomy grant. Recorded by the orchestrator on 2026-11-23.

**Alternatives considered:** Stock `CollectionView` only, as in the POC; it failed REQ-UIMIG-03.
**Reversibility:** Reversible with effort.
**Rationale:** The selected-row look failed in the POC; the choice needs evidence.

### Decision: Phase 0 control verification
**Chosen approach:** One time-boxed task (at most one working session) verifies every **U** row of § Control mapping. It uses Context7 for the Toolkit, the official Syncfusion docs for the Community suite (Context7 has no entry for it), Microsoft Learn for Shell contextual routes, and a one-screen emulator check per control. The results update § Control mapping. Any "escalate" outcome goes to Helder before Phase 1.
**Reversibility:** Easily reversible.
**Rationale:** Prevents builders from guessing APIs (Rule 6) and finds dead ends before pages depend on them.

### Decision: Drop the `Sf` suffix at removal
**Chosen approach:** Phase 5 renames, one commit per component or page, each building and passing tests.
**Alternatives considered:** Keep the suffix forever (noise once DX is gone).
**Reversibility:** Reversible with effort (git revert per commit).
**Rationale:** REQ-UIMIG-15.

### Decision: Removal of DevExpress (Phase 5)
**Chosen approach:** In order: delete the DX originals (and the stale `UI/Pages/Samples/**` Release exclusion in the csproj; the folder itself is already gone — added 2026-11-22), convert ViewModels to `SheetState`, remove the DX init, warm-up, global usings, styles and packages, run `check_dx.py --release`, then rename.
**Reversibility:** **Hard to reverse.** After the packages are removed, restoring any DX original needs the DevExpress 25.2.4 packages to resolve from Helder's DevExpress NuGet feed, and a DevExpress license to ship it. Rollback means `git revert` of the Phase 5 commits while the feed still serves 25.2.4.
**Rationale:** US-4. **Helder approves the start of Phase 5 explicitly.**

### Decision: License key from the build environment
**Chosen approach:** An environment variable feeds a generated constant under `obj/`; a Release build fails when it is empty.
**Alternatives considered:** A committed constant (publishes the key); a runtime file (missing on device builds).
**Reversibility:** Easily reversible.
**Rationale:** REQ-UIMIG-16/17/18.

## Rule changes (drafted for Helder; `amend:` commit, Phase 0 precondition)

Proposed for `CLAUDE.md § Constitutional Constraints`. The `[Unamendable]` items change by Helder's architecture decision of 2026-11-21.

- **UI Component Priority** `[Unamendable]`:
  - old: "DevExpress first; stock MAUI only when DevExpress has no equivalent";
  - new: "Syncfusion first — Toolkit, then the Community-License suite; stock MAUI only when Syncfusion has no equivalent. No new DevExpress usage; DX originals keep their DevExpress controls until the migration's removal phase."
- **Native dialogs** `[Unamendable]`: old "use `dx:BottomSheet`"; new "use `SfBottomSheet` (DX originals keep `dx:BottomSheet` until removal)".
- **Backward compatibility:** existing DX originals stay valid under the new rule until Phase 5. The DX constraint entries in `.claude/library/constraints-reference.md § DevExpress / UI` keep applying to them, and a new Syncfusion section is added beside them. The DX section is deleted in Phase 5.
- `.claude/library/component-safety-gate.md` gains each Sf component as it becomes governed.

## Resolved questions (Helder, 2026-11-22)

1. **Test-change authorization:** granted. The Tester changes only the type `BottomSheetState` → `SheetState` in `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` in Phase 5 (REQ-UIMIG-13).
2. **Platforms:** Android is the only mandatory target, and parity is checked on Android only. After Phase 5, two non-blocking follow-up trials run:
   - a brief **iOS** trial on a simulator, then a device if one becomes available. On Windows, the iOS simulator needs a paired Mac build host, so the trial depends on access to one.
   - a **Windows** build trial. DevExpress MAUI has no Windows support; Syncfusion supports WinUI, so a Windows target becomes possible after removal.

   Sf copies avoid Android-only APIs, except where the DX original already has a platform-specific path (e.g. back handling). Publishing on iOS or Windows is decided near the Play Store launch.
3. **Frozen `UI/Components/AutocompleteField/**`:** deleted in Phase 5. The release check needs no exclusion for it.

## Remaining precondition (Helder)

- **Syncfusion account + Community License:** Helder creates them and sets `SYNCFUSION_LICENSE_KEY` before the first cut-over and before Phase 3. Phases 0–2 work with the trial notice until then.
