---
id: artists-songs
title: **Artists & Songs Catalog**
status: 🟡 In Progress
target: —
section: BusinessFeatures
goal: full artist/song catalog management.
gate: "No blocker. Open: adding cover songs by hand (catalog picker, needs a spec) and Helder's re-run of the 16C.1 emulator smoke test."
pointer: BusinessFeatures/artists-songs/
order: 20
kind: feature
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

> **Unblocked [2026-11-21]:** 🔴 Blocked → 🟡 In Progress (Helder). The block chain is resolved:
> BUG-027 was fixed on 2026-08-03, and BUG-068 (registered as **BUG-071**) was fixed on
> 2026-08-03 and closed on 2026-08-20.
>
> **Catalog state on 2026-11-21:**
> - **Done:** song writes keep the author's catalog in sync (CATALOG-PROP, device-checked
>   2026-11-20). Creating or editing a song gives its artist the matching catalog row. Changing
>   the author moves the row, and deleting the song removes it. Catalog-mode Delete removes
>   cover rows only and never deletes songs.
> - **Missing:** adding cover songs to an artist's catalog by hand. The catalog picker
>   (`SongsViewModel.AddToCatalogAsync`, a Phase 15 stub) was out of scope of CATALOG-PROP.
>   Tracked as `changes/2026-11-21-artist-catalog-add-cover-songs-by-hand-catalog-picker/`.
>   Until it ships no cover row can be created, so the cover-remove path has nothing to act on.
> - **Open gate:** Phase 16C.1 (emulator smoke test) has not been re-run since it failed on
>   2026-07-03. Its catalog steps (add through the FAB picker, remove) wait for the picker.
