---
id: data-model-simplification-merge-artist-into-person-catalog-as-an-auto-filled-grid-discussion
title: Data model simplification — merge Artist into Person, catalog as an auto-filled grid (discussion)
status: 💡 Pending
target: 2026-11-12
section: BusinessFeatures
parent: artists-songs
goal: "Helder's 2026-11-12 ideas: one Person entity replaces Artist, the catalog becomes a grouped editable grid, and catalogs fill from karaoke participations. The DB is not in production, so a migration is cheap now."
gate: Helder opens the discussion; nothing is designed or built before it.
kind: change
---

# Data model simplification — merge Artist into Person, catalog as an auto-filled grid (discussion)

Helder's 2026-11-12 ideas: one Person entity replaces Artist, the catalog becomes a grouped editable grid, and catalogs fill from karaoke participations. The DB is not in production, so a migration is cheap now.


> **Status: idea capture only.** Recorded from Helder, 2026-11-12, for a later discussion about
> data-model optimisations. Nothing below is decided, specified or scheduled. The database is not in
> production yet, so this is a cheap moment for any migration.

## The ideas (Helder, 2026-11-12)

### 1. The catalog is one simple table, not a set of pages

Helder has not yet seen the catalog pages beyond the list-like page (FAB + toolbar) and suspects the
feature is over-engineered. The catalog should be a **single table**: a DevExpress component example
allows inline table editing, so the catalog could be **an editable table with one column and n rows,
grouped by artist**. A group "Ramones" would hold, below it, all the songs that artist performs.

### 2. One entity instead of Artist + Person

Having both an Artist entity and a People entity is felt to be redundant; they could become **a
single entity, the person/people one**. Artist is "too simple an entity, a name only".

- A solo artist is recorded as a person.
- A band is also recorded as a person, linked to other persons if its members want to register that
  relation.
- Everything in the business links to a person: a song has **a person who owns it** and **n people
  who perform it**, whatever the kind of performance (sing, drum, electric guitar, ...).
- The only thing MyVocaList takes advantage of is the **person's catalog**.

### 3. Catalogs fill themselves from karaoke participations

- A singer's catalog is **auto-filled**: when singer Helder performs a song he has never sung before,
  it is added to his catalog. This helps him remember which songs he likes, and lets the karaoke
  admin know what each person sings or plays.
- **Instrumentist participations** are recorded the same way singing turns are.
- An instrumentist's or band's catalog is auto-updated from the performances executed during the
  karaoke event.
- Participation history (singers and instrumentists) lives in a **1 → n table**: the 1 is the catalog
  song item, the n is each time the person or band performed it. Helder's view: this makes the
  database much simpler.

## Why this matters now (context, not a decision)

- **CATALOG-PROP** (`changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`) is built on
  D1-D4: `Song.ArtistId` is authorship and `Catalog(ArtistId, SongId)` is performance, with a derived
  row per song. Idea 2 would change what "artist" means there, and idea 3 would add a second way
  catalog rows appear. That item's spec was about to be written; the order of the two is the first
  thing to settle.
- The catalog picker (Phase 15, the empty `AddToCatalogAsync` stub) and BUG-086 are affected by idea 1.
- The Authors / Performers filter chips and the Artists CRUD would be affected by idea 2.
- Related items already in the backlog: the Queue and Event deletion, "Post-MVP: import an artist's
  song catalog from files", and the song-identity discussion (version de-duplication).

## Questions for the discussion (not answered)

1. Merge Artist into Person fully, or keep a thin Artist role on top of Person?
2. Band-to-member relation: optional link between persons, and what kinds of link exist?
3. Do song ownership and performance stay two relations (owner 1, performers n), and does ownership
   still imply performance (CATALOG-PROP D1)?
4. Catalog as a grouped grid: does it replace the Artist → Songs navigation entirely? Which DevExpress
   grid sample, and does it fit the UI Component Priority rule?
5. Participation history: how does it relate to the queue/round data that exists today, and what
   happens to history already recorded?
6. Migration: data conversion Artist → Person, ids, and the import/backup formats.
7. What does this do to the catalog-propagation spec and the catalog picker, which should wait?

## Not related to

BUG-086 (the trailing catalog button) is fixed independently on branch `fix/bug-086-round2` and is
not blocked by this.
