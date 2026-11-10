---
id: release-tag-discipline-mechanically-enforced-tag-creation
title: Release tag discipline — mechanically enforced tag creation
status: 💡 Pending
target: 2026-11-09
section: DevCycleCraft
parent: app-versioning
goal: A tag is created and pushed at every mandatory moment, and the build or a hook fails when one is missing, so no session can skip it.
gate: Helder approves the trigger list and the enforcement points.
kind: change
---

# Release tag discipline — mechanically enforced tag creation

A tag is created and pushed at every mandatory moment, and the build or a hook fails when one is missing, so no session can skip it.

## Origin

Helder, 2026-11-09, during BID (build identity): tag creation must be a must-do at defined moments,
and "if Claude's guidelines must be updated, you must ensure it will certainly be executed when
needed". A rule in prose is not enough; each moment needs a mechanical check.

## Proposed mandatory moments (draft, for Helder)

1. Before merging a change to the version scheme or the update check (BID precedent: D11).
2. Before raising `latestVersion` / `minRequiredVersion` in `version-manifest.json`.
3. Before any build leaves the dev machine (tester APK, store upload) → bump the prerelease.
4. When a milestone closes → bump the minor version.

## Proposed enforcement (draft)

| Moment | Check | Where it fails |
|--------|-------|----------------|
| 2 (and 1) | MinVer version < `minRequiredVersion` in the local `version-manifest.json` | MSBuild `<Error>` in the csproj, every build |
| 3 | Release build of a commit with no exact tag (MinVer height > 0) | MSBuild `<Error>` when `Configuration=Release` |
| 1, 4 | Merge into `develop` touching the version targets or `version-manifest.json`, or a milestone item set to ✅, with no new tag | Claude Code `PreToolUse` hook on `git merge` / `backlog_gen.py status` |
| all | Tag exists locally but not on `origin` | `/sln-release` final step checks `git ls-remote --tags` |

The prose rule goes in `/sln-release` and `workflow.md` (both human-reviewed per CLAUDE.md
Authorship). The checks above are what make it certain.
