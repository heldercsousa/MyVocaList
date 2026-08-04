---
id: form-ux-redesign
title: "**Artist & Song Form UX Redesign — autocomplete, similar-match warning, search-strip removal**"
status: "🔵 Deferred"
target: 2026-07-10
section: BusinessFeatures
parent: artists-songs
kind: change
order: 180
goal: "friction-free artist/song name entry (local + API autocomplete, never clear typed text, similar-match warning before create)."
gate: "parked by Helder only — the autocomplete foundations that once gated it are both retired; partial work sits on branch `feature/form-ux-redesign`. Now also owns cancelled BUG-028, BUG-029, BUG-031 and BUG-032."
pointer: BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/
---

# Artist & Song Form UX Redesign

Change folder for the artist/song form UX redesign: local + API autocomplete, a
similar-match warning before create, and removal of the in-form search strip.

Paused by Helder pending autocomplete foundations ① and ②.

> **Spec updated [2026-07-22]:** the pre-migration BACKLOG row's Gate carried the progress
> fraction *"~6/14 tasks done"*. `model._BANNED` reads `6/14` as a test count, so the fraction
> is recorded here verbatim instead of in the row. Nothing else in the row was reworded; the
> branch name `feature/form-ux-redesign` is preserved in the Gate.

Detail: `requirements.md`, `design.md`, `tasks.md`, `plan.md`, `task-log.md` in this folder.

> **Scope expanded [2026-08-04] — this item now owns four cancelled bug rows.** Helder closed
> BUG-028, BUG-029, BUG-031 and BUG-032 as 🔵 Superseded rather than fixing them
> separately, because each one lives on a surface this redesign reworks or restates a
> requirement this redesign must satisfy. Their acceptance obligations transfer here:
>
> | Retired row | What this redesign must deliver |
> |---|---|
> | BUG-028 | ArtistsPage trailing catalog button navigates to the artist's catalog |
> | BUG-029 | No crash path from the search-strip icon — satisfied by removing the strip |
> | BUG-030 | The search strip disappears from **both** forms (Helder, 2026-07-10) |
> | BUG-031 / BUG-032 | Autocomplete (local + API) on both the Artist Name and Song Title entries |
>
> BUG-030 was already folded in on 2026-07-10 and is listed for completeness; it remains
> 🔵 Deferred as its own row rather than cancelled. These obligations must become
> acceptance criteria in this folder's `requirements.md` when the item is un-parked —
> otherwise cancelling the bugs silently drops the behaviour.
>
> **Gate refreshed the same day:** the previous gate read *"gated on autocomplete foundations
> ① + ②"*. Both are gone — ① was retired 🔵 Superseded on 2026-08-04 and ② was
> absorbed by the 2026-07-19 DX adoption decision. The only thing holding this item now is
> Helder's own prioritisation.
