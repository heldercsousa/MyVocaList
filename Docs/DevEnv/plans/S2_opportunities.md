# S2 — Specification Design: Enhancement Opportunities

> Analysis against `.claude` current state. Only opportunities with genuine value for MyVocaList are included.

---

### OPP-2-1: Add spec completeness checklist to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1 — Spec Structure & Content
**Rationale:** The current workflow.md Rule 1 says "read design.md before any code" but gives no guidance on whether a spec is *ready* for implementation. Agents and Helder currently have no checklist to verify spec completeness before a subagent is dispatched. S2.1's seven-element completeness check (Inputs, Outputs, Preconditions, Postconditions, Invariants, Integration Contracts, State Machines, Edge Cases) would give a concrete gate before implementation starts — preventing dispatching subagents against incomplete specs that will produce misaligned code.
**Suggested content/change:** Add a "Spec Readiness Check" under Rule 1. It should be a brief inline checklist (not the full S2.1 detail) covering the elements most commonly missing in MyVocaList specs: validation rules explicit (not just "validate input"), error paths named (not "handle errors"), state transitions for multi-step features, and at least one edge case per category (null/empty, duplicate, permission). The gate is: if any element is missing or vague, refine the spec before dispatching a subagent.

---

### OPP-2-2: Add Given/When/Then format requirement to spec authoring guidance
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.2 — Quality Characteristics
**Rationale:** The current spec structure (requirements.md + design.md + tasks.md) is documented but the *format* of acceptance criteria in requirements.md is unspecified. Agents writing or reviewing specs have no format standard. S2.2 establishes Given/When/Then as the format that forces explicit preconditions, prevents vague Then assertions ("handles gracefully"), and maps directly to test cases. This is especially relevant for MyVocaList because the testing.md already mandates `{Method}_{Context}_{Expected}` naming — Given/When/Then acceptance criteria would make those test names derivable from the spec instead of invented at implementation time.
**Suggested content/change:** In Rule 1 (Spec-First), add a note: "Acceptance criteria in requirements.md must follow Given/When/Then format. Given sets up actor state and preconditions; When names a single trigger; Then asserts observable outcomes (UI state, return value, database change). Prose sentences without this structure are not testable and are not acceptable as acceptance criteria."

---

### OPP-2-3: Add domain glossary requirement to spec structure
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.3 — Functional vs Technical Separation; S2.2 — Ubiquitous Language
**Rationale:** MyVocaList has domain-specific terms (venue, queue entry, round, position, singer) that must stay consistent from requirements.md through design.md through code. Currently there is no rule requiring a domain vocabulary section in any spec file. S2.3 identifies ubiquitous language as a prerequisite for both spec layers — without it, agents invent synonyms ("location" for "venue", "song submission" for "queue entry") which then leak into code, breaking the naming conventions in code-principles.md. A required glossary section in design.md (or requirements.md) for any feature introducing new domain terms would prevent this class of drift.
**Suggested content/change:** Add to the spec structure table in Rule 1: "requirements.md must include a Domain Vocabulary section for any feature introducing entities or domain concepts not already defined in an existing spec. Each term: one-sentence definition, the corresponding C# type name, and any synonyms to avoid." The Venue spec is the reference — it already defines Venue, but future specs (Person, QueueEntry, Round) need the same treatment.

---

### OPP-2-4: Add explicit "Not Included" section requirement to requirements.md
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1.2 — Over-Specification Risk; S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.1.2 identifies "Missing Constraints (Negative Space)" as a critical anti-pattern: agents assume the general case and add unrequested features, logging, abstractions, or scope. In MyVocaList context this is concrete — a subagent implementing Person CRUD without an explicit "out of scope" section might add photo upload, search-by-attributes, or audit history because those are "reasonable" features for a person entity. The current spec structure has no enforced "not included" or "out of scope" slot. S2.1.2 shows this is not a nice-to-have: it directly prevents agent scope creep.
**Suggested content/change:** Add to the spec structure table in Rule 1 a mandatory "Out of Scope" section in requirements.md. Minimum 2–3 explicit exclusions per feature. Example: "Out of scope: photo upload, deletion of persons with active queue entries (deferred), admin audit log of person changes." Subagent briefings should explicitly reference this section.

---

### OPP-2-5: Add verification gates concept to workflow.md task completion
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.2.1 — Acceptance Criteria Subjectivity
**Rationale:** S2.2.1 documents the "done but not done" failure mode: agents satisfy the letter of acceptance criteria while missing functional wiring (the paginated component exists but is never imported, the service method is implemented but not called by the ViewModel). The current workflow Rule 3 says "commit after every task" but has no completion verification protocol beyond "code builds." For MyVocaList subagents this is a real risk — a subagent can create a service, a repository, and a page, all of which build and pass individual tests, but the page is not registered in AppShell and the repository is not wired into DI. S2.2.1's verification gates pattern addresses this directly.
**Suggested content/change:** Add a "Task Completion Verification" subsection to Rule 3. Verification gates for a standard CRUD feature: (1) `dotnet build` passes; (2) new route registered in AppShell.xaml; (3) DI registrations added to MauiProgram.cs; (4) page reachable via navigation in emulator (manual check). Briefings to subagents should instruct them to verify all four before committing. This does not require new tooling — it is a checklist the subagent must satisfy.

---

### OPP-2-6: Add spec staleness prevention rule to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.2.2 identifies spec staleness as the single largest failure mode in production SDD systems. In MyVocaList, specs in `Docs/specs/[feature]/` are written once and never referenced during implementation review. When a subagent makes a pragmatic design deviation (e.g., changes a service method signature discovered during implementation), the spec is not updated. Future subagents for that feature then read a stale spec and produce contradictory code. The fix is simple: update the spec in the same session as any code deviation. This is currently not in any rule file.
**Suggested content/change:** Add Rule 7 — Spec Sync: "If implementation deviates from design.md (changed method signature, renamed entity, added field, changed error message), update design.md in the same commit. Never allow spec and code to diverge silently. A spec that does not reflect the code is misleading — future agents will implement against the stale spec and produce contradictions."

---

### OPP-2-7: Add tacit knowledge capture protocol to spec authoring guidance
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.1.1 — Tacit Knowledge Capture
**Rationale:** S2.1.1 identifies tacit knowledge gaps as the primary source of business-logic errors in AI-generated code. For MyVocaList this is concrete: domain rules about queue round-robin logic, absence handling, and Bandokê vs Mechanical Karaoke mode differences are likely implicit in Helder's head but not in any spec. When a future spec for queue management is written, these implicit rules will not be captured in the first draft. S2.1.1's "Modular Specs with Explicit Gaps" pattern is the pragmatic response: mark known-incomplete areas explicitly rather than letting specs appear falsely complete.
**Suggested content/change:** Add to the spec authoring guidance in Rule 1: "When writing requirements.md, include a 'Known Gaps' section for any business rule that is not yet fully articulated. Format: `**Known gap:** [area] — [what is uncertain or not yet extracted].` This prevents specs from implying false completeness. A subagent that sees a known gap knows to escalate rather than invent a plausible rule."

---

### OPP-2-8: Add spec size calibration guideline to workflow.md
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S2.1.2 — Over-Specification Risk; S2.2.2 — Verbosity vs. Precision Tension
**Rationale:** S2.2.2 and S2.1.2 converge on a practical calibration: under 300 lines is usually too terse; 300–1000 lines is the sweet spot for a single feature; over 2000 lines begins hurting agent compliance. MyVocaList currently has no spec length guidance, and a verbose spec author (or an AI agent generating a spec) could produce a 3000-line design.md that degrades subagent performance. Applying the calibration heuristics from S2.1.2 (Minimal / Standard / Comprehensive tiers based on feature complexity) would give Helder a concrete signal when a spec needs trimming or splitting.
**Suggested content/change:** Add a "Spec Calibration" note to Rule 1: "requirements.md + design.md combined should target 300–1000 lines for standard CRUD features. If a feature spec exceeds 1500 lines, consider: (1) Is this two features? (2) Are implementation details leaking into requirements? (3) Are there repeated variations that could be expressed as a single boundary rule? A spec that reads like pseudo-code should be trimmed to intent-level language."

---

### OPP-2-9: Add spec-vs-code consistency check to review.md
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S2.2.2 — Verbosity vs. Precision Tension; S2.2.1 — Acceptance Criteria Subjectivity
**Rationale:** The current review.md checklist covers build, code quality, MAUI specifics, architecture, and DevExpress. It does not include any check that the implemented code matches the spec. S2.2.2 identifies spec drift as the primary production failure mode; S2.2.1 documents that agents frequently satisfy acceptance criteria letter-but-not-spirit. Adding a spec consistency step to the review command would catch both: compare the implemented interfaces/service signatures against design.md, and verify that acceptance criteria in requirements.md are covered by actual tests or manual verifications.
**Suggested content/change:** Add a "Spec Consistency" section to review.md checklist: (1) Open design.md for the feature — do all interface signatures match what is in code? (2) Open requirements.md — is each acceptance criterion verified by a test or a manual check? (3) Is anything implemented that is explicitly listed as "out of scope"? (4) If code deviated from design.md, was design.md updated?

---

### OPP-2-10: Document the functional/technical separation rule in workflow.md with MyVocaList examples
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S2.3 — Functional vs Technical Separation
**Rationale:** S2.3 provides a concrete boundary table for what belongs in requirements.md vs design.md. The current workflow.md mentions the three-file structure but gives no guidance on what each file should contain. Agents writing specs for future features (Person, Song, Queue) may put EF Core indexes in requirements.md or user stories in design.md, producing specs that are hard to review and harder to hand to subagents. The boundary examples from S2.3 are directly applicable to MyVocaList's domain (person entity, queue round logic, Singer CRUD).
**Suggested content/change:** Add a content boundary table to the spec structure section in Rule 1, using MyVocaList examples:

| Question | File | Example |
|----------|------|---------|
| What can the admin accomplish? | requirements.md | "Admin shall register a singer with a unique display name" |
| What constitutes a valid name? | requirements.md | "Name: 1–50 chars, non-empty after trim" |
| Which table stores the singer? | design.md | `Persons` entity with `FullNameNormalized` |
| How is uniqueness enforced? | design.md | Unique index on `FullNameNormalized` |
| Which service method creates a singer? | design.md | `IPersonService.CreatePersonAsync(...)` |
