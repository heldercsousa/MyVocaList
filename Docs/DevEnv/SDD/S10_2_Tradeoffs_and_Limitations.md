# S10.2 — Trade-offs & Limitations

**Status:** Researched  
**Predecessor(s) ID:** S10

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written from 12+ authoritative sources |

---

## Overview

Spec-Driven Development delivers measurable value in specific contexts but carries real costs and limitations that must be understood upfront. The discipline is not a universal solution. It adds overhead on small tasks, requires significant upfront thinking effort, and imposes ongoing maintenance burden. The decision to adopt SDD should be grounded in an honest assessment of context: Is the problem complex enough, long-lived enough, and collaborative enough to justify the investment? Or is the overhead of specification greater than the value gained?

This section addresses the honest costs, the situations where SDD creates drag, and what the research shows about when to use SDD versus when to step away from it.

---

## Overhead on Small Tasks

### The Specification-to-Implementation Ratio

For small, well-defined features, SDD adds overhead:

| Task Type | Vibe Coding Time | SDD Time | Overhead |
|-----------|-----------------|----------|----------|
| **Simple bug fix (root cause known)** | 15–30 min | 45–70 min | +50–100% |
| **Minor UI adjustment** | 10–20 min | 40–60 min | +150–300% |
| **Single-file refactor** | 20–30 min | 50–80 min | +50–150% |
| **Standard CRUD operation** | 30–45 min | 60–90 min | +50–100% |

On these small tasks, SDD does not save time. It trades faster code generation for higher certainty and traceability. The ROI is **negative** on throwaway code.

### When Overhead Exceeds Value

SDD imposes net cost when:

1. **The task is isolated and low-risk** — A bug fix with known root cause, a single-file refactor, or a minor UI adjustment has limited surface area for rework. Specification discipline does not proportionally reduce risk.

2. **Requirements are obvious** — When "implement CRUD for User entity" is self-explanatory and follows established patterns, detailed specs add ceremony without reducing ambiguity.

3. **The code will never be touched again** — Internal scripts, test harnesses, one-off migrations, or exploratory prototypes have zero long-term maintenance burden. Specifications are for systems that outlive their authors.

4. **Exploration is the goal, not execution** — When the goal is to discover *what* to build (not implement a known design), specifications are premature. Vibe coding explores faster; once exploration converges, formalize into a spec.

5. **Performance-critical code requiring manual optimization** — Specification cannot capture domain knowledge about memory layout, CPU cache behavior, or algorithmic trade-offs that human engineers discover through iterative performance tuning. AI generation from a spec produces "correct" but unoptimized code.

### The Hidden Cost: False Confidence

Over-specification on small tasks creates a different kind of overhead: **false confidence**. A 500-line specification for a 100-line feature creates the illusion of completeness. The specification covers edge cases on paper, giving confidence that the generated code handles them. Testing is skipped or abbreviated. Then production reveals an uncovered case, and the team discovers that the specification's edges were not actually implemented — the spec was just thorough writing.

Result: The team spent 60 minutes writing and reviewing a detailed spec, generated code that matched 85% of it, deployed without catching the 15% gap, and now faces a production incident that would have been caught by 10 minutes of manual code review on a vibe-coded alternative.

---

## Adoption ROI Timeline and Productivity Dip

### The Four-Week Wall

Research from industry adoption data shows a consistent pattern:

| Period | Productivity | Development Model |
|--------|--------------|------------------|
| **Weeks 1–2** | **-10–20%** (dip) | Learning workflow; specs feel like overhead |
| **Weeks 3–4** | **-5–10%** (recovering) | Specs become muscle memory; some benefits visible |
| **Weeks 5–8** | **0% baseline** | Specs neutralize hallucination costs |
| **Weeks 9–12** | **+15–30%** (inflection) | Rework decreases; debug time shrinks |
| **Month 4+** | **+30–50%** (compound) | Accumulated specs reduce onboarding; AI iterations decrease |

**The critical failure point:** Leadership sees week 2 productivity and concludes SDD is not working. Without clear communication about the ROI timeline, adoption is abandoned before benefits accrue.

### Why Weeks 1–2 Hurt

1. **New mental workflow** — Developers accustomed to "prompt the AI, iterate on code" must shift to "think clearly, write spec, iterate on spec, regenerate." The new loop feels slower even when it is not.

2. **Specification skill gap** — Writing good specs is harder than writing code. Good specs require anticipating edge cases, understanding architectural constraints, and articulating requirements precisely. Junior engineers especially struggle.

3. **Review overhead** — Spec reviews are new friction. Code reviews can be skipped under deadline pressure; spec reviews become an approval gate that feels like bureaucracy.

4. **Tooling friction** — SDD tools (Kiro, GitHub Spec Kit, custom harnesses) introduce new steps, keyboard shortcuts, and workflows. The cognitive load of tool-learning adds to process-learning.

### Breakeven Timeline Depends On:

- **Team size and feature velocity** — Large teams coordinating interdependent work see ROI faster (2–3 months). Solo developers may never see ROI on small features.
- **Codebase complexity** — Simple, straightforward codebases see ROI in 4–8 weeks. Complex systems with many implicit dependencies require 3–6 months.
- **Specification quality** — Teams that invest in *good* specs (focused, unambiguous) see ROI in month 2–3. Teams that write minimal specs see ROI in month 4–5.
- **Organizational protection** — Teams with leadership insulating spec discipline from deadline pressure cross the week 4 inflection point. Teams without that protection revert to vibe coding before benefits arrive.

### What the Data Show

IEEE research and production metrics from enterprise adoptions:
- 40–60% defect reduction (with comprehensive planning)
- 30–50% fewer late-stage defects (teams with SDD standard, not project-by-project)
- 60–80% fewer AI-generated regressions (vs. vibe coding without specs)
- BUT: none of these gains arrive before week 8–12

**The message:** The discipline is worth it on a 6+ month horizon. On a 4-week sprint, it is overhead.

---

## Specification Maintenance and Spec Drift

### The Spec-Code Divergence Problem

Specifications are not self-maintaining. A spec written on day 1, accurate on day 10, and then ignored will be actively misleading by month 2.

**Common divergence patterns:**

1. **Code evolves, spec does not** — A bug fix or a performance optimization changes implementation without updating the spec. The spec now describes the old behavior. Future agents regenerate from the old spec and undo the fix.

2. **Spec becomes pseudo-code** — Over-specification locks in implementation details. When the implementation changes (e.g., moving from List to HashSet for performance), the spec is now wrong. Do you update it, or do you mark it as stale?

3. **Implicit knowledge leaks into code comments** — A decision made during planning ("use ULID instead of UUID for better cache locality") ends up in code comments, not in the spec. The spec does not record it. Future agents do not know why this choice was made and may change it.

4. **Spec becomes longer than code** — Teams write comprehensive 50-line specs for 30-line implementations. The spec is thorough but also fragile. Small implementation changes require spec updates that feel disproportionate.

### Maintenance Cost

Keeping specs accurate is **ongoing work**:

- Each significant code change (bug fix, optimization, refactor) requires a spec update decision: update the spec or mark it snapshot-at-time-of-implementation?
- Spec reviews must happen alongside code reviews, adding 5–10 minutes per PR.
- Accumulated specs create documentation burden — teams end up with 50+ spec files that must be searched, understood, and maintained.

**Research finding (Roman Stranghöner, INNOQ 2026):** In the build phase, SDD often turns into *documentation work* that lengthens feedback loops. The planning phase reduces complexity and sharpens requirements. The build phase, if not careful, becomes a documentation bureaucracy.

### The "Golden Rule" Solution

The principle from the arXiv spec-driven development paper (2602.00180): **Use the minimum level of specification rigor that removes ambiguity for your context.**

- **Spec-First:** Write before coding, let it drift after. Suitable for one-off features, prototypes, simple tasks. Lower maintenance.
- **Spec-Anchored:** Update specs alongside code changes. Suitable for production systems with long-term maintenance. Higher maintenance burden, but reduces hallucination risk on regeneration.
- **Spec-as-Source:** Specs are the sole truth; code is always regenerated. Theoretical ideal; practical only with mature tooling and cultural discipline.

Most teams should aim for **Spec-Anchored with loose maintenance:** Update specs when they explicitly guide the next regeneration; don't update specs for internal refactors that don't change external behavior.

---

## Specification Writing Skill Floor

### The Paradox: SDD Raises the Skill Ceiling

Spec-Driven Development requires engineers to think like architects before writing code. This is harder than traditional development.

**Why:**

1. **Anticipating edge cases upfront** — Writing code, you discover edge cases and handle them incrementally. Specifying, you must anticipate them. This requires experience and mental discipline.

2. **Articulating implicit knowledge** — Senior engineers *know* what to build but struggle to formalize that knowledge into written spec. The act of writing is slow for deeply experienced people.

3. **Abstracting from implementation** — Good specs describe *what* without over-constraining *how*. This separation is hard. Teams often write specs that read like pseudo-code, which is overhead without the benefit.

4. **Learning curve compounds team velocity loss** — Junior engineers do not yet have the domain knowledge to write good specs. In early SDD adoption, specs from junior engineers are often incomplete or over-specified, adding rework.

### The Counter-Intuitive Finding

From practitioner research (Bilal Tahir, Hacky Experiments 2026):

> "The skill floor for spec-driven development is actually higher than for writing code directly. You just get dramatically more leverage from it."

And:

> "The best engineers in 2026 aren't the ones who write the most code. They're the ones who write the best specs."

**Implication:** SDD is not for teams learning to code. It is for teams that have learned to code and want to amplify productivity through clarity.

### Mitigation

1. **Start with moderate complexity** — Not simple bug fixes, not greenfield systems. Choose features complex enough to warrant specification but not so complex that specification becomes unwieldy.

2. **Use spec templates** — Reusable templates for common patterns (CRUD features, API additions, integrations) reduce the cognitive load of structure and let teams focus on content.

3. **Invest in coaching, not mandates** — A coach who helps teams write better specs and debug bad ones is more effective than a policy. People resist policies; they respond to expert guidance.

4. **Track and share learnings** — After each feature, ask: "What did the specification reveal that we would have discovered too late otherwise?" Accumulating these stories builds cultural adoption.

---

## When SDD Is Contraindicated

### Exploratory Coding and R&D

**Rule:** When the goal is to discover *what* to build (not implement a known design), specifications are premature.

Specifications require knowing what you want. In exploratory coding, you often do not know what you want until you see what is possible. You cannot write a specification for something you have not yet imagined.

**What works instead:**
- Use vibe coding to explore ideas quickly.
- Once the direction is clear (after days or weeks of exploration), formalize the winning approach into a spec.
- For subsequent implementation, use SDD.

**Danger:** Using SDD tooling to "structure" exploratory work accelerates burnout. The full SDD workflow (requirements → design → tasks → implementation) for something you are still learning to build is overhead. Specification forces pre-decisions that exploratory work has not validated yet.

### Single Developer, Small Scope, Well-Understood Domain

The coordination benefit of specs disappears when there is only one person. A single engineer building a single feature in a familiar domain with clear requirements has little need for a specification artifact. The engineer's internal model is sufficient.

**Better approach:** Document intent in commit messages and code comments. Use TDD to validate behavior. A minimal one-page "SPEC.md" at the repo root is sufficient boundary-keeping without ceremony.

### Performance-Critical Code

Specifications describe *what*; performance optimization requires understanding *how* — CPU cache behavior, memory layout, algorithmic trade-offs, and empirical profiling. AI generation from a spec produces correct but often unoptimized code. Teams optimizing for performance typically need:

1. Spec to define correctness constraints.
2. Manual implementation or AI-assisted iteration with profiling feedback.
3. Performance tests, not just functional tests.

SDD alone (spec → generate → test) does not produce optimized code without performance profiling as part of the task breakdown.

### Visual/UI-Heavy Projects

SDD tools are text-first. Requirements that are inherently visual (layout, interactions, visual hierarchy, accessibility) are difficult to express purely in markdown or EARS notation.

**Current limitation (Marvin Zhang 2025):** Lack of visual requirements documentation in tools like Kiro. Projects where designs are primary concerns (design systems, interactive dashboards, real-time UIs) benefit more from design-first workflows with AI supporting implementation, not spec-first workflows.

### Highly Regulated Domains (Caveat)

SDD is justified in compliance-heavy contexts (healthcare, fintech, EU AI Act compliance) because the spec becomes the audit trail — requirement to test to code is traceable. BUT the cost is high: specifications must be comprehensive and maintained rigorously. Casual SDD will not satisfy auditors. Full Spec-Anchored or Spec-as-Source discipline is required.

**ROI timeline:** 6–12 months (longer than typical projects because compliance value justifies overhead).

---

## The Waterfall Trap: Over-Specification

### How SDD Reintroduces Big Design Upfront

SDD's proponents say: "Specs are iterative; you write a spec for one feature at a time, implement, review, then iterate the spec based on learning."

In practice, teams often fall into **big design upfront** because:

1. **Specification tools encourage completeness** — AI-assisted spec generators (like Kiro) produce exhaustive 800+ line specifications even for simple features. Reviewing such specs feels like you should implement them all upfront or risk making the review work worthless.

2. **Approval gates feel expensive** — Spec reviews create approval ceremonies. When you have paid the cost of a formal review, the economic pressure is to extract full value — implement the whole spec, not iterate.

3. **Scope creep in spec** — "We might need X in the future" gets written into the spec. Once written, it feels wrong to leave it unimplemented.

4. **Tool momentum** — SDD tools are built around the assumption of comprehensive upfront planning. Fighting that momentum is work.

**Result:** Teams end up with long planning phases, limited feedback, and slow iteration — a mirror of Waterfall.

### The Antidote: Thin Specs, Fast Feedback

Research from SDD practitioners (Thiago Pacheco, sudoish 2026; Roman Stranghöner, INNOQ 2026):

**What works:**
- **One page per feature** — Describe the outcome, the constraints, what is out of scope. Leave room for what you do not know yet.
- **Iterate the spec, not just the code** — The spec changes every cycle. Decisions made, assumptions validated or invalidated, things learned by building. A living document, not a contract.
- **Fast feedback loops** — Specify, build, test, learn, adjust. But only if you keep the scope small enough to actually iterate (days or hours, not months).
- **Code is cheap to regenerate** — Use agents for exploration. Build a quick prototype to test an architectural assumption. Throw it away if it is wrong. Specifying the wrong architecture upfront costs you everything.

**The guard rail:** If the spec takes longer to write than the feature would take to implement, you have over-specified. If changing the spec feels expensive or bureaucratic, you have over-formalized.

---

## The Markdown Problem: Lossy Summarization

### Specification-to-Implementation Fidelity Loss

One of the most honest critiques from practitioners (Sibylline Software 2026):

> "Current spec driven development tools deliver the theater of spec driven development, but when it comes time to implement, the agents treat the specs more like suggestions. You end up spending a ton of time discussing and reviewing, just for the agent to follow 70% of what you told it."

**Root cause:** Markdown is a lossy medium. Conversational refinement of a specification yields deep understanding. Markdown summarization of that understanding is inherently lossy:

1. Detailed requirements elicitation sessions (50,000 tokens of conversation) are compressed into specs (15,000 tokens).
2. Nuance, rationale, and implicit constraints are lost in the summarization.
3. AI agents implementing from the markdown do 80–90% of what the spec intended.
4. You cannot tell what is missing until QA or production.

**Implication:** The spec is not a contract; it is a starting point. Implementation still requires detailed review and iteration.

### Why This Matters for Adoption

Teams expecting specifications to fully specify implementation details (like engineering blueprints for a building) are disappointed. Specifications reduce but do not eliminate rework. You still need to:

1. Review generated code against intent.
2. Test edge cases the spec implied but did not make explicit.
3. Iterate if the generated code misses the mark.

SDD does not automate away the need for skill, judgment, and review. It redirects that work upfront (into spec writing) rather than late (into code review and debugging). That redirection saves time for complex features but costs time for simple ones.

---

## Cultural and Organizational Resistance

### Why Developers Resist SDD

From research across enterprise adoptions:

| Objection | Root Cause | Reality |
|-----------|-----------|---------|
| **"Specs slow us down"** | Week 1–2 productivity dip is real; team has not yet seen benefits. | Dip is real. ROI arrives in month 3–4, not week 1. |
| **"The code is the spec"** | Senior engineers conflate implicit knowledge with explicit intent. AI agents cannot read minds. | True for humans; false for AI. Specs are for agents and future maintainers. |
| **"This is Waterfall"** | Bad SDD implementations do create long planning phases with limited feedback. | Risk is real. Antidote: thin specs, fast iterations, continuous feedback. |
| **"We don't have time for specs"** | Deadline pressure exists; specs feel optional under time pressure. | Specs reduce downstream rework. Skipping them *increases* total time over 3 months. |
| **"AI will replace my job"** | Anxiety about automation. Legitimate concern if framed poorly. | SDD amplifies judgment. Engineers focus on architecture and intent; AI handles mechanical coding. |

### Organizational Interventions That Work

From successful enterprise adoptions (Ministry of Programming, Thoughtworks, InfoQ case studies):

1. **Cross-functional spec review** — Product + Engineering + Architecture review specs *together*. Alignment before coding prevents rework.

2. **Visible metrics** — Track defects, QA cycle time, rework hours. Display every two weeks. Narrative changes from "specs are overhead" to "specs prevent the expensive bugs we used to chase for days."

3. **Protect from deadline pressure** — Do not let leadership optimize SDD away under time pressure. "We'll cut the spec review to get faster" kills the practice. Protect spec discipline; it pays back.

4. **Small pilots, not org-wide mandates** — Let a single team prove SDD reduces rework. Word spreads faster than policy.

5. **Coaching, not mandates** — A coach who helps teams write better specs is more effective than a policy. People resist policies; they respond to expert guidance.

6. **Transition training** — Roles shift. Architects now define intent early; developers validate and extend specs. QA verifies against specs. Explicit role training reduces anxiety.

---

## The Three-Month Wall: When Vibe Coding Hits a Ceiling

Industry data shows a consistent pattern: vibe coding (prompt and pray) ships prototypes faster for roughly **three months**. After that, technical debt compounds so fast that it overtakes any velocity gains.

### Where the Wall Hits

- Code reaches 10–20 interdependent files
- Multiple engineers work on the same module
- Requirements change mid-feature
- AI-generated regressions surface in integration tests
- Onboarding new engineers requires reading hundreds of lines of cryptic code
- Debug cycles lengthen because no one knows what the feature was supposed to do

At that point, the absence of a spec is **actively slowing down delivery**. SDD adoption moves the wall out to 6+ months.

### The Business Math

| Timeline | Vibe Coding | SDD |
|----------|------------|-----|
| **Week 1–4** | Fast ship | Slower (overhead) |
| **Month 2–3** | Slowing (rework) | Steady (less rework) |
| **Month 3–4** | Wall hit; productivity craters | Accelerating (accumulated specs) |
| **Month 6+** | Stalled or moving backward | 30–50% faster than vibe baseline |

**Decision point:** If your feature will live beyond 3 months, SDD ROI is positive. If it is a throwaway, vibe coding is faster.

---

## Practical Guidelines: When to Use SDD

### Decision Matrix

| Context | Use SDD? | Recommended Level | Comment |
|---------|----------|-------------------|---------|
| **New project, small team (2–5)** | Strongly justified | Spec-First | Start with specs; minimal overhead. ROI in 4–8 weeks. |
| **New project, larger team (8+)** | Strongly justified | Spec-Anchored | Coordination costs paid back fast. ROI in 3–6 months. |
| **Legacy code, small team** | Justified selectively | Spec-First for next feature only | Do not retrofit. Spec new features as you touch them. |
| **Legacy code, larger team** | Justified with caveats | Constitution first; then organic spec growth | 3–6 months phased adoption. Constitution surfaces architectural agreements first. |
| **Solo developer, exploratory** | Not justified | Vibe coding, then formalize | Formalize into spec *after* direction is clear. |
| **One-off scripts, migrations** | Not justified | Commit messages + comments | Specs are waste on throwaway code. |
| **Highly regulated domains** | Mandatory | Spec-Anchored or Spec-as-Source | 6–12 months ROI; compliance value justifies overhead. |
| **Simple, isolated bug fix** | Not justified | Vibe coding | Spec overhead exceeds implementation time. |
| **Performance-critical code** | Partial | Spec + manual iteration with profiling | Specification for correctness; manual tuning for performance. |
| **Visual/UI-heavy projects** | Partial | Design-first + AI implementation assist | Text-first specs are limiting. Design tools are primary. |

### Red Flags That Indicate SDD Is Wrong for This Task

1. **"We can describe this in one sentence"** — Single-sentence features do not warrant specification overhead.
2. **"The requirement keeps changing"** — Specifications work when requirements are stable. Highly volatile requirements waste specification effort.
3. **"There is no one to coordinate with"** — Solo developers on small tasks get minimal coordination benefit.
4. **"This is a POC; we might throw it away"** — Prototypes and proofs of concept are throwaway. Specs are for systems that outlive their authors.
5. **"I am exploring whether this is even possible"** — Exploration before specification. Once the direction is clear, formalize into a spec.

---

## Sources

- [The Right Kind of Hard – INNOQ (Roman Stranghöner, Apr 2026)](http://www.innoq.com/en/blog/2026/04/versteckte-kosten-spec-driven-development/)
- [The Problems with Spec Driven Development — Sibylline Software (Jan 2026)](https://sibylline.dev/articles/2026-01-28-problems-with-spec-driven-development/)
- [Copilot Workspaces vs. Intent — Augment Code (Apr 2026)](https://www.augmentcode.com/tools/copilot-workspaces-vs-intent)
- [Spec-Driven Development: The Shift from Writing Code to Writing Requirements — Hacky Experiments Blog (Bilal Tahir, Jan 2026)](https://www.hackyexperiments.com/blog/spec-driven-development)
- [Spec-Driven Development: A Systematic Approach to Complex Features — Marvin Zhang (Sep 2025)](https://marvinzhang.dev/blog/spec-driven-development)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship Production-Ready AI-Generated Code — Pockit Blog (Apr 2026)](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Spec-Driven Development with AI Coding Agents — Java Code Geeks (Mar 2026)](https://www.javacodegeeks.com/2026/03/spec-driven-developmentwith-ai-coding-agents-the-workflow-replacingprompt-and-pray.html)
- [Spec-Driven Development Isn't Waterfall — But It Keeps Ending Up There — sudoish (Thiago Pacheco, Apr 2026)](https://sudoish.com/spec-driven-development-waterfall-trap/)
- [Spec Driven Development as a Standard — Ministry of Programming (Mar 2026)](https://ministryofprogramming.ghost.io/spec-driven-development-as-a-standard/)
- [The $300K Bug: Spec Quality as a Direct Cost Lever — Umesh Malik (Feb 2026)](https://umesh-malik.com/blog/spec-driven-development-ai-agents-addy-osmani)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv (2602.00180, Feb 2026)](https://arxiv.org/html/2602.00180v1)
- [Spec-Driven Development from Vibe Coding to Structured Development — Zarar's blog](https://zarar.dev/spec-driven-development-from-vibe-coding-to-structured-development/)
- [When ADD Is Wrong: Recognizing the Limits of AI Development — Ivan Turkovic (Feb 2026)](https://www.ivanturkovic.com/2026/02/20/when-add-is-wrong-recognizing-limits/)
- [Spec-Driven Development: AI-Assisted Coding — SolGuruz (Paresh Mayani, Mar 2026)](https://solguruz.com/blog/spec-driven-development-guide/)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang (Oct 2025)](https://marvinzhang.dev/blog/sdd-tools-practices)
- [AutoSpec: A Specification-Driven Framework for Scalable AI-Assisted Software Development — Eli Hundia, GitHub (Mar 2026)](https://github.com/Hundia/autospec/blob/main/docs/ACADEMIC_PAPER.md)
