# MyVocaList — Project Backlog

> A simple list of desired app functionality and infrastructure initiatives, each with a status.
> Agents preparing to write a new spec should check this file to identify the relevant backlog item
> and reference it in `requirements.md`. Not a session-start read for all agents.

---

## Status reference

| Status | Meaning |
|--------|---------|
| `💡 Idea` | Captured, not yet evaluated |
| `📋 Spec` | Approved — spec being written |
| `🗺️ Plan` | Spec approved — plan being written |
| `🟢 Ready` | Plan approved — dispatchable |
| `🟡 In Progress` | Implementation underway |
| `🔵 Deferred` | Paused — reason documented |
| `🔴 Blocked` | Cannot proceed — dependency named |
| `✅ Done` | Shipped — changelog updated |

---

## 🟡 In Progress

- **Artists & Songs Catalog** `🟡 In Progress` — artist and song management with search; feeds singer song selection
  → Spec: `Docs/specs/artists-songs/` · Plan: `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md`

---

## 🟢 Ready

*(none — Artists & Songs must complete first)*

---

## 💡 Ideas / Planned

- **Queue management MVP** `⚪ Planned` — core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate
- **Singer self-registration** `💡 Idea` — singers register via public link
- **Song catalog** `💡 Idea` — song library with lyrics API integration
- **Social features** `💡 Idea` — post-event sharing, singer stats

---

## 🔵 Deferred — Infrastructure

### Day-to-day task management workflow review
The session-start protocol in `workflow.md` referenced an SDD-specific plan file (MASTER_PLAN.md, now complete). The guidance for how the project manages backlog + nested tasks (BACKLOG.md → plan → task-log → spec) is scattered and incomplete. Systematic review needed.
→ Scope: update workflow.md Rule 7 session start; clarify BACKLOG.md role; remove stale MASTER_PLAN reference
→ Process: spec → spec review → plan → plan review → Helder approval → apply

### CLAUDE.md Deep Restructure
CLAUDE.md has grown beyond its constitutional purpose. Candidate removals: evaluation notes (Tessl, sdd-mcp, Cursor, Playwright, GitHub MCP), full MCP governance rules, detailed rule amendment protocol, SDD applicability rationale.
→ Process: spec → spec review → plan → plan review → Helder approval → apply

### workflow.md Reduction
Agent role files (`implementor.md`, `verifier.md`) should absorb responsibilities currently scattered inline in `workflow.md`, reducing its line count and improving readability.
→ Reference: `memory/project_workflow_cleanup.md`
→ Process: spec → spec review → plan → plan review → Helder approval → apply

---

## ✅ Recently Done

| Feature | Completed | Plan |
|---------|-----------|------|
| SDD Master Plan (Phases 1–11, 162 steps) | 2026-05-07 | `Docs/DevEnv/SDD/plans/impl/MASTER_PLAN.md` |
| Hooks redesign | 2026-05-03 | *(infrastructure only)* |
| Artists & Songs — Domain + TDD RED | 2026-04 | `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` |
| Autocomplete field | 2026-04 | `Docs/superpowers/plans/2026-04-06-autocomplete-field.md` |
| Person CRUD | 2026-04 | `Docs/superpowers/plans/2026-04-07-person-crud.md` |
| Toolbar/FAB vibrant | 2026-04 | `Docs/superpowers/plans/2026-04-02-toolbar-fab-vibrant.md` |
| Styles & Structure | 2026-03 | `Docs/superpowers/plans/2026-03-31-styles-structure.md` |
| Venues MD3 rebuild | 2026-03 | `Docs/superpowers/plans/2026-03-29-venues-md3-rebuild.md` |
| M3 Lists | 2026-03 | `Docs/superpowers/plans/2026-03-11-m3-lists.md` |
