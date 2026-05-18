# Workflow & Folder Layout Alignment — Findings

**Created:** 2026-05-18
**Status:** Open — decisions pending from Helder
**BACKLOG entry:** Dev Cycle Craft — Workflow & Folder Layout Alignment

---

## Context

This document records the analysis from a conversation on 2026-05-18 where conflicts between three coexisting systems were identified:

1. **SDD principles** — `Docs/DevEnv/SDD/` (preferred methodology)
2. **Superpowers skills** — mature tooling (brainstorming, writing-plans, subagent-driven-development, etc.)
3. **Custom CLAUDE.md / workflow.md rules** — project-specific configuration

The current state produced inconsistent folder layouts and unaddressed workflow gaps.

---

## Finding 1 — Docs/ Folder Layout is Inconsistent

### What happened
The `writing-plans` superpowers skill defaults to placing design artifacts in `docs/superpowers/specs/`. Because CLAUDE.md never declared a user-preference override, files landed in the wrong place:

| File | Where it landed | Where it should be |
|------|----------------|--------------------|
| `2026-05-18-app-versioning-design.md` | `Docs/superpowers/specs/` | `Docs/specs/app-versioning/design.md` |
| `2026-03-30-autocomplete-field-design.md` | `Docs/superpowers/specs/` | `Docs/specs/persons/` (autocomplete was part of Person CRUD) |
| `2026-03-30-styles-structure-design.md` | `Docs/superpowers/specs/` | `Docs/specs/styles-structure/design.md` |
| `2026-03-11-m3-lists-design.md` | `Docs/superpowers/specs/` | `Docs/specs/m3-lists/design.md` |

`Docs/Plans/` and `Docs/Design/` are also legacy folders — nothing new should go there.

### Decision needed
**Open question from Helder (2026-05-18):** Should plans and task-logs live *beside* the spec files (inside `Docs/specs/[feature]/`) instead of in `Docs/superpowers/plans/`?

**Arguments for colocation (plan + task-log beside spec):**
- A single folder per feature contains everything: spec + execution plan + activity log
- Easier to navigate — no cross-folder lookup when reviewing a feature's history
- Natural for spec evolution: when the spec changes after coding, the related plan and log are adjacent
- Removes the `Docs/superpowers/plans/` folder as a separate concern

**Arguments for keeping plans in `Docs/superpowers/plans/`:**
- Superpowers skill generates plans there by default; overriding requires explicit user preference
- Separates spec (what) from execution script (how) — two different audiences
- BACKLOG.md already references plans by path in `Docs/superpowers/plans/` consistently
- The plans folder accumulates all historical plans in one place for a timeline view

**Proposed canonical layout (Option A — colocation):**
```
Docs/specs/[feature]/
  requirements.md
  design.md
  tasks.md
  findings.md        (spikes)
  plan.md            (execution plan — renamed from YYYY-MM-DD-<feature>.md)
  task-log.md        (activity log)
```

**Proposed canonical layout (Option B — keep separated):**
```
Docs/specs/[feature]/
  requirements.md
  design.md
  tasks.md
  findings.md

Docs/superpowers/plans/
  YYYY-MM-DD-<feature>.md     (execution plan)
  <feature>-task-log.md
  <feature>-handoff.md
```

### Recommendation
Option A (colocation) is simpler for a solo developer. Everything for a feature is in one place. The `writing-plans` skill supports user-preference overrides, so we tell it to write to `Docs/specs/[feature]/plan.md`. BACKLOG.md references would need to be updated to point to the new paths.

---

## Finding 2 — Priority Hierarchy Not Declared in CLAUDE.md

### What happened
CLAUDE.md lists skills and rules but never declares which takes precedence when they conflict. Agents must guess — and guessed wrong (produced `Docs/superpowers/specs/`).

### Fix needed
Add to CLAUDE.md (before `### Docs/ Context Scope`):

```markdown
## Authority Hierarchy

Priority order when rules conflict:
1. **SDD principles** (`Docs/DevEnv/SDD/`) — the methodology
2. **Superpowers skills** — authoritative for process execution (brainstorming, planning, subagent-driven-development, verification)
3. **Custom workflow/rules files** — project-specific addenda only (hotspot files, DRY Onion order, stack-specific patterns)

User-preference overrides apply to superpowers skill *defaults* (e.g. folder locations) — not to skill *disciplines* (e.g. TDD red/green/refactor).
```

---

## Finding 3 — Spec Evolution After Implementation

### What happened
Helder identified that specs are sometimes not updated when bugs are found during testing, or the changes are applied directly to the original spec without any record that a revision occurred.

### Current state
`workflow.md` has a "Session-End Spec Update Ritual" (Rule 3) and a "Living Spec Protocol" in `implementor.md`, but these are self-enforced and frequently bypassed.

### What SDD says (S3.1 — Planning Artifacts as Living Documents)
> "Requirements change when business priorities shift. Update requirements.md, re-review, and update the affected tasks. Design changes when implementation discovers a better approach. Update design.md, re-review the impact, and update tasks."
> "The spec changes before the code changes."

### Gap
There is no versioning or change-log convention inside spec files. When Helder finds a bug during testing and updates the spec, there is no record that the spec was revised, what changed, or why.

### Options
**Option A — Inline changelog in each spec file:**
```markdown
## Changelog
| Date | Change | Reason |
|------|--------|--------|
| 2026-05-18 | Added AC for empty queue edge case | Found during testing: app crashes on empty queue |
```

**Option B — Separate `spec-changelog.md` per feature:**
```
Docs/specs/[feature]/spec-changelog.md
```

**Option C — Git commit history as the log** (rely on descriptive commit messages with `amend:` prefix):
```
amend: specs/artists-songs — add empty queue edge case AC (found in testing 2026-05-18)
```

### Recommendation
Option A (inline changelog per spec file) is the lightest touch. Add a `## Changelog` section to each spec file template. The `amend:` commit prefix (already in CLAUDE.md) provides the git-level record; the inline changelog provides the human-readable summary without requiring a separate file.

---

## Finding 4 — Review Step Frequently Bypassed

### What happened
Helder noted that the `/project:review` step is often skipped. This is a self-enforced rule with no hook enforcement.

### Current state
`workflow.md` says: "Run `/project:review` after every completed task and after creating or updating any spec or plan file." But there is no hook that blocks progress if review is skipped.

### Why it's bypassed
- The review command adds friction at the end of every task
- No hook enforces it — it's advisory
- Subagents are briefed to run it but sometimes skip it under time pressure

### Options
**Option A — Hook enforcement:** Add a `PostToolUse` hook on `tasks.md` checkbox edits that warns if `/project:review` hasn't run since the last commit.

**Option B — Bake review into the plan format:** The superpowers `writing-plans` skill includes a "Self-Review" step after writing the plan. Similarly, each task in the plan can include a "Review" step as the final checkbox before the commit step. This makes review an explicit task step, not a meta-step.

**Option C — Reduce review scope:** Instead of reviewing after every task, review after every *phase* (Domain → Infra → Services → UI). Fewer review gates = higher compliance.

### Recommendation
Option B is the most durable: bake it into the plan as an explicit step. When the plan says "Step 5: Run `/project:review`", agents treat it as a task step, not an afterthought. This leverages the superpowers `writing-plans` skill's existing format rather than adding new enforcement overhead.

---

## Finding 5 — `Docs/superpowers/specs/` Should Not Exist

### Action items (pending decision on Finding 1)
If **Option A** (colocation): move all 4 misplaced files to their correct `Docs/specs/[feature]/` locations and delete `Docs/superpowers/specs/`.
If **Option B** (keep separated): move the 4 files to `Docs/superpowers/specs/` → `Docs/specs/[feature]/design.md` (correct subfolder, not the legacy superpowers location) and delete `Docs/superpowers/specs/`.
Either way, `Docs/superpowers/specs/` gets deleted.

---

## Open Decisions (needs Helder input)

| # | Decision | Options |
|---|----------|---------|
| D1 | Plan + task-log colocation vs separated? | Option A (beside spec) / Option B (Docs/superpowers/plans/) |
| D2 | Spec evolution changelog format? | Inline per file / Separate file / Git commits only |
| D3 | Review step enforcement? | Hook / Bake into plan steps / Phase-level only |
| D4 | Apply fixes now? | Move misplaced files, update BACKLOG.md paths, add hierarchy to CLAUDE.md |

---

## Next Steps (after decisions)

1. Apply folder layout decision (move files, update BACKLOG.md references)
2. Add authority hierarchy paragraph to CLAUDE.md
3. Add changelog section to spec file template (`.claude/library/spec-writing-guide.md`)
4. Update `writing-plans` user preference for spec location
5. Resume App Versioning implementation (was the original session goal)
