# MyVocaList — Project Memory Index

> **Purpose:** On-ramp for new sessions. Read this file first to orient quickly.
> **Granularity:** Update at feature milestones — not after every task. Staleness from over-updating
> defeats the purpose. If in doubt, leave it and update at the next phase completion.

---

## Current Phase

**SDD Implementation** — applying SDD (Spec-Driven Development) enhancements to `.claude/` config files.
See `Docs/DevEnv/plans/impl/MASTER_PLAN.md` for the full step list and progress.

Active branch: `develop`

---

## Recently Completed

- Phase 1 (P1): All status log files created (`S1_impl_status.md` through `S10_impl_status.md`)
- Phase 2 (P2): New support files — `constraints-registry.md`, `exception-registry.md`,
  `agents/implementor.md`, `agents/verifier.md`, `memory-bank/MEMORY.md` (this file)

---

## Active Work

- Phase 3 (P3): `.claude/rules/workflow.md` — Rule 1 (Spec-First) enhancements — **not started**
- 27 steps pending in P3 alone; see MASTER_PLAN.md for the full queue

---

## Upcoming

- Phase 4 — workflow.md Rule 2 (Subagent Delegation enhancements)
- Phase 5 — workflow.md Rules 3–6 + new rules
- Phase 6 — `.claude/settings.json` hooks
- Phase 7 — `.claude/rules/testing.md`
- Phase 8 — `.claude/rules/code-principles.md`
- Phase 9 — `CLAUDE.md`
- Phase 10 — `.claude/commands/review.md`
- Phase 11 — Conflict Analysis

---

## Top Constraints (quick reference)

1. **DisplayAlert is banned** — use `dx:BottomSheet` only (CLAUDE.md Non-Negotiables)
2. **Business logic in Services only** — never in ViewModels or pages (code-principles.md)
3. **SafeAreaEdges="Container"** required on every `ContentPage` (MAUI 10 breaking change)
4. **DXCollectionView** — max one `ReplaceRange` per `RunOnUiThread` block (ANR risk)
5. **Repository interfaces in Domain; implementations in Infra** — only MAUI references Infra

---

## Spec Index (active feature specs)

| Feature | Spec location | Status |
|---------|---------------|--------|
| Venues | `Docs/specs/venues/` | Implemented |
| Artists & Songs | `Docs/specs/artists-songs/` | Planned (see roadmap) |

See `memory/project_artists_songs_roadmap.md` for the Artists & Songs phase tracker.

---

## Key Architecture Decisions

| Decision | Why | Revisit if |
|----------|-----|------------|
| No MediatR yet | Overhead not justified for current complexity | Async events or CQRS needed |
| Round-based queue | Karaoke convention; singer expectation | Async/time-based events needed |
| Composition over inheritance in VMs | Testability and explicitness | Pattern universally misapplied |
| Tuple returns for business failures | No exception overhead for expected failures | FluentValidation introduced |
