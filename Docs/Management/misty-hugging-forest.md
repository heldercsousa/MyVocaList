# Plan — Fix "Page load frozen" (BACKLOG 2026-06, `💡 Pending`)

## Context

A debug deploy on a Galaxy S23 Ultra (Android 16) shows every implemented page freezing during load — worst on pages with data, but lagging even when empty. The shimmer loading containers exist and are correctly wired, yet they never animate.

**Root cause (confirmed by code trace):** `Microsoft.Data.Sqlite` has **no real async I/O** — its async methods execute *synchronously on the calling thread* (documented provider limitation). The CRUD list load path runs entirely on the UI thread:

1. `CrudListPageBase.OnAppearing()` (`MyVocaList/UI/Pages/Base/CrudListPageBase.cs:24`) fires `InitializeAsync()` on the UI thread.
2. `CrudListViewModelBase.InitializeAsync()` (`MyVocaList/UI/ViewModels/CrudListViewModelBase.cs:94-100`) sets `IsInitialLoading = true`, then `await Task.Yield()` — which resumes **back on the UI thread** (dispatcher SynchronizationContext).
3. `LoadFirstPageAsync` → `FetchPageAsync` → Service → Repository → EF Core → SQLite executes the COUNT query + page query + per-row correlated count subqueries (`VenueRepository.GetPagedWithEventInfoAsync`, `ArtistRepository.GetPagedAsync`) **synchronously on the UI thread**.
4. The `ShimmerView` in `CrudListView.xaml` (bound to `IsInitialLoading`) never gets a frame — the UI thread is busy running SQLite. User perceives a frozen page.

**Proof by contrast:** the search debounce path (`CrudListViewModelBase.cs:199`) already wraps `LoadFirstPageAsync` in `Task.Run` — and searching does not freeze.

**Secondary finding (handled here as containment):** `AppDbContext`, repositories, and services are registered `Scoped` in `MauiProgram.cs`, but MAUI has no per-page scope — they are effectively singletons sharing **one** `AppDbContext`. Concurrent use from two ViewModels (already possible today via the threadpool search path; window widens once loads move off the UI thread) risks `InvalidOperationException: A second operation was started on this context`. Full fix (context-per-operation via `IDbContextFactory`) is an architectural change → deferred to BACKLOG; this plan ships a contained app-wide load gate instead.

**Intended outcome:** every page renders immediately on navigation; the existing shimmer skeleton animates while data loads in the background; UI stays responsive (scroll, back gesture) throughout.

> Per `workflow.md` spec decision table this is a **bug fix — no spec required**; the fix commit message is the spec (Bug Fix Pattern format).

---

## Phase 1 — Core fix: offload all DB fetches in `CrudListViewModelBase`

**File:** `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (single choke point — fixes all 4 CRUD pages at once: Venues, People, Songs, Artists)

1. **`LoadFirstPageAsync` (line 102):** execute fetch + materialization on the thread pool:
   ```csharp
   var (list, totalCount) = await Task.Run(async () =>
   {
       var (itemsEnumerable, total) = await FetchPageAsync(
           _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);
       return (itemsEnumerable.ToList(), total);   // materialize lazy DTO mapping off the UI thread too
   }, cancellationToken);
   ```
   Note: `.ToList()` must move inside `Task.Run` — services return lazy `Select()` projections (e.g. `VenueService.GetPagedVenuesForListAsync`), so enumeration is part of the query cost. UI mutations stay in the existing `RunOnUiThread` block (unchanged).

2. **`LoadMoreAsync` (line 152):** wrap the `FetchMoreAsync` call + `.ToList()` in `Task.Run` the same way.

3. **Delete path:** in `ExecuteConfirmActionAsync` (line 243), the confirmed action (service delete + reload) also runs SQLite synchronously on the UI thread (delete + `TransactionLogInterceptor` JSON serialization). Change to `await Task.Run(action);`.
   **Pre-check (part of this task):** read each concrete `ExecuteDeleteAsync` override (VenuesViewModel, PersonsViewModel, SongsViewModel, ArtistsViewModel) and confirm every UI mutation inside is already marshalled via `RunOnUiThread` / snackbar service. If any isn't, marshal it rather than skipping the offload.

4. **Keep:** `await Task.Yield()` in `InitializeAsync` (still useful — lets the shimmer get its first frame before the gate wait), `IsInitialLoading` flow, and all `RunOnUiThread` blocks exactly as they are.

## Phase 2 — DbContext concurrency containment (app-wide load gate)

**Same file**, no Infra/DI changes:

1. Change `private readonly SemaphoreSlim _loadSemaphore = new(1, 1);` (line 12) to a **`private static readonly`** gate shared by all CRUD list ViewModels — at most one DB load runs at a time on the shared `AppDbContext`.
2. Route `LoadMoreAsync` through the same gate (today it relies only on the per-instance `_isLoading` flag, so a load-more can overlap another VM's first-page load).
3. **Deadlock guard:** do NOT hold the gate around the delete action — concrete deletes end with `ReloadAsync()` → `LoadFirstPageAsync`, which acquires the gate internally. Gate acquisition stays inside `LoadFirstPageAsync`/`LoadMoreAsync` only.

## Phase 3 — Bounded audit of other UI-thread DB calls

- `AppShellViewModel.InitializeAsync` is fired-and-forgotten from `AppShell.xaml.cs` on the UI thread at startup — check whether it reaches a repository; if so, apply the same `Task.Run` offload.
- Form pages (`VenueFormPage`, `SongFormPage`, etc.) do **not** load entities from the DB on navigation (data arrives via navigation params) — verified, no change needed. Form *saves* still execute SQLite synchronously on the UI thread; acceptable for now (sub-100 ms single-row writes), noted in the BACKLOG follow-up.

## Tests (TDD — `testing.md`, Level A: ViewModel state/threading behavior)

New `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` with a minimal test double subclassing `CrudListViewModelBase<T>`. Red first, one at a time:

1. `InitializeAsync_WithSynchronizationContext_ExecutesFetchOffContext` — install a custom `SynchronizationContext` (simulating the UI thread), record `SynchronizationContext.Current` inside the double's `FetchPageAsync`, assert it is **not** the installed context. This is the regression test that proves the freeze mechanism is gone.
2. `LoadMoreCommand_WithSynchronizationContext_ExecutesFetchOffContext` — same for `FetchMoreAsync`.
3. Existing suites (`PersonsViewModelTests`, etc.) exercise `InitializeAsync` through the base class — must stay green (they have no sync context, so behavior is unchanged for them).

## BACKLOG.md updates (main agent, same session)

1. Set **Page load frozen** row → `🟡 In Progress`, then `✅ Done` on ship.
2. Add follow-up row (Dev Cycle Craft): **DbContext-per-operation architecture review** — replace shared scoped-as-singleton `AppDbContext` with `IDbContextFactory<AppDbContext>` (context per repository operation); removes the need for the static load gate; covers form saves too. ~10 Infra files + `MauiProgram.cs`; requires architecture review (spec required).
3. Add follow-up row: **Paged query optimization — Venue/Artist count subqueries** — `VenueRepository.GetPagedWithEventInfoAsync` (line ~169) and `ArtistRepository.GetPagedAsync` (line ~46) project per-row `Events.Count()` / `CatalogEntries.Count()` correlated subqueries; rewrite (grouped count query or indexed FK check) to shorten shimmer time on data-heavy pages.

## Out of scope (explicitly)

- Repository query rewrites (BACKLOG follow-up above).
- DI lifetime changes / `IDbContextFactory` refactor (BACKLOG follow-up above).
- Debug-build JIT/XAML-inflation cost on first navigation to each page type — inherent to debug deploys; release/AOT builds are faster. The fix removes the *freeze*; it does not make debug builds as fast as release.

## Execution notes (workflow.md)

- **Branch isolation (required — another terminal is working in this checkout in parallel):** all changes happen on a new branch `fix/page-load-frozen` created from `develop`, checked out in a **git worktree** (native `EnterWorktree` tool, falling back to `git worktree add .worktrees/page-load-frozen develop` — `.worktrees/` is gitignored). A worktree, not a branch switch, because switching branches in this directory would disturb the other terminal's session. Merge back to `develop` only after verification, coordinated with Helder.
- Implementation via subagent (Rule 2); task fits sizing limits (2 files: `CrudListViewModelBase.cs` + new test file).
- Commit with Bug Fix Pattern message:
  ```
  fix: CrudListViewModelBase — page loads freeze UI thread

  Root cause: Microsoft.Data.Sqlite executes async methods synchronously, so EF Core paged queries ran on the UI thread after Task.Yield resumed on the dispatcher.
  Fix: offload FetchPageAsync/FetchMoreAsync/delete actions to the thread pool via Task.Run; app-wide static load gate prevents concurrent use of the shared AppDbContext.
  Regression risk: Low — UI mutations already marshalled via RunOnUiThread; search path already used Task.Run.
  ```

## Verification

1. `dotnet build` — 0 errors; `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` — all green including the 2 new threading tests.
2. Emulator: deploy debug build, navigate Venues → Artists → Songs → People. Expected: each page appears instantly with the shimmer skeleton animating (visible motion = UI thread free), then data replaces it. While the shimmer shows, the back gesture and drawer must respond.
3. Rapid navigation stress: open a data-heavy page and immediately navigate to another list page repeatedly — no `InvalidOperationException` (second operation on context), confirming the static gate works.
4. Helder smoke test on the S23 Ultra (same scenario). If data-heavy pages still show a *long* (but now animated, non-frozen) shimmer, that's the deferred query-optimization follow-up — request the device debug log at that point to quantify query time.
