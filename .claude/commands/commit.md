Perform a full commit and push cycle for the MyVocaList project. Follow these steps exactly:

1. **Build check**: Run `dotnet build` and verify it succeeds. If there are build errors, stop and report them — do not commit broken code.

2. **Changelog**: Update `Docs/Changelog/changelog.md` with a new entry for the changes being committed. Format: `- **MM/dd/yyyy** - Enhancement|Fix - Description`

3. **Stage all changes**: Run `git add -A`

4. **Review staged diff**: Run `git diff --cached --stat` to understand what is being committed.

5. **Generate commit message**: Based on the diff, write a message following the project format:
   ```
   <type>: <summary>

   - detail 1
   - detail 2

   Co-Authored-By: Claude <noreply@anthropic.com>
   ```
   Types: `feat:`, `fix:`, `refactor:`, `docs:`, `perf:`, `test:`

6. **Commit**: Run `git commit` with the generated message.

7. **Push**: Run `git push` to the remote.

8. **Confirm**: Report the commit hash and what was pushed.
