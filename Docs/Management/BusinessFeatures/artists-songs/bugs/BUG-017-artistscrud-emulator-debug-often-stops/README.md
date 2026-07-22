---
id: BUG-017
title: "BUG-017: form pages `navigate_next` icon missing — Glide exception per render (Major)"
status: "✅ Fixed"
severity: Major
target: 2026-06-27
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 50
closed: 2026-06
goal: "Fixed (icon replaced with an existing SVG); emulator-verified 2026-07-03. Duplicate BACKLOG row consolidated here."
pointer: BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/
---

# BUG-017: form pages `navigate_next` icon missing

Glide raised an exception on every render of the CRUD form pages because the
`navigate_next` icon asset was absent. Fixed by replacing it with an existing SVG;
emulator-verified. Detail: the task log and manual-test log in this folder.
