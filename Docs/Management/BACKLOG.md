# MyVocaList — Product Backlog

> Generated view of the active items, ordered by target. Each row comes from the item's `README.md` frontmatter: **never hand-edit a row** (the next `regen` overwrites it; pre-commit blocks a stale file). Goal, gate and folder of an item are in its `README.md`. Agents do not read this file: they run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"`. Closed items: `backlog-archive/`. Post-MVP items: `BACKLOG-POST-MVP.md`. Commands, frontmatter rules and status meanings: `.claude/library/backlog-generator.md`.

Statuses: 💡 Pending → 📋 Spec → 🗺️ Plan → 🟢 Ready → 🟡 In Progress → ✅ Done · 🔵 Deferred · 🔴 Blocked

## Business Features

<!-- BACKLOG:GENERATED:BEGIN business-features -->
| Target | Feature | Status |
|--------|---------|--------|
| 2026-03 | ↳ Bug: Venues list fetch slow — 2.2s paged query (BUG-012) | 💡 Pending |
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked |
| 2026-07-21 | ↳ **Song artist field — correctness fixes + inline "create new artist"** | 🟡 In Progress |
| 2026-07-03 | ↳ BUG-030: ArtistFormPage search strip UX unclear (spec gap) | 🔵 Deferred |
| 2026-07-10 | ↳ **Artist & Song Form UX Redesign — autocomplete, similar-match warning, search-strip removal** | 🔵 Deferred |
| 2026-08-04 | ↳ Song writes propagate to the artist catalog | 📋 Spec |
| 2026-11-09 | ↳ Song list item shows artist name, version and featured artists | 🟡 In Progress |
| 2026-11-11 | ↳ ArtistFormPage Name as the autocomplete field (split from SFT 6.3) | 🟡 In Progress |
| 2026-11-11 | ↳ Discuss showing long song titles in list items (MD3-compliant, secondary text line) | 💡 Pending |
| 2026-11-11 | ↳ Discuss: song identity by lyric, version de-duplication and richer Deezer data in the song UX | 💡 Pending |
| 2026-11-11 | ↳ Evaluate removing the Song form Version and Featured artists fields | 💡 Pending |
| 2026-11-11 | ↳ Song title limit raised from 100 to 250 characters | 💡 Pending |
| 2026-11-12 | ↳ Artists list rows: catalog action moves into the overflow menu | 💡 Pending |
| 2026-11-12 | ↳ Data model simplification — merge Artist into Person, catalog as an auto-filled grid (discussion) | 💡 Pending |
| 2026-11-12 | ↳ Songs list rows: YouTube action moves into the overflow menu, checkbox moves leading | 💡 Pending |
| 2026-11-13 | ↳ Backfill Deezer images for songs and artists saved before the artwork item | 💡 Pending |
| 2026-06 | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 💡 Pending |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending |
| 2026-06 | **Website** | 💡 Pending |
| 2026-06 | | 🏁 **MVP release** |
| — | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending |
| 2026-08-04 | ↳ RestoreFromBundleAsync overwrites the live SQLite file while AppDbContext holds it open | 💡 Pending |
| 2026-11-13 | ↳ Backups include uploaded pictures | 💡 Pending |
| — | **Singer self-registration** | 💡 Pending |
| — | **Music sourcing strategy — Deezer / LRCLIB / YouTube / Bandokê / BYO Audio** | 💡 Pending |
| — | **Social features** | 💡 Pending |
| 2026-07-03 | **Cross-cutting** | — |
| 2026-07-03 | ↳ BUG-026: HWUI native crash (SIGABRT) on render teardown (Major) | 💡 Pending |
| 2026-09-19 | ↳ DbLoadGate removal dropped its _currentPage serialisation role | 💡 Pending |
| 2026-11-06 | ↳ Person form follow-ups from ACF T7 run 2 | 💡 Pending |
| 2026-11-12 | ↳ People list rows: leading checkbox, trailing overflow button, no empty leading inset | 💡 Pending |
| 2026-11-12 | ↳ Venues list rows: leading checkbox, trailing overflow button, no empty leading inset | 💡 Pending |
<!-- BACKLOG:GENERATED:END business-features -->

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->
| Target | Activity | Status |
|--------|----------|--------|
| 2026-07-12 | **Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception** | 🟡 In Progress |
| 2026-11-03 | ↳ Post-ship review — did the agent-dispatch recalibration meet its expectations | 💡 Pending |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending |
| 2026-06-27 | **Mandatory Worktree Rule Enforcement — ALL Subagent Work** | 💡 Pending |
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending |
| 2026-07-19 | **`MyVocaList.Extensions` layer guidelines — placement criteria + rules-file promotion** | 💡 Pending |
| 2026-06 | **Bug: Shell navigation swallows button tap animations** | 💡 Pending |
| 2026-06-12 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 💡 Pending |
| — | **UI-2nd-refactor** | 📋 Spec |
| 2026-06-12 | Large-volume data stress test (1–2 year seed) | 💡 Pending |
| 2026-11-04 | ↳ Dev and QA data tooling — seed and empty app data on demand | 💡 Pending |
| 2026-06-12 | Cross-device / OS version compatibility test | 💡 Pending |
| 2026-06-12 | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending |
| 2026-06-12 | Full pre-release mobile testing checklist (all categories) | 💡 Pending |
| 2026-11-04 | ↳ Screen coverage — keyboard, safe area and snackbars hiding inputs and bottom bars | 💡 Pending |
| 2026-06-13 | **Session Continuity — Task Leasing & Auto-Resume** | 🟡 In Progress |
| 2026-06-27 | **Infra Repository Folder Consolidation** | 💡 Pending |
| 2026-06-27 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 💡 Pending |
| 2026-08-03 | ↳ DbContext lifetime & unit-of-work pattern — MAUI has no per-page scope | 🟡 In Progress |
| 2026-08-18 | ↳ Flaky ObjectDisposedException in the UOW SQLite test harness | 💡 Pending |
| 2026-08-24 | ↳ Service read paths still use the captive AppDbContext; one of them tracks | 💡 Pending |
| 2026-06-27 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 💡 Pending |
| 2026-06-30 | **Form validation** | 🟡 In Progress |
| 2026-08-04 | ↳ Pre-commit test gate blocks the RED-first commit that bug-tracking mandates | 💡 Pending |
| 2026-08-18 | ↳ Pre-commit hook inoperative in worktrees: dotnet test without --no-restore aborts on NETSDK1147 | 💡 Pending |
| 2026-09-18 | ↳ Pre-commit hook: allow TDD Red commits without --no-verify | 💡 Pending |
| 2026-11-03 | ↳ Move the Playwright MCP token to .env.local | 💡 Pending |
| 2026-11-03 | ↳ Rotate the DevExpress license key | 💡 Pending |
| 2026-11-09 | ↳ Release tag discipline — mechanically enforced tag creation | 💡 Pending |
| 2026-11-11 | ↳ AutocompleteDXField is the app-wide autocomplete pattern, whatever page uses it | 💡 Pending |
| 2026-11-12 | ↳↳ Correct the MD3 list, selection and toolbar rules in the library guidelines with verbatim official quotes | 💡 Pending |
| 2026-11-12 | ↳ CRUD list app bar in selection mode: Songs shows title plus count, the other lists only the count | 💡 Pending |
| 2026-11-12 | ↳ CRUD list FAB: placement, size and color when alone and when paired with the floating toolbar | 💡 Pending |
| 2026-11-12 | ↳ CrudListView: one list mode at a time (multi-action browse, multi-select selection) with toolbar Edit for exactly one | 💡 Pending |
| 2026-11-12 | ↳ List item overflow icon button in the ListItem trailing slot, opening the item actions menu | 💡 Pending |
| 2026-11-12 | ↳ MD3 overflow menu for list item actions | 💡 Pending |
| 2026-11-12 | ↳ ListItem: empty slots not rendered, content fills the freed width; spacing aligned to MD3 List - Common tokens | 💡 Pending |
| 2026-11-15 | ↳ Always-loaded context: path-scoped rules spike and superpowers plugin cost | 💡 Pending |
<!-- BACKLOG:GENERATED:END dev-cycle-craft -->
