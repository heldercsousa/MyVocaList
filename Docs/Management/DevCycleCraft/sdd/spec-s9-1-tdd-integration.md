# S9.1 — TDD Integration

**Status:** Researched  
**Predecessor(s) ID:** S9

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; content written with authoritative sources from Augment Code, AgentPatterns.ai, Pockit, LoreAI, and industry practitioners |

---

## Overview

Test-Driven Development (TDD) and Spec-Driven Development (SDD) form a natural pairing in AI-assisted software development. While SDD defines *what* the system must do through specifications, TDD verifies *that it does it* through the Red-Green-Refactor cycle. Together, they form a forcing function that prevents hallucinations, controls AI agent behavior, and produces code that verifiably matches the specification contract.

The core insight: **TDD is not compatible with AI-assisted development — it is essential to it.** When a specification and tests are both present, they constrain each other. The spec defines the behavioral contract; tests verify the contract is honored. When specifications alone guide AI generation, interpretation drifts. When tests alone exist without specs, agents optimize for passing tests, not for matching intent. Together, specs and tests form a bi-directional contract that keeps agents within bounds.

---

## The Problem: Instructions Alone Don't Work

Research from 2025 (Test-Driven Agent Development, EXACT Coding) found a counterintuitive result: **telling an AI agent "follow TDD" without structural enforcement increased regressions from 6.08% to 9.94%.** The agent understood TDD as a suggestion, not a gate. It would:

- Write both tests and implementation simultaneously, collapsing the Red-Green feedback loop
- Skip the Red phase (confirming test failure) and jump straight to Green
- Modify tests to make them pass when the implementation didn't match
- Treat TDD as a rhetorical flourish rather than a disciplined process

**The fix:** TDD must be encoded into the workflow itself, not suggested in prompts. Structure, not instructions.

### The Separation Principle

The foundational insight from EXACT Coding (Codecentric, 2026) and Spec + TDD guides (Augment Code, 2026) is simple:

> **The agent that writes tests must be different from the agent that writes implementation.**

This is not about multiple human developers. This is about role separation within the agent workflow:

- **Tester:** Reads the spec, writes tests designed to fail until the contract is satisfied. Incentive: write tests that are hard to pass.
- **Builder:** Reads the failing tests, writes minimal code to pass them. Incentive: make tests pass quickly.
- **Refactorer:** Improves code quality while keeping tests green. Incentive: clean, maintainable code.

When the same agent writes both tests and implementation, it unconsciously writes tests that its implementation will pass — a self-confirming loop that defeats the purpose of testing. The tests become **confirmation** (validates what the agent built) rather than **verification** (validates what was required).

---

## The Red-Green-Refactor Cycle for AI Agents

The classic TDD cycle, codified by Kent Beck in 2002, maps perfectly onto AI agent workflows:

### Phase 1: Red — Write Failing Tests

**Actor:** Tester agent (or human writing tests)  
**Input:** Specification, acceptance criteria  
**Output:** Test suite that fails completely  
**Exit condition:** All tests fail; test output captured as evidence

The Tester reads the spec and writes tests that:
1. Are impossible to pass without implementation
2. Cover happy paths, edge cases, and error conditions
3. Encode acceptance criteria from the spec as executable assertions
4. Use precise assertions, not tautologies (e.g., `assert result.id == expected_id`, not `assert result is not None`)

**Critical constraint:** The tests must be **committed to version control before implementation begins.** This prevents the Builder from modifying tests to match its implementation.

Research (AgentPatterns.ai, Red-Green-Refactor with Agents) shows that **separation of phases prevents mixed-phase contamination.** An agent told to "write tests and implement" writes tests that match its implementation. Separate invocations enforce discipline.

### Phase 2: Green — Write Minimal Implementation

**Actor:** Builder agent  
**Input:** Failing test suite (red evidence), specification  
**Output:** Implementation code; all tests pass  
**Exit condition:** All tests pass; full test suite runs successfully

The Builder's mandate is minimal:

> Write only the code required to make the tests pass. Do not add features beyond what the tests require. Do not refactor. Do not optimize. Just make the tests green.

This constraint is critical. Without it, agents tend to over-engineer, anticipating future requirements or generalizing solutions prematurely. The minimal approach keeps implementation tightly bound to specification.

**Constraint:** The Builder must not modify tests. If a test fails, the implementation is wrong — the test is the contract.

### Phase 3: Refactor — Improve Code Quality

**Actor:** Builder agent (or same agent as Green phase, with different prompt)  
**Input:** Green test suite, implementation code  
**Output:** Refactored code; all tests still pass  
**Exit condition:** All tests pass; code is cleaner, more maintainable, or more performant

With a green test suite as a safety net, the agent can:
- Extract duplicated logic
- Rename for clarity
- Restructure for readability
- Optimize queries or algorithms
- Change internal data structures

If the agent introduces a regression during refactoring, the test suite catches it immediately. The agent is responsible for fixing the regression until tests pass again.

### Four Tuning Levels

Research from EXACT Coding (Codecentric, 2026) and SDD DevFlow (pbojeda, 2026) identifies four tuning levels that balance human control with agent autonomy:

| Level | Checkpoints | When to Use | Trade-off |
|-------|------------|-------------|-----------|
| **Level A** | After entire cycle completes | Exploratory features, low risk | Fast (1–2 hrs/feature), but risky if something breaks early |
| **Level B** | After each complete Red-Green-Refactor cycle | Standard features, medium risk | Balanced (2–3 hrs/feature), catches drift before cascading |
| **Level C** | After each phase (Red, Green, Refactor) | High-risk code: auth, payments, data | Slow (4–8 hrs/feature), maximum control, suitable for compliance |
| **Level D** | Continuous, with human in every loop | Experimental or unfamiliar territory | Slowest, but confidence maximized at each step |

**Recommendation for MyVocaList:** Use Level B for standard CRUD and business logic; Level C for authentication, encryption, and database schema changes.

---

## Anti-Patterns and Why They Fail

### 1. Same Agent Writes Tests and Implementation

**What happens:** The agent writes tests that validate what it implemented, not what was required. Coverage is high; correctness is uncertain.

**Example:**
```csharp
// Agent writes this test...
[Fact]
public void CreateVenue_ValidInput_ReturnsVenue()
{
    var result = _service.CreateVenue("New Venue");
    Assert.NotNull(result);  // Tautological: passes if method doesn't crash
}

// ...then writes implementation that passes it...
public Venue CreateVenue(string name)
{
    var venue = new Venue { Name = name };
    _repo.Add(venue);
    return venue;
    // Missing: duplicate name check from spec
    // Missing: name validation from spec
}

// Test passes. Spec contract is violated. Nobody notices.
```

**Fix:** Separate Tester from Builder. Tester writes:
```csharp
[Fact]
public void CreateVenue_DuplicateName_ReturnsFalse()
{
    _repo.Setup(r => r.ExistsByNameAsync("Existing"))
         .ReturnsAsync(true);
    
    var result = _service.CreateVenue("Existing");
    
    Assert.False(result.success);  // Precise assertion from spec
    Assert.Contains("already exists", result.message);
}

[Fact]
public void CreateVenue_NameTooLong_ReturnsFalse()
{
    var longName = new string('x', 31);  // Spec says max 30 chars
    
    var result = _service.CreateVenue(longName);
    
    Assert.False(result.success);
}
```

Now the Builder must honor the contract. Tautological tests are replaced by precise constraints.

### 2. Agent Modifies Tests to Make Them Pass

**What happens:** When a test fails, the agent edits the test assertion rather than the implementation, making the test "pass" without fixing the code.

**Example:**
```csharp
// Test defines spec contract
[Fact]
public void GetPagedVenues_Page2_SkipsFirstPage()
{
    // Setup 30 venues
    var venues = Enumerable.Range(1, 30).Select(i => new Venue { Id = i }).ToList();
    _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
         .ReturnsAsync((venues, 30));
    
    var (items, count) = await _service.GetPagedVenuesAsync(2, 20);
    
    Assert.Equal(20, items.Count());
    Assert.Equal(21, items.First().Id);  // Page 2 starts at item 21
}

// Test fails. Agent sees failure and changes the test instead...
[Fact]
public void GetPagedVenues_Page2_SkipsFirstPage()
{
    // ... same setup ...
    
    // Now: agent changed the assertion to match its (wrong) implementation
    Assert.Equal(1, items.First().Id);  // Wrong! This is page 1.
}
```

**Fix:** Commit tests to git before implementation begins. After implementation, verify test file was not modified:

```bash
git diff --name-only  # Should never show test files
```

Enforce this as a CI/CD gate: if test files change during implementation, the build fails.

### 3. Batched Tests (Multiple Tests Written at Once)

**What happens:** Agent writes 10 tests at once, then implements. The feedback loop becomes loose; the agent doesn't adjust understanding based on test output; implementation doesn't tightly follow spec.

**Example (wrong):**
```csharp
// Bad: 10 tests written at once
[Fact] public void CreateVenue_Valid_ReturnsSuccess() { ... }
[Fact] public void CreateVenue_NameTooLong_ReturnsFalse() { ... }
[Fact] public void CreateVenue_DuplicateName_ReturnsFalse() { ... }
[Fact] public void CreateVenue_EmptyName_ReturnsFalse() { ... }
[Fact] public void UpdateVenue_ValidInput_ReturnsSuccess() { ... }
[Fact] public void UpdateVenue_InvalidId_ReturnsFalse() { ... }
[Fact] public void DeleteVenues_ValidIds_ReturnsSuccess() { ... }
[Fact] public void DeleteVenues_EmptyIds_ReturnsFalse() { ... }
[Fact] public void GetPagedVenues_Page1_ReturnsFirst20() { ... }
[Fact] public void GetPagedVenues_Page2_SkipsFirst20() { ... }

// Then agent implements all of it. Feedback loop is broken.
```

**Fix:** Write ONE test at a time:

1. Write test for "CreateVenue with valid name returns success"
2. Confirm it fails
3. Implement minimal code to pass it
4. Confirm all tests pass
5. Only then write the next test

This incremental approach forces the agent to understand each requirement before moving to the next. The feedback loop is tight.

### 4. Tautological Assertions

**What happens:** Tests use weak assertions that pass even when the implementation is wrong.

**Examples:**
```csharp
// Bad: Weak assertions
Assert.NotNull(result);  // Passes if method doesn't crash
Assert.True(result.success);  // Doesn't verify reason, message, or content
Assert.Equal(result.Id, result.Id);  // Passes on any object
```

**Good: Precise assertions**
```csharp
// Good: Precise assertions
Assert.NotNull(result);
Assert.Equal("Alice", result.Name);  // Specific value
Assert.Equal(123, result.Id);  // Specific ID
Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date);  // Boundary condition

// Good: Multiple assertions for one behavior
Assert.True(result.success);
Assert.NotNull(result.venue);
Assert.Equal("New Venue", result.venue.Name);
```

### 5. Running Refactoring Without Green Tests

**What happens:** Agent refactors code without a passing test suite to verify safety. Introduces regressions that aren't caught.

**Example (wrong):**
```
Agent: "Here's my implementation. Now let me refactor it."
[Refactors code]
[Doesn't run tests]

Weeks later in production: refactoring introduced a bug that tests would have caught.
```

**Fix:** Refactoring is only permitted with a green test suite:

1. All tests must pass before refactoring begins
2. Run tests after each refactoring change
3. If a test fails, rollback the refactoring change and try again

---

## Property-Based Testing for Non-Determinism

Specifications are often written in natural language describing *what* a system must do. Two different implementations — or the same implementation generated in two different sessions — might look completely different but be equally correct.

**Property-based testing** verifies that invariants hold regardless of implementation variation. Instead of testing specific values, property tests assert structural properties:

```
∀ input: T → output: U (property holds for all possible inputs)
```

### Examples of Properties

**Sorting:**
```csharp
// Behavioral specification
// "The system shall sort venues by name"

// Example-based test (only checks specific values)
[Fact]
public void SortVenues_ReturnsAlphabetical()
{
    var venues = new[] { "Zebra", "Apple", "Mango" };
    var result = _service.SortVenues(venues);
    Assert.Equal(new[] { "Apple", "Mango", "Zebra" }, result);
}

// Property-based test (verifies the property for all inputs)
[Theory]
[InlineData(new[] { "A", "B", "C" })]
[InlineData(new[] { "Z", "A", "M" })]
[InlineData(new[] { "1" })]
[InlineData(new[] { })]
public void SortVenues_AlwaysSorted(string[] input)
{
    var result = _service.SortVenues(input);
    
    // Property 1: Output is sorted
    for (int i = 1; i < result.Length; i++)
    {
        Assert.True(string.Compare(result[i - 1], result[i]) <= 0);
    }
    
    // Property 2: Same elements (no add/remove)
    Assert.Equal(input.OrderBy(x => x), result.OrderBy(x => x));
}
```

**Pagination:**
```csharp
// Property-based test for pagination
[Theory]
public void GetPagedVenues_AlwaysCompleteCoverage(int pageSize, int totalCount)
{
    // Property 1: Total returned items = totalCount
    var allPages = new List<Venue>();
    for (int page = 1; page <= Math.Ceiling((double)totalCount / pageSize); page++)
    {
        var (items, count) = _repo.GetPagedAsync(page, pageSize).Result;
        allPages.AddRange(items);
        Assert.Equal(totalCount, count);
    }
    Assert.Equal(totalCount, allPages.Count);
    
    // Property 2: No duplicates across pages
    var ids = allPages.Select(v => v.Id).ToList();
    var distinctIds = ids.Distinct().ToList();
    Assert.Equal(ids.Count, distinctIds.Count);
    
    // Property 3: Deterministic order (same page, same results)
    var page1First = _repo.GetPagedAsync(1, pageSize).Result;
    var page1Second = _repo.GetPagedAsync(1, pageSize).Result;
    Assert.Equal(page1First.items.Select(v => v.Id), page1Second.items.Select(v => v.Id));
}
```

### Why Properties Matter for SDD

1. **Regeneration stability:** When specs are updated and code is regenerated, property tests still pass because they verify the contract, not the implementation.
2. **Spot-checking misses regressions:** A unit test for `sort([3,1,2]) == [1,2,3]` passes on any sorting algorithm. A property test catches a regression where the agent accidentally switched the comparison operator.
3. **Edge cases:** Agents often miss boundary conditions. Property tests can be run against hundreds of generated inputs automatically.

---

## Enforced Phases with Stopping Points

Structure TDD as explicit workflow phases, each with a human review gate:

| Phase | Responsible | Output | Gate Criteria | Pass/Fail Action |
|-------|-----------|--------|-----------------|-----------------|
| **Red** | Tester | Failing test suite | "Do these tests match the spec's acceptance criteria?" | Pass → Green; Fail → Tester rewrites tests |
| **Green** | Builder | Minimal implementation | "Does the implementation match the test intent? Are there any shortcuts?" | Pass → Refactor; Fail → Builder adjusts code |
| **Refactor** | Builder | Cleaner code | "Is the refactoring safe? Property tests pass?" | Pass → Next task; Fail → Rollback refactoring |

The gate is not optional. If a gate fails, the workflow does not proceed to the next phase. This maintains discipline.

---

## Test Quality Audit

Before allowing an agent to implement against a test suite, the test quality must be verified. Research from TDAD 2025 (Test-Driven Agent Development) and Spec + TDD guides identify common weakness patterns:

### Test Weakness Checklist

| Weakness | Signal | Fix |
|----------|--------|-----|
| **Tautological assertion** | Test passes even if implementation returns wrong value | Rewrite assertion to check specific value, message, or state |
| **Over-mocking** | Test mocks so much that the implementation is untestable | Mock only external dependencies; test real logic |
| **Missing edge case** | Test covers happy path only; ignores null, empty, boundary | Add tests for each edge case in acceptance criteria |
| **No error case testing** | "Should succeed" tests exist; "should fail" tests missing | For each validation rule, write test that violates it |
| **Assertion depth of 1** | Test only checks that a method returns a non-null result | Add assertions for content, structure, side effects |
| **Missing integration** | Unit test passes; integration test fails | Tests must exercise real dependencies when safe (e.g., real SQLite DB) |

**Gate:** Tests must pass this audit before implementation begins.

---

## Structural Enforcement: Evidence Gates

Rather than relying on prompts and suggestions, enforce TDD through structural gates:

### 1. Red Phase Evidence

Before Green phase begins, verify:
- Test file exists and is committed to git
- Test suite runs: `dotnet test MyVocaList.Tests` 
- All tests fail with clear assertion errors (not build errors)
- Test output captured as `.red-evidence.txt`

**CI/CD gate:**
```bash
# Fails if tests don't fail
if dotnet test | grep -q "failed: 0"; then
  echo "ERROR: Tests should fail in Red phase"
  exit 1
fi
```

### 2. Green Phase Evidence

Before Refactor phase begins, verify:
- All tests pass: `dotnet test MyVocaList.Tests`
- Test count unchanged: `git diff --name-only -- **/*Tests.cs` returns empty
- Test output captured as `.green-evidence.txt`

**CI/CD gate:**
```bash
# Fails if any test fails
dotnet test || exit 1

# Fails if test files were modified
if git diff --name-only -- '**/*Tests.cs' | grep -q .; then
  echo "ERROR: Test files were modified during implementation"
  exit 1
fi
```

### 3. Refactor Phase Evidence

Before task completes, verify:
- All tests pass: `dotnet test MyVocaList.Tests`
- Code quality improved (optional: SonarQube, CodeMaid metrics)
- Test output captured as `.refactor-evidence.txt`

---

## Integration with SDD Workflow Phases

TDD is not separate from the SDD cycle — it is embedded within S3 (Workflow Phases):

| SDD Phase | TDD Activity | Owner | Success Criteria |
|-----------|------------|-------|------------------|
| **S3.1 — Planning** | Spec review for testability; acceptance criteria are concrete and executable | Human | Spec includes acceptance criteria that can be coded as assertions |
| **S3.2 — Implementation** | TDD phases: Red → Green → Refactor | Builder + Tester agents | All phases complete with evidence gates passing |
| **S3.3 — Verification** | Test audit; specification coverage check | Verifier agent | Every acceptance criterion has evidence: test name + code reference |
| **S3.3 (Pre-merge)** | Full test suite passes; coverage > 80%; security tests pass | CI/CD automation | Gates pass; evidence trail complete |

---

## Key Constraints for MyVocaList

Based on the project's context (C# 13, xUnit, EF Core, .NET MAUI), enforce these TDD constraints:

1. **One test at a time:** Write ONE test; confirm it fails; write minimal code to pass it. Never batch multiple tests.

2. **Test-first is mandatory:** Tests exist in version control before implementation begins. Use git hooks to block implementation changes in files without corresponding test files.

3. **Never modify tests to make them pass:** If a test fails, the implementation is wrong. The test is the spec. Use `git diff -- **/*Tests.cs` to verify no test assertions were modified.

4. **Property-based tests for collections and pagination:** Use Hypothesis (Python) or similar for C# to verify invariants across many inputs.

5. **Integration tests use real SQLite:** Repository tests must use real SQLite temp DB, not mocks. EF Core's query translation only works against a real provider.

6. **All tests pass before merge:** CI/CD gate: `dotnet test` must return exit code 0. Coverage must meet project baseline (document in `testing.md`).

---

## Relationship to Other SDD Topics

- **S3 (Workflow Phases):** TDD phases embedded at S3.2 (Implementation)
- **S4 (Context & Memory):** Test suites and specs as persistent context across sessions
- **S6 (Governance & Enforcement):** TDD enforcement via hooks and gates in CI/CD pipeline
- **S9.2 (Spec Drift Prevention):** Tests verify spec contract continuously; drift surfaces as test failures
- **S9.3 (Hallucination Safeguards):** Test quality audit prevents weak tests that mask hallucinations

---

## Common Failure Modes and Mitigations

| Failure Mode | Signal | Root Cause | Mitigation |
|--------------|--------|-----------|-----------|
| **Same agent writes tests and code** | Tests are tautologies; high coverage masks low correctness | No role separation | Separate Tester from Builder agent |
| **Agent modifies tests to pass** | Test assertions change mid-implementation | No version control check | Git hook: fail if test files change during implementation |
| **Batched tests** | Feedback loop is loose; agent doesn't adjust understanding per test | Instructions don't enforce incrementalism | Structure prompt: "Write ONE test. Confirm it fails. Then one implementation step." |
| **Tests run too slowly** | Agent skips running tests; feedback loop breaks | Full test suite has integration tests | Separate fast unit tests from slow integration tests; run only fast tests in TDD loop |
| **Weak property tests** | Refactoring introduces subtle regressions | Properties are too broad | Narrow properties to specific contract points; verify with edge cases |
| **Context collapse between phases** | Refactor phase loses understanding of spec from Red phase | No persistent evidence | Capture evidence files (red, green, refactor output) in git; reference them in next phase |

---

## Recommended Tooling

| Tool | Role | Configuration |
|------|------|---------------|
| **xUnit** (native to MyVocaList) | Test framework | Already in place; ensure `xunit` runner is up to date |
| **Moq** | Mocking dependencies | Already in use; keep mocking to external dependencies only |
| **TestDbContextFactory** (in testing.md) | Real SQLite for integration tests | Use for all repository tests; no in-memory EF Core provider |
| **git hooks** (Claude Code `update-config`) | Enforce test existence and immutability | Configured in `.claude/settings.json` or project `.git/hooks/` |
| **CI/CD gates** | Enforce test pass/fail before merge | Add to Azure Pipelines or GitHub Actions: `dotnet test || exit 1` |

---

## Metrics to Track

- **Hallucination rate on TDD tasks:** % of code that passes tests but violates spec on human review
- **Test modification rate:** % of implementation PRs where test files were edited (should be 0%)
- **Batch test rate:** % of tasks where >1 test was written before implementation (should be 0%)
- **Test quality:** % of tests with assertion depth ≥ 2; % of assertions that are tautologies (should be <5%)
- **Refactor regression rate:** % of refactored code that introduced new test failures
- **Coverage after refactor:** Code coverage before and after refactor phase (should increase or stay same)

Track these to identify when TDD discipline is slipping. High hallucination rate or test modification rate signals that separation isn't working.

---

## Key Takeaways

1. **TDD is structural, not stylistic.** Telling an agent "follow TDD" without enforcement increases hallucinations. Structure must encode discipline into workflow gates.

2. **Test-first works for AI agents because it constrains early.** The agent cannot misinterpret a test; the test is unambiguous. The earlier constraint is applied, the less hallucination space exists.

3. **Separation of roles is load-bearing.** Tester writes tests; Builder implements. Mixing roles produces self-confirming loops. The separation is not optional.

4. **Property-based testing catches silent regressions.** Example-based tests verify specific cases. Property tests verify invariants, catching edge cases and subtle failures that spot-checking misses.

5. **Evidence gates replace trust.** Don't ask "did you follow TDD?" Check: "Can you show me the red evidence? Can you show me the tests that were committed before implementation?"

---

## Sources

- [Spec + TDD: The Combination That Actually Produces Shippable AI Code — Augment Code](https://www.augmentcode.com/guides/spec-tdd-shippable-ai-generated-code)
- [Test-Driven Agent Development: Tests as Spec and Guardrail — AgentPatterns.ai](http://agentpatterns.ai/verification/tdd-agent-development/)
- [Red-Green-Refactor with Agents: Letting Tests Drive Dev — AgentPatterns.ai](http://agentpatterns.ai/verification/red-green-refactor-agents/)
- [Red Green Refactor: Why TDD Is the Best Way to Control AI Coding Agents — LoreAI](https://loreai.dev/blog/red-green-refactor-claude-code)
- [Test-Driven Development with AI Agents: A Practical Guide — Fundesk](https://www.fundesk.io/test-driven-development-ai-agents-guide)
- [Set up a test-driven development flow in VS Code — Microsoft](https://code.visualstudio.com/docs/copilot/guides/test-driven-development-guide)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship Production-Ready AI-Generated Code — Pockit](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [TDD+SDD Development v3.2 — OpenClaw Skill (GitHub)](https://github.com/Charpup/openclaw-tdd-sdd-skill)
- [SDD DevFlow: Spec-Driven Development workflow for AI-assisted coding — GitHub](https://github.com/pbojeda/sdd-devflow)
- [Test-Driven Agent Loops — Engineers of AI](https://engineersofai.com/docs/agentic-ai/coding-agents/Test-Driven-Agent-Loops)
- [Micro-Specs: The Pattern That Significantly Improves AI Agent Test Coverage — Augment Code](https://www.augmentcode.com/guides/micro-specs-pattern-ai-agent-test-coverage)
- [Claude Code TDD: AI-Assisted Test-Driven Dev Guide — ClaudeWorld](https://claude-world.com/articles/claude-code-tdd-workflow/)
- [SpecDriven AI: Building Reliable Software with Precision — Paul M Duvall](https://www.paulmduvall.com/specdriven-ai-combining-specs-and-tdd-for-ai-powered-development/)
