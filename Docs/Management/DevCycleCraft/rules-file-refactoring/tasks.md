# Rules File Refactoring — Tasks

## Strategy

> **Reprioritized 2026-07-04 (post-spike economics review).** The original order (small → large → measure last) put the *lowest-yield, highest-risk* files (workflow.md, testing.md) in the middle and deferred measurement to the very end. Corrected below.
>
> **Core insight:** converting unconditional load → on-demand load only saves tokens *if the on-demand condition is usually false*. Situational files (mediatr, component-governance, bug-tracking, constraints) are needed in a minority of sessions → high, safe savings. Core files (workflow.md, testing.md) are needed by almost every orchestrator/implementor → moving them to skill bodies that then reload per-agent nets ≈zero and risks an agent missing a hard rule. So: **do the situational files first, MEASURE the real per-subagent load, and only then decide whether the workflow/testing splits are worth it.**

Tasks are ordered in phases:
- **GATE-A (subagent inheritance — DONE 2026-07-04, PASS):** proved subagents inherit `.claude/rules/*` in full; measured 60,492-token cold-start with 0 tools. Findings: `findings-measurement.md`. This validated the per-wave multiplication premise *before* any Phase-1 file work (moved ahead of Phase 1 per Helder 2026-07-04, F2).
- **Phase 0 (Spike):** DONE — validated routing-table pattern on code-principles.md
- **Phase 1 (situational files — DO FIRST):** mediatr (delete), component-governance, bug-tracking, constraints — genuinely conditional, safe, high-yield
- **GATE-B (blocking for 06–10):** post-Phase-1 re-measure — confirm the real always-loaded rules total dropped as expected and decide whether the workflow.md/testing.md split economics justify the effort *before* investing in the expensive splits
- **Phase 2 (core files — CONDITIONAL on GATE-B):** workflow.md (3 waves) and testing.md (2 waves), only if the gate proves per-subagent savings are real
- **Phase 3:** Re-enable superpowers (narrowed to `brainstorming` + `writing-plans` — see Task 11), measure impact, update CLAUDE.md

All tasks are sequential except where marked `[P]`.

### Execution order (revised)

`[GATE-A ✓] → 01 (finalize) → 02 → 04 → 03 → 05 → [GATE-B] → 06–08 → 09–10 → 11 → 12`

> **Numbering vs execution order (F5):** task *numbers* (01–05) reflect authoring order; *execution* order is `02 → 04 → 03 → 05` (risk/yield: pure-delete first, then lowest-risk situational files). Follow the arrow, not the number.

Rationale for the situational-first order: 02 (mediatr) is a pure delete with zero risk; 04/03/05 are near-100% project-specific and needed only during component work / bug fixes / constraint lookups. 06–10 are gated on measured evidence.

---

## Phase 0 — Spike Validation [SEQUENTIAL — BLOCKING]

- [x] **[SPIKE] Validate routing-table pattern + skill invocation** — DONE 2026-07-04, PASS. Findings: `pilot-findings.md`. Executed directly by main agent (no subagent) to avoid ~55k/subagent cold-start that the rules bloat itself inflates.
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

### Task 01: Finalize code-principles.md (subsumed by spike)
- [ ] **01 - Finalize `code-principles.md`** — mostly done by the spike; this is a *verification pass*, not a rewrite
  - **Produces:** Confirmed routing table (already written) + skill-map row (already added)
  - **Consumes:** Spike findings from Phase 0
  - **Risk:** Low — spike already delivered the finished routing table + `code-style-reference.md`
  - **Files owned:** 
    - `.claude/rules/code-principles.md` (verify only — spike already rewrote it to 44 lines)
    - `.claude/library/code-style-reference.md` (verify only — spike already created it verbatim)
  - **DO NOT create `ddi-registration-conventions.md`** — per spike finding #4/#5 (over-fragmentation guard), the DI section is small and reads fine inside `code-style-reference.md`. One cohesive library file per rules file.
  - **Token savings:** ~2.5k (already realized by spike)
  - **Demo:** Run `myvocalist-coding` skill; navigate to code-style-reference.md library file from routing table pointer; confirm inbound `§` anchors still resolve from CLAUDE.md / constraints-registry.md / dialogs-validation.md
  - **Review lane:** Standard

### Task 02: DELETE mediatr-patterns.md (do first — pure win, zero risk)
- [ ] **02 - Delete `mediatr-patterns.md`; move content to library**
  - **Change from original plan (2026-07-04):** do NOT leave a stub in `.claude/rules/`. MediatR is **not registered in `MauiProgram.cs`** — the file documents code that does not exist yet, so it is 1.1k tokens of pure unconditional overhead every session. Remove it from the always-loaded set entirely; pull the reference back in only when MediatR is actually introduced.
  - **Produces:** `mediatr-patterns.md` removed from `.claude/rules/`; content preserved in library
  - **Consumes:** nothing (independent — can run first)
  - **Risk:** Low — reference-only, MediatR not active; no inbound `§` anchors expected (grep to confirm per Gotcha 1)
  - **Files owned:** 
    - `.claude/rules/mediatr-patterns.md` (DELETE)
    - `.claude/library/mediatr-reference.md` (NEW — current content moved here verbatim)
  - **Pre-step:** `grep -rn "mediatr-patterns" .claude CLAUDE.md Docs` — if any file links to it, replace the link with a pointer to the library file; add a one-line row to the `myvocalist-coding` skill map so it stays discoverable.
  - **Token savings:** ~1.1k (full removal, not partial)
  - **Demo:** Confirm `.claude/rules/mediatr-patterns.md` is gone; `myvocalist-coding` skill map routes to `mediatr-reference.md`
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
    - `.claude/library/constraints-reference.md` (NEW — single cohesive file with DevExpress / EF-Core / MAUI / .sln sections indexed by `##` headings)
  - **Change from original plan (2026-07-04):** one cohesive `constraints-reference.md` with internal section headings, NOT three separate files — per spike over-fragmentation guard (finding #5). Fewer, well-indexed files are more discoverable than many stubs. **Preserve the `§ EF Core / SQLite` heading** — `code-principles.md` links to it by anchor (Gotcha 1).
  - **Token savings:** ~2k
  - **Demo:** Look up a DevExpress constraint; confirm it's indexed in `constraints-reference.md` and reachable from the routing table + `myvocalist-coding` skill map
  - **Review lane:** Standard

---

## GATE-A — subagent inheritance [DONE 2026-07-04, PASS]

- [x] **GATE-A - Prove subagents inherit `.claude/rules/*` before any file work** — DONE 2026-07-04, PASS.
  - **Why moved ahead of Phase 1 (F2):** the headline savings ("5 agents × Nk per wave") assumes each subagent reloads the rules in full. That single fact validates/invalidates the premise for *every* phase, including Phase 1 — so it was cheap and decisive to verify first, not mid-plan.
  - **Method:** one throwaway `general-purpose` subagent, **zero tool uses**, introspecting only its injected context.
  - **Result:** all 7 rules files inherited in full (verbatim quotes captured); `code-principles.md` arrived in its reduced 44-line routing-table form (proves the pattern shrinks per-subagent load). Measured cold-start: **60,492 tokens, 0 tools**. Baseline now measured (supersedes the 17.2k/28.4k/33.3k estimates). Findings: `findings-measurement.md`.
  - **Verdict:** PASS → Phase 1 GO (every reduction sticks per-agent).

## GATE-B — split economics [SEQUENTIAL — BLOCKING for Tasks 06–10]

- [ ] **GATE-B - Re-measure after Phase 1; decide whether the workflow.md / testing.md splits are worth it**
  - **Question:** (1) After Phase 1, what is the real always-loaded rules total in a fresh subagent (target: measurable drop toward ~2–3k for the situational set)? (2) For core guidance (TDD, workflow), is a meaningful fraction of the full body *not* needed by the typical implementor — i.e. does routing-table-izing them save real per-agent tokens without hiding a hard rule?
  - **Method:** re-run the GATE-A probe post-Phase-1; compare cold-start token delta. Record in `findings-measurement.md` (append).
  - **Success (proceed to 06–10):** situational-set reduction confirmed AND workflow/testing full bodies are demonstrably unneeded by a majority of implementor tasks.
  - **Failure (STOP / re-scope):** every implementor needs TDD+workflow anyway → splitting adds indirection for ≈zero saving; prefer the role-scoped-loading approach in the *"Per-Agent MCP/Skill Context Isolation"* BACKLOG item instead.
  - **Files owned:** `Docs/Management/DevCycleCraft/rules-file-refactoring/findings-measurement.md` (append GATE-B section)
  - **Demo:** `findings-measurement.md` states, with measured numbers, whether 06–10 are justified.
  - **Review lane:** Standard — Helder confirms the go/no-go before Tasks 06–10.

---

## Phase 2–3 — Large Rules Files [SEQUENTIAL — GATED on MEASUREMENT GATE go decision]

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

- [ ] **11 - Re-enable superpowers (NARROWED) + verify on-demand loading** [SEQUENTIAL]
  - **Scope decision (Helder 2026-07-04, F4):** re-enable **only `brainstorming` + `writing-plans`**. Do **NOT** re-enable `test-driven-development` or `code-review` — they duplicate the project's heavily-customized `testing.md` / review flow (real-SQLite, Moq, AC traceability, Tester/Builder split), so re-enabling them (a) saves ≈0 tokens (bodies reload per-agent anyway) and (b) creates a second source of truth → authority ambiguity. Keep TDD/review guidance as project rules + `.claude/library/*` only.
  - **Produces:** 2 enabled superpowers plugins (brainstorming, writing-plans) + verification evidence
  - **Consumes:** All refactors complete (Tasks 01–10)
  - **Risk:** Low — only configuration change, no content changes
  - **Files owned:** 
    - `.claude/settings.json` (enable brainstorming, writing-plans ONLY)
    - `.claude/scripts/verify-skill-loading.py` (NEW — sanity check script to confirm skills load on-demand)
  - **Verification gates:** 
    - Run `/context fresh` — confirm Memory files shows the expected drop for .claude/rules/
    - Invoke brainstorming skill — confirm description loads (~50 tok), full body loads on-demand
    - Invoke writing-plans skill — same
  - **Rationale note (2026-07-04):** the bankable saving from this feature is the **unconditional rules reduction that sticks in every subagent** (~16–19k/agent per `findings-measurement.md`), NOT an "on-demand recovery per skill" ceiling. Narrowing to 2 skills that the project does *not* already cover avoids duplicate-source churn while keeping the sticky per-agent win.
  - **Demo:** Clean session run `/context` → record net token delta vs. before across a real brainstorm → plan wave
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
