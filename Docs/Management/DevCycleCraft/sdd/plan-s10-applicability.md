# S10 — Applicability: Enhancement Opportunities

> Source files analyzed: S10_Applicability.md, S10_1_Problem_Size_Suitability.md, S10_1_1_Brownfield_Retrofit_Difficulty.md, S10_2_Tradeoffs_and_Limitations.md, S10_2_1_Adoption_ROI_Timeline.md, S10_2_2_Cultural_Resistance.md
> Compared against: CLAUDE.md, .claude/rules/workflow.md, .claude/rules/testing.md, .claude/rules/code-principles.md
> Last reviewed: 2026-05-06

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, still unimplemented) | 8 |
| 🆕 New (not previously captured) | 7 |
| **Total** | **15** |

All 8 previously captured opportunities remain unimplemented — confirmed by inspecting current CLAUDE.md and workflow.md as of 2026-05-06. No existing opportunity has been superseded or made irrelevant.

---

## Previously Captured Opportunities

### ✅ OPP-10-01: Spec gate for small vs. large tasks — explicit bypass rule
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1 — Problem-Size Suitability / S10.2 — Trade-offs and Limitations
**Rationale:** The current workflow.md mandates spec-first for everything. SDD research is unambiguous: full spec overhead is net-negative on isolated bug fixes, single-file refactors, and one-off scripts. Giving Claude Code an explicit bypass condition prevents wasted ceremony and builds realistic expectations for what spec-first actually covers.
**Suggested content/change:** Add a "Spec bypass rule" block under Rule 1:

```
### When to skip the full spec workflow
Full spec discipline (requirements.md + design.md + tasks.md) is overhead when ALL of the following are true:
- Single developer, single session, scope fits in one file
- Root cause is known (bug fix) or requirement is one sentence (minor UI adjustment)
- Code will never be touched again (one-off migration, script)
- No coordination with other features or agents required

In these cases: document intent in the commit message; skip spec files; proceed with implementation.
For everything else — multi-session, multi-file, multi-agent, or ambiguous requirements — spec-first is mandatory.
```

---

### ✅ OPP-10-02: Exploration-first pattern — vibe-then-spec handoff
**Target:** `.claire/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.2 — When SDD Is Overhead (Exploratory Context) / S10.2.2 — Cultural Resistance
**Rationale:** The current workflow has no guidance for the common scenario where requirements are genuinely unknown. Forcing a spec before exploring produces specs that are premature or wrong. The SDD research recommends an explicit "explore first, formalize once direction is clear" pattern. Without this, Claude Code will either write a bad spec upfront or stall waiting for clarity that only emerges from exploration.
**Suggested content/change:** Add a "Discovery mode" section under Rule 1:

```
### Discovery mode (when requirements are unknown)
When the goal is to discover *what* to build rather than implement a known design:
1. Use vibe coding to explore 2–3 approaches (time-box: one session)
2. When one approach is chosen, STOP and write the spec for that approach
3. Then proceed with spec-first workflow as normal

Never write a spec for an approach you have not validated through exploration. A premature spec locks in a wrong design.
```

---

### ✅ OPP-10-03: Spec drift detection — review gate for spec vs. code consistency
**Target:** `.claude/commands/review.md`
**Action:** Add
**Source topic:** S10.2 — Specification Maintenance and Spec Drift
**Rationale:** The current review.md checklist covers build quality, architecture, and DevExpress usage, but has no check for spec-code divergence. SDD research identifies spec drift as a primary ROI killer: specs written at feature start become actively misleading by month 2 if not maintained. Adding a drift check to the review command creates a systematic gate to catch this before it accumulates.
**Suggested content/change:** Add to the review checklist:

```
## Spec-Code Consistency
- [ ] Does the implementation match the feature's `design.md`? Check key interfaces, data flow, and validation rules.
- [ ] If implementation diverged from the spec (e.g., different approach chosen, new constraint discovered), has `design.md` been updated to reflect the actual design?
- [ ] If a new edge case was handled that the spec did not cover, has `requirements.md` been updated?
- [ ] Tasks that were completed — are they checked off in `tasks.md`?

Note: Internal refactors that do not change external behavior do NOT require spec updates. Only update specs when behavior or design intent changes.
```

---

### ✅ OPP-10-04: Over-specification guard — thin spec standard
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — The Waterfall Trap: Over-Specification
**Rationale:** The current workflow has no guidance on spec length or detail level. SDD research warns that AI-assisted spec generators produce exhaustive 800+ line specs for simple features, which recreates Waterfall: long planning phases, limited feedback, scope creep from "we might need X." Explicit brevity guidance prevents this anti-pattern in Claude Code's spec writing.
**Suggested content/change:** Add a "Spec length guideline" note to the spec structure table in Rule 1:

```
### Spec length guideline
- **requirements.md**: 1 page. User stories + acceptance criteria + explicit out-of-scope list.
- **design.md**: 1–2 pages. Architecture, interfaces, key decisions. Not pseudo-code.
- **tasks.md**: One task per ~4–8 hours of work. Not one per line of code.

Guard rail: if writing the spec takes longer than the feature would take to implement, you have over-specified. Stop and trim.
Danger signal: "we might need X in the future" appearing in a spec — remove it. Specs describe what is being built now.
```

---

### ✅ OPP-10-05: Spec-Anchored maintenance rule — when to update specs
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — Specification Maintenance and Spec Drift
**Rationale:** No current rule covers when specs should be updated vs. left as-is after implementation. Without guidance, either all spec updates are skipped (specs become stale) or every tiny change triggers a spec update (overhead). The SDD "Spec-Anchored with loose maintenance" pattern draws a clear line: update specs when external behavior changes; skip updates for internal-only refactors.
**Suggested content/change:** Add under Rule 4 (Tasks.md is the source of truth):

```
### When to update specs after implementation
Update design.md or requirements.md when:
- A chosen approach differs from what was documented in design.md
- A new business rule or constraint was discovered during implementation
- An edge case was handled that requirements.md did not mention
- The public interface (method signatures, DTO shape) changed from the spec

Do NOT update specs for:
- Internal refactors that do not change external behavior
- Performance optimizations that preserve the same contract
- Code style or naming changes within a module

Rule: if the spec would mislead the *next* agent reading it, update it. If the spec still accurately describes *what* the feature does (not how), leave it.
```

---

### ✅ OPP-10-06: Brownfield rule — spec-first for new code only, not retroactive
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.1 — Brownfield Retrofit Difficulty / S10.1.5 — Brownfield Retrofit: The Hard Case
**Rationale:** MyVocaList already has Venue CRUD implemented without formal specs. When future features touch existing code, there is no guidance on whether to retroactively spec the touched code. SDD research is unambiguous: spec only the *new feature or change*, not the entire module. This prevents wasted effort and prevents agents from attempting to reverse-engineer specs from existing code.
**Suggested content/change:** Add a note to Rule 1:

```
### Brownfield rule: spec what you are building, not what already exists
When a new feature touches existing code that has no spec:
- Write the spec for the NEW FEATURE only — not for the existing code it integrates with
- Describe the change: what new behavior is added, how it integrates at the boundary
- Do NOT retroactively spec the existing code (VenueService, VenueRepository, etc.) unless you are explicitly changing its behavior
- When you DO change existing behavior: add a spec section describing the change and update the relevant `design.md`

This is the Spec-First-for-New-Features pattern. Coverage grows organically as features are added; legacy code remains unspecified until actively changed.
```

---

### ✅ OPP-10-07: Spec skip for bug fixes — commit message as the spec
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.2 — When SDD Is Overhead / S10.2 — Overhead on Small Tasks
**Rationale:** The current workflow has no explicit guidance for bug fixes. SDD research shows that spec overhead on bug fixes (where root cause is known and scope is clear) is 50–100% of implementation time — clearly net-negative. Giving Claude Code a "commit message is the spec" pattern for bug fixes prevents ceremony waste while preserving traceability.
**Suggested content/change:** Add to Rule 3 (Commit after every task):

```
### Bug fixes: commit message is the spec
For bug fixes where root cause is documented and scope is contained:
- No spec file required
- Document in the commit message: (1) root cause, (2) fix applied, (3) how to verify
- Format: "fix: [symptom] — root cause: [cause] — fix: [approach] — verified by: [test or manual step]"
- If the bug reveals a missing requirement, ADD that requirement to the relevant `requirements.md` — the bug fix itself still does not need a spec file
```

---

### ✅ OPP-10-08: Subagent spec delegation constraint — specs must be pre-approved
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — Specification Writing Skill Floor / S10.2.2 — Cultural Resistance
**Rationale:** SDD research identifies specification writing as requiring higher skill than code writing — it requires anticipating edge cases, understanding architectural constraints, and abstracting from implementation. The current workflow delegates implementation to subagents but has no explicit rule about who writes the spec. Subagents writing their own specs risks over-specification, under-specification, or wrong architectural choices. The spec must be written and approved by the main agent (or Helder) before subagents are dispatched.
**Suggested content/change:** Add a constraint to Rule 2 (Subagent Delegation):

```
### Spec ownership constraint
Subagents execute against specs — they do not write specs.
- Spec writing (requirements.md, design.md, tasks.md) is always the main agent's responsibility, reviewed by Helder before any subagent is dispatched.
- A subagent that discovers a spec gap (missing requirement, ambiguous design decision) must set status to `blocked: spec gap` and stop. It does not write or modify the spec unilaterally.
- The main agent resolves the gap, updates the spec, gets Helder's approval, then re-dispatches.
```

---

## New Opportunities

### 🆕 OPP-10-09: ROI timeline awareness — document the J-Curve for context
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2.1 — Adoption ROI Timeline / The Four-Week Wall
**Gap in current setup:** The current workflow.md enforces spec discipline but says nothing about why the first few weeks may feel slower. S10.2.1 documents a consistent industry pattern: productivity dips 10–20% in weeks 1–2, recovers by week 4–5, then inflects upward from week 9+. Without this framing, the main agent (or Helder) may misinterpret early friction as a sign the process is broken and abandon spec discipline prematurely. Documenting the expected J-Curve in the workflow rules preserves adoption commitment through the initial dip.
**Suggested content/change:** Add a brief "ROI expectation" note near the top of workflow.md:

```
### Why spec discipline feels slower at first (and why that is correct)
SDD imposes upfront overhead that pays back over weeks, not days:
- Weeks 1–2: –10–20% perceived velocity (spec writing is new overhead)
- Weeks 3–5: recovery to baseline (spec writing becomes routine)
- Month 2–3: +15–30% above baseline (rework decreases; subagent iterations decrease)
- Month 4+: +30–50% sustained (accumulated specs reduce onboarding and per-feature ambiguity)

If spec writing feels slow today, that is expected. The break-even for MyVocaList (solo developer, mid-complexity brownfield) is approximately 8–12 weeks after the first spec is written for a new feature. Do not reduce spec discipline under deadline pressure — that is precisely when the compounding benefit is being earned.
```

---

### 🆕 OPP-10-10: Decision framework table — when to apply SDD vs. skip it
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.6 — Decision Framework: Will SDD Pay for Itself? / S10.1.8 — Size vs. Complexity Trade-Off
**Gap in current setup:** The existing OPP-10-01 addresses the bypass rule for obvious cases (single-file, bug fix). However, there is a large middle ground — medium-complexity tasks, multi-file refactors, or ambiguous new features — where no decision framework exists. S10.1 provides an 8-question decision table that can be operationalized. Adding it to workflow.md gives the main agent a rapid assessment tool before deciding whether to invoke the spec workflow or skip it.
**Suggested content/change:** Add a "SDD decision table" to Rule 1, after the bypass rule:

```
### SDD decision table (for medium-complexity tasks)
Answer each question; count Yes answers:

| Question | Answer |
|---|---|
| Work spans more than one session? | Yes / No |
| More than one file or layer involved? | Yes / No |
| Another feature or agent depends on this output? | Yes / No |
| Requirements are not fully clear upfront? | Yes / No |
| Code will need maintenance beyond this sprint? | Yes / No |
| Integration with Domain, Contracts, or Services interfaces? | Yes / No |

**4+ Yes:** Full spec workflow required (requirements.md + design.md + tasks.md).
**2–3 Yes:** Lightweight spec — design.md only (one page, key decisions + interface); no requirements.md.
**0–1 Yes:** Skip spec; document in commit message.
```

---

### 🆕 OPP-10-11: Spec quality floor — minimum content for a valid spec
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — Specification Writing Skill Floor / S10.2.1 — Specification quality as ROI accelerator
**Gap in current setup:** OPP-10-04 adds brevity guidance (thin specs). The complementary need is a *minimum content floor*: what must a spec contain to be useful? S10.2.1 documents that "teams that write minimal, shallow specs see ROI in weeks 10–12" (vs. weeks 6–8 for focused, unambiguous specs). The project has no definition of "adequate spec" — only structural guidance (three files). Adding a minimum content checklist gives subagents and the main agent a quality gate before a spec is considered ready to implement against.
**Suggested content/change:** Add a "spec completeness checklist" to Rule 1, alongside the spec structure table:

```
### Spec completeness checklist (before implementation begins)
A spec is ready to implement against when it answers all of these:

**requirements.md must cover:**
- [ ] What the user can do (user story per scenario)
- [ ] What success looks like (acceptance criteria, ideally Given/When/Then)
- [ ] What is explicitly OUT of scope (prevents scope creep in implementation)
- [ ] Validation rules (field constraints, business rule boundaries)

**design.md must cover:**
- [ ] Which layers are affected (Domain / Contracts / Infra / Services / MAUI)
- [ ] New or changed interfaces (method signatures, DTO shapes)
- [ ] Key architectural decision and why (e.g., "queue position uses ordinal, not timestamp, because X")
- [ ] Integration points with existing code (what existing service/repo this depends on)

**tasks.md must cover:**
- [ ] Tasks ordered by layer (Domain → Infra → Services → ViewModel → Page)
- [ ] Each task independently committable (build passes after each)
- [ ] No task exceeds ~8 hours of work

If any item is missing, the spec is not ready. Fill the gap before dispatching a subagent.
```

---

### 🆕 OPP-10-12: Constitution alignment — CLAUDE.md as the brownfield constitution
**Target:** `CLAUDE.md`
**Action:** Update
**Source topic:** S10.1.1 — Pattern 2: Constitution Before Specs / S10.1.5 — Constitution captures existing patterns before specs extend them
**Gap in current setup:** S10.1.1 and S10.1.5 describe the "Constitution Before Specs" pattern for brownfield adoption as the enterprise best practice. The constitution documents actual codebase conventions so specs can extend them rather than conflict with them. MyVocaList's CLAUDE.md already functions as a constitution — it documents naming patterns, architectural constraints, DI conventions, and error handling idioms. However, CLAUDE.md does not explicitly declare itself as the project's constitutional document. This implicit role means subagents may not treat it as the authoritative source of conventions and may introduce conflicting patterns in specs they encounter.
**Suggested content/change:** Add a brief declaration to the top of CLAUDE.md (or the Architecture section):

```
## Constitutional Role
CLAUDE.md is this project's constitutional document for SDD purposes. Before writing any spec, verify that the proposed design is consistent with the conventions documented here:
- Architecture constraints (layer dependencies)
- Naming conventions (entities, services, ViewModels, commands)
- DI registration rules (Singleton / Scoped / Transient)
- Error handling idioms (tuple returns, no exceptions for business failures)
- UI component priority (DevExpress first)

A spec that conflicts with CLAUDE.md conventions is invalid regardless of how correct it appears in isolation. Resolve the conflict with Helder before proceeding.
```

---

### 🆕 OPP-10-13: Spec markdown fidelity warning — implementation review is mandatory
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — The Markdown Problem: Lossy Summarization
**Gap in current setup:** S10.2 documents an honest limitation of SDD: markdown specs are lossy. Conversational refinement yields deep understanding; markdown summarization loses nuance. The research finding: "agents implementing from the markdown do 80–90% of what the spec intended." The 10–20% gap is not visible until QA or production. The current workflow has no explicit instruction to review generated code against *intent* (not just against spec text). Subagents are told to build and commit — but not to explicitly verify that the spec's *intent* was captured, not just its literal text.
**Suggested content/change:** Add to Rule 2's subagent exit checklist:

```
### Intent verification (before marking task To Review)
Markdown specs capture 80–90% of intent. The remaining 10–20% is lost in summarization. Before marking a task "To Review":
- Re-read the relevant spec section
- Ask: does the implementation match the *intent* of the spec, or just the literal text?
- Common gaps: edge cases mentioned in passing but not spelled out; error handling described in prose but not implemented; integration contract implied but not tested
- If a gap is found: implement it and note it in the task-log
- If the gap reveals a spec ambiguity: set status to `blocked: spec gap` and stop

This check costs 5 minutes. Missing it costs hours in the next QA cycle.
```

---

### 🆕 OPP-10-14: Three-month wall awareness — document when MyVocaList hits it
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S10.2 — The Three-Month Wall: When Vibe Coding Hits a Ceiling / S10.1 — 10–20 file boundary
**Gap in current setup:** MyVocaList is already past the "three-month wall" threshold: it has more than 10–20 interdependent files and the main features (Venue CRUD, queue management) involve multiple layers. S10.2 documents that at this complexity level, vibe coding is actively slowing delivery — the wall has already been hit. CLAUDE.md has no statement of this reality, which means the agent may not prioritize spec discipline with the urgency it deserves. Explicitly noting that MyVocaList is in the "spec ROI positive" zone establishes the rationale for maintaining spec discipline even when it feels like overhead.
**Suggested content/change:** Add to CLAUDE.md's Development Workflow section or as a preamble to the Commands section:

```
## SDD Applicability for MyVocaList
MyVocaList is past the 10–20 interdependent file threshold where SDD becomes strictly beneficial:
- Multiple layers (Domain, Infra, Services, MAUI) interact on every feature
- Features span multiple sessions and require context persistence across resets
- Queue management logic has business rule complexity where hallucination cost is high

This means:
- Spec-first is not optional overhead — it is the mechanism that prevents compounding technical debt
- Vibe coding on new features increases total delivery time beyond 3 months due to rework
- The ROI on specs for MyVocaList is currently positive; skipping specs costs more than writing them

Exception: Bug fixes, cosmetic changes, and one-off scripts remain spec-exempt (see workflow.md bypass rule).
```

---

### 🆕 OPP-10-15: Tacit knowledge capture — rationale comments in design.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2.2 — Tacit Knowledge Resistance / S10.2 — Implicit knowledge leaks into code comments
**Gap in current setup:** S10.2.2 identifies a critical SDD failure mode: design decisions made during implementation end up in code comments rather than in the spec. Future agents do not find them in the spec, re-examine the decision, and may reverse it — reintroducing a bug or wrong design that was resolved before. S10.2 specifically names this: "A decision made during planning ('use ULID instead of UUID for better cache locality') ends up in code comments, not in the spec." The current workflow has no rule requiring architectural decisions made *during implementation* to be retroactively captured in design.md.
**Suggested content/change:** Add a note to Rule 1's spec update guidance (or alongside OPP-10-05's "when to update specs"):

```
### Capture architectural decisions in design.md, not code comments
When an implementation decision is made that a future agent could reasonably second-guess:
- Do NOT put it only in a code comment
- ADD it to the relevant `design.md` under a "Key Decisions" section
- Format: "**Decision:** [what was chosen] — **Reason:** [why] — **Alternative considered:** [what was rejected and why]"

Examples of decisions that belong in design.md, not code comments:
- Why a specific EF Core query pattern was chosen (performance, collation requirement)
- Why queue ordering uses ordinal position rather than timestamp
- Why a DTO field is nullable vs. required
- Why a service method returns a tuple vs. throwing

Code comments are for implementation-level notes. design.md is for decisions a future architect needs to understand.
```
