---
id: BUG-008
title: "Bug/Gap: SongFormPage Artist field autocomplete with blur-clear (BUG-008)"
status: "🔵 Superseded"
target: 2026-06
section: BusinessFeatures
parent: artists-songs
kind: bug
closed: 2026-06
order: 40
goal: "Originally fixed with blur-clear; the Artist & Song Form UX Redesign reverses that behavior and owns the field — no independent action."
pointer: BusinessFeatures/artists-songs/bugs/2026-06-01-BUG-008-songform-artist-autocomplete/
---

# Bug/Gap: SongFormPage Artist field autocomplete with blur-clear

Originally fixed with blur-clear behavior. The Artist & Song Form UX Redesign
(`BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`) reverses that behavior
and now owns the field — no independent action remains for this row.

> Migrated from the 2026-07 archive row (T12a Wave O). **REQ-SEV-18 routing edge case:** this
> row was listed in the 2026-07 archive file's Notes column but its own `closed:` date is
> 2026-06 (the row's own supersession date recorded against the original 2026-06-11 filing,
> superseded 2026-07-10) — Helder ruled (Blocker #3) to route by `closed:` month, so this
> folder sits at the 2026-06 fence even though its archive text currently sits in the 07 file.
> The pre-existing flat file `BUG-008-songform-artist-autocomplete.md` was moved in with
> `git mv`, nothing deleted. The source file's severity text ("Medium") was pre-scheme prose,
> not a scheme severity value — dropped from frontmatter per REQ-SEV-03/REQ-SEV-09, preserved
> verbatim in the moved file's body.
