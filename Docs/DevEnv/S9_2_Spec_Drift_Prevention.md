# S9.2 — Spec Drift Prevention

**Status:** Researched  
**Predecessor(s) ID:** S9

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written; covers detection, continuous conformance, versioning, and recovery patterns |

---

## Overview

Spec drift is the gradual divergence between a specification and the implementation it generated. Unlike a bug (a single point failure), drift is systemic: changes accumulate across multiple regeneration passes, refactorings, and maintenance cycles. Left unchecked, the implementation diverges so far from the spec that regenerating from scratch becomes faster than patching.

As documented in S9 (Quality Assurance), spec drift emerges from five root causes:

1. **Nondeterministic generation** — Running an agent against the same spec twice produces different code; without explicit acceptance of one as "correct," the spec becomes a memory artifact, not a true source of truth.
2. **Silent spec updates** — Some tools allow agents to modify specs when they encounter ambiguity; without review and versioning, specs drift to match implementations.
3. **Incremental changes without spec review** — Engineers refactor or optimize code without checking the spec, introducing changes that are correct in isolation but violate documented constraints.
4. **Spec maintenance burden** — As systems grow, keeping specs synchronized becomes tedious; developers skip it to move faster, and specs become stale.
5. **Regeneration context loss** — When code is regenerated, original design decisions and constraints documented in the spec are lost; the new generation simplifies or restructures, breaking assumptions elsewhere.

Spec Drift Prevention addresses these through three integrated mechanisms:

- **Continuous Conformance Checking:** CI/CD gates that run on every push, mapping specs to code and tests
- **Spec Versioning and Rollback:** Specs tracked in git like code, with explicit version bindings and recovery capability
- **Spec Rot Detection and Recovery:** Structural changes that couple spec review to code review, preventing silent accumulation of divergence

---

## Continuous Conformance Checking

Spec drift must be detected **continuously, not periodically.** Periodic checks (weekly, monthly, quarterly) allow divergence to compound. By the time a check runs, the codebase may already be months ahead of the spec.

### The Conformance Default: FAIL

Continuous conformance is implemented as a CI/CD gate that runs on **every push**. The gate defaults to **FAIL** — every feature must prove three things:

1. **Has a corresponding spec requirement** (in requirements.md, design.md, or acceptance criteria)
2. **Has implementation code** (with a source reference: file + line)
3. **Has test coverage** (with a test reference: test name + file)

### Conformance Gate Implementation

In pseudocode:

```yaml
stage: Verify Spec Conformance
  - Parse spec acceptance criteria from requirements.md
  - For each acceptance criterion:
    - Check: Is there implementation code?
      - If no  → WARN (Unimplemented feature)
      - If yes → Record source reference (file:line)
    - Check: Is there test coverage?
      - If no  → WARN (Untested feature)
      - If yes → Record test reference (test:file)
  - For each implementation file changed in diff:
    - Check: Is this file mentioned in any spec?
      - If no  → WARN (Undocumented feature)
      - If yes → Record spec reference
  - Report: Traceability matrix (Criterion → Code → Test)
  - Gate:
    - FAIL if any acceptance criterion has no code
    - WARN if any acceptance criterion has no test
    - WARN if code exists without spec coverage
```

### Tooling Examples

**spec-kit-sync** (GitHub, 2026) scans specs and code to detect three categories of drift:

| Finding | Example | Action |
|---------|---------|--------|
| **Drifted requirement** | Spec says "5 fields," code extracts 4–8 per type | Propose fix: align spec or fix code |
| **Unspecced feature** | Code exists, no spec covers it | Propose backfill: generate spec from code |
| **Spec conflict** | Two specs (or spec vs design doc) contradict | Surface for human review |

**SpecSync** (CorvidLabs, 2026) validates markdown module specs against source code bidirectionally:

| Direction | Severity | Example |
|-----------|----------|---------|
| Code exports something not in spec | Warning | Undocumented export, `getUserById()` |
| Spec documents something missing from code | Error | Phantom function, deleted but still documented |
| DB table in spec missing from schema | Error | `users` table declared but migrations don't create it |
| Column type mismatch | Warning | Spec says `email: string`, schema is `email: varchar(100)` |

The error/warning distinction is critical: **missing code is a blocker** (unmapped spec → no implementation), but **missing spec is a warning** (code exists, just undocumented).

**DriftLinter** (2026) focuses on API schema drift:

```bash
driftlint check --config .driftlinter.yml
# Reports:
#   - Missing Routes: in code but not in OpenAPI spec
#   - Zombie Routes: in spec but deleted from code
#   - Schema Validation: parameters, request bodies, HTTP methods mismatch
```

### Continuous Conformance Best Practices

1. **Run on every push, not selectively.** The gate becomes trustworthy only if it runs consistently. Skipping the gate on hotfixes or refactors defeats the purpose.

2. **Make the report actionable.** A conformance failure should include:
   - Which criteria are unmapped
   - Which code files lack spec coverage
   - Proposed fix (update spec, add implementation, or backfill)
   - Link to relevant spec file and PR template for fixes

3. **Gradual adoption.** On initial integration:
   - Week 1: Report mode (gather baseline data; gates don't fail)
   - Week 2–3: WARN on unmapped features (gate still green; alerts visible)
   - Week 4+: FAIL on unmapped features (gate red; fixes required)

This prevents a "false positive" burst that would overwhelm the team and erode trust in the gate.

4. **Pair with drift detection on file changes.** When a spec-referenced file changes, the gate should ask: "Has the corresponding spec been reviewed and approved?" This couples spec review to code review, preventing silent divergence.

---

## Spec Versioning and Rollback

Specs are not immutable, but **they must be versioned alongside code.** When a spec is updated, that change is a commit. When code is regenerated, the spec-to-code binding is explicit: "This code was generated from Spec v1.3.2."

Versioning enables **rollback:** If regeneration introduces regressions, revert to an older spec version and regenerate. This is only possible if specs are tracked in git like code.

### Versioning Discipline

**Naming convention:** Specs include a version field:

```markdown
# Venues Feature — Spec

**Version:** 1.3.2  
**Status:** Approved  
**Generated from:** arXiv:2602.00180  
**Last reviewed:** 2026-04-28  
**Last modified:** 2026-04-30 (clarified token limit, added test for boundary case)

## Acceptance Criteria
...
```

### Git as the Versioning System

Git history is the source of truth:

```bash
$ git log --oneline Docs/specs/venues/design.md
# Each commit represents a spec change
# Diffs show what changed and when

abcdef2 2026-05-01 Update: add pagination limits
bcdef34 2026-04-30 Fix: clarify token boundary condition
cdef456 2026-04-28 Initial: venues feature design
```

### Rollback and Regeneration

If regeneration based on Spec v1.4.0 produces code that fails acceptance tests:

```bash
# Checkout previous known-good version
git checkout abcdef2 -- Docs/specs/venues/design.md

# Regenerate from that version
/ speckit . implement Docs/specs/venues/

# Compare outputs
git diff HEAD
```

The old implementation is recovered; the problematic changes are discarded. The commit log shows exactly when and why the rollback occurred.

### Decision Log Pairing

Alongside specs, maintain a **decision log** (`decision-log.md`) that records:

```markdown
## Decision Log

### DEC-2026-05-01: Pagination Strategy
- **Context:** Venues list was returning all rows; performance degraded over 10k venues
- **Options Considered:**
  - Offset/limit pagination (spec 1.2.0)
  - Cursor-based pagination (spec 1.3.0 candidate)
  - Infinite scroll (rejected: no analytics)
- **Decision:** Cursor-based (specification v1.3.1)
- **Why:** Offset inefficient at high page numbers; cursor avoids N+1 queries
- **Trade-off:** Client must maintain cursor state; simpler backend
- **Reversal Condition:** If offset suffices (<1M rows), revert to spec 1.2.0
```

When drift is detected later, the decision log explains the "why" — was the change intentional, or has the system evolved in a different direction?

### Versioning for Brownfield (Legacy Codebases)

When retrofitting SDD into existing systems:

```bash
# 1. Extract current spec from code
specfact code import --create

# 2. Version it as Spec v1.0.0 — "Current State"
echo "Version: 1.0.0 (baseline from legacy system)" >> Docs/specs/venues/design.md
git add -A && git commit -m "initial: venue spec v1.0.0 (legacy extraction)"

# 3. Plan improvements as Spec v1.1.0, v1.2.0, etc.
# Future regenerations reference these explicit versions
```

This prevents the false claim that the legacy system was ever "designed by spec." It was built imperatively; now it has specs, and future changes will be spec-driven.

---

## Spec Rot Detection and Recovery

**Spec rot** occurs when specs become stale under a growing codebase. New features are added without updating specs. Code is refactored without reflecting changes in design docs. Within months, the spec describes a system that no longer exists.

### Root Causes of Spec Rot

1. **Disconnected workflows** — Spec updates and code changes happen in separate PRs, and reviewers don't check both
2. **Perceived overhead** — "Updating the spec takes extra time; I'll do it later" → it never happens
3. **Unclear ownership** — Nobody is responsible for keeping specs current
4. **Silent divergence** — No feedback loop; divergence accumulates until discovery is painful

### Structural Prevention: Tie Spec Review to Code Review

When a PR changes code:

**If the PR changes behavior:**
- The PR must update the spec(s), OR
- The PR must explain why the spec doesn't need updating (e.g., "Optimization only; behavior unchanged")

**If the PR doesn't change behavior:**
- The PR should mention relevant specs that were consulted

This is enforced as a **checklist in PR templates:**

```markdown
## Spec Alignment

- [ ] I have read the relevant spec(s)
- [ ] This PR changes behavior:
  - [ ] Yes — I have updated the spec(s)
  - [ ] No — This PR only refactors or optimizes
- [ ] Tests verify that the implementation matches the spec
- [ ] If spec was updated, I have bumped the version (e.g., 1.2.0 → 1.3.0)
```

Failing to check these boxes **blocks merge**. This makes spec review visible and continuous, not a historical afterthought.

### Automated Spec Rot Detection

Tools like **Drift** (Fiberplane, 2025) anchor specs to code using tree-sitter AST parsing:

```markdown
---
anchors:
  - path: src/auth/provider.ts
    symbol: AuthConfig
    provenance: abcdef2  # Last commit that reviewed this anchor
---

# Authentication Configuration

The `AuthConfig` interface defines...
```

On every `git push`, `drift check` asks: "Has the bound code changed since the provenance commit?"

```bash
$ drift check
✓ docs/auth.md — AuthConfig unchanged since abcdef2
✓ docs/pagination.md — getPagedVenues unchanged
✗ docs/sorting.md — SortField type changed (last touched 2026-04-15)
  Stale: spec was last reviewed 2026-04-01; code changed on 2026-04-15

Run: drift link docs/sorting.md src/venues/query.ts#SortField
```

This flags specs that have drifted **before** they reach production. The build fails, forcing the developer to either:

1. **Review the code change and update the spec** (if the change is intentional)
2. **Revert the code** (if the change was accidental)

### Recovery: Spec Reconciliation

When drift is discovered after the fact, tools like **spec-kit-reconcile** (GitHub, 2026) surgically reconcile specs to match reality:

```bash
# Gap report: plain-text observation of what drifted
/speckit.reconcile.run "Backend exists, but React screen is unreachable; need sidebar link and route"

# Output: updated tasks.md with new remediation tasks
# Updated spec.md and plan.md to match code reality
```

The reconciliation tool classifies drift into six types:

| Type | Signal | Resolution |
|------|--------|-----------|
| `NEW_FUNCTIONALITY` | Code exists without spec; has tests or is actively used | Auto: Update specs (code wins) |
| `REMOVED_FEATURE` | Spec exists without code; no recent commits touching it | Auto: Deprecate in specs (code wins) |
| `BEHAVIORAL_CHANGE` | Both exist but behavior differs | Ask user: Is code correct or is it a bug? |
| `REFACTORING` | Structure changed but behavior equivalent (tests pass) | Auto: Update technical specs only |
| `BUG_OR_DEFECT` | Code violates spec AND tests fail | Ask user: Fix code or update spec? |
| `AMBIGUOUS` | Cannot determine with confidence | Ask user: Classify manually |

The recovery process ensures that:

1. **Code is treated as source of truth for current behavior** (not aspiration, reality)
2. **Specs are updated to match reality** (not rewritten to match old aspirations)
3. **Every divergence is classified** and logged (traceability preserved)
4. **Decisions are explicit** (code-wins, spec-wins, or human review required)

---

## Integration with SDD Workflow

Spec drift prevention integrates at three points in S3 (Workflow Phases):

| Phase | Drift Activity | Owner | Gate |
|-------|---|---|---|
| **S3.1 — Planning** | Assign version to spec (v1.0.0); establish decision log | Human | Spec review before code generation |
| **S3.2 — Implementation** | Continuous conformance checks (every push); drift detection on file changes | Automation | Build fails if acceptance criterion unmapped |
| **S3.3 — Verification** | Spec versioning recorded in decision log; reconciliation if regressions appear | Verifier agent + Human | Specs reconciled before merge; decision log updated |
| **Post-merge** | Drift monitoring active; anchor checks run on every commit | Automation | Stale specs flagged; spec review required before next changes |

---

## Practical Workflow: Staying in Sync

### Week 1: Establish Baseline

```bash
# Extract current state as spec v1.0.0
specfact code import Docs/specs/venues/
git add Docs/specs/venues/
git commit -m "initial: spec v1.0.0 — baseline extraction"

# Link specs to code with tree-sitter anchors
drift link docs/specs/venues/design.md src/services/VenueService.cs#VenueService
git commit -m "anchor: link specs to code"
```

### Week 2–4: Continuous Synchronization

```bash
# On every change:
# 1. Read the relevant spec
# 2. Update spec if behavior changed
# 3. Bump spec version
git commit -m "spec: v1.1.0 — added pagination, updated acceptance criteria"

# 4. Conformance check runs automatically in CI
# 5. Drift check runs automatically in CI
# 6. If either fails, fix before merge
```

### Monthly: Drift Audit

```bash
# Run full drift report
specfact code drift detect venues --repo .

# Review stale specs
drift check --report

# Reconcile if needed
/speckit.reconcile.run "report.md"
```

---

## Metrics and Thresholds

Track these to calibrate drift prevention:

| Metric | Healthy | Warning | Critical |
|--------|---------|---------|----------|
| **Spec freshness** | Updated ≤2 weeks after code change | 2–4 weeks | >1 month |
| **Acceptance criteria coverage** | 100% of criteria have code + test | 90–99% | <90% |
| **Undocumented features** | 0–5% of codebase | 5–15% | >15% |
| **Drift detection latency** | Same-day (CI gate) | Same-week (manual review) | >1 week |
| **Spec reconciliation cycle** | <1 day from detection to update | <1 week | >1 week |
| **Anchor staleness** | 0–10% of specs | 10–30% | >30% |

If undocumented features exceed 15%, audit discovery — either specs are not comprehensive, or developers are not using the spec-first workflow.

---

## Common Failure Modes and Mitigations

| Failure Mode | Root Cause | Signal | Mitigation |
|---|---|---|---|
| **Specs become fossils** | No enforcement; updating specs is optional | "We have specs, but everyone codes without reading them" | Make conformance gate mandatory; block merge on unmapped criteria |
| **False negatives in conformance** | Acceptance criteria are vague ("system shall be performant") | Gate passes but spec violated at runtime | Audit specs for testability before conformance check; require acceptance criteria in EARS or Given/When/Then format |
| **Drift accumulates silently** | No continuous check; checks run monthly or quarterly | Specs and code are discovered out of sync six months later | Run drift detection on every push, not periodically |
| **Anchor staleness multiplies** | Specs are reviewed but anchors are not refreshed | 50% of specs report stale even though they were recently reviewed | Run `drift link` as part of spec update workflow; document it in PR template |
| **Rollback data loss** | Spec versions not in git; only latest version available | Need to revert to Spec v1.2.0; previous versions are unrecoverable | Version specs in git with explicit semantic versioning (v1.0.0, v1.1.0, v1.2.0) |
| **Decision log decouple** | Specs updated but decisions not recorded | Future developers (and AI agents) don't know why a constraint exists | Require decision log entries for every spec version bump |
| **Reconciliation rubber stamp** | Recovery tool runs but output is not reviewed | Reconciliation auto-applies without human validation | Require human approval gate for each reconciliation; preview diffs before applying |

---

## Tooling Summary

| Tool | Role | Drift Category | 2026 Status |
|------|------|---|---|
| **spec-kit-sync** | Detect & propose drift fixes (code/spec) | Bidirectional | Mature (GitHub) |
| **SpecSync** | Bidirectional spec-code validation; coverage reports | Bidirectional | Stable (Rust, CLI) |
| **spec-kit-reconcile** | Post-implementation gap closer; spec reconciliation | Recovery | Mature (GitHub) |
| **DriftLinter** | API schema drift detection (OpenAPI) | Schema-specific | Stable (CI/CD) |
| **Drift (Fiberplane)** | Anchor specs to code; detect staleness via AST | Anchor-based | Mature (tree-sitter) |
| **Specmatic MCP Auto-Test** | Schema drift detection for MCP servers | Schema-specific | Emerging (tooling) |
| **spec-gen-cli** | Drift detection + ADR impact analysis | Incremental | Stable (npm) |
| **Semcheck** | AI-powered spec-code compliance verification | AI-driven | Emerging |
| **SpecFact (Codebase module)** | Legacy codebase import, drift detection, drift reporting | Brownfield | Mature |

---

## Key Takeaways

1. **Drift is silent unless continuously detected.** Weekly or monthly checks compound divergence. Gates must run on every push.

2. **Conformance defaults to FAIL.** Every acceptance criterion must map to code and test evidence. Missing code is a blocker; missing spec is a warning.

3. **Specs are versioned in git alongside code.** Rollback is only possible if versions are explicit and tracked. Decision logs explain the "why" behind changes.

4. **Spec review is coupled to code review via PR template.** If a PR changes behavior, the spec must be updated. Failing to check the box blocks merge.

5. **Drift recovery is systematic, not reactive.** When detected, drift is classified into six types. Each type has a resolution path: code wins, spec wins, or human decides.

6. **Metrics drive prevention.** Track spec freshness, coverage, and anchor staleness. When metrics drift beyond thresholds, audit and remediate.

7. **Brownfield codebases require baseline extraction.** Retrofitting SDD into legacy systems starts with spec v1.0.0 extracted from current code. This is not a design doc; it's a baseline.

---

## Relationship to Other SDD Topics

- **S1.1 — Definition:** Spec-as-primary-artifact principle enables drift prevention
- **S2 — Specification Design:** Well-crafted specs are easier to keep in sync than vague ones
- **S3 — Workflow Phases:** Drift gates embedded at implementation and verification phases
- **S6 — Governance & Enforcement:** Constitutional rules that mandate continuous conformance checking
- **S9.1 — TDD Integration:** Tests are the executable specs that validate conformance
- **S9.3 — Hallucination Safeguards:** Drift prevents hallucinations from compounding via regeneration

---

## Sources

- [Spec-Kit-Sync — GitHub](https://github.com/bgervin/spec-kit-sync) — Drift detection and resolution for GitHub Spec Kit
- [SpecSync — CorvidLabs](https://corvidlabs.github.io/spec-sync) — Bidirectional spec-to-code validation with lifecycle enforcement
- [Spec-Kit-Reconcile — GitHub](https://github.com/stn1slv/spec-kit-reconcile) — Post-implementation gap closer; reconciles specs to code
- [DriftLinter — GitHub](https://github.com/driftlint/driftlint) — API schema drift detection for OpenAPI specs
- [Drift — Fiberplane](https://fiberplane.com/blog/drift-documentation-linter/) — Anchor-based spec staleness detection via tree-sitter
- [Specmatic MCP Auto-Test — Specmatic.io](https://specmatic.io/updates/testing-mcp-servers-how-specmatic-mcp-auto-test-catches-schema-drift-and-automates-regression/) — Schema drift detection for MCP servers
- [spec-gen-cli — npm](https://registry.npmjs.org/spec-gen-cli) — Drift detection with ADR impact analysis and architectural decisions
- [Semcheck — semcheck.ai](https://semcheck.ai/) — AI-powered spec-code compliance verification
- [SpecFact Codebase Module — specfact.io](https://modules.specfact.io/bundles/codebase/drift/) — Drift detection for legacy codebases
- [API Drift Prevention — APITect](https://apitect.com/blogs/stopping-schema-drift-how-to-keep-your-openapi-spec-and-code-in-sync-automatically) — Contract testing and schema synchronization patterns
- [From Vibe Coding to Verified Specs: SpecFact in DevSecOps — SpecFact.dev](https://specfact.dev/blog/from-vibe-coding-to-verified-specs/) — Automated enforcement and CI/CD integration
- [Spec-Driven Development: From Code to Contract — arXiv:2602.00180](https://arxiv.org/pdf/2602.00180) — Foundational SDD paper covering spec-anchored development and drift
- [GitHub Spec Kit](https://github.com/github/spec-kit) — Ecosystem of spec-driven development extensions including drift detection and reconciliation
- [auto-sdd — GitHub](https://github.com/fischmanb/auto-sdd) — Autonomous SDD build loop with drift checking on every feature iteration
