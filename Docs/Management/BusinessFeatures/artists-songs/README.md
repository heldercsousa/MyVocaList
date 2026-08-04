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

> **Note [2026-08-04]:** the song-writes-propagate-to-the-artist-catalog expectation briefly
> recorded here on 2026-08-03 has been moved out. A shipped feature's README describes what
> shipped; a new behavioural expectation belongs in a dated change folder, not appended to
> the parent. It now lives at
> `changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`.
