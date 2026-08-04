---
id: BUG-028
title: "BUG-028: ArtistsPage trailing catalog button no-op — regression of BUG-015/019 (Major)"
status: 🔵 Superseded
severity: Major
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
goal: trailing button must navigate to the artist's catalog.
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-028-artistspage-trailing-catalog-button-noop/
closed: 2026-08
order: 140
kind: bug
---

# BUG-028: ArtistsPage trailing catalog button no-op

Regression of the trailing-button half of BUG-015 / BUG-019. The investigation history
lives in the BUG-019 folder's task log; nothing was moved or deleted from it.

> **Spec updated [2026-07-22]:** folder created so that BUG-019 (archived) and BUG-028 (live)
> each own exactly one folder — one folder cannot back two rows (Helder decision 5A,
> spec-evolution-versioning). The row's pointer changes from the BUG-019 folder to this one.

> **Spec updated [2026-07-22]:** folder renamed from `BUG-028-artistspage-trailing-catalog-button-noop/` to the REQ-SEV-01 pattern `2026-07-03-BUG-028-…/` (finding F6). The date is the row's own `target: 2026-07-03`, not an invented one. Moved with `git mv`; the `pointer:` above was updated in the same commit (T11c).

> **Closed 🔵 Superseded [2026-08-04] -- cancelled as a standalone bug (Helder).** Ownership of the
> trailing catalog button behaviour transfers to **Artist & Song Form UX Redesign** (`BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`), which reworks the surface this
> defect lives on. Fixing it here first would be work thrown away by that redesign.
>
> This is a transfer of ownership, not a dismissal: the redesign must leave the trailing
> button navigating to the artist's catalog. Nothing was deleted from the BUG-019 task log
> linked above.
