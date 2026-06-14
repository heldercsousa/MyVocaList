# In-Session Auto-Resume Runbook (AC-4.1)

> Feature: Session Continuity — Task Leasing & Auto-Resume (Phase 6 / Task 9).
> Scope: **IN-SESSION usage-window reset only.** See § Out of Scope.

## Purpose

When a session is interrupted by a **usage-window (5-hour limit) reset** while the terminal
remains open, a scheduled in-session wakeup re-enters, reads the session's `resume_pointer`,
`tasks.md`, and the last commit, and continues the **exact next step** with no human
re-prompt (AC-4.1 / AC-4.2). The resume pointer is the canonical "continue from here" note
stored in the per-session claim file (`.claude/leases/<session_id>.json`,
`resume_pointer` field).

## Out of Scope — fully-closed terminal (durability)

A `/loop` / `CronCreate` / `ScheduleWakeup` schedule is **session-bound**: it fires only
while the terminal/session is open and idle, is lost if the session process exits, and is
only restored on `--resume` (≤ 7 days). **Recovering after the terminal is fully closed is
OUT OF SCOPE** for this feature and would require a *cloud routine* (`/schedule`) or an
external monitor process — see `findings.md § self-scheduled re-entry` and `findings.md
§ Bonus`. This runbook covers only the in-session case (AC-4.1 scope note in
`requirements.md` L98-100).

## Prerequisites

- The heartbeat hook is registered (`.claude/settings.json`, `PostToolUse` + `Stop`) and
  `.claude/leases/` is gitignored (Task 7, committed). The hook keeps this session's claim
  fresh automatically on every tool call (AC-3.1/3.3) and keys off the **parent**
  `session_id` even during subagent waves (AC-3.4).
- `python` is available on PATH (the repo's existing hooks already assume Python 3).
- The lease scripts exist under `.claude/scripts/lease/`.

## Step 1 — Record a resume pointer as you work (AC-4.3)

A resume pointer MUST exist before an interruption can be recovered. Whenever material
progress is made (claiming a unit, finishing a step), write a one-line pointer:

```bash
python .claude/scripts/lease/resume.py --set <session_id> "Continue Task 6 step 6.3 — reclaim path test"
```

- `<session_id>` is this session's Claude Code session id (the same id the heartbeat hook
  writes — i.e. the file name under `.claude/leases/`, minus `.json`).
- The pointer is truncated to ≤ 200 chars and stored in the claim file's `resume_pointer`
  field, which the heartbeat preserves on every subsequent tool call (AC-4.3).

## Step 2 — Arm the in-session wakeup

Use the `/loop` skill to schedule a recurring in-session wakeup that re-checks for a reset
and re-enters. Example (operator types in the session):

```
/loop 30m Run `python .claude/scripts/lease/resume.py <session_id>`; if a RESUME POINTER is
printed and the previous turn was interrupted by a usage-window reset, continue the exact
next step it names.
```

Notes:
- A 30-min cadence matches `LEASE_TTL_SECONDS=1800` so a wakeup lands shortly after a claim
  would age. Tighten/loosen as needed; the wakeup is idempotent (it only reads).
- The loop is session-bound: if the terminal is fully closed it stops firing (Out of Scope).
- The memory note for this feature already records `/loop continue every 30min, full
  autonomy pre-authorized`.

## Step 3 — On wakeup, re-enter and continue

When the scheduled wakeup fires, the agent runs:

```bash
python .claude/scripts/lease/resume.py <session_id>
```

which prints exactly three lines:

```
RESUME POINTER: <the one-line continue-from-here, or "(no resume pointer recorded)">
LAST COMMIT: <subject of the last git commit>
NEXT: read the active feature tasks.md, find the [~] step, and continue from the pointer.
```

The agent then:
1. Reads the printed `RESUME POINTER` — the exact next step.
2. Reads `LAST COMMIT` to confirm what is already committed (avoid redoing it).
3. Opens the active feature `tasks.md`, finds the `[~]` step, and continues from the pointer
   (AC-4.2 — pointer + tasks.md + last commit together fix the continuation point).
4. Resumes work; the heartbeat hook re-freshens the claim on the first tool call.

## Step 4 — Reclaiming session variant (different session continues)

If the interrupted session is gone (`/clear` retired its id) and **another** session picks
the work up, that session first reclaims, then resumes from the *same* pointer (the reclaim
preserves `resume_pointer`, AC-2.3):

```bash
python .claude/scripts/lease/reclaim.py <my_session_id> <interrupted_session_id>   # -> reclaimed
python .claude/scripts/lease/resume.py <interrupted_session_id>                     # -> prints the preserved pointer
```

## Operator quick reference

| Goal | Command |
|------|---------|
| Set/refresh resume pointer | `python .claude/scripts/lease/resume.py --set <sid> "<one line>"` |
| Read pointer + last commit | `python .claude/scripts/lease/resume.py <sid>` |
| Arm in-session wakeup | `/loop 30m Run python .claude/scripts/lease/resume.py <sid> and continue from the printed pointer` |
| Reclaim an interrupted session's unit | `python .claude/scripts/lease/reclaim.py <my_sid> <their_sid>` |

## Failure modes

- **No pointer recorded:** `resume.py` prints `(no resume pointer recorded)` — fall back to
  `tasks.md` `[~]` + last commit to infer the next step, and set a pointer immediately.
- **Claim file absent:** `resume.py` prints `NO CLAIM FOUND` (exit 1) — the session never
  heartbeated; start fresh from `tasks.md`.
- **Terminal fully closed:** no wakeup fires (Out of Scope) — manual re-prompt or a future
  cloud routine is required.
