---
id: song-writes-propagate-to-the-artist-catalog
title: Song writes propagate to the artist catalog
status: 💡 Pending
target: 2026-08-04
section: BusinessFeatures
parent: artists-songs
goal: Creating a song must also create the matching record in that artist's catalog, and editing a song must be reflected there. Today the two can drift apart.
gate: "Spec first: confirm what SongFormPage already anticipates, then trace to a new or existing acceptance criterion before any code."
kind: change
---

# Song writes propagate to the artist catalog

Post-ship change to **Artists & Songs Catalog** (`BusinessFeatures/artists-songs/`). The parent's
shipped `requirements.md` / `design.md` are immutable history and are **not** edited by this
item — this folder carries the delta (`CLAUDE.md` § Docs/ Folder Layout).

## The expectation

Recorded from Helder on 2026-08-03:

- Creating a song must **also** create the corresponding record in that artist's catalog.
- Updating a song entity must be **reflected** into the artist's catalog.

Helder believes `SongFormPage` / `SongFormViewModel` already anticipates part of this.

## Why this is a change and not a bug

It was first captured against BUG-028, which is the wrong container. Nothing here is a
regression or a defect against an existing acceptance criterion — there is no criterion
saying the two stay in step, which is precisely the gap. Recording an unbuilt behaviour as a
bug would make the bug ledger describe work that was never specified. BUG-028 has been closed
as superseded and this item carries the expectation forward as an enhancement.

## Before any code

Per the SDD Invariant (spec before code) this item is 💡 Pending and not dispatchable yet:

1. Confirm against the code what the song form already does on create and on update.
2. Decide the propagation rule — including the edit cases that are easy to get wrong: an
   artist reassignment (the old artist's catalog entry must not be orphaned) and a delete.
3. Either trace it to an existing AC in the parent's `requirements.md` or write a new AC in
   this folder's own `requirements.md`.

## Not related to

**BUG-071 (alias BUG-068)** — the edit-mode save failure — is a persistence-layer EF Core
tracking defect. It will make this behaviour untestable in edit mode until it is fixed, but it
is not the same problem and fixing it does not deliver this item.

