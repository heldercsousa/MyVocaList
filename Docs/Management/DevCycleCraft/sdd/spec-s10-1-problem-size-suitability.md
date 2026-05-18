# S10.1 — Problem-Size Suitability

**Status:** Researched
**Predecessor(s) ID:** S10

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written from Tier 1 and Tier 2 sources |

---

## Overview

SDD is economically justified in specific problem contexts and becomes overhead in others. The distinction depends on three dimensions: **problem size and longevity**, **team structure and distributed decision-making**, and **failure cost tolerance**. This section articulates the boundaries: when SDD pays its own way and when simpler approaches (vibe coding, prompt engineering, doc-driven development) are more efficient.

The core insight from production data (Thoughtworks, Ministry of Programming, Amazon case studies): SDD is a response to a real, measurable cost — the accumulation of architectural inconsistency, spec drift, and integration failure in systems larger than a single developer can hold in working memory. Below that threshold, the spec overhead is waste.

---

## S10.1.1 — When SDD Delivers Value (Greenfield Best-Case)

### Size Threshold: The 10–20 File Boundary

**SDD is strongly justified when:**

- **The codebase will exceed 10–20 interdependent files** — Once a project crosses this size, the implicit knowledge required to maintain consistency exceeds what a single developer can hold in working memory. Specs become the mechanism for externalizing architecture.
- **Multiple sessions required** — The work spans more than a few hours and requires context to survive across developer sessions. Specifications written in prose are continuity artifacts that outlive conversation history.
- **Multiple teams or agents working on the same surface** — Coordination across engineers or AI agents becomes necessary. Without specs, feature divergence accelerates exponentially with team size. The spec becomes the primary coordination layer, replacing alignment meetings and preventing interdependent teams from building incompatible solutions.

### Compliance and Regulatory Context

**SDD is non-negotiable in regulated domains:**

- **Healthcare, fintech, government** — Regulatory bodies require documented traceability from requirement → specification → implementation → test. SDD provides this chain natively: spec files in version control, code generated from specs, tests derived from specs, all with git history. Auditors see a complete audit trail. Without SDD, compliance documentation becomes a parallel overhead, often maintained manually and drifting from reality.
- **EU AI Act compliance** — AI-assisted development requires documented intent and human oversight at critical checkpoints. Specs create the required documentation of architectural decisions, constraints, and human review gates.
- **Security-critical systems** — Attack surfaces, cryptographic constraints, and failure modes must be explicitly documented before implementation. Specs allow security architects to review intent before code is written. Post-implementation security review is more expensive and less effective.

### Production Integration and Microservices Context

**SDD earns its cost when integration failures are expensive:**

- **Multi-target SDKs or protocols** — The same logic must exist in five languages, or a wire protocol must be stable across client and server implementations. Specs become the single source of truth; code is generated per target. Without specs, each implementation diverges incrementally, creating subtle incompatibilities discovered in production.
- **Microservice or API-heavy architecture** — Integration mismatches between services are expensive to debug in production. OpenAPI specs enforced via contract testing (Pact, Dredd, Schemathesis) prevent breaking changes from shipping. Example from Postman (2025 State of APIs): 60% of API-first teams ship at least one breaking change per quarter that their spec did not account for. Spec-driven contract testing eliminates most of these.
- **Long-lived systems with slow-changing core** — Features get added to stable, well-understood problem domains. The core logic is known; the risk of hallucination is proportional to the cost of integration failure. Specs reduce hallucination risk in proportion to domain stability.

### Observed Gains (Production Data)

**From organizations that adopted SDD systematically (not project-by-project):**

- **30–50% fewer late-stage defects** — Red Hat SDD quality study documented 40% reduction in defect density on systems where SDD was standard practice.
- **60–80% fewer AI-generated regressions** — Teams using specs report dramatically lower unintended behavioral changes compared to vibe coding. Specs provide a clear contract that code must satisfy.
- **30% increase in feature delivery velocity** — Measured end-to-end (specification → implementation → validation), not just coding velocity. Includes shorter QA cycles and fewer post-launch change requests.
- **Ambiguity resolution in the document, not in the debugger** — Specs force edge cases to surface during specification review, not during QA. Catching a missing timezone-handling requirement during spec review costs hours; catching it in production costs days.

---

## S10.1.2 — When SDD Is Overhead (Exploratory and Throwaway Context)

### The Exploration Phase Problem

**SDD imposes net cost when:**

- **Exploration phase — prototype or concept validation** — The goal is to discover what the feature *should* do, not implement a known design. Specifications are premature when requirements themselves are unknown. Vibe coding explores faster; once exploration converges on a direction, formalize into a spec before scaling to a production system.
  - **Example:** "Build a search feature for user profiles" is vague. Vibe coding sketches three approaches: full-text database search, vector embeddings, or a hybrid. Once one approach is chosen, write the spec for *that* approach; then deploy.

### Solo Developer / Single Session / Small Scope

- **One engineer, one feature, clear requirements** — The benefit of spec discipline is coordination and knowledge persistence across sessions. If there is no one to coordinate with and the work finishes in a single session, a single `SPEC.md` at the repo root is the minimal useful boundary. Full SDD workflow is overkill.
- **Well-isolated refactors of stable code** — Code is already there; the change is localized and low-risk. Reverse-engineering a spec for existing stable logic is waste. (The spec becomes relevant when you *change* the code, not when it's already working.)
- **Bug fixes with clear root cause** — The cause is documented, the fix is contained, no architectural ambiguity. A spec adds ceremony without reducing risk. Document the fix in a commit message; move on.

### Throwaway Internal Tools

- **Scripts, test harnesses, one-off migrations** — Tools that will be used once and discarded. Specs are for systems that outlive their authors. A throwaway tool with a README is sufficient.

### The Three-Month Wall (From Industry Data)

**Without specs, vibe coding ships prototypes faster for roughly three months. After that, technical debt compounds:**

- Code reaches ~10–20 interdependent files
- Multiple engineers work on the same module
- Requirements change mid-feature
- AI-generated regressions surface in integration tests
- Onboarding new engineers requires reading hundreds of lines of cryptic code

At that point, the absence of a spec is actively slowing down delivery. Teams without specs hit the wall; teams with SDD adoption move it to 6+ months.

**Implication:** Use vibe coding for the first prototype. Formalize into a spec the moment the work outlasts a single session or crosses into a second developer's purview.

---

## S10.1.3 — Multi-Session and Multi-Agent Context

### Persistent Context Across Sessions

**SDD is most valuable when work spans multiple sessions and engineers:**

- **Session survival of intent** — A prompt vanishes when the session ends. Spec files in git survive, can be reviewed by a second engineer, and seed the context for the next session. For teams that work in shifts or across time zones, specs are the only reliable persistence mechanism for architectural intent.
- **AI agent context isolation** — Without specs, each new agent session must re-read the entire codebase or rely on a prompt that decays with every iteration. Specs allow agents to start each task with a fresh 200k+ context window plus a clear contract to follow. (Observed in Pockit, Ministry of Programming, and GSD research: agents with specs reduce hallucination rate by 60–80% compared to prompt-only agents.)

### Distributed Team Coordination

**SDD prevents feature divergence in multi-team or multi-agent scenarios:**

- **Two teams building interdependent features** — Without a shared spec, Team A builds a notification service assuming REST polling; Team B builds a consumer expecting WebSocket push. Specs would have caught this mismatch in review before either team wrote a line of code.
- **Parallel agent execution** — Subagent delegation patterns depend on clear specs. Each agent receives a task extracted from the spec, executes it, and the results are validated against the spec. Agents working without specs produce contradictory code because they lack a shared contract.

---

## S10.1.4 — Problem Complexity and Architecture Stability

### Stable, Well-Understood Domains

**SDD reduces hallucination risk proportionally to domain stability:**

- **CRUD applications on known schemas** — The problem is well-defined: read/write records, validate input, enforce constraints. Specs eliminate ambiguity around validation rules, error handling, and UI flows. AI generates correct code because the spec is unambiguous.
- **Payment processing, billing, inventory** — These are well-understood domains with standardized patterns. Specs for these domains converge on a small set of variants. Specs reduce speculative implementation.
- **Highly novel or experimental domains** — If the problem has never been solved before, specs are harder to write and less effective. The domain knowledge required to specify well is expensive to acquire. Vibe coding (multiple explorations) is more cost-effective than spec-first in truly novel contexts.

### Architectural Constraints and Consistency

**SDD enforces consistent application of architectural rules:**

- **Multiple services implementing the same business logic** — If a payment retry strategy must work the same way in API, Worker, and Mobile services, a spec ensures consistency. Without it, each implementation drifts in subtle ways.
- **Design system enforcement** — UI components must follow Material Design 3 constraints, spacing, and accessibility rules. A spec enforces these; vibe coding produces inconsistent components.
- **Database schema evolution** — New features often require schema changes. Specs allow architects to review proposed changes for consistency with existing patterns, performance implications, and migration complexity before code is written.

---

## S10.1.5 — Brownfield Retrofit: The Hard Case

### Why Brownfield Is Harder Than Greenfield

Retrofitting SDD into existing codebases is significantly harder because:

1. **Reverse-engineering specs is lossy** — Code shows the "what"; specs should capture the "why." Extracting intent from working code often requires developer interviews or reverse-engineering from tests. LLMs can assist but cannot replace human judgment.

2. **Existing architectural patterns resist specification** — Mature codebases have accumulated implicit conventions. Codifying these into a constitution requires surfacing assumptions that developers take for granted. Unresolved architectural conflicts often surface during this process.

3. **Context windows exceed practical limits** — Large systems cannot be fully analyzed in a single LLM context. Piecemeal reverse-engineering leaves gaps.

4. **Teams resist retroactive documentation** — Developers on stable code often view spec-writing as busywork: "The code *is* the specification." Mandates from above meet passive resistance.

5. **Spec decay from day one** — Retroactively-written specs often drift immediately because the team did not participate in writing them. Ownership is unclear.

### Successful Brownfield Adoption Patterns

**Pattern 1: Spec-First for New Features (Most Successful)**

Do not retroactively spec existing code. Instead:
- Start specifying the *next* feature or change that touches the codebase
- Each new PR is an opportunity to incrementally build spec coverage for code being modified
- Coverage grows organically; legacy code remains un-specified until actively changed
- When you do touch legacy code, write the spec for the change only, not for the entire module

**Observation:** This pattern reaches 80+ hours of ROI faster than full retroactive specification. Teams report genuine productivity gains after 4–8 weeks instead of months of upfront overhead.

**Pattern 2: Constitution Before Specs (Enterprise Best Practice)**

For large teams on legacy systems:

1. **Establish architectural rules first** — Before writing a single feature spec, create `constitution.md` documenting the codebase's actual conventions:
   - Naming patterns (package structure, file organization)
   - Error handling idioms
   - Where to place new modules
   - Coding standards
   - Architecture constraints (what depends on what; forbidden circular imports)

2. **Force the team to surface implicit assumptions** — This conversation often uncovers unresolved architectural decisions. Resolving them *before* specs prevents specs from encoding conflicting patterns.

3. **Establish review gates** — Linting, architectural boundary checks, test coverage thresholds. Enforce these before SDD specs even exist.

4. **Then introduce spec discipline gradually** — With the foundation solid, specs feel like a natural extension of existing practice, not an imposed process.

**Observation (Ministry of Programming):** Teams that start at this level of discipline see ROI in 3–6 months, vs. 6+ months for full retroactive specification attempts. Morale impact is significantly better.

**Pattern 3: Brownfield Bootstrap (Tool-Assisted Reverse-Engineering)**

SDD tools now offer automated brownfield onboarding (OpenSpec, Kiro, Actualyze):
- `reverse-engineer` skills analyze code, tests, schemas, and configuration
- Results are tagged `[INFERRED]` or `[IMPLICIT-RULE]` and require human review
- Gaps are identified; reconciliation tools detect and align spec-code divergence

**Realistic timeline:** 2–4 weeks of focused effort for a mid-sized system (5–10k LOC); 6–8 weeks for a large system (50k+ LOC).

### Anti-Patterns: Do Not Do These

- **Mandate full-system specs overnight** — Attempt to spec the entire codebase retroactively before touching a single line. Results in massive, unreviewed specs that decay immediately.
- **Ignore team resistance** — Treat SDD as a technical rollout only, ignoring cultural and human factors. Developers will find ways to work around it.
- **Retrofit without preserving existing knowledge** — Lose custom CLAUDE.md, git history context, or institutional patterns when adopting SDD tools.
- **Treat all code equally** — Try to specify stable, low-risk legacy code at the same level of detail as new features. Wastes time on areas that change rarely.

---

## S10.1.6 — Decision Framework: Will SDD Pay for Itself?

Use this decision tree to evaluate whether SDD is justified for your current work:

| Question | SDD Justified If | SDD Overhead If |
|----------|------------------|----------------|
| **Is this a prototype or exploration?** | No — specs are premature | Yes — use vibe coding; formalize once direction is clear |
| **Will this code outlast this session?** | Yes — specs provide continuity | No — document in commit; move on |
| **Will multiple people/agents touch this code?** | Yes — specs prevent divergence | No — single developer overhead |
| **Will this system exceed 10–20 files?** | Yes — specs maintain consistency | No — implicit knowledge is sufficient |
| **Is integration failure expensive?** | Yes — specs prevent mismatches | No — isolated code, low risk |
| **Are there regulatory or compliance requirements?** | Yes — specs are mandatory | No — specs add ceremony only |
| **Is the domain novel or poorly understood?** | No — stable domains benefit most | Yes — exploration is more cost-effective |
| **Are requirements likely to change mid-build?** | Yes — specs surface change cost | No — clear, stable requirements |

**Decision rule:** If 4+ questions answer "Yes," SDD is justified. If 4+ answer "No," vibe coding is more efficient.

---

## S10.1.7 — Real-World Problem Sizing

### Small Greenfield Project (1–5k LOC)

**Recommended approach:** Minimal SDD structure
- Single `requirements.md` (design + requirements combined)
- No formal design review; architect reviews with developer
- Tasks are loose; not every line of code needs a task
- One-person spec writing

**ROI timeline:** 2–3 weeks before specs pay for themselves via reduced rework

### Medium Greenfield Project (5–20k LOC)

**Recommended approach:** Full Spec-First workflow
- requirements.md, design.md, tasks.md
- Design review by 2+ engineers or a senior architect
- Tasks are one per developer session (~4–8 hours)
- Multiple developers, or one developer across multiple weeks

**ROI timeline:** 4–8 weeks before productivity gains exceed initial spec overhead

### Large Greenfield / Multiple Teams (20k+ LOC)

**Recommended approach:** Spec-Anchored with constitutional governance
- Formal spec structure with versioning and traceability
- Constitution.md enforcing architectural constraints
- Design review gates before implementation
- Contract testing (Pact, Dredd, Schemathesis) on API specs
- Separate spec review PR from implementation PR

**ROI timeline:** 3–6 months; larger teams see benefits faster due to reduced coordination cost

### Legacy System, Single Team Adding Features

**Recommended approach:** Spec-First for new features only
- Do not retroactively spec existing code
- Spec only the next feature or change
- Use Spec-First pattern; mature into Spec-Anchored over time
- Constitution captures existing patterns; specs extend them

**ROI timeline:** 4–8 weeks per feature; cumulative ROI grows as specs accumulate

### Highly Regulated Domain (Finance, Healthcare, Government)

**Recommended approach:** Spec-Anchored with audit trail
- Full traceability: Requirement ID → Spec section → Implementation code → Test case
- All specs in git with mandatory review
- Compliance annotations in specs (which requirement satisfies which regulation)
- Automated drift detection (code must match spec at each commit)

**ROI timeline:** 6–12 months; compliance value justifies overhead even on small changes

---

## S10.1.8 — Size vs. Complexity Trade-Off

**Size** (lines of code, number of files) correlates with SDD ROI.
**Complexity** (interconnection, state management, novel logic) amplifies SDD ROI.

A 500-line financial calculation is more suitable for SDD than a 5000-line UI framework.

| Size | Complexity | SDD Justified? | Example |
|------|-----------|----------------|---------|
| <200 LOC | Low | No | Single API endpoint, simple validation |
| 200–2k LOC | Low | Maybe — if multi-person | Isolated feature, single developer |
| 200–2k LOC | High | Yes — specs prevent bugs | Complex financial logic, payment processing |
| 2k–10k LOC | Low | Maybe — if multi-session | Large feature, one developer, clear requirements |
| 2k–10k LOC | High | Yes | Multi-service system, intricate interactions |
| 10k+ LOC | Low | Yes — if multi-person | Large codebase, many collaborators |
| 10k+ LOC | High | Yes — strongly justified | Large system, complex domain, multiple teams |

---

## Sources

- [Spec-Driven Development with AI Agents: A Practical Guide — Xcapit Inc.](https://www.xcapit.com/en/blog/spec-driven-development-ai-agents)
- [Why Spec-Driven Development Breaks at Scale (And How to Fix It) — Arcturus Labs](https://arcturus-labs.com/blog/2025/10/17/why-spec-driven-development-breaks-at-scale-and-how-to-fix-it/)
- [Spec-Driven LLM Development (SDLD) — David Lapsley, Ph.D.](https://blog.davidlapsley.io/engineering/process/best%20practices/ai-assisted%20development/2026/01/11/spec-driven-development-with-llms.html)
- [Contract Testing Plan: From OpenAPI to CI — Spec Coding](https://spec-coding.dev/blog/contract-testing-plan-from-openapi-to-ci)
- [Spec Driven Development as a Standard — Ministry of Programming](https://ministryofprogramming.ghost.io/spec-driven-development-as-a-standard/)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship Production-Ready AI-Generated Code — Pockit](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Spec-Driven Development (2026 Guide): Build Production AI Code — Product Builder](https://www.productbuilder.net/ru/learn/spec-driven-development)
- [Spec-Driven Development with AI Coding Agents: The Workflow Replacing "Prompt and Pray" — Java Code Geeks](https://www.javacodegeeks.com/2026/03/spec-driven-developmentwith-ai-coding-agents-the-workflow-replacingprompt-and-pray.html)
- [Spec-Driven Development: GSD vs Spec Kit vs OpenSpec — azanello](https://azanello.com/blog/spec-driven-development-tools-compared)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices)
- [Why Spec-Driven Development Tools Fail in the Enterprise — Simon Martinelli](https://martinelli.ch/why-spec-driven-development-tools-fail-in-the-enterprise/)
- [Spec-Driven Development: AI-Assisted Coding — SolGuruz](https://solguruz.com/blog/spec-driven-development-guide/)
- [Spec-Driven Development (2026 Guide): Build Production AI Code — Product Builder](https://www.productbuilder.net/learn/spec-driven-development)
- [Spec-Driven Development (SDD): A Technical Deep Dive — Rushi](https://www.rushis.com/spec-driven-development-sdd-a-technical-deep-dive-into-the-methodologies-reshaping-ai-assisted-engineering/)
- [SDD, Compound Engineering, BMAD: Which AI Development Philosophy Should You Choose? — Angelo Lima](https://angelo-lima.fr/en/sdd-compound-engineering-bmad-philosophies-en/)
