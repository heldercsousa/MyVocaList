# Design — Token-usage reduction, workstreams D and E (remaining part)

Requirements: `requirements.md`. Status: draft for Helder's gate. Sections marked **Done** describe what is already committed.

All work here is tooling and documents (`.claude/`, `Docs/`, `MyVocaList.sln`). No `.cs` or `.xaml` file changes, so it runs on `develop` in the main checkout. Constitutional constraints are not touched.

## D1 — Slim live rows and short header (REQ-TUR-01, 02). Done.

`render.py`: `render_row(..., slim=True)` returns `| Target | Label | Status |`; `render_backlog` uses the slim heads; `render_archive` is unchanged. Row rules and the status table moved to `.claude/library/backlog-generator.md § Row and frontmatter rules`. Tests: `test_render.py` (`test_live_rows_are_slim_three_cells_without_notes`, `test_slim_row_keeps_depth_arrows_and_status`, `test_archived_rows_keep_the_notes_cell_even_when_slim_is_asked`).

## D2 — Post-MVP backlog (REQ-TUR-03)

- **Schema:** new optional frontmatter key `horizon`, allowed value `post-mvp`. `model.py`: add the field to `Item`, validate the value (AC-03.6). A child inherits its parent's horizon unless it sets its own (same walk as `_section_of`).
- **Render:** `render_backlog(existing_text, items)` filters out post-MVP items. New `render_post_mvp(existing_text, items)` renders the others into `Docs/Management/BACKLOG-POST-MVP.md`, same two fenced regions, same slim rows. The file is created once by hand with a three-line header and the four fences.
- **Generator:** `backlog_gen.py` adds the file to the outputs written by `regen` and compared by `regen --check`. `protect-generated.py` and the pre-commit `STAGED` pattern add the file name.
- **Query:** `query` gets `--horizon {mvp,post-mvp,all}`, default `mvp`.
- **Register:** `--horizon post-mvp` writes the key.
- **Migration:** one commit sets `horizon: post-mvp` on the five items and removes the "Post-MVP: " title prefix. `test_command_docs_guard.py` learns the new flag.
- **Tests (Level A, generator logic):** one per AC-03.1 to 03.7, plus inheritance (AC-03.9) and an active item changing horizon (AC-03.10), in `test_render.py` / `test_backlog_gen.py`. Archiving does not read `horizon`, so AC-03.3 is covered by one test that a terminal post-MVP item lands in the month file.
- **Docs:** `backlog-generator.md` (key, file, flag). `CLAUDE.md` is not changed.

## D3 — Ledger rotation (REQ-TUR-04)

- **Script:** `.claude/scripts/ledger_rotate.py [--dry-run]`. It parses `LEDGER.md § Completed`, and for each row reads the branch cell and the merge date cell. Move and stay conditions are exactly AC-04.1 and AC-04.1b: a row moves when its merge date parses and is 7 or more days old, or when its branch cell names one branch that is marked `(deleted)` or is absent locally and on the remote. An empty, `—`, `develop` or multi-branch cell never confirms "gone" (such a row can move only by its date), and an unparseable date never satisfies the date condition. Before writing, the script checks the row count (AC-04.2) and aborts on a mismatch. Moved rows are appended verbatim under the current-month heading of `LEDGER-archive.md`. The Active table, the header and the footer are copied through unchanged.
- **Guard:** `ledger-row-guard.py --check` adds a row-count check on `§ Completed` (limit 15). It fails only when `ledger_rotate.py --dry-run` finds a movable row, with the message "run `python .claude/scripts/ledger_rotate.py`"; otherwise it warns (AC-04.5).
- **Text:** the ledger header "Row lifecycle" sentence and `/sln-ledger` say: on merge, set Status `merged` and move the row to Completed; `ledger_rotate.py` moves it to the archive once the branch is deleted or after 7 days.
- **Why not replace the ledger by a generated view:** the Active rows hold "Next action" and "Last commit", written by the agent at each phase change; item frontmatter and leases do not hold them. The ledger stays hand-maintained; only the closed part shrinks.
- **Tests:** temp-file tests for: branch deleted, branch alive and recent, branch alive and old, unparseable date, Active untouched, dry-run writes nothing, archive receives identical text, row count conserved, empty and multi-branch cells stay, more than 15 rows with nothing movable only warns.
- **Expected effect:** `LEDGER.md` from about 11.9 KB to about 3 KB.

## D4 — Solution view (REQ-TUR-10)

- **One-time script** `.claude/scripts/sln_nest_docs.py [--dry-run]`: reads `MyVocaList.sln` byte-preservingly (reuse `_read_sln` / `_write_sln` from `backlog_gen.py`). For each `Docs\...` entry in the root solution folder it resolves the target solution folder chain from the path (`Management` → section → feature → `bugs` | `changes`), reuses an existing solution folder when name and parent match (via `NestedProjects`), creates the missing ones with GUIDs from the counter rule, and moves the entry. A file inside a bug or change folder is listed under `bugs` / `changes` (D-4); the entry text stays the full relative path, so same-named files remain distinct (AC-10.7). The script counts `Docs\` entries before and after and aborts on a difference (AC-10.6).
- **`register`:** `_add_to_sln` calls the same resolver instead of inserting into the first `SolutionItems` section.
- **`sync-docs-to-sln.ps1`:** replaced by a call to the Python resolver, so there is one implementation. The same step finds out why the hook did not register this item's `task-log.md` (open point 10 in `README.md`).
- **Verification:** AC-10.2 and AC-10.3 commands, then Helder opens the solution in Visual Studio. Visual Studio must be closed while the script runs.

## E1 — `CLAUDE.md` cut list (REQ-TUR-05)

| Text to remove | Home that already states it |
|----------------|-----------------------------|
| "Task-log rotation" bullet (Docs/ Folder Layout), about 330 bytes | `tasklog-size-guard.py` gives the instruction when the cap is passed. `session-ops.md` does not state the rule today, so the text is added there in the same commit (AC-05.5) |
| The list of `/sln-*` commands (Commands), about 300 bytes | The skills listing names each command with its description; the `[HARD RULE]` on the `sln-` prefix and the "before any completion claim" line stay |
| "Planned: MediatR · FluentValidation" and the "Post-MVP: Blazor Hybrid ..." sentence (App), about 190 bytes | `project-governance-reference.md`; the Context7 line keeps naming MediatR |
| Second half of the Orchestrator read-scope bullet (Roles), from "Grep + read at most 2 source files", about 270 bytes | `orchestrator.md § Orchestrator Read-Scope`; the first sentence and the `[HARD RULE]` tag stay |

One `amend:` commit, body per `CLAUDE.md § Amending These Rules`, after Helder reads the diff.

## E2 — Path-scoped rules spike (REQ-TUR-06)

Time-box 1 hour, output `findings.md`. Steps: add `paths:` frontmatter to a copy of `testing.md` on a scratch branch; fresh process; record `/context`; read a matching file; record again; dispatch an `implementor` into a worktree and ask it to quote a sentence that exists only in that rules file. Candidates if it passes: `testing.md` (`**/*Tests*/**`, `MyVocaList.Services/**`), `code-principles.md`, `constraints-registry.md`, `component-change-governance.md` (`**/*.cs`, `**/*.xaml`). Pass and fail per question are in AC-06.1b. If (c) fails, nothing is scoped and the result goes back to Helder.

## E3 — Hook text (REQ-TUR-07). Done.

`settings.json`: `UserPromptSubmit` regex `\b(implement|dispatch|resume)\b|\bstart (the |a )?(next )?(task|feature|wave|coding)\b`; `PostCompact` removed; `SessionStart` matcher `compact` prints one line; health-check list updated.

## E4 — Memory index (REQ-TUR-08)

Candidates and their homes: "Docs belong on develop" (`workflow.md` Rule 2), "Read lane + ITF over dispatch" (`CLAUDE.md § Roles`), "Token-scoped library reads" (`testing.md § Routing`, library index stubs), "Route subagents to cheaper models" (agent frontmatter). Each file is read before removal to check AC-08.2; a short script then checks that every link in `MEMORY.md` resolves and every memory file is linked (AC-08.3).

## E5 — Superpowers (REQ-TUR-09)

Measure only (AC-09.1). The decision and any vendoring follow D-7.

## Order and risk

1. D3 ledger rotation (small, tested, largest per-session saving).
2. E1 `CLAUDE.md` cuts and E4 memory index (text only).
3. D2 post-MVP backlog (generator change, covered by its test suite).
4. E2 spike, then E5 measurement (both need fresh processes).
5. D4 solution view (largest file rewrite; done alone, in its own commit, easy to revert).

Risks: D4 rewrites `MyVocaList.sln` (mitigated by dry-run, the two checks and a single revertible commit). D3 touches the ledger (mitigated by verbatim moves and the untouched Active table). E2 can show that scoping does not reach subagents; then nothing is scoped.
