# Tasks — UI library migration: DevExpress → Syncfusion

Source of truth for status (Rule 4). Steps and recipes (C = component port, P = page port, X = cut-over): `plan.md`, same task ids. Spec: `requirements.md`, `design.md`.
Markers: `[ ]` available · `[~]` claimed · `[x]` committed · `[CANCELLED: reason]`. `[P]` = may run in a wave (max 4, one writer per file).
Sequential-only files are applied by the orchestrator after each wave (`plan.md` § Global Constraints). Every task also writes its `task-log.md` entry.

Done before Phase 0: the `amend:` commit making the rule "Syncfusion first" (2026-11-22).

## Phase 0 — Foundation

- [ ] **T0.1a Control verification — Toolkit** [P]
  - **Produces:** V/failed for `SfBottomSheet`, `SfShimmer`, `SfButton`, `SfTextInputLayout` (Entry/Editor/password), `SfChipGroup`, `SfPopup`; theme dictionary type + color keys; plain `net10.0` asset yes/no
  - **Consumes:** nothing
  - **Risk:** Medium — an "escalate" result blocks later phases
  - **Files owned:** its `task-log.md` entry; branch `task/spike-sf-controls-a` (the orchestrator writes the combined results into `design.md` after the wave)
  - **Demo:** each Toolkit row reads V or failed, with a source and a screenshot
  - **Review lane:** Elevated
- [ ] **T0.1b Control verification — Community suite** [P]
  - **Produces:** V/failed for `SfComboBox`, `SfAutocomplete` (async), `SfCheckBox`; NuGet ids + versions; host and license calls
  - **Consumes:** nothing
  - **Risk:** Medium — async autocomplete may need escalation
  - **Files owned:** its `task-log.md` entry; branch `task/spike-sf-controls-b` (the orchestrator writes the combined results into `design.md` after the wave)
  - **Demo:** each suite row reads V or failed; package ids and versions written in the design
  - **Review lane:** Elevated
- [ ] **T0.1c Control verification — Shell routes + stock replacements** [P]
  - **Produces:** contextual-route verdict; V/failed for `Image`, `Border`, `ScrollView`, `StaticResource` replacements
  - **Consumes:** nothing
  - **Risk:** Low — fallback is already designed
  - **Files owned:** its `task-log.md` entry; branch `task/spike-sf-controls-c` (the orchestrator writes the combined results into `design.md` after the wave)
  - **Demo:** the design states which page opened for `venue-form` from `venues-sf`
  - **Review lane:** Standard
- [ ] **T0.2 Check script `check_dx.py` + pre-commit wiring** [P]
  - **Produces:** `check_dx.py` (`--staged`, `--release`), its tests, the hook block, `artifacts/` ignored
  - **Consumes:** nothing
  - **Risk:** Medium — a false positive blocks every commit
  - **Files owned:** `.claude/scripts/migration/check_dx.py`, `.claude/scripts/migration/tests/test_check_dx.py`, `.claude/githooks/pre-commit`, `.gitignore`
  - **Demo:** staging an Sf file with `xmlns:dx` is refused by the hook
  - **Review lane:** Elevated
- [ ] **T0.3 Packages, license key, host registration** [SEQUENTIAL — after T0.1b]
  - **Produces:** Syncfusion packages; generated `SyncfusionLicense.Key`; Release error without the key
  - **Consumes:** T0.1b ids, versions, calls
  - **Risk:** Medium — the build target runs on every build
  - **Files owned:** `Directory.Packages.props`, `MyVocaList/MyVocaList.csproj`, `MyVocaList/MauiProgram.cs`, `.claude/library/dev-env-setup.md`, `Docs/DevEnv/devenv-setup.md`
  - **Demo:** a Release build without the key fails naming the variable; a Debug build runs on the emulator
  - **Review lane:** Elevated
- [ ] **T0.4a Theme dictionary and Sf styles** [SEQUENTIAL — after T0.3]
  - **Produces:** `SyncfusionTheme.xaml`; `IconTint.Color` attached property; styles `SfIconButton`, `SfTextButton`, `SfFilledButton`, `SfFlyoutMenuButton` + implicit Sf styles
  - **Consumes:** T0.1a theme type; T0.3 packages
  - **Risk:** Low — new keys only; DX keys untouched
  - **Files owned:** `MyVocaList/Resources/Styles/SyncfusionTheme.xaml`, `MyVocaList/UI/Behaviors/IconTint.cs`, `MyVocaList/Resources/Styles/MaterialStyles.xaml`, `MyVocaList/App.xaml`
  - **Demo:** shimmer, sheet, chips and an icon button render in the dark tokens
  - **Review lane:** Standard
- [ ] **T0.4b `OutlinedTextFieldSf`** [SEQUENTIAL — after T0.4a]
  - **Produces:** `OutlinedTextFieldSf` (Text, Placeholder, IsPassword, Keyboard, ReturnCommand, ClearButtonVisibility, Completed)
  - **Consumes:** T0.4a tokens and styles
  - **Risk:** Low — stock `Border` + `Entry`, proven in the POC
  - **Files owned:** `MyVocaList/UI/Components/TextFields/OutlinedTextFieldSf.xaml(.cs)`
  - **Demo:** keyboard opens, text binds, outlined look in dark tokens
  - **Review lane:** Standard
- [ ] **T0.5 Sheet-state bridge** [P — after T0.3; not in a wave with T0.4a]
  - **Produces:** `DxSheetStateConverter` (key `DxSheetStateToSf`)
  - **Consumes:** T0.1a Sf state enum; T0.3 packages
  - **Risk:** Low — one converter, unit-tested
  - **Files owned:** `MyVocaList/UI/Migration/DxSheetStateConverter.cs`, `Tests/Unit/Migration/DxSheetStateConverterTests.cs` (`App.xaml` resource and any `Tests/MyVocaList.Tests.csproj` reference → orchestrator)
  - **Demo:** six state-transition tests pass
  - **Review lane:** Standard
- [ ] **T0.6a Preview page and route registry** [SEQUENTIAL — after T0.3]
  - **Produces:** `PreviewRoutes` (`Entries`, `Register()`), `SyncfusionPreviewPage` — Debug only
  - **Consumes:** T0.1c contextual-route verdict
  - **Risk:** Low
  - **Files owned:** `MyVocaList/UI/Migration/PreviewRoutes.cs`, `MyVocaList/UI/Migration/SyncfusionPreviewPage.xaml(.cs)`, `MyVocaList/MyVocaList.csproj` (Release exclusion)
  - **Demo:** the page builds and lists no entries yet
  - **Review lane:** Standard
- [ ] **T0.6b Preview wiring** [SEQUENTIAL — after T0.6a]
  - **Produces:** flyout entry "Syncfusion preview" in Debug; nothing of it in Release
  - **Consumes:** `PreviewRoutes.Register()`
  - **Risk:** Medium — must vanish from Release (REQ-UIMIG-09)
  - **Files owned:** `MyVocaList/AppShell.xaml.cs`, `MyVocaList/Navigation/NavigationConfig.cs`
  - **Demo:** the Debug flyout shows the entry; the Release dll has no `syncfusion-preview` or `venues-sf` string
  - **Review lane:** Elevated

## Phase 1 — Simple pages [after Phase 0]

Each copy task: **Consumes** T0.3–T0.6b (packages, styles, `OutlinedTextFieldSf`, preview registry) · **Produces** the Sf page on its preview route · Recipe P · Lane Standard. Each cut-over: Recipe X · Lane Standard · **Consumes** the copy's passed parity, Helder's go and the key.

- [ ] **T1.1 `AboutSfPage`** [P] — Files: `MyVocaList/UI/Pages/About/AboutSfPage.xaml(.cs)` · Risk: Low · Demo: preview opens About on Syncfusion; P10, P14 match
- [ ] **T1.2 `BackupRestoreSfPage`** [P] — Files: `MyVocaList/UI/Pages/BackupRestore/BackupRestoreSfPage.xaml(.cs)` · Risk: Medium (backup actions) · Demo: each button runs its action; P10, P14 match
- [ ] **T1.3 `SettingsSfPage`** [P] — Files: `MyVocaList/UI/Pages/Settings/SettingsSfPage.xaml(.cs)` · Risk: Medium (password field) · Demo: save works; P10, P12, P14 match
- [ ] **T1.4 `VenueMapPickerSfPage`** [P] — Files: `MyVocaList/UI/Pages/Venues/VenueMapPickerSfPage.xaml(.cs)` · Risk: Medium (map pick) · Demo: pick + confirm on `venue-map-picker-sf`
- [ ] **T1.5 Cut over Settings** [SEQUENTIAL] — Files: `AppShell.xaml` (both `ShellContent`s), `NavigationConfig.cs`, `PreviewRoutes.cs` · Risk: Low · Demo: `preferences`, `settings` and PushAsync all open the Sf page; no trial notice (first cut-over)
- [ ] **T1.6 Cut over About** [SEQUENTIAL] — Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Risk: Low · Demo: flyout and PushAsync open the Sf page
- [ ] **T1.7 Cut over BackupRestore** [SEQUENTIAL] — Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Risk: Low · Demo: as T1.6
- [ ] **T1.8 Cut over VenueMapPicker** [SEQUENTIAL] — Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Risk: Low · Demo: the venue form opens the Sf picker

## Phase 2 — List components and list pages [after Phase 1 copies]

Component tasks: Recipe C · **Produces** the named Sf component with the DX original's bindable API · Lane Standard unless noted.

- [ ] **T2.0 List control check (2 h, throwaway)** [SEQUENTIAL] — Produces: verdict in `design.md` § Decision: List control · Consumes: T0.3, T0.4a · Files: `design.md`; branch `task/spike-sflistview` · Risk: Medium · Demo: verdict recorded; **Helder confirms** · Lane: Architectural
- [ ] **T2.1 `ListItemCheckboxSf`** [P, wave 2a] — Consumes: T0.4a · Files: `MyVocaList/UI/Components/Lists/ListItemCheckboxSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.2a `ListItemLeadingAvatarSf`, `ListItemLeadingImageSf`** [P, wave 2a] — Consumes: T0.4a · Files: `MyVocaList/UI/Components/Lists/ListItemLeadingAvatarSf.xaml(.cs)`, `…/ListItemLeadingImageSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.3 `EmptyStateSf`, `FloatingToolbarSf`** [P, wave 2a] — Consumes: T0.4a · Files: `MyVocaList/UI/Components/States/EmptyStateSf.xaml(.cs)`, `MyVocaList/UI/Components/Toolbars/FloatingToolbarSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.4 `MenuSf`** [P, wave 2a] — Consumes: T0.4a, `IMenuHost` · Files: `MyVocaList/UI/Components/Menus/MenuSf.xaml(.cs)` · Risk: Medium (popup back handling) · Demo: builds; check script clean
- [ ] **T2.2b `ListItemLeadingMonogramSf`** [P, wave 2b] — Consumes: T0.4a · Files: `MyVocaList/UI/Components/Lists/ListItemLeadingMonogramSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.5 `SearchBarSf`, `SmallAppBarSf`** [P, wave 2b] — Consumes: T0.4b, `AppBarBase` · Files: `MyVocaList/UI/Components/AppBars/SearchBarSf.xaml(.cs)`, `…/SmallAppBarSf.xaml(.cs)` · Risk: Medium (keyboard, REQ-UIMIG-05) · Demo: builds; check script clean
- [ ] **T2.6 `OverflowMenuButtonSf`** [P, wave 2b] — Consumes: T0.4a · Files: `MyVocaList/UI/Components/Menus/OverflowMenuButtonSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.7 `SearchAppBarSf`, `ListItemOverflowButtonSf`** [P, wave 2c — after T2.5, T2.6] — Consumes: `SearchBarSf`, `SmallAppBarSf`, `OverflowMenuButtonSf` · Files: `MyVocaList/UI/Components/AppBars/SearchAppBarSf.xaml(.cs)`, `MyVocaList/UI/Components/Lists/ListItemOverflowButtonSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean
- [ ] **T2.8a `CrudListViewSf` — list body** [SEQUENTIAL — after T2.0 confirmed, T2.1–T2.7] — Produces: `CrudListViewSf : ContentView, IMenuHost` (same bindables and template slots as `CrudListView`) · Consumes: all Phase 2 components, T2.0 verdict · Files: `MyVocaList/UI/Components/CrudListViewSf.xaml(.cs)` · Risk: High (selection, shimmer) · Demo: builds; check script clean · Lane: Elevated
- [ ] **T2.8b `CrudListViewSf` — sheets, toolbar, menu** [SEQUENTIAL — after T2.8a] — Consumes: `DxSheetStateToSf`, `FloatingToolbarSf`, `MenuSf` · Files: same as T2.8a · Risk: High (sheet state) · Demo: builds; check script clean · Lane: Elevated
- [ ] **T2.9 `VenuesSfPage`** [SEQUENTIAL — first consumer] — Produces: Venues on `venues-sf` · Consumes: `CrudListViewSf`, `SmallAppBarSf`, `SearchAppBarSf`, `VenuesViewModel` · Files: `MyVocaList/UI/Pages/Venues/VenuesSfPage.xaml(.cs)` · Risk: High · Demo: P1–P11 + P14 match DX; Review Focus 3 and 4 checked · Lane: Elevated
- [ ] **T2.10 `PeopleSfPage`** [P, wave after T2.9] — Consumes: `CrudListViewSf`, app bars, `PersonsViewModel` · Files: `MyVocaList/UI/Pages/People/PeopleSfPage.xaml(.cs)` · Risk: Medium · Demo: list states + P9 match DX · Lane: Standard
- [ ] **T2.11 `SongsSfPage`** [P, wave after T2.9] — Consumes: `CrudListViewSf`, app bars, `SongsViewModel` · Files: `MyVocaList/UI/Pages/Songs/SongsSfPage.xaml(.cs)` · Risk: Medium · Demo: list states match DX · Lane: Standard
- [ ] **T2.12 `ArtistsSfPage`** [P, wave after T2.9] — Consumes: `CrudListViewSf`, app bars, `ArtistsViewModel` · Files: `MyVocaList/UI/Pages/Artists/ArtistsSfPage.xaml(.cs)` · Risk: Medium · Demo: list states match DX · Lane: Standard
- [ ] **T2.13 `YouTubeSearchSfPage`** [P, wave after T2.9] — Consumes: `SearchAppBarSf`, T2.0 list verdict, `YouTubeSearchViewModel` · Files: `MyVocaList/UI/Pages/Songs/YouTubeSearchSfPage.xaml(.cs)` · Risk: Medium (filter chips) · Demo: search + chips match DX · Lane: Standard
- [ ] **T2.14 Cut over Venues** [SEQUENTIAL] — Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Demo: flyout and PushAsync open the Sf page
- [ ] **T2.15 Cut over People** [SEQUENTIAL] — Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Demo: the `people` flyout item and PushAsync open the Sf page
- [ ] **T2.16 Cut over Songs** [SEQUENTIAL] — Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Demo: the `songs` flyout item and PushAsync open the Sf page
- [ ] **T2.17 Cut over Artists** [SEQUENTIAL] — Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml`, `NavigationConfig.cs`, `PreviewRoutes.cs` · Demo: the `artists` flyout item and PushAsync open the Sf page
- [ ] **T2.18 Cut over YouTubeSearch** [SEQUENTIAL] — Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the song form opens the Sf search

## Phase 3 — Forms [after Phase 2 copies; key set]

- [ ] **T3.1 `ImagePickerFieldSf`** [P, wave 3a] — Recipe C · Consumes: T0.4a · Files: `MyVocaList/UI/Components/Media/ImagePickerFieldSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean · Lane: Standard
- [ ] **T3.2 `TapToEditFieldSf`** [P, wave 3a] — Recipe C · Consumes: T0.4a, `TapToEditFieldLogic` · Files: `MyVocaList/UI/Components/TextFields/TapToEditFieldSf.xaml(.cs)` · Risk: Low · Demo: builds; check script clean · Lane: Standard
- [ ] **T3.3 `AutocompleteFieldSf`** [P, wave 3a] — Recipe C · Consumes: T0.1b verdict, `AutocompleteDXFieldLogic`, suggestion services · Files: `MyVocaList/UI/Components/TextFields/AutocompleteFieldSf.xaml(.cs)` · Risk: High (async suggestions) · Demo: builds; check script clean · Lane: Elevated
- [ ] **T3.4 `VenueFormSfPage`** [P, wave 3b] — Recipe P · Consumes: T3.1–T3.3, `VenueFormViewModel` · Files: `MyVocaList/UI/Pages/Venues/VenueFormSfPage.xaml(.cs)` · Risk: Medium · Demo: P12 (+ P13) match DX · Lane: Standard
- [ ] **T3.5 `PersonFormSfPage`** [P, wave 3b] — Recipe P · Consumes: T3.1–T3.3, `PersonFormViewModel` · Files: `MyVocaList/UI/Pages/People/PersonFormSfPage.xaml(.cs)` · Risk: Medium · Demo: as T3.4 · Lane: Standard
- [ ] **T3.6 `ArtistFormSfPage`** [P, wave 3b] — Recipe P · Consumes: T3.1–T3.3, `ArtistFormViewModel` · Files: `MyVocaList/UI/Pages/Artists/ArtistFormSfPage.xaml(.cs)` · Risk: Medium · Demo: as T3.4 · Lane: Standard
- [ ] **T3.7 `SongFormSfPage`** [P, wave 3b] — Recipe P · Consumes: T3.1–T3.3, `SongFormViewModel` · Files: `MyVocaList/UI/Pages/Songs/SongFormSfPage.xaml(.cs)` · Risk: Medium · Demo: as T3.4 · Lane: Standard
- [ ] **T3.8 `FeedbackSfPage`** [SEQUENTIAL] — Recipe P · Consumes: T0.1b `SfComboBox` verdict, `FeedbackViewModel` · Files: `MyVocaList/UI/Pages/Feedback/FeedbackSfPage.xaml(.cs)` · Risk: Medium · Demo: send feedback works; P12 matches · Lane: Standard
- [ ] **T3.9 Cut over VenueForm** [SEQUENTIAL] — Recipe X · Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the list opens the Sf form
- [ ] **T3.10 Cut over PersonForm** [SEQUENTIAL] — Recipe X · Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the singers list opens the Sf page
- [ ] **T3.11 Cut over ArtistForm** [SEQUENTIAL] — Recipe X · Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the artists list opens the Sf page
- [ ] **T3.12 Cut over SongForm** [SEQUENTIAL] — Recipe X · Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the songs list opens the Sf page
- [ ] **T3.13 Cut over Feedback** [SEQUENTIAL] — Recipe X · Consumes: the copy's parity, Helder's go · Files: `AppShell.xaml.cs`, `PreviewRoutes.cs` · Demo: the Preferences page's feedback action opens the Sf page

## Phase 4 — Shell [after Phase 3 cut-overs]

- [ ] **T4.1a `WhatsNewBottomSheetSf`, `UpdateAvailableBottomSheetSf`** [P] — Recipe C · Consumes: T0.4a · Files: `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheetSf.xaml(.cs)`, `…/UpdateAvailableBottomSheetSf.xaml(.cs)` · Risk: Low · Demo: each opens from the preview page · Lane: Standard
- [ ] **T4.1b `UpdateRequiredBottomSheetSf`** [P] — Recipe C · Consumes: T0.4a · Files: `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheetSf.xaml(.cs)` · Risk: Low · Demo: opens from the preview page · Lane: Standard
- [ ] **T4.2 AppShell on Syncfusion (in place)** [SEQUENTIAL] — Consumes: T4.1a, T4.1b, `SfFlyoutMenuButton` · Files: `MyVocaList/AppShell.xaml`, `MyVocaList/AppShell.xaml.cs` · Risk: High (every navigation) · Demo: flyout and sheets match DX; P10, P14 · Lane: Elevated

## Phase 5 — Removal [**Helder approves the start explicitly; hard to reverse**]

Each deletion briefing records the I-4 grep gate first. Paths are under `MyVocaList/` unless noted; each `X` means `X.xaml` + `X.xaml.cs`.

- [ ] **T5.1a Delete the 4 Phase-1 DX pages** [SEQUENTIAL]
  - **Produces:** nothing (deletions) · **Consumes:** T1.5–T1.8 cut-overs · **Risk:** Low
  - **Files owned (deleted):** `UI/Pages/About/AboutPage`, `UI/Pages/BackupRestore/BackupRestorePage`, `UI/Pages/Settings/SettingsPage`, `UI/Pages/Venues/VenueMapPickerPage`; their DI lines in `MauiProgram.cs`
  - **Demo:** builds; tests pass; every Phase-1 route opens its Sf page · **Review lane:** Elevated
- [ ] **T5.1b Delete the 5 Phase-2 DX pages** [SEQUENTIAL]
  - **Produces:** nothing · **Consumes:** T2.14–T2.18 · **Risk:** Low
  - **Files owned (deleted):** `UI/Pages/Venues/VenuesPage`, `UI/Pages/People/PeoplePage`, `UI/Pages/Songs/SongsPage`, `UI/Pages/Artists/ArtistsPage`, `UI/Pages/Songs/YouTubeSearchPage`; their DI lines
  - **Demo:** as T5.1a for the list routes · **Review lane:** Elevated
- [ ] **T5.1c Delete the 5 Phase-3 DX pages** [SEQUENTIAL]
  - **Produces:** nothing · **Consumes:** T3.9–T3.13 · **Risk:** Low
  - **Files owned (deleted):** `UI/Pages/Venues/VenueFormPage`, `UI/Pages/People/PersonFormPage`, `UI/Pages/Artists/ArtistFormPage`, `UI/Pages/Songs/SongFormPage`, `UI/Pages/Feedback/FeedbackPage`; their DI lines
  - **Demo:** as T5.1a for the form routes · **Review lane:** Elevated
- [ ] **T5.1d Delete DX components and unused components** [SEQUENTIAL]
  - **Produces:** nothing · **Consumes:** T5.1a–c, T4.2 · **Risk:** Medium (a missed consumer breaks the build)
  - **Files owned (deleted):** `UI/Components/CrudListView`; `Lists/ListItemCheckbox`, `Lists/ListItemLeadingAvatar`, `Lists/ListItemLeadingImage`, `Lists/ListItemLeadingMonogram`, `Lists/ListItemOverflowButton`, `Lists/ListItemLeadingIcon`; `States/EmptyState`; `Toolbars/FloatingToolbar`; `Menus/Menu`, `Menus/OverflowMenuButton`; `AppBars/SearchBar`, `AppBars/SmallAppBar`, `AppBars/SearchAppBar`; `Media/ImagePickerField`; `TextFields/AutocompleteDXField`, `TextFields/TapToEditField`; `Sheets/ConfirmSheet`, `Sheets/WhatsNewBottomSheet`, `Sheets/UpdateAvailableBottomSheet`, `Sheets/UpdateRequiredBottomSheet` (all under `UI/Components/`). Kept: `ListItem`, `AppBarBase.cs`, `IMenuHost.cs`, `MenuItemSelection.cs`, `MenuItemShape.cs`, `MenuResolution.cs`, `AutocompleteDXFieldLogic.cs`, `TapToEditFieldLogic.cs`, `CurrentSingerCard`, `SnackbarComponent.cs`
  - **Demo:** builds; tests pass · **Review lane:** Elevated
- [ ] **T5.1e Delete the frozen AutocompleteField folder and stale csproj entries** [SEQUENTIAL]
  - **Produces:** nothing · **Consumes:** nothing · **Risk:** Low
  - **Files owned:** `UI/Components/AutocompleteField/**` (deleted); `MyVocaList.csproj` (L159-163 Samples exclusion, L172-176 AutocompleteField exclusions)
  - **Demo:** Debug and Release builds pass · **Review lane:** Standard
- [ ] **T5.2 `SheetState`; ViewModels off DevExpress; converter retargeted; type-only test edits (Tester, same commit)** [SEQUENTIAL — exception to the 5-file limit logged first]
  - **Produces:** `SheetState` enum; `SheetStateConverter` (key `SheetStateToSf`) · **Consumes:** T5.1d, REQ-UIMIG-13 authorization (granted 2026-11-22) · **Risk:** High (sheet state in every list)
  - **Files owned:** `UI/ViewModels/SheetState.cs`, `UI/ViewModels/CrudListViewModelBase.cs`, `UI/ViewModels/ICrudListViewModel.cs`, `UI/ViewModels/PersonsViewModel.cs`, `UI/ViewModels/VenuesViewModel.cs`, `UI/Converters/SheetStateConverter.cs` (moved), `App.xaml`, `UI/Components/CrudListViewSf.xaml`; Tester: `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`, `Tests/Unit/Converters/SheetStateConverterTests.cs` (moved)
  - **Demo:** tests pass; sheets open and close on Venues · **Review lane:** Architectural
- [ ] **T5.3 Remove DX init and warm-up; rewrite DX comments in shared files** [SEQUENTIAL — exception logged first]
  - **Produces:** no DX call at startup · **Consumes:** T5.2 · **Risk:** Medium (startup path)
  - **Files owned:** `MauiProgram.cs`, `App.xaml.cs`, `UI/ViewModels/SongFormViewModel.cs`, `UI/ViewModels/ICrudListViewModel.cs`, `UI/ViewModels/PersonsViewModel.cs`, `UI/ViewModels/VenuesViewModel.cs`, `UI/Components/TextFields/AutocompleteDXFieldLogic.cs`, `UI/Components/TextFields/TapToEditFieldLogic.cs`, `UI/Collections/ObservableRangeCollection.cs`, `Platforms/Android/PopupBackHandler.cs`
  - **Demo:** app starts on the emulator; no `UseDevExpress` or warm-up left · **Review lane:** Elevated
- [ ] **T5.4 Remove DX global usings, styles, packages, setup docs; release check = 0** [SEQUENTIAL — after T5.3]
  - **Produces:** a DevExpress-free solution · **Consumes:** T5.3, REQ-UIMIG-13 authorization (granted 2026-11-22) · **Risk:** High (packages)
  - **Files owned:** `GlobalUsings.cs`, `Resources/Styles/MaterialStyles.xaml`, `MyVocaList.csproj`, `Directory.Packages.props` (repo root), `Tests/MyVocaList.Tests.csproj`, `Docs/DevEnv/devenv-setup.md` § 2; Tester: the authorized test-comment and `using` lines
  - **Demo:** `python .claude/scripts/migration/check_dx.py --release` exits 0 · **Review lane:** Elevated
- [ ] **T5.5a Drop the `Sf` suffix — components** [SEQUENTIAL — one commit per rename; exception logged once]
  - **Produces:** components under their original names · **Consumes:** T5.4 · **Risk:** Medium (many references)
  - **Files owned:** each renamed pair plus its consumers and registration lines (list in `plan.md` § Phase 5)
  - **Demo:** builds and tests pass after each commit · **Review lane:** Standard
- [ ] **T5.5b Drop the `Sf` suffix — pages** [SEQUENTIAL — one commit per rename]
  - **Produces:** pages under their original names · **Consumes:** T5.5a · **Risk:** Medium
  - **Files owned:** each renamed page pair, `MauiProgram.cs`, `AppShell.xaml(.cs)`, `NavigationConfig.cs`
  - **Demo:** every route in `Routes.cs` and `AppShell.xaml` opens its page · **Review lane:** Standard
- [ ] **T5.5c Drop the `Sf` suffix — forms and sheets** [SEQUENTIAL — one commit per rename]
  - **Produces:** forms and sheets under their original names · **Consumes:** T5.5b · **Risk:** Medium
  - **Files owned:** each renamed pair, `MauiProgram.cs`, `AppShell.xaml.cs`
  - **Demo:** every form route opens; sheets open at startup checks · **Review lane:** Standard
- [ ] **T5.6 Remove preview infrastructure and the migration folder; close the item** [SEQUENTIAL]
  - **Produces:** no preview code; item closed · **Consumes:** T5.5c · **Risk:** Low
  - **Files owned:** `UI/Migration/**` (deleted), `AppShell.xaml.cs`, `Navigation/NavigationConfig.cs`, `MyVocaList.csproj`, `.claude/githooks/pre-commit` (`--staged` block), `.claude/library/constraints-reference.md` § DevExpress / UI (Helder reviews)
  - **Demo:** Release build has no preview code; the backlog row shows ✅ · **Review lane:** Standard
