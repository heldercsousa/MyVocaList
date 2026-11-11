# Plan — CRUD list selection gestures (tap-to-edit, long-press-to-select)

## Context
Today on all 4 CRUD list pages (Venues, Songs, Artists, People) a tap on a row toggles selection (DXCollectionView `SelectionMode="Multiple"`). Editing only works through the floating toolbar's **Edit selected** button, and deleting only through **Delete selected**. Helder wants:
- **No selection:** tap → edit form; long-press → selects that row (enters selection mode).
- **Selection mode (≥1 selected):** tap any row → toggle it. Leaving selection mode (toggle-all clear, or the last row deselected) brings back long-press for the first selection.
- **Row checkbox:** a tap always toggles that row, in any mode.
- **Per-item actions (Edit / Delete one item)** in selection mode go through a contextual menu.
- **Floating toolbar:** remove the Edit button. Stay MD3-compliant and use MD3 terms.

## MD3 research (rendered from m3.material.io with Playwright + Edge; texts in scratchpad `m3/*.txt`)
| Question | Official answer |
|---|---|
| Must a floating toolbar have ≥ 3 actions? | **No.** No minimum appears in Overview, Guidelines, Specs or Accessibility. The only limits found: "Be cautious of including too many controls", a 48×48dp minimum target, and "should be fully visible on screen". |
| Can the FAB be paired with it? | **Yes** (Guidelines § Floating toolbar with FAB): "A FAB can be placed next to a floating toolbar to present one high-priority action alongside a unified set of toolbar actions." Specs: "On other platforms, each component needs to be added separately". That matches our current `HorizontalStackLayout`. |
| Emphasis | "Don't emphasize multiple buttons with bold, primary colors, such as a button and FAB together. Emphasize one action at a time." |
| Contextual use | "Floating toolbar … best used for contextual actions relevant to the body content or the specific page." |
| Vibrant toggle colors | Specs: vibrant "Toggle tonal button: (Surface container, On surface)", "Standard button (On primary container)". |
| Context menu trigger | Menus: "A menu should open when a person … performs a specific action to trigger the menu, like right-click or **press-and-hold**." |
| Menu on mobile | Bottom sheets: "Use a **modal bottom sheet** as an alternative to inline menus or simple dialogs on mobile"; "can contain … menu items (in list … layouts)". |
| Lists | "Multi-select list items: … Can't have secondary nested actions". Checkbox = "Trailing selection control". |
| App bar alternative | Not needed. App bars allow "1–2 essential actions" and "If the product has many actions, place those in a toolbar". |

**Decision:** keep the floating toolbar with **2 icon buttons (Select all toggle, Delete)** paired with the existing FAB. The app bar and docked-toolbar fallbacks are not needed.

## Behavior (state machine — the spec ACs)
| Mode | Tap row | Long-press row | Tap row checkbox |
|---|---|---|---|
| Browse (0 selected) | edit form (`NavigateToEditAsync`) | select row → Selection mode | select row → Selection mode |
| Selection (≥1) | toggle row (DX native) | open **item actions sheet** (modal bottom sheet, menu items **Edit** and **Delete**) for that row, selected or not | toggle row |

- In Browse mode the floating toolbar is **hidden** (FAB alone). In Selection mode it is visible: Select all (toggle; clears when all are selected) and Delete selected. This closes backlog item `floatingtoolbar-visibility-verify`.
- The selected state of the Select all toggle changes from Primary-filled to the MD3 vibrant toggle colors. Delete loses its `IsSelected` highlight. This removes the double-Primary emphasis next to the FAB.
- Item actions: **Edit** closes the sheet and runs `NavigateToEditAsync(item)`. **Delete** closes it and opens the existing confirm sheet for that one item (same `BuildDeleteConfirmMessage` / `ExecuteDeleteAsync` path). After any delete the selection is cleared (existing reload behavior).
- Hardware back in Selection mode: close an open sheet first (existing), else clear the selection.
- The "N selected" app bar title stays as it is.

## Technical approach
DXCollectionView 25.2 has `Tap` / `LongPress` (`CollectionViewGestureEventArgs.Item`). It has no way to stop a tap from selecting (checked via Context7). Therefore:
- **Dynamic `SelectionMode`:** `None` in Browse mode, `Multiple` in Selection mode, bound to VM `IsSelectionMode`. DX's native tap-toggle then gives Selection-mode taps for free.
- **Event-order guard:** `CrudListView.OnSelectionChanged` forwards the count to the VM through `Dispatcher.Dispatch`. When the last row is deselected, the mode then switches to `None` *after* the same gesture's `Tap` has run, so that tap cannot also open the edit form.

### Files (most changes in shared code)
- `MyVocaList/UI/ViewModels/ICrudListViewModel.cs` + `CrudListViewModelBase.cs`
  - add `IsSelectionMode`, `ItemTapCommand`, `ItemLongPressCommand`, `ToggleItemSelectionCommand`, `ItemActionsSheetState`, `EditItemCommand`, `DeleteItemCommand`
  - refactor `RequestBatchDelete` → `RequestDelete(IList<TItem>)`, reused by both delete paths
  - remove `EditSelectedCommand`, `CanEditSelected`, `ExecuteEditSelectedAsync`
  - `OnSelectionChanged(0)` → `IsSelectionMode = false`
- `MyVocaList/UI/Components/CrudListView.xaml(.cs)` (**governed**, 4 consumers)
  - `SelectionMode` binding; wire `Tap` / `LongPress` to the VM commands, replacing the unused `ItemTapCommand` bindable
  - deferred `OnSelectionChanged`
  - toolbar: 2 actions + `IsVisible="{Binding IsSelectionMode}"`
  - new item-actions `dx:BottomSheet` beside `confirmSheet`, with sheet-to-sheet hand-off dispatched in sequence
  - expose `ToggleItemSelectionCommand` for row checkboxes
- **New** `MyVocaList/UI/Components/Lists/ListItemCheckbox.xaml(.cs)`
  - shared trailing selection control: `dx:DXButton` (consumes the touch, like the existing YouTube/catalog row buttons) wrapping an input-transparent `dx:CheckEdit`, 48dp target
  - command resolved through `RelativeSource AncestorType=CrudListView` → pages only write `<lists:ListItemCheckbox IsChecked="True|False" />`
- `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml.cs` — `ApplySelectedState` → vibrant toggle tonal colors (single consumer, not governed)
- `MyVocaList/Resources/Styles/MaterialStyles.xaml` — `BottomSheetMenuItem` style if no equivalent exists
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — back button clears the selection
- 4 pages (`UI/Pages/{Venues,Songs,Artists,People}/*Page.xaml`) — replace the 8 `dx:CheckEdit` occurrences with `ListItemCheckbox`. Nothing else per page.
- Tests: `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (new Level-A tests per AC); `PersonsViewModelTests.cs` (drop the `CanEditSelected_*` tests and record the removal in the spec)

Not touched: `ListItem` (governed), `SmallAppBar`, services, DB.

## Execution (autonomous; Sonnet implementors per memory; worktree for code)
1. ✅ **Spec on develop** (done 2026-11-11)
   - `crud-list-deduplication` is shipped (✅), so the spec lives in this post-ship change folder (created by `backlog_gen.py register --kind change`).
   - `floatingtoolbar-visibility-verify` → 🔵 Superseded.
   - `spec-reviewer` + `plan-reviewer` findings folded into rev. 2 of requirements/design/tasks.
2. **Worktree** `feature/crud-list-selection-gestures` off develop.
3. **Tasks T1 → T2a ∥ T2b → T2c → T3 → T4 → T5** — the exact scope, files, ACs and governed gates are in `tasks.md` (authoritative over the summary above).
4. **Verify:** `/sln-build` Android 0 errors; full `dotnet test`; `verifier` subagent; `/sln-review`; task-log AC traceability matrix; `/sln-docs-sync`; push the branch.
5. **Hand off** to Helder for on-device tests (checklist below). Merge into develop only after the reported pass (pre-authorized), then mark the feature ✅.

## Verification (Helder's device checklist, recorded in task-log)
For each of the 4 pages:
1. Tap a row with nothing selected → edit form opens.
2. Long-press → row selected, toolbar appears, title "1 selected".
3. Tap other rows → they toggle.
4. Tap the last selected row → mode exits, toolbar hides, the edit form does **not** open (event-order guard).
5. Checkbox tap selects from Browse mode.
6. Long-press in Selection mode → sheet with Edit/Delete; Edit opens that item; Delete confirms and deletes only that item.
7. Select all → toggles all / clears and exits.
8. Back button clears the selection (Venues: a second back shows the exit sheet). Back with a sheet open closes the sheet.
9. Songs YouTube and Artists catalog row buttons run only their own action in both modes (no navigation, no selection change).
10. Toolbar Select all selected state is tonal (not Primary); only the FAB is Primary.

Known risk: the order of DX `Tap` vs `SelectionChanged` is unverified on device (`design.md § CrudListView` cases A/B/C). If step 4 fails, apply the `TapAfterSelectionChangeGuard` fallback. That fallback is a governed `CrudListView` edit, logged with its own gate record.

Out of scope, reported only: `PeoplePage.xaml:34` binds `IsEmptyNoPeople` (the VM has `IsEmptyNoPersons`); dead handlers in `CrudListPageBase`; the Songs/Artists nested row buttons are an existing MD3 "secondary nested actions" deviation.
