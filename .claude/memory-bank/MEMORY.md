# MyVocaList — Project Memory Index

> **Purpose:** On-ramp for new sessions. Read this file first to orient quickly.
> **Granularity:** Update at feature milestones — not after every task. Staleness from over-updating
> defeats the purpose. If in doubt, leave it and update at the next phase completion.

---

## Current Phase

**Artists & Songs** — next feature in the roadmap. Spec drafted; repository + service tests written (RED).
See `Docs/specs/artists-songs/` for spec and `Docs/superpowers/plans/` for the active plan.

Active branch: `develop`

---

## Recently Completed

- **SDD Implementation (Phases 1–11, 162 steps)** — all `.claude/` config files enhanced with
  Spec-Driven Development patterns. Conflict analysis (Phase 11) identified 61 findings; 60/61
  applied. Implementation complete as of 2026-05-07.

---

## Active Work

- Artists & Songs: ArtistRepositoryTests (17 failing — RED), pending Builder phase

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
| Artists & Songs | `Docs/specs/artists-songs/` | In progress |

See `memory/project_artists_songs_roadmap.md` for the Artists & Songs phase tracker.

---

## Key Architecture Decisions

| Decision | Why | Revisit if |
|----------|-----|------------|
| No MediatR yet | Overhead not justified for current complexity | Async events or CQRS needed |
| Round-based queue | Karaoke convention; singer expectation | Async/time-based events needed |
| Composition over inheritance in VMs | Testability and explicitness | Pattern universally misapplied |
| Tuple returns for business failures | No exception overhead for expected failures | FluentValidation introduced |
