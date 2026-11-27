---
id: versioncode-counts-all-commits-so-a-develop-build-is-never-below-a-branch-build
title: versionCode counts all commits so a develop build is never below a branch build
status: ✅ Done
target: 2026-11-27
parent: app-versioning
goal: Debug APKs from develop install over APKs built from task branches
closed: 2026-11
kind: change
---

# versionCode counts all commits so a develop build is never below a branch build

Debug APKs from develop install over APKs built from task branches

## Problem
REQ-BID-05 (2026-11-09 change) sets `versionCode` = `git rev-list --count --first-parent HEAD`.
A merge into `develop` adds 1 first-parent commit, while the task branch counts all its commits.
An APK built on a task branch (`8270f468` → 2061) was higher than the later develop APKs
(`a3547ff3` → 2053), so Android refused the update ("app não instalado").

## Change
- **REQ-BID-05 (amended)** — THE Android `versionCode` SHALL equal `git rev-list --count HEAD`
  (all reachable commits). A merge commit contains every commit of the merged branch, so a develop
  build is never below a build of a branch it has merged. D2's properties are kept: same commit gives
  the same value, it does not change on every build, and tags cannot reset it.
- Value jump: 2053 → 2979 for `a3547ff3`. It only goes up, so the update is allowed.

## Verification
Manual: build and check the manifest `versionCode` ≥ 2979; install over the 2061 APK.
