# Plan — Ship BACKLOG-first Registration Enforcement

> Feature: `Docs/Management/DevCycleCraft/backlog-first-registration/`
> Spec: `requirements.md` · `design.md` · `tasks.md` (Phase 0 complete, reviewer-passed)
> Posture: **A — advisory / non-blocking** (ratified by Helder 2026-06-23)
> On approval, this becomes `plan.md` beside `design.md` (CLAUDE.md folder-routing override).

---

## Context

Agents sometimes record a new work item **only** in device-scoped auto-memory
(`~/.claude/projects/<project>/memory/`) instead of in `Docs/Management/BACKLOG.md`. Device memory
is per-device, not git-tracked, and not team-visible — so a memory-only registration is invisible to
everyone but the agent that wrote it. `workflow.md` Rule 1 already states the obligation, but nothing
mechanically reminds an agent when it's violated.

This feature ships a **fail-open, advisory** safety net (a Stop-hook orphan check that *warns*, never
blocks) plus rule strengthening, so a memory-only orphan is surfaced before session end. The full
design is review-corrected and authoritative; this plan only sequences execution and flags your gates.

---

## ⚠️ Pending on Helder (your side) — read first

| # | Gate | Blocks | What you do |
|---|------|--------|-------------|
| **1** | **Spec approval** — `tasks.md` Phase 0 task 3 is `[~]` "⏸️ STOPPED at Helder approval gate". | **All of Phases 1–5.** | Approve the spec (approving this plan covers it). |
| **1a** | **AC-13 precedence decision** — surfaced to you; `design.md §2.1` proposes a default contract: *a new untracked work-item noun → `candidate`; pure continuation of a tracked item → `exempt`.* | Phase 3 classifier behavior. | Confirm the default, or override. |
| **2** | **Phase 2 — `workflow.md amend:`** — agent writes `proposed-diffs.md` ONLY; it never edits `workflow.md`. | Phase 5 close (POST-2). | Read, **edit** (don't rubber-stamp — Authorship gate), apply the diff, commit `amend:` + changelog triple. |
| **3** | **Phase 2 — `session-ops.md` Authorship review** — agent edits it directly, but it needs your read/edit. | Nothing hard; quality gate. | Review the 6th-tier edit. |
| **4** | **BACKLOG `✅ Done`** is only set **after** you apply gate #2. | Final close. | (Happens after #2.) |

**Non-blocking defaults (no action unless you object):**
- `.sln` layout → **flat** under DevCycleCraft (folder already exists). 
- `CLAUDE.md` touch → **none recommended** (600-line budget); optional one-line pointer is yours to add.

> If gate #1 is not yet given, approving this plan **is** the approval to proceed.

---

## Confirmed infrastructure (reuse, don't reinvent)

- **Stop wiring:** append a new command group to the existing `Stop` array, mirroring `heartbeat.py`:
  `python .claude/scripts/backlog/orphan_check.py 2>/dev/null || true`. No new top-level key.
- **SessionStart health check** only verifies top-level key *presence* — adding a `Stop` command group will **not** trip it (AC-10 / INV-2 safe).
- **Pure-logic precedent:** `.claude/scripts/lease/lease_lib.py` (no I/O) + `tests/test_lease_lib.py`
  (`unittest`, `sys.path.insert` import, **no** conftest/pytest). Mirror this exactly for `backlog_lib.py`.
- **Path resolution:** lease scripts use `os.environ.get("CLAUDE_PROJECT_DIR", ".")`. The device-memory
  path has **no existing resolver** → the Phase 1 spike is genuinely greenfield (path-determinism first).
- **`.sln`:** folder `backlog-first-registration` `{FA1234BC-…0029}` exists under DevCycleCraft; spec
  `.md` already registered. `sync-docs-to-sln.ps1` covers `Docs\` Writes only → `.py` files need a **manual** `.sln` task.
- **changelog format:** `- **MM/DD/YYYY** - amend - <Title> — **Old:**…/**New:**… Effective <date>. Rationale:… Authorship: requires Helder human review.` under lowercase month header `## Entries for june 2026`.

---

## Execution plan (DRY-onion; orchestrator delegates all code to subagents)

**Pre-flight (main agent):** materialize this file as `plan.md` beside `design.md`; advance BACKLOG
line 150 `💡 → 📋 → 🗺️ → 🟢`; reset Phase 0 task 3 to `[x]` once approval is recorded.

### Phase 1 — Spike (throwaway only; 60-min hard stop) — gates Phase 4 B-branch
- One subagent. Question (lead with path-determinism): *is the device-memory dir deterministically
  resolvable, AND is a memory write observable by any hook?*
- **PASS** → Option B (PostToolUse memory-write buffer) is viable.
  **FAIL** → record `findings.md` "Option B DEAD"; advisory ships reviewer-driven; **no mtime baseline**.
- Output: `findings.md` + `design.md` update. No production code.

### Phase 2 — Rule / definition diffs (no code) — two `[P]` subagents
- **[RULE] `proposed-diffs.md`** — workflow.md Rule 1 obligation upgrade + Rule 2 exit-checklist line +
  hook-table row + `amend:`/changelog triple + the "Helder must read & edit" note. **Never touches `workflow.md`/`CLAUDE.md`/`changelog.md`.** → your gate #2.
- **[RULE] `session-ops.md`** — add device auto-memory as the **6th tier, explicitly "NOT a registration surface"** (direct edit; `library/` is not deny-listed). → your gate #3.

### Phase 3 — Pure logic (Tester → Builder; Level A full TDD)
- **[TDD-RED]** Tester writes `tests/test_backlog_lib.py` (unittest, mirror lease layout): 4 exempt
  categories → exempt; new-work line → candidate; adversarial precedence ("NEXT: implement X") AC-13;
  backlog-already-changed → no reminder; empty/garbage → exempt. Confirm **RED**.
- **[TDD-GREEN]** Builder writes `backlog_lib.py` (`classify_memory_change`, `should_remind`) to GREEN.
  Builder must not modify tests. Honors the AC-13 precedence contract (gate #1a).

### Phase 4 — Tooling + hook wiring (SEQUENTIAL — `settings.json` single-writer)
- **[TDD] `orphan_check.py`** — thin fail-open Stop wrapper, **parameterized** device-dir path
  (fixture-testable), `backlog_changed_this_session()` detecting BACKLOG change across the **whole
  session** (committed *and* working-tree — not bare `git diff HEAD`). Always `return 0`. + fixture/fail-open tests.
- **[HOOK]** Append the `orphan_check.py` command group under `Stop`. Verify expected-keys unchanged.
- **[HOOK] (spike-PASS only)** PostToolUse memory-write buffer; else `[CANCELLED: spike failed]`.
- **[SLN]** Manual `MyVocaList.sln` registration for every `.claude/scripts/backlog/*.py`; per-file verify.

### Phase 5 — Backstop + close
- **[BACKSTOP] `.claude/commands/review.md`** lane note (direct edit — it's a command, not deny-listed),
  applied **after** the workflow.md `amend:` so the halves don't diverge.
- **[CLOSE]** Verification pass + session-end spec ritual + AC-1..AC-13 traceability matrix.
  BACKLOG → `✅ Done` **only after** you apply gate #2.

---

## Files created / changed

**Agent-owned (in-session):** `plan.md`, `findings.md`, `task-log.md`, `proposed-diffs.md`,
`.claude/scripts/backlog/{backlog_lib.py, orphan_check.py, tests/…}`, `.claude/settings.json` (Stop +
maybe PostToolUse), `.claude/library/session-ops.md`, `.claude/commands/review.md`, `MyVocaList.sln`,
`Docs/Management/BACKLOG.md` (status cell).

**Helder-owned (proposed only, you apply):** `.claude/rules/workflow.md`, `Docs/Changelog/changelog.md`
(the `amend:` triple), optional `CLAUDE.md` pointer.

---

## Verification

- **Phase 3/4 tests:** `python -m unittest` in `.claude/scripts/backlog/tests/` — all GREEN; each
  classifier/precedence behavior seen RED first. Fail-open test proves `orphan_check.py` returns 0 on
  any exception/missing dir.
- **Hook live check:** trigger session end → confirm advisory prints only when a candidate memory line
  exists AND BACKLOG unchanged; confirm it **never blocks**; confirm SessionStart "HOOK HEALTH OK".
- **No-false-positive check (AC-7/AC-11):** a session with only exempt memory writes prints nothing.
- **`.sln` gate (AC-9):** grep `MyVocaList.sln` confirms every new `.md` and `.py` is registered.
- **Close:** AC-1..AC-13 traceability matrix complete in `task-log.md`; BACKLOG `✅ Done` after gate #2.
