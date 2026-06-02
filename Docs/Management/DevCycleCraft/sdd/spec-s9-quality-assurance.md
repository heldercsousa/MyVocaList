# S9 — Quality Assurance

**Status:** Researched  
**Predecessor(s) ID:** S3 (Workflow Phases), S4 (Context & Memory), S5 (Agent Patterns), S6 (Governance & Enforcement)

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; covers TDD integration, spec drift prevention, and hallucination safeguards |

---

## Overview

Quality assurance in spec-driven development is fundamentally different from traditional QA. In classical software development, testing happens after code is written — a reactive gate that catches bugs before release. In SDD with AI agents, QA becomes proactive: **quality is structured into the workflow itself**, not grafted onto the end.

Three challenges define QA in the AI-assisted age:

1. **Hallucination cascades** — An AI agent misunderstands a spec, writes code that passes tests it itself generated, and ships logic that silently violates the original requirement. The tests are green. The spec is ignored. Nobody notices until production.
2. **Spec drift** — As agents regenerate code across multiple sessions and refactorings, they reinterpret the spec inconsistently, diverging from the original intent. Code compiles. Tests pass. The implementation has drifted.
3. **Regression under uncertainty** — When AI generates both code and tests, the tests can become tautologies that validate what was implemented rather than what was required. High test coverage masks fundamental misalignment with the specification.

Quality Assurance in SDD addresses these through three integrated practices:

- **S9.1 — TDD Integration:** Red-Green-Refactor discipline enforced at the agent layer, with property-based testing to verify invariants regardless of implementation variation
- **S9.2 — Spec Drift Prevention:** Continuous conformance checking, versioning, and rollback capability to detect when code diverges from specification
- **S9.3 — Hallucination Safeguards:** Adversarial verification, multi-agent review, and evidence-based gates that prevent false-confidence scenarios where tests pass but contracts are violated

---

## S9.1 — TDD Integration

Traditional TDD (Red → Green → Refactor) is a design tool: writing tests first forces the engineer to think through the interface before implementing. When AI agents are the implementors, TDD's role inverts: the tests become the **forcing function** for the agent, constraining each step and preventing multi-file cascades of hallucination.

### The Problem: TDD Instructions Alone Make Things Worse

Research (TDAD 2025) found a counterintuitive result: simply telling an agent "use red-green TDD" **increased regressions from 6.08% to 9.94%**. The agent understood TDD as a suggestion, not a gate. It would write code to pass tests it hadn't yet verified, or skip the failing test step entirely when optimizing for speed.

The fix: **structure, not suggestions.** TDD discipline must be encoded into the workflow itself:

- **Separate test generation from implementation.** One agent writes tests designed to be hard to pass (the Tester). A different agent makes those tests pass (the Builder). When the same agent writes both, it unconsciously writes tests that its implementation will pass — a self-confirming loop that defeats the purpose.
- **Mandate failing tests before implementation.** A hard gate: implementation code does not exist until a test suite fails. If a test passes without implementation, something is wrong.
- **Verify test quality before allowing refactoring.** Weak tests (tautological assertions, over-mocking, missing edge cases) are blockers. The Tester reviews the Test Quality gate explicitly: "Would this test catch a wrong implementation?"

### Property-Based Testing for Non-Determinism

Specs written for AI agents are natural-language descriptions of what the system must do. Agents interpret these descriptions and generate code. Two different agents (or the same agent in a different session) might produce different implementations — both correct, but different.

**Property-based testing** verifies that invariants hold regardless of which implementation was generated. Instead of testing specific values, property tests assert structural properties:

```
∀ input: T → output: U (property holds for all generated inputs)
```

For example:
- **For authentication:** Given any valid token and any endpoint, the handler must verify the token before processing the request (property: "all user input is validated before database access").
- **For sorting:** Given any list and any comparison function, the result is sorted and contains the same elements as the input (properties: "sorted order," "no elements added or removed").
- **For pagination:** Given any page size and any result set, the total count is accurate and no items are skipped or duplicated (properties: "completeness," "deterministic order").

Property-based testing becomes **critical in SDD** because:

1. **Agents regenerate code.** When a spec is regenerated (architecture changes, dependency updates, security patches), the new implementation may be completely different. Property tests still pass because they verify the contract, not the implementation.
2. **Spot-checking misses subtle regressions.** A unit test that verifies `sort([3,1,2]) == [1,2,3]` passes on any sorting algorithm. A property test that verifies "for all lists, the output is sorted" catches a regression where the agent accidentally switched the comparison operator.
3. **Edge cases are AI's weakness.** Agents often miss boundary conditions (null, empty, max size, concurrent access). Property tests generate hundreds of inputs automatically, including edge cases the engineer might not manually list.

### Enforced Phases with Stopping Points

Structure TDD as explicit phases with human-review gates:

| Phase | Agent | Output | Gate |
|-------|-------|--------|------|
| **Red** | Tester | Failing test suite (all tests fail) | Human verifies: "Do these tests match the spec's acceptance criteria?" |
| **Green** | Builder | Minimal implementation (all tests pass) | Human: "Does the implementation match the test intent?" |
| **Refactor** | Builder | Refactored code (tests still pass) | Human: "Is the refactoring safe?" (property tests re-run) |

Each phase has a **stopping condition** — if the gate fails, the agent repeats the phase or the workflow pauses for human intervention. Critical: do not allow the agent to merge when a gate fails.

### Tuning for Agent Velocity

The three TDD levels from EXACT Coding (practice framework) calibrate the human review burden:

- **Level A:** Agent works through all test cases autonomously; human reviews at the end. Fast (1–2 hours for a feature), but risky if something goes fundamentally wrong early.
- **Level B:** Pause after each complete Red-Green-Refactor cycle (every 20–30 minutes). Balances speed and control; catches drift before cascading.
- **Level C:** Pause after each phase (Red, Green, Refactor). Maximum control, slower (4–8 hours for a feature), but suitable for high-risk code (auth, payments, compliance).

**MyVocaList guidance:** Use Level B for standard features, Level C for authentication and data-persistence code.

### Testing the Application, Not Just Components

BDD tests (Given-When-Then acceptance scenarios) pass against APIs and internal interfaces. **QA tests** validate the running application as a user would experience it:

```
BDD passes → Component contracts are satisfied
QA passes  → The system actually works end-to-end
```

Integration boundaries are where AI agents most commonly fail. An agent might generate correct business logic that violates a database schema, or valid API code that fails under concurrent load. Property-based tests at the component level don't catch these.

**Required:** After code generation, run the application end-to-end (emulator, live server, or staging environment) and verify critical user journeys. This is not optional; it is where most hallucinations surface.

---

## S9.2 — Spec Drift Prevention

Spec drift is the gradual divergence between the specification and the implementation it generated. Unlike a bug (a single point failure), drift is systemic: changes accumulate across multiple regeneration passes, refactorings, and maintenance cycles. By the time drift is detected, the implementation has drifted so far from the original spec that regenerating from scratch is faster than patching.

### Root Causes of Spec Drift

1. **Nondeterministic generation.** Running an agent against the same spec twice may produce different code. If the engineer implicitly accepts the first implementation as "correct," the spec becomes a memory artifact, not a true source of truth.
2. **Silent spec updates.** Some tools (e.g., Intent's auto-updating specs) allow agents to modify the spec when they encounter ambiguity. Without explicit review and versioning, the spec drifts to match the implementation, not the other way around.
3. **Incremental changes without spec review.** An engineer refactors code, adds a feature, or optimizes a query without reviewing the original spec. The change is correct in isolation but violates a constraint documented in the spec.
4. **Spec maintenance burden.** Keeping a spec in sync with code requires discipline. As the system grows, updating specs becomes tedious. Developers skip it to move faster. The spec becomes stale documentation.
5. **Regeneration context loss.** An agent regenerates code from a spec, but the original design decisions, edge cases, and constraints documented in the spec are no longer accessible as context. The new generation is simpler or differently architected, breaking assumptions elsewhere in the system.

### Continuous Conformance Checking

**Spec drift must be detected continuously, not periodically.** Periodic drift checks (weekly, monthly) allow divergence to compound. By the time a check runs, the codebase may already be months ahead of the spec.

Continuous conformance is implemented as a CI/CD gate that runs on every push:

```yaml
# Pseudocode: CI/CD stage for spec conformance
stage: Verify Spec Conformance
  - Parse spec acceptance criteria
  - Map each criterion to implementation evidence (file + line)
  - Map each criterion to test evidence (test name + file)
  - If criterion has implementation but no test: warn
  - If criterion has neither: fail build
  - If implementation exists but spec doesn't document it: warn (undocumented feature)
```

The gate defaults to **FAIL**. Every feature must prove that it:
1. Has a corresponding spec requirement
2. Has implementation code (with a source reference)
3. Has test coverage (with a test reference)

Specwright (2026 practitioner tool) operationalizes this: `/sw-ship` creates PRs where every acceptance criterion is mapped to code and test evidence. Reviewers don't trust — they **verify** by reading the evidence trail.

### Spec Versioning and Rollback

Specs are not immutable, but **they must be versioned alongside code.** When a spec is updated, that change is a commit. When code is regenerated, the spec-to-code binding is explicit: "This code was generated from Spec v1.3.2."

Versioning enables **rollback:** If regeneration introduces regressions, revert to an older spec version and regenerate. This is only possible if specs are tracked in git like code.

**Naming convention:** Specs include a version field:

```markdown
## Spec

**Version:** 1.3.2  
**Generated from:** arXiv:2602.00180  
**Last reviewed:** 2026-04-28  
**Modified:** 2026-04-30 (clarified token limit, added test for boundary case)
```

Git history is the source of truth:

```bash
$ git log Docs/specs/venues/design.md
# Each commit is a spec change
# Diffs show what changed and when
```

If a regeneration based on Spec v1.4.0 produces code that fails acceptance, rollback to Spec v1.3.2 (the previous known-good version) and regenerate. The old implementation is recovered; the problematic changes are discarded.

### Spec Rot Under Evolution

**Spec rot** occurs when specs become stale under a growing codebase. New features are added without updating specs. Code is refactored without reflecting changes in design docs. Within months, the spec describes a system that no longer exists.

The structural fix: **Tie spec review to code review.**

When a PR changes code:
- **If the PR changes behavior:** The PR must update the spec or explain why the spec doesn't need updating.
- **If the PR doesn't change behavior:** The PR should mention relevant specs that were consulted.

This is enforced as a checklist in PR templates:

```markdown
## Spec Alignment

- [ ] I have read the relevant spec(s)
- [ ] This PR changes behavior:
  - [ ] Yes — I have updated the spec(s)
  - [ ] No — This PR only refactors or optimizes
- [ ] Tests verify that the implementation matches the spec
```

Failing to check these boxes blocks merge. This makes spec review visible and continuous, not a historical afterthought.

---

## S9.3 — Hallucination Safeguards

**Hallucination** in the SDD context means an agent generates code that:
- Compiles and passes tests
- But violates a behavioral contract or architectural constraint documented in the spec
- Silently — the failure only surfaces under specific conditions (high load, concurrent access, edge case input)

The classic hallucination: An agent generates an API endpoint that matches the documented schema, parses the request correctly, and returns data. But the spec says "enforce role-based access control (RBAC) on this endpoint," and the agent forgot that constraint. Tests don't exercise RBAC (because they use admin credentials). The endpoint ships without RBAC. A user without permission succeeds anyway. Compliance violation. Audit failure.

### The False Confidence Trap

**The core problem:** When the same agent writes both code and tests, tests become confirmation rather than verification. The agent misunderstands a requirement, implements it wrong, writes tests that validate the wrong implementation, and the test suite passes with 100% coverage. False confidence.

This is measurable. Research (TDAD, arXiv 2025) found that AI-generated tests often encode **tautological assertions**:

```csharp
// Weak test (tautological — always passes)
[Fact]
public void GetUser_ReturnsUser()
{
    var user = _repo.GetUser(123);
    Assert.NotNull(user);  // This asserts nothing; passes if the call doesn't crash
}

// Strong test (validates behavior)
[Fact]
public void GetUser_WithValidId_ReturnsMatchingUser()
{
    _repo.Add(new User { Id = 123, Name = "Alice" });
    var result = _repo.GetUser(123);
    Assert.Equal("Alice", result.Name);
    Assert.Equal(123, result.Id);
}
```

High coverage + weak tests = false safety. An engineer sees "95% coverage" and assumes the feature is well-tested. In reality, the tests are confirmation theater.

### Adversarial Verification

The antidote is **adversarial review:** A different agent (different model, different context window, fresh perspective) reviews the spec, tests, and implementation with instructions to find flaws.

The Adversary's mindset is zero-tolerance:

> "Assume the worst. Your job is not to approve; it is to find what the Builder missed. If you can't find anything wrong, you must hallucinate flaws because real ones no longer exist."

This is called **hallucination-based termination** (VSDD, 2026). When the Adversary has exhausted genuine flaws and is forced to invent problems, the system is considered "Zero-Slop" — correct, with full coverage, and architecture sound.

### Verifier Agents and Evidence-Based Gates

A **Verifier agent** is a dedicated role that validates implementation against the spec before code merges:

```
Coordinator → Implementor → Verifier → Merge
              Spec         Spec         Spec
```

The Verifier reads the spec, compares it to the code, and answers:

1. **Are there acceptance criteria in the spec with no implementation?** (Missing features → block)
2. **Is there implementation code not covered by spec?** (Undocumented features → warn)
3. **Do the tests actually validate the acceptance criteria, or are they tautologies?** (Weak tests → block)
4. **Are there architectural violations?** (Code crosses layers, introduces circular dependencies → block)
5. **Is there evidence of security issues?** (Secrets in code, input validation gaps, auth weakening → block)

The Verifier produces an **evidence report** — not a pass/fail, but a traceability matrix:

```
Acceptance Criterion       | Implementation         | Test Evidence
--------------------------|------------------------|------------------------------
"Authenticate all writes"  | VenueService.cs:142    | VenueServiceTests.cs:234
"Reject duplicate names"   | VenueService.cs:156    | VenueServiceTests.cs:198
"Support pagination"       | VenueRepository.cs:89  | VenueRepositoryTests.cs:301
"Validate role"            | ❌ MISSING             | ❌ MISSING
```

Merges are blocked until all acceptance criteria have evidence.

### Multi-Model Review

When a single agent writes code and tests, it has no external corrective. When two different models review the same code independently, their different training, blind spots, and reasoning styles mean they catch different failures.

**Pattern:** The Builder (Claude Code, Copilot) generates implementation. A different model (Gemini, Kimi, or a second Claude Code instance with different system prompt) reviews independently. Results are compared:

| Finding | Builder | Reviewer | Action |
|---------|---------|----------|--------|
| Missing error handling | No | Yes | Blocker — must be addressed |
| Over-mocking in tests | Yes | Yes | Blocker — high agreement |
| Inefficient query | Yes | No | Advisory — one flag, probably worth investigating |

High agreement between models increases confidence. Disagreement flags uncertainty.

### Integration and E2E Testing as Regression Check

Unit tests and BDD tests verify components. **Integration and E2E tests verify the system.**

An agent might generate code that:
- Compiles ✓
- Passes unit tests ✓
- Passes BDD acceptance scenarios ✓
- But under load, causes an N+1 query explosion
- Or in concurrent scenarios, causes a race condition
- Or across service boundaries, violates a contract

These failures don't show up until the application runs end-to-end. **Required:** After code generation, run the application (emulator or staging) and execute critical user journeys. This is where semantic drift surfaces.

### Entry Criteria: Spec Quality as a Prerequisite

**No verification gate is stronger than the spec it checks against.** If the spec is ambiguous, vague, or incomplete, the Verifier will pass code that violates the spirit of the requirement.

Before any gate runs, the spec itself must be verified:

- **Is it ambiguous?** Rewrite for clarity.
- **Are acceptance criteria testable?** Vague: "The system shall be performant." Testable: "Page load time shall be ≤ 100ms on all endpoints."
- **Are edge cases documented?** What about null input, empty arrays, concurrent requests, network failures?
- **Are architectural constraints explicit?** "Data must be encrypted at rest." "All writes must be ACID transactions." "No direct database access from the UI."

Spec review is the first gate; implementation gates follow. Bad specs amplify hallucinations; good specs contain them.

---

## Integration with SDD Workflow Phases

Quality assurance is not a separate phase. It is **embedded at every step of S3 (Workflow Phases):**

| SDD Phase | QA Activity | Owner | Success Criteria |
|-----------|-------------|-------|------------------|
| **S3.1 — Planning** | Spec review for ambiguity, completeness, testability | Human (with AI expansion) | Spec passes quality gate before any code generation |
| **S3.2 — Implementation** | TDD discipline: Red-Green-Refactor with phase gates | Agent (Builder + Tester) | All tests fail before implementation; all pass after; property tests pass |
| **S3.3 — Verification** | Verifier agent checks code against spec; adversarial review of tests | Verifier agent + human | Evidence matrix complete; no acceptance criteria unmapped |
| **S3.3 (Pre-merge)** | CI/CD gates: spec conformance, test coverage, security scan, architecture check | Automation + Verifier | All gates pass; evidence trail complete |
| **S3.3 (Post-merge)** | E2E/integration testing on staging or emulator | QA/Human | Critical user journeys work; no regressions detected |

Quality is measured at each step, not collected at the end.

---

## Common Failure Modes and Mitigations

| Failure Mode | Signal | Root Cause | Mitigation |
|--------------|--------|-----------|-----------|
| **Hallucination passes gate** | Tests green, spec violated | Same agent writes tests and code | Separate Tester from Builder; Adversary reviews tests |
| **Spec becomes outdated** | Code drifts silently over months | No continuous conformance check | CI/CD gate on every push mapping spec→code→test |
| **False coverage** | 95% coverage, code still broken | Tautological assertions | Audit what tests actually assert, not just coverage % |
| **Context loss on regeneration** | New code ignores constraints from old spec | Spec is read once, then discarded | Versioned specs in git; regeneration anchored to spec version |
| **Integration surprise** | Unit tests pass, system fails under load | Only component-level testing | E2E/integration tests mandatory after code generation |
| **Verifier as rubber stamp** | Spec drift undetected | Verifier uses outdated or ambiguous spec | Gate on spec quality before Verifier runs |
| **Security regression in iteration loop** | Functional correctness maintained, security erodes | Feedback loop only measures functional tests | Security checks at iteration boundary; block security regressions |

---

## Tooling and Automation

### Recommended Tools by Phase

| Tool | Role | What It Does |
|------|------|-------------|
| **Specwright** | Evidence capture | Maps every acceptance criterion to code + test evidence before PR merge |
| **Intent (Augment Code)** | Verifier coordination | Coordinator-Implementor-Verifier architecture; Verifier compares code against living spec |
| **VSDD (practitioner pattern)** | Adversarial review | Builder + Tester + Adversary; Adversary hallucination-terminates when no real flaws remain |
| **CrossCheck** | Structural enforcement | Git hooks enforce conventions; multi-model review at merge gate |
| **Property-Based Testing** | Invariant verification | Quickcheck (Haskell), PropEr (Erlang), Hypothesis (Python), QuickTheories (Java), xUnit Property-based (C#) |

### Metrics to Track

- **Hallucination rate:** % of generated code that passes tests but violates spec on review
- **Spec drift:** Months elapsed since spec was last updated; % of code paths undocumented by spec
- **Test quality:** % of tests with assertion depth ≥ 2 (vs. tautological tests)
- **Verifier false-negative rate:** Issues found in production that Verifier should have caught
- **Integration surprise rate:** Features passing all gates but failing in E2E/integration
- **Regression rate:** % of regenerated code that introduces new failures

Track these metrics to calibrate gates: if hallucination rate is high, tighten spec review; if false-negatives are frequent, enhance Verifier rules.

---

## Relationship to Other SDD Topics

- **S3 (Workflow Phases):** Quality gates embedded at each phase
- **S4 (Context & Memory):** Specs as the persistent context that guides quality checkpoints
- **S5 (Agent Patterns):** Verifier agent as a dedicated role in the Coordinator-Implementor-Verifier pattern
- **S6 (Governance & Enforcement):** Constitutional rules (e.g., "TDD is mandatory") backed by automated hooks and gates
- **S9.1, S9.2, S9.3:** The three technical pillars of SDD QA

---

## Key Takeaways

1. **Quality in SDD is proactive, not reactive.** TDD discipline, continuous conformance checking, and adversarial verification prevent bugs from being shipped, rather than catching them after.
2. **Spec is the source of truth for verification.** Every acceptance criterion must map to implementation evidence and test evidence. "Tests pass" is necessary but insufficient — the tests must verify the spec, not the implementation.
3. **Hallucinations are systematic, not random.** They emerge when agents write tests for their own code, or when specs are ambiguous. Mitigations: separate Tester from Builder; review specs before code generation.
4. **Drift is silent unless continuously detected.** Weekly or monthly conformance checks compound divergence. Gates must run on every push.
5. **E2E/integration testing is not optional.** Unit and component tests can all pass while the system fails under realistic conditions. Run the app end-to-end.

---

## Sources

- [VSDD: When Your AI Writes Code, Who Checks Its Homework? — Vibe Sparking AI](https://www.vibesparking.com/en/blog/ai/vibe-coding/2026-03-03-verified-spec-driven-development/)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Part 2 — Quality in the Age of LLMs — Clayton Davis Blog](https://blog.claytondavis.dev/post/009-ai-sdlc-quality/index.html)
- [Beyond Vibe-coding: Spec-Driven Development — evanisnor.com](https://evanisnor.com/blog/2026/spec-driven-development-with-coding-agents)
- [EXACT Coding: AI-powered development with a focus on quality — Codecentric](https://www.codecentric.de/en/knowledge-hub/blog/exact-coding-with-ai)
- [Specwright: Spec-Driven Development That Closes the Loop — Obsidian Owl Engineering](https://obsidian-owl.github.io/engineering-blog/posts/specwright-spec-driven-development-that-closes-the-loop/)
- [Testing AI-Generated Code: The Self-Confirming Loop — codemyspec.com](https://codemyspec.com/blog/agentic-testing)
- [Spec + TDD: The Combination That Actually Produces Shippable AI Code — Augment Code](https://www.augmentcode.com/guides/spec-tdd-shippable-ai-generated-code)
- [QA in AI Assisted Development: Safety through Deterministic Verification — BitDive](https://bitdive.io/blog/quality-assurance-ai-assisted-software-development/)
- [CI/CD for AI Agents: How to Integrate Agent Orchestration into Your Pipeline — Augment Code](https://www.augmentcode.com/guides/cicd-ai-agents-pipeline-integration)
- [How AI Agent Verification Prevents Production Bugs Before Merge — Augment Code](https://www.augmentcode.com/guides/ai-agent-pre-merge-verification)
- [Micro-Specs: The Pattern That Significantly Improves AI Agent Test Coverage in High-Risk Modules — Augment Code](https://www.augmentcode.com/guides/micro-specs-pattern-ai-agent-test-coverage)
- [Swarm Orchestrator: Verification and governance layer for AI coding agents — GitHub](https://github.com/tfxdevelopment/swarm-orchestrator)
- [Specwright: Craft quality software with AI discipline — GitHub](https://github.com/Obsidian-Owl/specwright)
- [Agent Verifier: Verify code against organizational policies and best practices — GitHub](https://github.com/Aurite-ai/agent-verifier)
- [Security Drift in Iterative LLM-Driven Code Refinement — AgentPatterns.ai](https://agentpatterns.ai/security/security-drift-iterative-refinement/)
- [Spec-Driven Development with AI: Complete 2025 Guide — dplooy](https://www.dplooy.com/blog/spec-driven-development-with-ai-complete-2025-guide)
- [CrossCheck: Self correcting system for AI coding loops — GitHub](https://www.github.com/sburl/CrossCheck)
- [Spec-Driven Development. The Fifth Generation of Programming — Medium](https://medium.com/google-cloud/spec-driven-development-54cdf7e0b088)
