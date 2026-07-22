---
id: BUG-018
title: "BUG-018: ArtistFormPage Edit Save — fatal EF Core duplicate-tracking crash (Critical)"
status: "✅ Fixed"
severity: Critical
target: 2026-06-27
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 60
closed: 2026-06
goal: "Fixed (global NoTracking + read models); regression test green."
pointer: BusinessFeatures/artists-songs/bugs/BUG-018-artistformpage-edit-save-crash/
---

# BUG-018: ArtistFormPage Edit Save — duplicate-tracking crash

Saving an edited artist crashed fatally because EF Core tracked two instances of the
same entity. Fixed with global NoTracking plus dedicated read models. Detail: the plan
and task log in this folder.
