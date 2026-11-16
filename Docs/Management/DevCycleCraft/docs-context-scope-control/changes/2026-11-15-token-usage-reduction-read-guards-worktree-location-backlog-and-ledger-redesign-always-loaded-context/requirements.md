# Requirements — Token-usage reduction, workstreams D and E (remaining part)

Status: decided by Helder on 2026-11-15 and implemented, except REQ-TUR-06 and REQ-TUR-09, which moved to `../2026-11-15-always-loaded-context-path-scoped-rules-spike-and-superpowers-plugin-cost/`. Results: `README.md § Status`.

Brainstorm input: Helder's written direction of 2026-11-15 (quoted in `README.md § Helder's direction for D and E`), the measurements in `README.md`, and the hooks and worktrees references read on 2026-11-15. Helder asked for autonomous work with open points left pending, so each open decision is listed in § Decisions for Helder with a recommendation instead of being asked one by one.

## Goal

Lower the tokens an agent spends on backlog, ledger and always-loaded context, without losing a rule and without weakening the ledger's job: knowing where every in-flight task lives.

## Out of scope

- Workstreams A, B, C (done; see `task-log.md`).
- Any change to archive row format (`backlog-archive/*.md`).
- Any change to the status vocabulary (see D-2 in § Decisions).

## Requirements

### D — Backlog

**REQ-TUR-01 — Slim live rows. Done (`c80dda4d`).**
- AC-01.1: every generated row in `BACKLOG.md` has exactly three cells: Target, Feature/Activity, Status.
- AC-01.2: no generated row in `BACKLOG.md` contains `Goal:`, `Gate:` or `Pointer:`.
- AC-01.3: rows in `backlog-archive/*.md` are byte-identical before and after.

**REQ-TUR-02 — Short header. Done (`c80dda4d`).**
- AC-02.1: the text above the first fence in `BACKLOG.md` is at most 12 lines.
- AC-02.2: each rule removed from the header is present in `.claude/library/backlog-generator.md`.

**REQ-TUR-03 — Post-MVP items live in their own generated file. Done (`0e4e4275`); four items carried the title prefix, not five.**
- AC-03.1: an item whose frontmatter has `horizon: post-mvp` is rendered in `Docs/Management/BACKLOG-POST-MVP.md` and not in `BACKLOG.md`.
- AC-03.2: an item without the key is rendered as today.
- AC-03.3: a post-MVP item reaching a terminal status is archived exactly as any other item.
- AC-03.4: `backlog_gen.py query --status ...` excludes post-MVP items unless `--horizon post-mvp` (or `--horizon all`) is passed.
- AC-03.5: `regen --check` fails when `BACKLOG-POST-MVP.md` is stale, as it does for `BACKLOG.md`.
- AC-03.6: an unknown `horizon:` value aborts regeneration with a message naming the item.
- AC-03.7: `register` accepts `--horizon post-mvp` and writes the key.
- AC-03.8: the five live items titled "Post-MVP: ..." carry the key after migration, and their titles no longer need the prefix.
- AC-03.9: a child with no `horizon:` key takes its parent's value; a child that sets the key keeps its own. One test for each case.
- AC-03.10: an active item whose key is added or removed moves between the two files on the next `regen`, and appears in exactly one of them.

### D — Ledger

**REQ-TUR-04 — A merged row leaves the ledger when its branch is gone. Done (`de18316a`).**
- AC-04.1: a row stays in `LEDGER.md § Completed` when neither move condition of AC-04.1b holds: its branch is not confirmed gone, and its merge date is less than 7 days old or cannot be parsed. After a rotation `§ Completed` holds only such rows.
- AC-04.1b: a row moves only when (1) its merge date parses and is 7 or more days old, or (2) its branch is confirmed gone: the branch cell names exactly one branch and either carries the text `(deleted)` or the branch is absent from both `git branch --list` and `git branch -r --list`. A branch cell that is empty, `—`, `develop`, or names more than one branch never confirms "gone"; such a row can move only by condition (1).
- AC-04.2: every row moved out is appended to `LEDGER-archive.md` with identical text (no row is edited or dropped). The script checks that rows in `§ Completed` before equals rows left plus rows appended, and writes nothing when the counts differ.
- AC-04.3: the Active table is never touched by the rotation.
- AC-04.4: the rotation is a script with a `--dry-run` mode that lists the rows it would move.
- AC-04.5: `ledger-row-guard.py --check` (pre-commit) fails when `§ Completed` holds more than 15 rows **and** `ledger_rotate.py --dry-run` reports at least one movable row; the message names the script. More than 15 rows with nothing movable is a warning, and the commit is allowed (same model as the task-log cap).
- AC-04.7: after the first rotation `LEDGER.md` is under 5,000 bytes (today 11,882); the figure is recorded in `task-log.md`.
- AC-04.6: the ledger header and `/sln-ledger` state the new row lifecycle; the Resume chain text is unchanged.

### E — Always-loaded context

**REQ-TUR-05 — `CLAUDE.md` carries only what every session needs. Done (`c6be7255`); AC-05.3 missed by 273 bytes, see `README.md`.**
- AC-05.1: each sentence removed has a named home, listed old-text to home in the `amend:` commit body.
- AC-05.2: no `[HARD RULE]` or `[Unamendable]` item is removed or reworded. In particular the `sln-` prefix `[HARD RULE]` line, the "Before any completion claim" line and the first sentence of the Orchestrator read-scope bullet stay.
- AC-05.3: `CLAUDE.md` is at most 7,300 bytes (today 8,279); the four cuts in `design.md § E1` are estimated at about 1,100 bytes together.
- AC-05.5: before a sentence is removed, its home file is opened and the sentence (or its content) is confirmed there; where the home does not yet state it, the text is added to the home in the same commit.
- AC-05.4: Helder has reviewed the diff before the commit (authorship rule).

**REQ-TUR-06 — Rules that apply only to code load only with code, if that is safe. Moved to the follow-up item.**
- AC-06.1: a spike records, from fresh processes, whether a rules file with a `paths:` scope (a) is absent from `/context` at session start, (b) loads when the main agent reads a matching file, (c) reaches an `implementor` subagent working under `.claude/worktrees/<name>/`.
- AC-06.1b: pass and fail per question. (a) passes when the file is not listed under memory files in `/context` of a fresh process. (b) passes when it is listed after the main agent reads a matching file. (c) passes when the subagent quotes, word for word, a marker sentence that exists only in that file. Any other outcome is a fail for that question.
- AC-06.2: a rules file is path-scoped only if (a), (b) and (c) all pass. If (c) fails, nothing is scoped and the result goes back to Helder.
- AC-06.3: `workflow.md` and `bug-tracking.md` stay always-loaded.

**REQ-TUR-07 — Hook text that fires per prompt or per compaction is minimal. Done (`b577259d`).**
- AC-07.1: the WORKFLOW GATE line is not emitted for the prompts "continue", "fix the typo and add a note", "what does this code do?".
- AC-07.2: it is emitted for "implement task 3", "resume SFT", "start the next wave".
- AC-07.3: no hook is registered on an event whose output is not delivered (`PostCompact`).

**REQ-TUR-08 — The memory index holds no entry that a rule already states. Done (3 entries removed, 1 kept).**
- AC-08.1: each removed entry names the rule file and section that states it.
- AC-08.2: no entry recording a Helder ruling that exists nowhere else is removed.
- AC-08.3: after the change every link in `MEMORY.md` points to an existing file, and every memory file is linked.

**REQ-TUR-09 — Superpowers plugin: its fixed cost is measured before any decision. Moved to the follow-up item.**
- AC-09.1: `/context` is recorded from two fresh processes, plugin on and plugin off (messages and skills lines).
- AC-09.2: the four skills `CLAUDE.md` relies on (`brainstorming`, `writing-plans`, `verification-before-completion`, `systematic-debugging`) remain available, either through the plugin or as vendored copies in `.claude/skills/`. Check: each of the four names appears in the skills listing of a fresh process.
- AC-09.3: the plugin's fixed cost is the difference, in tokens, of the `/context` total at the first prompt of a fresh process between plugin on and plugin off.

### Cross-cutting

**REQ-TUR-10 — Solution view mirrors the folder tree. Done (`97909bfc`). Helder ruled a stricter form than D-4: every file sits under the solution folder of its own directory.**
- AC-10.1: no `Docs\` file is listed in the root "Itens de Solução" solution folder.
- AC-10.2: `process_checks.py --staged` (sln check) passes after the move.
- AC-10.3: `dotnet sln MyVocaList.sln list` returns the same project list before and after.
- AC-10.4: `backlog_gen.py register` and `sync-docs-to-sln.ps1` add a new file under the solution folder of its parent feature.
- AC-10.6: the number of `Docs\` entries in `MyVocaList.sln` is the same before and after the move, and no entry path changes (only its solution folder).
- AC-10.7: entries keep their full relative path as the entry text, so two `README.md` files under the same `bugs` or `changes` solution folder stay distinct entries; the script fails if two entries would be identical.
- AC-10.5: the GUID counter rule in `constraints-reference.md § Visual Studio Solution (.sln)` is followed for every new solution folder.

**REQ-TUR-11 — Measure. Done (`README.md § Measure of success`).**
- AC-11.1: `README.md` records `/context` (memory files, skills, messages) from a fresh process before and after the E changes.
- AC-11.2: `README.md` records the byte size of `BACKLOG.md`, `LEDGER.md` and `CLAUDE.md` before and after (BACKLOG: 35,182 to 8,322, done).

## Decisions for Helder

| # | Decision | Recommendation |
|---|----------|----------------|
| D-1 | Key and file for post-MVP items (REQ-TUR-03) | `horizon: post-mvp`, file `BACKLOG-POST-MVP.md`. |
| D-2 | Short status codes without icons | Do not change. The status text is stored in every item, the archives, `query`, and several rules; the gain is about 10 bytes per row in a file agents no longer read. |
| D-3 | Ledger rotation trigger (REQ-TUR-04) | Rotate when the branch is deleted, or 7 days after merge. Cap of 15 Completed rows enforced at pre-commit. |
| D-4 | Solution folder depth (REQ-TUR-10) | One solution folder per feature, with `bugs` and `changes` under it holding the files prefixed by their item folder name. Not one solution folder per bug or change (that would add about 150 folders). |
| D-5 | `CLAUDE.md` cut list (REQ-TUR-05) | The four cuts in `design.md § E1`. |
| D-6 | Path-scoped rules (REQ-TUR-06) | Run the spike first; decide on its result. |
| D-7 | Superpowers (REQ-TUR-09) | Measure first. If the fixed cost (AC-09.3) is over 2,000 tokens per session, vendor the four skills and disable the plugin. |
| D-8 | Leftover worktree folders and branches (workstream B; a clean-up, so it has no REQ here: the evidence table is in `task-log.md`) | Delete the 3 empty folders and the 37 `worktree-agent-*` branches; Helder looks at `.worktrees/_orch` (22 scratch files) before it is removed. |
