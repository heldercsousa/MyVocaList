# MyVocaList — Product Backlog

> **SCRUM product backlog.** Items are priority-ordered — top = most important. Every feature begins here as a brief idea and is promoted through the lifecycle below before any code is written.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.
>
> **MVP scope:** All items above the `── MVP scope ends here ──` marker are in-scope for the MVP release. Items below are post-MVP.

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

## MVP Features

| Feature | Status | Notes |
|---------|--------|-------|
| **Venues CRUD** | ✅ Done | Full MD3 list, search, multi-select, swipe-delete |
| **Person CRUD** | ✅ Done | Autocomplete field, duplicate detection |
| **Artists & Songs Catalog** | 🟡 In Progress | Spec: `Docs/specs/artists-songs/` · Plan: `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` |
| ↳ Song Karaoke URLs | 🟡 In Progress | YouTube URL management per song; next-singer alert; nested feature. Spec: `Docs/specs/youtube-karaoke/` |
| **Queue Management** | 💡 Pending | Core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate |
| **Visual Theme Refresh** | 💡 Pending | App UI is too dark and monochromatic; spike for richer accent colors, gradient surfaces, warmer tones |

---
> ── MVP scope ends here ──
---

## 🗺️ Plan — Infrastructure

- **App Versioning Strategy** `🗺️ Plan` — git-tag-driven semver via MinVer NuGet; `ApplicationDisplayVersion` and `ApplicationVersion` bound to MSBuild properties; `/project:release` command; version-bump prompt in `/project:commit`
  → Spec: `Docs/superpowers/specs/2026-05-18-app-versioning-design.md` · Plan: `Docs/superpowers/plans/2026-05-18-app-versioning.md`

---

## 💡 Pending — Post-MVP

- **Singer self-registration** — singers register via public link
- **Social features** — post-event sharing, singer stats

---

## ✅ Recently Done

| Feature | Completed | Plan / Reference |
|---------|-----------|------|
| SDD Master Plan (Phases 1–11, 162 steps) | 2026-05-07 | `Docs/DevEnv/SDD/plans/impl/MASTER_PLAN.md` |
| Day-to-day task management workflow review | 2026-05-13 | BACKLOG.md promoted to first-class SCRUM board; workflow.md Rule 1 step 0 + Rule 7 updated; MASTER_PLAN.md reference removed |
| CLAUDE.md Deep Restructure | 2026-05-08 | 7 coding rule files moved to `.claude/library/` (on-demand via skill); `.claudeignore` Docs/ scope gates; context size governance section added |
| workflow.md Reduction | 2026-05-07 | Agent role files (`implementor.md`, `orchestrator.md`) absorb scattered responsibilities; Phase 11 conflict resolution across all rules files (61 findings resolved) |
| Hooks redesign | 2026-05-03 | *(infrastructure only)* |
| Artists & Songs — Domain + TDD RED | 2026-04 | `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` |
| Autocomplete field | 2026-04 | `Docs/superpowers/plans/2026-04-06-autocomplete-field.md` |
| Person CRUD | 2026-04 | `Docs/superpowers/plans/2026-04-07-person-crud.md` |
| Toolbar/FAB vibrant | 2026-04 | `Docs/superpowers/plans/2026-04-02-toolbar-fab-vibrant.md` |
| Styles & Structure | 2026-03 | `Docs/superpowers/plans/2026-03-31-styles-structure.md` |
| Venues MD3 rebuild | 2026-03 | `Docs/superpowers/plans/2026-03-29-venues-md3-rebuild.md` |
| M3 Lists | 2026-03 | `Docs/superpowers/plans/2026-03-11-m3-lists.md` |
