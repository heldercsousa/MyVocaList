---
id: BUG-030
title: "BUG-030: ArtistFormPage search strip UX unclear (spec gap)"
status: "🔵 Deferred"
target: 2026-07-03
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 160
goal: "resolve the search-strip spec gap on the Artist form."
gate: "Answered by Helder 2026-07-10: the element must disappear from both forms — folded into the Form UX Redesign."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-030-artistformpage-search-strip-ux-unclear/
---

# BUG-030: ArtistFormPage search strip UX unclear (spec gap)

What the Artist form's search strip is for was never specified, so its intended
behaviour could not be validated.

Answered by Helder 2026-07-10: the element must disappear from both forms — folded
into the Form UX Redesign.

> **No `severity:` — faithful transcription.** The pre-migration row carries no severity
> (it is a spec gap, not a defect classification). `model.validate` requires a severity
> only in the negative sense — a `Minor` folder is an error (REQ-SEV-03); an unset one is
> not. Left unset rather than invented. Declared for T12.

> **Goal sentence agent-authored [2026-07-22] — pending Helder's review.** Same class as
> BUG-029: the row carries no `Goal:`, `model.REQUIRED` mandates one, and the answer text
> is transcribed verbatim as `gate:`.

**History / back-link:** `BusinessFeatures/artists-songs/task-log.md`

> **Spec updated [2026-07-22]:** folder created by the spec-evolution migration (T11b,
> REQ-SEV-01). The row's pointer moves from the shared `artists-songs` task-log to this
> folder. **Nothing was removed from that task-log** (REQ-SEV-27); it remains the
> narrative record and is linked above.
