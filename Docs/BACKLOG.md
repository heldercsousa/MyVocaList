# MyVocaList — Project Backlog

> **Single source of truth for what is planned, in progress, and deferred.**
> Read this file at the start of every session — before any spec or plan file.
> Update it whenever a feature changes status. Never let it go stale.

---

## File relationship map

```
BACKLOG.md  (this file — project board, feature/initiative level)
  └── Docs/superpowers/plans/<plan>.md          (feature plan — phases and waves)
        └── Docs/superpowers/plans/<plan>-task-log.md   (audit trail per task)
        └── Docs/specs/<feature>/tasks.md                (atomic task checklist)
              └── Docs/specs/<feature>/requirements.md   (acceptance criteria)
              └── Docs/specs/<feature>/design.md         (architecture decisions)
```

**Granularity rule:** BACKLOG.md tracks one entry per *desired functionality or initiative* — not per task.
Atomic tasks live in `tasks.md`. This file is the 10,000ft view.

**Cross-reference rule:** Every entry must link to its plan/spec file once one exists. One-line description only — the linked file owns the detail.

---

## Status lifecycle (maps to SDD pipeline)

| Status | Meaning | Entry condition |
|--------|---------|-----------------|
| `💡 Idea` | Captured, not yet evaluated | Any time |
| `📋 Spec` | Approved — spec being written | Helder approves the idea |
| `🗺️ Plan` | Spec approved — plan being written | Spec reviewed and signed off |
| `🟢 Ready` | Plan approved — first wave dispatchable | Plan reviewed and signed off |
| `🟡 In Progress` | Implementation underway | First subagent dispatched |
| `🔵 Deferred` | Paused — reason and blocker documented | Explicit decision |
| `🔴 Blocked` | Cannot proceed — dependency named | Blocker identified |
| `✅ Done` | Shipped — changelog updated | All tasks committed, review passed |

---

## 🟡 In Progress

### Artists & Songs Catalog
**Next:** Phase 3 — Infrastructure (EF configs, migration, ArtistRepository, SongRepository)
→ Plan: `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md`
→ Task log: `Docs/superpowers/plans/2026-04-23-artists-songs-catalog-task-log.md`

| Phase | What | Status |
|-------|------|--------|
| 1 | Domain & Contracts | ✅ |
| 2 | Tests (TDD RED) | ✅ |
| 3 | Infrastructure (EF configs, migration, repositories) | 🟡 Next |
| 4 | Services (GREEN unit tests) | ⬜ |
| 5 | DI Registration | ⬜ |
| 6 | Artists UI | ⬜ |
| 7 | Songs UI | ⬜ |
| 8 | Final smoke test + review | ⬜ |

---

### SDD Infrastructure — Phase 11 Conflict Resolution
**Current:** P11-03 — reviewing `conflict_report.md` with Helder, recording decisions in Decision Registry
→ Plan: `Docs/DevEnv/plans/impl/MASTER_PLAN.md` (Phase 11 section)

| Step | What | Status |
|------|------|--------|
| P11-01-A | Agent A — workflow.md analysis | ✅ (33 findings) |
| P11-01-B | Agent B — review.md + testing.md | ✅ (23 findings) |
| P11-01-C | Agent C — CLAUDE.md + code-principles.md | ✅ (20 findings) |
| P11-02 | Synthesizer — consolidated conflict_report.md | ✅ (61 findings) |
| P11-03 | Decision Registry — Helder approves/defers/rejects each finding | 🟡 In Progress |
| P11-04-A | Apply resolutions → workflow.md | ⬜ |
| P11-04-B | Apply resolutions → review.md | ⬜ |
| P11-04-C | Apply resolutions → testing.md | ⬜ |
| P11-04-D | Apply resolutions → CLAUDE.md (+ deep restructure — see Deferred) | ⬜ |
| P11-04-E | Apply resolutions → code-principles.md + constraints-registry.md | ⬜ |

---

## 🟢 Ready

*(Nothing approved and ready — Artists & Songs must complete first)*

---

## 💡 Idea / ⚪ Planned

- **Queue management MVP** `⚪ Planned` — core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate
- **Singer self-registration** `💡 Idea` — singers register via public link; future feature

---

## 🔵 Deferred

### CLAUDE.md Deep Restructure
Move detailed guides out of CLAUDE.md into dedicated rule files, reducing it to a routing table + non-negotiables (~75 lines). Specific moves:
- Evaluation notes (Tessl, sdd-mcp, Cursor, Migration Path, Playwright MCP, GitHub MCP) → `Docs/DevEnv/tooling-evaluation.md`
- MCP rules (Context Budget, Security Stance, Token Discipline, Availability Gate) → `.claude/rules/mcp-governance.md`
- Rule Authority Hierarchy + Amendment Protocol + Continuous Enhancement detail → `.claude/rules/rule-governance.md`
- Skill & MCP Lookup table → `.claude/rules/skill-lookup.md`
- SDD Applicability rationale → remove (covered in `Docs/DevEnv/`)
→ **Blocked by:** Phase 11-04-D (same file — do in one pass)
→ **Scope note:** Phase 11-04-D subagent must include this restructure alongside conflict resolutions

### BACKLOG.md References in Guideline Files
Wire BACKLOG.md into workflow.md (Rule 7 session start + Rule 1 new feature workflow) and commands/review.md (feature close-out checklist). Currently blocked by hooks protecting those files.
→ **Blocked by:** Phase 11 completion (those files are being edited in P11-04-A/B)
→ **Files:** `workflow.md` Rule 7 (step 0), Rule 1 (new feature step 0), `review.md` close-out section

### Phase 9 MASTER_PLAN Reconciliation
MASTER_PLAN Phase 9 shows 28 CLAUDE.md steps as Pending (0 Done), but CLAUDE.md already contains most of that content — added informally before the plan was created. Needs a verification pass: confirm each P9 step is reflected in current CLAUDE.md, mark Done or apply what's missing.
→ **Blocked by:** Phase 11-04-D (same file)

### workflow.md Reduction
Agent role files (`implementor.md`, `verifier.md`) should absorb scattered responsibilities currently inline in workflow.md, after Phase 11 conflict resolution is complete.
→ **Blocked by:** Phase 11-04-A completion
→ Reference: `memory/project_workflow_cleanup.md`

---

## ✅ Recently Done

| Feature | Completed | Plan |
|---------|-----------|------|
| Hooks redesign | 2026-05-03 | (no plan file — infrastructure only) |
| SDD Master Plan phases 1–10 | 2026-05 | `Docs/DevEnv/plans/impl/MASTER_PLAN.md` |
| Autocomplete field | 2026-04 | `Docs/superpowers/plans/2026-04-06-autocomplete-field.md` |
| Person CRUD | 2026-04 | `Docs/superpowers/plans/2026-04-07-person-crud.md` |
| Toolbar/FAB vibrant | 2026-04 | `Docs/superpowers/plans/2026-04-02-toolbar-fab-vibrant.md` |
| Styles structure | 2026-03 | `Docs/superpowers/plans/2026-03-31-styles-structure.md` |
| Venues MD3 rebuild | 2026-03 | `Docs/superpowers/plans/2026-03-29-venues-md3-rebuild.md` |
| M3 Lists | 2026-03 | `Docs/superpowers/plans/2026-03-11-m3-lists.md` |

---

## How to maintain this file

| Event | Action |
|-------|--------|
| New idea captured | Add under 💡 Idea with one-line description |
| Idea approved for spec | Move to 📋 Spec, link spec folder once created |
| Spec signed off | Move to 🗺️ Plan, link plan file once created |
| Plan signed off | Move to 🟢 Ready |
| First wave dispatched | Move to 🟡 In Progress, add phase table |
| Phase completes | Check off phase row in table |
| Feature ships | Move to ✅ Done, remove phase table |
| Work paused | Move to 🔵 Deferred with blocker named |
| Review command closes a feature | Mark ✅ Done here before committing |
