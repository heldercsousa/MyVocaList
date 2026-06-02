# S6.1.2 — Amendment Governance

**Status:** Researched  
**Predecessor(s) ID:** S6.1

## Changelog

| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent from 10 authoritative sources |

---

## Overview

Constitutional constraints are only valuable if they can evolve without either (a) becoming obstacles when they are wrong, or (b) drifting informally without governance. Amendment governance is the formal process that allows constitutional rules to change while maintaining their binding nature and preventing silent reinterpretation.

The amendment process answers four critical questions:

1. **Who can propose changes?** Any agent? Only architects? Only humans?
2. **Who decides whether to accept?** Consensus? Technical authority? Project leadership?
3. **What is the process?** Documented or ad-hoc? Ratification, pilot, rollback?
4. **How is it versioned and tracked?** Git commit with justification? Changelog? Cryptographic proof?

Without a formal amendment process, constitutional constraints devolve into optional guidelines — because agents will work around rules they perceive as wrong, teams will deprioritize updating rules out of convenience, and silent drift becomes indistinguishable from intentional evolution.

---

## Core Principles

### 1. Amendments Are Rare But Necessary

A constitution that never changes becomes a dead letter. A constitution that changes without process becomes arbitrary. The balance is achieved by:

- **High barrier to amendment** — Proposed changes require documented justification, not just preference.
- **Formal review** — Technical authority (usually the Tech Lead, Principal Engineer, or Architect) must review against the project's architectural principles.
- **Recorded decision** — Every amendment is recorded with its rationale, effective date, and transition plan.

### 2. Visibility Over Silence

The worst amendment governance is none. The second worst is silent. A formal amendment process makes governance visible — pulling the rule change through version control, pull requests, code review, and changelog entries.

**Rule:** Every amendment must be:
- Documented in a Git commit with clear rationale
- Cross-referenced in `CHANGELOG.md` or `.claude/amendment-log.md`
- Reviewed by appropriate authority (see Authority Matrix below)
- Announced to affected parties (team, agents, stakeholders)

### 3. Immutability of Amendment Record

Once an amendment is ratified and documented, its prior state cannot be retroactively erased. The history of the rule — what it was, why it changed, when it changed — is part of the governance record.

**Mechanisms:**
- Git commit hash as immutable record
- `amendment-log.md` with timestamp and signature (if possible)
- Cryptographic chain (advanced: AOS Constitution, Article 11 AI use SHA-256 chaining)

---

## Amendment Authority Matrix

Who can change what, and under what conditions:

| Rule Type | Current Authority | Proposal Gate | Review Required | Pilot Period | Retroactive Application |
|-----------|-------------------|---------------|--------------------|------------|--------|
| **Architecture Principles** (e.g., "Services depend only on Domain interfaces") | Tech Lead / Principal Architect | RFC + technical justification | 2 Architects + Tech Lead | 1 sprint (new code only) | After pilot + unanimous approval |
| **Technology Constraints** (e.g., "Target platform is .NET MAUI 10") | Tech Lead | RFC + cost-benefit analysis | CTO / Principal Architect | N/A (breaks existing code) | Requires migration plan |
| **Code Quality Standards** (e.g., "Async methods use CancellationToken") | Tech Lead | Feature branch + linter rule | 1 Senior Engineer | N/A (linter enforces) | Via pre-commit hook |
| **Security Requirements** (e.g., "All input validated at boundary") | Security Lead / CTO | Security review + threat model | Security Lead + Tech Lead + Legal | None (must apply everywhere) | Immediate |
| **Workflow Rules** (e.g., "All commits reference a spec") | Tech Lead / Team Lead | Discussion in team sync | Team Lead + Tech Lead | N/A (policy enforcement) | Via hooks + training |
| **CLAUDE.md / AGENTS.md** | Whole team + Tech Lead | Pull request + discussion | 1 reviewer (peer review) | N/A | Immediate on merge |

### Proposal Gate
How a proposed amendment enters the system.

**RFC (Request for Change):**
- For Architecture, Technology, Security constraints: open an RFC in `.claude/amendments/`
- For Code Quality: propose via feature branch with linter rule + examples
- For Workflow: discuss in team sync and document decision

**Pull Request:**
- For CLAUDE.md: normal PR with justification in description
- For rules files: PR to `.claude/rules/` with before/after examples

### Review Required
Who must approve the amendment.

**2 Architects:** For changes to foundational architectural principles. The second reviewer provides adversarial review — looking for unintended consequences, edge cases, and consistency with other rules.

**1 Senior Engineer + Tech Lead:** For code quality standards. The senior engineer verifies that the new rule is enforceable and that existing code can be migrated without catastrophic refactoring.

**Security Lead + Tech Lead:** For security requirements. Changes to input validation, secrets handling, or threat models require security sign-off.

### Pilot Period
Whether the amendment applies to new code only before full adoption.

**1 sprint (new code only):** For architecture changes. New features must follow the amended rule; old code gets a grace period. This reduces migration risk and allows the team to discover edge cases before retroactive application.

**None:** For security or workflow rules. These apply everywhere immediately because they cannot be selectively enforced.

---

## Amendment Process — Detailed Workflow

### Phase 1: Proposal

**Trigger:** A constitutional constraint is identified as wrong or obsolete.

**Example triggers:**
- A feature requirement cannot be implemented under the current constraint
- New tool / library makes the constraint outdated
- Performance or security issue surfaces that contradicts the rule
- Team feedback that the rule causes friction without clear benefit

**Proposal document (RFC):** Create `.claude/amendments/YYYYMMDD_<constraint_name>.md`

```markdown
# Amendment Proposal: [Constraint Name]

## Current Rule
[Quote the exact rule from CLAUDE.md or rules file]

## Problem
[Specific, documented problem the current rule creates]
- Concrete example of violation attempt / blocked work
- Impact assessment (velocity cost, code quality cost, security risk)

## Proposed Amendment
[New rule text, as it will appear in CLAUDE.md]

## Rationale
[Why the new rule is better]
- What does it enable?
- What safety or consistency is maintained?
- How does it align with existing architecture?

## Backward Compatibility
[Impact on existing code]
- Which files / areas will be affected?
- Is the change additive or breaking?
- Migration plan (if any)

## Authority Required
[Who must review]
- Tech Lead: yes / no
- 2 Architects: yes / no
- Security Lead: yes / no

## References
[Prior art, external sources, related constraints]
```

### Phase 2: Review

**Duration:** 1 business week (minimum). Allows interested parties to raise concerns asynchronously.

**Reviewers** (per Authority Matrix):
- Tech Lead: always
- +1 Architect: if architecture principle
- +1 Security Lead: if security-related
- +1 Senior Engineer: if code quality

**Review checklist:**

- [ ] Is the problem clearly documented and real?
- [ ] Does the new rule solve the problem without creating new constraints?
- [ ] Is the rule enforceable (mechanically or through review)?
- [ ] Does it conflict with other existing rules?
- [ ] Is the amendment scope (who can change, under what conditions) appropriate?
- [ ] Have edge cases been considered?
- [ ] Is backward compatibility addressed?

**Approval gate:** All required reviewers sign off (+1) OR consensus decision by Tech Lead if disagreement exists.

### Phase 3: Pilot (if applicable)

**Applies to:** Architecture and code quality changes. Not security or workflow rules.

**Duration:** 1 sprint (7–14 days typical)

**Scope:** New code only. Existing code is exempt during pilot.

**Verification:**
- All new files/commits follow the amended rule
- No working-around of the rule is discovered
- The rule is clear and unambiguous as written
- No unforeseen edge cases emerge

**Go/No-Go Decision:**
- **Go:** Approved for retroactive application
- **No-Go:** Rule is revised, pilot repeats, or amendment is rejected

**Failure mode:** If pilot reveals the rule is unworkable, roll back to the old rule and return to Phase 2 with revised proposal.

### Phase 4: Ratification

**Action:** Commit the amendment to the codebase.

**Steps:**

1. Update `CLAUDE.md` or `.claude/rules/*.md` with the new rule
2. Create `.claude/amendment-log.md` entry (or append to existing)
3. Update `.claude/amendments/YYYYMMDD_*.md` with status = "Ratified"
4. Create a Git commit with clear message:
   ```
   amend: <constraint_name> — <brief rationale>

   Ratified: <date>
   RFC: .claude/amendments/YYYYMMDD_<constraint_name>.md
   Authority: Tech Lead + Architect review
   Effective: <date>
   
   Old rule: <exact quote>
   New rule: <exact quote>
   
   Backward compatibility: [description]
   ```

5. Update `CHANGELOG.md` with amendment entry
6. Announce the change (team slack, email, meeting)

### Phase 5: Retroactive Application (if applicable)

**Applies to:** Architecture and code quality changes after successful pilot.

**Process:**
- File a follow-up issue: `[Amendment] Retroactive application of <rule> to existing code`
- Create a tracking PR / task to update existing files
- Stagger changes across PRs to avoid merge conflicts and review fatigue
- Update related rules/documentation to reflect the amended constraint

**Timeline:** 2–4 weeks typical, depending on codebase size.

---

## Amendment Log Format

Create or maintain `.claude/amendment-log.md` to track all amendments:

```markdown
# Amendment Log

| Date | Constraint | Status | Authority | RFC | Pilot End | Ratified | Notes |
|------|-----------|--------|-----------|-----|-----------|----------|-------|
| 2026-05-15 | Architecture: Service Layer Isolation | Ratified | Tech Lead + 2 Arch | RFC-001 | 2026-05-22 | 2026-05-25 | Relaxed Infra imports for migration helpers |
| 2026-06-10 | Code Quality: Test naming | Ratified | Tech Lead | PR #4521 | N/A | 2026-06-10 | Added pattern: `{Method}_Given{Context}_Then{Expected}` |
| 2026-06-15 | Security: Secrets in logs | Rejected | Security Lead + Tech | RFC-003 | N/A | N/A | Withdrawn: linter handles this case |
```

---

## Restrictions on Amendment

### 1. Unamendable Core (if applicable)

In some projects or organizations, certain rules are immutable. Examples:

- **Human primacy** (AOS Constitution): "AI cannot override human decisions on existential matters"
- **Architectural invariant** (MyVocaList): "Services depend only on Domain interfaces" — too foundational to change
- **Compliance requirement** (fintech, healthcare): Rules mandated by regulation cannot be amended without legal review

**Decision:** Project leadership + Tech Lead decide which rules are unamendable at project inception.

For MyVocaList, consider the following as candidates for "immutable unless extraordinary circumstances":
- "Business logic lives in Services only"
- "Never use DisplayAlert for dialogs" (security + UX consistency)
- "Repository interfaces in Domain, implementations in Infra"

### 2. Amendment Cannot Weaken (usually)

A new rule cannot be weaker than the prior rule unless:

- The old rule is proven to be actively harmful (blocks work, no clear benefit)
- A new constraint elsewhere compensates (e.g., linter replaces manual rule)
- Retroactive enforcement is impossible (legacy code grandfathered in)

**Example:** You cannot amend "All methods have XML docs" to "All public methods have XML docs" without establishing an alternative enforcement mechanism for internal methods.

### 3. Amendment Requires Consensus on Rationale

Amendments cannot be "because we feel like it." There must be a documented problem and a clear benefit to the change.

---

## Inter-Project Amendment Propagation

If a project uses a shared constitution (e.g., in a monorepo or across related services), amendments must propagate:

**Rule:** Any amendment to a shared rule must be synchronized across all projects within 2 weeks.

**Process:**
1. Tech Lead opens an RFC in shared infrastructure
2. All affected project leads review
3. Single amendment is committed once
4. All projects use the amended rule

**Fallback:** If synchronization is impossible, a project can adopt an override rule in its own `CLAUDE.md` + amendment-log, documented as "divergence exception".

---

## Common Failure Modes

### 1. Silent Rule Changes

**Failure:** CLAUDE.md is edited without an RFC, without a commit message, without announcement.

**Prevention:** Pre-commit hook that requires `.claude/amendments/` entry for any change to CLAUDE.md. Enforce via:
```bash
# In .claude/hooks/pre-commit
if git diff --name-only | grep -q CLAUDE.md; then
    # Check for corresponding amendment RFC
    ls .claude/amendments/ | grep -q "$(date +%Y%m%d)" || {
        echo "Error: CLAUDE.md changed without amendment RFC"
        exit 1
    }
fi
```

### 2. Pilot Period Skipped

**Failure:** Rule is declared "obvious" and applied immediately to all code, breaking existing patterns.

**Prevention:** Enforce pilot period in checklist. Code review template includes "Pilot period satisfied? Y/N".

### 3. Amendment Never Completes

**Failure:** Rule is approved, pilot succeeds, but retroactive application is never completed. Code partially follows old rule, partially follows new rule.

**Prevention:** Create a follow-up issue in `.claude/amendments/` marked "post-pilot cleanup". Tech Lead verifies completion before closing the amendment.

### 4. Rationale Lost

**Failure:** Amendment is committed, but the RFC / rationale is deleted or forgotten. Six months later, nobody knows why the rule changed.

**Prevention:** RFC documents are never deleted. Amendment-log entry preserves link to RFC. Git commit references amendment clearly.

---

## Amendment Authority — MyVocaList

Adapt the Authority Matrix to MyVocaList's team structure:

| Rule | Current Authority | Proposal | Review | Notes |
|------|-------------------|----------|--------|-------|
| Architecture (CLAUDE.md / code-principles.md) | Helder (Tech Lead) | RFC | Helder + Senior Dev (as available) | MyVocaList is single-developer; consensus is "self-review + fresh context" |
| Technology (Stack, .NET, DevExpress) | Helder | RFC | Helder | Locked stack; changes require strong justification |
| Code Quality (testing.md, naming) | Helder | PR to rules/ | Helder | Applied immediately via linter / hook |
| Workflow (workflow.md, commit style) | Helder | PR to rules/ | Helder | Enforced by hooks |
| CLAUDE.md / rules/ | Helder + advisors (if any) | PR | Helder | Changes encouraged as learning accrues (living document) |

**Special case:** If MyVocaList becomes a team project, upgrade authority matrix to include peers and require 1-reviewer rule on amendments.

---

## Integration with SDD Phases

Amendment governance fits into the broader SDD cycle (S3):

- **Planning Phase:** Amendment is discovered during requirements analysis (constraint blocks feature). RFC is written. Review happens synchronously with stakeholder review.
- **Implementation Phase:** If amendment is approved, pilot is conducted on new feature code (reduces risk). Retroactive application happens post-feature-launch.
- **Verification Phase:** Review gate checks that amendment RFC exists and is linked in commit message.

---

## Living Constitution Principles

A constitution that is truly living — not just static — exhibits these qualities:

1. **Learns from failures** — Each bug fix, incident, or missed deadline is reviewed for constitutional implications
2. **Evolves without fragmentation** — Rules change as a coherent set, not piecemeal
3. **Remains auditable** — Every change is documented with clear rationale and effective date
4. **Scales with the project** — As complexity grows, the constitution grows with it (new rules added as categories emerge)
5. **Distinguishes obsolete from wrong** — Old rules are explicitly deprecated (marked as sunset date) before removal

**Practice:** After every significant milestone (sprint, phase completion, incident review), ask:

> "Did we discover anything about the architecture, code quality, security, or workflow that should amend our constitutional rules?"

Update CLAUDE.md accordingly.

---

## Tools and Automation

### Pre-Commit Hook Example

```bash
#!/bin/bash
# .claude/hooks/pre-commit — Enforce amendment RFC for CLAUDE.md changes

if git diff --cached --name-only | grep -qE "CLAUDE\.md|\.claude/rules/"; then
    # Check for corresponding amendment RFC
    if ! ls .claude/amendments/ 2>/dev/null | grep -q "$(date +%Y%m%d)"; then
        echo "Error: Amendment to constitutional rule requires ./.claude/amendments/YYYYMMDD_<name>.md"
        echo "Create RFC, get review, then amend."
        exit 2
    fi
fi
exit 0
```

### CI Gate Example

```yaml
# .github/workflows/constitution-check.yml
- name: Validate Amendment Record
  run: |
    git log --oneline -1 | grep -q "^amend:" || {
        echo "Constitutional changes must be committed with 'amend:' prefix"
        exit 1
    }
```

### Amendment Workflow (GitHub)

Use GitHub Discussions or a dedicated label `type:amendment` to track RFCs in issues. Link RFC issues to amendment files in `.claude/amendments/`.

---

## Sources

- [The Project Constitution — Agent Factory / Panaversity (2026)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/the-project-constitution)
- [Constitutional Self-Governance for Autonomous AI Agents — CTE Research (2026)](https://www.cteinvest.com/research/constitutional-self-governance.html)
- [Axionic Agency XII.5 — Reflective Amendment Under Frozen Sovereignty (Results) — Axionic Agency Lab (2026)](https://axionic.org/papers/Axionic-Agency-XII.5.html)
- [AOS Constitution — Amendments — AOS Foundation (2026)](https://aos-constitution.com/amendments)
- [Compiling 5QLN as a Legal Constitution — Amihai Loven (2026)](https://www.5qln.com/implementing-5qln-as-a-legal-constitution-an-end-to-end-technical-blueprint/)
- [Article 11 AI — Constitutional Governance for Artificial Intelligence (2025)](https://article11.ai/)
- [MAC: Multi-Agent Constitution Learning — arXiv:2603.15968 (2026)](https://arxiv.org/html/2603.15968v1)
- [CLAUDE.md Design Principles: Build Your Project Constitution — ClaudeWorld (2026)](https://claude-world.com/articles/claude-md-design/)
- [CLAUDE.md Protocol — Aura Docs (Naridon, Inc.)](https://docs.auravcs.com/claude-md-integration/)
- [Enforcing CLAUDE.md — Soban Raza (Medium, 2026)](https://medium.com/@sobanr4/enforcing-claude-md-089d2da37399)
