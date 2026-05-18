# S3.3.2 — Authority Ambiguity

**Status:** Researched  
**Predecessor(s) ID:** S3.3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; RACI/DACI frameworks, delegation of authority, approval governance, and AI agent authorization patterns documented |

---

## Overview

Authority ambiguity occurs when it is unclear who is empowered to approve a given phase, task, or change type. This is one of the most underestimated failure modes in SDD workflows.

In the context of verification and review gates (S3.3), authority ambiguity manifests as:

- "Who approves the planning phase — the product owner, the architect, or both?"
- "If automated gates pass and the code looks correct, can any engineer approve it, or does it need a senior engineer?"
- "Are schema changes approved by the database architect, the tech lead, or both?"
- "For security-sensitive changes (auth, encryption, data access), who has approval authority — the architect, the security team, or the product owner?"
- "In a distributed team, if the reviewer is unavailable, can someone else approve, or does it block?"

**Undefined approval authority is functionally equivalent to no approval authority.** When it is unclear who can approve something, gates get bypassed informally. People either:
- Approve changes they are not authorized to approve
- Avoid submitting for approval, hoping nobody notices
- Form ad-hoc approval committees that change composition per decision
- Escalate to leadership for routine decisions that should be delegated

This cascades into:
- **Authority drift:** Actual approval authority diverges from documented authority, creating audit exposure and risk
- **Bottlenecks:** Every decision goes to the same person because nobody knows who else can approve
- **Inconsistent outcomes:** The same type of change gets approved differently on different days
- **Compliance failures:** Regulators (SOX, DORA, NIS2, ISO 27001) require demonstrable accountability; vague authority structures fail audits

---

## The Problem: Why Authority Ambiguity Is Silent

Authority ambiguity is particularly insidious because:

1. **It is invisible until a crisis.** Approvals happen informally, then a compliance audit asks "who approved this and what was their authority?" The answer is discovered post-hoc.

2. **It is cultural, not technical.** No amount of process improvement or tooling fixes it if the organization does not explicitly assign decision rights. Automation enforces the rules you wrote; it cannot invent rules you did not write.

3. **It compounds at scale.** With one engineer, approvals are obvious (the tech lead). With 10 teams, 50 engineers, and distributed repositories, informal approval becomes impossible to track.

4. **It decouples responsibility from authority.** A reviewer may be listed as "accountable" in a RACI chart but have no formal authority to approve the decision. This creates the accountability-without-authority trap documented in governance research.

---

## Frameworks for Defining Authority

Three complementary frameworks exist for defining and structuring approval authority. All three are necessary; none is sufficient alone.

### Framework 1: Delegation of Authority (DoA)

**What it is:** A formal system that assigns decision rights and approval limits to specific roles or individuals. It defines who can commit the organization to a decision, under what thresholds, conditions, and with what evidence.

**In SDD context:** DoA specifies which roles can approve each phase or change type. It is the legal and governance foundation.

**Example:**

| Decision | Approval Authority | Threshold / Condition |
|----------|-------------------|----------------------|
| Planning phase (requirements + design) | Product Manager + Architect | Blocking decision; both must approve |
| Implementation task | Any software engineer | Non-blocking; engineer can self-approve if they authored the code, otherwise peer review |
| Test coverage increase | Team Lead | ≥90% coverage must be achieved before phase advances |
| Security-sensitive changes (auth, encryption, secrets) | Architect + Security Team | Both must approve; cannot be overridden |
| Schema changes | Database Architect + Architect | Both must approve; data migration impact review required |
| Third-party dependency additions | Tech Lead + Security | Both must approve; license and vulnerability scan required |

**Key principle:** Authority boundaries must be explicit. If a role is not listed, it does not have approval authority for that decision type.

**Where it maps to SDD:** DoA defines the structural authority. It answers "what role can make this call."

### Framework 2: RACI Matrix

**What it is:** Clarifies who is Responsible, Accountable, Consulted, and Informed for executing a process or task.

**In SDD context:** RACI maps to workflow steps (e.g., Planning → Implementation → Verification → Merge). Each step has named role assignments.

**Example (per workflow step):**

| Step | Responsible | Accountable | Consulted | Informed |
|------|-------------|-------------|-----------|----------|
| Requirements definition | Product Manager | Product Owner | Architect, Domain Experts | Team |
| Design review | Architect | Tech Lead | Security (if applicable), Database (if schema involved) | Implementation team |
| Implementation | Software Engineer | Code Owner (team member) | Tech Lead, Domain Expert | QA, Release Manager |
| Testing | QA Engineer | QA Lead | Security (if security-critical) | Architect |
| Code review | Any peer engineer | Code Owner | N/A | N/A |
| Merge approval | (Automated + human gate) | Tech Lead | N/A | Team |
| Deployment | Release Manager | Tech Lead | Security, Operations | All teams |

**Critical rule (from governance research):** There must be exactly one "A" (Accountable) per step. If accountability is shared, it is diluted. In regulatory terms, diluted accountability is no accountability.

**Where it maps to SDD:** RACI clarifies execution roles. It answers "who performs, oversees, and signs off on this step."

**Distinction from DoA:** RACI does NOT grant decision authority. A person can be "Accountable" in RACI but have no DoA authority to approve. This gap must be closed by explicitly naming the "Accountable" person as the decision-maker for that step.

### Framework 3: DACI (Decide, Approver, Contributors, Informed)

**What it is:** A decision-making framework that designates one "Approver" with final say. DACI is lighter than RACI — it focuses on decisions, not ongoing responsibilities.

**In SDD context:** DACI is useful for specific decision points where consensus is required but final authority must be clear.

**Example:**

| Decision | Driver | Approver | Contributors | Informed |
|----------|--------|----------|----------------|-----------|
| Architecture choice for new microservice | Tech Lead | Architect | All team leads, Security | Engineering leadership |
| Third-party tool evaluation | Engineer | PM | Tech Lead, relevant domain expert | Team |
| Rollback of a failed deployment | On-call engineer | CTO | Release Manager, affected services | Team leads |
| Exception to the coding standard | Engineer | Tech Lead | Requestor's manager | Code review team |

**Critical rule:** There is exactly one "Approver." Multiple approvers create veto dynamics. If multiple stakeholders must sign off, designate one as Approver and list others as Contributors whose input the Approver weighs.

**Where it maps to SDD:** DACI clarifies escalation and judgment calls. It answers "who makes the final call on this tradeoff or exception."

---

## Authority Ambiguity in SDD Review Gates

The S3.3 Verification / Review Gates section defines six automated gates and one human review gate. Authority ambiguity surfaces at the human review gate, where judgment and tradeoffs are involved.

### Planning Phase Approval

**Gate:** Planning gate (after requirements.md + design.md are complete, before task.md is finalized)

**Authority ambiguity:** Who approves the design?

**Possible answers (all wrong if not explicit):**
- "The product owner approved it"
- "We reviewed it as a team"
- "The architect signed off"
- "Everyone agreed"

**Correct answer (explicit DoA + RACI):**

```
DoA: Architecture design decisions are approved by [Architect].
RACI: Design step is:
  R: Designer (author)
  A: Architect (decision authority)
  C: Product Owner, Security (if applicable)
  I: Team
```

This means:
- The designer writes the design
- The architect is accountable and has final approval authority
- The product owner and security team provide input the architect weighs
- The rest of the team is informed

If the architect is unavailable, the decision either waits or escalates explicitly to the next tier (e.g., Tech Lead or CTO).

### Implementation Phase Approval

**Gate:** Per-task implementation micro-gate (after agent completes a task)

**Authority ambiguity:** Who approves the implementation — the agent, the team lead, a peer, the tech lead, or someone else?

**Correct answer (explicit DoA + DACI per task):**

```
DoA: Implementation tasks are approved by any qualified software engineer on the team.
  If the task touches security-critical code (auth, encryption), approval authority is the Architect + Security.
  If the task modifies the database schema, approval authority is the Database Architect.
DACI (for each task): 
  D: Task author / assigned engineer
  A: [Architect if security-critical; DBA if schema; otherwise Code Owner of the module]
  C: [Peer reviewer if complex]
  I: Team
```

This operationalizes trust in a graduated way:
- Simple feature tasks → any peer can approve
- Schema changes → DBA approves
- Security-sensitive → Architect approves
- Complex → multiple reviewers

### Verification Phase Approval

**Gate:** Final verification gate (after automated gates pass, before merge)

**Authority ambiguity:** Does the code reviewer approve the merge, or is there another approval step?

**Correct answer (explicit DoA):**

```
DoA: Merge authority is delegated as follows:
  - For protected branches (main, release branches): Tech Lead or Architect
  - For development branches: Code Owner of modified modules
  - Approval SLA: 2 business days for standard changes, 4 hours for hotfixes
Escalation: If approver unavailable, escalate to next tier (Architect → Tech Lead → Engineering Manager)
```

---

## Building an Approval Authority Matrix (Practical)

The standard approach is to create an **Approval Authority Matrix** — a reference table that operationalizes DoA into searchable rules.

### Structure

```markdown
# Approval Authority Matrix

## Standard Changes (Features, Bugfixes, Tests, Docs)

| Change Type | Approval Authority | Threshold | SLA | Escalation |
|-------------|-------------------|-----------|-----|------------|
| CRUD feature | Code Owner (module) | Automated gates must pass | 2 days | Tech Lead |
| Bugfix | Code Owner (module) | Automated gates must pass + regression test | 1 day | Tech Lead |
| Test addition | Any engineer | ≥1 engineer review | 1 day | N/A |
| Documentation | Any engineer | ≥1 review for technical docs | 1 day | N/A |
| Internal refactor | Code Owner (module) | Automated gates must pass + no test changes | 2 days | Tech Lead |

## Schema Changes

| Change Type | Approval Authority | Threshold | SLA | Notes |
|-------------|-------------------|-----------|-----|-------|
| Add column | Database Architect | + Data Engineer + Architect | 2 days | Migration plan required |
| Drop column | Database Architect | + Architect | 1 day | Backward compatibility check |
| Rename column | Database Architect | + Data Engineer | 2 days | Alias period required |
| New table | Database Architect | + Data Engineer + Architect | 3 days | Indexing review required |

## Security-Sensitive Changes

| Change Type | Approval Authority | Threshold | SLA | Escalation |
|-------------|-------------------|-----------|-----|------------|
| Authentication changes | Architect + Security Team | Both must approve | 1 day | CTO |
| Authorization changes | Architect + Security Team | Both must approve | 1 day | CTO |
| Encryption / secrets handling | Architect + Security Team | Both must approve | 1 day | CTO |
| Data access restrictions | Database Architect + Security Team | Both must approve | 1 day | CTO |
| Dependency with known CVE | Tech Lead + Security | Both must approve | 4 hours | CTO |

## Architectural Changes

| Change Type | Approval Authority | Threshold | SLA | Escalation |
|-------------|-------------------|-----------|-----|------------|
| New layer / component | Architect | + Tech Lead | 2 days | CTO |
| Technology change | Architect | + Tech Lead | 3 days | CTO + Engineering Manager |
| API contract change | Architect | + Code Owner (consuming module) | 2 days | CTO |

## Exceptions & Overrides

| Scenario | Decision Authority | Threshold | SLA |
|----------|-------------------|-----------|-----|
| Deadline override (approve without review) | Engineering Manager | + CTO | 1 hour |
| Security exception (allow known risk) | CTO + Security | Risk register entry required | 4 hours |
| Performance exception (accept slower change) | Architect | Technical rationale required | 2 hours |

```

### Key Principles

1. **Every decision type gets a named approver, not a committee.** If you cannot name one person, you do not have authority — you have a committee masquerading as authority.

2. **Authority is delegated, not implied.** If a role is not listed, it does not have approval authority. Silence is not consent.

3. **SLA is part of authority.** "Approves within 2 business days" is part of the delegated authority. If the SLA is breached, escalate to the next tier.

4. **Escalation is mechanical, not social.** If the approver is unavailable past the SLA, the decision escalates to the next listed authority (not to a meeting, not to the whole team).

5. **The matrix is enforceable in tooling.** Git branch protection, Jira automation, CI/CD policy-as-code — the matrix is the source of truth that these tools reference.

---

## Authority Ambiguity in Multi-Agent SDD

When multiple AI agents work in parallel (S5.2 — Parallel Agent Execution), authority ambiguity amplifies because agents do not have social negotiation skills.

### Problem: Agents Cannot Resolve Authority Gaps

Agent workflows fail silently when authority is unclear. An agent that cannot determine "who should approve this" does not escalate to a human — it either:
- Blocks the task
- Approves itself (violating separation of duties)
- Pushes the decision to a default authority who was not intended to handle it

### Solution: Constitutional Authority Clauses

The CLAUDE.md or equivalent constitution should include an explicit authority appendix:

```markdown
## Constitutional Authority Rules

1. **Approval authority is role-based, not person-based.** 
   Agents consult the Authority Matrix. If a role is listed, that role approves. 
   Agents never assume a person can approve; they always check the Authority Matrix first.

2. **Agents must verify approval authority before requesting approval.**
   Example: Before asking for approval of a schema change, agent checks the matrix:
   "This is a schema change. Authority Matrix says 'Database Architect + Architect.' 
   I need approval from both. Requesting now."

3. **If the approver is unavailable, escalate per the matrix.**
   Agents never block indefinitely. If SLA is breached, escalate to the next tier.
   Example: "Database Architect approval SLA (2 days) breached. Escalating to Tech Lead."

4. **Agents never self-approve.**
   Even if an agent wrote the code, an agent cannot approve it. 
   Approval always goes to a named human role.

5. **Agents log all approvals in the decision record.**
   Every approval must be timestamped, attributed to a person/role, and linked to the decision.
```

---

## Authority Ambiguity in Regulated Environments

Regulatory frameworks (SOX, DORA, NIS2, ISO 27001) explicitly require documented decision authority.

### DORA (Digital Operational Resilience Act)

**Article 5** requires the management body (typically the board or senior leadership) to maintain overall ICT risk oversight. Decision rights must be formally documented, with clear delegation chains and evidence of review.

**Implication for SDD:** Your Authority Matrix is an artifact that auditors will inspect. It must be:
- Version-controlled (stored in the repository alongside code)
- Dated (effective dates and review cycles)
- Signed off by leadership (board or executive sponsor)
- Demonstrated in practice (commit messages, PR approvals, audit logs match the matrix)

### ISO 27001 (Information Security Management)

**Clause 5.4** requires documented information security roles and responsibilities. Ambiguous authority is a control failure.

**Implication for SDD:** Security-sensitive approvals (auth, encryption, data access) require explicit authority assignment and must be evidenced in approval logs.

### SOX Section 302 (Financial Controls)

Requires management to certify the effectiveness of internal controls. Vague approval authority is a documented control weakness.

**Implication for SDD:** Code that affects financial systems, billing, or reporting must have explicit approval authority, signed off, and auditable.

---

## Anti-Patterns: What Authority Ambiguity Looks Like

### Anti-Pattern 1: The Default Approver

"Who approves this? I guess the tech lead will look at it."

**Problem:** The tech lead is not formally authorized; they are the default because nobody else was assigned. This is not authority; it is ad-hoc assignment.

### Anti-Pattern 2: The Committee

"Let's get feedback from the team and make a decision together."

**Problem:** Committees dilute accountability. "The team decided" is not a decision. A named person must have made the decision.

### Anti-Pattern 3: The Escalation Loop

A change blocks because the approver is unavailable. Nobody escalates; the change just waits.

**Problem:** Authority does not exist if there is no escalation path. Waiting indefinitely is equivalent to blocking forever.

### Anti-Pattern 4: Accountability Without Authority

"She is accountable for security in the code review, but she cannot approve it; the tech lead has to."

**Problem:** This creates the accountability-without-authority trap. The security reviewer is blamed if something goes wrong, but has no authority to prevent it.

**Solution:** Either the security reviewer has approval authority, or the tech lead has security accountability. One role, one authority.

### Anti-Pattern 5: The Assumption of Authority

"Since I wrote the test, I can approve the feature."

**Problem:** Agents and humans should never assume authority. Authority must be explicitly granted via the Authority Matrix.

---

## Resolving Authority Ambiguity: A Checklist

When establishing or auditing authority for an SDD workflow, verify:

- [ ] **Authority is explicit, not implicit.** Every decision type has a named approver in the Authority Matrix.
- [ ] **Authority matches accountability.** The "Accountable" role in RACI is the same role that has DoA authority for that decision.
- [ ] **Escalation is defined.** If the approver is unavailable, the next tier is named. SLA is specified.
- [ ] **Authority is enforceable in tooling.** Git branch protection, CI/CD policy-as-code, Jira automation reference the Authority Matrix.
- [ ] **Approval is logged.** Every approval is attributed to a person/role, timestamped, and linked to the decision.
- [ ] **Authority is reviewed periodically.** The Authority Matrix is revisited after reorganizations, role changes, or when approvals are consistently breached.
- [ ] **Authority is communicated.** Every team member and every agent knows the Authority Matrix. It is not a hidden document.
- [ ] **Authority is delegated clearly.** If a manager delegates authority to a direct report, the delegation is documented in the matrix with effective dates.

---

## Authority Ambiguity in MyVocaList SDD Context

For the MyVocaList project, authority ambiguity manifests in the planning and implementation phases (S3.1, S3.2).

**Current state (likely gaps):**
- Who approves the spec (requirements.md + design.md)? Helder (architect)? The community?
- Who approves a task from tasks.md before a subagent implements it? Helder only, or any qualified engineer?
- Who approves a schema migration? Helder as architect?
- Who approves a security-sensitive change (auth, encryption)?
- Who approves an urgent bugfix (bypass 2-day SLA)?

**Recommended Authority Matrix for MyVocaList:**

```markdown
## MyVocaList — Approval Authority

| Phase | Change Type | Approval Authority | SLA |
|-------|-------------|-------------------|-----|
| Planning | Spec (requirements + design) | Helder (Architect) | 3 days |
| Planning | Task refinement | Claude Code + Helder | 1 day |
| Implementation | Standard feature task | Claude Code (self-approval) + Helder (code review) | 2 days |
| Implementation | Schema/Database change | Helder (Architect) | 2 days |
| Implementation | Security-sensitive (auth, encryption) | Helder (Architect) | 1 day |
| Verification | Code merge to develop | Helder (code review + build gate) | 2 days |
| Verification | Merge to main | Helder (final approval) + CI pass | 1 day |
```

**Note:** Single-person approval authority (Helder) is valid for a small team. As the project grows, authority should be delegated to team leads or a rotating reviewer list.

---

## Sources

- [DOA vs Approval Matrix vs RACI: Key Differences — Aptly Resources (2026-04-18)](https://www.aptlydone.com/resource-articles/doa-vs-approval-matrix-vs-raci)
- [Decision Rights, Accountability, and Separation of Duties — COMPEL Framework (2026-01-01)](https://www.compelframework.org/articles/decision-rights-accountability-separation-of-duties)
- [Creating cross-functional RACI clarity to stop ownership gaps — FitGap (2026)](https://us.fitgap.com/stack-guides/creating-cross-functional-raci-clarity-to-stop-ownership-gaps-and-duplicate-work)
- [Delegation of Authority (DOA) 101 — Aptly Resources (2026-03-05)](https://www.aptlydone.com/resource-articles/delegation-of-authority-101)
- [DevSecOps RACI Matrix for Regulated Organizations — Regulated DevSecOps (2026-03-25)](https://regulated-devsecops.com/ci-cd-governance/devsecops-raci-matrix-regulated-organizations/)
- [The COMPEL Operating Model: Roles, RACI, and Decision Rights — COMPEL Framework (2026-01-01)](https://www.compelframework.org/articles/the-compel-operating-model-roles-and-decision-rights)
- [Delegation of Authority (DoA) Framework — Umbrex (2026-01-16)](https://umbrex.com/resources/frameworks/organization-frameworks/delegation-of-authority-doa-framework/)
- [DACI vs RACI — IdeaPlan (2026-03-04)](https://www.ideaplan.io/compare/daci-vs-raci)
- [AI Coding Agent Governance: Claude Code, Cursor, Devin — Cordum (2026-04-01)](https://cordum.io/blog/governing-coding-agents-control-plane)
- [Taming the Code Flood: Practical Governance for AI-Generated Pull Requests — digitalinsight.cloud (2026-04-16)](https://digitalinsight.cloud/taming-the-code-flood-practical-governance-for-ai-generated-)
- [AI Governance in Production (2026): Policy-First Control Plane — Cordum (2026-04-01)](https://cordum.io/blog/ai-governance)
- [AI Code Governance: What No Review Costs You — Q Services (2026-04-14)](https://www.qservicesit.com/what-happens-when-ai-writes-code-and-nobody-reviews-it)
- [Governing AI-Driven Software Change: Why the Control Plane Matters — XOPS360 (2026-02-01)](https://xops360.com/blog/tech/ai-governance-control-plane)
- [The Code You Didn't Authorize: Shadow tools and agentic risk — Medium (2026-02-12)](https://medium.com/@aiguruin/the-code-you-didnt-authorize-f21b53092292)
- [AI Governance for Engineering Teams: A Practical Playbook — AutonomyAI (2026-01-14)](https://autonomyai.io/technology/ai-governance-for-engineering-teams-a-practical-playbook-for-safe-fast-software-delivery/)
- [AI Coding Workflow — System Design One (2026-02-02)](https://newsletter.systemdesign.one/p/ai-coding-workflow)
