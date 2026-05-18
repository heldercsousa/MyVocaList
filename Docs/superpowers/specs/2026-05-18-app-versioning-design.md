# App Versioning Strategy — Design Spec
**Date:** 2026-05-18  
**Status:** Approved — plan written, pending execution

---

## Problem

The app currently has hardcoded version values (`ApplicationDisplayVersion=1.0.0`, `ApplicationVersion=1`) with no tooling, no scheme, and no process governing when or how the version changes. There is no way to distinguish a development build from a stable one, or to trace a binary back to a point in git history.

---

## Version Scheme

Format: `MAJOR.MINOR.PATCH-LABEL.HEIGHT`

| Segment | Example | Rule |
|---|---|---|
| `MAJOR` | `0` | Manual only — bumped on milestone release (v1.0 ship). Currently `0`. |
| `MINOR` | `2` | Bumped when a new coding feature begins (previous feature implicitly closed). |
| `PATCH` | `0` | Bumped for fix-only cycles (no new feature in scope). |
| `LABEL` | `alpha` | Always `alpha` until explicitly marked stable. Omitted on stable builds. |
| `HEIGHT` | `3` | Automatic — MinVer increments per commit after the last tag. |

### Examples

```
0.1.0-alpha.0    ← tag placed when first coding feature begins
0.1.0-alpha.4    ← 4 commits later, automatic (no action required)
0.1.0            ← marked stable via /project:release stable
0.2.0-alpha.0    ← next coding feature begins
0.2.0-alpha.1    ← one commit later
```

### Label chain

Current: `alpha → stable`  
Future-extensible: `alpha → beta → rc → stable` (no scheme change required — labels are additive)

---

## MAUI Property Mapping

.NET MAUI requires two separate version values in the `.csproj`:

```xml
<!-- Human-readable: store listing, About screen -->
<ApplicationDisplayVersion>$(MinVerVersion)</ApplicationDisplayVersion>

<!-- Store integer gate: Android versionCode / iOS CFBundleVersion -->
<!-- Must be monotonically increasing; never decreasing between store submissions -->
<ApplicationVersion>$(MinVerMajor)$(MinVerMinor:00)$(MinVerPatch:00)</ApplicationVersion>
```

Example output for tag `v0.2.3-alpha.5`:
- `ApplicationDisplayVersion` = `0.2.3-alpha.5`
- `ApplicationVersion` = `00203` → `203` (integer, always increases with MINOR/PATCH)

**Constraint:** `ApplicationVersion` must be a plain integer. The formula `MAJOR * 10000 + MINOR * 100 + PATCH` gives adequate headroom (max 999 minor versions, 99 patches per minor) and is human-readable.

---

## Tooling

**MinVer** (NuGet: `MinVer`) — git-tag-driven, zero config, MSBuild-native.

- Reads the nearest `vX.Y.Z[-label[.N]]` ancestor tag in git history
- Emits MSBuild properties: `$(MinVerVersion)`, `$(MinVerMajor)`, `$(MinVerMinor)`, `$(MinVerPatch)`, `$(MinVerPreRelease)`, `$(MinVerBuildMetadata)`
- No `.yml` config file required for this scheme
- Compatible with the existing conventional-commits workflow

**No other versioning tool required.** GitVersion and nbgv are evaluated and ruled out — see BACKLOG.md research notes.

---

## Automation — Version Bump Trigger

**Trigger:** Automatic prompt when a new coding feature begins (session where implementation tasks are about to be dispatched — spec/planning sessions do not trigger).

Claude detects the feature-start signal and asks before dispatching any subagent:

```
Starting new coding feature: [feature name]

Version bump before proceeding?
  bump  →  minor (new feature)  /  patch (fixes only)  /  skip
  label →  alpha  /  stable
```

Claude then:
1. Creates the git tag: `git tag v{MAJOR}.{MINOR}.{PATCH}-{LABEL}.0`
2. Pushes the tag: `git push origin v{...}`
3. Proceeds with feature dispatch

**On-demand command:** `/project:release` — for out-of-cycle version events:
- Marking a version stable after manual testing: `/project:release stable`
- Emergency patch tag: `/project:release patch alpha`

---

## Changelog Integration

The existing `changelog` skill (`Docs/Changelog/changelog.md`) continues unchanged. Git version tags create natural boundaries — release notes for a version can be extracted as all changelog entries between two consecutive tags.

No changes to the changelog format or workflow are required.

---

## Out of Scope

- CI/CD pipeline enforcement of monotonically increasing `ApplicationVersion` (no CI pipeline exists)
- Automated store submission or release channel management
- Beta / RC label stages (deferred — additive change when needed)
- Automatic MAJOR bump logic (MAJOR is always manual)
- Version badge in the app UI (separate feature if desired)

---

## Implementation Tasks (high-level)

1. Add MinVer NuGet package to the MAUI `.csproj`
2. Bind `ApplicationDisplayVersion` and `ApplicationVersion` to MinVer MSBuild properties
3. Place the initial git tag `v0.1.0-alpha.0` to establish baseline
4. Update `/project:commit` command to include the version bump prompt on coding-feature start
5. Create `/project:release` command
6. Update BACKLOG.md entry status from `🔵 Deferred` to reflect current state
7. Add versioning process note to `workflow.md` Rule 3 (commit discipline)
