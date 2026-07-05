# Rules File Refactoring — Task Log

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

### RESUME POINTER (cheap restart after weekly-budget reset)
1. **GATE-B** is next: re-run the GATE-A throwaway-subagent probe (0 tools), measure the post-Phase-1 cold-start delta, append to `findings-measurement.md`, Helder confirms go/no-go. (Deferred tonight to conserve near-cap weekly budget — subagent dispatch ≈60k.)
2. If GATE-B = GO: Tasks 06–08 (workflow.md, 3 waves) then 09–10 (testing.md) **using the 3-file correction in `skill-overlap-findings.md`**, then 11 (enable brainstorming + writing-plans + maui-unit-testing) and 12 (CLAUDE.md).
3. Separate tracked item: move `myvocalist-coding` skill user→project (needs a restart-verification window — see BACKLOG 2026-07-05).
4. All rules-file edits still require **Helder authorship review** (CLAUDE.md § Continuous Enhancement — Authorship).


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
