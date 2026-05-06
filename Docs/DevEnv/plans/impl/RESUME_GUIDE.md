# SDD Implementation — Human Resume Guide

> For Helder's use when resuming this work in a new Claude Code session.

---

## Context

We reviewed the SDD (Spec-Driven Development) methodology across 10 sections (S1–S10)
and identified 120+ enhancement opportunities for the project's internal config files.
All opportunities are documented and tracked. Implementation proceeds one atomic step at a time.

---

## How to Resume

### Step 1 — Start a fresh Claude Code session
Use `/clear` or open a new session to ensure a clean context window.

### Step 2 — Give Claude this prompt

```
Continue the SDD implementation plan. Read Docs/DevEnv/plans/impl/MASTER_PLAN.md,
find the first step that is not Done, mark it In Progress, and execute that one step only.
After committing, stop and report what was done.
```

### Step 3 — Review and continue
After each step completes and is committed, either:
- Tell Claude to continue to the next step, or
- Do a `/clear` and repeat Step 2 above

---

## Key Files

| File | Purpose |
|------|---------|
| `Docs/DevEnv/plans/impl/MASTER_PLAN.md` | Atomic step list with Pending/In Progress/Done status |
| `Docs/DevEnv/plans/impl/S{N}_impl_status.md` | Per-OPP status for each SDD section (S1–S10) |
| `Docs/DevEnv/plans/S{N}_opportunities.md` | Detailed description of each opportunity |

---

## Rules Claude Must Follow (remind if needed)

- One step at a time — never batch multiple steps in one subagent
- Update MASTER_PLAN.md step to `In Progress` before starting
- Update to `Done` + commit after finishing
- Main agent: shell only (`dotnet build`, `git` commands)
- All file edits: delegate to a subagent
- Never rewrite files from scratch — edit only

---

## Current Progress

Check `MASTER_PLAN.md` → Progress Summary table at the bottom.

As of session end (2026-05-06):
- Phase 1 (status log files): **Done**
- Phase 2 through 11: **Pending**
- Next step: **P2-A** — Create `.claude/rules/constraints-registry.md`
