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

### RESOLUTION 2026-07-05 (plugin-skill suppression decision made)
**Decision:** Accept the full `superpowers@claude-plugins-official` plugin. Removed ineffective bare-name skillOverrides suppressions from `settings.json` (they don't apply to plugin-provided skills).

**Why:** 
1. `skillOverrides` by bare skill name does NOT suppress plugin-provided skills (proven across 3 fresh `/context` loads) — no documented namespaced key syntax exists.
2. Token cost of all superpowers skills (~250 tok) is negligible vs the ~8k saved by refactoring.
3. Behavioral conflicts (TDD Iron Law, brainstorming design-gate) are already neutralized by Task 12's CLAUDE.md § Methodology Authority Hierarchy line, which explicitly states "project rules override skill defaults where they conflict".
4. Cleaner than guessing a fourth time on the syntax.

**Action taken:** 
- Line 155 of `.claude/settings.json` confirms `superpowers@claude-plugins-official: true` already enabled from Task 11.
- Removed lines 158-161 (`test-driven-development`, `requesting-code-review`, `receiving-code-review`, `subagent-driven-development` bare-name overrides) — they were harmless no-ops.
- Kept `maui-unit-testing: on` (project skill, works correctly).

**Result:** Full plugin loaded, behavioral governance intact via CLAUDE.md authority. Restart recommended but not blocking.

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


## Moved from BACKLOG.md (2026-07-15) — Rules File Refactoring — Reduce Unconditional Load

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-04 | **Rules File Refactoring — Reduce Unconditional Load** | ✅ Done | **Root problem:** `.claude/rules/*.md` files load unconditionally (17.2k tokens), duplicating procedures already in superpowers skills (brainstorming, writing-plans, test-driven-development, code-review) — which are disabled to save tokens. **Solution:** Condense rules to routing tables + skill pointers; re-enable superpowers. **Result:** 17.2k → ~2–3k unconditional + ~13–14k on-demand via skills. **Per-subagent impact:** 5 agents × 14k recovery = 70k tokens saved per multi-agent wave. **Strategy (REPRIORITIZED 2026-07-04, post-spike economics review; measure-first correction same day):** **GATE-A (subagent inheritance) done first — PASS**, proving rules reload per-subagent (60,492-token cold-start measured) → situational files first (mediatr→delete, component-governance, bug-tracking, constraints) → **GATE-B** (post-Phase-1 re-measure; workflow/testing splits only if the full bodies are unneeded by most implementors) → re-enable superpowers **narrowed to brainstorming + writing-plans** (TDD/code-review dropped — duplicate the project's customized rules for ≈0 token gain + authority ambiguity). **Key correction:** unconditional→on-demand only saves if the condition is usually false; core files (workflow/testing) are needed by nearly every agent, so their skill bodies reload per-agent anyway (~net-zero, higher risk). The bigger lever for core files is role-scoped loading (see *Per-Agent MCP/Skill Context Isolation* row). **Spec/Design/Plan:** `Docs/Management/DevCycleCraft/rules-file-refactoring/` (tasks.md reordered + measurement gate added 2026-07-04). **STATUS 2026-07-05: all 12 tasks implemented** (rules files 01–10 → routing tables; workflow.md 671→174; testing.md 724→~90; superpowers narrowed-enable + CLAUDE.md override line). **VERIFIED WIN (fresh /context ×3):** memory 29.2k→20.8k; workflow.md 13.9k→5.1k — sticky per agent. superpowers plugin enabled; brainstorming/writing-plans/verification-before-completion/maui-unit-testing live. **UPDATE 2026-07-07 (autonomous session): Tasks 13–18 ALL DONE** (13/14 earlier same day; 15 agent-brief frontmatter+preload ⏳ restart-verify; 16 CLAUDE.md 298→178 lines `amend:`; 17 record-correction; 18 library hygiene). Former open items resolved: plugin-skill suppression CLOSED 2026-07-05 by accepting the full superpowers plugin (~250 tok; conflicts neutralized by Task 12 authority line — `task-log.md § RESOLUTION 2026-07-05`); 3 dangling orchestrator.md self-refs repointed (with Task 15). **CLOSED 2026-07-09:** (1) authorship review of all refactored rules files + CLAUDE.md amends complete (Helder approved 2026-07-09; bug-tracking pair + workflow.md provisional pending Spec Evolution feature); (2) Task 15 restart-verify PASSED — all 5 agent types registered. |


## Moved from BACKLOG.md (2026-07-15) — [SPIKE] Validate routing-table pattern + skill invocation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-04 | ↳ [SPIKE] Validate routing-table pattern + skill invocation | ✅ Done | **PASS 2026-07-04.** `code-principles.md` 268→44 lines; full detail → `.claude/library/code-style-reference.md` (verbatim, zero loss); `myvocalist-coding` skill map row added. 4 gotchas found (preserve inbound `§` anchors; register library file in skill map; defer all plugin enablement to Task 11; `.claude/` subfiles not `.sln`-registered). Done directly by main agent (no subagent) per Helder — avoids ~55k/agent cold-start. Findings: `rules-file-refactoring/pilot-findings.md`. ⏳ Helder: async-review findings before Tasks 02–10. |


## Moved from BACKLOG.md (2026-07-15) — 01 - Finalize `code-principles.md` (subsumed by spike)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 01 - Finalize `code-principles.md` (subsumed by spike) | ✅ Done | Verified 2026-07-05 — spike delivered the 44-line routing table + `code-style-reference.md` verbatim; inbound `§` anchor to `constraints-registry.md § EF Core / SQLite` confirmed intact. No `ddi-registration-conventions.md` (over-fragmentation guard). ~2.5k already realized. |


## Moved from BACKLOG.md (2026-07-15) — 02 - DELETE `mediatr-patterns.md` (do first)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 02 - **DELETE** `mediatr-patterns.md` (do first) | ✅ Done | **DONE 2026-07-04.** Deleted the rule; content moved verbatim to `.claude/library/mediatr-reference.md`; sole live inbound pointer (`CLAUDE.md § Rules Files`) repointed + tagged "reference only — not loaded unconditionally"; skill-map row added; historical Docs mentions left as-is (record past state, not live routing). ~1.1k unconditional tokens recovered per agent. |


## Moved from BACKLOG.md (2026-07-15) — 03 - Refactor `bug-tracking.md`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 03 - Refactor `bug-tracking.md` | ✅ Done | 2026-07-05 (`23049cd`). Routing table + ONE cohesive `library/bug-tracking-reference.md` (not two — over-fragmentation guard). HARD RULE regression table inline. ~1.7k unconditional recovered. |


## Moved from BACKLOG.md (2026-07-15) — 04 - Refactor `component-change-governance.md`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 04 - Refactor `component-change-governance.md` | ✅ Done | 2026-07-05 (`6fcb9c3`). Routing table + `library/component-safety-gate.md`. Four-gate HARD RULE + no-bundling inline. ~1.4k recovered. |


## Moved from BACKLOG.md (2026-07-15) — 05 - Refactor `constraints-registry.md`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 05 - Refactor `constraints-registry.md` | ✅ Done | 2026-07-05 (`cdbca05`). Routing table + cohesive `library/constraints-reference.md`; both anchored headings (`EF Core / SQLite`, `Visual Studio Solution (.sln)`) preserved. ~3.2k recovered. **Phase 1 complete → GATE-B next.** |


## Moved from BACKLOG.md (2026-07-15) — GATE-A — subagent inheritance (was mid-plan; moved ahead of Phase 1)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-04 | ↳ **GATE-A** — subagent inheritance (was mid-plan; moved ahead of Phase 1) | ✅ Done | **PASS 2026-07-04.** Throwaway subagent (0 tools) proved all 7 rules files inherit in full into a child context; `code-principles.md` arrived in its reduced 44-line form (pattern shrinks per-subagent load). **Measured baseline: 60,492-token cold-start, 0 tools** — supersedes the disagreeing 17.2k/28.4k/33.3k estimates. Rules portion ~18–22k → recovers ~16–19k per subagent, sticky. Artifact: `rules-file-refactoring/findings-measurement.md`. |


## Moved from BACKLOG.md (2026-07-15) — GATE-B — split economics (blocking for 06–10)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ **GATE-B** — split economics (blocking for 06–10) | ✅ Done | **Decided GO 2026-07-05 (`ba53e32`), analytically (no ~60k probe).** GATE-A already proved inheritance; Q1 deterministic from `wc -l`; Q2 answered by `skill-overlap-findings.md`. Direction: split unconditional→routing (delete/extract, NOT skill-substitute). Full decision: `findings-measurement.md § GATE-B`. |


## Moved from BACKLOG.md (2026-07-15) — 06 - Refactor `workflow.md` Phase 1 (Rules 1–2)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 06 - Refactor `workflow.md` Phase 1 (Rules 1–2) | ✅ Done | 2026-07-05 (`7512bf7` + `3e21ea6`). Done with 07+08 as ONE coherent pass into ONE cohesive `library/workflow-reference.md` (over-fragmentation guard; not the 8 separate files first drafted). |


## Moved from BACKLOG.md (2026-07-15) — 07 - Refactor `workflow.md` Phase 2 (Rules 3–5)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 07 - Refactor `workflow.md` Phase 2 (Rules 3–5) | ✅ Done | 2026-07-05 (folded into the 06–08 single pass). |


## Moved from BACKLOG.md (2026-07-15) — 08 - Refactor `workflow.md` Phase 3 (Rules 6–8)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 08 - Refactor `workflow.md` Phase 3 (Rules 6–8) | ✅ Done | 2026-07-05. **workflow.md 671→174 lines (~10k off every agent cold start).** All 6 inbound `§` anchors preserved inline (Rule 1, Rule 7, Bug Fix Pattern, Sequential-only file registry, Spike validation task pattern, Spec quality four-gate review) + all never-miss HARD RULEs + SDD Invariant + 7-step session-start order. Rules 6–7 kept largely inline (per-session never-miss). **Flagged for Helder:** 3 pre-existing dangling `orchestrator.md` self-refs (`§ Review SLA…`, `§ Verifier subagent`, `§ Pre-dispatch validation checklist`) that cite workflow.md but live in orchestrator.md — repoint them. |


## Moved from BACKLOG.md (2026-07-15) — 09–10 - Refactor `testing.md`

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 09–10 - Refactor `testing.md` | ✅ Done | 2026-07-05 (`6f3b1fc`). 724→~90-line routing table; bulk → **3** cohesive library files (testing-reference + stryker + fscheck, NOT 6). Never-miss core inline (AC traceability, TDD Level A/B/C + Conflict-#1 authority note, Regression-tests anchor, Builder-must-not-modify). `maui-unit-testing` forward-ref; TDD/code-review skills stay disabled. ~11.6k unconditional recovered. **GATE-B GO applied.** |


## Moved from BACKLOG.md (2026-07-15) — 11 - Re-enable superpowers (NARROWED) + verify on-demand loading

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 11 - Re-enable superpowers (NARROWED) + verify on-demand loading | ✅ Done ⏳ restart-verify | 2026-07-05 (`bf9d1cb`). `superpowers@claude-plugins-official: true` at PROJECT level (overrides user false per precedence). `skillOverrides`: test-driven-development=off, code-review=off, subagent-driven-development=user-invocable-only, maui-unit-testing=on (also settings.local). brainstorming/writing-plans/verification-before-completion=on. **Mechanism confirmed vs official schema (`update-config` skill).** **Correction:** disabled skills only saved name+description (~hundreds tok), NOT ~3k each — bodies always load on-demand; real win is Tasks 01–10. **Restart-verify pending:** `/context` fresh + invoke brainstorming/writing-plans; possible one-time plugin-trust prompt. |


## Moved from BACKLOG.md (2026-07-15) — 12 - Update CLAUDE.md § Skill & MCP Lookup table

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-05 | ↳ 12 - Update CLAUDE.md § Skill & MCP Lookup table | ✅ Done | 2026-07-05 (`5ddc667`, amend). Added pointed "Project rules override skill defaults where they conflict" block to § Methodology Authority Hierarchy (names brainstorming HARD-GATE + TDD Iron-Law conflicts; cross-refs skillOverrides + skill-overlap-findings). CLAUDE.md 300 lines (<600). Aspirational <200 restructure deferred (broad blast radius). |


## Moved from BACKLOG.md (2026-07-15) — AUDIT — post-implementation context & skill-config audit

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ **AUDIT** — post-implementation context & skill-config audit | ✅ Done | Fresh-session `/context` (41.7k total; memory 20.8k; rules 11.0k) vs GATE-A 60,492 baseline. **Measured win real (~8–11k rules / ~20k cold start) but ~half of claimed:** 2–3k routing-table target missed ~4× (never-miss inline kept); per-task BACKLOG "recovered" figures (~28k summed) don't reconcile with measurement; tasks.md success criteria #1/#4 checked ✅ but not true. 9 new findings (F1–F9) incl. git-tracked secrets in `.mcp.json`, broken `tooling-evaluations` skill, CLAUDE.md routing to disabled skills, unregistered agent briefs. Full report + consolidated recommendations R1–R8: `rules-file-refactoring/context-audit-2026-07-07.md`. Docs/Management confirmed NOT auto-loaded (see `sprightly-launching-corbato.md`); rejected again: `paths:` scoping, bypass-folder, further micro-splits. |


## Moved from BACKLOG.md (2026-07-15) — 13 - Fix `tooling-evaluations` skill registration (audit F2/R2)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 13 - Fix `tooling-evaluations` skill registration (audit F2/R2) | ✅ Done | **Resolved by archive, not fix (Helder 2026-07-07):** content moved to `Docs/Design/tooling-evaluations.md` (glob-ignored; explicit-path read); CLAUDE.md § Tooling Evaluation repointed; Design solution folder (GUID 0042) registered in `.sln`. |


## Moved from BACKLOG.md (2026-07-15) — 14 - Reconcile CLAUDE.md skill routing vs enablement (audit F3/R3)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 14 - Reconcile CLAUDE.md skill routing vs enablement (audit F3/R3) | ✅ Done | 2026-07-07 (amend). § MCP & Skills / § Skill & MCP Lookup / § Methodology Layering now route to enabled tooling only. **ddd-dotnet dropped per Claude evaluation, Helder concurring:** project is MVVM + anemic domain + Services — tactical DDD (rich aggregates) directly conflicts with the unamendable "business logic in Services" constraint; DDD stays conceptual at spec time. `maui-current-apis` re-enabled (registered live, no restart needed). dotnet-skills + superpowers:TDD rows removed. Changelog entry added. ⏳ Helder authorship review. |


## Moved from BACKLOG.md (2026-07-15) — 15 - Register agent briefs + preload `myvocalist-coding` (audit F8/R4)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 15 - Register agent briefs + preload `myvocalist-coding` (audit F8/R4) | ✅ Done ⏳ restart-verify | 2026-07-07. YAML frontmatter added to all 5 `.claude/agents/*.md`: `name` + `description` on all; `tools: Read, Grep, Glob` on spec/plan-reviewer (+Bash on verifier — report-only roles get read-only toolsets); orchestrator/implementor inherit full toolset (their constraints are prose HARD RULEs); `skills: [myvocalist-coding]` preloaded on all 5 (~400-tok routing table only, per "preload nothing bigger"). **Also fixed the 3 dangling `orchestrator.md` self-refs** (row 195 item 3): `§ Review SLA…` and `§ Pre-dispatch validation checklist` repointed to their in-file sections; `§ Verifier subagent` repointed to `.claude/agents/verifier.md`. ⏳ **Restart-verify (Helder or next fresh session):** confirm the 5 appear as agent types in the Agent tool list and that `myvocalist-coding` body is present in a dispatched agent's context. **RESTART-VERIFY 2026-07-08: FAILED, root-caused, fixed** — only 2 of 5 registered (spec-reviewer, verifier); orchestrator/implementor/plan-reviewer had a UTF-8 BOM before the `---` frontmatter fence, which broke YAML parsing. BOM stripped from all three. **RESTART-VERIFY 2026-07-09: PASSED** — all 5 agent types (orchestrator, implementor, spec-reviewer, plan-reviewer, verifier) registered in the Agent tool list with correct toolsets (reviewers Read/Grep/Glob, verifier +Bash). |


## Moved from BACKLOG.md (2026-07-15) — 16 - CLAUDE.md <200-line restructure (un-deferred from Task 12; audit R5)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 16 - CLAUDE.md <200-line restructure (un-deferred from Task 12; audit R5) | ✅ Done | 2026-07-07 (`amend:`), executed with `claude-md-improver` skill support per Helder instruction. **CLAUDE.md 298 → 178 lines** (~9.3k → est. ~5.5–6k tok, sticks per subagent). Moved verbatim to TWO new library files (over-fragmentation guard): `.claude/library/mcp-governance.md` (MCP context budget, Security Stance, response token discipline, emerging patterns, Playwright, SQLite operational detail) + `.claude/library/project-governance-reference.md` (SDD-applicability essay, Continuous Enhancement full procedure + Quarterly Audit, Methodology Layering rationale, scope-of-inspection full rule, Docs/ layout examples + glob-exclusion list, Tool Selection). Kept inline: all Constitutional Constraints, authority hierarchies, Context7 version-pinning trigger, MCP Availability Gate (condensed), Authorship rule, Amending process, Docs routing rule + user-preference overrides, roles + orchestrator HARD RULE. Both files registered in `myvocalist-coding` skill map. ⏳ Helder authorship review (with `f18df7e` batch). |


## Moved from BACKLOG.md (2026-07-15) — 17 - Record-correction pass (audit R6)

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 17 - Record-correction pass (audit R6) | ✅ Done | 2026-07-07. `tasks.md` success criteria #1 (⚠️ partially met — workflow/testing kept never-miss inline, exceed 1–2 pages by design) and #4 (❌ incoherent as written; replaced with measured ~8–11k/agent) corrected; `findings-measurement.md` § Post-implementation reconciliation appended (measured 11.0k rules total vs ~18–22k GATE-A; per-task claims were estimates at ~17–20 tok/line vs actual ~29). **NOTE for readers of rows 196–207:** the per-task "recovered" figures there are pre-measurement estimates — measured total is ~8–11k, see `context-audit-2026-07-07.md § Part 1`. GATE-A-style post-probe deliberately SKIPPED (token thrift; number derivable from `/context` + `wc -l`). |


## Moved from BACKLOG.md (2026-07-15) — 18 - Library hygiene: trim `testing-reference.md` overlap + delete `mediatr-reference.md` (audit F9/…

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-07 | ↳ 18 - Library hygiene: trim `testing-reference.md` overlap + delete `mediatr-reference.md` (audit F9/R8) | ✅ Done | 2026-07-07. `testing-reference.md`: generic csproj skeleton/OutputType trick, generic ViewModel scaffolding, and run-command variants replaced with pointers to the enabled `maui-unit-testing` skill; kept everything project-specific (net10.0-only TFM + EF Sqlite package + project refs, empty-state/selection derived-state test targets, paged-tuple mock shape, real-SQLite rationale, `TestDbContextFactory`, Service tuple-return tests, Tester/Builder split, quality audit, anti-patterns). `mediatr-reference.md` deleted (`git rm`); both live pointers updated — skill-map row removed, `CLAUDE.md § Rules Files` line now says "derive via Context7 when MediatR is introduced". Historical mentions left as-is. |
