# S1.2.1 — Level Gap: Anchored → Source

**Status:** Researched  
**Predecessor(s) ID:** S1.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Full research and content completed |

---

## Overview

The jump from **Spec-Anchored** (Level 2) to **Spec-as-Source** (Level 3) is not a simple incremental step. It represents a fundamental philosophical shift about the nature of code itself: is code a maintained artifact or a disposable output?

In Spec-Anchored systems, code and spec coexist as equally important assets. Developers maintain both, review both, and version both. In Spec-as-Source systems, code becomes a build artifact — analogous to compiled binaries — that can be regenerated, deleted, and rebuilt without loss of fidelity. The gap between these two views is profound, and it explains why Level 3 adoption remains conditional even in 2026.

---

## The Philosophical Divide

### Code as Maintained Artifact (Spec-Anchored)

In traditional development and in Spec-Anchored practice, code is a primary asset:

- Code is the lived expression of intent. It encodes the team's decisions, workarounds, and accumulated knowledge.
- Code is maintained directly. Developers refactor it, fix it, and evolve it over the lifetime of the product.
- Specs are guardrails that prevent drift, but they don't replace code ownership.
- The spec describes what should happen; the code describes what actually happens and why it matters.
- Maintenance means maintaining the codebase — improving clarity, fixing bugs, refactoring architecture.

**Cost structure:** Significant upfront cost in spec writing, ongoing cost in code maintenance.

**Risk model:** Code is the primary risk. If the code is wrong, the product fails, regardless of spec accuracy. Therefore, code review is where the most important validation happens. Specs provide context but code is the final arbiter.

### Code as Disposable Output (Spec-as-Source)

In Spec-as-Source systems, code is a projection of the spec:

- Code is generated from specs. It is not manually authored.
- Code is disposable. If a better implementation approach emerges, the spec is updated and code is regenerated from scratch.
- Maintenance means maintaining specifications. The spec is the artifact that lives for years; code generations are ephemeral.
- Refactoring is not an activity applied to code; refactoring means reconceptualizing the spec and regenerating.
- Technical debt doesn't accumulate in code; it accumulates as spec ambiguity or incompleteness. Fixing it means fixing the spec, then regenerating clean code.

**Cost structure:** Significant upfront cost in spec writing and code generation tooling. Ongoing cost is spec maintenance only, not code maintenance.

**Risk model:** Spec quality is the primary risk. If the spec is incomplete or ambiguous, generated code will be incorrect in non-obvious ways. The final validation still falls to tests and human review, but the code itself is trustworthy only insofar as the spec is trustworthy.

---

## Why the Gap Exists

The philosophical shift from Level 2 to Level 3 is not just a change in tools — it is a change in how teams organize work and where they invest human effort. Four structural differences explain why the gap is so large:

### 1. Regeneration Fidelity

**Spec-Anchored:** Code can be manually edited. A developer can fix a bug directly, without needing to update the spec. This is fast and low-friction.

**Spec-as-Source:** Code must never be manually edited. Any change requires a spec update and regeneration. The generation pipeline must be reliable enough that developers trust the regenerated code more than they trust a hand-edited patch.

**The gap:** Code generation for general application logic is not yet mature. Non-determinism remains a real problem: the same spec fed to the same code generator on different days can produce subtly different implementations. Until the "same spec always generates the same code" assumption is true, Spec-as-Source is risky for mission-critical systems.

Current evidence (2026):
- OpenAPI / Protobuf code generation is mature. It has been spec-as-source for 10+ years and is battle-tested.
- AI-assisted code generation (Tessl, Kiro advanced mode) is still "Assess" maturity (Thoughtworks, 2025). Non-determinism is documented. Generation failures require human intervention to fix the spec or the generation parameters, not to patch the code.

### 2. Debugging Complexity

**Spec-Anchored:** When code behaves incorrectly, developers read the code to understand what it does. The code is the ground truth for runtime behavior. Debugging is familiar: set breakpoints, trace execution, understand what went wrong.

**Spec-as-Source:** When generated code behaves incorrectly, the code itself tells you what was built, but not why. The spec is the source of truth for intent, and the gap between spec intent and generated behavior is the debugging target. Fixing it requires:
- Determining if the spec was correct (but ambiguously written)
- Determining if the generator misunderstood the spec
- Tracing from runtime behavior back through generated code to the spec clause that should have prevented it
- Updating the spec to be less ambiguous

This is much harder than reading code and understanding it.

**The gap:** No standard debugging workflow exists for spec-as-source systems. Kiro and Tessl are still defining how to close this gap. Until tracing from generated code back to spec intent is as intuitive as reading code, organizations resist the shift.

### 3. Escape Hatches and Drift

**Spec-Anchored:** If a developer finds a faster or cleaner implementation, they can code it directly. The spec is then updated to match. This is the path of pragmatism.

**Spec-as-Source:** There is no escape hatch. Any code change means updating the spec and regenerating. If a developer hand-edits generated code to work around a generation limitation, the next spec update wipes their change. This is by design — it forces the fix to happen at the spec level, not at the code level.

**The gap:** Teams that have experienced "escape hatch migration" — where hand-edits accumulate faster than specs are updated — know that an enforce-regeneration-only model is a cultural change, not just a technical one. Some organizations embrace it; others resist it as too rigid.

### 4. Organizational and Skill Changes

**Spec-Anchored:** The team structure doesn't change much. Developers write specs and code, just in that order. The skillset is "good developer who thinks before coding."

**Spec-as-Source:** The team structure changes. Code generation becomes a core competency. Not all developers are skilled at generation-quality spec writing. The role of "infrastructure/tools engineer who designs generators" becomes more important. The role of "application developer who refactors code" becomes less important.

**The gap:** This is cultural and organizational, not technical. Many teams are not ready to restructure or retrain around spec-as-source. Adopting it requires confidence that the business value (no manual code maintenance) outweighs the cost (retraining, tooling investment, initial friction).

---

## Current State of Practice (2025–2026)

### Spec-Anchored Is the Practical Ceiling

As of May 2026:

- **Thoughtworks Technology Radar (April 2026):** Spec-as-Source rated "Assess" — no change from Nov 2025. This signals that industry confidence in the approach has not materially advanced. Specific domains (OpenAPI SDKs, design systems, safety-critical embedded) are viable. General application development is not yet.

- **Tessl (funded by Snyk's founder in 2025):** The highest-profile spec-as-source platform for general code. It remains invite-only and early-stage. Customers report benefits in certain domains (data workflows, business logic) but acknowledge that debugging generated code is still challenging and non-determinism in generated output is an operational reality.

- **Kiro advanced mode (AWS, 2025):** Offers an opt-in spec-as-source workflow where a full task list is executed by agentic regeneration with no human edits to generated code. Early adopters report positive results in greenfield projects and exploratory work but do not yet recommend it for brownfield or long-lived production systems.

- **GitHub Spec Kit (2025):** Explicitly positioned at Spec-First and Spec-Anchored levels. The design acknowledges that spec-as-source is not yet ready for broad adoption.

- **Augment Code analysis (April 2026):** Comprehensive comparison of the three levels. Conclusion: Spec-Anchored remains the "sweet spot" for most production systems. Spec-as-Source is viable and recommended only for:
  - Multi-target SDKs (one spec, many language implementations)
  - Compliance-required systems where rebuildability is mandated
  - Domains with mature code generation (OpenAPI, Protobuf, database schemas)

### Where Spec-as-Source Works Today

The pattern is not "spec-as-source is ready" but "spec-as-source works in narrow domains":

| Domain | Maturity | Example | Since |
|--------|----------|---------|-------|
| API specifications (OpenAPI, AsyncAPI) | Production-proven | Stripe, Twilio, every major API | ~2012 |
| Data contracts (Protobuf, gRPC) | Production-proven | Google, Uber, Netflix internal | ~2008 |
| Database schemas (SQL migrations) | Production-proven | Every relational database | ~1990s |
| Design system components | Mature | Figma tokens → CSS, design tokens | ~2020 |
| Infrastructure-as-code (Terraform, Pulumi) | Mature | Cloud infrastructure generation | ~2015 |
| Web API client SDKs (OpenAPI codegen) | Mature | 40+ languages, mature ecosystem | ~2015 |

### Where Spec-as-Source Struggles

| Domain | Issue | Status |
|--------|-------|--------|
| Full-stack application code | Non-determinism, debugging, escape hatches | Early / Experimental |
| Business logic with implicit decisions | Spec completeness, tacit knowledge loss | Problematic |
| UI / UX code | Component variability, visual regression | Not ready |
| Complex architectural decisions | Spec enforcement, tradeoff encoding | Difficult |
| Multi-year maintained products | Long-term spec drift, generator updates | Risky |

---

## The Rebuild Test: A Concrete Measure of Spec Quality

One concrete way to assess whether a system is actually spec-as-source (or ready to be) is the **rebuild test**, articulated by Augment Code and others:

1. **Delete the entire codebase.** (Or src/ directory.)
2. **Open a fresh AI agent session with no prior context.**
3. **Provide only the specification files and the test suite.**
4. **Ask the agent to regenerate the codebase from the spec.**
5. **Run the tests.**

**Outcome:**
- If tests pass and the regenerated code matches production behavior: The spec is generation-grade. It encodes enough detail that a fresh agent can rebuild the system. Spec-as-Source is viable.
- If tests fail or behavior diverges: The spec has gaps. These gaps are what make spec-as-source risky. Each gap is a point where the next regeneration may make a different (incorrect) decision.

**Interpretation:** The rebuild test is not a pass/fail gate for adoption. It is a diagnostic tool that reveals where the spec is incomplete, ambiguous, or reliant on implicit decisions. Every gap discovered is a place where spec-as-source would introduce risk.

As of 2026, most production systems fail the rebuild test. The gaps are usually in three categories:
1. **Architectural decisions** (why we chose PostgreSQL over MongoDB; why we cache at this layer)
2. **Business rule tradeoffs** (why retry-safety matters more than idempotent semantics)
3. **Integration contracts** (what upstream services return; what error shapes we expect)

These are expensive to document and easy to omit. They are also exactly what make generation non-deterministic.

---

## Why Teams Stay at Spec-Anchored

The reasons organizations choose not to adopt Spec-as-Source are rational:

1. **Tooling risk:** Generators are not yet commodity. A team must either build its own (expensive) or depend on a vendor's (risky if the vendor changes direction or fails). Spec-Anchored requires only Git and a text editor.

2. **Non-determinism cost:** If the same spec generates different code on Monday than on Friday, there is no "source of truth" — only an artifact that you rebuild and pray it works. This is acceptable for prototypes; unacceptable for production.

3. **Debugging friction:** When code behaves incorrectly and is generated, the code itself is not the problem. The spec or the generator is. Debugging requires understanding the generation algorithm, not just the code. This skill is rarer and more expensive to develop.

4. **Escape hatch temptation:** If the generator produces sub-optimal code, a developer can hand-edit it. This feels pragmatic in the moment but breaks the spec-as-source invariant. Once escape hatches exist, you have a hybrid system that has the overhead of spec-as-source (all changes must go through the spec) with the flexibility of hand-coding (developers want to edit code directly). Worst of both.

5. **Cultural lock-in:** Spec-as-Source is a commitment. It requires that all team members believe that the spec is more important than the code. Some teams embrace this; many resist it as a loss of autonomy or craftsmanship.

---

## The Progression Path

Teams that want to adopt Spec-as-Source do not jump there. The natural progression is:

### Stage 1: Adopt Spec-First
Write specs before you code. Use them as AI prompts. Measure whether spec-guided generation produces better first-draft code than unstructured prompting.

**Gate:** Specs are written before coding and they materially improve AI output quality.

### Stage 2: Graduate to Spec-Anchored
Start updating specs when code changes. Add spec review to PR checklists. Use specs as AI context in follow-up sessions.

**Gate:** Specs stay current. New developers use them to onboard. AI sessions pick up context from specs without needing re-explanation.

### Stage 3: Pilot Spec-as-Source on a Single Contract
Pick a narrow contract (an API, a schema, a design system component) and make it spec-as-source. Do not attempt it for the entire codebase.

**Gate:** The regenerated artifact passes tests and behaves identically to the hand-coded version. The team reports that maintaining the spec is easier than maintaining the code.

### Stage 4: Expand Spec-as-Source Carefully
If the pilot succeeds, expand to other narrow contracts. Do not expect full system spec-as-source for years, if ever.

---

## Key Decisions Teams Must Make

Organizations that reach this level gap must answer these questions before proceeding:

1. **Is code a craft or a commodity?** If your team views code as a craft — something worth refining and maintaining — Spec-Anchored is the right choice. If you view code as disposable output of a specification — a means to an end — Spec-as-Source may be viable.

2. **Can you enforce regeneration-only discipline?** If developers will hand-edit generated code when it's convenient, Spec-as-Source will fail. Do you have the organizational maturity to say "no hand edits; update the spec and regenerate"?

3. **Is your spec generator mature and stable?** Spec-as-Source assumes that the generation pipeline is more reliable than humans writing code. If your generator is experimental or vendor-locked, this assumption is false.

4. **Can you live with non-determinism?** Even mature generators can produce slightly different code across runs. Is your system designed to handle that variability (via tests, contracts, and validation) or will it break?

5. **Is the spec more valuable than the code?** This is the core philosophical question. If you believe that understanding the intent is worth more than understanding the implementation, Spec-as-Source is a natural fit. If you believe the opposite, stick with Spec-Anchored.

---

## Sources

- [Code is Disposable: Treating Specifications as Your Source of Truth — Recursive AI](https://recursiveai.net/articles/code-is-disposable/)
- [Spec-driven.md — GitHub Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices)
- [The Spec as Source of Truth: Why Codebases Should Be Rebuildable from Documentation — Augment Code](https://www.augmentcode.com/guides/spec-as-source-of-truth-rebuildable-codebase)
- [Living Specs vs Static Specs: Better Agent Output — Augment Code](https://www.augmentcode.com/guides/living-specs-vs-static-specs)
- [Vibe Coding Got Us Here. Can Spec-Driven Development Save Us? — William Collins](https://wcollins.io/posts/2026/from-vibes-to-specs/)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft for Developers](https://developer.microsoft.com/blog/spec-driven-development-spec-kit)
- [Philosophy — SpecWeave](https://spec-weave.com/docs/overview/philosophy/)
- [Spec-First, Spec-Anchored, Spec-as-Truth: The Three Levels of Spec-Driven Development — Rushi](http://www.rushis.com/spec-first-spec-anchored-spec-as-truth-the-three-levels-of-spec-driven-development/)
- [Specification-Driven Development: The Four Pillars — Alex Rezvov](https://blog.rezvov.com/specification-driven-development-four-pillars)
- [Contract-First API Development: The Spec as Executable Truth — DevGuide.dev](https://devguide.dev/blog/contract-first-api-development)
- [Knowledge-Driven Development — The specification is the truth](https://knowledge-driven.dev/)
- [Spec-Driven Development with OpenSpec — Hari Krishnan's Blog](https://blog.harikrishnan.io/2025-11-09/spec-driven-development-openspec-source-truth)
