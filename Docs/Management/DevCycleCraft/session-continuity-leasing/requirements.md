# Session Continuity — Task Leasing & Auto-Resume — Requirements

> Status: **Spec (awaiting Helder review)** · Owner decisions: Helder, 2026-06-13/14
> Companion direction doc: [`design.md`](./design.md)
> Execution model: Opus 4.8, all phases.

## Purpose

Eliminate two failure modes observed live on 2026-06-13 without requiring Helder to
arbitrate manually:

1. **Collision** — two Claude Code sessions both decide the same BACKLOG task / `tasks.md`
   step is theirs to start.
2. **Interruption without resume** — a usage-window reset, `/clear`, or crash leaves
   in-progress work with no machine-readable "continue from here", so it cannot auto-resume.

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Claim** | A lease stamped onto a work unit recording who holds it and a liveness signal. |
| **Lease** | A claim valid only while its owner is *fresh*; auto-expires so a dead owner never freezes the work. Contrast with a **lock**, which must be explicitly released and freezes forever if the holder dies. |
| **Owner** | The session holding a claim, identified by the Claude Code `session_id`. |
| **Liveness signal** | `last_active` timestamp (heartbeat) plus optional `pid`, used to decide whether an owner is still working. |
| **Freshness / fresh** | `now − last_active < TTL`, OR the recorded `pid` is still a running process on the same host. |
| **Stale / abandoned** | Not fresh — the claim is reclaimable. |
| **Reclaim** | A new session takes over a stale claim and resumes its work unit. |
| **Resume pointer** | A one-line "continue from here" note attached to a claim, bounding the cost of a cold resume. |
| **Work unit** | Either a feature/phase (BACKLOG `🟡 In Progress` row) or a step (`tasks.md` `[~]` marker). |
| **TTL** | Time-to-live for the heartbeat freshness window. Starting value ~45 min, tied to task-sizing limits (most tasks ≤ 90 min, commit-per-task). |

## User Stories & Acceptance Criteria

### Story 1 — A fresh session detects an actively-owned work unit and skips it
As a Claude Code session starting work, I want to detect that an in-progress task is
held by a live session, so that I do not collide with it.

- **AC-1.1** GIVEN a work unit with a claim whose `last_active` is within TTL,
  WHEN a second session evaluates that work unit,
  THEN it classifies the claim as **fresh** and does NOT start that work unit.
- **AC-1.2** GIVEN a work unit whose claim's `last_active` is older than TTL BUT whose
  recorded `pid` is still a running process on the same host,
  WHEN a second session evaluates it,
  THEN it classifies the claim as **fresh** (a live PID is a sufficient freshness
  condition on its own, independent of the TTL window).
- **AC-1.3** GIVEN a claim classified as fresh, WHEN the second session is blocked from it,
  THEN it selects the next available work unit per `workflow.md` Rule 4 instead of waiting.

### Story 2 — A fresh session reclaims an abandoned work unit
As a Claude Code session, I want to reclaim a work unit whose owner is no longer alive,
so that interrupted work is never permanently blocked.

- **AC-2.1** GIVEN a claim whose `last_active` is older than TTL AND whose `pid` is not a
  running process (or no `pid` is recorded), WHEN a new session evaluates it,
  THEN it classifies the claim as **stale** and is permitted to reclaim it.
- **AC-2.2** GIVEN a claim whose recorded `pid` is provably not running on the same host,
  WHEN a new session evaluates it BEFORE the TTL has elapsed,
  THEN it MAY reclaim immediately (fast-reclaim path) without waiting out the TTL.
- **AC-2.3** WHEN a session reclaims a work unit,
  THEN it overwrites the claim's `owner`, `pid`, and `last_active` with its own values
  before performing any work on that unit.
- **AC-2.4** GIVEN two sessions both observe the same stale claim,
  WHEN both attempt to reclaim it concurrently (TOCTOU race),
  THEN the mechanism guarantees a single winner: after writing its own claim, a reclaiming
  session **RE-READS** the claim and proceeds only if `owner` equals its own `session_id`;
  the loser (whose re-read shows a different `owner`) re-evaluates and selects the next
  available work unit. This enforces INV-3.
- **AC-2.5** GIVEN a claim file that is corrupt, unparseable, or half-written (e.g. a hook
  that crashed mid-write), WHEN a session evaluates it,
  THEN the claim is treated as **ABSENT/stale** (reclaimable). Atomic writes (see Validation
  Rules) make this rare, but the rule MUST be explicit so a crashed mid-write never
  permanently blocks a work unit.

### Story 3 — Heartbeat is maintained automatically, not manually
As the system, I want the liveness signal to update on its own while a session works,
so that no manual ping step exists to be forgotten.

- **AC-3.1** WHEN the owning session performs any tool call,
  THEN a hook updates that session's active claim(s) `last_active` to the current time.
- **AC-3.2** WHEN the owning session is interrupted (usage-window reset, `/clear`, or crash),
  THEN `last_active` stops advancing (no further tool calls occur), so the claim ages into
  staleness after TTL with no manual action.
- **AC-3.3** The heartbeat MUST NOT require the agent to run a background timer or emit a
  periodic ping by itself.
- **AC-3.4** GIVEN a subagent performing a tool call (its hook payload carries
  `agent_id`/`agent_type`), WHEN the heartbeat fires,
  THEN it updates the **OWNING (parent) session's** claim — keyed by the parent `session_id`,
  NOT the subagent — so a long-running subagent wave keeps the parent claim fresh and does
  not let it age into staleness (self-collision prevention).

### Story 4 — An interrupted work unit auto-resumes after a usage-window reset
As Helder, I want interrupted work to continue without me typing "continue".

- **AC-4.1** GIVEN a work unit this session owns with a written resume pointer,
  WHEN the session is interrupted by a usage-window reset,
  THEN a scheduled wakeup re-enters and continues from the resume pointer without a
  manual re-prompt.
  *Scope: this covers the IN-SESSION usage-window reset case via an in-session scheduled
  wakeup. Resilience to a FULLY-CLOSED terminal is out of scope (would require a cloud
  routine / external monitor) — see `findings.md § self-scheduled re-entry`.*
- **AC-4.2** GIVEN a reclaiming session, WHEN it takes over a stale work unit,
  THEN it reads the resume pointer + `tasks.md` + last commit and determines the exact
  next step before acting.
- **AC-4.3** A resume pointer MUST be written/updated whenever a session claims or makes
  material progress on a work unit, so a cold session is never left without one. Its
  canonical storage location is the `resume_pointer` field inside the per-session claim file
  (see Storage Locations); a human-readable one-line echo MAY also be appended to the
  `tasks.md` `[~]` line, but the claim file is authoritative.

### Story 5 — The spike validates the linchpin before rules are locked
As the team, I want to confirm the hook mechanism works before committing the design.

- **AC-5.1** [DONE 2026-06-14 — see findings.md] A spike confirms hooks expose `session_id`
  in their payload (or documents the exact alternative available).
- **AC-5.2** [DONE 2026-06-14 — see findings.md] A spike confirms a hook can reliably
  write/update the claim file on tool use.
- **AC-5.3** [DONE 2026-06-14 — see findings.md] IF the spike fails either check, THEN the
  fallback (git-commit-on-branch freshness) is documented in `findings.md` and the design
  updated before implementation.

## Validation Rules

| Field | Rule |
|-------|------|
| `owner` | Required on every claim. Sourced from Claude Code `session_id`. |
| `pid` | Optional. When present, used only for the same-host fast-reclaim path. |
| `last_active` | Required. ISO-8601 UTC timestamp. Updated by the heartbeat hook. |
| `resume_pointer` | Required once material progress exists. One line, ≤ ~200 chars. Stored in the per-session claim file (see Storage Locations). |
| TTL | Single source of truth: the named constant `LEASE_TTL_SECONDS` (default `2700` = 45 min), defined once in the lease hook script and read by BOTH the heartbeat writer and the reclaim/freshness check. |
| claim file write | MUST be written atomically (write to `<file>.tmp` then `mv`/rename over the target). Claim files are per-session, keyed by `session_id` (one file per session), so no two sessions ever write the same file. |

## Storage Locations

| Item | Location |
|------|----------|
| **Claim file** *(ARCHITECT-DISCRETION DEFAULT — flag for Helder)* | `.claude/leases/<session_id>.json` — one JSON file per session. MUST be gitignored (ephemeral, per-machine, never committed). **DECISION DEFAULTED 2026-06-14 — Helder may relocate; low blast radius.** |
| **Claim record format** | JSON. The claim record shape (`owner`, `pid`, `last_active`, `resume_pointer` — see `design.md`) is serialized as the JSON body of the claim file. |
| **TTL constant** *(ARCHITECT-DISCRETION DEFAULT — flag for Helder)* | A single named constant `LEASE_TTL_SECONDS` (default `2700` = 45 min) defined once in the lease hook script; the single source of truth read by both the heartbeat writer and the reclaim/freshness check. **DECISION DEFAULTED 2026-06-14 — Helder may retune the value/location.** |
| **Resume pointer** | Canonical: the `resume_pointer` field inside the per-session claim file. A human-readable one-line echo MAY also be appended to the `tasks.md` `[~]` line, but the claim file is authoritative. |

## Out of Scope

- Cross-machine / multi-host coordination. (Liveness PID checks assume same host; the
  heartbeat TTL is the only cross-host-safe signal and is sufficient.)
- A central lock server, daemon, or database. Files + hooks + git only.
- Real-time mutual exclusion finer than the work-unit granularity (feature/phase + step).
- Automatically killing or signalling another running session.
- Conflict *resolution* of already-committed divergent work (this prevents collision; it
  does not merge two sessions that both edited the same files before claiming).
- Auto-resume resilience to a **fully-closed terminal**. AC-4.1 covers only the in-session
  usage-window reset case (an in-session scheduled wakeup). Recovering after the terminal
  process is fully closed would require a cloud routine / external monitor — see
  `findings.md § self-scheduled re-entry`.

## Invariants & Postconditions

- **INV-1** At most one **fresh** claim exists per work unit at any time.
- **INV-2** A claim that is not fresh is always reclaimable — no work unit is ever
  permanently blocked by an interruption.
- **INV-3** After a reclaim, the claim's `owner`/`pid`/`last_active` reflect the new owner
  before any work proceeds (no two sessions believe they own the same unit). A reclaiming
  session enforces this by **re-reading** its just-written claim and proceeding only if
  `owner` equals its own `session_id` (see AC-2.4); a concurrent reclaim therefore yields a
  single winner.
- **INV-4** The heartbeat advances only as a side effect of genuine session activity.

## Reversibility

Low blast radius. The mechanism is additive: claim fields on existing artifacts
(BACKLOG rows, `tasks.md` markers), one new hook, two `workflow.md` rule edits, and a
scheduled-wakeup wiring. Removing it = delete the hook, revert the rule edits, ignore the
claim fields. No schema, no migration, no data loss.

## Demo Statement

With two terminals open: terminal A claims a task and works it; terminal B starts, reads
the claim, finds it fresh, and selects a different task automatically. Then terminal A is
`/clear`ed; after TTL, terminal B (or a scheduled wakeup) finds the claim stale, reclaims
it, reads the resume pointer, and continues the exact next step — with no Helder
arbitration at any point.
