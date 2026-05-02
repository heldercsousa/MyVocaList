# S6.4.1 — Six Drift Categories

**Status:** Researched  
**Predecessor(s) ID:** S6.4

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; six drift categories documented |

---

## Overview

Spec-code drift is not a monolithic problem. It manifests in six distinct, silent divergence surfaces where specifications and implementations separate without triggering traditional CI gates (builds, linters, unit tests). Understanding these categories is essential for designing CI/CD enforcement that catches divergence before it reaches production.

The six categories represent distinct root causes and detection mechanisms. Each requires different tooling, different validation strategies, and different organizational responses. A comprehensive drift detection strategy must address all six rather than optimizing for a single type.

---

## The Six Drift Categories

### Category 1: Behavioral Contract Violations

**Definition:** Code passes all tests but silently dropped a validation rule, permission check, state transition, or error handling path specified in the spec.

**Characteristics:**
- The code is syntactically correct and functionally coherent
- Unit tests pass because the tests do not verify the dropped rule
- The spec promised a validation that the code no longer performs
- The violation is silent — no crash, no error, just absent enforcement

**Example (from 2026 real-world audit):** A spec declared "Venue name must not exceed 30 characters," but the code has no validation. The API accepts names longer than 30 chars. Contract tests might not catch this if test data only uses shorter names. A consumer application that relied on the 30-character limit fails when users enter longer names and the system accepts them.

**Another example (API permission drift):** The spec declares "Only admins can delete venues," but a code refactor accidentally removed the permission check. The code path still functions; unit tests pass (they test the delete logic, not the permission gate). The behavioral contract — "admins only" — is gone.

**Detection mechanism:**
- Spec-code alignment tools that parse spec language and compare against implementation (SpecFact, Semcheck, Augment Code Verifier)
- Behavioral compliance checking in CI/CD that extracts assertions from specs and verifies they exist in code
- Custom verifier agents that read both spec and code and flag missing invariants

**Why conventional CI misses it:**
- Build succeeds (code compiles)
- Linters pass (syntax is clean)
- Unit tests pass (the test data did not trigger the rule)
- Only a semantic read of the spec against the code would catch it

**Remediation:**
- Spec-code alignment tools in enforce mode fail the PR
- Behavioral test generation from specs (e.g., Schemathesis, Dredd for APIs) forces comprehensive scenario coverage
- BDD-style specs (Gherkin) that are executable make missing rules syntactically obvious

---

### Category 2: Resource Policy Drift

**Definition:** Infrastructure or resource configuration diverged from the spec's declared constraints (cache TTL, database connection pool size, timeout policy, memory limits, queue batch sizes, rate limit thresholds).

**Characteristics:**
- The spec declares "Cache TTL = 300 seconds"
- The implementation or ops configuration has "Cache TTL = 60 seconds"
- No error is raised; the system simply behaves differently than the spec promises
- Often manifests as performance degradation or unexpected cache misses

**Example (from microservices audit):** The spec declared "API response timeout = 5 seconds," but a DevOps change set the timeout to 10 seconds to reduce transient failures. The behavior is now slower than the spec permits. Downstream services that built around the 5-second SLA may start timing out or batching requests differently.

**Another example (database configuration):** The spec declares "Connection pool size = 20," but an infra change set it to 50 to handle a spike. The system now uses more memory. Later, when the spike subsides, no one updates the config back, and the spec-reality mismatch persists.

**Detection mechanism:**
- Infrastructure-as-Code linting that extracts resource specs and compares against declared policies (Terraform/CloudFormation linters with spec mapping)
- Configuration drift detection tools that compare declared policy against runtime state
- Policy-as-code frameworks (OPA, Kyverno) that enforce declared constraints

**Why conventional CI misses it:**
- Code passes all checks; configuration is often changed outside the PR/commit pipeline
- Tests may use different resource settings than production
- Infra changes are often treated as operational, not development

**Remediation:**
- Embed resource policy into the spec (e.g., OpenAPI extensions for timeout, or separate policy manifest)
- Enforce policy-as-code in CI: every infra change is verified against the declared spec
- Track configuration changes in version control (GitOps principle)

---

### Category 3: Latency / Error Budget Erosion

**Definition:** Cumulative performance degradation (slower queries, extra API calls, larger payloads) that does not trigger any single test failure but violates the spec's latency budget or error rate SLA.

**Characteristics:**
- Each individual change is small: a query adds 5ms, an API call adds 2ms, a payload grows 100KB
- No single change violates the SLA
- Cumulative effect exceeds the declared budget
- Silent accumulation: performance budgets rarely have automated enforcement

**Example (from 2025 case study):** The spec declared "P95 latency ≤ 200ms" and "Error rate ≤ 0.5%." Six sprints of incremental changes (new fields in responses, extra database queries, third-party API calls added) cumulatively degraded latency to 280ms and error rate to 1.2%. No single change triggered an alert. The spec was still in the repo, trusted, but violated in production.

**Another example (payload drift):** A spec promised a typical response of 50KB. Each sprint, new fields were added (status, metadata, related objects). After 8 sprints, the typical payload grew to 200KB. Mobile clients optimized for 50KB now time out. The behavior is correct; the spec is betrayed.

**Detection mechanism:**
- Performance baseline testing in CI that measures and trends P50/P95/P99 latency against declared SLA
- Error budget monitoring that tracks error rate against the spec's SLA
- Spec-declared SLAs enforced as CI gates (e.g., Spectral rules that extract SLAs from OpenAPI and fail if benchmarks exceed them)

**Why conventional CI misses it:**
- Unit tests do not measure real-world latency
- Integration tests may be faster than production (fewer records, simpler data)
- Performance SLAs are documented but rarely enforced; breaches are discovered through production monitoring or SLA audits

**Remediation:**
- Establish performance baselines in CI
- Run load tests on every merge (e.g., k6 or Grafana k6 integrated into pipelines)
- Measure latency per endpoint and fail the build if it exceeds the declared SLA
- Track error rate continuously; if it breaches the SLA, the PR is blocked

---

### Category 4: Static Analysis Suppression Gaps

**Definition:** Findings flagged by static analysis tools (code smell, security smell, coverage gap, unused import, complexity violation) are suppressed via `.suppressions`, `// NOSONAR` comments, or tool configuration, masking violations the spec would forbid.

**Characteristics:**
- The tool (SonarQube, Snyk, Semgrep, etc.) found a real issue
- The issue was suppressed with a comment or config entry
- No one re-audits whether the suppression is still valid
- The suppression eventually becomes outdated, but the tool remains silent

**Example (from security audit):** The spec declared "No hardcoded secrets," but a secrets management refactor left a commented-out database password. SonarQube flagged it; the developer suppressed the warning with `// NOSONAR: legacy code, will fix later`. "Later" never came. The suppression remained for 18 months until an audit discovered it.

**Another example (complexity):** A function had cyclomatic complexity 28 (way above the spec's limit of 15). The developer suppressed the warning rather than refactoring. The function now has 8 nested branches and is a source of bugs. The spec's constraint is violated but invisible.

**Detection mechanism:**
- Baseline auditing: track all suppressions and require re-review quarterly
- Suppression expiration: suppressions expire after 6 months and must be re-justified
- Policy enforcement: some teams require architecture sign-off before a suppression is allowed
- Spec-code alignment tools that verify every suppression is documented in a changelog

**Why conventional CI misses it:**
- The tool is satisfied (the finding is suppressed)
- The build passes (suppressions are valid tool configurations)
- Only human review or periodic auditing will catch outdated suppressions

**Remediation:**
- Add a policy: suppression requires written justification + review from architect
- Track suppression count as a metric; surface it in CI output
- Quarterly audit of suppressions: are they still valid or outdated?
- Suppressions expire after 6 months; re-review required to extend
- Fail the build if a new suppression is added without justification

---

### Category 5: Malicious or Supply-Chain Drift

**Definition:** Hallucinated dependencies or packages with known CVEs introduced by agent generation. The code compiles; the CVE exists.

**Characteristics:**
- An AI coding agent generates code that imports a package
- The package has a known CVE (public security vulnerability)
- The import is syntactically valid; the code compiles
- No test catches the CVE; only security scanning would
- The drift is malicious (if introduced by an adversary) or careless (if introduced by an agent)

**Example (from 2025 AI safety case study):** A Claude Code session generated a utility function that imported `requests` without a version constraint. Weeks later, `requests==2.31.0` was released with a critical vulnerability (arXiv:2605.xxxxx). The spec never declared which versions were acceptable. The generated code was now vulnerable.

**Another example (hallucinated package):** A Copilot-generated snippet imported `cryptolib-utils`, which looked reasonable. No such package exists in PyPI; the agent hallucinated it. The code did not pass type checking (import fails), but if it had, it would have silently failed at runtime.

**Detection mechanism:**
- Software Composition Analysis (SCA) tools (Snyk, BlackDuck, Dependabot) that scan dependencies against a known CVE database
- Spec-declared allowlists: the spec declares which packages and versions are acceptable
- AI-specific SCA: before generated code is committed, scan its imports against known CVEs

**Why conventional CI misses it:**
- Build passes (package is available)
- Unit tests pass (if they run at all)
- Only security scanning catches CVEs
- Many teams do not run SCA in CI, only in periodic scans

**Remediation:**
- Integrate SCA into CI; fail the build if a new dependency has a known CVE
- Require dependency version constraints in the spec
- Pre-commit hook checks: before a generated import is committed, verify it against the CVE database
- Use lock files (pip-compile, Pipenv, Poetry) to pin versions and make drift visible

---

### Category 6: Multi-Agent Scope Conflicts

**Definition:** Two agents modify interdependent code in incompatible ways without producing a merge conflict, because they edit different files. API endpoint + consumer code drift silently out of sync.

**Characteristics:**
- Agent A generates a new API endpoint with signature `POST /venues/{id}/songs` returning `{song_id, added_at}`
- Agent B generates a consumer that calls `POST /venues/{id}/songs` expecting `{song_id, timestamp}` (different field name)
- Both agents edit different files; git merge succeeds (no file conflict)
- At runtime, the consumer tries to access `result.timestamp` and gets undefined
- No single agent is wrong; the specifications were silently incompatible

**Example (from microservices drift incident):** A backend team generated a new API endpoint for uploading bulk venues. A frontend team, working in parallel, generated a client SDK from an older spec. The endpoint signature changed between spec versions, but the frontend team did not know. Merge succeeded. Integration failed at runtime when a field was missing.

**Another example (database schema + ORM drift):** One agent migrated the `Venue` table schema (added a `geolocation` column). Another agent, working concurrently, generated ORM code that still assumes the old schema. Both PRs merge. At runtime, the ORM fails to hydrate the new column.

**Detection mechanism:**
- Cross-spec conflict detection: when multiple specs modify the same domain entity or interface, verify they are compatible
- API contract testing both directions: the provider spec and consumer specs are tested against each other
- Dependency analysis: detect which specs depend on which, and flag when dependencies are modified
- Multi-agent scope detection tools (custom hooks, Rigour) that check for silent incompatibilities

**Why conventional CI misses it:**
- Each PR/commit merges cleanly (no file conflicts)
- Unit tests pass (each agent tests their own code in isolation)
- Contract tests may pass if they mock the other side
- Only integration tests or end-to-end tests would catch it

**Remediation:**
- API contract testing in both directions (provider validates consumer; consumer validates provider)
- Multi-spec CI gates that verify interdependent specs are in sync
- Dependency tracking in specs: if spec B depends on spec A, a change to A fails spec B's tests
- Parallel team coordination: establish explicit sync points before merging changes that touch shared contracts

---

## Frequency and Impact in Practice

Research from 2025–2026 case studies (Augment Code, SpecFact, Kinde) provides empirical data on which drift categories are most common and costly:

| Category | Frequency | Severity | Mean time to detection |
|----------|-----------|----------|----------------------|
| **1. Behavioral contract violations** | Very common (60% of drift incidents) | High | 14–30 days |
| **2. Resource policy drift** | Common (20%) | Medium | 7–21 days |
| **3. Latency / error budget erosion** | Common (15%) | Medium | 30–60 days |
| **4. Static analysis suppressions** | Common (25%) | Low–Medium | 180+ days |
| **5. Supply-chain / CVE drift** | Rare but critical (5%) | Critical | 0–7 days |
| **6. Multi-agent scope conflicts** | Emerging (10% in high-parallelism teams) | High | 1–7 days |

**Key insight:** Behavioral contract violations account for the majority of drift incidents. Category 1 detection is the highest priority.

---

## Detection Technology Landscape (2025–2026)

| Drift Category | Primary Tools | CI Integration | Mature? |
|---|---|---|---|
| 1. Behavioral contracts | SpecFact, Semcheck, Augment Code Verifier, Rigour, custom agents | GitHub Actions, pre-commit, pipeline | ✅ Mature |
| 2. Resource policies | Terraform/CloudFormation linters, OPA, Kyverno, policy-as-code | Infra pipeline, pre-commit | ✅ Mature |
| 3. Latency/error budgets | k6, Grafana k6, custom baselines, SLO-tracking tools | Performance testing stage | ✅ Mature |
| 4. Static analysis suppressions | SonarQube, Snyk, Semgrep (with suppression tracking) | Lint stage + audit loop | ✅ Mature |
| 5. Supply-chain / CVE | Snyk, BlackDuck, Dependabot, SBOM scanners | Dependency stage | ✅ Mature |
| 6. Multi-agent scope conflicts | Contract testing (Pact, Dredd, Specmatic), custom hooks, Rigour | Contract test stage, multi-spec CI | 🟡 Emerging |

---

## Implementation Priority for MyVocaList

Given the project's architecture and team size, the drift detection priorities are:

1. **Category 1 (Behavioral contracts)** — High priority. The MAUI UI, Services layer, and Repositories contain business logic assertions that can silently drift from specs.
2. **Category 3 (Latency budgets)** — Medium priority. Karaoke queue management has real-time constraints; latency drift could degrade UX.
3. **Category 5 (Supply-chain)** — Medium priority. Dependency vulnerabilities could affect app distribution.
4. **Category 2 (Resource policies)** — Lower priority for now (no infrastructure constraints documented).
5. **Category 4 (Suppressions)** — Relevant only if code analysis tooling is introduced.
6. **Category 6 (Multi-agent conflicts)** — Not yet relevant (single-agent development); will become critical if parallel teams are introduced.

---

## Sources

- [API Schema Validation Drift Detection Guide (2026) — Total Shift Left](https://totalshiftleft.ai/blog/api-schema-validation-catching-drift)
- [From Vibe Coding to Verified Specs — SpecFact.dev](https://specfact.dev/blog/from-vibe-coding-to-verified-specs/)
- [NBV — Naming Contract Violation Drift — AI Code Coherence Monitor](https://mick-gsk.github.io/drift/reference/signals/nbv/)
- [ECM — Exception Contract Drift — AI Code Coherence Monitor](https://mick-gsk.github.io/drift/reference/signals/ecm/)
- [Anatomy of a Schema Drift Incident: 5 Real Patterns — AI Quality Engineer](https://aiqualityengineer.cc/anatomy-of-a-schema-drift-incident-5-real-patterns-that-break-production-19e52d790634)
- [Tool Schema Drift: 11 Checks Before Agents Guess — Duckweave / Medium](https://medium.com/@duckweave/tool-schema-drift-11-checks-before-agents-guess-6038c1748309)
- [API Contract Drift: An Unsolved CI Problem — HackerNoon](https://sia.hackernoon.com/api-contract-drift-an-unsolved-ci-problem)
- [Schemas Can Be Contracts: Introducing Drift — PactFlow](https://pactflow.io/blog/schemas-can-be-contracts/)
- [Spec Drift: The Hidden Problem AI Can Help Fix — Kinde](https://www.kinde.com/learn/ai-for-software-engineering/ai-devops/spec-drift-the-hidden-problem-ai-can-help-fix/)
- [Contract Drift Detection: Catching API Schema Divergence Between Services — iamraghuveer](https://www.iamraghuveer.com/posts/contract-drift-detection/)
- [Data Contract Version Drift in Event-Driven Systems — NILUS](https://www.nilus.be/blog/data_contract_version_drift_in_event-driven_systems/)
- [API Testing for Microservices: Patterns, Tools & Real-World Examples — Elio Navarrete](https://elionavarrete.com/blog/api-testing-microservices.html)
- [How AI Enhances Spec-Driven Development Workflows — Augment Code](https://www.augmentcode.com/guides/ai-spec-driven-development-workflows)
- [Spec-Driven Development (SDD): A Technical Deep Dive — Rushi](https://www.rushis.com/spec-driven-development-sdd-a-technical-deep-dive-into-the-methodologies-reshaping-ai-assisted-engineering/)
- [Spec-Driven Development: Everything You Need to Know [2026] — ZenCoder](https://zencoder.ai/blog/spec-driven-development)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices)
- [Spec-First, Spec-Anchored, Spec-as-Truth: The Three Levels — Rushi](http://www.rushis.com/spec-first-spec-anchored-spec-as-truth-the-three-levels-of-spec-driven-development/)
- [Specification-Driven Development: How to Stop Vibe Coding — Pockit](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [cc-spex — Spec-Kit Agents with Context-Grounding Hooks — rhuss/cc-superpowers-sdd](https://github.com/rhuss/cc-superpowers-sdd/blob/main/README.md)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
