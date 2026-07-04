# Rules File Refactoring — Execution Plan

## Overview

Incremental token-efficiency initiative reducing unconditional rules load from 17.2k to ~2–3k tokens, with 70k+ recovery per multi-agent wave via re-enabled superpowers skills.

**Duration:** ~3–4 weeks (12 sequential + concurrent tasks across 4 phases)  
**Resource:** 1 main agent (orchestrator) + 2–3 concurrent subagents per wave  
**Blocking dependencies:** Spike (Phase 0) must complete before any other refactors begin  
**Success metric:** `/context fresh` shows 14k on-demand tokens per skill; total rules load <20k  

---

## Phase Breakdown

### Phase 0: Validation (1 task, ~90 min) — BLOCKING

**Objective:** Prove routing-table pattern + library extraction on smallest file (code-principles.md)

**Task:** [SPIKE] Validate routing-table pattern + skill invocation
- **Dispatch:** Solo subagent, no parallelism needed
- **Time estimate:** 90 min (hard stop)
- **Success gates:** (1) pilot-findings.md completed, (2) no workflow changes required, (3) skill invocation confirmed
- **Blocker release:** Once spike succeeds with findings, Phase 1–5 tasks are unblocked

**Failure mode:** If spike finds routing-table pattern is impractical (agents cannot discover library files, skill invocation needs config), escalate to Helder before proceeding

---

### Phase 1–5: Small Rules Files (5 tasks, 1–2 weeks) — Sequential

**Objective:** Refactor smaller rules files (1–2k tokens each) following spike pattern

**Dispatch strategy:**
- One task per session / subagent wave
- Sequential (not parallel) to apply learnings incrementally
- Each task: refactor rules file → extract to library → verify skill invocation

**Tasks:**
1. **Task 01:** code-principles.md (uses spike pattern, dependencies on spike complete)
2. **Task 02:** mediatr-patterns.md (depends on Task 01)
3. **Task 03:** bug-tracking.md (depends on Task 02)
4. **Task 04:** component-change-governance.md (depends on Task 03)
5. **Task 05:** constraints-registry.md (depends on Task 04)

**Checkpoint after Phase 1–5:** Confirm all small files are 1–2 pages; library files are well-indexed

---

### Phase 2–3: Large Rules Files (5 tasks, 2–3 weeks) — Sequential Waves

**Objective:** Refactor largest files (workflow.md, testing.md) by breaking into independent phases

**Dispatch strategy:**
- workflow.md split into 3 waves (Rules 1–2, 3–5, 6–8)
- testing.md split into 2 waves (TDD section, test types section)
- Sequential (not parallel) — each wave must complete before next begins

**Tasks:**
6. **Task 06:** workflow.md Phase 1 (Rules 1–2) → spec-writing-guide, subagent-patterns
7. **Task 07:** workflow.md Phase 2 (Rules 3–5) → commit-ceremony, task-atomization, task-log-format
8. **Task 08:** workflow.md Phase 3 (Rules 6–8) → research-tool-selection, session-start-protocol, github-collision-protocol
9. **Task 09:** testing.md Phase 1 (TDD + AC) → test-driven-development-levels, acceptance-criteria-format
10. **Task 10:** testing.md Phase 2 (Test types) → unit/integration/E2E patterns, test naming, anti-patterns

**Checkpoint after Phase 2–3:** Confirm routing tables are 1–2 pages; all library files created and indexed

---

### Phase 4: Skill Re-Enablement & Measurement (2 tasks, 2–3 days) — Sequential

**Objective:** Re-enable superpowers skills, verify on-demand loading, measure token savings

**Tasks:**
11. **Task 11:** Re-enable superpowers + verify on-demand loading
    - Enable brainstorming, writing-plans, test-driven-development, code-review in settings.json
    - Run `/context fresh`, confirm skills load descriptions (~100 tok) but bodies load on-demand
    - Create verify-skill-loading.py script for sanity checks
12. **Task 12:** Update CLAUDE.md § Skill & MCP Lookup table
    - Add enabled superpowers rows
    - Remove redundant narrative paragraphs
    - Target <200 lines total CLAUDE.md
    - Final measurement: `/context` showing token recovery

**Checkpoint:** `/context fresh` confirms net 14k token recovery per skill; rules files total <20k tokens

---

## Resource Allocation

| Phase | Tasks | Duration | Dispatch pattern | Parallelism |
|-------|-------|----------|------------------|-------------|
| 0 (Spike) | 1 | 90 min | Solo subagent | None (blocking) |
| 1–5 | 5 | ~1–2 weeks | 1 subagent per session | Sequential (1 task at a time) |
| 2–3 | 5 | ~2–3 weeks | 1 subagent per session | Sequential by phase (6→7→8, 9→10) |
| 4 | 2 | 2–3 days | 1 subagent per session | Sequential (11→12) |

**Total effort:** ~3–4 weeks solo development (no parallelism because tasks have sequential dependencies)

---

## Handoff & Approval Gates

| Gate | Approval | Trigger |
|------|----------|---------|
| Spike findings valid | Helder (async review of pilot-findings.md) | After Phase 0 completes |
| Library files indexed | Main agent (visual confirmation post-task) | After Tasks 01–05 complete |
| Routing tables minimal | Main agent (confirm <2 pages per file) | After Tasks 06–10 complete |
| Skill re-enablement approved | Helder (if any config changes needed) | Before Task 11 dispatch |
| Token recovery validated | Main agent (run `/context fresh`, record baseline/after) | After Task 12 completes |

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Skill invocation breaks workflow | Low | High | Spike validates end-to-end; abort if pattern breaks |
| Agents cannot find library files | Low | High | Spike confirms discoverability; routing tables in rules files serve as index |
| Superpowers skills have bugs | Low | Medium | Test each skill invocation in Task 11; escalate to Helder if broken |
| Context savings less than estimated | Medium | Low | Task 11 / Task 12 measure actual; if <10k, document findings |
| Ongoing tasks affected by live rules changes | Low | High | Announce feature to team before Phase 1; freeze rules-file edits during refactor |

---

## Acceptance Criteria (Plan perspective)

- ✅ All 12 tasks in tasks.md with atomization + dependencies + demo statements
- ✅ Phase 0 spike completes successfully; findings artifact exists
- ✅ Phase 1–5 tasks complete sequentially; 5 small files refactored
- ✅ Phase 2–3 tasks complete sequentially; workflow.md and testing.md refactored
- ✅ Phase 4 tasks complete; skills re-enabled; `/context` shows 14k token recovery
- ✅ CLAUDE.md updated with enabled superpowers
- ✅ Zero agent workflow changes; all rules accessible via routing tables + skills

---

## Timeline (Target)

| Week | Phase | Tasks | Status |
|------|-------|-------|--------|
| 1 | Phase 0 + Phase 1–2 | Spike + tasks 01–02 | Spike on 2026-07-05, tasks 01–02 on 2026-07-05 to 2026-07-06 |
| 2 | Phase 1–3 | Tasks 03–08 | Tasks 03–05 on 2026-07-07 to 2026-07-08; tasks 06–08 on 2026-07-08 to 2026-07-10 |
| 3 | Phase 3–4 | Tasks 09–12 | Tasks 09–10 on 2026-07-11; tasks 11–12 on 2026-07-12 |

**Handoff to Helder:** 2026-07-12 with final `/context` measurement + CLAUDE.md update

---

## Success Criteria (Execution perspective)

1. **Token efficiency:** Rules load <20k unconditional; 14k on-demand per skill used
2. **Behavioral invariance:** Agents follow identical workflows; only documentation source changes
3. **Content integrity:** Zero content loss; every rule line accounted for
4. **Discoverability:** Library files are indexed in routing tables; agents can navigate
5. **Skill coverage:** Every superpowers skill referenced in routing tables is invoked & verified
6. **No rework:** Each task completes in one pass; no post-phase corrections needed
