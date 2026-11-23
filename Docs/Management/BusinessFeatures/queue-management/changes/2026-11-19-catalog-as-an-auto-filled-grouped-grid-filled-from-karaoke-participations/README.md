---
id: catalog-as-an-auto-filled-grouped-grid-filled-from-karaoke-participations
title: Catalog as an auto-filled grouped grid, filled from karaoke participations
status: 💡 Pending
target: 2026-11-19
section: BusinessFeatures
parent: queue-management
goal: "Helder's wish (2026-11-12, re-homed 2026-11-19): the catalog becomes an editable grid grouped by artist, and singer, instrumentist and band catalogs fill themselves from the performances recorded during a karaoke event."
gate: Tackled together with the queue pages; nothing is designed or built before then.
kind: change
---

# Catalog as an auto-filled grouped grid, filled from karaoke participations

Helder's wish (2026-11-12, re-homed 2026-11-19): the catalog becomes an editable grid grouped by artist, and singer, instrumentist and band catalogs fill themselves from the performances recorded during a karaoke event.

> **Status: idea capture only.** Helder recorded these ideas on 2026-11-12, in a data-model discussion
> item under `artists-songs`. On 2026-11-19 he dropped the other idea in that item (merging Artist
> into Person) and moved these two here, because they belong to the queue process. They will be
> taken up only when the queue pages are tackled. Nothing below is decided, specified or scheduled.

## The ideas (Helder, 2026-11-12)

### 1. The catalog is one simple table, not a set of pages

Helder suspects the current catalog pages are over-engineered. The catalog should be a **single
table**: a DevExpress component sample supports inline table editing, so the catalog could be **an
editable table with one column and n rows, grouped by artist**. A "Ramones" group would list all the
songs that artist performs.

### 2. Catalogs fill themselves from karaoke participations

- A singer's catalog is **auto-filled**: when a singer performs a song they have never sung before, it
  is added to their catalog. This helps them remember which songs they like, and lets the karaoke
  admin know what each person sings or plays.
- **Instrumentist participations** are recorded the same way as singing turns.
- An instrumentist's or band's catalog is updated automatically from the performances during the
  karaoke event.
- Participation history (singers and instrumentists) lives in a **1 → n table**: the 1 is the catalog
  song item, the n is each time the person or band performed it.

## Related

- **CATALOG-PROP** (`artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`)
  is the first way catalog rows appear: a song write adds one. Idea 2 adds a second way. The
  CATALOG-PROP spec must not rule that out.
- The catalog picker (Phase 15, the empty `AddToCatalogAsync` stub) is affected by idea 1.
- Song rows carry singer history and are never recreated, so the participation history attaches to
  stable song ids.

## Questions for when the queue work starts (not answered)

1. Does the grouped grid replace the Artist → Songs navigation entirely? Which DevExpress grid sample,
   and does it fit the UI Component Priority rule?
2. How does participation history relate to the queue/round data that exists today, and what happens
   to the history already recorded?
3. Is an auto-filled catalog row the same kind of row as one added by hand or by CATALOG-PROP, or is
   it marked by its source?
