# Tasks — BACKLOG-first Registration Enforcement

> Source of truth for sequencing (workflow.md Rule 4). Check off as each completes.
> Markers: `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`.
> Posture A (advisory / non-blocking) RATIFIED 2026-06-23 — Phase 4 wiring may proceed.

---

## Phase 0 — Spec (orchestrator; full ceremony)

- [x] **[SPEC] Write requirements / design / tasks** [SEQUENTIAL]
  - **Produces:** `requirements.md`, `design.md`, `tasks.md`
  - **Consumes:** `analysis-pipeline/09-final-consolidated-plan.md`
  - **Risk:** Low — converging an already-reviewed plan
  - **Files owned:** the three spec files
  - **Demo:** three spec files exist in the feature folder; brainstorming HARD-GATE satisfied
  - **Review lane:** Standard

- [x] **[SPEC] Register spec `.md` in `.sln` + verify** [SEQUENTIAL]
  - **Produces:** `.sln` SolutionItems entries under `backlog-first-registration` folder
  - **Consumes:** the three spec files
  - **Risk:** Low
  - **Files owned:** `MyVocaList.sln`
  - **Demo:** `grep backlog-first-registration MyVocaList.sln` shows requirements/design/tasks rows
  - **Review lane:** Standard

- [x] **[SPEC] Dispatch spec-reviewer subagent → STOP for Helder approval** [SEQUENTIAL]
  - **Produces:** spec-reviewer verdict; BACKLOG `💡 → 📋`
  - **Consumes:** the three spec files
  - **Risk:** Low
  - **Files owned:** none (review only) + `BACKLOG.md` status cell
  - **Demo:** spec-reviewer report returned; HARD STOP at Helder approval gate
  - **Status:** spec-reviewer returned **PASS WITH MINOR ISSUES** (no blocking). 4 factual/clarity fixes applied. **Spec + execution plan APPROVED by Helder 2026-06-24** (plan approval covered spec approval; AC-13 precedence default per `design.md §2.1` confirmed). `plan.md` written; Phases 1–5 unblocked.

> **GATE:** ✅ CLEARED — Helder approved the spec + plan 2026-06-24. BACKLOG advanced `💡 → 🟡`; `plan.md` written. Phases 1–5 may proceed.

---

## Phase 1 — Spike (throwaway only; gates Phase 4 B-branch)

- [ ] **[SPIKE] Device-memory write hook-observability + path-determinism** [SEQUENTIAL]
  - Time-box: **60 min — hard stop**
  - Question: Is a device-scoped auto-memory write observable by ANY hook, AND is the device dir path deterministically resolvable? (lead with path-determinism)
  - Success criterion: a hook event fires on a memory write AND the device dir resolves deterministically → Option B viable
  - Failure criterion: no observable event OR path not resolvable → Option B DEAD; ship D+C+advisory-A; reviewer-driven signal; **no mtime baseline**
  - **Produces:** `findings.md`; `design.md` update
  - **Files owned:** throwaway only — no production code
  - **Demo:** N/A (findings, not behavior)
  - **Review lane:** Standard

---

## Phase 2 — Rule / definition diffs (innermost; no code)

- [ ] **[RULE] workflow.md Rule 1 obligation — proposed diff** [P]
  - **Produces:** `proposed-diffs.md` (workflow.md Rule 1 upgrade + Rule 2 exit-checklist line + hook-table row + `amend:` + changelog triple)
  - **Consumes:** requirements § 4 (work-item def + 4 exempt categories)
  - **Risk:** Medium — deny-listed file; must be proposed diff only, Authorship gate (R1-8)
  - **Files owned:** `proposed-diffs.md` ONLY (NEVER edits `workflow.md`/`CLAUDE.md`/`changelog.md`)
  - **Demo:** `proposed-diffs.md` contains the exact diff + the "Helder must read and edit" note
  - **Review lane:** Elevated (rule change)

- [ ] **[RULE] session-ops.md — device memory as 6th tier** [P]
  - **Produces:** edited `session-ops.md` (6th tier "NOT a registration surface")
  - **Consumes:** requirements § 2, AC-3
  - **Risk:** Medium — directly editable but needs Helder Authorship review
  - **Files owned:** `.claude/library/session-ops.md`
  - **Demo:** session-ops.md tiered model lists device auto-memory as the 6th, non-registration tier
  - **Review lane:** Elevated

---

## Phase 3 — Pure logic (Tester → Builder; Level A full TDD)

- [ ] **[TDD-RED] Tester: write failing classifier + precedence tests** [SEQUENTIAL]
  - **Produces:** `.claude/scripts/backlog/tests/test_backlog_lib.py` (red)
  - **Consumes:** AC-4, AC-13, AC-5, AC-6 from requirements
  - **Risk:** Low
  - **Files owned:** the test file
  - **Demo:** tests run and FAIL (no implementation yet)
  - **Review lane:** Standard

- [ ] **[TDD-GREEN] Builder: implement `backlog_lib.py`** [SEQUENTIAL — waits for RED]
  - **Produces:** `.claude/scripts/backlog/backlog_lib.py` (`classify_memory_change`, `should_remind`)
  - **Consumes:** the failing tests
  - **Risk:** Medium — precedence rule is the subtle part
  - **Files owned:** `backlog_lib.py` ONLY (Builder must not modify tests)
  - **Demo:** all classifier + precedence tests GREEN
  - **Review lane:** Standard

---

## Phase 4 — Tooling + hook wiring (gated on posture ✅; SEQUENTIAL)

- [ ] **[TDD] `orphan_check.py` + fixture/fail-open tests** [SEQUENTIAL]
  - **Produces:** `.claude/scripts/backlog/orphan_check.py` + its tests in `tests/`
  - **Consumes:** `backlog_lib.py`, AC-6, AC-12, spike outcome
  - **Risk:** Medium — parameterized path; must be fail-open
  - **Files owned:** `orphan_check.py` + `tests/test_orphan_check.py`
  - **Demo:** fixture-dir tests + fail-open test GREEN; wrapper always exits 0
  - **Review lane:** Standard

- [ ] **[HOOK] Wire `orphan_check.py` into `settings.json` Stop key** [SEQUENTIAL — settings.json single-writer]
  - **Produces:** new command-type entry under existing `Stop` key (mirrors `heartbeat.py`)
  - **Consumes:** `orphan_check.py`, AC-10, INV-2
  - **Risk:** High — hotspot file; no new top-level key; expected-keys must stay unchanged
  - **Files owned:** `.claude/settings.json`
  - **Demo:** Stop fires `orphan_check.py`; SessionStart expected-keys check still passes
  - **Review lane:** Architectural (hook wiring)

- [ ] **[HOOK] (spike-pass ONLY) PostToolUse memory-write buffer** [SEQUENTIAL — settings.json single-writer]
  - **Produces:** PostToolUse buffer command entry (only if Phase 1 spike PASSED)
  - **Consumes:** spike findings
  - **Risk:** High — hotspot; conditional on spike
  - **Files owned:** `.claude/settings.json`
  - **Demo:** memory writes captured to buffer; `[CANCELLED: spike failed]` if Option B is DEAD
  - **Review lane:** Architectural

- [ ] **[SLN] Manual `.sln` registration for `.claude/scripts/backlog/*.py`** [SEQUENTIAL]
  - **Produces:** `.sln` entries for all backlog `.py` files
  - **Consumes:** all Phase 3/4 `.py` files
  - **Risk:** Low — but the sync hook does NOT cover `.py` (AC-9)
  - **Files owned:** `MyVocaList.sln`
  - **Demo:** every `.py` appears in `.sln`; per-file verification gate passes
  - **Review lane:** Standard

---

## Phase 5 — Backstop + close

- [ ] **[BACKSTOP] review.md lane note** [SEQUENTIAL — applied AFTER the workflow.md `amend:`]
  - **Produces:** `.claude/commands/review.md` backstop note (applied separately so the two halves don't diverge in git, R2)
  - **Consumes:** workflow.md `amend:` (Helder-applied)
  - **Risk:** Medium — ordering matters
  - **Files owned:** `.claude/commands/review.md` — this is a **command**, NOT under the `rules/*.md` deny glob, so it is a **direct edit** (not a proposed diff)
  - **Demo:** review checklist references the BACKLOG orphan backstop
  - **Review lane:** Elevated

- [ ] **[CLOSE] Verification pass + session-end ritual + BACKLOG `✅ Done`** [SEQUENTIAL]
  - **Produces:** final verification evidence; BACKLOG `🟢/🟡 → ✅` (ONLY after Helder applies the `amend:`)
  - **Consumes:** all prior phases
  - **Risk:** Low
  - **Files owned:** `BACKLOG.md` status cell, task-log
  - **Demo:** AC-1..AC-13 traceability complete; tests green; `.sln` verified; BACKLOG Done
  - **Review lane:** Standard

---

## Acceptance-criteria → task map (filled at close)

| AC | Task |
|----|------|
| AC-1 | Phase 0 spec + Phase 2 RULE — **verified-by-review** (human-readability criterion; no automated test) |
| AC-2 | Phase 2 workflow.md proposed diff |
| AC-3 | Phase 2 session-ops.md |
| AC-4 | Phase 3 RED/GREEN |
| AC-5 | Phase 3 + Phase 4 orphan_check |
| AC-6 | Phase 3/4 fail-open tests |
| AC-7 / AC-11 | Phase 3 exempt tests |
| AC-8 | Phase 1 spike |
| AC-9 | Phase 0 SLN + Phase 4 SLN manual |
| AC-10 | Phase 4 HOOK wiring |
| AC-12 | Phase 4 orphan_check tests |
| AC-13 | Phase 3 adversarial precedence tests |
