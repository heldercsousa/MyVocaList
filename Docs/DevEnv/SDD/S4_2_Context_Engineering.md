# S4.2 — Context Engineering

**Status:** Researched
**Predecessor(s) ID:** S4

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; peer-reviewed sources; practical patterns documented |

---

## Overview

Context engineering is the discipline of designing and building dynamic systems that curate and maintain the optimal set of tokens presented to a large language model at inference time. It is the natural progression from prompt engineering — where the focus was on crafting clever instructions — to the far more comprehensive practice of controlling everything the model sees when it makes a decision.

The term gained industry-wide adoption in 2025–2026, championed by researchers at Anthropic, practitioners at LangChain, and teams building production agent systems at companies like Manus, Zep, Google, and Cloudflare. By early 2026, context engineering has emerged as the single most important determinant of agent reliability and cost efficiency in real-world deployments.

**The core insight:** An agent with perfect reasoning capabilities but poor context will fail. An agent with adequate reasoning but excellent context will succeed. Context engineering shifts the optimization target from "build a better model" (a constraint the team cannot control) to "design a better information environment" (fully within the team's control).

---

## Why Context Engineering Matters More Than Prompt Engineering

For decades, software engineers optimized for a single consumer: the human developer. The rise of agentic AI development — where LLM-based agents autonomously read, write, navigate, and debug codebases — introduces a new primary consumer with fundamentally different constraints.

Human developers benefit from explanatory prose, visual formatting, and small files. LLMs benefit from semantic density (high-information tokens), consolidated access (fewer file reads), and navigational indexes. The optimization function changed, yet most teams still structure codebases and context files for human consumption.

**Anthropic's core definition (Effective Context Engineering for AI Agents, Sept 2025):** Context engineering is "thinking in context — considering the holistic state available to the LLM at any given time and what potential behaviors that state might yield." This shift from "prompt quality" to "context quality" is supported by empirical research:

- **Stanford / UC Berkeley (2026):** A peer-reviewed study of 9,649 experiments confirmed that the context surrounding a prompt influences output quality more than the prompt itself.
- **ETH Zurich (2026):** LLM-generated context files actively hinder AI agents and should be omitted. Human-curated, minimal files provide only marginal improvement when they contain zero-information tokens.
- **Dextra Labs (2025):** Enterprises transitioning from ad-hoc prompting to structured context engineering achieved 93% reduction in agent failures and 40–60% cost savings.

The practical implication: a 10-minute investment in context engineering yields better results than hours spent tuning prompts.

---

## The Four Canonical Strategies

Anthropic, LangChain, and the IMPACT Framework (swyx, AI Engineer Summit 2025) converge on four complementary strategies for context engineering. These are not mutually exclusive — production systems combine all four.

### 1. Write — Save context outside the window for later retrieval

Store information outside the active context window so it can be retrieved and refreshed on demand.

**Mechanisms:**
- Agent scratchpads and structured note-taking (progress files, task logs)
- Memory files updated at session end (`MEMORY.md`, topic-specific files)
- Progress logs that encode what was done and what comes next

**Why it matters:**
- Prevents context fragmentation — the agent's work history is preserved in a single, revisable file
- Enables multi-session coherence — subsequent sessions load only the first 200 lines of MEMORY.md rather than full conversation history
- Reduces hallucination by making past decisions auditable

**MyVocaList pattern:** `Docs/task-log.md` serves this role — a human-maintained, machine-readable log that agents read before and update after each task, providing persistence across sessions without consuming context during work.

---

### 2. Select — Pull relevant context in at the right time

Load the minimum context necessary for the current task, and load it only when the agent needs it.

**Mechanisms:**
- RAG (retrieval-augmented generation) for codebase knowledge
- Path-scoped rules that activate only when matched files are in context
- Just-in-time MCP tool calls that fetch library documentation on demand (rather than loading it statically)
- Sparse memory loading: an index in CLAUDE.md routes to at most 1–2 memory files per task type
- AGENTS.md as a project index — a single file loaded everywhere that tells agents which other files to read

**Why it matters:**
- Avoids the "lost-in-the-middle" effect (Stanford/UC Berkeley 2026): model correctness degrades significantly around 32,000 tokens, with attention concentrated at the beginning and end of context. Selective loading keeps relevant tokens at the front.
- Token budgets are finite. Every token spent on irrelevant context is a token not available for the active task.
- Example: MyVocaList's `.claude/rules/` directory contains path-scoped rules. Database-indexing rules do not load when the agent is working on UI-only tasks.

**Best practice per research (Design.dev, Anthropic 2026):** Structure context files to place the most critical instructions concise and early — information at the beginning and end of context receives stronger model attention.

---

### 3. Compress — Reduce context without losing signal

Retain only necessary tokens while preserving high-information content. This is distinct from aggressive summarization: compressing high-information tokens is counterproductive because it forces the model to reconstruct lost semantics during reasoning.

**Mechanisms:**
- Claude Code's auto-compact: triggers at 95% context window usage, summarizes the full session trajectory into a new context
- Hierarchical summarization in subagent architectures: subagents return 1,000–2,000 token summaries of work that consumed tens of thousands
- CLAUDE.md pruning: a root file beyond ~500 lines consumes context that could be used for actual work
- Semantic density optimization: eliminate zero-information tokens while preserving all high-information tokens

**Why it matters — and the common mistake:**
A February 2026 study (Beyond Human-Readable: Rethinking Software Engineering Conventions for the Agentic Development Era) found that aggressive compression increased total session cost by 67% despite reducing input tokens by 17%, because it shifted interpretive burden to the model's reasoning phase. The lesson: compress by removing noise, not by obscuring signal.

**Antipattern:** Abbreviating variable names, removing comments, or stripping context to reduce tokens. This creates more work for the model, not less.

---

### 4. Isolate — Split context across separate agents or sandboxes

Separate concerns into distinct agents, each with its own focused context window. This prevents one agent's exploration from polluting another's working context.

**Mechanisms:**
- Subagent architectures: specialized subagents handle focused tasks with clean context windows; the main agent sees only their condensed output
- This is the structural choice MyVocaList's workflow enforces: main agent orchestrates shell commands; subagents execute all file creation and editing with their own fresh context
- Multi-agent delegation patterns: coordinator → implementor → verifier (opposing incentives, separate contexts)
- Cross-session isolation: when a session approaches context exhaustion, spawn a new session with a fresh window rather than compacting

**Why it matters:**
- Context isolation prevents compounding hallucination — one agent's mistake does not cascade into the next agent's reasoning
- Subagents are stateless, so each isolation boundary is a checkpoint for verification
- Parallelism becomes safe: 4 subagents working on 4 tasks can be monitored independently, with only the coordinator's overhead consumed by the main session

---

## The Context Stack — What's Actually in the Window

A practical context window at any given inference step contains these layers, from system to immediate:

| Position | Layer | Content | Mechanism |
|----------|-------|---------|-----------|
| 1 | System instructions | CLAUDE.md, rules files, skills | Loaded at session start; survives compaction |
| 2 | Long-term memory | MEMORY.md, topic files | First 200 lines loaded; full file on-demand |
| 3 | Retrieved documents | MCP tool output, RAG results | Just-in-time, loaded only when queried |
| 4 | Tool definitions | Available MCP tools, agent tools | Loaded on-demand (Claude Code v2.1.7, Jan 2026) |
| 5 | Conversation history | Recent turns | Short-term working memory; triggers compaction at 95% |
| 6 | Current task | The active request | The immediate goal the model is solving |

Context engineering is the discipline of keeping each layer appropriately sized and making deliberate tradeoffs between them. The guiding principle per Anthropic: find the smallest set of high-signal tokens that maximize the likelihood of the desired outcome.

---

## CLAUDE.md Bloat — The Failure Mode

Practitioners consistently identify CLAUDE.md growth as the most common context engineering failure in long-running SDD projects. A file that starts at 150 lines reaches 1,000+ lines within six months as rules, patterns, and examples accumulate. At that size, a significant fraction of every session's context window is consumed by instructions before any work begins, capping the complexity of tasks the agent can handle.

**Research findings (Cloudflare iMARS team, Anthropic 2026):**
- A CLAUDE.md beyond ~500 lines becomes a context tax rather than an asset
- Rules added to steer agents away from mistakes are a signal of structural friction
- The ideal response is to fix the underlying friction (ambiguous codebase structure, missing type information, outdated defaults) and then delete the rule

**Mitigation strategies:**
1. **Periodic pruning:** Move stable, detailed patterns to separate files (memory, skills, rule files). Keep only the highest-value routing tables and non-negotiables in CLAUDE.md.
2. **Toolchain-first principle:** If a constraint can be enforced by a linter, type checker, or CI gate, it belongs in the toolchain, not CLAUDE.md. Restating it creates maintenance debt and dilutes signal.
3. **Version control:** Track CLAUDE.md size and review growth quarterly. If it exceeds 600 lines, schedule a refactoring.

**MyVocaList current state:** Root CLAUDE.md is ~550 lines. This is acceptable because content is high-density (architecture decisions, non-negotiables, role definitions) rather than explanatory. `.claude/rules/` contains detailed patterns organized by scope (mediatr-patterns.md, code-principles.md, testing.md), keeping system instructions lean.

---

## AGENTS.md — The Open Standard for Agent Context

AGENTS.md is an open, Linux Foundation-governed format that has become the de facto standard for agent context files. Unlike tool-specific formats (CLAUDE.md for Claude Code, .cursorrules for Cursor), AGENTS.md is natively supported by 60,000+ repositories and works across Claude Code, Cursor, GitHub Copilot, Codex, Windsurf, Gemini CLI, and 30+ other agents.

**AGENTS.md philosophy (ASDLC.io, 2026):**
AGENTS.md is a "README for agents" — a dedicated, predictable place for the minimal, human-authored context that agents need to work effectively on a project. It answers:
- What does this codebase do?
- How is it organized?
- What patterns and conventions matter?
- What should I ask before acting?

**Critical finding (Gloaguen et al., 2026):** LLM-generated context files reduce agent task success rates while increasing inference cost by over 20%. Developer-written context files provide only marginal improvement (+4%) — and only when they are minimal and precise. The conclusion is unambiguous: unnecessary requirements in context files actively harm agent performance, not because agents ignore them, but because agents follow them faithfully, broadening exploration and increasing reasoning cost.

**Structure (Design.dev, 2026):**
1. **Core identity** — what the project is, who uses it
2. **Principles** — the non-negotiable values (3–5 bullet points, not paragraphs)
3. **Architecture overview** — high-level structure for new sessions (orientation, not file-by-file navigation)
4. **Behavioral rules** — constraints that cannot be enforced by a tool
5. **Anti-patterns** — what not to do (use sparingly — the "pink elephant problem")
6. **Commands** — one-liners for common tasks

**Avoid in AGENTS.md:**
- Restating linter/type-checker rules (the tool is the source of truth)
- Restating library defaults (agents can infer from package.json, go.mod, etc.)
- Telling agents what not to do (activates the concept in their attention mechanism)
- Exhaustive file structure (agents discover this; orientation is what matters)

---

## Structured Context and File-Scoped Rules

A limitation of CLAUDE.md and AGENTS.md is that all instructions load for every request, even when irrelevant. If you're editing a CSS file, you don't need database schema rules. If you're debugging an API, you don't need UI component patterns.

**Structured Context (sctx.dev, 2026):** A YAML-based evolution of context files with two building blocks:

1. **Context entries** — scoped guidance for specific files and actions
   - Glob patterns control which files see which instructions
   - Action filters (`on: edit` vs `on: create`) control when they appear
   - Prompt positioning (`when: before` vs `when: after`) controls where they land in the LLM's attention window

2. **Decisions** — records of what was rejected and why
   - The code shows what was chosen
   - Decisions capture the invisible part: alternatives evaluated, constraints that killed them, conditions for revisiting

**Inheritance:** Context files placed throughout the codebase merge with parent directories, enabling modular scaling without duplication.

**MyVocaList pattern alignment:** `.claude/rules/` already implements this pattern conceptually via path-scoped imports in root CLAUDE.md, though not yet with formal AGENTS.yaml structure.

---

## The "Lost-in-the-Middle" Effect and Attention Budgets

Research from Stanford and UC Berkeley (2026) identified a critical LLM behavior: model correctness starts dropping significantly around 32,000 tokens, with models prioritizing information at the beginning and end of their context window. The middle section receives substantially weaker attention.

**Practical implications:**
- Your most critical instructions must be concise and positioned early
- Explanatory prose pushes working code further toward the problematic middle
- A 1,000-line CLAUDE.md positioned at session start means actual codebase content lands in the zone of reduced attention

**Mitigation:**
- Keep system instructions under 500 lines; move details to separate files
- Use an index or routing table early (first 50 lines) so the model knows where to find detailed context
- Load rules and memory on-demand via MCP or explicit retrieval rather than preloading everything

---

## Context Engineering in SDD Workflows

In SDD, agents work across long sessions on large codebases. Context pollution (irrelevant information crowding out relevant information) and context exhaustion (running out of window space) are two failure modes that directly degrade the spec-anchored workflow.

**SDD-specific context engineering strategies (Morphllm, 2026):**

1. **Spec-first with executable context:** The spec.md is the primary context input. Before any code is written, the spec defines the requirements, architecture, and testing strategy. This is "waterfall in 15 minutes" — rapid, structured planning that prevents the agent from going off the rails.

2. **Progress files as memory:** A progress.md or task-log.md tracks what was done and what comes next. Each session loads this file and updates it at exit. This provides multi-session coherence without loading full conversation history.

3. **Subagent compartmentalization:** The coordinator agent sees only high-level task structure and subagent outputs. Subagents work with focused tasks and fresh context windows. Failures in one subagent do not pollute the coordinator's reasoning.

4. **Filesystem as extended context:** When observations are too large for the context window (a full repository map, a large output file, a PDF), write it to the sandbox filesystem and keep only a reference (file path, URL). The model can read it back on demand.

---

## MCP Tool Search and On-Demand Loading (Claude Code v2.1.7+)

Claude Code v2.1.7 (January 2026) introduced MCP Tool Search, a mechanism that defers tool definition loading. Rather than including full tool schemas at session start (which consumes thousands of tokens), only tool names load at startup; full definitions are retrieved on-demand when the agent actually calls a tool.

**Impact:** An agent with 50 MCP tools configured (Context7, GitHub, Jira, SQLite, etc.) pays nearly zero context cost at startup, enabling what Anthropic calls "tool discovery without tool bloat."

**Comparison — before and after Tool Search:**
- **Before:** 50 tools × ~200 tokens per schema = 10,000 tokens of tool definitions at session start
- **After:** 50 tool names × ~5 tokens = 250 tokens; full schema only loaded on first call

This is a canonical example of the "Select" strategy in practice.

---

## Context Monitoring and Optimization Tools

A structural problem emerged in 2025–2026: developers had no visibility into how their context budget was consumed. Every open tab, instruction file, and conversation turn contributes silently to token usage. An Anthropic-priced input at $5.00/MTok means a single 200,000-token prompt costs $1.00 for input alone.

**Tokalator (arXiv:2604.08290, 2026):** An open-source context-engineering toolkit that includes:
- VS Code extension with real-time budget monitoring
- 11 slash commands for context optimization
- Cobb-Douglas quality modeling (econommetric analysis of token quality vs. cost)
- Cost calculators for caching break-even analysis
- Support for 17 LLMs across Anthropic, OpenAI, and Google

The `/optimize` command identifies low-relevance open tabs (R < 0.3 relevance score) and closes them to free up context budget. This automates the "compress" and "select" strategies.

**Cloudflare iMARS pattern:** The internal AI engineering stack at Cloudflare (April 2026) includes a cost-tracking backend that logs all MCP server calls, context window size per request, and inference cost. This enables teams to identify and fix expensive context decisions in production.

---

## Anti-Patterns — What Silently Fails

### 1. LLM-Generated Context Files

A February 2026 ETH Zurich study found that asking an LLM to generate your CLAUDE.md or AGENTS.md produces verbose, generic instructions that add token weight without meaningful signal. The AI has no basis for knowing your team's actual preferences or past architectural decisions.

**Correct pattern:** Context files are authored by humans, reviewed by the team, and versioned. They are living documents updated when patterns change, not one-off outputs from an AI.

### 2. Restating Toolchain Constraints

If your linter forbids a pattern, don't add it to CLAUDE.md. If your type system enforces a constraint, don't rephrase it in prose. The tool is the authoritative source; restating creates maintenance debt and dilutes signal.

### 3. Context Anchoring ("Pink Elephant Problem")

Telling an LLM what not to do ensures the concept is front-and-center in its attention mechanism. If AGENTS.md says "do not use tRPC," the agent might reach for it precisely because the token is highly active. The better response: delete the antipattern from the codebase, then delete it from the rules file.

### 4. Massive Single-File Context Files

A context file beyond ~1,000 lines is a red flag. Large CLAUDE.md or AGENTS.md files force the agent to parse conditional logic ("if you're editing SQL do X, if you're editing YAML do Y") in prose when it should be declarative (AGENTS.yaml with scoped rules).

---

## Relationship to Other S4 Topics

- **S4.1 — Memory Bank / Context Files** (predecessor): CLAUDE.md patterns, rules file design, auto memory, agent memory scopes, the Memory Bank methodology
- **S4.1.1 — Cross-Session Context Loss** (related): Why no single mechanism fully solves persistent architectural context; the residual gap after CLAUDE.md, auto memory, and Memory Bank approaches are applied
- **S4.3 — External Integrations** (related): MCP server selection and configuration; Context7 setup and routing rules; how MCP Tool Search integrates with context engineering

---

## Sources

- [Effective context engineering for AI agents — Anthropic Engineering Blog (Sept 2025)](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)
- [Context Engineering for Agents — LangChain Blog (July 2025)](https://blog.langchain.com/context-engineering-for-agents)
- [Context Engineering Guide — Design.dev (March 2026)](https://design.dev/guides/context-engineering/)
- [Context Engineering for AI Coding Agents: Rules That Work — The Agentic Blog (March 2026)](https://blog.appxlab.io/2026/03/26/context-engineering-ai-coding-agents/)
- [AI Agent Context Engineering: 8 Codebase Patterns — The Agentic Blog (April 2026)](https://blog.appxlab.io/2026/04/05/context-engineering-ai-coding-agents-2/)
- [Beyond Human-Readable: Rethinking Software Engineering Conventions for the Agentic Development Era — arXiv (2026)](https://arxiv.org/html/2604.07502v1)
- [Tokalator: A Context Engineering Toolkit for Artificial Intelligence Coding Assistants — arXiv:2604.08290 (2026)](https://arxiv.org/html/2604.08290v1)
- [The AI engineering stack we built internally — Cloudflare Engineering Blog (April 2026)](https://blog.cloudflare.com/internal-ai-engineering-stack/)
- [Agent Engineering: Harness Patterns, IMPACT Framework & Coding Agent Architecture — Morphllm (March 2026)](https://www.morphllm.com/agent-engineering)
- [Context Engineering: The Critical Discipline for AI Agents in 2026 — Context Graph Marketplace (Feb 2026)](https://www.contextgraph.tech/learn/context-engineering)
- [AGENTS.md Specification: A Research-Backed Guide — ASDLC.io (Feb 2026)](https://asdlc.io/practices/agents-md-spec/)
- [Structured Context — sctx.dev](https://sctx.dev/)
- [Coding Agent Loop Specification — StrongDM / Attractor](https://github.com/strongdm/attractor/blob/main/coding-agent-loop-spec.md)
- [Open Agent Specification Technical Report — arXiv:2510.04173](https://arxiv.org/html/2510.04173v1)
- [SuperSpec: Context Engineering and BDD for Agentic AI — Superagentic AI](https://super-agentic.ai/resources/super-posts/super-spec-context-engineering-bdd-agentic-ai)
