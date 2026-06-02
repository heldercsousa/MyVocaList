# S6.2.1 — Enforcement Cost Overhead

**Status:** Researched  
**Predecessor(s) ID:** S6.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written |

---

## Overview

Runtime conformance checking—the enforcement mechanisms described in S6.2 (Automated Hooks)—adds measurable latency and resource overhead when applied at scale. This section documents the empirical cost of hook enforcement on agent loop latency, concurrent throughput, memory overhead, and operational resource consumption. Understanding this cost is essential for operators deciding whether to enforce a rule deterministically (via hooks) or accept the compliance risk of advisory guidance.

The critical insight: **enforcement overhead is negligible compared to LLM API latency**, but it compounds rapidly across hundreds of parallel agents and thousands of tool calls per session. Where enforcement is necessary (security-critical, non-negotiable constraints), the cost is justified. Where it is advisory (style preferences, minor conventions), the cost may exceed the benefit.

---

## Hook Execution Latency

### Command Hooks (Shell Scripts)

Shell-script hooks executing at `PreToolUse` or `PostToolUse` add synchronous latency because they block the agent loop until completion.

**Benchmark: Single hook execution (baseline)**

| Metric | Value | Context |
|--------|-------|---------|
| **Policy evaluation (single rule)** | 0.011 ms (p50) | ~84K ops/sec |
| **Policy evaluation (100 rules)** | 0.030 ms (p50) | ~32K ops/sec |
| **Full kernel enforcement** | 0.103 ms (p50) | ~9.7K ops/sec |
| **Adapter governance overhead** | 0.005–0.007 ms (p50) | Framework adapters (OpenAI, Anthropic, LangChain) |
| **Pattern matching (per call)** | 0.007 ms (p50) | Regex/glob matchers for tool filtering |

**Key finding:** A single policy check adds **< 0.1 milliseconds** of latency. Even 100 rules evaluated in sequence produce p50 latency under 0.03 ms and p99 under 0.11 ms.

### PreToolUse vs PostToolUse Trade-off

- **PreToolUse hooks:** Synchronous and blocking. The agent cannot proceed to tool execution until the hook completes. For security-critical checks (e.g., blocking destructive operations), this is a necessary cost. Even at 100 ms per check, this is negligible relative to LLM round-trip latency (200–3000 ms).
- **PostToolUse hooks:** Asynchronous and non-blocking (in most agent frameworks). The tool executes while the hook runs in parallel. Cost is amortized across multiple hook executions. Useful for logging, formatting, and non-blocking observability.

### Stop Hooks

Stop hooks run once per agent turn (not per tool call) but may trigger heavyweight validation (full test suite, linter on all changed files). Benchmarks from MyVocaList's own practices show:
- Simple Stop hook (git status check, task validation): **< 100 ms**
- Full test suite (dotnet test on moderate codebase): **30–120 seconds** (blocking)

**Implication:** Stop hooks gate agent completion and must complete within acceptable timeframes. Slow Stop hooks (full test suites) should run asynchronously in CI/CD rather than blocking the agent interactively.

---

## Concurrency and Throughput at Scale

When dozens or hundreds of agents run in parallel—each triggering hooks on every tool call—throughput degradation becomes material.

**Benchmark: Microsoft Agent Governance Toolkit (100,000 concurrent operations)**

| Concurrency | Total ops | Wall time (s) | ops/sec | Degradation vs single-threaded |
|-------------|-----------|---------------|---------|-------------------------------|
| **50 agents × 200 ops** | 10,000 | 0.216 | 46,329 | 4.8× speedup |
| **100 agents × 100 ops** | 10,000 | 0.209 | 47,920 | 5.0× speedup |
| **500 agents × 100 ops** | 50,000 | 1.085 | 46,089 | 4.8× speedup |
| **1,000 agents × 100 ops** | 100,000 | 2.124 | 47,085 | 4.9× speedup |

**Key finding:** Throughput remains stable at **~47K ops/sec** from 50 to 1,000 concurrent agents. No degradation at scale. The enforcement layer is thread-safe and scales linearly.

**Memory overhead per enforcement instance:** ~2 KB (policy state) + 0.5 KB per evaluation context. A 1,000-agent swarm with 100 rules per agent consumes **~2.1 MB** in enforcement overhead — negligible compared to agent process memory (100s of MB per agent).

---

## Latency Relative to Agent Loop Operations

The critical comparison: enforcement overhead in context of actual agent operations.

| Operation | Typical latency | Multiple of hook overhead |
|-----------|-----------------|---------------------------|
| **Policy evaluation (this layer)** | **0.01–0.03 ms** | **1×** |
| **Full kernel enforcement** | **0.10 ms** | **10×** |
| **Adapter overhead** | **0.005–0.007 ms** | **<1×** |
| Python function call | 0.001 ms | 0.1× |
| **Redis read (local)** | **0.1–0.5 ms** | **10–50×** |
| Database query (simple) | 1–10 ms | 100–1,000× |
| **LLM API call (GPT-4)** | **200–2,000 ms** | **20,000–200,000×** |
| **LLM API call (Claude)** | **300–3,000 ms** | **30,000–300,000×** |

**Critical insight:** Enforcement overhead is **10,000× faster than an LLM API call**. In practical terms: if a PreToolUse hook adds 100 ms, it adds only 3–10% latency to a typical tool execution (tool I/O ranges 500 ms–5 sec), and is invisible compared to the preceding LLM reasoning (200–3,000 ms).

The agent's latency budget is dominated by:
1. **LLM reasoning** (26–44% of end-to-end task time)
2. **Tool execution** (~40% of end-to-end time, mostly I/O)
3. **OS overhead** (initialization, context switching: 10–20%)

Enforcement is a sub-percent contribution to total latency in realistic scenarios.

---

## Cascading Cost of Multiple Hooks

A single agent session triggers dozens to hundreds of tool calls. Each call fires hooks. Costs compound.

**Example: 100 tool calls with 3 hooks each**

| Scenario | Hook latency per call | Total hook time | Agent downtime | Verdict |
|----------|----------------------|-----------------|----------------|---------|
| **1 rule, fast script** | 5 ms | 1.5 sec | Negligible | Accept |
| **10 rules, moderate script** | 20 ms | 6 sec | Minor | Accept |
| **50 rules, complex policy** | 100 ms | 30 sec | Noticeable | Reconsider scope |
| **Full test suite (Stop only)** | 60 sec | 60 sec (once per turn) | Significant | Move to async CI/CD |

**Implication:** Broad, complex hooks on every tool call become a visible cost. Strategies to mitigate:

1. **Matcher filtering:** Fire hooks only on relevant tool names (e.g., `Edit|Write` only, not all tools)
2. **Caching:** Cache hook decisions for repeated identical tool calls
3. **Async for non-blocking:** Use async hooks for logging, formatting, telemetry
4. **Batching:** Aggregate multiple evaluations into a single hook invocation (PostToolBatch)

---

## Framework-Specific Overhead

Different agent frameworks implement hooks differently. Overhead varies by framework architecture and hook handler type.

**Latency by framework adapter (p50, milliseconds)**

| Framework | Command hook | HTTP hook | Prompt hook | Agent hook |
|-----------|--------------|-----------|------------|-----------|
| OpenAI | 0.005–0.007 | 10–50 | 150–300 | 500–2000 |
| **Anthropic** | **0.006–0.008** | **10–50** | **200–400** | **600–2500** |
| LangChain | 0.006–0.007 | 20–60 | 200–500 | 800–3000 |
| CrewAI | 0.005–0.006 | 15–40 | 180–350 | 700–2500 |
| LlamaIndex | 0.006–0.007 | 20–50 | 220–480 | 1000–3500 |
| Semantic Kernel | 0.005–0.007 | 15–45 | 190–420 | 750–2800 |

**Key findings:**
- **Command hooks:** Overhead is frame-invariant, ~0.005–0.008 ms across frameworks
- **HTTP hooks:** Network latency dominates (10–50 ms typical for co-located services; 100+ ms for regional), not framework overhead
- **Prompt hooks:** ~1 LLM token per hook ≈ 150–400 ms depending on model and routing
- **Agent hooks:** Spawn a full subagent, typically 500 ms–5 sec depending on scope

**Implication:** Choose hook handler types based on enforcement need, not framework preference. Command hooks are always the fastest. Agent hooks for complex policy are acceptable in Stop events (run once) but prohibitive in PreToolUse (run per tool call).

---

## Enforcement Cost Dimensions

Hook overhead manifests across multiple dimensions. Single-metric views (latency alone) miss the full picture.

### 1. Wall-Clock Latency (per hook execution)

Already covered above. Range: 0.01 ms (command) to 5 sec (agent).

### 2. Concurrency Impact (per session, multiple hooks)

With N tool calls and M hooks firing per call:
- Total hook time = N × M × avg_hook_latency
- At N=100, M=3, avg=20ms → 6 seconds total hook time per session
- For interactive development, this is the "feel" users experience

**Mitigation:**
- Use matchers to reduce M (only fire hook on relevant tool calls)
- Use async for non-blocking hooks
- Move heavyweight checks to CI/CD gates (Stop hook async)

### 3. Memory Footprint (per concurrent agent)

Enforcement requires in-memory state:
- Policy engine: ~2 KB per 100 rules
- Hook registry: ~0.5 KB per hook handler
- Evaluation context stack: ~1 KB per concurrent evaluation
- Audit/logging buffers: 10–100 KB (depending on verbosity)

**At scale (1,000 concurrent agents):**
- Total enforcement memory: ~120 MB (reasonable)
- Per-agent marginal cost: ~120 KB (negligible vs agent process memory of 100s MB)

### 4. CPU Utilization (during heavy enforcement)

Policy evaluation is CPU-light: pattern matching, rule traversal, decision trees. CPU overhead < 1% except when:
- Heavyweight RegEx matchers on large strings
- Cryptographic signing/verification (milliseconds)
- Full AST parsing for code hooks (seconds, rare)

**Practical concern:** CPU costs only spike if hooks spawn subprocesses. Avoid spawning processes in PreToolUse hooks (high frequency). Reserve subprocess hooks for PostToolUse or Stop events.

### 5. I/O Cost (hook-induced filesystem/network traffic)

If hooks execute shell commands that read/write files or make API calls:
- File read: 1–10 ms
- File write: 10–50 ms
- HTTP request to local service: 10–50 ms
- HTTP request to remote service: 100+ ms

**Example: Hook that calls a policy service over the network**
- Hook latency: 100–200 ms (network round-trip)
- Per tool call: negligible
- At 100 tool calls: 10–20 sec total overhead
- Perception: "Agent feels slow"

**Mitigation:** Co-locate policy services. Use local caching. Prefer command hooks over HTTP hooks.

---

## Cost at Different Scales

Enforcement overhead manifests differently depending on the scope of deployment.

### Single Developer, Single Session

- Tool calls per session: 50–200
- Concurrent hooks: 2–5
- Total hook time per session: 100 ms–5 sec
- User perception: Imperceptible (< 5 sec) or minor delay (5–30 sec)
- Verdict: Cost is acceptable for security-critical rules

### Team, Multiple Sessions Per Day

- Sessions per developer per day: 5–10
- Concurrent agents per session: 1–3
- Total hook overhead per developer per day: 1–50 sec
- Accumulated across 10 developers: 10–500 sec/day → 2–100 min/day
- Verdict: Cost is acceptable; may justify caching and optimization

### Enterprise, Swarm of 100s of Agents

- Concurrent agents: 100–1,000
- Tool calls per agent per session: 100–1,000
- Total concurrency: 100K–1M tool calls per day
- Hook throughput: 47K ops/sec (from benchmarks) → accommodates load
- Memory cost: ~100 MB (for 1,000 agents)
- Verdict: Cost is acceptable with proper architecture (async hooks, matchers, caching)

---

## The SDD-Specific Cost Profile

In SDD workflows (S6.4.2 — Continuous Conformance Requirement), enforcement is mandatory at every stage:

1. **Spec phase:** Linting and schema validation on every spec update
2. **Implementation phase:** Hook enforcement on every tool call and phase transition
3. **CI/CD phase:** Contract testing, property tests, spec-code alignment checks
4. **Continuous phase:** Drift detection on every commit

This creates a multi-layer enforcement stack. Cumulative latency is the concern, not single-layer overhead.

**SDD enforcement cost profile:**

| Layer | Frequency | Latency per invocation | Total per session |
|-------|-----------|----------------------|-------------------|
| **PreToolUse hook** | Per tool call (50–200/session) | 20–100 ms | 1–20 sec |
| **PostToolUse hook** | Per tool call (50–200/session) | 10–50 ms (async) | <1 sec (amortized) |
| **Stop hook** | Once per turn | 500 ms–60 sec | 0.5–60 sec |
| **CI/CD contract testing** | Per commit | 5–120 sec | Amortized across developers |

**Implication:** A single SDD session with multi-layer enforcement adds 2–100 seconds of observable latency. Acceptable for phase gates (Stop hooks, CI/CD) but not for every tool call (PreToolUse should stay < 50 ms).

---

## Cost-Benefit Analysis: When to Enforce vs. Advise

Enforcement costs should be justified by the cost of non-compliance.

| Rule type | Advisory cost | Enforcement cost | Threshold decision |
|-----------|---------------|------------------|-------------------|
| **Security (secrets, destructive ops)** | Critical (breach, data loss) | Acceptable (20–100 ms per call) | **Always enforce** |
| **Compliance (audit, logging)** | High (audit failure) | Acceptable (async, <50 ms) | **Enforce asynchronously** |
| **Phase gates (test passing, tasks checked)** | Medium (rework, unfinished work) | Acceptable (50–60 sec, once per turn) | **Enforce at Stop hook** |
| **Architectural boundaries (layer imports)** | Medium (tech debt accumulation) | Acceptable (10–50 ms, narrow matchers) | **Enforce with matchers** |
| **Style (naming, formatting)** | Low (minor rework) | Expensive (5–20 ms per call × 100+ calls) | **Advise + auto-format post-hoc** |
| **Performance hints (token usage)** | Low (later optimization) | Expensive (10+ ms per call, noisy) | **Advise, don't enforce** |

**Key decision rule:** If the cost of non-compliance exceeds 10 minutes of developer time (rework, debugging, audit), enforcement is justified even if it adds 30–60 seconds per session. If non-compliance is purely a style preference, advisory guidance is preferable.

---

## Optimization Strategies

### 1. Matcher-Based Filtering

Fire hooks only on relevant tool calls. Example:
```json
{
  "PreToolUse": [{
    "matcher": "Edit|Write",
    "hooks": [{ "type": "command", "command": "lint.sh" }]
  }]
}
```

**Effect:** Reduces hook frequency by 70–90% (only fires on Edit/Write, not Bash/Read/Grep). Latency drops correspondingly.

### 2. Caching Hook Decisions

Cache the result of expensive hooks for repeated identical operations.

Example: PreToolUse hook evaluating the same file path multiple times in one session.

```bash
CACHE_DIR="/tmp/hook-cache-$$"
CACHE_FILE="$CACHE_DIR/$(echo "$FILE_PATH" | md5sum | cut -d' ' -f1)"

if [ -f "$CACHE_FILE" ]; then
    cat "$CACHE_FILE"
    exit 0
fi

# Expensive evaluation
DECISION=$(evaluate_policy "$FILE_PATH")
echo "$DECISION" > "$CACHE_FILE"
echo "$DECISION"
```

**Effect:** 2nd+ evaluations of same path take < 1 ms (cache hit). Effective when agents re-edit the same files.

### 3. Async Hooks for Non-Blocking Work

Use async hooks for logging, telemetry, formatting, and other operations that don't require the agent to wait.

```json
{
  "PostToolUse": [{
    "type": "command",
    "command": "log-and-format.sh",
    "async": true
  }]
}
```

**Effect:** Hook runs in background while agent continues. Latency savings: 10–50 ms per tool call.

### 4. Batching (PostToolBatch)

Aggregate multiple tool calls into a single hook invocation.

Example: Instead of a PreToolUse hook on every Bash call, a PostToolBatch hook runs once per batch, checking all shell commands together.

**Effect:** Reduces hook invocations by 3–10×. Useful for aggregate checks (total cost per batch, rate limits).

### 5. Colocating Policy Services

If using HTTP-based policy hooks, run the policy service on the same machine or container.

- Local HTTP round-trip: 10–50 ms
- Regional network round-trip: 50–200 ms
- Cross-region: 100–500 ms

**Effect:** Locality reduces latency by 5–50×.

### 6. Hook Dependency Optimization

Dependencies between hooks create head-of-line blocking. Order hooks by execution cost.

```json
{
  "PreToolUse": [
    { "type": "command", "command": "fast-check.sh", "timeout": 5 },
    { "type": "command", "command": "slow-check.sh", "timeout": 30 }
  ]
}
```

**Effect:** Fast checks run first; if they block, slow checks never run. Latency reduced by 30–50%.

---

## Operational Risks from Enforcement Overhead

### Risk 1: Timeout-Induced Agent Halting

A PreToolUse hook that times out may cause the agent to halt indefinitely instead of receiving feedback. Versions of Claude Code (as documented in S6.2) have exhibited this behavior:

```
Agent calls Edit
PreToolUse hook fires
Hook times out after 30 seconds
Expected: Agent receives "timeout" message and continues
Actual: Agent halts idle, no feedback
```

**Mitigation:** Set explicit timeouts on all hooks. Use JSON `decision: "block"` field instead of relying on exit code 2. Test hook failure modes in staging.

### Risk 2: Silent Hook Failures

Hook failures (missing dependencies, syntax errors in output) are treated as "hook error" and do not block. The tool call proceeds unchecked.

```
Agent calls Edit
PreToolUse hook fires
jq not installed → hook script fails
Exit code: non-zero (not 0, not 2)
Result: Hook error treated as non-blocking, Edit proceeds without validation
```

**Mitigation:** Health-check hooks at SessionStart. Include cleanup in hook scripts (ensure dependencies exist). Log hook failures to stderr.

### Risk 3: Subagent Hook Inheritance Gaps

Subagents don't automatically inherit parent agent hooks. If hooks are in user-scope `~/.claude/settings.json`, subagents won't see them.

**Mitigation:** Always use project-scope `.claude/settings.json` for shared hooks. Never rely on user-scope hooks for SDD enforcement.

---

## Empirical Guidance: Hook Performance Budgets

Based on research and production data from 2025–2026:

| Hook event | Max recommended latency | Rationale |
|-----------|----------------------|-----------|
| **PreToolUse** | < 50 ms | Fires per tool call; latency multiplies |
| **PostToolUse** | < 100 ms | Runs after tool completes; less critical |
| **Stop** | < 120 sec | Runs once per turn; acceptable for heavyweight validation |
| **TaskCompleted** | < 20 sec | Runs when task marked done; should not block completion |
| **SessionStart** | < 5 sec | Runs once; minimal impact |

These budgets reflect the SDD context where hooks are part of a continuous conformance pipeline. Adjust based on your tolerance for agent latency.

---

## Research Summary

Enforcement cost overhead is empirically measurable and acceptable for security-critical and compliance-critical rules. However, applying enforcement uniformly to all constraints (style, preferences, minor conventions) produces visible latency degradation without corresponding benefit.

The SDD practice should:
1. **Enforce deterministically** any rule where non-compliance is unacceptable (security, approval gates, architectural layers)
2. **Advise in prompts** any rule where non-compliance is recoverable (style, performance hints, documentation)
3. **Optimize enforcement** through matchers, caching, and async handling to keep PreToolUse overhead < 50 ms
4. **Test hook failure modes** before deploying to production (timeout behavior, inheritance, silent failures)

---

## Sources

- [BENCHMARKS.md — Microsoft Agent Governance Toolkit](https://github.com/microsoft/agent-governance-toolkit/blob/636aecfab73c0904fe38f7eb6c27936b26686ac0/BENCHMARKS.md)
- [Policy Enforcement Latency: Real-World Benchmarks — PolicyLayer](https://policylayer.com/blog/policy-enforcement-latency-benchmarks)
- [AWS CloudFormation Lambda Hooks](https://aws.amazon.com/blogs/devops/validate-your-lambda-runtime-with-cloudformation-lambda-hooks/)
- [EnforceCore — Runtime Enforcement for AI Agents](https://akios.ai/enforcecore/architecture.html)
- [Policy Enforcement Runtime Guardrails — hoop.dev](https://hoop.dev/blog/policy-enforcement-runtime-guardrails-real-time-compliance-for-safe-fast-delivery)
- [AI Agent Hooks and Middleware: Runtime Behavior Interception and Control Patterns — Zylos Research](https://zylos.ai/research/2026-03-27-ai-agent-hooks-middleware-runtime-behavior-control)
- [Agent Hooks in Azure SRE Agent — Microsoft Learn](https://learn.microsoft.com/en-us/azure/sre-agent/agent-hooks)
- [Hooking Coding Agents with the Cedar Policy Language — Sondera](https://blog.sondera.ai/p/hooking-coding-agents-with-the-cedar?hide_intro_popup=true)
- [Enforcing Agent Behavior with Hooks — AgentPatterns.ai](https://agentpatterns.ai/instructions/enforcing-agent-behavior-with-hooks/)
- [Claude Code Hooks: The Deterministic Control Layer for AI Agents — Dotzlaw Consulting](https://dotzlaw.com/insights/claude-hooks/)
- [A Look At An Emerging Runtime Enforcement Layer For Agents - Hooks — ResilientCyber](https://www.resilientcyber.io/p/a-look-at-an-emerging-runtime-enforcement)
- [AI Agent Policy Enforcement for Claude: Introducing Captain Hook — SecurityReview.ai](https://www.securityreview.ai/blog/captain-hook-ai-agent-policy-enforcement-for-claude)
- [arXiv:2602.09345 — Agent Resource Management and Performance Characterization](https://www.arxiv.org/pdf/2602.09345)
- [How to Add Runtime Enforcement Without Breaking Your Agents — Cycles](https://runcycles.io/blog/how-to-add-runtime-enforcement-without-breaking-your-agents)
- [Contract Testing Plan: From OpenAPI to CI — Spec Coding](https://spec-coding.dev/blog/contract-testing-plan-from-openapi-to-ci)
- [Spec-Driven LLM Development (SDLD): Precise Engineering Through Specifications — David Lapsley](https://blog.davidlapsley.io/engineering/process/best%20practices/ai-assisted%20development/2026/01/11/spec-driven-development-with-llms.html)
- [Spec-Driven Development in 2025: Industrial Tools, Frameworks, and Best Practices — Marvin Zhang](https://marvinzhang.dev/blog/sdd-tools-practices)
- [AgentContract — Specification for AI Agent Behavior Enforcement](https://github.com/agentcontract/spec)
- [Specification-Driven Development: The Four Pillars — Alex Rezvov](https://blog.rezvov.com/specification-driven-development-four-pillars)
