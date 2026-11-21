# MyVocaList — Product Backlog

> Generated view of the active items, ordered by target. Each row comes from the item's `README.md` frontmatter: **never hand-edit a row** (the next `regen` overwrites it; pre-commit blocks a stale file). Goal, gate and folder of an item are in its `README.md`. Agents do not read this file: they run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"`. Closed items: `backlog-archive/`. Post-MVP items: `BACKLOG-POST-MVP.md`. Commands, frontmatter rules and status meanings: `.claude/library/backlog-generator.md`.

Statuses: 💡 Pending → 📋 Spec → 🗺️ Plan → 🟢 Ready → 🟡 In Progress → ✅ Done · 🔵 Deferred · 🔴 Blocked

## Business Features

<!-- BACKLOG:GENERATED:BEGIN business-features -->
| Target | Feature | Status |
|--------|---------|--------|
| — | **Artists & Songs Catalog** | 🟡 In Progress |
| 2026-11-11 | ↳ Discuss showing long song titles in list items (MD3-compliant, secondary text line) | 💡 Pending |
| 2026-11-11 | ↳ Discuss: song identity by lyric, version de-duplication and richer Deezer data in the song UX | 💡 Pending |
| 2026-11-12 | ↳ Artists list rows: catalog action moves into the overflow menu | 💡 Pending |
| 2026-11-12 | ↳ Songs list rows: YouTube action moves into the overflow menu, checkbox moves leading | 💡 Pending |
| 2026-11-13 | ↳ Backfill Deezer images for songs and artists saved before the artwork item | 💡 Pending |
| 2026-11-16 | ↳ Songs list searches Deezer: one tap saves a thin song row, lyrics copy with edit mark | 💡 Pending |
| 2026-11-17 | ↳ Remote and local field searches show a loading indicator | 💡 Pending |
| 2026-11-21 | ↳ Artist catalog: add cover songs by hand (catalog picker) | 💡 Pending |
| — | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 💡 Pending |
| 2026-11-19 | ↳ Catalog as an auto-filled grouped grid, filled from karaoke participations | 💡 Pending |
| — | | 🏁 **MVP release** |
| — | **Music sourcing strategy — Deezer / LRCLIB / YouTube / Bandokê / BYO Audio** | 💡 Pending |
| — | **Cross-cutting** | — |
| 2026-11-19 | ↳ Duplicate singer saved when the name is typed without picking the existing one | 🟡 In Progress |
<!-- BACKLOG:GENERATED:END business-features -->

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->
| Target | Activity | Status |
|--------|----------|--------|
| — | Large-volume data stress test (1–2 year seed) | 💡 Pending |
| — | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending |
| — | Full pre-release mobile testing checklist (all categories) | 💡 Pending |
| — | ↳ CRUD list app bar in selection mode: Songs shows title plus count, the other lists only the count | 💡 Pending |
| — | ↳ ListItem: empty slots not rendered, content fills the freed width; spacing aligned to MD3 List - Common tokens | 💡 Pending |
| — | **UI component library licensing — DevExpress 25.2 is paid; evaluate Syncfusion Community** | 💡 Pending |
<!-- BACKLOG:GENERATED:END dev-cycle-craft -->
