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

---
## Task: T9c-1 — Folder-less Business Features rows → `cross-cutting/` folders
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md`
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

Six of the seven target rows migrated to `Docs/Management/cross-cutting/<slug>/README.md`.
The `cross-cutting/` tree did not exist before this task, so **no pre-existing README was
touched** — every file is new (checked with an explicit `os.path.exists` guard that aborts the
script, the T9b `extensions-layer-guidelines` overwrite being the precedent). REQ-SEV-28 honoured:
`cross-cutting-log.md` is retained untouched and every new README back-references it.

### Rows written (row, order, status)

| # | Row | order | status | target |
|---|-----|-------|--------|--------|
| 20 | **Form & Autocomplete UX Overhaul** | 200 | 💡 Pending | 2026-07-11 |
| 29 | Dead-code cleanup: superseded `QueueService`/`IQueueService` | 290 | 💡 Pending | 2026-06 |
| 33 | **User Tutorial/Learning** | 330 | 💡 Pending | 2026-06 |
| 34 | **Website** | 340 | 💡 Pending | 2026-06 |
| 37 | **Singer self-registration** | 370 | 💡 Pending | — |
| 38 | **Social features** | 380 | 💡 Pending | — |

**`order:` mismatch found.** The briefing gave Form & Autocomplete UX Overhaul as global
position **21 → order 210**. Its actual 1-based position in the Business Features table is
**20**, so `order: 200` was written. The other five matched the briefing. Positions were counted
mechanically from the rows between the `business-features` fences (header + separator excluded).

Goal/Gate/Status/Target were transcribed verbatim from the pre-migration rows. No row needed
notes-overflow relocation: all six pass `model.notes_violations` unchanged (≤3 sentences,
≤55 words, no banned content). Rows with no Pointer in the original (33, 34, 37, 38) omit
`pointer:`, so the generator falls back to the folder path — a storage-format consequence, not a
content edit. Rows 20 and 29 keep their original pointers verbatim.

### Row skipped
- **Windows version** (position 39) — BLOCKED, as flagged in `tasks.md`. Its BACKLOG row carries
  Gate + Pointer but **no Goal**, and `model.REQUIRED` makes `goal` mandatory. Writing one would be
  content fabrication, so the row was left unmigrated pending Helder's decision (option A: supply a
  one-line goal).

### Changed files
- `Docs/Management/cross-cutting/form-autocomplete-ux-overhaul/README.md` (new)
- `Docs/Management/cross-cutting/queueservice-deadcode-cleanup/README.md` (new)
- `Docs/Management/cross-cutting/user-tutorial-learning/README.md` (new)
- `Docs/Management/cross-cutting/website/README.md` (new)
- `Docs/Management/cross-cutting/singer-self-registration/README.md` (new)
- `Docs/Management/cross-cutting/social-features/README.md` (new)
- `MyVocaList.sln` (new `cross-cutting` Solution Folder + 6 child folders)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T9c-1 ticked)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

### Verification evidence

`regen --check` (never `regen`):

```
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
REGEN_EXIT=1
```

Exit **1** = stale, the expected result. Not 2, so no README written here is malformed and no
validation rule is violated.

All six rows confirmed to parse and resolve through the generator's own walk:

```
💡 Pending 2026-07-11  **Form & Autocomplete UX Overhaul**  → Docs/Management/cross-cutting-log.md
💡 Pending 2026-06  **User Tutorial/Learning**  → cross-cutting/user-tutorial-learning/
💡 Pending 2026-06  **Website**  → cross-cutting/website/
💡 Pending —  **Singer self-registration**  → cross-cutting/singer-self-registration/
💡 Pending —  **Social features**  → cross-cutting/social-features/
💡 Pending 2026-06  Dead-code cleanup: superseded `QueueService`/`IQueueService`  → BusinessFeatures/queue-management/queue-deadcode-cleanup.md
```

`BACKLOG.md` NOT modified:

```
 .claude/changed-files.txt | 321 +++++++++++++++++++++++++++++++++++++
 MyVocaList.sln            |  39 ++++++
 2 files changed, 360 insertions(+)
```

`.sln` gate: written by a Python **script file** in binary (never a Bash heredoc — the T9a `\a`/`\b`
escape corruption). UTF-8 BOM and CRLF preserved and re-asserted after the write; all six
SolutionItems paths re-read with `repr()` and confirmed uncorrupted, e.g.
`'\t\tDocs\Management\cross-cutting\form-autocomplete-ux-overhaul\README.md = ...'`.
New `cross-cutting` folder GUID `…0057` nested under Management; children `…0058`–`…005D`
(sequential counter continued from `…0056`, verified free of collisions before writing).

Post-edit re-read: all six READMEs re-parsed by the generator's frontmatter walk (the `--check`
and `query` runs above).

---
## Task: T9c-2a — Folder-less Dev Cycle Craft rows, first half (9 rows)
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md`
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

### Enumeration (done independently, not trusted from T9b)

Global positions counted over the rows inside the `dev-cycle-craft` fence (header and separator
lines excluded, first data row = position 1). Counting basis verified against two committed
anchors: `inline-trivial-fix` = position 2 / `order: 20`, and ① Autocomplete Mobile UX Pattern =
position 24. Both agree — no disagreement with the briefed anchor.

18 folder-less **top-level** Dev Cycle Craft rows found, matching T9b's count exactly:

| # | Pos | Order | Row | Lane |
|---|-----|-------|-----|------|
| 1 | 1 | 10 | Documentation & spec-tracking governance — where docs live | **T9c-2a** |
| 2 | 4 | 40 | Inline Undo Pattern — UX Standard | **T9c-2a** |
| 3 | 5 | 50 | Mandatory Worktree Rule Enforcement — ALL Subagent Work | **T9c-2a** |
| 4 | 6 | 60 | Search Pattern Standardization + Navigation Result Service | **T9c-2a** |
| 5 | 7 | 70 | IAsyncRelayCommand Standardization | **T9c-2a** |
| 6 | 8 | 80 | Search Error State UX Standardization | **T9c-2a** |
| 7 | 9 | 90 | Filter Pattern Standardization | **T9c-2a** |
| 8 | 13 | 130 | Bug: Shell navigation swallows button tap animations | **T9c-2a** |
| 9 | 14 | 140 | Bug/Verify: FloatingToolbar always visible | **T9c-2a** |
| 10 | 24 | 240 | ① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline | T9c-2b (excluded by briefing) |
| 11 | 25 | 250 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | T9c-2b |
| 12 | 30 | 300 | Large-volume data stress test (1–2 year seed) | T9c-2b |
| 13 | 31 | 310 | Cross-device / OS version compatibility test | T9c-2b |
| 14 | 32 | 320 | Play Store + Samsung Galaxy Store pre-submission compliance | T9c-2b |
| 15 | 33 | 330 | Full pre-release mobile testing checklist (all categories) | T9c-2b |
| 16 | 42 | 420 | Infra Repository Folder Consolidation | T9c-2b |
| 17 | 43 | 430 | Read Model + Global NoTracking Pattern — Guidelines Update | T9c-2b |
| 18 | 44 | 440 | CRUD Read Model Refactoring — Persons, Songs, Venues | T9c-2b |

**Skipped / not applicable:**
- **BACKLOG-first Registration Enforcement** (pos 39) — excluded by briefing (banned-content
  conflict). Note for the record: it is **not** folder-less — its pointer is
  `DevCycleCraft/backlog-first-registration/`, an existing folder. It is therefore absent from the
  18 above and belongs to the existing-folder lane, not to T9c-2.
- All `↳`-prefixed sub-rows and all separator rows — out of scope per briefing.

**No row required a notes-overflow relocation and no row was skipped for banned content.** All
nine Goals/Gates pass `model.notes_violations` unchanged; every Goal, Gate, Status and Target was
transcribed verbatim from the BACKLOG row (only the leading `Goal: ` / `Gate: ` labels and the
trailing `Pointer: ` clause were dropped, as they become frontmatter keys).

### Written (row, order, status)

| Row | order | status |
|-----|-------|--------|
| Documentation & spec-tracking governance — where docs live | 10 | 💡 Pending |
| Inline Undo Pattern — UX Standard | 40 | 💡 Pending |
| Mandatory Worktree Rule Enforcement — ALL Subagent Work | 50 | 💡 Pending |
| Search Pattern Standardization + Navigation Result Service | 60 | 💡 Pending |
| IAsyncRelayCommand Standardization | 70 | 💡 Pending |
| Search Error State UX Standardization | 80 | 💡 Pending |
| Filter Pattern Standardization | 90 | 💡 Pending |
| Bug: Shell navigation swallows button tap animations | 130 | 💡 Pending |
| Bug/Verify: FloatingToolbar always visible | 140 | 💡 Pending |

### Changed files:
- `Docs/Management/cross-cutting/documentation-spec-tracking-governance/README.md` (new)
- `Docs/Management/cross-cutting/inline-undo-pattern/README.md` (new)
- `Docs/Management/cross-cutting/mandatory-worktree-rule-enforcement/README.md` (new)
- `Docs/Management/cross-cutting/search-pattern-standardization/README.md` (new)
- `Docs/Management/cross-cutting/iasyncrelaycommand-standardization/README.md` (new)
- `Docs/Management/cross-cutting/search-error-state-ux-standardization/README.md` (new)
- `Docs/Management/cross-cutting/filter-pattern-standardization/README.md` (new)
- `Docs/Management/cross-cutting/shell-navigation-tap-animations/README.md` (new)
- `Docs/Management/cross-cutting/floatingtoolbar-visibility-verify/README.md` (new)
- `MyVocaList.sln`
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md`
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md`

### Evidence

Overwrite guard: the script aborts with exit 2 on `os.path.exists(README)` **before** writing
anything. All nine paths were new (all nine folders appear as `??` in `git status`), so no
pre-existing README was touched and no byte-for-byte preservation was needed.

`regen --check` — exit **1** (stale), the expected value. Exit 2 (malformed) never occurred:

```
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
REGEN_EXIT=1
```

`BACKLOG.md` NOT modified (`git diff --stat`; `.claude/changed-files.txt` was already dirty at
session start and is not part of this commit):

```
 .claude/changed-files.txt | 324 ++++++++++++++++++++++++++++++++++++++++++++++
 MyVocaList.sln            |  54 ++++++++
 2 files changed, 378 insertions(+)
```

`.sln` gate: written by a Python **script file** in binary (never a Bash heredoc — the T9a
``/`` escape corruption). BOM and CRLF preserved and **re-asserted after the write**
(`chk.startswith(b'ï»¿')` and a zero bare-LF assertion), and every SolutionItems path
re-read with `repr()`:

```
'		Docs\Management\cross-cutting\documentation-spec-tracking-governance\README.md'  guid=...005E
'		Docs\Management\cross-cutting\inline-undo-pattern\README.md'                     guid=...005F
'		Docs\Management\cross-cutting\mandatory-worktree-rule-enforcement\README.md'      guid=...0060
'		Docs\Management\cross-cutting\search-pattern-standardization\README.md'           guid=...0061
'		Docs\Management\cross-cutting\iasyncrelaycommand-standardization\README.md'       guid=...0062
'		Docs\Management\cross-cutting\search-error-state-ux-standardization\README.md'    guid=...0063
'		Docs\Management\cross-cutting\filter-pattern-standardization\README.md'           guid=...0064
'		Docs\Management\cross-cutting\shell-navigation-tap-animations\README.md'          guid=...0065
'		Docs\Management\cross-cutting\floatingtoolbar-visibility-verify\README.md'        guid=...0066
BOM=ok bare-LF=none written=9
```

GUID counter continued from the highest in use (`…0057` folder, children `…0058`–`…005D`) → new
children `…005E`–`…0066`, all nested under the existing `cross-cutting` folder `…0057`.

Post-edit re-read: all nine READMEs re-parsed by the generator's frontmatter walk —
`backlog_gen.py query` lists all nine with the correct title, status and target, confirming the
restricted frontmatter subset is valid and no key was dropped.

REQ-SEV-28 honoured: every README back-references `Docs/Management/cross-cutting-log.md`, which is
retained, not deleted.

---
## Task: T9c-2b — Folder-less Dev Cycle Craft rows, second half (9 rows)
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md`
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

Completes the Phase-2 additive migration of **top-level** Dev Cycle Craft rows. Positions were
re-counted independently over the rows inside the `dev-cycle-craft` fence (header + separator
excluded, first data row = position 1); the table holds **53 rows**. Counting basis verified against
the committed anchor `inline-trivial-fix` = position **2** / `order: 20` — agreement, so no
off-by-one. T9c-2a's remaining-row list was verified against the table rather than trusted.

### Rows written (row, order, status)

| Pos | Row | order | status | target |
|-----|-----|-------|--------|--------|
| 24 | **① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline** | 240 | 🔵 Deferred | 2026-07-11 |
| 25 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 250 | 💡 Pending | 2026-06-12 |
| 30 | Large-volume data stress test (1–2 year seed) | 300 | 💡 Pending | 2026-06-12 |
| 31 | Cross-device / OS version compatibility test | 310 | 💡 Pending | 2026-06-12 |
| 32 | Play Store + Samsung Galaxy Store pre-submission compliance | 320 | 💡 Pending | 2026-06-12 |
| 33 | Full pre-release mobile testing checklist (all categories) | 330 | 💡 Pending | 2026-06-12 |
| 42 | **Infra Repository Folder Consolidation** | 420 | 💡 Pending | 2026-06-27 |
| 43 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 430 | 💡 Pending | 2026-06-27 |
| 44 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 440 | 💡 Pending | 2026-06-27 |

Goal, Gate, Status and Target were **transcribed verbatim** — only the leading `Goal: ` / `Gate: `
labels and the trailing `Pointer: ` clause were dropped, since those become frontmatter keys.
**No row needed a notes-overflow relocation and no row was skipped for banned content:** all nine
pass `model.notes_violations` unchanged (≤3 sentences, ≤55 words, no banned token). This was checked
as a *pre-flight* that aborts the writer with exit 3 before any file is created.

Row 24 got its **own** folder `cross-cutting/autocomplete-mobile-ux-pattern/` and keeps its original
`pointer:` verbatim (the decision file inside `autocomplete-component/`). It deliberately does **not**
claim `autocomplete-component/README.md`, whose folder is owned by the sub-rows this row does not
parent. Rows carrying a Gate (24, 43, 44) keep it as a `gate:` key; the rest omit it.

### Rows NOT written — accounted for

- **BACKLOG-first Registration Enforcement** (pos 39) — excluded by briefing; blocked by the systemic
  banned-content conflict recorded under T9b (its Goal/Gate name `BACKLOG.md`, `workflow.md` and an
  AC number, all matched by `model._BANNED`). It is **not** folder-less — its pointer is the existing
  `DevCycleCraft/backlog-first-registration/` folder — so it belongs to the existing-folder lane, not
  to T9c-2. Awaiting Helder's decision (T9b Option A: supply a one-line Goal).
- Separator rows (`kind: milestone` / `kind: group`) — **T10b**.
- `↳`-prefixed sub-rows — out of scope of this task.

### Is the Phase-2 additive migration of top-level rows complete?

**Dev Cycle Craft: yes, except one blocked row.** 28 top-level rows: 9 with existing folders (T9b) +
9 folder-less (T9c-2a) + 9 folder-less (T9c-2b) = **27 migrated**; the 28th is BACKLOG-first
Registration Enforcement, blocked as above. `backlog_gen.py query` lists exactly 27 Dev Cycle Craft
items, confirming the arithmetic against the tree rather than against a tally.

**Business Features: one blocked row remains** — **Windows version** (its BACKLOG row carries no
Goal; T9a/T9c-1 escalation). So across both sections the top-level migration is complete **apart
from those two rows, both blocked on a Helder decision, neither of which this task was authorised
to resolve.**

### Changed files:
- `Docs/Management/cross-cutting/autocomplete-mobile-ux-pattern/README.md` (new)
- `Docs/Management/cross-cutting/crud-page-structural-reduction/README.md` (new)
- `Docs/Management/cross-cutting/large-volume-data-stress-test/README.md` (new)
- `Docs/Management/cross-cutting/cross-device-os-compatibility-test/README.md` (new)
- `Docs/Management/cross-cutting/store-presubmission-compliance/README.md` (new)
- `Docs/Management/cross-cutting/pre-release-mobile-testing-checklist/README.md` (new)
- `Docs/Management/cross-cutting/infra-repository-folder-consolidation/README.md` (new)
- `Docs/Management/cross-cutting/read-model-notracking-guidelines/README.md` (new)
- `Docs/Management/cross-cutting/crud-read-model-refactoring/README.md` (new)
- `MyVocaList.sln` (9 child Solution Folders under the existing `cross-cutting` folder)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T9c-2b ticked)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

### Verification evidence

Overwrite guard: the writer runs `os.path.exists(README)` over all nine paths and **aborts with exit
2 before writing anything** if any exists (the T9b `extensions-layer-guidelines` overwrite is the
precedent). All nine were new — all nine folders appear as `??` in `git status` — so no pre-existing
README was touched and no body preservation was required.

`regen --check` (never bare `regen` — the migration stays additive until T12):

```
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
REGEN_EXIT=1
```

Exit **1** = stale, the expected value. Exit 2 (malformed) never occurred, so all nine READMEs parse
and validate. Re-run after the `.sln` write with the identical result.

`BACKLOG.md` **NOT** modified (`.claude/changed-files.txt` was already dirty at session start and is
not part of this task):

```
$ git diff --stat
 .claude/changed-files.txt | 327 ++++++++++++++++++++++++++++++++++++++++++++++
 MyVocaList.sln            |  54 ++++++++
 2 files changed, 381 insertions(+)
```

`.sln` gate: written by a Python **script file** in **binary** (never a Bash heredoc — the T9a
escape-expansion corruption). BOM asserted before the write and **re-asserted after**, together with
a zero bare-LF assertion; every SolutionItems path and every NestedProjects line re-read and
verified, paths printed via `repr()`:

```
'\t\tDocs\\Management\\cross-cutting\\autocomplete-mobile-ux-pattern\\README.md = ...'      guid=...0067
'\t\tDocs\\Management\\cross-cutting\\crud-page-structural-reduction\\README.md = ...'      guid=...0068
'\t\tDocs\\Management\\cross-cutting\\large-volume-data-stress-test\\README.md = ...'       guid=...0069
'\t\tDocs\\Management\\cross-cutting\\cross-device-os-compatibility-test\\README.md = ...'  guid=...006A
'\t\tDocs\\Management\\cross-cutting\\store-presubmission-compliance\\README.md = ...'      guid=...006B
'\t\tDocs\\Management\\cross-cutting\\pre-release-mobile-testing-checklist\\README.md = ...' guid=...006C
'\t\tDocs\\Management\\cross-cutting\\infra-repository-folder-consolidation\\README.md = ...' guid=...006D
'\t\tDocs\\Management\\cross-cutting\\read-model-notracking-guidelines\\README.md = ...'    guid=...006E
'\t\tDocs\\Management\\cross-cutting\\crud-read-model-refactoring\\README.md = ...'         guid=...006F
BOM=ok bare-LF=0 registered=9
```

GUID counter continued from the highest **actually in use** (`...0066`, read from the file — T9c-2a
advanced it) → `...0067`–`...006F`, each asserted collision-free before use and each nested under the
existing `cross-cutting` Solution Folder `{FA1234BC-0001-4000-8000-000000000057}`. Recorded for the
next task: a *second*, unrelated `cross-cutting` Solution Folder exists at `...0038`; `...0057` is
the T9c-1/T9c-2a folder and is the correct parent.

Post-edit re-read: all nine READMEs re-parsed by the generator's own frontmatter walk —
`backlog_gen.py query` lists all nine with the correct title, status, target and pointer, in the
intended `order:` positions relative to the already-migrated rows.

REQ-SEV-28 honoured: `Docs/Management/cross-cutting-log.md` is **retained**, untouched, and
back-referenced from every new README.

### Deviations
None.

---

## Task: T10a — READMEs for existing `bugs/` folders
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md`
**Status:** blocked: sequencing — T10a cannot land before T12's archive fences
**Started:** 2026-07-22
**Completed:** — (blocked)

### Changed files
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T10a annotated `blocked`)

No README.md was written; no `MyVocaList.sln` change; `BACKLOG.md` untouched. T10a is 0-for-9.

### Enumeration (briefed count was wrong again — verified via `git ls-files`)
`ls` under the six `bugs/` directories reports them EMPTY (rtk-proxied `ls` misreports); the
authoritative enumeration is `git ls-files | grep '/bugs/'`. **9 bug FOLDERS** exist (the rest of the
`bugs/` content is flat `.md`/`.jpg` files, which are not items and are out of T10a's scope — BUG-011
and BUG-012 are flat files; BUG-012 is T11c's).

| # | Bug folder | Parent feature | BACKLOG row lives in | Writable? |
|---|-----------|----------------|----------------------|-----------|
| 1 | `BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/` | artists-songs | ARCHIVE-2026-06 | terminal → blocked |
| 2 | `…/BUG-018-artistformpage-edit-save-crash/` | artists-songs | ARCHIVE-2026-06 | terminal → blocked |
| 3 | `…/BUG-019-artistspage-listitem-button-noop/` | artists-songs | ARCHIVE-2026-06 (+ live BUG-028) | blocked (2 reasons) |
| 4 | `…/BUG-021-songspage-fab-crash/` | artists-songs | ARCHIVE-2026-07 | terminal → blocked |
| 5 | `…/BUG-023-songform-bottomsheet-broken/` | artists-songs | ARCHIVE-2026-07 | terminal → blocked |
| 6 | `…/BUG-024-songform-edit-data-loss/` | artists-songs | ARCHIVE-2026-07 | terminal → blocked |
| 7 | `BusinessFeatures/persons/bugs/BUG-022-singerform-birthday-mask/` | persons | ARCHIVE-2026-07 | blocked — **Minor** |
| 8 | `BusinessFeatures/cross-cutting/bugs/BUG-026-hwui-sigabrt-render-teardown/` | (none) | live BACKLOG L80 | blocked — no parent item |
| 9 | `DevCycleCraft/autocomplete-component/bugs/bug-043/` | (none) | ARCHIVE-2026-07 | blocked — no parent item |

### ⛔ BLOCKER 1 (systemic, decides T10a's schedule) — terminal items crash `regen` before T12
6 of the 9 folders (rows 1–6) are **archived rows** (`✅ Fixed`), so their READMEs carry
`status: "✅ Fixed"` + `closed:`. `render_backlog` excludes terminal items from the live tables
(REQ-SEV-16) and `_render_all` routes them through `bucket_by_month` → `render_archive` → `splice`.
The five archive files have **no `archive` fence** — T8 inserted fences into `BACKLOG.md` only, and
`tasks.md` assigns the archive fences to **T12** (*"Archive fences + the equivalence gate"*, files
owned: the 5 archive files). Proven empirically: the 5 READMEs were written, `regen --check` was run,
and it did not return a status code at all — it raised an **uncaught `render.RenderError: missing
generated fence for region 'archive'`** (`backlog_gen.py:114` → `render.py:122` → `render.py:71`).
That is strictly worse than exit 2. The 5 probe files were removed and the baseline re-verified.

**Therefore T10a's archived-row half is a T12-successor, not a T10b-predecessor.** Options for Helder:
(A) move the archive-fence insertion out of T12 into its own small predecessor task and re-run T10a
after it — recommended, it is a 5-line additive edit and it also unblocks T12a; (B) reorder T10a to
run after T12; (C) make `_render_all` create the fence when absent (changes generator behaviour —
needs its own task and tests).

### ⛔ BLOCKER 2 — BUG-022 is `Minor`; `model.py` forbids a folder for it
`model.validate` raises *"severity 'Minor' must not have a folder (REQ-SEV-03) — record it in the
parent task-log instead"*. The folder **already exists** and predates the rule. Writing a README makes
`regen` exit 2 permanently. Options: (A) Helder re-classifies BUG-022's severity; (B) the folder is
dissolved into `persons/task-log.md` (destructive — outside T10a, which is purely additive);
(C) exempt pre-existing folders from the Minor rule. Not resolvable inside T10a.

### ⛔ BLOCKER 3 — two folders have no parent item to attach to
- `BUG-026` needs `parent: <BusinessFeatures/cross-cutting item id>`; that folder has **no README**
  (it is not the migrated `Docs/Management/cross-cutting/` group either). `parent` naming no existing
  item is a validation error, and falling back to `section: BusinessFeatures` would render it as a
  **top-level** row — a silent structural change, not a transcription.
- `bug-043` needs `parent: <autocomplete-component>`; `DevCycleCraft/autocomplete-component/README.md`
  exists but carries **no frontmatter**, so `walk()` skips it and it is not an item.
Both are unblocked as soon as the two parent rows get frontmatter — likely T10b/T12a territory.

### ⛔ BLOCKER 4 — BUG-019 has no valid status and two claimants
Its archive Status cell reads free-text **"Closed — partially regressed"**, which is not in
`model.STATUSES`; and the live row **BUG-028** points at the *same* folder. Mapping the status would
be rewording, and one folder cannot back two rows. Needs Helder.

### Evidence
- `regen --check` **before** any write: exit **1** (stale), only `Docs/Management/BACKLOG.md` listed.
- `regen --check` **with the 5 probe READMEs present**: uncaught `RenderError` traceback (above).
- `regen --check` **after revert**: exit **1**, identical to baseline — no residue.
- `git status --porcelain` after revert: only `.claude/changed-files.txt` (pre-existing, untouched by
  this task). `BACKLOG.md` **not modified**; `MyVocaList.sln` **not modified** (nothing to register).

### Order-within-parent convention (prepared, unused)
Sibling position among the parent's bug sub-rows in pre-migration table order × 10, chronological by
target across archive months: BUG-017=10, BUG-018=20, BUG-021=30, BUG-023=40, BUG-024=50 (×10 leaves
slots for BUG-019/028 and the not-yet-foldered artists-songs bugs).

### Deviations
None. Nothing was written, moved or deleted; `regen` was never run without `--check`.

---

## Task: T9d — `archive` generated-region fences in the 5 monthly archive files
**Plan:** `Docs/Management/DevCycleCraft/spec-evolution-versioning/plan.md`
**Status:** To Review
**Started:** 2026-07-22
**Completed:** 2026-07-22

Split out of **T12** to fix the sequencing defect recorded in the T10a entry above (BLOCKER 1):
T10a needs the archive fences to exist, but T12 runs later. T9d is purely additive — it inserts the
fence pair around each file's EXISTING hand-written table. Nothing is regenerated, moved, reordered
or reformatted; `regen` was never run without `--check`; `BACKLOG.md` was not touched.

### Changed files
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-03.md`
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-04.md`
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-05.md`
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-06.md`
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-07.md`
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/tasks.md` (T9d row added before T10a, ticked; T10a blocker 1 annotated resolved; T12 row amended)
- `Docs/Management/DevCycleCraft/spec-evolution-versioning/task-log.md` (this entry)

`.sln`: **no change expected or made** — no file was created, moved or deleted; all 5 archive files
are already registered.

### Fence placement (T8 precedent)
`BACKLOG.md` puts `<!-- BACKLOG:GENERATED:BEGIN <region> -->` immediately above the table head and
the END marker immediately after the last row, with the prose header outside. T9d copies that
exactly, region name `archive` — byte-identical to `render.FENCE_BEGIN/FENCE_END.format("archive")`,
which is what `render_archive` → `splice(existing_text, "archive", body)` searches for.

**Implementation decision (deviation worth Helder's eye):** every archive file contains **two**
tables (`## Business Features` and `## Dev Cycle Craft`), whereas `ARCHIVE_TEMPLATE` defines a
**single flat** `archive` region. With a one-fence-pair / +2-lines budget the only placement that
keeps every archived row inside the generated region is BEGIN above the *first* table head and END
after the *last* row — so the intervening `## Dev Cycle Craft` heading now sits **inside** the
region and will be consumed when T12 regenerates. The alternative (fencing only one of the two
tables) would leave the other table's rows outside the region, where T12 would silently drop them.
Flagged, not self-adjudicated: if the two-section split must survive regeneration, that is a
`render.py` change (two regions, e.g. `archive-business` / `archive-craft`) and belongs to its own
task before T12.

### Verification evidence

Written by a Python **script file** in **binary** (never a Bash heredoc — the T9a escape-expansion
corruption). Per file: BOM absent (all 5), line endings LF (all 5, 0 CRLF), trailing newline present
— all preserved.

`git diff --numstat` — exactly `2  0` for all five:

```
2	0	Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-03.md
2	0	Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-04.md
2	0	Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-05.md
2	0	Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-06.md
2	0	Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-07.md
```

Byte-preservation proof — `git show HEAD:<path>` vs the new file with the 2 fence lines removed,
sha256 (first 16 hex), all identical:

```
FILE                         HEAD              NEW-minus-2-fences  IDENTICAL
BACKLOG-ARCHIVE-2026-03.md   31c90ee8381884f8  31c90ee8381884f8    True
BACKLOG-ARCHIVE-2026-04.md   73a34616e55eacb3  73a34616e55eacb3    True
BACKLOG-ARCHIVE-2026-05.md   1c86bd83f1f45300  1c86bd83f1f45300    True
BACKLOG-ARCHIVE-2026-06.md   f2b21611336b8e3a  f2b21611336b8e3a    True
BACKLOG-ARCHIVE-2026-07.md   80cf1e6a06074959  80cf1e6a06074959    True
```

`regen --check` AFTER the fences (never bare `regen`) — exit **1** (stale), the expected value, and
**no `RenderError` traceback**:

```
BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen
  - .\Docs\Management\BACKLOG.md
REGEN_EXIT=1
```

Because T10a's probe READMEs were reverted there are currently **no terminal items**, so that run
does not by itself exercise `render_archive`. The fence was therefore proven directly against the
real files by calling the generator's own code in-process — the T10a traceback
(`render.RenderError: missing generated fence for region 'archive'`, `render.py:71`) no longer
occurs for any of the five:

```
BACKLOG-ARCHIVE-2026-03.md splice OK (region 'archive' found) -> len 626
BACKLOG-ARCHIVE-2026-04.md splice OK (region 'archive' found) -> len 532
BACKLOG-ARCHIVE-2026-05.md splice OK (region 'archive' found) -> len 532
BACKLOG-ARCHIVE-2026-06.md splice OK (region 'archive' found) -> len 627
BACKLOG-ARCHIVE-2026-07.md splice OK (region 'archive' found) -> len 627
```

(That call was read-only — the spliced result was discarded, never written back.)

### Deviations
Only the fence-placement decision documented above. No item README was created (T10a/T12a's job);
no `.sln` change; `BACKLOG.md` untouched; `regen` never run without `--check`.
