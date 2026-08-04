# Enhancement seed — Artist-owned songs auto-link to their author's catalog

> **Status:** NOT yet a BACKLOG item. Register via `backlog_gen.py register` (kind=change/enhancement, parent=Artists & Songs Catalog) once SPEC-EVO's BACKLOG migration settles — do NOT hand-edit fenced BACKLOG. **Origin:** Helder's product decision 2026-07-23, reframing BUG-059 (see below). Written as the spec seed; full requirements/design to be authored when the task is picked up.

## Why this exists — reframing BUG-059
During INLINE-AC T10 (item i) the artist's catalog appeared empty right after creating a song on `SongFormPage`. Investigation (fix batch 2, commit `b0e45da`) found this is **current designed behavior**: `Catalog` is a deliberate join table populated **only** via `ICatalogService.AddSongToCatalogAsync` from the Catalog-mode song-picker flow; `SongService.CreateSongAsync`/`CreateSongWithUrlsAsync`/`UpdateSongAsync` never touch it.

Helder's decision: **BUG-059 is CANCELLED (works-as-designed, not a defect).** The desired behavior is a genuine new requirement, captured here as an enhancement.

## Business rule (Helder, 2026-07-23 — authoritative)
- A song added or updated via `SongFormPage` **is**, from the business viewpoint, an entry in **its artist's catalog** (the artist = the song's creator/owner/author).
- The **manual** catalog build (the existing picker flow) exists **only for "performer" artists** — artists who perform *covers* of songs owned by other (actual) artists.
- Therefore: **whenever a song is registered or updated via `SongFormPage`, the author-artist's catalog must be updated automatically** (no manual step).
- **Invariant (crucial):** an artist's own song recorded via `SongFormPage` **can never be removed from that same artist's catalog** — the author's own songs are **perpetually hooked** to the author's catalog. (Removal, if any, must be prevented/guarded for author-owned entries; performer/cover catalog entries remain manually managed as today.)

## Scope seed / open questions for the real spec
- Distinguish "author-owned" catalog links (auto, permanent) from "performer/cover" catalog links (manual, removable) — likely a flag/kind on the `Catalog` join row.
- Auto-link on create AND update; handle artist change on edit (old author link vs new — but note the perpetual-hook invariant: can an author link ever be severed if the song's artist is reassigned? needs Helder ruling).
- Wire `ICatalogService.AddSongToCatalogAsync` (or a new author-link method) into `SongService` save paths — business logic stays in Services (constitutional).
- Guard catalog-removal so author-owned entries cannot be unlinked.
- Regression tests at the service/repo seam; on-device E2E (create song → author catalog shows it; attempt remove → blocked).

## T10 consequence
INLINE-AC T10 **item i must be dropped/rewritten** — it was asserting behavior that does not exist yet by design. Do not gate INLINE-AC closeout on it.
