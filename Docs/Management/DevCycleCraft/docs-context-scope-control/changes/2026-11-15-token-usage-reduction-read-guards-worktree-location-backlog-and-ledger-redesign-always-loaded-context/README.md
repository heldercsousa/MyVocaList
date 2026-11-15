---
id: token-usage-reduction-read-guards-worktree-location-backlog-and-ledger-redesign-always-loaded-context
title: "Token-usage reduction: read guards, worktree location, backlog and ledger redesign, always-loaded context"
status: 🟡 In Progress
target: 2026-11-15
section: DevCycleCraft
parent: docs-context-scope-control
goal: "Cut the tokens spent on reading without losing accuracy: fix the read guard gaps, settle one worktree location, slim or replace the backlog and ledger reads, and shrink what loads at every session start and compaction."
gate: Helder approves the plan for each workstream before any rule or context file changes.
kind: change
---

# Token-usage reduction: read guards, worktree location, backlog and ledger redesign, always-loaded context

Cut the tokens spent on reading without losing accuracy: fix the read guard gaps, settle one worktree location, slim or replace the backlog and ledger reads, and shrink what loads at every session start and compaction.


## Status (2026-11-15, end of the implementation session)

Workstreams A, B and C are done. D and E are done in part; the rest is specified in `requirements.md` and `design.md` (spec-reviewer: PASS WITH FINDINGS, findings applied 2026-11-15), is listed under
"Pending Helder" and waits for a discussion. Plan: `plan.md`. Evidence per step: `task-log.md`.
The findings below this section are the original handover text, kept as recorded.

| Workstream | Result | Commit |
|------------|--------|--------|
| A2, A3 | Full read only within 200 lines and 12 KB; `limit` required and capped at 250 lines; slice capped at 20 KB; `BACKLOG.md` denial points to `query` | `108ef530` |
| A1, A4 | Source files inside `.claude/worktrees/<name>/` are guarded; `cat`-like Bash reads of a large file get a note (never blocked) | `54ea4336` |
| B | One location, `.claude/worktrees/`; other locations are refused by `bash-guard.py`; `worktree.baseRef: head` | `54ea4336` |
| C | Reminders use the JSON forms that are delivered; TDD inline reminder deleted | `766077cb` |
| D (backlog) | Live rows are `Target, Feature, Status`; header is one paragraph. `BACKLOG.md` 35.2 KB to 8.3 KB | `c80dda4d` |
| E (settings) | WORKFLOW GATE regex narrowed; dead `PostCompact` echo replaced by a `SessionStart` `compact` line; 3 Artifact skills off (the tool is denied here) | `b577259d` |

### Helder's direction for D and E (2026-11-15, during the session)

- The Feature column is enough; notes and the folder path are not needed in a backlog row. **Done.**
- Post-MVP items could live in their own backlog, read only when needed. **Pending 1.**
- `<!-- -->` lines are unneeded. **Not done on purpose:** the only comment lines in `BACKLOG.md` are the four generator fences; `regen` fails without them.
- The top sections of `BACKLOG.md` spend tokens without need. **Done** (moved to `.claude/library/backlog-generator.md`).
- Statuses could be short codes without icons. **Pending 2.**
- `LEDGER.md` is crucial; treat with caution; a safe reduction is welcome. **Pending 3.**
- Many `plan.md`, `README.md`, `task-log.md` files appear flat under "Itens de Solução" in Visual Studio. **Diagnosed, pending 4.**
- `CLAUDE.md` should be analysed anyway. **Analysed, pending 5.**

### Pending Helder (nothing below was changed)

1. **Post-MVP backlog.** Post-MVP items are marked only by a "Post-MVP:" title prefix (5 live rows). A separate file needs a frontmatter key (for example `horizon: post-mvp`) and a second generated file. Decide the key and the file name.
2. **Short status codes.** The status text with its icon is stored in every item frontmatter, in the archives, in `query --status`, and in several rules files; a pre-commit test checks documented commands against it. Changing it is a wide migration for about 10 bytes per row. Recommendation: keep the statuses as they are.
3. **Ledger.** `LEDGER.md` is 11.9 KB; about 9 KB is the "Completed (this month)" table (45 merged rows). Only the Active table (3 rows) is needed to resume work. Proposal: move a row to `LEDGER-archive.md` (verbatim) when its branch is merged and deleted, instead of at month end. No row is lost; the rule text in the ledger header and `/sln-ledger` changes.
4. **Flat files in the Visual Studio solution view.** Measured: 157 `Docs/` files sit in the root "Itens de Solução" solution folder. The files are in the right folders on disk. Cause: `backlog_gen.py register` adds each new item `README.md` to the first `SolutionItems` section of `MyVocaList.sln`, and the files added later follow it. Fix: one script moves the 157 entries into nested solution folders (creating about 150 of them), and `register` plus `sync-docs-to-sln.ps1` target the nested folder. Decide how deep the solution folders should go (one per feature, or one per change/bug folder). Close Visual Studio while it runs.
5. **`CLAUDE.md` (3.1k tokens).** Candidates to cut, each with its home: the Task-log rotation bullet (the guard now gives the instruction); the Commands list (the skills listing already names every `sln-` command); "Planned" and "Post-MVP" stack notes (not needed in every session); the read-scope bullet in Roles (`orchestrator.md` holds it). Estimated saving about 1,100 bytes (roughly 0.4k tokens); an earlier estimate of 0.6k to 0.9k was too high. Needs an `amend:` commit and your review of the wording.
6. **Rules files on demand.** `testing.md`, `code-principles.md`, `component-change-governance.md` and `constraints-registry.md` (3.8k tokens together) could carry a `paths:` scope so they load only when code or test files are read. Not verified: how a path-scoped rule reaches a subagent working in a worktree. Needs a test in a fresh process before any change.
7. **Superpowers plugin.** Its session-start text and skill bodies are a fixed cost; `CLAUDE.md` relies on four of its skills (`brainstorming`, `writing-plans`, `verification-before-completion`, `systematic-debugging`). Keep or replace is your decision. Plugin skills ignore `skillOverrides`, so they cannot be switched off one by one.
8. **Memory index (1.2k tokens).** Entries that a rule already states: "Docs belong on develop", "Read lane + ITF over dispatch", "Token-scoped library reads", "Route subagents to cheaper models". Candidates for removal.
9. **Leftover clean-up (workstream B).** Table in `task-log.md`: 3 empty folders, `.worktrees/_orch` (22 scratch files), 7 + 30 `worktree-agent-*` branches with no unique commits. Nothing was deleted.
10. **`.sln` auto-registration.** The `PostToolUse` `Write` hook did not register this item's `task-log.md`; it was added by hand. Not investigated.
11. **Measure of success.** Taken on 2026-11-15 from a fresh headless process (`claude -p "/context"`), after the changes: total 32.1k tokens at the first prompt; memory files 12.2k (before: about 12k; unchanged, because the `CLAUDE.md`, rules and memory-index reductions are still at the gate); skills 2.5k (before: 2.9k); messages 1.4k (no "before" figure was recorded). File sizes: `BACKLOG.md` 35,182 to 8,322 bytes; `LEDGER.md` 11,882 (unchanged); `CLAUDE.md` 8,279 (unchanged). The saving of this session is therefore in reads (backlog, guarded slices, worktree files) and in per-prompt hook text, not yet in the always-loaded 12k.

## Already done (2026-11-14 and 15)

- `read-slice-guard.py`: a full `Read` of a file over 200 lines is denied (commit `31d97e17`).
- `tasklog-size-guard.py` + `tasklog_rotate.py`: 300-line cap on `task-log.md`; the notice reaches the model;
  pre-commit blocks only when ready entries exist; rotation moves only closed entries (commits `463b0c2c`,
  `9069aecb`). Only two logs were rotated (venue picture item, Deezer artwork item). Other oversize logs were
  left on purpose; they rotate when next touched.

## Workstream A — Read guard gaps (`.claude/scripts/read-slice-guard.py`)

| # | Finding | Evidence | Proposed fix |
|---|---------|----------|--------------|
| A1 | The exemption test is "path contains `.claude/`". Worktrees live under `.claude/worktrees/`, so every source file read inside a worktree is exempt. Feature coding happens only in worktrees, so the guard is off for most code reads. | Read (script lines 6-12, 27) | Exempt `.claude/` only when the path is not under a worktree folder. Depends on Workstream B (final worktree location). |
| A2 | Any `offset` or `limit` passes, so `limit: 5000` reads the whole file. The deny message says "<= 100 lines per slice" but nothing enforces it. | Read (script line 31) | Enforce a maximum slice. Suggested about 250 lines, not 100: very small slices add round trips and can cut a method in two. |
| A3 | Only lines are counted. `Docs/Management/BACKLOG.md` is 143 lines and 34.5 KB, so a full read passes and costs roughly 9k tokens. | Measured (`wc`) | Add a size budget (for example 12 KB) beside the line count. Deny a full read of `BACKLOG.md` and point to `backlog_gen.py query`. |
| A4 | The guard covers only the `Read` tool. `cat`, `sed -n`, `head` through Bash, and `Grep` in content mode with a large context, bypass it. | Read (settings matcher `Read`); done by the agent in the recording session | Decide: leave, or add a warn-only Bash filter for `cat`-like commands on large `Docs/` files. A hard block adds friction. |

## Workstream B — One worktree location

| # | Finding | Evidence |
|---|---------|----------|
| B1 | All 8 live worktrees are under `.claude/worktrees/` (`git worktree list` shows 9 lines: the main folder plus 8). | Measured |
| B2 | The project documents name `.worktrees/` instead: `.claude/agents/orchestrator.md` (8 mentions), `.claude/library/workflow-rule-2.md`, `.claude/library/session-ops.md`, `.claude/commands/sln-ledger.md`. No rule file names `.claude/worktrees/`. | Measured (grep) |
| B3 | `.worktrees/` exists and holds two folders, `_orch` and `safearea-all`, that are not in `git worktree list` (leftovers). | Measured |
| B4 | Worktrees were also created beside the solution folder, under `C:\Users\helde\source\repos\`. None exists there today. | Reported |
| B5 | Seven `worktree-agent-*` branches are not merged into `develop` and have no live worktree. | Measured (`git branch --no-merged develop`) |

Assumed cause: the harness creates worktrees in `.claude/worktrees/` by default (agent isolation and the
worktree tool), while the project documents and hand-run `git worktree add` commands use `.worktrees/` or a
sibling folder. Not verified against the Claude Code documentation.

To decide and do: pick one location; make documents, `worktree-base-check.py`, `.gitignore` and the guard
exemptions (A1) agree with it; decide whether a hook should refuse `git worktree add` outside it; clean the
leftovers in B3 and B5 only after checking each for unmerged work (ask Helder before deleting anything).

## Workstream C — Reminders that probably do not reach the model

Plain `print` output from a `PostToolUse` or `PreToolUse` hook with exit code 0 is, as far as the recording
agent knows, not passed to the model. Tested for one case only: the task-log guard reached the model once it
returned JSON `additionalContext`. Same plain-print pattern, not tested one by one:

- `.claude/scripts/library-size-guard.py`
- `.claude/scripts/ledger-row-guard.py` (hook mode; its `--check` mode in pre-commit works)
- the inline TDD reminder in `.claude/settings.json` (PostToolUse `Edit|Write`)
- the inline "Subagent returned with uncommitted changes" warning (PostToolUse `Agent`)
- `.claude/scripts/delegate-reminder.py` (PreToolUse)
- the `Stop` hook warnings

To do: confirm the delivery rule in the Claude Code hooks documentation; then for each reminder either convert
it to the JSON form or delete it. A converted reminder costs tokens each time it fires, so keep only the ones
that change behaviour.

## Workstream D — Backlog and ledger

| # | Finding | Evidence |
|---|---------|----------|
| D1 | `BACKLOG.md` is 34.5 KB in 143 lines. Rows carry long goal text that already lives in each item `README.md`. | Measured |
| D2 | `LEDGER.md` is 11.9 KB in 81 lines and is read at every session start (`workflow.md` Rule 7, `ledger-row-guard.py` docstring). It has a row-length cap (450 characters) but no row-count cap and no rotation of closed rows. | Measured, Read |
| D3 | Helder finds the ledger partly redundant with the backlog, and wants both reconsidered, possibly replaced by one lighter mechanism. | Reported (Helder, 2026-11-15) |
| D4 | Session start is already told to use `backlog_gen.py query --status ...` instead of reading `BACKLOG.md`. | Read (`CLAUDE.md`, `workflow.md` Rule 7) |

Questions for the design (brainstorm with Helder, then spec):

- Should the generated backlog row hold only id, title, status and pointer, with the goal text left in the
  item `README.md`?
- Is `BACKLOG.md` needed as a file agents read at all, or only as a human view, with agents using `query`?
- What does the ledger give that item frontmatter plus the task-log `### Checkpoint` block do not? If little,
  can a generated "active work" view (from frontmatter and leases) replace it?
- If the ledger stays: cap the row count and rotate closed rows monthly, as the backlog archive does.

## Workstream E — Context loaded at every session start and compaction

Measured with `/context` on 2026-11-15 (tokens):

| Source | Tokens |
|--------|--------|
| Project `CLAUDE.md` | 3.1k |
| `.claude/rules/workflow.md` | 2.8k |
| `.claude/rules/testing.md` | 1.6k |
| `.claude/rules/constraints-registry.md` | 0.9k |
| `.claude/rules/bug-tracking.md` | 0.8k |
| `.claude/rules/code-principles.md` | 0.7k |
| `.claude/rules/component-change-governance.md` | 0.6k |
| Auto-memory index `MEMORY.md` | 1.2k |
| User `RTK.md` | 0.4k |
| Skills listing | 2.9k |
| **Memory files total** | **about 12k** |

Further findings:

- The superpowers plugin injects its `using-superpowers` text at session start and again after each
  compaction, and its "invoke a skill at 1% chance" rule pulls full skill bodies into context (for example
  `brainstorming`). Measured as present; its token size was not measured.
- The `UserPromptSubmit` hook adds the "WORKFLOW GATE" line whenever the prompt holds a common word (`add`,
  `fix`, `update`, `start`, `complete`, `code`, `write`, ...). It fired on plain questions in the recording
  session. Read (regex in `.claude/settings.json`).
- The `PostCompact` hook asks for a re-read of `CLAUDE.md` constraints and the active spec after every
  compaction. Read.
- Helder finds `CLAUDE.md` too long and repeating what other files say (Reported, 2026-11-15).

Directions to evaluate (none decided):

- `CLAUDE.md`: keep only what must be known in every session (app one-liner, the unamendable constraints,
  roles, pointers). Move the rest to rules or library files that load on demand. Remove each statement that
  a rules file already makes; one home per rule.
- Rules files: check which can load on demand (path-scoped rules or a skill) instead of always.
- Memory index: prune entries that a rule or the repo already records.
- Superpowers: decide whether the plugin earns its fixed cost, or whether only the needed skills should stay
  enabled (several are already switched off in `skillOverrides`).
- Narrow the `UserPromptSubmit` pattern.

Constraint: context files must be human-authored or human-reviewed (`CLAUDE.md § Continuous Enhancement`);
changes to `CLAUDE.md` or `.claude/rules/` follow `CLAUDE.md § Amending These Rules`.

## Suggested order

1. A2 and A3 (one script, no dependency).
2. B (decision by Helder), then A1.
3. C (documentation check first).
4. D and E: brainstorm with Helder, write requirements and design here, then implement.

## Measure of success

Record `/context` at a fresh session start before and after (memory files and messages), and the token cost
of the session-start protocol reads. No rule may be lost: each removed sentence must have a named home.
