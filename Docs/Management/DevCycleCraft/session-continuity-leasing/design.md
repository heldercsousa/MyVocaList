# Session Continuity — Task Leasing & Auto-Resume

> Status: **direction captured** (this doc). Needs `brainstorming → requirements.md → plan.md` before implementation.
> Owner decision session: Helder, 2026-06-13.

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

### 2. Freshness keyed off git (not a written timestamp)
"Is this claim alive?" = "has its dedicated worktree branch had a commit within TTL?" Tamper-proof, survives a forgotten heartbeat, and leans on the existing per-wave worktree/branch rule (`orchestrator.md § Git Worktrees`). A claim whose branch is stale → reclaimable.

- **TTL purpose (Helder, 2026-06-13):** the expiry exists so a *fresh* session after a 5-hour-limit reset can auto-resume — NOT as a cross-terminal race window. ~45 min is a starting value tied to task-sizing limits (most tasks ≤ 90 min, commit-per-task).

### 3. Resume pointer (the only genuinely new artifact)
A one-line "continue from here" note attached to the claim, so a cold session reads claim + `tasks.md` + last commit and knows the exact next step. Bounds resume cost.

### 4. Reclaim + auto-resume rules
- Reclaim rule added to `workflow.md` Rule 4 (`[~]` handling) and Rule 8 (collision check — already does a collision check, just without liveness).
- Auto-resume wired via a scheduled wakeup that re-enters the session and reads the resume pointer.

## Decision required before spec
1. **TTL source** — ~45 min written timestamp, or "last git commit on branch" (recommended: git — tamper-proof).
2. **Session ID source** — Claude Code exposes no stable self-id; generate one at session start (token in a session-scoped file) or key off branch/worktree path (already unique per wave). Recommended: worktree/branch identity.

## Live validation (2026-06-13 incident)
Danger was uncommitted work on a shared branch with no claim and no isolation; resolution required manual human arbitration. This mechanism removes that manual step and would have: (a) marked the work claimed+fresh so this session auto-skipped, and (b) left a resume pointer for the owning session after its own interruption.

## Companion task
Context-Size Self-Monitoring & Auto-Clear Advisory (separate BACKLOG row) — the agent advises when context is large enough to clear, emits a continuation prompt + handoff file, and optionally self-interrupts. Complements auto-resume: one ends a bloated session cleanly, the other resumes it.
