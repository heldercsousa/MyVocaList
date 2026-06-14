# HELDER HANDOFF — apply manually; `.claude/rules/` is write-protected

> **Why this is a handoff and not a direct edit (R1):** `.claude/settings.json`
> `permissions.deny` blocks `Edit(.claude/rules/*.md)` and `Write(.claude/rules/*.md)`,
> and CLAUDE.md § *Amending These Rules* requires an `amend:` commit prefix plus a
> `Docs/Changelog/changelog.md` entry for any change to `workflow.md`. A subagent
> therefore **cannot** apply these edits. This document is the reviewed proposal; Helder
> applies it to `.claude/rules/workflow.md` himself.
>
> Feature: Session Continuity — Task Leasing & Auto-Resume (Phase 5 / Task 8).
> Produced by: lease implementation wave, 2026-06-14.
> Depends on the committed scripts: `.claude/scripts/lease/reclaim.py`,
> `.claude/scripts/lease/resume.py`, `.claude/scripts/lease/lease_lib.py`,
> `.claude/scripts/lease/heartbeat.py`.

All three edits are **additive** (low blast radius, per requirements.md § Reversibility).
None weakens an existing rule; each strengthens the existing `[~]`/collision handling by
adding a *liveness* check before a stale-vs-owned decision is made. Removing the feature =
revert these three inserts.

---

## Edit 1 — Rule 4: `[~]` reclaim semantics

**Target section:** `## Rule 4 — Tasks.md Is the Source of Truth`
→ subsection `### In-progress marker — [~] for claimed tasks` (≈ line 405).

**Rationale (one line):** today a `[~]` task is treated as untouchable; with leasing a
session must first decide whether the claim is *live* (skip) or *stale* (reclaim) using
the freshness helper, so an interrupted owner never permanently blocks the step.

**Insert** the following block immediately AFTER the existing rule line
`**Rule:** Never dispatch a task marked [~]. If a subagent was killed without completing a [~] task, reset it to [ ] before re-dispatching.`

### Before

```markdown
**Rule:** Never dispatch a task marked `[~]`. If a subagent was killed without completing a `[~]` task, reset it to `[ ]` before re-dispatching.
```

### After

```markdown
**Rule:** Never dispatch a task marked `[~]`. If a subagent was killed without completing a `[~]` task, reset it to `[ ]` before re-dispatching.

#### Lease-aware `[~]` reclaim (Session Continuity)

A `[~]` claim is a **lease, not a lock** — it is only binding while its owner is *fresh*.
Before treating a `[~]` task as owned-and-blocked, classify its claim with the lease
helper rather than assuming the owner is still alive:

1. Identify the owner session id from the claim file under `.claude/leases/` (the claim
   whose `resume_pointer` matches the work, or the only live claim on this host).
2. Run `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>` and act
   on the single printed word:
   - `fresh`     → the owner is alive; **leave the `[~]` task** and select the next `[ ]`
     task (this is AC-1.3 — do not wait).
   - `reclaimed` → the claim was stale; you now own it. Run
     `python .claude/scripts/lease/resume.py <owner_session_id>` to read the resume pointer,
     then continue the exact next step (AC-2.3 / AC-4.2). Leave the marker `[~]` (it is now
     yours) — do not reset to `[ ]`.
   - `lost`      → a concurrent session reclaimed first; re-evaluate and select the next
     `[ ]` task (AC-2.4 / INV-3).

> Only reset a `[~]` to `[ ]` when the claim classifies as **stale** AND you choose not to
> reclaim it. Never reset a `fresh` claim.
```

---

## Edit 2 — Rule 7: session-start claim refresh + resume-pointer read

**Target section:** `## Rule 7 — Session Start Protocol`
→ subsection `### Session start reading order` (the numbered list 0–6).

**Rationale (one line):** on start a session must (a) refresh/write its own claim so other
sessions see it as live (heartbeat), and (b) read any existing claim's `resume_pointer`
before resuming, so continuation is exact and collisions are prevented from step one.

**Insert** a new step **7** immediately after the existing step `6.` (the `task-log.md`
read), and extend the "Rule" line to cover steps 1–7.

### Before

```markdown
6. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/task-log.md`** — check for unresolved `blocked:` statuses or `Spec updated — re-planning required` entries

**Rule:** Steps 1–6 are mandatory. Steps 3–6 may be scoped to the specific feature being worked on if multiple features are in flight.
```

### After

```markdown
6. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/task-log.md`** — check for unresolved `blocked:` statuses or `Spec updated — re-planning required` entries
7. **Lease claim refresh + resume-pointer read (Session Continuity):**
   - For the picked work unit, classify any existing `[~]`/`🟡 In Progress` claim under
     `.claude/leases/` via `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>`:
     `fresh` → pick a different unit; `reclaimed` → take over; `lost` → pick the next unit
     (see Rule 4 lease-aware reclaim). Reclaim any **stale** unit before starting new work.
   - Before resuming, read the resume pointer with
     `python .claude/scripts/lease/resume.py <session_id>` and continue from it.
   - The heartbeat hook (registered in `.claude/settings.json`, `PostToolUse`/`Stop`) writes
     and keeps this session's own claim fresh automatically on every tool call — no manual
     ping is required (AC-3.1/3.3). Record a resume pointer as material progress is made via
     `python .claude/scripts/lease/resume.py --set <session_id> "<one-line continue-from-here>"` (AC-4.3).

**Rule:** Steps 1–7 are mandatory. Steps 3–7 may be scoped to the specific feature being worked on if multiple features are in flight.
```

---

## Edit 3 — Rule 8: add the LIVENESS dimension to the collision check

**Target section:** `## Rule 8 — GitHub MCP Pre-Task Collision Check`
→ subsection `### Pre-task collision check protocol`, and the
`**Collision types and responses:**` table row for an unattributed `[~]` task (≈ line 630).

**Rationale (one line):** the current collision check treats any `[~]` with no known
running agent as abandoned and resets it; that is unsafe when the owner is merely a
different live session — classify liveness first and reset only when **stale**.

### Edit 3a — protocol step (add to the "If the GitHub MCP is NOT available" list AND as a general note)

#### Before

```markdown
**If the GitHub MCP is NOT available:**
- Run `git log --oneline -10` to check recent commits
- Run `git status` to confirm no uncommitted changes from a previous interrupted session
- Check `tasks.md` for any tasks marked `[~]` that should not be in-progress
```

#### After

```markdown
**If the GitHub MCP is NOT available:**
- Run `git log --oneline -10` to check recent commits
- Run `git status` to confirm no uncommitted changes from a previous interrupted session
- Check `tasks.md` for any tasks marked `[~]` that should not be in-progress
- **Liveness check (Session Continuity):** for every `[~]` task with no known running
  agent, classify its claim under `.claude/leases/` via
  `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>` (which calls
  `lease_lib.classify`) **before** assuming abandonment. A `fresh` result means another live
  session owns it — do NOT reset to `[ ]`.
```

### Edit 3b — collision-types table row

#### Before

```markdown
| `[~]` task exists but no agent is known to be running it | Reset to `[ ]` and re-dispatch. |
```

#### After

```markdown
| `[~]` task exists but no agent is known to be running it | Classify the claim via `reclaim.py` / `lease_lib.classify`. `fresh` → another live session owns it, leave it and pick the next unit. `stale` → reclaim (`reclaimed`) and resume from the pointer, or reset to `[ ]` and re-dispatch if not resuming. Never reset a `fresh` claim. |
```

---

## Required `amend:` commit message (CLAUDE.md § Amending These Rules)

```text
amend: workflow.md Rules 4/7/8 — lease-aware [~] reclaim, session-start claim/resume, collision liveness

What was wrong: Rule 4 treated every [~] task as untouchable and Rule 8 reset any
unattributed [~] to [ ], with no liveness check — so a live owner could be stomped and a
dead owner could permanently block a step. Rule 7 had no session-start step to refresh the
session's own claim or read a resume pointer.

Change: add lease-aware reclaim semantics (Rule 4), a session-start claim-refresh +
resume-pointer-read step (Rule 7), and a liveness dimension to the collision check (Rule 8),
all driven by the committed .claude/scripts/lease/ helpers.

Backward compatibility: additive only; no existing rule weakened. With no claim files
present the new steps are no-ops (reclaim.py on an absent claim returns reclaimed/—,
classify treats absent as stale), so prior behavior is preserved.

Feature: Session Continuity — Task Leasing & Auto-Resume (Phase 5 / Task 8).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
```

## Required changelog entry (`Docs/Changelog/changelog.md`)

```markdown
### [amend] workflow.md Rules 4/7/8 — lease-aware reclaim (Session Continuity) — effective 2026-06-14

- **Old rule (Rule 4):** "Never dispatch a task marked `[~]`. … reset it to `[ ]` before re-dispatching." — no liveness distinction.
- **New rule (Rule 4):** adds *lease-aware `[~]` reclaim* — classify the claim via `reclaim.py`; `fresh` → skip, `reclaimed` → take over + resume, `lost` → pick next; reset to `[ ]` only when stale and not reclaiming.
- **Old rule (Rule 7):** session-start steps 1–6, no claim/resume step.
- **New rule (Rule 7):** adds step 7 — refresh own claim (heartbeat hook), classify/reclaim existing claims, read resume pointer before resuming.
- **Old rule (Rule 8):** unattributed `[~]` → "Reset to `[ ]` and re-dispatch."
- **New rule (Rule 8):** classify liveness first; reset only when `stale`; never reset a `fresh` claim.
- **Effective date:** 2026-06-14.
- **Rationale:** prevent both collision (stomping a live owner) and permanent block (dead owner never releases).
```

---

## After applying

1. Apply Edits 1–3 to `.claude/rules/workflow.md` with the `amend:` commit above.
2. Add the changelog entry to `Docs/Changelog/changelog.md`.
3. Decide whether to keep this proposal doc (already registered in `.sln`) or delete it
   once applied. If deleted, remove its `.sln` entry in the same commit.
