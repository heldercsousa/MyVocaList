---
id: form-autocomplete-ux-overhaul
title: **Form & Autocomplete UX Overhaul**
status: 🔵 Superseded
target: 2026-07-11
section: BusinessFeatures
goal: umbrella sequencing all form-presentation + AppBar-save + adaptive-autocomplete changes.
gate: foundation order ② → component build → first application → ①; forms convert in order Venue → Artist → Singer.
pointer: Docs/Management/cross-cutting-log.md
closed: 2026-08
order: 200
kind: feature
---

# Form & Autocomplete UX Overhaul

Migrated from the pre-migration BACKLOG.md row (no prior spec folder).

Back-reference: `Docs/Management/cross-cutting-log.md` (retained; not migrated).

> **Closed 🔵 Superseded [2026-08-04] -- cancelled (Helder).** This umbrella sequenced its phases
> around *building a new autocomplete component*. That build is no longer needed: Helder's
> 2026-07-19 adoption decision replaced it with migration onto the DevExpress built-in
> (`DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/`).
>
> With its central phase removed the gate ordering no longer describes any real plan, so the
> umbrella is retired rather than re-sequenced. The form-presentation work it was sequencing
> survives under **Artist & Song Form UX Redesign** (`BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`). Narrative retained in `Docs/Management/cross-cutting-log.md`.
