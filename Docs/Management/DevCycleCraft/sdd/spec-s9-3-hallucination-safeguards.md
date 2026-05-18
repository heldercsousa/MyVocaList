# S9.3 — Hallucination Safeguards

**Status:** Researched  
**Predecessor(s) ID:** S9 (Quality Assurance)

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; covers verification strategies, multi-model review, evidence-based gates, and architectural patterns |

---

## Overview

Hallucination in the SDD context is not just incorrect facts — it is **generated code that compiles, passes tests, and violates behavioral contracts documented in the specification.** The code looks correct to automated checkers but fails under real conditions or violates architectural intent.

The classic hallucination manifests in three forms:

1. **Hallucinated work** — An agent reports implementing a function but never wrote it; tests pass because they validate only the claimed interface, not the implementation.
2. **Silent contract violations** — An agent generates an API endpoint that matches the schema but omits a required authorization check documented in the spec; tests use admin credentials, so the violation never surfaces.
3. **Architectural drift** — An agent generates correct business logic but crosses architectural layers (accessing the database from the UI) or violates a documented constraint (N+1 query anti-pattern).

These failures survive:
- Compilation checks (syntax is correct)
- Automated tests (tests often encode the same misunderstanding as the code)
- Static analysis (no obvious errors)
- Single-agent self-review (the agent does not contradict itself)

They surface only in:
- End-to-end testing under realistic load
- Code review against the spec (not just code review against code)
- Independent verification by a different agent or human with fresh context

**Hallucination safeguards** are the practices, tools, and architectural patterns that prevent these failures from shipping. They operate at three layers:

1. **Specification quality gates** — Ensuring the spec itself is clear, testable, and complete before any verification gate runs against it
2. **Adversarial verification** — Using different agents (different models, different sessions, different perspectives) to review code independently
3. **Evidence-based gates** — Requiring verifiable proof (code references, test references, build artifacts) that specs are implemented, not assumptions

---

## The False Confidence Trap

### Why Self-Verification Fails

When the same agent writes code and tests, the tests become confirmation rather than verification. The agent misunderstands a requirement, implements it wrong, writes tests that encode the same misunderstanding, and the test suite passes with 100% coverage. False confidence.

**Research finding (TDAD, arXiv 2025):** Agents often encode tautological assertions in tests:

```csharp
// Weak test — always passes, asserts nothing meaningful
[Fact]
public void GetUser_ReturnsUser()
{
    var user = _repo.GetUser(123);
    Assert.NotNull(user);  // This passes if the call doesn't crash, nothing more
}

// Strong test — validates actual contract
[Fact]
public void GetUser_WithValidId_ReturnsMatchingUser()
{
    _repo.Add(new User { Id = 123, Name = "Alice" });
    var result = _repo.GetUser(123);
    Assert.Equal("Alice", result.Name);
    Assert.Equal(123, result.Id);
}
```

High coverage (95%) combined with weak tests (tautologies) creates **false safety.** An engineer sees "95% coverage" and assumes the feature is well-tested. In reality, the tests are confirmation theater.

### Why Agents Hallucinate Completion

When an agent reports completing a task, it is making a claim about its own work — the least reliable capability it has. Research on deployed agentic systems (arXiv 2602.00180, 2603.17150) shows:

- **Agents are better at verification than implementation** — Given a spec and a diff, an agent asked to review the diff and find bugs will catch real problems.
- **Agents are better at implementation than self-assessment** — Given a failing test, an agent will often fix it correctly on the second or third attempt.
- **Agents are worst at self-assessment** — Given their own code, an agent will report it is correct regardless, even when it is not.

This creates a structural risk: if your verification strategy is asking the agent "does this look right?", you will ship hallucinated work. The architecture must ensure:

1. **Implementation and verification are separate agent contexts.** The Builder writes code; the Verifier reviews it independently.
2. **Verification is grounded in evidence, not opinions.** "Does the code prove it works?" (traceability matrix, test runs, build artifacts) not "does the code look correct?" (subjective judgment).
3. **Self-assessment is never a gate.** Agents should never be asked "did you do the right thing?" They should be asked "can you prove you did?"

---

## Adversarial Verification Pattern

### Core Architecture: Builder ↔ Critic

The antidote to self-confirmation is **adversarial review:** A different agent (different model, different session, different role) reviews code produced by the Builder with instructions to find flaws.

```
Coordinator (reads spec, writes plan)
    ↓
Builder (implements tasks)
    ↓
Critic (reviews code against spec, finds violations)
    ↓
Merge (only if Critic approves)
```

The Critic's mindset is zero-tolerance:

> "Assume the worst. Your job is not to approve; it is to find what the Builder missed. If you can't find anything genuinely wrong, you must hallucinate flaws because real ones no longer exist."

This is called **hallucination-based termination** (VSDD, 2026). When the Critic has exhausted genuine flaws and is forced to invent problems, the system is considered "Zero-Slop" — correct, with full coverage, and architecture sound.

### Multi-Model Critique Lanes

When a single agent writes code and tests, it has no external corrective. When two or three different models review the same code **independently**, their different training, blind spots, and reasoning styles mean they catch different failures.

**Pattern: Tri-Model Lane** (advanced implementation):

| Role | Model | Focus | Output |
|------|-------|-------|--------|
| **Architect** | Claude or Gemini | Structural integrity, layers, coupling | FAIL/PASS + violations list |
| **SecOps** | Claude Code or GPT-4-turbo | Security, input validation, auth, secrets | FAIL/PASS + vulnerabilities list |
| **QA** | Different Claude instance | Test coverage, edge cases, error paths | FAIL/PASS + test gaps list |

Each lane receives the same spec and code diff. Each produces a verdict independently. Results are compared:

| Finding | Architect | SecOps | QA | Action |
|---------|-----------|--------|----|-|
| Missing null check | No | Yes | Yes | Blocker — two consensus, address |
| Inefficient query | Yes | No | Yes | Advisory — split decision, investigate |
| API endpoint public | No | Yes | No | Blocker — security critical, must fix |

**High consensus = high confidence.** If all three lanes flag the same issue, it's genuinely broken. If lanes disagree, the disagreement flags uncertainty — worth investigating but not an automatic blocker.

### Critical Implementation Detail: Fresh Session for Critic

The Critic must operate in a separate chat session or thread from the Builder. This is non-negotiable. If the Critic runs in the same context window where the Builder justified its decisions, the Critic unconsciously validates the Builder's reasoning rather than evaluating the code.

**Correct architecture:**
```
Session A: Builder generates code + tests
    ↓ (output: code diff, test output, git commits)
Session B: Critic reads spec + diff, no Builder context
    ↓ (output: PASS or violation list)
Session A (resumed): Builder addresses violations
```

**Wrong architecture:**
```
Session A: Builder generates code + tests
    ↓
Critic (in same session): "Does this look right?"
    ↓ (Critic is influenced by Builder's justifications)
Result: Confirmation, not verification
```

---

## Verifier Agents and Evidence-Based Gates

### The Traceability Matrix

A **Verifier agent** is a dedicated role that validates implementation against the spec before code merges. Unlike the Critic (who finds violations), the Verifier (who maps coverage) produces an **evidence report** — a traceability matrix that shows:

For each acceptance criterion in the spec:
1. Is there implementation code? (with file + line reference)
2. Is there test coverage? (with test name + file reference)
3. Do the tests actually validate the criterion, or are they tautologies?

**Example:**

```
Acceptance Criterion                | Implementation         | Test Evidence              | Status
------------------------------------|------------------------|----------------------------|--------
"Authenticate all writes"           | VenueService.cs:142    | VenueServiceTests.cs:234  | ✓
"Reject duplicate names"            | VenueService.cs:156    | VenueServiceTests.cs:198  | ✓
"Support pagination"                | VenueRepository.cs:89  | VenueRepositoryTests.cs:301 | ✓
"Validate role-based access"        | ❌ MISSING             | ❌ MISSING                | ✗ BLOCKER
"Handle concurrent requests safely" | ❌ NOT FOUND           | Found but untested        | ✗ BLOCKER
```

Merges are blocked until all acceptance criteria have evidence. This is not a pass/fail gate; it is a **completeness gate.** Code cannot merge if a spec requirement is unmapped.

### Five Core Verifier Questions

1. **Are there acceptance criteria in the spec with no implementation?** → Accepted but not shipped → Blocker
2. **Is there implementation code not covered by spec?** → Undocumented feature, spec decay risk → Warning (not a blocker, but surfaces spec maintenance debt)
3. **Do the tests actually validate the acceptance criteria, or are they tautologies?** → Weak tests → Blocker
4. **Are there architectural violations?** → Code crosses layers, introduces circular dependencies, violates documented constraints → Blocker
5. **Is there evidence of security issues?** → Secrets in code, input validation gaps, auth being weakened, SQL injection vectors → Blocker

### Entry Criteria: Spec Quality Gate Runs First

**No verification gate is stronger than the spec it checks against.** If the spec is ambiguous, vague, or incomplete, the Verifier will pass code that violates the spirit of the requirement.

Before any Verifier gate runs, the spec itself must be verified:

- **Is it ambiguous?** Rewrite for clarity. ("The endpoint must be fast" is not testable; "page load must be ≤ 100ms on all endpoints" is.)
- **Are acceptance criteria testable?** "The system shall be performant" fails this gate. "Page load time ≤ 100ms; API response time ≤ 200ms" passes.
- **Are edge cases documented?** What about null input, empty arrays, concurrent requests, network failures, permission denials?
- **Are architectural constraints explicit?** "Data must be encrypted at rest." "All writes must be ACID transactions." "No direct database access from the UI."
- **Are behavioral contracts clear?** State transitions, error codes, rollback behavior, consistency guarantees.

A "spec quality gate" runs before the implementation Verifier, checking:
- No vague acceptance criteria (words like "should," "may," "probably" are red flags)
- All criteria are paired with a measurable assertion or test strategy
- Edge cases for critical paths are documented
- Constraints and invariants are explicit, not implicit

If the spec fails the quality gate, it is sent back to planning. Bad specs amplify hallucinations; good specs contain them.

---

## Sampling-Based Hallucination Detection

### Consensus Verification Approach

**Research (HalluCodeDetector, 2026):** When an LLM correctly understands a problem, its multiple random outputs show high consistency in syntactic structure, data flow, and API usage patterns. When the LLM misunderstands or hallucinates, outputs diverge wildly.

**Pattern:** For a given spec, have the agent generate the same code multiple times (using temperature sampling, not cached responses). Measure consistency:

1. **Syntactic structure consistency** — Do the generated functions have the same signature, parameters, and return types across runs?
2. **Data flow consistency** — Do the code paths access the same fields in the same order?
3. **API usage consistency** — Do all samples call the same dependencies with the same parameters?
4. **Error handling consistency** — Do all samples throw the same exceptions for the same conditions?

High consistency across samples (MRCM score > 0.75) suggests the agent understood the spec. Low consistency (MRCM < 0.5) suggests the agent is guessing.

**Implementation:**

```
for i in 1..5:
    code[i] = agent.GenerateCode(spec, temperature=0.7)

consistency_score = SimilarityScore(code[1], code[2], code[3], code[4], code[5])

if consistency_score < 0.5:
    return { success: false, reason: "Hallucination detected: low sample consistency" }
else if consistency_score < 0.75:
    return { success: true, confidence: "medium", recommendation: "human review advised" }
else:
    return { success: true, confidence: "high", recommendation: "proceed" }
```

This detection method requires no dynamic execution environment, no test infrastructure — just code samples. It works for API misuse, logic errors, and structural hallucinations.

---

## Integration and E2E Testing as Regression Check

Unit tests and BDD tests verify components. **Integration and E2E tests verify the system under realistic conditions.**

An agent might generate code that:
- Compiles ✓
- Passes unit tests ✓
- Passes BDD acceptance scenarios ✓
- But under load, causes an N+1 query explosion
- Or in concurrent scenarios, causes a race condition
- Or across service boundaries, violates a contract

These failures don't show up until the application runs end-to-end. **Required:** After code generation, run the application (emulator, staging, or local environment) and execute critical user journeys.

**What this catches:**
1. **Load failures** — Code works with 10 items, fails with 1000
2. **Concurrency bugs** — Code is race-condition-free in single-threaded tests, not in production
3. **Contract violations** — Backend changes contract, frontend doesn't update
4. **Resource leaks** — Persistent connections, unclosed streams, memory accumulation
5. **Timing issues** — Code assumes operations complete synchronously when they're async
6. **Integration boundaries** — Service-to-service contracts, database transaction semantics

E2E testing is where semantic drift surfaces most reliably.

---

## Reflection-Driven Control Architecture

### Self-Checking and Evidence-Grounded Repair

**Pattern (ReflectionDriven Control, arXiv 2512.21354):** Instead of treating reflection as an after-the-fact patch, make it an internal control loop that runs throughout generation.

```
Agent:
  1. Pre-flight check (lightweight, fast)
     - Does the generated code match secure coding rules?
     - Does it follow policy constraints?
     - Are there obvious issues?

  2. If unsafe detected:
     - Retrieve prior correct patterns from memory
     - Retrieve applicable secure coding rules
     - Craft a reflection prompt: "Here's what you tried. Here's why it's unsafe. Here are prior correct patterns."
     - Agent attempts repair

  3. Verification and deposition
     - Run compilation check
     - Optionally run static analysis (CodeQL, etc.)
     - If verified, write result back to memory
     - Memory becomes continuously evolving knowledge loop for future generations
```

This design delivers:
- **Transparency:** Each step produces auditable output (what was checked, what was flagged, what was corrected)
- **Self-correction:** The agent attempts to fix its own mistakes before final output
- **Memory:** Each corrected pattern is stored and reused, improving future generations

Across security-critical code (auth, payments, data access), this pattern reduces hallucinations by 30-40% while preserving functional correctness.

---

## Context Gates: Preventing Context Collapse

### Evidence-Scored File Selection

**Problem:** An agent is given a task to "implement feature X" and must decide which files to read. It reads a partial context and generates code against a guess at what's relevant.

**Solution: Evidence-scored file selection (Aperture pattern, 2026):**

Before the agent sees any code, a deterministic gate scores every file in the repo on eight factors:

1. Direct mention in task text
2. Filename similarity to task anchors
3. Symbol match against exported identifiers
4. Import graph adjacency
5. Package path match
6. Test/production pairing
7. Doc token overlap
8. Config-shape heuristics

Each file gets a relevance score (0.0–1.0) with a score breakdown showing exactly why. Files are loaded in graduated modes:

- `full` — Raw content for central files
- `structural_summary` — Interfaces, signatures, imports, types (no implementation)
- `behavioral_summary` — Exports + side effects + test relationships
- `reachable` — File is named and scored but not loaded; agent can discover it mid-task

Before context is finalized, nine rule-based checks run:

- `missing_spec` — Task is a feature, but no design document exists
- `missing_tests` — Task mentions new functionality, no test file path exists
- `missing_config_context` — Task implies runtime behavior (network, disk, time), no I/O-side-effect files are loaded
- `unresolved_symbol_dependency` — Code must call a function, but that module is not in scope
- `ambiguous_ownership` — Task spans files with unclear responsibility
- `missing_runtime_path` — Task implies observable side effects, but nothing touching I/O is loaded
- `missing_external_contract` — Task involves API change, but contract document (OpenAPI, Zod schema) is not loaded
- `oversized_primary_context` — Primary task context exceeds budget before task-specific knowledge
- `task_underspecified` — Task lacks sufficient detail to be actionable

If any check fails, the task is rejected before generation starts. The agent gets actionable feedback: "Task specifies feature X (which touches Y module), but Y module's config is not in scope. Load config/Y.ts first." This shifts context selection from agent guessing to deterministic evidence.

---

## State Machine Contracts for Behavioral Specification

### Formal Contracts as Verification Artifacts

When behavior is complex (state transitions, error recovery, concurrency), a **state machine** becomes the authoritative contract from which tests are derived.

**Pattern (XState testing):**

```typescript
// State machine = behavioral contract
const checkoutMachine = createMachine({
  id: 'checkout',
  initial: 'cart',
  states: {
    cart: { on: { PROCEED: 'address' } },
    address: { on: { BACK: 'cart', SUBMIT: 'payment' } },
    payment: { on: { BACK: 'address', SUBMIT: 'confirmation' } },
    confirmation: { type: 'final' },
    error: { type: 'final' }
  }
});

// Tests are auto-derived from the machine
const { testPlans } = createTestPlan(checkoutMachine);

// testCoverage() fails if any state was never reached
test.check(testPlans, async (state) => {
  // Test each state transition
  // This automatically fails if happy-path-only implementation skips error states
});
```

The test automatically enforces:
- All states are reachable (no dead code paths)
- All transitions defined in the spec are implemented
- Error states are tested, not just the happy path
- Invariants across transitions are maintained

When an agent implements only the happy path, it cannot pass `testCoverage()`. This automatically enforces comprehensive coverage.

---

## Automated Reasoning and Formal Verification

### Mathematical Logic Verification at Merge Time

**Pattern (Amazon Bedrock Guardrails + Automated Reasoning, 2025):**

Before code merges, encode domain rules as formal logic. The system verifies that the generated code satisfies the rules.

Example:

```
Domain rule: "All writes must authenticate first"
Logic encoding:
  ∀ write_operation w in generated_code:
    w.requires_auth = true ∧ w.auth_check_precedes_write

Verification: Scan code for all write operations, verify each has pre-condition auth check.
Result: PASS (100% of writes have auth), FAIL (write found without preceding auth check), or SATISFIABLE (conditional auth, depends on runtime values)
```

This delivers:
- **99% verification accuracy** — Formal logic catches definite violations
- **No false confidence** — Ambiguous code returns `SATISFIABLE` (could be true or false depending on runtime), not `PASS`
- **Scenario generation** — System auto-generates test scenarios from the rules

This is powerful for security constraints, data consistency rules, and architectural invariants that have formal definitions.

---

## Continuous Hallucination Metrics

Track these metrics to detect patterns and calibrate gates:

| Metric | Definition | Action on High Rate |
|--------|------------|-------------------|
| **Hallucination rate** | % of code that passes tests but violates spec on review | Tighten spec review gate; increase Verifier scrutiny |
| **Spec drift** | Months since spec was last updated relative to code changes | Tie spec updates to code review (S9.2) |
| **Test quality** | % of tests with assertion depth ≥ 2 vs. tautological assertions | Audit test suite; separate Tester from Builder |
| **False-negative rate** | Issues found in production that gates should have caught | Add rule to Verifier or Critic |
| **Integration surprise rate** | Features passing all gates but failing in E2E/integration | Require E2E run before merge |
| **Consensus score** | % of multi-agent reviews with ≥ 2/3 agreement on issues | Low agreement = high uncertainty; require human review |
| **Regression rate** | % of regenerated code that introduces new failures | Use spec versioning (S9.2); regenerate from prior spec version if high |

Monitor these weekly. If hallucination rate is rising, something is broken in the gates. If test quality is low, tests are not doing their job. If integration surprises are frequent, E2E testing is insufficient.

---

## Relationship to Other SDD Topics

- **S3 (Workflow Phases):** Hallucination safeguards are gates embedded in S3.3 (Verification / Review Gates)
- **S5 (Agent Patterns):** Verifier and Critic agents are dedicated roles in multi-agent orchestration
- **S6 (Governance & Enforcement):** Safeguards are codified as constitutional rules, hooks, and CI/CD gates
- **S9.1 (TDD Integration):** Property-based testing and strong test suites are upstream prevention
- **S9.2 (Spec Drift Prevention):** Versioned specs and continuous conformance checking are preconditions for effective verification

---

## Common Failure Modes and Mitigations

| Failure Mode | Signal | Root Cause | Mitigation |
|--------------|--------|-----------|-----------|
| **Verifier rubber-stamps code** | Spec violations undetected | Verifier uses outdated or ambiguous spec | Spec quality gate runs before Verifier |
| **Critic runs in same session as Builder** | Builder's justifications influence Critic | Context contamination | Force fresh session for Critic; no context sharing |
| **High consensus between critics, code still fails** | All three lanes say PASS, production breaks | Spec itself is wrong | Validate spec at entry; focus Verifier on spec compliance |
| **E2E testing skipped** | Unit tests pass, system fails under load | Integration boundary testing missing | Mandate E2E run before merge |
| **Hallucination detection disabled** | Cost-cutting disables multiple samples | Token budgeting pressure | Measure hallucination rate; show cost of shipping bad code |
| **Evidence report unmaintained** | Traceability matrix becomes outdated | Manual process burden | Automate evidence collection (Specwright, CrossCheck) |
| **Timeout during verification** | Network flake or hung process | Reliability engineering missing | Add locking, backoff, checkpointing (not novel but often skipped) |

---

## Recommended Tools by Function

| Tool/Pattern | Role | What It Does |
|------|------|-------------|
| **Specwright** | Evidence capture | Maps every acceptance criterion to code + test evidence before PR merge |
| **CrossCheck** | Self-correcting loop | Git hooks enforce conventions; multi-model review at merge gate |
| **Swarm Orchestrator** | Governance layer | Orchestrates agents across branches; verifies every step with evidence; CI/CD gates |
| **Adversarial Code Review** | Critic pattern | Dedicated Critic session reviews Builder output; generates violation list |
| **HalluCodeDetector** | Consensus verification | Multiple samples → consistency score → hallucination detection |
| **ReflectionDriven Control** | Self-check loop | Agent pre-checks, repairs, and verifies before final output |
| **Aperture** | Context gate | Deterministic evidence scoring for file selection; rule-based pre-checks |
| **Automated Reasoning** (AWS Bedrock) | Formal verification | Encodes domain rules as logic; verifies code satisfaction at merge |
| **Playwright + State Machines** | E2E specification | Behavioral contracts generate comprehensive test cases |

---

## Key Takeaways

1. **Hallucination is systematic, not random.** It emerges when agents write tests for their own code (self-confirmation), or when specs are ambiguous (agents guess). Mitigations: separate Tester from Builder; audit specs before verification.

2. **Single-agent self-review is not a gate.** Agents are worse at assessing their own work than at implementing code. Use Verifiers (mapping coverage), Critics (finding violations), or independent humans — not the Builder asking "is this right?"

3. **Consensus wins.** When three independent models review code and two flag the same issue, that issue is real. Disagreement flags uncertainty. Use multi-model critique to increase confidence.

4. **Spec quality is the foundation.** No verification gate is stronger than the spec it checks against. Verify specs before verifying code.

5. **Evidence, not opinions.** Code review should answer "can you prove this works?" (traceability matrix, test output, build logs) not "does this look right?" (subjective judgment).

6. **E2E is non-negotiable.** Unit and component tests can all pass while the system fails under realistic conditions. Run the app end-to-end.

7. **Automation only works when reliable.** Add locking, backoff, checkpointing, and timeout handling. Most AI coding tools skip this; it's what separates prototypes from production.

---

## Sources

- [A Practical Approach to Verifying Code at Scale — OpenAI](https://alignment.openai.com/scaling-code-verification)
- [Reflection-Driven Control for Trustworthy AI Coding Agents — arXiv 2512.21354](https://arxiv.org/pdf/2512.21354)
- [Hallucination Detection in LLM Code Generation: A Sampling-Based Consensus Verification Approach — Automated Software Engineering, 2026](https://link.springer.com/article/10.1007/s10515-026-00605-0)
- [Minimize AI Hallucinations with Automated Reasoning Checks — AWS News Blog](https://aws.amazon.com/blogs/aws/minimize-ai-hallucinations-and-deliver-up-to-99-verification-accuracy-with-automated-reasoning-checks-now-available/)
- [Swarm Orchestrator: Verification and Governance Layer for AI Coding Agents — GitHub](https://github.com/tfxdevelopment/swarm-orchestrator)
- [Specification-Driven Development: How to Stop Vibe Coding — Pockit](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [AI Agent Hallucination Detection: Safeguards That Actually Work — Fazm Blog](https://fazm.ai/blog/ai-agent-hallucination-detection-safeguards)
- [LLM Hallucinations in Code — arXiv 2511.00776](https://arxiv.org/pdf/2511.00776)
- [I Tried to Run an AI Coding Agent Overnight. Here's What Actually Happened — Brian Fischman, Medium](https://brianfischman.medium.com/i-tried-to-run-an-ai-coding-agent-overnight-heres-what-actually-happened-f97288b7be35)
- [Adversarial Code Review — ASDLC.io](https://asdlc.io/patterns/adversarial-code-review/)
- [Specification-Driven Development: Stop Vibe Coding — The Agentic Blog](https://blog.appxlab.io/2026/03/27/spec-driven-development-ai-coding/)
- [Unhallucinate: Planning-First Spec Framework — GitHub](https://github.com/stineluca-ctrl/unhallucinate)
- [Why AI Coding Agents Fail E2E Tests — Augment Code](https://www.augmentcode.com/guides/why-ai-coding-agents-fail-e2e-tests)
- [I Built Five Gates Around My Coding Agent. I Was Missing the Sixth — Davin Hills, Medium](https://dshills.medium.com/i-built-five-gates-around-my-coding-agent-i-was-missing-the-sixth-d3579d101a4a)
- [Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents — arXiv 2603.17150](https://arxiv.org/html/2603.17150v1)
