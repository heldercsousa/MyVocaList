# Task Log — BACKLOG-first Registration Enforcement

**Plan:** `Docs/Management/DevCycleCraft/backlog-first-registration/plan.md`
**Posture:** A — advisory / non-blocking (ratified by Helder 2026-06-23).

---

## Task: Phase 1 — Spike (path-determinism + hook-observability)
**Status:** Review task done
**Started:** 06/24/2026 · **Completed:** 06/24/2026

### Changed files
- `Docs/Management/DevCycleCraft/backlog-first-registration/findings.md` — spike verdicts + path recipe.
- `Docs/Management/DevCycleCraft/backlog-first-registration/design.md` — §4 spike outcome.
- `MyVocaList.sln` — register findings.md.

### Verification evidence
- Path determinism: **DETERMINISTIC** — `git rev-parse --git-common-dir` → strip `/.git` → mangle `[:/\\]→-` → `~/.claude/projects/<mangled>/memory/`. Computed dir matches the real on-disk dir (verified `memory/` + `MEMORY.md` exist). `$CLAUDE_PROJECT_DIR` observed unset → not relied upon.
- Hook observability: **OBSERVABLE** — existing PostToolUse `Edit|Write` logs memory writes to `.claude/changed-files.txt` (29 memory lines of 1540). Caveat: cumulative across sessions → needs session-scoping.
- Option B: **VIABLE**. Run inline by orchestrator after 2 subagent infra (usage-limit) failures; read-only shell + markdown only (no source, no `.py`).

---

## Task: Phase 2 — Rule / definition diffs
**Status:** To Review (Helder gates #2/#3)
**Started:** 06/24/2026 · **Completed:** 06/24/2026

### Changed files
- `Docs/Management/DevCycleCraft/backlog-first-registration/proposed-diffs.md` (new) — workflow.md Rule 1 obligation + Rule 2 exit-checklist line + Hook-Notes row + `amend:`/changelog triple + Authorship note. **NEVER edited workflow.md/CLAUDE.md/changelog.md** (INV-4).
- `.claude/library/session-ops.md` — device auto-memory added as 6th tier + governance rule #6 ("NOT a registration surface"). Committed `de23e13`.
- `MyVocaList.sln` — register proposed-diffs.md.

### Verification evidence
- `proposed-diffs.md` carries the exact diffs for Helder to read/edit/apply (Authorship gate). ⏳ **Helder gate #2** (apply `amend:` + changelog) and **gate #3** (session-ops Authorship review) pending.

---

## Task: Phase 3 — Pure logic `backlog_lib.py` (Level A, full TDD)
**Status:** To Review
**Started:** 06/24/2026 · **Completed:** 06/24/2026

### Changed files
- `.claude/scripts/backlog/tests/test_backlog_lib.py` (new) — 17 tests, RED first (`20485da`).
- `.claude/scripts/backlog/backlog_lib.py` (new) — `classify_memory_change`, `should_remind`; pure, stdlib-only (`45395ff`).
- `MyVocaList.sln` — `backlog-scripts` folder `{FA1234BC-…0030}` + both files.

### Verification evidence
- Tests: **17/17 PASS**, independently re-run by orchestrator. RED confirmed before GREEN (ModuleNotFoundError → green). Builder did not modify tests.
- AC-13 precedence: whole-word verb match + tracked-marker short-circuit. New noun → candidate; continuation → exempt.

---

## Task: Phase 4 — `orphan_check.py` + session-scoped signal + settings wiring
**Status:** To Review
**Started:** 06/24/2026 · **Completed:** 06/25/2026

### Changed files
- `.claude/scripts/backlog/orphan_check.py` (new) — fail-open Stop wrapper; `main()` always returns 0.
- `.claude/scripts/backlog/session_marker.py` (new) — SessionStart marker (`.session-marker`: changed-files.txt offset + git HEAD).
- `.claude/scripts/backlog/tests/test_orphan_check.py` (new) — 12 tests (fixture enumeration, fail-open, reminder/suppression, all-exempt).
- `.claude/settings.json` — `orphan_check.py` under existing `Stop`; `session_marker.py` under existing `SessionStart`.
- `.gitignore` — `.claude/scripts/backlog/.session-marker`.
- `MyVocaList.sln` — register orphan_check.py, session_marker.py, test_orphan_check.py.
- Committed `4f00675` (work also captured by auto-commits `b4f36ec`/`41a9461`/`56b7aa8`/`cddcfa6`).

### Verification evidence (orchestrator-independent)
- Tests: **29/29 PASS** (17 Phase 3 + 12 Phase 4), re-run by orchestrator.
- `settings.json`: valid JSON (UTF-8 BOM → `utf-8-sig`); top-level keys `['hooks','permissions','plansDirectory']` — **unchanged, no new top-level key** (AC-10/INV-2); hook sub-keys unchanged.
- Fail-open: `python orphan_check.py; echo exit=$?` → **exit=0** (no marker present).
- `.gitignore`: `git check-ignore .claude/scripts/backlog/.session-marker` → ignored.
- Session-scoping: SessionStart-marker mechanism chosen over a dedicated PostToolUse buffer (simpler, reuses existing `Edit|Write` logging).

---

## Task: Phase 5 — Backstop + close
**Status:** in progress — CLOSE done; `[BACKSTOP] review.md` DEFERRED to after Helder gate #2

- `[CLOSE]` verification + AC traceability matrix: this file. Done.
- `[BACKSTOP]` `.claude/commands/review.md` lane note: **DEFERRED** — must be applied AFTER Helder applies the `workflow.md amend:` (gate #2), so the two halves do not diverge in git (plan R2).
- BACKLOG `✅ Done`: **HELD** — POST-2 requires Helder to apply the `amend:` first.

---

## Acceptance Criteria Traceability Matrix

| AC ID | Criterion (short) | Implementation location | Test / evidence |
|-------|-------------------|-------------------------|-----------------|
| AC-1 | Discriminator self-sufficient | `requirements.md §4` + `proposed-diffs.md` | Verified-by-review (human-readability; no automated test) |
| AC-2 | workflow.md obligation as **proposed diff** (not self-applied) | `proposed-diffs.md` | Review; ⏳ Helder gate #2 applies. INV-4 honoured (workflow.md untouched) |
| AC-3 | session-ops.md 6th tier "NOT a registration surface" (direct edit) | `.claude/library/session-ops.md` (`de23e13`) | Review; ⏳ Helder gate #3 Authorship |
| AC-4 | Classifier correctness red→green | `backlog_lib.classify_memory_change` | `test_backlog_lib.py` exempt×4 + candidate; RED-first confirmed |
| AC-5 | Advisory fires on candidate+backlog-unchanged; never blocks | `orphan_check.main` + `backlog_lib.should_remind` | `test_orphan_check` reminder/suppression; `test_backlog_lib` should_remind; exit=0 proof |
| AC-6 | Fail-open (any error → exit 0, silent) | `orphan_check.main` try/except | `test_orphan_check` fail-open; `test_backlog_lib` garbage→exempt; manual exit=0 |
| AC-7 | No false positive on legitimate use | `should_remind` (all-exempt → no remind) | `test_orphan_check` all-exempt no-print |
| AC-8 | Spike gate | `findings.md` (PASS → Option B viable) | `findings.md` evidence; `design.md §4` |
| AC-9 | `.sln` registration incl. `.py` | `MyVocaList.sln` | grep confirms backlog_lib/orphan_check/session_marker + 2 test files + all `.md` |
| AC-10 | Single Stop entry; expected-keys unchanged | `.claude/settings.json` | top-level keys unchanged (`hooks`/`permissions`/`plansDirectory`); no new top-level key |
| AC-11 | Hard invariant: no legit memory use flagged | `should_remind` / classifier exempt paths | same tests as AC-7 (restated as invariant) |
| AC-12 | `orphan_check.py` deterministic tests (fixture + fail-open) | `tests/test_orphan_check.py` | 12 tests against fixture dir + fail-open |
| AC-13 | Precedence proven adversarially | `classify_memory_change` precedence | `test_backlog_lib` "NEXT: implement X"→candidate / "run smoke for <tracked>"→exempt |

### Invariants / postconditions
- INV-1 fail-open: ✅ `main()` only returns 0. INV-2 no new top-level key: ✅. INV-3 line-level classification: ✅ (no blanket file exemption; MEMORY.md lines individual). INV-4 deny-list respected: ✅ (workflow.md/CLAUDE.md/changelog.md never edited by agent).
- POST-1 every new `.md`+`.py` in `.sln`: ✅. POST-2 BACKLOG `✅ Done` only after Helder `amend:`: ⏳ held.


## Moved from BACKLOG.md (2026-07-15) — BACKLOG-first Registration Enforcement

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-20 | **BACKLOG-first Registration Enforcement** | 🟡 In Progress | Agents must register work items in BACKLOG.md (nested under parent feature when applicable) before writing to memory. Memory is personal/device-scoped and not team-visible. **Design analysis COMPLETE (2026-06-21):** 7-agent (Opus) design pipeline → `DevCycleCraft/backlog-first-registration/analysis-pipeline/09-final-consolidated-plan.md`. **POSTURE RATIFIED (Helder, 2026-06-23): A — advisory / non-blocking** (Stop-hook orphan check WARNS, never blocks; fail-open). **MERGED to `develop` 2026-07-14** (worktree-triage session; settings.json conflicts resolved keeping develop's newer hooks — the branch's orphan_check/session_marker wiring survived; backlog-scripts .sln folder re-GUIDed 0030→0042 due to collision). **IMPLEMENTATION COMPLETE (2026-06-25)** — Phases 1–5 built & pushed: Phase 1 spike PASS (path deterministic via `git rev-parse --git-common-dir`, writes observable, Option B viable → `findings.md`); Phase 2 `proposed-diffs.md` + `session-ops.md` 6th tier; Phase 3 `backlog_lib.py` (TDD, 17 tests); Phase 4 `orphan_check.py` fail-open Stop wrapper + `session_marker.py` SessionStart-marker session-scoping + `settings.json` wiring (29/29 tests green, no new top-level key, exit 0). AC-1..AC-13 traceability matrix in `task-log.md`. ⏳ **Remaining (Helder-gated, blocks `✅ Done` per POST-2):** (#2) read/edit/apply the `workflow.md` `amend:` from `proposed-diffs.md` (+ changelog triple); (#3) `session-ops.md` Authorship review; (#1a) confirm AC-13 precedence default (proceeded on documented default). **DEFERRED to after #2:** `[BACKSTOP]` direct edit to `.claude/commands/review.md` (kept with the rule half so they don't diverge). `.sln` flat. |
