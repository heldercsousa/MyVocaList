# MyVocaList — Product Backlog

> **Product backlog.** Business features are ordered by target delivery date. Every feature begins as `💡 Pending` and is promoted through the lifecycle below before any code is written.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.
>

---

## Status reference

| Status | Meaning |
|--------|---------|
| `💡 Pending` | Captured, not yet evaluated |
| `📋 Spec` | Approved — spec being written |
| `🗺️ Plan` | Spec approved — plan being written |
| `🟢 Ready` | Plan approved — dispatchable |
| `🟡 In Progress` | Implementation underway |
| `🔵 Deferred` | Paused — reason documented |
| `🔴 Blocked` | Cannot proceed — dependency named |
| `✅ Done` | Shipped — changelog updated |

---

## Business Features

| Target | Feature | Status | Notes |
|--------|---------|--------|-------|
| 2026-03 | **Venues CRUD** | ✅ Done | Full MD3 list, search, multi-select, swipe-delete |
| 2026-04 | **Person CRUD** | ✅ Done | Autocomplete field, duplicate detection. Plan: `Docs/Management/BusinessFeatures/persons/plan.md` |
| 2026-05 | **Artists & Songs Catalog** | 🟡 In Progress | Spec: `Docs/Management/BusinessFeatures/artists-songs/` · Plan: `Docs/Management/BusinessFeatures/artists-songs/plan.md` · ⏳ **Helder:** Phase 16C emulator smoke test pending — see `tasks.md § Phase 16C` |
| 2026-05 | ↳ Song Karaoke URLs | ✅ Done | YouTube URL management per song; SongFormPage section, settings, converters, tests. Spec: `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/` |
| 2026-05 | ↳ Bug: GoToSettings navigation exception | ✅ Fixed | `GoToAsync("settings")` called from pushed-page context (SongFormPage); FlyoutItem requires absolute route `//settings`. Single-line fix in `SongFormViewModel.cs`. |
| 2026-06 | ↳ Bug: Artists page missing back button + unclear trailing toggle | 💡 Pending | No back button on ArtistsPage AppBar; trailing pill button has no icon/label. Details: `artists-songs/bugs/BUG-001-artists-page-no-back-button.md` |
| 2026-06 | ↳ Bug: Artist/Song form search strip non-MD3 | 🔴 Blocked | Blocked on **Search Page Component** (row below). MD3 research confirmed: no inline form search pattern. Option C (dedicated search page) approved. Details: `artists-songs/bugs/BUG-002-artist-form-search-non-md3.md` |
| 2026-06 | **Search Picker** | 🟡 In Progress | Replaces non-MD3 inline search strips with 3 picker pages (Artist, Song, YouTube). Phase 1 in progress. Spec: `Docs/Management/BusinessFeatures/search-page-component/` · Plan: `search-page-component/plan.md` · Tasks: `search-page-component/tasks.md` |
| 2026-06 | **Crash & Error Reporting** | ✅ Done | 2 bugs found in review (fixed). Spec: `Docs/Management/BusinessFeatures/crash-reporting/` |
| 2026-06 | ↳ Pre-release checklist | 💡 Pending | App-wide: fill Sentry DSN, run smoke test, add multi-env DSN if needed. Details: `tasks.md`. |
| 2026-06 | **What's New / Release Notes** | ✅ Done | Bundled `releases.json`; one-time modal on version upgrade. Plan: `Docs/Management/BusinessFeatures/whats-new/plan.md` |
| 2026-06 | **User Suggestions** | ✅ Done | In-app form → GitHub Issues API; auto-captures device metadata. ⏳ **Helder:** Add fine-grained GitHub PAT to `appsettings.json` (Issues R/W on heldercsousa/MyVocaList). Plan: `Docs/Management/BusinessFeatures/user-suggestions/plan.md` |
| 2026-06 | **App Update Check** | ✅ Done | Remote version manifest; soft nudge + hard block sheets; fail-open. ⏳ **Helder:** Update `version-manifest.json` versions when shipping; replace `idXXXXXXX` with real App Store ID. Plan: `Docs/Management/BusinessFeatures/app-update-check/plan.md` |
| 2026-06 | **App Settings** | ✅ Done | YouTube API key management (PasswordEdit, save/test/clear); flyout "Preferences" now navigates to SettingsPage; stale `HasYouTubeApiKey` refreshed on `OnAppearing`. Spec: `Docs/Management/BusinessFeatures/app-settings/` |
| 2026-06 | **About Page** | ✅ Done | Version, logo, goal sentence, Since year, CC BY-NC-ND 4.0 license, What's New stub (hidden). Spec: `Docs/Management/BusinessFeatures/about-page/` |
| 2026-06 | **Queue Management** | 💡 Pending | Core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate |
| 2026-06 | **Visual Theme Refresh** | 🔵 Deferred | Two paths evaluated (2026-06-02). Path B (Blazor Hybrid) chosen as long-term direction. Path A (theme-only) deferred pending spike outcome. |
| 2026-06 | ↳ Theme Refresh Only | 🔵 Deferred | Superseded by Blazor Hybrid decision. Will apply Karaoke Neon palette via MudTheme during the spike/migration, not as a standalone DevExpress theme patch. |
| 2026-06 | ↳ ui-2nd-refactor | 📋 Spec | **Direction decided (2026-06-02):** Blazor Hybrid + MudBlazor + shared RCL. MudMCP installed. Parallel spike approach approved. Spec: `Docs/Management/BusinessFeatures/UI-2nd-refactor/` — prompt.md documents full decision session. · ⏳ **Helder (next session):** verify MudMCP index (~100 components) |
| 2026-06 | **Data Backup & Restore — Tier 1 + 3** | ✅ Done | Local auto-backup (SQLite snapshot + transaction log) + manual share sheet export/restore. Plan: `Docs/Management/BusinessFeatures/backup-restore/plan.md`. Spec: `Docs/Management/BusinessFeatures/backup-restore/design.md` |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending | Local or/and online tutorials. Evaluation of best practices in the lower possible effort to produce 1st version and update it always app receives new features and updates existing ones |
| 2026-06 | **Website** | 💡 Pending | Evaluate the usage of the website - for marketing, documentation, and community engagement - myvocalist.com / myvocalist.app |
| 2026-06 | | 🏁 **MVP release** | |
| - | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending | mDNS auto-discovery + TCP sync + AES-256 pairing code encryption. Second device on same WiFi auto-receives transaction log in real time; fresh install auto-discovers mirror and restores in one tap. Spec: `Docs/Management/BusinessFeatures/backup-restore/design.md § Tier 2`. Depends on Tier 1 being shipped. |
| — | **Singer self-registration** | 💡 Pending | Singers register via public link / kiosk device / self device app connected to host device or able to self register into the host somehow |
| — | **Social features** | 💡 Pending | Post-event sharing, singer stats |
| — | **Windows version** | 🔴 Blocked | Blocked on DevExpress MAUI Windows support (no Windows renderer exists). Re-evaluate when DX announces Windows support. Spec: `Docs/Management/BusinessFeatures/windows-version/design.md` |

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

| Completed | Activity | Status | Plan / Reference |
|-----------|----------|--------|-----------------|
| 2026-03 | Solution Structure Refactor | ✅ Done | Move service interfaces to Domain, delete IDatabaseInit, reorganize MAUI project. Plan: `Docs/Management/DevCycleCraft/solution-structure-refactor/plan.md` |
| 2026-03 | MD3 App Bar Components | ✅ Done | SmallAppBar + SearchAppBar ContentView components. Plan: `Docs/Management/DevCycleCraft/md3-appbar-components/plan.md` |
| 2026-03 | M3 Lists | ✅ Done | `Docs/Management/DevCycleCraft/m3-lists/plan.md` |
| 2026-03 | Venues MD3 rebuild | ✅ Done | `Docs/Management/BusinessFeatures/venues/plan.md` |
| 2026-03 | Styles & Structure | ✅ Done | `Docs/Management/DevCycleCraft/styles-structure/plan.md` |
| 2026-04 | Toolbar/FAB vibrant | ✅ Done | `Docs/Management/DevCycleCraft/toolbar-fab-vibrant/plan.md` |
| 2026-04 | Autocomplete field | ✅ Done | `Docs/Management/BusinessFeatures/persons/plan-autocomplete.md` |
| 2026-04 | Hooks redesign | ✅ Done | Stop/TaskCreated/TaskCompleted hooks; session-end auto-commit. Plan: `Docs/Management/DevCycleCraft/hooks-redesign/plan.md` |
| 2026-05-07 | SDD Master Plan (Phases 1–11, 162 steps) | ✅ Done | `Docs/Management/DevCycleCraft/sdd/impl-master-plan.md` |
| 2026-05-07 | workflow.md Reduction | ✅ Done | Agent role files (`implementor.md`, `orchestrator.md`) absorb scattered responsibilities; 61 findings resolved. Plan: `Docs/Management/DevCycleCraft/workflow-compression/plan.md` |
| 2026-05-08 | CLAUDE.md Deep Restructure | ✅ Done | 7 coding rule files → `.claude/library/`; `.claudeignore` Docs/ scope gates. Plan: `Docs/Management/DevCycleCraft/docs-context-scope-control/plan.md` |
| 2026-05-12 | Architecture Tests Evaluation | ✅ Done | Research: NetArchTest.Rules vs ArchUnitNET adoption decision — evaluated, no changes produced. Plan: `Docs/Management/DevCycleCraft/architecture-tests-evaluation/plan.md` |
| 2026-05-15 | Claude Managed Agents Evaluation | ✅ Done | Research: Anthropic hosted agents for dev cycle — discarded; duplicates existing workflow and lacks community maturity. Plan: `Docs/Management/DevCycleCraft/claude-managed-agents-evaluation/plan.md` |
| 2026-05-13 | Day-to-day task management workflow review | ✅ Done | BACKLOG.md first-class SCRUM board; workflow.md Rule 1/7 updated. Plan: `Docs/Management/DevCycleCraft/backlog-workflow-integration/plan.md` |
| 2026-05-31 | **App Versioning Strategy** | ✅ Done | MinVer NuGet; git-tag-driven semver; `/project:release` command. Spec: `Docs/Management/DevCycleCraft/app-versioning/design.md` · Plan: `Docs/Management/DevCycleCraft/app-versioning/plan.md` |
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Resolve conflicts between SDD, superpowers skills, and custom rules; canonicalize Docs/ layout; spec evolution tracking; review enforcement. Findings: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` · Plan: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/plan.md` |
| 2026-05 | **VS Solution File Registration Rule** | ✅ Done | Mandatory rule: any doc file visible in VS must be registered in .sln before commit. See `workflow.md` and `constraints-registry.md`. |
| 2026-05 | **Proactive BACKLOG Entry Rule** | ✅ Done | Agents must add brief BACKLOG entries for untracked work identified during sessions. See `workflow.md` Rule 1. |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | All inline destructive actions (sub-item removals within a form page, where the user never navigates away) must offer snackbar Undo using the commit-first pattern: delete immediately, undo re-inserts. Discovered fixing broken undo in SongFormPage URL removal. Applies to any future inline remove within a form context. Does NOT apply to list-page batch deletes (different interaction model). |
| 2026-06 | **Enforce Git Worktrees for Parallel Subagents** | ✅ Done | Mandatory rule encoded in `orchestrator.md § Git Worktrees as Isolation Primitive` — threshold lowered from 3+ to 2+ subagents, made hard gate, staging-collision rationale added. `.worktrees/` confirmed gitignored. workflow.md pointer needs manual `amend:` commit (rules dir is write-protected). |
| 2026-06 | **Bug Tracking Procedure** | 💡 Pending | `workflow.md` has a Bug Fix Pattern (commit message as spec) but no rules for: nesting bugs under parent features in BACKLOG, when bugs get task-log entries, severity classification, or regression test requirements per bug class. Discovered while handling the GoToSettings navigation exception. |
| 2026-06 | **Haiku Model Assignment for Low-Risk Subagent Tasks** | ✅ Done | Classification table + eligibility rules + gate condition encoded in `orchestrator.md § Model Selection for Subagent Tasks`. Haiku-eligible: docs-only, .sln registration, migration-only, boilerplate scaffolding, XAML cosmetic, single-file rename. Gate: first use in each type must be verified against Sonnet output before encoding as default. |
| 2026-06 | **Search Pattern Standardization + Navigation Result Service** | 💡 Pending | The app currently has two distinct search patterns that must be reconciled before more search surfaces are added: (1) **Reactive inline search** — existing CRUD pages (VenuesPage, ArtistsPage, SongsPage, PersonsPage) bind `SearchText` to a ViewModel that debounces and searches locally, results shown on the same page. (2) **Push-navigate picker** — new picker pages (ArtistPickerPage, SongPickerPage, YouTubeSearchPage, introduced in Search Page Component) navigate to a standalone destination and return a result to the caller via WeakReferenceMessenger typed messages. A third candidate pattern — **Navigation Result Service** (`INavigationResultService<T>`: caller registers a typed callback before navigating, picker calls it on selection, no message types needed) — was evaluated and deferred. Investigation required: (a) deep analysis of all three patterns including pros/cons, testability, scalability, and MD3 alignment; (b) full audit of every search surface in the app; (c) decision on whether a shared base container page (SearchPageBase or SearchContentView) should be introduced as the enforcement mechanism to prevent pattern drift — this was proposed during Search Page Component brainstorming and deferred; (d) if a new canonical pattern is chosen, a migration plan for all existing CRUD search pages. The 3 picker pages are the first instances of the push-navigate pattern and serve as the reference implementation for this review. Do not add further search surfaces until this standardization task is at least in `📋 Spec` state. |
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending | Investigate and standardize usage of `IAsyncRelayCommand` vs `IRelayCommand` for async operations across all ViewModels in the app. Currently mixed — some commands use `IRelayCommand` with manual `IsLoading` toggling, others may already use `IAsyncRelayCommand`. Investigation must cover: (1) pros/cons of `IAsyncRelayCommand` (built-in `IsRunning`/`CanExecute` during execution, exception handling, cancellation support) vs manual approach, (2) full audit of all `[RelayCommand]`-generated and manually declared commands across every ViewModel, (3) risk of changing existing commands that views already bind to. The three picker ViewModels introduced in Search Page Component (`ArtistPickerViewModel`, `SongPickerViewModel`, `YouTubeSearchViewModel`) are the first instances of the standardized pattern — use them as the reference implementation. |
| 2026-06 | **Component Change Governance Rule** | 💡 Pending | Encode a hard rule (workflow.md or orchestrator.md) forbidding any modification to a custom component (`SearchAppBar`, `ListItem`, `SmallAppBar`, `EmptyState`, `AutocompleteField`, etc.) without: (1) a dedicated task with MD3 compliance review for the proposed change, (2) a systematic map of every page/view that consumes the component, (3) a risk assessment for each consumer, and (4) Helder approval before implementation starts. No component change may be bundled into a feature task — it must be its own tracked task. Discovered when a spec proposed adding `SearchCommand` to `SearchAppBar` without any of the above. |
| 2026-06 | **Search Error State UX Standardization** | 💡 Pending | Picker pages (ArtistPickerPage, SongPickerPage, YouTubeSearchPage) will show a "Search failed. Please try again." error state via EmptyState when an API call fails. This behavior does not exist on any current CRUD search page (VenuesPage, ArtistsPage, SongsPage, PersonsPage). For UX consistency, the same error state should be retrofitted to all app search pages. Track which pages already have it once Search Page Component ships, then standardize. |
| 2026-06 | **Code Cleanup — CRUD List Page Deduplication** | 💡 Pending | 4 code-behinds + 4 ViewModels share ~57% identical code. Plan: `Docs/Management/DevCycleCraft/crud-list-deduplication/plan.md`. Approach: `ICrudListViewModel` interface + `CrudListPageBase` (abstract ContentPage, events for XAML elements) + `CrudListViewModelBase<TItem>` (abstract generic ViewModel, abstract methods). Est. -890 lines. |
| 2026-06 | ↳ Step 1: Implement shared infrastructure | 🔴 Blocked | Blocked on parent plan approval. Create `ICrudListViewModel`, `CrudListPageBase`, `CrudListViewModelBase<TItem>` — no page migrations yet. Build must be green before Step 2 starts. |
| 2026-06 | ↳ Step 2: Migrate VenuesPage + VenuesViewModel | 🔴 Blocked | Blocked on Step 1. First migration — acts as proof-of-concept. Build + emulator smoke test required before proceeding. |
| 2026-06 | ↳ Step 3: Migrate PeoplePage + PersonsViewModel | 🔴 Blocked | Blocked on Step 2 green. |
| 2026-06 | ↳ Step 4: Migrate SongsPage + SongsViewModel | 🔴 Blocked | Blocked on Step 3 green. SongsPage has extra `OnItemTapped` — must be preserved. |
| 2026-06 | ↳ Step 5: Migrate ArtistsPage + ArtistsViewModel | 🔴 Blocked | Blocked on Step 4 green. ArtistsViewModel has filter chips + `ViewCatalogCommand` + `GoBackCommand` — must be preserved as entity-specific. |
| 2026-06 | ↳ Step 6: Post-migration guideline review | 🔴 Blocked | Blocked on Step 5 green. Review `.claude/library/crud-pages.md` and any other CLAUDE.md / rules files that document CRUD page patterns. Update to reflect the new `CrudListPageBase` + `CrudListViewModelBase<TItem>` canonical pattern so future agents start from the correct baseline. |
| 2026-06 | **DB-Side Collation — Remove All Normalized Columns** | ✅ Done | **Decided direction (Helder, 2026-06-01):** All accent+case normalization must be handled by the database, never by C# code. (1) Drop all `*Normalized` shadow columns (`Artist.NameNormalized`, `Song.TitleNormalized`, `Person.FullNameNormalized`) — only the original display field survives. (2) UNIQUE indexes (e.g. song title per artist) must be defined on the original column with the collation applied, so the DB enforces uniqueness accent+case insensitively. (3) All queries (search, duplicate checks, autocomplete) must rely on the DB collation — no `ToLowerInvariant()` or `RemoveDiacritics()` in service or repository code. (4) Collation registration must be abstracted via EF Core configuration so that adding a second DB provider (MySQL, MSSQL, PostgreSQL) requires only a provider-specific collation name — no business logic changes. Currently `NOCASE_NOACCENT` is a custom SQLite collation registered via `CollationInterceptor`; the pattern must be designed to swap the collation name per provider. Spec folder: `Docs/Management/DevCycleCraft/db-collation-normalization/`. |
