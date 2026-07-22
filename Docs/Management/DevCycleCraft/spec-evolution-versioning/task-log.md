# Spec Evolution — Nested folders + generated BACKLOG — Task Log

Spec: `requirements.md`, `design.md` (approved by Helder 2026-07-22) · Plan: `plan.md` · Tasks: `tasks.md`

---

## Phase 1 — Generator (worktree `feature/backlog-generator`)

Worktree: `C:\Users\helde\source\repos\mvl-backlog-generator`, branch `feature/backlog-generator`, based on `develop` (verified `git merge-base --is-ancestor develop HEAD`).

| Task | Commit | Tests | Status |
|------|--------|-------|--------|
| T0 — worktree | — | — | Done |
| T1 — frontmatter parser | `6a7f0bd` | 8 | Done |
| T2 — item model, validation, ordering | `2dda20e` | 22 | Done |
| T3 — row/table rendering + fenced splice | `a3d7e0e` | 13 | Done |
| T4 — monthly archive rendering | `da1f73b` | 17 | Done |
| T5 — CLI `regen` / `--check` / `query` | `8a5fb97` | 10 | Done |
| T6 — `register` / `status` / `renumber` | `51124a7` | 14 | Done |
| T7 — widen `orphan_check` watch set | `ee8ed2d` | 4 | Done |
| T7a — fix blocking review defects | `2468ea5` | +5 | Done |

Every task: Red confirmed before Green, no test weakened or deleted across the branch
(`git log -p develop..HEAD -- tests/` shows zero removed assertion lines).

### T0 finding — `.sln` scope for `.claude/scripts/*` — RESOLVED

The plan flagged this as unresolved (`constraints-registry.md` exempts only `library/` and
`rules/`). **Answer: scripts ARE `.sln`-registered.** `MyVocaList.sln` has a `backlog-scripts`
solution folder (GUID `{C9CDD2BC-B529-48CA-9EFD-24A2A2D92DE7}`) already listing `backlog_lib.py`,
`orphan_check.py`, `session_marker.py` and their tests, plus a sibling `lease-scripts` folder.
All new generator modules and tests were registered there.

> **Orchestrator error, recorded:** the orchestrator's T0 check used a malformed grep pattern,
> got "No matches found", and briefed the T1 implementor that scripts were exempt. The implementor
> checked the file itself, found the precedent, and registered the entries anyway. The false premise
> was corrected in every subsequent briefing.

---

## T7b — Pre-merge code review

**Verdict 1: FAIL** — 2 blocking defects, both reproduced empirically.

- **B1 — BOM'd README silently dropped.** `_read` used `encoding="utf-8"`; a UTF-8 BOM is not
  whitespace, so `text.lstrip().startswith("---")` was False and the file took the *silent-skip*
  branch rather than the error branch. A valid item vanished from both tables with exit code 0.
  Visual Studio and PowerShell `Out-File` emit BOMs by default on Windows, so this would have
  occurred in practice. Fixed in `2468ea5` (`utf-8-sig` on read; writes stay BOM-free).
- **B2 — `cmd_register` not atomic (REQ-SEV-21a).** Writes happened, *then* validation ran. A
  post-write validation failure left folder + README on disk with `regen` permanently failing —
  while the non-zero exit implied nothing had happened. Fixed in `2468ea5` by validating
  `items + [prospective]` before any write.
- **N7 — the covering test proved nothing.** `test_register_is_atomic_nothing_written_on_failure`
  used `parent="ghost"`, a *pre-flight* rejection that returns before staging; it passed against
  the non-atomic implementation. Renamed to `test_register_rejects_unknown_parent_before_staging`
  (assertions byte-identical) and a genuine post-write-failure test added.

> **Orchestrator error, recorded:** after T6 the orchestrator reported "register is atomic — all
> writes staged, every failure path returns before the write loop" on the strength of that green
> test name. A green test name is not evidence.

**Verdict 2 (re-review of the fix): CONDITIONAL PASS.** B1, B2, N7, N1-warning and N2 confirmed
fixed, each new test verified to fail against the pre-fix implementation. No collateral damage.

### Idempotency — explicitly cleared (the load-bearing guarantee)

Both reviews searched for nondeterminism in `regen` and found none: no timestamps in any rendered
byte (`date.today()` appears only in `cmd_register`, injectable via `today=`); `order_items`
terminates on unique `rel_path` so there are no unstable tie-breaks; `sorted()` on `str` is
codepoint-based and locale-independent; dict/set iteration never reaches output; path separators
are normalised at the boundary. The one latent hazard — duplicate `id` making `by_id` last-wins —
is correctly gated, because `validate` rejects duplicates and `cmd_regen` returns 2 before
rendering. **`regen --check` is therefore a trustworthy gate, and the T12 equivalence gate rests
on it.**

### Finding overturned on re-review

The first review reported that a CRLF `.sln` would defeat `sln_add_entry`'s
`"\tEndProjectSection\n"` marker. The implementor disputed it with evidence and the re-review
ruled independently: `_read` opens in text mode with universal newlines, so CRLF is normalised
to `\n` before `sln_add_entry` sees it, and the marker matched all along. The new CRLF test
passes against the *old* implementation, so it cannot distinguish fixed from broken.
**The finding was wrong.** Recorded because a disputed-and-refuted finding is as useful as a
confirmed one.

---

## Deferred findings — not merge-blocking, tracked for follow-up

Raised by the T7b reviews, deliberately scoped out of the fix commits. To be registered as
BACKLOG rows (next free id is **BUG-060**) once the generator is merged and these become
defects in shipped tooling rather than in-flight work.

| # | Finding | Severity | Why deferred |
|---|---------|----------|--------------|
| D1 | **`cmd_register` is atomic only in the WEAK sense** — nothing written on a *validation* failure, but a `RenderError` (BACKLOG missing a fence) or an `OSError` mid-write-loop still leaves folder + `.sln` partially written. Same failure shape as B2, different trigger. | Major | The common path (validation) is closed; the residual needs write-to-temp + `os.replace`, a larger change than the blocker warranted |
| D2 | **Orphaned archive months are never cleaned and `--check` cannot see them.** If the last item of month M is retargeted, `bucket_by_month` stops emitting M, so the stale `BACKLOG-ARCHIVE-M.md` keeps ghost rows forever and `regen --check` still reports clean — the generated view silently diverges from the tree. | Major | Needs a design decision: delete orphaned archives, or fail `--check` on them |
| D3 | **A `parent` cycle makes both rows vanish from both tables** with exit 0. `validate` checks only that the parent id exists, not that the chain resolves; `_section_of` hits its cycle guard and returns `None`. | Major | Same class as C1 (row resolving to no section) — needs a chain-resolution check in `validate` |
| D4 | **`splice` targets the first fence hit with no uniqueness assertion.** A duplicated fence pair, or a fence copied into a markdown code sample, silently mis-targets. Content outside is still preserved and output stays stable, so idempotency holds. | Minor | Cheap fix (`assert text.count(begin) == 1`) but out of scope of the blockers |
| D5 | **CRLF whole-file rewrite of markdown.** `_write` forces `newline="\n"`, so the first `regen` over a CRLF `BACKLOG.md` rewrites every line ending. Content survives and `--check` stays honest. | Minor | Likely correct given `.gitattributes` normalisation — document rather than fix |
| D6 | **`BUG_ID = \bBUG-(\d{1,4})\b` cannot match `BUG-12345`** — a hard ceiling that fails *open* (id reuse) rather than loudly. | Minor | No action needed before 9999 bugs |
| D8 | **`RenderError` escapes `cmd_regen` as an uncaught traceback, exiting 1 instead of 2.** Observed on develop immediately after the T7b merge: with no fences yet in BACKLOG.md, `regen --check` printed a full traceback and exited 1. Exit 1 means *stale*, exit 2 means *error* — so a crash currently masquerades as staleness, and T12b's gate reads those codes. | Major | Harmless until T8 inserts the fences, but must be fixed before T12b relies on the exit-code contract. Fix: catch `RenderError` in `cmd_regen`, print the message, return 2 |
| D7 | **`milestone` and `group` separators render in different shapes** — `milestone` puts its label in the Status column, `group` puts its label in the Feature column with `—` as status; `status_label()` special-cases only `milestone`. Matches the frozen fixture, so not a defect. | Minor | Needs Helder's decision, not a fix — the fixture is the spec |

### Acceptance criteria with no covering test (flagged by review)

- **REQ-SEV-17 (order equivalence)** — names the frozen fixture
  `migration/BACKLOG-pre-migration.md`, which does not exist until T8. Ordering has unit tests,
  but "reproduces today's reading order" is unverified until the T12 equivalence gate. Correctly
  sequenced; recorded so it cannot be lost.
- **REQ-SEV-20 (archive hand-written header round-trip)** — only the synthetic `ARCHIVE_TEMPLATE`
  is exercised; no test splices a real existing archive file. Closed at T12.
- **REQ-SEV-21a (atomicity)** — closed for validation failures, open for render/IO failures (D1).
- **REQ-SEV-14 under BOM input** — closed by B1's fix and the byte-level assertion.

---

## Spec deviations recorded

| Deviation | Spec note |
|-----------|-----------|
| `--renumber` shipped as its own subcommand (`renumber BUG-053`), not a flag on `register` — argparse cannot relax `register`'s `required=True` args, making the flag unreachable | `design.md` §3 + REQ-SEV-11a updated on develop (`53dabb2`, `b141f73`) |

---

## Phase 2 — Migration, additive (develop)

### T8 — Freeze fixture + insert fences
**Status:** To Review
**Branch:** `develop` (docs land on develop — HARD RULE; T8 is a docs/migration task)
**Completed:** 2026-07-22

Additive only: froze a byte-exact copy of the hand-curated BACKLOG.md as the T12 equivalence
fixture, and inserted the four generated-region fence markers around the two existing tables.
No rows were moved, reordered, reformatted or regenerated.

### Changed files
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md` (created — byte-exact freeze)
- `Docs/Management/BACKLOG.md` (modified — 4 fence markers inserted, nothing else)
- `MyVocaList.sln` (modified — registered the fixture in the `spec-evolution-versioning` Solution Folder; UTF-8 BOM + CRLF preserved, verified)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

### Verification evidence

Fence placement (`render.FENCE_BEGIN`/`FENCE_END` formats, region names `business-features`
and `dev-cycle-craft`):

```
$ git diff --stat Docs/Management/BACKLOG.md
Docs/Management/BACKLOG.md | 4 ++++
 1 file changed, 4 insertions(+)
```

4 insertions, 0 deletions — no content altered.

Byte identity of the frozen fixture vs. the pre-fence BACKLOG.md:

```
$ git show HEAD:Docs/Management/BACKLOG.md | sha256sum
23497b290efc77dbeedee7c6cc1c44a77b91681558a44b78ecd887dfbaabb1fd *-
$ sha256sum Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md
23497b290efc77dbeedee7c6cc1c44a77b91681558a44b78ecd887dfbaabb1fd *...BACKLOG-pre-migration.md
```

Generator wiring signal (expected stale — no item READMEs exist until T9–T11):

```
$ python .claude/scripts/backlog/backlog_gen.py regen --check
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
exit=1
```

Exit 1 (stale), not 2 — the fences are found and parsed; no `RenderError` traceback, so known
issue D8 did not trigger and no pre-existing `README.md` under `Docs/Management` breaks the walk.

### Deviations
None.

---

## Task: T9a — Feature READMEs: Business Features top-level rows

**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md` § Task 9
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

### Changed files:
- `Docs/Management/BusinessFeatures/artists-songs/README.md` (created)
- `Docs/Management/BusinessFeatures/queue-management/README.md` (created)
- `Docs/Management/BusinessFeatures/backup-restore/README.md` (created)
- `MyVocaList.sln` (3 SolutionItems entries)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T9a ticked)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

### Rows written (feature | order | status)

| Feature | `order:` | Status | Fixture row (BACKLOG.md line) |
|---------|----------|--------|-------------------------------|
| `artists-songs` | 20 | 🔴 Blocked | 41 (table row 2) |
| `queue-management` | 300 | 💡 Pending | 69 (table row 30) |
| `backup-restore` | 360 | 💡 Pending | 75 (table row 36) |

`order:` = 1-based position of the row in the Business Features table × 10 (plan Task 9 Step 2 /
REQ-SEV-17). Global table position, not position among the migrated subset — this keeps the
curated order monotonic once the remaining rows land.

### Rows NOT written (10) — with reason

Every one of these is a **top-level Business Features row with no dedicated feature folder**, so per
the plan they belong to T9c (folder-less rows) rather than T9a. No folder was invented.

| Row (BACKLOG line) | Position | Reason skipped |
|---|---|---|
| Bug: Venues list fetch slow (BUG-012) | 1 | pointer is a flat file `venues/bugs/BUG-012-….md`; plan phase 3 converts it via `git mv` |
| **Form & Autocomplete UX Overhaul** | 21 | pointer is `cross-cutting-log.md` → T9c |
| Dead-code cleanup: `QueueService`/`IQueueService` | 29 | pointer is a file inside `queue-management/`, whose folder is already owned by the Queue Entry Point Redesign row → needs its own folder (T9c) |
| **User Tutorial/Learning** | 33 | no pointer, no folder |
| **Website** | 34 | no pointer, no folder |
| 🏁 **MVP release** | 35 | `kind: milestone` separator → T10b |
| **Singer self-registration** | 37 | no pointer, no folder |
| **Social features** | 38 | no pointer, no folder |
| **Windows version** | 39 | folder exists, but the row carries **no Goal** (Gate + Pointer only) and `model.REQUIRED` makes `goal` mandatory — see Spec gap below |
| **Cross-cutting** | 40 | `kind: group` separator → T10b |

### Spec gap: BACKLOG rows with no Goal cannot be transcribed

**Location:** `model.py` `REQUIRED = ("id", "title", "status", "target", "goal")` vs. BACKLOG row
`| — | **Windows version** | 🔴 Blocked | Gate: … Pointer: … |`.
**Gap description:** the row template assumes every row has a Goal, but the Windows version row has
only a Gate; writing a Goal would be invention, and omitting it fails validation with exit 2.
**Options:**
- Option A: Helder supplies the one-line Goal, then the README is written — faithful, needs a human.
- Option B: relax `model.REQUIRED` to drop `goal` and render `Gate:`-only Notes — mechanical, but
  weakens REQ-SEV-09's template enforcement for every future row.
**Recommendation:** Option A — one row, one sentence, and the template stays enforced.
**Blocking:** No — the row is skipped and recorded here; it must be resolved before T12's
equivalence gate, which would otherwise show the row as missing.

### Design concern: `target: -` is valid to sort but invalid to validate

`model.target_sort` treats both `—` and `-` as "no target", but `model.validate` accepts only `—`.
The Data Backup & Restore row's fixture target is a plain hyphen `-`, so transcribing it verbatim
would abort regeneration with exit 2. Transcribed as `—` and flagged in that README's body; T12
should record the resulting `| - |` → `| — |` cell as a permitted diff. Design §2 documents the
target grammar as `YYYY-MM-DD | YYYY-MM | "—"`, so **`model.py` is right and the fixture row is the
outlier** — no code change requested, only the diff-class note.

### Verification evidence

```
$ python .claude/scripts/backlog/backlog_gen.py regen --check ; echo exit=$?
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
exit=1
```

Exit **1** (stale) as the plan requires — never 2, so all three new READMEs parse and validate.
`regen` (without `--check`) was not run; the migration stays additive until T12.

```
$ git diff --stat        # BACKLOG.md absent from the list
 MyVocaList.sln | 3 +
```

`.sln` verified after writing: UTF-8 BOM present, 100% CRLF line endings, three new
`SolutionItems` entries with literal backslash paths in the `artists-songs`, `backup-restore` and
`queue-management` solution folders.

Post-edit re-read: all three READMEs re-parsed by the generator's own walk (the `regen --check`
run above); `.sln` entries re-read byte-wise via `repr()` after a first attempt corrupted two
paths (shell escape) — reverted and rewritten from a script file.

---
## Task: T9b — Feature READMEs: Dev Cycle Craft top-level rows
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md` (Task 9, split T9b)
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

### Enumeration (the number T9c-2 needs)

The Dev Cycle Craft fenced table holds **53 rows**, of which **28 are top-level** (non-`↳`).
Of those 28, **9 have a dedicated spec folder** under `Docs/Management/DevCycleCraft/` and received a
`README.md`; the remaining **19 point at `Docs/Management/cross-cutting-log.md`** (or at a folder they
do not own) and route to **T9c-2**. The row count is therefore *not* the README count, exactly as T9a
found.

### READMEs written (feature, order, status)

| order | id (folder) | status |
|-------|-------------|--------|
| 20 | `inline-trivial-fix` | 🟡 In Progress |
| 30 | `workflow-folder-layout-alignment` | 🟡 In Progress |
| 100 | `persisted-string-trimming` | 🗺️ Plan |
| 110 | `extensions-layer-guidelines` | 💡 Pending |
| 150 | `appbar-searchbar-redesign` | 🟡 In Progress |
| 290 | `UI-2nd-refactor` | 📋 Spec |
| 340 | `session-continuity-leasing` | 🟡 In Progress |
| 450 | `ui-form-validation-guide` | 🟡 In Progress |
| 520 | `spec-evolution-versioning` | 🗺️ Plan |

`order:` = the row's 1-based position in the Dev Cycle Craft table × 10, the same convention T9a used,
so Helder's hand-curated row order survives regeneration (REQ-SEV-17).

### Rows skipped, with reasons

- **BACKLOG-first Registration Enforcement** (row 39, would be `order: 390`, folder
  `backlog-first-registration/` exists) — **blocked: cannot transcribe.** Its Goal is
  *"work items must be registered in BACKLOG.md before memory writes (advisory Stop-hook posture)"*
  and its Gate names `workflow.md` and `AC-13`. `model._BANNED` rejects `\S+\.(cs|xaml|py|md)` as a
  "file path beyond the pointer" and `AC-\d+` as an "AC number", so the row cannot be written without
  either rewording (data loss, fails T12) or dropping the required `goal`. Escalated below.
- **① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline** (row 24, would be
  `order: 240`) — its pointer is a *file* inside `autocomplete-component/`, a folder owned by the
  autocomplete sub-rows (rows 18–23, T10b's territory), not by this row. Assigning it that folder's
  `README.md` would make it the parent of rows it is not the parent of. Routed to **T9c-2** for a
  `cross-cutting/` folder decision rather than invented here.
- The other 18 folder-less top-level rows (all pointing at `cross-cutting-log.md`) → **T9c-2**.

### Allowed diff class (d) — Notes overflow moved to the README body

Per plan Task 9 step 2, overflow sentences were moved **verbatim** into the README body under
`**Notes overflow (transcribed from the pre-migration BACKLOG row):**`; no sentence was reworded.
Rows affected and why:

| order | reason the fixture Notes could not be transcribed whole |
|-------|--------------------------------------------------------|
| 20 | > 3 sentences and > 55 words; banned `PASS` / `CONDITIONAL PASS`, `33/33` test count, `handoff.md` path |
| 100 | > 55 words |
| 110 | banned `code-principles.md` path |
| 150 | banned `PASS` verdict |
| 520 | banned `CONDITIONAL PASS` verdict |

`UI-2nd-refactor`'s fixture target is the plain hyphen `-`; `model.validate` accepts only the em dash
`—`, so it was transcribed as `—` — the same permitted diff T9a recorded for the Data Backup &
Restore row.

### Spec gap: banned-content regex rejects faithful transcription of two rows

**Location:** `.claude/scripts/backlog/model.py` `_BANNED` vs. the fixture rows for
`BACKLOG-first Registration Enforcement` (and, as overflow, four more rows).
**Gap description:** the "file path beyond the pointer" and "review verdict" patterns fire on Notes
text that is genuinely the row's Goal, not stray detail — `BACKLOG.md` and `workflow.md` *are* the
subject of that row.
**Options:**
- Option A: Helder supplies a one-line Goal for the row that avoids the banned tokens — faithful to
  intent, needs a human, keeps the rule mechanical.
- Option B: narrow `_BANNED`'s file-path pattern to exclude bare `*.md` governance filenames —
  mechanical, but weakens REQ-SEV-09 for every future row.
**Recommendation:** Option A, consistent with T9a's Windows-version escalation.
**Blocking:** No — the row is skipped and recorded here; it must be resolved before T12's equivalence
gate, which would otherwise show the row as missing.

### Design concern: `extensions-layer-guidelines/README.md` already existed

That folder already had a substantive, `.sln`-registered `README.md` (the placement-guidelines
document). Frontmatter was **prepended**, body preserved byte-for-byte — verified against
`git show HEAD:…` after an initial overwrite was reverted with `git checkout --`. Future migration
tasks must check for an existing `README.md` before writing.

### Changed files

- `Docs/Management/DevCycleCraft/inline-trivial-fix/README.md` (new)
- `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/README.md` (new)
- `Docs/Management/DevCycleCraft/persisted-string-trimming/README.md` (new)
- `Docs/Management/DevCycleCraft/extensions-layer-guidelines/README.md` (frontmatter prepended)
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/README.md` (new)
- `Docs/Management/DevCycleCraft/UI-2nd-refactor/README.md` (new)
- `Docs/Management/DevCycleCraft/session-continuity-leasing/README.md` (new)
- `Docs/Management/DevCycleCraft/ui-form-validation-guide/README.md` (new)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/README.md` (new)
- `MyVocaList.sln` (8 `SolutionItems` entries; `extensions-layer-guidelines` already had one)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T9b ticked)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

### Verification evidence

```
$ python .claude/scripts/backlog/backlog_gen.py regen --check ; echo exit=$?
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
exit=1
```

Exit **1** (stale) as the plan requires — never 2, so all nine READMEs parse and validate. `regen`
without `--check` was not run; the migration stays additive until T12.

```
$ git diff --stat
 .claude/changed-files.txt                          | 316 +++++++++++++++++++++
 .../extensions-layer-guidelines/README.md          |  15 +
 MyVocaList.sln                                     |   8 +
 3 files changed, 339 insertions(+)
```

`Docs/Management/BACKLOG.md` is **absent** from the list — BACKLOG.md was not touched.
(`.claude/changed-files.txt` was already dirty at session start and is not part of this task.)

`.sln` verified after writing: UTF-8 BOM present, 0 LF-only line endings (100% CRLF), and each of the
eight new entries re-read byte-wise via `repr()` — no shell-escape corruption (T9a's `\a`/`\b`
heredoc failure avoided by using Python script files throughout).

Post-edit re-read: all nine READMEs re-parsed by the generator's own walk (the `regen --check` run
above); `extensions-layer-guidelines/README.md` re-read in full to confirm the original body survived.
