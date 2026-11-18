# MyVocaList — Product Backlog

> Generated view of the active items, ordered by target. Each row comes from the item's `README.md` frontmatter: **never hand-edit a row** (the next `regen` overwrites it; pre-commit blocks a stale file). Goal, gate and folder of an item are in its `README.md`. Agents do not read this file: they run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"`. Closed items: `backlog-archive/`. Post-MVP items: `BACKLOG-POST-MVP.md`. Commands, frontmatter rules and status meanings: `.claude/library/backlog-generator.md`.

Statuses: 💡 Pending → 📋 Spec → 🗺️ Plan → 🟢 Ready → 🟡 In Progress → ✅ Done · 🔵 Deferred · 🔴 Blocked

## Business Features

<!-- BACKLOG:GENERATED:BEGIN business-features -->
| Target | Feature | Status |
|--------|---------|--------|
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked |
| 2026-08-04 | ↳ Song writes propagate to the artist catalog | 📋 Spec |
| 2026-11-11 | ↳ Discuss showing long song titles in list items (MD3-compliant, secondary text line) | 💡 Pending |
| 2026-11-11 | ↳ Discuss: song identity by lyric, version de-duplication and richer Deezer data in the song UX | 💡 Pending |
| 2026-11-11 | ↳ Evaluate removing the Song form Version and Featured artists fields | 💡 Pending |
| 2026-11-12 | ↳ Artists list rows: catalog action moves into the overflow menu | 💡 Pending |
| 2026-11-12 | ↳ Data model simplification — merge Artist into Person, catalog as an auto-filled grid (discussion) | 💡 Pending |
| 2026-11-12 | ↳ Songs list rows: YouTube action moves into the overflow menu, checkbox moves leading | 💡 Pending |
| 2026-11-13 | ↳ Backfill Deezer images for songs and artists saved before the artwork item | 💡 Pending |
| 2026-11-16 | ↳ Songs list searches Deezer: one tap saves a thin song row, lyrics copy with edit mark | 💡 Pending |
| 2026-11-16 | ↳ Songs list searches title and artist; Deezer versions collapsed; Deezer artist frozen; lyrics on first open | 🟡 In Progress |
| 2026-11-17 | ↳ Remote and local field searches show a loading indicator | 💡 Pending |
| 2026-06 | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 💡 Pending |
| 2026-06 | | 🏁 **MVP release** |
| — | **Music sourcing strategy — Deezer / LRCLIB / YouTube / Bandokê / BYO Audio** | 💡 Pending |
| 2026-07-03 | **Cross-cutting** | — |
| 2026-11-17 | ↳ Debug builds hold the loading shimmer for a while | 🟡 In Progress |
| 2026-11-06 | ↳ Person form follow-ups from ACF T7 run 2 | 💡 Pending |
| 2026-11-12 | ↳ People list rows: leading checkbox, trailing overflow button, no empty leading inset | 💡 Pending |
| 2026-11-12 | ↳ Venues list rows: leading checkbox, trailing overflow button, no empty leading inset | 💡 Pending |
| 2026-11-17 | ↳ Venue duplicates: name and address together; same name at another address allowed | 🟡 In Progress |
<!-- BACKLOG:GENERATED:END business-features -->

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->
| Target | Activity | Status |
|--------|----------|--------|
| 2026-07-12 | **Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception** | 🟡 In Progress |
| 2026-11-03 | ↳ Post-ship review — did the agent-dispatch recalibration meet its expectations | 💡 Pending |
| 2026-06-12 | Large-volume data stress test (1–2 year seed) | 💡 Pending |
| — | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending |
| — | Full pre-release mobile testing checklist (all categories) | 💡 Pending |
| — | ↳ Screen coverage — keyboard, safe area and snackbars hiding inputs and bottom bars | 💡 Pending |
| 2026-11-12 | ↳ MD3 overflow menu for list item actions | 💡 Pending |
| 2026-11-18 | ↳↳ ImagePickerField: bigger centred picture, tap to pick, corner remove button | 🟢 Ready |
| — | ↳ CRUD list app bar in selection mode: Songs shows title plus count, the other lists only the count | 💡 Pending |
| — | ↳ Move the Playwright MCP token to .env.local | 💡 Pending |
| — | ↳ Rotate the DevExpress license key | 💡 Pending |
| — | ↳ ListItem: empty slots not rendered, content fills the freed width; spacing aligned to MD3 List - Common tokens | 💡 Pending |
| — | ↳ Always-loaded context: path-scoped rules spike and superpowers plugin cost | 💡 Pending |
<!-- BACKLOG:GENERATED:END dev-cycle-craft -->
