# S2.2.2 — Verbosity vs. Precision Tension

**Status:** Researched
**Predecessor(s) ID:** S2.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written |

---

## Overview

Spec-Driven Development creates a fundamental tradeoff: the more detailed a specification becomes, the more precisely it guides AI agents, but the more it risks becoming unmaintainable, difficult to review, and prone to gathering stale information. Conversely, terse specifications remain readable and maintainable but force agents to invent behavior, introducing hallucination and drift.

This tension is not merely a style preference — it directly impacts team velocity, code quality, and the viability of the spec-as-primary-artifact principle. Understanding where the boundary lies, and how to structure specs across multiple levels of abstraction, is essential to making SDD work in practice.

---

## 1. The Complexity Displacement Problem

The central insight from 2025–2026 research is that **detailed specs do not eliminate engineering work — they relocate it**. A specification precise enough to drive reliable AI code generation must encode type constraints, algorithm logic, schema definitions, and edge case coverage. When a spec becomes detailed enough to prevent ambiguity, it converges toward code-like structure.

### The Sweet Spot Narrows

Tim Kapp (January 2026) observed that modern systems generating 150–360+ page specifications are doing "far more cognitive work up front" — yet this is rational only because "the cost of researching, validating, and refining intent up front has collapsed." But this does not mean the total work shrinks. Scott Logic (2025) measured Spec Kit producing 2,000+ lines of Markdown per feature while still introducing bugs — traditional iterative prompting produced working code ten times faster.

The tension manifests as the **Curse of Instructions**: as specification detail accumulates, adherence to individual instructions degradates (Addy Osmani, O'Reilly). The spec becomes too large for the agent's attention span, and priorities get lost in noise.

### Spec Slop vs. Over-Specification

Two opposite failures bound the viable range:

| Failure Mode | Cause | Outcome |
|---|---|---|
| **Spec Slop** | Low-precision prose written at speed | Unreliable agent output; assumptions propagate; hallucination |
| **Over-Specification** | Excessive detail accumulates beyond model capacity | Agent compliance degrades; specs become pseudo-code; maintenance burden |

Neither extreme is salvageable. A vague spec forces the agent to guess; precise but bloated specs cause the agent to miss critical details amid noise.

### Specification Complexity Displacement Pattern

AgentPatterns.ai (2026) documents the phenomenon: making a spec vague and reliability collapses; making it exhaustive and model adherence collapses. The complexity was not eliminated — it was moved from code into the specification.

Example: "POST /auth/login accepts `{ email, password }`. Hash password using bcrypt with cost factor 12. Return 200 with `{ token, expires_at }` on success. Return 401 for invalid credentials. Rate-limit to 5 attempts per IP per 15 minutes." This is precise enough to guide generation — but it is also a type signature, a schema, a hashing spec, a rate-limiting algorithm, and a logging requirement, all in prose. The engineering burden did not disappear; it shifted upstream to the spec author.

---

## 2. Spec Length and AI Adherence

Empirical research from 2025–2026 reveals a non-linear relationship between specification detail and agent output quality:

### Initial Detail Improves Output

Early research (arXiv:2602.00180, Thoughtworks 2025) confirmed that structured specifications dramatically improve AI output quality by removing ambiguity. Agents given a 200-line feature spec with acceptance criteria produce code more aligned with intent than agents given a 10-line prompt.

### Beyond a Threshold, Detail Hurts Compliance

But the relationship breaks down. Research by Cortex (2026 State of AI Benchmark) and practitioner reports from major fintech projects found that specifications growing beyond 2,000–3,000 lines begin showing diminishing or negative returns: agents miss edge cases because those edge cases are buried in lengthy prose; agents hallucinate requirements they perceive to be priorities; the cost of maintaining and reviewing the specification exceeds the cost of code review on the generated output.

### Practical Calibration

- **Under 300 lines of spec per feature:** Generally too terse. Agents invent behavior; hallucination is common.
- **300–2,000 lines of spec:** Sweet spot. Detail sufficient to guide, concise enough that agents maintain attention and reviewers can process it in 30–45 minutes.
- **2,000–5,000 lines of spec:** Diminishing returns. Agents start missing details; review burden becomes significant.
- **5,000+ lines of spec:** Common failure pattern. Agents ignore sections; maintainers stop updating the spec; it becomes stale. Marmelab (Nov 2025) called this "sledgehammer to crack a nut."

---

## 3. The Two-Tier Spec Architecture

The most successful SDD teams (documented by Tim Kapp, Rushi's technical deep-dive, and multiple practitioners) have adopted a **two-tier specification structure** to navigate the tension:

### Tier 1: Human-Readable Intent (10–20 pages)

This is the specification written for humans. It captures:
- **Why:** The business problem, user intent, and differentiators
- **What:** Functional outcomes and key acceptance criteria in Given/When/Then format
- **Constraints:** Non-functional boundaries (performance, security, compatibility)
- **Out-of-scope:** What the feature explicitly does not do

This layer is stable, rarely changes, and serves as the audit trail and rationale for architectural decisions.

### Tier 2: Machine-Consumable Appendices (modular, selective)

Organized as isolated appendices, each corresponding to a subsystem or component:
- **Database schemas** with fields, types, constraints, and migration rules
- **API contracts** with input/output shapes, status codes, and error responses
- **Algorithm specifications** in pseudocode or formal notation
- **Configuration and deployment rules**
- **File paths** and module structure

Each appendix is designed for **selective consumption** by agents. A detailed table of contents exposes H1/H2 structure, and agents are instructed to retrieve only the sections needed for their task.

### Why Two Tiers Work

- **Humans review the main spec (10–20 pages) for intent alignment.** This is fast — 20–30 minutes — and high-leverage. Errors caught here prevent hundreds or thousands of bad code lines.
- **Agents retrieve appendices on demand.** They pull only the schemas, APIs, and rules relevant to their immediate task, avoiding context overload.
- **Maintenance is localized.** Appendices update in isolation. When a schema changes, only that appendix updates. The main spec remains stable.
- **Reusability is structural.** Multiple agents working different features can pull the same database spec appendix without conflict.

---

## 4. Specification Staleness — The Primary Failure Mode

Codified Context research (arXiv:2602.20478) identified **specification staleness as the single largest failure mode** in production SDD systems. When a subsystem's implementation changes but its specification is not updated, agents generate code based on stale information, producing silent bugs that surface only during testing.

### Why Specs Go Stale

- **Decoupled update cadence:** Code changes in the session; specs are updated in a later biweekly review (if at all). The lag creates a window where agents act on outdated information.
- **No automated enforcement:** Unlike code that must pass tests, specs have no automated validator that fails when they diverge from implementation. Staleness is invisible until the agent reads the outdated context and produces contradictory code.
- **Maintenance overhead:** Keeping specs synchronized requires discipline. Teams report 1–2 hours per week for biweekly review passes (Codified Context, 2026). When teams are under schedule pressure, spec maintenance is deferred or skipped.

### Mitigation Strategies

1. **Update specs in the same session as code changes.** If a service refactor changes a function signature, update the relevant spec appendix immediately. This adds 5–10 minutes per session but prevents session-spanning staleness.

2. **Automated drift detection:** Store acceptance criteria as machine-readable assertions (JSON, OpenAPI, Gherkin) and validate them against code on every PR. When spec and implementation diverge, the build fails.

3. **Spec versioning alongside code.** Include spec checksums or version IDs in the codebase. If code references an outdated spec version, fail the build. (This requires tooling investment.)

4. **Lightweight acceptance tests as the authority.** Rather than trusting prose specs, encode acceptance criteria as passing tests. Agents are instructed to: "Implement behavior such that all tests in `test-auth-spec.ts` pass." Tests are the enforceable contract; prose augments with rationale.

---

## 5. The Maintenance Cost of Verbosity

Empirical data from 2025–2026 shows the hidden costs of verbose specifications:

### Specification Bloat

GitHub Spec Kit users report that AI-generated specs are often bloated with unnecessary assumptions (multiple sources, 2025–2026). Manual trimming and refinement consistently improves downstream code quality. This suggests that verbosity is not precision — it is noise.

### Review Burden Scaling

- **Lightweight specs (200–400 lines per feature):** 15–30 minutes to review. High leverage — errors caught here prevent rework.
- **Detailed specs (2,000+ lines per feature):** 1–2 hours to review. Review burden begins exceeding code review (Faros AI data showed 91% longer review times under high AI adoption without spec rigor).
- **Very detailed specs (5,000+ lines):** Reviewers begin skimming, missing edge cases. Maintenance becomes a bottleneck rather than a leverage point.

### The Paradox of Double Review

Teams adopting high-verbosity specs without clear boundaries often fall into a trap: they review the spec, then review the generated code. The review burden is not halved — it is doubled. Rushi's technical deep-dive (2026) noted: "Verbose markdown is tedious to review. Specs may not reveal implementation issues. Code review still catches actual bugs. Double review burden (specs + code)."

---

## 6. Selective Application of Detail

Not all features need the same level of specification detail. Research from Augment Code (2026) on **micro-specs** and decision matrices from multiple frameworks identify when detail pays off:

### High Detail Justified

- **Security and authentication logic:** Edge cases have material consequences (e.g., rate-limiting gaps, token expiry, replay attack prevention). Detail prevents expensive bugs.
- **Payment and financial workflows:** Non-functional requirements (idempotency, audit trails, reversibility) are complex and material. Detail ensures correctness.
- **Database schema and migration rules:** Schema mistakes cascade. Detailed specifications prevent data corruption and backward-compatibility breaks.
- **Multi-system integrations:** Contract mismatches between systems are costly. Detailed API specs, with examples and error cases, prevent integration failures.

### Minimal Detail Sufficient

- **Simple CRUD operations without branching logic:** A generic create/read/update/delete pattern with input/output shapes is enough. Agents reliably implement these without extensive detail.
- **UI layout and styling:** Behavior is predictable; agents can infer from design systems and examples.
- **Trivial getters and pass-through functions:** Writing a micro-spec for a single-line function creates busywork without improving coverage on logic that actually fails.

---

## 7. The Golden Rule: Minimum Necessary Detail

The canonical guidance from arXiv:2602.00180 (the foundational SDD paper) is:

> **Use the minimum level of specification rigor that removes ambiguity for your context.**

This applies at multiple levels:

1. **Feature level:** Does the feature justify 200 lines of detail or 2,000? Does it touch security? Financial? If so, lean toward detail. If it is a form submission, lean toward concision.

2. **Specification layer:** Does this requirement need prose, a table, a schema, or an executable test? Use the minimum artifact type that encodes the constraint.

3. **Appendix level:** Do agents need a 500-line database specification, or is a 50-line schema sufficient? Start minimal; expand only when agents produce contradictory code.

The principle is **parsimony with traceability** (Alex Rezvov, Feb 2026): exclude everything that does not affect behavior, but ensure what remains is complete enough to reconstruct intent unambiguously.

---

## 8. Spec-First vs. Spec-Anchored Trade-Offs

The three maturity levels defined in S1.2 have different verbosity-maintenance profiles:

| Level | Typical Spec Length | Maintenance | When to Use |
|-------|---------------------|-------------|-------------|
| **Spec-First** | 300–500 lines per feature | Low (initial write only) | Greenfield projects, rapid prototyping, when code won't be maintained long-term |
| **Spec-Anchored** | 500–1,500 lines per feature | Medium (biweekly sync) | Production systems with iterative evolution; specs and code coexist and both are maintained |
| **Spec-as-Source** | 1,500–3,000+ lines per feature, often split into appendices | High (continuous sync via CI/CD) | Mission-critical systems; specs are the sole source of truth; manual code edits are prohibited or tracked as overrides |

**Practical observation:** Most teams in 2025–2026 operate at Spec-First or Spec-Anchored. Spec-as-Source requires mature tooling and discipline; the maintenance burden is significant.

---

## 9. Preventing Over-Specification: Checklists and Gates

Teams should apply explicit gates when authoring specs to prevent the accumulated verbosity that leads to staleness and review burnout:

### Pre-Review Spec Audit

Before handing a spec to an agent or reviewer, ask:

1. **Can this sentence be eliminated without losing meaning?** If an example illustrates a point and the prose restates it, delete the prose.

2. **Is this implementation detail (the "how") or intent (the "what")?** If the spec says "use Redis for caching," that is implementation. Replace with "response times must be under 300ms cached" and let the agent choose.

3. **Is this constraint enforceable?** "The system should be fast" is not enforceable. "API calls complete within 100ms p99 on the test server" is. If not enforceable, either make it measurable or delete it.

4. **Would this fail a test?** Acceptance criteria should map to test cases. If a criterion does not correspond to a failing test when unimplemented, it is not a requirement — it is a wish.

5. **Is this repetition?** Specification rot often takes the form of the same rule stated three times in different contexts. Consolidate.

### Specification Size Review

For each feature spec:
- **Under 300 lines:** Likely too terse; expand critical paths and error cases.
- **300–1,000 lines:** Good zone. Reviewable in 20–30 minutes. Sufficient detail without bloat.
- **1,000–2,000 lines:** Acceptable for complex features. Ensure appendices are modular.
- **Over 2,000 lines:** Question whether detail is justified. Apply two-tier architecture (main + appendices). Consider if feature should be split.

---

## 10. The Real Constraint: Context Window Capacity

The most practical bound on specification verbosity is **AI agent context window capacity**. As of May 2026, leading models (Claude 3.5 Sonnet, GPT-4 Turbo, Opus 4.6) support 100K–200K token windows. A single feature spec that consumes 20% of the context window leaves 80% for codebase context, conversation history, and error messages. Beyond that threshold, agents become "forgetful" — they miss details buried in the middle sections.

### Practical Limits

- **Textual spec:** roughly 1,000 words = 1,500 tokens.
- **With appendices and examples:** roughly 3,000–5,000 words = 5,000–7,500 tokens.
- **Reserve 40%+ of context for agent-discovered context** (codebase files, error messages, iteration history).

If a feature spec approaches 20,000+ tokens, it cannot fit within safe context budgets. The architectural solution is: split the feature into smaller tasks or restructure the spec into a two-tier system where agents retrieve appendices on demand rather than loading them all at once.

---

## Integration With Quality Characteristics (S2.2)

The four quality characteristics from S2.2 interact with the verbosity-precision tension:

1. **Ubiquitous Language:** Consistent terminology reduces the need for repeated explanations, lowering verbosity without sacrificing precision.

2. **Given/When/Then Structure:** Structured scenarios compress behavior specification more efficiently than paragraph prose. A single Given/When/Then scenario often replaces 3–5 sentences of narrative.

3. **Completeness on Critical Path, Conciseness Elsewhere:** This principle directly addresses the tension. Cover the critical path (security, correctness, API contracts) with detail. Omit obvious variations.

4. **Clarity and Determinism:** Deterministic language is precise without being verbose. "The system responds within 100ms" is clearer and shorter than "the system should be fast and responsive in typical usage scenarios."

Together, these four characteristics are **the mechanism by which specs achieve high precision with low verbosity.**

---

## Known Tensions (Unresolved in Industry Practice)

### Spec Formality vs. Agile Iteration

Spec-Driven Development's phase-gate workflow (requirements → design → tasks → implementation) echoes waterfall planning, contradicting Agile's iterative principle. Critics (notably Marmelab, Nov 2025) ask: "Can iterative Agile coexist with comprehensive specifications?" Some teams have answered yes through **nested iteration:** specifications are written for a single feature (small scope), reviewed, then implemented in an iteration. The iteration is short; the planning is rigorous. But this hybrid model remains under-documented in the literature.

### Spec Maintenance at Scale

For large codebases (100K+ lines), maintaining specification synchronization becomes a visible cost. Practitioners have documented that spec-anchored approaches degrade as the codebase grows and the number of subsystems multiplies. The mitigation is still tooling — automated spec validation, drift detection, and templating — but tooling maturity lags adoption.

### When SDD Provides Value vs. Overhead

SDD's fixed overhead (spec authorship, review, maintenance) makes sense for complex features, compliance-sensitive work, and multi-session projects. But for a one-line bug fix or a single-session experimental change, spec overhead exceeds benefit. The decision framework in S10.1 (Applicability) addresses this, but practitioners consistently report confusion about where the threshold lies.

---

## Practical Recommendations

### For Practitioners

1. **Start with the Golden Rule:** Write enough spec to remove ambiguity, no more.

2. **Use two-tier architecture for feature specs over 1,000 lines:** Main spec (human-readable intent) + appendices (machine-consumable schemas, APIs, algorithms).

3. **Enforce spec review before implementation.** Catch ambiguity and over-specification at review time, not during code generation.

4. **Update specs in the same session as code changes.** Prevent staleness by treating spec maintenance as part of the implementation task.

5. **Measure the cost-benefit tradeoff.** Track review time, agent hallucination rates, rework cycles, and spec staleness. Use this data to tune your specification rigor per feature type.

### For Tool Builders

1. **Implement selective loading of specification appendices.** Agents should retrieve only the schema/API/rules relevant to their task, not load the entire 3,000-line spec.

2. **Add automated drift detection.** Fail the build when acceptance criteria in specs conflict with passing test behavior.

3. **Support modular spec composition.** Allow features to reference shared specs (database, auth, API style) rather than repeating them.

4. **Provide diff-aware spec review.** When a spec updates, show the delta to human reviewers, not the full document.

---

## Conclusion

The verbosity-precision tension is real and cannot be eliminated — only managed. Detailed specifications guide agents with fewer hallucinations but risk becoming unmaintainable. Terse specifications remain readable but force agent invention and drift.

The practical resolution is **strategic granularity:** apply detail where the stakes are high (security, correctness, integration contracts), use concise intent-focused language where behavior is standard, and structure large specifications into a two-tier system that keeps human review lightweight and agent context focused.

The common failure patterns — spec slop, over-specification, staleness, and double-review burden — are all preventable with disciplined authorship and minimal governance. Teams that apply these patterns report better outcomes than teams that choose either extreme.

---

## Sources

- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Specification-Driven Development: The Four Pillars — Alex Rezvov](https://blog.rezvov.com/specification-driven-development-four-pillars)
- [Spec-Driven LLM Development: Precise Engineering Through Specifications — David Lapsley](https://blog.davidlapsley.io/engineering/process/best%20practices/ai-assisted%20development/2026/01/11/spec-driven-development-with-llms.html)
- [Why Spec-Driven Development Is the Antidote to Vibe Coding — Rupeshit Patekar, Medium](https://medium.com/@rupeshit/why-spec-driven-development-is-the-antidote-to-vibe-coding-516200fe51cc)
- [GitHub Spec Kit Deep Dive: AI-Driven Specification Development Methodology — Redreamality](https://redreamality.com/garden/notes/github-spec-kit-guide/)
- [AI 101: From Vibe Coding to Spec-Driven Development — Alyona Vert., TuringPost](https://www.turingpost.com/p/sdd)
- [Codified Context: Infrastructure for AI Agents in a Complex Codebase — arXiv:2602.20478](https://arxiv.org/html/2602.20478v1)
- [The Specification Layer: Why Enterprises Can't Scale AI Development Without It — David Daniel Research](https://daviddaniel.tech/research/articles/specification-layer/)
- [Spec-Driven AI Coding: Writing Specs Agents Execute Well (2026) — SurePrompts](https://sureprompts.com/blog/spec-driven-ai-coding)
- [Why the Hard Part of Coding No Longer Lives in Code — Tim Kapp](https://www.timkapp.com/articles/why-the-hard-part-of-coding-no-longer-lives-in-code)
- [Spec Complexity Displacement: When Specs Become Code — AgentPatterns.ai](https://agentpatterns.ai/anti-patterns/spec-complexity-displacement/)
- [The Spec-First Development Paradigm — Advanced Context Engineering for Coding Agents, DeepWiki](https://deepwiki.com/humanlayer/advanced-context-engineering-for-coding-agents/6-the-spec-first-development-paradigm)
- [Micro-Specs: The Pattern That Significantly Improves AI Agent Test Coverage in High-Risk Modules — Augment Code](https://www.augmentcode.com/guides/micro-specs-pattern-ai-agent-test-coverage)
- [The Role of Specs in the Claude Code Era — Masatake Yamoto](https://www.yamotty.me/post/20260310)
- [Critical Analysis of Spec-Driven Development — Cameron SJ, GitHub](https://github.com/cameronsjo/spec-compare/blob/main/docs/critical-analysis.md)
- [Spec-Driven Development (SDD): A Technical Deep Dive — Rushi's](http://www.rushis.com/spec-driven-development-sdd-a-technical-deep-dive-into-the-methodologies-reshaping-ai-assisted-engineering/)
