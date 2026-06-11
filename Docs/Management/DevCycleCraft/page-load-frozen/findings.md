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
