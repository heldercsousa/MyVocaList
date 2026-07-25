# BACKLOG Archive — 2026-06

> Closed backlog rows completed in 2026-06, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. Full technical narratives were relocated verbatim into the feature docs named in each pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | Bug: QueuePage BottomSheet double-add on navigation (under: **Queue Entry Point Redesign — QueuePage as CRUD event list**) | ✅ Fixed | Goal: Fixed via safe ConfirmSheet wrapper; fix branch merged to develop and confirmed present in code (audit 2026-07-15). E2E still blocked until the Queue Entry Point Redesign ships an entry point. Pointer: `BusinessFeatures/queue-management/bugs/2026-06-01-BUG-011-queuepage-bottomsheet-double-add/`. |
| 2026-06 | **Search Picker** | ✅ Done | Goal: standalone search picker pages for form fields. Shipped (3 picker pages, tests, DI/routes, guidelines). Pointer: `BusinessFeatures/search-picker/`. |
| 2026-06-03 | Bug: Artists page missing back button + unclear trailing toggle (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed 2026-06-03. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-03-BUG-001-artists-page-no-back-button/`. |
| 2026-06-27 | BUG-015: ArtistsPage trailing ViewCatalog button no-op (Major) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed (binding resolution inside DataTemplate). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-27-BUG-015-artistspage-trailing-button-noop/`. |
| 2026-06 | Bug: Artist/Song form search strip non-MD3 (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed via the Search Picker feature (dedicated picker pages). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-002-artist-form-search-non-md3/`. |
| 2026-06 | Search Picker sub-row: ArtistPickerPage | ✅ Done | Goal: Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06-27 | BUG-016: SongsPage FAB crash on Add tap — route collision (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed (route rename + regression test). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-27-BUG-016-songspage-fab-crash/`. |
| 2026-06 | Search Picker sub-row: SongPickerPage | ✅ Done | Goal: Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | Bug/Gap: SongFormPage Artist field autocomplete with blur-clear (BUG-008) (under: **Artists & Songs Catalog**) | 🔵 Superseded (closed 2026-06) | Goal: Originally fixed with blur-clear; the Artist & Song Form UX Redesign reverses that behavior and owns the field — no independent action. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-008-songform-artist-autocomplete/`. |
| 2026-06 | Search Picker sub-row: YouTubeSearchPage | ✅ Done | Goal: Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | **Visual Theme Refresh** | ✅ Done | Goal: apply the Karaoke Neon palette across the app's material colors and add matching neon glow shadow styles. Pointer: `Docs/Changelog/changelog.md`. |
| 2026-06 | Search Picker sub-row: Update search picker coding guidelines | ✅ Done | Goal: New search-picker coding guidelines file created. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06-27 | BUG-017: form pages `navigate_next` icon missing — Glide exception per render (Major) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed (icon replaced with an existing SVG); emulator-verified 2026-07-03. Duplicate BACKLOG row consolidated here. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/`. |
| 2026-06 | Bug: New Song - Save has no effect (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed by Song Import & Entity Resolution Wave 4B. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-005-new-song-save-broken/`. |
| 2026-06-27 | BUG-018: ArtistFormPage Edit Save — fatal EF Core duplicate-tracking crash (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed (global NoTracking + read models); regression test green. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-018-artistformpage-edit-save-crash/`. |
| 2026-06 | Bug: New Song - double-tap on search link crashes app (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed by Wave 4A (concurrency guard). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-006-search-song-double-tap-crash/`. |
| 2026-06-30 | BUG-019: ArtistsPage list item trailing button no-op + name invisible (Major) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Name visibility fix holds; the trailing-button regression is re-tracked as active BUG-028. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/`. |
| 2026-06 | Bug: SearchAppBar duplicate back arrow in picker (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed by Wave 4A. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-007-searchappbar-duplicate-back-arrow/`. |
| 2026-06 | Bug/UX: Add YouTube URL before song saved shows blocking validation (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed by Wave 4B (atomic song+URL save). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-009-add-url-before-save-ux/`. |
| 2026-06 | Bug/Gap: Song API auto-fill never functional (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed by Wave 4A (Deezer + MusicBrainz wired, external ids persisted). Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-010-song-api-autofill-broken/`. |
| 2026-06 | Fuzzy entity matching for API import (BUG-010 follow-up) | ✅ Fixed | Goal: Subsumed into Song Import & Entity Resolution. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-06-20 | Spec cleanup - post-Phase-2 reconciliation | ✅ Done | Goal: 6 spec-vs-code gaps fixed 2026-06-23. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-06 | **YouTube Search Launch Button** | ✅ Done | Goal: one-tap YouTube karaoke search from song surfaces. Shipped. Pointer: `BusinessFeatures/artists-songs/youtube-search-launch/`. |
| 2026-06 | **Crash & Error Reporting** | ✅ Done | Goal: production crash/error telemetry. Shipped (2 review bugs fixed). Pointer: `BusinessFeatures/crash-reporting/`. |
| 2026-06 | **What's New / Release Notes** | ✅ Done | Goal: one-time modal on version upgrade. Shipped. Pointer: `BusinessFeatures/whats-new/`. |
| 2026-06 | User Suggestions | ✅ Done | Goal: in-app suggestion form to GitHub Issues. Shipped; Helder gate: add fine-grained GitHub PAT to appsettings.json. Pointer: `BusinessFeatures/user-suggestions/task-log.md`. |
| 2026-06 | App Update Check | ✅ Done | Goal: remote version manifest with soft/hard update prompts. Shipped; Helder gate: maintain manifest versions + real App Store ID. Pointer: `BusinessFeatures/app-update-check/task-log.md`. |
| 2026-06 | **App Settings** | ✅ Done | Goal: settings page incl. YouTube API key management. Shipped. Pointer: `BusinessFeatures/app-settings/`. |
| 2026-06 | **About Page** | ✅ Done | Goal: version/license/about surface. Shipped. Pointer: `BusinessFeatures/about-page/`. |
| 2026-06 | Queue Management | ✅ Done | Goal: core product — active queue, rounds, registration, absences, time estimate. Shipped, all 5 waves + tests. Pointer: `BusinessFeatures/queue-management/task-log.md`. |
| 2026-06 | Data Backup & Restore — Tier 1 + 3 | ✅ Done | Goal: local auto-backup + manual export/restore. Shipped. Pointer: `BusinessFeatures/backup-restore/task-log.md`. |
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | Bug: "To Review tasks need attention" rewakes every session (Stop hook noise) | ✅ Fixed | Goal: remove the To-Review-tasks scanner from the Stop hook (session-continuity-leasing). Scanner removed 2026-06-27. Pointer: `DevCycleCraft/session-continuity-leasing/task-log.md`. |
| 2026-06 | **Enforce Git Worktrees for Parallel Subagents** | ✅ Done | Goal: mandatory rule for git worktree isolation across parallel subagents. Pointer: `cross-cutting/worktree-enforcement/`. |
| 2026-06-04 | Steps 1-7e (shared infra, 4 page migrations, XAML sharing, guidelines) | ✅ Done | Goal: all sub-steps completed 2026-06-04 through 2026-06-06; smoke-tested by Helder. Pointer: `DevCycleCraft/crud-list-deduplication/task-log.md`. |
| 2026-06 | **Orchestrator Role Enforcement — Root Cause Investigation** | ✅ Done | Goal: close the negative-space gap where rules forbade the orchestrator writing code but never reading source. Pointer: `cross-cutting/orchestrator-role-enforcement/`. |
| 2026-06 | **Bug Tracking Procedure** | ✅ Done | Goal: establish BUG-NNN ids, BACKLOG nesting, severity classification, and per-severity regression-test requirements. Pointer: `cross-cutting/bug-tracking-procedure/`. |
| 2026-06 | **Haiku Model Assignment for Low-Risk Subagent Tasks** | ✅ Done | Goal: route low-risk subagent tasks (docs-only, migration-only, boilerplate) to the Haiku model by default. Pointer: `cross-cutting/haiku-model-assignment/`. |
| 2026-06 | **Component Change Governance Rule** | ✅ Done | Goal: require four gates (dedicated task + MD3 review, consumer map, per-consumer risk assessment, Helder approval) before any shared custom-component change. Pointer: `cross-cutting/component-change-governance/`. |
| 2026-06 | **MD3/DevExpress Compliance Gap — Internal Guidelines** | ✅ Done | Goal: pre-implementation DX component audit checklist to catch missing MD3-compliant component usage before code review. Pointer: `cross-cutting/md3-devexpress-compliance-gap/`. |
| 2026-06-14 | ~~SongsPage Filter Chips~~ → ArtistsPage Filter Chips (under: **Artists & Songs Catalog**) | 🔵 Duplicate (closed 2026-06) | Goal: misattribution corrected — SongsPage has no filter surface; duplicate of the Artists CRUD List filter fix. Closed as duplicate, no separate work. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-14-BUG-003-songpage-filter-chips/`. |
| 2026-06-14 | MD3 Non-Compliance: BottomSheetTitle Style Missing (under: **Artists & Songs Catalog**) | ✅ Done | Goal: already resolved — BottomSheetTitle style added to the central style resource file under BUG-004/song-import-resolution Wave 4A; verified on develop 2026-06-14. Pointer: `BusinessFeatures/artists-songs/bugs/2026-06-14-BUG-004-bottomsheet-title-style-missing/`. |
| 2026-06 | **Code Cleanup — CRUD List Page Deduplication** | ✅ Done | Goal: deduplicate 4 CRUD list pages (~890 lines). Shipped via base classes + CrudListView (Steps 1-7e all done). Pointer: `DevCycleCraft/crud-list-deduplication/task-log.md`. |
| 2026-06 | **DB-Side Collation — Remove All Normalized Columns** | ✅ Done | Goal: all accent+case normalization handled by the database, never by C# code — normalized shadow columns dropped, collation-based unique indexes, no C#-side string normalization. Pointer: `cross-cutting/db-side-collation/`. |
| 2026-06 | **Navigation Icon Pattern — Root Pages vs Pushed Pages** | ✅ Done | Goal: standardize leading AppBar icon: root flyout pages show hamburger; pushed detail pages show back arrow. Dynamic icon shipped in CrudListPageBase. Pointer: `cross-cutting/navigation-icon-pattern/`. |
| 2026-06 | **Page load frozen** | ✅ Done | Goal: unfreeze page loads (sync SQLite calls on the UI thread). Fixed via thread-pool offload plus a load gate. Pointer: `DevCycleCraft/page-load-frozen/task-log.md`. |
| 2026-06 | **Artists CRUD List filter issue** | ✅ Done | Goal: fix the Author/Performer FilterChipGroup not rendering in ArtistsPage — CrudListView filter slot hosting bug. Fixed; Helder gate: emulator smoke test of the chips. Pointer: `cross-cutting/artists-crud-filter-fix/`. |
| 2026-06-12 | Add missing queue_music_outlined icon asset | ✅ Done | Goal: add the missing queue_music_outlined icon asset (Material Symbols queue_music outlined), consumed by ArtistsPage. Merged to develop. Pointer: `cross-cutting/queue-music-icon-asset/`. |
| 2026-06-30 | 01 - Form validation guide | ✅ Done | Goal: validation standard encoded across .claude/library rule files (guide + 5 form updates shipped); two Helder gates open (DateEdit birthday confirm; Integer field TODO). Pointer: `DevCycleCraft/ui-form-validation-guide/task-log.md`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
