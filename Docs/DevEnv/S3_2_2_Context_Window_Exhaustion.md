# S3.2.2 — Context Window Exhaustion

**Status:** Researched
**Predecessor(s) ID:** S3.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; context rot mechanisms, threshold analysis, and practical bounds established |

---

## Overview

Context window exhaustion is the silent performance killer in long-running AI coding sessions. Large tasks that accumulate tool invocations, file reads, debugging traces, and reasoning steps will eventually degrade LLM behavior — not through a hard crash, but through progressive quality loss: hallucination, inconsistent behavior, repeated logic, and dropped constraints.

The problem is not that context windows are too small. Modern frontiers like Claude (200K tokens) and GPT-5 (1M tokens) advertise capacities that seem unlimited. The problem is that **effective working memory degrades long before the advertised limit** due to three mechanisms: attention dilution, positional bias, and information retrieval failure. Research in 2025–2026 has quantified this degradation and established practical bounds that directly inform task granularity in the Implementation Phase.

---

## Core Findings from 2025–2026 Research

### Maximum Effective Context Window (MECW) ≠ Maximum Context Window (MCW)

Paulsen et al. (2025) systematized the distinction between advertised context window and actual usable capacity:

| Metric | Finding |
|--------|---------|
| **Advertised limits** | Claude 200K, GPT-5 1M, Gemini 1.5 Pro 2M tokens |
| **Actual effective limit** | Models show significant degradation at 50–100K tokens, well before the advertised maximum |
| **Degradation point** | All 18 frontier models tested (Anthropic, OpenAI, Google, Alibaba families) showed measurable accuracy drops starting at 2,500–5,000 tokens for complex reasoning tasks |
| **Failure threshold** | Some models reached near-zero accuracy (99%+ hallucination) by 2,000–10,000 tokens depending on task complexity |

**Key insight:** The gap between MCW and MECW is not 10–20%. It is 50–99%. A model with a 200K-token context window can be effectively exhausted at 50K tokens.

### Three Mechanisms Drive Context Rot

Schnabel et al. (2025) identified why context fills faster than raw token accounting suggests:

1. **Attention Dilution:** Transformer attention spreads across all tokens. More tokens = less attention per token. Critical information competes with noise and repetitive tool outputs.

2. **Positional Bias (Lost-in-the-Middle):** Models consistently perform better when relevant information appears near the beginning or end of the context, and worse when buried in the middle. Stanford researchers found cases where GPT-3.5 with relevant material in the middle performed *worse than without the retrieval at all*.

3. **Retrieval Failure:** As context grows, the model struggles to locate and prioritize specific facts. EMNLP 2025 research showed accuracy drops of 13.9–85% depending on task type when information is dispersed across the corpus.

**Practical implication for task design:** Placing task-critical files (spec files, design.md, CLAUDE.md) at the beginning of an agent session is not just helpful—it is necessary.

### Working Memory vs. Context Window Capacity

Recent work (Schnabel et al., 2025) introduces the concept of **BAPO-hardness**: a theoretical measure of the amount of information an LLM must actively track to solve a task.

- **BAPO-easy tasks** (needle-in-haystack, simple lookup): Constant working memory; easily scale to long contexts
- **BAPO-hard tasks** (code tracing, complex summarization, inconsistency detection, architectural decision tracking): Working memory grows with the problem size (variables, dependencies, constraints)

BAPO-hard tasks are precisely the ones agents perform in Implementation: understanding layered architecture, tracking cross-file invariants, detecting inconsistencies between spec and code, managing state across a multi-file refactor.

**Implication:** As task size grows, the working memory burden grows faster than token count grows. A task that requires understanding 5 files and 3 interdependencies may be manageable; one requiring 20 files and 50 interdependencies quickly exceeds effective capacity, even if raw token count fits within the window.

---

## Context Rot in Practice (Claude Code, 200K Token Window)

The Claude Code environment is the most directly relevant case study for SDD workflows.

### Token Budget Breakdown

Morph (2026) mapped the token allocation in a Claude Code session:

| Component | Tokens | % of Window | Notes |
|-----------|--------|------------|-------|
| System prompt | ~2,600 | 1.3% | Base instructions |
| System tools | ~17,600 | 8.8% | Read, Write, Bash, Grep, Edit, etc. |
| MCP servers | 900–51,000 | 0.5–25% | Varies wildly by count and type |
| CLAUDE.md + rules | ~2,000–5,000 | 1–2.5% | Project instructions |
| **Auto-compact buffer** | **~33,000** | **~16.5%** | **Reserved before any work begins** |
| **Available for work** | **~100,000–114,000** | **~50–57%** | **Actual practical capacity** |

**Critical insight:** Before an agent types a single command, it has already consumed 40–45% of its 200K-token window. The practical ceiling is not 200K—it is ~120K tokens before performance starts degrading meaningfully.

### When Does Degradation Begin?

Morph and Chroma (2025) converge on thresholds:

- **50–60% capacity:** Early degradation begins. Instructions from the session start may be ignored. Previously-loaded file context is poorly weighted.
- **64–75% capacity:** Claude Code's auto-compaction triggers automatically.
- **80%+ capacity:** Severe degradation. Hallucination rates spike. Repeated logic. Inconsistent decision-making.

**Rule of thumb:** Treat 50% of the practical capacity (i.e., 50–60K tokens into the session) as a soft trigger to pause and assess. By 75K tokens, manual compaction or session restart is nearly mandatory for quality.

### What Gets Lost in Compaction

When Claude Code auto-compacts (or when compaction is manually invoked), the process:
1. Generates a summary of conversation history
2. Clears old tool outputs (file reads, grep results, bash outputs)
3. Restarts with the summary + recent turns
4. Loses fine-grained details: exact error messages, specific code patterns, low-level design rationale

Result: After one compaction, responses become vaguer. After two compactions, the agent may forget what it decided three hours ago.

---

## Large Tasks and Hallucination Compounding

The Implementation Phase Specification (S3.2) relies on large tasks being decomposed into smaller subagents. This design addresses context exhaustion directly, but misses a subtler risk: **hallucination compounding across sequential tasks within the same agent session**.

### Accumulated Debugging Traces

A single agent session executing tasks T1, T2, T3 sequentially will accumulate:
- Failed approaches and dead ends from T1 (discarded but still in history)
- Exploration traces from T2 (code reads, search results, test failures)
- State mutations from T3 (file edits, refactors)

By task T3, the agent is reasoning about the spec and design through a fog of two prior tasks' debugging noise.

**Research finding (arXiv:2601.14914, CODEDELEGATOR):** Ephemeral agents (fresh context per task) outperform persistent agents (same agent across multiple tasks) by 15–30% on task accuracy and code quality metrics. The improvement comes from eliminating accumulated debugging noise.

### Spec Drift Risk

As context fills and compaction occurs, the agent's access to the spec becomes indirect:
1. First task: Agent reads requirements.md, design.md directly
2. Compaction: Spec details are summarized and may be pruned
3. Later tasks: Agent must rely on summaries or re-reading—but re-reading consumes context when it is already full

Risk: The spec becomes stale in the agent's working memory. The agent drifts from intent and produces code that technically compiles but diverges from the spec's behavioral intent.

---

## Task Granularity Calibration

The primary defense against context window exhaustion is **task sizing**. Tasks must be small enough to complete before context degradation meaningfully affects quality.

### Recommended Bounds

Based on research and SDD practice patterns:

| Metric | Recommended Bound | Rationale |
|--------|------------------|-----------|
| **Tool invocations per task** | 50–150 | Each Read, Grep, Bash, Edit call leaves artifact in context. 150+ calls creates significant accumulation. |
| **Unique files touched** | 1–5 | Touching more than 5 files suggests the task cuts across layers or is under-decomposed. |
| **Lines of code produced** | 100–500 | Larger changes require more context to reason about invariants and avoid regressions. |
| **Test cases written** | 1–3 | Each test is a separate specification the agent must hold in working memory. |
| **Acceptance criteria** | 1 | A task should map to a single, focused user story or architectural component. |

Tasks violating these bounds should be split during Planning (S3.1). A task that requires "implement user authentication, add profile page, and send welcome email" is three tasks, not one.

### Task Completion Before Compaction

An agent should complete a task and commit before context approaches 75% capacity. If a task is not done by that point, it should be split in planning, or the agent should:
1. Commit the incomplete work
2. Document the blocking point in the task-log
3. Signal for re-planning or re-tasking
4. Exit the session

Continuing past 75% capacity risks hallucination-induced bugs that are harder to debug because the context fog obscures the root cause.

---

## Architectural Pattern: Ephemeral-Persistent Separation

The CODEDELEGATOR pattern (arXiv:2601.14914) formalizes the solution:

- **Persistent layer (Orchestrator):** Maintains task list, commits, architectural decisions, global state. Lightweight context.
- **Ephemeral layer (Coder agents):** Execute individual tasks in isolated, fresh contexts. Discard state after task completion.

This is precisely the Orchestrator-Worker pattern described in S3.2 (Implementation Phase).

**Why it works:**
- Each subagent starts with 100% effective context capacity (or 50K+ usable tokens out of 200K)
- The agent focuses on one task in isolation, avoiding accumulated noise
- When the task completes and is committed, all debugging traces are discarded
- The next subagent starts fresh, never inheriting the prior agent's dead ends

**Cost:** Task boundaries must be clean. If tasks have complex inter-dependencies, context becomes cluttered transferring state between subagents. Design the task graph first, then assign subagents.

---

## Compaction Strategy for Long Sessions

When a single agent must complete multiple tasks or handle extended implementation:

### Manual Compaction at Logical Breakpoints

- After completing a major subtask or feature, invoke `/compact` with explicit guidance:
  ```
  /compact Preserve file paths, API signatures, and acceptance criteria. 
  Discard debugging traces and failed approaches.
  ```
- This produces a higher-quality summary than automatic compaction because the context is clean at that moment

### Avoid Reusing the Same Agent Across Waves

If parallelism requires sequential waves of agents (Wave 1 → review → Wave 2 → review → Wave 3), do **not** reuse the same agent instance for multiple waves. Each wave should spawn fresh subagents. The orchestrator aggregates results between waves.

### Monitor Token Usage

Use the `/context` command (or equivalent) in Claude Code to monitor context percentage. When approaching 60% capacity:
- Summarize progress in the task-log
- Prepare to compact or reset
- Adjust remaining task scope if necessary

---

## Interaction with Task Dependencies

Context exhaustion becomes more problematic when task dependencies are tight.

### The Tight-Dependency Problem

If Task 3 depends heavily on understanding the output of Task 2, and Task 2 is executed by a different agent:
- Task 2's agent loads files, runs tests, makes decisions → commits
- Task 3's agent reads Task 2's commit and design documentation, but loses access to Task 2's reasoning process, file context, and debugging knowledge

This can be mitigated by:
1. Writing detailed task-completion summaries in task-log (not relying on implicit transfer)
2. Updating design.md with Task 2's outputs **before** Task 3 starts
3. Having Task 3 agent explicitly re-read design.md and task-log at the start

**Best practice:** Minimize tight dependencies during planning. Design tasks so Task 3 depends on Task 2's *outputs* (committed code), not on Task 2's *reasoning*.

---

## Verification and Detection

### Signs of Context Exhaustion During a Session

- Agent forgets earlier constraints or instructions from CLAUDE.md
- Agent produces code that contradicts decisions made earlier in the session
- Agent suggests the same exploration twice (loses memory it already tried it)
- Agent's responses become generic or vague ("should be straightforward to implement")
- Hallucinated code that is subtly wrong (compiles, but violates spec intent)

### Detection in Code Review

- Diff quality degrades in later commits within the same agent session (compare commit 1 vs. commit 10 within the same agent session)
- Test coverage in later tasks is weaker than earlier tasks
- Code style drift increases (naming conventions, formatting inconsistency)

### Mitigation

If review detects context exhaustion:
1. Do not request the agent to revise — the context is already too polluted
2. Instead: reset the session, re-read the task, and ask a fresh agent to re-implement
3. Use the fresh agent's output, discarding the exhausted agent's work

---

## Interaction with SDD Principles

Context window exhaustion is a direct threat to the **Spec-as-Primary-Artifact** principle:

- The spec is only useful if the agent can hold it in working memory
- Long tasks that fill the context window force the agent to reason from summaries and implicit knowledge
- Implicit knowledge == hallucination risk

**Implication:** SDD is maximally effective when task sizes are small enough that the full spec remains salient throughout the agent's reasoning. Large tasks, even if technically "doable" in a single session, violate the spirit of SDD because they force spec details into the background.

---

## Recommendations for the MyVocaList Implementation

1. **Keep tasks strictly under 200 lines of code.** Target 50–150 lines per task. This naturally fits tasks into the sub-60% context threshold.

2. **Each task should produce one focused artifact.** One service method, one repository query, one page, one test suite. Not "add service + repository + UI" in one task.

3. **Require re-reading the spec on each task.** Each subagent should start by reading requirements.md and the relevant section of design.md. Do not assume implicit transfer of context.

4. **Commit frequently.** After each task completes, commit immediately. Do not allow agents to accumulate 3–4 uncommitted changes before committing.

5. **Use wave-based parallelism.** Dispatch 4 subagents per wave. Wait for all to complete, review, commit, then start the next wave. Do not try to parallelize beyond 4 agents; human review capacity becomes a bottleneck before context exhaustion does.

6. **For long features (20+ tasks), use checkpoints.** After every 3–5 tasks, pause and write a checkpoint: a summary of completed work, outstanding dependencies, and next steps in a file (e.g., `Docs/checkpoints/feature-name-wave-2.md`). The next wave reads this checkpoint to understand what was done without having to reconstruct it from scattered commits.

---

## Sources

- [Context Is What You Need: The Maximum Effective Context Window for Real World Limits of LLMs — Paulsen et al. (2025, arXiv:2509.21361)](https://arxiv.org/abs/2509.21361)
- [Your 1M+ Context Window LLM Is Less Powerful Than You Think — Schnabel et al., Towards Data Science (2025-07-17)](https://towardsdatascience.com/your-1m-context-window-llm-is-less-powerful-than-you-think/)
- [When Refusals Fail: Unstable Safety Mechanisms in Long-Context LLM Agents — arXiv:2512.02445 (2025)](https://arxiv.org/html/2512.02445v1)
- [Context Rot: When More Tokens Mean Worse Results — Ray Svitla, self.md (2026-01-21)](https://self.md/concepts/context-rot/)
- [Not All Needles Are Found: How Fact Distribution and Don't Make It Up Prompts Shape Literal Extraction, Logical Inference, and Hallucination Risks in Long-Context LLMs — arXiv:2601.02023 (2025)](https://arxiv.org/pdf/2601.02023)
- [Thus Spake Long-Context Large Language Model — arXiv:2502.17129 (2025)](https://arxiv.org/html/2502.17129v1)
- [7 Ways Context Windows Still Break Modern LLMs — Kyle Beyke (2026-04-15)](https://kylebeyke.com/llm-context-window-hallucination-memory-limitations/)
- [Claude Code Context Window: Limits, Compaction & Management Guide — Morph Team (2026-02-27)](https://www.morphllm.com/claude-code-context-window)
- [Context Condensing — Kilo AI Documentation](https://kilo.ai/docs/customize/context/context-condensing)
- [Understanding Context Windows and Token Limits — Developer Toolkit (2026-04-30)](https://developertoolkit.ai/en/shared-workflows/context-management/context-windows/)
- [Context Window Management and Session Lifecycle for Long-Running AI Agents — Zylos Research (2026-03-31)](https://zylos.ai/research/2026-03-31-context-window-management-session-lifecycle-long-running-agents)
- [Managing context in GitHub Copilot CLI — GitHub Docs](https://docs.github.com/en/copilot/concepts/agents/copilot-cli/context-management)
- [Context Window Management for AI Coding: Complete Developer Guide — vexp Blog (2026-03-10)](https://vexp.dev/blog/context-window-management-ai-coding)
- [Taming Context Windows: Disable Auto-Compact for Better AI — Agentic Engineer (2025-10-28)](https://www.agentic-engineer.com/blog/2025-10-28-taming-context-windows)
- [CODEDELEGATOR: Decoupling Planning from Implementation — arXiv:2601.14914 (2026)](https://arxiv.org/pdf/2601.14914)
- [Compaction — OpenAI API Documentation](https://developers.openai.com/api/docs/guides/context-management)
