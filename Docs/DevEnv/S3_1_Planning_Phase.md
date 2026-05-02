# S3.1 — Planning Phase

**Status:** Researched
**Predecessor(s) ID:** S3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; sources from Tier 1 & 2 SDD sources (GitHub Spec Kit, Kiro, Martin Fowler, Thoughtworks, arXiv:2602.00180) |

---

## Overview

The Planning Phase is the foundational stage of the SDD cycle where human intent is converted into a structured, machine-consumable specification set. It is the phase in which humans do the most thinking and analysis; subsequent phases are intentionally designed to minimize human improvisation. All planning decisions must be reviewed and approved before implementation begins, because changes made to specifications take minutes while changes made after code is generated take hours or days.

The Planning Phase is not a one-time activity. It is iterative within a feature scope. The human and AI collaborate through multiple passes: the agent drafts proposals, the human reviews and refines them, and the human approves when the specification captures the intent with sufficient precision and completeness.

---

## Three-Document Structure

The Planning Phase converges on a consistent three-document output across all major SDD tooling (GitHub Spec Kit, Amazon Kiro, Tessl, cc-sdd, and MyVocaList):

| Document | Content | Audience |
|----------|---------|----------|
| **requirements.md** (or spec.md) | User stories, acceptance criteria, edge cases, validation rules, success metrics, non-goals, constraints | Product, QA, stakeholders |
| **design.md** (or plan.md / technical-spec.md) | Architecture decisions, component structure, data model, technology choices, integration approaches, key trade-offs | Engineering, architects |
| **tasks.md** | Ordered, checkboxed implementation tasks derived from design; each atomic, independently executable, with dependencies marked and file paths specified | AI agents, developers |

### The separation of concerns

This three-document structure enforces a crucial separation:

- **Requirements capture *what* and *why*.** User stories answer: what does the user need? Why does this feature exist? What behaviors must the system exhibit? Requirements are free of implementation detail.
- **Design captures *how*.** Technical decisions answer: which libraries? What data model? How do components interact? Design translates requirements into architectural choices.
- **Tasks capture *in what order*.** Implementation tasks answer: what is the first step, second, and third? What must complete before the next task can begin? Tasks break design into atomic, verifiable work units.

Mixing these three layers into a single document is a common source of spec ambiguity that leads to agent drift. Separation enables human reviewers to focus their attention correctly: product owners review requirements; architects review design; project managers review task order.

---

## The Planning Gate — The Critical Checkpoint

Before implementation begins, all three documents go through a structured human review gate. This is the highest-leverage decision point in the SDD cycle. The review follows a strict order:

### 1. Feature Specification Review
**File:** `requirements.md` (or `spec.md`)

Check:
- Are user stories understandable to someone who wasn't in the original conversation?
- Are acceptance criteria measurable, binary, and testable? (Not "should be fast" but "p95 latency < 200ms")
- Are edge cases explicit? (What happens on network failure? When input is invalid? When a race condition occurs?)
- Are out-of-scope items clearly marked and reasoned? (What are we *not* doing, and why?)
- Are timelines and estimates realistic?
- Do success metrics align with the original business intent?

### 2. Technical Decisions Review
**File:** `design.md` (or `plan.md`)

Check:
- Are library and framework choices appropriate for the constraints?
- Are performance targets achievable given the chosen stack?
- Are security measures adequate for the threat model?
- Does the proposed architecture avoid known anti-patterns?
- Are integration points with existing systems well-defined?
- Are there hidden dependencies or coupling that would surprise implementation?
- Is the data model correct and sufficient?

### 3. Database / Schema Review (if applicable)
**File:** `design.md` or separate `data-model.md`

Check (critical, as schema changes affect existing data):
- Are indexes sufficient for the access patterns?
- Is the migration approach safe — non-breaking, no data loss, reversible?
- Are new tables actually needed, or can existing schema be reused?
- Are column types correct and sufficient?
- Are constraints (primary, foreign, unique) appropriate?

### 4. Scope Review
**File:** All three documents together

Check:
- Is scope creep present? (Are new features sneaking in?)
- Are the three documents internally consistent? (Design implements requirements; tasks implement design)
- Are core requirements covered? (No essential features missing?)
- Are dependencies on external teams clearly marked?
- Is the task list in a realistic order? (No blocked tasks at the start?)

### 5. Constitution/Governance Check
**File:** Project `CLAUDE.md`, `.claude/rules/`, `constitution.md` (if exists)

Check:
- Does the spec violate any non-negotiable project rules?
- Are language, naming, and architectural constraints respected?
- Are platform and compliance requirements satisfied?

**Gate outcome:** Once all five reviews pass, the specification is **approved for implementation**. No implementation begins until this gate is signed off. If review identifies gaps, the specification is updated and re-reviewed (not implementation-and-refactor).

---

## The Planning Workflow

### Phase 1: Human drafts initial brief
A human (product owner or engineer) writes a high-level narrative: what problem are we solving, who is the user, what is the core value? This may be 1-2 paragraphs or a bullet list.

### Phase 2: AI agent drafts specification suite
If using AI-assisted planning (Kiro, GitHub Spec Kit, or similar), the AI agent takes the brief and produces:
- Initial requirements.md with user stories and acceptance criteria
- Initial design.md with architecture and technology choices
- Initial tasks.md with task breakdown and dependencies

If manual planning, the human writes these directly.

**Key insight:** Letting the AI draft is faster than pure manual specification. The AI surfaces edge cases and dependencies that humans often miss. But the drafts are proposals, not gospel — every artifact is subject to human review before proceeding.

### Phase 3: Human reviews and refines
The human reads each artifact in order (requirements → design → tasks). For each:
- Challenge assumptions ("Why OAuth2 instead of JWT?" → updates design.md with reasoning)
- Add missing edge cases ("What if the API is down?" → adds requirement)
- Reorder tasks if dependencies are wrong ("Task B blocks Task A" → reorder)
- Tighten acceptance criteria ("The system should be fast" → "p95 latency must be < 200ms")

Each refinement is a direct edit to the markdown files. No separate request queue — the reviewer and the spec are the same feedback loop.

### Phase 4: AI agent iterates (optional)
If the human's edits raised architectural questions or the spec is incomplete, the AI agent can read the updated specs and propose follow-up artifacts:
- Performance analysis (if latency was questioned)
- Dependency analysis (if coupling was unclear)
- Integration contracts (if external APIs were mentioned)

This is an optional loop. The human has final authority.

### Phase 5: Approval
Once all five review areas pass, the human approves:
- `requirements.md` is approved
- `design.md` is approved
- `tasks.md` is approved

**Approval means:** The implementation agent is authorized to execute. The code should match this specification, not improve on it, not reinterpret it. If the implementation discovers a spec gap, the spec is updated first, then the implementation is adjusted.

---

## Planning and Implementation Relationship

A common misconception is that planning is complete before implementation. In practice, planning and implementation are tightly coupled:

- **Planning may discover implementation constraints.** An architect realizes during design that a chosen library doesn't support the required access patterns. The design is revised before implementation.
- **Implementation may discover planning gaps.** A developer finds an edge case the spec didn't mention. The spec is updated; the affected tasks are re-planned; then implementation resumes.

The key rule: **the spec changes before the code changes**. When implementation reveals a spec deficiency, the response is to update the specification, re-review the change, and then re-implement the affected tasks. Never patch the code without updating the spec — that is how spec drift begins.

---

## Planning Sub-Topics and Risks

### S3.1.1 — Architecture Debt from Early Decisions

The Planning Phase locks architectural choices (library selection, data model shape, integration approach) before the most informative evidence exists: working code. If a planning-phase decision is discovered to be wrong during implementation — a library doesn't support a required feature, or the chosen stack has a fatal performance limitation — reversing that decision requires:

1. Updating the design document
2. Re-reviewing the change with stakeholders
3. Regenerating the task list
4. Re-implementing affected tasks (or rolling back and starting over)

The risk is not that planning produces bad decisions. The risk is that planning *locks in* decisions before evidence is available, making reversal expensive.

**Mitigation strategies:**
- Make reversible choices during planning (prefer modular architecture)
- Identify high-risk decisions early and validate them quickly during a pre-implementation spike
- Build contingency margin into task scheduling if a key decision is uncertain
- Document the reasoning behind architectural choices so drift is visible

### S3.1.2 — Dependency Analysis Incompleteness

A planning-phase dependency graph (which tasks block which other tasks) is necessarily incomplete because full coupling is only visible in the implementation. A task list may declare Task B depends on Task A, but hidden coupling emerges during coding:
- Task C depends on a database migration that both A and B assume
- Task D requires a configuration constant that Task A creates
- Task E uses a utility function that Task B implements

When hidden dependencies surface, the task list must be re-sequenced, and earlier tasks may need reversion or rework.

**Mitigation strategies:**
- Use parallel markers `[P]` conservatively; assume sequential unless absolutely certain
- Review the task list with architects who understand the codebase deeply
- During initial task execution, watch for unexpected blocking and update the list immediately
- Prefer fine-grained tasks over coarse ones — smaller tasks have fewer hidden dependencies
- Document assumptions about which tasks are truly independent

---

## Planning Artifacts as Living Documents

Specifications are not write-once. A mature SDD practice treats planning artifacts as living documents:

- **Requirements change when business priorities shift.** Update requirements.md, re-review, and update the affected tasks.
- **Design changes when implementation discovers a better approach.** Update design.md, re-review the impact, and update tasks.
- **Tasks are reordered when dependencies become clear.** No approval gate required for task reordering alone (it's within the approved design scope), but reordering must be logged.

The spec repository (git, file system, wiki) becomes the audit trail. Reviewers can see what was planned, why it changed, and what was approved at each gate.

---

## Tools and Automation

### GitHub Spec Kit
Provides slash commands (`/speckit.specify`, `/speckit.plan`, `/speckit.tasks`) that guide the user through planning interactively. Each command produces markdown artifacts that are committed to the repository. The `/speckit.plan` command generates a planning checklist that mirrors the review gate structure described above.

### Amazon Kiro
A dedicated SDD IDE (VS Code fork) that walks users through requirements, design, and tasks generation within an integrated environment. Kiro emphasizes structured capture and iterative refinement, preventing the AI from proposing code before planning is complete.

### Tessl
Treats the spec as the primary maintained artifact. Code is continuously regenerated from the spec. Useful for projects where spec-as-source is the explicit goal.

### Sudocode
A workflow platform that structures planning as a co-creation loop: humans and agents iterate on specs, agents propose designs, agents create tasks, and the system tracks feedback anchored to specific spec lines.

### BMAD / OpenSpec
Lightweight, filesystem-based approaches to planning. Specs are plain markdown in `.bmad/`, `.openspec/`, or `.specify/` directories. No proprietary tooling required; works with any editor and any AI agent.

---

## When Planning Is Sufficient

Not every change needs all three documents. The "golden rule" of planning rigor:

**Use the minimum level of specification rigor that removes ambiguity for your context.**

- **Bug fix:** Often a single `fix.md` describing the issue, the expected behavior, and test cases.
- **Small feature:** Two documents: `requirements.md` (user story + acceptance criteria) and `design.md` (which files change, why).
- **Large feature or architectural change:** All three: `requirements.md`, `design.md`, `tasks.md`.

---

## Planning as Team Communication

Planning artifacts serve a secondary purpose beyond code guidance: they are team communication. A well-written spec answers:
- "Why are we building this?" (requirements.md)
- "Why did we choose this approach?" (design.md with decision reasoning)
- "What needs to happen in what order?" (tasks.md)

New team members onboard faster when they read the spec before the code. Reviewers and stakeholders align around the spec before diverging interpretations develop. This communication value is separate from and as important as the code-generation value.

---

## Common Planning Mistakes

| Mistake | Consequence | Fix |
|---------|-----------|-----|
| Mixing requirements and design in one document | Reviewers can't distinguish intent from implementation choice | Separate into two documents; keep "what" and "how" distinct |
| Over-specifying implementation details | Spec becomes pseudo-code; constrains the agent; adds maintenance burden | Write acceptance criteria, not implementation steps |
| Leaving edge cases implicit | Agent invents behavior; spec-code mismatch | Write edge cases explicitly ("What if the API times out?") |
| Not ordering tasks by dependency | Parallel execution fails; blocked tasks stall the implementation | Use dependency markers; review order with architect |
| Skipping the planning review gate | Spec flaws are discovered too late, during implementation | Make the gate mandatory; invest time in planning review |
| Failing to update spec when implementation finds a gap | Spec becomes documentation; code is the real truth | Update spec first, then re-plan affected tasks |
| Too much detail too early | Spec becomes a novel; reviewers lose focus | Write a skeleton spec; iterate through review cycles |
| Too little detail | Spec is ambiguous; agent guesses; implementation drifts | Add examples, edge cases, acceptance criteria until unambiguous |

---

## Sources

- [Spec-Driven Development Planning Phase Review — Mark Valdez (2024-12-25)](https://valdezm.com/ai-engineering/spec-driven-development)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180 (2026-02-02)](https://arxiv.org/pdf/2602.00180)
- [Templates - GitHub Spec Kit (Mintlify docs)](https://www.mintlify.com/github/spec-kit/advanced/templates)
- [The SDD Workflow — GitHub Spec-Kit (Mintlify docs)](https://www.mintlify.com/github/spec-kit/concepts/workflow)
- [Spec-Driven Development with SpecKit — Atal Upadhyay (2025-12-12)](https://atalupadhyay.wordpress.com/2025/12/12/spec-driven-development-with-speckit/)
- [Complete Guide to Spec-Driven Development (SDD) with AI in 2026 — oshy.tech (2026-01-27)](https://oshy.tech/en/blog/spec-driven-development-ia/)
- [Spec-Driven Development Made Easy: A Practical Guide with OpenSpec — Ali Irz](https://aliirz.com/getting-started-with-sdd)
- [Specifica — A markdown format that makes AI collaboration natural (2024-01-01)](https://specifica.org/)
- [Spec-Driven Development (2026 Guide): Build Production AI Code — Product Builder (2025-10-15)](https://www.productbuilder.net/learn/spec-driven-development)
- [The SDD Playbook: Build Reliable Features With AI — ZenCoder AI (2026-01-21)](https://zencoder.ai/blog/the-sdd-playbook-build-reliable-features-with-ai)
- [Spec-Driven Development (SDD): The AI Engineering Method — ZenCoder AI (2025-12-29)](https://zencoder.ai/blog/spec-driven-development-sdd-the-engineering-method-ai-needed)
- [What Is Spec-Driven Development? — sdd.sh (2026-03-21)](https://sdd.sh/2026/03/what-is-spec-driven-development/)
- [Spec-Driven Development (SDD): A Technical Deep Dive into the Methodologies Reshaping AI-Assisted Engineering — Rushi's (2026-03-25)](https://www.rushis.com/spec-driven-development-sdd-a-technical-deep-dive-into-the-methodologies-reshaping-ai-assisted-engineering/)
- [Spec-Driven Workflow — shep-ai/shep (GitHub)](https://github.com/shep-ai/shep/blob/main/docs/development/spec-driven-workflow.md)
- [Spec-Driven Development — sudocode (Sudocode Docs)](https://docs.sudocode.ai/examples/spec-driven-development)
