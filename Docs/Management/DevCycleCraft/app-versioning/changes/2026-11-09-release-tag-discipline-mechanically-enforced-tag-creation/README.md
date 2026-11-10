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

> **Self-contained handoff.** Everything gathered in the BID session (2026-11-09/10) is below, so the
> next session reviews and implements rather than re-researching. Start with brainstorming → spec
> (requirements.md + design.md), using this README as the input.

## Helder's rulings

- **2026-11-09:** tag creation must be a must-do at defined moments. "If Claude's guidelines must be
  updated, you must ensure it will certainly be executed when needed": prose alone is not enough;
  every moment needs a mechanical check.
- **2026-11-10:** "I don't want to be asked for when to push tag." Context decides, not Helder:
  Claude picks the moment and the bump level, creates and pushes the tag, and **reports** it (no
  confirmation prompt). Helder is learning the process from what the project builds, so the rule
  text must explain *why* each moment needs a tag. Claude drafts the prose in the proper places;
  Helder reviews it (CLAUDE.md § Authorship: context files are never committed unreviewed).

## How versioning works today (verified 2026-11-09)

- **MinVer 6.1.0**, tag prefix `v`. Version = latest reachable tag + first-parent height
  (tag `v0.2.0-alpha.0` + 4 commits → `0.2.0-alpha.0.4`). No reachable tag → `0.0.0-alpha.0.N`.
- `MyVocaList/MyVocaList.csproj` target `SetVersionFromMinVer` (DependsOnTargets `MinVer`,
  BeforeTargets `_GetAndroidPackageName;GenerateAssemblyInfo`) sets `ApplicationDisplayVersion` to
  the MinVer version and `ApplicationVersion` (versionCode) to
  `git rev-list --count --first-parent HEAD`. Properties set at csproj *evaluation* cannot see
  `$(MinVerVersion)`, so any new check must also be a target that depends on `MinVer`.
- `Services/VersionCheckService.cs` compares `AppInfo.VersionString` (NuGetVersion) with the live
  manifest on `main` (repo-root `version-manifest.json`: `latestVersion` 0.1.0,
  `minRequiredVersion` 0.1.0). current < minRequired → forced-update sheet. A prerelease sorts
  *below* its release (AC-UC-06), so `0.1.0-alpha.N` < `0.1.0` → forced update. That is why BID
  needed tag `v0.2.0-alpha.0` before merging (BID design D11, REQ-BID-14).
- Tags: `v0.1.0-alpha.0`; `v0.2.0-alpha.0` (annotated, 2026-11-09, pushed).
- Release APKs come from `MyVocaList/Platforms/Android/releaseApk.bat` (Helder's desktop shortcut).

## Current guideline text that conflicts with the rulings

| File | Where | Conflict → change |
|------|-------|-------------------|
| `.claude/commands/sln-release.md` | step 3 | "Confirm with user before creating the tag" → report after pushing. |
| `.claude/commands/sln-release.md` | step 4 | Lightweight `git tag` → annotated `git tag -a -m "<trigger + reason>"`. |
| `.claude/commands/sln-release.md` | step 5 | Local verify only → add `git ls-remote --tags origin v{new}`. |
| `.claude/commands/sln-commit.md` | lines 5–29, Version Bump Check | Prompts the user at every coding-feature start → replace with a pointer to the trigger table. |

## Proposed mandatory moments (Claude decides; no prompt)

| # | Trigger | Bump | Why (goes into the rule text) |
|---|---------|------|-------------------------------|
| 1 | A merge changes the version scheme, the update check or the manifest semantics | enough that the new build ≥ `minRequiredVersion` | Otherwise every build hits the forced-update gate (BID D11). |
| 2 | Before raising `latestVersion` / `minRequiredVersion` in `version-manifest.json` | tag ≥ the new value, **first** | Builds below the minimum are forced to update; dev builds must not be. |
| 3 | A build leaves the dev machine (tester APK via `releaseApk.bat`, store upload) | prerelease `alpha.N+1` | Each distributed APK maps to one exact tag, so a bug report names a commit. |
| 4 | A milestone / MVP feature set closes ✅ | MINOR, `alpha.0` | Marks the base for the next alpha series. |
| 5 | Stable store release | `stable` (strip label) | Store versions carry no prerelease label. |

## Proposed enforcement

| Moment | Check | Where it fails |
|--------|-------|----------------|
| 1, 2 | MinVer version < `minRequiredVersion` in the local `version-manifest.json` | MSBuild `<Error>` in a target depending on `MinVer` — every build |
| 3, 5 | `Configuration=Release` and MinVer height > 0 (no exact tag) | MSBuild `<Error>` — `releaseApk.bat` cannot produce an untagged APK |
| 1, 4 | `git merge` into develop/main touching the version targets or the manifest, or `backlog_gen.py status <milestone> ✅`, with no new tag | Claude Code `PreToolUse` hook (`.claude/settings.json`; list in `workflow-hooks-invariant.md`) |
| all | Tag local but not on `origin` | `/sln-release` step 5 `git ls-remote` |

Open questions for the spec: (a) moment 3 means every Release build needs a fresh tag — should
`releaseApk.bat` fail with a clear message telling Helder to ask Claude for the tag, or should a
script create it? (b) how does the hook know an item is a milestone (frontmatter field?).

## Prose to draft (Helder reviews before commit)

- `/sln-release`: trigger table + "no confirmation; report" + annotated tag + remote verify.
- `/sln-commit`: Version Bump Check → pointer to the trigger table.
- `workflow.md`: one never-miss line — tags are created at the `/sln-release` moments; build and
  hooks enforce them.
- Use `superpowers:writing-skills` if the commands are restructured.
