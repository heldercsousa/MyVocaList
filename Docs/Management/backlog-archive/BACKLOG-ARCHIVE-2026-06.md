# BACKLOG Archive — 2026-06

> Closed backlog rows completed in 2026-06, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. Full technical narratives were relocated verbatim into the feature docs named in each pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06 | ↳ Bug: Artists page missing back button + unclear trailing toggle (BUG-001) | ✅ Fixed | Fixed 2026-06-03. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-001-artists-page-no-back-button.md`. |
| 2026-06 | ↳ Bug: Artist/Song form search strip non-MD3 (BUG-002) | ✅ Fixed | Fixed via the Search Picker feature (dedicated picker pages). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-002-artist-form-search-non-md3.md`. |
| 2026-06-27 | ↳ BUG-015: ArtistsPage trailing ViewCatalog button no-op (Major) | ✅ Fixed | Fixed (binding resolution inside DataTemplate). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-015-artistspage-trailing-button-noop.md`. |
| 2026-06-27 | ↳ BUG-016: SongsPage FAB crash on Add tap — route collision (Critical) | ✅ Fixed | Fixed (route rename + regression test). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-016-songspage-fab-crash.md`. |
| 2026-06-27 | ↳ BUG-017: form pages `navigate_next` icon missing — Glide exception per render (Major) | ✅ Fixed | Fixed (icon replaced with an existing SVG); emulator-verified 2026-07-03. Duplicate BACKLOG row consolidated here. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/`. |
| 2026-06-27 | ↳ BUG-018: ArtistFormPage Edit Save — fatal EF Core duplicate-tracking crash (Critical) | ✅ Fixed | Fixed (global NoTracking + read models); regression test green. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-018-artistformpage-edit-save-crash/`. |
| 2026-06-30 | ↳ BUG-019: ArtistsPage list item trailing button no-op + name invisible (Major) | Closed — partially regressed | Name visibility fix holds; the trailing-button regression is re-tracked as active BUG-028. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/`. |
| 2026-06 | **Search Picker** | ✅ Done | Goal: standalone search picker pages for form fields. Shipped (3 picker pages, tests, DI/routes, guidelines). Pointer: `BusinessFeatures/search-picker/`. |
| 2026-06 | ↳ ArtistPickerPage | ✅ Done | Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | ↳ SongPickerPage | ✅ Done | Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | ↳ YouTubeSearchPage | ✅ Done | Shipped. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | ↳ Update search picker coding guidelines | ✅ Done | `.claude/library/search-picker-pattern.md` created. Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | ↳ Bug: New Song — Save has no effect (BUG-005) | ✅ Fixed | Fixed by Song Import & Entity Resolution Wave 4B. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-005-new-song-save-broken.md`. |
| 2026-06 | ↳ Bug: New Song — double-tap on search link crashes app (BUG-006) | ✅ Fixed | Fixed by Wave 4A (concurrency guard). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-006-search-song-double-tap-crash.md`. |
| 2026-06 | ↳ Bug: SearchAppBar duplicate back arrow in picker (BUG-007) | ✅ Fixed | Fixed by Wave 4A. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-007-searchappbar-duplicate-back-arrow.md`. |
| 2026-06 | ↳ Bug/UX: Add YouTube URL before song saved shows blocking validation (BUG-009) | ✅ Fixed | Fixed by Wave 4B (atomic song+URL save). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-009-add-url-before-save-ux.md`. |
| 2026-06 | ↳ Bug/Gap: Song API auto-fill never functional (BUG-010) | ✅ Fixed | Fixed by Wave 4A (Deezer + MusicBrainz wired, external ids persisted). Pointer: `BusinessFeatures/artists-songs/bugs/BUG-010-song-api-autofill-broken.md`. |
| 2026-06 | ↳ Fuzzy entity matching for API import (BUG-010 follow-up) | ✅ Fixed | Subsumed into Song Import & Entity Resolution. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-06 | ↳ Bug: QueuePage BottomSheet double-add on navigation (BUG-011) | ✅ Fixed | Fixed via safe ConfirmSheet wrapper; fix branch merged to develop and confirmed present in code (audit 2026-07-15). E2E still blocked until the Queue Entry Point Redesign ships an entry point. Pointer: `BusinessFeatures/queue-management/bugs/BUG-011-queuepage-bottomsheet-double-add.md`. |
| 2026-06-20 | ↳ Spec cleanup — post-Phase-2 reconciliation | ✅ Done | 6 spec-vs-code gaps fixed 2026-06-23. Pointer: `BusinessFeatures/artists-songs/spec-cleanup-p2.md`. |
| 2026-06 | ↳ YouTube Search Launch Button | ✅ Done | Goal: one-tap YouTube karaoke search from song surfaces. Shipped. Pointer: `BusinessFeatures/artists-songs/youtube-search-launch/`. |
| 2026-06 | **Crash & Error Reporting** | ✅ Done | Goal: production crash/error telemetry. Shipped (2 review bugs fixed). Pointer: `BusinessFeatures/crash-reporting/`. |
| 2026-06 | **What's New / Release Notes** | ✅ Done | Goal: one-time modal on version upgrade. Shipped. Pointer: `BusinessFeatures/whats-new/`. |
| 2026-06 | **User Suggestions** | ✅ Done | Goal: in-app suggestion form to GitHub Issues. Shipped; Helder gate: add fine-grained GitHub PAT to `appsettings.json`. Pointer: `BusinessFeatures/user-suggestions/task-log.md`. |
| 2026-06 | **App Update Check** | ✅ Done | Goal: remote version manifest with soft/hard update prompts. Shipped; Helder gate: maintain manifest versions + real App Store ID. Pointer: `BusinessFeatures/app-update-check/task-log.md`. |
| 2026-06 | **App Settings** | ✅ Done | Goal: settings page incl. YouTube API key management. Shipped. Pointer: `BusinessFeatures/app-settings/`. |
| 2026-06 | **About Page** | ✅ Done | Goal: version/license/about surface. Shipped. Pointer: `BusinessFeatures/about-page/`. |
| 2026-06 | **Queue Management** | ✅ Done | Goal: core product — active queue, rounds, registration, absences, time estimate. Shipped, all 5 waves + tests. Pointer: `BusinessFeatures/queue-management/`. |
| 2026-06 | **Visual Theme Refresh** | ✅ Done | Goal: Karaoke Neon palette + glow styles. Shipped (applied in `MaterialColors.xaml`/`MaterialStyles.xaml`). Pointer: `Docs/Changelog/changelog.md`. |
| 2026-06 | **Data Backup & Restore — Tier 1 + 3** | ✅ Done | Goal: local auto-backup + manual export/restore. Shipped. Pointer: `BusinessFeatures/backup-restore/`. |

## Dev Cycle Craft

| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-06 | **Enforce Git Worktrees for Parallel Subagents** | ✅ Done | Goal: worktree isolation for parallel subagents. Rule encoded in `orchestrator.md`. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Orchestrator Role Enforcement — Root Cause Investigation** | ✅ Done | Goal: stop the orchestrator reading source files. Read-scope HARD RULE added 2026-06-15. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Bug Tracking Procedure** | ✅ Done | Goal: BUG-NNN scheme + severity + regression-test rules. Rule shipped (`.claude/rules/bug-tracking.md`). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Haiku Model Assignment for Low-Risk Subagent Tasks** | ✅ Done | Goal: cheaper model for low-risk tasks. Encoded in `orchestrator.md § Model Selection`. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Component Change Governance Rule** | ✅ Done | Goal: four-gate governance for shared components. Rule shipped (`.claude/rules/component-change-governance.md`). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **MD3/DevExpress Compliance Gap — Internal Guidelines** | ✅ Done | Goal: pre-implementation DX component audit checklist. Encoded in library rule files. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | ~~SongsPage Filter Chips~~ → ArtistsPage Filter Chips | 🔵 Duplicate (closed) | Closed 2026-06-14 as duplicate of the Artists CRUD List filter fix. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **MD3 Non-Compliance: BottomSheetTitle Style Missing** | ✅ Done | Already resolved under BUG-004/Wave 4A; verified 2026-06-14. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Code Cleanup — CRUD List Page Deduplication** | ✅ Done | Goal: deduplicate 4 CRUD list pages (~890 lines). Shipped via base classes + `CrudListView` (Steps 1–7e all done). Pointer: `Docs/Management/DevCycleCraft/crud-list-deduplication/task-log.md`. |
| 2026-06 | ↳ Steps 1–7e (shared infra, 4 page migrations, XAML sharing, guidelines) | ✅ Done | All sub-steps completed 2026-06-04/06; smoke-tested by Helder. Pointer: `Docs/Management/DevCycleCraft/crud-list-deduplication/task-log.md`. |
| 2026-06 | **DB-Side Collation — Remove All Normalized Columns** | ✅ Done | Goal: all accent/case normalization in the DB, no C#-side normalization. Shipped; HARD RULE encoded in `constraints-registry.md`. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Navigation Icon Pattern — Root Pages vs Pushed Pages** | ✅ Done | Goal: hamburger on root pages, back arrow on pushed pages. Shipped (dynamic icon in `CrudListPageBase`). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Page load frozen** | ✅ Done | Goal: unfreeze page loads (sync SQLite on UI thread). Fixed 2026-06-10 (thread-pool offload + load gate). Pointer: `Docs/Management/DevCycleCraft/page-load-frozen/`. |
| 2026-06 | **Artists CRUD List filter issue** | ✅ Done | Fixed 2026-06-14 (FilterContent host); Helder gate: emulator smoke test of the chips. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-12 | Add missing queue_music_outlined icon asset | ✅ Done | SVG added and merged; emulator smoke test pending. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-27 | ↳ BUG: "To Review tasks need attention" rewakes every session (Stop hook noise) | ✅ Fixed | Scanner removed from Stop hooks 2026-06-27. Pointer: `Docs/Management/DevCycleCraft/session-continuity-leasing/task-log.md`. |
| 2026-06-30 | ↳ 01 - Form validation guide | ✅ Done | Validation standard encoded across `.claude/library/*`; two Helder gates open (DateEdit birthday confirm; Integer/R10 TODO). Pointer: `Docs/Management/DevCycleCraft/ui-form-validation-guide/`. |
