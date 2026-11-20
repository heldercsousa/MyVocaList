# Task Log — MD3 overflow menu for list item actions

## 2026-11-20 — Spec

- DevExpress-first check (Context7 `/websites/devexpress_maui`, 25.2.4): DevExpress has no menu control. `DXPopup` + `PlacementTarget` with DXButton items is DevExpress's documented context-menu scenario. Evidence: `design.md § DevExpress-first check`.
- MD3 crawl: m3.material.io Menus, Lists and Icon buttons (overview, guidelines, specs, accessibility), using Playwright from the npx cache with Edge. The token table was expanded with `expand_all`. The vertical menu tokens settled both open discrepancies: item height 44dp and icon-to-label gap 12dp. The container shape value is not exposed, so 16dp is Claude's call, to be confirmed by Helder in T7.
- Component decision: no `MenuItem` component or `SlotItemBase`. The only new view is `ListItemOverflowButton`. This supersedes README Decision 2026-11-12.
- Review round 1: fresh `spec-reviewer` (3 Major: target clearing, Delete→confirm hand-off, no-scrim tap-through and back ownership) and `plan-reviewer` (bindings inside the BindableLayout, the second test fake, review lanes, task-logs on develop). All were folded in, except the request for full code bodies in the plan; the plan keeps signatures and tests (writing-plans proportion rule).
- Docs commit `057a2e24` on develop.

## 2026-11-20 — T1 VM contract + behaviour — To Review

Branch `task/md3-overflow-menu` (worktree `.claude/worktrees/md3-overflow-menu`), commit `6cd28f40`.

### Changed files
- `MyVocaList/UI/ViewModels/ItemMenuAction.cs`: new record.
- `MyVocaList/UI/ViewModels/ICrudListViewModel.cs`: additive members `IsItemMenuOpen`, `ItemMenuActions`, `OpenItemMenuCommand`, `DismissItemMenuCommand`, `ExecuteItemMenuActionCommand`.
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`: the menu state and the first branches in `HandleBack`, `OnItemTappedAsync` and `OnItemLongPressed`. `CloseItemActions` now closes the menu.
- `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`: 16 tests and a new `ActionsTestCrudListViewModel` fake.

### Verification
- `CrudListViewModelBaseTests` 37/37. Full suite: 1865 passed, 0 failed.
- Red was observed at compile level (CS0246/CS0115/CS1061) before Green, not per assertion.
- An unrelated flake in an earlier run (`SongFormViewModelFrozenArtistTests…` "Collection was modified") passed on re-run. Worth a separate look.

### AC traceability

| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-OVFM-01 | Browse open sets target, menu open | `OpenItemMenu` | `OpenItemMenu_InBrowseMode_SetsTargetAndOpensMenu`, `OpenItemMenu_Twice_KeepsSameTargetAndMenuOpen` |
| REQ-OVFM-02a | No-op in selection mode | `OpenItemMenu` guard | `OpenItemMenu_InSelectionMode_IsNoOp` |
| REQ-OVFM-02b | No-op for a non-item parameter | `OpenItemMenu` guard | `OpenItemMenu_NonTItemParameter_IsNoOp` |
| REQ-OVFM-03 | Edit closes and navigates | `EditItemAsync` + `CloseItemActions` | `EditItem_FromMenu_ClosesMenuAndNavigatesToTarget` |
| REQ-OVFM-04 | Delete closes and confirms the target | `DeleteItem` + `CloseItemActions` | `DeleteItem_FromMenu_ClosesMenuAndOpensConfirmForTarget` |
| REQ-OVFM-05a | Dismiss has no side effects | `DismissItemMenuCommand` | `DismissItemMenu_ClosesMenuWithoutSideEffects` |
| REQ-OVFM-05b | Setting false clears the target, idempotently | `OnIsItemMenuOpenChanged` | `SetIsItemMenuOpenFalse_ClearsTarget`, `SetIsItemMenuOpenFalse_WhenAlreadyClosed_IsNoOp` |
| REQ-OVFM-06 | Back closes the menu only | `HandleBack` | `HandleBack_WithMenuOpen_ClosesMenuAndReturnsTrue` |
| REQ-OVFM-07 | Empty by default; order kept | `ItemMenuActions`, `BuildItemMenuActions` | `ItemMenuActions_ByDefault_IsEmpty`, `ItemMenuActions_DerivedVm_KeepsDeclaredOrder` |
| REQ-OVFM-08 | Page action runs after close, once, with the target | `ExecuteItemMenuActionAsync` | `ExecuteItemMenuAction_ClosesMenuThenRunsActionOnceWithTarget`, `ExecuteItemMenuAction_WithNullTarget_DoesNotRunAction` |
| REQ-OVFM-15 | Tap with the menu open only closes it | `OnItemTappedAsync` | `TapItem_WhileMenuOpen_ClosesMenuAndDoesNotNavigate` |
| REQ-OVFM-16 | Long-press with the menu open only closes it | `OnItemLongPressed` | `LongPressItem_WhileMenuOpen_ClosesMenuAndDoesNotSelect` |

Note: the commands are typed `ICommand` (per the design), so the tests call `Execute`. The async exception path is left to `GlobalExceptionHandler`.

## 2026-11-20 — T2 icon + MenuItem style — To Review

Commit `e025a7bf`.

### Changed files
- `MyVocaList/Resources/Images/more_vert_outlined.svg`: new, Material Symbols Outlined.
- `MyVocaList/Resources/Styles/MaterialStyles.xaml`: new `MenuItem` DXButton style (44 / 16,0 / IconIndent 12 / icon 20 / RobotoMedium 14 / OnSurface, OnSurfaceVariant).

### Verification
- Android build: 0 errors. Level C: no test (REQ-OVFM-12a is a XAML property check plus T6 visual).
- The design's `FontAttributes=Bold` line was corrected: Label Large weight 500 is the `RobotoMedium` face, as the `Label.Large` class uses.

## 2026-11-20 — T4 Governed: CrudListView hosts the menu — To Review

Commit `5ce001d9`. Governed gates: dedicated task + MD3 review (`design.md`), consumer map and risk table (below), and Helder approval 2026-11-20 (`design.md § Governance`).

### Changed files
- `MyVocaList/UI/Components/CrudListView.xaml`: `DXPopup x:Name="itemMenu"` (Edit, BindableLayout of `ItemMenuActions` through `x:Reference self`, Delete), `Closed="OnItemMenuClosed"`.
- `MyVocaList/UI/Components/CrudListView.xaml.cs`: `ShowItemMenu(View, object)`, the `_showConfirmWhenMenuClosed` deferral and `OnItemMenuClosed`.

### Consumer map (grep `<views:CrudListView`, 2026-11-20)

| Consumer | What could break | Verification (T6 emulator) |
|----------|------------------|----------------------------|
| `UI/Pages/Venues/VenuesPage.xaml:26` | Layout or z-order of the FAB, toolbar or sheets; back handling | Open and close the menu, Edit, Delete, back in each state; FAB and toolbar unchanged |
| `UI/Pages/People/PeoplePage.xaml:25` | Same as Venues | Same as Venues |
| `UI/Pages/Artists/ArtistsPage.xaml:27` | Popup never opens; risk limited to the added XAML and HandleBack | List renders, catalog button works, back unchanged |
| `UI/Pages/Songs/SongsPage.xaml:26` | Same as Artists | List renders, YouTube button works, back unchanged |

### Verification
- Android build: 0 errors. Full suite: 1865 passed. The first run had an unrelated SQLite disposed-object flake in `SongRepositorySearchTests`, which passed on re-run.
- Open for T6:
  - Does the confirm deferral engage? It runs only if `itemMenu.IsOpen` is still true when `ConfirmSheetState` changes (REQ-OVFM-17).
  - Do string icons for page actions render?
  - Does DXPopup render `BackgroundColor`?

## 2026-11-20 — T3 ListItemOverflowButton + T5 Venues/People rows — To Review

Commits `b4de1cdb` (T3) and `d4e88ca1` (T5).

### Changed files
- `MyVocaList/UI/Components/Lists/ListItemOverflowButton.xaml(.cs)`: new. 48x48 `StandardIconButton` with `more_vert_outlined` and "More options". `IsVisible` is bound to the inverse of `IsSelectionMode`. `Clicked` walks `Parent` to `CrudListView.ShowItemMenu(this, BindingContext)`; with no host it logs a warning and does nothing.
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`, `MyVocaList/UI/Pages/People/PeoplePage.xaml`: `ItemTemplate` trailing slot is `HorizontalStackLayout Spacing=8` with the overflow button, then the checkbox. `SelectedItemTemplate` is untouched.
- `.claude/library/component-safety-gate.md`: `ListItemOverflowButton` added to the governed list (consumers: Venues, People).

### Consumer map (grep `ListItemOverflowButton`)

| Consumer | Risk | Verification |
|----------|------|--------------|
| `VenuesPage.xaml:48` | The row tap could fire through the button; the button could stay visible in selection mode | Emulator T6 (below) |
| `PeoplePage.xaml:47` | Same | Emulator T6 |

### Verification
- Android build: 0 errors. Full suite: 1865 passed.

## 2026-11-20 — T6 Verification

- `verifier`: CONDITIONAL PASS, no blockers. Contracts, tokens, deferral, governance and AC markers all match.
  - Warnings: this entry was missing, and the UI ACs were still to be verified.
- Emulator (Pixel 5, API 36), debug build from the task branch:

| AC | Result | Evidence |
|----|--------|----------|
| — | **FAIL, then fixed** | App crashed on launch. `DXPopup.HorizontalAlignment="End"` is not a `PopupHorizontalAlignment` value; the runtime XAML load threw `ArgumentException`. Fixed in `20b330cc` (`Right`). |
| REQ-OVFM-09 | PASS | An overflow button on every Venues row. Tapping it opens the menu anchored below the button, with no navigation. |
| REQ-OVFM-11 | PASS | Edit, then Delete (no page actions on Venues). |
| REQ-OVFM-12a/b | PASS (visual) | 44dp items, Label Large (RobotoMedium 14), SurfaceContainerLow container, rounded 16 corner, shadow. |
| REQ-OVFM-03 | PASS | Edit from the menu opens Edit Venue for the tapped row. |
| REQ-OVFM-13a | PASS | Tapping another row with the menu open closes the menu only, with no navigation. |
| REQ-OVFM-13b | **FAIL, then fixed** (`880da326`) | Re-verified by implementor and orchestrator: one back press closes the menu, the page stays, no exit sheet; a second back shows the exit sheet as before. | The back press with the menu open did nothing (menu stayed, page stayed). Root cause in `design.md § CrudListView` "Back ownership, revised": DXPopup's own focused window swallows back, and the page handler is never called (diagnostic log). |
| REQ-OVFM-14 | PASS | The last visible row's menu opens above the button. |
| REQ-OVFM-17 | PASS | Delete from the menu: the menu closes, and the "Delete 1 venue(s)?" sheet shows once. |
| REQ-OVFM-10 | PASS | In selection mode the overflow button is hidden on every row, selected and unselected. |
| REQ-OVFM-09/11 (People) | PASS | Singers rows show the overflow button; the menu (Edit, Delete) opens anchored. |
| Regression: Artists | PASS | The list renders, the catalog button is unchanged, no overflow button (as designed). |

### Fix commits after T6
- `20b330cc` fix: `DXPopup.HorizontalAlignment` `End` → `Right`. A crash on every list page; the runtime XAML load is not caught at build time.
- `880da326` fix: back with the menu open. New `MyVocaList/Platforms/Android/PopupBackHandler.cs`; `CrudListView.xaml(.cs)` registers it on `Opened` and disposes it on `Closed` (API 33+ `OnBackInvokedDispatcher`, API 24–32 key listener). Build 0 errors; tests 1865 passed. The API 24–32 path is not exercised on the emulator (API 36).

### Notes for Helder (T7)
- The 16dp container corner is Claude's unverified call (the MD3 token value is not exposed).
- `AndroidManifest.xml` has no `android:enableOnBackInvokedCallback` opt-in. Back works on API 36 regardless; flagged only.
- Songs regression was not walked on the emulator (same shared code path as Artists).
