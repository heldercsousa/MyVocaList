# Requirements — BACKLOG-first Registration Enforcement

> **Type:** Dev Cycle Craft (process/tooling)
> **BACKLOG row:** line ~150, "BACKLOG-first Registration Enforcement"
> **Design input:** `analysis-pipeline/09-final-consolidated-plan.md` (7-agent Opus pipeline, review-corrected — authoritative).
> **Posture:** RATIFIED by Helder 2026-06-23 — **A (advisory / non-blocking)**.
> Language rule: English only (CLAUDE.md § Constitutional Constraints).

---

## 1. Overview

Agents sometimes record a newly-identified work item **only** in device-scoped auto-memory
instead of in `Docs/Management/BACKLOG.md`. Device memory is personal, not team-visible, and
not in git, so a memory-only registration is invisible to everyone except the agent that wrote
it on that device. `workflow.md` Rule 1 ("Proactive BACKLOG triage") already states the
obligation, but nothing mechanically reminds the agent when it is violated.

This feature adds a **mechanical, advisory** safety net that detects probable memory-only work-item
registrations and reminds the agent (and Helder) at session end — **without ever blocking the
session**. It also strengthens the written rule (workflow.md / session-ops.md) so the obligation is
explicit and the device-memory surface is documented as *not* a registration surface.

### Goal
Make "memory is never the sole home for a work item" both **written** (rule strengthening) and
**nudged** (a fail-open Stop-hook advisory), so a memory-only orphan is surfaced before the session
ends — never silently lost, never forcibly blocked.

### Non-goal
Hard-blocking session end; forcing CI/headless lockout; replacing human review; perfect detection.

---

## 2. Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Work item** | A new business feature, a new Dev Cycle Craft activity, a bug, a deferred follow-up, or a material one-off investigation — anything that MUST get a BACKLOG row (nested per `bug-tracking.md` when it has a parent feature). |
| **Exempt** | A memory write that is legitimately NOT a work item and must never be flagged (see § 4, the 4 exempt categories). |
| **Device (auto-)memory** | The harness-managed per-device memory tree at `~/.claude/projects/<project>/memory/` (16 live out-of-tree files at design time). Not git-tracked, not team-visible. |
| **Orphan** | A work item recorded in device memory in a session where BACKLOG.md was NOT changed — the violation this feature detects. |
| **Advisory** | A non-blocking reminder printed at session end. Fail-open: any error, missing dir, or ambiguity → silent `exit 0`. Never blocks, never fails a session. |
| **Candidate** | A classifier verdict: a changed memory line that looks like a new work item and therefore warrants a reminder. Opposite of **exempt**. |
| **Spike-confirmed signal** | The mechanism (per Phase 1 spike) by which the advisory learns which memory files/lines changed this session. If the spike fails, the signal is **reviewer-supplied** instead. |
| **Posture A (advisory)** | The ratified enforcement stance: warn only, never block. Opposite of the literal BACKLOG-line text ("block session end"). |

---

## 3. User Stories

### US-1 — As the team, I want memory-only work items surfaced, so they are not silently lost.
When an agent records a work item only in device memory and does not touch BACKLOG.md, the agent
must be reminded at session end to add a BACKLOG row.

### US-2 — As Helder, I want the obligation written down, so agents have an authoritative rule to follow.
`workflow.md` Rule 1 must state the same-session BACKLOG obligation and the work-item definition
explicitly; `session-ops.md` must document device memory as a non-registration surface.

### US-3 — As an agent in a background/headless session, I want the check to never block me, so automated runs always complete.
The advisory must fail open and never prevent session end, regardless of its verdict.

### US-4 — As an agent making legitimate memory writes (feedback, continuation pointers, caches), I do not want false reminders, so the advisory stays trustworthy.
Exempt memory writes must never trigger the advisory.

---

## 4. The Work-item vs Exempt Discriminator

**Work item** → MUST get a BACKLOG row.

**Exempt — 4 categories (locked working definition; final workflow.md prose gated to Helder `amend:`):**
1. `feedback_*` learnings — guidance on how the agent should work.
2. `project_*` continuation pointers — "NEXT:" / resume notes for an **already-tracked** item.
3. Reference-fact caches — email, current date, architecture snapshots.
4. Harness-**automatic** captures the agent did not author.

**Classification is line/content-level, never a blanket file exemption.** Category 4 is applied at
the line level: a new-work line inside `MEMORY.md` is a candidate even though `MEMORY.md` is
otherwise auto-captured. **Precedence rule:** when an exempt marker and a new-work verb co-occur on
the same line (e.g. a `project_*`-style pointer that says "NEXT: implement <new thing>"), the
documented precedence in `design.md` decides the verdict — it is not left ambiguous.

---

## 5. Acceptance Criteria

> AC-1..AC-11 carried from the synthesis plan with the §3.5 revisions; AC-12/AC-13 are new (R2).
> Each AC is testable. AC IDs are authoritative for the traceability matrix.

**AC-1 — Discriminator is self-sufficient.**
GIVEN the work-item definition and the 4 exempt categories,
WHEN an agent reads them,
THEN any memory change can be classified as work-item or exempt **without asking a human**.

**AC-2 — workflow.md obligation (proposed-diff, not self-applied).**
GIVEN the rule strengthening,
WHEN delivered,
THEN it is a **proposed diff** in `proposed-diffs.md` (NOT applied to `workflow.md` by the agent)
stating "memory is never the sole home for a work item" + the same-session obligation + work-item
definition + 4 exempt categories; it carries the `amend:` commit + changelog triple; AND it records
that Helder must **read and edit** (not rubber-stamp) the generated prose (Authorship gate, R1-8).

**AC-3 — session-ops.md 6th tier (direct edit + Authorship review).**
GIVEN the tiered memory governance model in `session-ops.md`,
WHEN amended,
THEN device auto-memory is added as a **6th tier explicitly labelled "NOT a registration surface"**,
edited directly in-session (it is in `library/`, not under the `rules/*` deny glob), and routed
through Helder Authorship review.

**AC-4 — Classifier correctness (red→green).**
GIVEN `classify_memory_change(filename, line_or_diff)`,
WHEN called with each of the 4 exempt categories → returns **exempt**;
WHEN called with a new-work line → returns **candidate**;
AND each behavior was seen failing (red) before implementation (green).

**AC-5 — Advisory fires correctly, never blocks (spike-branched).**
GIVEN session end,
WHEN at least one changed memory line classifies **candidate** AND BACKLOG.md was NOT changed this
session — **where "changed memory line" is reported by the spike-confirmed signal, else
reviewer-supplied** — THEN the advisory prints a reminder; in ALL cases it **never blocks** and a
background/headless session completes normally.

**AC-6 — Fail-open.**
GIVEN any error, exception, missing device dir, or unreadable input,
WHEN the advisory runs,
THEN it exits silently with code 0 and prints no error.

**AC-7 — No false positive on legitimate use.**
GIVEN a session whose only memory writes are exempt (feedback / continuation / cache / auto),
WHEN the advisory runs,
THEN it prints **no** reminder.

**AC-8 — Spike gate.**
GIVEN the Phase 1 spike,
WHEN it succeeds (device-memory write is hook-observable AND path deterministically resolvable),
THEN Option B (PostToolUse interception) is built;
WHEN it fails, THEN `findings.md` records Option B **DEAD**, and only D + C + advisory-A ship; the
advisory operates on the spike-confirmed signal or is **reviewer-driven** (no mtime baseline).

**AC-9 — `.sln` registration (incl. `.py`).**
GIVEN every new file (`.md` AND `.claude/scripts/backlog/*.py`),
WHEN committed,
THEN it appears in `MyVocaList.sln` in the **same commit**. The `sync-docs-to-sln.ps1` hook only
handles `Docs\` paths on Write, so the `.py` files require an **explicit manual** `.sln` task.

**AC-10 — Single Stop entry, expected-keys unchanged.**
GIVEN `settings.json`,
WHEN `orphan_check.py` is wired,
THEN it is exactly one **command-type entry under the existing `Stop` key** (mirroring
`heartbeat.py`), introduces **no new top-level key**, and the SessionStart expected-keys check is
unchanged.

**AC-11 — Non-negotiable: no legitimate non-work-item memory use is ever flagged.**
(Stronger restatement of AC-7 as a hard invariant — a false positive on legitimate use is a defect.)

**AC-12 — `orphan_check.py` deterministic tests.**
GIVEN the wrapper,
WHEN tested,
THEN it has deterministic unit tests for path-resolution/enumeration against a **fixture dir**
(injected/parameterized path, not hardcoded-mangled) AND a fail-open test.

**AC-13 — Classifier precedence proven adversarially.**
GIVEN the precedence rule (exempt marker + new-work verb on one line),
WHEN tested,
THEN adversarial conflict cases (e.g. "NEXT: implement X") have tests proving the documented verdict.

---

## 6. Validation Rules

- `classify_memory_change` must return one of exactly two verdicts: `exempt` | `candidate`. No third state.
- `should_remind(classified_changes, backlog_changed_this_session)` returns `(bool, str)`; returns
  `(False, …)` whenever `backlog_changed_this_session` is true, regardless of candidates.
- Empty / whitespace / garbage input → `exempt` (never `candidate`), never raises.
- The device-dir path is a **parameter**, never a hardcoded literal, so it is fixture-testable.
- The advisory must never write to `changelog.md` (the `TaskCompleted`/`Stop` agent hooks own that file).

---

## 7. Invariants & Postconditions

- **INV-1 (fail-open):** No code path in `orphan_check.py` exits non-zero or blocks session end.
- **INV-2 (no new top-level settings key):** Only the existing `Stop` (and, spike-pass, `PostToolUse`) keys gain a command entry.
- **INV-3 (line-level classification):** No file is blanket-exempted; `MEMORY.md` lines are classified individually.
- **INV-4 (deny-list respected):** `workflow.md` and `CLAUDE.md` are never edited by the agent — only proposed diffs.
- **POST-1:** After Phase 5, every new `.md` and `.py` file is registered in `MyVocaList.sln`.
- **POST-2:** The BACKLOG row reaches `✅ Done` **only after** Helder applies the `workflow.md` `amend:`.

---

## 8. Out of Scope

- Hard-blocking session end (explicitly rejected — posture A).
- Headless/CI lockout.
- Retroactively auditing existing memory files for past orphans.
- Changing `CLAUDE.md` (600-line budget; recommend no change — Helder-reserved one-line pointer only if wanted).
- An mtime baseline fallback (explicitly rejected — reviewer-driven if the spike fails).
- Stamping the BACKLOG `🟡 In Progress` feature/phase row (that is a separate Session-Continuity follow-up).
- Cross-feature correlation precision: a session updating BACKLOG for feature X while writing a
  memory-only orphan for feature Y will suppress the reminder (documented limitation, § design 3.7).

---

## 9. Helder Decision Gates (downstream — do NOT block Phase 0)

| # | Decision | Recommendation / default | Surfaced at |
|---|----------|--------------------------|-------------|
| a | Exact Rule 1 obligation wording + 4 exempt categories | Plan wording locked as working def (Helder approves prose at `amend:`) | Phase 2 |
| b | Apply the `workflow.md` proposed diff | `amend:` + changelog triple, Helder-applied | Phase 5 |
| c | `session-ops.md` Authorship review | Direct edit, then Helder reads/edits | Phase 2 |
| d | Spike-fail fallback | Reviewer-driven advisory; drop mtime baseline | Phase 1 |
| e | Dedicated `.sln` subfolder vs flat | **Flat** under DevCycleCraft (default) | Phase 0 |
| — | CLAUDE.md touch | Recommend **none** | Phase 2 |
