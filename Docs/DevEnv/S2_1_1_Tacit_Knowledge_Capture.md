# S2.1.1 — Tacit Knowledge Capture

**Status:** Researched
**Predecessor(s) ID:** S2.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written by research agent |

---

## Overview

The gap between what expert practitioners know implicitly and what an AI agent needs explicitly is the single largest source of business-logic errors in deployed AI systems. Business rules live in the judgment, heuristics, and contextual intuition of experienced people — not in documentation. When those rules remain tacit (unspoken, unwritten, known only by doing), they cannot be captured in specifications. When specifications are incomplete, AI agents invent plausible but wrong behavior, generating code that passes tests but fails in production.

This document addresses the core tension: **What is the cost of articulating tacit knowledge, and when does that cost exceed the cost of leaving rules implicit?**

---

## The Tacit Knowledge Problem

### What Is Tacit Knowledge?

Tacit knowledge is expertise that practitioners apply automatically but struggle to articulate on demand. A domain expert asked "What validation rules apply to a refund request?" may describe the headline rule. But when watching that expert handle refund requests in real time, they apply five additional constraints the expert themselves cannot name without concrete examples. The gap between the articulated rule and the applied rule is tacit knowledge.

Tacit knowledge exists across all domains where expertise compounds:
- **Business rules:** Refund policies with exceptions, tier-assignment logic with conditions, approval routing with override scenarios
- **Architectural heuristics:** "Always validate inputs before processing," "Prefer dependency injection for external services," "Cache queries that run >100x/day"
- **Quality standards:** What makes code reviewable, what signals a security risk, what constitutes acceptable test coverage
- **Domain context:** Why certain decisions were made, what constraints are buried in prior decisions, what the system should never do

### The Cost of Incompleteness

Research on AI-assisted development (2025–2026) consistently shows that incomplete specifications drive hallucination. When an AI agent does not know the full rule, it inverts the problem: instead of asking "What does the rule say?", it answers "What would a reasonable rule be?" That reasonable-sounding rule is often close but wrong in exactly the ways that matter most.

A concrete example from industry practice: A payment processor's refund rules were specified as "refunds are allowed within 30 days of purchase." The rule was incomplete. The specification did not mention:
- Enterprise customers with active support contracts get 90 days
- Custom-built integrations get 0 days after acceptance testing
- Bulk refunds from the same card require escalation approval
- Refunds on high-value items follow a different approval path

When the implementation was handed to an AI agent, it generated code that handled the 30-day case correctly but auto-approved all enterprise refunds and all bulk refunds. The result: $300K in incorrect payouts before the bug was caught in production.

The explicit cost is the time to discover and fix the bug (2–3 weeks discovery, 4–8 hours engineering time per incident). The implicit cost is the refactoring overhead: code that "works" for the happy path but violates the implicit rules is harder to maintain, and fixing it retroactively costs 3–5 times the original development time. Trust in the AI tool declines. Code review overhead increases 40–60%.

### Why Tacit Knowledge Resists Articulation

Practitioners cannot readily articulate tacit knowledge because:

1. **It is learned by doing.** The rule was internalized through years of handling exceptions, not through formal documentation. The expert does not have an explicit articulation ready to speak.

2. **It is context-sensitive.** Rules that appear universal in isolation are actually context-dependent: "refunds require approval" — except when they don't, under conditions the expert recognizes on sight. The exception is sensed, not calculated.

3. **It is partially implicit even to the expert.** A master chess player recognizes a strong position instantly but cannot fully articulate why. The pattern was learned, not reasoned from first principles. Some of what the expert "knows" is pattern-matching below conscious articulation.

4. **It compounds over time.** Each decision builds on prior decisions; the expert carries the full history. A new hire never sees the buried constraints that shaped earlier choices. The accumulated weight is invisible.

---

## The Cost-Benefit Tension

Articulating tacit knowledge is expensive. Keeping rules implicit is risky. The tradeoff is not straightforward.

### The Cost of Extraction

Research on knowledge engineering (2025–2026) identifies three cost centers in tacit knowledge extraction:

#### 1. Direct Extraction Cost

Interviewing domain experts to surface implicit rules is labor-intensive and lossy. Traditional approaches (asking "What are the rules?") surface only the rules practitioners can consciously articulate — which excludes the tacit portion. Better approaches surface tacit knowledge through:

- **Failure-mode analysis:** Review recent agent errors with the expert; ask "What would be correct here, and why?" The explanation reveals implicit standards.
- **Scenario annotation:** Present the expert with agent outputs (mixed good and bad); ask them to annotate each. Disagreements across experts reveal where the tacit knowledge is ambiguous or contested.
- **Example-based elicitation:** Watch the expert handle tough cases; extract the decision trees implied by the case-by-case choices.

A domain expert's time costs $150–$250/hour. Extracting and validating tacit knowledge from a domain expert requires 40–200 hours per functional area, depending on complexity and how well decisions are already documented. That is $6K–$50K per area.

#### 2. Encoding Cost

Once extracted, tacit knowledge must be encoded in a form an AI agent can consume. This requires translation into one of four patterns:
- **Decision tables** (JSON/YAML/CSV): $500–$2K per table, ~20–50 tables per organization → ~$10K–$100K total
- **Rule engines** (Drools, OPA, Cedar): $10K–$50K setup + $5K–$20K per rule set
- **Policy-as-code** (OPA Rego, Cedar): $20K–$100K setup
- **Constraint solvers** (OR-Tools, Z3): $50K–$200K+

Total encoding cost: $15K–$300K, depending on the scale and formalism required.

#### 3. Maintenance Cost

Tacit knowledge is living knowledge. It evolves as the business evolves, exceptions accumulate, and new patterns emerge. Once encoded, the rules must be kept in sync with reality — or they become a source of new hallucinations. Maintenance requires:
- Regular review cycles (quarterly or event-driven)
- Change management (who approves rule updates? how fast can they deploy?)
- Versioning (how do you roll back a bad rule update?)

Ongoing maintenance costs 10–30% annually of the initial encoding investment.

---

## When Extraction Is Worth the Cost

Research and practitioner experience (2025–2026) suggest extraction is economically justified when:

| Condition | Signal | Justification |
|-----------|--------|---------------|
| **High volume** | Decision is made >100 times/day across the org | Cost of a wrong decision is compounded across volume. Cost to extract rule < cost of one month of wrong decisions |
| **High cost of error** | A wrong decision costs $10K+ in direct loss or rework | Even a 1% error rate in a manually-coded rule becomes $100K+ annually; extraction ROI is clear in 6–12 months |
| **Expert departure risk** | Key person carries the rule; retirement or turnover is imminent | Knowledge walks out the door; extraction is a knowledge preservation strategy, not an optimization |
| **Regulatory requirement** | Rule is part of a compliance obligation; decisions must be auditable and explainable | Cannot delegate to implicit knowledge; formality is mandatory |
| **Repeated hallucination** | AI agents consistently make the same wrong assumption | Extraction directly addresses the known gap |
| **Team scale** | 5+ people need to apply the rule consistently | Tacit knowledge creates inconsistency; formalization improves consistency |

### Extraction Is Premature When:

| Condition | Signal | Consequence |
|-----------|--------|------------|
| **Rare decision** | Rule is applied <10 times per year | Extraction cost exceeds the value of preventing rare errors |
| **Rapidly evolving** | Rule changes monthly or more | Maintenance cost dominates; rule formalization becomes a bottleneck |
| **Highly context-sensitive** | Rule is "it depends" 80% of the time | Encoding fails; the rule becomes a set of exceptions to exceptions, which obscures intent more than it clarifies |
| **Prototype/exploratory phase** | System is still discovering what it needs to do | Premature formalization locks in assumptions that will change; extraction happens after the domain stabilizes |
| **Expert available and trusted** | Domain expert is on-call and empowered to override defaults | Implicit rule + human override is acceptable; extraction overhead is unnecessary |

---

## Extraction Strategies That Work

Research on tacit knowledge elicitation (2025–2026) has identified techniques that surface more of what experts actually know (not just what they say they know):

### 1. Failure-Mode Analysis (Most Effective)

**Protocol:** Review recent agent failures with the domain expert. For each failure, ask:
- "What would be a correct output here?"
- "Why is that correct and the agent's output is wrong?"
- "What did you notice that the agent missed?"

**Why it works:** Experts are better at explaining what is wrong than articulating rules in the abstract. Each failure surfaces an implicit constraint or priority.

**Output:** Structured list of "when [condition], the agent must [action] because [reason]"

**Timeframe:** 2–4 hours per functional area; yields 60–80% of tacit knowledge in that area.

### 2. Scenario Annotation

**Protocol:** Generate 10–20 decision scenarios (mix of obvious cases, edge cases, and ambiguous ones). Ask the expert to:
- Mark each scenario as "good," "acceptable," or "wrong"
- Explain the reasoning for each mark
- Flag scenarios where they are uncertain

**Why it works:** Disagreements between experts reveal where the tacit knowledge is ambiguous or contested. Annotations reveal evaluation criteria the expert applies non-consciously.

**Output:** Rubric of decision criteria + flagged areas requiring team alignment

**Timeframe:** 4–8 hours; yields clarity on what is actually contested vs. what is consensus.

### 3. LLM-Assisted Extraction with Validation

**Protocol:** Use a language model to propose structured rules from unstructured policy text (e.g., runbooks, prior documentation, support tickets). Then have a domain expert validate and refine the output.

**Why it works:** LLMs are good at pattern-matching and propose rule structures; experts are good at catching incompleteness and exceptions. The hybrid approach scales better than expert-only extraction.

**Output:** Drafted decision tables or rule engines; expert refinement identifies gaps.

**Timeframe:** 6–16 hours (LLM draft + expert review) vs. 40–200 hours (expert-only extraction).

**Caution:** LLMs achieve good policy-to-code conversion when logic is simple, but struggle with complex multi-condition rules. Georgetown Beeck Center research shows LLMs draft at 70–80% accuracy for straightforward rules but only 40–50% for rules with 3+ nested conditions. Always validate before production.

### 4. Knowledge Activation Pipeline (Compression & Injection)

**Protocol:** Formalize the knowledge extraction workflow as three stages:

1. **Codification:** Extract tacit knowledge from experts into structured documents (markdown skills, decision tables)
2. **Compression:** Distill the documented rules into token-efficient units that fit in agent context windows
3. **Injection:** Deliver the compressed knowledge at the point of need (in prompts, as agent tools, as validation middleware)

**Research finding:** A runbook consuming 2,000 tokens can be compressed into an agent skill using ~300 tokens while maintaining task completion capability — a 6–7× density improvement.

**Output:** Modular rule artifacts that agents consume efficiently.

---

## The Specification Paradox

SDD assumes that specifications are the primary artifact. But specifications themselves often encode only articulated knowledge, not tacit knowledge. A team that writes a detailed specification for a refund process may still miss the context-dependent exceptions because those exceptions live in expert judgment, not in documentation.

The result: the spec is complete-looking but incomplete in practice. The AI agent implements the spec correctly and still generates wrong behavior because the spec itself is tacitly incomplete.

**Ways to Detect Tacit Gaps in Specs:**

1. **Run failure-mode analysis against the spec.** Give the spec to the AI agent and test on real cases. Every failure reveals a tacit gap.
2. **Have domain experts review the spec for "it depends" statements.** Any statement with implicit exceptions should be unpacked into explicit decision tables.
3. **Solicit edge-case challenges.** Ask "What if [edge case]?" for each high-risk decision. Spec gaps surface as "the spec doesn't say."

---

## Strategies for Incomplete Specs

Since complete specs are unattainable and extraction is expensive, teams have adopted hybrid strategies:

### 1. Progressive Extraction (Failure-Driven)

Write specs for what you can articulate clearly. Deploy with those specs. When AI-generated code fails in ways that reveal tacit knowledge gaps, extract the missing rule and update the spec. This is slower but cheaper than up-front extraction, because you only extract rules that actually matter in practice.

**Cost:** High review overhead initially; drops as the spec converges to reality.

**Timeline:** 2–4 quarters to stable state.

### 2. Hybrid Encoding (Rules + Human Judgment)

Encode the rules that are high-volume or high-cost. Leave low-frequency decisions to human override or expert escalation. This is not a technology failure; it is a structural choice: automate what you can afford to get wrong 1% of the time, escalate what you cannot.

**Example:** Auto-approve refunds <$100. Escalate >$100 for human decision. Encode the business rules for bulk refunds (detected automatically) to require escalation even if <$100.

**Cost:** Lower extraction cost (encode only the volume cases); ongoing human labor for escalation.

### 3. Modular Specs with Explicit Gaps

Write specs that explicitly mark areas of tacit knowledge. Use annotations like:

```markdown
**Rule:** Refunds are approved within 30 days of purchase.

**Known Gaps (Tacit):**
- Enterprise contracts may extend this window; ask domain expert Sarah
- Bulk refunds (>5 same-source) require escalation; rules not yet extracted
- High-value items (>$10K) have separate approval path; contact Finance
```

This prevents the spec from implying false completeness. Agents see the gaps and can ask for clarification or escalate uncertain cases.

---

## Sources

- [Encoding Tacit Knowledge into Agent Improvement Loops — AgentPatterns.ai](http://agentpatterns.ai/workflows/encoding-tacit-knowledge/)
- [From Documentation to Executable Context: The Encoding Process — NimbleBrain](https://www.nimblebrain.ai/method/encoding-knowledge--from-docs-to-executable-context/)
- [Business-as-Code: The Complete Guide to Structuring Your Organization for AI — NimbleBrain](https://nimblebrain.ai/guides/business-as-code-guide)
- [Explicating Tacit Regulatory Knowledge from LLMs to Auto-Formalize Requirements for Compliance Test Case Generation — arXiv:2601.09762](https://arxiv.org/abs/2601.09762)
- [Business Rules as Code: Stop AI From Making Things Up — AI Native Builders](https://www.ainative.builders/data/business-rules-as-code)
- [GitHub Spec Kit: A Guide to Spec-Driven AI Development — IntuitionLabs](http://intuitionlabs.ai/articles/spec-driven-development-spec-kit)
- [Stop writing business rules and let the system learn them instead — Medium](https://medium.com/@f.sazanavets/stop-writing-business-rules-and-let-the-system-learn-them-instead-266773131464)
- [DeepRule: An Integrated Framework for Automated Business Rule Generation — arXiv:2512.03607](https://arxiv.org/html/2512.03607v1)
- [The High Cost of Ambiguity: Why Standardized Business Requirements are a Strategic Imperative — Klariti](https://klariti.com/2026/01/26/the-high-cost-of-ambiguity-why-standardized-business-requirements-are-a-strategic-imperative/)
- [The $300K Bug That Was Never the AI's Fault — Umesh Malik](https://umesh-malik.com/blog/spec-driven-development-ai-agents-addy-osmani)
- [Specification Economy and the Role of AI Tools — The Cincinnati Exchange](https://thecincinnatiexchange.com/specification-economy-ai-work/)
- [Tacit — Organisational Cognition Infrastructure](https://tacitlabs.ai/)
- [Extract tacit team knowledge from GitHub PRs — GitHub Repository](https://github.com/BayramAnnakov/tacit)
- [Business as Rulesual: A Benchmark and Framework for Business Rule Flow Modeling with LLMs — arXiv](https://arxiv.org/html/2505.18542v3)
- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog](https://resources.github.com/increasing-collaborative-development-with-ai/)
- [Auto-generate_Business_Rules — GitHub Repository](https://github.com/paulbrowne-irl/Auto-generate_Business_Rules)
