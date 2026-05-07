# 0_0_0_0_0 — SDD: Spec-Driven Development

**Status:** Researched  
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial main index file created with full 56-topic map |
| 2026-04-30 | Updated | Added Authoritative Sources section |

---

## Purpose

This is the root index of the SDD (Spec-Driven Development) documentation set for MyVocaList.
Each topic listed below has a dedicated file following the naming convention:

```
<Topic ID with underscores>_<Topic Title words separated by underscores>.md
```

Example: `S9_2_1_Spec_versioning_n_rollback.md`

Each dedicated file is independently researched, reviewed, and versioned.
This index is the entry point; individual files are the authoritative source per topic.

---

## File Naming Convention

| Pattern segment | Meaning |
|----------------|---------|
| `0_0_0_0_0_SDD_` | Main index prefix |
| `S<N>_` | Top-level section |
| `S<N>_<M>_` | Second-level topic |
| `S<N>_<M>_<P>_` | Third-level (deep) topic |

---

## Topic Map

| ID | File | Title | Brief description |
|----|------|-------|-------------------|
| **S1** | S1_Core_Concepts.md | **Core Concepts** | What SDD is and its foundational principles |
| S1.1 | S1_1_Definition.md | Definition | Specs as primary artifacts; AI generates code from them |
| S1.2 | S1_2_Implementation_Levels.md | Implementation levels | Spec-First · Spec-Anchored · Spec-as-Source |
| S1.2.1 | S1_2_1_Level_Gap_Anchored_to_Source.md | Level gap: Anchored → Source | Philosophical divide: is code a maintained artifact or disposable output? |
| S1.3 | S1_3_SDD_vs_TDD_BDD_Waterfall.md | SDD vs TDD/BDD/Waterfall | How SDD differs from and extends prior methodologies |
| **S2** | S2_Specification_Design.md | **Specification Design** | How to write good specs |
| S2.1 | S2_1_Spec_Structure_and_Content.md | Spec structure & content | Inputs/outputs, preconditions, invariants, integration contracts, state machines |
| S2.1.1 | S2_1_1_Tacit_Knowledge_Capture.md | Tacit knowledge capture | Business rules often live implicitly; articulating them exhaustively is costly and lossy |
| S2.1.2 | S2_1_2_Over_Specification_Risk.md | Over-specification risk | Spec becomes pseudo-code: constrains implementation, adds maintenance burden |
| S2.2 | S2_2_Quality_Characteristics.md | Quality characteristics | Domain language, Given/When/Then, concise yet complete, deterministic |
| S2.2.1 | S2_2_1_Acceptance_Criteria_Subjectivity.md | Acceptance criteria subjectivity | "Done" remains subjective; agents implement the letter, not the spirit |
| S2.2.2 | S2_2_2_Verbosity_vs_Precision_Tension.md | Verbosity vs. precision tension | Detailed specs improve AI guidance but become unmaintainable |
| S2.3 | S2_3_Functional_vs_Technical_Separation.md | Functional vs technical separation | Business intent vs implementation detail — kept in separate artifacts |
| S2.3.1 | S2_3_1_Spec_Format_Selection.md | Spec format selection | Wrong format cascades into mismatched tooling and agent behavior |
| **S3** | S3_Workflow_Phases.md | **Workflow Phases** | The SDD development cycle |
| S3.1 | S3_1_Planning_Phase.md | Planning phase | Requirements → Design → Tasks (Markdown files, iterative human review) |
| S3.1.1 | S3_1_1_Architecture_Debt_from_Early_Decisions.md | Architecture debt from early decisions | Tech choices locked in planning may not survive implementation realities |
| S3.1.2 | S3_1_2_Dependency_Analysis_Incompleteness.md | Dependency analysis incompleteness | Hidden coupling surfaces only during coding, forcing re-sequencing |
| S3.2 | S3_2_Implementation_Phase.md | Implementation phase | Code generation by AI agents against approved spec |
| S3.2.1 | S3_2_1_Task_Granularity_Calibration.md | Task granularity calibration | Too coarse: agents lose guidance. Too fine: artificial fragmentation |
| S3.2.2 | S3_2_2_Context_Window_Exhaustion.md | Context window exhaustion | Large tasks compound hallucination risk across many LLM calls |
| S3.3 | S3_3_Verification_Review_Gates.md | Verification / review gates | Human + automated checkpoints before proceeding to next phase |
| S3.3.1 | S3_3_1_Approval_Bottleneck.md | Approval bottleneck | Human gates require synchronous scheduling; becomes a pipeline chokepoint |
| S3.3.2 | S3_3_2_Authority_Ambiguity.md | Authority ambiguity | Unclear who is empowered to approve each phase type |
| **S4** | S4_Context_and_Memory.md | **Context & Memory** | Persistent knowledge that guides agents across sessions |
| S4.1 | S4_1_Memory_Bank_Context_Files.md | Memory bank / context files | CLAUDE.md, AGENTS.md, rules files — applied to all sessions |
| S4.1.1 | S4_1_1_Cross_Session_Context_Loss.md | Cross-session context loss | No framework fully solves persistent architectural context |
| S4.2 | S4_2_Context_Engineering.md | Context engineering | Structuring context to optimize agent-LLM interaction |
| S4.3 | S4_3_External_Integrations.md | External integrations | MCP servers (Context7, Jira, Confluence, etc.) |
| **S5** | S5_Agent_Patterns.md | **Agent Patterns** | How AI agents collaborate on a spec |
| S5.1 | S5_1_Adversarial_Agent_Pattern.md | Adversarial agent pattern | Coordinator → Implementor → Verifier (opposing incentives) |
| S5.1.1 | S5_1_1_Persona_Role_Confusion.md | Persona/role confusion | Agents switching roles mid-session risk losing prior-role context |
| S5.2 | S5_2_Parallel_Agent_Execution.md | Parallel agent execution | Multiple agents working different tasks simultaneously |
| S5.2.1 | S5_2_1_Dependency_Ordering_Fragility.md | Dependency ordering fragility | Parallel task sequence breaks when hidden inter-task dependencies emerge |
| S5.2.2 | S5_2_2_Cross_Agent_Spec_Conflicts.md | Cross-agent spec conflicts | Agents on interdependent specs may produce contradictory outputs |
| S5.3 | S5_3_Subagent_Delegation.md | Subagent delegation | Main agent directs; subagents execute all file writes |
| S5.3.1 | S5_3_1_Silent_Task_Completion.md | Silent task completion | Agents mark verification tasks done without executing them |
| **S6** | S6_Governance_and_Enforcement.md | **Governance & Enforcement** | Ensuring agents stay inside the spec |
| S6.1 | S6_1_Constitutional_Constraints.md | Constitutional constraints | Non-negotiable rules the agent cannot override |
| S6.1.1 | S6_1_1_Constitutional_Rigidity_and_Staleness.md | Constitutional rigidity & staleness | Immutable principles become obstacles as project evolves |
| S6.1.2 | S6_1_2_Amendment_Governance.md | Amendment governance | No clear process for who can change constitutional rules or version them |
| S6.2 | S6_2_Automated_Hooks.md | Automated hooks | Pre/post tool-use checks enforced by the harness |
| S6.2.1 | S6_2_1_Enforcement_Cost_Overhead.md | Enforcement cost overhead | Runtime conformance checking adds latency/resource cost at scale |
| S6.3 | S6_3_Review_Gates.md | Review gates | Mandatory review steps before merging or advancing phases |
| S6.3.1 | S6_3_1_Reviewer_Context_Loss.md | Reviewer context loss | Approvers often lack domain context to judge spec adequacy |
| S6.4 | S6_4_CICD_Integration.md | CI/CD integration | Pipeline enforces spec compliance on every push |
| S6.4.1 | S6_4_1_Six_Drift_Categories.md | Six drift categories | Six silent divergence surfaces where spec-code alignment silently breaks |
| S6.4.2 | S6_4_2_Continuous_Conformance_Requirement.md | Continuous conformance requirement | Drift detection must be active and continuous — periodic checks compound divergence |
| **S7** | S7_Tooling.md | **Tooling** | Software that supports SDD workflows |
| S7.1 | S7_1_Spec_First_Tools.md | Spec-first IDEs/tools | Kiro, GitHub Spec-Kit, Tessl |
| S7.1.1 | S7_1_1_Vendor_Lock_In.md | Vendor lock-in | Framework choice binds model selection; migration is non-trivial |
| S7.1.2 | S7_1_2_Tool_Switching_Friction.md | Tool-switching friction | Specs encode tool-specific assumptions; switching frameworks is costly |
| S7.2 | S7_2_AI_Coding_Assistants.md | AI coding assistants | Claude Code, Cursor, GitHub Copilot |
| S7.3 | S7_3_MCP_Servers.md | MCP servers | Tool-use extensions that give agents real-time context |
| S7.3.1 | S7_3_1_MCP_Protocol_Immaturity.md | MCP protocol immaturity | MCP ecosystem is young; inconsistent agent support and versioning gaps |
| **S8** | S8_Project_Management.md | **Project Management** | Tracking progress and coordinating work |
| S8.1 | S8_1_Task_Tracking.md | Task tracking | tasks.md as ordered checklist; check off per task |
| S8.1.1 | S8_1_1_Task_Atomization.md | Task atomization | Decomposing work into units safe for agent execution without over-fragmentation |
| S8.2 | S8_2_Parallel_Work_Coordination.md | Parallel work coordination | Git worktrees, branch isolation for concurrent features |
| S8.2.1 | S8_2_1_Cross_Team_Spec_Consistency.md | Cross-team spec consistency | No framework resolves conflicts between interdependent specs across teams |
| S8.3 | S8_3_Progress_Visibility.md | Progress visibility | Status dashboards, session recap, memory persistence |
| **S9** | S9_Quality_Assurance.md | **Quality Assurance** | Preventing drift, hallucination, and regression |
| S9.1 | S9_1_TDD_Integration.md | TDD integration | Red → Green → Refactor inside the SDD cycle |
| S9.1.1 | S9_1_1_Property_Based_Testing.md | Property-based testing for non-determinism | Property tests verify invariants regardless of implementation variation |
| S9.2 | S9_2_Spec_Drift_Prevention.md | Spec drift prevention | Keeping code and spec in sync over time |
| S9.2.1 | S9_2_1_Spec_Versioning_n_Rollback.md | Spec versioning & rollback | Git tracks changes but provides no semantic versioning; rollback policy undefined |
| S9.2.2 | S9_2_2_Spec_Rot_Under_Evolution.md | Spec rot under evolution | As codebases grow, specs silently become stale and actively mislead |
| S9.3 | S9_3_Hallucination_Safeguards.md | Hallucination safeguards | Verification agents, automated tests, human review |
| S9.3.1 | S9_3_1_False_Confidence_Trap.md | False confidence trap | Passing tests on a flawed spec gives false safety |
| S9.3.2 | S9_3_2_Agent_Autonomy_Without_Reliability.md | Agent autonomy without reliability | Agents reduce but don't eliminate drift; documented cases of false completion |
| **S10** | S10_Applicability.md | **Applicability** | When SDD helps vs. when to skip it |
| S10.1 | S10_1_Problem_Size_Suitability.md | Problem-size suitability | Multi-session, multi-service, compliance-required → use SDD |
| S10.1.1 | S10_1_1_Brownfield_Retrofit_Difficulty.md | Brownfield retrofit difficulty | Retrofitting SDD into existing codebases requires reverse-engineering specs |
| S10.2 | S10_2_Tradeoffs_and_Limitations.md | Trade-offs & limitations | Overhead on small/exploratory tasks; spec maintenance cost |
| S10.2.1 | S10_2_1_Adoption_ROI_Timeline.md | Adoption ROI timeline | Productivity dip before benefits accrue; breakeven threshold undocumented |
| S10.2.2 | S10_2_2_Cultural_Resistance.md | Cultural resistance | Developers prefer exploratory coding; implicit knowledge resists codification |

---

## Authoritative Sources for Topic Research

This section defines which sources are authoritative for SDD topic research in this project. All research agents must use these tiers when selecting and citing sources. The tier ranking reflects source quality, currency (2025–2026), and relevance to the SDD practice landscape.

### Tier 1 — Primary Sources

These sources are cited by name in the existing research files and have been validated as active, reputable, and publishing current SDD content in 2025–2026.

| Domain | Source | Why Primary |
|--------|--------|-------------|
| Academic | **arXiv** (arxiv.org) | Peer-reviewed preprints including arXiv:2602.00180 (the canonical SDD paper), 2601.03878, 2603.17399, 2603.25773, 2603.25697. Active SDD corpus as of Q1 2026. |
| Industry analysis | **Thoughtworks** (thoughtworks.com) | Named SDD as a key 2025 practice. Technology Radar entries (Nov 2025, Apr 2026). Blog post Dec 2025. Ongoing SDD commentary through 2026. |
| Practitioner synthesis | **Martin Fowler** (martinfowler.com) | Published multi-part SDD series covering Kiro, spec-kit, Tessl, and ubiquitous language. Authoritative voice for architecture and AI-assisted development. |
| Tooling / product | **Kiro** (kiro.dev) | The dedicated SDD IDE. Primary source for spec-as-source and requirements/design/tasks workflow. Official documentation and blog. |
| Open source tooling | **GitHub Blog** (github.blog) | Published the spec-kit announcement and SDD toolkit guide. Primary source for GitHub Spec Kit. |

### Tier 2 — Secondary Sources

Strong secondary sources: reputable, current, and used in the existing research files but with narrower or more derivative coverage than Tier 1.

| Domain | Source | Notes |
|--------|--------|-------|
| Developer news | **InfoQ** (infoq.com) | Published the Kiro launch article (Aug 2025). Good for tooling announcements and practitioner interviews. |
| AI tooling vendor | **Augment Code** (augmentcode.com) | Published a comprehensive SDD guide cited in S1.1 and S1.2. Vendor perspective; validate claims against Tier 1. |
| Learning platform | **O'Reilly** (oreilly.com) | Ran a dedicated live event on SDD with Claude Code (2025). Signals practitioner adoption and learning demand. |
| Critical analysis | **Marmelab** (marmelab.com) | Published a critical "Waterfall Strikes Back" analysis of SDD (Nov 2025). Useful for trade-offs and limitations topics. |

### Tier 3 — Tertiary Sources

Use for corroboration, practitioner anecdotes, or emerging tool coverage. Do not cite as primary authority. Validate against Tier 1 before including claims.

| Domain | Source | Notes |
|--------|--------|-------|
| Developer community | Medium, DEV Community, personal blogs | Used in existing research for practitioner anecdotes. High variability in quality. Always corroborate. |
| Vendor guides | SoftwareSeni, dplooy, XB Software, AltexSoft | Used in existing research. Marketing-adjacent; useful for basic definitions but not for authoritative claims. |
| JetBrains blog | blog.jetbrains.com | Junie team perspective on spec-driven approach. Useful for IDE integration topics. |
| GitHub repositories | github.com/github/spec-kit, github.com/gotalab/cc-sdd | Primary for understanding tool internals; not authoritative for SDD theory. |

### Search Protocol

When researching any SDD topic, apply the following sequence:

1. **arXiv first** — search `arxiv.org` for papers on the specific topic. Use search terms like `"spec-driven development"`, `"specification-driven"`, `"AI coding agent specifications"`. Prioritize papers from 2025–2026.
2. **Thoughtworks + Martin Fowler** — check for Technology Radar entries or blog posts. These provide practitioner-validated assessments.
3. **Kiro / GitHub Blog** — for tooling topics (S7.x), check the tool's own documentation and the GitHub Blog announcement.
4. **Tier 2 sources** — for context, announcements, and practitioner perspectives.
5. **Tier 3 sources** — only for corroboration or anecdote. Never as sole citation.

**Tool to use:** All web research must use Exa MCP (`mcp__exa__web_search_exa`) before falling back to raw `WebFetch`. Context7 applies only for library/SDK documentation, not for SDD topic research.

**Currency requirement:** Prefer sources dated 2025 or 2026. Sources older than 2024 are acceptable only for foundational concepts (BDD, TDD, OpenAPI) that predate the AI-assisted SDD wave.
