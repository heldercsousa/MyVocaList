---
id: BUG-028
title: "BUG-028: ArtistsPage trailing catalog button no-op — regression of BUG-015/019 (Major)"
status: "💡 Pending"
severity: Major
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 140
goal: "trailing button must navigate to the artist's catalog."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-028-artistspage-trailing-catalog-button-noop/
---

# BUG-028: ArtistsPage trailing catalog button no-op

Regression of the trailing-button half of BUG-015 / BUG-019. The investigation history
lives in the BUG-019 folder's task log; nothing was moved or deleted from it.

> **Spec updated [2026-07-22]:** folder created so that BUG-019 (archived) and BUG-028 (live)
> each own exactly one folder — one folder cannot back two rows (Helder decision 5A,
> spec-evolution-versioning). The row's pointer changes from the BUG-019 folder to this one.

> **Spec updated [2026-07-22]:** folder renamed from `BUG-028-artistspage-trailing-catalog-button-noop/` to the REQ-SEV-01 pattern `2026-07-03-BUG-028-…/` (finding F6). The date is the row's own `target: 2026-07-03`, not an invented one. Moved with `git mv`; the `pointer:` above was updated in the same commit (T11c).
