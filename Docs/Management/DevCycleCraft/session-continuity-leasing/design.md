# Session Continuity — Task Leasing & Auto-Resume

> Status: **Spec (awaiting Helder review)** — spike COMPLETE 2026-06-14 (PASS), design LOCKED. `brainstorming` complete → see [`requirements.md`](./requirements.md); spike outcome in [`findings.md`](./findings.md); next is `writing-plans → plan.md` (plan execution still gated on Helder's approval of the spec).
> Owner decision session: Helder, 2026-06-13/14.
> Execution model: Opus 4.8, all phases.

> **Decisions locked 2026-06-14** (supersede the "Decision required before spec" section below):
> - **Freshness mechanism:** a hook-driven *activity heartbeat*. A `PostToolUse`/`Stop` hook
>   stamps `last_active = now` onto the owning session's claim(s) on every tool call. This
>   pings only while the session genuinely works and stops instantly on usage-window reset,
>   `/clear`, or crash — catching all three interruption modes uniformly, with no manual
>   ping and no background timer. Git-commit-on-branch freshness is retained only as the
>   *fallback* if the spike (AC-5) shows hooks cannot supply `session_id` or write the claim.
> - **Session ID source:** the Claude Code `session_id` exposed in hook payloads — a stable
>   self-id, nothing to generate, no worktree dependency.
> - **Liveness rule (Helder's two-fact model):** owner identity (`session_id`) + "is it
>   alive?" Alive = `last_active` within TTL **OR** recorded `pid` still running on this host.
>   A same-host dead `pid` permits *immediate* reclaim before TTL (fast path).
> - **Claim record shape:**
>   ```
>   owner: <claude session_id>   # who holds it
>   pid: <process id>            # optional same-host fast-reclaim hint
>   last_active: <ISO-8601 UTC>  # hook-maintained heartbeat (primary liveness signal)
>   resume_pointer: <one line>   # "continue from here"
>   ```
> - **Spike COMPLETE 2026-06-14 — AC-5.1 and AC-5.2 both PASS; design LOCKED.** Hooks expose
>   `session_id` (AC-5.1 PASS) and `PostToolUse`/`Stop` hooks can write the claim file on every
>   tool call (AC-5.2 PASS); `cwd` is present so the git-commit fallback is viable (AC-5.3 PASS).
>   See [`findings.md`](./findings.md).

## Problem

Two failure modes, both observed live on 2026-06-13:

1. **Collision** — two Claude Code terminals can both decide the same BACKLOG task is theirs to start. Resolved that day only because Helder manually arbitrated ("another terminal owns it"). One session left **uncommitted, incomplete work directly on `develop`** (no worktree, no claim, no resume pointer) — invisible to and un-resumable by any other session.
2. **Interruption without resume** — when the 5-hour usage window resets (or a session is `/clear`ed / crashes), in-progress work has no machine-readable "continue from here", so it cannot auto-resume; Helder must manually re-prompt.

## Goal (the "what")

- A fresh session can determine **on its own** whether an in-progress task is actively owned by a live session (→ skip) or abandoned (→ safe to reclaim and resume) — without Helder arbitrating.
- An interrupted feature/task **auto-resumes** after a usage-window reset without Helder typing "continue".
- No progress step is ever **permanently** blocked by an interruption: every claim has a built-in expiry.

How is unconstrained; favor the lightest mechanism that reuses existing artifacts (BACKLOG status, `tasks.md` `[~]`, git, worktrees).

## Why a plain lock is wrong (key insight)

A lock must be explicitly released by its holder. The whole concern is the holder **dying** (token reset, `/clear`, crash) — a dead holder never releases, converting a recoverable collision into a permanent freeze. The correct primitive is a **lease**: a claim valid only while *fresh*, auto-expiring so abandoned work becomes reclaimable.

## Design direction

### 1. Claim = lease (not lock)
Stamp a claim at two existing scopes:
- BACKLOG `🟡 In Progress` row → claims the **feature/phase** (prevents two sessions both starting the spec/plan/exec/review phase).
- `tasks.md` `[~]` marker → claims the **step** (already exists; upgrade it).

A claim carries: which session holds it + a **freshness signal**.

### ~~SUPERSEDED~~ 2. Freshness keyed off git (not a written timestamp)
> Superseded by Decisions locked 2026-06-14 (heartbeat primary; git = fallback only). Retained for history.

"Is this claim alive?" = "has its dedicated worktree branch had a commit within TTL?" Tamper-proof, survives a forgotten heartbeat, and leans on the existing per-wave worktree/branch rule (`orchestrator.md § Git Worktrees`). A claim whose branch is stale → reclaimable.

- **TTL purpose (Helder, 2026-06-13):** the expiry exists so a *fresh* session after a 5-hour-limit reset can auto-resume — NOT as a cross-terminal race window. ~45 min is a starting value tied to task-sizing limits (most tasks ≤ 90 min, commit-per-task).

### 3. Resume pointer (the only genuinely new artifact)
A one-line "continue from here" note attached to the claim, so a cold session reads claim + `tasks.md` + last commit and knows the exact next step. Bounds resume cost.

### 3a. Storage locations (pinned 2026-06-14)
- **Claim file** *(ARCHITECT-DISCRETION DEFAULT — flag for Helder)*: `.claude/leases/<session_id>.json` — one JSON file per session, gitignored (ephemeral, per-machine, never committed). The claim record shape above (L19-25) is serialized as the JSON body. **DECISION DEFAULTED 2026-06-14 — Helder may relocate; low blast radius.**
- **TTL single source** *(ARCHITECT-DISCRETION DEFAULT — flag for Helder)*: a single named constant `LEASE_TTL_SECONDS` (default `2700` = 45 min) defined once in the lease hook script and read by BOTH the heartbeat writer and the reclaim/freshness check. **DECISION DEFAULTED 2026-06-14 — Helder may retune the value/location.**
- **Resume pointer**: canonical storage is the `resume_pointer` field inside the per-session claim file; a human-readable one-line echo MAY also be appended to the `tasks.md` `[~]` line, but the claim file is authoritative.
- **Atomic write**: the heartbeat/reclaim hook writes the claim atomically (write `<file>.tmp`, then `mv`/rename over the target). Because files are keyed by `session_id` (one per session), no two sessions ever write the same file; a corrupt/half-written file is treated as absent/stale (reclaimable).
- **Concurrent reclaim (single-winner)**: after writing its own claim, a reclaiming session RE-READS the claim and proceeds only if `owner` equals its own `session_id` (enforces INV-3); the loser re-evaluates and selects the next work unit.

### 4. Reclaim + auto-resume rules
- Reclaim rule added to `workflow.md` Rule 4 (`[~]` handling) and Rule 8 (collision check — already does a collision check, just without liveness).
- Auto-resume wired via a scheduled wakeup that re-enters the session and reads the resume pointer.

## ~~SUPERSEDED~~ Decision required before spec
> Superseded by Decisions locked 2026-06-14 (heartbeat primary; git = fallback only). Retained for history.

1. **TTL source** — ~45 min written timestamp, or "last git commit on branch" (recommended: git — tamper-proof).
2. **Session ID source** — Claude Code exposes no stable self-id; generate one at session start (token in a session-scoped file) or key off branch/worktree path (already unique per wave). Recommended: worktree/branch identity.

## Live validation (2026-06-13 incident)
Danger was uncommitted work on a shared branch with no claim and no isolation; resolution required manual human arbitration. This mechanism removes that manual step and would have: (a) marked the work claimed+fresh so this session auto-skipped, and (b) left a resume pointer for the owning session after its own interruption.

## Companion task
Context-Size Self-Monitoring & Auto-Clear Advisory (separate BACKLOG row) — the agent advises when context is large enough to clear, emits a continuation prompt + handoff file, and optionally self-interrupts. Complements auto-resume: one ends a bloated session cleanly, the other resumes it.
