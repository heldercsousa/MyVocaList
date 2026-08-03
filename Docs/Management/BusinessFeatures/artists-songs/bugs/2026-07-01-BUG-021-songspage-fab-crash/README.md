---
id: BUG-021
title: "BUG-021: SongsPage FAB crash — `ISimilarityScorer` not registered in DI (Critical)"
status: "✅ Fixed"
severity: Critical
target: 2026-07-01
section: BusinessFeatures
parent: artists-songs
kind: bug
order: 20
closed: 2026-07
goal: "Fixed via `AddAppServices()` extension + DI regression tests; emulator-verified 2026-07-03."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-021-songspage-fab-crash/
---

# BUG-021: SongsPage FAB crash — `ISimilarityScorer` not registered

Tapping the SongsPage FAB crashed because `ISimilarityScorer` was never registered in
the container. Fixed by an `AddAppServices()` registration extension with DI regression
tests. Detail: the bug note and exception capture in this folder.
