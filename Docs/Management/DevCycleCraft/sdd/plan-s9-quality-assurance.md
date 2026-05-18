# S9 — Quality Assurance: Enhancement Opportunities

> Source files analyzed: S9_Quality_Assurance.md, S9_1_TDD_Integration.md, S9_1_1_Property_Based_Testing.md, S9_2_Spec_Drift_Prevention.md, S9_2_1_Spec_Versioning_n_Rollback.md, S9_2_2_Spec_Rot_Under_Evolution.md, S9_3_Hallucination_Safeguards.md, S9_3_1_False_Confidence_Trap.md, S9_3_2_Agent_Autonomy_Without_Reliability.md
> Compared against: CLAUDE.md, .claude/rules/workflow.md, .claude/rules/testing.md, .claude/rules/code-principles.md, .claude/settings.json
> Last reviewed: 2026-05-06

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, still unimplemented) | 13 |
| 🆕 New (not previously captured) | 6 |
| **Total** | **19** |

All 13 previously captured opportunities remain unimplemented — confirmed by searching testing.md, workflow.md, and review.md for their key terms.

---

## Previously Captured Opportunities

### ✅ OPP-9-01: Tester/Builder role separation rule
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** The current testing.md documents TDD workflow (Red→Green→Refactor) but does not enforce Tester/Builder role separation. When the same subagent writes both tests and implementation, tests become confirmation theater. This is documented as the #1 cause of tautological tests in AI-assisted development.
**Suggested content/change:** Add a section to testing.md titled "Role Separation — Tester vs Builder" with this rule:

> The subagent that writes tests must be different from the subagent that writes implementation. When a single agent writes both, it unconsciously writes tests its implementation will pass — a self-confirming loop. Dispatch the Tester task and Builder task as separate subagent invocations. The Tester receives the spec; the Builder receives the failing test suite. Never combine them in a single subagent session.

Add anti-pattern example: same-agent test-then-implement produces `Assert.NotNull(result)` style tautologies that pass even for wrong implementations.

---

### ✅ OPP-9-02: One-test-at-a-time incremental TDD discipline
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** testing.md prescribes Red→Green→Refactor but allows batched test writing. S9.1 documents that batched tests (write 10 tests at once, then implement all) breaks the feedback loop. The agent doesn't adjust understanding per test; hallucinations compound.
**Suggested content/change:** Add to the TDD Workflow section:

> Write ONE test at a time. Confirm it fails. Write minimal code to pass it. Confirm all tests pass. Only then write the next test. Never batch multiple tests before implementing. Batching loosens the feedback loop and is documented to increase hallucination rates.

---

### ✅ OPP-9-03: Builder must not modify tests during Green phase
**Target:** `.claude/rules/testing.md`
**Action:** Update
**Source topic:** S9.1 — TDD Integration
**Rationale:** No current rule prevents a Builder subagent from editing test files to make them pass, which is the most dangerous form of spec violation — the tests themselves are corrupted. This must be an explicit, stated prohibition.
**Suggested content/change:** Add to the TDD Workflow section and the Anti-Patterns table:

> The Builder must never modify test files. If a test fails, the implementation is wrong — the test is the spec contract. Verify that test files were not modified after the Red phase: check `git diff --name-only -- **/*Tests.cs` before completing any implementation task. If test files appear in the diff, the subagent violated this rule.

---

### ✅ OPP-9-04: Test quality audit checklist before implementation
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

### ✅ OPP-9-05: Property-based testing for collections and pagination
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

### ✅ OPP-9-06: Spec quality gate before any verification or implementation
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

### ✅ OPP-9-07: Adversarial Critic pattern for subagent code review
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

### ✅ OPP-9-08: Spec versioning discipline for spec files
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

### ✅ OPP-9-09: Session-end spec update ritual
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

### ✅ OPP-9-10: Spec alignment checklist in review.md
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

### ✅ OPP-9-11: Subagent false-completion prevention — proof of action
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3.2 — Agent Autonomy Without Reliability (Mandatory Proof of Action Protocol)
**Rationale:** The current subagent exit protocol does not require evidence-based verification of what was actually changed. S9.3.2 documents that agents are structurally prone to reporting completion of tasks they never executed or partially executed. Requiring explicit changed-files evidence in the task-log forces verification.
**Suggested content/change:** Update the "Subagent return protocol" and exit checklist in Rule 2 to make the "Changed files" section of the task-log mandatory (not optional):

> The task-log `Changed files` section is **not optional**. Every subagent must list every file it created or modified with an absolute path. This is the proof-of-action artifact. A task-log with an empty `Changed files` section is treated as a failed delivery — the task is marked `Build failure` and returned to the queue.
>
> Before stopping, run `git diff --name-only HEAD` and copy the output to `Changed files`. Do not rely on memory of what was changed.

---

### ✅ OPP-9-12: TDD level guidance for high-risk vs standard features
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

### ✅ OPP-9-13: E2E emulator run is a mandatory gate before marking task complete
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3 — Hallucination Safeguards (Integration and E2E Testing section) and S9.1 — TDD Integration
**Rationale:** The current subagent exit checklist ends at `dotnet test` passing. Unit tests can all pass while the app fails on-device. S9.3 explicitly states: "After code generation, run the application (emulator or staging) and execute critical user journeys. This is not optional; it is where most hallucinations surface." This is not currently codified in the workflow.
**Suggested content/change:** Add to Rule 3 (Commit After Every Task):

> **E2E verification (required before `To Review`):**
> For tasks that add or change UI pages, navigation, or queue state transitions: run the app on the emulator and manually execute the critical user journey for that feature. Unit tests pass before this; E2E confirms the integration boundary is correct. Only after a successful emulator run may the task be marked `To Review`. If the emulator is unavailable, mark the task `Check build` and note "E2E pending" in the task-log.

---

## New Opportunities

### 🆕 OPP-9-14: Mutation testing as a CI gate for test suite quality
**Target:** `.claude/rules/testing.md`
**Action:** Add
**Source topic:** S9.3.1 — False Confidence Trap (Mutation Testing section)
**Rationale:** testing.md has no mechanism to catch tautological test suites beyond human review. S9.3.1 documents that AI-generated test suites commonly achieve 85–91% line coverage while scoring only 20–45% on mutation testing — a 46–57% gap that reveals the tests are not asserting the right things. Line coverage is already the default CI metric; mutation score is not tracked at all.
**Suggested content/change:** Add a "Mutation Testing" subsection after the Running Tests section:

> **Mutation testing (Stryker.NET):**
> Add `Stryker.NET` to the test project to verify that the test suite actually catches bugs, not just executes lines.
>
> ```bash
> dotnet tool install -g dotnet-stryker
> dotnet stryker --project MyVocaList.Services/MyVocaList.Services.csproj
> ```
>
> Target mutation scores:
> - Services (business logic): ≥ 60% (blocking gate)
> - Repositories (query logic): ≥ 50% (warning gate)
>
> If mutation score is ≥ 85% line coverage but ≤ 40% mutation score, the test suite is predominantly tautological. Audit and rewrite before proceeding.
>
> Run mutation testing before marking a feature's test suite complete, not in every CI build (mutation runs are slow — run on feature branches, not every commit).

---

### 🆕 OPP-9-15: Acceptance criteria traceability matrix in task-log
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.3 — Hallucination Safeguards (Verifier Agents and Evidence-Based Gates)
**Rationale:** The task-log format currently records `Changed files` but has no field for acceptance criteria coverage. S9.3 defines the Verifier's traceability matrix — every acceptance criterion must map to an implementation file+line and a test name+file. Without this artifact, review consists of "the subagent said it's done" rather than verifiable evidence. This is the core evidence-based gate that prevents hallucinated completions.
**Suggested content/change:** Add a `Traceability` section to the task-log format in Rule 5 of workflow.md:

> ```
> ### Traceability
> | Acceptance Criterion | Implementation | Test |
> |----------------------|----------------|------|
> | <criterion from requirements.md> | <file:line> | <TestClass.MethodName> |
> | ... | ... | ... |
> ```
>
> Every acceptance criterion from the feature's `requirements.md` must appear in this table with both implementation and test evidence. A criterion with either field missing is a blocker for `To Review` status.
>
> For tasks that are purely refactoring (no new acceptance criteria): note "Refactor only — no new criteria" in place of the table.

---

### 🆕 OPP-9-16: Pre-task context gate — check spec and test files exist before implementation
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S9.3 — Hallucination Safeguards (Context Gates / Aperture pattern)
**Rationale:** S9.3 documents the Aperture pattern: before an agent receives any code, a deterministic gate checks that the required spec file, test file, and configuration context exist. One of the nine rule-based checks is `missing_spec` — "task is a feature, but no design document exists." Currently no such gate exists in the workflow; subagents frequently proceed without reading the spec, producing architecturally inconsistent output. A lightweight pre-task checklist prevents the most common context collapse.
**Suggested content/change:** Add a pre-task checklist requirement to Rule 2 (Subagent Delegation):

> **Pre-task context gate (subagent must verify before writing any code):**
> Before writing a single line of implementation, verify:
> 1. `Docs/specs/[feature]/design.md` exists and has been read
> 2. `Docs/specs/[feature]/requirements.md` exists and acceptance criteria are unambiguous
> 3. A test file exists (or will be created as the first deliverable)
> 4. Any external contracts the task touches (e.g., domain interfaces, DTO shapes) are loaded into context
>
> If any item is missing, stop and report `blocked: spec gap` — do not proceed with guessed implementations. The main agent fills the gap, then re-delegates.

---

### 🆕 OPP-9-17: Spec rot multiplier warning for parallel multi-agent waves
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S9.2.2 — Spec Rot Under Evolution (Multiplier Effect section)
**Rationale:** Rule 2 allows up to 4 parallel subagents per wave. S9.2.2 documents that when N agents read the same stale spec, each additional agent increases spec-based failure probability by 20–30% multiplicatively. By the 5th agent operating on the same stale spec, the failure rate reaches 80%+. This makes spec freshness a critical prerequisite for any multi-agent wave — not just a nice-to-have. The current Rule 2 has no check that the spec is fresh before dispatching a wave.
**Suggested content/change:** Add to Rule 2 (Subagent Delegation), before the wave-based parallelism section:

> **Spec freshness gate before dispatching a wave:**
> Before starting a multi-agent wave, verify the feature spec was updated for any code shipped in the previous wave. If the spec is more than one session old and implementation has shipped since the last spec update, update the spec now before dispatching. A stale spec read by 4 parallel agents produces 4 divergent hallucinated implementations. The time cost of updating the spec is lower than the rework cost of a contaminated wave.

---

### 🆕 OPP-9-18: Bounded autonomy rule — irreversible actions require explicit human confirmation
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S9.3.2 — Agent Autonomy Without Reliability (Bounded Autonomy with Escalation)
**Rationale:** S9.3.2 documents that the production reliability standard is L3–L4 (conditional autonomy with escalation), not L5 (full autonomy). MyVocaList's workflow currently has no explicit category of "actions that require human confirmation before execution." Documented failure cases (AWS Kiro, Perplexity Computer) all involved agents executing irreversible actions (database deletions, EF Core migrations, file overwrites) without confirmation. The project uses SQLite with EF Core migrations — a dropped migration or destructive schema change is hard to recover from without git.
**Suggested content/change:** Add a new rule or sub-rule to workflow.md:

> **Rule: Irreversible actions require explicit human confirmation**
> Before executing any of the following, a subagent must stop and request explicit approval — it must NOT proceed autonomously:
> - EF Core migration additions or changes (`dotnet ef migrations add`)
> - Any `git push --force` or history-rewriting operation
> - Deleting spec files, plan files, or task-log files
> - Changes to `MauiProgram.cs` DI registration that add or remove service lifetimes
> - Any change that removes an existing database index or unique constraint
>
> The subagent describes the action and its impact, then waits for "yes, proceed" before continuing. This is the difference between L3 (supervised autonomy) and L5 (unguided autonomy). For MyVocaList, L3 is the default for all data-persistent operations.

---

### 🆕 OPP-9-19: Decision log alongside spec files for architectural choices
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S9.2.1 — Spec Versioning & Rollback (Decision Logs Paired with Specs)
**Rationale:** workflow.md specifies the three spec file structure (requirements.md, design.md, tasks.md) but has no concept of a decision log. S9.2.1 documents that without a decision log, rollback decisions are guesswork — when a regeneration fails, the team doesn't know if a constraint was essential or legacy. As MyVocaList grows (queue modes, singer management, song catalog), architectural decisions about round-based queue progression, data shapes, and mode behavior will accumulate and be lost between sessions unless recorded.
**Suggested content/change:** Add a fourth optional file to the spec structure table in Rule 1:

> | File | What it answers |
> |------|----------------|
> | `requirements.md` | User stories, acceptance criteria, validation rules, out-of-scope |
> | `design.md` | Architecture, interfaces, page structure, interaction flows, key decisions |
> | `tasks.md` | Ordered checkboxed tasks — check off as each completes |
> | `decision-log.md` | **(optional, required for MAJOR spec changes)** Why constraints exist, options considered, reversal conditions |
>
> A decision log entry is required when a spec receives a MAJOR version bump. Format:
> ```markdown
> ## DEC-YYYY-MM-DD: <title> (Spec vX.0.0)
> **Condition:** <what problem triggered this decision>
> **Options considered:** <A, B, C with brief pros/cons>
> **Decision:** <chosen option and why>
> **Trade-offs:** <what was given up>
> **Reversal condition:** <under what circumstances to revisit>
> ```
