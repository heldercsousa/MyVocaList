# Task Log — crud-list-deduplication


## Moved from BACKLOG.md (2026-07-15) — Code Cleanup — CRUD List Page Deduplication

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Code Cleanup — CRUD List Page Deduplication** | ✅ Done | 4 code-behinds + 4 ViewModels share ~57% identical code. Plan: `Docs/Management/DevCycleCraft/crud-list-deduplication/plan.md`. Approach: `ICrudListViewModel` interface + `CrudListPageBase` (abstract ContentPage, events for XAML elements) + `CrudListViewModelBase<TItem>` (abstract generic ViewModel, abstract methods). Est. -890 lines. |


## Moved from BACKLOG.md (2026-07-15) — Step 1: Implement shared infrastructure

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 1: Implement shared infrastructure | ✅ Done | `ICrudListViewModel`, `CrudListPageBase`, `CrudListViewModelBase<TItem>` created. Commit `37613a2` (2026-06-04). |


## Moved from BACKLOG.md (2026-07-15) — Step 2: Migrate VenuesPage + VenuesViewModel

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 2: Migrate VenuesPage + VenuesViewModel | ✅ Done | ~220 lines removed. Commit `f784551` (2026-06-04). |


## Moved from BACKLOG.md (2026-07-15) — Step 3: Migrate PeoplePage + PersonsViewModel

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 3: Migrate PeoplePage + PersonsViewModel | ✅ Done | Commit `eff8e0a` (2026-06-04). |


## Moved from BACKLOG.md (2026-07-15) — Step 4: Migrate SongsPage + SongsViewModel

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 4: Migrate SongsPage + SongsViewModel | ✅ Done | `IsCatalogMode` + catalog commands preserved as entity-specific. Commit `eff8e0a` (2026-06-04). |


## Moved from BACKLOG.md (2026-07-15) — Step 5: Migrate ArtistsPage + ArtistsViewModel

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 5: Migrate ArtistsPage + ArtistsViewModel | ✅ Done | `RoleFilter`, `ViewCatalogCommand`, `GoBackCommand` preserved. Commit `334db86` + review fix `13998c4` (2026-06-04). |


## Moved from BACKLOG.md (2026-07-15) — Step 6: Post-migration guideline review

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 6: Post-migration guideline review | ✅ Done | Unblocked. Review `.claude/library/crud-pages.md` and any other CLAUDE.md / rules files that document CRUD page patterns. Update to reflect the new `CrudListPageBase` + `CrudListViewModelBase<TItem>` canonical pattern so future agents start from the correct baseline. |


## Moved from BACKLOG.md (2026-07-15) — Step 7: CRUD XAML sharing — approach evaluation & design

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Step 7: CRUD XAML sharing — approach evaluation & design | ✅ Done | Research + decide best MAUI pattern for sharing structural XAML across 4 CRUD list pages. Decision already pre-analysed: `CrudListView` ContentView with BindableProperties (Option A). Design: `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/design.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7a: Implement `CrudListView` ContentView + update `CrudListPageBase`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7a: Implement `CrudListView` ContentView + update `CrudListPageBase` | ✅ Done | Blocked on Step 7. Create ContentView with 13 BindableProperties; extend `ICrudListViewModel` with `IsEmptyNoResults`; mark old events `[Obsolete]`. Plan: `crud-list-deduplication/xaml-sharing/plan-7a.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7a.1: Expand ICrudListViewModel for compiled bindings

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7a.1: Expand ICrudListViewModel for compiled bindings | ✅ Done (reverted) | Review finding: x:DataType on root ContentView causes MAUI compiled bindings to incorrectly cast CrudListView to ICrudListViewModel for all Source={x:Reference self} bindings, silently nulling FabIcon, ItemsSource, IsEmptyNoItems etc. Fix: remove x:DataType from root (reflection correctly resolves Source type). Compiled bindings for ICrudListViewModel deferred indefinitely — not needed. |


## Moved from BACKLOG.md (2026-07-15) — Step 7-guidelines: Update crud-pages.md with CrudListView canonical pattern

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7-guidelines: Update crud-pages.md with CrudListView canonical pattern | ✅ Done | Update `.claude/library/crud-pages.md` to document CrudListView as the canonical CRUD page shell. Plan: `crud-list-deduplication/xaml-sharing/plan-guidelines.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7b: Migrate VenuesPage.xaml to CrudListView

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7b: Migrate VenuesPage.xaml to CrudListView | ✅ Done | Smoke tested by Helder 2026-06-06 — list, FAB, empty state, toolbar all working. Plan: `crud-list-deduplication/xaml-sharing/plan-7b.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7c: Migrate PeoplePage.xaml to CrudListView

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7c: Migrate PeoplePage.xaml to CrudListView | ✅ Done | Blocked on Step 7b green. Notable: `ListItemLeadingMonogram` + `ParticipationsAbsencesNumber` in template. Plan: `crud-list-deduplication/xaml-sharing/plan-7c.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7d: Migrate SongsPage.xaml to CrudListView

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7d: Migrate SongsPage.xaml to CrudListView | ✅ Done | Blocked on Step 7c green. Notable: AppBarSubtitle stays in Shell.TitleView (not in CrudListView); Tap no-op omitted. Plan: `crud-list-deduplication/xaml-sharing/plan-7d.md` |


## Moved from BACKLOG.md (2026-07-15) — Step 7e: Migrate ArtistsPage.xaml to CrudListView

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳↳ Step 7e: Migrate ArtistsPage.xaml to CrudListView | ✅ Done | Blocked on Step 7d green. Most complex: FilterContent slot, ViewCatalog trailing button, 2-row Grid collapse, [Obsolete] event removal. Plan: `crud-list-deduplication/xaml-sharing/plan-7e.md` |
