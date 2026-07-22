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
gate: "parked by Helder; gated on autocomplete foundations ① + ②; partial work sits on branch `feature/form-ux-redesign`."
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
