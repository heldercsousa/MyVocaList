# Plan: Fix CRUD List Deduplication BACKLOG Status

## Context

The BACKLOG.md shows Steps 1–6 of "Code Cleanup — CRUD List Page Deduplication" as `🔴 Blocked`, but a prior session (June 4, 2026) already completed all implementation through Step 5. The user noticed the code-behind files already inherit `CrudListPageBase` and asked for a status audit.

---

## What Was Found

### Infrastructure (Step 1) — ✅ DONE
Committed `37613a2` on Jun 4:
- `MyVocaList/UI/Pages/ICrudListViewModel.cs` — interface with `ConfirmSheetState`, `IsSearchMode`, `IsScrolled`, `SelectedCount`, `InitializeAsync()`, `OnSelectionChanged()`, `CloseSearchCommand`
- `MyVocaList/UI/Pages/CrudListPageBase.cs` — abstract `ContentPage` subclass handling lifecycle, scroll, selection, back-button, and confirm-sheet sync
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` — abstract generic base with all 9 shared commands (Refresh, LoadMore, Delete, Edit, SelectAll, ConfirmAction, DismissConfirm, OpenSearch, CloseSearch), pagination, debounced search, empty-state logic (~286 lines)

### VenuesPage migration (Step 2) — ✅ DONE
Committed `f784551` on Jun 4: ~220 lines removed from `VenuesViewModel` + `VenuesPage.xaml.cs`.

### PeoplePage migration (Step 3) — ✅ DONE
Committed `eff8e0a` on Jun 4 (batched with Songs): ~500 lines removed across `PersonsViewModel` + `SongsViewModel`.

### SongsPage migration (Step 4) — ✅ DONE
Same commit `eff8e0a`. `SongsViewModel` retains entity-specific `IsCatalogMode`, `AddToCatalogCommand`, `RemoveFromCatalogCommand`.

### ArtistsPage migration (Step 5) — ✅ DONE
Committed `334db86` on Jun 4: `ArtistsViewModel` retains `RoleFilter` chip filtering, `ViewCatalogCommand`, `GoBackCommand`.

### Code review fix pass — ✅ DONE
Committed `13998c4` on Jun 4:
- `ICrudListViewModel` now extends `INotifyPropertyChanged`
- `OnSelectionChanged` uses `ICollection.Count` (O(1)) instead of `Cast<object>().Count()` (O(n))
- `CrudListViewModelBase` accepts `ILogger` via constructor
- Removed `LogLoadMoreError` abstract method (was leaking identical boilerplate into all 4 VMs)
- Added protected `ReloadAsync()` for filter-change reloads without triggering shimmer
- `ArtistsViewModel.OnRoleFilterChanged` now calls `ReloadAsync()` — fixes visible shimmer on filter tap

### MVVM pattern — CLEAN
`CrudListPageBase` contains **zero business logic**. It only handles platform-level lifecycle events (OnAppearing, OnBackButtonPressed, scroll events, selection events) and bridges them to the ViewModel via the `ICrudListViewModel` interface. No ViewModel responsibility leaked into the page base.

### Step 6 (Post-migration guideline review) — ⬜ NOW UNBLOCKED
Was blocked on Step 5. Step 5 is done. Step 6 requires updating `.claude/library/crud-pages.md` and any related rules files to document `CrudListPageBase` + `CrudListViewModelBase<TItem>` as the canonical pattern.

### XAML repetition — acknowledged, not in scope
The 4 XAML files still have ~80–95% structural overlap (AppBar, ShimmerView, DXCollectionView, EmptyState, Toolbar, FAB, BottomSheet). This is expected — XAML templates cannot be shared via C# inheritance. The per-entity differences (icon type, support text, filter chips, catalog mode) mean further XAML extraction would require a custom composite control, which is a separate future task if desired.

---

## Action Required

Update `Docs/Management/BACKLOG.md`:

| Row | Old Status | New Status |
|-----|-----------|------------|
| **Code Cleanup — CRUD List Page Deduplication** (parent) | `💡 Pending` | `🟡 In Progress` |
| ↳ Step 1: Implement shared infrastructure | `🔴 Blocked` | `✅ Done` |
| ↳ Step 2: Migrate VenuesPage | `🔴 Blocked` | `✅ Done` |
| ↳ Step 3: Migrate PeoplePage | `🔴 Blocked` | `✅ Done` |
| ↳ Step 4: Migrate SongsPage | `🔴 Blocked` | `✅ Done` |
| ↳ Step 5: Migrate ArtistsPage | `🔴 Blocked` | `✅ Done` |
| ↳ Step 6: Post-migration guideline review | `🔴 Blocked` | `💡 Pending` (unblocked — ready to dispatch) |

The parent row stays `🟡 In Progress` because Step 6 is not yet done.

---

## Verification

No code changes — this is a BACKLOG.md status correction only. Verification: read the updated rows in BACKLOG.md and confirm they match the table above.
