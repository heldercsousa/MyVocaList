# Tasks — CRUD list selection gestures

> DRY Onion: VM base → shared components → pages. Code tasks run in a worktree on branch `feature/crud-list-selection-gestures`, cut from `develop`. Incremental XAML rule: edit one file, build, then move to the next. Every task ends with a green build and green tests.
> Waves: W1 = T1 · W2 = T2a ∥ T2b · W3 = T2c · W4 = T3 · then T4 (develop) · T5.

- [x] **T1 — VM base + interface, additive only (TDD, Level A)**
  - Scope: add every *added* member of `design.md § Contracts` to `ICrudListViewModel` and `CrudListViewModelBase`. Removals belong to T2c.
  - `RequestBatchDelete` becomes `RequestDelete(IList<TItem>)`. `DeleteSelectedCommand` keeps its behavior.
  - `OnSelectedCountChanged` sets `IsSelectionMode`.
  - Tests first, in `CrudListViewModelBaseTests`: one test per U-AC. The 04c test asserts the selection is cleared after the confirmed delete action completes.
  - Produces: selection-mode VM contract · Consumes: nothing · Risk: Medium · Review lane: Elevated
  - Files owned: `CrudListViewModelBase.cs`, `ICrudListViewModel.cs`, `CrudListViewModelBaseTests.cs`
  - ACs: REQ-CLSG-01, 01b, 02, 04a–d, 05a–b, 06, 07, 08, 12 · Demo: `dotnet test` green with the new tests
- [x] **T2a — New `ListItemCheckbox` component** [P with T2b]
  - Scope: per `design.md § ListItemCheckbox`. Not governed yet (0 consumers).
  - Produces: `ListItemCheckbox` · Consumes: T1 (`ToggleItemSelectionCommand`) · Risk: Low · Review lane: Standard
  - Files owned: `UI/Components/Lists/ListItemCheckbox.xaml`, `ListItemCheckbox.xaml.cs`
  - ACs: REQ-CLSG-05a/b (UI half) · Demo: Android build green
- [x] **T2b — `FloatingToolbar` toggle colors + `BottomSheetMenuItem` style** [P with T2a]
  - Scope: per `design.md § FloatingToolbar` and `§ Style`. Includes the comment updates.
  - Produces: toggle-tonal selected state; menu item style · Consumes: nothing · Risk: Low (single consumer) · Review lane: Standard
  - Files owned: `UI/Components/Toolbars/FloatingToolbar.xaml.cs`, `Resources/Styles/MaterialStyles.xaml`
  - ACs: REQ-CLSG-11 · Demo: Android build green
- [x] **T2c — Governed: `CrudListView` + removal of Edit-selected**
  - Scope: all of `design.md § CrudListView`, in this order:
    1. gestures
    2. dead `ItemTapCommand` removal
    3. dynamic `SelectionMode` + deferred `OnSelectionChanged`
    4. toolbar: delete the Action2 Edit block, move Delete Action3→Action2, add `IsVisible`
    5. item actions sheet + hand-off
  - Removal (same commit, keeps the build green):
    - `EditSelectedCommand`, `CanEditSelected`, `ExecuteEditSelectedAsync` and their lines in `OnSelectedCountChanged`, from the VM and the interface;
    - `PersonsViewModelTests.cs` lines 38–53, only the `CanEditSelected_*` tests. The header comment is trimmed to "CanDeleteSelected" and the `CanDeleteSelected_*` tests stay.
  - Produces: gesture-driven list · Consumes: T1, T2b (style) · Risk: High · Review lane: Architectural
  - Files owned: `CrudListView.xaml`, `CrudListView.xaml.cs`, `CrudListViewModelBase.cs`, `ICrudListViewModel.cs`, `PersonsViewModelTests.cs` (5)
  - ACs: REQ-CLSG-03, 06–10 (UI half) · Demo: all 4 pages tap → form, long-press → select, with no page edits yet
  - **Governed-component gates:**
    1. Dedicated task: this one. MD3 review: `design.md § MD3 evidence`.
    2. Consumer map (grep `<views:CrudListView` 2026-11-11): `VenuesPage.xaml:36`, `PeoplePage.xaml:31`, `ArtistsPage.xaml:33`, `SongsPage.xaml:36`.
    3. Per-consumer risk:

       | Consumer | What could break | Verification |
       |---|---|---|
       | VenuesPage | back press: exit sheet vs selection clear; tap now navigates | device checklist 1–8 on Venues; back from selection clears, a second back shows the exit sheet |
       | PeoplePage | row tap now navigates to the Person form | checklist 1–8 on People |
       | ArtistsPage | leading checkbox + trailing catalog button: catalog tap must not select/navigate | checklist 1–9 on Artists |
       | SongsPage | YouTube button; `PrimaryFabCommand` FAB switching; subtitle "N selected" | checklist 1–9 on Songs; FAB unchanged |
    4. Helder approval: granted in advance on 2026-11-11 ("you're already authorized in advance to do anything needed… I trust you"), plus plan approval (ExitPlanMode) on 2026-11-11.
- [x] **T3 — Pages + back button**
  - Scope: replace the 8 `dx:CheckEdit` with `lists:ListItemCheckbox`, in the order Venues → People → Artists → Songs, building after each page. `CrudListPageBase.OnBackButtonPressed` → `HandleBack()`. Delete the unwired leftovers `CrudListPageBase.OnSelectionChanged`, `OnCollectionViewScrolled` and `OnConfirmSheetStateChanged`; grep 2026-11-11 found no XAML wiring, since the handlers of the same name in `CrudListView.xaml` bind to `CrudListView`'s own code-behind.
  - Produces: final UI · Consumes: T2a, T2c · Risk: Medium · Review lane: Elevated
  - Files owned: `VenuesPage.xaml`, `PeoplePage.xaml`, `ArtistsPage.xaml`, `SongsPage.xaml`, `CrudListPageBase.cs`
  - ACs: REQ-CLSG-05, 12, 13, 14 · Demo: checkbox tap selects from browse mode
- [x] **T4 — Guideline amendments** (`amend:` commit `8df8ef14` on the feature branch — see task-log)
  - `.claude/library/crud-appbar-list-toolbar.md`:
    - List Layout law: dynamic `SelectionMode` owned by `CrudListView`, and drop the "Never: SelectionMode.None / mode toggle" bullets;
    - FloatingToolbar law: visible only in selection mode, 2 slots, `IsSelected` only on toggles.
  - `.claude/library/m3-floating-toolbar.md`: selected = toggle tonal; FAB emphasis rule; MD3 quotes.
  - `.claude/library/component-safety-gate.md`: add `ListItemCheckbox` to the governed list.
  - Produces: updated laws · Consumes: T3 · Files owned: the 3 library files · Review lane: Elevated (Helder reviews at hand-off; authorship rule)
- [ ] **T5 — Verify + hand-off**
  - Android build with 0 errors; full `dotnet test`; `verifier` subagent; AC traceability matrix in `task-log.md`; push the branch.
  - Helder runs the on-device checklist (`plan.md § Verification`). Merge into develop after the reported pass, then set status ✅.
  - Produces: merge-ready branch · Consumes: T1–T4

**Sequential-only files touched:** none from the registry.
**Helder manual gate:** the on-device checklist across the 4 pages.
