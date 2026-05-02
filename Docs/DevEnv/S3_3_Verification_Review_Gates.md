# S3.3 — Verification / Review Gates

**Status:** Researched
**Predecessor(s) ID:** S3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; comprehensive SDD verification patterns documented |

---

## Overview

Verification and review gates are the checkpoints that close the loop between an approved specification and implemented code. They answer the critical question: does what was built match what was approved? Without verification gates, SDD reduces to spec-as-documentation — the spec exists, but no mechanism enforces conformance.

In SDD, verification gates operate at two levels:

1. **Automated gates** — deterministic checks that cannot be gamed: build status, test results, acceptance criteria traceability, architecture compliance, code coverage
2. **Human review gates** — judgment-based checkpoints where a qualified reviewer confirms implementation intent, scope compliance, and architectural coherence

The sequence is mandatory: automated gates pass first, then human review. Automated gates catch mechanical errors; human review catches intent drift that automated tools cannot detect.

---

## Gate Architecture

### Automated Gates (Run First, Deterministic)

Automated verification gates are the structural foundation. They run before any human review and must pass without exception:

**1. Build Gate**
- Code compiles with zero errors
- All existing tests pass
- No warnings treated as errors (if strict mode is enabled)
- Static analysis / linting passes (no code style violations)

**2. Test Quality Gate**
- All unit tests pass
- All integration tests pass
- Test count meets minimum threshold (if defined in spec)
- Test coverage meets or exceeds the threshold defined in the spec
- Edge cases are covered (e.g., null inputs, boundary conditions, error paths)

**3. Acceptance Criteria Traceability Gate**
- Every acceptance criterion from the spec is mapped to:
  - A specific implementation reference (file and line number)
  - A specific test case (test method name or test ID)
- A criterion without both implementation AND test evidence is flagged as FAIL
- No criterion is left untestable by design

**4. Security Gate** (Two phases, if applicable)
- Automated scanning for common patterns:
  - Secrets or credentials in committed files (.env, API keys, passwords)
  - Missing `.gitignore` entries for sensitive files
  - Known vulnerable package versions
  - Injection vulnerability patterns (SQL, XSS, command injection)
- LLM-based analysis for domain-specific risks:
  - Authentication weakening or missing auth checks
  - Authorization logic violations
  - Data trust assumptions that may be unsafe
  - Compliance implications (if domain-sensitive)

**5. Architecture Compliance Gate**
- No layer violations (e.g., presentation layer directly accessing infra, business logic in the UI)
- No circular dependencies between modules
- All exports are actually used (no orphaned code)
- Naming conventions follow project standards
- Project-specific rules (defined in CONSTITUTION.md or equivalent) are satisfied

**6. Scope Gate**
- Only files declared in the spec's scope are modified
- No accidental changes outside the feature boundary
- No refactoring bundled into feature implementation (refactors are separate tasks)

### Human Review Gate (Runs After Automated Gates Pass)

Once all automated gates pass, the human reviewer confirms:

**1. Intent Alignment**
- Does the implementation embody the original design intent?
- Has the implementation team accurately understood the problem?
- Do the code patterns match the architectural decisions from design.md?

**2. Design Coherence**
- Is the implementation architecturally sound?
- Are the interfaces clean and predictable?
- Does it integrate naturally with the existing codebase?

**3. Quality Beyond Tests**
- Performance characteristics (N+1 queries, memory patterns, algorithmic complexity)
- Error handling quality (not just presence, but clarity and user-facing messaging)
- Code clarity and maintainability (is it readable without context-switching?)

**4. Diff Governance**
- Confirm scope compliance: touch only declared files
- Confirm no scope creep or speculative engineering
- Confirm changes are bounded and reviewable

---

## Verification Patterns in Practice

### Pattern 1: Per-Task Micro-Gates (MyVocaList Workflow)

Each task in `tasks.md` has its own verification micro-gate:

```
Implementation (agent completes task)
     ↓
Build gate (0 errors, tests pass)
     ↓
Acceptance criteria traceability (every AC has implementation + test)
     ↓
Human review (agent-optional, PM/architect-required for scope/intent)
     ↓
Task marked complete ✓
     ↓
(next task begins)
```

**Advantages:**
- Failures are caught and corrected at task granularity, not feature granularity
- Review context stays fresh (one reviewer, one task, one day)
- Easy to parallelize — independent tasks gate independently

**Where it applies:** Feature-level SDD workflows (Kiro, GitHub Spec Kit, MyVocaList)

### Pattern 2: Spec-Gated Delivery (ctxt.dev Model)

The primary trust checkpoint is the approved spec, not the code diff:

```
Approved Spec (locked, version controlled)
     ↓
Blinded Execution (agent implements; doesn't see holdout criteria)
     ↓
Deterministic Verification (every acceptance criterion is checked)
     ↓
Holdout Criteria (hidden acceptance tests run; agent never saw them)
     ↓
Signed Conformance Artifact (cryptographically attested evidence bundle)
     ↓
Human Decision Gate (human reviews conformance evidence, not the diff)
```

**Key insight:** The evidence artifact (spec version → commit hash → test results → signed certificate) becomes the primary truth. Code review becomes secondary audit, not the primary gate.

**Where it applies:** Highly autonomous SDD with minimal human touchpoint (auto-merge scenarios, overnight agent runs)

### Pattern 3: Holdout Criteria (The Dark Factory Pattern)

A subset of acceptance criteria are designated "holdout" — the agent never sees them during implementation:

1. **Visible criteria** (agent sees these): 7 of 10 acceptance criteria
2. **Holdout criteria** (agent doesn't see): 3 of 10 acceptance criteria
3. Agent implements against visible criteria and writes tests
4. CI runs holdout criteria as a separate, hidden quality gate
5. If ≥90% pass, code auto-merges; if <90%, agent receives failure details and retries

**Constraints on holdout criteria:**
- Holdouts must be logical derivations of the visible spec, not undisclosed requirements
- Example (valid): visible spec says "rate limit POST /api/tokens at 5/min"; holdout checks "counter resets after window expiry" (consequence of visible requirement)
- Example (invalid): visible spec says nothing about a `/webhook` endpoint; holdout checks it exists (undisclosed requirement)

**Where it applies:** High-confidence SDD (auto-merge enabled), teams with proven evaluator reliability

### Pattern 4: Progressive Autonomy Levels

SDD verification can be staged by trust level:

| Level | Name | Human Checkpoints | Human Review Gate? |
|-------|------|-------------------|--------------------|
| **L1** | Full Control | All automated gates must pass; all gates require human approval | Yes, always |
| **L2** | Trusted | All automated gates pass; human reviews only scope + intent | Yes, selective |
| **L3** | Autopilot | All automated gates pass; human review only at feature boundary | Yes, end-of-feature |
| **L4** | Full Auto | Automated gates + holdout criteria only; no human review | No (holdouts replace humans) |

**Key principle:** No level skips automated gates. Human review is optional (at higher autonomy levels); automated verification is not.

**Where it applies:** Teams learning SDD (start at L1, graduate to L2/L3 as confidence builds)

---

## Verification Failure Modes and Recovery

### Failure Mode 1: Automated Gate Failure (Build, Tests, Architecture)

**Response:** Block merge, return to Implementation Phase.

When an automated gate fails:
1. Agent is immediately notified of failure with evidence
2. Agent diagnoses root cause and attempts fix-in-place (patch without full rewrite)
3. If fix-in-place fails, agent receives failure summary and attempts informed fresh retry
4. If retry fails after N attempts (typically 2–3), human intervention required; task is marked BLOCKED

**Critical rule:** A failing automated gate NEVER proceeds to human review. Automated gates are non-negotiable.

### Failure Mode 2: Acceptance Criteria Untraceable

**Response:** Task marked INCOMPLETE; human notifies agent.

If one or more acceptance criteria cannot be traced to both implementation and tests:
1. The spec is incomplete (AC was defined but agent didn't implement/test it)
2. Human review immediately surfaces this gap
3. Agent re-enters Implementation Phase for the missing AC
4. Task remains incomplete until all ACs are traceable

**Why this matters:** If a criterion is un-testable, it's un-verifiable. Verification gates exist to prove conformance; absence of evidence is evidence of absence.

### Failure Mode 3: Scope Creep Detected

**Response:** Request changes (don't force rejection).

If automated scope gate detects files modified outside the declared scope:
1. Human review surfaces exactly which files were touched
2. Human determines if creep is harmless (e.g., a formatting fix) or material (adds features not in the spec)
3. If material: request changes → Implementation Phase (agent removes out-of-scope changes)
4. If harmless: approve with comment explaining why the scope drift is acceptable

**Why permissive here:** Fixing a typo in an unrelated file is not a security risk. Being too strict gates blocks necessary tiny fixes.

### Failure Mode 4: Intent Drift (Code is Correct, But Wrong)

**Response:** Request changes; explain the gap.

Automated gates pass, but human review reveals the implementation misunderstands the design:

Example: "The AC says 'search results sorted alphabetically.' Tests pass. But the spec's design intent was to sort by relevance — the agent picked the simpler interpretation."

**Response:**
1. Human surfaces the intent gap with a specific comment
2. Agent re-enters Implementation Phase to correct the logic
3. Task returns from review state back to in-progress state

**Why human review is irreplaceable:** Intent cannot be fully encoded in testable form. A spec can define "what happened when I search for 'apple'," but the decision to optimize for relevance vs. simplicity is a design tradeoff that requires human judgment.

---

## Verification Cost and Bottlenecks

### Approval Bottleneck (S3.3.1)

Human review gates require synchronous human availability. When a reviewer is unavailable:

- **Single reviewer:** task blocks until reviewer returns
- **Parallel tasks:** tasks 2–4 block while reviewer handles task 1

At scale, this becomes the pipeline bottleneck. Techniques to mitigate:

**1. Async Review Windows**
- Define specific review hours (e.g., 10–11am, 2–3pm daily)
- Agents schedule completion for those windows
- Reduces context switching; batches reviews together

**2. Escalation Policies**
- Low-risk tasks (CRUD operations, well-tested features) auto-approve after 2 hours
- Medium-risk tasks require explicit approval within 4 hours
- High-risk tasks (security, schema changes) block indefinitely; require explicit reviewer

**3. Low-Risk Auto-Approval Lanes**
- Certain task categories (e.g., pure typo fixes, documentation updates) bypass human review
- Defined by task tags in tasks.md
- Still require all automated gates to pass

**4. Parallel Reviewers**
- Assign multiple reviewers per task category
- A change is approved once ANY assigned reviewer approves
- Reduces single-point-of-failure risk

### Authority Ambiguity (S3.3.2)

In multi-person teams, it is often unclear who is authorized to approve a given gate:

- Is the planning gate approved by product owner, architect, or both?
- Is the implementation gate approved by task author, peer, or senior engineer?
- Are schema changes approved by DBA, architect, or both?
- Are security-sensitive changes (auth, encryption) approved by security team, architect, or both?

**Undefined approval authority is functionally equivalent to no approval authority:** gates get bypassed informally because "I wasn't sure who to ask."

**Solution:** SDD requires explicit approval RACI matrix, defined per phase and per change type:

```markdown
# Approval Authority

| Phase | Standard Changes | Schema Changes | Security Changes | Architecture Changes |
|-------|------------------|----------------|------------------|----------------------|
| Planning | PM (A) | PM + DBA (A) | PM + Security (A) | PM + Architect (A) |
| Implementation | Engineer (A) | Engineer + DBA (A) | Engineer + Security (A) | Engineer + Architect (A) |
| Verification | Architect (A) | Architect + DBA (A) | Architect + Security (A) | Tech Lead (A) |

A = Approver (decision authority)
I = Informed (must read but not approve)
C = Consulted (input requested but not required)
```

Once defined, all review gates reference this matrix. "This is a schema change, so it needs Architect + DBA approval" becomes mechanical.

---

## Verification in CI/CD Integration

### Continuous Conformance (S6.4.2 Concept)

Verification gates should run continuously, not periodically:

- **On every commit:** Automated gates re-run (build, tests, linting)
- **On every branch:** Spec compliance check compares implementation against spec from which it was generated
- **Before merge:** Final human review gate confirms one last time

**Why continuous matters:** Spec drift is cumulative. If verification runs only monthly, weeks of drift go undetected. By then, the gap is larger and more costly to fix.

### Spec Drift Detection

A specific automated gate should detect when code has drifted from its specification:

1. Check which spec version the code was implemented from (commit message, branch name, or file marker)
2. Run acceptance criteria traceability against that spec version
3. If any AC cannot be traced to current implementation, flag as DRIFT DETECTED
4. Alert: "Code no longer matches spec version X. Either update spec or restore implementation."

This prevents the silent killer: code that passes all tests but violates the original specification.

---

## Verification as Proof of SDD

The key insight that distinguishes mature SDD from spec-first waterfall is this:

**Verification is not terminal; it is continuous.**

Each task has its own micro-gate. The final feature-level gate is a cumulative verification: if all N tasks have verified, the feature is verified by construction.

This is why SDD changes the economics of review. Traditional waterfall: 100 tasks complete, then one big review. SDD: 100 reviews of 1 task each. The same total review effort, but distributed, with failures caught early.

The ctxt.dev formulation (March 2026) captures this precisely:

> "The primary trust artifact should not be a diff. It should be an approved specification. The primary gate should not be 'does this look right to a reviewer.' It should be 'does this pass deterministic checks against the approved intent.' The primary evidence should not be review comments. It should be a signed conformance artifact."

This is the philosophical move from code review (subjective, expensive, late) to spec verification (objective, automated, early).

---

## Verification Checklist for Review Gates

Before approving an implementation, the human reviewer confirms all of the following:

- [ ] All automated gates pass (build, tests, architecture, security)
- [ ] All acceptance criteria are traceable to both implementation and tests
- [ ] Scope is bounded to declared files only
- [ ] Intent alignment: the code embodies the design, not a simpler misinterpretation
- [ ] Architecture coherence: integration with the codebase is clean
- [ ] Error handling: is meaningful and user-facing
- [ ] Performance: no obvious N+1, missing indexes, or algorithmic issues
- [ ] Code clarity: is the logic clear without requiring context-switching?
- [ ] Security: any auth/data/injection concerns (if applicable)
- [ ] Coverage: are edge cases tested, not just the happy path?

---

## Sources

- [Aviator Verify — Spec-Driven Code Review](https://docs.aviator.co/verify)
- [SpecWeave — Validation Workflow](https://spec-weave.com/docs/workflows/validation/)
- [Spec-Gated Delivery: Why PR Review Is the Wrong Trust Checkpoint for AI Code — ctxt.dev (2026-03-06)](https://ctxt.dev/posts/en/spec-gated-delivery)
- [Specwright: Spec-Driven Development That Closes the Loop — Obsidian Owl Engineering (2026-02-15)](https://obsidian-owl.github.io/engineering-blog/posts/specwright-spec-driven-development-that-closes-the-loop/)
- [ACDC Development Methodology: SDD + TDD + Human-in-the-Loop — Eastgate Software](https://eastgate-software.com/whitepapers/acdc-development/)
- [Spec-Driven Development with Claude Skills — Claude Skills Hub (2026-03-10)](https://claudeskills.info/blog/spec-driven-development-claude-skills/)
- [Acceptance Criteria / Spec-Driven Verification — Pydantic AI Harness Issue #80](https://github.com/pydantic/pydantic-ai-harness/issues/80)
- [The Dark Factory Pattern Part 3: Spec-Driven Development — env.dev (2026-03-09)](https://env.dev/guides/dark-factory-pattern-part-3)
- [fischmanb/auto-sdd — GitHub (autonomous SDD build loop)](https://github.com/fischmanb/auto-sdd)
- [Specification-Driven Development: How to Stop Vibe Coding — Pockit (2026-04-07)](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Spec-driven development: Unpacking one of 2025's key new engineering practices — Thoughtworks (2025-12-04)](https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [SDD Pilot — Reference Documentation](https://github.com/attilaszasz/sdd-pilot/blob/main/docs/reference.md)
- [SDD DevFlow — Complete development methodology — GitHub](https://github.com/pbojeda/sdd-devflow)
- [AIDLC Phase 3: Design — SwarmAI (DDD + SDD + TDD)](https://github.com/xg-gh-25/SwarmAI/blob/main/docs/AIDLC-Phase3-Design.md)
- [How to Build Human-in-the-Loop Approval Gates for AI Coding Agents — Code on Grass (2026-04-25)](https://codeongrass.com/blog/how-to-build-human-in-the-loop-approval-gates-ai-coding-agents/)
- [How to Review AI-Generated Code That Ships Faster Than You Can Read — DEV Community (2026-04-24)](https://dev.to/sahil_kat/how-to-review-ai-generated-code-that-ships-faster-than-you-can-read-6oj)
