# T12a — Remaining Waves Plan (2026-05 / 2026-06 / 2026-07)

Plan artifact only — no code/folders touched by producing this document. Written by a Plan
subagent per Helder's brief 2026-07-23. Read against branch `feature/backlog-migration`,
worktree `mvl-backlog-migration`.

## Baseline verified before planning

- **Total archived rows across the 5 files: 105** (03=6, 04=4, 05=11, 06=47, 07=37) —
  matches the T12a-inventory figure in `tasks.md`.
- **Already backed by a folder (exclude from this plan):**
  - Wave A (2026-03, 6 rows) — committed `b862248`.
  - Wave B (2026-04, 4 rows) — committed (validator-downgrade session, `fix(backlog-gen)` /
    `docs(spec-evolution)`), *not* "cross-month READMEs" as the brief assumed — verified directly
    against `task-log.md`; Wave B is a plain 2026-04 month-wave like A.
  - T10a's 8 archived bug rows (folders already exist, confirmed via `git ls-files`):
    **BUG-017, BUG-018, BUG-019, BUG-021, BUG-022, BUG-023, BUG-024, bug-043** (`022`/`043` are
    the two archived-in-DevCycleCraft-table bugs T10a's log calls out explicitly).
  - `bug-043`'s folder is `DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-043-release-build-zero-suggestions/` — lowercase, no
    date, no slug: **REQ-SEV-01 naming debt, out of scope for T12a, tracked at item (vi) in
    `tasks.md`. Do not re-plan a folder for it; only a future `git mv` fixes the name.**
  - **BUG-015 and BUG-016 are NOT done** — despite being 2026-06 archived rows adjacent to
    017-019/021/023/024, they still exist only as flat files
    (`BUG-015-artistspage-trailing-button-noop.md`, `BUG-016-songspage-fab-crash.md`) with no
    folder. T10a's "9 of 9" count did not include them. **Included below, Wave D.**
- **Remaining rows to fold into new waves: 105 minus 10 (A+B) minus 8 (T10a archived bugs) = 87.**
- **`.sln` GUID audit:** highest `FA1234BC-0001-4000-8000-00000000NNNN` in the tree today is
  **`007E`** (Wave-B-unblock's `changes`/`2026-04-01-autocomplete-field` pair) — confirmed by
  regex scan of the whole file, not assumed. **Next free GUID is `007F`**, matching the brief.
  Each wave below consumes GUIDs sequentially from there; running total noted per wave.

## Row-kind legend

- **(i) feature/activity** -> new `README.md` dropped into an **already-existing** folder (spec
  files present, no frontmatter yet) unless noted "new folder".
- **(ii) archived bug** -> new `bugs/YYYY-MM-DD-BUG-NNN-slug/README.md`; **git mv** the flat file
  in if one exists (noted per row), else the folder is created fresh (no flat file exists for
  BUG-010/036/040/041/042/044/045/047 - those rows only ever pointed at a shared task-log).
- **(iii) F-1 log-pointer** -> `changes/<slug>/README.md`; family **(a)** `cross-cutting/<slug>/`
  for `cross-cutting-log.md` pointers, family **(b)** `<parent-feature>/changes/<slug>/` for a
  shared feature `task-log.md`/`form-validation-task-log.md`. `pointer:` stays on the shared log
  (REQ-SEV-27, nothing deleted).
- **(iv) NEW GAP, not in F-1's taxonomy** — rows point at `.claude/rules/*.md` or
  `Docs/Changelog/changelog.md` (governance/changelog files), not a Docs item log or a folder.
  Flagged as blockers below, not planned into a wave.

`closed:` = the archive file's month for every row (REQ-SEV-18/20). `-01` day rule (REQ-SEV-00)
applies to every invented folder date (F-1/F-5 shapes); rows that already carry a day in their
`Target` cell use that day verbatim.

---

## Wave C — 2026-05, part 1 of 3 (5 folders)

| # | Title (archive) | Status | Kind | Target folder | id | order | Authored? |
|---|---|---|---|---|---|---|---|
| 1 | Song Karaoke URLs (Artists & Songs Catalog) | Done | (i) | `BusinessFeatures/artists-songs/youtube-karaoke/README.md` (folder exists) | `youtube-karaoke` | 10 | order |
| 2 | Bug: GoToSettings navigation exception | Fixed | (iii-b) | `BusinessFeatures/artists-songs/changes/2026-05-01-gotosettings-navigation-fix/README.md` | `gotosettings-navigation-fix` | 20 | slug+title+order (no BUG-NNN, unnumbered fix, pointer stays on task-log.md) |
| 3 | SDD Master Plan (Phases 1-11, 162 steps) | Done | (i) | `DevCycleCraft/sdd/README.md` (folder exists, no frontmatter) | `sdd` | 10 | order |
| 4 | workflow.md Reduction | Done | (i) | `DevCycleCraft/workflow-compression/README.md` (folder exists) | `workflow-compression` | 20 | order |
| 5 | CLAUDE.md Deep Restructure | Done | (i) | `DevCycleCraft/docs-context-scope-control/README.md` (folder exists) | `docs-context-scope-control` | 30 | order |

`.sln`: GUIDs `007F`-`0083` (5 new SolutionItems lines on 4 existing folders + 1 new `changes`
Solution Folder + 1 nested item folder for row 2). Running high-water mark after Wave C: `0083`.

## Wave D — 2026-05, part 2 of 3 (5 folders) + the 2 stray 2026-06 bug rows T10a missed

| # | Title | Status | Kind | Target folder | id | order | Authored? |
|---|---|---|---|---|---|---|---|
| 1 | Architecture Tests Evaluation | Done | (i) | `DevCycleCraft/architecture-tests-evaluation/README.md` | `architecture-tests-evaluation` | 40 | order |
| 2 | Claude Managed Agents Evaluation | Done | (i) | `DevCycleCraft/claude-managed-agents-evaluation/README.md` | `claude-managed-agents-evaluation` | 50 | order |
| 3 | Day-to-day task management workflow review | Done | (i) | `DevCycleCraft/backlog-workflow-integration/README.md` | `backlog-workflow-integration` | 60 | order |
| 4 | App Versioning Strategy | Done | (i) | `DevCycleCraft/app-versioning/README.md` | `app-versioning` | 70 | order |
| 5 | BUG-015: ArtistsPage trailing ViewCatalog button no-op (Major) | Fixed | (ii) | `BusinessFeatures/artists-songs/bugs/2026-06-27-BUG-015-artistspage-trailing-button-noop/README.md` - git mv the existing flat `BUG-015-artistspage-trailing-button-noop.md` in | `bug-015-artistspage-trailing-button-noop` | 10 (2026-06 table pos 3) | order |

`.sln`: GUIDs `0084`-`0088`. Running high-water mark: `0088`.

**Note:** row 5 pulls one row forward from the 2026-06 file to close out the T10a gap while its
sibling BUG-016 is next - kept together deliberately so Wave E doesn't split the "T10a missed
these two" story across two commits/reviews.

## Wave E — 2026-05, part 3 of 3 (1 folder) + BUG-016; open items 10/11 held back (see Blockers)

| # | Title | Status | Kind | Target folder | id | order | Authored? |
|---|---|---|---|---|---|---|---|
| 1 | BUG-016: SongsPage FAB crash - route collision (Critical) | Fixed | (ii) | `BusinessFeatures/artists-songs/bugs/2026-06-27-BUG-016-songspage-fab-crash/README.md` - git mv the existing flat `BUG-016-songspage-fab-crash.md` in | `bug-016-songspage-fab-crash` | 20 | order |

`.sln`: GUIDs `0089`. Running high-water mark: `0089`.

**2026-05 rows 10-11 NOT planned into a wave - see Blockers.** They point at
`.claude/rules/constraints-registry.md` and `.claude/rules/workflow.md`, not a Docs log or a
folder; no F-1 sub-case covers "pointer targets a rules file". Wave E is intentionally short
(1 folder) rather than padded with unresolved rows.

---

## Wave F-N — 2026-06 (41 remaining rows -> 9 waves of <=5)

Reading order = the archive table's own row order (top to bottom), Business Features before Dev
Cycle Craft, per REQ-SEV-17/frozen-snapshot. `order` continues the Wave-A/B convention
(10-per-row, restarting per table/parent-feature run).

### Wave F (5) — Search Picker family (all kind i/iii-b, one parent)

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Search Picker | Done | (i) | `BusinessFeatures/search-picker/README.md` (folder exists, no frontmatter) | `search-picker` | 10 |
| 2 | Search Picker sub-row: ArtistPickerPage | Done | (iii-b) | `BusinessFeatures/search-picker/changes/2026-06-01-artist-picker-page/README.md` | `artist-picker-page` | 20 |
| 3 | Search Picker sub-row: SongPickerPage | Done | (iii-b) | `BusinessFeatures/search-picker/changes/2026-06-01-song-picker-page/README.md` | `song-picker-page` | 30 |
| 4 | Search Picker sub-row: YouTubeSearchPage | Done | (iii-b) | `BusinessFeatures/search-picker/changes/2026-06-01-youtube-search-page/README.md` | `youtube-search-page` | 40 |
| 5 | Search Picker sub-row: Update search picker coding guidelines | Done | (iii-b) | `BusinessFeatures/search-picker/changes/2026-06-01-coding-guidelines-update/README.md` | `coding-guidelines-update` | 50 |

Rows 2-5: **slug + title agent-authored** (F-1b - sub-rows of one `task-log.md`, per the
briefing's ~22-row family-b estimate). `.sln` GUIDs `008A`-`0090` (1 existing-folder line + 4 new
`changes`-item Solution Folders under one new `changes` parent). Running mark: `0090`.

### Wave G (5) — artists-songs bug backlog, part 1

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Bug: Artists page missing back button + unclear trailing toggle (BUG-001) | Fixed | (ii) | `bugs/2026-06-03-BUG-001-artists-page-no-back-button/README.md` - git mv flat file | `bug-001-artists-page-no-back-button` | 10 |
| 2 | Bug: Artist/Song form search strip non-MD3 (BUG-002) | Fixed | (ii) | `bugs/2026-06-01-BUG-002-artist-form-search-non-md3/README.md` - git mv flat file | `bug-002-artist-form-search-non-md3` | 20 |
| 3 | Bug: New Song - Save has no effect (BUG-005) | Fixed | (ii) | `bugs/2026-06-01-BUG-005-new-song-save-broken/README.md` - git mv flat file | `bug-005-new-song-save-broken` | 60 |
| 4 | Bug: New Song - double-tap on search link crashes app (BUG-006) | Fixed | (ii) | `bugs/2026-06-01-BUG-006-search-song-double-tap-crash/README.md` - git mv flat file | `bug-006-search-song-double-tap-crash` | 70 |
| 5 | Bug: SearchAppBar duplicate back arrow in picker (BUG-007) | Fixed | (ii) | `bugs/2026-06-01-BUG-007-searchappbar-duplicate-back-arrow/README.md` - git mv flat file | `bug-007-searchappbar-duplicate-back-arrow` | 80 |

All target `BusinessFeatures/artists-songs/bugs/...`. **Day-01 authored** for every row whose
`Target` cell is bare `2026-06` (rows 2-5); row 1 keeps its own `2026-06-03`. `.sln` GUIDs
`0091`-`0095`. Running mark: `0095`.

### Wave H (5) — artists-songs bug backlog, part 2 + BUG-010 pair

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Bug/UX: Add YouTube URL before song saved shows blocking validation (BUG-009) | Fixed | (ii) | `bugs/2026-06-01-BUG-009-add-url-before-save-ux/README.md` - git mv flat file | `bug-009-add-url-before-save-ux` | 90 |
| 2 | Bug/Gap: Song API auto-fill never functional (BUG-010) | Fixed | (ii) | `bugs/2026-06-01-BUG-010-song-api-autofill-broken/README.md` - **no flat file exists; net-new folder** (verify at wave time before assuming) | `bug-010-song-api-autofill-broken` | 100 |
| 3 | Fuzzy entity matching for API import (BUG-010 follow-up) | Fixed | (iii-b) | `BusinessFeatures/artists-songs/changes/2026-06-01-fuzzy-entity-matching-import/README.md` | `fuzzy-entity-matching-import` | 110 |
| 4 | Bug: QueuePage BottomSheet double-add on navigation (BUG-011) | Fixed | (ii) | `BusinessFeatures/queue-management/bugs/2026-06-01-BUG-011-queuepage-bottomsheet-double-add/README.md` - git mv flat file | `bug-011-queuepage-bottomsheet-double-add` | 10 |
| 5 | Spec cleanup - post-Phase-2 reconciliation | Done | (iii-b) | `BusinessFeatures/artists-songs/changes/2026-06-20-spec-cleanup-p2/README.md` | `spec-cleanup-p2` | 120 |

Row 3's slug/title agent-authored (F-1b "follow-up" sub-row of a bug row, not itself numbered).
`.sln` GUIDs `0096`-`009A`. Running mark: `009A`.

### Wave I (5) — remaining artists-songs feature rows

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | YouTube Search Launch Button | Done | (i) | `BusinessFeatures/artists-songs/youtube-search-launch/README.md` (folder exists) | `youtube-search-launch` | 130 |
| 2 | Crash & Error Reporting | Done | (i) | `BusinessFeatures/crash-reporting/README.md` (folder exists) | `crash-reporting` | 140 |
| 3 | What's New / Release Notes | Done | (i) | `BusinessFeatures/whats-new/README.md` (folder exists) | `whats-new` | 150 |
| 4 | User Suggestions | Done | (iii-b) | `BusinessFeatures/user-suggestions/changes/2026-06-01-github-issues-integration/README.md` - pointer is task-log.md, not the folder itself; slug agent-authored | `github-issues-integration` | 160 |
| 5 | App Update Check | Done | (iii-b) | `BusinessFeatures/app-update-check/changes/2026-06-01-remote-update-manifest/README.md` - pointer is task-log.md; slug agent-authored | `remote-update-manifest` | 170 |

`.sln` GUIDs `009B`-`009F`. Running mark: `009F`.

### Wave J (5) — final 2026-06 Business rows

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | App Settings | Done | (i) | `BusinessFeatures/app-settings/README.md` (folder exists) | `app-settings` | 180 |
| 2 | About Page | Done | (i) | `BusinessFeatures/about-page/README.md` (folder exists) | `about-page` | 190 |
| 3 | Queue Management | Done | (i) | `BusinessFeatures/queue-management/README.md` (folder exists) | `queue-management` | 200 |
| 4 | Visual Theme Refresh | Done | (iv, blocked) | `cross-cutting/visual-theme-refresh/README.md` - pointer is `Docs/Changelog/changelog.md`, not a folder; F-1-adjacent case, slug/title/section agent-authored - **held, see Blockers** | `visual-theme-refresh` | 210 |
| 5 | Data Backup & Restore - Tier 1 + 3 | Done | (i) | `BusinessFeatures/backup-restore/README.md` (folder exists) | `backup-restore` | 220 |

Row 4 is the **second instance of the (iv) gap** (pointer targets a file outside
`Docs/Management` item space) - same open question as the two 2026-05 rules-file rows. Wave J
ships only 4 real folders until Helder resolves it; the 5th slot is provisional. `.sln` GUIDs
`00A0`-`00A3` (4 real).

### Wave K (5) — 2026-06 Dev Cycle Craft, F-1a log-pointer batch 1

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Enforce Git Worktrees for Parallel Subagents | Done | (iii-a) | `cross-cutting/worktree-enforcement/README.md` - cf. existing, unrelated `cross-cutting/mandatory-worktree-rule-enforcement/` (a live item, different id - do NOT reuse/merge) | `worktree-enforcement` | 10 |
| 2 | Orchestrator Role Enforcement - Root Cause Investigation | Done | (iii-a) | `cross-cutting/orchestrator-role-enforcement/README.md` | `orchestrator-role-enforcement` | 20 |
| 3 | Bug Tracking Procedure | Done | (iii-a) | `cross-cutting/bug-tracking-procedure/README.md` | `bug-tracking-procedure` | 30 |
| 4 | Haiku Model Assignment for Low-Risk Subagent Tasks | Done | (iii-a) | `cross-cutting/haiku-model-assignment/README.md` | `haiku-model-assignment` | 40 |
| 5 | Component Change Governance Rule | Done | (iii-a) | `cross-cutting/component-change-governance/README.md` | `component-change-governance` | 50 |

All 5 slugs agent-authored (F-1a, part of the 21-row cross-cutting-log.md family). **Collision
check required at wave time:** confirm none of these ids collide with the 19 pre-existing live
`cross-cutting/*` folders enumerated during this planning pass (none do today, but Wave K must
re-check since Waves F-J may not have touched `cross-cutting/`). `.sln` GUIDs `00A4`-`00A8`.

### Wave L (5) — F-1a batch 2

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | MD3/DevExpress Compliance Gap - Internal Guidelines | Done | (iii-a) | `cross-cutting/md3-devexpress-compliance-gap/README.md` | `md3-devexpress-compliance-gap` | 60 |
| 2 | ~~SongsPage Filter Chips~~ -> ArtistsPage Filter Chips (BUG-003) | Duplicate (closed) | (ii) | `BusinessFeatures/artists-songs/bugs/2026-06-14-BUG-003-songpage-filter-chips/README.md` - git mv flat `BUG-003-songpage-filter-chips.md`; status Duplicate needs T12-pre's extended STATUSES (already shipped `e7b29a5`) | `bug-003-songpage-filter-chips` | 70 |
| 3 | MD3 Non-Compliance: BottomSheetTitle Style Missing (BUG-004) | Done | (ii) | `BusinessFeatures/artists-songs/bugs/2026-06-14-BUG-004-bottomsheet-title-style-missing/README.md` - git mv flat file | `bug-004-bottomsheet-title-style-missing` | 80 |
| 4 | Code Cleanup - CRUD List Page Deduplication | Done | (i) | `DevCycleCraft/crud-list-deduplication/README.md` (folder exists) | `crud-list-deduplication` | 90 |
| 5 | Steps 1-7e (shared infra, 4 page migrations, XAML sharing, guidelines) | Done | (iii-b) | `DevCycleCraft/crud-list-deduplication/changes/2026-06-04-steps-1-7e-migration/README.md` | `steps-1-7e-migration` | 10 |

Row 2's Craft-table item is filed in the Business Features bug tree (its subject is an
Artists-page UI bug), matching the BUG-022/Wave-B cross-file precedent; `section: DevCycleCraft`
set explicitly. `.sln` GUIDs `00A9`-`00AD`.

### Wave M (5) — F-1a batch 3 + standalone activities

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | DB-Side Collation - Remove All Normalized Columns | Done | (iii-a) | `cross-cutting/db-side-collation/README.md` | `db-side-collation` | 100 |
| 2 | Navigation Icon Pattern - Root Pages vs Pushed Pages | Done | (iii-a) | `cross-cutting/navigation-icon-pattern/README.md` | `navigation-icon-pattern` | 110 |
| 3 | Page load frozen | Done | (i) | `DevCycleCraft/page-load-frozen/README.md` (folder exists) | `page-load-frozen` | 120 |
| 4 | Artists CRUD List filter issue | Done | (iii-a) | `cross-cutting/artists-crud-filter-fix/README.md` | `artists-crud-filter-fix` | 130 |
| 5 | Add missing queue_music_outlined icon asset | Done | (iii-a) | `cross-cutting/queue-music-icon-asset/README.md` | `queue-music-icon-asset` | 140 |

`.sln` GUIDs `00AE`-`00B2`.

### Wave N (1) — final 2026-06 row

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | 01 - Form validation guide | Done | (i) | `DevCycleCraft/ui-form-validation-guide/README.md` (folder exists) | `ui-form-validation-guide` | 150 |

`.sln` GUID `00B3`. **Excluded from this wave (needs its own check, not resolved here):** the
"BUG: To Review tasks need attention rewakes every session" row - `session-continuity-leasing/`
already has a README (prior work), but that row's own pointer
(`session-continuity-leasing/task-log.md`) may still need a dedicated `changes/` sub-item; this
plan pass did not open that README to check its scope, so it is **not counted in the 87** and must
be re-triaged (could add one more row to Wave N or a new micro-wave).

---

## Wave O-U — 2026-07 (35 remaining rows -> 7 waves of <=5)

### Wave O (5) — 2026-07 Business bugs, part 1

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | BUG-020: SongsPage FAB crash - unguarded SecureStorage (Critical) | Fixed | (ii) | `BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-020-songspage-fab-crash-secure-storage/README.md` - git mv flat file | `bug-020-songspage-fab-crash-secure-storage` | 10 |
| 2 | Bug/Gap: SongFormPage Artist field autocomplete with blur-clear (BUG-008) | Superseded (closed 2026-07-10) | (ii) | `BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-008-songform-artist-autocomplete/README.md` - git mv flat file; `closed: 2026-06` (the row's own supersession date) even though listed in the 07 archive file - **REQ-SEV-18 routing edge case, confirm at wave time which file's fence it round-trips through** | `bug-008-songform-artist-autocomplete` | 40 |
| 3 | Song form -> stays full-screen page + AppBar-save pattern | Done | (i) | `DevCycleCraft/crud-form-action-pattern/README.md` (folder exists) | `crud-form-action-pattern` | 50 |
| 4 | Hamburger menu on all hamburger-loaded pages (CRUD-only scope) | Done | (i) | `DevCycleCraft/hamburger-nav-pattern/README.md` (folder exists) | `hamburger-nav-pattern` | 60 |
| 5 | BUG-036: PersonFormPage birthday validation rejects masked input (Major) | Fixed | (ii) | `BusinessFeatures/persons/bugs/2026-07-03-BUG-036-personformpage-birthday-mask/README.md` - no flat file; net-new (pointer was always the shared form-validation-task-log.md) | `bug-036-personformpage-birthday-mask` | 10 (Craft-table run) |

Rows 1-2 target `BusinessFeatures/artists-songs/bugs/...`; BUG-023/024 (Critical, same table) are
**already done** (T10a) and excluded here. `.sln` GUIDs `00B4`-`00B8`.

### Wave P (4 real + 1 provisional) — autocomplete-component F-1b bug cluster

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Branch-lock avoidance - orchestrator must also work in worktrees | Done | (iii-a) | `cross-cutting/branch-lock-avoidance/README.md` | `branch-lock-avoidance` | 20 |
| 2 | CRUD Form Action Pattern - MD3 Save/Cancel placement (Craft-table row) | Done | (i, dup pointer) | **SAME target as Wave-O row 3** (`DevCycleCraft/crud-form-action-pattern/`) - do not create two READMEs for one folder. Reconcile at wave time per the F-5 Venues precedent (distinct id/order, or a changes/ split) - **held, see Blockers** | `crud-form-action-pattern` (provisional) | - |
| 3 | AutocompleteField Component Evaluation - Adjust or Replace | Done | (iii-b) | `DevCycleCraft/autocomplete-component/changes/2026-07-11-component-evaluation/README.md` | `component-evaluation` | 10 |
| 4 | Apply new component to the simplest candidate | Done | (iii-b) | `DevCycleCraft/autocomplete-component/changes/2026-07-11-apply-to-simplest-candidate/README.md` | `apply-to-simplest-candidate` | 20 |
| 5 | BUG-040: mobile autocomplete input loses focus (Major) | Fixed | (ii) | `DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-040-mobile-input-loses-focus/README.md` - no flat file, net-new | `bug-040-mobile-input-loses-focus` | 30 |

Wave P ships **4 real folders** (row 2 held per the Blockers list). `.sln` GUIDs `00B9`-`00BC`.

### Wave Q (4) — autocomplete-component bug cluster, cont'd + form-validation start

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | BUG-041: mobile Search View cannot be dismissed; duplicates on back (Critical) | Fixed | (ii) | `.../autocomplete-component/bugs/2026-07-12-BUG-041-search-view-duplicate-on-back/README.md` - no flat file, net-new | `bug-041-search-view-duplicate-on-back` | 40 |
| 2 | BUG-042: every back tap repeats the reappear/duplicate cycle (Critical) | Fixed | (ii) | `.../autocomplete-component/bugs/2026-07-12-BUG-042-back-tap-repeat-cycle/README.md` - no flat file, net-new | `bug-042-back-tap-repeat-cycle` | 50 |
| 3 | 02 - Update Venues form (validation) | Done | (iii-b) | `BusinessFeatures/venues/changes/2026-06-30-form-validation-update/README.md`, pointer `venues/form-validation-task-log.md` (venues/README.md already exists from Wave A - this is the sub-item, not the parent) | `form-validation-update` | 60 |
| 4 | 03 - Update Singer form (validation) | Done | (iii-b) | `BusinessFeatures/persons/changes/2026-06-30-form-validation-update/README.md`, pointer `persons/form-validation-task-log.md` | `form-validation-update` | 20 |

Note: BUG-022 (2026-07-01 row, "SingerForm birthday field mask missing") appears in the archive
text but is **already done** (T10a) - excluded, not part of the 87. `.sln` GUIDs `00BD`-`00C0`.

### Wave R (5) — form-validation sub-rows, cont'd + cross-cutting

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | 04 - Update Songs form (validation) | Done | (iii-b) | `BusinessFeatures/artists-songs/changes/2026-06-30-form-validation-update-songs/README.md`, pointer `artists-songs/form-validation-task-log.md` | `form-validation-update-songs` | 30 |
| 2 | 05 - Update Artists form (validation) | Done | (iii-b) | `BusinessFeatures/artists-songs/changes/2026-06-30-form-validation-update-artists/README.md`, same pointer - **needs a slug distinct from row 1; both archive rows share one Target/table position, disambiguation is agent-authored** | `form-validation-update-artists` | 40 |
| 3 | 06 - Character-counter threshold alignment | Done | (iii-a) | `cross-cutting/character-counter-threshold-alignment/README.md` - pointer is `ui-form-validation-guide/task-log.md`, cross-feature scope | `character-counter-threshold-alignment` | 160 |
| 4 | Local enforcement automations (solo, pre-prod) | Done | (iii-a) | `cross-cutting/local-enforcement-automations/README.md` | `local-enforcement-automations` | 170 |
| 5 | Scope myvocalist-coding skill to project level | Done | (iii-a) | `cross-cutting/myvocalist-coding-skill-scoping/README.md` | `myvocalist-coding-skill-scoping` | 180 |

`.sln` GUIDs `00C1`-`00C5`.

### Wave S (5) — 2026-07 Craft, rules-file refactoring + secrets cluster

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Rules File Refactoring - Reduce Unconditional Load | Done | (i) | `DevCycleCraft/rules-file-refactoring/README.md` (folder exists) | `rules-file-refactoring` | 190 |
| 2 | Rules File Refactoring sub-tasks (SPIKE, 01-18, GATE-A/B, AUDIT) | Done | (iii-b) | `DevCycleCraft/rules-file-refactoring/changes/2026-07-04-spike-01-18-gate-audit/README.md` | `spike-01-18-gate-audit` | 10 |
| 3 | SECURITY - rotate + de-commit secrets in .mcp.json | Done | (iii-a) | `cross-cutting/mcp-secrets-rotation/README.md` | `mcp-secrets-rotation` | 200 |
| 4 | MCP governance sync + Docs housekeeping | Done | (iii-a) | `cross-cutting/mcp-governance-docs-housekeeping/README.md` | `mcp-governance-docs-housekeeping` | 210 |
| 5 | HELDER MANUAL ACTIONS (reminder) | Done | (iii-a) | `cross-cutting/helder-manual-actions-2026-07-09/README.md` - date-suffixed slug since the title is generic and could recur | `helder-manual-actions-2026-07-09` | 220 |

`.sln` GUIDs `00C6`-`00CA`.

### Wave T (5) — final Craft cluster

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | Tool-registry cleanup: context-budget plugin, duplicate review, exa MCP | Done | (iii-a) | `cross-cutting/tool-registry-cleanup/README.md` | `tool-registry-cleanup` | 230 |
| 2 | Per-Agent MCP/Skill Context Isolation | Done | (i) | `DevCycleCraft/per-agent-context-isolation/README.md` (folder exists) | `per-agent-context-isolation` | 240 |
| 3 | Search AppBar Pattern - Root Page + Search Interaction | Superseded (closed 2026-07-10) | (iii-a) | `cross-cutting/search-appbar-pattern/README.md` - status needs T12-pre's Superseded (shipped) | `search-appbar-pattern` | 250 |
| 4 | BACKLOG.md purpose review - restore it as a PO-level business artifact | Done | (i) | `DevCycleCraft/backlog-purpose-review/README.md` - **folder not found in `git ls-files`; verify/create at wave time** | `backlog-purpose-review` | 260 |
| 5 | BUG-044: duplicate PersonFormPage after Save via autocomplete (Critical) | Superseded (closed 2026-07-19) | (ii) | `.../autocomplete-component/bugs/2026-07-15-BUG-044-duplicate-personformpage-after-save/README.md` - no flat file, net-new; status Superseded | `bug-044-duplicate-personformpage-after-save` | 60 |

`.sln` GUIDs `00CB`-`00CF`.

### Wave U (2, final wave — closes T12a)

| # | Title | Status | Kind | Target | id | order |
|---|---|---|---|---|---|---|
| 1 | BUG-045: name entry cursor stuck at leading position after autocomplete usage (Major) | Superseded (closed 2026-07-19) | (ii) | `.../autocomplete-component/bugs/2026-07-15-BUG-045-cursor-stuck-after-autocomplete/README.md` - no flat file, net-new | `bug-045-cursor-stuck-after-autocomplete` | 70 |
| 2 | BUG-047: stale autocomplete suggestions popup on Edit-mode load (Major) | Superseded (closed 2026-07-19) | (ii) | `.../autocomplete-component/bugs/2026-07-15-BUG-047-stale-suggestions-popup-edit-mode/README.md` - no flat file, net-new | `bug-047-stale-suggestions-popup-edit-mode` | 80 |

`.sln` GUIDs `00D0`-`00D1`. **After Wave U, every archived row across all 5 files has a backing
folder (modulo the Blockers list below) - T12a's own gate is met and T12 (archive regeneration +
equivalence gate) can start.**

---

## Blockers requiring a Helder decision before their wave ships

1. **(iv) Rule-file / changelog pointers, 3 rows total** - 2026-05 "VS Solution File Registration
   Rule" (`.claude/rules/constraints-registry.md`), 2026-05 "Proactive BACKLOG Entry Rule"
   (`.claude/rules/workflow.md`), 2026-06 "Visual Theme Refresh" (`Docs/Changelog/changelog.md`).
   None of F-1's two families cover a pointer that targets a file **outside** `Docs/Management`'s
   item tree. Proposed in this plan (as `cross-cutting/<slug>/README.md`, pointer unchanged) but
   **not decided** - held out of Waves C/E/J. Inventing a new F-1 sub-case is exactly the kind of
   agent-authored-shape decision the T12a PLANNING block asks to be surfaced before code, not
   after.
2. **CRUD Form Action Pattern duplicate pointer (Wave O/P reconciliation)** - the 2026-07-10 Craft
   row and the 2026-07-11 Business row both describe the same shipped change and may collide on
   one folder (`crud-form-action-pattern/`), the same shape as the Wave-A Venues F-5 case. Needs
   the same kind of resolution (distinct id+order, or a `changes/` split) - **not resolved**,
   flagged rather than decided in Waves O/P.
3. **BUG-008's cross-month filing** - archived in the **2026-07** file's Business table but
   `closed: 2026-06` (superseded 2026-07-10, its own text says the underlying fix predates that).
   REQ-SEV-18/20 routing needs a decision on which file's fence backs it before Wave O ships.
4. **The `session-continuity-leasing` "To Review tasks" row** (2026-06 Craft, last table row) -
   this plan pass did not open that folder's README to check whether the archived row needs its
   own `changes/` sub-item or is already covered; re-triage before Wave N/a follow-up wave.
5. **No Minor-severity bug found among the remaining 87 rows** - BUG-022 (the only Minor) is
   already folder-backed and reclassified Major (decision 3A, done). **No REQ-SEV-03 blocker
   exists in this remaining set** - explicitly checked, not merely assumed.

## Agent-authored field count (for the gate audit set)

- **F-1a slugs+titles (cross-cutting-log.md family):** ~19-22 across Waves J-T (Waves K/L/M/R/S/T
  each contribute several: worktree-enforcement, orchestrator-role-enforcement,
  bug-tracking-procedure, haiku-model-assignment, component-change-governance,
  md3-devexpress-compliance-gap, db-side-collation, navigation-icon-pattern,
  artists-crud-filter-fix, queue-music-icon-asset, character-counter-threshold-alignment,
  local-enforcement-automations, myvocalist-coding-skill-scoping, mcp-secrets-rotation,
  mcp-governance-docs-housekeeping, helder-manual-actions-2026-07-09, tool-registry-cleanup,
  search-appbar-pattern, branch-lock-avoidance - 19 confirmed, plus the 3 blocked (iv) rows'
  pre-drafted slugs if Helder approves the cross-cutting placement = up to 22).
- **F-1b slugs+titles (shared feature task-log family):** ~19 across Waves C, F, H, N(pending),
  P-R (Search-Picker x4, crud-list-dedup Steps, autocomplete-component evaluation/apply,
  form-validation-update x4, spike-01-18-gate-audit, fuzzy-entity-matching-import,
  spec-cleanup-p2, gotosettings-navigation-fix, github-issues-integration,
  remote-update-manifest).
- **Combined F-1 agent-authored total: ~38-41**, not the ~22 the T12a PLANNING block estimated
  for family (b) alone. The original estimate only flagged family (b); it did not anticipate
  family (a)'s ~19-21 rows also needing invented slugs. **Flag this discrepancy explicitly for
  Helder** rather than silently reconciling the count - it roughly doubles the audited-slug
  surface from the PLANNING block's stated expectation.
- **`order` values agent-assigned:** all 87 rows across every wave C-U - this repeats the Wave A/B
  pattern where `order` is agent-assigned for the entire batch, not a subset.
- **Goals:** transcribed verbatim from each row's Notes cell throughout (same method as Waves
  A/B) - zero net-new agent-authored Goal text expected in this remaining set (no
  Windows-version/banned-content-class rows remain undone), but each wave's implementor must
  re-verify per row against `model._BANNED`, the way Waves A/B did, rather than assume clean.

## Wave-sequencing summary

| Wave | Month | Folders | Contains F-1 rows? |
|---|---|---|---|
| C | 2026-05 | 5 | 1 (iii-b) |
| D | 2026-05 | 5 | 0 |
| E | 2026-05 | 1 | 0 |
| F | 2026-06 | 5 | 4 (iii-b) |
| G | 2026-06 | 5 | 0 |
| H | 2026-06 | 5 | 2 (iii-b) |
| I | 2026-06 | 5 | 2 (iii-b) |
| J | 2026-06 | 4 real (+1 blocked) | 1 (iv, blocked) |
| K | 2026-06 | 5 | 5 (iii-a) |
| L | 2026-06 | 5 | 1 (iii-a) |
| M | 2026-06 | 5 | 3 (iii-a) |
| N | 2026-06 | 1 (+1 to re-triage) | 0 |
| O | 2026-07 | 5 | 0 |
| P | 2026-07 | 4 real (+1 blocked) | 3 (iii-a/b) |
| Q | 2026-07 | 4 | 2 (iii-b) |
| R | 2026-07 | 5 | 4 (iii-a/b) |
| S | 2026-07 | 5 | 4 (iii-a/b) |
| T | 2026-07 | 5 | 2 (iii-a) |
| U | 2026-07 | 2 | 0 |

**19 waves, 85 real folders planned + 2 held pending Helder decisions + 1 to re-triage (item 4
above), against 87 total remaining rows.** Heaviest F-1 log-pointer waves (heaviest authoring +
audit burden): **K** (5/5 rows are F-1a), **F** (4/5), **R** and **S** (4/5 each).
