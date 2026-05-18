# Plan: BACKLOG.md ↔ .claude Integration

**Context:** BACKLOG.md was introduced as a SCRUM-style product backlog — a prioritized wish list where every feature starts as a brief idea and progresses through Spec → Plan → In Progress → Done. However, none of the `.claude` workflow rules reference it. Agents starting a new feature cycle have no instruction to consult BACKLOG.md, and no workflow step tells agents to update BACKLOG.md statuses as a feature progresses. Additionally, `workflow.md` Rule 7 references `MASTER_PLAN.md` (a one-off SDD document now ✅ Done and no longer relevant as a session-start artifact).

**Goal:** Make BACKLOG.md a first-class citizen in the dev workflow — agents know when to read it, when to update it, and what the statuses mean in the context of each workflow step.

---

## What changes and why

### 1. `Docs/BACKLOG.md` — clarify its contract

Add/update the header preamble to make explicit:
- The list is **priority-ordered** (top = most important)
- The **MVP cut-line** should be marked (a horizontal separator labelled `MVP scope ends here`)
- Who updates statuses and when (the main agent updates BACKLOG.md as part of the new feature workflow — not subagents)
- The `Recently Done` table gets a new row when a feature ships

The Status reference table **stays in BACKLOG.md** — it's definitional content for this file, not guidance for agents. No need to move it.

Current "Ideas / Planned" section: rename to `💡 Backlog` for SCRUM alignment.

### 2. `workflow.md` Rule 1 — add BACKLOG.md lifecycle steps

The new feature workflow becomes:

```
0. Identify — read BACKLOG.md; find the highest-priority 🟢 Ready or 💡 Idea item
1. Brainstorm — invoke superpowers:brainstorming; update BACKLOG.md status → 📋 Spec
2. Write spec — write all three files; user approves; update status → 🗺️ Plan
   2a. Constitution check
3. Write plan — invoke superpowers:writing-plans; user approves; update status → 🟢 Ready
4. Implement — dispatch subagents; update status → 🟡 In Progress
5. Phase-gate review — /project:review after each phase
   [on ship] update status → ✅ Done; move to Recently Done table
```

Each status transition is **the main agent's responsibility** (not subagents). One-line rule: "After each workflow milestone, update the corresponding BACKLOG.md status before proceeding to the next step."

### 3. `workflow.md` Rule 7 — fix stale MASTER_PLAN.md reference

Remove step 2 (`MASTER_PLAN.md`). Replace with a conditional:

> "If no active feature is known from the handoff file, read `Docs/BACKLOG.md` to identify the current 🟡 In Progress item or the highest-priority 🟢 Ready item — that is the current work context."

This makes BACKLOG.md the authoritative "what are we working on?" source for session start, replacing the one-off MASTER_PLAN.md.

---

## Files to change

| File | Change |
|------|--------|
| `Docs/BACKLOG.md` | Rewrite header preamble; add MVP cut-line; rename "Ideas/Planned" → "💡 Backlog"; add update-responsibility note |
| `.claude/rules/workflow.md` | Rule 1: add step 0 + status-update callouts at each milestone. Rule 7: remove MASTER_PLAN.md ref; add BACKLOG.md conditional |

No other files need to change. The Status reference table stays in BACKLOG.md. `session-ops.md` and `spec-writing-guide.md` are unaffected.

---

## Verification

After changes:
- Rule 1 reads as a coherent 0→5 flow with clear BACKLOG.md update points
- Rule 7 no longer mentions MASTER_PLAN.md
- BACKLOG.md header tells agents when to read it and who updates statuses
- The current "Artists & Songs" item status (🟡 In Progress) is still correct
- The MVP cut-line is present and positioned correctly in BACKLOG.md
