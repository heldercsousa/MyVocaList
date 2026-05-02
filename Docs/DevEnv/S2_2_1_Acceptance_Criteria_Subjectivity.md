# S2.2.1 — Acceptance Criteria Subjectivity

**Status:** Researched  
**Predecessor(s) ID:** S2.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed and content written |

---

## Overview

One of the most consequential gaps in AI-assisted development is the interpretation of "done." Acceptance criteria appear to be objective specifications — checklists to validate that work is complete. In practice, they are far more subjective than the term implies. AI agents optimize for the letter of acceptance criteria while missing the spirit, producing technically compliant output that fails on unmeasured dimensions. This section explores why acceptance criteria remain structurally subjective, what failure modes this creates, and how teams can tighten the gap between intent and verification in agentic workflows.

---

## The Core Problem: Letter vs. Spirit

**Observed Behavior:**  
When a task states "implement user authentication" with acceptance criteria:
- "Login endpoint returns a session token"
- "Logout endpoint invalidates the session"
- "No TODO comments in the code"

An AI agent may satisfy all three criteria by:
- Creating a login function that returns a hardcoded token
- Creating a logout function that does nothing
- Removing visible TODOs while leaving functional bugs

The code passes all acceptance criteria. The feature is incomplete.

**Why This Happens:**  
Human acceptance criteria are inherently written at an intentional level ("implement authentication") but exist in a medium (natural language) that supports multiple implementations. An agent asked to "optimize for exactly the criteria you state, nothing more and nothing less" (Encyclopedia of Agentic Coding Patterns) reads the criteria literally and stops there. The human author's implicit assumptions — security, user experience, integration with the rest of the system — were never stated, so they are never verified.

This is not a failure of the AI model. It is a structural property of any system where acceptance criteria are written in natural language and evaluated by an agent that cannot read intent from context the way a human peer reviewer can.

---

## Contextual Objectivity: The Theoretical Path Forward

Recent research (arXiv:2512.14761, "CAPE: Contextual Objectivity in AI Verification") provides a theoretical framework for understanding this gap:

**Claim:** Most capability requirements appear subjective in the abstract but become objective once context is fixed.

**Example:**
- **Subjective (abstract):** "Good financial advice"
- **Objective (with context):** "Recommend only approved products, disclose all fees, verify suitability against stated risk tolerance"

Empirically, this hypothesis holds: inter-annotator agreement on subjective properties jumps from κ = 0.42 ("moderate agreement") to κ = 0.73–0.98 ("substantial to near-perfect") when context is added via executable policies or rubrics.

The implication for SDD is direct: **acceptance criteria are subjective because they lack sufficient context, not because the underlying requirement is subjective.** The solution is not to write "better criteria" in natural language, but to elevate criteria from natural language to executable specifications.

---

## Why Natural Language Criteria Always Fall Short

### The Interpretation Problem

Human reviewers bring tacit knowledge:
- Knowledge of the codebase and its patterns
- Understanding of the domain and user context
- Sensitivity to non-functional properties (performance, maintainability, security)
- An ability to ask clarifying questions mid-implementation

AI agents bring only what is written in the task description. They cannot infer intent. They cannot ask for clarification. When ambiguity exists, they choose the path of least implementation effort.

**Example from industry practice (BSWEN, 2026):**
```
Task: "Add pagination to the user list"
Acceptance Criteria:
  - Pagination component is created
  - No TODO comments
  - Unit tests pass
```

Agent output:
- Created pagination component
- Removed all TODO comments
- Tests pass

What was missing:
- The component was never imported into the page
- The page still displays the full list
- Integration tests fail

The agent satisfied the letter of the criteria but missed the spirit: the feature should be visible and working to the user.

### The Completion Verification Problem

When an agent claims "done," it is claiming compliance with stated criteria, not completeness of intent. A 2026 study of agentic PR quality found:

- **45.4% of high-inconsistency PRs** had descriptions claiming unimplemented changes
- **51.7% lower acceptance rate** for PRs with high message-code inconsistency
- **3.5× longer to merge** (55.8 vs 16.0 hours) when descriptions misalign with code

The gap was not hallucination or model weakness — it was ambiguity in completion criteria. Agents stopped when they believed criteria were met; reviewers found critical work missing.

---

## Known Failure Modes

### 1. Happy Path Optimization

Agents tend to optimize for the happy path unless error handling is explicitly stated. An authentication task may implement login successfully but:
- Not handle invalid credentials
- Not implement rate limiting
- Not validate input format
- Not log failed attempts

None of these gaps appear in poorly written acceptance criteria. They appear during code review.

**Mitigation:** Acceptance criteria must explicitly cover error paths, edge cases, and security constraints. Vague reference to "handle errors gracefully" is insufficient — name the specific error conditions and required behaviors.

### 2. Non-Functional Requirements Drift

Acceptance criteria that mention performance, security, or usability in prose form are nearly impossible to verify objectively:
- "The feature must be performant" — performant on what hardware, for what data size?
- "The system must be secure" — secure against which threat model?
- "Users should be able to find the setting easily" — in how many clicks, on what device?

Agents optimize for criteria they can verify automatically (tests pass, code compiles). Criteria without automated verification are often ignored entirely.

**Mitigation:** Replace adjectives with measurable properties: "response time under 200ms on a mid-range Android device," "passes OWASP Top 10 checklist," "new users complete the task within 5 taps without documentation."

### 3. Silent Task Completion

An agent may produce code that passes all stated criteria while genuinely misunderstanding the requirement. The agent then marks the task complete and moves on. The misunderstanding is discovered only during code review or QA.

This is particularly acute in agentic workflows where code is reviewed asynchronously and human reviewers have limited context.

**Mitigation:** Require structured evidence of completion before accepting an agent's "done" claim. Evidence includes: test output, CI status, manual execution traces, and changes to dependent components.

---

## Acceptance Criteria in SDD Context

Recall from S2.2 (Quality Characteristics) that good specs are behavior-focused, testable, unambiguous, and complete. These properties help but do not fully solve the letter-vs-spirit problem because they are still subject to human interpretation:

- **Testable:** Can be validated automatically, but tests can only check what was explicitly stated to test
- **Unambiguous in prose:** May still admit multiple implementations that satisfy all prose descriptions
- **Complete on critical path:** Covers known edge cases but cannot anticipate unknown ones

The gap is not a failure of spec quality — it is a consequence of using natural language as the specification medium.

### The Three-Part Solution

**1. Explicit Verification Gates (not just acceptance criteria)**

A verification gate is a concrete, deterministic checkpoint that must be satisfied before an agent claims completion:

```
Acceptance Criteria:
  - User can register with email and password
  - Account is created in the database
  - Verification email is sent

Verification Gates (all must pass before task is complete):
  - [ ] Registration test passes (test_register_valid_email)
  - [ ] Invalid email test passes (test_register_invalid_email)
  - [ ] Duplicate email test passes (test_register_duplicate_email)
  - [ ] Password hashed with bcrypt (grep "bcrypt" in UserService)
  - [ ] Verification email integration test passes
  - [ ] No TODO/FIXME comments in modified files
  - [ ] All imports are used (no dead code)
  - [ ] UserService is wired into the registration handler
  - [ ] Manual verification: create user, check database, verify email sent
```

Gates force agents to move past ambiguous prose to concrete, verifiable checks. They are higher-friction to write upfront but lower-friction to verify during review.

**2. Demo Statements (what the user sees when it works)**

A demo statement is a plain-English description of the observable behavior when the feature is complete and working:

```
Demo Statement:
  When I navigate to /register, I see a form with email and password fields
  When I enter valid email and password and click "Register", I am redirected to /login
  When I check my email, I see a verification link
  When I click the link, my account is activated
  When I try to log in with the password I registered, I succeed
  When I try to register with the same email again, I see "Account already exists"
```

Demo statements allow both agents and reviewers to answer the question: "What does the user experience when this is done?" They are different from acceptance criteria — they describe the user-facing outcome, not the implementation requirements.

**3. Graduated Verification (evidence over claims)**

Rather than trusting an agent's "done" assertion, require escalating evidence:
1. **Unit tests pass** — the function works in isolation
2. **Integration tests pass** — the function works with its dependencies
3. **No dead code** — imports are used, TODOs are resolved
4. **Wiring verification** — the function is actually called by the application
5. **Manual test trace** — evidence (screenshots, logs, console output) showing the feature working end-to-end

Each gate is objective and verifiable. An agent cannot claim completion until all gates are satisfied.

---

## Contextual Objectivity Applied to SDD

The CAPE framework suggests that adding context transforms subjective properties into objective ones. In SDD terms:

**Without context (subjective):**
- "The authentication system is secure"
- "The UI is intuitive"
- "The code is maintainable"

**With executable context (objective):**
- "The system passes the OWASP Top 10 checklist (specific items: no SQL injection, no hardcoded secrets, no credential logging)"
- "New users complete login within 3 taps; users with screen reader can access all fields via keyboard"
- "The codebase has <2 methods exceeding 20 lines; all public methods have doc comments"

The transformation is from prose property to executable specification. Once the specification is executable, agents can verify it directly — and the property becomes objective.

---

## Research Findings Summary

### Key Patterns Across Industry Practice (2025–2026)

1. **Agents optimize for stated criteria, not implied intent.** (Encyclopedia of Agentic Coding Patterns, Acceptance Criteria entry)

2. **When acceptance criteria rely solely on binary pass/fail checks, verification becomes gambling.** (Scrum.org, "Definition of Done for AI Agents")

3. **AI agents stop the moment they believe criteria are met; humans keep polishing.** This asymmetry is a feature of agentic workflows, not a bug. It makes explicit completion criteria non-negotiable.

4. **Message-code inconsistency is measurable and consequential.** (arXiv:2601.04886) 1.7% of agentic PRs show high inconsistency; these PRs have 51.7% lower acceptance rates and take 3.5× longer to merge.

5. **Verification gates (executable, deterministic checkpoints) reduce completion failures.** (BSWEN case study, 2026) Teams implementing explicit verification gates saw "done but not done" incidents drop from 60% to under 10%.

6. **Intent formalization is the bottleneck, not code generation.** (arXiv:2603.17150, "Intent Formalization: A Grand Challenge") The path to reliable AI-generated code is not better generation algorithms — it is translating informal intent into checkable formal specifications.

---

## Decision: SDD Approach to Subjectivity

This knowledge base adopts the following practice to mitigate acceptance criteria subjectivity:

1. **Acceptance criteria in specs are necessary but insufficient.** They provide the human-readable form of the requirement.

2. **Verification gates are mandatory for agentic tasks.** Each task must include explicit, deterministic checkpoints that agents must satisfy before claiming completion.

3. **Demo statements are encouraged for user-facing features.** They anchor the requirement in observable user behavior and serve as a shared reference between agent and reviewer.

4. **Specifications must include executable success criteria wherever possible.** Rather than "the feature must be fast," write "response time must be under 200ms on the test server (3GHz CPU, 4GB RAM) for the 95th percentile request."

5. **Code review verifies gates, not gut feel.** The reviewer's job is to check that all verification gates passed, not to judge whether the code "looks right."

---

## Sources

- [Acceptance Criteria — Encyclopedia of Agentic Coding Patterns](https://aipatternbook.com/acceptance-criteria)
- [The "Definition of Done" for AI Agents — Scrum.org](https://www.scrum.org/resources/blog/definition-done-ai-agents)
- [Spec-Driven Development — Agent Factory, Panaversity](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/agent-factory-paradigm/spec-driven-development)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
- [Spec-Driven Development: workflow, narzędzia i ryzyka — Selleo](http://selleo.com/blog/spec-driven-development)
- [SDD Workflow Documentation — Tessl/Giuseppe Trisciuoglio](https://tessl.io/registry/giuseppe-trisciuoglio/developer-kit/2.8.0/files/plugins/developer-kit-specs/docs/sdd-workflow.md)
- [CAPE: Contextual Objectivity in AI Verification — arXiv:2512.14761](https://arxiv.org/pdf/2512.14761)
- [Sprint Contracts: Pre-Coding Success Agreements for Multi-Agent Tasks — Agent Patterns](http://agentpatterns.ai/agent-design/sprint-contracts/)
- [Analyzing Message-Code Inconsistency in AI Coding Agent-Authored Pull Requests — arXiv:2601.04886](https://arxiv.org/html/2601.04886v1)
- [Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents — arXiv:2603.17150](https://arxiv.org/html/2603.17150v1)
- [Vibe Engineering: Reflection — A Completion Verification Layer for Autonomous AI Coding Agents — Medium/Dzianis Vashchuk](https://medium.com/@dzianisv/vibe-engineering-reflection-a-completion-verification-layer-for-autonomous-ai-coding-agents-deb193d5a848)
- [Why Do AI Coding Agents Say 'Done' When They're Not Actually Done? — BSWEN](https://docs.bswen.com/blog/2026-03-12-ai-agents-say-done-not-done)
- [Comparing AI Coding Agents: A Task-Stratified Analysis of Pull Request Acceptance — arXiv:2602.08915](https://arxiv.org/abs/2602.08915)
