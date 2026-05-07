# SDD Implementation Master Plan

> **Resume guide:** Read this file first in any new session. Find the first step that is NOT `Done` and continue from there.
> **Progress discipline:** Update status to `In Progress` when starting a step, `Done` when committed. Never batch steps.

---

## Conventions

**Status values:**
- `Pending` — not started
- `In Progress` — subagent dispatched or work underway
- `Done` — committed and pushed
- `Skipped` — deliberately omitted (reason in Notes)
- `Deferred` — moved to a later step (reason + target step in Notes)

**One subagent per step. Each subagent:**
1. Reads only the assigned opportunities file(s) and the target file
2. Applies ONLY the opportunities listed for that step
3. Commits with message `sdd: [step ID] — [brief description]`
4. Updates this file: marks step `Done`
5. Pushes

**Main agent role:** shell only — run `dotnet build` after each step, verify, then authorize next step.

---

## Phase 1 — Status Log Files

| Step | Description | Status | Notes |
|------|-------------|--------|-------|
| P1-S1 | Create `Docs/DevEnv/plans/impl/S1_impl_status.md` | Done | Created in setup session |
| P1-S2 | Create `Docs/DevEnv/plans/impl/S2_impl_status.md` | Done | Created in setup session |
| P1-S3 | Create `Docs/DevEnv/plans/impl/S3_impl_status.md` | Done | Created in setup session |
| P1-S4 | Create `Docs/DevEnv/plans/impl/S4_impl_status.md` | Done | Created in setup session |
| P1-S5 | Create `Docs/DevEnv/plans/impl/S5_impl_status.md` | Done | Created in setup session |
| P1-S6 | Create `Docs/DevEnv/plans/impl/S6_impl_status.md` | Done | Created in setup session |
| P1-S7 | Create `Docs/DevEnv/plans/impl/S7_impl_status.md` | Done | Created in setup session |
| P1-S8 | Create `Docs/DevEnv/plans/impl/S8_impl_status.md` | Done | Created in setup session |
| P1-S9 | Create `Docs/DevEnv/plans/impl/S9_impl_status.md` | Done | Created in setup session |
| P1-S10 | Create `Docs/DevEnv/plans/impl/S10_impl_status.md` | Done | Created in setup session |
| P1-MASTER | Create this master plan file | Done | This file |

---

## Phase 2 — New Support Files (no build impact, safe to do first)

These files are net-new and do not affect existing code.

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P2-A | Create `.claude/rules/constraints-registry.md` | OPP-4-9 | Done | |
| P2-B | Create `.claude/exception-registry.md` | OPP-6-12 | Done | |
| P2-C | Create `.claude/agents/implementor.md` | OPP-5-11 | Done | |
| P2-D | Create `.claude/agents/verifier.md` | OPP-5-11 | Done | |
| P2-E | Create `.claude/memory-bank/MEMORY.md` | OPP-4-12 | Done | |

> After each step: commit + push. No build check needed (no .cs/.xaml files).

---

## Phase 3 — `.claude/rules/workflow.md` (Rule 1: Spec-First enhancements)

One opportunity or small group per step. Sequential — same file.

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P3-01 | Add SDD Invariant block (spec changes before code changes) | OPP-3-01, OPP-1-12 | Done | |
| P3-02 | Add phase-gate reviews to New Feature Workflow | OPP-1-13 | Done | |
| P3-03 | Add spec-update gate (after implementation, update spec) | OPP-1-2, OPP-2-6 | Done | |
| P3-04 | Add When-to-Skip-SDD guidance + spec bypass rule | OPP-1-6, OPP-10-01, OPP-10-02 | Done | |
| P3-05 | Add SDD decision table for medium-complexity tasks | OPP-10-10 | Done | |
| P3-06 | Add spec structure enhancements (Out of Scope, Domain Vocab, Invariants sections) | OPP-2-3, OPP-2-4, OPP-2-11 | Done | |
| P3-07 | Add Given/When/Then + EARS format guidance | OPP-1-8, OPP-2-2, OPP-2-16 | Done | |
| P3-08 | Add spec completeness checklist + quality four-gate | OPP-2-1, OPP-2-19, OPP-10-11 | Done | |
| P3-09 | Add spec size calibration + two-tier spec trigger | OPP-2-8, OPP-2-17 | Done | |
| P3-10 | Add tacit knowledge capture + LLM extraction technique | OPP-2-7, OPP-2-22 | Done | |
| P3-11 | Add functional/technical separation table with examples | OPP-2-10 | Done | |
| P3-12 | Add state machine + integration contract sections to design.md template | OPP-2-12, OPP-2-18 | Done | |
| P3-13 | Add demo statement requirement | OPP-2-14 | Done | |
| P3-14 | Add failure-mode analysis + regeneration test practice | OPP-2-13, OPP-2-15 | Done | |
| P3-15 | Add constitution check (step 2a) to New Feature Workflow | OPP-3-02 | Done | |
| P3-16 | Add spec quality gate (mandatory before implementation) | OPP-9-06 | Done | |
| P3-17 | Add spec versioning discipline | OPP-9-08 | Done | |
| P3-18 | Add brownfield rule + When to update specs | OPP-10-06, OPP-10-05 | Done | |
| P3-19 | Add bug fix pattern (commit message as spec) | OPP-10-07 | Done | |
| P3-20 | Add over-specification guard + spec length guideline | OPP-10-04 | Done | |
| P3-21 | Add capture architectural decisions rule | OPP-10-15 | Done | |
| P3-22 | Add discovery mode section | OPP-10-02 | Done | |
| P3-23 | Add spec-as-source-of-truth rule | OPP-6-07 | Done | |
| P3-24 | Add architecture reversibility documentation | OPP-3-09 | Done | |
| P3-25 | Add Key Decisions section to design.md template | OPP-4-4 | Done | |
| P3-26 | Add decision log as fourth optional spec file | OPP-9-19 | Done | |
| P3-27 | Add spec ownership constraint (subagents don't write specs) | OPP-10-08 | Done | |

---

## Phase 4 — `.claude/rules/workflow.md` (Rule 2: Subagent Delegation enhancements)

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P4-01 | Add mandatory spec reads at session start (briefing protocol) | OPP-3-06, OPP-1-9 | Done | |
| P4-02 | Add role scope declaration block to briefing template | OPP-5-9 | Done | |
| P4-03 | Add task sizing limits (prevent context window exhaustion) | OPP-3-05 | Done | |
| P4-04 | Add sequential-only file registry | OPP-5-13 | Done | |
| P4-05 | Add pre-wave dependency check + scope isolation | OPP-5-3, OPP-6-08 | Done | |
| P4-06 | Add wave handoff: inject actual contracts for new artifacts | OPP-5-4 | Done | |
| P4-07 | Add shared contracts section requirement before parallel impl | OPP-5-5 | Done | |
| P4-08 | Add cross-spec review gate before multi-spec wave | OPP-6-15 | Done | |
| P4-09 | Add verifier subagent guidance | OPP-5-1 | Done | |
| P4-10 | Add Adversarial Critic pattern | OPP-9-07 | Done | |
| P4-11 | Add subagent scope constraint (no unilateral redesign) | OPP-5-2 | Done | |
| P4-12 | Add living spec protocol (write decisions back before stopping) | OPP-5-12 | Done | |
| P4-13 | Add build retry cap (3 attempts → stop) | OPP-5-16 | Done | |
| P4-14 | Add kill criteria for stuck subagents | OPP-8-06, OPP-8-14 | Done | |
| P4-15 | Add silent task completion: post-edit re-read requirement | OPP-5-6 | Done | |
| P4-16 | Add proof-of-action: Changed files mandatory in task-log | OPP-9-11 | Done | |
| P4-17 | Update task-log format: add Verification evidence block | OPP-5-7 | Done | |
| P4-18 | Add acceptance criteria traceability matrix to task-log | OPP-9-15 | Done | |
| P4-19 | Update exit checklist: make build + test explicit steps | OPP-6-17 | Done | |
| P4-20 | Add post-wave verification (main agent runs build independently) | OPP-5-8 | Done | |
| P4-21 | Add wave completion discovery briefs | OPP-5-14 | Done | |
| P4-22 | Add context reset discipline for orchestrator | OPP-5-15 | Done | |
| P4-23 | Add spec gap escalation documentation requirement | OPP-3-07 | Done | |
| P4-24 | Add context exhaustion warning signs | OPP-4-3 | Done | |
| P4-25 | Add multi-session state handoff protocol | OPP-4-6 | Done | |
| P4-26 | Add fresh-context iteration pattern | OPP-4-13 | Done | |
| P4-27 | Add subagent MCP isolation per task | OPP-7-8 | Done | |
| P4-28 | Add pre-task context gate (verify spec + test exist) | OPP-9-16 | Done | |
| P4-29 | Add bounded autonomy rule (irreversible actions need confirmation) | OPP-9-18 | Done | |
| P4-30 | Add spec freshness gate before dispatching a wave | OPP-9-17 | Done | |

---

## Phase 5 — `.claude/rules/workflow.md` (Rules 3–6 + new rules)

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P5-01 | Add task completion verification (demo statement + DI check) | OPP-2-5 | Done | |
| P5-02 | Add E2E emulator gate before To Review | OPP-9-13 | Done | |
| P5-03 | Add session-end spec update ritual | OPP-9-09 | Done | |
| P5-04 | Add task entry format (produces/consumes/risk/files fields) | OPP-3-03, OPP-3-13, OPP-3-18 | Done | |
| P5-05 | Add DRY Onion task ordering rule | OPP-3-04 | Done | |
| P5-06 | Add task atomization checklist + DGI complexity classification | OPP-1-5, OPP-8-01, OPP-8-13 | Done | |
| P5-07 | Add thick-slice task format for briefings | OPP-8-02 | Done | |
| P5-08 | Add dependency ordering example (phases template) | OPP-8-03 | Done | |
| P5-09 | Add in-progress marker [~] for claimed tasks | OPP-8-04 | Done | |
| P5-10 | Add single-writer rule for hotspot files | OPP-8-05 | Done | |
| P5-11 | Add pre-dispatch validation checklist | OPP-3-11 | Done | |
| P5-12 | Add spike validation task pattern | OPP-3-12 | Done | |
| P5-13 | Add review SLA + risk-tiered review lanes | OPP-3-13, OPP-3-15 | Done | |
| P5-14 | Add approval authority matrix | OPP-3-16 | Done | |
| P5-15 | Add multi-wave checkpoint pattern | OPP-3-10 | Done | |
| P5-16 | Add git worktrees as isolation primitive | OPP-8-10 | Done | |
| P5-17 | Add dependency-first merge sequencing | OPP-8-11 | Done | |
| P5-18 | Add pre-parallel interface contracts rule | OPP-8-12 | Done | |
| P5-19 | Add findings.md as session artifact | OPP-8-09 | Done | |
| P5-20 | Add ACTIVE-CONSIDERATIONS.md as session priority stack | OPP-8-15 | Done | |
| P5-21 | Add session start protocol (what to read at session start) | OPP-4-1, OPP-8-07 | Done | |
| P5-22 | Add tiered memory governance rule | OPP-4-17 | Done | |
| P5-23 | Add hook enforcement notes sub-section | OPP-6-04 | Done | |
| P5-24 | Add spec ceremony calibration table | OPP-7-15 | Done | |
| P5-25 | Add spec format portability rule | OPP-7-1 | Done | |
| P5-26 | Add ROI J-Curve awareness note | OPP-10-09 | Done | |
| P5-27 | Add intent verification before To Review | OPP-10-13 | Done | |
| P5-28 | Add spec rot multiplier warning | OPP-9-17 | Done | |
| P5-29 | Add rebuild test as spec quality check (feature close-out) | OPP-3-17 | Done | |
| P5-30 | Add GitHub MCP pre-task collision check | OPP-7-11 | Done | |

---

## Phase 6 — `.claude/settings.json`

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P6-01 | Update PostCompact hook — add spec re-read reminder | OPP-3-20 | Done | |
| P6-02 | Add SessionStart hook for hook health verification | OPP-6-13 | Done | |
| P6-03 | Add PostToolUse hook for Services file TDD reminder | OPP-7-18 | Done | |
| P6-04 | Add deny rules for CLAUDE.md and rules files | OPP-4-10 | Done | |

> Note: OPP-6-14 (phase-gate hook for spec approval flag) deferred — requires `.claude/approvals/` infrastructure that adds complexity beyond current scope.

---

## Phase 7 — `.claude/rules/testing.md`

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P7-01 | Add TDD-within-SDD framing note | OPP-1-7 | Done | |
| P7-02 | Add acceptance criteria traceability rule | OPP-2-20 | Done | |
| P7-03 | Add Tester/Builder role separation rule | OPP-9-01 | Done | |
| P7-04 | Add one-test-at-a-time discipline | OPP-9-02 | Done | |
| P7-05 | Add Builder must not modify tests rule | OPP-9-03 | Done | |
| P7-06 | Add test quality audit checklist | OPP-9-04 | Done | |
| P7-07 | Add property-based testing with FsCheck | OPP-9-05 | Done | |
| P7-08 | Add TDD level guidance by risk (A/B/C) | OPP-9-12 | Done | |
| P7-09 | Add mutation testing with Stryker.NET | OPP-9-14 | Done | |

---

## Phase 8 — `.claude/rules/code-principles.md`

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P8-01 | Add determinism rule for quality attributes | OPP-2-21 | Done | |
| P8-02 | Add suppression justification policy | OPP-6-16 | Done | |

---

## Phase 9 — `CLAUDE.md`

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P9-01 | Declare SDD Level 2 (Spec-Anchored) | OPP-1-1 | Done | |
| P9-02 | Add DDD+SDD+TDD layering guidance | OPP-1-11 | Done | |
| P9-03 | Add rebuild test as spec quality diagnostic | OPP-1-10 | Done | |
| P9-04 | Add CLAUDE.md size monitoring guidance | OPP-4-2 | Done | |
| P9-05 | Add anti-pattern guard (no LLM-generated context files) | OPP-4-15 | Done | |
| P9-06 | Add MCP security guidance for untrusted content | OPP-4-16 | Done | |
| P9-07 | Add GitHub MCP evaluation note | OPP-4-14 | Done | |
| P9-08 | Add Rule Authority Hierarchy section | OPP-6-10 | Done | |
| P9-09 | Add rationale to Non-Negotiable rules | OPP-6-02 | Done | |
| P9-10 | Distinguish constitutional constraints from guidelines | OPP-6-01 | Done | |
| P9-11 | Add amendment governance process | OPP-6-03 | Done | |
| P9-12 | Add periodic constitutional audit | OPP-6-11 | Done | |
| P9-13 | Add MCP availability gate | OPP-7-2 | Done | |
| P9-14 | Add MCP security stance / allowlist | OPP-7-3 | Done | |
| P9-15 | Add MCP context budget guidance | OPP-7-4 | Done | |
| P9-16 | Update Context7 invocation discipline | OPP-7-7 | Done | |
| P9-17 | Add Context7 version-pinning discipline | OPP-7-12 | Done | |
| P9-18 | Add Tool Selection ADR section | OPP-7-6 | Done | |
| P9-19 | Add Tessl Registry evaluation note | OPP-7-9 | Done | |
| P9-20 | Add Cursor as complementary tooling note | OPP-7-10 | Done | Combined into P9-19 Tool Selection block |
| P9-21 | Add .mcp.json.template reference | OPP-7-13 | Done | Combined into P9-19 Tool Selection block |
| P9-22 | Add sdd-mcp evaluation note | OPP-7-14 | Done | Combined into P9-19 Tool Selection block |
| P9-23 | Add MCP response token discipline | OPP-7-19 | Done | |
| P9-24 | Add MCP tool batching readiness note | OPP-7-16 | Done | Combined into P9-23 MCP block |
| P9-25 | Add Playwright MCP evaluation note | OPP-7-17 | Done | Combined into P9-23 MCP block |
| P9-26 | Add Spec Kit migration path note | OPP-7-20 | Done | Combined into P9-18 Tool Selection section |
| P9-27 | Declare CLAUDE.md as constitutional document | OPP-10-12 | Done | |
| P9-28 | Add SDD applicability statement for MyVocaList | OPP-10-14 | Done | |

---

## Phase 10 — `.claude/commands/review.md`

| Step | Description | OPPs | Status | Notes |
|------|-------------|------|--------|-------|
| P10-01 | Add spec-drift detection checklist item | OPP-1-3 | Pending | |
| P10-02 | Add spec-vs-code consistency section | OPP-2-9 | Pending | |
| P10-03 | Add Spec Conformance section | OPP-3-08 | Pending | |
| P10-04 | Add AC traceability format | OPP-3-14 | Pending | |
| P10-05 | Add spec drift detection step | OPP-3-19, OPP-4-5 | Pending | |
| P10-06 | Add spec compliance section (cross-service contracts) | OPP-5-10 | Pending | |
| P10-07 | Add severity classification (Blocker/Warning/Suggestion) | OPP-6-05 | Pending | |
| P10-08 | Add Spec Alignment section | OPP-6-06, OPP-9-10 | Pending | |
| P10-09 | Add six spec-code drift categories checklist | OPP-6-09 | Pending | |
| P10-10 | Add spec drift check section | OPP-7-5 | Pending | |
| P10-11 | Add spec-code alignment check | OPP-8-08 | Pending | |
| P10-12 | Add spec-code consistency check | OPP-10-03 | Pending | |

---

## Phase 11 — Conflict Analysis

| Step | Description | Status | Notes |
|------|-------------|--------|-------|
| P11-01 | Read all modified target files in post-implementation state | Pending | |
| P11-02 | Produce conflict report at `Docs/DevEnv/plans/impl/conflict_report.md` | Pending | |
| P11-03 | Present conflict report to Helder for approval | Pending | |
| P11-04 | Apply approved resolutions (one subagent per affected file) | Pending | |

---

## Progress Summary

| Phase | Steps | Done | In Progress | Pending |
|-------|-------|------|-------------|---------|
| Phase 1 — Status log files | 11 | 11 | 0 | 0 |
| Phase 2 — New support files | 5 | 5 | 0 | 0 |
| Phase 3 — workflow.md Rule 1 | 27 | 27 | 0 | 0 |
| Phase 4 — workflow.md Rule 2 | 30 | 30 | 0 | 0 |
| Phase 5 — workflow.md Rules 3–6 | 30 | 30 | 0 | 0 |
| Phase 6 — settings.json | 4 | 4 | 0 | 0 |
| Phase 7 — testing.md | 9 | 9 | 0 | 0 |
| Phase 8 — code-principles.md | 2 | 0 | 0 | 2 |
| Phase 9 — CLAUDE.md | 28 | 0 | 0 | 28 |
| Phase 10 — review.md | 12 | 0 | 0 | 12 |
| Phase 11 — Conflict Analysis | 4 | 0 | 0 | 4 |
| **Total** | **162** | **11** | **0** | **151** |
