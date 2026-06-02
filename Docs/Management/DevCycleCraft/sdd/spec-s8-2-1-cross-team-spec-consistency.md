# S8.2.1 — Cross-Team Spec Consistency

**Status:** Researched  
**Predecessor(s) ID:** S8.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; conflict types, detection frameworks, resolution strategies, tooling landscape |

---

## Overview

Cross-team spec consistency is the challenge of ensuring that specifications written in parallel by different teams or agents remain aligned when they encode interdependent features. When multiple agents work on features that depend on shared APIs, database schemas, or architectural assumptions, their independent spec changes can silently encode contradictory decisions. Code generated from these specs will compile but fail at runtime or integration time. No current framework fully resolves this — it remains a known tension in SDD practice.

The fundamental issue: specifications are written in isolation, each with its own context window and framing. Two agents may implement the same API shape differently, constrain data models in conflicting ways, or make incompatible architectural choices. These conflicts are often semantic (both teams understood the business intent differently) rather than syntactic (Git merge would catch those). Detecting and resolving semantic conflicts requires either upfront coordination (expensive, reduces parallelism) or post-execution detection and reconciliation (catches problems late).

Research across 600 multi-agent runs (Acharya 2026) shows that 79% of multi-agent system failures stem from specification and coordination issues, not model capability. Of those failures, 41–79% originate from conflicting specs between parallel teams. This is the dominant class of problem facing practitioners who attempt to scale SDD beyond single-team workflows.

---

## Conflict Types

### Type 1: Contradictory Intent

Two specs describe the same component but encode incompatible functional requirements. 

**Example:** Team A specifies that an API endpoint `/users/{id}` returns only non-sensitive profile fields by default, with a `?include=email,phone` flag for extended data. Team B, unaware of Team A's design, specifies the same endpoint returns all fields including email and phone in the standard response. Both are internally consistent, both are reasonable, but they contradict.

**Detection:** AST-based or semantic model inspection — requires parsing both specs and comparing type signatures, data model shapes, and logical constraints. Precision improves with spec rigor: EARS-formatted specs (SHALL/SHALL NOT) catch this more reliably than free-form prose.

**Cost:** 41–53% of semantic conflicts in large-scale studies are contradictory intents.

### Type 2: Resource Contention

Two specs claim exclusive control of a shared resource or file path.

**Example:** Team A specifies a migration that adds a `users.verification_status` column to the database. Team B, writing a separate feature, specifies a migration that adds `users.security_level` to the same table. Both migrations are correct in isolation, but running them in the wrong order or merge order can corrupt the database state or violate constraints.

**Detection:** Static analysis of file paths, database schema modifications, and dependency declarations. OpenSpec's parallel-merge-plan identifies this category explicitly.

**Cost:** 18–25% of detected conflicts are resource contention.

### Type 3: Causal Violation

One spec depends on an assumption made by another, but the dependency is not declared.

**Example:** Team A writes a spec assuming an authentication service returns a `jwt_token` in the response. Team B, writing the auth service spec, specifies it returns an `access_token` and a `refresh_token` separately. Team C, writing the frontend spec, depends on Team A's assumption. When implementation merges, Team C's code fails because it expects `jwt_token` which no longer exists.

**Detection:** Requires explicit dependency tracking between specs. Frameworks like Ossature use `@depends` directives to model this graph; automated checkers then verify that all assumed outputs are actually provided by dependent specs.

**Cost:** 12–19% of conflicts are causal violations that only surface during integration.

### Type 4: Semantic Drift

Over long-running parallel work, agents on interdependent specs gradually diverge in their interpretation of a shared term or constraint.

**Example:** Two teams working on a messaging feature both use the term "message" but one interprets it as individual events (immutable, never edited) while the other allows full CRUD operations (mutable, versionable). Neither team explicitly stated this, but it becomes a core architectural difference. After weeks of parallel development, reconciliation is expensive.

**Detection:** Continuous semantic monitoring using latent-space embeddings or periodic cross-spec review. Traditional text-based diff catches surface changes but misses semantic drift because the intent slowly shifts without obvious markers.

**Cost:** Compound cost — grows over time, becomes exponentially more expensive to fix.

---

## Resolution Strategies

### Strategy 1: Upfront Interface Contracts

Before any agent writes specs, establish shared contracts for:
- API endpoint schemas (OpenAPI, gRPC, or hand-written)
- Database schema changes (required fields, types, constraints)
- Shared types (TypeScript interfaces, Protobuf definitions)
- Messaging formats (event shapes, Avro schemas)

These contracts live on `main` branch. All parallel spec work inherits these contracts and implements against them without needing to modify them.

**Advantage:** Eliminates contradictory intent conflicts entirely — agents cannot write conflicting APIs if the API shape is locked.

**Disadvantage:** Requires upfront consensus. For exploratory features or when the team is uncertain about the API shape, this is expensive and creates false confidence (a locked contract that turns out to be wrong forces expensive re-scoping).

**Adoption:** Used in Kiro (AWS's SDD framework), Spec Kit, and Ossature. Standard practice for cross-service API development.

### Strategy 2: Dependency-First Merge Sequencing

After parallel spec work completes, don't merge in arbitrary order. Instead:

1. **Build the dependency graph:** Which specs depend on which other specs?
2. **Identify leaf nodes:** Specs with no downstream dependents merge first.
3. **Rebase downstream specs:** After merging a dependency, rebase all specs that depend on it and re-verify their assumptions.
4. **Iterative merge:** Continue until all specs are merged.

This mirrors Git's standard practice but applied at the spec level.

**Advantage:** Ensures downstream specs see the actual decisions made by dependencies, not outdated assumptions.

**Disadvantage:** Requires strict discipline. If Team C doesn't re-validate after Team A merges, old assumptions silently persist in the code.

**Adoption:** Practiced in teams using multi-spec projects (Ossature, GitHub Spec Kit workflows). Less common with unstructured SDD.

### Strategy 3: Three-Way Spec Merge with Fingerprints

OpenSpec's parallel-merge-plan proposes treating spec changes as mergeable artifacts with explicit base versions:

1. **Capture fingerprint:** When an author begins writing a spec change, store a hash of the current spec (the "base").
2. **Track delta:** Author writes delta (changes, not full future state).
3. **Validate fingerprint at merge:** When archiving, recompute the hash of the live spec. If it differs from the stored base, the spec diverged while the author was working — merge cannot proceed without sync.
4. **3-way merge:** Perform diff3 on the base, current, and author's delta at the requirement level (not line level, to preserve semantic units).
5. **Conflict markers:** If merge produces conflicts, write conflict markers (like Git) directly into the change delta and require human reconciliation.

**Advantage:** Catches divergence explicitly, prevents silent overwrites, mirrors Git's proven merging discipline.

**Disadvantage:** Adds process overhead (rebase step before archive). Requires developer discipline. Tools that don't implement this (many current tools) are vulnerable to spec overwrites.

**Adoption:** Planned for OpenSpec 2.0 (roadmap Q2 2026). Not yet standard practice.

### Strategy 4: Semantic Consensus Framework (SCF)

A middleware approach that detects and resolves semantic conflicts in real time during parallel work:

1. **Process Context Layer (PCL):** Provides shared operational semantics to all agents — organizational policies, domain rules, architectural principles.
2. **Semantic Intent Graph (SIG):** Each agent declares its intent before specifying. Intents are formalized as structured objects capturing objectives, constraints, and dependencies.
3. **Conflict Detection Engine (CDE):** Monitors all declared intents and detects Type 1/2/3 conflicts in real time. Conservative detection strategy (high recall, lower precision) flags potential issues before they compound.
4. **Consensus Resolution Protocol (CRP):** When conflicts are detected, resolve via three-tier hierarchy: Policy (organizational rules, highest priority) → Authority (domain expert, second) → Temporal (who asked first, tiebreaker).
5. **Drift Monitor (DM):** Continuous monitoring for semantic drift even in non-conflicting specs — detects subtle divergence before it becomes a problem.
6. **Governance Integration (PAGI):** Policies are externalized and auditable; all resolutions are logged.

**Research results:** Across 600 runs, SCF achieved 100% workflow completion vs. 25.1% for baseline approaches. Detected 65.2% of semantic conflicts with 27.9% precision. Median governance overhead: 145ms (<3% of interaction time).

**Advantage:** Catches conflicts early and continuously, eliminates "surprise" semantic mismatches at integration time. Explicit governance layer satisfies enterprise audit requirements.

**Disadvantage:** Requires formalization of organizational policies upfront. Adds latency to every spec decision. High false-positive rate (27.9% precision means 3 out of 10 flagged issues are not real conflicts) can create "alert fatigue."

**Adoption:** Emerging but not yet standard (Semantic Consensus Framework, 2026). Requires buy-in on process formalization.

### Strategy 5: Context Grounding and Validation Hooks

A phased approach used in Spec Kit Agents:

1. **Discovery hooks (pre-phase):** Before each phase (specify → plan → tasks → implement), run read-only probes against the repository to collect evidence: existing files, conventions, dependencies, git history, other parallel specs.
2. **Validation hooks (post-phase):** After each phase, validate the generated artifact for internal consistency and compatibility with the repository. For specs, validate that referenced APIs exist, required libraries are present, and the plan is feasible.

The context is gathered from the live repository state, not from spec isolation. This ensures specs remain grounded in reality even as parallel work evolves.

**Advantage:** Catches hallucinations (agents inventing APIs that don't exist) and feasibility issues early. Provides automatic grounding so agents see the shared context.

**Disadvantage:** Requires access to live repository state during spec authoring — imposes latency and dependencies. If specs from other parallel teams are not yet merged, grounding sees an incomplete picture.

**Adoption:** GitHub's Spec Kit Agents (2026 research), Azure Verified Modules SDD workflow.

---

## Tooling Landscape

### Specification Formats That Support Multi-Spec Work

**OpenSpec (Fission-AI):** Supports parallel change folders with delta specs. Parallel-merge-plan document outlines fingerprint-based merging (planned for v2.0). Currently vulnerable to silent overwrites if multiple teams archive changes to the same requirement simultaneously.

**GitHub Spec Kit:** Multi-feature workflow with explicit `/speckit.analyze` cross-artifact consistency checking. Supports team-reviewed specs in branches merged like code. No built-in multi-spec dependency tracking (GitHub issue #2238 requests this feature).

**Kiro (Amazon):** Enforces three-phase workflow (specify → plan → tasks) with upfront interface contracts. Assumes single feature per session; multi-team coordination happens through the shared API contracts on main branch. Team collaboration model is based on interface stability, not spec merging.

**Ossature:** Native multi-spec support with `@depends` directives and dependency graph tracking. Incremental re-planning when one spec changes. Interface extraction: after a spec is implemented, public interfaces are extracted and available to downstream specs, preventing drift from implementation changes.

**Spec Kit Agents (GitHub research, 2026):** Adds context-grounding hooks that validate specs against repository state before and after each phase. Catches hallucinated paths, missing dependencies, and infeasible plans early.

### Frameworks for Detecting Semantic Conflict

**Semantic Consensus Framework (SCF):** Middleware that detects three categories of semantic conflict in real time. Process-aware (understands organizational policies). Requires explicit intent declaration before action. Emits structured conflict reports with category, severity, and resolution options.

**MPAC (Multi-Principal Agent Coordination Protocol):** Five-layer protocol (Session, Intent, Operation, Conflict, Governance) with 21 message types. Agents declare intent before acting; coordinator detects overlaps and contradictions. Structured conflict handling with human-in-the-loop arbitration. Prototype shows 95.6% reduction in coordination overhead and 4.8× wall-clock speedup on 3-agent code-review tasks.

**SpecWeave:** Detects contradictions in AI agent skills/rules. Applies four-level priority resolution (Local > Project > Vendor > Community). When contradictions detected, applies replacement merge (one instruction wins per priority), union merge (both instructions apply in different scopes), or manual merge (developer decides).

**SpecBridge:** Extracts architectural decisions from specs and code, stores them as versioned YAML with constraints (invariant/convention/guideline). Verifies code against architectural decisions at the constraint level. Provides inference (learns implicit patterns before enforcement) and automated drift detection.

### Supporting Infrastructure

**Colign (Project Memory):** Maintains a shared "Project Memory" of domain rules, technical decisions, and architectural principles. This memory is automatically injected into every spec change made by any agent, giving all agents a common factual baseline to prevent divergence.

**Specledger:** Tracks every spec change with intent and reasoning. Checkpoint model: Specledger creates alignment points where human review confirms that parallel spec changes are not contradictory before implementation begins.

**spec-kit-sync (GitHub):** VS Code extension that detects drift between specs and implementation code. Runs `/speckit.sync.analyze` to produce drift reports (% of spec coverage, conflicting requirements, unspecced code). Proposes fixes: backfill (update spec to match code), align (fix code to match spec), or supersede (mark old spec as obsolete).

---

## Known Gaps and No-Silver-Bullets

**Gap 1: Silent overwrites.** Most current SDD tools (OpenSpec, Spec Kit) have no built-in protection against parallel teams archiving changes to the same requirement block simultaneously. The second archive silently overwrites the first without warning. This is explicitly acknowledged as a limitation in OpenSpec's parallel-merge-plan (Fission-AI, 2026).

**Gap 2: Semantic drift is harder to detect than syntax conflicts.** AST-based conflict detectors achieve 97% precision on structural mismatches but miss subtle divergence in intent. Latent-space approaches (embedding-based drift detection) show promise but remain research-grade (Semantic Drift in Agent Pipelines, 2026).

**Gap 3: Process formalization overhead.** Frameworks like SCF and MPAC that detect semantic conflicts require explicit upfront formalization of organizational policies, domain rules, and decision authority. This is expensive and can fail if the process model is incomplete or inaccurate.

**Gap 4: Multi-team adoption requires discipline.** Even with tooling, cross-team spec consistency depends on teams respecting merge ordering, running validation hooks, and maintaining shared contracts. Teams that skip these steps (or don't know about them) silently accumulate conflicts.

**Gap 5: Dependency tracking is fragile.** Tools that support multi-spec dependency graphs (Ossature, SpecBridge) assume explicit `@depends` directives. Hidden dependencies (Team A's spec assumes Team B's implementation without declaring it) are not caught by tooling and surface only during integration testing.

---

## Best Practices (Emerging)

1. **Establish shared contracts before parallel work begins.** API schemas, database migrations, shared types should be locked on `main` before agents branch. This eliminates contradictory intent conflicts.

2. **Declare dependencies explicitly.** Use `@depends` or equivalent to make hidden dependencies visible. Automate validation that all declared dependencies are satisfied.

3. **Use fingerprint-based merging for specs.** Store the base spec version when a change begins. Validate the fingerprint before archiving. If divergence detected, rebase and re-validate using 3-way merge. This prevents silent overwrites.

4. **Implement context-grounding hooks.** Before finalizing a spec, run read-only probes against the live repository to catch hallucinations (invented APIs, missing dependencies) early.

5. **Sequence merges by dependency order.** Don't merge specs in arbitrary order. Identify leaves of the dependency tree; merge those first. Rebase and re-validate downstream specs after each merge.

6. **Continuous semantic monitoring.** For long-running parallel work, run periodic cross-spec consistency checks to catch semantic drift before it compounds. Tools like spec-kit-sync can detect misalignment between specs and code.

7. **Invest in formalized organizational context.** If using Semantic Consensus Framework or similar, spend upfront time documenting organizational policies, domain rules, and architectural principles. This shared context prevents divergence.

8. **Plan for human arbitration.** No framework eliminates the need for human judgment in resolving conflicting specs that are both semantically sound but architecturally incompatible. Build review workflows that surface these conflicts and empower domain experts to decide.

---

## Sources

- [Semantic Consensus: Process-Aware Conflict Detection and Resolution for Enterprise Multi-Agent LLM Systems — Acharya et al., arXiv:2604.16339](https://arxiv.org/abs/2604.16339) (2026-03-13)
- [MPAC: Multi-Principal Agent Coordination Protocol — arXiv:2604.09744](https://arxiv.org/pdf/2604.09744) (2026)
- [Multi-Agent LLM Systems: Specification and Coordination as Root Causes of Failure — arXiv:2603.24284](https://arxiv.org/pdf/2603.24284) (2026)
- [Spec Kit Agents: Context-Grounded Multi-Agent SDD Workflow — GitHub Research, arXiv:2604.05278](https://arxiv.org/pdf/2604.05278v1) (2026)
- [OpenSpec: Parallel Merge Plan — Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec/blob/main/openspec-parallel-merge-plan.md) (2026)
- [Cross-Feature Orchestration: Tracking State Across Parallel Specs — GitHub/Spec-Kit Issue #2238](https://github.com/github/spec-kit/issues/2238) (2026-04-16)
- [OpenSpec Conventions: Specification Framework — Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec/blob/main/openspec/specs/openspec-conventions/spec.md) (2026)
- [Spec-Driven AI Code Generation with Multi-Agent Systems — Augment Code](https://www.augmentcode.com/guides/spec-driven-ai-code-generation-with-multi-agent-systems) (2025-09-24)
- [The Inter-Agent Communication Problem — Tomás Garcia, Medium](https://medium.com/@tomasqgarcia/the-inter-agent-communication-problem-how-to-pass-requirements-between-ai-7e1a2f41f667) (2026-03-11)
- [Spec-Driven Development Tools Compared: GSD vs Spec Kit vs OpenSpec — Ale Zanello](https://azanello.com/blog/spec-driven-development-tools-compared) (2025-01-01)
- [SpecWeave: Skill Contradiction Resolution — spec-weave.com](https://spec-weave.com/docs/skills/skill-contradiction-resolution/) (2026-04-03)
- [SpecBridge: Architecture Decision Runtime — GitHub nouatzi/specbridge](https://github.com/nouatzi/specbridge) (2026-01-26)
- [Semantic Drift in Agent Pipelines: Latent-Space Auditing as Alignment Infrastructure — Academia.edu](https://www.academia.edu/164985635/Semantic_Drift_in_Agent_Pipelines_A_Case_for_Latent_Space_Auditing_as_Alignment_Infrastructure) (2026-01-01)
- [Agent Drift: Quantifying Behavioral Degradation in Multi-Agent LLM Systems — arXiv:2601.04170](https://arxiv.org/abs/2601.04170v1) (2026)
- [Spec-Driven Development: Living Documentation and API Contracts — Diving Into SDD with GitHub Spec Kit — Microsoft for Developers](https://developer.microsoft.com/blog/spec-driven-development-spec-kit) (2025-09-15)
- [The SDD Workflow — GitHub Spec Kit](https://www.mintlify.com/github/spec-kit/concepts/workflow) (2026)
- [Multi-Spec Projects: Dependency Tracking and Incremental Re-Planning — Ossature Documentation](https://docs.ossature.dev/advanced/multi-spec.html) (2026)
- [CodexSpec: Cross-Artifact Consistency Analysis — zts0hg/codexspec](https://zts0hg.github.io/codexspec/user-guide/commands/) (2026)
- [Team Collaboration at Scale: Shared Rules and Coordination — Cursor Developer Toolkit](https://developertoolkit.ai/en/cursor-ide/advanced-techniques/team-collaboration/) (2026-04-29)
- [Identifying Conflicting Requirements in Systems of Systems — Viana et al., IEEE](https://scispace.com/pdf/identifying-conflicting-requirements-in-systems-of-systems-21606o32v5.pdf) (2009)
- [Merge Strategies for Code Consistency — Git Documentation](https://git-scm.com/docs/merge-strategies) (2026)
- [Merge Strategies: Semantic vs. Prefer-Ours vs. Prefer-Theirs — Aura VCS Documentation](https://docs.auravcs.com/merge-strategies/) (2026)
