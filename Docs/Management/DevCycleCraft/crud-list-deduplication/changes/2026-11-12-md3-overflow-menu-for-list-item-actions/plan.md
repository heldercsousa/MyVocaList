# MD3 overflow menu for list item actions — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Project overrides: `myvocalist-coding` skill before coding, `maui-current-apis` for MAUI code, Context7 at DevExpress **25.2.4** for any DX API.

**Goal:** a trailing overflow icon button on the Venues and People rows opens an MD3 vertical menu (Edit, page-specific items, Delete), hosted once in `CrudListView` as a `DXPopup`.

**Architecture:**
- All state and behaviour live in `CrudListViewModelBase` and reuse the existing `ItemActionsTarget` / `CloseItemActions` / `EditItemAsync` / `DeleteItem` path.
- `CrudListView` hosts one anchored `DXPopup`.
- The new `ListItemOverflowButton` hands its own view to the popup as the anchor.

**Tech Stack:** .NET MAUI (net10.0-android), C#, CommunityToolkit.Mvvm, DevExpress MAUI 25.2.4 (`DXPopup`, `DXButton`), xUnit + Moq.

**Execution order (first version, done):** T1 → T2 → T4 → T3 → T5 → T6. *Revision 2026-11-20b order is at the end of this file.* (T3 consumes T4's `ShowItemMenu`). **Task-logs:** the implementor reports Changed files, the consumer map, the risk table and the traceability matrix in its hand-back. The orchestrator writes `task-log.md` on `develop` (docs land on develop), never on the task branch.

**Spec:** `requirements.md` and `design.md` in this folder. Read both; `design.md § Contracts` is binding.

## Global Constraints

- Branch `task/md3-overflow-menu`, in a worktree cut from `develop`. Never edit `.cs` or `.xaml` on `develop`.
- XAML: edit one file → build → fix → next. Never batch.
- No logic in pages or code-behind beyond the anchor hand-off, the popup show/close sync and the confirm deferral.
- English only. MD3 names (`MenuItem` style key; "More options" accessible name).
- No `DisplayAlert` or other native dialogs. Use DevExpress first.
- Test traceability: the first comment line of each test is `// [AC] REQ-OVFM-xx: <criterion>`, one AC per test.
- Builder never edits a test to make it pass. A test that looks wrong is logged as `blocked: spec gap` and escalated.
- Commits: `feat:` with an `Item: md3-overflow-menu-for-list-item-actions` trailer.

## Review Focus

- A tap on another row while the menu is open must neither navigate nor select (REQ-OVFM-15/16, tested in T1).
- One back press with the menu open must close only the menu, without popping the page (REQ-OVFM-13b, T6 emulator).
- Delete from the menu must show the confirm sheet exactly once (REQ-OVFM-17, T4 code and T6 emulator).
- Entering selection mode while the menu is open: blocked by the long-press guard (T1 test `LongPressItem_WhileMenuOpen_ClosesMenuAndDoesNotSelect`).
- The TwoWay close write-back must be idempotent, so the target is not cleared twice and nothing re-enters (T1 test `SetIsItemMenuOpenFalse_WhenAlreadyClosed_IsNoOp`).

---

### Task 1: ViewModel contract and behaviour (TDD, Level A)

**Files:**
- Create: `MyVocaList/UI/ViewModels/ItemMenuAction.cs`
- Modify: `MyVocaList/UI/ViewModels/ICrudListViewModel.cs`, `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (`HandleBack` ~362, `OnItemTappedAsync` ~389, `OnItemLongPressed` ~397, `CloseItemActions` ~442), (`CrudListViewModelBase` is the only implementer of the interface)
- Test: `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`. Leave the existing nested `TestCrudListViewModel` untouched; it serves the default-empty test. Add a second nested fake, `ActionsTestCrudListViewModel`. It overrides `BuildItemMenuActions` with 2 actions ("First", "Second"), and each records its calls: the target and `IsItemMenuOpen` at call time.
- Review lane: Elevated · Consumes: nothing · Demo: `dotnet test` green

**Interfaces:**
- Produces:
  - `public sealed record ItemMenuAction(string Text, string Icon, Func<object, Task> Execute);`
  - `ICrudListViewModel` additions:
    - `bool IsItemMenuOpen { get; set; }`
    - `IReadOnlyList<ItemMenuAction> ItemMenuActions { get; }`
    - `ICommand OpenItemMenuCommand { get; }`
    - `ICommand DismissItemMenuCommand { get; }`
    - `ICommand ExecuteItemMenuActionCommand { get; }`
  - Base members:
    - `protected virtual IReadOnlyList<ItemMenuAction> BuildItemMenuActions()`
    - `protected static ItemMenuAction MenuAction(string text, string icon, Func<TItem, Task> execute)`
- Existing commands reused unchanged: `EditItemCommand`, `DeleteItemCommand`, `ItemTapCommand`, `ItemLongPressCommand`.

- [ ] **Step 1: Write the failing tests.** Names, ACs and assertions:

  | Test | AC | Assert |
  |---|---|---|
  | `OpenItemMenu_InBrowseMode_SetsTargetAndOpensMenu` | 01 | `IsItemMenuOpen` true; target == item; `SelectedCount` 0; `IsSelectionMode` false |
  | `OpenItemMenu_InSelectionMode_IsNoOp` | 02a | after a long-press (selection), open → `IsItemMenuOpen` false |
  | `OpenItemMenu_NonTItemParameter_IsNoOp` | 02b | open with `42` → `IsItemMenuOpen` false |
  | `EditItem_FromMenu_ClosesMenuAndNavigatesToTarget` | 03 | menu closed; the fake's navigate-to-edit recorded the item |
  | `DeleteItem_FromMenu_ClosesMenuAndOpensConfirmForTarget` | 04 | menu closed; confirm sheet visible; then `ConfirmActionCommand.ExecuteAsync` → `DeletedBatches == [[item]]` |
  | `DismissItemMenu_ClosesMenuWithoutSideEffects` | 05a | menu closed; no navigation; confirm hidden |
  | `SetIsItemMenuOpenFalse_ClearsTarget` | 05b | set the property false → a subsequent `EditItemCommand` does not navigate |
  | `SetIsItemMenuOpenFalse_WhenAlreadyClosed_IsNoOp` | 05b | no exception; no PropertyChanged for `IsItemMenuOpen` |
  | `HandleBack_WithMenuOpen_ClosesMenuAndReturnsTrue` | 06 | returns true; menu closed; confirm still hidden |
  | `ItemMenuActions_ByDefault_IsEmpty` | 07 | a fake without an override → empty |
  | `ItemMenuActions_DerivedVm_KeepsDeclaredOrder` | 07 | a fake with 2 actions → `Text` order preserved |
  | `ExecuteItemMenuAction_ClosesMenuThenRunsActionOnceWithTarget` | 08 | menu already closed when the action runs (the action records `IsItemMenuOpen`); called once with the item |
  | `ExecuteItemMenuAction_WithNullTarget_DoesNotRunAction` | 08 | menu never opened → execute → no recorded calls |
  | `OpenItemMenu_Twice_KeepsSameTargetAndMenuOpen` | 01 | second open with the same item → still open, same target |
  | `TapItem_WhileMenuOpen_ClosesMenuAndDoesNotNavigate` | 15 | menu closed; no navigation |
  | `LongPressItem_WhileMenuOpen_ClosesMenuAndDoesNotSelect` | 16 | menu closed; `IsSelectionMode` false |

- [ ] **Step 2: Run the tests and confirm they fail.** Command: `dotnet test Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~CrudListViewModelBaseTests"`. Expected: compile failure or failures on the new tests only.
- [ ] **Step 3: Implement** per `design.md § Contracts`, `§ Invariants` and `§ Edge cases`.
  - Follow the existing command declaration pattern in the base (same generator or attributes as `EditItemCommand`).
  - `ExecuteItemMenuActionCommand` is async.
  - `IsItemMenuOpen` change to false → `ItemActionsTarget = default`.
- [ ] **Step 4: Run the full test project and confirm all pass.** Command: `dotnet test Tests/MyVocaList.Tests.csproj`. Expected: 0 failed.
- [ ] **Step 5: Commit.** Message: `feat: list item menu state in CrudListViewModelBase`, with the `Item:` trailer.

### Task 2: `more_vert` icon and `MenuItem` style (Level C)

**Files:**
- Create: `MyVocaList/Resources/Images/more_vert_outlined.svg`. Material Symbols Outlined "more_vert", 24×24 viewBox, same structure and fill convention as `edit_outlined.svg`.
- Modify: `MyVocaList/Resources/Styles/MaterialStyles.xaml`. Add `Style x:Key="MenuItem" TargetType="dx:DXButton"` next to `BottomSheetMenuItem` (~272), with:
  - `HeightRequest=44`, `Padding=16,0`, `IconIndent=12`, `IconWidth=20`, `IconHeight=20`, start-aligned content
  - `BackgroundColor=Transparent`, `TextColor={StaticResource OnSurface}`, `IconColor={StaticResource OnSurfaceVariant}`
  - `FontFamily=RobotoMedium`, `FontSize=14` (the exact setters of the `Label.Large` class, `MaterialStyles.xaml:144-147`)

**Interfaces:** Consumes nothing. Produces the style key `MenuItem` and the image `more_vert_outlined`. Review lane: Standard · Demo: build green, and a grep of the style shows 44 / 16,0 / 12 / 20 / RobotoMedium / 14 (REQ-OVFM-12a).

- [ ] **Step 1:** Add the SVG, then build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`. Expected: 0 errors.
- [ ] **Step 2:** Add the style, then build. Expected: 0 errors.
- [ ] **Step 3: Commit.** Message: `feat: more_vert icon and MD3 MenuItem button style`.

### Task 3: `ListItemOverflowButton` (Level C UI)

**Files:** Create `MyVocaList/UI/Components/Lists/ListItemOverflowButton.xaml` and `.xaml.cs`, modelled on `ListItemCheckbox.xaml`.

**Interfaces:**
- Consumes: `CrudListView.ShowItemMenu(View anchor, object item)` from Task 4. **Execution order is T1 → T2 → T4 → T3 → T5**, so that this method exists when T3 builds.
- Produces: `<lists:ListItemOverflowButton />`, which takes its `BindingContext` from the row item. Review lane: Standard · Demo: build green (wired in T5; ACs REQ-OVFM-09/10 are delivered through T5).

- [ ] **Step 1:** XAML per `design.md § ListItemOverflowButton`: 48×48, `StandardIconButton`, `more_vert_outlined`, "More options", and `IsVisible` bound to the inverse of `IsSelectionMode`. Code-behind `OnClicked` walks `Parent` to the first `CrudListView`. If none is found, it logs `Serilog.Log.ForContext("SourceContext", nameof(ListItemOverflowButton)).Warning(...)` (the pattern at `CrudListView.xaml.cs:209`) and does nothing. Declare the `pages:` xmlns, as in `ListItemCheckbox.xaml`. `InverseBoolConverter` is app-level (`App.xaml:8`), so `StaticResource` resolves. If one is found, it calls `ShowItemMenu(this, BindingContext)`.
- [ ] **Step 2:** Build. Expected: 0 errors.
- [ ] **Step 3: Commit.** Message: `feat: ListItemOverflowButton component`.

### Task 4: `CrudListView` hosts the menu (governed)

**Files:** Modify `MyVocaList/UI/Components/CrudListView.xaml` (next to `itemActionsSheet` ~139) and `CrudListView.xaml.cs` (`OnViewModelPropertyChanged` / confirm hand-off ~247-266, ~374-389).

**Interfaces:**
- Consumes: T1 members and the T2 `MenuItem` style.
- Produces: `public void ShowItemMenu(View anchor, object item)`. Review lane: Elevated (governed) · Demo: build and tests green; the popup opens when `ShowItemMenu` is called (exercised from T3/T5).

- [ ] **Step 1: XAML.** Add `DXPopup x:Name="itemMenu"` per `design.md § CrudListView`:
  - Edit (`edit_outlined`, label "Edit", `EditItemCommand`)
  - a `BindableLayout` over `ItemMenuActions` (`Content=Text`, `Icon=Icon`, `CommandParameter={Binding .}`). Each child's BindingContext is an `ItemMenuAction`, so its command needs an explicit source: give the CrudListView root `x:Name="root"` (or reuse the existing one) and bind `Command="{Binding Source={x:Reference root}, Path=BindingContext.ExecuteItemMenuActionCommand}"`, with `x:DataType={x:Null}` on that element. Edit and Delete sit outside the layout and bind directly.
  - Delete (`delete_outlined`, label "Delete", `DeleteItemCommand`)
  - all three use `Style=MenuItem`
  - stack `Padding=0,2`; `MinimumWidthRequest=112`, `MaximumWidthRequest=280`; `CornerRadius=16`; `AllowShadow=True`; `BackgroundColor=SurfaceContainerLow`; `IsOpen` TwoWay with `IsItemMenuOpen`
  - Then build.
- [ ] **Step 2: Code-behind.** Add `ShowItemMenu`, the `_showConfirmWhenMenuClosed` deferral and the `itemMenu.Closed` flush, per `design.md` (the handler never calls `DismissItemMenuCommand`). Then build and run the full tests. Expected: 0 errors, 0 failed.
- [ ] **Step 3: Hand-back notes.** For the orchestrator's task-log: the consumer map, the risk table and Changed files.
- [ ] **Step 4: Commit.** Message: `feat: CrudListView hosts the list item menu`.

### Task 5: Venues and People rows + governed registration

Consumes: `ListItemOverflowButton` (T3). Review lane: Standard · Demo: an overflow button on the Venues/People rows on the emulator.

**Files:** Modify `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` (ItemTemplate trailing ~46-48), then `MyVocaList/UI/Pages/People/PeoplePage.xaml` (~45-47), then `.claude/library/component-safety-gate.md` (governed list).

- [ ] **Step 1:** Venues: in the ItemTemplate trailing content, put `HorizontalStackLayout Spacing=8` holding `ListItemOverflowButton` and then the existing `ListItemCheckbox`. `SelectedItemTemplate` is untouched. Build.
- [ ] **Step 2:** People: the same change. Build.
- [ ] **Step 3:** Add `ListItemOverflowButton` (consumers: Venues, People) to the governed list.
- [ ] **Step 4:** Hand-back with the AC traceability matrix (AC | criterion | implementation | test or manual) for all REQ-OVFM rows.
- [ ] **Step 5: Commit.** Message: `feat: overflow menu on Venues and People rows`. Then push the branch.

### Task 6: Verification (orchestrator)

- [ ] `verifier` subagent on the branch diff plus the task-log.
- [ ] `dotnet build` (0 errors) and `dotnet test Tests/MyVocaList.Tests.csproj` (0 failed).
- [ ] Android emulator walk-through on Venues and People:
  - REQ-OVFM-09, 10 (unselected row in selection mode), 11, 12a/b
  - 13a (tap another row with the menu open)
  - 13b (one back press, page stays)
  - 14 (last visible row)
  - 17 (confirm sheet shown once)
  - scroll with the menu open
  - Screenshot the open menu and the last-row menu.
- [ ] Artists and Songs regression: the list renders, the trailing button works, back is unchanged.
- [ ] Orchestrator writes `task-log.md` on `develop`. Merge the branch into `develop`, set status 🟡, and halt for Helder.

### Task 7: Helder on-device pass (gate)

- [ ] Helder passes REQ-OVFM-09…17 on device and confirms the 16dp corner. Then status ✅.

---

# Revision 2026-11-20b — shared Menu component (T11a → T11c → T11b → T12)

**Goal:** turn the menu into reusable `Menu` + `OverflowMenuButton` components (usable on any page), make `CrudListView` a consumer of them, and apply the final MD3 deltas. **Spec:** `design.md § Revision 2026-11-20b` (signatures and mechanisms are binding), `requirements.md § Revision 2026-11-20b` (REQ-OVFM-12a/12b amended, 18–25).

**Dispatch units (plan-review B1):** T11a is split into four sequential briefings, T11a1–T11a4, each with its own commit. Order: T11a1 → T11a2 → T11a3 → T11a4 → T11c → T11b → T12. One implementor per briefing, never in parallel, all in the same worktree and branch.

**Registry (plan-review B3):** `Menu` and `OverflowMenuButton` are already registered in `.claude/library/component-safety-gate.md`. The orchestrator did it on `develop` in commit `1161ac52`, because library docs land on develop. No implementor touches that file.

**Order rationale:** T11c runs before T11b. It removes the only caller of `CrudListView.ShowItemMenu`, so T11b can delete it without touching `ListItemOverflowButton`, and each governed component is changed only in its own task. Between the T11c and T11b commits the row button builds but finds no `IMenuHost` (warning only). That intermediate state exists only on the branch. **It must never be deployed or tested on its own**: the menu would open without an anchor. T12 tests only the final head.

**Branch:** `task/md3-menu-shared-component`, in a worktree cut from `develop` (after the docs commit for this revision). One implementor runs the tasks sequentially. **Each task gets its own commit**, with an `Item: md3-overflow-menu-for-list-item-actions` trailer, and its own hand-back section (no bundling across the governed components). **Global Constraints above still apply.**

## T11a — new code, split into T11a1–T11a4 (not a governed-component change)

Every test starts with `// [AC] REQ-OVFM-xx: <criterion>`, one AC per test. The VM tests trace to REQ-OVFM-18. The `MenuItemSelection` tests trace to 19: the `CanExecute`-false case traces to the binding `Select` contract in `design.md § Signatures` ("Runs item.Command (when CanExecute) … THEN closes"). The `MenuResolution` tests trace to 20 and the `MenuItemShape` tests to 21.

### T11a1 — `MenuItemModel` + `ItemMenuItems` (TDD, Level A)
- **Files owned:**
  - `MyVocaList/UI/ViewModels/MenuItemModel.cs` (new)
  - `MyVocaList/UI/ViewModels/ICrudListViewModel.cs`
  - `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`
  - `Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`
- **Produces:** `MenuItemModel`, `ICrudListViewModel.ItemMenuItems`. **Consumes:** existing commands. **ACs:** REQ-OVFM-18. **Review lane:** Elevated. **Estimate:** 45 min. **Demo:** `dotnet test` green.
- [ ] 1. Write the failing tests first:
  - `ItemMenuItems_Default_IsEditThenDelete`
  - `ItemMenuItems_WithPageActions_EditActionsDelete` (existing `ActionsTestCrudListViewModel`)
  - `ItemMenuItems_PageAction_CommandIsExecuteWithActionParameter`
  - `ItemMenuItems_IsBuiltOnce` (same instance on two reads)
  Run them and confirm they fail for the right reason.
- [ ] 2. Implement `MenuItemModel` (`design.md § Signatures`), add `ItemMenuItems` to the interface, and build it once in the base class: Edit (`edit_outlined`) → actions → Delete (`delete_outlined`). Run the tests green, then the full test run.
- [ ] 3. Commit `feat: MenuItemModel and ItemMenuItems for the list item menu`.

### T11a2 — pure helpers (TDD, Level B)
- **Files owned:**
  - `MyVocaList/UI/Components/Menus/MenuItemSelection.cs`, `MenuResolution.cs`, `MenuItemShape.cs` (all new)
  - `Tests/Unit/Components/MenuItemSelectionTests.cs`, `MenuResolutionTests.cs`, `MenuItemShapeTests.cs` (all new; follow the existing `Tests/Unit/Components/` pattern)
  - That is 7 small pure-C# files, accepted above the 5-file guideline because each is under 40 lines.
- **Produces:** the helpers and `IMenuHost`, which references `Menu`. Because `Menu` does not exist yet, declare `IMenuHost` in T11a3 instead and keep this task to the helpers plus tests (6 files).
- **Consumes:** `MenuItemModel` (T11a1). **ACs:** REQ-OVFM-19, 20, 21. **Review lane:** Elevated. **Estimate:** 45 min. **Demo:** `dotnet test` green.
- [ ] 1. `MenuItemSelection` tests, then the implementation:
  - `Select_RunsCommandBeforeClose`
  - `Select_DoesNotCloseWhenCommandAlreadyClosed`
  - `Select_DoesNotCloseWhenCloseOnItemSelectedFalse`
  - `Select_CommandCannotExecute_DoesNotRun_StillCloses`
  - `Select_NullCommand_DoesNotThrow` (use `null!`)
- [ ] 2. `MenuResolution` tests (`TMenu = string`), then the implementation:
  - `Resolve_ExplicitWins`
  - `Resolve_NearestHost`
  - `Resolve_SkipsHostWithNullMenu`
  - `Resolve_NoneReturnsNull`
- [ ] 3. `MenuItemShape` tests, then the implementation: `For_SingleItem_All12`, `For_First_Top12Bottom4`, `For_Last_Top4Bottom12`, `For_Middle_All4`.
- [ ] 4. Full test run. Commit `feat: menu selection, resolution and item-shape helpers`.

### T11a3 — `MenuItem` style + `Menu` + `IMenuHost`
- **Files owned:**
  - `MyVocaList/Resources/Styles/MaterialStyles.xaml` (`MenuItem` style only)
  - `MyVocaList/UI/Components/Menus/Menu.xaml(.cs)` (new)
  - `MyVocaList/UI/Components/Menus/IMenuHost.cs` (new)
- **Produces:** `Menu`, `IMenuHost`. **Consumes:** T11a1 and T11a2. **ACs:** REQ-OVFM-12a (amended), 21 (padding), 23, 24, 25. **Review lane:** Elevated. **Estimate:** 90 min. **Demo:** build green; `Menu` is not used yet.
- [ ] 1. Style: in `MenuItem`, `Padding` becomes `12,0` and `CornerRadius="4"` is added. Build.
- [ ] 2. `Menu.xaml(.cs)` per `design.md § Signatures / § Mechanisms`:
  - the DXPopup chrome copied from `CrudListView.xaml` (`itemMenu`), with content `Padding="4,2"`;
  - item buttons built in code-behind: `MenuItemShape` corners, the `CanExecute` → `IsEnabled` sync, and a tap calls `MenuItemSelection.Select`;
  - the `PopupBackHandler` attach on Opened and detach on Closed (logic moved from `CrudListView.xaml.cs`; `PopupBackHandler.cs` unchanged; `CrudListView` is not edited here);
  - `SemanticExtensions.SetSemanticFocus` on the first item on Opened;
  - `IsOpen` kept in sync both ways; `Closed` raised for every close path.
  Then `IMenuHost.cs`. Check the W9 name collision. Build with 0 errors, then the full test run.
- [ ] 3. Commit `feat: shared MD3 Menu component`.

### T11a4 — `OverflowMenuButton`
- **Files owned:** `MyVocaList/UI/Components/Menus/OverflowMenuButton.xaml(.cs)` (new).
- **Produces:** `OverflowMenuButton`. **Consumes:** `Menu`, `IMenuHost`, `MenuResolution`. **ACs:** REQ-OVFM-20 (wiring), 22, 25. **Review lane:** Elevated. **Estimate:** 45 min. **Demo:** build green.
- [ ] 1. Implement per spec:
  - resolution through `MenuResolution` over the `Parent` chain;
  - `PlacementTarget = this`;
  - `Command` vs `Show`;
  - the pressed state layer until that menu's `Closed`;
  - no reference to `CrudListView`, `ICrudListViewModel` or `CrudListPageBase` (REQ-OVFM-25).
  Build, then the full test run.
- [ ] 2. Commit `feat: shared OverflowMenuButton component`.

Each T11a hand-back: Changed files, AC traceability rows, test counts.

## T11b — `CrudListView` consumes `Menu` (governed; gates in `design.md § Governance (revision 2026-11-20b)`)

Files owned: `MyVocaList/UI/Components/CrudListView.xaml`, `CrudListView.xaml.cs`.
**Produces:** `CrudListView : IMenuHost` hosting `Menu`. **Consumes:** `Menu` (T11a3) and `ItemMenuItems` (T11a1). **ACs:** REQ-OVFM-11, 12b, 13a/b, 17 (unchanged behaviour). **Review lane:** Architectural (governed). **Estimate:** 45 min. **Demo:** the full feature works on the emulator.

- [ ] 1. **XAML** (no build until step 2: the code-behind still references the old popup members, so steps 1 and 2 share one build point). Replace the inline `dx:DXPopup x:Name="itemMenu"` with `<menus:Menu x:Name="itemMenu" IsOpen="{Binding IsItemMenuOpen, Mode=TwoWay}" ItemsSource="{Binding ItemMenuItems}" Closed="OnItemMenuClosed"/>` and add the `menus` xmlns.
- [ ] 2. **Code-behind.**
  - Implement `IMenuHost` (`HostedMenu => itemMenu`).
  - Delete `OnItemMenuOpened`, `_menuBackRoute`, the back callback, and `ShowItemMenu` (no caller remains after T11c; grep to confirm).
  - **Keep** the `_showConfirmWhenMenuClosed` hand-off, reading `itemMenu.IsOpen`.
  - Build, then run the full tests.
- [ ] 3. Commit `refactor: CrudListView hosts the shared Menu`.
- Hand-back: Changed files, the consumer map, and the risk table copied from the spec.

## T11c — `ListItemOverflowButton` wraps `OverflowMenuButton` (governed)

Files owned: `MyVocaList/UI/Components/Lists/ListItemOverflowButton.xaml(.cs)` only. Runs **before** T11b.
**Produces:** a wrapper that opens the menu through `OverflowMenuButton`. **Consumes:** `OverflowMenuButton` (T11a4) and `OpenItemMenuCommand`. **ACs:** REQ-OVFM-09, 10 (unchanged behaviour). **Review lane:** Architectural (governed). **Estimate:** 30 min. **Demo:** build green; not deployable alone (see Order rationale).

- [ ] 1. **XAML.** The content becomes `menus:OverflowMenuButton` with the selection-mode `IsVisible` binding (unchanged), `Command="{Binding BindingContext.OpenItemMenuCommand, Source={RelativeSource AncestorType={x:Type pages:CrudListPageBase}}}"` and `CommandParameter="{Binding .}"`. Build.
- [ ] 2. **Code-behind:** the constructor only. Build, then run the full tests.
- [ ] 3. Commit `refactor: ListItemOverflowButton wraps OverflowMenuButton`.
- Hand-back: Changed files, the consumer map (Venues, People), and the risk rows.

## T12 — Verification (orchestrator)

**Review lane:** Standard. **Estimate:** 60 min.

AC → verification map:
- 12a/21: reviewer check of the style and `Menu` padding, plus visual.
- 18–21: unit tests.
- 22: emulator.
- 23: reviewer check of `SetSemanticFocus`, plus a TalkBack step on Helder's device (optional).
- 24, 25: reviewer check and grep.
- 09–17: emulator regression.

- [ ] `verifier` subagent on the branch diff against the 2026-11-20b ACs.
- [ ] Clean Android build (delete `obj/` and `bin/` for `net10.0-android`), deploy to the emulator, then walk through:
  - Venues and People: menu open, anchor, Edit, Delete → confirm once, back closes the menu only, outside tap closes only, last-row reposition;
  - the item shapes (rounded first and last item, inset 4) and the pressed state on the overflow button while open (REQ-OVFM-21, 22);
  - the Artists and Songs regression.
- [ ] Task-log entries per task, on `develop`. Merge into `develop`, push. Then Helder's on-device pass (including TalkBack for REQ-OVFM-23, optional).
