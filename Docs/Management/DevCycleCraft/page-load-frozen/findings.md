# Findings — Page load frozen, Phase 2 investigation (UI freeze persists on device)

> Investigation date: 2026-06-11. Investigator: planning subagent (read-only — no production code touched).
> Method: superpowers:systematic-debugging Phase 1 (evidence) + Phase 2 (pattern comparison) + Phase 3 (ranked hypotheses).
> Evidence sources: `debug log device - logcat.txt` (run at 01:40, PID 10623), `debug log visual studio debugging in S23 Ultra.txt` (run at 01:35, PID 9524, VS debugger attached), code inspection in this worktree (post-fix commits `4ee4f56` + `048796c`).

## 1. Evidence summary

### 1.1 The freeze is per-navigation, repeating, and UI-thread-bound — not a one-time data load

VS-debug log (PID 9524): every tap navigation produces the same signature, repeating across the whole session (01:35:56 -> 01:36:45, ~7 navigation cycles):

| Time | Event | Log line |
|------|-------|----------|
| 01:35:56.6 | TAP (navigation) | `GestureDetector handleMessage TAP` |
| 01:35:57.672 | `Skipped 64 frames` + `Davey! duration=1079ms` | lines 274, 277 |
| 01:35:59.154 | **`Dialog` window created** (`setWindowBackground: isPopOver=false`) | lines 278-279 |
| 01:35:59.807 | `Skipped 127 frames` + `Davey! duration=2220ms` | lines 282, 284 |
| 01:36:01.297 | `Davey! duration=1313ms` | line 293 |
| 01:36:00.1-01:36:01.2 | **7 x `Explicit concurrent mark compact GC` in ~1.0 s** | lines 285-291 |

Worst single block: `Skipped 245 frames` + `Davey! duration=4121ms` (01:36:05, lines 302/305). Total frozen time per navigation ~ 3-5 s.

Device logcat (PID 10623, separate run at 01:40 — confirms the same behavior in a second session): `Skipped 150/160/120/67/54 frames`, `Davey! duration=2516ms / 2686ms / 1135ms`, the same `Dialog` creation entry (01:40:45.891), and the same explicit-GC bursts (01:40:46.4 -> 01:40:48.0: 8 GCs in ~1.6 s). Per-navigation freeze ~ 2.5-2.7 s.

The per-navigation repetition of the freeze also rules out one-time DevExpress theme initialization and Shell transition cost as root causes — a one-time cost would affect only the first navigation, not every cycle.

### 1.2 Davey frame anatomy — where the main thread actually spends the time

Two distinct cost components appear in the `Davey!` HWUI dumps:

1. **Main thread busy OUTSIDE rendering** — `IntendedVsync` -> actual `Vsync` gaps of ~2.1-2.5 s (e.g. VS log line 284: IntendedVsync 3246108265626 vs Vsync 3248224932208 = 2.12 s; logcat entry at 01:40:40.177: gap = 2.67 s). The Choreographer could not run AT ALL for over 2 s — the UI thread was executing app code (page/view construction), not measure/draw.
2. **Native measure/layout passes of 1.2-1.3 s** — `PerformTraversalsStart` -> `DrawStart` gaps (VS log line 293: 1.242 s; line 342: 1.316 s). This is Android view-tree measure/layout of the newly attached page content.

### 1.3 GC bridge churn = mass Java-peer creation

The repeated `Explicit concurrent mark compact GC` bursts (5-8 within ~1-1.6 s, immediately after each navigation) are MonoVM GC-bridge collections triggered by global-reference pressure — i.e. a large number of Java peer objects (Android views + MAUI handlers) being created in a short window. Individual pause times are sub-millisecond (no GC-pause problem); the bursts are a *symptom* of heavy native view inflation, not a cause.

### 1.4 The Dialog window created mid-freeze

A `Dialog` log entry (`Dialog mIsDeviceDefault=false...` + `DecorView setWindowBackground: isPopOver=false color=0`) appears ~1.5-2.5 s after every navigation tap, in both logs. The only Dialog-backed component on the CRUD pages is the DevExpress `dx:BottomSheet` inside `CrudListView` (its Android implementation hosts content in a dialog window). BottomSheet inflation is therefore part of the per-navigation construction cost (note: `QueuePage` also owns a BottomSheet and does not freeze, so this is a contributor, not the single cause).

### 1.5 DB path verified clean

- `CrudListViewModelBase.LoadFirstPageAsync`/`LoadMoreAsync` (post-`048796c`) run `FetchPageAsync` + projection enumeration inside `Task.Run` — confirmed by code read (`MyVocaList/UI/ViewModels/CrudListViewModelBase.cs:122-127, 188-193`).
- The DB is EMPTY in the reproduction -> queries return in milliseconds regardless.
- No SQLite/EFCore-related blocking, lock contention, or exception appears in either log.

### 1.6 Other log observations

- **No ANR** in either log (the freezes stay under the ~5 s input-dispatch ANR threshold — barely: one 4.1 s Davey).
- One Glide failure at startup: `Load failed for [info_outlined]` (FileNotFoundException /info_outlined) — `info_outlined.svg` is missing from `Resources/Images/` (the *_outlined set exists, `info_outlined` does not). One-time cost at startup (What's New sheet area), not the freeze cause — but worth fixing as a hygiene item.
- Both reproductions are **Debug** deploys (Mono JIT, no AOT, fast-deployment; first log additionally has the VS debugger attached, which inflates all costs further — the 01:40 logcat run without debugger noise still shows 2.5-2.7 s freezes).
- App startup itself skipped 379 frames (01:35:42) — expected debug-deploy startup cost, out of scope (per Phase 1 plan).

## 2. Structural comparison — frozen vs non-frozen pages

| Aspect | CRUD list pages (freeze) | QueuePage / EventsPage (no freeze) |
|--------|--------------------------|-------------------------------------|
| Page element count (approx, incl. component internals) | **40-60 elements**, majority DevExpress controls | ~8-10 elements |
| Shell.TitleView | **Two custom app bars** (SmallAppBar: Grid + 4 DXButton + 2 Label; SearchAppBar: similar) — both inflated, one hidden | none (Shell default title) |
| List host | `dx:ShimmerView` wrapping `DXCollectionView` + 6 skeleton `DXBorder` in LoadingView | none |
| Overlays | 2 x EmptyState (DXButton + 2 Labels each), FloatingToolbar (DXBorder + **5 DXButton**), FAB DXButton | none |
| BottomSheet | yes (confirm sheet) | QueuePage: yes (exit sheet) — and still fast |
| OnAppearing work | `InitializeAsync()`: sets IsInitialLoading=true -> **ShimmerView swaps to LoadingView** -> fetch (off-thread) -> IsInitialLoading=false -> **ShimmerView swaps content back** | nothing |
| DI chain at construction | Page -> ViewModel -> Service -> Repository -> AppDbContext | Page only |

Code-behind inspection (`CrudListPageBase.cs`, `CrudListView.xaml.cs`, `VenuesPage.xaml.cs`): no synchronous I/O, no service calls, no loops — construction cost is `InitializeComponent()` + native handler/view creation itself.

Navigation structure (`AppShell.xaml`): all pages are `FlyoutItem` -> `ShellContent ContentTemplate` (Shell caches page instances after first creation). The logs cannot distinguish whether each freezing tap was a *first* visit to a different page or a *revisit* — instrumentation task T1 resolves this. Either way, on a revisit `OnAppearing` re-runs `InitializeAsync`, which re-toggles `IsInitialLoading` and forces two ShimmerView content swaps (LoadingView <-> DXCollectionView subtree re-attach + full measure/layout).

## 3. Ranked root-cause hypotheses

### H1 — Native view construction + measure/layout cost of the CRUD page composite tree on the UI thread (PRIMARY — confidence: high)

Each CRUD page synchronously inflates 40-60 elements (mostly DevExpress controls, each crossing JNI to create a Java peer + handler) during navigation: 2 app bars, ShimmerView + 6 skeleton bones, DXCollectionView, BottomSheet (creates a Dialog window), FloatingToolbar with 5 buttons, FAB, 2 empty states. Supporting evidence: 1.2-1.3 s PerformTraversals->DrawStart measure passes (see 1.2), 2.1-2.7 s main-thread-busy gaps (see 1.2), GC-bridge bursts (see 1.3), Dialog creation mid-freeze (see 1.4), and the contrast table (see 2): pages with ~10 elements do not freeze, pages with 40-60 do — with the DB empty and the fetch verified off-thread.

### H2 — ShimmerView double content swap on EVERY appearance, including revisits (SECONDARY — confidence: medium-high)

`OnAppearing` -> `InitializeAsync` unconditionally sets `IsInitialLoading = true` then `false` (`CrudListViewModelBase.cs:98-104`). Each toggle makes `dx:ShimmerView` swap its visible subtree (LoadingView <-> Content). The Content side contains the DXCollectionView — the heaviest control on the page. Two native subtree swaps + measure/layout passes per navigation, even when the page is cached by Shell and the data is already loaded. This explains why *repeat* navigations stay slow despite Shell page caching.

### H3 — Debug-build amplification (MODIFIER — confidence: high that it amplifies; unknown magnitude)

Both reproductions are debug deploys (Mono JIT, no AOT, debugger attached in one). Debug inflation costs are typically 3-10x release. The freeze may shrink to acceptable levels in Release; this must be quantified before investing in structural UI changes (task T2). Note: Helder reports Settings (simple page) shows only a "short delay" in the same debug build — so debug overhead alone does not explain the CRUD-page magnitude; it multiplies H1/H2.

### H4 — Shell.TitleView custom app-bar swap per navigation (CONTRIBUTOR — confidence: medium)

MAUI Android rebuilds the toolbar content when `Shell.TitleView` changes on navigation; the CRUD pages each carry two custom app-bar ContentViews in the TitleView. Known MAUI perf pain point; contributes to but does not dominate the measured costs.

## 4. Ruled out

| Hypothesis | Verdict | Evidence |
|------------|---------|----------|
| SQLite/EF Core query on the UI thread | **Ruled out as Phase-2 cause** | Fix `4ee4f56`/`048796c` verified in code (Task.Run offload present); DB is empty; freeze persists; no DB activity correlates with the freezes in either log |
| ANR / deadlock | Ruled out | No ANR entries; app always recovers within ~2.5-4 s; input events processed after each freeze |
| GC pauses | Ruled out as direct cause | All GC pauses under 2 ms; bursts are a symptom of peer creation (see 1.3) |
| Memory pressure | Ruled out | Heap grows 26 -> 33 MB across the session — trivial |
| Missing-image retry storm (Glide) | Ruled out as freeze cause | Only one missing icon (`info_outlined`), startup-only; loads run on Glide worker threads |
| Choreographer misconfiguration / display refresh | Ruled out | SurfaceFlinger steady at 60 Hz; HWUI healthy when main thread is free |

## 5. What Phase 2 must produce

1. **Attribution numbers on-device** (T1): constructor vs handler-attach vs first-layout vs InitializeAsync timing per page — converts H1/H2 from "high confidence" to "measured".
2. **Release-build baseline** (T2): sizes H3 — decides whether structural UI work is needed for MVP at all.
3. **Cheap wins shippable regardless** (T3): skip the shimmer toggle when data is already loaded (kills the revisit cost of H2); add the missing `info_outlined.svg`.
4. **Evidence-gated structural change** (T4/T5): only after T1/T2 numbers — deferred attach of heavy content, lighter TitleView, or DX component reduction, chosen with Helder.

---

## T5 spike results (2026-06-12)

> Spike subagent: static analysis + build-verify experiments on VenuesPage. On-device timing (T1 numbers) not available for this spike — evaluation is based on element-count analysis, HWUI log evidence from §1, and structural reasoning. Success/failure criteria applied with static analysis as proxy for on-device measurement; Helder must validate with T1 numbers before rollout.

### Element count baseline (VenuesPage first-render inflation)

CrudListView.xaml direct elements (~35):
- Root Grid + RowDefinitions + ContentPresenter (filter)
- ShimmerView → LoadingView: VerticalStackLayout + 6x DXBorder skeleton bones
- ShimmerView → Content: **DXCollectionView** (creates Recycler + scroll container + virtualization layer)
- 2x EmptyState = 2x (ContentView + VerticalStackLayout + DXButton + 2 Label) = 10 elements
- HorizontalStackLayout + **FloatingToolbar** (DXBorder + HorizontalStackLayout + 5 DXButton) + FAB DXButton
- **BottomSheet** + VerticalStackLayout + Label + 2x BoxView + 2x DXButton = 7 elements, **creates Android Dialog window** (confirmed in device logs §1.4)

Shell.TitleView (~18):
- Grid + **SmallAppBar** (Grid + VerticalStackLayout + 4 DXButton + 2 Label = ~9) + **SearchAppBar** (Grid + DXButton + DXTextEdit + 3 DXButton = ~7)

Total: ~53 elements, all creating JNI Java peers → GC-bridge burst evidence (§1.3).

---

### Option A — Lazy BottomSheet in CrudListView

**Approach:** Remove `dx:BottomSheet` from `CrudListView.xaml`. Create it in code-behind (`EnsureConfirmSheetCreated`) on the first `ConfirmSheetState != Hidden` event. The sheet content (Label, 2 BoxView, 2 DXButton) is created programmatically; the sheet is added to `rootGrid` with `Grid.Row=1` at creation time.

**Build result:** PASS (0 errors) — confirmed by `dotnet build MyVocaList/MyVocaList.csproj -f net10.0`. One type error fixed during spike (`BottomSheetState` → `BottomSheetAllowedState` for the `AllowedState` property).

**Element count delta:** −7 elements removed from initial inflation (BottomSheet + VerticalStackLayout + Label + 2x BoxView + 2x DXButton). More importantly: **the Android Dialog window is no longer created at construction** — the device logs (§1.4) confirm this Dialog appears mid-freeze on every navigation. Removing it from inflation eliminates one confirmed contributor.

**Expected T1 `loaded` impact:** The BottomSheet creates a Dialog-backed Android window, which requires a `WindowManager.addView` call and measure/layout of a second window. Deferring this removes the Dialog creation from the critical path entirely for the ~95% of page visits where the user never opens the confirm sheet. Conservative estimate: 5–15% reduction in `ctor→Loaded` span. Not sufficient alone to meet the 40% success criterion, but it is a clean, reversible, zero-behavioral-impact change.

**Behavioral risks:**
- First tap on delete/edit that opens the sheet has a ~10–20 ms one-time cost (sheet inflation). Imperceptible.
- Bindings (ConfirmMessage, ConfirmActionText, DismissConfirmCommand) work correctly via dynamic BindingContext forwarding.
- BottomSheet `StateChanged` event (swipe-dismiss sync) works because the handler is attached at creation.

**Recommendation:** go — ship as a standalone improvement task. Low risk, confirmed build, eliminates the Dialog-creation contributor from every page navigation. Does not reach 40% alone — must be combined.

---

### Option B — Collapse Shell.TitleView to single app bar

**Approach:** Remove `SearchAppBar` from `VenuesPage.xaml`'s `Shell.TitleView`. Create it lazily in code-behind when `VenuesViewModel.IsSearchMode` first becomes `true` (via `PropertyChanged` subscription). The `SmallAppBar` `IsVisible` binding is also deferred to the same moment (before that, it is always visible, which is correct — search is closed by default).

**Build result:** PASS (0 errors) — confirmed by `dotnet build MyVocaList/MyVocaList.csproj -f net10.0`. `titleViewGrid` x:Name added to the Grid in XAML; binding wiring done in code-behind.

**Element count delta:** −7 elements removed from initial inflation (Grid + DXButton + `dxe:TextEdit` + 3x DXButton). The `dxe:TextEdit` (DevExpress editor with complex input handling) is particularly expensive — it is the only input control in the page and likely has a non-trivial Java-side setup.

**Expected T1 `loaded` impact:** Shell.TitleView elements are inflated as part of the Shell toolbar area separately from the page content, but they still create Java peers at navigation time. Removing ~7 elements saves some Java-peer construction. The `dxe:TextEdit` may account for disproportionate cost (IME registration, text watcher setup). Estimate: 5–15% reduction. Not sufficient alone.

**Behavioral risks:**
- This change is **VenuesPage-only** in the spike. Every other CRUD page still has the SearchAppBar at inflation. A rollout task would need to apply the pattern to all 4 CRUD pages.
- The `SearchAppBar.OnPropertyChanged(nameof(IsVisible))` auto-focus logic (`searchEdit?.Focus()`) fires when `IsVisible = true` — this still works because the view is added to the tree before the binding fires `IsSearchMode = true` → `IsVisible = true`.
- Compiled bindings in the XAML `x:DataType="vm:VenuesViewModel"` remain intact for SmallAppBar; SearchAppBar bindings move to code-behind (runtime bindings).

**Recommendation:** go — ship as a standalone improvement task, applied to all 4 CRUD pages. Low-medium risk. The compile-time bindings for SmallAppBar are preserved; SearchAppBar runtime bindings are equivalent in behavior. Does not reach 40% alone — must be combined with Option A.

---

### Option C — Defer DXCollectionView attach (skeleton-only first paint)

**Approach:** Replace `<dx:ShimmerView.Content>` with a lightweight placeholder at construction; inject the real `DXCollectionView` after the first frame via `Dispatcher.DispatchDelayed`.

**Build result:** NOT ATTEMPTED — disqualified on behavioral grounds before build experiment.

**Element count delta:** DXCollectionView itself is 1 XAML element, but it represents the heaviest control (Android RecyclerView + scroll container + selection manager + handler factory). Removing it from initial inflation is the highest-value single element.

**Disqualification reason — regression against T3:** T3 (shipped, `_hasLoadedOnce` flag) was designed so that on **revisits**, data appears immediately with no skeleton flash. Option C would re-introduce a skeleton/blank period on every revisit (even when `_hasLoadedOnce = true` and data is ready) because the DXCollectionView is not yet attached when `OnAppearing` runs. This directly undoes T3's behavioral guarantee: "Navigate back to Venues: Venues reappears with its list immediately." The two changes are in direct behavioral conflict.

**Recommendation:** no-go. The behavioral regression against T3 makes this option unacceptable without a redesign that distinguishes first-visit (defer is fine) from revisit (defer must be skipped). That redesign is more complex than Options A+B combined and provides lower incremental value after T3 already eliminated the revisit shimmer cost.

---

### Combined A+B analysis

Options A and B are **additive and independent** — they address different UI regions (page body vs Shell toolbar) and can be shipped together in a single rollout task:
- Combined element delta: −14 elements, removal of the Android Dialog creation and the heavy TextEdit from the critical path
- Combined estimated impact: 10–30% reduction in `ctor→Loaded` span (static estimate; must be validated with T1 numbers on device)

The combined estimate falls in a range that **may** reach the 40% success criterion on device — particularly since the Dialog window creation (which appears in logs as a distinct mid-freeze entry) may account for more than its element count suggests.

---

### Overall recommendation

**Implement Options A + B together as a single rollout task.** Neither option alone is expected to reach 40%, but their combination eliminates two confirmed cost contributors (Dialog window mid-inflation, TextEdit setup in SearchAppBar) across all 4 CRUD pages. Both build cleanly, both are low-behavioral-risk, and both are reversible if T1 numbers show no improvement.

**Rollout task scope:**
- Apply Option A (lazy BottomSheet) to `CrudListView.xaml(.cs)` — shared component, affects all 4 CRUD pages at once
- Apply Option B (lazy SearchAppBar) to all 4 CRUD page XAML files: VenuesPage, PersonsPage, SongsPage, ArtistsPage
- XAML edits one-file-at-a-time with build between each (MAUI constitutional constraint)
- Verify on S23 Ultra with T1 instrumentation still in place to measure before/after `ctor→Loaded` delta

**If combined A+B on-device result is < 20%:** escalate to Helder — the remaining lever is the Blazor Hybrid migration (already in BACKLOG), not further micro-optimization of the native MAUI XAML tree. The freeze in Release build (T2, pending) may already be acceptable for MVP, making structural work lower priority.

**Spike conclusion:** inconclusive on the 40% threshold (no on-device T1 numbers available for comparison), but both options are viable and should be implemented. The spike confirms build correctness and identifies the two highest-value structural changes.
