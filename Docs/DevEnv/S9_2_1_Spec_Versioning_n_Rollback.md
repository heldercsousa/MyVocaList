# S9.2.1 — Spec Versioning & Rollback

**Status:** Researched
**Predecessor(s) ID:** S9.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; covers semantic versioning, Git-based versioning, decision logs, and rollback strategies |

---

## Overview

Specification versioning answers a fundamental question: when a spec changes, how do we recover to a known-good state? This is particularly critical in spec-driven development, where regenerating code from an earlier spec version is often faster than manual debugging. Unlike code, which benefits from semantic versioning practices (MAJOR.MINOR.PATCH), specifications have no standardized versioning discipline. This gap creates three problems:

1. **No semantic meaning in version numbers** — Is a change in the spec a breaking change (major), a backward-compatible enhancement (minor), or a bug fix (patch)?
2. **Rollback uncertainty** — When regeneration from Spec v1.4.0 fails, which prior version is safe to revert to? Git provides history, but no explicit binding between specs and their stability.
3. **Decision context loss** — Why was a constraint added in v1.2.1? What problem did it solve? Without a decision log, reverting that constraint is guesswork.

Spec Versioning & Rollback establishes three mechanisms to address these gaps:

- **Semantic versioning discipline for specifications** — Apply MAJOR.MINOR.PATCH to spec changes with explicit intent
- **Git-based version bindings** — Track which code was generated from which spec version, enabling precise rollback
- **Decision logs paired with specs** — Record the "why" behind each version change so reversals are informed decisions, not desperate guesses

---

## Semantic Versioning for Specifications

### Core Versioning Model

Specifications should follow semantic versioning (SemVer) principles, adapted for specification artifacts:

**MAJOR (X.0.0):** Backward-incompatible changes
- Breaking change to acceptance criteria (a requirement is removed or fundamentally altered)
- A constraint is removed that implementations depended on (e.g., "max response time 100ms" removed)
- API contract changes that invalidate prior implementations
- Example: Changing "password must be 8–20 characters" to "password must be 12–30 characters" breaks existing user data

**MINOR (x.Y.0):** Backward-compatible additions
- New acceptance criteria added (new behavior, but old behavior still valid)
- New optional constraints or fields (implementations can ignore them)
- Clarification of ambiguous requirements (no behavior change, just precision)
- Example: Adding "user can reset password via email link" while keeping SMS option intact

**PATCH (x.y.Z):** Bug fixes to the specification itself
- Correction of typos, grammatical errors, or formatting
- Fixing a contradictory requirement within the spec
- Clarifying the same requirement across multiple sections (no intent change)
- Example: Fixing "field must be unique" that was accidentally listed twice with different context

### When to Bump Each Version

| Change Type | Signal | Bump | Example |
|---|---|---|---|
| Breaking constraint removal | Implementations that met old constraint now violate new spec | MAJOR | Remove "response time < 100ms" |
| Behavioral change | Old acceptance criteria are no longer valid | MAJOR | Change "sort by creation date" to "sort by relevance score" |
| New requirement | New AC added; old AC still valid | MINOR | Add "pagination supports cursor-based navigation" |
| Optional constraint | New guideline that doesn't invalidate existing implementations | MINOR | Add "should cache for 5min if possible" |
| Spec error | Contradiction or ambiguity within the spec, no intent change | PATCH | Fix: "field is required" appears twice with conflicting contexts |
| Clarification | Ambiguous requirement restated more clearly, behavior unchanged | PATCH | Reword "user should see fast results" to "search completes in < 500ms" |

### Version Metadata

Every spec should include a version header:

```markdown
# Venues Feature — Specification

**Version:** 1.3.2
**Status:** Approved
**Date:** 2026-05-02
**Last reviewed:** 2026-05-01
**Last modified:** 2026-04-30
**Reason for change:** Added filter by capacity; supports existing venue listing AC
**Breaking changes:** None (v1.3.1 → v1.3.2)
**Regenerated from this version:** v1.2.0 (code generation on 2026-04-28 used this stable version)
```

The "Regenerated from this version" field is critical: it creates an explicit binding between running code and the spec version that generated it. If bugs appear in that code generation, you know exactly which spec to revert to.

---

## Git as the Versioning System

Git history is the source of truth for spec evolution. Every version change is a commit.

### Version Binding in Git

```bash
$ git log --oneline Docs/specs/venues/design.md
abcdef2 2026-05-01 spec: v1.3.2 — add capacity filter to acceptance criteria
bcdef34 2026-04-30 spec: v1.3.1 — clarify pagination limit constraint (PATCH)
cdef456 2026-04-28 spec: v1.3.0 — add advanced filters to requirements (MINOR)
def4567 2026-04-27 spec: v1.2.1 — fix contradictory field type definitions (PATCH)
ef45678 2026-04-20 spec: v1.2.0 — change sorting default to relevance (MAJOR)
```

Each commit message follows the pattern: `spec: vX.Y.Z — <reason>`. The diff shows exactly what changed:

```bash
$ git show bcdef34
# Shows the change: clarified that pagination "limit" has a max of 100
# This is a PATCH because no AC changed, just specification clarity
```

### Rollback via Git Checkout

When code regenerated from Spec v1.4.0 fails tests:

```bash
# Identify the previous stable version
git log --oneline Docs/specs/venues/design.md | head -5

# Checkout the spec at v1.3.2 (a known-good version)
git show v1.3.2:Docs/specs/venues/design.md > /tmp/design-v1.3.2.md

# Regenerate code from that version
/speckit . implement --spec /tmp/design-v1.3.2.md

# Run tests against the regenerated code
dotnet test

# If tests pass, decide: keep the rollback or fix v1.4.0?
```

This workflow is only reliable if:
1. **Every version has a commit** — specs are never modified without committing the change
2. **Commits are tagged** — optionally, tag stable versions: `git tag spec-venues-v1.3.2`
3. **Diffs are readable** — commit messages explain why the version changed

### Anti-Pattern: Mutable Latest Version

❌ **Wrong:** Changing the spec file without committing, then committing bundled changes:

```bash
# Developer updates spec locally
# ... makes other code changes ...
# Days pass ...
# ... then commits everything as "wip: venues feature"
# Now the spec history is lost; you can't pinpoint when v1.3.0 became v1.3.1
```

✅ **Right:** Every spec version is immutable once committed:

```bash
# Spec change only
git add Docs/specs/venues/design.md
git commit -m "spec: v1.3.1 — clarify pagination limit (PATCH)"

# Code changes follow in a separate commit
git add src/Venues/VenueService.cs
git commit -m "feat: implement pagination limit constraint from spec v1.3.1"
```

---

## Decision Logs Paired with Specs

A specification version number alone doesn't explain why a change was made. Decision logs answer the "why" question, enabling informed rollbacks.

### Decision Log Structure

Alongside each spec, maintain a `decision-log.md`:

```markdown
# Venues Feature — Decision Log

## DEC-2026-05-01: Pagination Strategy (Spec v1.3.0)

**Condition:** Venues list was returning all 50k rows; load time exceeded 10 seconds.

**Options Considered:**
- **Option A: Offset/Limit Pagination** (Spec v1.2.1)
  - How: Client sends `page=2&size=20` to get rows 20–40
  - Pros: Simple for clients, standard REST pattern
  - Cons: Slow at high page numbers (DB must scan and skip first N rows)
  - Decision: Rejected — at 50k venues, page 1000 requires scanning 20k rows

- **Option B: Cursor-Based Pagination** (Spec v1.3.0 — **chosen**)
  - How: Client sends `cursor=abc123` (points to a specific venue ID)
  - Pros: Constant-time lookup; DB doesn't need to scan
  - Cons: Client must maintain cursor state; less intuitive
  - Decision: Chosen — eliminates N+1 query problem; scales to 1M+ venues

- **Option C: Keyset Pagination**
  - How: Similar to cursor but uses composite keys (venue_id, created_at)
  - Cons: Requires multiple indices; more complex
  - Decision: Rejected — cursor-based is sufficient for current scale

**Decision:** Spec v1.3.0 — implement cursor-based pagination

**Trade-offs:**
- Gain: O(1) query cost, unlimited scale
- Give up: Simpler REST semantics; backward-incompatible with offset-based clients

**Reversal Condition:** If the venue count stays under 10k for the next 12 months, consider reverting to offset-based pagination in v2.0.0.

**Linked Issue:** GH#1234

---

## DEC-2026-04-27: Sorting Default (Spec v1.2.0)

**Condition:** Product team A/B tested two sorting defaults and found relevance-based sorting increased engagement by 12%.

**Decision:** Changed default sort from "creation date" (v1.1.x) to "relevance score" (v1.2.0)

**Impact:** This is a MAJOR version change — existing clients that rely on date ordering will see unexpected results without code changes.

**Reversal Condition:** If engagement drops below baseline within 30 days, revert to v1.1.5 immediately.
```

### When Reverting, the Decision Log Guides You

Suppose code regenerated from Spec v1.3.2 fails pagination tests. Before deciding to rollback to v1.2.9, the decision log answers:

- **Why did we change from offset to cursor-based pagination?** (DEC-2026-05-01) — because of scale concerns
- **Is that concern still valid?** Venue count is still 50k, so yes
- **What was the reversal condition?** "If count stays under 10k for 12 months" — hasn't happened
- **Conclusion:** Don't revert the pagination logic. The spec v1.3.2 is correct; the code generation is wrong. Fix the generator or the acceptance tests.

Without the decision log, reverting is guesswork. With it, rollback decisions are informed.

---

## Rollback Strategies: Three Scenarios

### Scenario 1: Spec Changes Introduced a Bug

**Symptom:** Code generated from Spec v1.3.2 fails acceptance tests; v1.3.1 passes.

**Process:**
```bash
# 1. Identify what changed in v1.3.2
git diff v1.3.1 v1.3.2 -- Docs/specs/venues/design.md

# 2. Read the decision log to understand why the change was made
cat Docs/specs/venues/decision-log.md | grep -A 10 "DEC-2026-05-01"

# 3. Decide: Is the change correct, or was there a mistake in the spec?
#    - If spec is correct but generator is broken: fix the generator
#    - If spec is wrong: revert to v1.3.1 and update decision log

# Option A: Spec is wrong
git revert HEAD  # Revert the v1.3.2 commit
git commit -m "spec: revert v1.3.2; generator failed AC tests; investigate root cause"

# Option B: Spec is right; generator is broken
git commit -m "fix: generator — handle new pagination field from spec v1.3.2"
```

### Scenario 2: Requirements Changed; Old Spec Is Obsolete

**Symptom:** Users want a new sorting option; Spec v1.2.0 doesn't include it.

**Process:**
```bash
# 1. Update the spec for the new requirement
# This becomes Spec v1.2.1 (MINOR — new feature, backward-compatible)
git add Docs/specs/venues/design.md
git commit -m "spec: v1.2.1 — add popularity-based sorting option"

# 2. Add a decision log entry
# Link this to the requester and acceptance criteria

# 3. Generate code from v1.2.1
/speckit . implement --spec Docs/specs/venues/design.md

# Don't rollback to v1.2.0 — it's now outdated
```

### Scenario 3: Regeneration Fails; Need to Pin to Stable Version

**Symptom:** Code generation is nondeterministic; same spec generates different code twice.

**Process:**
```markdown
## Code Generation Pinning

If the generator produces nondeterministic output:

1. Identify a spec version that generated correct code (e.g., v1.3.0 on 2026-04-28)
2. Pin the codebase to that version in configuration:
   
   ```yaml
   # .codegen/config.yml
   spec:
     version: venues@1.3.0
     pinned_until: 2026-06-01  # Re-evaluate in 2 months
     reason: Generator nondeterminism; v1.3.0 produces stable output
   ```

3. When regenerating, always use the pinned version until the generator is fixed
4. Once fixed, update the config and regenerate from latest

This prevents spec drift (staying behind the latest spec) while protecting against bad generators.
```

---

## Brownfield Retrofit: Establishing a Baseline

When retrofitting spec versioning into an existing codebase:

```bash
# 1. Extract current spec from code
specfact code import --create Docs/specs/venues/

# 2. Version it as v1.0.0 — "Current State"
echo "Version: 1.0.0 (baseline from existing code)" >> Docs/specs/venues/design.md
git add Docs/specs/venues/
git commit -m "spec: v1.0.0 — baseline extraction from legacy code"
git tag spec-venues-v1.0.0

# 3. Create decision log documenting this is NOT a design, just a baseline
cat > Docs/specs/venues/decision-log.md << EOF
# Venues Spec — Decision Log

## DEC-2026-05-02: Baseline Extraction (v1.0.0)

This specification was extracted from existing code on 2026-05-02.
It documents the current behavior; it is not a design doc.
Future changes will be spec-driven, with explicit versions and decisions.

The extraction serves as a reference point for measuring improvement over time.
EOF

# 4. Plan improvements starting from v1.1.0
```

---

## Tooling and Automation

### Git Hooks for Spec Versioning

Enforce semantic versioning discipline:

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Check: if specs change, version header must also change
changed_specs=$(git diff --cached --name-only | grep "^Docs/specs/.*design.md$")

if [ -n "$changed_specs" ]; then
  for spec in $changed_specs; do
    current_version=$(grep "^**Version:**" "$spec" | cut -d' ' -f2)
    prev_version=$(git show HEAD:"$spec" | grep "^**Version:**" | cut -d' ' -f2)
    
    if [ "$current_version" = "$prev_version" ]; then
      echo "Error: Spec $spec changed, but version stayed at $current_version"
      echo "Update the version header (MAJOR.MINOR.PATCH) before committing"
      exit 1
    fi
  done
fi
```

### CI/CD Gate: Rollback Testing

```yaml
# .github/workflows/spec-rollback-test.yml
name: Spec Rollback Validation

on: [pull_request]

jobs:
  rollback_test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 10  # Get last 10 commits
      
      - name: Find previous spec version
        run: |
          git log --oneline -- Docs/specs/venues/design.md | head -2 > versions.txt
          cat versions.txt
      
      - name: Regenerate from previous version
        run: |
          prev_commit=$(sed -n '2p' versions.txt | cut -d' ' -f1)
          git show $prev_commit:Docs/specs/venues/design.md > /tmp/design-prev.md
          /speckit . implement --spec /tmp/design-prev.md
      
      - name: Verify older generation still builds
        run: dotnet build && dotnet test
```

This validates that rollback is always possible: if the current spec doesn't work, reverting to the previous version should.

---

## Key Takeaways

1. **Specs are versioned like code** — MAJOR.MINOR.PATCH discipline applied consistently. Every version change is a commit.

2. **Git history is queryable** — `git log Docs/specs/` shows the complete evolution. Diffs show exactly what changed and why (via commit message).

3. **Decision logs enable informed rollback** — When reverting a spec version, read the decision log to understand the trade-offs and reversal conditions.

4. **Rollback is a first-class operation** — Code is regenerated from an older, stable spec version when current generation fails. This is faster than debugging.

5. **Immutable versions prevent drift** — Once a spec version is committed, it is never modified. Changes become new versions, preserving history.

6. **Tooling enforces discipline** — Git hooks, CI gates, and commit message conventions make versioning automatic and auditable.

---

## Relationship to Other SDD Topics

- **S9.2 — Spec Drift Prevention:** Versioning is the first line of defense; paired with decision logs, it enables recovery
- **S3.2 — Implementation Phase:** Code generators reference explicit spec versions, enabling rollback when generation fails
- **S6.4 — CI/CD Integration:** Rollback testing embedded in CI validates that older spec versions still build
- **S4.1 — Memory Bank:** Decision logs are part of context that persists across sessions and team changes

---

## Sources

- [Semantic Versioning 2.0.0](https://semver.org/) — The canonical SemVer specification; applies to specs with adaptation
- [PEP 440 — Version Identification and Dependency Specification](https://peps.python.org/pep-0440/) — Python's approach to versioning schemes; relevant for spec version ranges
- [Git Revert Documentation](https://git-scm.com/docs/git-revert/) — Safe history-preserving undo; applies to spec version rollback
- [Git Undo: Reset, Revert & Restore — The Complete Guide for 2026](https://devtoolbox.dedyn.io/blog/git-undo-reset-revert-guide) — Practical Git undo workflows for shared branches
- [Spec-Driven Development: From Code to Contract](https://arxiv.org/html/2602.00180v1) — arXiv:2602.00180; covers spec versioning in context of AI-assisted development
- [GitHub Spec Kit — Feature Issue #512: Failure Recovery and Task Resumption](https://github.com/github/spec-kit/issues/512) — Discusses rollback and recovery mechanisms in SDD workflows
- [Architecture Decision Records (ADR) — Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) — Foundational decision log pattern; paired with spec versioning
- [Architecture Decision Records: The Complete Guide](https://www.archyl.com/blog/architecture-decision-records-complete-guide) — Practical ADR patterns; numbering, status lifecycle, and supersession discipline
- [How to Write Architecture Decision Records That Actually Get Used](https://www.jonoherrington.com/blog/how-to-write-adrs-that-actually-get-used) — Emphasis on context section and maintaining ADR collections over time
- [Architecture Decision Records Overview — Google Cloud](https://cloud.google.com/architecture/architecture-decision-records) — Enterprise ADR patterns; storing alongside code, review workflows
- [Architecture Decision Records — GOV.UK Framework](https://www.gov.uk/government/publications/architectural-decision-record-framework/) — Governance-heavy ADR adoption; decision ownership and escalation
- [Design Decision Log — Microsoft Engineering Playbook](https://microsoft.github.io/code-with-engineering-playbook/design/design-reviews/decision-log/) — Integration of decision logs into design reviews; timestamping and status tracking
- [SDD Documentation — GitHub Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md) — Official GitHub SDD patterns; workflow phases and spec evolution
- [scafld — Spec-Driven Development with Phase-by-Phase Execution](https://github.com/nilstate/scafld) — YAML-based specs with explicit rollback commands per phase
