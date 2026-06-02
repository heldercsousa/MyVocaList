# S6.4 — CI/CD Integration

**Status:** Researched
**Predecessor(s) ID:** S6

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written |

---

## Overview

CI/CD integration transforms spec-code conformance from a session-time concern (hooks, review gates) into a merge-time concern. Where hooks and reviews operate during generation, CI gates run on every push and block merge if the pipeline detects divergence. This final enforcement layer is critical because agents can mark tasks complete without executing verification checks (see S5.3.1 — Silent Task Completion), and CI provides a structural backstop that runs independent of agent diligence.

The canonical pattern for SDD CI/CD integrates four sequential validation layers:

1. **Spec validation:** Ensures the specification itself is structurally sound
2. **Contract testing:** Verifies that live behavior matches the spec contract
3. **Spec-code alignment checking:** Detects behavioral or implementation drift
4. **Multi-agent scope conflict detection:** Ensures parallel agents' changes compose safely

Together, these gates convert the spec from a document into an enforced contract.

---

## S6.4.1 — Six Drift Categories

The Augment Code analysis (2026) and post-incident SDD case studies identify six distinct categories of silent divergence that conventional CI (build, lint, unit tests) does not catch:

| # | Drift category | Description | Detection mechanism |
|---|----------------|-------------|---------------------|
| 1 | **Behavioral contract violations** | Code passes all tests but silently dropped a validation rule, permission check, or state transition specified in the spec | Spec-code alignment tools (SpecFact, Semcheck, Verifier agents) |
| 2 | **Resource policy drift** | Infrastructure or storage configuration diverged from the spec's resource constraints (e.g., cache TTL, database connection pool size, timeout policy) | Infrastructure-as-Code linting + spec mapping |
| 3 | **Latency / error budget erosion** | Cumulative performance degradation (slower queries, extra API calls, larger payloads) that does not trigger any single test failure but violates the spec's latency or error budget | Performance baseline testing + spec-declared SLAs |
| 4 | **Static analysis gaps** | Findings flagged by static analysis tools (code smell, security smell, coverage gap) are suppressed via `.suppressions`, masking violations the spec would forbid | Baseline auditing — track what is suppressed and force re-review |
| 5 | **Malicious or supply-chain drift** | Hallucinated dependencies or packages with known CVEs introduced by agent generation. The code compiles; the CVE exists. | Software Composition Analysis (SCA) + known CVE database |
| 6 | **Multi-agent scope conflicts** | Two agents modify interdependent code in incompatible ways without producing a merge conflict, because they edit different files (e.g., API endpoint + consumer code drift silently out of sync) | Cross-spec conflict detection, API contract testing between consumers/providers |

Category 1 is the most common in practice. Categories 5 and 6 are emerging in high-parallelism SDD environments.

---

## S6.4.2 — Continuous Conformance Requirement

Spec-code drift is not a threshold phenomenon. It does not accumulate until it crosses a visible line and then become obvious. It compounds. A 1% drift today (one spec clause not enforced) creates a 2% drift after the next sprint because subsequent agents read the already-drifted code and accept it as ground truth.

The Kinde analysis (2025) makes this explicit: "If the spec doesn't keep pace with code changes, or code doesn't keep pace with spec changes, you end up with a dangerous gap between what you say your software does and what it actually does."

Continuous conformance means:
- **Drift detection runs on every push, not periodically.** Weekly or monthly checks allow drift to compound between scans.
- **The spec is source of truth; code that diverges fails the gate.** When a conflict arises, the code is corrected or the spec is formally updated through change control — never ignored.
- **When drift is detected, the correction is applied to code.** The spec is only updated if there is an intentional requirement change, reviewed and approved separately.
- **Drift detection tools operate in enforce mode, not observe mode, in production pipelines.** Observe mode (audit-only) produces reports but permits merge; it is appropriate only for baseline calibration periods.

SpecFact's staged rollout model (observe → enforce) reflects operational practice: teams add CI conformance checks in observe mode first to measure false positive rates, then promote to enforce mode once confidence is established. Starting in enforce mode on an existing codebase risks blocking legitimate work during the calibration period.

---

## S6.4.3 — Multi-Layer Pipeline Architecture

Modern SDD CI/CD pipelines follow a structured progression of gates:

### Stage 1: Specification Validation

Validates that the specification itself meets structural and semantic requirements before code generation or merge proceeds.

**Tools:** Spectral, Vacuum, OpenAPI linters, custom rule engines
**Checks:**
- EARS notation compliance (for requirement specs)
- Schema validation (for API specs)
- Required fields present (summary, acceptance criteria, tasks)
- Internal consistency (no contradictory constraints)
- Traceability (every task links back to a requirement)

**Failure mode:** Spec is rejected before reaching implementation. Cost of fixing is minimal (usually hours).

```yaml
# Example: OpenAPI linting in GitHub Actions
- name: Lint OpenAPI Spec
  run: |
    npx spectral lint api-spec.yaml
  if: github.event_name == 'pull_request'
```

### Stage 2: Backward Compatibility Checking

For APIs and contracts, verify that the spec change does not break existing consumer contracts.

**Tools:** Specmatic, Pact, Dredd (contract testing), semcheck
**Checks:**
- New endpoints do not break existing ones
- Response schema changes are backward compatible
- Deprecation path is clear if removing functionality

**Failure mode:** Consumer-breaking change is detected at PR review time, not in production when a client breaks.

### Stage 3: Contract Testing (Provider-Driven)

Runs the live implementation against the specification contract. Verifies that every API endpoint, every response, and every state transition matches what the spec declares.

**Tools:** Dredd, Schemathesis, Total Shift Left, generated tests from OpenAPI
**Checks:**
- Responses match declared schema
- Status codes match spec
- All documented endpoints are implemented
- All documented response variants exist
- Validation rules in spec are enforced

**Failure mode:** Implementation diverges from spec. Example: a validation constraint was dropped, but the endpoint still returns 200 instead of the 400 the spec promises.

```yaml
# Example: Contract testing in GitHub Actions
- name: Run Dredd Contract Tests
  run: |
    dredd api-spec.yaml http://localhost:3000
  if: github.event_name == 'pull_request'
```

### Stage 4: Behavioral Compliance Checking

Compares the generated code against the specification at the semantic level, not just the interface level. Verifies that the behavior and business logic align with the spec's intent.

**Tools:** SpecFact, Semcheck, Augment Code Verifier, Rigour, custom agents
**Checks:**
- Validation rules implemented match spec language
- Error handling paths match spec's error responses
- State transitions follow spec's declared flow
- Permission checks and security constraints enforced
- No features silently dropped

**Example detection:** The spec says "Venue name must not exceed 30 characters," but the code has no validation. Contract test might not catch this if the test suite passes shorter names. Behavioral checking detects the missing validation rule.

**Failure mode:** Silent feature loss. Code compiles, tests pass, but a business rule was forgotten.

### Stage 5: Multi-Agent Scope Conflict Detection

For parallel development teams or multi-spec repositories, verify that changes from different agents/specs do not conflict silently.

**Tools:** Custom hooks, dependency analysis, scope-aware linters
**Checks:**
- Two specs modifying the same file do not produce incompatible results
- Consumer and provider API contracts stay in sync (API contract testing both directions)
- Shared domain entities are modified consistently across specs
- Database schema changes from different specs compose without conflict

**Failure mode:** Merge conflict surfaces days or weeks later when a consumer tries to integrate with a provider that changed under different assumptions.

---

## S6.4.4 — Tooling Landscape (2025–2026)

| Tool | Category | Primary use | Integration |
|------|----------|------------|-----------|
| **Spectral** | Spec linting | OpenAPI, JSON Schema validation | Pre-commit, GitHub Actions, GitLab CI |
| **Semcheck** | Behavioral compliance | AI-powered spec-code comparison | Pre-commit hooks, CLI, CI pipelines |
| **SpecFact** | Drift detection + enforcement | Analyze code, find hidden specs, run checks | GitHub Actions, Azure DevOps, Jira sync |
| **Dredd** | Contract testing | API contract verification against OpenAPI | Docker, CLI, CI/CD integration |
| **Schemathesis** | Property-based API testing | Generate and run fuzz tests from OpenAPI | pytest plugin, CLI, CI/CD |
| **Rigour** | Multi-layer quality gates | 27+ quality gates, real-time hooks, scope detection | All major CI platforms, all AI tools |
| **Pact** | Consumer-driven contracts | Multi-consumer API verification | JVM, .NET, Node.js, Go, Python, CLI |
| **Specmatic** | Contract + backward compatibility | Lint, examples, compatibility checking | Docker, CLI, Gradle, Maven |
| **Total Shift Left** | Spec-driven test generation | Generate tests from OpenAPI, run in CI | GitHub Actions, Azure DevOps |
| **SpecWeave** | SDD GitHub Actions integration | Auto-generate specs, validate PRs, enforce test coverage | GitHub Actions (native) |
| **Augment Code / Intent** | Living spec + Verifier | Spec layer + code generation verification | GitHub Actions, Slack notifications |

---

## S6.4.5 — Implementation Patterns

### Pattern 1: Spec-First Enforcement

Gates run in this order:
1. Spec validation (linting, schema check, EARS notation)
2. Spec approval required before code generation permitted
3. Contract testing on every code commit
4. Behavioral compliance checking before merge

**Appropriate for:** New features, APIs with external consumers, services handling sensitive data

### Pattern 2: Brownfield Retrofit (Observe → Enforce)

For existing codebases adding SDD retrospectively:
1. Deploy all gates in **observe mode** — they report violations but do not block
2. Run for 1–2 sprints to establish baseline false positive rate and tune rules
3. Fix accumulated violations in a dedicated sprint
4. Promote gates to **enforce mode** — violations now block merge

**Why this matters:** Enabling all gates at enforce on day one blocks 50+ PRs per day in a large codebase. Observe mode lets teams calibrate before strictness takes effect.

### Pattern 3: Progressive Enforcement Tiers

Different gates apply at different branch targets:

```
PR to feature branch  → Lint only (fast feedback)
PR to develop         → Lint + contract tests
PR to main            → Lint + contract + compliance + multi-agent checks (slowest, most thorough)
```

This reduces friction on exploratory branches while maintaining high gates for production-bound code.

### Pattern 4: Dual Verification (Pre-Push + CI Gate)

Verifier agents run twice:
1. **Locally before push** (agent-time, see S6.4.3 Stage 4) — catches violations early
2. **In CI on every PR** (merge-time, structural enforcement) — catches violations the agent missed

The second run is non-optional; agents demonstrably skip or falsely complete verification steps. CI is the backup.

---

## S6.4.6 — Conformance Cost and Calibration

### Runtime Cost

Modern CI gates are fast:
- **Spec linting:** 5–10 seconds
- **Contract testing:** 30–60 seconds (depends on endpoint count)
- **Behavioral compliance:** 10–30 seconds (AI-powered, typically Haiku model)
- **Multi-agent scope detection:** 5–15 seconds

Total per-PR cost: ~60–120 seconds for a full gate suite. On a 500-PR/month team, this is ~1.5–2 hours of aggregate CI time per month.

### False Positive Calibration

Drift detection tools in the wild report false positive rates of 3–8% initially (Augment Code case study, 2026). False positives are configuration mismatches or over-broad rules. The calibration process:

1. **Run in observe mode for 2 weeks.** Collect all reported violations.
2. **Audit 10% of violations (stratified).** Are they real or false alarms?
3. **Adjust rules to eliminate systematic false positives.** Common culprits: overly strict validation patterns, specs that permit multiple implementation strategies but the tool expects one.
4. **Re-run observe mode for 1 week.** Verify false positive rate dropped.
5. **Promote to enforce mode.** Set CI to block on violations.

Skipping calibration results in developer frustration and pressure to disable gates. The 1–2 week calibration investment pays for itself in unblocked productivity.

### Operational Runway

Once gates are in enforce mode, violations should decline over time. A healthy trajectory is:

- **Week 1 (day of activation):** 5–10% of PRs fail conformance gates. Developers are learning rules.
- **Week 3:** 1–3% fail. Team understands expectations.
- **Week 8+:** <0.5% fail. Violations are genuine issues, not misunderstandings.

If failure rate stays high (>5% after 4 weeks), re-audit the rules. They may be incorrect or overly strict.

---

## S6.4.7 — Failure Modes and Remediation

### Failure 1: Gate Noise ("It Blocks Every Other PR")

**Cause:** Rules are too broad, or false positive rate was underestimated.

**Remediation:**
- Revert gates to observe mode
- Audit 20% of violations (larger sample than calibration)
- Identify the rule(s) causing 80% of false positives
- Tighten rule scope or remove rule
- Re-calibrate for 1 week
- Promote back to enforce

### Failure 2: Silent Bypass (Developers Suppress Warnings)

**Cause:** Violations are real but developers add `.suppressions` or `// NOSONAR` comments to bypass gates.

**Remediation:**
- Add a policy: suppression requires written justification + review from architect
- Track suppression count in CI output — surface as a metric
- Quarterly audit of suppressions: are they still valid or outdated?
- Suppressions expire after 6 months; re-review required

### Failure 3: Drift Compounds During Gate Downtime

**Cause:** Gates were disabled for maintenance, and drift accumulated faster than baseline.

**Remediation:**
- Enable gates immediately after maintenance
- Run gates in observe mode for the next 3–5 PRs to catch backlog drift
- File issues for all detected violations
- Promote back to enforce

---

## S6.4.8 — Integration with Spec-Driven Workflow

CI gates are most effective when integrated with the full SDD workflow from S3.x:

```
Spec Review (S6.3)
    ↓
Spec Validation (S6.4 Stage 1) ← CI gate 1
    ↓
Code Generation
    ↓
Contract Testing (S6.4 Stage 3) ← CI gate 2
    ↓
Behavioral Compliance (S6.4 Stage 4) ← CI gate 3 (dual verification: pre-push + CI)
    ↓
Multi-Agent Conflict Check (S6.4 Stage 5) ← CI gate 4
    ↓
Merge to main
```

The critical insight: every gate before merge is an opportunity to block broken work early. Cost of fixing at merge time is high; cost of fixing during generation is low. Gates should front-load detection.

---

## Sources

- [CI/CD for AI Agents — Augment Code](https://www.augmentcode.com/guides/cicd-ai-agents-pipeline-integration)
- [Spec Drift: The Hidden Problem AI Can Help Fix — Kinde](https://www.kinde.com/learn/ai-for-software-engineering/ai-devops/spec-drift-the-hidden-problem-ai-can-help-fix/)
- [Semcheck — AI-Powered Specification Compliance Tool](https://semcheck.ai/)
- [SpecFact — Review AI-assisted code before drift reaches PR or main](https://specfact.com/)
- [Rigour — Deterministic quality gates for AI-generated code](https://docs.rigour.run/)
- [Total Shift Left — API Schema Validation Drift Detection Guide (2026)](https://totalshiftleft.ai/blog/api-schema-validation-catching-drift)
- [Specmatic — Continuous Integration](https://docs.specmatic.io/references/continuous_integration)
- [Contract Testing Plan: From OpenAPI to CI — Spec Coding](https://spec-coding.dev/blog/contract-testing-plan-from-openapi-to-ci)
- [SpecWeave — GitHub Actions Integration Setup Guide](https://spec-weave.com/docs/guides/github-action-setup)
- [SpecWeave — Validation Workflow](https://spec-weave.com/docs/workflows/validation/)
- [How to Generate API Tests from OpenAPI Spec (2026) — Total Shift Left](https://totalshiftleft.ai/blog/how-to-generate-api-tests-from-openapi)
- [SpecFact — CI/CD Pipeline Module Documentation](https://modules.specfact.io/guides/ci-cd-pipeline/)
- [SDD Plugin for Claude Code — Agents (Guardrails & Verification)](https://www.mintlify.com/noelserdna/claude-plugin-sdd/automation/agents)
- [From Vibe Coding to Verified Specs — SpecFact.dev](https://specfact.dev/blog/from-vibe-coding-to-verified-specs/)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices)
- [SDD (Spec-Driven Development) CI/CD Pipeline — bhargavvc/sdd](https://github.com/bhargavvc/sdd/blob/main/docs/ci-cd-pipeline.md)
- [Agents: Spec Kit Agents with Context-Grounding Hooks — arXiv:2604.05278](https://www.arxiv.org/pdf/2604.05278)
- [SDD Plugin for Claude Code — Guardrails (Hooks & Agents)](https://noelserdna-claude-plugin-sdd.mintlify.app/automation/guardrails)
- [Spec-Driven Development Plugin for Claude Code — LiorCohen/sdd](https://github.com/LiorCohen/sdd)
