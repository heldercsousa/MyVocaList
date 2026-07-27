---
id: BUG-020
title: "BUG-020: SongsPage FAB crash — unguarded SecureStorage in async void OnAppearing (Critical)"
status: "✅ Fixed"
severity: Critical
target: 2026-07-01
section: BusinessFeatures
parent: artists-songs
kind: bug
closed: 2026-07
order: 10
goal: "Fixed with try-catch fallback + regression test; emulator-verified 2026-07-03."
pointer: BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-020-songspage-fab-crash-secure-storage/
---

# BUG-020: SongsPage FAB crash — unguarded SecureStorage in async void OnAppearing

Fixed with a try-catch fallback around the `SecureStorage.GetAsync` call plus a regression
test; emulator-verified 2026-07-03.

> Migrated from the 2026-07 archive row (T12a Wave O). The pre-existing flat file
> `BUG-020-songspage-fab-crash-secure-storage.md` was moved in with `git mv`, nothing
> deleted. Severity (Critical) was already scheme-compliant in the source. Goal text is
> transcribed verbatim from the archived BACKLOG Notes cell (`closed: 2026-07`).
