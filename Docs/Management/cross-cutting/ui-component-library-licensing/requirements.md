# Requirements — UI library migration: DevExpress → Syncfusion (coexisting)

Status: **draft for review, revised after spec-reviewer pass** (2026-11-21).

Decisions by Helder on 2026-11-21:
- migrate to Syncfusion, using the Toolkit plus the Community-License suite;
- zero DevExpress before the MVP launch;
- copy components **and** pages, then cut over page by page (approach B in `design.md`).

Evidence: `Docs/Management/cross-cutting/ui-component-library-licensing/findings.md` (§ 1–6 licensing, § 7 POC).

## Why

- The DevExpress free offer ended on 2024-12-31 and never covered the 25.2.4 packages this app builds with. Staying would cost about US$2.3k per year.
- The Syncfusion Community License and the MIT Syncfusion Toolkit are free while Helder stays eligible.
- The app must keep working at every commit while the migration runs.

## Domain Vocabulary

| Term | Meaning |
|---|---|
| **DX original** | An existing page or component built on DevExpress controls, as on `develop` at 2026-11-21. |
| **Sf copy** | A new page or component that replaces a DX original using Syncfusion or stock MAUI controls. Its class and file name end in `Sf` or `SfPage` (e.g. `CrudListViewSf.xaml`, `VenuesSfPage.xaml.cs`). |
| **Syncfusion Toolkit** | The MIT-licensed NuGet package `Syncfusion.Maui.Toolkit` (no license key). |
| **Community-License suite** | The commercial Syncfusion .NET MAUI packages (e.g. list view, autocomplete, combo box). Free under the Syncfusion Community License, but they need a registered license key. |
| **Coexistence period** | From the first Sf copy to the removal of the last DevExpress package. |
| **Preview entry** | A Debug-build-only flyout entry, "Syncfusion preview", listing the Sf pages that are not yet cut over. |
| **Preview route** | The Debug-only Shell route of an Sf page that is not cut over: the original route plus `-sf` (e.g. `venues-sf`). |
| **Cut-over** | The single change that makes a page's user-facing route open its Sf copy instead of its DX original. |
| **Parity checklist** | The fixed list of screen states P1–P14 in `design.md` § Parity checklist, compared between the DX original and the Sf copy before a cut-over. |
| **Exit sheet** | The bottom sheet "Exit MyVocaList?" shown on back from a root page with nothing open and nothing selected. |
| **Governed component** | As defined in `.claude/rules/component-change-governance.md`: a custom component with 2 or more consumers. |
| **Removal** | Phase 5: deleting the DX originals, the DevExpress packages and every DevExpress reference after the last cut-over. |
| **MVP launch** | The first store release build of MyVocaList (Play Store / Galaxy Store). |

## User stories

- **US-1** As a host using the app, I want every screen to look and behave as it does today after it moves to Syncfusion, so that the migration changes nothing I rely on.
- **US-2** As Helder, I want to open the Sf copy and the DX original of the same page in one Debug build, so that I can compare them on the device before a page switches.
- **US-3** As Helder, I want each page's switch to be a single change that I can revert, so that a regression found after the switch is undone in one commit.
- **US-4** As Helder, I want the MVP release build to contain no DevExpress code or package, so that the app ships without a DevExpress license.
- **US-5** As Helder, I want the Syncfusion license key kept out of the repository, so that the key is not published with the source.

## Acceptance criteria

The last column says how each criterion is verified.
- **E** is an emulator parity check: screenshots in `task-log.md`, plus Helder's device check before a cut-over.
- **S** is the repository check script.
- **U** is a unit test.
- **B** is a build check.

### Parity (US-1)

| ID | Criterion | Verify |
|---|---|---|
| **REQ-UIMIG-01** | Given a page with both a DX original and an Sf copy, when each parity checklist state that applies to the page is reproduced on both, then both show the same items in the same order, the same visible actions, and the same page after each action. | E |
| **REQ-UIMIG-02** | Given an Sf copy, on each surface listed in `design.md` § Parity checklist P14, the system SHALL show the same `MaterialColors.xaml` token as the DX original on that surface. *(Covers the white background below the list seen on 2026-11-21.)* | E |
| **REQ-UIMIG-03** | Given a list page in selection mode with N ≥ 1 items selected, when the list is shown, then each selected row shows the DX original's selected look (checked checkbox, selected container color) and each unselected row the unselected look. *(Covers "20 selected" with no row marked.)* | E |
| **REQ-UIMIG-04** | WHILE an Sf copy is loading its data, the system SHALL show exactly one loading skeleton, colored with `MaterialColors.xaml` tokens, and no DevExpress `ShimmerView`. *(Covers the DX shimmer flashing before the Sf one.)* | E |
| **REQ-UIMIG-05** | Given the search bar of an Sf copy, in normal and in selection mode, then it shows a leading search icon, the placeholder in `OnSurfaceVariant`, no underline, and a clear button once text is typed. | E |
| **REQ-UIMIG-06** | Given an Sf list copy: a tap on a row opens the same form as on the DX original; a long-press on a row starts selection mode with that row selected. | E |
| **REQ-UIMIG-07** | Given an Sf copy, the Android back button behaves in this order: with a bottom sheet or menu open, it closes it; else, in selection mode, it clears the selection; else, on a root page, it opens the exit sheet. | E |

### Coexistence (US-2, US-3)

| ID | Criterion | Verify |
|---|---|---|
| **REQ-UIMIG-08** | WHILE a page is not cut over, the system SHALL open its DX original from every user-facing route. Its Sf copy SHALL be reachable only from the preview entry. | E |
| **REQ-UIMIG-09** | The system SHALL include the preview entry and the preview routes only in Debug builds. | B (Release build has no `-sf` route) |
| **REQ-UIMIG-10** | Given a page whose Sf copy passed the parity checklist and Helder's explicit go, when it is cut over, then the commit changes only the page class referenced at that page's user-facing route: the `ShellContent` template in `AppShell.xaml` for a flyout page, or the `Routing.RegisterRoute` type for a pushed page. Reverting that commit opens the DX original again. | E + diff review |
| **REQ-UIMIG-11** | The system SHALL NOT reference a DevExpress namespace, type or XAML schema in any Sf copy file (name pattern in `design.md` § Check script). | S |
| **REQ-UIMIG-12** | WHILE the coexistence period lasts, no commit SHALL add a line that references DevExpress. The only exception is the bridge file listed in `design.md` § Sheet-state bridge. | S |
| **REQ-UIMIG-13** | The system SHALL keep the full test suite passing at every commit of the migration. The one authorized test change is in `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (Phase 5): the type `BottomSheetState` becomes `SheetState`, and nothing else changes — no assertion, setup value or test case is removed or weakened. *[Authorization pending Helder — `design.md` § Open questions 1.]* | U + diff review |

### Release without DevExpress (US-4)

| ID | Criterion | Verify |
|---|---|---|
| **REQ-UIMIG-14** | WHEN the MVP launch build is produced, the solution SHALL reference no `DevExpress.*` package, and no compiled C# or XAML file SHALL reference a DevExpress namespace, type or XAML schema. | S (`--release`) + B |
| **REQ-UIMIG-15** | After removal, each Sf copy carries the name of the DX original it replaced, and every page is reachable from the same routes as on 2026-11-21 (`MyVocaList/Navigation/Routes.cs`, `AppShell.xaml`). | E + S |

### License key (US-5)

| ID | Criterion | Verify |
|---|---|---|
| **REQ-UIMIG-16** | WHEN the app starts, the system SHALL register the Syncfusion license key before the first Syncfusion control is created. | E (no trial notice once the key is set) |
| **REQ-UIMIG-17** | The system SHALL take the license key from the environment variable `SYNCFUSION_LICENSE_KEY` at build time. The key never appears in a tracked file. | S |
| **REQ-UIMIG-18** | IF `SYNCFUSION_LICENSE_KEY` is empty, THEN a Debug build SHALL build and run (a Syncfusion trial notice is acceptable), and a Release build SHALL fail with an error naming the variable. | B |

Note: a Release app binary contains the key. That is inherent to client-side license registration; REQ-UIMIG-17 only keeps it out of the repository.

## Validation rules

- Parity is judged on the dark "Karaoke Neon" palette only; the app has no light theme today.
- Parity is judged on the Android emulator AVD `pixel_5_-_api_36_0_1`, plus Helder's own device.
- **Cut-over gate:** a page is cut over only after (a) every applicable parity state passes on the emulator, with screenshots in the task log, and (b) Helder has given an explicit go after checking on his device.
- **Before the first cut-over:** `SYNCFUSION_LICENSE_KEY` is set, so that no trial notice covers the parity screenshots.
- **Every Sf page:**
  - declares `SafeAreaEdges` — `"All"` on pages with text input, otherwise `"Container"`;
  - shows no native dialog (`DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`);
  - reuses the DX original's ViewModel, so list pages keep `CrudListViewModelBase` and the loading pattern (`code-principles.md § Loading pattern`);
  - puts no business logic in code-behind; code-behind only forwards events to ViewModel commands.
- **Every Sf component** exposes the same BindableProperty names and types as its DX original, unless `design.md` § Control mapping lists a deviation.

## Out of Scope

- Any new feature, new screen or visual redesign. The look changes only where Syncfusion cannot match the DX original; each such difference is listed in the task log and accepted by Helder.
- A light theme.
- Windows and macOS targets.
- Upgrading .NET MAUI or other packages, apart from adding Syncfusion and removing DevExpress.
- **iOS parity checks — unless Helder decides otherwise** (`design.md` § Open questions 2).
- The frozen `MyVocaList/UI/Components/AutocompleteField/**` folder (excluded from the build); its fate is `design.md` § Open questions 3.
