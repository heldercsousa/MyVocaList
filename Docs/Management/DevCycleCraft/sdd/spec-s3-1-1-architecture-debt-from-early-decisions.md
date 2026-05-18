# S3.1.1 — Architecture Debt from Early Decisions

**Status:** Researched
**Predecessor(s) ID:** S3.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; sources from Tier 1 & 2 SDD sources (Thoughtworks, Martin Fowler, arXiv:2602.00180, InfoQ, Kiro, GitHub Spec Kit, academic research on AI architecture, and 2025–2026 practitioner case studies) |

---

## Overview

Architecture debt from early planning-phase decisions is a category of technical liability that surfaces during implementation but cannot be escaped without significant rework. The Planning Phase locks architectural choices — technology selection, data model design, service boundaries, integration approaches — before implementation reveals whether those decisions are actually viable. Once code is written against a locked architectural choice, reversing that decision requires updating the specification, re-reviewing the change with stakeholders, regenerating the task list, and re-implementing affected tasks. The risk is not that planning produces bad decisions, but that planning locks in decisions before evidence is available to validate them.

This document explores how architecture debt accumulates in SDD practices, the mechanisms that make it sticky, mitigation strategies, and the organizational patterns that either amplify or contain the damage.

---

## The Lock-In Problem

The Planning Phase works by design-before-code: humans decide on architecture, and implementation executes that design. This phase separation is cost-effective when it prevents rework in code — but it becomes costly when the locked-in decision proves wrong during implementation.

### Reversibility Spectrum

Architectural decisions exist on a reversibility spectrum:

| Category | Reversibility | Cost to Change at Mid-Project | Example |
|----------|---------------|-------------------------------|---------|
| **One-way door** | Irreversible or near-irreversible | 3–12 months + team disruption | Database choice, core data model, multi-tenancy strategy, service boundaries |
| **Semi-reversible** | Requires significant rework | 2–8 weeks | Technology stack within a layer (e.g., web framework, ORM), API contract changes |
| **Two-way door** | Easily reversible | < 1 week | UI component choice, caching strategy, internal refactoring |

The problem: planning-phase decisions often assume they are reversible (category 2) when they are actually one-way doors (category 1). A database choice made casually during design becomes the structural foundation of every table, migration, and query. A service boundary declared in the design becomes embedded in team communication, deployment pipelines, and cross-service contracts.

### Why Planning Lacks Evidence

The Planning Phase operates with three information deficits:

1. **No working code exists yet.** The architecture has not been tested against the actual implementation constraints. A chosen library might lack a required feature. A data model might create unexpected indexing problems. A service boundary might turn out to require constant synchronization that defeats the purpose of separation.

2. **Hidden dependencies are invisible.** Full coupling surfaces only when code is written. A planning-phase dependency graph (Task A → Task B → Task C) is necessarily incomplete. During implementation, hidden coupling emerges: Task C depends on a database migration that both A and B assume exist, or a utility that Task B creates, or a configuration constant that Task A initializes.

3. **Scaling assumptions are untested.** A design that "scales to 100 concurrent users" has no proof until code is deployed and tested under load. A schema that "can handle millions of rows" is a claim, not a fact. A service architecture that "enables parallel development" has not been tried with an actual team.

The mitigation is not to avoid planning altogether (planning without evidence is worse than planning with incomplete evidence). The mitigation is to recognize that planning-phase decisions carry reversibility debt, and to manage that debt explicitly.

---

## Mechanisms of Lock-In

### Dependency Accretion

Once a planning-phase decision is coded, subsequent decisions build on top of it. A database choice affects schema design. Schema design shapes repository queries. Repository queries constrain service APIs. Service APIs constrain ViewModel bindings. ViewModel bindings constrain XAML data binding structure. Each layer adds coupling, and together they create an interlocking system where reversing the original decision requires reworking layers built on top of it.

**Example from case study (Rova, a Y Combinator startup, 18-month AI-generated codebase):**

> By month 8, the database choice (chosen for its "flexibility" and "scalability") had spawned 11 different customer schemas, each with slight variations. By month 14, a seemingly simple schema migration to fix a discovered design flaw cascaded across all eleven schemas. The cost of "fixing" the database choice that month would have been 6–8 weeks of engineering time and customer data migration risk. The cost of recognizing the mistake 6 months earlier, when no customers existed yet, would have been 2–4 days of specification revision and task reordering.

### The "Temporary" Trap

Planning-phase decisions that feel temporary ("we'll refactor this later," "this is good enough for MVP") often acquire supporting infrastructure that actively resists change:

- Monitoring dashboards tuned to the current approach
- On-call runbooks documenting how to operate the system
- Incident patterns and tribal knowledge about failure modes
- Cross-team contracts that depend on the current behavior

After six months, a "temporary" architecture choice has developed an immune system. The cost of changing it is no longer just the technical effort; it includes retraining the team, rewriting runbooks, rebuilding dashboards, and updating every service that depends on the contract.

### Organizational Inertia

Teams mirror their communication structures in their architecture. A "temporary" choice that was made by one team becomes normalized when a second team copies it, then locked in when a third team depends on it. By the time anyone recognizes the pattern as problematic, fixing it requires organizational alignment that doesn't exist.

**Example:** If the API Gateway team chose JWT authentication and the Payments team chose OAuth 2.0 (because they didn't communicate during planning), the system now has two authentication patterns. Unifying them is now a cross-team coordination problem, not just a technical one. The original decision wasn't evil; the organization structure ensured inconsistency.

---

## Why SDD Amplifies This Risk

Spec-Driven Development can amplify architecture debt in several ways:

### 1. Planning Becomes More Detailed and Binding

In traditional development, a loosely-specified plan can be revised mid-implementation without ceremony. In SDD, the specification is the contract. Changing the specification during implementation requires:
- Updating requirements.md or design.md
- Re-reviewing the change with stakeholders
- Regenerating the task list
- Potentially rolling back implemented code that now violates the new spec

This friction is intentional and usually beneficial (it prevents drift). But it means that a planning-phase mistake is more costly to reverse.

### 2. Agents Propagate Decisions at Scale

An AI agent executing a task from a locked-in design decision doesn't question the decision; it implements against it. An agent generating a utility function propagates the architectural choice to every callsite. An agent implementing schema migrations applies the locked-in data model to every table. A single architectural decision, once encoded in a design document, gets amplified across hundreds of lines of agent-generated code.

### 3. The Specification Becomes Binding at Multiple Levels

In the MyVocaList project (and similar SDD setups), the specification cascade is: constitution → requirements → design → tasks → implementation. Each layer constrains the next. If the design locks in a technology choice, the tasks become task-specific, and agents execute tasks without the option to propose a better technology. They can only propose micro-optimizations within the locked choice.

### 4. Validation Gates Can Defer Discovery of Architectural Flaws

The planning review gate (S3.1) checks design against requirements, but it can only do so based on the information available at planning time. Hidden complexity surfaces later, during implementation. By the time an agent discovers that a chosen library lacks a required feature, the task is halfway done, the specification says to use that library, and reversing the decision requires re-planning.

---

## Manifestations of Architecture Debt from Early Decisions

### Technical Manifestations

1. **Pipeline Jungle:** A data pipeline designed in planning as a "simple ETL flow" evolves into a convoluted system of interdependent scripts, retry logic, and ad-hoc fixes. The original design assumed a level of data consistency that production doesn't guarantee.

2. **Query Performance Degradation:** A schema designed for flexibility (with many optional columns and wide tables) becomes slow as data grows. Queries that were fast at 100,000 rows timeout at 100 million. The schema choice made sense in planning (more flexible, fewer migrations). The performance cost was not visible until scale.

3. **Service Boundary Violations:** Service boundaries declared in design to "enable independent deployment" require constant cross-service calls, background synchronization, or eventual-consistency hacks that defeat the purpose. Coupling emerges that the original decomposition didn't anticipate.

4. **Scaling Bottlenecks:** An architecture designed for "100 concurrent users" hits contention at 1000 users. The chosen locking strategy, connection pooling, or cache invalidation approach creates a bottleneck that wasn't visible during implementation. Fixing it requires rearchitecting components that depend on the original choice.

### Organizational Manifestations

1. **Onboarding Cost Inflation:** New team members struggle to understand why the system is structured the way it is. The original planning rationale was sound at the time but is now invalid. The system's current shape reflects decisions that made sense six months ago but don't anymore.

2. **Incident Patterns:** Certain failure modes repeat. On-call engineers know "if X happens, restart the service" but don't know why. The original architectural decision created the failure mode, but because the decision is deeply embedded, the workaround becomes permanent.

3. **Velocity Degradation:** Adding a new feature requires changing more than seems necessary. The architecture doesn't support the new feature well, but pivoting to a better approach is "too expensive" because too much code has been built on top of the current design.

### Cost Progression Over Time

Research on AI-generated codebases shows a consistent pattern:

| Timeline | Manifestation | Cost of Fixing |
|----------|---------------|----------------|
| **Month 0–2 (Planning → Early Implementation)** | Architectural mismatch becomes obvious | 1–2 weeks of specification revision + task reordering |
| **Month 3–6 (Feature Growth Phase)** | Multiple features now depend on the original decision | 2–8 weeks of targeted rework |
| **Month 9–12 (System Complexity Phase)** | Organizational patterns have formed; workarounds are in place | 6–16 weeks of selective rewrite + team alignment |
| **Month 18+ (Structural Lock-In Phase)** | Full rewrite or migration required | 2–6 months of engineering effort + customer/operational risk |

A 2–4 week mistake in planning, caught early, costs 2 days of specification work. The same mistake, discovered at month 6, costs 6 weeks of rework. At month 12, it may cost a full rewrite.

---

## Mitigation Strategies

### 1. Reversibility-Aware Design

Treat architecture design as a set of reversibility decisions: one-way doors, semi-reversible decisions, and two-way doors.

**Best practice:**
- Identify the one-way-door decisions explicitly in the design document
- For each one-way door, document:
  - Why this choice was made (what alternatives were considered and rejected)
  - What evidence would prove it wrong
  - What the upgrade path looks like if it needs to change
  - When that upgrade path should be triggered (revenue threshold, user count, team size, measured performance metric)

**Example for MyVocaList:**

> **One-way Door: SQLite for persistence**
> - Reasoning: SQLite is suitable for single-device, single-user mobile apps. It eliminates the need for cloud backend deployment.
> - Evidence that would trigger reconsideration: Multi-device sync requirements, collaborative editing, real-time synchronization across a shared queue.
> - Upgrade path: Migrate to a cloud backend (PostgreSQL + REST/gRPC API). Estimated effort: 4–6 weeks. Trigger: When the app supports multiple devices per user, or shared queue management across users.
> - Migration strategy: Implement a sync adapter that can hydrate local SQLite from cloud on app startup; populate cloud from SQLite on app close.

### 2. Spike Validation for High-Risk Decisions

For planning-phase decisions that carry significant reversibility debt, allocate a pre-implementation spike (2–5 days of engineering time) to validate the choice against the actual implementation constraints.

**Spike deliverable:** A report answering:
- Does the chosen library/framework support all required features?
- What are the known performance limitations at the target scale?
- What breaking changes are likely in the next 12 months?
- How many team members would need to be upskilled to work with this technology?

**Example:** Before committing to DevExpress MAUI in the planning phase, a spike might have been: "Build a sample CRUD page with offline sync, test collation-based search, measure performance at 10,000 items. Do all required features work?" If the spike reveals that a feature is missing or performance is unacceptable, the design is updated before tasks are created.

### 3. Contingency Margins in Task Scheduling

Build contingency into the task schedule for decisions that are uncertain. Instead of:

```
Task 1: Design schema (1 day)
Task 2: Implement repository (2 days)
Task 3: Implement service (1 day)
```

Structure it as:

```
Task 1: Design schema (1 day)
Task 1.1: Validate schema design via spike (1 day) ← CONTINGENCY
Task 2: Implement repository (2 days)
Task 2.1: Performance test at 10,000 items; if performance targets not met, redesign schema (1–3 days) ← CONTINGENCY
Task 3: Implement service (1 day)
```

The contingency tasks don't execute unless the underlying decision needs validation. If the design is sound, they're skipped and the project finishes early. If the design is flawed, the contingency provides a structured opportunity to pivot without derailing the entire feature.

### 4. Decision Inventory and Reversal Policy

Every quarter, conduct a decision inventory: identify the architectural decisions that are causing the most friction (incidents, slow onboarding, repeated questions, velocity drag). For each, ask:

- Is this decision still valid?
- What evidence would we need to reverse it?
- Do we have that evidence already?
- What's the cost of continuing with this decision vs. the cost of reversing it?

Document a reversal policy:
- Which decisions are "decided and final" (low reversibility, high certainty)
- Which are "revisable in Q2" (medium reversibility, medium certainty)
- Which are "under trial" (high reversibility, low certainty)

**Example from MyVocaList:**
> **CLAUDE.md Reversal Policy**
> - **"Use DevExpress MAUI for all UI"** — Decided and final (high switching cost, no viable alternative in scope)
> - **"Use SQLite for persistence"** — Decided but revisable in Q3 2026 if multi-device sync is required (reversibility: 4–6 weeks, trigger: documented user request)
> - **"Use EF Core + migrations for schema management"** — Under trial; evaluating for performance (if queries slow beyond acceptable thresholds in Q2, consider query generation or raw SQL)

### 5. Architecture Contracts as Executable Constraints

In multi-agent or large-team settings, encode architectural decisions as architecture contracts (ADRs with enforcement):

- **Structural contract:** Which services can call which other services? Which dependencies are forbidden? Enforce via linting rules.
- **Data model contract:** Which tables can be modified by which services? Enforce via repository interfaces and authorization.
- **API contract:** Which endpoints expose which data shapes? Enforce via OpenAPI specs and contract tests.

**Enforcement mechanism:** Before an agent implementation is accepted, validate it against these contracts. A violation signals either (1) the agent misunderstood the constraints, or (2) the constraints are wrong and need updating.

This prevents one agent's task from silently violating another agent's architectural decisions.

### 6. Living Specification, Not Frozen

Update the specification as implementation reveals architectural insights. If an agent discovers that the chosen library doesn't support a required feature, update the design.md to document the workaround or propose a new choice. Update tasks.md if the order needs to change. Make the revision visible so the next agent (or human reviewer) understands the decision evolved.

**Example change log in design.md:**
```
### Evolution Log

**2026-05-02:** Schema design validation spike revealed that the initial "wide table" design 
would result in table scans > 500ms at 100K items. 
Updated design to use columnar storage + composite indexes. 
Cost: +1 week of schema redesign + migration strategy. 
Tasks updated accordingly in tasks.md.
```

---

## When Planning Misses Reality: Case Studies

### Case Study 1: Rova (Y Combinator, 18-month AI-generated system)

Rova's planning phase locked in a "flexible, scalable database architecture" designed to support multi-tenant SaaS. By month 8, as customers grew, that flexibility became a liability: each customer's schema had slight variations, migrations became complex, and performance tuning was per-customer.

**What went wrong:** The planning phase optimized for scalability and flexibility (customer-facing concerns) without validating that schema flexibility could be operationalized. The design assumed "flexible schema is good"; implementation revealed "flexible schema creates operational complexity."

**Cost:** Month 14 discovery that schema migration (a task that should be hours) would take 6–8 weeks due to per-customer variations. The feature roadmap was blocked. Partial rewrite was required.

**Lesson:** Spike the operational constraints (migration complexity, per-tenant variance) during planning, not just the feature set.

---

### Case Study 2: Digital Scientists (AI-Generated Backend Study, 2026)

A startup built its backend on Claude-generated code, with architecture chosen during planning to "scale to thousands of concurrent users." By month 6, the system worked fine at 50 users but exhibited contention at 200+. The locking strategy chosen during planning didn't scale.

**What went wrong:** The planning phase validated the design against the business case ("we expect 1000 users in 2 years") but not against the near-term reality (50–100 actual users). The architecture was over-engineered for the current stage and under-engineered for a different bottleneck (locking, not throughput).

**Cost:** Month 8, a user-facing incident revealed contention. Month 9–10, rearchitecting the locking strategy. By month 14, the team realized the original architecture was "suited to neither their original direction nor their new one" because the business had pivoted.

**Lesson:** Plan for the stage you're at (50 users), not the stage you hope to reach (1000 users). Design reversible upgrade paths that are triggered by measured need, not projections.

---

### Case Study 3: Faros AI (Multi-Agent Architecture, 2025–2026)

Faros AI's planning phase declared service boundaries to "enable independent deployment" (one planning goal). By month 6, agents working on different services created hidden coupling: Service A's public API didn't match the contract that Service B expected. Service B had to add defensive code, creating coupling in the opposite direction.

**What went wrong:** The planning phase defined service boundaries as a topology ("Service A is independent from Service B") but didn't define contract specifications that both services would honor. Agents optimized their implementations locally without global visibility into cross-service contracts.

**Cost:** Month 10, incident cascades between services because one service's schema change broke another's assumptions. Both services had to be reworked to explicit contracts. Architecture decision that was supposed to enable independence became a coordination bottleneck.

**Lesson:** Architectural decisions about boundaries must include enforcement mechanisms (contracts, schema registries, API versioning). A boundary described in prose can be silently violated during implementation.

---

## When Architecture Debt Does NOT Emerge

Debt from early decisions is not inevitable. Projects that successfully avoid it share these patterns:

1. **Small, reversible scope:** Teams that scope each feature to a single architecture decision avoid compounding. A feature that requires deciding on "database choice" is scoped to that decision. The next feature uses a different decision.

2. **Continuous validation:** Teams that test planning decisions during implementation (spike validation, performance testing, contract verification) catch flaws early, before they compound. The cost of fixing is linear in time; discovery is delayed, not prevented.

3. **Living specifications:** Teams that update specs as implementation reveals gaps treat the spec as a contract that reflects reality, not a frozen artifact. This prevents drift between the spec (what we planned) and the code (what we built).

4. **Reversible architecture from the start:** Teams that use modular design, dependency injection, and explicit interfaces make reversibility structural. Swapping a database or framework is expensive but not a full rewrite.

---

## The S3.1.1 Mitigation Framework

To minimize architecture debt from planning-phase decisions in MyVocaList and similar SDD projects:

1. **Classify decisions by reversibility** in design.md
2. **Spike high-risk, one-way-door decisions** before committing to the spec
3. **Build contingency margin** into task scheduling for uncertain architectural choices
4. **Document the reasoning and upgrade path** for each major decision
5. **Enforce contracts** if multiple agents or teams are building against the same architecture
6. **Update the spec when implementation reveals gaps** — the spec should reflect reality, not hope
7. **Monitor for debt signals** (velocity degradation, incident patterns, onboarding difficulty) and conduct quarterly reversal reviews

---

## Sources

- [Why AI Architecture Decisions Are Hard to Reverse — Huzefa Motiwala (altersquare.io, 2026-01-30)](https://www.altersquare.io/ai-architecture-decisions-hard-reverse/)
- [The Specification Layer: Why Enterprises Can't Scale AI Development Without It — David Daniel (daviddaniel.tech, 2026-02-13)](https://daviddaniel.tech/research/articles/specification-layer/)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang (marvinzhang.dev, 2025-10-22)](https://marvinzhang.dev/blog/sdd-tools-practices)
- [SDD, Compound Engineering, BMAD: Which AI Development Philosophy Should You Choose? — Angelo Lima (angelo-lima.fr, 2026-04-03)](https://angelo-lima.fr/en/sdd-compound-engineering-bmad-philosophies-en/)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship Production-Ready AI-Generated Code — Pockit Blog (2026-04-07)](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Why AI Makes Architecture the Only Skill That Matters — Max L (orthogonal.info, 2026-02-02)](https://orthogonal.info/ai-plan-driven-development-architecture-only-skill-that-matters/)
- [AI Architecture Cost: An 18-Month Case Study — Bob Klein (digitalscientists.com, 2026-04-10)](https://digitalscientists.com/blog/ai-technical-debt-startup-case-study/)
- [SaaS Architecture Decision Framework: From MVP to Scale — Alex Mayhew (alexmayhew.dev, 2026-01-28)](https://alexmayhew.dev/blog/saas-architecture-decision-framework)
- [Preventing AI-Caused Tech Debt: How to Enforce Clean Architecture from Day One (overctrl.com, 2025-05-09)](https://overctrl.com/preventing-ai-caused-tech-debt-how-to-enforce-clean-architecture-from-day-one/)
- [Spec Driven Development: When Architecture Becomes Executable — InfoQ (2026-01-12)](https://www.infoq.com/articles/spec-driven-development)
- [Spec-Driven Architecture: When Agents Build, Architecture Must Speak — Philipp Beyerlein (innoq.com, 2026-02-01)](http://www.innoq.com/en/blog/2026/02/spec-driven-architecture-contracts-fuer-agenten/)
- [Stop Architecture Drift: Operationalizing ADRs with Automated Fitness Functions — Alexandre Castro (platformtoolsmith.com, 2026-04-06)](https://platformtoolsmith.com/blog/operationalizing-adrs-fitness-functions/)
- [The architecture of decisions nobody made — Alex Rios (alexriosme.substack.com, 2026-03-27)](https://alexriosme.substack.com/p/the-architecture-of-decisions-nobody)
- [AI-Generated Backends Break in Production. We Replaced Code with Specs. — Fascia (dev.to, 2026-02-25)](https://dev.to/fasciarun/why-we-banned-llms-from-runtime-and-what-we-do-instead-7ck)
- [From Components to Guarantees. < Spec-as-System — Michail (medium.com, 2026-02-16)](https://medium.com/@mtuchkov/from-components-to-guarantees-6303aa9edd93)
- [Spec-Driven Development Isn't Waterfall — But It Keeps Ending Up There — Thiago Pacheco (sudoish.com, 2026-04-17)](https://sudoish.com/spec-driven-development-waterfall-trap/)
- [Spec Kit Spark: Months Later, Lessons Learned — Mark Hazleton (markhazleton.com, 2026-03-18)](https://markhazleton.com/blog/spec-kit-spark-months-later-lessons-learned)
- [Structure Beats Prose: Specs for Coding Agents That Actually Work — Stefan van Egmond (medium.com, 2026-02-10)](https://medium.com/%40stefanvanegmond/structure-beats-prose-specs-for-coding-agents-that-actually-work-e035929b0f3d)
- [Good Architecture Delays Decisions — Bad Architecture Freezes Them — syarif (levelup.gitconnected.com, 2026-01-28)](https://levelup.gitconnected.com/good-architecture-delays-decisions-bad-architecture-freezes-them-fae717675fc8)
