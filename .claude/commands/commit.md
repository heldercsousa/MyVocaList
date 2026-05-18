# Commit Command

Perform a full commit and push cycle for the MyVocaList project. Follow these steps exactly:

## Pre-Commit Checklist
- [ ] Build is clean: run `/project:build` and confirm 0 errors
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
   - Include ALL changed files: code, specs (`Docs/specs/`), plans (`Docs/specs/[feature]/plan.md`, `Docs/DevEnv/plans/`), rules (`.claude/rules/`), `CLAUDE.md`, command files (`.claude/commands/`), and changelog.
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

9. **Run changelog command**: After committing, always run `/project:changelog` to verify the changelog entry.