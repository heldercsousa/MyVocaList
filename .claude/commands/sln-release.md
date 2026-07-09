# Release Command (`/sln-release`)

Create and push a version tag to mark a release milestone. Run this for out-of-cycle version events.

## Usage

- `/sln-release stable` — strip pre-release label from current version (e.g. `0.1.0-alpha.0` → `0.1.0`)
- `/sln-release minor alpha` — bump MINOR, reset PATCH, keep alpha label (e.g. `0.1.0` → `0.2.0-alpha.0`)
- `/sln-release patch alpha` — bump PATCH only (e.g. `0.1.0-alpha.2` → `0.1.1-alpha.0`)
- `/sln-release patch stable` — bump PATCH, stable label (e.g. `0.1.0` → `0.1.1`)

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

- MAJOR is always manual — no `/sln-release major` command. Bump MAJOR by running `git tag vX.0.0` manually.
- Never delete or overwrite an existing tag — if a tag already exists at the target version, stop and report the conflict.
- After `/sln-release stable`, the next feature should start with `/sln-release minor alpha` or the version-bump prompt in `/sln-commit`.
