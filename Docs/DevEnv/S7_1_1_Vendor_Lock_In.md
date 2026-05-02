# S7.1.1 — Vendor Lock-In

**Status:** Researched
**Predecessor(s) ID:** S7.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; content written with lock-in trade-offs and framework comparison |

---

## Overview

Spec-Driven Development tooling creates different degrees of vendor lock-in depending on the architecture chosen. Unlike generic software where lock-in is purely a procurement issue, SDD lock-in binds multiple layers: the IDE, the specification format, agent support, and the model selection strategy. A framework choice made early in a project can constrain model options for years — and reversing that choice is non-trivial.

This section analyzes the lock-in surface for each of the three major SDD tools (Kiro, GitHub Spec Kit, Tessl) and quantifies the switching costs teams should expect.

---

## The SDD Lock-In Problem

### Why SDD Tooling Creates Unique Lock-In

In traditional development, vendor lock-in is primarily infrastructure-level (database, hosting, CI/CD platform). AI-assisted development adds layers on top:

1. **Specification Format Lock-In** — Specs encode tool-specific syntax (EARS notation for Kiro, constitution.md format for Spec Kit, Tessl's capability lists). Changing tools requires reformatting the entire spec corpus.

2. **Agent Binding** — Tool choice constrains which AI coding agents your team can use. Kiro is Kiro-agent-only. Spec Kit works with 22+ agents. This affects model selection and pricing flexibility.

3. **Workflow Assumptions** — Each tool encodes assumptions about how teams work: parallel agents (BMAD), sequential phases (Spec Kit), living specs (Intent). Switching tools requires retraining team muscle memory and recalibrating review processes.

4. **Integration Depth** — Native IDE integration (Kiro), hooks system (Kiro), registry dependencies (Tessl), and MCP integration (Tessl) create architectural coupling that becomes harder to undo as the system matures.

### The Trade-Off Paradox

The best SDD tools create lock-in precisely because their value comes from deep integration. A tool that tightly understands your spec format, auto-generates tasks, validates architectural assumptions, and syncs with your codebase in real-time is more valuable than a generic toolkit. But that value is built on coupling — remove the coupling and you lose the feature.

The uncomfortable truth, documented by practitioner analyses: **avoiding lock-in has a cost that often exceeds the cost of accepting calculated lock-in and planning an exit path upfront.**

---

## Lock-In Levels by Tool

### 1. Amazon Kiro — High Lock-In Risk

**Model:** Proprietary IDE (VS Code fork) + proprietary agent + Kiro-specific workflows

**Lock-In Surface:**
- **IDE coupling** — Kiro specs run only in the Kiro IDE. You cannot execute Kiro-generated tasks (`requirements.md`, `design.md`, `tasks.md`) with Claude Code, Cursor, or GitHub Copilot without reformatting the task structure.
- **EARS notation** — Kiro generates acceptance criteria in EARS format (Easy Approach to Requirements Syntax). Converting back to Gherkin or unstructured prose for other tools requires manual rewrite.
- **Agent binding** — Kiro uses its own internal agent. If you want to switch to Claude Opus 4.7 or a custom fine-tuned model, you cannot — Kiro's agent is the only execution option. Model selection strategy is locked to Kiro's roadmap.
- **Hook system** — Kiro's event-driven automation (run tests on save, validate against Figma, optimize for performance) is Kiro-proprietary. These automations do not exist in other tooling. Migrating off Kiro means losing this automation layer and rebuilding it manually or through CI/CD.
- **Steering files** — Project-level configuration (analogous to CLAUDE.md) is Kiro-specific. Migrating to another tool requires translating steering file rules to that tool's config format.

**Switching Cost:** Martin Fowler's analysis noted that Kiro generated 16 acceptance criteria for a simple bug fix — illustrating potential over-specification and coupling. Industry practitioners estimate 4–8 weeks to migrate a greenfield Kiro project to Spec Kit or another framework due to spec reformatting, task restructuring, and process retraining.

**Exit Path Credibility:** AWS has committed to Kiro as a product and is investing in it as part of Bedrock. However, if AWS changes priority or Kiro pricing increases substantially, teams are stuck. No documented Kiro-to-Spec Kit migration guide exists.

**Best For:** Teams deeply integrated with AWS infrastructure, willing to optimize for speed and IDE integration at the cost of flexibility. Organizations that can afford 12-month lock-in because the business case justifies it.

**Worst For:** Multi-cloud teams, teams with existing vendor agreements preventing AWS tooling, organizations that require agent flexibility for compliance or cost reasons.

---

### 2. GitHub Spec Kit — Low Lock-In Risk

**Model:** Open-source CLI toolkit (MIT license) + agent-agnostic specifications

**Lock-In Surface:**
- **Spec format coupling** (low) — Spec Kit specs are markdown with a standard structure (spec.md, plan.md, tasks/, constitution.md). This format is portable to other SDD frameworks with minimal reformatting. Not proprietary.
- **Agent flexibility** (high) — Works with 22+ agents: Claude Code, GitHub Copilot, Cursor, Gemini CLI, Windsurf, Codex, and others. No lock-in to a single agent or model. Swap agents without changing spec format.
- **No lock-in at execution** — Specs are generated once, then consumed by whatever agent you choose. No proprietary agent binding.
- **License transparency** — MIT-licensed. You can fork, extend, or maintain internally if GitHub stops supporting Spec Kit.
- **Ceremonial overhead** (moderate) — Scott Logic documented that Spec Kit generates significant spec volume (one feature generated 2,577 lines of specification). Migrating off means investing the time already spent in spec writing; moving to a lighter-weight tool (like OpenSpec) may not recover that investment but avoids future overhead.

**Switching Cost:** Switching away from Spec Kit is low-friction for the tool layer. The real cost is retraining team muscle memory around the new tool's workflow. A developer trained on `/speckit.specify`, `/speckit.plan`, `/speckit.tasks` needs 1–2 weeks to relearn OpenSpec's lightweight pattern or Tessl's registry-driven approach.

**Exit Path Credibility:** GitHub has commercial incentive to maintain Spec Kit (it drives Copilot adoption) and the spec format is open. Exporting your specs is straightforward — they're git-checked-in markdown files. The ecosystem has matured with multiple competing tools (GSD, OpenSpec, Intent) that can consume Spec Kit specs with minimal changes.

**Best For:** Teams seeking portability, multi-agent teams, organizations that value open-source flexibility, teams on strict vendor selection policies.

**Worst For:** Teams that need opinionated IDE integration, organizations prioritizing automated spec-to-code orchestration (Spec Kit does not provide multi-agent parallelism management).

---

### 3. Tessl — Medium Lock-In Risk (Ecosystem Dependent)

**Model:** CLI + Spec Registry (freemium platform) + agent-agnostic skills

**Lock-In Surface:**
- **Registry dependency** — Tessl strongly encourages publishing and consuming context via its registry. Teams that standardize on Tessl's registry face lock-in if:
  - They adopt 10+ registry-published skills in their workflows
  - They customize registry skills for internal use and store them there
  - They depend on Tessl's version-matching of library specs to prevent API hallucination
  
  If the registry becomes unavailable or pricing changes, teams face a one-time migration cost to archive published skills and rebuild internal distribution.

- **Spec format assumptions** (moderate) — Tessl's spec format (description, capabilities, API section, test links) is more opinionated than Spec Kit's markdown. Converting Tessl specs to OpenSpec or GSD requires structural adjustment.

- **Agent flexibility** (high) — Tessl skills are agent-agnostic markdown files. They work across Claude Code, Cursor, Copilot, Gemini, Codex, Windsurf with no tool-specific syntax. This is a deliberate design choice to avoid lock-in.

- **Governance immaturity** — While skills are versioned, deprecation policy, major-version bumping rules, and yank procedures are still emerging. Teams publishing internal skills face uncertainty around how Tessl's governance will evolve.

**Switching Cost:** Moving away from Tessl's registry (keeping the spec format) is low-friction. You export your skill packages and maintain them internally or republish on a competing registry (if one emerges). Adopting a different SDD tool with Tessl-formatted specs requires converting the spec structure to match the new tool's assumptions.

**Exit Path Credibility:** Tessl is well-funded (backed by Snyk founders) and positioned as a long-term platform play. However, the company's core revenue model depends on registry adoption and paid tiers. If business priorities shift, pricing changes could affect adoption. The open-source aspects (CLI, skill format) can be forked, but the registry data remains vendor-controlled.

**Best For:** Teams adopting Spec-as-Source maturity, organizations that want library-aware agent context (prevents API hallucination), teams publishing internal skills across multiple AI agents.

**Worst For:** Organizations with strict policies against external dependencies, teams building on regulated infrastructure where hosted registries are not allowed, teams seeking purely local-first tooling.

---

## Comparative Lock-In Matrix

| Dimension | Kiro | Spec Kit | Tessl |
|-----------|------|----------|-------|
| **IDE coupling** | Proprietary IDE only | None (CLI) | None (CLI) |
| **Agent flexibility** | Locked to Kiro agent | 22+ agents | 10+ agents |
| **Spec portability** | Low (EARS format reformatting required) | High (markdown, portable) | Medium (opinionated structure, adaptable) |
| **Exit path documented** | No | Yes (open source) | Partial (registry dependency noted) |
| **Switching cost estimate** | 4–8 weeks (spec + process retraining) | 1–2 weeks (muscle memory retraining) | 2–4 weeks (spec reformatting + registry export) |
| **License transparency** | Proprietary (AWS-controlled) | MIT (forked if needed) | Freemium platform (core CLI open-source adjacent) |
| **Model selection flexibility** | None (Kiro agent only) | Complete (any agent, any model) | Complete (any agent, any model) |
| **Calculated lock-in acceptable?** | Yes (if 12+ month horizon and AWS alignment) | No (portability by design) | Conditional (if registry adoption is light) |

---

## Hidden Switching Costs

### The 80/20 Rule of Migration

A Zapier survey (cited in The Register, April 2026) of 542 US executives found only 42% of organizations that attempted AI vendor migration reported smooth outcomes. Analysis of actual migration projects reveals why: **only 20% of switching cost is the tool itself; the other 80% is distributed across surrounding systems.**

**The 20% (visible):**
- Uninstalling the old tool
- Installing and configuring the new tool
- Validating basic functionality

**The 80% (hidden):**
- **System prompt adaptation** — Custom instructions written for Kiro's behavior patterns must be retested against a new agent's refusal boundaries, output formatting, and reasoning style
- **Eval suite rebuild** — If your team has written test cases to validate Kiro-generated code quality, those tests may need recalibration for a different agent's output distribution
- **Workflow muscle memory** — Keyboard shortcuts, command sequences, context-switching patterns become automatic over time. New tools require 2–4 weeks of reduced productivity as developers relearn (JetBrains/UC Irvine study found 74% of developers didn't consciously notice context-switching overhead, but telemetry showed measurable productivity dip)
- **Team training and standardization** — If your team of 10 all switched tools, that's potentially 10 weeks of collective productivity loss — the real TCO calculation
- **Integration debt** — CI/CD pipelines, custom hooks, MCP server integrations, and automation built around the old tool must be rebuilt for the new one

---

## Mitigation Strategies

### 1. Accept Lock-In Consciously

Instead of pretending you can avoid lock-in, accept it when the benefits outweigh the exit cost:

- **Document the decision** — Record in your architecture decision log why you chose Kiro (fast execution, IDE integration) vs Spec Kit (portability, agent flexibility). This makes the trade-off explicit.
- **Estimate the exit cost** — If migrating off Kiro takes 6 weeks of engineering time, ask: is the 12-month productivity gain worth it? For many organizations, yes.
- **Set a migration horizon** — Commit to reevaluating tool choice at a specific point (Series B, 2-year anniversary, major version bump). This prevents indefinite lock-in.

### 2. Architecture for Portability

If you're concerned about lock-in, design for exit from day one:

- **Spec format standardization** — Use markdown and a portable format (Spec Kit's structure, or OpenSpec's lightweight approach) rather than tool-specific syntax.
- **Agent abstraction** — Define a team-level interface for "spec consumption" so you could swap agents without rewriting all your specs. Keep custom instructions in a config file (CLAUDE.md, constitution.md) rather than baked into the IDE.
- **CLI-first tooling** — Prefer CLI-based tools (Spec Kit, Tessl, OpenSpec) over IDE-integrated tools (Kiro) if you value flexibility.
- **Version-matched dependencies** — If using a registry (Tessl), maintain your own internal copy of frequently-used specs so you're not dependent on external registry availability.

### 3. Graduated Adoption

Avoid cold-switching between tools at scale:

- **Start with a pilot** — Run both tools in parallel for 2 weeks (e.g., Spec Kit and Kiro). Let a small team use the new tool while others continue. This reveals switching costs before full rollout.
- **Document the gaps** — Record what workflows break, what muscle memory doesn't transfer, what specs need reformatting.
- **Plan the wave** — Based on pilot learnings, schedule full migration during a natural pause (between sprints, after a release) rather than mid-feature.

### 4. Maintain Compatibility Layers

If portability is critical:

- **Tool-agnostic spec format** — Write specs in plain markdown that any tool can consume. Don't use Kiro's EARS-specific syntax or Tessl registry-specific linking if you might switch later.
- **MCP integration** — Use MCP servers to decouple agent implementation from workflow. An MCP server that exposes your specs can work with any MCP-compatible agent, reducing tool lock-in to the MCP level (which is more portable).
- **Test harness** — Keep your eval/quality criteria independent of the tool. A test suite that validates "generated code passes security checks and performance targets" works regardless of which agent produced it.

---

## When to Accept Lock-In

Accept lock-in to Kiro when:
- You are an AWS customer with deep Bedrock integration
- Your team values streamlined IDE experience over agent flexibility
- Your feature scope justifies upfront spec ceremony (complex features, not bug fixes)
- You plan a 12–24 month horizon before reconsidering

Accept lock-in to Tessl's registry when:
- API hallucination prevention (10,000+ library specs) is your primary pain
- You have 10+ custom internal skills you're publishing for multi-team use
- Your compliance model allows external registry dependency

Prefer Spec Kit when:
- You need multi-agent flexibility for cost or compliance reasons
- You value portability over IDE integration
- Your team already uses GitHub and wants native Copilot integration
- You prefer open-source tooling with clear exit paths

---

## Conclusion

Vendor lock-in in SDD tooling is not binary — it's a spectrum. The key is to make the trade-off **consciously**:

1. **Identify what you're locked into** (IDE, agent binding, spec format, registry dependency)
2. **Estimate the switching cost** (spec reformatting + process retraining + eval rebuild + team training)
3. **Compare to the benefit** (IDE integration, spec automation, agent flexibility)
4. **Document the decision** and set a reevaluation horizon

The most portable choice is **Spec Kit** — it's open-source, format-agnostic, works with any agent, and has no registry dependency. The most productive choice may be **Kiro** for teams optimizing for speed at the cost of flexibility. **Tessl** offers a middle path if library-aware context is your bottleneck.

Switching costs are real. Ignoring them by pretending portability is free leads to worse outcomes than choosing lock-in deliberately and planning an exit path upfront.

---

## Sources

### Tier 1 — Primary

- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html) — Authoritative tool comparison with lock-in analysis
- [Intent vs GitHub Spec Kit (2026): Platform or Framework? — Augment Code](https://www.augmentcode.com/tools/intent-vs-github) — Comparative analysis of platform vs framework approaches
- [AI vendor lock-in raises migration costs and procurement risks — The Register](https://letsdatascience.com/news/ai-vendor-lock-in-raises-migration-costs-and-procurement-ris-89db7866) — Zapier survey on migration smooth outcomes and switching friction
- [The Hidden Switching Costs of LLM Vendor Lock-In — Tian Pan](https://tianpan.co/blog/2026-04-17-llm-vendor-lock-in-hidden-switching-costs) — Analysis of 80/20 rule: 20% tool, 80% surrounding systems
- [Spec-Driven Development: GSD vs Spec Kit vs OpenSpec — Ale Zanello](https://azanello.com/blog/spec-driven-development-tools-compared) — Framework comparison with switching costs and ceremony overhead

### Tier 2 — Secondary

- [Spec-Driven Development Framework Patterns — David Daniel Research](https://daviddaniel.tech/research/papers/sdd-frameworks/) — Comprehensive enterprise adoption analysis
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices) — Framework positioning and adoption patterns
- [AI Tool Switching Is Stealth Friction – Beat It at the Access Layer — JetBrains](https://blog.jetbrains.com/ai/2026/02/ai-tool-switching-is-stealth-friction-beat-it-at-the-access-layer/) — Study on invisible context-switching overhead
- [Developer Tools and Vendor Lock-In: The Real Trade-off — Quinn Reed](https://futurion.blog/the-uncomfortable-truth-about-developer-tools-and-vendor-lock-in/) — Trade-off analysis: when to accept lock-in
- [How to Break Up With Your AI Coding Assistant (Without the Drama) — Listicler](https://listicler.com/blog/how-to-break-up-with-your-ai-coding-assistant-without-the-drama) — Practical migration workflow and cost estimates

### Tier 3 — Tertiary

- [GitHub Spec Kit vs. Vibe Coding: Why SDD Is the Better Way — Ananya Rajeev](https://ossels.ai/github-spec-kit-spec-driven-development/) — Spec Kit positioning and feature trade-offs
- [Moving from Other Tools -- Decision Framework — Developer Toolkit](https://developertoolkit.ai/en/comparison/migration-guide/) — Step-by-step migration planning and rollback strategies
- [Tool Migration Checklist — Developer Toolkit](https://developertoolkit.ai/en/appendices/migration-checklist/) — Practical pre/post-migration checklist
- [What Is Spec-Driven Development? — sdd.sh](https://sdd.sh/2026/03/what-is-spec-driven-development/) — Overview of SDD maturity levels and tooling ecosystem
- [Spec-Driven Development (2026 Guide) — Product Builder](https://www.productbuilder.net/learn/spec-driven-development) — Industry adoption trends and tool maturity updates
