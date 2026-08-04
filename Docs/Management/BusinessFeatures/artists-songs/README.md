---
id: artists-songs
title: "**Artists & Songs Catalog**"
status: "🔴 Blocked"
target: 2026-05
section: BusinessFeatures
kind: feature
order: 20
goal: "full artist/song catalog management."
gate: "BUG-068 (Critical) — an EF Core tracking conflict aborts every edit-mode song save; smoke test 16C.1 must re-run green before phases 16C.2–16C.5 resume."
pointer: BusinessFeatures/artists-songs/
---

# Artists & Songs Catalog

Full artist/song catalog management. Specs: `requirements.md`, `design.md`, `tasks.md`, `task-log.md`.

> **Gate re-pointed [2026-08-03]:** BUG-027 closed ✅ Fixed — its three symptoms (no
> autocomplete, no validation, blur clears typed text) all verify clean. The catalog stays
> 🔴 Blocked, but on **BUG-068**, not BUG-027.

## Open behavioural expectation — song writes must reflect into the artist's catalog

Recorded from Helder 2026-08-03, not yet traced to an acceptance criterion:

- Creating a song must **also** create the corresponding record in that artist's catalog.
- Updating a song entity must be **reflected** into the artist's catalog.

Helder believes `SongFormPage`/`SongFormViewModel` already anticipates part of this. Before
phases 16C.2–16C.5 resume, this must be confirmed against the code and either traced to an
existing AC in `requirements.md` or added as a new one (SDD Invariant — spec before code).
It is **not** covered by BUG-068, which is a persistence-layer tracking defect.
