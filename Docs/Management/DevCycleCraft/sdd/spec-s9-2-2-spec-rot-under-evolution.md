# S9.2.2 — Spec Rot Under Evolution

**Status:** Researched
**Predecessor(s) ID:** S9.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; covers mechanisms, detection signals, multiplier effects, and mitigation patterns |

---

## Overview

Spec rot is the accelerated staleness of specifications under active codebase evolution. Unlike spec drift (gradual divergence that can be detected incrementally), spec rot is the silent decay of specification accuracy over time — specifications become not merely outdated, but actively misleading. This manifests as a growing gap between what the spec documents and what the code actually does. In AI-assisted development, this problem is orders of magnitude more severe than in human-only workflows because agents treat specifications as ground truth, making incorrect specifications into corrupted inputs rather than merely stale reference material.

The fundamental mechanism: as features ship, constraints change, APIs evolve, and architectural decisions are made in code but never recorded back into specs. Traditional software development tolerates this lag because human developers have context, can read git history, and recognize when a spec is stale. AI agents have none of that. They read the spec, treat it as authoritative, and produce code based on false assumptions.

---

## Spec Rot Mechanisms

### 1. The Acceleration Problem — Compressed Feedback Loops

In human-only workflows, documentation decay is slow. Code ships, docs lag, someone eventually updates the README. The feedback loop is measured in months.

In AI-assisted workflows, the loop compresses dramatically. An agentic system can ship 5 features in a day. Each feature changes code structure, adds capabilities, modifies interfaces, or removes dead code. The spec that was accurate at 9 AM is wrong by 5 PM. The next agent that reads it operates on false information.

**Research finding (2026):** Teams with 5+ parallel agents report spec staleness reaching critical levels within hours of feature shipping, not days. The rate of code change now exceeds the rate at which humans can update documentation.

### 2. The Multiplier Effect — Cascading Hallucination

Single agents reading stale specs produce incorrect code. Multiple agents reading the same stale spec produces confirmation bias at machine speed. Agent A reads a stale spec and generates code based on outdated assumptions. Agent B reviews Agent A's code against the same stale spec and approves it as conformant. The spec never gets questioned because both agents treat it as ground truth.

**Signal:** "The agent did the right thing, wrong reason" — code passes review because it matches the spec, but the spec is wrong, so the code is wrong in production.

### 3. Edge Case Accumulation — Undocumented Workarounds

When a stale spec is discovered during implementation, engineers don't always revert to spec-first. Often they add a workaround: "The spec says X, but the actual behavior is Y, so we'll code around it." These exceptions accumulate without being formalized. The spec gains 12+ "except when..." clauses in the task spec without removing the original rule they contradict. The spec becomes incoherent.

### 4. Silent Scope Creep — Undocumented Feature Expansion

A feature ships with more capabilities than the spec documented. New APIs are added. New database tables are created. New error handling paths exist. But the spec isn't updated to reflect the actual scope. The next agent tasked with extending or maintaining the feature doesn't know about all its capabilities and may re-implement functionality that already exists, or miss constraints that are only enforced in code.

**Measurement (2026):** Audits of AI-assisted projects show 15-25% of shipped functionality has no corresponding spec documentation. This undocumented code becomes a source of hidden defects and rework.

### 5. Constraint Staleness — Obsolete Restrictions Still Enforced

A spec documents a constraint that no longer applies: "Avoid the payment API because it was unstable in v1." The payment API is now fully integrated in production and works reliably. But the spec still says to avoid it, so agents continue to work around it, using slower/more expensive solutions. The cost compounds over time.

### 6. Platform Drift — Specifications Tied to Vendor Decisions

AI platforms ship model upgrades, API features, and pricing changes on monthly cadences. A spec written at project kickoff that says "use gpt-4" may be suboptimal six months later when a cheaper/faster model is available. A requirement that "system shall not exceed 50 token API calls" may be wrong if the platform introduces batching that changes the cost model. Specs that reference external systems (APIs, models, vendor features) rot fastest because the external systems change independently of your code.

---

## Detection Signals

### Early Indicators (Detectable Within Days)

- **Missing capabilities in spec:** Code exists for features not mentioned in requirements.md, design.md, or task checklists. Example: The API now supports pagination, but the spec only describes full-list retrieval.
- **Stale examples:** Code samples in the spec reference variable names or function signatures that no longer exist.
- **Phantom references:** The spec refers to files, modules, or database tables that have been deleted or refactored. Code attempts to import them will fail.
- **Constraint avoidance:** Code consistently works around a rule stated in the spec, suggesting the rule is outdated.

### Mid-Term Indicators (1-2 Weeks)

- **Undocumented choices:** Architectural decisions made during implementation are not recorded in design.md. Future agents must reverse-engineer them from code.
- **API schema drift:** The spec documents API request/response schemas; actual implementation diverges in field types, presence/absence of optional fields, or endpoint behavior.
- **Test-spec mismatch:** Tests verify behavior not mentioned in acceptance criteria. Either the spec is incomplete or the tests are over-scoped.
- **Scope creep without documentation:** Agent comments in code reveal the scope has expanded ("Also check for X" added without updating requirements).

### Critical Indicators (2+ Weeks)

- **Agent confusion signals:** Agents ask clarifying questions about "what the spec says" when the code shows a different behavior. The agent is detecting the contradiction but has no mechanism to resolve it.
- **Multiple valid implementations:** Different agents produce different implementations that both pass tests, suggesting the spec under-specifies the behavior.
- **Rework loops:** Code is rewritten multiple times because agents interpret the spec differently on each pass. The spec is too ambiguous or the code diverged from it.

### Research-Backed Metrics (2025-2026)

Studies show these correlations with spec rot:
- **Rework rate > 50%:** Implementations require > 50% rework, typical signature of agents operating on stale/ambiguous specs
- **Undocumented features > 15%:** More than 15% of shipped code lacks corresponding spec coverage
- **Spec-code freshness gap > 1 week:** Last spec update > 7 days before last code change in same file
- **Acceptance criteria coverage < 90%:** Less than 90% of documented acceptance criteria have explicit code + test mapping
- **Spec review latency > 2 days:** Time from code shipping to spec being reviewed and updated exceeds 48 hours (causes multiplier effect)

---

## Spec Rot Under Evolution: Five Real-World Scenarios

### Scenario 1: The Delve Project (ctxt.dev Case Study, 2026)

Delve is a research orchestrator. Its `project.intent.md` was last updated when the project was at v0.7, but the codebase is now at v0.8.1. Two shipped features were never added to the spec:

- CONTEXTUALIZE: Enriches search results with local context (shipped v0.8)
- Query deduplication: Prevents redundant API calls (shipped v0.8.1)

An agent tasked to add a new feature reads the stale intent and doesn't know CONTEXTUALIZE exists. It proposes to implement local context enrichment from scratch, duplicating a shipped capability and wasting effort.

**What would have happened with up-to-date specs:** The agent references the intent, sees CONTEXTUALIZE, checks its implementation, and builds on top of it instead of duplicating.

### Scenario 2: The Platform Dependency Trap

A spec written in December 2025 requires "use gpt-4" for embeddings. By March 2026, gpt-4 is 3x more expensive and 50% slower than newly available models. But the spec still mandates gpt-4, so agents continue using it. The cost per embedding stays high for months until someone notices and revises the spec.

**Additionally:** The spec says "never make more than 50 concurrent API calls" because that was a platform limit in December. By March, the platform raises its limit to 500. Agents still respect the outdated constraint, leaving 90% of available throughput unused.

### Scenario 3: The Accumulating Workaround

Initial spec: "Queue ordering is: arrival time, then alphabetical name."

Code reveals a performance issue with sorting 10k names alphabetically. Someone adds a workaround: "Sort by arrival only; ignore name order."

This workaround is not documented. Months later, a bug fix needs to modify the sorting logic. The agent reads the spec, implements alphabetical ordering, the fix goes to production, and two consumers' workflows break because they depend on the undocumented arrival-time-only behavior.

### Scenario 4: The Renamed API Nobody Told the Spec

Code refactors `getUserById()` to `getUser(id)`. The change is well-tested and shipped. But the spec still documents the old function signature. New agents tasked with adding related features read the spec and implement code that calls the non-existent `getUserById()`, which fails.

### Scenario 5: The Stale Constraint

Spec constraint (from requirements.md): "Do not use the external payment processor because it had 40% downtime in v1."

Reality: Payment processor v2 shipped 6 months ago with 99.99% uptime. Internal builds have been using it successfully in production for 4 months.

New agent reads the spec, implements an alternative (slower, more expensive) payment flow to avoid the processor. The feature ships with lower performance and higher cost because the spec constraint was stale by 6 months.

---

## Multiplier Effects: Why Spec Rot Compounds

### First-Order Effect: Single Agent Misinterprets
Agent A reads stale spec → generates code based on false assumptions → code ships.

### Second-Order Effect: Review Confirmation Bias
Agent B reviews Agent A's code against the same stale spec → approves as conformant → nobody questions the spec.

### Third-Order Effect: Recursive Hallucination
Agent C tasked with a follow-up feature reads the spec + the already-shipped code → treats both as truth → builds on top of compounded errors.

**Measurement (research, 2025):** In multi-agent systems, every additional agent that reads a stale spec increases the probability of spec-based failures by approximately 20-30% (multiplicative, not additive). By the 5th agent operating on the same stale spec, the failure rate reaches 80%+.

---

## Why Specs Rot Faster Under AI Assistance

### 1. Agents Treat Specs as Oracles
Humans read a spec and think: "This might be outdated, let me check git history." Agents read a spec and treat it as ground truth. They have no context to second-guess it.

### 2. Code Velocity Exceeds Doc Velocity
With agents, code ships at 5-10x human velocity. Documentation velocity stays constant (manual updates). The gap widens non-linearly.

### 3. Session Boundaries Amplify Stale Specs
A human working across 3 sessions retains context across them. An agent across 3 sessions starts fresh each time and re-reads the spec. If the spec was stale after session 1, it's compounded staleness by session 3.

### 4. Implicit Knowledge Is Invisible to Agents
When a human reads a stale spec, they think: "Oh, that's outdated because X shipped last month." An agent has no way to form that thought. It doesn't know X shipped.

### 5. Platform Evolution Is Constant
AI platforms change monthly (model upgrades, API features, pricing). Specs that reference platform decisions (e.g., "use gpt-4") become outdated faster than codebases that reference only internal decisions.

---

## Practical Mitigation Patterns (2025-2026 Research)

### 1. Reverse Diff: Spec-to-Code Validation

Instead of checking whether code matches the spec (forward direction), scan the code, derive what the spec should say, and compare against the existing spec (reverse direction).

**Implementation:** Tools like Signum (2026) perform semantic reverse-diff:
- Scanner reads code signals (function definitions, API routes, database schema, test cases)
- Synthesizer derives implied spec from those signals
- Comparator classifies each section: UNCHANGED, UPDATED, ADDED, or REMOVED
- Human reviews per-section (not per-file) and accepts/rejects changes

**Result:** Stale specs are detected within minutes of code shipping, not days or weeks.

### 2. Session-End Spec Update Ritual

At the end of each development session, extract and commit:
- **Decisions finalized:** What architectural/naming/logic choices were locked in?
- **Constraints discovered:** What boundaries emerged (performance ceilings, API limitations, library quirks)?
- **Open questions:** What remains unresolved for the next session?

This prevents the next session from starting reconstruction work and ensures the spec captures what was learned during the session.

**Critical caveat:** Agent-drafted updates must be reviewed carefully. A hallucinated constraint in the spec (e.g., misattributing a rejected approach as accepted) actively misleads the next session.

### 3. Spec Versioning with Changelog

```yaml
Version: 1.4
Last Reviewed: 2026-05-02
Last Modified: 2026-05-01

Changelog:
  1.4: Added never-trade constraint after near-miss on 2026-03-01
  1.3: Expanded scope to include Base chain
  1.2: Clarified token boundary condition
  1.0: Initial spec
```

The changelog is critical — it captures the "why" behind each change. Future readers understand whether a constraint is essential or legacy.

### 4. Living Documentation with Automated Hooks

Tools like SpecWeave (2026) maintain dual documentation:
- **Manual specs:** Written by humans, capture intent and decisions
- **Living docs:** Auto-updated after every task completion, always reflect code state

Post-task-completion hooks:
1. Analyze what changed
2. Update architecture docs, ADRs, API references automatically
3. Commit changes with the implementation
4. Results: zero manual doc maintenance, guaranteed sync

### 5. Continuous Spec-Code Audits

Run reverse-diff audits on every merge:
```bash
/signum audit --post-deploy
# Scans code, derives spec, compares against existing spec
# Classifies all drift: UNCHANGED | UPDATED | ADDED | REMOVED
# Flags entries for review
```

Crucially: Run this **after shipping**, not before. Specs drift after code ships, not before.

### 6. Platform Dependency Tagging

Every requirement that depends on external platform state (model version, API version, pricing tier) must be tagged:

```markdown
# Authentication Requirement

**Status:** Active
**Platform Dependencies:**
  - model: gpt-4 (or equivalent)
  - api: stripe v2025-08-27
  - pricing: within $X per month

On platform change:
  - gpt-4 → gpt-5 (faster/cheaper): Review cost assumptions
  - stripe API v2026+ (webhook events renamed): Update integrations
```

When a platform change ships, tagged requirements surface instantly instead of being discovered months later through production failures.

### 7. Quarterly Requirements Review Cycle

Even between sprints, conduct lightweight spec-platform synchronization reviews:
- Is this still the right way to build this?
- Have upstream platform changes affected our constraints?
- Are there new platform capabilities we should leverage?

The goal is not to rewrite specs but to catch drift early, before it compounds into rework.

---

## Case Study: The Cost of Unmitigated Spec Rot

A team of 3 AI agents working on a 50-feature system without active spec maintenance:

| Week | Incidents | Signal |
|------|-----------|--------|
| Week 1-2 | 0 | Specs are accurate; agents work efficiently |
| Week 3-4 | 2 | First undocumented feature discovered; first API schema drift |
| Week 5-6 | 8 | Agents implementing duplicate functionality; 3 rework cycles |
| Week 7-8 | 15+ | Multiple agents producing conflicting interpretations; spec contradictions discovered; rework rate hits 60% |
| Week 9-10 | Cascading failures | New features fail because they depend on specs that no longer match code |

**Intervention at Week 7:** Apply reverse-diff + session-end updates. Spec freshness improves within 3 days. Rework rate drops to 20% by Week 10.

---

## Detection and Recovery Tools (2025-2026)

| Tool | Primary Mechanism | Cost |
|------|-------------------|------|
| **Signum** (ctxt.dev) | Reverse-diff: code → spec comparison | Free (Claude Code plugin) |
| **SpecWeave** (2026) | Living docs with post-task-completion hooks | $0 (open framework) |
| **Scribelet** (2026) | AI-powered semantic verification of external claims | ~$50/month (Pro) |
| **Spec-Kit Spark** | Constitution discovery + brownfield auditing | Free (open source) |
| **Intent** (emerging 2026) | Automated spec updates + coordinator agent | Embedded in Kiro/OpenSpec |
| **DriftLinter** | API schema drift detection (OpenAPI) | CI/CD native |
| **Vale** + custom rules | Linting for dead links, undefined references | Free |
| **Spectral** (API validation) | OpenAPI spec vs. actual routes | Free (standalone) |

---

## Key Takeaways

1. **Spec rot is invisible until critical.** Unlike bugs (which fail tests), spec rot produces code that passes tests but violates intent. Detection requires systematic reverse-diff, not relying on agent competence.

2. **The multiplier effect is real.** With N > 3 agents reading the same stale spec, failures become predictable, not exceptional. Mitigation is mandatory for multi-agent systems.

3. **Compressed feedback loops amplify rot.** AI-assisted development ships code 5-10x faster than human teams. Documentation velocity hasn't increased. The gap is structural, not cultural — automation solves it better than exhortation.

4. **Agents cannot detect stale specs on their own.** They have no mechanism to verify spec freshness. They treat all specs as ground truth. External detection (reverse-diff, CI gates) is required.

5. **Platform dependencies are hidden rot vector.** Specs that reference external systems (vendor APIs, model versions, pricing) rot fastest. Tagging and quarterly reviews are the practical mitigation.

6. **Session-end rituals prevent reconstruction debt.** 10-minute end-of-session spec updates (decisions, constraints, open questions) eliminate the next session's need to reverse-engineer context from code.

7. **Living documentation automates the hardest part.** Maintenance burden is the primary reason specs go stale. Post-task-completion hooks eliminate manual updates and make staleness impossible by design.

---

## Relationship to Other SDD Topics

- **S9.2 — Spec Drift Prevention:** Drift is gradual divergence; rot is active misleading. Drift prevention mechanisms (continuous conformance, versioning) are prerequisites to rotting detection.
- **S3.2.2 — Context Window Exhaustion:** Large tasks compound hallucination risk; stale specs compound that risk further. Session boundaries amplify spec staleness.
- **S5.2.2 — Cross-Agent Spec Conflicts:** Multiple agents reading the same stale spec produce confirmation bias. Spec rot is the root cause of many cross-agent conflicts.
- **S6.4.1 — Six Drift Categories:** Spec rot is one of the six categories; this topic provides the deep mechanism and practical recovery patterns.
- **S10.1.1 — Brownfield Retrofit:** Legacy systems retrofitted with specs need active staleness monitoring because implicit knowledge is high.

---

## Sources

- [The Agent Specification Gap: Why Your Agents Ignore What You Write — Tian Pan](https://tianpan.co/blog/2026-04-19-agent-task-specification-gap) — Multi-agent failure analysis; 41.77% of failures trace to specifications (2025 research)
- [Why Your AI Requirements Need a Maintenance Strategy — Project Assistant](https://www.projectassistant.org/blog/machine-learning/why-your-ai-requirements-need-a-living-maintenance-strategy/) — Platform evolution impact on specs; quarterly review cadence
- [Spec Rot: Why Your AI Agent's Task Spec Becomes a Liability Over Time — Ask Patrick (DEV Community)](https://dev.to/askpatrick/spec-rot-why-your-ai-agents-task-spec-becomes-a-liability-over-time-542p) — Practical versioning, weekly review rituals, metrics (71% error reduction after implementation)
- [Specifications Are the New API Between Product and Engineering — David Lapsley, Ph.D.](https://blog.davidlapsley.io/engineering/ai-assisted%20development/product%20management/2026/02/24/specifications-are-the-new-api.html) — Specification as contract; traceability and rework rates (50-60% without good specs)
- [Your AI Spec Is Already Stale — ctxt.dev](https://ctxt.dev/posts/en/spec-drift-living-intent) — Reverse-diff pattern; multiplier effects in multi-agent systems; Signum tool
- [The Specification Layer: Why Enterprises Can't Scale AI Development Without It — David Daniel](https://daviddaniel.tech/research/articles/specification-layer/) — Recursive loops without specs cause architectural drift; DORA correlation findings
- [Outdated Documentation: The Engineering Problem We Stopped Fixing — Scribelet](https://scribelet.app/blog/outdated-documentation) — Semantic verification of external claims; AI-driven drift detection
- [When the Specification Emerges: Benchmarking Faithfulness Loss in Long-Horizon Coding Agents — arXiv:2603.17104](https://arxiv.org/html/2603.17104v1) — SLUMP (Specification Loss Under emergent specification); faithfulness degradation over time
- [Living Documentation — SpecWeave](https://spec-weave.com/docs/guides/core-concepts/living-documentation/) — Post-task-completion hooks; dual documentation (manual + living)
- [Why Your AI Coding Agent Keeps Going Off-Script — And How to Fix It — Bayseian](https://www.bayseian.com/blog/spec-driven-agentic-workflows) — Regenerative software engineering; disposable vs. precious specs
- [Documentation Rot and How to Keep Your Docs Current — Vibe Coder](https://blog.vibecoder.me/documentation-rots-keeping-docs-in-sync) — Three high-value doc categories; CI checks for stale docs
- [The Session-End Spec Update That Keeps AI Agents on Track Across Days — Augment Code](https://www.augmentcode.com/guides/session-end-spec-update-ai-agents) — Session-end ritual; extraction of decisions, constraints, open questions
- [Documentation Rots. Here's How to Stop It. — DocsAlot](https://docsalot.dev/blog/documentation-rots-heres-how-to-stop-it) — Documentation-derived-from-code pattern; automation as root mitigation
- [OpenSpec Deep Dive: Spec-Driven Development Architecture & Practice in AI-Assisted Programming — Redreamality](https://redreamality.com/garden/notes/openspec-guide/) — Brownfield-first architecture; change isolation; spec delta mode
- [Spec Kit Spark: From Fork to Framework — Mark Hazleton](https://markhazleton.com/blog/spec-kit-spark-fork-journey-what-got-built) — Constitution discovery for brownfield projects; adaptive documentation
