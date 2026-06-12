# Plan — Fix "Page load frozen" (BACKLOG 2026-06)

> Approved by Helder 2026-06-10. Bug fix — no spec required per `workflow.md` decision table; this plan + the fix commit message are the artifacts.
> Branch: `fix/page-load-frozen` (worktree `.worktrees/page-load-frozen`, base `develop`).

## Context

A debug deploy on a Galaxy S23 Ultra (Android 16) shows every implemented page freezing during load — worst on pages with data, but lagging even when empty. The shimmer loading containers exist and are correctly wired, yet they never animate.

**Root cause (confirmed by code trace):** `Microsoft.Data.Sqlite` has **no real async I/O** — its async methods execute *synchronously on the calling thread* (documented provider limitation). The CRUD list load path runs entirely on the UI thread:

1. `CrudListPageBase.OnAppearing()` (`MyVocaList/UI/Pages/Base/CrudListPageBase.cs:24`) fires `InitializeAsync()` on the UI thread.
2. `CrudListViewModelBase.InitializeAsync()` (`MyVocaList/UI/ViewModels/CrudListViewModelBase.cs:94-100`) sets `IsInitialLoading = true`, then `await Task.Yield()` — which resumes **back on the UI thread** (dispatcher SynchronizationContext).
3. `LoadFirstPageAsync` → `FetchPageAsync` → Service → Repository → EF Core → SQLite executes the COUNT query + page query + per-row correlated count subqueries (`VenueRepository.GetPagedWithEventInfoAsync`, `ArtistRepository.GetPagedAsync`) **synchronously on the UI thread**.
4. The `ShimmerView` in `CrudListView.xaml` (bound to `IsInitialLoading`) never gets a frame — the UI thread is busy running SQLite. User perceives a frozen page.

**Proof by contrast:** the search debounce path (`CrudListViewModelBase.cs:199`) already wraps `LoadFirstPageAsync` in `Task.Run` — and search does not freeze.

**Secondary finding (handled here as containment):** `AppDbContext`, repositories, and services are registered `Scoped` in `MauiProgram.cs`, but MAUI has no per-page scope — they are effectively singletons sharing **one** `AppDbContext`. Concurrent use from two ViewModels (already possible today via the threadpool search path; window widens once loads move off the UI thread) risks `InvalidOperationException: A second operation was started on this context`. Full fix (context-per-operation via `IDbContextFactory`) is an architectural change → deferred to BACKLOG; this plan ships a contained app-wide load gate instead.

**Intended outcome:** every page renders immediately on navigation; the existing shimmer skeleton animates while data loads in the background; UI stays responsive (scroll, back gesture) throughout.

## SQLite-Temporary Addendum (Helder, 2026-06-10)

SQLite is a **temporary provider** — a minimal free MSSQL variant is planned, delivered as a new Infra-layer project (working name `INFRA_MSSQL`) beside the SQLite-dedicated `Infra` project. Consequence for this fix:

- The `Task.Run` offload and the static DB load gate in `CrudListViewModelBase` are workarounds for SQLite's synchronous execution + the shared singleton `AppDbContext`, and they live **outside the Infra project** (UI layer).
- **Revert obligation:** when SQLite goes out of usage, the static load gate must be removed, and the `Task.Run` offload re-evaluated (unnecessary with a truly async provider, though harmless). Tracked in `.claude/rules/constraints-registry.md § EF Core / SQLite` and in the BACKLOG "DbContext-per-operation architecture review" follow-up.
- The fix code must carry a comment marking it as a SQLite-driven workaround so it is findable when INFRA_MSSQL lands.

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

1. `InitializeAsync_WithSynchronizationContext_ExecutesFetchOffContext` — install a custom `SynchronizationContext` (simulating the UI thread), record `SynchronizationContext.Current` inside the double's `FetchPageAsync`, assert it is **not** the installed context. This is the regression test that proves the freeze mechanism is gone. (Pitfall: the fake context must actually pump posted continuations or `Task.Yield` deadlocks; restore the original context in `finally`.)
2. `LoadMoreCommand_WithSynchronizationContext_ExecutesFetchOffContext` — same for `FetchMoreAsync`.
3. Existing suites (`PersonsViewModelTests`, etc.) exercise `InitializeAsync` through the base class — must stay green (they have no sync context, so behavior is unchanged for them).

Regression tests for a bug fix carry a `// Regression: page-load-frozen` comment instead of an `[AC]` tag (no requirements.md exists for this fix).

## BACKLOG.md updates (main agent, same session)

1. Set **Page load frozen** row → `🟡 In Progress` (done 2026-06-10), then `✅ Done` on ship.
2. Follow-up row added (Dev Cycle Craft): **DbContext-per-operation architecture review** — `IDbContextFactory<AppDbContext>`; removes the need for the static load gate; covers form saves too. Architecture review + spec required. Must be designed against the INFRA_MSSQL direction (see Addendum).
3. Follow-up row added: **Paged query optimization — Venue/Artist count subqueries** — evidence-driven; only if the S23 smoke test still shows long (animated) shimmer after this fix.

## Out of scope (explicitly)

- Repository query rewrites (BACKLOG follow-up above).
- DI lifetime changes / `IDbContextFactory` refactor (BACKLOG follow-up above).
- Debug-build JIT/XAML-inflation cost on first navigation to each page type — inherent to debug deploys; release/AOT builds are faster. The fix removes the *freeze*; it does not make debug builds as fast as release.

## Execution notes (workflow.md)

- **Branch isolation (required — another terminal works in the main checkout in parallel):** all changes on branch `fix/page-load-frozen` in worktree `.worktrees/page-load-frozen`. No branch switching in the main checkout. Merge back to `develop` only after verification, coordinated with Helder.
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

---

# Phase 2 — UI freeze persists on device (2026-06-11)

> **Status:** Phase 1 (DB offload + load gate) shipped and verified in code, but the S23 Ultra still freezes 2.5–4 s per navigation to any CRUD list page — **with an empty database**. Device-log analysis (see `findings.md` in this folder) attributes the freeze to UI-thread page/view construction and measure/layout, NOT to data access.
>
> **Evidence digest (full detail in `findings.md`):** per navigation tap the logs show `Choreographer Skipped 53–245 frames`, HWUI `Davey!` frames of 1.0–4.1 s in which (a) the main thread is busy outside rendering for 2.1–2.7 s (IntendedVsync→Vsync gap) and (b) native measure/layout alone takes 1.2–1.3 s (PerformTraversalsStart→DrawStart); bursts of 5–8 MonoVM GC-bridge collections (mass Java-peer creation = native view inflation); and an Android `Dialog` window created mid-freeze (the `dx:BottomSheet` in `CrudListView`). Hypotheses ranked: **H1** composite-tree inflation cost (40–60 elements/page incl. 2 TitleView app bars, ShimmerView, DXCollectionView, BottomSheet, FloatingToolbar, FAB, 2 EmptyStates) — high confidence; **H2** ShimmerView double content swap on every `OnAppearing` (incl. revisits to Shell-cached pages) — medium-high; **H3** debug-build amplification (JIT, no AOT, debugger) — magnitude unknown, must be measured; **H4** Shell.TitleView swap cost — contributor.
>
> **Rules honored by all tasks below:** DevExpress-first UI; no `DisplayAlert`/native dialogs; business logic stays in Services; English only; the `SQLITE-WORKAROUND` revert markers from Phase 1 must remain untouched; XAML edits are incremental (ONE file → build → fix → next file).

## Ordered tasks

- [x] **T1 — Instrument CRUD page lifecycle timings (Serilog)** [SEQUENTIAL]
  - **Context:** We must attribute the 2.5–4 s freeze between (a) page construction (`InitializeComponent` + DI resolution), (b) native handler attach/first layout, and (c) `InitializeAsync` shimmer toggling. Logs to read on-device via existing Serilog sinks (logcat/file).
  - **Approach (decided — single):** base-class timestamp + lifecycle-event spans. A timestamp recorded in the `CrudListPageBase` constructor fires BEFORE the derived page's `InitializeComponent()` (base ctor runs first), so it cannot measure `InitializeComponent` in isolation — but the spans base-ctor → `OnAppearing` and base-ctor → `Loaded` INCLUDE the derived `InitializeComponent` + DI resolution cost, which is exactly the attribution needed, with zero per-page edits.
  - **What to do — exact insertion points:**
    1. `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — add a protected constructor (the class currently has none): record `_ctorTimestamp = Stopwatch.GetTimestamp()` in a private field and subscribe `Loaded += handler` logging elapsed-since-ctor. In `OnAppearing()` (currently line 21): log elapsed-since-ctor at method start and again immediately after `_ = ListViewModel.InitializeAsync();`. Override `OnNavigatedTo(NavigatedToEventArgs)` to log elapsed-since-ctor. Tag every line with the page subclass name via `GetType().Name`.
    2. `MyVocaList/UI/Components/CrudListView.xaml.cs` — in the `CrudListView()` constructor (line 187): wrap the existing `InitializeComponent()` call in a `Stopwatch` and log its duration (this captures the shared component's inflation directly); subscribe `Loaded` to log time-from-ctor.
    3. `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` — in `InitializeAsync` (line 98): log ms for the full method; in `LoadFirstPageAsync`: log ms for the `Task.Run` fetch block alone (lines 122–127).
    Use `Serilog.Log.ForContext` or injected `ILogger` (already available in VMs; for pages use `Serilog.Log.Logger` directly — UI-layer diagnostics, no business logic). Mark every added block with `// PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.`
  - **Produces:** timing log lines `[PageLoad] {Page} ctor={ms} appearing={ms} loaded={ms} initAsync={ms} fetch={ms}` on every navigation.
  - **Consumes:** `CrudListPageBase.cs`, `CrudListView.xaml.cs`, `CrudListViewModelBase.cs` (committed state on this branch).
  - **Files owned:** `MyVocaList/UI/Pages/Base/CrudListPageBase.cs`, `MyVocaList/UI/Components/CrudListView.xaml.cs`, `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`.
  - **Demo:** Deploy debug build, navigate Venues → People → Venues; logcat shows one `[PageLoad]` line per navigation with all five timings; first-visit vs revisit numbers visibly differ.
  - **Risk:** Low — additive logging only; no behavior change. Existing 271 tests must stay green (instrumentation must not break the no-SynchronizationContext regression tests — keep all logging outside `RunOnUiThread` blocks).

- [x] **T2 — Release-configuration baseline run (no production code change)** [SEQUENTIAL — after T1 so the same instrumentation logs are present] *(build done 2026-06-12 — APK at `MyVocaList/bin/Release/net10.0-android/com.myvocalist-Signed.apk`; on-device timing numbers pending Helder S23 Ultra run)*
  - **Context:** Both freeze reproductions were Debug deploys (Mono JIT, no AOT; one with debugger attached). If Release timing is acceptable, the structural UI work (T5) drops in priority; if not, H1 is confirmed as a real product defect. Phase 1 plan already flagged debug-build cost as out of scope — this task quantifies it instead of guessing.
  - **What to do:** Build the Android app in Release (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release`) and produce a signed-or-debuggable APK artifact for Helder to sideload on the S23 Ultra (document the exact `adb install` step in the task-log). Do NOT change csproj settings other than what Release already defines. Collect the `[PageLoad]` Serilog timings from the Release run (Serilog file sink or logcat) and append a Debug-vs-Release comparison table to `findings.md`.
  - **Produces:** Release APK + comparison table in `findings.md` (Debug ctor/appearing/loaded ms vs Release).
  - **Consumes:** T1 instrumentation (committed).
  - **Files owned:** `Docs/Management/DevCycleCraft/page-load-frozen/findings.md` (append only), `Docs/Management/DevCycleCraft/page-load-frozen/task-log.md`.
  - **Demo:** Helder installs the Release APK, navigates the 4 CRUD pages, and reports whether the freeze is gone/acceptable; findings.md contains the numbers.
  - **Risk:** Low — no code change. Note: Release build may take >10 min on first run (AOT/linker).

- [x] **T3 — Skip the shimmer initial-load cycle on revisits** [SEQUENTIAL — after T1 (so before/after timings exist); independent of T2 results]
  - **Context:** `CrudListViewModelBase.InitializeAsync` (lines 98–104) unconditionally sets `IsInitialLoading = true` → `false` on every `OnAppearing`. Each toggle makes the `dx:ShimmerView` in `CrudListView.xaml` swap its native subtree (LoadingView with 6 skeleton bones ↔ Content with DXCollectionView), forcing re-attach + full measure/layout twice per navigation — even when the page instance is cached by Shell (all CRUD pages are `ShellContent` in `AppShell.xaml`) and data is already loaded. This is hypothesis H2 in `findings.md` and is a correct behavior fix regardless of other outcomes (a revisit should show existing data instantly, then refresh quietly).
  - **What to do (TDD — testing.md Level A, regression comment `// Regression: page-load-frozen phase 2`):** Add a private `bool _hasLoadedOnce` to `CrudListViewModelBase`. In `InitializeAsync`: if `_hasLoadedOnce` is false → current behavior (shimmer on, load, shimmer off). If true → do NOT touch `IsInitialLoading`; still call `LoadFirstPageAsync` (silent refresh keeps the list current after add/edit flows return to the page). **"First successful load" — precise definition:** `LoadFirstPageAsync` returns no success indicator (plain `Task`, no result value) and swallows only `OperationCanceledException`, so `InitializeAsync` cannot detect success from the returned task. Therefore set `_hasLoadedOnce = true` INSIDE `LoadFirstPageAsync`, immediately after the `RunOnUiThread` block that applies `Items.ReplaceRange` (currently lines 134–144) — that point is reached only when the fetch completed without cancellation and the list was applied. The cancellation paths (the `cancellationToken.IsCancellationRequested` early return and the `OperationCanceledException` catch) must NOT set the flag. Write the failing test first: `InitializeAsync_SecondCall_DoesNotToggleIsInitialLoading` (assert no `PropertyChanged` for `IsInitialLoading` on second call) in the existing `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` test-double infrastructure. Confirm the 2 existing SynchronizationContext regression tests still pass. Do NOT remove or alter the `SQLITE-WORKAROUND` comments/code.
  - **Produces:** revisit navigations with zero ShimmerView subtree swaps; 1 new regression test.
  - **Consumes:** `CrudListViewModelBase.cs` (with T1 instrumentation in place).
  - **Files owned:** `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs`.
  - **Demo:** Navigate Venues → People → back to Venues: Venues reappears with its list immediately (no skeleton flash); T1 logs show revisit `initAsync` ms drop versus the pre-T3 baseline.
  - **Risk:** Medium — changes the visible loading behavior on revisits (intended); pull-to-refresh and search paths must be re-verified (they call `LoadFirstPageAsync` directly and never touched `IsInitialLoading`, so they are unaffected by design). Residual: on revisits the silent refresh still fires a `ReplaceRange` Reset, so the scroll-position/selection/pagination reset that was previously hidden behind the shimmer is now visible to the user without shimmer cover.

- [x] **T4 — Add the missing `info_outlined` icon asset** [P — parallel with T2/T3]
  - **Context:** Both device logs show a Glide `FileNotFoundException: /info_outlined` at startup. `Resources/Images/` contains the full `*_outlined.svg` icon set but `info_outlined.svg` is absent; some XAML (What's New / About area) references it. Not the freeze cause — hygiene fix that removes a misleading error from future log captures.
  - **What to do:** Grep the `MyVocaList/` project for `info_outlined` to confirm the consumer(s). Add `info_outlined.svg` to `MyVocaList/Resources/Images/` following the exact style/viewBox conventions of the sibling Material icons (copy the source convention of e.g. `check_circle_outlined.svg` — Material Symbols "info" outlined glyph). `MauiImage Include="Resources\Images\*"` already globs the folder — no csproj change.
  - **Produces:** `MyVocaList/Resources/Images/info_outlined.svg`; no Glide error on startup.
  - **Consumes:** nothing.
  - **Files owned:** `MyVocaList/Resources/Images/info_outlined.svg`.
  - **Demo:** Deploy, open the page/sheet that uses the icon — icon renders; logcat shows no `Load failed for [info_outlined]`.
  - **Risk:** Low.

- [x] **T5 — [DECISION GATE + SPIKE] Structural reduction of CRUD page first-render cost** [SEQUENTIAL — only after T1+T2 numbers reviewed with Helder]
  - **Spike completed 2026-06-12.** Findings in `findings.md § T5 spike results`. On-device T1 numbers not available (T2 pending with Helder) — static analysis + build-verify experiments. Recommendation: Options A (lazy BottomSheet) + B (lazy SearchAppBar) combined; Option C disqualified (regression against T3). Rollout is a separate task requiring Helder review of T1/T2 numbers first.
  - **Decision gate:** No preset numeric threshold; Helder reviews T1/T2 numbers and decides go/no-go. Spike-internal success criteria (≥40% reduction / <20% abandon) unchanged.
  - **Time-box:** 2 hours (hard stop) for the spike portion.
  - **Question:** Which single structural change yields the largest reduction in `ctor`+`loaded` time on-device: (a) deferring the `dx:BottomSheet` in `CrudListView` to lazy creation (create/attach on first `ConfirmSheetState` change instead of at inflation — it currently creates an Android Dialog window during every page construction); (b) collapsing `Shell.TitleView` to a single app bar inflated on demand (SearchAppBar created only when search opens); or (c) deferring `DXCollectionView` attach until after the first frame (skeleton-only first paint)?
  - **Success criterion:** one option shows ≥40% reduction of the T1 `loaded` timing on the S23 Ultra (or emulator as proxy) for VenuesPage.
  - **Failure criterion:** no option reaches 20% — escalate to Helder; the remaining lever is the Blazor Hybrid migration track already in BACKLOG, not micro-optimization.
  - **Constraints:** spike on VenuesPage ONLY, throwaway branch commits clearly marked `spike:`; DevExpress-first stays in force (no stock-MAUI replacements of DX components); production rollout of the winning option is a SEPARATE task authored after Helder reviews the spike numbers; XAML edits one-file-at-a-time with a build between each.
  - **Produces:** `findings.md` § "T5 spike results" with per-option timings + recommendation.
  - **Consumes:** T1 timings, T2 Debug-vs-Release table, Helder's go decision.
  - **Files owned (spike, throwaway):** `MyVocaList/UI/Components/CrudListView.xaml(.cs)`, `MyVocaList/UI/Pages/Venues/VenuesPage.xaml(.cs)` — restored or properly re-implemented after the spike.
  - **Demo:** N/A (spike produces findings, not shipped behavior).
  - **Risk:** Medium — touches the shared `CrudListView`; mitigated by spike isolation and the separate-rollout rule.

## Phase 2 exit criteria

1. T1 timings captured for all 4 CRUD pages (first visit + revisit) and recorded in `findings.md`.
2. Debug-vs-Release comparison recorded; Helder decides whether Release-build performance is acceptable for MVP.
3. T3 shipped: revisits show no skeleton flash and measurably lower `initAsync`/swap cost.
4. If structural work is needed: T5 spike completed and the winning option scheduled as its own task with Helder's approval.
5. All instrumentation marked `PHASE2-INSTRUMENTATION` is either removed or explicitly kept by Helder's decision before the branch merges to `develop`.
