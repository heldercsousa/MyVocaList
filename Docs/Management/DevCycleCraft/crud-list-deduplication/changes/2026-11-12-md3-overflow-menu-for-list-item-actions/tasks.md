# Tasks — MD3 overflow menu for list item actions

> DRY Onion: VM base → assets/styles → new component → governed CrudListView → pages. Code tasks run in one worktree on branch `task/md3-overflow-menu`, cut from `develop`. Incremental XAML rule: edit one file, build, then move to the next. Every task ends with a green build and green tests.
> Task-logs are written by the orchestrator on `develop` from the implementor's hand-back.
> Waves: sequential (T1 → T2 → T4 → T3 → T5; T3 consumes T4's `ShowItemMenu`), one implementor. Then T6 (verification, develop) and T7 (Helder on device).

- [x] **T1 — VM contract + behaviour (TDD, Level A)**
  - Scope: `design.md § Contracts`. Covers `ItemMenuAction`, the `ICrudListViewModel` additions and the `CrudListViewModelBase` logic.
  - Tests first, in `CrudListViewModelBaseTests`, one per AC. A second nested fake, `ActionsTestCrudListViewModel`, carries 2 page-specific actions for 07/08; the existing fake is unchanged.
  - Files owned: `UI/ViewModels/ItemMenuAction.cs` (new), `ICrudListViewModel.cs`, `CrudListViewModelBase.cs`, `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`
  - ACs: REQ-OVFM-01, 02a, 02b, 03, 04, 05a, 05b, 06, 07, 08, 15, 16 · Level A · Review lane: Elevated · Demo: `dotnet test` green
- [x] **T2 — Icon asset + `MenuItem` style (Level C)**
  - Scope: `more_vert_outlined.svg`, plus the `MenuItem` DXButton style in `MaterialStyles.xaml` per `design.md § Tokens`.
  - Files owned: `Resources/Images/more_vert_outlined.svg`, `Resources/Styles/MaterialStyles.xaml`
  - The style is additive: no existing style changes, so there is no consumer risk.
  - ACs: REQ-OVFM-12a (XAML property check, then visual in T6) · Demo: build green
- [x] **T3 — `ListItemOverflowButton` component (Level C, UI)**
  - Scope: `design.md § ListItemOverflowButton`. Not governed yet (0 consumers).
  - Files owned: `UI/Components/Lists/ListItemOverflowButton.xaml(.cs)`
  - ACs: REQ-OVFM-09, 10, delivered through T5 · Review lane: Standard · Demo: build green
- [x] **T4 — `CrudListView` hosts the menu (governed, Level C UI)**
  - Scope: `design.md § CrudListView`, including the Delete → confirm hand-off and back ownership.
  - The four gates are recorded in `design.md § Governance`. The task-log entry repeats the consumer map and risk table.
  - Files owned: `UI/Components/CrudListView.xaml(.cs)`
  - ACs: REQ-OVFM-11, 12b, 17 · Review lane: Elevated · Demo: build and tests green
- [x] **T5 — Venues then People rows + governed registration**
  - Scope: `design.md § Pages`. Venues first, then build, then People.
  - Add `ListItemOverflowButton` to the governed list in `.claude/library/component-safety-gate.md`.
  - Files owned: `UI/Pages/Venues/VenuesPage.xaml`, `UI/Pages/People/PeoplePage.xaml`, `.claude/library/component-safety-gate.md`
  - ACs: REQ-OVFM-09, 10 · Review lane: Standard · Demo: an overflow button on the rows on the emulator
- [x] **T6 — Verification**
  - `verifier` subagent, then full build and test run.
  - Android emulator walk-through on Venues and People:
    - REQ-OVFM-09, 10, 11, 12a/b, 13a (tap another row with the menu open), 13b (one back press, page stays), 14 (last visible row), 17
    - scroll with the menu open
  - Plus the Artists/Songs regression rows of `design.md § Governance`.
  - Merge into `develop`.
- [x] **T7 — Helder on-device pass** (2026-11-20: all points fine; corner 16 accepted)
  - Includes confirming the 16dp container corner (Claude's unverified call).
  - Then status ✅.

## Follow-up round (Helder, 2026-11-20)

- [x] **T8 — Final MD3 revision** (Playwright crawl of menus overview/specs/guidelines/accessibility); deltas go into `design.md § MD3 evidence`
- [x] **T9 — Spec: shared menu component** (usable anywhere, not CrudListView-only); spec-reviewer PASS WITH MINOR ISSUES; blockers B1/B2 and warnings W1–W9 addressed
- [x] **T10 — Plan** (`plan.md § Revision 2026-11-20b`) + plan-reviewer (3 blockers fixed: T11a split, task fields, registry ownership)
- [x] **T11a1 — `MenuItemModel` + `ItemMenuItems`** (TDD, Level A) · REQ-OVFM-18
- [x] **T11a2 — menu helpers** (`MenuItemSelection`, `MenuResolution`, `MenuItemShape`; TDD, Level B) · REQ-OVFM-19, 20, 21
- [x] **T11a3 — `MenuItem` style + `Menu` + `IMenuHost`** · REQ-OVFM-12a, 21, 23, 24, 25
- [x] **T11a4 — `OverflowMenuButton`** · REQ-OVFM-20, 22, 25
- [x] **T11c — `ListItemOverflowButton` wraps `OverflowMenuButton`** (governed; own commit and log entry; runs before T11b)
- [x] **T11b — `CrudListView` consumes `Menu`** (governed; own commit and log entry)
- [x] **T12 — Verify** (verifier CONDITIONAL PASS; emulator PASS; merged `9fd344ba`)
- [x] **T13 — Helder on-device pass** (revision 2026-11-20b; optional TalkBack for REQ-OVFM-23), then status ✅
