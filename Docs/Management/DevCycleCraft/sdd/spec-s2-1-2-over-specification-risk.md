# S2.1.2 — Over-Specification Risk

**Status:** Researched
**Predecessor(s) ID:** S2.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written by research agent |

---

## Overview

Over-specification is the failure mode that occurs when a specification becomes so detailed, constraining, or pseudo-code-like that it:

1. **Prevents agent flexibility** — The agent cannot find better implementation approaches because the spec has locked in assumptions
2. **Exceeds the agent's attention budget** — The spec grows so large that model performance degrades and the agent ignores constraints
3. **Becomes unmaintainable** — The spec reads like code, drifts from code, and loses its value as a human-readable artifact
4. **Creates false completeness** — Teams believe detailed specs prevent all bugs, but detailed specs often hide ambiguity under layers of pseudo-code

The fundamental tension: **A spec must be precise enough to guide reliable code generation, but vague enough to let agents optimize for your actual codebase and constraints.**

---

## The Precision-Adherence Tradeoff

Industry research (2025–2026) has identified a narrow "sweet spot" between two failure modes:

### Failure 1: Spec Slop (Under-Specification)

**What it is:** Low-precision prose written at speed, leaving critical decisions unspecified.

**Visible symptoms:**
- Agent makes arbitrary choices and later contradicts itself
- Multiple implementation approaches are plausible; agent picks one without asking
- Edge cases are missing; agent fills them with statistically likely but wrong behavior
- Output shape is unpredictable (error messages, field names, status codes change between runs)

**Example:**
```
Spec says: "Validate the payment amount"
Agent interprets this as:
  - Check if amount > 0? ✓
  - Check if amount is numeric? (assumed)
  - Check against transaction limits? (unknown)
  - Reject if amount is too large? (where's "too large"?)
```

### Failure 2: Over-Specification

**What it is:** Excessive detail accumulates beyond the agent's ability to adhere to all constraints simultaneously, causing the spec itself to become pseudo-code.

**Visible symptoms:**
- Agent misinterprets individual instructions as detail grows (the "curse of instructions")
- Agent adds unrequested features that seemed reasonable given the verbose context
- Adherence to spec requirements degrades as the spec grows larger
- The spec reads like code (type signatures, schemas, algorithm details in prose) instead of intent

**Example (from arXiv:2602.00180):**
```
Spec says: "Use bcrypt with 12 salt rounds, validate against a 
hardcoded salt constant in utils/crypto.ts, iterate exactly 
12 times before hashing, store the result in the users table 
column password_hash as TEXT NOT NULL"

Problem: This is not a specification (what to achieve) — 
it is an implementation detail (how to achieve it). The agent 
is locked into this approach even if bcrypt is already available 
elsewhere in the codebase with a different salt configuration.
```

---

## The "Curse of Instructions"

Research from 2025 (Osmani, O'Reilly; cited in arXiv:2602.00180 and industry tooling guides) identified the empirical relationship:

**As spec detail increases, agent adherence to individual constraints decreases.**

### Measured Evidence

- **Scott Logic (2025):** GitHub Spec Kit generated 2,000+ lines of Markdown per feature but still introduced bugs. Iterative prompting (short, focused requests) produced working code 10× faster with fewer corrections.

- **LeanSpec (2025):** Spec Kit specs grew to 1,166 lines; AI agents started corrupting specs during edits, code generation became unreliable, responses slowed, and more time was spent fixing mistakes. After applying context engineering (reducing largest spec to 378 lines), reliability recovered.

- **Model Performance Studies (arXiv):** All models perform worse when given more tools, options, or constraints simultaneously. The effect is measurable even when far below context window limits.

### Why This Happens

1. **Attention Dilution:** Transformer attention has O(N²) complexity. More tokens to process = harder to focus on what matters.

2. **Context Rot:** With large context, models start repeating patterns from the context history instead of applying training knowledge.

3. **Option Overload:** Too many explicit choices lead to wrong selections. This is not unique to AI — it is a cognitive constraint.

4. **Token Dilution:** Each irrelevant word in the context increases the ratio of noise to signal, forcing the model to filter harder.

---

## Spec Complexity Displacement: The Hidden Cost

A critical insight from 2026 research: **Over-specification does not eliminate precision — it relocates complexity from code to spec.**

### The False Promise

SDD practitioners often frame spec-driven development as "describe intent without bearing the cost of implementation." The fallacy is that precision can be postponed rather than moved.

**Reality:** A spec precise enough to reliably generate correct code must encode:
- Type constraints and schemas
- Algorithm logic (implicitly or explicitly)
- Edge case coverage
- State transition rules
- Integration failure modes

The OpenAI Symphony specification analyzed by Gabriel Gonzalez (2026) contains database schemas, algorithm pseudocode, and configuration checklists — it reads as code, not prose.

### Measurement

- **Lobsters community (2025):** 3,388 lines of spec producing 16,063 lines of Elixir. Precision was high; writing burden was enormous.
- **Thoughtworks (2025):** SDD relocates complexity rather than eliminating it. Planning replaces chaos, but total work does not shrink.

### The Paradox

A vague spec means agents hallucinate (fails safe — visible bugs). An over-precise spec means agents follow pseudo-code (fails silent — looks correct, but locks in assumptions). The spec-complexity curve has an inverted-U peak: there is an optimal level of detail, narrower than most teams initially expect.

---

## When Specs Become Pseudo-Code

A spec has crossed into pseudo-code territory when:

1. **Implementation details appear as requirements:**
   - "Use HashMap with synchronized access" (implementation) vs. "must be thread-safe" (contract)
   - "Iterate exactly N times before checking" (implementation) vs. "check on each iteration" (contract)

2. **The spec encodes sequential logic:**
   - "First validate, then hash, then store" (sequence of steps) vs. "password must be hashed and stored" (outcome)

3. **The spec includes low-level data structure decisions:**
   - "Use a Redis sorted set with TTL of 300s" (implementation) vs. "cache must expire within 5 minutes" (requirement)

4. **The spec reads like code in prose:**
   ```
   ❌ "The PaymentProcessor shall create a PaymentModel instance,
        call the stripe API with retry logic using exponential
        backoff 1s/2s/4s, catch SocketTimeoutException and retry,
        then serialize to JSON and POST to /webhooks"

   ✓ "The payment processor must be resilient to network failures,
      retrying transient failures up to 3 times before failing;
      clients must receive a status code and reason."
   ```

---

## Maintenance Burden and Specification Debt

Over-specification creates a form of documentation debt:

### Synchronization Cost

When specs are detailed enough to read like code, they must be updated whenever the code changes. This creates a two-artifact maintenance problem:

- **Code drifts from spec:** Developers fix bugs or optimize, but don't update the corresponding spec section. The spec becomes actively misleading.
- **Spec drifts from code:** The spec is updated during planning but never re-read during implementation. They diverge silently.

### Specification Rot

As codebases evolve, over-detailed specs become stale:
- The spec prescribes "use HashMap" but the codebase switched to a custom data structure
- The spec says "retry with exponential backoff" but the implementation now uses a circuit breaker
- The spec encodes a deprecated pattern that no longer applies

Unlike code, which is executed and tested, outdated specs are invisible until someone reads them months later and trusts a now-false statement.

### Organizational Cost

Teams may view spec maintenance as bureaucracy:
- Specs feel like forms to fill out rather than tools for clarity
- The specification process adds overhead without improving quality
- Teams game the system or abandon it entirely in favor of "just coding"

---

## Over-Specification as a Signal of Incomplete Understanding

Research (Isoform, 2026; Sibylline Software, 2026) identifies a pattern: **Over-specification often masks gaps in actual understanding.**

### The Illusion of Completeness

A detailed spec creates confidence that "all cases are covered," but this confidence is often false. The spec describes what was planned, not what will work in reality. Key insights emerge during implementation:

- Async behavior that was not considered
- Integration points that interact unexpectedly
- Performance constraints that conflict with the designed approach
- Edge cases that only manifest under production load

### Exploratory Development vs. Specification-Driven

Software development is fundamentally exploratory. The most important insights emerge after building begins. Being too fixed to a static spec leads to:
- Less iteration
- Reduced creativity and emergent solutions
- Brittle, waterfall-like development (despite using AI)

---

## Anti-Patterns to Avoid

### 1. Pseudo-Code Disguised as Specification

**Problem:**
```
Spec: "The function shall allocate a map of type Map<String, Integer>,
iterate through the list using a for-each loop, extract the value field,
increment the counter, and store in the map."

Issue: This is implementation, not contract.
```

**Fix:**
```
Spec: "The function shall return a count of values by category.
Input: List of items with category and value fields.
Output: Map from category to integer count."
```

### 2. Implementation Hints Instead of Behavior

**Problem:**
```
Spec: "Use a HashMap for fast lookups and a LinkedList for ordering."

Issue: Locks the agent into a specific data structure. The agent
may find a better approach (e.g., a custom index) but can't use it.
```

**Fix:**
```
Spec: "Lookups must complete in O(1) time; iteration must reflect
insertion order."
```

### 3. Vague Success Criteria

**Problem:**
```
Spec: "The system works correctly" or "performs efficiently"

Issue: Unmeasurable. "Works" to whom? How fast is "efficient"?
```

**Fix:**
```
Spec: "All 47 existing tests pass (regression). New endpoints return
within 200ms p95. Error responses include machine-readable codes."
```

### 4. Missing Constraints (Negative Space)

**Problem:**
```
Spec: Lists all things that MUST happen.
Missing: All things that must NOT happen.

Issue: Agent assumes the general case. Your system is not the
general case. Agent adds authentication where not needed, creates
unnecessary abstractions, over-engineers for edge cases not in scope.
```

**Fix:**
```
Spec constraints:
  - Do NOT add authentication (handled separately)
  - Do NOT introduce new dependencies without approval
  - Do NOT create new database tables (use existing schema)
  - Do NOT add logging beyond what is specified
```

### 5. Implementation Details in Edge Cases

**Problem:**
```
Spec edge case: "If the cache is full, check free memory. If < 100MB,
trigger garbage collection. If still < 100MB after GC, evict LRU items
until 50% free."

Issue: This is a cache implementation algorithm, not a behavioral requirement.
```

**Fix:**
```
Spec edge case: "If the cache is full, the system must reclaim space.
Subsequent operations must succeed without OutOfMemory errors."

(Let the agent choose the eviction strategy.)
```

---

## Calibrating Spec Detail: The Framework

Research from 2025–2026 has converged on a practical calibration framework:

### Minimal Spec (Simple Tasks)

Use for: CRUD operations, straightforward UI changes, isolated utility functions.

**Content:**
- **What:** Brief description (1–2 sentences)
- **Acceptance criteria:** 1–3 concrete examples (inputs/outputs)
- **Constraints:** 1–2 explicit "do not" boundaries

**Example (from Addy Osmani):**
```
Add dark mode toggle to header.

Acceptance criteria:
  - Toggle switch in top-right
  - Applies to entire app
  - Persists across sessions
  - No animation delay

Constraints:
  - Do NOT add new CSS variables
  - Do NOT modify the routing system
```

### Standard Spec (Medium Complexity)

Use for: Features with multiple states, integration points, or validation logic.

**Content:**
- **Inputs/Outputs:** Tables with source, format, validation
- **Preconditions:** States that must be true before operation
- **State machines:** Valid transitions (if applicable)
- **Edge cases:** 1 example per category (null, duplicate, timeout, permission)
- **Constraints:** 3–5 explicit boundaries

### Comprehensive Spec (High Complexity)

Use for: Systems with external dependencies, concurrent operations, strict correctness requirements, or that will be regenerated later.

**Content:** All seven structural elements from S2.1, plus:
- **Architecture decisions:** Why this approach was chosen
- **Performance bounds:** Specific latency/throughput targets
- **Failure modes:** Explicit handling for each integration point
- **Non-goals:** What is explicitly NOT included
- **Dependencies:** What this depends on; what depends on this

---

## Measuring Spec Quality: Regeneration Tests

A practical diagnostic for over-specification comes from regeneration testing (cited by Augment Code, 2026):

### The Test

1. Write a spec
2. Generate code from the spec (Agent A)
3. One week later, delete the code and regenerate (Agent B, fresh)
4. Compare the two implementations for behavioral parity

### Interpreting Divergence

Each difference between regenerations indicates a missing or ambiguous constraint:

- **Output structure changes** (different field names, error codes) → Input/Output contract section is missing or vague
- **Agent adds features** not requested → "Not Included" scope boundary is incomplete
- **Edge cases handled differently** → Acceptance criteria don't explicitly cover them
- **Algorithm differs** → Postconditions or state machine is underspecified

**Key insight:** Divergence is not a model reliability problem; it is a signal of spec incompleteness. Each difference points to exactly which constraint was missing from the spec.

---

## Best Practice Checklist

### Before Writing the Spec
- [ ] Is this feature complex enough to justify a detailed spec? (If not, keep it minimal)
- [ ] Who will implement this? (Individual developer vs. multiple agents changes spec style)
- [ ] Will this code be regenerated later? (Yes → comprehensive spec; No → lighter is OK)

### While Writing the Spec
- [ ] Every "must" statement is testable, not aspirational
- [ ] Implementation hints are removed (no "use HashMap", only "O(1) lookup")
- [ ] Constraints list what NOT to do, not just what to do
- [ ] Success criteria are measurable (status code, latency, test count, not "works")
- [ ] If the spec reads like pseudo-code, simplify it

### After Writing the Spec
- [ ] Ask: "Could this be interpreted in two different ways?" If yes, clarify.
- [ ] Ask: "Am I constraining the implementation unnecessarily?" If yes, remove the constraint.
- [ ] Ask: "Would an agent understand what done looks like?" If no, add acceptance criteria.
- [ ] Have someone unfamiliar with the system read the spec. Do they understand what must be built?

---

## The Optimal Scope

The consensus from 2025–2026 research is **minimalism within scope:**

- Specify **what** (behavior, contract, outcomes)
- Leave blank **how** (implementation approach, data structures, algorithms)
- Specify **constraints** (what NOT to do, boundaries, non-negotiable rules)
- Specify **why** (business rationale, priority trade-offs, user intent) only if the agent needs it to make decisions

**The litmus test:** If you deleted the implementation and regenerated it, the new code should pass the same tests and fulfill the same contract — even if it looks different.

---

## Relationship to S2.1

S2.1 defines the seven structural elements that must appear in complete specs. S2.1.2 (this document) addresses the failure mode of specifying those elements too thoroughly:

- **S2.1:** "Every input source must be named, schema explicit, validation specific"
- **S2.1.2:** "But not so specific that you lock in implementation assumptions or exceed the agent's attention budget"

The tension is real and unavoidable. The framework provided here is the current best practice for navigating it.

---

## Sources

- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180 (2026)](https://arxiv.org/html/2602.00180v1)
- [The Limits of Spec-Driven Development — Isoform AI (2026)](https://isoform.ai/blog/the-limits-of-spec-driven-development)
- [Spec Complexity Displacement: When Specs Become Code — AgentPatterns.ai (2026)](https://agentpatterns.ai/anti-patterns/spec-complexity-displacement/)
- [Why Your AI Agent Gets Dumber with Large Specs — LeanSpec (2025)](https://lean-spec.dev/blog/ai-agent-performance)
- [Spec-driven development: writing specs that AI agents actually ship — Jakub Kontra (2026)](https://jakubkontra.com/en/blog/spec-driven-development-writing-specs-ai-agents-ship)
- [How to Write Specs for AI Agents — Victorino Group (2026)](https://victorinollc.com/thinking/specs-for-ai-agents)
- [How to write a good spec for AI agents — Addy Osmani (2026)](https://addyosmani.com/blog/good-spec)
- [AI Spec Template: What to Include and Leave Out — Augment Code (2026)](https://www.augmentcode.com/guides/ai-spec-template)
- [Phase 2: Writing Effective Specifications — The AI Agent Factory (2026)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/writing-effective-specs)
- [Spec-Driven Development: When Intent Becomes the Source Code — Deepak Babu Piskala, Medium (2026)](https://medium.com/data-science-collective/spec-driven-development-when-intent-becomes-the-source-code-3af39f86b9d3)
- [Spec Driven Development: When Architecture Becomes Executable — InfoQ (2026)](https://www.infoq.com/articles/spec-driven-development/)
- [Spec-Driven Development: Review, Radar Rating & Alternatives — Tekai (2026)](https://tekai.dev/catalog/spec-driven-development)
- [The Problems with Spec Driven Development — Sibylline Software (2026)](https://sibylline.dev/articles/2026-01-28-problems-with-spec-driven-development/)
