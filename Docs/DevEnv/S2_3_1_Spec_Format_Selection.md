# S2.3.1 — Spec Format Selection

**Status:** Researched
**Predecessor(s) ID:** S2.3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; content written |

---

## Overview

The format in which a specification is written — plain Markdown narrative, EARS syntax, OpenAPI definitions, structured YAML, or hybrid — is not a cosmetic choice. Format directly influences how AI agents parse intent, how reviewers validate completeness, which tooling ecosystem becomes available, and whether specifications can be machine-validated at all. Wrong format cascades: if you specify in free-form Markdown, you cannot use linters; if you choose YAML but your review process expects Markdown prose, reviewers will skim and miss rigor; if you mix formats inconsistently, agents struggle to extract preconditions and postconditions.

This section outlines the five major format families in use in SDD practice (2025–2026), the trade-offs of each, and how to pick the right format for your context.

---

## Five Format Families

### 1. Narrative Markdown (Free-Form Prose)

**Example:** User stories, acceptance criteria written as plain Markdown lists and paragraphs.

```markdown
# User Registration

As a new user, I want to sign up with my email and password so that I can access the app.

## Acceptance Criteria
- The system should accept a valid email address
- The password should be at least 8 characters long
- If the email already exists, show an error message
- On success, redirect to the dashboard
```

**Strengths:**
- Readable by non-technical stakeholders (product owners, designers)
- Flexible; any software can edit it
- Low barrier to adoption — developers already know Markdown
- Works for exploratory specs where exact structure is still being discovered

**Weaknesses:**
- No machine validation; ambiguity detector is a human reviewer
- Vague triggers and conditions ("should accept", "if") — leaves agent interpretation wide open
- Hard to trace which acceptance criterion is which; diffs become unclear
- Scales poorly; a 50-requirement document becomes unfocused quickly
- Agents must infer preconditions, postconditions, and invariants from prose

**Best for:**
- Early brainstorming and discovery phases
- Small features (< 5 requirements)
- Teams with strong synchronous communication
- Spec-First mode (specs guide but do not lock code)

**When to avoid:**
- Spec-as-Source mode (code regeneration requires unambiguous specs)
- Safety-critical or compliance-heavy domains
- Distributed teams or asynchronous workflows
- Features with complex state transitions

---

### 2. EARS (Easy Approach to Requirements Syntax)

**Example:**

```markdown
### Requirement: Email Validation
WHEN a user enters an email address,
the system SHALL validate the format matches RFC 5322 standard.

#### Scenario: Valid Email
GIVEN a user is on the sign-up form
WHEN they enter "user@example.com"
THEN the system SHALL accept the input
AND enable the "Next" button

#### Scenario: Invalid Format
GIVEN a user is on the sign-up form
WHEN they enter "not-an-email"
THEN the system SHALL reject the input
AND display the error message "Invalid email format"
```

**Syntax structure:** 
- **Triggers:** WHEN (event), IF (state), WHILE (continuous), WHERE (context)
- **Modal verbs:** SHALL (binding), SHOULD (recommended), MAY (optional)
- **Scenarios:** Given/When/Then format with clear preconditions and outcomes

**Strengths:**
- Structured yet readable — non-technical stakeholders can follow the patterns
- Machine-parseable; linters can enforce trigger/action/outcome structure
- Forces explicit preconditions, triggers, and postconditions
- Agents can decompose EARS requirements into preconditions, actors, and actions automatically
- De facto industry standard in safety-critical domains (aerospace, automotive, medical devices)
- GitHub Spec Kit, Amazon Kiro, and cc-sdd all default to or strongly recommend EARS
- Scenario blocks (Given/When/Then) map directly to test cases

**Weaknesses:**
- Steeper learning curve for free-form writers
- Requires discipline; bad EARS is worse than free-form (overly formal without clarity)
- Not suitable for architectural decisions or technology selections (those belong in design.md)
- Scenario explosion: 10 requirements × 3–5 scenarios each = 30–50 test cases to maintain
- Some requirements fit EARS patterns naturally; others feel forced

**Best for:**
- Spec-Anchored and Spec-as-Source modes
- Safety-critical, compliance-heavy, or regulated domains
- Cross-team distributed work (structure reduces ambiguity)
- Large features (15+ requirements) where traceability matters
- Teams using GitHub Spec Kit, Kiro, or cc-sdd (native tooling support)

**When to avoid:**
- Very early brainstorming (structure is premature)
- Single-person, tight-loop iteration
- Architectural or design documents (use design.md instead)

---

### 3. OpenAPI / Structured Schemas

**Example:**

```yaml
openapi: 3.1.0
info:
  title: "User Registration API"
  version: "1.0.0"
paths:
  /auth/register:
    post:
      summary: "Register a new user"
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - email
                - password
              properties:
                email:
                  type: string
                  format: email
                  description: "Valid RFC 5322 email"
                password:
                  type: string
                  minLength: 8
      responses:
        '201':
          description: "User created successfully"
        '400':
          description: "Invalid email or password too short"
        '409':
          description: "Email already registered"
```

**Strengths:**
- Machine-validated; schema violations caught by linters and schema validators
- Maps directly to API contracts and SDKs
- Ideal for REST/GraphQL/gRPC services
- Tools can generate client libraries, mock servers, integration tests from the spec
- Tooling maturity: Swagger UI, Redoc, stoplight, postman all consume OpenAPI natively
- Reduces client-server misalignment

**Weaknesses:**
- Not suitable for UI/UX requirements (no way to spec form flows, user interactions)
- Overly formal for business logic; a business rule ("users can only register once") is not a schema property
- Domain-specific to API/service contracts; cannot capture domain events or state machines
- Mixing API schema with functional requirements leads to conflated specs
- Does not address acceptance criteria or edge cases beyond HTTP status codes

**Best for:**
- Service/API contracts (REST, GraphQL, gRPC)
- Backend-heavy features where the spec is primarily about data contracts
- Teams that already use OpenAPI for other APIs
- Integration testing frameworks (can auto-generate test fixtures from spec)

**When to avoid:**
- Features that are primarily UI-driven (forms, navigation, workflows)
- Functional requirements that transcend HTTP (background jobs, state machines, event flows)
- Teams that need non-technical stakeholder input on acceptance criteria

---

### 4. Structured Agent-Focused Formats (YAML/JSON)

**Ecosystem:** Open Agent Spec (OAS), OSSA, AgentContract, SpecDD.

**Example (SpecDD — `.sdd` file alongside source code):**

```yaml
Spec:
  Purpose: "Register new users with email and password"
  Actors:
    - "End user (new)"
    - "Admin (future)"

Must:
  - "Accept valid RFC 5322 emails"
  - "Require password ≥ 8 characters"
  - "Prevent duplicate registration"
  - "Send confirmation email"

Must Not:
  - "Store plaintext passwords"
  - "Register accounts without verification"

Scenario:
  - GIVEN: user on sign-up form
    WHEN: enters valid email + password
    THEN: account created + email sent
  - GIVEN: email already registered
    WHEN: user tries to register same email
    THEN: error shown + no new account created
```

**Strengths:**
- Designed specifically for AI agents; agents extract intent more reliably than from prose
- Compact; fits local context window (under 2KB per spec)
- Can be colocated with code (one `.sdd` file per module)
- Supports inheritance and composition (child specs inherit parent constraints)
- Reduces global context size compared to all-in-one spec documents
- SpecDD research (2025–2026) shows agents produce fewer architectural divergences when given local `.sdd` specs

**Weaknesses:**
- Narrower ecosystem; not as many tools as OpenAPI or EARS
- Steeper adoption curve (teams must learn multiple format conventions)
- YAML indentation errors are silent; JSON is stricter but more verbose
- Not naturally human-reviewable by non-technical stakeholders
- SpecDD, OSSA, and OAS are still maturing; no single clear winner

**Best for:**
- Spec-as-Source mode with agentic architectures (agents read and regenerate)
- Distributed/modular specs (many small specs, not one monolithic document)
- Teams using polyglot tooling (code agents, orchestration agents, safety monitors)
- Research/prototyping in agent-driven development

**When to avoid:**
- Requirements that must be signed off by non-technical stakeholders
- Teams without existing YAML/structured data experience
- Greenfield projects where spec format is not yet stabilized

---

### 5. Hybrid Multi-Format (Recommended for Large Projects)

**Pattern:** Combine EARS for functional requirements + OpenAPI for service contracts + YAML config for non-functional constraints.

```
specs/user-registration/
├── requirements.md          ← EARS format + scenarios
├── design.md               ← Narrative + architecture decisions
├── api-contract.yaml       ← OpenAPI endpoint spec
└── constraints.yaml        ← Non-functional: rates, timeouts, security policies
```

**Strengths:**
- Each format carries what it is best at: EARS for business logic, OpenAPI for contracts, YAML for config
- Tooling coverage is best-in-class: linters for each format, agents can parse all
- Scales to large systems (different teams own different spec layers)
- Reduces format bikeshedding: "when should EARS be used?" is answered once, upfront
- GitHub Spec Kit and Kiro both recommend or assume this pattern

**Weaknesses:**
- Requires discipline; inconsistency becomes a risk (EARS requirement not reflected in OpenAPI)
- Multiple validation passes needed
- Teams must maintain multiple format validators
- More surface area for error

**Best for:**
- Medium to large features (20+ requirements)
- Multi-team coordination where different teams own different layers
- Spec-Anchored or Spec-as-Source modes
- Compliance-heavy projects (clear traceability across layers)

**When to avoid:**
- Small, single-team projects (overhead not justified)
- Very early brainstorming (too much structure too soon)

---

## Format Selection Decision Tree

```
Start: Do you need spec machine validation?
├─ NO
│  ├─ Is this exploratory / early brainstorming?
│  │  └─ YES → Use Narrative Markdown (free-form)
│  └─ Is it a public API / service contract?
│     └─ YES → Use OpenAPI
│
└─ YES: Machine validation required
   ├─ Is this primarily a business logic / workflow feature?
   │  └─ YES → Use EARS
   │     ├─ Large team / distributed? → Add YAML constraints + OpenAPI
   │     └─ Small team / single-domain? → EARS alone
   │
   ├─ Is this primarily a service / API contract?
   │  └─ YES → Use OpenAPI
   │     ├─ Also have business rules? → Add EARS layer
   │     └─ Complex state? → Add YAML state machines
   │
   └─ Is this an agent-centric or modular architecture?
      └─ YES → Use structured format (SpecDD, OSSA, OAS)
         └─ Also have functional requirements? → Add EARS layer
```

---

## Current Industry Defaults (2025–2026)

| Tool / Framework | Default Format | Second Choice |
|---|---|---|
| **GitHub Spec Kit** | EARS (free-form refinable) | Hybrid EARS + OpenAPI |
| **Amazon Kiro** | EARS + YAML design | OpenAPI for services |
| **cc-sdd** | EARS (strict) | Hybrid multi-layer |
| **Claude Code custom workflows** | Hybrid (narrative `requirements.md` + `design.md` + YAML) | Pure EARS |
| **SpecDD** | YAML with EARS scenarios | Pure EARS as fallback |
| **Cursor / GitHub Copilot** | Free-form narrative (improving → EARS) | (Tools in flux; no standard yet) |

The dominant pattern (2025–2026) is **hybrid: EARS for requirements + structured YAML/OpenAPI for technical layers**. This is the pattern used by the most mature open-source tools (Spec Kit, cc-sdd) and the most successful commercial SDD tools (Kiro).

---

## MyVocaList Codebase Alignment

MyVocaList currently uses **hybrid narrative + EARS emergence**:

- `Docs/specs/venues/requirements.md` — free-form narrative with implicit EARS
- `Docs/specs/venues/design.md` — narrative architecture
- `Docs/specs/venues/tasks.md` — ordered task list

The codebase is **Spec-First / Spec-Anchored mode**: specs guide initial generation, agents read them, but specs are not yet formally machine-validated.

**Recommendation for future evolution:**

1. **Immediate (current state):** Continue as-is. The hybrid narrative approach works for single-developer iteration and Spec-Anchored maintenance.

2. **When features become complex (20+ requirements):** Introduce EARS formatting in `requirements.md` acceptance criteria section. No tool change; just enforce Given/When/Then structure on scenarios.

3. **If multi-team coordination emerges:** Add `api-contract.yaml` (OpenAPI) and `constraints.yaml` (rates, security, collation) as separate layers. Kiro pattern.

4. **If moving toward Spec-as-Source (full regeneration):** Adopt formal validation tooling (EARS linter, schema validator). This is not necessary until code generation becomes primary workflow.

---

## Anti-Patterns and Pitfalls

### Anti-pattern 1: Mixing EARS and free-form arbitrarily
**Problem:** Some requirements use EARS format, others don't. Agents cannot establish a parsing pattern.

**Fix:** Choose one format per section. If using EARS, all acceptance criteria must follow the pattern.

### Anti-pattern 2: Storing all constraints in one format, splitting logic across formats
**Problem:** "Business rules in EARS, state machines in narrative, API contracts in OpenAPI." Becomes impossible to trace a change across layers.

**Fix:** Use the multi-layer hybrid *intentionally*. One layer = one responsibility. EARS for "what must the system do", OpenAPI for "what does the API look like", YAML for "what are the operational constraints".

### Anti-pattern 3: Format chosen for tooling, not for human comprehension
**Problem:** "We use YAML because our linter validates it," but only 2 engineers can read YAML fluently.

**Fix:** Choose format for the **primary audience** (product owner, engineer, agent). Tooling is secondary. If stakeholders must review, readability wins.

### Anti-pattern 4: Over-specifying in formal syntax
**Problem:** Every detail is a SHALL statement; the spec becomes 200 pages of EARS requirements. Agents drown in signal-to-noise.

**Fix:** Use formal syntax for critical requirements; narrative for context. A good EARS spec is 30–50 requirements, not 200.

### Anti-pattern 5: Treating format as immutable
**Problem:** "We chose Markdown five years ago; we can't switch to YAML now."

**Fix:** Format choice is *reversible*. Markdown EARS can be converted to YAML. Formats are tools, not commitments. If a new format serves the team better, switch.

---

## Sources

- [EARS Format Guide — forztf/open-skilled-sdd](https://github.com/forztf/open-skilled-sdd/blob/HEAD/skills/openspec-proposal-creation/reference/EARS_FORMAT.md)
- [Feature Request: EARS Integration — GitHub Spec Kit Issue #1356](https://github.com/github/spec-kit/issues/1356)
- [Spec Requirements Generation — cc-sdd](https://github.com/gotalab/cc-sdd/blob/fd3fc86c/tools/cc-sdd/templates/agents/claude-code/commands/spec-requirements.md)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft for Developers](https://developer.microsoft.com/blog/spec-driven-development-spec-kit)
- [Specification Proposal Creation Skill — Agent Skills](https://agent-skills.md/skills/forztf/open-skilled-sdd/openspec-proposal-creation)
- [Spec-Driven Development Conventions — Weaverse/.agents](https://github.com/Weaverse/.agents/blob/main/rules/spec-driven-development.md)
- [SpecDD — Specification-Driven Development framework](https://specdd.ai/)
- [What Is Spec-Driven Development? — sdd.sh](https://sdd.sh/2026/03/what-is-spec-driven-development/)
- [Define Once, Deploy Everywhere: Agent Definition Languages Compared — Agentic Academy](https://agentic-academy.ai/posts/agent-description-languages-compared/)
- [AGENTS.md Specification: A Research-Backed Guide — ASDLC.io](https://asdlc.io/practices/agents-md-spec/)
- [Specification-Driven Agent Development — Agentic Patterns](https://agentic-patterns.com/patterns/specification-driven-agent-development)
- [OSSA Specification: The Normative Contract for AI Agents — OSSA](https://openstandardagents.org/specification)
- [AgentContract SPEC.md — GitHub](https://github.com/agentcontract/spec/blob/main/SPEC.md)
- [Open Agent Specification Technical Report — arXiv](https://arxiv.org/html/2510.04173v2)
- [Open Agent Spec 1.5 — GitHub](https://github.com/prime-vector/open-agent-spec/blob/main/spec/open-agent-spec-1.5.md)
- [Agent Skills Open Standard Explained — Paperclipped](https://www.paperclipped.de/en/blog/agent-skills-open-standard-interoperability/)
