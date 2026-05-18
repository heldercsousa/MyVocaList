# App Versioning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire MinVer into the MAUI `.csproj` so every build is automatically stamped with a semver derived from git tags, and add the `/project:release` command plus a version-bump prompt to `/project:commit`.

**Architecture:** MinVer reads the nearest `vX.Y.Z[-label[.N]]` git tag and emits MSBuild properties (`$(MinVerVersion)`, `$(MinVerMajor)`, `$(MinVerMinor)`, `$(MinVerPatch)`). The `.csproj` replaces the two hardcoded version lines with expressions using those properties. No config file is required. A new `.claude/commands/release.md` slash command covers out-of-cycle version events. The commit command gains a version-bump prompt section.

**Tech Stack:** MinVer NuGet, MSBuild property expressions, git tags, `.claude/commands/` markdown commands, `workflow.md`

---

## File Map

| File | Action | Change |
|------|--------|--------|
| `MyVocaList/MyVocaList.csproj` | Modify | Add MinVer `<PackageReference>`, replace hardcoded version properties |
| `.claude/commands/commit.md` | Modify | Add version-bump prompt section before Step 1 |
| `.claude/commands/release.md` | Create | New `/project:release` command |
| `.claude/rules/workflow.md` | Modify | Add versioning note to Rule 3 |
| `Docs/BACKLOG.md` | Modify | Update App Versioning entry status to `🟡 In Progress` |

---

## Task 1 — Add MinVer to the MAUI .csproj

**Files:**
- Modify: `MyVocaList/MyVocaList.csproj` (lines 15–18 `<ItemGroup>` with Scrutor, and lines 48–50 version block)

### Context
MinVer is a NuGet package that hooks into the MSBuild pipeline. Once added, it emits these properties at build time:
- `$(MinVerVersion)` → full semver string e.g. `0.1.0-alpha.4`
- `$(MinVerMajor)` → `0`
- `$(MinVerMinor)` → `1`
- `$(MinVerPatch)` → `0`

The formula for `ApplicationVersion` (must be a plain integer, monotonically increasing) is:
`MAJOR * 10000 + MINOR * 100 + PATCH` → e.g. `0*10000 + 1*100 + 0 = 100`

- [ ] **Step 1: Add MinVer PackageReference**

In `MyVocaList/MyVocaList.csproj`, add to the existing `<ItemGroup>` that contains Scrutor:

```xml
<ItemGroup>
  <PackageReference Include="Scrutor" />
  <PackageReference Include="MinVer" PrivateAssets="All" />
</ItemGroup>
```

`PrivateAssets="All"` prevents MinVer from appearing as a transitive dependency in consuming projects.

- [ ] **Step 2: Replace hardcoded version properties**

Still in `MyVocaList/MyVocaList.csproj`, replace the `<!-- Versions -->` block (currently lines 48–50):

```xml
<!-- Versions — driven by MinVer git tags (vX.Y.Z-label.height) -->
<ApplicationDisplayVersion>$(MinVerVersion)</ApplicationDisplayVersion>
<ApplicationVersion>$([MSBuild]::Add($([MSBuild]::Add($([MSBuild]::Multiply($(MinVerMajor), 10000)), $([MSBuild]::Multiply($(MinVerMinor), 100)))), $(MinVerPatch)))</ApplicationVersion>
```

> **Why the long expression?** MSBuild does not support arithmetic operators directly in property values. `$([MSBuild]::Add(...))` is the standard way to compute integer math in `.csproj` files.

- [ ] **Step 3: Restore packages**

```powershell
dotnet restore MyVocaList/MyVocaList.csproj
```

Expected: packages restored, no error about MinVer not found.

- [ ] **Step 4: Build to verify MinVer integration compiles**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Expected: build succeeds. MinVer will warn `No version tag found` or use a fallback version like `0.0.0-alpha.0` — this is expected before the tag is placed in Task 2. The build must not error.

- [ ] **Step 5: Commit**

```powershell
git add MyVocaList/MyVocaList.csproj
git commit -m "feat: wire MinVer into MAUI csproj for git-tag-driven versioning

- Add MinVer NuGet package (PrivateAssets=All)
- Replace hardcoded ApplicationDisplayVersion/ApplicationVersion with MinVer properties
- ApplicationVersion formula: MAJOR*10000 + MINOR*100 + PATCH

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2 — Place the initial git tag

**Files:** none (git operation only)

### Context
MinVer requires at least one tag matching `vX.Y.Z` or `vX.Y.Z-label` to compute versions. The tag format must start with `v`. The initial tag `v0.1.0-alpha.0` marks the start of the first tracked coding feature.

- [ ] **Step 1: Confirm you are on the `develop` branch and the tree is clean**

```powershell
git status
git branch --show-current
```

Expected: `develop`, no uncommitted changes (Task 1 commit is already done).

- [ ] **Step 2: Place the initial tag**

```powershell
git tag v0.1.0-alpha.0
```

No `-a` (annotated) flag needed — MinVer reads lightweight tags. If you want a message: `git tag -a v0.1.0-alpha.0 -m "baseline: first versioned build"`.

- [ ] **Step 3: Push the tag to remote**

```powershell
git push origin v0.1.0-alpha.0
```

Expected: `* [new tag] v0.1.0-alpha.0 -> v0.1.0-alpha.0`

- [ ] **Step 4: Verify MinVer picks up the tag**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android /p:MinVerVerbosity=Detailed 2>&1 | Select-String -Pattern "MinVer|version"
```

Expected output includes something like:
```
MinVer: Using version 0.1.0-alpha.0.
```

If the build was run in the same commit as the tag, `HEIGHT` is 0, so the version is `0.1.0-alpha.0` exactly.

---

## Task 3 — Update `/project:commit` with version-bump prompt

**Files:**
- Modify: `.claude/commands/commit.md`

### Context
The commit command must prompt Claude to ask the user about a version bump **before Step 1 (build check)**, but only when the session is starting a new coding feature. The prompt is informational — it tells Claude what to ask, not a hard automation step.

- [ ] **Step 1: Add version-bump prompt section to commit.md**

Open `.claude/commands/commit.md` and insert the following section **before** the `## Pre-Commit Checklist` block:

```markdown
## Version Bump Check (coding-feature start only)

If this commit marks the beginning of a new coding feature (i.e., implementation tasks are about to be dispatched for a feature that did not exist in the previous session), prompt the user before proceeding:

```
Starting new coding feature: [feature name]

Version bump before proceeding?
  bump  →  minor (new feature)  /  patch (fixes only)  /  skip
  label →  alpha  /  stable
```

If the user chooses `minor` or `patch`:
1. Compute the new version from the current latest git tag:
   - `git describe --tags --abbrev=0` → get latest tag (e.g. `v0.1.0-alpha.0`)
   - For `minor`: increment MINOR, reset PATCH to 0 → `v0.2.0-alpha.0`
   - For `patch`: increment PATCH → `v0.1.1-alpha.0`
   - For `stable`: strip label → `v0.1.0`
2. Create the tag: `git tag v{new-version}`
3. Push the tag: `git push origin v{new-version}`
4. Continue with the commit steps below.

If `skip`, continue with no tag change.

> **When NOT to prompt:** spec-only commits, docs-only commits, rule/CLAUDE.md updates, bug fixes, changelog-only commits, plan files. The prompt is for feature implementation sessions only.
```

- [ ] **Step 2: Build (no code change — docs only, build not required)**

Confirm `.claude/commands/commit.md` is saved with valid markdown.

- [ ] **Step 3: Commit**

```powershell
git add .claude/commands/commit.md
git commit -m "feat: add version-bump prompt to /project:commit for feature-start sessions

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4 — Create `/project:release` command

**Files:**
- Create: `.claude/commands/release.md`

### Context
`/project:release` handles out-of-cycle version events:
- Mark a version stable: `/project:release stable`
- Emergency patch tag: `/project:release patch alpha`
- Next minor (feature starting, out of commit flow): `/project:release minor alpha`

The command reads the current latest tag, computes the new version, creates and pushes the tag.

- [ ] **Step 1: Create `.claude/commands/release.md`**

```markdown
# Release Command

Create and push a version tag to mark a release milestone. Run this for out-of-cycle version events.

## Usage

- `/project:release stable` — strip pre-release label from current version (e.g. `0.1.0-alpha.0` → `0.1.0`)
- `/project:release minor alpha` — bump MINOR, reset PATCH, keep alpha label (e.g. `0.1.0` → `0.2.0-alpha.0`)
- `/project:release patch alpha` — bump PATCH only (e.g. `0.1.0-alpha.2` → `0.1.1-alpha.0`)
- `/project:release patch stable` — bump PATCH, stable label (e.g. `0.1.0` → `0.1.1`)

## Steps

1. **Read current version**

   ```powershell
   git describe --tags --abbrev=0
   ```

   Example output: `v0.1.0-alpha.0`

2. **Parse and compute new version** from the argument(s) provided:

   | Arg(s) | Rule | Example in → out |
   |--------|------|-----------------|
   | `stable` | Strip label entirely | `v0.1.0-alpha.2` → `v0.1.0` |
   | `minor alpha` | MINOR+1, PATCH=0, label=alpha.0 | `v0.1.0-alpha.2` → `v0.2.0-alpha.0` |
   | `minor stable` | MINOR+1, PATCH=0, no label | `v0.1.0` → `v0.2.0` |
   | `patch alpha` | PATCH+1, label=alpha.0 | `v0.1.0` → `v0.1.1-alpha.0` |
   | `patch stable` | PATCH+1, no label | `v0.1.0-alpha.2` → `v0.1.1` |

3. **Confirm with user before creating the tag**

   Show: `About to create tag: v{new-version}. Confirm? (yes/no)`

   Do NOT create the tag without user confirmation.

4. **Create and push the tag**

   ```powershell
   git tag v{new-version}
   git push origin v{new-version}
   ```

5. **Verify**

   ```powershell
   git describe --tags --abbrev=0
   ```

   Expected: `v{new-version}`

6. **Report**

   Print: `Tag v{new-version} created and pushed. Next build will use this version.`

## Notes

- MAJOR is always manual — no `/project:release major` command. Bump MAJOR by running `git tag vX.0.0` manually.
- Never delete or overwrite an existing tag — if a tag already exists at the target version, stop and report the conflict.
- After `/project:release stable`, the next feature should start with `/project:release minor alpha` or the version-bump prompt in `/project:commit`.
```

- [ ] **Step 2: Commit**

```powershell
git add .claude/commands/release.md
git commit -m "feat: add /project:release command for out-of-cycle version tagging

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5 — Update workflow.md Rule 3 with versioning note

**Files:**
- Modify: `.claude/rules/workflow.md`

### Context
Rule 3 covers commit discipline. A brief versioning note here ensures agents reading the workflow during a session understand when version tagging is expected. The note should not duplicate the full logic — just point to `/project:commit` and `/project:release`.

- [ ] **Step 1: Add versioning note to Rule 3**

In `.claude/rules/workflow.md`, locate the `## Rule 3 — Commit After Every Task` section. After the introductory paragraph, add:

```markdown
### Version tagging

Version tags are placed via the version-bump prompt in `/project:commit` (at coding-feature start) or on demand via `/project:release`. Do NOT place version tags manually during task commits — the command handles tag creation and push.

See `Docs/superpowers/specs/2026-05-18-app-versioning-design.md` for the full scheme (`MAJOR.MINOR.PATCH-label.height`).
```

- [ ] **Step 2: Commit**

```powershell
git add .claude/rules/workflow.md
git commit -m "amend: add versioning process note to workflow.md Rule 3

References: 2026-05-18-app-versioning-design.md

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6 — Update BACKLOG.md status

**Files:**
- Modify: `Docs/BACKLOG.md`

### Context
The BACKLOG entry for App Versioning Strategy is currently marked `🔵 Deferred`. It should be updated to `🟡 In Progress` now that implementation is underway, and eventually to `✅ Done` when all tasks complete.

- [ ] **Step 1: Locate and update the entry**

In `Docs/BACKLOG.md`, find the `### App Versioning Strategy` section. Update its status indicator from `🔵 Deferred` to `🟡 In Progress`.

Also update the `**Target pattern:**` description to match the approved spec scheme:

Old:
```
**Target pattern:** `MAJOR.MINOR.BUILD` — e.g. `0.1.42`
- `MAJOR` (AA): release milestone — currently `0` (pre-release). Bumped manually on milestone ship.
- `MINOR` (BBB): stable feature count — bumped per merged feature (conventional commits + git tag trigger).
- `BUILD` (CCC): monotonically increasing integer — derived from commit height since last tag; maps to Android `versionCode` / iOS `CFBundleVersion`.
```

New:
```
**Target pattern:** `MAJOR.MINOR.PATCH-LABEL.HEIGHT` — e.g. `0.1.0-alpha.4`
- `MAJOR`: milestone release — manual only (currently `0`).
- `MINOR`: bumped when a new coding feature begins.
- `PATCH`: bumped for fix-only cycles.
- `LABEL`: always `alpha` until stable. Omitted on stable builds.
- `HEIGHT`: automatic — MinVer increments per commit after the last tag.

**Tooling:** MinVer NuGet. Spec: `Docs/superpowers/specs/2026-05-18-app-versioning-design.md`.
```

- [ ] **Step 2: Commit**

```powershell
git add Docs/BACKLOG.md
git commit -m "docs: update BACKLOG.md App Versioning entry to In Progress

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Self-Review

### Spec coverage check

| Spec requirement | Task |
|-----------------|------|
| MinVer NuGet added to MAUI .csproj | Task 1 |
| `ApplicationDisplayVersion` bound to `$(MinVerVersion)` | Task 1 |
| `ApplicationVersion` bound to MAJOR*10000+MINOR*100+PATCH | Task 1 |
| Initial tag `v0.1.0-alpha.0` placed and pushed | Task 2 |
| Build verifies MinVer picks up tag | Task 2 |
| `/project:commit` version-bump prompt | Task 3 |
| `/project:release` command | Task 4 |
| workflow.md Rule 3 versioning note | Task 5 |
| BACKLOG.md status updated | Task 6 |

All 9 spec requirements are covered. ✓

### Placeholder scan
No "TBD", "TODO", or "implement later" present. All code blocks contain real content. ✓

### Type consistency
MSBuild property names (`$(MinVerVersion)`, `$(MinVerMajor)`, `$(MinVerMinor)`, `$(MinVerPatch)`) are consistent across Tasks 1 and 2. Command argument names (`minor`, `patch`, `stable`, `alpha`) are consistent across Tasks 3 and 4. ✓
