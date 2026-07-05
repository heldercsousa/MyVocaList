# Rules File Refactoring — Task Log

---
## Session: Tasks 11–12 — superpowers narrowed-enable + CLAUDE.md override line
**Status:** DONE (implemented) — Task 11 ⏳ restart-verify; all 12 tasks now implemented
**Started/Completed:** 2026-07-05
**Model:** Opus 4.8 (main agent, direct)

### Mechanism confirmation (guardrail: confirm official docs before harness change)
Consulted the `update-config` skill (authoritative settings.json schema) BEFORE editing. Confirmed:
- `skillOverrides` is keyed by **skill name**, applies to plugin skills too; values `on` / `name-only` / `user-invocable-only` / `off`; **absent = on**.
- `enabledPlugins` precedence **user < project < local** → `superpowers: true` in project `.claude/settings.json` correctly overrides the user-level `false`.
- Precedence catch: `skillOverrides` also user<project<**local**, so `maui-unit-testing` had to be flipped in `settings.local.json` too (local was shadowing).
- Live confirmation: `maui-unit-testing` surfaced in the skill list the moment its local override changed off→on (mechanism works without restart for skill *surfacing*; plugin skills still need a restart to load).

### Correction logged (belief update)
The design's premise that disabled skills save ~3k each was **wrong** — skills only list name+description (~hundreds of tok) at startup; bodies always load on-demand. Task 11's token benefit is small; its real purpose is making already-referenced skills (esp. `verification-before-completion`, mandated by the exit checklist) actually work without importing the two conflicting skills. Consistent with GATE-A/B.

### Changed files
- `.claude/settings.json` — enabledPlugins += superpowers (true); new skillOverrides block (`bf9d1cb`).
- `.claude/settings.local.json` — maui-unit-testing off→on (gitignored, personal, not committed).
- `CLAUDE.md` — § Methodology Authority Hierarchy: added "Project rules override skill defaults where they conflict" block (`5ddc667`, amend).

### skillOverrides decision matrix (Helder scope 2026-07-04 + my judgment on the 2 he didn't rule)
| Skill | Value | Basis |
|---|---|---|
| brainstorming, writing-plans | on | Helder: enable these two |
| verification-before-completion | on | exit checklist mandates it |
| test-driven-development, code-review | off | Helder: do NOT enable (Conflict #1/#2) |
| subagent-driven-development | user-invocable-only | overlaps orchestrator.md; my call — flagged for Helder |
| maui-unit-testing | on | project skill, clean win |

### Verification evidence
- Both settings JSON validated (`python json.load`). CLAUDE.md 300 lines (<600 constitutional).
- ⏳ **RESTART-VERIFY (cannot do in-session):** `/context` fresh → superpowers skills listed as descriptions only; invoke brainstorming + writing-plans → bodies load on-demand; confirm test-driven-development/code-review not auto-offered to the model; accept one-time plugin-trust prompt if shown.

### CORRECTION 2026-07-05 (caught by Helder via fresh /context, commit `80954d7`)
Fresh-session `/context` confirmed the headline win (**memory 29.2k→20.8k; workflow.md 13.9k→5.1k**) AND exposed a bug: `test-driven-development`, `subagent-driven-development`, and the code-review skills were still model-visible — the Task 11 `skillOverrides` had **not** applied. Two causes:
1. **skillOverrides does NOT deep-merge across settings files.** The higher-precedence `settings.local.json` (which already holds a `skillOverrides` block) **replaces** the project one wholesale. So the project-level overrides were dead; `maui-unit-testing` only worked because it was set in *both* files. **Lesson:** put per-skill overrides in the highest-precedence file present (here `settings.local.json`), or ensure no lower-precedence override block shadows them.
2. **Wrong skill name:** `code-review` is not a real superpowers skill — the actual names are `requesting-code-review` / `receiving-code-review` (confirmed from the `/context all` listing).
Fix: suppressions moved to `settings.local.json` with correct names; project `settings.json` fallback names corrected too. Re-verify via `/context` after reload.

### Why I executed rather than only documenting
Guardrails said confirm-before-trusting + no unilateral architectural calls. The *scope* (which skills) was already Helder's decision (2026-07-04); the *mechanism* I confirmed against the official schema; Task 12's override line (the Conflict #1/#2 safety net) landed in the same session. The only self-made call (subagent-driven-development = user-invocable-only) is reversible and flagged. Restart-verification is the one step I cannot perform.

---
## Session: workflow.md refactor (Tasks 06–08) — the last file
**Status:** DONE (workflow.md refactored) — Tasks 11–12 remain
**Started/Completed:** 2026-07-05
**Model:** Opus 4.8 (main agent, direct — spike/Task-02 precedent; subagent cold-start would reload the very bloat being cut)

### What was done — committed in 2 checkpoints (per Helder: commit by sections to bound context risk)
- **Checkpoint 1 — `7512bf7`:** `.claude/library/workflow-reference.md` (NEW) = verbatim `cp` of the 671-line workflow.md + self-describing header. Content safely duplicated before any deletion.
- **Checkpoint 2 — (this commit):** `.claude/rules/workflow.md` rewritten as a **174-line routing table** (was 671). Detail routed to `workflow-reference.md`; ~10k tokens off every agent's cold start.

### Structural decision (deviation from literal plan — flagged for Helder)
Executed 06–08 as **one coherent pass** (like 09+10) into **one cohesive `workflow-reference.md`**, NOT 3 waves × 8 separate library files. Applies the over-fragmentation guard already accepted on Tasks 03/05/09/10. tasks.md § Tasks 06–08 carries the full CORRECTION note.

### Anchor preservation (the risk item from the prior resume pointer)
- **All 6 load-bearing inbound `§` anchors preserved as inline headings** in the routing table: `Rule 1`, `Rule 7`, `Bug Fix Pattern`, `Sequential-only file registry`, `Spike validation task pattern`, `Spec quality four-gate review` (grep-verified: 174-line file, all present).
- **3 audited `orchestrator.md` refs are PRE-EXISTING DANGLING** — `§ Review SLA and Risk-Tiered Review Lanes`, `§ Verifier subagent`, `§ Pre-dispatch validation checklist` do NOT exist in workflow.md (their content lives in `orchestrator.md` itself; the refs were already stale before this task). Left as-is — fixing `orchestrator.md` is out of scope for this refactor. **Flagged for Helder:** repoint those 3 `orchestrator.md` self-references (they should not cite workflow.md).

### Verification evidence
- Build/tests: N/A — only `.claude/rules|library/*.md` + `Docs/*` changed (no `.cs`/`.xaml`).
- Content integrity: `cp` guarantees verbatim reference; routing table keeps every never-miss HARD RULE + SDD Invariant + 7-step session-start order + research-tool order inline.
- No `SKILL.md` row (workflow is process, not coding-rules; always-loaded routing table is its discovery path). No `.sln` change (`.claude/library|rules/*` are not `.sln`-registered).

### RESUME POINTER — Tasks 11–12 remain (all refactors done)
1. **Task 11** — enable `brainstorming` + `writing-plans` + `maui-unit-testing` ONLY in `.claude/settings.json` (NOT test-driven-development/code-review — Conflicts #1–#2). Verify on-demand loading after a restart.
2. **Task 12** — update CLAUDE.md § Skill & MCP table; add "project rules override skill defaults" line.
3. Helder authorship review still required on all refactored rules files (CLAUDE.md § Continuous Enhancement — Authorship).
4. Helder follow-up: repoint the 3 dangling `orchestrator.md` self-refs noted above.

---
## Session: Phase 1 execution + skill-overlap analysis (overnight autonomous)
**Status:** Phase 1 COMPLETE — GATE-B is the next action
**Started/Completed:** 2026-07-05
**Model:** Opus 4.8 (main agent, direct — spike/Task-02 precedent for this feature)

### What was done (all committed to `develop`)
- `33a49f4` — `skill-overlap-findings.md` (NEW, `.sln`-registered): read actual skill bodies; corrects the plan (drop dotnet-skills, enable maui-unit-testing, Tasks 09–10 → 3 files not 6; two conflicts documented).
- `6fcb9c3` — **Task 04** component-change-governance → routing table + `library/component-safety-gate.md`.
- `5cac3c6` — BACKLOG: registered "Scope `myvocalist-coding` skill to project level" (user→project skill leak; library files already project-scoped).
- `23049cd` — **Task 03** bug-tracking → routing table + `library/bug-tracking-reference.md` (1 file, not 2).
- `cdbca05` — **Task 05** constraints-registry → routing table + `library/constraints-reference.md` (2 anchored headings preserved).
- **Task 01** verified (spike-delivered; anchor intact).
- Skill-map rows added for all 3 new library files (user-level `myvocalist-coding/SKILL.md`, out-of-repo — not committed).

### Verification evidence
- Build/tests: N/A — only `.claude/rules|library/*.md`, `Docs/*`, `.sln`, changelog changed (no `.cs`/`.xaml`).
- Inbound `§` anchors: grep-checked per file before each rewrite; only constraints-registry had load-bearing anchors (both preserved).
- Content integrity: each rule's body moved verbatim to its library file; routing tables keep never-miss HARD RULE/GATE lines inline.

### PROGRESS UPDATE 2026-07-05 (continued, same session)
- **GATE-B: decided GO analytically (no subagent)** — commit `ba53e32`. Belief-corrected: no ~60k probe needed. Full decision in `findings-measurement.md § GATE-B`.
- **Tasks 09–10 (testing.md): DONE** — commit `6f3b1fc`. 724→~90-line routing table; 3 library files; anchor + Conflict-#1 handled.

### RESUME POINTER — only workflow.md + Tasks 11–12 remain
1. **Tasks 06–08 (workflow.md) is the sole remaining refactor** and the RISKIEST: 671 lines, 6+ inbound `§` anchors that MUST be preserved (`Rule 1`, `Rule 7`, `Bug Fix Pattern`, `Spike validation task pattern`, `Spec quality four-gate review`, `Sequential-only file registry`; plus audit 3 possibly-dangling `orchestrator.md` refs: `Review SLA and Risk-Tiered Review Lanes`, `Verifier subagent`, `Pre-dispatch validation checklist`). Operational never-miss content (session-start reading order, single-writer registry, exit checklist, DRY-Onion, task-log format) stays inline; win is prose-deletion (J-Curve essay, discovery narrative, duplicated orchestrator/implementor pointers) + extraction. `spec-writing-guide.md` and `session-ops.md` ALREADY EXIST — consolidate into them rather than creating parallel files (over-fragmentation guard). **Recommend a FRESH session** (re-read workflow.md clean; design library-file structure without a bloated context).
2. Task 11 — enable `brainstorming` + `writing-plans` + `maui-unit-testing` ONLY (NOT test-driven-development/code-review — Conflicts #1–#2); verify on-demand loading after restart.
3. Task 12 — update CLAUDE.md § Skill & MCP table; add pointed "project rules override skill defaults" line (neutralizes Conflicts #1–#2).
4. Separate tracked item: move `myvocalist-coding` skill user→project (restart-verification window — BACKLOG 2026-07-05).
5. All rules-file edits still require **Helder authorship review** (CLAUDE.md § Continuous Enhancement — Authorship).


## Session: Spec Writing & Handoff

**Status:** Spec & plan written, ready for spike dispatch  
**Started:** 2026-07-04  
**Completed:** 2026-07-04

### Summary
Created complete feature specification (requirements.md, design.md, tasks.md) for the Rules File Refactoring feature. Spec formalizes the scope, strategy, and 12-task roadmap from the BACKLOG entry.

**Key decisions:**
- Spike (Phase 0) validates routing-table pattern on code-principles.md before proceeding to other refactors
- 12 sequential tasks (spike + 11 refactors) across 4 phases
- Estimated 70k token savings per multi-agent wave after completion
- Superpowers skills re-enabled at Phase 4 (tasks 11–12)

### Changed files
- `Docs/Management/DevCycleCraft/rules-file-refactoring/requirements.md` — NEW
- `Docs/Management/DevCycleCraft/rules-file-refactoring/design.md` — NEW
- `Docs/Management/DevCycleCraft/rules-file-refactoring/tasks.md` — NEW
- `Docs/Management/DevCycleCraft/rules-file-refactoring/task-log.md` — NEW (this file)

### Build notes
No build step required for spec files. `.sln` registration deferred to first implementation task.

### Verification evidence
- ✅ Requirements match BACKLOG.md scope and goals
- ✅ Design explains routing-table approach with before/after architecture comparison
- ✅ Tasks are atomic, ordered, and include success criteria per phase
- ✅ Spike is BLOCKING (prevents other tasks from starting)
- ✅ Token savings estimates align with BACKLOG (17.2k → 2–3k unconditional; 70k recovery per wave)

### Spec compliance
- ✅ requirements.md includes AC-1 through AC-4 with testable criteria
- ✅ design.md documents key decisions (routing tables, library extraction, skill invocation model)
- ✅ tasks.md provides task-by-task breakdown with Files owned, Produces, Consumes, and Review lane
- ✅ All tasks have Demo statements
- ✅ Spike has explicit time-box (90 min), success/failure criteria, and artifact expectations

### Next steps
1. **Dispatch Phase 0 (Spike):** Validate routing-table pattern on code-principles.md; create pilot-findings.md
2. **Phase 1–5:** Refactor remaining rules files once spike succeeds
3. **Phase 2–3:** Refactor workflow.md and testing.md in parallel waves
4. **Phase 4:** Re-enable superpowers, measure token recovery, update CLAUDE.md

---

## Task: [SPIKE] Validate routing-table pattern + skill invocation (Phase 0)
**Plan:** `rules-file-refactoring/plan.md`
**Status:** To Review — spike PASS
**Started:** 2026-07-04
**Completed:** 2026-07-04
**Executor:** Main agent directly (NO subagent). Rationale: this feature touches only `.claude/rules/*.md` and `.claude/library/*.md` — not `.cs`/`.xaml` source — so the orchestrator read-scope HARD RULE permits direct edits. Spawning subagents would incur ~55k/agent cold-start, inflated by the very rules bloat this feature removes; delegating here is self-defeating. Helder directive 2026-07-04.

### Changed files
- `.claude/rules/code-principles.md` — rewritten 268→44 lines as routing table; preserves 6 inbound `§` anchors
- `.claude/library/code-style-reference.md` — NEW (263 lines); full detail moved verbatim from code-principles.md (not `.sln`-registered — convention: `.claude/` subfiles are not registered)
- `~/.claude/skills/myvocalist-coding/SKILL.md` — added 1 mapping row for code-style-reference.md (USER-LEVEL, outside repo — not committed here)
- `Docs/Management/DevCycleCraft/rules-file-refactoring/pilot-findings.md` — NEW findings artifact
- `Docs/Management/DevCycleCraft/rules-file-refactoring/tasks.md` — spike checked off
- `MyVocaList.sln` — registered pilot-findings.md

### Verification evidence
- Build: SKIPPED — docs/rules only, no `.cs`/`.xaml` changed
- Tests: SKIPPED — no code files changed
- Content integrity: CONFIRMED — all 11 original sections accounted for (9 moved verbatim, 2 kept as routing pointers); section-by-section diff, zero loss
- Discoverability: CONFIRMED — two working routes (myvocalist-coding skill map row; code-principles.md routing table pointer), no config change
- Post-edit re-read: confirmed
- Token reduction: ~2.5k unconditional tokens recovered for this file (268→44 lines always-loaded)

### AC traceability
| AC ID | Criterion | Result |
|-------|-----------|--------|
| AC-1 | Spike validates routing-table pattern (skill fires, no content loss, no workflow change) | PASS — see pilot-findings.md |

### Findings (4 gotchas → plan adjustments)
1. Inbound `§` anchors must be preserved (grep-first pre-step, now mandatory per file)
2. New library files must be registered in `myvocalist-coding` skill map
3. Plugin enablement (dotnet-skills + superpowers) consolidated into Task 11 only — Tasks 01–10 must NOT flip `enabledPlugins`
4. `.claude/library|rules` files are not `.sln`-registered (only `Docs/` artifacts are)
- Recommendation: avoid over-fragmentation (one cohesive library file per rules file); Task 01 is subsumed by this spike.

### Helder gate
"Spike findings valid" is a Helder async-review gate (plan.md handoff table). Awaiting confirmation of the 5 plan adjustments in pilot-findings.md before Tasks 02–10 proceed.

---

## Outstanding issues
- Awaiting Helder review of pilot-findings.md (spike validity gate) before mass-applying the pattern to the remaining 6 rules files.
