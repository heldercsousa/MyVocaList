# Commit Command (`/sln-commit`)

Perform a full commit and push cycle for the MyVocaList project. Follow these steps exactly:

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

## Pre-Commit Checklist
- [ ] Build is clean: run `/sln-build` and confirm 0 errors
- [ ] No half-finished work (no TODO markers left in modified code unless pre-existing)
- [ ] No non-English text introduced
- [ ] No `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` in modified files

## Files to NEVER Commit
- `.claude/settings.local.json`
- `bin/`, `obj/`, `.vs/` directories
- `*.user` files
- Any file containing secrets or API keys

## Steps

1. **Build check**: Run `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`. Stop if errors.

2. **Stage changes**: Stage specific files — never `git add -A` blindly. Review what is being staged.
   - Include ALL changed files: code, specs (`Docs/specs/`), plans (`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/plan.md`, `Docs/DevEnv/plans/`), rules (`.claude/rules/`), `CLAUDE.md`, command files (`.claude/commands/`), and changelog.
   - Any file touched as part of the task belongs in the commit.

3. **Review staged diff**: Run `git diff --cached --stat` to confirm staged files are intentional.

4. **Update changelog**: Update `Docs/Changelog/changelog.md` with a new entry:
   ```
   - **MM/dd/yyyy** - <type> - <description>
   ```
   Types: `feat` `fix` `refactor` `docs` `perf` `test`

5. **Generate commit message** following this format:
   ```
   <type>: <summary>

   - detail 1
   - detail 2

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   ```
   Types: `feat:` `fix:` `refactor:` `docs:` `perf:` `test:`

6. **Commit**: `git commit` with the generated message (use HEREDOC).

7. **Push**: `git push` to remote.

8. **Confirm**: Report the commit hash and what was pushed.

9. **Run changelog command**: After committing, always run `/sln-changelog` to verify the changelog entry.