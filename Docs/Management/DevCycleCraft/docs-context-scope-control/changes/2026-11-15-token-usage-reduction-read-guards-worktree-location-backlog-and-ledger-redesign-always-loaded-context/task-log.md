# Task log — Token-usage reduction

Plan: `plan.md` (approved by Helder 2026-11-15; workstreams A, B, C now; D and E after a brainstorm).

### Checkpoint

- **Updated:** 2026-11-15
- **Branch / worktree:** develop / main tree (hooks and docs only)
- **Step:** Steps 1 to 4 done (A, B, C); D and E next
- **Context manifest:** `plan.md` · `README.md` (this item) · `.claude/settings.json` · `.claude/scripts/backlog/render.py` · `CLAUDE.md`

## Steps 2 + 3 — B, A1, A4: one worktree location, worktree-aware guard, Bash notice

**Status:** Done (2026-11-15), commit `54ea4336`. Leftover clean-up is **pending Helder** (below).

### Changed files

- `.gitignore`, `.claude/settings.json` (`worktree.baseRef: head`; `bash-guard.py` on `PreToolUse` `Bash|PowerShell`)
- `.claude/agents/orchestrator.md`, `.claude/library/workflow-rule-2.md`, `.claude/library/session-ops.md`, `.claude/commands/sln-ledger.md`, `.claude/scripts/constitutional-guard.py`, `Docs/Management/LEDGER.md` (column note): `.worktrees/` → `.claude/worktrees/`
- `.claude/scripts/bash-guard.py` (new), `.claude/scripts/hook_out.py` (new), `.claude/scripts/read_budget.py`, `.claude/scripts/tests/test_bash_guard.py` (new)

### Verification evidence

- Live: `git worktree add ../MyVocaList-guard-test ...` was denied; nothing was created.
- Live: `cat` of a 300-line scratch file returned the READ BUDGET note and still ran.
- A false positive was found and fixed before the commit: a commit message that only mentioned the command was blocked. The guard now ignores heredoc bodies and matches only at the start of a command (regression test added).
- 12 tests in `test_bash_guard.py`, OK.

### Leftovers found (read-only checks; nothing deleted) — pending Helder

| What | Finding |
|------|---------|
| `.worktrees/safearea-all`, `.claude/worktrees/agent-a277ea963132adde0`, `.claude/worktrees/develop-merge` | Empty folders, not checkouts. |
| `.worktrees/_orch` | 22 untracked scratch files (logs, briefing notes, small scripts), newest 2026-11-04. Not a checkout. |
| 7 `worktree-agent-*` branches not merged into `develop` | Each holds 0 commits that are not already in `develop` or `main` (their tips are "Merge develop into main" commits). |
| 30 `worktree-agent-*` branches merged into `develop` | No live worktree. |

After the clean-up, the `.worktrees/` line can leave `.gitignore`.

## Step 4 — C: reminders that did not reach the model

**Status:** Done (2026-11-15)

### C1 — delivery rule (Claude Code hooks reference, read 2026-11-15)

> "On these events, your plain-text stdout reaches Claude or the compaction summary: `UserPromptSubmit`, `UserPromptExpansion`, `SessionStart`, and `PostModelSwitch`: Claude Code adds plain-text stdout as context that Claude can see and act on. [...] `PreCompact`: your stdout becomes instructions for the compaction summary."

> "When a subagent calls a tool, tool events such as `PreToolUse` and `PostToolUse` fire the same configured hooks as in the main conversation, and the input carries the `agent_id` and `agent_type` common input fields that identify the subagent."

> "PostCompact hooks have no decision control." (and `systemMessage` is discarded there)

So every reminder listed in workstream C was invisible to the model, and so is the `PostCompact` echo.

### Changed files

- `.claude/scripts/library-size-guard.py`, `.claude/scripts/ledger-row-guard.py` (hook mode): `additionalContext`
- `.claude/scripts/delegate-reminder.py`: `additionalContext`; silent when the input has `agent_id`
- `.claude/scripts/worktree-base-check.py` + its test: reports MISBASED only, as `additionalContext`; BEHIND notes removed
- `.claude/scripts/uncommitted-warning.py` (new): replaces the two inline one-liners (`--after-agent` → model; `--stop` → `systemMessage` for Helder)
- `.claude/scripts/backlog/orphan_check.py`: `systemMessage`
- `.claude/settings.json`: inline TDD reminder deleted (it never matched a worktree path; home of the rule: `testing.md § TDD within SDD`)
- `.claude/library/workflow-hooks-invariant.md`: hook table and the delivery rule

### Verification evidence

- `.claude/scripts/tests`: 99 tests OK; `lease/tests`: 46 OK; `backlog/tests`: 180 OK.
- Piped payloads: delegate reminder prints JSON for the main agent and nothing with `agent_id`; `uncommitted-warning.py --stop` prints `systemMessage`; `--after-agent` prints `additionalContext`.
- Not checked live: the library and ledger notes (they fire only on an over-limit edit). They use the same JSON form as the task-log guard, which was seen to reach the model on 2026-11-14.

### Open point — pending Helder

- `PostCompact` echo ("CONTEXT RESTORED ...") reaches nobody. Options: delete it, or move a one-line version to a `SessionStart` hook with matcher `compact` (that event's output does reach the model). Part of workstream E.

## Step 1 — A2 + A3: slice cap and size budget

**Status:** Done (2026-11-15)

### Changed files

- `.claude/scripts/read_budget.py` (new): budgets and `decide(path, offset, limit)`
- `.claude/scripts/read-slice-guard.py`: now a thin wrapper over `read_budget.decide`
- `.claude/scripts/tests/test_read_budget.py` (new): 12 tests

### What changed

- Full read allowed only within 200 lines **and** 12 KB.
- Over budget: `limit` is required and capped at 250 lines; `offset` alone no longer passes.
- A slice is also capped at 20 KB, measured on the actual lines requested. The plan said "about 12 KB"; 20 KB was chosen so a 250-line slice of ordinary code (about 50 to 80 bytes per line) is not refused.
- `BACKLOG.md` denial points to `backlog_gen.py query`.
- New: files with a NUL byte in the first 8 KB (images) and `.pdf` / `.ipynb` pass. Before, an image over 200 "lines" could be denied by accident.

### Verification evidence

- `python -m unittest test_read_budget`: 12 tests, OK.
- Live: `Read Docs/Management/BACKLOG.md` with no limit was denied with the query hint (144 lines, 35.2 KB).
- Piped payloads: `BACKLOG.md` with `offset 1, limit 30` allowed; garbage stdin allowed (fail-open).
