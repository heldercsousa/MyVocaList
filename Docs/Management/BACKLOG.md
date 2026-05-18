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
| 2026-05 | **Artists & Songs Catalog** | 🟡 In Progress | Spec: `Docs/Management/BusinessFeatures/artists-songs/` · Plan: `Docs/Management/BusinessFeatures/artists-songs/plan.md` |
| 2026-05 | ↳ Song Karaoke URLs | 🟡 In Progress | YouTube URL management per song; next-singer alert; nested feature. Spec: `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/` |
| 2026-06 | **Queue Management** | 💡 Pending | Core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate |
| 2026-06 | **Visual Theme Refresh** | 💡 Pending | App UI is too dark and monochromatic; spike for richer accent colors, gradient surfaces, warmer tones |
| 2026-06 | | 🏁 **MVP release** | |
| — | **Singer self-registration** | 💡 Pending | Singers register via public link |
| — | **Social features** | 💡 Pending | Post-event sharing, singer stats |

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
| 2026-04 | Hooks redesign | ✅ Done | Stop/TaskCreated/TaskCompleted hooks; session-end auto-commit |
| 2026-05-07 | SDD Master Plan (Phases 1–11, 162 steps) | ✅ Done | `Docs/DevEnv/SDD/plans/impl/MASTER_PLAN.md` |
| 2026-05-07 | workflow.md Reduction | ✅ Done | Agent role files (`implementor.md`, `orchestrator.md`) absorb scattered responsibilities; 61 findings resolved |
| 2026-05-08 | CLAUDE.md Deep Restructure | ✅ Done | 7 coding rule files → `.claude/library/`; `.claudeignore` Docs/ scope gates |
| 2026-05-13 | Day-to-day task management workflow review | ✅ Done | BACKLOG.md first-class SCRUM board; workflow.md Rule 1/7 updated |
| 2026-05 | **App Versioning Strategy** | 🗺️ Plan | MinVer NuGet; git-tag-driven semver; `/project:release` command. Spec: `Docs/Management/DevCycleCraft/app-versioning/design.md` · Plan: `Docs/Management/DevCycleCraft/app-versioning/plan.md` |
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Resolve conflicts between SDD, superpowers skills, and custom rules; canonicalize Docs/ layout; spec evolution tracking; review enforcement. Findings: `Docs/DevEnv/workflow-layout-findings.md` |
