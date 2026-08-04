---
id: BUG-024
title: "BUG-024: SongForm edit-mode Save silently wipes fields (Critical)"
status: "✅ Fixed"
severity: Critical
target: 2026-07-02
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 40
closed: 2026-07
goal: "Fixed with full edit hydration + 7 regression tests; emulator re-run pending on BUG-027."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-02-BUG-024-songform-edit-data-loss/
---

# BUG-024: SongForm edit-mode Save silently wipes fields

Saving in edit mode discarded fields the form had not hydrated. Fixed with full edit
hydration plus regression tests. Detail: the bug note in this folder.
