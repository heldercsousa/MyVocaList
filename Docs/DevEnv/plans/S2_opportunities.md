# S2 — Specification Design: Enhancement Opportunities

> Source files analyzed: S2_Specification_Design.md, S2_1_Spec_Structure_and_Content.md, S2_1_1_Tacit_Knowledge_Capture.md, S2_1_2_Over_Specification_Risk.md, S2_2_Quality_Characteristics.md, S2_2_1_Acceptance_Criteria_Subjectivity.md, S2_2_2_Verbosity_vs_Precision_Tension.md, S2_3_Functional_vs_Technical_Separation.md, S2_3_1_Spec_Format_Selection.md
> Compared against: CLAUDE.md, .claude/rules/workflow.md, .claude/rules/testing.md, .claude/rules/code-principles.md, .claude/settings.json
> Last reviewed: 2026-05-06

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, still unimplemented) | 18 |
| 🆕 New (not previously captured) | 4 |
| **Total** | **22** |

All 18 previously captured opportunities (OPP-2-1 through OPP-2-18) remain unimplemented — confirmed by inspecting workflow.md and review.md as of 2026-05-06. None have been superseded. Four new opportunities were identified from a full reading of all nine S2 source files.

---

## Previously Captured Opportunities

---

### ✅ OPP-2-1: Add spec completeness checklist to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1 — Spec Structure & Content
**Rationale:** The current workflow.md Rule 1 says "read design.md before any code" but gives no guidance on whether a spec is *ready* for implementation. Agents and Helder currently have no checklist to verify spec completeness before a subagent is dispatched. S2.1's seven-element completeness check (Inputs, Outputs, Preconditions, Postconditions, Invariants, Integration Contracts, State Machines, Edge Cases) would give a concrete gate before implementation starts — preventing dispatching subagents against incomplete specs that will produce misaligned code.
**Suggested content/change:** Add a "Spec Readiness Check" under Rule 1. It should be a brief inline checklist (not the full S2.1 detail) covering the elements most commonly missing in MyVocaList specs: validation rules explicit (not just "validate input"), error paths named (not "handle errors"), state transitions for multi-step features, and at least one edge case per category (null/empty, duplicate, permission). The gate is: if any element is missing or vague, refine the spec before dispatching a subagent.

---

### ✅ OPP-2-2: Add Given/When/Then format requirement to spec authoring guidance
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.2 — Quality Characteristics
**Rationale:** The current spec structure (requirements.md + design.md + tasks.md) is documented but the *format* of acceptance criteria in requirements.md is unspecified. Agents writing or reviewing specs have no format standard. S2.2 establishes Given/When/Then as the format that forces explicit preconditions, prevents vague Then assertions ("handles gracefully"), and maps directly to test cases. This is especially relevant for MyVocaList because the testing.md already mandates `{Method}_{Context}_{Expected}` naming — Given/When/Then acceptance criteria would make those test names derivable from the spec instead of invented at implementation time.
**Suggested content/change:** In Rule 1 (Spec-First), add a note: "Acceptance criteria in requirements.md must follow Given/When/Then format. Given sets up actor state and preconditions; When names a single trigger; Then asserts observable outcomes (UI state, return value, database change). Prose sentences without this structure are not testable and are not acceptable as acceptance criteria."

---

### ✅ OPP-2-3: Add domain glossary requirement to spec structure
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.3 — Functional vs Technical Separation; S2.2 — Ubiquitous Language
**Rationale:** MyVocaList has domain-specific terms (venue, queue entry, round, position, singer) that must stay consistent from requirements.md through design.md through code. Currently there is no rule requiring a domain vocabulary section in any spec file. S2.3 identifies ubiquitous language as a **prerequisite** for both spec layers — without it, agents invent synonyms ("location" for "venue", "song submission" for "queue entry") which then leak into code, breaking the naming conventions in code-principles.md. S2.3.1 reinforces: ubiquitous language must be established before either spec layer is written. A required glossary section in design.md (or requirements.md) for any feature introducing new domain terms would prevent this class of drift.
**Suggested content/change:** Add to the spec structure table in Rule 1: "requirements.md must include a Domain Vocabulary section for any feature introducing entities or domain concepts not already defined in an existing spec. Each term: one-sentence definition, the corresponding C# type name, and any synonyms to avoid." The Venue spec is the reference — it already defines Venue, but future specs (Person, QueueEntry, Round) need the same treatment. Additionally, the design.md for any feature using existing entities must re-state the canonical term mapping to prevent subagent synonyms.

---

### ✅ OPP-2-4: Add explicit "Not Included" section requirement to requirements.md
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1.2 — Over-Specification Risk; S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.1.2 identifies "Missing Constraints (Negative Space)" as a critical anti-pattern: agents assume the general case and add unrequested features, logging, abstractions, or scope. In MyVocaList context this is concrete — a subagent implementing Person CRUD without an explicit "out of scope" section might add photo upload, search-by-attributes, or audit history because those are "reasonable" features for a person entity. The current spec structure has no enforced "not included" or "out of scope" slot. S2.1.2 shows this is not a nice-to-have: it directly prevents agent scope creep.
**Suggested content/change:** Add to the spec structure table in Rule 1 a mandatory "Out of Scope" section in requirements.md. Minimum 2–3 explicit exclusions per feature. Example: "Out of scope: photo upload, deletion of persons with active queue entries (deferred), admin audit log of person changes." Subagent briefings should explicitly reference this section.

---

### ✅ OPP-2-5: Add verification gates concept to workflow.md task completion
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.2.1 — Acceptance Criteria Subjectivity
**Rationale:** S2.2.1 documents the "done but not done" failure mode: agents satisfy the letter of acceptance criteria while missing functional wiring (the paginated component exists but is never imported, the service method is implemented but not called by the ViewModel). The current workflow Rule 3 says "commit after every task" but has no completion verification protocol beyond "code builds." For MyVocaList subagents this is a real risk — a subagent can create a service, a repository, and a page, all of which build and pass individual tests, but the page is not registered in AppShell and the repository is not wired into DI. S2.2.1's verification gates pattern addresses this directly. Research shows "done but not done" incidents dropped from 60% to under 10% with explicit verification gates.
**Suggested content/change:** Add a "Task Completion Verification" subsection to Rule 3. Verification gates for a standard CRUD feature: (1) `dotnet build` passes; (2) new route registered in AppShell.xaml; (3) DI registrations added to MauiProgram.cs; (4) page reachable via navigation in emulator (manual check). Additionally, add a "Demo Statement" requirement for user-facing features: a plain-English walkthrough of what the user sees when the feature is working. Briefings to subagents should instruct them to verify all gates and write the demo statement before committing.

---

### ✅ OPP-2-6: Add spec staleness prevention rule to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.2.2 identifies spec staleness as the single largest failure mode in production SDD systems. In MyVocaList, specs in `Docs/specs/[feature]/` are written once and never referenced during implementation review. When a subagent makes a pragmatic design deviation (e.g., changes a service method signature discovered during implementation), the spec is not updated. Future subagents for that feature then read a stale spec and produce contradictory code. The fix is simple: update the spec in the same session as any code deviation. This is currently not in any rule file.
**Suggested content/change:** Add Rule 7 — Spec Sync: "If implementation deviates from design.md (changed method signature, renamed entity, added field, changed error message), update design.md in the same commit. Never allow spec and code to diverge silently. A spec that does not reflect the code is misleading — future agents will implement against the stale spec and produce contradictions."

---

### ✅ OPP-2-7: Add tacit knowledge capture protocol to spec authoring guidance
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1.1 — Tacit Knowledge Capture
**Rationale:** S2.1.1 identifies tacit knowledge gaps as the primary source of business-logic errors in AI-generated code. For MyVocaList this is concrete: domain rules about queue round-robin logic, absence handling, and Bandokê vs Mechanical Karaoke mode differences are likely implicit in Helder's head but not in any spec. When a future spec for queue management is written, these implicit rules will not be captured in the first draft. S2.1.1's "Modular Specs with Explicit Gaps" pattern is the pragmatic response: mark known-incomplete areas explicitly rather than letting specs appear falsely complete.
**Suggested content/change:** Add to the spec authoring guidance in Rule 1: "When writing requirements.md, include a 'Known Gaps' section for any business rule that is not yet fully articulated. Format: `**Known gap:** [area] — [what is uncertain or not yet extracted].` This prevents specs from implying false completeness. A subagent that sees a known gap knows to escalate rather than invent a plausible rule."

---

### ✅ OPP-2-8: Add spec size calibration guideline to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.1.2 — Over-Specification Risk; S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.2.2 and S2.1.2 converge on a practical calibration: under 300 lines is usually too terse; 300–1000 lines is the sweet spot for a single feature; over 2000 lines begins hurting agent compliance. MyVocaList currently has no spec length guidance, and a verbose spec author (or an AI agent generating a spec) could produce a 3000-line design.md that degrades subagent performance. Applying the calibration heuristics from S2.1.2 (Minimal / Standard / Comprehensive tiers based on feature complexity) would give Helder a concrete signal when a spec needs trimming or splitting.
**Suggested content/change:** Add a "Spec Calibration" note to Rule 1: "requirements.md + design.md combined should target 300–1000 lines for standard CRUD features. If a feature spec exceeds 1500 lines, consider: (1) Is this two features? (2) Are implementation details leaking into requirements? (3) Are there repeated variations that could be expressed as a single boundary rule? A spec that reads like pseudo-code should be trimmed to intent-level language."

---

### ✅ OPP-2-9: Add spec-vs-code consistency check to review.md
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S2.2.2 — Verbosity vs. Precision Tension; S2.2.1 — Acceptance Criteria Subjectivity
**Rationale:** The current review.md checklist covers build, code quality, MAUI specifics, architecture, and DevExpress. It does not include any check that the implemented code matches the spec. S2.2.2 identifies spec drift as the primary production failure mode; S2.2.1 documents that agents frequently satisfy acceptance criteria letter-but-not-spirit. Adding a spec consistency step to the review command would catch both: compare the implemented interfaces/service signatures against design.md, and verify that acceptance criteria in requirements.md are covered by actual tests or manual verifications.
**Suggested content/change:** Add a "Spec Consistency" section to review.md checklist: (1) Open design.md for the feature — do all interface signatures match what is in code? (2) Open requirements.md — is each acceptance criterion verified by a test or a manual check? (3) Is anything implemented that is explicitly listed as "out of scope"? (4) If code deviated from design.md, was design.md updated?

---

### ✅ OPP-2-10: Document the functional/technical separation rule in workflow.md with MyVocaList examples
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.3 — Functional vs Technical Separation
**Rationale:** S2.3 provides a concrete boundary table for what belongs in requirements.md vs design.md. The current workflow.md mentions the three-file structure but gives no guidance on what each file should contain. Agents writing specs for future features (Person, Song, Queue) may put EF Core indexes in requirements.md or user stories in design.md, producing specs that are hard to review and harder to hand to subagents. S2.3 also identifies the key failure mode that is highly applicable here: "specs written after implementation" produce specs that describe what the code does rather than what the system should do — the Spec-First rule already guards against this, but adding the boundary table makes the intention explicit for spec authors.
**Suggested content/change:** Add a content boundary table to the spec structure section in Rule 1, using MyVocaList examples:

| Question | File | Example |
|----------|------|---------|
| What can the admin accomplish? | requirements.md | "Admin shall register a singer with a unique display name" |
| What constitutes a valid name? | requirements.md | "Name: 1–50 chars, non-empty after trim" |
| Which table stores the singer? | design.md | `Persons` entity with `FullNameNormalized` |
| How is uniqueness enforced? | design.md | Unique index on `FullNameNormalized` |
| Which service method creates a singer? | design.md | `IPersonService.CreatePersonAsync(...)` |

---

### 🆕 OPP-2-11: Add invariants and postconditions section to design.md template
**Target:** `.claude/rules/workflow.md` (spec structure table); reference implementation `Docs/specs/venues/design.md`
**Action:** Update spec template
**Source topic:** S2.1 — Spec Structure & Content (postconditions and invariants element)
**Gap in current setup:** The current design.md has no section for invariants (rules that must never be violated across all states) or postconditions (what must be true after an operation succeeds). These are two of the seven structural elements that S2.1 identifies as required for a complete spec. Without them, subagents may generate code that violates system-wide constraints (e.g., a singer's queue position never goes negative, a venue name is always unique) under edge cases because the constraint was never stated as an invariant.
**Concrete enhancement action:** Add an "Invariants & Postconditions" section to the design.md template. For each service method, specify: (1) what database state changes are required (postcondition), (2) what must remain true regardless of input (invariant). Example for CreatePersonAsync: Postcondition: `Persons` row inserted with `FullNameNormalized` set; Invariant: no two `Person` rows share the same `FullNameNormalized` value. Update the Venues reference spec to include this section as a worked example.

---

### ✅ OPP-2-12: Add state machine documentation requirement for multi-step entities
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1 — Spec Structure & Content (state machines element)
**Gap in current setup:** Queue entries, rounds, and event sessions will have lifecycle states (e.g., a queue entry can be pending → singing → done → skipped). Currently no spec template requires state machine documentation. S2.1 identifies state machines as one of the seven required structural elements, specifically because "agents generate unreachable code paths or allow invalid state transitions" without them. For the upcoming queue management feature, the absence of a state machine spec is high-risk.
**Concrete enhancement action:** Add a "State Machine" section to the design.md template, required for any entity that can change status over its lifetime. Format: states list, valid transitions (A → B under condition X), rejected transitions with error behavior. Mark this section as "N/A" for simple CRUD entities that have no lifecycle states (e.g., Venue, Person). The queue entry lifecycle and round progression are the primary candidates for this.

---

### ✅ OPP-2-13: Establish a regeneration test practice for spec validation
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.1.2 — Over-Specification Risk (regeneration tests for spec quality measurement)
**Gap in current setup:** There is no practice for verifying that a spec is complete enough to regenerate an equivalent implementation. S2.1.2 describes a concrete diagnostic: write a spec, generate code from it, one week later delete the code and regenerate from the same spec, then compare behavioral parity. Each divergence reveals a missing or ambiguous constraint. For MyVocaList this is actionable for the Venue CRUD feature, which already has a complete spec and implementation — it is the ideal reference to validate spec quality and identify which constraints are currently underspecified.
**Concrete enhancement action:** Add a note under Rule 1 or the review command: "For any feature spec that will be reused for regeneration (complex features, core entities), run a regeneration test: regenerate the implementation from the spec in isolation and compare it to the existing implementation for behavioral parity. Divergences in interface signatures, error messages, or edge case handling each identify one missing constraint in the spec. Update the spec with each divergence found." Apply this first to `Docs/specs/venues/` as a calibration exercise.

---

### ✅ OPP-2-14: Add demo statement requirement to tasks.md items for user-facing features
**Target:** `.claude/rules/workflow.md`; `Docs/specs/venues/tasks.md` as reference
**Action:** Update
**Source topic:** S2.2.1 — Acceptance Criteria Subjectivity (demo statements)
**Gap in current setup:** Tasks in tasks.md are currently written as implementation steps ("Create VenueService", "Add VenueRepository", "Wire DI"). They do not describe what the user sees when the task is complete. S2.2.1 introduces the "demo statement" pattern: a plain-English description of the observable user experience when a task is done and working. This anchors both the subagent and the reviewer on the user-visible outcome, not just the implementation checklist. Currently there is no such requirement — subagents can claim completion based on build success alone.
**Concrete enhancement action:** Add a "Demo Statement" field to tasks.md items for user-facing tasks. Format: "Demo: [describe what the user sees when navigating to this feature and performing the key action]." Example: "Demo: Admin taps Add Venue, enters 'Jazz Club', taps Save — venue appears in the list immediately. Tapping an existing venue and submitting the same name shows 'A venue with this name already exists' inline." This is written by Helder during planning and verified by the subagent before committing.

---

### ✅ OPP-2-15: Add failure-mode analysis as a spec quality improvement step to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.1.1 — Tacit Knowledge Capture (failure-mode analysis as tacit knowledge extraction)
**Gap in current setup:** When a subagent produces incorrect behavior, the current workflow logs it as a build failure or blocks with "spec gap" status. But there is no protocol for feeding that failure back into the spec. S2.1.1 identifies failure-mode analysis as the most effective technique for surfacing tacit knowledge: review the failure with the domain expert (Helder), ask "what would be correct here and why," and update the spec with the extracted rule. Without this loop, the same tacit gap will cause the same incorrect behavior in the next implementation cycle.
**Concrete enhancement action:** Add a step to the "blocked: spec gap" recovery flow in Rule 2 or a new Rule 8: "When a subagent returns 'blocked: spec gap' or produces incorrect behavior after implementation: (1) Identify the gap — what rule was not stated in the spec? (2) Articulate the correct rule explicitly (not just fix the code). (3) Update requirements.md or design.md with the extracted rule before re-dispatching. (4) If the same tacit gap appears twice, add a 'Known Gaps' annotation for similar rules in adjacent spec sections." This turns each failure into a spec improvement.

---

### ✅ OPP-2-16: Add EARS format as the target acceptance criteria format for requirements.md
**Target:** `.claude/rules/workflow.md`
**Action:** Update (evolves OPP-2-2, adds format specificity)
**Source topic:** S2.3.1 — Spec Format Selection (EARS as the recommended structured format)
**Gap in current setup:** OPP-2-2 established Given/When/Then as the target format for acceptance criteria, but did not specify whether to use free-form Markdown or the EARS structured syntax. S2.3.1 documents EARS (WHEN/IF/WHILE/WHERE + SHALL) as the dominant industry standard for requirements that must be parseable by agents and traceable to tests. MyVocaList is currently at "free-form narrative with implicit EARS" per the S2.3.1 codebase alignment note. Given that the testing.md format (`{Method}_{Context}_{Expected}`) already implies structured thinking, formalizing EARS would close the gap between spec and test authoring.
**Concrete enhancement action:** Amend OPP-2-2's guidance: acceptance criteria should use EARS keywords (WHEN for event triggers, IF for state conditions, SHALL for binding requirements) alongside Given/When/Then scenario blocks. The transition is incremental — apply EARS structure to new features starting with the next spec after Person CRUD. Do not retroactively rewrite the Venues spec. Reference the S2.3.1 MyVocaList alignment note as the evolution path.

---

### ✅ OPP-2-17: Add two-tier spec architecture trigger to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.2.2 — Verbosity vs. Precision Tension (two-tier spec architecture)
**Gap in current setup:** For complex features (queue management, event session lifecycle, multi-singer round progression), a single design.md may grow beyond the 1000-line threshold that begins degrading agent compliance. S2.2.2 documents the two-tier pattern: a human-readable main spec (10–20 pages of intent) plus modular machine-consumable appendices (database schemas, API contracts, algorithm specs). Currently workflow.md has no trigger condition or guidance for when to apply this pattern. A queue management spec written as a single design.md could easily reach 2000+ lines and cause subagents to miss edge cases buried in the middle.
**Concrete enhancement action:** Add a note to the "Spec Calibration" guidance (OPP-2-8): "When a feature spec is projected to exceed 1500 lines, split into a two-tier structure: `design.md` (intent, architecture decisions, interface signatures — target 400–800 lines) + `design-appendix-*.md` files (one per subsystem: schema, state machine, integration contract). Subagent briefings reference only the appendix relevant to their task, keeping their context budget free for codebase exploration."

---

### ✅ OPP-2-18: Add integration contract section to design.md template for features with external dependencies
**Target:** `.claude/rules/workflow.md`; spec template
**Action:** Update
**Source topic:** S2.1 — Spec Structure & Content (integration contracts element)
**Gap in current setup:** MyVocaList currently has no external service integrations, but planned features include lyrics API, song catalog, and potentially social features. The design.md template has no slot for integration contracts (external service calls, failure modes, retry semantics, idempotency). S2.1 identifies integration contracts as one of the seven required structural elements: "without them, agents may make unsafe assumptions (assuming all calls succeed, or not handling eventual consistency)." Adding this section to the template now costs nothing; omitting it when the first HTTP-dependent feature is specced will produce a subagent that silently swallows network failures.
**Concrete enhancement action:** Add an "Integration Contracts" section to the design.md template, marked as "N/A — no external dependencies" for features without them. For features with HTTP calls or event-driven dependencies, require: external service name, input/output schema, failure behavior (4xx vs 5xx handling), retry strategy, idempotency key (if applicable). This section becomes mandatory when any feature in CLAUDE.md's "Planned" column (MediatR, FluentValidation, lyrics API) is specced.

---

## New Opportunities

---

### 🆕 OPP-2-19: Add spec quality four-gate review to the spec authoring phase
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.2 — Quality Characteristics (Quality Gates section)
**Gap in current setup:** S2.2 defines four quality checks that should be applied to a spec before handing it to any agent: (1) domain language check — every business term maps to a code-level identifier with no synonyms; (2) Given/When/Then audit — every acceptance criterion follows the structure with no vague Then assertions; (3) completeness audit — for each design decision, ask "Is the critical path covered? Are error cases explicit?"; (4) determinism audit — scan for "should," "might," "typically," "reasonable," and replace with measurable properties. Currently workflow.md has no checklist at spec-review time. OPP-2-1 adds a readiness gate before dispatching, but that is later in the workflow. This opportunity adds an earlier gate — during spec writing/review — that prevents a spec from reaching the dispatch stage incomplete.
**Concrete enhancement action:** Add a "Spec Quality Gate" subsection to Rule 1 (after the spec structure table), structured as four checkboxes that Helder applies when reviewing a spec before approving it for planning:
```
- [ ] Domain language: every business term in requirements.md maps to a C# type name — no synonyms introduced
- [ ] Given/When/Then: every acceptance criterion uses the structure — no vague Then assertions ("handles gracefully", "works correctly")
- [ ] Completeness: every branch with a different outcome has a scenario — critical path, duplicate, permission, and null/empty cases are explicit
- [ ] Determinism: all quality attributes are measurable — no "fast", "robust", "user-friendly" without a concrete threshold
```
A spec that fails any of the four checks is revised before the plan is written.

---

### 🆕 OPP-2-20: Add acceptance criteria traceability to testing.md
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S2.2 — Quality Characteristics ("Link each scenario to a test before implementation begins. Traceability established upfront prevents spec/test drift.")
**Gap in current setup:** testing.md defines test naming (`{Method}_{Context}_{Expected}`) and test categories (unit/integration/ViewModel) but has no requirement to link each acceptance criterion in requirements.md to a corresponding test before implementation begins. S2.2 is explicit that traceability must be established before implementation, not after. Without it, there is no way to know at review time whether all acceptance criteria have test coverage, and test names invented during implementation may not map back to any spec scenario. This gap is especially acute because testing.md and workflow.md are maintained separately — neither currently requires the cross-file link.
**Concrete enhancement action:** Add a "Traceability" rule to testing.md: "Before implementation begins, each Given/When/Then scenario in requirements.md must have a corresponding test method stub in the test file. The test method name (`{Method}_{Context}_{Expected}`) must be derivable from the scenario. Example: scenario 'Given a duplicate venue name, When the user submits, Then an error is returned' maps to `CreateVenueAsync_DuplicateName_ReturnsFalseWithMessage`. This pre-declaration prevents both missed coverage and test-naming drift." Also add to the pre-implementation checklist in testing.md: "Confirm all spec scenarios have a named test stub before writing any implementation code."

---

### 🆕 OPP-2-21: Add determinism rule for quality attributes to code-principles.md
**Target:** `.claude/rules/code-principles.md`
**Action:** Add
**Source topic:** S2.2 — Quality Characteristics (Clarity and Determinism); S2.2.1 — Acceptance Criteria Subjectivity
**Gap in current setup:** S2.2 identifies vague language as the primary driver of non-deterministic agent behavior. The terms "fast," "robust," "handles gracefully," "user-friendly," and "should be secure" appear nowhere in current code-principles.md, testing.md, or workflow.md as prohibited language. An agent generating a spec, or a human writing acceptance criteria, has no rule prohibiting these terms. S2.2 and S2.2.1 both show that these terms force agents to invent their own thresholds, producing code that is technically compliant but misaligned with intent. code-principles.md is the right home for a general language rule; it already enforces English-only, nullable discipline, and naming standards — determinism belongs alongside those.
**Concrete enhancement action:** Add a "Spec Language — Determinism" section to code-principles.md: "In spec files (requirements.md, design.md) and in task descriptions, vague quality adjectives are prohibited. Prohibited terms: fast, slow, quick, responsive, robust, secure, user-friendly, intuitive, handles gracefully, works correctly, performs well, reasonable. Replace with measurable thresholds: 'list renders within 300ms on mid-range Android', 'returns HTTP 400 with a JSON body `{error, message}`', 'password hashed with bcrypt 12 rounds'. If the threshold is not yet known, write: `[threshold TBD — establish before implementation starts]`."

---

### 🆕 OPP-2-22: Add LLM-assisted tacit knowledge extraction technique to spec authoring guidance
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.1.1 — Tacit Knowledge Capture (LLM-Assisted Extraction with Validation strategy)
**Gap in current setup:** OPP-2-7 adds a "Known Gaps" annotation pattern to mark where tacit knowledge is incomplete. But it does not provide a technique for *extracting* that knowledge before the gap causes a failure. S2.1.1 documents LLM-Assisted Extraction as a practical approach: use Claude to draft structured rules from existing code, comments, commit messages, and prior task-logs; then have Helder validate and refine. S2.1.1 notes this yields 70–80% accuracy for straightforward rules, reduces extraction effort from 40–200 hours (expert-only) to 6–16 hours (LLM draft + expert review), and is particularly effective when the LLM can review both the existing spec and the implemented code to surface what the code does that the spec does not say. MyVocaList already has Claude Code in-session — this technique requires no tooling investment.
**Concrete enhancement action:** Add a note under Rule 1 spec authoring guidance: "Before writing requirements.md for a domain-heavy feature (queue management, round progression, singer absence handling), run a tacit knowledge extraction pass: ask Claude Code to read the existing codebase for related patterns and propose a list of implicit rules it observes (e.g., 'The code checks X before doing Y — is that a business rule?'). Helder reviews each proposed rule as correct, incorrect, or incomplete. Validated rules are added to requirements.md explicitly. Rejected rules are noted as 'Not a rule — [correct behavior].' This surfaces assumptions that would otherwise only appear as bugs."
