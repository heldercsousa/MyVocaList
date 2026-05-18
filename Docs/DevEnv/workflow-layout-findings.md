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

---

## Finding 6 — Review Is Already Hooked but Conflicts with the Skill

### What the hooks do
The `TaskCompleted` hook (settings.json) already does a review step — but **inline** (same completion agent, not a fresh subagent):
> "SPAWN INLINE REVIEW: Read changed files... verify against ACs... Append ### Review notes..."

The `Stop` hook has an `asyncRewake` that detects tasks marked "To Review" and tells the main agent to spawn fresh review subagents.

### What the skill says
`superpowers:subagent-driven-development` mandates **two fresh subagents** after every task:
1. **Spec compliance reviewer** — verifies code matches spec ACs (nothing missing, nothing extra)
2. **Code quality reviewer** — verifies implementation quality

The skill explicitly says: *"Never skip reviews (spec compliance OR code quality)"* and *"Start code quality review before spec compliance is ✅ — wrong order."*

### The conflict
| | Hook (TaskCompleted) | Skill (subagent-driven-development) |
|--|---------------------|--------------------------------------|
| Who reviews | Same completion agent (inline) | Fresh subagent per review type |
| How many review passes | 1 (inline) | 2 (spec compliance → code quality) |
| When triggered | After every task completion | After every task, before marking done |
| Order of concern | Build → test → review | Spec compliance → code quality |

The hook's inline review is conceptually correct but violates the "fresh subagent" principle — it uses the same agent that processed the completion, which inherits its context bias.

### Resolution options
**Option R1 — Remove inline review from hook, trust the skill:** The `subagent-driven-development` skill handles both review stages. The `TaskCompleted` hook keeps build+test verification only. The Stop hook's asyncRewake for "To Review" items becomes the safety net.

**Option R2 — Upgrade the hook to dispatch a fresh review subagent:** Replace the inline review block in `TaskCompleted` with a `type: agent` dispatch using the spec-reviewer and code-quality-reviewer prompts from the skill.

**Option R3 — Keep hook as lightweight, skill as full review:** Hook does build+test only (infrastructure verification). Skill does spec+quality review (behavioral verification). These are complementary, not redundant.

### Recommendation
**Option R3** — clearest separation of concerns. Hooks verify the build artifact; the skill verifies behavioral correctness. The hook's inline review text should be removed from `TaskCompleted` (it's redundant with the skill and uses wrong agent context).

---

## Finding 7 — workflow.md `/project:review` Duplicates the Skill's Review Loop

### What workflow.md says
Rule 3: *"Run `/project:review` after every completed task."*

### What the skill says
`subagent-driven-development` dispatches spec-reviewer and code-quality-reviewer subagents after every task — this IS the review loop.

### The conflict
`/project:review` is a custom command that presumably runs its own review logic. The skill already defines a complete two-stage review protocol with dedicated subagents. If both are run, review happens twice — with different scope, different agents, different criteria.

### Resolution
After checking what `/project:review` actually does: if it overlaps with the skill's review stages, remove the "run `/project:review` after every task" rule from workflow.md and replace it with "use `superpowers:subagent-driven-development` which includes the two-stage review loop." Keep `/project:review` for manual/spot-check use, not as a mandatory per-task step.

---

## Finding 8 — Custom orchestrator.md/implementor.md Partially Duplicates the Skill

### What workflow.md/orchestrator.md does
Defines elaborate orchestrator protocols: pre-dispatch checklist, pre-wave dependency check, wave-based parallelism, single-writer rule, adversarial critic, kill criteria, etc.

### What the skill does
`subagent-driven-development` defines: implementer → spec-reviewer → code-quality-reviewer per task, with re-review loops. No explicit wave management.

### The overlap
- Both define how to dispatch subagents
- Both define review protocols (skill more rigorously)
- The skill doesn't cover: wave parallelism cap (4 max), single-writer rule, DRY Onion ordering, hotspot file registry — these are project-specific addenda with no skill equivalent

### Resolution
Keep orchestrator.md/implementor.md for **project-specific addenda only**. Add explicit header: "These rules extend `superpowers:subagent-driven-development`. For the base execution loop, use that skill. Rules here apply only where the skill is silent." Delete any section that duplicates what the skill already covers.

---

## Finding 9 — `superpowers:verification-before-completion` Is Underused

### What it does
Mandates: run verification command → read full output → only then claim completion. No "should pass", no "probably works", no trusting agent reports.

### Current state
Workflow.md's subagent exit checklist mentions verification evidence, and the `TaskCompleted` hook runs build+test. But neither explicitly requires this skill to be invoked.

### Resolution
Add to CLAUDE.md skills reference: "Before any task completion claim: invoke `superpowers:verification-before-completion`." This is already in the subagent exit checklist (step 1) but is not connected to the skill by name.

---

---

## Finding 10 — Root Cause of Misplaced Spec Files

The `brainstorming` skill (step 6) explicitly writes design docs to `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`. It says "(User preferences for spec location override this default)" — but no override was ever declared in CLAUDE.md or settings.json. Every time `brainstorming` was used to produce a spec, it wrote to `Docs/superpowers/specs/` correctly per its own rules. The fix is the user-preference override — not a skill bug.

Same situation with `writing-plans`: defaults to `docs/superpowers/plans/` for the plan file, which is fine. But the design artifact that `brainstorming` produces lands in the wrong place unless the override is declared.

**Fix:** CLAUDE.md must declare, before the `### Docs/ Context Scope` section:
- Spec/design docs → `Docs/specs/[feature]/design.md` (not `Docs/superpowers/specs/`)
- Plans → `Docs/superpowers/plans/YYYY-MM-DD-<feature>.md` (already correct)

---

## Finding 11 — Spec and Plan Reviews Are a Gap in the Cycle

### What exists for spec/plan review today

| Stage | What reviews it | Type |
|-------|----------------|------|
| After `brainstorming` writes spec | Spec self-review (same agent, step 7) | Inline, same-context |
| After self-review | User Review Gate — Helder reads and approves | Human |
| After `writing-plans` writes plan | Plan self-review (same agent) | Inline, same-context |
| SDD Planning Gate (S3.1) | Helder reviews 5 areas | Human |

No fresh-subagent review exists for specs or plans. Self-review by the same agent that wrote the artifact has the same bias problem as the hook's inline code review (Finding 6): the agent confirms what it expects, not what is actually correct or complete.

### Should spec/plan reviews follow the same pattern as code reviews?

**Yes — for identical reasons.** The `subagent-driven-development` skill uses fresh subagents for code review precisely because the implementer has context bias. The same bias applies to the agent that wrote the spec.

A fresh spec-reviewer subagent, given only the spec + review criteria, can catch:
- Missing structural elements (SDD S2.1 — 7 elements checklist)
- Vague or untestable acceptance criteria
- Constitutional constraint violations (CLAUDE.md non-negotiables)
- Conflicts with existing specs in `Docs/specs/[feature]/`
- Spec quality four-gate failures (Correctness, Completeness, Consistency, Testability)

A fresh plan-reviewer subagent can catch:
- Tasks with no corresponding spec requirement (over-building)
- Spec requirements with no task (missing coverage)
- Placeholder violations (TBD, TODO, "add validation" without showing how)
- Wrong task ordering (DRY Onion violated)
- File ownership conflicts (same file in two tasks)

### Where these reviews slot in the workflow

```
brainstorming skill
  └─ writes spec (design.md)
  └─ spec self-review (inline — keep, catches trivial issues fast)
  └─ [NEW] fresh spec-reviewer subagent (before human review gate)
  └─ Helder reviews (human gate — now focused on intent, not completeness)

writing-plans skill
  └─ writes plan file
  └─ plan self-review (inline — keep)
  └─ [NEW] fresh plan-reviewer subagent (before Helder approves)
  └─ Helder approves (now focused on approach, not gaps)
```

The fresh reviews are a **pre-filter before Helder's time is spent** — not a replacement for human review. Helder should review intent and approach; the agent reviewer catches structural gaps and mechanical violations.

### Review criteria (per artifact type)

**Spec reviewer checklist:**
- All 7 SDD structural elements present (Inputs, Outputs, Preconditions, Postconditions/Invariants, Integration Contracts, State Machines, Edge Cases)
- Every user story has ≥1 AC in Given/When/Then or EARS/GEARS format
- No vague ACs ("fast", "validates input", "handles errors")
- Out-of-scope section present and non-empty
- Domain vocabulary defined
- No constitutional violations (no DisplayAlert, DevExpress-first, English-only, business logic in Services)
- No conflict with existing specs in `Docs/specs/`

**Plan reviewer checklist:**
- Every spec AC maps to ≥1 task (traceability)
- Every task maps to ≥1 spec AC (no gold-plating)
- No placeholders (TBD, TODO, "add appropriate validation")
- All task steps include actual code, not descriptions of code
- DRY Onion order: Domain → Infra → Services → UI
- No file listed in two parallel tasks (single-writer rule)
- Each task fits sizing limits (≤5 files, ≤2 hours)

### Implementation path
Since no superpowers skill covers spec/plan review, the criteria above should be codified as review prompts (similar to `code-reviewer.md`) in `.claude/agents/`. The orchestrator.md can then reference them: "After brainstorming produces a spec, dispatch spec-reviewer subagent before handing to Helder."

---

## Open Decisions (needs Helder input)

| # | Decision | Options |
|---|----------|---------|
| D1 | Plan + task-log colocation vs separated? | Option A (beside spec) / Option B (Docs/superpowers/plans/) |
| D2 | Spec evolution changelog format? | Inline per file / Separate file / Git commits only |
| D3 | Review: remove inline review from TaskCompleted hook? | Option R1 / R2 / R3 |
| D4 | Apply fixes now? | Move misplaced files, update BACKLOG.md paths, add hierarchy to CLAUDE.md |
| D5 | Remove "run /project:review after every task" rule from workflow.md? | Yes (trust the skill) / No (keep as manual gate) |
| D6 | Slim orchestrator.md — remove sections duplicated by subagent-driven-development? | Yes / Partial |
| D7 | Add fresh-subagent spec review (brainstorming self-review → agent review → Helder gate)? | Yes |
| D8 | Add fresh-subagent plan review (writing-plans self-review → agent review → Helder approval)? | Yes |
| D9 | Codify spec-reviewer and plan-reviewer prompts in `.claude/agents/`? | Yes — mirrors code-reviewer.md pattern |

---

## Next Steps (after decisions)

1. Apply folder layout decision (move files, update BACKLOG.md references)
2. Add authority hierarchy + user-preference override to CLAUDE.md
3. Add changelog section to spec file template (`.claude/library/spec-writing-guide.md`)
4. Remove inline review from `TaskCompleted` hook; keep build+test only (per D3)
5. Slim orchestrator.md to project-specific addenda only (per D6)
6. Add `verification-before-completion` skill reference to CLAUDE.md
7. Create `spec-reviewer.md` and `plan-reviewer.md` in `.claude/agents/` (per D9)
8. Wire spec/plan review into brainstorming and writing-plans workflow notes in CLAUDE.md (per D7/D8)
9. Resume App Versioning implementation (was the original session goal)
