---
id: song-writes-propagate-to-the-artist-catalog
title: Song writes propagate to the artist catalog
status: 📋 Spec
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

---

## Decisions — Helder, 2026-08-24

These answers close the "Before any code" questions above. They are the authoritative model; the
`requirements.md` / `design.md` in this folder are written against them.

### D1 — The catalog is the performer relation, and authorship implies performance

Verbatim: *"I consider 'author' as the 'artist', and it is a required data… every author obviously is
his own song performer… not all artists registered will have song registered, but may have catalog
registered… an author of multiple songs can also do a cover performance of another artist song, then
he is a performer only for such song. And an artist could be only a cover performer… In summary, a
catalog is all about performers only."*

The resulting model:

| Relation | Meaning | Cardinality |
|---|---|---|
| `Song.ArtistId` → `Artist` | **Authorship.** Required, exactly one per song. | 1 song → 1 author |
| `Catalog(ArtistId, SongId)` | **Performance / repertoire.** Who performs this song. | many ↔ many |

**The invariant (new):** for every `Song`, a `Catalog` row MUST exist with
`ArtistId == Song.ArtistId`. An author always performs their own songs. Additional `Catalog` rows for
other artists represent covers and are user-managed.

**Why this does not break the Authors / Performers filter chips.** It makes authors a strict subset of
performers, but the chips still separate two real populations: `Authors` = artists with
`OriginalSongs` (they wrote something); `Performers` = artists with `CatalogEntries`, which now
additionally includes **cover-only artists who have written nothing at all**. That second group is the
one Helder called out, and it is exactly what the `Performers` chip is for. No chip redefinition is
needed — the earlier concern was based on a wrong reading of the domain.

### D2 — Artist reassignment moves the derived row (option 1), and derived rows are locked in the UI

Verbatim: *"Option 1. I'd like to also have a lock for manual manipulation of catalog entries for
those having the very same IDs registered as the song owner. An author must always have a catalog
entrie for each song he owns. Those records might be only manipulated by the songformpage, never
manualy by user. I supose such lock must live in the catalog form where user has oportunity to
promote changes on them and shall be denyied for these cases."*

Two rules:

1. **Reassignment moves it.** When a song's author changes A → B, the `Catalog(A, song)` row is
   removed and `Catalog(B, song)` is created, in the same transaction as the song update. A is not
   left holding a derived row for a song they no longer own.
2. **Derived rows are read-only to the user.** A `Catalog` row where `ArtistId == Song.ArtistId` is
   *derived* and may only be written by the song-form path. The catalog UI must deny manual
   remove (and any future manual edit) of such a row. Enforcement belongs in the catalog surface where
   the user can act on entries — with a Services-layer guard behind it, since the UI is not a security
   boundary and `CLAUDE.md` puts business rules in Services only.

A consequence worth stating plainly: a "delete from catalog" on a derived row is not a catalog
operation at all — the only way to remove it is to delete or reassign the song.

### D3 — Sequencing: UoW Phase 4.1 first

Helder chose *"Do UOW Phase 4.1 first, then the catalog spec"*. `CatalogService` currently injects
`ICatalogRepository` and calls `SaveChangesAsync` on it directly, with no `IUnitOfWork` dependency;
`SongService`'s writes are all inside `_uow.ExecuteAsync`, where REQ-UOW-28 requires every collaborator
to be resolved from the lambda's own `sp`. Propagation cannot be written correctly until 4.1 lands.

### D4 — Existing data is backfilled once, by migration (Helder, 2026-08-24)

D1's invariant is stated over *every* song, not only songs written from now on. Songs already in the
database predate the rule and carry no derived row. Helder chose the **one-off backfill migration**:
an EF Core migration inserts the missing `Catalog(Song.ArtistId, Song.Id)` rows exactly once, so the
invariant holds over all data from the moment this ships.

Rejected alternatives, and why the record matters: a *startup self-heal* was rejected because it
writes on every launch and hides drift instead of surfacing it; *leaving old data alone* was rejected
because it would leave the invariant merely aspirational, so no code could safely rely on it.

Consequences the spec must state as expected, not as defects:

- On first launch after the update, existing artists' catalogs gain entries they never had, and the
  `Performers` filter chip starts returning authors it previously omitted. This is the intended
  correction, but it is user-visible and should not surprise anyone reading a bug report later.
- The migration must be **idempotent in effect** — insert only where no `Catalog(ArtistId, SongId)`
  row already exists. A song whose author already had a manually-added catalog entry must not produce
  a duplicate, since `(ArtistId, SongId)` is the composite primary key and a duplicate insert throws
  `DbUpdateException`.
- Rows the backfill creates are indistinguishable from rows a user added by hand — the `Catalog`
  entity is a bare join with no provenance column. Under D2 that is acceptable, because "derived" is
  computed (`Catalog.ArtistId == Song.ArtistId`), not stored. Worth stating explicitly so nobody later
  adds a provenance flag believing one is needed.
## Open questions

None outstanding. D1–D4 close every question the "Before any code" section raised.
