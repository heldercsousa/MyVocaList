# Rules File Refactoring — Tasks

## Strategy

Tasks are ordered in phases:
- **Phase 0 (Spike):** Validate routing-table pattern on code-principles.md; pilot the extraction, library, skill invocation workflow
- **Phase 1–5:** Refactor remaining small rules files (1–2k tokens each)
- **Phase 2–3:** Refactor workflow.md (3 waves) and testing.md (2 waves) — the largest files
- **Phase 4:** Re-enable superpowers, measure impact, update CLAUDE.md

All tasks are sequential except where marked `[P]`.

---

## Phase 0 — Spike Validation [SEQUENTIAL — BLOCKING]

- [ ] **[SPIKE] Validate routing-table pattern + skill invocation**
  - **Time-box:** 90 min (hard stop)
  - **Question:** Does extracting sections from code-principles.md → library file + routing table + skill invocation work end-to-end without workflow changes?
  - **Success criterion:** (1) code-principles.md rewritten as 1-page routing table; (2) ~1k tokens of architecture/naming/style content extracted to `~/library/code-style-reference.md`; (3) dotnet-skills skill invoked and confirmed to load; (4) zero content loss documented; (5) no agent workflow changes needed
  - **Failure criterion:** (1) extraction approach requires agents to discover new library files (breaks discoverability); (2) skill invocation fails or requires special configuration; (3) content loss not recoverable
  - **Artifact:** `pilot-findings.md` documenting pattern success and any gotchas
  - **Files owned:** 
    - `.claude/rules/code-principles.md` (rewrite as routing table)
    - `.claude/library/code-style-reference.md` (NEW)
    - `pilot-findings.md` (findings artifact)
  - **Demo:** Invoke `myvocalist-coding` skill; confirm it references dotnet-skills; run `/context` and verify code-principles.md is <0.5k tokens
  - **Review lane:** Spike validation only — no subagent review yet

---

## Phase 1–5 — Small Rules Files [SEQUENTIAL]

### Task 01: Refactor code-principles.md
- [ ] **01 - Refactor `code-principles.md`**
  - **Produces:** Minimal 1-page routing table + skill pointers
  - **Consumes:** Spike findings from Phase 0
  - **Risk:** Low — spike already validated pattern
  - **Files owned:** 
    - `.claude/rules/code-principles.md` (rewrite)
    - `.claude/library/code-style-reference.md` (already created in spike, finalize)
    - `.claude/library/ddi-registration-conventions.md` (NEW — extract DI section)
  - **Token savings:** ~2k
  - **Demo:** Run `myvocalist-coding` skill; navigate to code-style-reference.md library file from routing table pointer
  - **Review lane:** Standard

### Task 02: Refactor mediatr-patterns.md
- [ ] **02 - Refactor `mediatr-patterns.md`**
  - **Produces:** Stub or consolidated into code-principles routing table
  - **Consumes:** code-principles.md refactor
  - **Risk:** Low — file is reference-only, MediatR not yet active
  - **Files owned:** 
    - `.claude/rules/mediatr-patterns.md` (minimal stub or merge into code-principles)
    - `.claude/library/mediatr-reference.md` (NEW — current content moved here)
  - **Token savings:** ~1k
  - **Demo:** Reference mediatr-patterns from code-principles routing table; confirm stub is minimal
  - **Review lane:** Standard

### Task 03: Refactor bug-tracking.md
- [ ] **03 - Refactor `bug-tracking.md`**
  - **Produces:** Minimal 0.5-page routing table + severity/regression tables in library
  - **Consumes:** Previous refactors
  - **Risk:** Medium — bug-tracking is actively used; ensure severity/regression tables are discoverable
  - **Files owned:** 
    - `.claude/rules/bug-tracking.md` (rewrite as routing table)
    - `.claude/library/bug-severity-classification.md` (NEW)
    - `.claude/library/regression-test-requirements.md` (NEW)
  - **Token savings:** ~1.5k
  - **Demo:** Create a hypothetical BUG-NNN entry; confirm severity table in library is referenced and accessible
  - **Review lane:** Standard

### Task 04: Refactor component-change-governance.md
- [ ] **04 - Refactor `component-change-governance.md`**
  - **Produces:** Minimal 0.5-page routing table + governance gates in library
  - **Consumes:** Previous refactors
  - **Risk:** Low — 4-gate process is well-defined
  - **Files owned:** 
    - `.claude/rules/component-change-governance.md` (rewrite as routing table)
    - `.claude/library/component-safety-gate.md` (NEW — 4-gate checklist + consumer map template)
  - **Token savings:** ~1k
  - **Demo:** Hypothetical component change; walk through 4-gate checklist from library
  - **Review lane:** Standard

### Task 05: Refactor constraints-registry.md
- [ ] **05 - Refactor `constraints-registry.md`**
  - **Produces:** Minimal 1-page routing table + indexed constraint categories in library
  - **Consumes:** Previous refactors
  - **Risk:** Low — constraints are well-organized; indexing by DevExpress/EF/MAUI straightforward
  - **Files owned:** 
    - `.claude/rules/constraints-registry.md` (rewrite as routing table + how-to-add instructions)
    - `.claude/library/devexpress-constraints.md` (NEW)
    - `.claude/library/ef-core-constraints.md` (NEW)
    - `.claude/library/maui-constraints.md` (NEW)
  - **Token savings:** ~2k
  - **Demo:** Look up a DevExpress constraint; confirm it's indexed in library and discoverable
  - **Review lane:** Standard

---

## Phase 2–3 — Large Rules Files [SEQUENTIAL]

### Tasks 06–08: Refactor workflow.md (3 waves)

- [ ] **06 - Refactor `workflow.md` Phase 1 (Rules 1–2)** [SEQUENTIAL]
  - **Produces:** Routing table for Rules 1–2 + linked library guides
  - **Consumes:** Previous phase complete
  - **Risk:** Medium — workflow.md is largest (9.9k tokens); Rules 1–2 are foundational
  - **Files owned:** 
    - `.claude/rules/workflow.md` (extract Rules 1–2 to library, rewrite as routing table)
    - `.claude/library/spec-writing-guide.md` (FINALIZE — consolidate existing + workflow Rule 1)
    - `.claude/library/subagent-patterns.md` (NEW — task sizing, wave parallelism, checklist)
  - **Token savings:** ~4k
  - **Demo:** New agent reads routing table, navigates to spec-writing-guide and subagent-patterns for procedure detail
  - **Review lane:** Standard

- [ ] **07 - Refactor `workflow.md` Phase 2 (Rules 3–5)** [SEQUENTIAL]
  - **Produces:** Routing table for Rules 3–5 + linked library references
  - **Consumes:** Task 06 complete
  - **Risk:** Medium — Rules 3–5 cover commit, tasks.md, task-log ceremony
  - **Files owned:** 
    - `.claude/rules/workflow.md` (extract Rules 3–5, add to routing table)
    - `.claude/library/commit-ceremony.md` (NEW — Rule 3 extract)
    - `.claude/library/task-atomization.md` (NEW — Rule 4 extract, DRY Onion order)
    - `.claude/library/task-log-format.md` (NEW — Rule 5 extract, AC traceability matrix)
  - **Token savings:** ~3k
  - **Demo:** Complete a task; navigate from workflow rule 4 to task-atomization guide; write task-log from task-log-format template
  - **Review lane:** Standard

- [ ] **08 - Refactor `workflow.md` Phase 3 (Rules 6–8)** [SEQUENTIAL]
  - **Produces:** Final routing table for Rules 6–8 + linked library references
  - **Consumes:** Task 07 complete
  - **Risk:** Medium — Rules 6–8 are integration points (research tools, session start, GitHub collision check)
  - **Files owned:** 
    - `.claude/rules/workflow.md` (extract Rules 6–8, complete routing table)
    - `.claude/library/research-tool-selection.md` (NEW — Rule 6 extract, Context7 → Exa → WebSearch hierarchy)
    - `.claude/library/session-start-protocol.md` (NEW — Rule 7 extract, lease-aware reclaim)
    - `.claude/library/github-collision-protocol.md` (NEW — Rule 8 extract)
  - **Token savings:** ~3k
  - **Demo:** New session reads workflow Rule 7 routing table; navigates to session-start-protocol for full reading order + lease reclaim steps
  - **Review lane:** Standard

### Tasks 09–10: Refactor testing.md (2 waves)

- [ ] **09 - Refactor `testing.md` Phase 1 (TDD + AC traceability)** [SEQUENTIAL]
  - **Produces:** Routing table for TDD section + AC format reference
  - **Consumes:** Tasks 01–08 complete
  - **Risk:** Medium — testing.md is large (8.3k); TDD is foundational
  - **Files owned:** 
    - `.claude/rules/testing.md` (extract TDD levels, AC format, test naming to library; rewrite section as routing table)
    - `.claude/library/test-driven-development-levels.md` (NEW — High/Medium/Low risk classification + test requirements per level)
    - `.claude/library/acceptance-criteria-format.md` (NEW — EARS format, traceability matrix, examples)
  - **Token savings:** ~4k
  - **Demo:** Agent classifies new task as High-risk; navigates to test-driven-development-levels; confirms test requirements
  - **Review lane:** Standard

- [ ] **10 - Refactor `testing.md` Phase 2 (Test types, structure, anti-patterns)** [SEQUENTIAL]
  - **Produces:** Final routing table for testing.md + comprehensive test patterns reference
  - **Consumes:** Task 09 complete
  - **Risk:** Medium — test patterns must remain discoverable
  - **Files owned:** 
    - `.claude/rules/testing.md` (extract test patterns, complete routing table)
    - `.claude/library/unit-test-patterns.md` (NEW)
    - `.claude/library/integration-test-patterns.md` (NEW)
    - `.claude/library/testing-anti-patterns.md` (NEW)
    - `.claude/library/test-naming-conventions.md` (NEW)
  - **Token savings:** ~4k
  - **Demo:** Write a service test; navigate from testing routing table to unit-test-patterns guide; confirm naming convention from library
  - **Review lane:** Standard

---

## Phase 4 — Skill Re-Enablement & Measurement [SEQUENTIAL]

- [ ] **11 - Re-enable superpowers + verify on-demand loading** [SEQUENTIAL]
  - **Produces:** Enabled superpowers plugins + verification evidence
  - **Consumes:** All refactors complete (Tasks 01–10)
  - **Risk:** Low — only configuration change, no content changes
  - **Files owned:** 
    - `.claude/settings.json` (enable brainstorming, writing-plans, test-driven-development, code-review)
    - `.claude/scripts/verify-skill-loading.py` (NEW — sanity check script to confirm skills load on-demand)
  - **Verification gates:** 
    - Run `/context fresh` — confirm Memory files shows <20k for .claude/rules/
    - Invoke brainstorming skill — confirm description loads (~50 tok), full body loads on-demand (~3k tok)
    - Invoke writing-plans skill — same
    - Invoke test-driven-development skill — same
    - Invoke code-review skill — same
  - **Demo:** Clean session run `/context` → see 14k token savings vs. before; run full workflow (brainstorm → plan → implement → review) confirming skill bodies load on-demand
  - **Review lane:** Standard

- [ ] **12 - Update CLAUDE.md § Skill & MCP Lookup table** [SEQUENTIAL]
  - **Produces:** Updated CLAUDE.md with enabled superpowers + reduced narrative
  - **Consumes:** Task 11 complete (skills verified)
  - **Risk:** Low — documentation-only change
  - **Files owned:** 
    - `CLAUDE.md` (update Skill & MCP Lookup section; add rows for enabled superpowers; remove redundant narrative; target <200 lines total)
  - **Demo:** Read updated CLAUDE.md; confirm superpowers section is clear + concise; run `/context` final measurement showing net token recovery
  - **Review lane:** Standard

---

## Checkpoint Gates

After every 2 tasks:
- [ ] **Build:** `dotnet build` confirms no errors (rules are docs, but .sln must be updated if any files created)
- [ ] **Spike findings applied:** Lessons from Phase 0 spike are applied to Phase 1–5 tasks
- [ ] **Token measurement:** Rough estimate of tokens saved per task aligns with BACKLOG estimate
- [ ] **Library file integrity:** New library files are well-indexed, discoverable from routing tables

---

## Success Criteria (Task perspective)

All 12 tasks complete when:
1. ✅ All .claude/rules/*.md files are 1–2 pages (routing tables only)
2. ✅ All extracted content is in .claude/library/*.md files
3. ✅ Every superpowers skill is invoked at least once and verified to work
4. ✅ /context fresh shows net 14k token savings per skill used (measured in Phase 4)
5. ✅ CLAUDE.md Skill & MCP table is updated with enabled superpowers
6. ✅ No agent workflow changes required; all rules accessible via routing tables + skills
