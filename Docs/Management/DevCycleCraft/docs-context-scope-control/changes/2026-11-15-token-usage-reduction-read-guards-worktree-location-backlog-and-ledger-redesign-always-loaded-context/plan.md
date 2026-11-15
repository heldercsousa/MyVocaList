# Plan — Token-usage reduction, workstreams A, B, C

Item: `Docs/Management/DevCycleCraft/docs-context-scope-control/changes/2026-11-15-token-usage-reduction-.../README.md`

## Context

The item records five workstreams to cut tokens spent on reading. Helder's decisions (2026-11-15, this session):

- Implement **A, B, C now**. D (backlog/ledger) and E (always-loaded context) get a brainstorm afterwards, with their own approval.
- Worktree location: **`.claude/worktrees/`** (the harness default; all 8 live worktrees are already there).
- A4: **warn-only** notice for `cat`-like Bash/PowerShell reads of large files.
- Add **`"worktree": {"baseRef": "head"}`** to `.claude/settings.json`.

New facts found while planning:

- **Read (Claude Code worktrees doc):** the harness creates worktrees under `.claude/worktrees/<name>/`; only a `WorktreeCreate` hook can move them. Harness worktrees branch from the remote default branch (`main`) unless `worktree.baseRef` is `"head"`. This is the likely cause of the misbased `worktree-agent-*` branches.
- **Measured:** `.claude/worktrees/` is ignored only through the local `.git/info/exclude`, not through `.gitignore`. `.gitignore` line 516 ignores `.worktrees/`.
- **Measured:** two more folders under `.claude/worktrees/` are not in `git worktree list`: `agent-a277ea963132adde0` and `develop-merge`. `.worktrees/_orch` holds scratch files (logs, small scripts), not a checkout.
- **Measured:** the 7 unmerged `worktree-agent-*` branches each have a tip that is a "Merge develop into main" commit, 1 to 4 commits ahead of `develop`. Whether those commits are all in `main` is **not yet checked**.
- **Read:** the inline TDD reminder tests `fp.startswith(<main checkout>/MyVocaList.Services)`. Service edits happen in worktrees, so it never matches there.
- **Assumed, not yet confirmed in the hooks doc:** exit-0 plain stdout reaches the model only for `UserPromptSubmit` and `SessionStart`. Step C1 confirms this before anything is converted.
- A file referenced with `@` in a prompt is included without a tool call, so no `PreToolUse` guard sees it. Known gap, not addressed here.

## Step 0 — Housekeeping

1. Copy this plan to `<item folder>/plan.md`; delete `Docs/Management/ancient-cuddling-stroustrup.md` and the `.sln` entry the Write hook added for it.
2. `backlog_gen.py status <item-id> "🟡 In Progress"`; create `<item folder>/task-log.md` with a `### Checkpoint` block.

## Step 1 — A2 + A3: slice cap and size budget (`.claude/scripts/read-slice-guard.py`)

Rewrite the decision into one function `decide(path, offset, limit) -> reason | None`, so it can be unit-tested:

- Constants: `MAX_FULL_LINES = 200`, `MAX_FULL_BYTES = 12_000`, `MAX_SLICE_LINES = 250`.
- A file within both full-read budgets passes, whatever `offset`/`limit` say.
- Over a budget:
  - no `limit` (with or without `offset`): deny. The Read default would return up to 2000 lines.
  - `limit > 250`: deny, message names the cap.
  - `limit <= 250`: allow.
- The byte budget also applies to slices: a slice is denied when the file's average line is so long that `limit` lines exceed about 12 KB. Message: lower `limit`, or use `Grep`.
- `Docs/Management/BACKLOG.md`: specific message pointing to `python .claude/scripts/backlog/backlog_gen.py query --status "..."`.
- Deny text corrected to "`limit` <= 250 lines per slice".
- Exemptions stay as today in this step (changed in Step 3).

Tests: new `.claude/scripts/tests/test_read_slice_guard.py`, same style as `.claude/scripts/lease/tests/`, with temp files for each branch above.

## Step 2 — B: one worktree location, `.claude/worktrees/`

1. `.gitignore`: add `.claude/worktrees/`. Keep the `.worktrees/` line until the leftovers are gone.
2. `.claude/settings.json`: add `"worktree": {"baseRef": "head"}`.
3. Replace `.worktrees/` with `.claude/worktrees/` in:
   - `.claude/agents/orchestrator.md` (7 lines, § Git Worktrees as Isolation Primitive)
   - `.claude/library/workflow-rule-2.md` line 45
   - `.claude/library/session-ops.md` line 190
   - `.claude/commands/sln-ledger.md` line 35
   - `.claude/scripts/constitutional-guard.py` (docstring line 90, block message line 114)
   - `.claude/scripts/lease/tests/test_lease_lib.py` keeps its fixture string (it tests a path function, not the convention).
4. Refuse other locations: new `.claude/scripts/bash-guard.py` (PreToolUse, `Bash|PowerShell`). It denies `git worktree add <path>` when `<path>` does not resolve under `<repo>/.claude/worktrees/`. The deny message gives the correct command. Fail-open on parse errors.
5. `worktree-base-check.py`: no logic change (it reads `git worktree list`, so it is location-independent).
6. Leftover report, **no deletion in this step**. One table for Helder, built from read-only commands:
   - `.worktrees/_orch`, `.worktrees/safearea-all`, `.claude/worktrees/agent-a277ea963132adde0`, `.claude/worktrees/develop-merge`: is it a checkout, branch, uncommitted files, last modified.
   - the 7 unmerged `worktree-agent-*` branches: `git log develop..<branch>` and whether each commit is in `main`.
   - the 30 `worktree-agent-*` branches already merged into `develop`.
   Helder marks what to delete; only then `git worktree remove` / `git branch -d` / folder removal. After `.worktrees/` is empty, drop its `.gitignore` line.

No `CLAUDE.md` or `.claude/rules/` text names a worktree folder, so no `amend:` commit is needed for B.

## Step 3 — A1 + A4: worktree-aware exemption and Bash notice

- `read-slice-guard.py`: compute the path relative to the checkout root. If the path contains `.claude/worktrees/<name>/`, strip through `<name>/` first, then apply the exemption list to the remainder. Result: `.claude/worktrees/x/MyVocaList.Services/Foo.cs` is guarded; `.claude/worktrees/x/.claude/library/y.md` and the main checkout's `.claude/` stay exempt. Tests added to the Step 1 test file.
- `bash-guard.py` (same script as Step 2.4): when the command is a plain `cat` / `head` / `tail` / `sed -n` / `type` / `Get-Content` / `gc` on one file that is over the Step 1 budgets and not exempt, return `hookSpecificOutput.additionalContext` with one line: file size, and "use Read with offset/limit or Grep". Never blocks. `head -n N` / `-TotalCount N` with N <= 250 stays silent. It imports the budgets from `read-slice-guard.py` logic (move shared code to `.claude/scripts/read_budget.py`).
- `Grep` content mode with large context: left alone (no reliable size estimate before the call).

## Step 4 — C: reminders that do not reach the model

**C1.** Confirm the delivery rule in the Claude Code hooks reference for `PreToolUse`, `PostToolUse`, `Stop`, `PostCompact`, `TaskCreated`, and whether the hook input carries a subagent marker (`agent_id`). Record the quoted sentences in the task-log. If the doc contradicts the assumption above, stop and report before C2.

**C2.** Per reminder (proposed; each fires rarely unless noted):

| Reminder | Action | Reason |
|----------|--------|--------|
| `library-size-guard.py` | Convert to `additionalContext` | Fires only when a library file passes 400 lines; changes behaviour. |
| `ledger-row-guard.py` hook mode | Convert to `additionalContext` | Fires only on a long row; pre-commit `--check` already blocks. May disappear with workstream D. |
| Inline TDD reminder (`settings.json`) | **Delete** | Never matches worktree paths; would fire on every Services edit if fixed; `testing.md` and the implementor briefing already carry the rule. |
| Inline "Subagent returned with uncommitted changes" | Convert to `additionalContext`, move to a script | Changes behaviour at the right moment; fires only when the tree is dirty. |
| `delegate-reminder.py` (PreToolUse) | Convert to `additionalContext`; silent for subagents if C1 confirms a subagent marker, otherwise silent for paths under `.claude/worktrees/` | The main agent editing code without ITF is the case it targets; implementors must not pay for it on every edit. |
| `Stop` hook warnings (inline + `orphan_check.py`) | Emit as JSON `systemMessage` (shown to Helder, no model tokens) | A model-visible Stop message needs `decision: block`, which forces another turn. |
| `worktree-base-check.py` | `additionalContext` for MISBASED only; drop the BEHIND notes | BEHIND is "not a violation" by its own text and lists every old worktree. |
| `PostCompact` echo | Delivery checked in C1; wording left for workstream E | |

Shared helper: `.claude/scripts/hook_out.py` with `additional_context(event, text)` and `system_message(text)`, used by all converted scripts. `tasklog-size-guard.py` already has the working JSON form and is the pattern.

**C3.** Update `.claude/library/workflow-hooks-invariant.md` to match (which reminders exist, which are deleted).

## Step 5 — Close A, B, C; open D and E

- Item `README.md`: mark A, B, C done with commit hashes; record the `/context` figures Helder takes from a fresh process (per memory note: `/clear` is not a fresh start).
- Then start the D + E brainstorm (`superpowers:brainstorming`), writing `requirements.md` and `design.md` in the item folder; `spec-reviewer` before Helder's gate. No D or E file changes in this plan.

## Commits

One per step, on `develop` (hooks, docs and `.claude/` only; no `.cs`/`.xaml`): `chore(hooks): ...` for Steps 1, 3, 4; `chore(worktrees): ...` for Step 2; `docs(context): ...` for Steps 0 and 5. Via `/sln-commit`.

## Verification

- `python -m pytest .claude/scripts/tests` (new tests) plus the existing lease and backlog tests still pass.
- Each guard run by hand with a piped JSON payload: expected deny / allow / notice.
- Live in the session, after each step:
  - `Read Docs/Management/BACKLOG.md` with no limit: denied, message names `backlog_gen.py query`.
  - `Read` of a 500-line file with `limit: 5000`: denied; with `limit: 200`: allowed.
  - `Read` of a >200-line `.cs` inside `.claude/worktrees/<name>/` with no limit: denied.
  - `cat` on a large `Docs/` file: notice appears in context, command still runs.
  - `git worktree add ../x -b tmp develop`: denied (nothing created).
  - Edit a library file past 400 lines in a scratch copy, and edit `LEDGER.md` with a long row then revert: the notice is visible to the model.
- Hook health line at next session start still reports OK; `settings.json` parses (`python -c "import json; json.load(open('.claude/settings.json'))"`).
- No rule lost: the deleted TDD reminder's home is `testing.md § TDD within SDD`.
