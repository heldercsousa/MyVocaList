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
