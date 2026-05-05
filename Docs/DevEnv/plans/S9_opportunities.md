# S9 — Quality Assurance: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

---

### OPP-9-01: Tester/Builder role separation rule
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** The current testing.md documents TDD workflow (Red→Green→Refactor) but does not enforce Tester/Builder role separation. When the same subagent writes both tests and implementation, tests become confirmation theater. This is documented as the #1 cause of tautological tests in AI-assisted development.
**Suggested content/change:** Add a section to testing.md titled "Role Separation — Tester vs Builder" with this rule:

> The subagent that writes tests must be different from the subagent that writes implementation. When a single agent writes both, it unconsciously writes tests its implementation will pass — a self-confirming loop. Dispatch the Tester task and Builder task as separate subagent invocations. The Tester receives the spec; the Builder receives the failing test suite. Never combine them in a single subagent session.

Add anti-pattern example: same-agent test-then-implement produces `Assert.NotNull(result)` style tautologies that pass even for wrong implementations.

---

### OPP-9-02: One-test-at-a-time incremental TDD discipline
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** testing.md prescribes Red→Green→Refactor but allows batched test writing. S9.1 documents that batched tests (write 10 tests at once, then implement all) breaks the feedback loop. The agent doesn't adjust understanding per test; hallucinations compound.
**Suggested content/change:** Add to the TDD Workflow section:

> Write ONE test at a time. Confirm it fails. Write minimal code to pass it. Confirm all tests pass. Only then write the next test. Never batch multiple tests before implementing. Batching loosens the feedback loop and is documented to increase hallucination rates.

---

### OPP-9-03: Builder must not modify tests during Green phase
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** No current rule prevents a Builder subagent from editing test files to make them pass, which is the most dangerous form of spec violation — the tests themselves are corrupted. This must be an explicit, stated prohibition.
**Suggested content/change:** Add to the TDD Workflow section and the Anti-Patterns table:

> The Builder must never modify test files. If a test fails, the implementation is wrong — the test is the spec contract. Verify that test files were not modified after the Red phase: check `git diff --name-only -- **/*Tests.cs` before completing any implementation task. If test files appear in the diff, the subagent violated this rule.

---

### OPP-9-04: Test quality audit checklist before implementation
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration (Test Quality Audit section)
**Rationale:** testing.md has strong test examples but no explicit quality audit gate. Before a Builder subagent begins implementation, the human should verify the Tester's tests are not tautological. S9.1 identifies six weakness patterns with specific signals.
**Suggested content/change:** Add a "Test Quality Gate" section:

> Before the Builder begins implementing against a test suite, audit for these weaknesses:
> | Weakness | Signal | Fix |
> |----------|--------|-----|
> | Tautological assertion | Test passes even if implementation returns wrong value | Assert specific value, message, or state |
> | Over-mocking | Test mocks so much that no real logic runs | Mock only external dependencies |
> | Missing edge case | Only happy path tested | Add null, empty, boundary tests per spec acceptance criteria |
> | No error case | "Should succeed" tests only; no "should fail" tests | For each validation rule, write a test that violates it |
> | Assertion depth of 1 | Only checks non-null | Add content/structure/side-effect assertions |
>
> Tests that fail this audit must be rewritten before implementation begins.

---

### OPP-9-05: Property-based testing for collections and pagination
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1.1 — Property-Based Testing for Non-Determinism
**Rationale:** testing.md covers example-based tests only. For collection operations (sorting, filtering, pagination) and validation rules, property-based tests verify invariants across many inputs and survive code regeneration from updated specs. FsCheck is the natural xUnit complement in C#.
**Suggested content/change:** Add a new subsection "Property-Based Testing (FsCheck)":

> Use property-based tests alongside example-based tests for:
> - Collection operations (sorting, filtering, searching) — verify invariants hold for any input
> - Pagination — verify completeness (no duplicates, no skipped items, accurate total count)
> - Validation rules — verify boundary properties hold for all values in range
>
> Library: `FsCheck.Xunit` (add to test project). Test ratio target: ~70% example-based, ~30% property-based.
>
> Core property categories to use:
> - **Invariant:** condition that holds before and after transformation (e.g., total count preserved after pagination)
> - **Idempotence:** applying twice = applying once (e.g., sorting twice = same result)
> - **Content safety:** output respects size/format constraints (e.g., page never exceeds requested page size)
>
> Property tests belong in `Integration/Repositories/` alongside real-SQLite tests.

---

### OPP-9-06: Spec quality gate before any verification or implementation
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3 — Hallucination Safeguards (Entry Criteria section)
**Rationale:** No current workflow rule mandates validating spec quality before coding begins. S9.3 establishes that no verification gate is stronger than the spec it checks against — ambiguous specs amplify hallucinations rather than containing them.
**Suggested content/change:** Add to Rule 1 (Spec-First) in workflow.md:

> **Spec Quality Gate (mandatory before implementation):**
> Before delegating any implementation task, verify the relevant spec file passes these checks:
> - Acceptance criteria are unambiguous (no "should," "may," "efficient," "fast" without measurable thresholds)
> - Every acceptance criterion is testable (expressible as a concrete assertion)
> - Edge cases are documented: null input, empty collections, boundary values, concurrent access, error conditions
> - Architectural constraints are explicit: which layer owns the logic, which dependencies are forbidden
>
> If any criterion fails this gate, rewrite the spec before proceeding. Sending a Tester or Builder into an ambiguous spec is the fastest path to hallucinated, spec-violating code.

---

### OPP-9-07: Adversarial Critic pattern for subagent code review
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3 — Hallucination Safeguards (Adversarial Verification Pattern)
**Rationale:** The current subagent exit checklist (verify → build → commit → push) relies entirely on the Builder's self-assessment. S9.3 documents that agent self-assessment is fundamentally unreliable — agents are 5.5× more likely to confidently predict success on a failing task. A fresh Critic session that reviews spec + diff independently catches violations the Builder encoded into both code and tests.
**Suggested content/change:** Add to Rule 2 (Subagent Delegation) in workflow.md, under "Subagent exit checklist":

> **Optional Critic pass (required for high-risk features):**
> After the Builder completes and commits, dispatch a separate Critic subagent (fresh session, no Builder context) with:
> - The spec file path
> - The git diff of changed files
> - The question: "Find spec violations, architectural violations, weak tests, and missing acceptance criteria coverage. If you cannot find genuine problems, say so explicitly."
>
> The Critic must NOT be the same subagent instance that built the code. Context contamination invalidates the review. Use for: authentication features, data-persistence code, any feature touching the queue round-based progression logic.

---

### OPP-9-08: Spec versioning discipline for spec files
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.2.1 — Spec Versioning & Rollback
**Rationale:** specs exist at `Docs/specs/[feature]/` and are tracked in git, but there is no convention for version headers, semantic versioning bumps, or decision logs. Without explicit version headers, rollback to a known-good spec for code regeneration is unreliable.
**Suggested content/change:** Add to Rule 1 (Spec-First) or as a new Rule 7:

> **Spec Versioning (every spec file must have a version header):**
> ```markdown
> **Version:** 1.0.0
> **Status:** Approved
> **Last modified:** YYYY-MM-DD
> **Reason for change:** <one sentence>
> **Breaking changes:** None | <list>
> ```
> Version bump discipline:
> - MAJOR — acceptance criterion removed, changed, or a behavioral contract flipped
> - MINOR — new acceptance criterion added; old criteria still valid
> - PATCH — typo fix, clarification without intent change
>
> Every spec change is its own commit: `spec: v1.2.0 — add queue round ordering constraints`.
> Never bundle spec changes with implementation commits.

---

### OPP-9-09: Session-end spec update ritual
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.2.2 — Spec Rot Under Evolution
**Rationale:** No current rule requires updating specs after a development session completes. S9.2.2 documents that AI-assisted teams ship 5x more code per day than they can document, causing specs to become actively misleading within hours. The session-end ritual (10 minutes of spec updates) prevents next-session context reconstruction debt.
**Suggested content/change:** Add to Rule 3 (Commit After Every Task) or as its own rule:

> **Session-end spec update (mandatory when behavior changes):**
> Before committing the final task of a session, check:
> - Did any implementation decision diverge from the spec's current text?
> - Were any constraints discovered (performance limits, library quirks, edge cases) that the spec doesn't document?
> - Were any spec requirements abandoned or deferred?
>
> If yes to any of the above: update the spec file, bump the version (MINOR or PATCH), and commit the spec change separately before the implementation commit.
> If no: add a one-line note to the task-log: "Spec consulted; no updates required."
>
> Never let a session end with spec-to-code drift unrecorded.

---

### OPP-9-10: Spec alignment checklist in review.md
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S9.2 — Spec Drift Prevention (Spec Rot section) and S9.3 — Hallucination Safeguards
**Rationale:** review.md has a build/architecture/DevExpress checklist but no spec-alignment section. Every PR that changes behavior should verify the spec was read and updated. This is the structural prevention for spec rot.
**Suggested content/change:** Add a "Spec Alignment" section to the review checklist:

> **Spec Alignment**
> - [ ] Relevant spec file(s) identified and read before implementation
> - [ ] PR changes behavior:
>   - [ ] Yes — spec updated and version bumped
>   - [ ] No — refactor/optimization only (confirm: no new behavior)
> - [ ] Every acceptance criterion in the spec has a corresponding implementation reference
> - [ ] Every acceptance criterion has test coverage (not just NotNull assertions — actual value/behavior assertions)
> - [ ] No acceptance criteria are unmapped (if any, mark as `blocked: spec gap` not `To Review`)
>
> A PR that changes behavior without updating the spec is not complete. Block it.

---

### OPP-9-11: Subagent false-completion prevention — proof of action
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3.2 — Agent Autonomy Without Reliability (Mandatory Proof of Action Protocol)
**Rationale:** The current subagent exit protocol does not require evidence-based verification of what was actually changed. S9.3.2 documents that agents are structurally prone to reporting completion of tasks they never executed or partially executed. Requiring explicit changed-files evidence in the task-log forces verification.
**Suggested content/change:** Update the "Subagent return protocol" and exit checklist in Rule 2 to make the "Changed files" section of the task-log mandatory (not optional):

> The task-log `Changed files` section is **not optional**. Every subagent must list every file it created or modified with an absolute path. This is the proof-of-action artifact. A task-log with an empty `Changed files` section is treated as a failed delivery — the task is marked `Build failure` and returned to the queue.
>
> Before stopping, run `git diff --name-only HEAD` and copy the output to `Changed files`. Do not rely on memory of what was changed.

---

### OPP-9-12: TDD level guidance for high-risk vs standard features
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration (Four Tuning Levels section)
**Rationale:** testing.md does not differentiate review cadence by risk level. S9.1 documents four TDD tuning levels (A through D) and provides a specific recommendation for MyVocaList: Level B for standard CRUD/business logic, Level C for authentication, encryption, and database schema changes.
**Suggested content/change:** Add a "Review Cadence by Risk" section to the TDD Workflow:

> | Level | Review Checkpoints | Apply To |
> |-------|-------------------|----------|
> | **Level A** | After full Red-Green-Refactor cycle completes | Exploratory features, low-risk UI |
> | **Level B** | After each complete Red-Green-Refactor cycle | Standard CRUD, business logic, ViewModel commands |
> | **Level C** | After each phase (Red, then Green, then Refactor) | Authentication, database schema changes, migration logic, queue round-progression rules |
>
> Default to Level B. Escalate to Level C for any feature that, if wrong, would corrupt persisted data or lock users out of the queue.

---

### OPP-9-13: E2E emulator run is a mandatory gate before marking task complete
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3 — Hallucination Safeguards (Integration and E2E Testing section) and S9.1 — TDD Integration
**Rationale:** The current subagent exit checklist ends at `dotnet test` passing. Unit tests can all pass while the app fails on-device. S9.3 explicitly states: "After code generation, run the application (emulator or staging) and execute critical user journeys. This is not optional; it is where most hallucinations surface." This is not currently codified in the workflow.
**Suggested content/change:** Add to Rule 3 (Commit After Every Task):

> **E2E verification (required before `To Review`):**
> For tasks that add or change UI pages, navigation, or queue state transitions: run the app on the emulator and manually execute the critical user journey for that feature. Unit tests pass before this; E2E confirms the integration boundary is correct. Only after a successful emulator run may the task be marked `To Review`. If the emulator is unavailable, mark the task `Check build` and note "E2E pending" in the task-log.
