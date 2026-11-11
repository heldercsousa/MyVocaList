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
