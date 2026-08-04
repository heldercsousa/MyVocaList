# Cross-Cutting Log

> Shared home for displaced BACKLOG.md narratives belonging to items with **no dedicated feature folder** (one-off DevCycleCraft activities, cross-cutting bugs, manual-action reminders). One dated heading per item. Do not create a folder per item — append here.


## Moved from BACKLOG.md (2026-07-15) — Form presentation — bottom-sheet/modal conversion for simple forms

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-10 | ↳ **Form presentation — bottom-sheet/modal conversion for simple forms** | 💡 Pending | Registered by Helder 2026-07-10. ArtistFormPage has a single entry — candidate to switch to a BottomSheet-like UI/UX instead of a full page; possible conflict with the autocomplete UI/UX (dropdown inside a sheet) — **Claude feedback requested** during spec. Same desire for other simple CRUD forms: e.g. VenueFormPage is really simple (single name entry, no autocomplete) and a sheet/modal would avoid navigating away and back just to add/update a venue. Interacts with the Dev Cycle Craft row *CRUD Form Action Pattern — MD3 Save/Cancel placement* (forms that stay full-screen get the AppBar-save pattern; sheets/modals keep in-sheet actions). |


## Moved from BACKLOG.md (2026-07-15) — Form & Autocomplete UX Overhaul

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | **Form & Autocomplete UX Overhaul** | 💡 Pending | Registered by Helder 2026-07-11. Umbrella sequencing every form-presentation + AppBar-save + adaptive-autocomplete change across the app. **Autocomplete foundation for any autocomplete-bearing form (order INVERTED 2026-07-11 — see `DevCycleCraft/autocomplete-component/`):** ② *AutocompleteField Component Evaluation* runs FIRST → new-component build → first application (proven concept) → THEN ① *Autocomplete Mobile UX Pattern guideline*. Downstream "depends on ① & ②" notes still hold (both foundations must be done), only their internal order changed. **Form-presentation ordering (each waits for the prior):** Venue sheet → Artist sheet → Singer sheet(?) → Song form (stays full-screen). CRUD-list AppBar/search enhancement and hamburger-on-all-pages are parallel-capable. Cross-ref: ↳ *Form presentation — bottom-sheet/modal conversion* (above), Dev Cycle Craft *AppBar / SearchAppBar Interaction Redesign* + *CRUD Form Action Pattern*, and the parked *Artist & Song Form UX Redesign* (under Artists & Songs Catalog). |


## Moved from BACKLOG.md (2026-07-15) — Venue form → bottom-sheet conversion

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ Venue form → bottom-sheet conversion | 💡 Pending | **FIRST form to convert; predecessor of ALL other form-presentation tasks below.** Single name entry, **no autocomplete** → cleanest pilot for the auto-focusing modal bottom sheet (MD3): sheet opens → input auto-focused → keyboard raised → in-sheet Save/Cancel. **No dependency on the autocomplete foundations.** Proves the sheet pattern before any autocomplete-bearing form adopts it. Cross-ref DevCycleCraft *CRUD Form Action Pattern* (sheets keep in-sheet actions). |


## Moved from BACKLOG.md (2026-07-15) — Artist form → bottom-sheet conversion

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ Artist form → bottom-sheet conversion | 💡 Pending | Same conversion as Venue. **Depends on: Venue form success + Autocomplete foundations ① & ②** (Artist name entry is autocomplete). The dropdown-in-a-sheet conflict is resolved by ① — on phones autocomplete is a full-screen view, not a dropdown inside the sheet. Adapts the parked *Artist & Song Form UX Redesign* autocomplete logic. |


## Moved from BACKLOG.md (2026-07-15) — Singer (Person) form → bottom-sheet conversion (candidate)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ Singer (Person) form → bottom-sheet conversion (candidate) | 💡 Pending | Maybe — evaluate whether the Person form benefits from a sheet. **Depends on: Venue form success.** Person form HAS autocomplete → also depends on ① & ② if converted. |


## Moved from BACKLOG.md (2026-07-15) — CRUD lists → AppBar + SearchBar logic enhancement

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ CRUD lists → AppBar + SearchBar logic enhancement | 💡 Pending | **Parallel-capable** — can be the very last task or run concurrently with the form work. Implements DevCycleCraft *AppBar / SearchAppBar Interaction Redesign* across all CRUD list pages (SmallAppBar/SearchAppBar governed → four gates). |


## Moved from BACKLOG.md (2026-07-15) — BUG-026: HWUI native crash (SIGABRT) — `pthread_mutex_lock` on destroyed mutex in `hwuiTask0` (Major…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | ↳ BUG-026: HWUI native crash (SIGABRT) — `pthread_mutex_lock` on destroyed mutex in `hwuiTask0` (Major) | 💡 Pending | Native Android render-thread crash captured during the "frozen UI in emulator" investigation, distinct from the ANR (resolved separately as a Debug+emulator artifact). Fired at the moment the debug process was force-stopped by VS/vsdbg — may be debugger-teardown noise rather than a live defect; not yet confirmed reproducible during normal (non-debugger-forced) app close on Release/device. Investigation plan (Release logcat + normal-close emulator logcat) before any fix attempt. Details: `BusinessFeatures/cross-cutting/bugs/2026-07-03-BUG-026-hwui-sigabrt-render-teardown/BUG-026-hwui-sigabrt-render-teardown.md` |


## Moved from BACKLOG.md (2026-07-15) — Token-scoped subagent reads — library file split + size-budget guard

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-14 | **Token-scoped subagent reads — library file split + size-budget guard** | ✅ Done | Registered + executed 2026-07-14 (Helder-approved via `/claude-code-setup:claude-automation-recommender`). Four large library files (`devexpress-patterns` 775, `workflow-reference` 671, `crud-pages` 623, `m3-components` 510 lines) split into 28 section files (all ≤ 225 lines) with the originals rewritten as index stubs (inbound `§` references keep resolving); `myvocalist-coding` skill map + `workflow.md` routing table point at section files directly; new `library-size-guard.py` PostToolUse hook warns when any library file regrows past 400 lines; CLAUDE.md Team Environment Setup moved to `library/dev-env-setup.md` (pointer left) to cut always-loaded context. Human-reviewed and approved by Helder 2026-07-14. Changelog 07/14/2026 entry has full detail. |


## Moved from BACKLOG.md (2026-07-15) — Documentation & spec-tracking governance — where docs live

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | **Documentation & spec-tracking governance — where docs live** | 💡 Pending | Registered by Helder 2026-07-11 (to discuss later). Recurring pain: spec/design/management docs authored on a feature/worktree branch get **lost from tracking** until merged; Helder must remind every time to route them to `develop`. Decide the standing rule/mechanism: e.g. (a) documentation is **always committed to `develop` directly** (never left on a feature branch), or (b) a **separate docs repo/location** so docs are not coupled to code-branch lifecycles, or (c) a hook/checklist that forces it. Define + encode in `workflow.md` (and, if a repo split, in `project-governance-reference.md`). Interim rule already in effect: docs pushed to `develop`. |


## Moved from BACKLOG.md (2026-07-15) — Evaluate guideline update — allow inline trivial-task execution to save tokens

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-12 | **Evaluate guideline update — allow inline trivial-task execution to save tokens** | 💡 Pending | Registered 2026-07-12. Goal is purely token-cost optimization, not a scope/authority change. Currently `workflow.md` Rule 2 requires all coding to go through a dispatched subagent; for a genuinely trivial task (e.g. a one-line doc append, a `.sln` registration entry) dispatching a fresh subagent loads a large context just to perform a tiny edit — an unneeded token spend. **Evaluate:** whether any agent type (not only the orchestrator) should be permitted to implement trivial tasks directly, in-line, without spawning a fresh agent, when the task is trivial enough that delegation overhead clearly outweighs the work. **Hard constraint to preserve in the evaluated guideline:** the decision of whether to (i) merge already-existing changes into `develop` when those changes live on another branch, or (ii) truly delegate to a fresh agent session, must always be made by the **orchestrator** (usually the main/orchestrator agent) — never by an ad-hoc subagent. This is a constraint on the guideline itself, not something the evaluation should relax. Cross-ref: `workflow.md` Rule 2 (Subagent Delegation), `orchestrator.md`. |


## Moved from BACKLOG.md (2026-07-15) — Workflow & Folder Layout Alignment

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Resolve conflicts between SDD, superpowers skills, and custom rules; canonicalize Docs/ layout; spec evolution tracking; review enforcement. Findings: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` · Plan: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/plan.md` |


## Moved from BACKLOG.md (2026-07-15) — Inline Undo Pattern — UX Standard

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | All inline destructive actions (sub-item removals within a form page, where the user never navigates away) must offer snackbar Undo using the commit-first pattern: delete immediately, undo re-inserts. Discovered fixing broken undo in SongFormPage URL removal. Applies to any future inline remove within a form context. Does NOT apply to list-page batch deletes (different interaction model). |


## Moved from BACKLOG.md (2026-07-15) — Enforce Git Worktrees for Parallel Subagents

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Enforce Git Worktrees for Parallel Subagents** | ✅ Done | Mandatory rule encoded in `orchestrator.md § Git Worktrees as Isolation Primitive` — threshold lowered from 3+ to 2+ subagents, made hard gate, staging-collision rationale added. `.worktrees/` confirmed gitignored. workflow.md pointer needs manual `amend:` commit (rules dir is write-protected). |


## Moved from BACKLOG.md (2026-07-15) — Mandatory Worktree Rule Enforcement — ALL Subagent Work

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | **Mandatory Worktree Rule Enforcement — ALL Subagent Work** | 💡 Pending | **Discovery (2026-06-27):** Git worktrees are mandatory for **ALL** subagent dispatches, not just 2+ concurrent within same session. **Root cause:** other terminals/sessions may run in parallel on develop branch; without worktrees, direct branch edits cause inevitable conflicts. Current rule (workflow.md Rule 2) only mandates worktrees for 2+ concurrent subagents — scope too narrow. **Action:** (1) Update workflow.md Rule 2 to mandate worktrees for every subagent, regardless of cardinality; (2) update orchestrator.md briefing protocol to require worktree setup before dispatch; (3) add to orchestrator pre-dispatch checklist; (4) document in constraints-registry.md. **Scope:** rule update + orchestrator protocol change; no code changes. **Note:** BUG-018 subagent (2026-06-27) executed on develop branch directly — retroactively acceptable (low collision risk, no concurrent work), but all future dispatches must use worktrees. |


## Moved from BACKLOG.md (2026-07-15) — Branch-lock avoidance — orchestrator (incl. BACKLOG handling) must also work in worktrees

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-10 | ↳ **Branch-lock avoidance — orchestrator (incl. BACKLOG handling) must also work in worktrees** | ✅ Done | Registered by Helder 2026-07-10 (extends parent row's scope from "all subagents" to "all agents, including the main/orchestrator"). Observed problem: a Claude Code session ends up locking the dev environment on a specific branch, concurring with (a) the developer's own IDE usage and (b) another terminal running a distinct task set started after the session that locked the branch. This must not happen: no session may pin the shared checkout — even the orchestrator's own edits (BACKLOG.md, specs, task-logs) need worktree isolation so parallel agents and the developer can freely navigate across branches. Worktree usage should be mandatory for a defined set of subagent types, and worktrees must always be created from the most-updated branch (`develop`). **Resolved 2026-07-14:** worktrees now mandatory for ALL code edits (workflow.md Rule 2 HARD RULEs), branch guard in constitutional-guard.py blocks code edits on develop/main, worktree-base-check.py verifies develop base, LEDGER.md + /sln-ledger + /sln-docs-sync added — see changelog 07/14/2026. |


## Moved from BACKLOG.md (2026-07-15) — Orchestrator Role Enforcement — Root Cause Investigation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Orchestrator Role Enforcement — Root Cause Investigation** | ✅ Done | Fixed 2026-06-15 (merged to develop). Root cause: negative-space omission — rules forbade the orchestrator *writing* code but never *reading* source, and no read-scope list existed. Added HARD RULE `.claude/agents/orchestrator.md § Orchestrator Read-Scope` (MAY-read allow-list / MAY-NOT deny-list, delegation requirement, plan-mode reconciliation, session-start self-check) + surgical `workflow.md` Rule 2 pointer + `CLAUDE.md § Roles` pointer (added 2026-06-15, commit `d0f3a70`) + changelog. ⏳ **Helder (only remaining gate):** human-review the rule file (authorship constraint). |


## Moved from BACKLOG.md (2026-07-15) — Bug Tracking Procedure

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Bug Tracking Procedure** | ✅ Done | New rule `.claude/rules/bug-tracking.md` — BUG-NNN IDs, BACKLOG nesting, severity table (Critical/Major/Minor), task-log + regression-test requirements per class. Changelog + .sln registered + `CLAUDE.md § Rules Files` pointer (added 2026-06-15, commit `d0f3a70`). Merged to develop. ⏳ **Helder (only remaining gate):** human-review the rule file (authorship constraint). |


## Moved from BACKLOG.md (2026-07-15) — Haiku Model Assignment for Low-Risk Subagent Tasks

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Haiku Model Assignment for Low-Risk Subagent Tasks** | ✅ Done | Classification table + eligibility rules + gate condition encoded in `orchestrator.md § Model Selection for Subagent Tasks`. Haiku-eligible: docs-only, .sln registration, migration-only, boilerplate scaffolding, XAML cosmetic, single-file rename. Gate: first use in each type must be verified against Sonnet output before encoding as default. |


## Moved from BACKLOG.md (2026-07-15) — Search Pattern Standardization + Navigation Result Service

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Search Pattern Standardization + Navigation Result Service** | 💡 Pending | The app currently has two distinct search patterns that must be reconciled before more search surfaces are added: (1) **Reactive inline search** — existing CRUD pages (VenuesPage, ArtistsPage, SongsPage, PersonsPage) bind `SearchText` to a ViewModel that debounces and searches locally, results shown on the same page. (2) **Push-navigate picker** — new picker pages (ArtistPickerPage, SongPickerPage, YouTubeSearchPage, introduced in Search Page Component) navigate to a standalone destination and return a result to the caller via WeakReferenceMessenger typed messages. A third candidate pattern — **Navigation Result Service** (`INavigationResultService<T>`: caller registers a typed callback before navigating, picker calls it on selection, no message types needed) — was evaluated and deferred. Investigation required: (a) deep analysis of all three patterns including pros/cons, testability, scalability, and MD3 alignment; (b) full audit of every search surface in the app; (c) decision on whether a shared base container page (SearchPageBase or SearchContentView) should be introduced as the enforcement mechanism to prevent pattern drift — this was proposed during Search Page Component brainstorming and deferred; (d) if a new canonical pattern is chosen, a migration plan for all existing CRUD search pages. The 3 picker pages are the first instances of the push-navigate pattern and serve as the reference implementation for this review. Do not add further search surfaces until this standardization task is at least in `📋 Spec` state. |


## Moved from BACKLOG.md (2026-07-15) — IAsyncRelayCommand Standardization

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending | Investigate and standardize usage of `IAsyncRelayCommand` vs `IRelayCommand` for async operations across all ViewModels in the app. Currently mixed — some commands use `IRelayCommand` with manual `IsLoading` toggling, others may already use `IAsyncRelayCommand`. Investigation must cover: (1) pros/cons of `IAsyncRelayCommand` (built-in `IsRunning`/`CanExecute` during execution, exception handling, cancellation support) vs manual approach, (2) full audit of all `[RelayCommand]`-generated and manually declared commands across every ViewModel, (3) risk of changing existing commands that views already bind to. The three picker ViewModels introduced in Search Page Component (`ArtistPickerViewModel`, `SongPickerViewModel`, `YouTubeSearchViewModel`) are the first instances of the standardized pattern — use them as the reference implementation. |


## Moved from BACKLOG.md (2026-07-15) — Component Change Governance Rule

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Component Change Governance Rule** | ✅ Done | New rule `.claude/rules/component-change-governance.md` — four gates (dedicated task + MD3 review, consumer map, per-consumer risk assessment, Helder approval) before any shared custom-component change; no bundling. Changelog + .sln registered + `CLAUDE.md § Rules Files` pointer (added 2026-06-15, commit `d0f3a70`). Merged to develop. ⏳ **Helder (only remaining gate):** human-review the rule file (authorship constraint). |


## Moved from BACKLOG.md (2026-07-15) — Search Error State UX Standardization

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Search Error State UX Standardization** | 💡 Pending | Picker pages (ArtistPickerPage, SongPickerPage, YouTubeSearchPage) will show a "Search failed. Please try again." error state via EmptyState when an API call fails. This behavior does not exist on any current CRUD search page (VenuesPage, ArtistsPage, SongsPage, PersonsPage). For UX consistency, the same error state should be retrofitted to all app search pages. Track which pages already have it once Search Page Component ships, then standardize. |


## Moved from BACKLOG.md (2026-07-15) — Filter Pattern Standardization

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Filter Pattern Standardization** | 💡 Pending | ArtistsPage FilterChipGroup displays correctly on CrudListView (fix: 2026-06-10 commit `b416e1e`). Before a second CRUD page adds a filter, define a standardized pattern: (1) when/where filters appear vs inline search, (2) UI standard (FilterChipGroup as default? Other options?), (3) data binding contract for filter state, (4) performance: in-memory LINQ vs DB-side collation filtering. **Action:** Create lightweight governance spec (`.claude/rules/filter-governance.md` or section in existing rules) or `Docs/Management/DevCycleCraft/filter-pattern-standardization/spec.md`. **Blocking rule:** any new filter additions to CRUD pages must follow the standardized pattern (similar to Component Change Governance Rule). **Note:** CrudListView.FilterContent BindableProperty already supports any View type; current ArtistsPage implementation serves as reference. Queue Management and future entities may have different filter needs (different chip options, quantities, UI styles). |


## Moved from BACKLOG.md (2026-07-15) — MD3/DevExpress Compliance Gap — Internal Guidelines

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **MD3/DevExpress Compliance Gap — Internal Guidelines** | ✅ Done | Discovered: developers coded filter UI without verifying DX has built-in MD3 components available. Result: SongsPage uses plain DXButton instead of `dxe:FilterChipGroup`; BottomSheetTitle style missing from `MaterialStyles.xaml`. Root cause: no pre-implementation checklist in `.claude/library/` files to verify component availability against DX docs + MD3 spec. **Action:** enhance `devexpress-patterns.md` + `m3-components.md` with a pre-implementation DX component audit pattern to catch this class of error before code review. |


## Moved from BACKLOG.md (2026-07-15) — ~~MD3 Non-Compliance: SongsPage Filter Chips~~ → ArtistsPage Filter Chips

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ~~MD3 Non-Compliance: SongsPage Filter Chips~~ → **ArtistsPage Filter Chips** | 🔵 Duplicate | **Misattribution corrected (Helder, 2026-06-14):** the app has filter chips on **ArtistsPage only** (Author/Performer); there is no "songs chips" surface. SongsPage has no filter UI and no song-filter domain concept. This item is a duplicate of **"Artists CRUD List filter issue"** (Author/Performer chip regression), tracked + fixed under branch `fix/artists-filter-regression`. Closed as duplicate — no separate work. |


## Moved from BACKLOG.md (2026-07-15) — MD3 Non-Compliance: BottomSheetTitle Style Missing

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **MD3 Non-Compliance: BottomSheetTitle Style Missing** | ✅ Done | Already resolved — `BottomSheetTitle` style exists at `MaterialStyles.xaml:230` (titleLarge 22sp RobotoRegular, OnSurface, padding 16,16,16,0); added under BUG-004 / song-import-resolution Wave 4A (commit `9b37d2a`). Consumed by SongFormPage.xaml:244,325. Verified on develop 2026-06-14. |


## Moved from BACKLOG.md (2026-07-15) — Bug: Shell navigation swallows button tap animations

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Bug: Shell navigation swallows button tap animations** | 💡 Pending | Tapping a nav button (e.g. Venues in flyout) triggers Shell navigation immediately, so the ripple/press animation on the tapped button never completes visually. When returning to the menu and tapping again, the queued animation from the previous tap fires. Root cause: Shell navigation runs on the UI thread and preempts the animation frame. Fix: delay navigation by one frame or use `Dispatcher.DispatchDelayed` so the animation completes before the page transition begins. Affects all Shell flyout items and any button that triggers navigation on tap. |


## Moved from BACKLOG.md (2026-07-15) — Bug/Verify: FloatingToolbar always visible — should appear only on multi-select

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Bug/Verify: FloatingToolbar always visible — should appear only on multi-select** | 💡 Pending | Smoke test (2026-06-06) shows FloatingToolbar + SelectAll button always visible on VenuesPage even with no selection. Per original design, FloatingToolbar should only appear when ≥1 item is selected (multi-select mode). Verify: (1) check original spec/design for intended visibility behavior; (2) confirm whether CrudListView or page controls toolbar visibility; (3) if always-visible is intentional (UX decision), close as won't-fix; if not, implement `IsVisible` binding tied to `SelectedCount > 0` or `IsAnySelected`. |


## Moved from BACKLOG.md (2026-07-15) — DB-Side Collation — Remove All Normalized Columns

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **DB-Side Collation — Remove All Normalized Columns** | ✅ Done | **Decided direction (Helder, 2026-06-01):** All accent+case normalization must be handled by the database, never by C# code. (1) Drop all `*Normalized` shadow columns (`Artist.NameNormalized`, `Song.TitleNormalized`, `Person.FullNameNormalized`) — only the original display field survives. (2) UNIQUE indexes (e.g. song title per artist) must be defined on the original column with the collation applied, so the DB enforces uniqueness accent+case insensitively. (3) All queries (search, duplicate checks, autocomplete) must rely on the DB collation — no `ToLowerInvariant()` or `RemoveDiacritics()` in service or repository code. (4) Collation registration must be abstracted via EF Core configuration so that adding a second DB provider (MySQL, MSSQL, PostgreSQL) requires only a provider-specific collation name — no business logic changes. Currently `NOCASE_NOACCENT` is a custom SQLite collation registered via `CollationInterceptor`; the pattern must be designed to swap the collation name per provider. Spec folder: `Docs/Management/DevCycleCraft/db-collation-normalization/`. |


## Moved from BACKLOG.md (2026-07-15) — Navigation Icon Pattern — Root Pages vs Pushed Pages

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Navigation Icon Pattern — Root Pages vs Pushed Pages** | ✅ Done | Standardize leading AppBar icon behavior: root flyout pages show hamburger (menu icon, opens drawer); pushed detail pages show back arrow (pops stack). Currently all 4 CRUD list pages hardcode back arrow even when reached from flyout. Plan: `cls-mellow-lighthouse.md`. Solution: dynamic icon in `CrudListPageBase.OnNavigatedTo` based on `NavigationStack.Count`. |


## Moved from BACKLOG.md (2026-07-15) — Search AppBar Pattern — Root Page + Search Interaction

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Search AppBar Pattern — Root Page + Search Interaction** | 🔵 Deferred | **Superseded 2026-07-10** by *AppBar / SearchAppBar Interaction Redesign* (nested below) — Helder's new desires overwrite this row's option set; the MD3-research requirement carries over. Original: define MD3-compliant pattern for search toggle on root CRUD list pages when hamburger icon is present; with hamburger, tapping it no longer toggles search off like back button did. Discovered during Navigation Icon Pattern work. |


## Moved from BACKLOG.md (2026-07-15) — AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.
>
> **Decision recorded 2026-07-19:** Helder's bar-swap-kill hypothesis was validated against official MD3 docs (Material Components Search: persistent SearchBar is the recommended pattern; bar swap is M2-era) and NN/g search-visibility research, and approved. Standard: SmallAppBar stays sole TitleView occupant; persistent 56dp M3 standalone search bar (no leading back arrow) hosted inside `CrudListView`; SearchAppBar + `IsSearchMode` swap machinery retired via four-gate governance. Full record: `DevCycleCraft/appbar-searchbar-redesign/2026-07-19-persistent-searchbar-decision.md`.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-10 | ↳ **AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar** | 💡 Pending | Registered by Helder 2026-07-10 (supersedes parent row's options). (1) **Pattern for any page navigated via hamburger-menu buttons or not:** the AppBar must show the hamburger menu with the very same root options; the AppBar must also show the back button returning to where the user came from — but this may not always make sense, so evaluate per page context. (2) **Bar-swap problem:** today the AppBar search button swaps the top bar to SearchAppBar, whose back button swaps it back — confusing even to Helder; back buttons are for navigating back to the prior page, not for switching bars. Helder's hypothesis (**to be validated against official MD3 docs — do NOT agree by default**): kill the bar swap; show the search bar always, right below the AppBar, without its weird leading back button (a leading back in the search bar could still be useful for a hypothetical page without an AppBar handling the title — none exists today, but may appear). Inspect MD3 compliance for the options before deciding. Consumers: all CRUD list pages; SmallAppBar/SearchAppBar are governed components → component-change-governance four gates apply. |


## Moved from BACKLOG.md (2026-07-15) — Artists CRUD List filter issue

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | **Artists CRUD List filter issue** | ✅ Done | Fixed 2026-06-14 (`fix/artists-filter-regression`, merged to develop). Root cause: `CrudListView` hosted `FilterContent` in a bare `ContentPresenter`, which only renders inside a `ControlTemplate` — so the Author/Performer chips never appeared; the prior "Restore" commit only un-hid an empty presenter. Fix: replaced with a `ContentView` host. Only ArtistsPage uses the slot; other CRUD pages collapse gracefully. ⏳ **Helder:** emulator smoke test to confirm chips render + filter. |


## Moved from BACKLOG.md (2026-07-15) — Add missing queue_music_outlined icon asset

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-12 | Add missing queue_music_outlined icon asset | ✅ Done | Added `Resources/Images/queue_music_outlined.svg` (Material Symbols queue_music outlined; consumed by ArtistsPage). Merged to develop. ⏳ emulator smoke test to confirm Glide load. |


## Moved from BACKLOG.md (2026-07-15) — CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-12 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 💡 Pending | T5 spike confirmed viable; deferred — Release already instantaneous; nice-to-have for Debug experience |


## Moved from BACKLOG.md (2026-07-15) — DbContext-per-operation architecture review

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ DbContext-per-operation architecture review | 💡 Pending | Replace shared scoped-as-singleton `AppDbContext` with `IDbContextFactory<AppDbContext>` (context per repository operation). Removes the need for the static load gate added by the Page-load-frozen fix; also covers form saves. ~10 Infra files + `MauiProgram.cs`. Architectural change — spec + Helder review required. **Known residual (review 2026-06-10):** delete operations run outside the load gate (deadlock guard), so a delete can still overlap a gated load on the shared context — fully resolved only by this refactor. Must be designed against the planned INFRA_MSSQL provider direction. |


## Moved from BACKLOG.md (2026-07-15) — Flaky test: SongRepositoryTests/QueueRepositoryTests parallel SQLite race

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Flaky test: SongRepositoryTests/QueueRepositoryTests parallel SQLite race | 💡 Pending | Transient `ObjectDisposedException` on SQLitePCL handle during `sqlite3_create_collation` in `SongRepositoryTests.InitializeAsync` under parallel test execution (observed 2026-06-10, 1-in-3 runs). **Also (2026-06-19): `QueueRepositoryTests` intermittently fails with `SQLite Error 19: FOREIGN KEY constraint failed` on SaveChanges under full-suite parallelism (3→1 failures across runs); 5/5 green in isolation and unaffected by the BUG-011 change.** Pre-existing; likely xunit parallel classes racing on SQLite connection/collation setup. Investigate collation registration vs connection disposal, and consider a non-parallel collection for integration repo tests. |


## Moved from BACKLOG.md (2026-07-15) — Paged query optimization — Venue/Artist count subqueries

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Paged query optimization — Venue/Artist count subqueries | 💡 Pending | `VenueRepository.GetPagedWithEventInfoAsync` and `ArtistRepository.GetPagedAsync` project per-row `Events.Count()` / `CatalogEntries.Count()` correlated subqueries. Rewrite (grouped count query or indexed FK) to shorten shimmer time on data-heavy pages. Evidence-driven: only if S23 smoke test after the unfreeze fix still shows long shimmer. |


## Moved from BACKLOG.md (2026-07-15) — Infra Repository Folder Consolidation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | **Infra Repository Folder Consolidation** | 💡 Pending | Two folders do the same job: `Infra/Repository/` (8 repos) and `Infra/Repositories/` (2 repos). Merge into a single `Infra/Repositories/` folder. No logic changes — file moves + namespace updates + `.sln` re-registration. Independent of BUG-018; can be done any session. |


## Moved from BACKLOG.md (2026-07-15) — Read Model + Global NoTracking Pattern — Guidelines Update

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 💡 Pending | After BUG-018 is verified on-device: encode the new canonical patterns into `.claude/library/database-indexing.md` and `crud-pages.md`. Patterns to document: (1) `ChangeTracker.QueryTrackingBehavior = NoTracking` on `AppDbContext` constructor; (2) `{Entity}ListItem` record in `Domain/ReadModels/` as the single list-projection type (replaces Contracts DTO); (3) explicit `.AsTracking()` on `GetByIdAsync` for the edit path; (4) `_db.Set<T>().Update(entity)` as the standard write pattern — no ChangeTracker guard; (5) `{Entity}ListItem` is used as a **command parameter type** in form ViewModels (e.g. `SelectDuplicateCommand<ArtistListItem>`) — not just in list ViewModels. Guidelines must call this out explicitly so form VMs are not overlooked during refactoring. **Depends on:** BUG-018 ✅ and on-device smoke test passing. |


## Moved from BACKLOG.md (2026-07-15) — CRUD Read Model Refactoring — Persons, Songs, Venues

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 💡 Pending | Apply the same architectural pattern introduced by BUG-018 (Artists) to the remaining CRUD entities: `PersonListItem` (Domain), `SongListItem` (Domain), `VenueListItem` (Domain) — each replacing its Contracts DTO counterpart. Per entity: (1) add `{Entity}ListItem` record to `Domain/ReadModels/`; (2) update `I{Entity}Repository.GetPagedAsync` to return the read model with SQL-level column projection; (3) update `{Entity}Repository` implementation; (4) update `{Entity}Service` to return the read model; (5) delete the Contracts DTO; (6) **update the `{Entity}FormViewModel`** — each form VM likely uses the Contracts DTO as the `SelectDuplicate` command parameter type (same pattern as `ArtistFormViewModel.SelectDuplicateCommand<ArtistListItemDto>`); update to `{Entity}ListItem` in the same commit as the DTO deletion. Apply sequentially (one entity per session) or in parallel waves. When all three are done, evaluate whether `MyVocaList.Contracts/DTOs/List/` can be removed entirely. **Depends on:** Guidelines Update ✅. |


## Moved from BACKLOG.md (2026-07-15) — Local enforcement automations (solo, pre-prod)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-03 | **Local enforcement automations (solo, pre-prod)** | ✅ Done | Two mechanical gates added while solo/pre-production (team CI + GitHub MCP deferred to Play Store launch). (1) **Constitutional guard** — `PreToolUse` blocking hook `.claude/scripts/constitutional-guard.py` denies any Write/Edit/MultiEdit that *introduces* a native-dialog call (`DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync`, call-form `Name(` only) into a `.cs`/`.xaml`/`.xaml.cs` file (exit 2; fail-open on error). Turns the `[Unamendable]` no-native-dialogs constraint from review-checklist into a hard gate. No existing usages in repo (zero migration risk). (2) **Local pre-commit gate** — `.claude/githooks/pre-commit` (registered via `core.hooksPath`) runs `dotnet test MyVocaList.Tests` when staged files include `.cs`/`.xaml`, blocking commits on red build/tests; skips docs/config-only commits; `--no-verify` to bypass. CI-substitute for solo dev; same script becomes real CI at launch. Both verified this session (guard: 4 payload cases; gate: branch-detection cases + live skip-path on this commit). |


## Moved from BACKLOG.md (2026-07-15) — Scope `myvocalist-coding` skill to project level

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | **Scope `myvocalist-coding` skill to project level** | ✅ Done | The MyVocaList-only `myvocalist-coding` skill currently lives at USER level (`~/.claude/skills/myvocalist-coding/`), so its description loads (and it is invocable) in every unrelated project — a small cross-project token/scope leak (Helder flagged 2026-07-05). Move it to project scope (`<repo>/.claude/skills/myvocalist-coding/`), matching the already-project-scoped `maui-*` skills; this also brings the skill map in-repo, fixing rules-refactoring Gotcha 2 (out-of-repo skill-map edits). **NOTE:** `.claude/library/*.md` are ALREADY project-scoped (in-repo, on-demand only, not memory-loaded) — no leak there; only the skill needs moving. Migration requires a session restart to confirm the project-scoped skill is discovered BEFORE deleting the user-level copy (cannot verify in-session — same constraint as Gotcha 3). Do in a supervised window, not autonomously. **UPDATE 2026-07-07 (`5fe5247`): copy to project scope DONE**; `/reload-skills` confirmed the project copy registers (skill listed twice while both existed); user-level copy DELETED same day. Now shows `myvocalist-coding Skill · project`. **DONE.** |


## Moved from BACKLOG.md (2026-07-15) — SECURITY — rotate + de-commit secrets in `.mcp.json`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | **SECURITY — rotate + de-commit secrets in `.mcp.json`** | ✅ Done | **De-commit DONE 2026-07-07** (`7d2ed72`): secrets replaced with `${CONTEXT7_API_KEY}`, `${GITHUB_MCP_PAT}`, `${PLAYWRIGHT_MCP_EXTENSION_TOKEN}` expansion — no literal secrets remain in the working tree. **REMAINING (Helder, manual):** (1) rotate all three tokens (old values persist in git history); (2) set the env vars before next context7/playwright use (github stays disabled). History rewrite optional (private repo) — Helder decides. |


## Moved from BACKLOG.md (2026-07-15) — MCP governance sync + Docs housekeeping

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | **MCP governance sync + Docs housekeeping** | ✅ Done | **MCP part DONE 2026-07-07** (`5fe5247`): `github` (unused per Helder) + `sequential-thinking` (redundant with native extended thinking on Claude 4.5+/5 models; never in approved list) removed from `enabledMcpjsonServers` — active set now context7 + devexpress-maui only; CLAUDE.md GitHub row updated. **REMAINING (Docs housekeeping from `sprightly-launching-corbato.md`):** move 38 MB debug log, `.claudeignore` debug-capture rule (partially present), archive scratch-named root files, delete empty `ManagementByPass`, rename the 2 research docs into feature folders + `.sln`. |


## Moved from BACKLOG.md (2026-07-15) — HELDER MANUAL ACTIONS (reminder)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-09 | **HELDER MANUAL ACTIONS (reminder)** | ✅ Done | All three complete (confirmed by Helder 2026-07-09): (1) GitHub PAT + Context7 key + Playwright token rotated and env vars set; (2) `context7` + `devexpress-maui` re-enabled in `/mcp` (context7 confirmed live in-session 2026-07-09); (3) authorship review of CLAUDE.md amends + refactored rules files complete (rules files approved 2026-07-09 — see workflow.md/testing.md etc. authorship notes). |


## Moved from BACKLOG.md (2026-07-15) — Tool-registry cleanup: context-budget plugin, duplicate `review`, exa MCP

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | **Tool-registry cleanup: context-budget plugin, duplicate `review`, exa MCP** | ✅ Done | ✅ **(a) DONE 2026-07-07** — `context-budget@teslasoft-skills` entry removed from `enabledPlugins`. ✅ **(c) DONE 2026-07-08** — Rule 6 amended to two tiers (Context7 → WebSearch/WebFetch); `workflow-reference.md § Rule 6` mirrored. ✅ **EXTRA: exa removed from settings.json `disabledMcpjsonServers` (2026-07-09)** — entry had been pending deletion since Rule 6 amend. ✅ **(b) DONE 2026-07-09** — Collision resolved by renaming ALL 5 project commands with a new `sln-` prefix pattern (`sln-build`, `sln-commit`, `sln-changelog`, `sln-release`, `sln-review`), encoded as `[HARD RULE]` in `CLAUDE.md § Commands`: prefix marks solution-local dev-workflow commands, prevents built-in/plugin skill collisions, and stays valid when these dev settings bootstrap another solution. `/sln-review` = this solution's post-task review; built-in `/review` = GitHub PR review. All live refs updated (CLAUDE.md, workflow.md, workflow-reference.md, testing-reference.md, settings.json hook messages, command files' cross-refs, artists-songs/tasks.md, whats-new + app-versioning specs, backlog-first-registration plan paths); historical docs/plans/reports left as-is per record-preservation precedent. Command files are not `.sln`-registered (verified) — no `.sln` change. **All items (a)/(b)/(c)/extra complete.** |


## String trimming on persistence — centralized normalization analysis (registered 2026-07-15)

Registered by Helder 2026-07-15 as an extension of BUG-046 (autocomplete query whitespace). Scope: analyse whether all strings sent to the DB on persistence should be trimmed (leading/trailing; possibly collapse internal double spaces) and whether a centralized place (e.g. interceptor, base service, or entity-config convention) can do it with **zero friction** — friction kills the proposal. Constraints: no C#-side normalization for *search/dedup* (that stays DB-collation per `constraints-registry.md`); this item is about stored-value hygiene only. Deliverable: short findings + proposal for Helder's decision before any implementation.
