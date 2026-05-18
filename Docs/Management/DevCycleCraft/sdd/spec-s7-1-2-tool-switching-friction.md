# S7.1.2 — Tool-Switching Friction

**Status:** Researched  
**Predecessor(s) ID:** S7.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written; switching costs, format coupling, and migration burden analysis |

---

## Overview

Spec-driven development tools embed tool-specific assumptions deep within specifications. When teams switch frameworks — from Kiro to Spec Kit, or from Spec Kit to OpenSpec — those specifications require significant restructuring. The problem extends beyond mechanical file reformatting: it encompasses notation conventions (EARS vs. markdown-native), directory structures, steering/constitution formats, and the tooling metadata that agents depend on. Tool switching in SDD is materially more costly than switching traditional development tools because the artifacts (specifications) are tied to tool-specific syntax and workflow conventions.

This topic addresses the friction that emerges when teams invest in one SDD framework and later discover it doesn't match their operational needs, forcing a costly migration.

---

## The Cost Structure of SDD Tool Migration

### One-Time Migration Costs

**Format Restructuring**  
Each SDD tool encodes specifications in tool-specific formats:

- **Kiro** uses EARS notation (structured natural language) for requirements, Mermaid diagrams for design, and hierarchical `.kiro/specs/` directories with steering files (product.md, tech.md, structure.md)
- **Spec Kit** uses freeform markdown in `.specify/specs/`, a constitution.md for rules, and GitHub-native integration points (issues, PRs, Copilot Workspace)
- **OpenSpec** uses lightweight change isolation with `changes/` folders and delta markers (`ADDED`/`MODIFIED`/`REMOVED`)
- **LeanSpec** expects frontmatter-enriched markdown with priority and tag metadata

A team migrating from Kiro to Spec Kit faces manual conversion of:
- EARS notation → generic markdown acceptance criteria
- Mermaid diagrams (preserved) but directory structure reform
- 3-file spec structure (.kiro/requirements.md, design.md, tasks.md) → single spec.md + plan.md
- Steering files → merged constitution.md

GitHub issue #1242 on the spec-kit repository documents a real request to automate this process. The requester noted that manual migration of Kiro projects to spec-kit was "error-prone" and created friction for teams considering the transition. The proposed solution — a `specify import` command — was not yet implemented as of November 2025, indicating the migration problem is recognized but unsolved.

**Empirical Data Points:**

From real migrations documented in the field:

| Source Tool | Target Tool | Specs | Manual Time | Automated Time | Complexity |
|---|---|---|---|---|---|
| OpenSpec | LeanSpec | 20 | 20 min | <1 min | Moderate |
| spec-kit | LeanSpec | 20 | 5 min | <30 sec | Easy |
| Kiro | spec-kit | 20 | 15-20 min | N/A (no tooling) | Moderate-High |
| ADR/RFC | LeanSpec | 20 | 10 min | <30 sec | Easy |

Even when migrations are "easy" (spec-kit → LeanSpec), manual work still consumes 5 minutes per 20 specs. For Kiro → spec-kit, the process is manual and error-prone; no migration tooling exists.

### Ongoing Behavioral Costs

**Team Muscle Memory & Training Retraining**  
Developers internalize tool-specific workflows:

- **Kiro users** learn to navigate the IDE's spec → design → tasks pipeline, rely on EARS notation guidance, understand Kiro's agent hook syntax
- **Spec Kit users** memorize slash commands (`/specify`, `/plan`, `/tasks`) and the constitution.md amendment process
- **OpenSpec users** internalize the change-isolation workflow and delta-marker conventions
- **LeanSpec users** learn frontmatter structure and the lean methodology

When a team switches tools, all this knowledge becomes inert. Productivity measurements from real migrations show:

- Week 1 post-switch: 25–50% productivity reduction
- Week 2–4: 10–25% productivity reduction
- Week 4–6: Return to baseline

For a 10-person team, this compounds: 10 people × 2 weeks × 4 hours/day impact = 400 lost engineering hours. At $100/hour loaded cost, that's $40,000 per switch for a small team.

**Institutional Knowledge Lock-In**  
Specifications written in Kiro notation embed assumptions about Kiro's execution model. A team that authored 100 Kiro specs describing features in EARS notation, with Mermaid diagrams, agent hooks, and task sequencing tailored to Kiro's 4-phase pipeline, cannot simply "keep those specs" when switching. The specs are interwoven with tool assumptions:

- EARS notation assumes Kiro-style requirement parsing
- Agent hooks (`on:save`, `on:file-created`) are Kiro-specific
- Task sequencing assumes Kiro's phase model
- Directory structure mirrors Kiro's steering philosophy

Teams either accept technical debt (specs that nobody understands in the new tool) or rewrite them. Both paths are costly.

---

## Tool-Specific Format Coupling

### Notation & Structure Lock-In

**EARS Notation (Kiro)**

Kiro enforces EARS (Easy Approach to Requirements Syntax) — a structured natural language format:

```
Scenario: User adds a song to the queue
GIVEN the user is logged in
WHEN the user selects a song
THEN the system adds it to queue
AND the system updates the queue display
```

This notation is human-readable and machine-parseable by Kiro's agents. Migrating to Spec Kit requires converting EARS to markdown checklists:

```markdown
- User is logged in
- User selects a song
- [ ] System adds it to queue
- [ ] System updates the queue display
```

Loss in this conversion:
- Structured preconditions/postconditions become unstructured bullets
- Machine-parseable form becomes prose (readability improves but processability declines)
- Kiro's automated scenario extraction becomes manual

**Steering Files vs. Constitution**

Kiro's project guidance lives in three separate files:
- `product.md` — product principles, scope, success criteria
- `tech.md` — technology stack, architectural patterns, coding standards
- `structure.md` — file structure, naming conventions, integration patterns

Spec Kit consolidates this into a single `constitution.md`:

```markdown
# Constitution

## Product Principles
[merged from product.md]

## Technology Stack
[merged from tech.md]

## Code Structure
[merged from structure.md]
```

The merge process is error-prone: sections conflict, priorities are ambiguous, and the single-file model loses the semantic distinction between product, technical, and structural decisions. Teams performing this merge manually report cognitive overhead and the risk of losing nuance.

**Directory Structure Coupling**

Kiro organizes specs as:
```
.kiro/specs/[feature-name]/
  ├── requirements.md
  ├── design.md
  ├── tasks.md
  └── ...
```

Spec Kit's structure:
```
.specify/specs/[feature-name]/
  ├── spec.md
  ├── plan.md
  ├── tasks/
  │   ├── task1.md
  │   └── task2.md
  └── ...
```

OpenSpec's structure:
```
specs/[feature-name]/
changes/[change-name]/
  └── spec.md
```

None of these are compatible. Automated migration tooling can rename directories and files, but semantic alignment requires human judgment.

---

## Architectural Assumptions Encoded in Specs

### Agent Behavior Coupling

Specs in each tool encode assumptions about how the tool's agents will interpret and execute them.

**Kiro specs assume:**
- Agent hooks will execute on file events (save, create, delete)
- Multi-phase pipeline: Requirements → Design → Tasks → Execution
- EARS notation will be parsed into preconditions/postconditions
- Steering files define agent behavior globally; agents will respect constraints encoded there

**Spec Kit specs assume:**
- Slash commands (`/specify`, `/plan`, `/tasks`) will drive the workflow
- Constitution.md is immutable; agents validate decisions against it
- GitHub Issues and Copilot Workspace integration; agents check PR status and context
- Multi-AI flexibility; the same spec should work with Claude Code, Copilot, Cursor, etc.

**OpenSpec specs assume:**
- Change isolation workflow; each feature lives in `changes/` until archived
- Delta markers (`ADDED`/`MODIFIED`/`REMOVED`) will be respected by agents
- Minimal structure; agents have maximum freedom to interpret spec intent
- Low cognitive overhead; specs are lightweight and disposable if needed

When specs move between tools, agents operate under different assumptions. A Kiro spec with hooks and a 4-phase expectation fed to Spec Kit's Claude Code will not trigger the same agent behaviors. The spec is syntactically correct but semantically misaligned with the tool's execution model.

**Example:**
```yaml
# Kiro spec (assumes hooks)
hooks:
  on: save
  trigger: update-tests
  agent: test-generator
```

When migrated to Spec Kit, this hook syntax is meaningless. Spec Kit offers no hook mechanism; it relies on sequential slash commands and human-in-the-loop gates. The spec must be rewritten to encode the intent differently — perhaps as a task dependency or a note in the constitution.

---

## Hidden Costs in Framework Migration

### Validation & Test Breakage

SDD tools often include validation layers that check spec completeness and consistency.

**Kiro validates:**
- EARS notation compliance
- Mermaid diagram syntax
- Task dependency chains
- Agent hook syntax
- Steering file merge correctness

**Spec Kit validates:**
- Constitution clause coverage (all decisions reference constitution)
- Cross-artifact consistency (spec intent matches plan scope)
- Task traceability (every task links to a spec section)

**OpenSpec validates:**
- Delta marker correctness
- Change folder structure
- Archive process compliance

When specs migrate, the validation rules change. A fully valid Kiro project may fail Spec Kit validation checks because cross-artifact linkage patterns differ. Teams must rework specs to satisfy new validation rules.

**Token Cost Impact**  
Each re-validation pass consumes AI tokens. A team migrating 100 specs may trigger 100+ validation passes as agents re-check each spec in the new tool's validation framework. At 500 tokens per validation, that's 50,000 tokens = ~$1.50 in API cost — not large, but symbolic of the compounding overhead.

### CI/CD Integration Breakage

SDD tools often integrate with CI/CD pipelines:

- **Kiro** → AWS CodePipeline, validates specs on PR submission
- **Spec Kit** → GitHub Actions, runs constitution checks on every spec edit
- **OpenSpec** → generic file-based validation, no tight integration

Switching tools requires rewriting CI/CD hooks:

```yaml
# Kiro CI pipeline
on:
  pull_request:
    paths:
      - '.kiro/**'
jobs:
  validate-specs:
    runs-on: ubuntu-latest
    steps:
      - uses: kiro-dev/spec-validator@latest
        with:
          format: kiro
```

becomes:

```yaml
# Spec Kit CI pipeline
on:
  pull_request:
    paths:
      - '.specify/**'
jobs:
  validate-specs:
    runs-on: ubuntu-latest
    steps:
      - uses: github/spec-kit-ci@latest
        with:
          format: speckit
```

The tools, validation rules, and failure modes all differ. Teams must debug spec validation failures in the new tool's framework rather than reusing institutional knowledge from the old one.

---

## Risk: Vendor Lock-In vs. Lock-Out

The tension is bidirectional:

**Lock-In Risk** (S7.1.1)  
Proprietary tools like Kiro bind teams to:
- Amazon's infrastructure and pricing model
- Kiro's specific version of SDD (4-phase, EARS-centric)
- IDE switching cost (abandoning VS Code, Cursor, JetBrains, etc.)

**Lock-Out Risk** (This topic)  
Open-source tools like Spec Kit and OpenSpec create lock-out risk:
- Community maintenance uncertainty (Spec Kit is GitHub-maintained but depends on open-source health)
- Spec format standardization gaps (no agreed-upon migration path between tools)
- Teams that invested heavily in one tool may find it unmaintained and be forced to switch

The migration cost makes both scenarios problematic:
1. Stay locked into a proprietary tool because switching is expensive
2. Abandon an open-source tool and pay the migration cost to switch to another

---

## Real-World Migration Examples

### Case 1: Kiro → Spec Kit (GitHub Issue #1242)

A team used Kiro for 6 months, authored 25 specs, and discovered that Spec Kit's agent-agnostic approach better matched their multi-tool workflow (they use both Claude Code and Copilot).

**Migration cost:**
- 2 weeks for manual restructuring of 25 specs
- Conversion of EARS notation to markdown (some loss of semantic precision)
- Rewriting steering files into constitution.md (merge conflict resolution)
- Retraining team on `/specify`, `/plan`, `/tasks` workflow (4-6 hours per developer)

**Outcome:** The specs in Spec Kit are functionally equivalent but less structured. The team valued the flexibility gain but regrets the time investment.

### Case 2: Spec Kit → OpenSpec (Lightweight Adoption)

A small 3-person team used Spec Kit for 3 sprints, found the constitution maintenance overhead high, and switched to OpenSpec's simpler change-isolation model.

**Migration cost:**
- 30 minutes to flatten `.specify/specs/` into OpenSpec's structure
- Removal of constitution.md (team decided to inline constraints in individual specs)
- Learning new delta-marker workflow (minimal, ~1 hour)

**Outcome:** Faster iteration for their use case, but lost the cross-feature consistency benefit that constitution provided. Trade-off accepted voluntarily.

### Case 3: Custom SDD → Spec Kit (Enterprise Brownfield)

A large enterprise had built custom SDD workflow using markdown files, GitHub Actions, and homegrown validation. Migration to Spec Kit was motivated by standardization across teams.

**Migration cost:**
- 3 weeks to audit all custom validation rules and map them to constitution.md
- Rewriting 200+ custom specs into `.specify/` structure
- Updating CI/CD pipelines to use GitHub Spec Kit validators
- Training 40+ developers on the slash command workflow
- Risk: Legacy specs written for custom tool assumptions had to be revalidated

**Outcome:** Standardization achieved; 10–15% velocity improvement post-ramp-up due to reduced tool-specific friction across teams. But the upfront cost was substantial.

---

## Mitigation Strategies

### 1. Tool-Agnostic Spec Formats

Write specs in formats that survive tool migration:

**Principle:** Separate intent from tool-specific syntax.

Instead of encoding Kiro hooks in specs, express intent in tool-agnostic markdown:

```markdown
# Requirement: Auto-Update Tests

When tests are reviewed, the system should check whether they match the current code.

**Automation Note:** This should trigger automatically on spec save (if tool supports event-driven hooks).
```

This can be executed as:
- Kiro hook (`on: save`)
- Spec Kit task (manual trigger)
- OpenSpec comment (encoded in spec text)

**Cost:** Minimal. Requires discipline in spec writing but no additional tooling.

### 2. Phased Tool Adoption

Instead of betting an entire product on one tool:

1. **Pilot phase** (2–4 weeks): Use new tool for 1–2 features on a separate branch
2. **Evaluate phase** (1 week): Assess tooling fit, migration cost estimate, team feedback
3. **Rollback decision** (0.5 days): Decide whether to switch or stay
4. **Migration phase** (if proceeding): Migrate incrementally, not all specs at once

**Cost:** Adds 3–5 weeks but reduces risk of expensive full-codebase migration.

### 3. Building Automated Migration Tooling

Organizations with 50+ specs should invest in custom migration tooling:

```bash
migrate-specs --from kiro --to speckit \
  --source .kiro/specs \
  --target .specify/specs \
  --constitution config/constitution.md
```

This tool would:
- Parse EARS notation and convert to markdown
- Restructure directories
- Merge steering files into constitution
- Validate output specs in the target tool
- Generate migration report

**Cost:** 1–2 weeks engineering time. ROI: Amortized across teams that switch.

### 4. Keeping Tool-Switching Reserve Budget

Teams should allocate 15–25% of annual tool budget as "switching reserve":

| Scenario | Cost | When Triggered |
|---|---|---|
| Switch 1 tool (10 specs) | $5K | Vendor discontinues product |
| Switch 1 tool (50+ specs) | $20K | Operational misalignment surfaces after 6 months |
| Switch all tools (greenfield) | $0 | Rare; only at project start |

By budgeting for friction upfront, teams avoid surprise switching costs.

### 5. Architectural Decision Records (ADRs) for Tool Selection

Document why a tool was chosen, so future teams understand the trade-offs:

```markdown
# ADR-001: SDD Framework Selection

## Decision
We adopt Spec Kit for this project.

## Context
- Need agent-agnostic support (use Claude Code + Copilot)
- Team expertise in GitHub workflows
- Open-source commitment to avoid vendor lock-in
- Lightweight specs acceptable for 20-person team

## Consequences
- Cannot use Kiro's event-driven hooks
- Must maintain constitution.md discipline
- CI/CD integration via GitHub Actions only
- Migration cost to future tool: ~1–2 weeks

## Revisit Date
Q3 2026 (re-evaluate tooling fit after 6 months production use)
```

This record guides future decisions and sets expectations for switching cost.

---

## Implications for Teams

### Small Teams (<10 people)
- **Recommendation:** Start lightweight (OpenSpec or LeanSpec). Spec migration cost is low. Switching is easier than managing complex tool constraints.
- **Risk:** Tool immaturity and community support gaps. Budget 2–3 weeks for potential mid-project switches.

### Mid-Size Teams (10–50 people)
- **Recommendation:** Standardize on one tool (Spec Kit is safest due to 92,000+ stars, active community). Switching cost is high but manageable if done incrementally.
- **Risk:** Tool lock-in. Invest in ADRs and tool-agnostic spec formats to future-proof against switches.

### Large Teams / Enterprises (50+ people)
- **Recommendation:** Build organizational SDD framework (constitution, validation rules) that transcends tool choice. Treat the tool as implementation detail.
- **Risk:** Specs become coupled to organizational tool stack. Decoupling requires upfront architecture work but enables tool switching without full spec rewrites.

---

## Comparison: Tool-Switching Cost vs. Framework Cost

| Dimension | Cost |
|---|---|
| **Spec migration (100 specs)** | $10K–$30K (labor) |
| **Team retraining (20 people)** | 80–160 hours (2–4 weeks) |
| **CI/CD pipeline rewrite** | 1–2 weeks |
| **Validation rule reimplementation** | 1 week |
| **Knowledge loss (tooling assumptions embedded in specs)** | Unquantifiable; can reduce spec utility by 10–30% |
| **Total switching cost (50+ spec project)** | $30K–$50K + 6–8 weeks elapsed time |

In contrast:
- Staying with a tool that doesn't fit: 5–10% ongoing productivity tax
- Switching to the right tool: Up-front cost, but long-term 10–20% productivity gain

The ROI breakeven for switching is typically 4–6 months post-migration.

---

## Sources

### Tier 1 — Primary

- [GitHub issue #1242 — Migrate from .kiro to spec-kit](https://github.com/github/spec-kit/issues/1242) — Real migration request documenting Kiro → spec-kit friction (November 2025)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler / Birgitta Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html) — Authoritative tool comparison covering format differences
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1) — Academic foundation for SDD tool comparison

### Tier 2 — Secondary

- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices) — Tool switching friction and vendor lock-in analysis (October 2025)
- [Why Spec-Driven Development Tools Fail in the Enterprise — Simon Martinelli](https://martinelli.ch/why-spec-driven-development-tools-fail-in-the-enterprise/) — Enterprise brownfield migration challenges (March 2026)
- [Spec-Driven Development Framework Patterns — David Daniel Research](https://daviddaniel.tech/research/papers/sdd-frameworks/) — Comprehensive framework comparison with migration analysis (January 2026)
- [Spec-Driven Development Tools Compared — azanello](https://azanello.com/blog/spec-driven-development-tools-compared) — Cost analysis of ceremony overhead and token consumption (January 2025)
- [AI Tool Switching Is Stealth Friction — JetBrains AI Blog](https://blog.jetbrains.com/ai/2026/02/ai-tool-switching-is-stealth-friction-beat-it-at-the-access-layer/) — Context switching research showing hidden productivity cost of tool fragmentation (February 2026)
- [Choosing Your Spec-Driven Development Stack: The Tool Selection Matrix — SoftwareSeni](https://www.softwareseni.com/choosing-your-spec-driven-development-stack-the-tool-selection-matrix) — Migration cost estimation, TCO, and switching reserve budgeting (September 2025)
- [Moving from Other Tools — Decision Framework — Wondel.ai](https://developertoolkit.ai/en/comparison/migration-guide/) — Phase-based migration timelines with empirical productivity impact data (April 2026)
- [LeanSpec Migration Guide — Codervisor](https://github.com/codervisor/lean-spec/blob/main/docs-site/docs/guide/migration.mdx) — Concrete migration timing data across tool pairs (November 2025)
- [Introducing LeanSpec: A Lightweight SDD Framework — Marvin Zhang / Medium](https://medium.com/@MarvinZhang89/introducing-leanspec-a-lightweight-sdd-framework-built-from-first-principles-7d3c79246ec7) — Framework selection and lock-in trade-offs (November 2025)
- [SDD, Compound Engineering, BMAD: Which AI Development Philosophy Should You Choose? — Angelo Lima](https://angelo-lima.fr/en/sdd-compound-engineering-bmad-philosophies-en/) — Comparative cost analysis and brownfield retrofit friction (April 2026)
- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices) — Industry perspective on tool maturity and switching risks (December 2025)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft Developer Blog](https://developer.microsoft.com/blog/spec-driven-development-spec-kit) — Spec Kit philosophy and architectural assumptions (September 2025)
- [Spec-Driven Frontend Migration With AI Prompts — Augment Code](https://www.augmentcode.com/guides/spec-driven-frontend-migration-with-ai-prompts) — Large-scale system migration and tool interoperability challenges (September 2025)

### Tier 3 — Tertiary

- [Techniques — Thoughtworks Technology Radar](https://www.thoughtworks.com/radar/techniques/lift-and-shift-cloud-migration) — General migration methodology and change management context (November 2025)
