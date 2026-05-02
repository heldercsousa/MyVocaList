# S5.2.2 — Cross-Agent Spec Conflicts

**Status:** Researched
**Predecessor(s) ID:** S5.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written |

---

## Overview

Cross-agent spec conflicts occur when two agents working on interdependent tasks produce outputs that are locally correct against their individual task specs but globally contradictory when integrated. Unlike dependency ordering fragility (S5.2.1), which manifests as hidden dependencies that only surface at runtime, cross-agent spec conflicts emerge because agents interpret the same shared specification in incompatible ways and have no mechanism to discover this misalignment until their outputs are merged.

This is one of the most damaging failure modes in parallel agent execution. Both agents' outputs pass local verification — they satisfy their individual task specs. The conflict only surfaces at integration, when the system-wide incompatibilities become visible (type mismatches, field naming conflicts, contradictory API contracts, incompatible state schemas). A verifier testing each agent's output in isolation cannot catch these semantic conflicts unless the shared specification is precise enough to prevent multiple interpretations.

---

## The Core Problem

### Manifestation

Agent A implements Service X and produces a DTO `UserProfileDto` with fields `{id, name, status}`. Agent B, running simultaneously on Service Y that depends on Service X, reads the same shared requirement ("return user profile") and independently generates code expecting `{id, title, active}` — a subtly different field naming convention. Both agents pass their local Verifier. The system integrates, and the mismatch breaks at runtime or during integration testing.

### Why It Happens

Parallel agents working from the same spec make independent decisions about details the spec does not fully nail down:

1. **Field naming conventions.** The spec says "user status" but does not prescribe whether that becomes `status`, `user_status`, `active`, `state`, or `condition`. Agent A chooses `status`; Agent B chooses `active`.

2. **Data type choices.** The spec says "a list of items" but does not specify array vs. linked list vs. dictionary keyed by ID. Agent A uses `List<Item>[]`; Agent B builds code assuming `Dictionary<int, Item>`.

3. **Enum vs. string vs. bool.** For a "published/draft" state, the spec does not specify whether this is an `enum PublishStatus { Published, Draft }`, a `string`, or a `bool isPublished`. Agent A uses `enum`; Agent B uses `bool`.

4. **Error codes and exception types.** The spec says "return an error if the item is not found" but does not define the error code, message format, or exception type. Agent A throws `ItemNotFoundException` with code 404; Agent B returns a tuple `(success: false, code: "NOT_FOUND")`.

5. **API contract details.** For HTTP endpoints, the spec says "get user by ID" but does not specify the URL path (`/users/{id}` vs. `/api/users/{id}` vs. `/v1/user/{id}`), request/response wrapping, pagination semantics, or status codes for edge cases.

6. **Configuration keys and defaults.** The spec says "configurable retry count" but does not define the config key name, default value, or unit (milliseconds vs. seconds).

These details matter. When Agent A calls Service X expecting a `status` field and Agent B's implementation provides `active`, the integration breaks. When Agent A wraps responses in `{ data: {...}, meta: {...} }` and Agent B expects flat `{...}`, serialization fails. When Agent A uses codes from one enumeration and Agent B expects codes from a different enumeration, logic breaks.

### Research Finding: Specification Completeness Is the Dominant Factor

A 2025–2026 academic study (arXiv:2603.24284) analyzed multi-agent code generation across 51 class-generation tasks, progressively removing specification detail from full docstrings (L0) to bare signatures (L3) and introducing opposing structural biases to stress-test integration.

**Key results:**
- Two-agent integration accuracy drops from 58% to 25% as specification detail is removed
- A single-agent baseline degrades more gracefully: 89% to 56%
- This creates a persistent 25–39 percentage-point "coordination gap" that is consistent across two Claude models (Sonnet and Haiku)
- An AST-based conflict detector achieves 97% precision at identifying type and state conflicts—but providing conflict reports to a merger agent adds **zero measurable benefit**
- **Full specification alone recovers the single-agent ceiling (89%)**, while conflict reports without specification actually hurt performance (−6.6pp at the strongest spec level)

The conclusion: **The specification is the sufficient and necessary coordination instrument.** Conflict detection tools are valuable for diagnosis but do not improve repair outcomes. Richer specs are both the primary coordination mechanism and the recovery instrument.

---

## Types of Cross-Agent Spec Conflicts

Research and production experience identify the following conflict categories:

### 1. Type and Data Structure Conflicts

**Type conflicts:** Agent A implements a field as `int id`, Agent B as `string id`. Integration fails or produces silent data corruption.

**Structure conflicts:** Agent A uses `List<Item>`, Agent B uses `Dictionary<string, Item>`. LINQ queries fail; iteration patterns produce wrong results.

**Detection cost:** Zero — AST-based parsers reliably catch these through static analysis.

**Mitigation:** The spec must specify type signatures. `string user_id` is more precise than "a unique identifier for the user." DTO contracts should be defined once and referenced by both agents.

### 2. Field Naming and Enumeration Conflicts

**Field naming:** "user status" becomes `status` vs. `user_status` vs. `state` vs. `active`. Serializers fail if the spec does not pin the exact name.

**Enum values:** A boolean `is_active` in one agent becomes `{ enum UserStatus { Active, Inactive } }` in another.

**Semantic naming:** Agent A names a field `count`, Agent B names it `total_count`. Both are "correct" if the spec says "a number," but integration fails.

**Mitigation:** Specs must include the actual field names and types that agents will generate. Example:

```
The UserProfileDto must include:
- id: UUID (required)
- name: string (required, max 100 chars)
- status: enum Status { Active, Inactive, Suspended } (required)
- created_at: datetime (required)
```

Not:

```
The UserProfileDto contains profile information including the user's status.
```

### 3. API Contract Conflicts

**URL path conventions:** `/users/{id}` vs. `/api/users/{id}` vs. `/v1/users/{id}`.

**Request/response wrapping:** Agent A returns `{ data: user }`, Agent B returns `user` directly.

**Pagination:** Agent A implements `limit/offset`, Agent B implements `page/size`. Frontend code fails.

**Status codes:** Agent A returns 404 for "not found," Agent B returns 200 with a null payload.

**Error format:** Agent A returns `{ error: string }`, Agent B returns `{ message: string, code: number }`.

**Mitigation:** API specs must include OpenAPI/Swagger definitions or equivalent JSON schemas. Example contracts must be provided before agents implement.

### 4. Configuration and DI Registration Conflicts

**Configuration keys:** The spec says "configurable timeout" but does not define the key name. Agent A registers `services.AddScoped<IUserService>()`, Agent B registers `services.AddSingleton<IUserService>()` (wrong lifetime).

**DI scope:** Agent A assumes singleton, Agent B assumes scoped. Thread-safety assumptions differ.

**Initialization order:** Agent A registers `ILoggerService` before `IAuthService` (which depends on logging), Agent B reverses the order. Circular dependency or initialization failure.

**Mitigation:** The spec must define:
- Dependency lifetimes (singleton, scoped, transient)
- Configuration key names and expected types
- DI registration order for interdependent services
- Required initialization hooks and their sequence

### 5. Error Handling and Recovery Semantics

**Exception types:** Agent A throws domain exceptions (`UserNotFoundException`), Agent B uses tuple returns `(success, message)`.

**Retry behavior:** Agent A implements exponential backoff with jitter, Agent B implements fixed retry intervals. One agent starves the other.

**Cancellation semantics:** Agent A cancels via `CancellationToken`, Agent B via a `volatile bool`. Cancellation propagation fails.

**Logging levels:** Agent A logs failures at Error level, Agent B at Warning level. Monitoring rules based on log levels fail.

**Mitigation:** Specs must define the error contract:
- Exception types or result tuple format
- Retry policies and backoff curves
- Cancellation propagation mechanism
- Logging severity for each failure class

---

## Mitigation Strategies

### Strategy 1: Shared Contracts Defined in the Spec Before Implementation Begins

The most effective mitigation: DTOs, API contracts, error code enumerations, and event schemas must be specified in the shared spec, not left to each agent's judgment.

**Implementation:**
- Write a `contracts.md` file in the spec that defines all shared types, interfaces, and enumerations
- Use OpenAPI/AsyncAPI for APIs; JSON Schema for data structures
- Specify exact field names, types, and validation rules
- Include example payloads

Example:

```markdown
## Shared Contracts (contracts.md)

### Data Transfer Objects

#### UserProfileDto
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "id": { "type": "string", "format": "uuid" },
    "name": { "type": "string", "minLength": 1, "maxLength": 100 },
    "status": { "enum": ["active", "inactive", "suspended"] },
    "created_at": { "type": "string", "format": "date-time" }
  },
  "required": ["id", "name", "status", "created_at"]
}
```

### API Contracts

#### GET /api/v1/users/{user_id}
- Request: UUID in path as `user_id`
- Response: 200 with UserProfileDto
- Error: 404 if user not found, with error body:
```json
{
  "error_code": "USER_NOT_FOUND",
  "message": "User with ID {user_id} does not exist",
  "timestamp": "2026-05-02T..."
}
```
```

**Benefits:**
- Both agents have a machine-readable contract
- Spec becomes the single source of truth for shared types
- Mismatches surface at verification time (type checkers, schema validators) not integration time

**Cost:**
- Requires upfront investment in contract definition
- Specs become more verbose (necessary trade-off)
- Must be kept in sync as requirements evolve

### Strategy 2: Living Spec Updates as Implementation Progresses

When an agent makes a spec-level decision (choosing a DTO field name, deciding on a retry policy), that decision must be written back to the spec before parallel agents consume the same interface.

**Implementation:**
- Agents working on parallel tasks have a "discovery brief" requirement: before submitting work, they update the shared spec with any decisions they made
- The Coordinator inspects the spec update before unblocking downstream agents
- If two agents make contradictory decisions, the Coordinator detects the conflict in the spec, not at code merge time

**Example workflow:**
1. Wave 1: Agent A implements UserService and discovers it needs a `retry_policy` field in UserProfileDto. It updates `contracts.md` with this decision.
2. Coordinator reads the spec update and injects it into Wave 2 agents' spawn prompts
3. Wave 2: Agent B reads the updated spec and sees the `retry_policy` field. It generates code that matches Agent A's decision.

**Benefits:**
- Decisions are visible and auditable
- Downstream agents inherit knowledge of upstream decisions without guessing
- Specs stay synchronized with implementation

**Cost:**
- Requires discipline: agents must update the spec, not just the code
- Coordinator must monitor spec changes for consistency
- If an agent makes a wrong decision, the spec updates reflect that wrong decision (requires fallback to full re-specification if discovered)

### Strategy 3: Verifier Validates Cross-Service Contracts, Not Just Local Output

The Verifier's job is to check against the full system specification, not just local diffs.

**Implementation:**
- After each agent completes, the Verifier runs two passes:
  1. **Local verification:** Does the code satisfy its own task spec?
  2. **Integration verification:** Does the code match all shared contracts? Do the data types match? Do the API signatures match? Do the error codes align?

- For integration verification, the Verifier loads the shared `contracts.md` and type-checks generated code against it
- The Verifier rejects code that passes local verification but violates shared contracts

**Tools:**
- Static type checkers (TypeScript's `tsc`, C#'s `dotnet build`)
- Schema validators (JSON Schema, OpenAPI linters)
- Interface compliance checkers (check that returned DTO matches the schema)
- Cross-service contract auditors (custom scripts that verify DTO fields, API paths, error codes across agents' outputs)

**Example:**
```bash
# After Agent A completes UserService:
typescript-compiler --lib contracts/user-profile-dto.ts src/services/UserService.ts
# Verify that UserService returns UserProfileDto with correct fields

# After Agent B completes UserController:
openapi-linter api/openapi.yaml
# Verify that API paths match the contracts.md specification

# Cross-service verification:
contract-auditor --verify-all-agents src/
# Check that all generated DTOs, API paths, error codes are in contracts.md
```

**Benefits:**
- Integration issues surface early, before merge
- Verifier enforces spec compliance globally, not just locally

**Cost:**
- Requires tools and setup for cross-service validation
- Must be run after each agent completes (adds latency to the wave)

---

## Real-World Patterns and Cases

### Case 1: Field Naming Divergence (Medium / specfact-cli)

A team running parallel agents on a CLI refactor found that one agent implemented `--timeout-ms` while another agent implemented `--timeoutMs`. The spec said "configurable timeout" but did not specify the flag name.

**Resolution:** The team retrofitted a `CLI_FLAGS.md` spec file that pinned exact flag names, types, and defaults. Subsequent agents referenced this file and avoided divergence.

**Lesson:** Spec-first approach requires specs for every shared surface (flags, config keys, DTOs, API paths).

### Case 2: Service Lifetime Conflict (DDD/MAUI Project)

Two agents working on DI registration chose different service lifetimes:
- Agent A registered `IUserRepository as Singleton` (cached reads)
- Agent B expected `IUserRepository as Scoped` (per-request isolation)

The mismatch broke when concurrent requests corrupted shared state in Agent A's implementation.

**Resolution:** The team added a `DI_MANIFEST.md` spec that pinned service lifetimes based on domain requirements (repositories are scoped; loggers are singletons, etc.).

**Lesson:** Architectural constraints must be spec-level requirements, not implementation details.

### Case 3: Error Contract Divergence

Agent A implemented error handling as exceptions:
```csharp
public async Task<User> GetUserAsync(int id)
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
        throw new UserNotFoundException($"User {id} not found");
    return user;
}
```

Agent B implemented error handling as tuples:
```csharp
public async Task<(bool success, string message, User? user)> GetUserAsync(int id)
{
    var user = await db.Users.FindAsync(id);
    if (user == null)
        return (false, $"User {id} not found", null);
    return (true, "", user);
}
```

The integration layer expected one pattern; the service returned the other. Integration tests failed because try-catch logic did not match the tuple-based logic.

**Resolution:** The team specified in the Service contract that "all service methods return `Task<(bool success, string message, T? result)>`" and added schema validation to reject exceptions from the service layer.

**Lesson:** Error semantics are contracts, not implementation details. They must be specified before coding.

---

## Architectural Patterns That Reduce Conflict Risk

### Pattern 1: Boundary-First Spec Discipline

Define file structure and module boundaries in the spec before agents implement. Each agent owns exactly one boundary (one file, one directory, one service).

**From cc-sdd and cortex-ia documentation:**
- Specs include a "File Structure Plan" (YML format) that assigns every file to exactly one agent
- Specs include a "Contracts" section that defines all inter-module interfaces
- During implementation, agents cannot modify files outside their boundary, preventing silent overwrites

**Example:**
```yaml
boundaries:
  UserService:
    files: [src/Services/UserService.cs, tests/Services/UserServiceTests.cs]
    dependencies: [IUserRepository, ILoggerService]
    contracts:
      exports:
        - IUserService (interface definition)
        - UserProfileDto (DTO definition)
      imports:
        - IUserRepository.GetByIdAsync
        - ILoggerService.LogAsync

  UserRepository:
    files: [src/Infra/UserRepository.cs, tests/Integration/UserRepositoryTests.cs]
    dependencies: [AppDbContext]
    contracts:
      exports:
        - IUserRepository (interface definition)
      imports:
        - AppDbContext.Users
```

### Pattern 2: Contract-Validated Handoffs

After each wave, the Coordinator does not just collect outputs — it validates them against the shared contracts before passing them to the next wave.

**Implementation:**
- Coordinator invokes a Contract Validator tool that runs type checkers, schema validators, and interface audits on each agent's output
- Only outputs that pass contract validation are injected into the next wave's spawn prompts
- Failed outputs are returned to the agent for remediation

**Tools that implement this:**
- Spec Kit Agents (discovery and validation hooks at each phase)
- Intent's living specs with Verifier step
- cortex-ia's phase validation gates

### Pattern 3: Observation-Driven Coordination

Instead of agents communicating through explicit messages, they coordinate through a shared state substrate (CRDT, blackboard, or tuple space).

**From CodeCRDT and Wave Orchestrator:**
- Agents observe edits to shared files and skip completed work
- When Agent A writes a contract definition to `contracts.md`, Agent B's next read sees the updated definition
- Merging happens via CRDTs with deterministic convergence — zero character-level merge conflicts

**Example:**
```
Agent A updates src/Contracts/UserProfileDto.cs:
  Add field "status: enum Status { Active, Inactive }"

Agent B reads src/Contracts/UserProfileDto.cs (via file watch):
  Sees the new "status" field, generates code that uses it
  No guessing, no misalignment
```

---

## When Spec-Only Mitigation Is Insufficient

Even with complete specs, a 25–30 percentage-point gap persists between single-agent and multi-agent integration accuracy. Research decomposes this gap into two components:

- **Coordination cost:** +16pp — The genuine difficulty of producing compatible code without shared decisions
- **Information asymmetry:** +11pp — Hidden knowledge about how decisions should be made

This gap is approximately additive and cannot be fully eliminated through specifications alone. To close it further requires:

1. **Adversarial review passes:** A fresh-context reviewer (different model family) attacks the combined output of parallel agents and identifies semantic inconsistencies specs did not capture
2. **Formal verification:** Property-based tests that verify the entire system satisfies invariants (e.g., "all public APIs return DTO types that match the contract schema")
3. **Runtime contract enforcement:** Wrapper layers that validate at runtime that all serialized/deserialized data matches the schema

---

## Practical Guidance for SDD Teams

### For the Coordinator

Before launching parallel agents:

1. **Enumerate all shared artifacts:** Types, interfaces, API contracts, configuration keys, error codes, event schemas, DI scopes
2. **Write or update the contracts spec** with exact definitions for each artifact
3. **Trace dependencies:** Which artifacts does Agent A produce? Which does Agent B consume? Ensure B's spawn prompt includes A's actual definitions, not references
4. **Validate specs for precision:** For each type, can two independent LLMs generate the same code from the spec? If no, the spec is too vague

### For Implementor Agents

1. **Read the contracts spec first:** Before writing any code, read `contracts.md` or equivalent
2. **Assume the spec is incomplete:** If you must make a decision the spec does not cover, update the spec before completing. Do not add assumptions to the code.
3. **Validate local code against contracts:** Use type checkers and schema validators on your output before submitting
4. **Reject mismatches proactively:** If your code would violate a contract, stop and ask for clarification instead of guessing

### For Verifiers

1. **Run two passes:** Local (does this code meet its own spec?) and Integration (does this code match all shared contracts?)
2. **Use automation:** Type checkers, schema validators, interface auditors. Do not rely on manual inspection
3. **Reject on contract violation:** Even if the code is well-written, if it violates a shared contract, reject it
4. **Block downstream work:** Do not pass output to the next wave if contracts are violated

### When Repairing Conflicts

1. **Go back to the spec:** If Agent A and Agent B produced conflicting outputs, the root cause is spec ambiguity, not agent failure
2. **Update the spec:** Add the missing detail that would have prevented the conflict
3. **Ask agents to regenerate:** Do not manually merge conflicting outputs. Update the spec and re-run code generation
4. **Validate the merge:** Run contract validators on the merged output before proceeding

---

## Current Limitations

### Specification Authoring is Expensive

Writing specs with enough precision to eliminate conflicts requires upfront effort. For large systems with many shared contracts, this effort is substantial. Teams often resist this friction and try to move straight to code — which reintroduces the conflict risks.

**Mitigation:** Start with the highest-risk interfaces (API contracts, shared DTOs, DI scopes) and progressively spec others. Do not spec every detail; focus on shared boundaries.

### Tool Support Is Immature

As of mid-2026, no single IDE or tool suite fully automates contract violation detection. Teams cobble together type checkers, schema validators, and custom scripts. This fragmentation introduces gaps.

**Emerging tools:** Spec Kit Agents, cortex-ia, Intent, and ControlFlow all include validation hooks at phase boundaries, but none is dominant.

### Living Specs Create New Risks

If an agent implements something incorrectly and the spec auto-updates to reflect what was built, subsequent agents will generate against incorrect behavior described as correct intent. The Verifier catches many mismatches, but human review of spec changes (not just code changes) remains necessary.

**Mitigation:** Treat spec changes as risky. Require human approval for spec updates that cross domain boundaries.

### Coordination Cost Cannot Be Fully Eliminated

Even with perfect specs, agents working on interdependent code incur a genuine coordination penalty. The 16pp cost component reflects this fundamental cost. Parallelism saves time on independent work but compounds communication overhead on tightly coupled code.

**Mitigation:** Use parallelism only for tasks that are structurally independent. For tightly coupled work, sequential execution is cheaper.

---

## Summary: The Spec-First Recovery Model

Research and production experience converge on a single finding: **The specification is the sufficient and necessary coordination mechanism.** When agents working on interdependent tasks start with a shared, precise specification, integration success jumps from 25% (bare signatures) to 58% (full specification). Conflict detection tools diagnose problems but do not improve recovery. Post-hoc merge strategies do not fix fundamentally misaligned code.

The path forward is not better tooling for detecting conflicts after they occur. It is **better specifications written before implementation begins**:

1. **Enumerate shared contracts** (DTOs, API paths, error codes, DI scopes)
2. **Specify them precisely** (JSON Schema, OpenAPI, structured YAML, or equivalent)
3. **Inject them into agent spawn prompts** (paste the actual definitions, not references)
4. **Validate all outputs against contracts** (type checkers, schema validators, interface auditors)
5. **Update the spec as implementation discovers details** the spec did not capture (living spec discipline)

This is expensive upfront. It eliminates the vast majority of cross-agent conflicts. Teams that have adopted this model report lower integration failures and faster overall delivery, despite the spec authoring overhead.

---

## Sources

### Tier 1 — Primary Sources

- [The Specification Gap: Coordination Failure Under Partial Knowledge in Code Agents — arXiv:2603.24284](https://arxiv.org/abs/2603.24284) — 51-task study, specification completeness as coordination mechanism, AST-based conflict detection, recovery experiment showing spec-only sufficiency (88.9% vs. conflict reports 0pp benefit)
- [Why Do Multi-Agent LLM Systems Fail? — arXiv:2503.13657](https://arxiv.org/abs/2503.13657) — Comprehensive taxonomy of 14 unique failure modes across 150+ tasks, inter-agent misalignment category, failure categories analysis
- [Multi-Agent Workflows for Code Generation and Software Engineering — CodeCRDT paper, arXiv:2510.18893](https://arxiv.org/html/2510.18893v1) — Observation-driven coordination, CRDT-based state management, semantic conflict detection (5–10% semantic conflicts despite zero character-level conflicts)
- [Reaching Agreement Among Reasoning LLM Agents — arXiv:2512.20184](https://arxiv.org/html/2512.20184v1) — Formal model of agreement among stochastic agents, stability horizon thresholds, semantic equivalence criteria
- [On the Robustness and Generalizability of Large Language Model-based Collaboration Frameworks — arXiv:2509.04451](https://arxiv.org/abs/2509.04451) — Task-critical disagreements in single-path tasks, self-repair through path multiplicity, heterogeneous agent diversity
- [Spec Kit Agents: Grounded Multi-Agent Development with Phase-Scoped Context Validation — arXiv:2604.05278](https://arxiv.org/abs/2604.05278) — Context-grounding hooks, discovery and validation gates, phase-scoped artifact validation before code generation

### Tier 2 — Secondary Sources

- [Swarm vs. Supervisor: Multi-Agent Architecture Guide — Augment Code](https://www.augmentcode.com/guides/swarm-vs-supervisor) — Architecture selection, output validation and ordering, cascading failure analysis, 20–40% coordination overhead
- [Claude Code Agent Teams vs. Intent: Workspace or Terminal Multi-Agent? — Augment Code](https://www.augmentcode.com/tools/claude-code-agent-teams-vs-intent) — Living specs coordination, Intent's worktree isolation, shared directory overwrites vs. git merge conflicts, spec auto-update risks
- [Data Consistency Across Multiple Independent AI Agents — Fazm Blog](https://fazm.ai/blog/data-consistency-multiple-independent-ai-agents) — Silent file overwrites, shared configuration conflicts, git worktree isolation, coordination layer tracking
- [cc-sdd — GitHub repository](https://github.com/gotalab/cc-sdd) — Boundary-first spec discipline, File Structure Plan (YML), kiro-spec-batch for multi-spec consistency checking
- [From Chaos to Clarity: How SDD and Multi-Agent Workflows Transform AI Coding — Medium / specfact-cli case study](https://medium.com/@dominikus.nold/from-chaos-to-clarity-how-sdd-and-multi-agent-workflows-transformed-our-ai-assisted-development-4d16b5742031) — Field naming divergence example, living spec mechanism, contract-first gates with runtime validation
- [Spec Kit Agents: High-Quality Multi-Agent SDD through Phase-Scoped Grounding — GitHub / Spec Kit](https://github.com/github/spec-kit) — OpenAPI/AsyncAPI specifications, JSON Schema for data structures, discovery hooks, validation hooks
- [cc-sdd Spec-Driven Guide — Documentation](https://github.com/gotalab/cc-sdd/blob/main/docs/spec-driven-guide.md) — Specs as contracts, code as maintained artifact vs. generated output, agent coordination via shared boundaries
- [cortex-ia SDD Workflow Documentation — GitHub](https://github.com/lleontor705/cortex-ia/blob/main/docs/sdd-workflow.md) — Multi-agent phase coordination (Specify → Plan → Apply → Verify), file_reserve for conflict prevention, Constitutional Self-Critique before submitting

### Tier 3 — Tertiary Sources

- [Skill Contradiction Resolution — SpecWeave](https://spec-weave.com/docs/skills/skill-contradiction-resolution) — Skill-level contradiction detection, priority chain (Developer > Vendor > Community), contradiction classification (tool-coordination, API usage, numerical threshold, dynamic)
- [Adversarial Multi-Model Development Pipeline — AgentPatterns.ai](http://agentpatterns.ai/multi-agent/adversarial-multi-model-pipeline/) — Adversarial review with context reset, six-phase convergence, Purity Boundary Map, spec as living hypothesis
- [Spec-Driven Development — sudocode documentation](https://docs.sudocode.ai/examples/spec-driven-development) — Anchored feedback system, dependency graph as DAG, bidirectional feedback between agents and specs, issue blocking relationships
- [Scaling Multi-Agent Coordination — arXiv:2512.08296](https://arxiv.org/abs/2512.08296) — Tool-coordination trade-off (β=−0.096), architecture-dependent error amplification (17.2× independent vs. 4.4× centralized), token budget fragmentation
- [cc-sdd / pdoronila — GitHub](https://github.com/pdoronila/cc-sdd) — Specification-first workflow (EARS notation), phase-specific subagent spawn, dependency management as part of task planning
