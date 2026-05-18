# Plan: Enforce Automated Commit, Test, and Review After Subagent Work

## Context

Prior sessions delegated feature work to subagents but changes were never committed, tests were never run, and no review was triggered. The root causes are:

1. **`TaskCompleted` hook skips tests** — the CHECK BUILD SEQUENCE only runs `dotnet build`, never `dotnet test`.
2. **`Stop` hook only warns** — uncommitted changes produce a warning but are never auto-committed.
3. **No hook fires after `Agent` tool returns** — when the main agent dispatches a subagent informally (without `TaskCreate`/`TaskUpdate`), `TaskCompleted` never fires, so nothing commits.
4. **Review is manual** — no hook invokes `/project:review` after a task completes.

## Approach

Edit `.claude/settings.json` (the project settings file — not blocked by any deny rule). Apply all changes in a single atomic edit. The deny list (`CLAUDE.md`, `.claude/rules/*.md`) is unrelated to `.json` files and does not block these edits, but per the user's instruction, temporarily remove it before editing and restore it after.

### Change 1 — `TaskCompleted` hook: add `dotnet test` after build succeeds

In the CHECK BUILD SEQUENCE of the `TaskCompleted` agent prompt, after a successful `dotnet build`, add:
```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-build -v q 2>&1 | tail -30
```
- If tests pass → mark "To Review", update changelog, commit, push, **then spawn a review subagent** (read the task-log changed-files section, verify each file against the spec's acceptance criteria, append "Review task done" to task-log, commit).
- If tests fail → mark "Test failure", append test failure summary, commit, stop.

### Change 2 — `Stop` hook: upgrade from warn-only to auto-commit agent

Replace the current warn-only `Stop` command with an **agent** that:
1. Runs `git status --porcelain`
2. If uncommitted changes exist:
   a. Runs `dotnet build MyVocaList.sln --no-restore -v q`
   b. If build passes → runs `dotnet test --no-build -v q`
   c. If build+test pass → `git add -A && git commit -m "chore: auto-commit at session end" && git push origin HEAD`
   d. If build or test fails → reports failure details and asks main agent to fix before stopping

### Change 3 — `PostToolUse(Agent)`: commit guard after every subagent call

Add a new `PostToolUse` hook matching `Agent` tool. After every `Agent` call completes, run a lightweight command that checks for uncommitted changes and prints a reminder (not auto-commit — the auto-commit is the Stop hook's job; this is a faster in-session nudge):
```
git status --porcelain | grep -q . && echo '⚠️ Subagent returned with uncommitted changes — run /project:commit now.'
```

## Execution Steps

1. **Disable deny list**: Remove `permissions.deny` block from `settings.json`
2. **Apply Change 1**: Update `TaskCompleted` agent prompt to add test step and inline review dispatch
3. **Apply Change 2**: Replace `Stop` command hook with an agent hook (type: `agent`)
4. **Apply Change 3**: Add `PostToolUse` matcher for `Agent` tool
5. **Restore deny list**: Re-add `permissions.deny` to `settings.json`
6. **Verify**: Run `SessionStart` hook health check mentally (all expected hooks present: TaskCreated, PreToolUse, PostToolUse, PostCompact, TaskCompleted, Stop, SessionStart)
7. **Commit**: `git add .claude/settings.json && git commit -m "amend: enforce auto-commit, test, and review after subagent work"`

## Files Modified

- `.claude/settings.json` — all hook changes live here

## Verification

After applying:
- `SessionStart` hook should report HOOK HEALTH OK with all 7 hooks present
- Next time a subagent completes work and the session stops with uncommitted changes, the `Stop` agent should auto-commit
- Next time a task is completed via `TaskCreate`/`TaskUpdate`, `dotnet test` should run automatically
- After every `Agent` tool call returns, `PostToolUse` should check for uncommitted changes

## Risk

- `Stop` hook is now an agent (type: `agent`) with a 300s timeout — this adds latency to session end. Acceptable trade-off.
- Auto-commit on session end could commit partial work. Mitigated: the agent runs build+test first; only commits if both pass.
