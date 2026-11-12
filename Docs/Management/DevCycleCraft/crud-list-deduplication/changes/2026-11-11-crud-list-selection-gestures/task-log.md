# Task Log — CRUD list selection gestures

## 2026-11-11 — Spec

- MD3 docs rendered from m3.material.io (Playwright 1.x from the npx cache + installed Edge; Playwright MCP not configured in this session). Evidence quoted in `design.md § MD3 evidence`.
- Context7 (`/websites/devexpress_maui`): `DXCollectionView.Tap` / `LongPress` (`CollectionViewGestureEventArgs.Item`, `ItemHandle`); no tap-selection suppression → dynamic `SelectionMode` design.
- Spec written: README, requirements, design, tasks, plan.
- Spec relocated to the post-ship `changes/` folder of `crud-list-deduplication` (shipped spec immutability); registered via `backlog_gen.py register --kind change`; `floatingtoolbar-visibility-verify` → 🔵 Superseded.
- Review round 1 (fresh `spec-reviewer` + `plan-reviewer`): both FAIL. Blocking items: interface signatures, ancestor resolution, mode-switch race, AC testability, T1 build-green/ownership, Delete slot move, deferred decisions, governed-gate artifacts, T2 sizing. All folded into rev. 2.

## 2026-11-11 — T1 VM base + interface (additive) — To Review

Branch `feature/crud-list-selection-gestures` (worktree `.worktrees/clsg`), commit `653fc741`.

### Changed files
- `MyVocaList/UI/ViewModels/ICrudListViewModel.cs` — added selection-mode contract (design.md § Contracts); `EditSelected*` kept until T2c.
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` — implementation; `RequestBatchDelete` → `RequestDelete(IList<TItem>)`; `IsSelectionMode` maintained in `OnSelectedCountChanged`.
- `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` — 16 new tests; double records navigation/delete args.

### Verification
- `CrudListViewModelBaseTests` 21/21; full suite 969 passed, 0 failed (pre-commit hook re-ran tests).
- Red was observed at compile level (60 missing-member errors) before Green; not per-assertion.

## 2026-11-11 — T2b toolbar colors + style / T2a ListItemCheckbox — To Review

Commits `78841416` (T2b), `28af0a6a` (T2a) on `feature/crud-list-selection-gestures`.

### Changed files
- `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml.cs` — selected state = `SurfaceContainer` bg + `OnSurface` icon (toggle tonal); comments updated.
- `MyVocaList/Resources/Styles/MaterialStyles.xaml` — new `BottomSheetMenuItem` style.
- `MyVocaList/UI/Components/Lists/ListItemCheckbox.xaml(.cs)` — new trailing selection control (DXButton 48×48 + input-transparent CheckEdit, command via `AncestorType=CrudListPageBase`).

### Verification
- Android build exit 0 (NU19xx package warnings only); pre-commit tests 969 passed.
- Level C for T2a/T2b (XAML/plumbing; behavior covered by T1 VM tests + device checklist). Open: checkbox color uses `{StaticResource Primary}` while pages used `{dx:ThemeColor Primary}` — align in T3 if visually different.

## 2026-11-11 — T2c Governed: CrudListView + Edit-selected removal — To Review

Commit `6b11644` on `feature/crud-list-selection-gestures`. Governed-component gates: `tasks.md § T2c` (dedicated task + MD3 review, consumer map Venues/People/Artists/Songs, per-consumer risk table, Helder pre-approval 2026-11-11).

### Changed files
- `MyVocaList/UI/Components/CrudListView.xaml` — `SelectionMode="None"` initial; `Tap`/`LongPress`; toolbar = Select all + Delete (Action2), `IsVisible=IsSelectionMode`; new `itemActionsSheet`.
- `MyVocaList/UI/Components/CrudListView.xaml.cs` — gesture forwarding; dead `ItemTapCommand` bindable removed; `ApplySelectionMode()` (I4); dispatched `OnSelectionChanged` re-reading the count; item-sheet sync + confirm hand-off.
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, `ICrudListViewModel.cs` — `EditSelectedCommand`/`CanEditSelected`/`ExecuteEditSelectedAsync` removed.
- `MyVocaList.Tests/Unit/ViewModels/PersonsViewModelTests.cs` — `CanEditSelected_*` tests removed (spec-removed behavior, requirements § Removed behavior).

### Verification
- Grep: no `EditSelected|CanEditSelected|ExecuteEditSelectedAsync` in MyVocaList/MyVocaList.Tests; no `CrudListView.ItemTapCommand` consumer.
- Android build 0 errors; tests 967 passed (969 − 2 removed).

## 2026-11-11 — T3 Pages + back button — To Review

Commit `bf1f4baf` on `feature/crud-list-selection-gestures`.

### Changed files
- `MyVocaList/UI/Pages/{Venues,People,Artists,Songs}/*Page.xaml` — the 8 `dx:CheckEdit` → `lists:ListItemCheckbox` (`VerticalOptions="Center"` kept from the originals); Songs YouTube / Artists catalog buttons untouched.
- `MyVocaList/UI/Components/Lists/ListItemCheckbox.xaml` — checked color `{dx:ThemeColor Primary}` (matches the replaced CheckEdits; design.md updated).
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — `OnBackButtonPressed` → `ListViewModel.HandleBack() || base…`; deleted unwired leftovers `OnSelectionChanged`, `OnCollectionViewScrolled`, `OnConfirmSheetStateChanged`.

### Verification
- Grep: no `dx:CheckEdit` left in the 4 pages; leftover handler names unreferenced.
- Android build 0 errors after each page; tests 967 passed.
- `ListItemCheckbox` now has 4 consumers → governed (registered in `component-safety-gate.md` by the T4 develop commit).

## 2026-11-11 — T4 Guideline amendments (`amend:` commit `8df8ef14` on the feature branch)

- `crud-appbar-list-toolbar.md` (List Layout: Gestures law, Never list; FloatingToolbar law: selection-mode visibility, 2 slots, `IsSelected` toggles only), `m3-floating-toolbar.md` (toggle-tonal selected state, no MD3 minimum, contextual use), `crud-listview.md`, `crud-checklists.md`, `dx-collectionview.md`, `testing-conventions.md`, `testing-test-types.md` (stale `CanEditSelected`/`EditSelected` references), `component-safety-gate.md` (`ListItemCheckbox` governed).
- Needs Helder's human review (context-file authorship rule).
- Landed on the feature branch, not develop: the develop pre-commit documented-command guard fails on another session's nested worktree `.claude/worktrees/agent-ae30a6ef9c3387d12` (32 backlog_gen + 37 lease invocations in its docs). That environment issue was left untouched, since the worktree is not ours. The laws take effect at merge, together with the code.

## 2026-11-11 — T5 Verification — awaiting Helder on-device gate

Branch `feature/crud-list-selection-gestures` pushed (head `8df8ef14`). Fresh `verifier` subagent: **CONDITIONAL PASS** (no Critical/Major; tests 967/967).

### AC traceability matrix
| AC | Implementation | Test / verification |
|---|---|---|
| REQ-CLSG-01 | `CrudListViewModelBase` item-tap handler | `TapItem_InBrowseMode_NavigatesToEditAndDoesNotSelect`, `TapItem_NonTItemParameter_IsNoOp` |
| REQ-CLSG-01b | same | `TapItem_InSelectionMode_DoesNotNavigate` |
| REQ-CLSG-02 | `OnItemLongPressed` → `BeginSelection` | `LongPressItem_InBrowseMode_SelectsExactlyThatItemAndEntersSelectionMode`, `LongPressItem_NonTItemParameter_IsNoOp` |
| REQ-CLSG-03 | DX native `Multiple` + `CrudListView.ApplySelectionMode` | device |
| REQ-CLSG-04a | `OnSelectedCountChanged` | `OnSelectionChangedToZero_LeavesSelectionMode` |
| REQ-CLSG-04b | `ToggleSelectAll` | `SelectAllToggle_WhenAllSelected_ClearsSelectionAndLeavesSelectionMode` |
| REQ-CLSG-04c | `RequestDelete` closure | `ConfirmedDeleteOfSelection_ClearsSelectionAndLeavesSelectionMode` |
| REQ-CLSG-04d | `LoadFirstPageAsync` | `Reload_InSelectionMode_ClearsSelectionAndLeavesSelectionMode` |
| REQ-CLSG-05a | `ToggleItemSelection` + `ListItemCheckbox` | `ToggleItemSelection_InBrowseMode_SelectsItemWithoutNavigating`, `ToggleItemSelection_NonTItemParameter_IsNoOp`; device (checkbox tap) |
| REQ-CLSG-05b | `ToggleItemSelection` | `ToggleItemSelection_InSelectionMode_TogglesItemAndLeavesModeWhenEmpty` |
| REQ-CLSG-06 | `OpenItemActions` + `itemActionsSheet` | `LongPressItem_InSelectionMode_OpensItemActionsAndKeepsSelection` |
| REQ-CLSG-07 | `EditItemAsync` | `EditItemAction_HidesSheetClearsTargetAndNavigatesToTarget` |
| REQ-CLSG-08 | `DeleteItem` + `RequestDelete` + sheet hand-off | `DeleteItemAction_HidesSheetOpensConfirmForExactlyThatItem` |
| REQ-CLSG-09 | `CrudListView.xaml` toolbar | device |
| REQ-CLSG-10 | `CrudListView.xaml` toolbar `IsVisible` | device |
| REQ-CLSG-11 | `FloatingToolbar.ApplySelectedState` | device |
| REQ-CLSG-12 | `HandleBack` + `CrudListPageBase.OnBackButtonPressed` | `HandleBack_ConsumesLayersInOrderThenReturnsFalse`; device (back key) |
| REQ-CLSG-13 | 4 page XAMLs | review: grep 8 `ListItemCheckbox`, 0 `dx:CheckEdit` |
| REQ-CLSG-14 | unchanged row buttons (DXButton consumes touch) | device |

### Verifier warnings (Minor, accepted / tracked)
1. `CrudListView` has no unsubscribe on `Unloaded` (old VM is unsubscribed on BindingContext change; pages are shell-cached) — accepted.
2. `OnSelectionChanged(int)` sets `SelectedCount` without `RunOnUiThread`; safe because the view dispatches to the UI thread — accepted.
3. Case C (DX `Tap` in a later cycle than `SelectionChanged`) — device checklist step 4; fallback `TapAfterSelectionChangeGuard` in design.md.
4. Test nits: 04a test leaves `SelectedItems` populated; non-`TItem` tests reuse AC IDs (extra tests, same AC) — accepted.

### Pending — Helder
- On-device checklist (`plan.md § Verification`, steps 1–10 on all 4 pages).
- Human review of the T4 `amend:` library changes (`8df8ef14`).
- After a reported pass: merge `feature/crud-list-selection-gestures` → develop (pre-authorised), set status ✅.

## T5 — Verify + hand-off (2026-11-12) — Done

- Helder's device check: T1, T2a, T2b, T2c, T3 OK; T4 guideline files reviewed via paths.
- Merged `feature/crud-list-selection-gestures` into develop on Helder's instruction. Verification on merged develop: `dotnet test` 967/967 passed; Android build 0 errors (27 warnings).
- **Deviation:** REQ-CLSG-06 (long-press in selection mode) does nothing on device. Carried into "CrudListView: one list mode at a time", which redefines it as a toggle (no menu).
- Superseded parts (item actions bottom sheet, ListItemCheckbox placement) are replaced by the registered ListItem, overflow menu and overflow button tasks.

### Changed files
- None in T5 (merge only); `tasks.md`, `task-log.md`, README status.

