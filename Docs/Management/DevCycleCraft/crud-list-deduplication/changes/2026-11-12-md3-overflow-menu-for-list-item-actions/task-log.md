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
- Songs regression: walked 2026-11-20 on `develop` @ `19555574` (see below).

## 2026-11-20 — T6 addendum: Songs regression on `develop`

| AC | Result | Evidence |
|----|--------|----------|
| Regression: Songs | PASS | The list renders, the catalog button is unchanged, no overflow button (as designed). Tapping a row opens Edit Song; back returns to Songs. No crash in logcat. |
| Regression: Venues (post-merge) | PASS | The overflow button shows on every row of the `develop` build. |

- Note: the first `-t:Run` launch after the merge crashed in `ShellFlyoutTemplatedContentRenderer.LoadView` (an NRE). It reproduced on every relaunch. The cause was a stale incremental Android build: after deleting `obj/` and `bin/` for `net10.0-android` and redeploying the same commit, the app launches cleanly. No code change. If the crash reappears, clean the Android `obj/` and `bin/` before suspecting code.

## 2026-11-20 — T7 Helder on-device pass + follow-up round (T8–T12)

- T7 (Helder, device): every T7 point is fine. The 16dp corner is accepted. Back and tap outside close the menu only, as REQ-OVFM-13a/13b specify.
- Follow-up requested by Helder (autonomous; ask only on crucial points):
  1. Final MD3 revision of the menu against menus overview, specs, guidelines and accessibility. Read through Playwright with the Chrome extension; the token was rotated in `.env.local` and the User env, never committed.
  2. The menu must be a shared component usable anywhere in the UI, not hard-coded in `CrudListView`. Adjust the spec, then code.
  3. Update the internal guidelines for governed components.

### Checkpoint
- Step: T13 done 2026-11-21 (Helder device pass: all 15 steps OK). Status ✅. Item closed.
- Next: none. Follow-ups in the T13 entry.
- Context manifest: this `task-log.md`, `tasks.md`, `design.md` (§ Revision 2026-11-20b), `requirements.md` (§ Revision 2026-11-20b), `plan.md` (§ Revision 2026-11-20b), `.claude/library/component-safety-gate.md`
- Playwright note: the session's MCP had the old token, so the crawl drives `@playwright/mcp@0.0.78 --extension` from a script (scratchpad), with the token read from `.env.local`.

## 2026-11-20 — T11a1 MenuItemModel + ItemMenuItems — To Review

Commit `0935dee3` on `task/md3-menu-shared-component`.

### Changed files
- `MyVocaList/UI/ViewModels/MenuItemModel.cs` (new)
- `MyVocaList/UI/ViewModels/ICrudListViewModel.cs` (+`ItemMenuItems`)
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (`ItemMenuItems` built once: Edit → page actions → Delete)
- `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (+4 tests)

### Verification
- Red: the 4 tests did not compile (CS1061, no `ItemMenuItems`). Green: 41/41 in `CrudListViewModelBaseTests`; full suite 1869 passed, 0 failed. Android build: 0 errors.

### AC traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-OVFM-18 | Edit, then Delete (no page actions) | `CrudListViewModelBase.BuildItemMenuItems` | `ItemMenuItems_Default_IsEditThenDelete` |
| REQ-OVFM-18 | Page actions between Edit and Delete, in order | same | `ItemMenuItems_WithPageActions_EditActionsDelete` |
| REQ-OVFM-18 | A page item runs `ExecuteItemMenuActionCommand` with the action as its parameter | same | `ItemMenuItems_PageAction_CommandIsExecuteWithActionParameter` |
| REQ-OVFM-18 | Built once | `ItemMenuItems` (`??=`) | `ItemMenuItems_IsBuiltOnce` |

## 2026-11-20 — T11a2 menu helpers — To Review

Commit `9a26ab81`.

### Changed files
- `MyVocaList/UI/Components/Menus/MenuItemSelection.cs`, `MenuResolution.cs`, `MenuItemShape.cs` (new)
- `Tests/Unit/Components/MenuItemSelectionTests.cs`, `MenuResolutionTests.cs`, `MenuItemShapeTests.cs` (new)

### Verification
- Red: each helper's tests failed to compile first (CS0234/CS0103). Green: full suite 1882 passed, 0 failed. Android build: 0 errors.

### AC traceability
| AC | Implementation | Tests |
|----|----------------|-------|
| REQ-OVFM-19 | `MenuItemSelection.Select` | `Select_RunsCommandBeforeClose`, `Select_DoesNotCloseWhenCommandAlreadyClosed`, `Select_DoesNotCloseWhenCloseOnItemSelectedFalse`, `Select_CommandCannotExecute_DoesNotRun_StillCloses`, `Select_NullCommand_DoesNotThrow` |
| REQ-OVFM-20 | `MenuResolution.Resolve<TMenu>` | `Resolve_ExplicitWins`, `Resolve_NearestHost`, `Resolve_SkipsHostWithNullMenu`, `Resolve_NoneReturnsNull` |
| REQ-OVFM-21 | `MenuItemShape.For` | `For_SingleItem_All12`, `For_First_Top12Bottom4`, `For_Last_Top4Bottom12`, `For_Middle_All4` |

## 2026-11-20 — T11a3 MenuItem style + Menu + IMenuHost — To Review

Commit `46982a87`.

### Changed files
- `MyVocaList/Resources/Styles/MaterialStyles.xaml`: `MenuItem` `Padding` 16,0 → 12,0, plus `CornerRadius` 4
- `MyVocaList/UI/Components/Menus/Menu.xaml(.cs)` (new): one DXPopup; items built in code-behind; `MenuItemShape` corners; `CanExecute` → `IsEnabled`; tap → `MenuItemSelection.Select`; `PopupBackHandler` attach and detach; initial focus
- `MyVocaList/UI/Components/Menus/IMenuHost.cs` (new)

### Verification
- Android build: 0 errors. Full suite: 1882 passed. Not consumed yet, so there is no emulator gate (covered in T11b and T12).
- Grep of `UI/Components/Menus/` for `CrudListView|ICrudListViewModel|CrudListPageBase`: 0 matches (REQ-OVFM-25).

### AC traceability
| AC | Implementation | Verification |
|----|----------------|--------------|
| REQ-OVFM-12a (amended) | `MenuItem` style | reviewer check |
| REQ-OVFM-21 | `Menu.RebuildItems` (corners), content `Padding="4,2"` | Level B tests (T11a2), plus reviewer check and visual in T12 |
| REQ-OVFM-23 | `OnPopupOpened` → `SetSemanticFocus(first item)` | reviewer check; TalkBack on device |
| REQ-OVFM-24 | `IsEnabled` from `CanExecute`, refreshed on `CanExecuteChanged` | reviewer check |
| REQ-OVFM-25 | no host references | grep |

### Decisions (orchestrator, under Helder's autonomy grant)
- `SemanticExtensions.SetSemanticFocus` is obsolete in MAUI 10 (CS0618). Its suggested replacement, `SemanticScreenReader`, can only announce text and cannot move focus. **Kept**, so REQ-OVFM-23 still works. Follow-up: replace it with the native Android accessibility-focus action when the obsolete API is removed.
- A `MenuItemModel` with a null `Command` shows enabled, and selecting it only closes the menu. Accepted.

## 2026-11-20 — T11a4 OverflowMenuButton — To Review

Commit `9d07e9c5`.

### Changed files
- `MyVocaList/UI/Components/Menus/OverflowMenuButton.xaml(.cs)` (new)

### Verification
- Android build: 0 errors. Full suite: 1882 passed. Not consumed yet, so the emulator check is in T12.
- Anchor: `PlacementTarget` is the `OverflowMenuButton` itself, whose bounds equal the 48×48 inner DXButton. Pressed layer: `OnSurfaceVariant` from `MaterialColors.xaml`, with `WithAlpha(0.10f)`, applied when the menu is open after the tap and restored on `Closed`.
- Accepted assumption: the pressed state needs the menu to be open synchronously after the tap. This holds for `OpenItemMenuCommand`, a synchronous RelayCommand with a TwoWay `IsOpen`.

### AC traceability
| AC | Implementation | Verification |
|----|----------------|--------------|
| REQ-OVFM-20 | `OnClicked` → `MenuResolution.Resolve` (explicit, then ancestor `IMenuHost`, else a warning) | Level B tests (T11a2) and reviewer check |
| REQ-OVFM-22 | `BeginPressedState` / `OnPressedMenuClosed` | emulator (T12) |
| REQ-OVFM-25 | no host references | grep |

## 2026-11-20 — T11c ListItemOverflowButton wraps OverflowMenuButton (governed) — To Review

Commit `5c53b3c2`. Gates: `design.md § Governance (revision 2026-11-20b)`.

### Changed files
- `MyVocaList/UI/Components/Lists/ListItemOverflowButton.xaml`: the content is now `menus:OverflowMenuButton`. The selection-mode `IsVisible` binding is unchanged; `Command` is bound to `CrudListPageBase.BindingContext.OpenItemMenuCommand`, with `CommandParameter={Binding .}`.
- `ListItemOverflowButton.xaml.cs`: the constructor only. The `CrudListView` parent walk and the Clicked handler are removed.

### Consumer map (grep `ListItemOverflowButton`)
| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `UI/Pages/Venues/VenuesPage.xaml:48` | the ancestor `IMenuHost` lookup, the anchor, the pressed state | emulator on the final head (T11b/T12) |
| `UI/Pages/People/PeoplePage.xaml:47` | same | same |

### Verification
- Android build: 0 errors. Full suite: 1882 passed. `ShowItemMenu` now has no caller (removed in T11b). This commit is not deployable alone, so there was no emulator check.

## 2026-11-20 — T11b CrudListView hosts the shared Menu (governed) — To Review

Commit `16122733`. Gates: `design.md § Governance (revision 2026-11-20b)`.

### Changed files
- `MyVocaList/UI/Components/CrudListView.xaml`: the inline DXPopup is replaced by `menus:Menu` (`IsOpen` TwoWay, `ItemsSource=ItemMenuItems`, `Closed=OnItemMenuClosed`).
- `CrudListView.xaml.cs`: implements `IMenuHost`. Removed `_menuBackRoute`, `ShowItemMenu`, `OnItemMenuOpened` and `OnMenuBackPressed`. The Delete → confirm hand-off is kept.

### Consumer map (grep `<views:CrudListView`)
| Consumer | What could break | Result |
|----------|------------------|--------|
| `Pages/Venues/VenuesPage.xaml:26` | the back route, the confirm hand-off, the target cleared too early, anchoring | PASS (emulator) |
| `Pages/People/PeoplePage.xaml:25` | same | PASS (emulator) |
| `Pages/Artists/ArtistsPage.xaml:27` | XAML regression | PASS (orchestrator, T12) |
| `Pages/Songs/SongsPage.xaml:26` | XAML regression | PASS (orchestrator, T12) |

### Verification
- Android build: 0 errors. Full suite: 1882 passed. One run had a flaky failure in `SongRepositoryListSearchTests` (an SQLite handle disposed during parallel runs, unrelated to this change); the rerun was green.

## 2026-11-20 — T12 Verification (revision 2026-11-20b)

- **`verifier`: CONDITIONAL PASS, no blockers.**
  - Signatures and reviewer-check ACs (12a, 12b, 21, 23, 24, 25) pass. Every test carries one `[AC]`. Governance commits are separate. No leaks: every subscription is detached.
  - Warnings, not fixed:
    - the `[AC]` comment sits below `[Fact]` in the VM tests;
    - the doc-comment typo in `ListItemOverflowButton.xaml.cs` ("`ListItemOverflowButton` rows" should read "`ListItem` rows");
    - the pressed state relies on a synchronous open.
- **Emulator** (Pixel 5, API 36; clean `obj/` and `bin/`; branch head `16122733`):

| AC | Result | Evidence |
|----|--------|----------|
| REQ-OVFM-09/11 | PASS | The overflow opens an anchored menu with Edit and Delete (Venues, People). |
| REQ-OVFM-21 | PASS | Holding Edit shows the pressed item inset inside the container, with 12dp top corners and 4dp bottom corners. The container corner is 16. |
| REQ-OVFM-22 | PASS | A tint circle shows on the overflow button while the menu is open and clears on close. |
| REQ-OVFM-13a / 13b | PASS | An outside tap closes the menu only. Back closes the menu only, and the page stays. |
| REQ-OVFM-03 / 17 | PASS | Edit opens the tapped row. Delete shows the confirm sheet once (cancelled; no data deleted). |
| REQ-OVFM-14 | PASS | The last row's menu flips above the button. |
| Regression: Artists, Songs | PASS | The lists render, the trailing buttons are unchanged, and logcat shows no crash. |
| REQ-OVFM-23 | open | Code verified (`SetSemanticFocus`). TalkBack check left to Helder's device. |

- Observation for Helder: the pressed colour of a menu item comes from the existing `MenuItem` style (a strong tonal fill). The MD3 pressed state layer is On surface at about 10%, so that is a possible refinement.
- Merged into `develop` as `9fd344ba`.

## 2026-11-21 — T13 Helder on-device pass — Done
- Every step of the 15-step checklist passed, including TalkBack (REQ-OVFM-23).
- Open question: a tap on the ⋮ of an open menu leaves it open. The spec says nothing explicit; REQ-OVFM-13a (outside tap closes) implies it should close. Likely a close-then-reopen. Not ordered; follow-up only.
- Follow-ups: trigger re-tap toggle · softer item pressed fill · native accessibility focus instead of `SetSemanticFocus` · doc-comment typo.
