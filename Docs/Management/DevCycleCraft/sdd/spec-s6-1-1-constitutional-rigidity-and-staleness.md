# S6.1.1 — Constitutional Rigidity & Staleness

**Status:** Researched  
**Predecessor(s) ID:** S6.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent |

---

## Overview

Constitutional constraints, as defined in S6.1, are enforced as immutable principles. The power of immutability is structural: a hook or CI gate that prevents a violation is more reliable than a prompt that the model might forget or deprioritize. Yet immutability becomes pathological when the constraint is wrong, when project evolution has rendered it obsolete, or when the cost of obedience now exceeds the cost of the original problem it was designed to prevent.

This is the constitutional rigidity problem: **strong enforcement of a principle that is no longer sound forces developers to either break the rule (risking system integrity) or obey the rule (accepting architecture that no longer fits).** The dilemma is acute in long-running projects where the rationale behind early constraints may no longer apply, but the constraints remain mechanically enforced.

The staleness problem is the companion failure mode: **constraints written at project inception degrade in relevance as the codebase evolves, and stale constraints mislead agents, block legitimate work, and accumulate unexamined clutter.** Without an amendment process as formal as the constitution itself, constraints either accumulate (cluttering the rules with dead code) or drift (rules changed informally without coordination), eroding the trust that makes constitutional enforcement meaningful.

---

## The Rigidity Trap

Constitutional constraints derive their power from rigidity. A hook that exits with code 2 cannot be argued with, cannot be deprioritized, cannot be forgotten between sessions. The model cannot reason its way around a hard denial — the tool simply fails. This is the point. Rules that are merely documented in CLAUDE.md, without mechanical enforcement, fail when context fills or pressure mounts. Constitutional constraints persist.

But persistence is only a virtue when the constraint is correct. When circumstances change and the constraint becomes wrong, rigidity transforms from a feature into an obstacle.

### Case: Database Platform Constraint

A project locked in SQLite at inception for its simplicity and portability:

```
# CLAUDE.md (month 1)
**Technology Constraints:** Database is SQLite; no migrations to 
PostgreSQL without architecture review.
```

This constraint was sensible when the project was a single-user mobile app with modest data volume. Six months later, the application has grown to require:
- Concurrent access from multiple devices
- Complex relational queries with performance requirements
- Advanced features (full-text search, JSON queries) that SQLite handles poorly

The constraint is now wrong. But if it is enforced via CI gates that reject migrations to PostgreSQL, agents cannot execute the rational architectural change without either breaking the rule (risky, erodes discipline) or escalating for a formal amendment (slow, creates bottlenecks).

### Case: Language Version Constraint

A project targeted C# 12 at inception:

```
# CLAUDE.md (month 1)
**Language Version:** C# 12 minimum. Use modern C# (13+) patterns.
```

Twelve months later:
- .NET 9 is released with language features that solve three classes of bugs the project has accumulated
- The runtime overhead of older patterns is now measurable in production
- The team is newly comfortable with language-level nullability analysis

The constraint is no longer optimal. But if it is mechanically enforced via compiler settings that error on C# 13 syntax, agents cannot adopt the features that would improve code correctness without first amending the constitution.

### Case: Test Coverage Threshold

A project mandates 80% code coverage as non-negotiable:

```
# CLAUDE.md (month 1)
**Testing Constitutional Constraint:** All new code must be covered 
by tests. Minimum coverage: 80% project-wide. CI gate enforces this.
```

Six months in, the project has shipped a feature that is genuinely difficult to unit-test (e.g., a real-time animation loop with precise timing constraints) because the test infrastructure for that domain was immature when the constraint was written. Adding tests for this feature would consume three person-weeks to build harness infrastructure for marginal value. Alternatively, the tests could be written in a lower-assurance way (mocking timers, accepting brittleness) that satisfies the mechanical gate but provides false confidence.

The constraint is contextually wrong here — not universally, but for this feature. Yet the rigid gate blocks deployment, and amending the constitution requires formal review.

---

## The Staleness Problem

Over time, constitutional constraints degrade even when they start correct. Three mechanisms cause this:

### 1. Implicit Knowledge Erosion

The original rationale for a constraint lives in the author's head or in a conversation that is not recorded. When the author leaves or forgets the rationale, future maintainers see only the constraint itself — divorced from the problem it solved — and cannot judge whether it is still warranted.

**Example:**
```
# CLAUDE.md (historical)
**Non-Negotiable:** All async methods must use CancellationToken.
```

This rule is sound — cancellation enables responsive shutdown and proper resource cleanup. But six months later, a new team member reads this constraint and asks: "Why?" If the answer is a vague "good practice," the constraint will be ignored when time pressure mounts. If the answer is rooted in a past production incident ("We had a hung thread pool on shutdown that crashed the app — CancellationToken solved it"), the constraint has teeth.

Without the incident context, the constraint is just a style preference that can be dropped.

### 2. Specification Drift

As the codebase evolves, the spec that justified a constraint may become false. The constraint persists, but the problem it addressed has been solved by other means.

**Example:**
```
# CLAUDE.md (original)
**Architecture Constraint:** Repositories are always injected; never 
instantiated directly. This prevents hidden coupling to the data layer.
```

At inception, this was important because the project had no dependency injection framework and repositories were being instantiated ad-hoc throughout the codebase, creating tight coupling. Once a DI framework (CommunityToolkit.Mvvm in MyVocaList) was properly wired, direct instantiation became impossible — the DI container simply could not find the type to create.

The constraint is now redundant. The problem is solved by the type system, not by the constitutional rule. But the rule remains in CLAUDE.md, consuming context and adding noise.

### 3. Silent Obsolescence

Technology and practices evolve faster than documentation. A constraint about database indexes might become irrelevant when the database engine is upgraded. A constraint about thread safety might be superseded when the language adds better primitives. A constraint about API versioning might be wrong after the API schema system changes.

**Example:**
```
# CLAUDE.md (2024)
**Database Constraint:** Always add a UNIQUE constraint on email columns 
to prevent duplicates. Do not rely on application-level validation.
```

This constraint was correct in 2024 when database constraints were the only reliable way to enforce uniqueness across concurrent writes. In 2025, the data validation framework was upgraded with strong distributed consensus primitives. The constraint is no longer the only (or best) way to ensure uniqueness, but agents are still following the old rule.

---

## Measuring Constitutional Staleness

Stale constitutional constraints exhibit these signals:

| Signal | Meaning |
|--------|---------|
| **Rationale absent** | The constraint text has no explanation of why it exists. Agents and humans cannot judge relevance. |
| **Complaint patterns** | Multiple developers independently complain that the rule "doesn't fit anymore" — sign of context shift since the rule was written. |
| **Violation acceleration** | Violations of the rule are increasing, not decreasing. Suggests the rule is fighting reality, not preventing it. |
| **Amendment deadlock** | Requests to relax or remove the constraint are pending review for weeks with no decision. The amendment process is broken. |
| **Contradiction emergence** | Two rules contradict each other; they were never in conflict at inception, but circumstances have changed. |
| **Coverage erosion** | Exclusions and exceptions to the rule accumulate — `unless X`, `for new code only`, `except in Y scenario`. The rule is becoming a special case. |

---

## The Amendment Process as Constitutional Constraint

The paradox of constitutional rigidity is that the solution — amending the constitution — must itself be constitutional. If amendments are informal, undocumented, or authorized arbitrarily, the constitution loses meaning. But if the amendment process is too rigid, it becomes a bottleneck that prevents necessary evolution.

S6.1.2 (Amendment Governance, not yet written) will define the formal amendment process. But the core insight applies here: **without an amendment process as rigorous as the constitutional constraints themselves, the constitution becomes a liability.**

The research sources (see below) identify this as a systemic problem in both political constitutions and in AI-assisted software development:

- **Political science (Tsebelis, Cambridge 2025):** High constitutional rigidity reduces amendment frequency but also increases the variance in amendment significance. When amendments finally occur, they tend to be larger and more disruptive than incremental changes would be.
- **SDD practice (Spec-Kit / GitHub, 2025–2026):** The "constitution" in Spec-Kit is described as "immutable principles," but Spec-Kit also acknowledges that "constitutional evolution" requires documented rationale, review, and backwards compatibility assessment.
- **Claude Code governance (anthropic.com, 2026):** CLAUDE.md instructions are read but not reliably followed. Compliance degrades with context length. No enforcement mechanism exists except hooks (which are a best-effort layer, not guaranteed).

---

## Constitutional Rigidity in MyVocaList

MyVocaList implements constitutional constraints via CLAUDE.md and `.claude/rules/`. Known areas of potential rigidity:

### 1. Technology Stack

```
**Stack:** .NET MAUI 10 · net10.0-android · net10.0-ios · 
C# 13 · CommunityToolkit.Mvvm · Serilog · EF Core 10 · SQLite
```

This is locked at project inception. If MAUI 11 is released with features that solve a pressing problem, or if iOS or Android support requirements shift, changing the target requires amendment.

### 2. Architecture Boundaries

```
**Architecture Constraint:** Services depend only on Domain interfaces 
— never on Infra directly. Only the MAUI project references Infra 
(for DI wiring, AppDbContext, migrations).
```

This constraint is sound and is enforced by the type system (Infra internals are `internal`). It is unlikely to become wrong. But if a future feature requires tight coupling between a Service and a specialized Infra component, the constraint could become an obstacle.

### 3. ViewModel Pattern

```
**ViewModel Pattern:** ObservableProperty with source generators. 
Partial methods for change notification. Never throw exceptions from 
property setters.
```

This pattern is locked in. If testing requirements evolve or a future ViewModel becomes fundamentally unsuitable for this pattern, changing it requires amendment or deliberate rule-breaking.

### 4. UI Component Priority

```
**Non-Negotiable:** DevExpress first, always. Use stock MAUI only 
when DevExpress has no equivalent.
```

This constraint is justified by the project's commercial DevExpress license and the UI consistency it provides. But if DevExpress releases a buggy component or if stock MAUI introduces a feature that DevExpress cannot match, the constraint could become wrong.

---

## Amendment Governance (Cross-Reference)

Constitutional amendments require:

1. **Documented rationale** — Why is the old constraint wrong? What has changed? What is the cost of keeping the old constraint vs. amending it?
2. **Review and approval** — By whom? The Technical Lead? An Architecture Review Board? How long does review take?
3. **Effective date** — Does the amendment apply to old code retroactively or only to new code written after the effective date?
4. **Backwards compatibility assessment** — Does changing this constraint break anything? What migration is needed?
5. **Documentation of the amendment** — Where is the change recorded? In CHANGELOG.md? In a separate constitutional amendment log?

Without these steps, amendments either:
- Accumulate silently (rules get added, never removed)
- Drift informally (team members follow different interpretations)
- Deadlock (proposed amendments languish in review)

See **S6.1.2 — Amendment Governance** (forthcoming) for the full process.

---

## Interaction with Agent Behavior

Constitutional rigidity interacts with AI agent behavior in two ways:

### 1. Agents Follow Stale Rules Literally

Agents read CLAUDE.md at session start and generally follow the constraints mechanically. When a constraint is stale (no longer correct), agents continue to obey it, producing architecturally wrong decisions that satisfy the letter of a superseded rule.

**Example:**
- Constraint: "All endpoints validate input against schema."
- Six months later: Schema validation framework is upgraded; a new feature uses a different validation library for legitimate reasons.
- Agent behavior: Agent still requires schema validation on all endpoints, forcing the new feature to shoehorn its logic into a framework it doesn't fit.

### 2. Agents Cannot Initiate Amendments

Agents can suggest that a rule is problematic (and some skilled prompting makes them more likely to do so), but agents cannot formally propose amendments. The amendment process requires human judgment, stakeholder review, and authorization — things agents cannot do. This creates a bottleneck: agents identify that a rule is wrong, but nobody formally changes it, so future sessions repeat the same sub-optimal behavior.

---

## Countermeasures

### 1. Rationale Capture

Every constitutional constraint must include its rationale — the problem it solves and the conditions that make it relevant. Format:

```markdown
**Constraint:** [rule statement]

**Rationale:** [problem it solves] — [incident reference if applicable] 
— [conditions under which it applies]

**Amendment trigger:** If [circumstance], this constraint should be re-evaluated.
```

### 2. Periodic Audits

At least quarterly, review CLAUDE.md and `.claude/rules/` to:
- Identify rules with no explicit rationale
- Check whether any rule's rationale has become false
- Consolidate redundant rules
- Remove rules that have been superseded by other mechanisms

### 3. Explicit Deprecation

Instead of removing a constraint abruptly, mark it as deprecated:

```markdown
**DEPRECATED (as of 2026-05-15):** [constraint] — [reason]
**Replacement:** [new approach, if applicable]
**Removal date:** [date, e.g., 3 months hence]
```

This gives agents and humans time to adjust.

### 4. Exception Registry

Maintain a document of approved exceptions to constitutional constraints. This serves multiple purposes:

- **Transparency:** Exceptions are documented, not ad-hoc.
- **Pattern detection:** If exceptions accumulate, the constraint is probably wrong.
- **Audit trail:** Future maintainers can understand why certain code does not follow the rules.

Format:
```
## Exception Registry

| Date | Constraint | Reason | Expires |
|------|-----------|--------|---------|
| 2026-04-15 | DevExpress-first UI | Stock MAUI bug XYZ only, workaround | 2026-06-15 |
| 2026-04-20 | CancellationToken on Services | Async fire-and-forget on startup only | indefinite |
```

### 5. Amendment Process (Formal)

Establish a clear, documented process for proposing, reviewing, and implementing constitutional amendments. See S6.1.2 for detailed patterns.

---

## Sources

- [Constitutional Rigidity Matters — Veto Players Approach (Tsebelis, 2021)](https://sites.lsa.umich.edu/tsebelis/wp-content/uploads/sites/246/2021/02/constitutional-rigidity-matters-a-veto-players-approach.pdf)
- [Constitutional Rigidity — Institutional Approach (Tsebelis, Cambridge Core 2025)](https://www.cambridge.org/core/books/changing-the-rules/an-institutional-approach-to-constitutional-rigidity/70F32EB1B8029DFC85AA746C50860455)
- [How Constitutions Die — Sundial, Grenade, and Hourglass Models (Albert, SSRN 2025)](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=5140635)
- [The Adaptability Paradox — Constitutional Resilience (Skowronek & Orren, Perspectives on Politics, Cambridge Core)](https://www.cambridge.org/core/journals/perspectives-on-politics/article/adaptability-paradox-constitutional-resilience-and-principles-of-good-government-in-twentyfirstcentury-america/FB59BB14C4AD5883AAD84B986D7B8A9F)
- [SDD Philosophy & Principles — Constitutional Foundation (Spec-Kit / Mintlify)](https://www.mintlify.com/github/spec-kit/concepts/philosophy)
- [Spec-Driven Development: A Technical Deep Dive (Rushi, 2026)](http://www.rushis.com/spec-driven-development-sdd-a-technical-deep-dive-into-the-methodologies-reshaping-ai-assisted-engineering/)
- [Self-Improving Instructions in CLAUDE.md (Claude Code Issue #23075, GitHub 2026)](https://github.com/anthropics/claude-code/issues/23075)
- [CLAUDE.md Instructions Not Reliably Followed (Claude Code Issue #18660, GitHub 2026)](https://github.com/anthropics/claude-code/issues/18660)
- [Case Study: Governing Stateless Sessions at Scale with CLAUDE.md + MEMORY.md (Claude Code Issue #29990, GitHub 2026)](https://github.com/anthropics/claude-code/issues/29990)
- [CLAUDE.md and AGENTS.md, In Depth: From Basics to Counterintuitive Patterns (Redreamality, 2026)](https://redreamality.com/blog/claude-md-agents-md-deep-dive/)
- [Operating a Meta-Repo (The Dev Newsletter, 2026)](https://devnewsletter.com/p/operating-a-meta-repo/)
- [Enhancement: Proactive Tool Discovery and Stricter Rule Adherence (Claude Code Issue #37066, GitHub 2026)](https://github.com/anthropics/claude-code/issues/37066)
- [Effective Context Engineering for AI Agents (Anthropic, 2026)](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)
- [How Claude Remembers Your Project (Claude Code Docs, Anthropic)](https://docs.anthropic.com/en/docs/claude-code/claude-md)
