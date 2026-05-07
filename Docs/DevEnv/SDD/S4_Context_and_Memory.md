# S4 — Context & Memory

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Every AI coding agent session begins with a blank context window. Without deliberate engineering, the knowledge, decisions, and constraints that shaped one session are invisible to the next. The S4 cluster covers how SDD practitioners solve this structural limitation: through persistent instruction files (S4.1), careful curation of what enters the context window at each step (S4.2), and external integrations that supply live knowledge on demand (S4.3).

**The core challenge:** An LLM has no inherent memory. All knowledge it acts on must be present in its context window at inference time. For short, single-session tasks this is manageable. For multi-session software projects — the normal case in SDD — it creates a persistent context loss problem (S4.1.1) that must be actively solved by the team, not delegated to the model.

**The three-layer response:**

| Layer | Mechanism | What it solves |
|-------|-----------|----------------|
| **S4.1** | Memory bank / context files (CLAUDE.md, AGENTS.md, rules files) | Stable project context that survives session boundaries |
| **S4.2** | Context engineering | Optimizing what enters the context window at each step — avoiding pollution and exhaustion |
| **S4.3** | External integrations (MCP servers, Context7) | Just-in-time knowledge retrieval from live, versioned sources |

These layers are complementary. A well-written CLAUDE.md gives the agent stable project rules; context engineering ensures those rules do not crowd out working memory; MCP servers supply library documentation or issue-tracker state on demand without bloating startup context.

---

## S4.1 — Memory Bank / Context Files

### What they are

Context files are plain-text (typically Markdown) files that are loaded into the agent's context window at the start of every session. In Claude Code, the primary mechanism is `CLAUDE.md` — a file at the repository root (and optionally in subdirectories) that is automatically prepended to the agent's context on startup. Anthropic's official documentation describes CLAUDE.md as "the place for instructions and rules that should persist across sessions."

Other tools use equivalent conventions:
- **AGENTS.md** — used by some AI coding agents (Codex, Cursor variants). Claude Code can import an AGENTS.md from a CLAUDE.md to share a single source of truth across agents.
- **rules files** — path-scoped instruction files (e.g., `.claude/rules/`) that activate conditionally when the agent works in specific parts of the repository.
- **skills files** — reusable procedure definitions that can be invoked on demand rather than loaded unconditionally at startup.

### The two complementary systems in Claude Code

Claude Code provides two distinct memory mechanisms, both loaded at every session start:

| System | Who writes | What it contains | Scope |
|--------|-----------|------------------|-------|
| **CLAUDE.md** | The developer (manually) | Instructions, rules, architecture decisions, non-negotiables | Project, user, or org |
| **Auto memory** | Claude (automatically) | Learned patterns, debugging insights, build commands, preferences | Per working tree |

Auto memory is stored in `~/.claude/projects/{project}/memory/MEMORY.md` and topic-specific sibling files. The first 200 lines (or 25 KB) of `MEMORY.md` are loaded at session start; deeper topic files are available for on-demand retrieval during the session.

### Scoping and hierarchy

Claude Code reads CLAUDE.md files by walking up the directory tree from the current working directory. All discovered files are concatenated into context rather than overriding each other. A `CLAUDE.md` in a subdirectory adds context specific to that part of the codebase without overriding the root instructions. `CLAUDE.local.md` files are appended after `CLAUDE.md` at each level, so personal local notes take precedence over shared project instructions at the same directory level.

Rules files extend this further: unconditional rules load at session start; path-scoped rules activate only when the agent touches files matching specified patterns. This keeps context lean — a rules file for database indexing patterns does not consume context during UI-only tasks.

### The Memory Bank pattern

Community practitioners have extended the Claude Code memory primitives into a "Memory Bank" methodology — a structured set of files in the repository that together answer: what is this project, what has been done, what comes next, and what patterns matter.

A typical Memory Bank includes:
- `projectbrief.md` — core project overview
- `productContext.md` — requirements and goals
- `activeContext.md` — current work focus
- `systemPatterns.md` — architecture and recurring patterns
- `techContext.md` — technology stack details
- `progress.md` — development progress tracking

These files are imported via CLAUDE.md and updated at the end of each session. A subagent's exit checklist — update memory, commit, push — ensures the next session inherits accurate state.

### Agent memory (subagent-scoped, introduced 2026)

Claude Code v2.1.33 introduced a `memory` frontmatter field for subagents, giving each named subagent its own persistent markdown-based knowledge store. Subagent memory exists in three scopes: user-scoped (cross-project, not version-controlled), project-scoped (team-shared, version-controlled), and local (git-ignored, personal). The first 200 lines of the subagent's `MEMORY.md` are injected into its system prompt; additional topic files are available for on-demand reads. This enables a code-reviewer agent to accumulate patterns across reviews without those patterns polluting the main session context.

### Managed Agents memory stores (API, April 2026)

For applications built on the Claude Managed Agents API, Anthropic introduced workspace-scoped memory stores in April 2026 public beta. A memory store is a collection of text documents mounted at `/mnt/memory/` inside the agent's sandbox. Up to 8 stores can be attached per session; stores can be read-only (organizational standards) or read-write (per-user context). Every write creates an immutable version with 30-day retention.

---

## S4.2 — Context Engineering

### Definition

Context engineering is defined by Anthropic as "the set of strategies for curating and maintaining the optimal set of tokens (information) during LLM inference." It is the natural progression of prompt engineering: where prompt engineering focuses on how to write instructions, context engineering focuses on what information is present in the context window at each moment of inference.

The LangChain team (July 2025) offers a complementary framing: context engineering is "the art and science of filling the context window with just the right information at each step of an agent's trajectory." Both framings converge on the same insight — agent behavior quality is determined not just by the instructions the agent receives, but by the totality of what it can see when making each decision.

### Why it matters more for SDD

In SDD workflows, agents work across long sessions on large codebases. Context pollution (irrelevant information crowding out relevant information) and context exhaustion (running out of window space) are two failure modes that directly degrade the spec-anchored workflow. An agent that cannot maintain coherence across a feature implementation because its context window is full of obsolete conversation history is not following the spec — it is hallucinating against stale context.

### Four strategies

Anthropic and the LangChain research team identify the same four canonical strategies:

**1. Write** — Save context outside the window for later retrieval.
- Agent scratchpads and structured note-taking
- Memory files updated at session end
- Progress logs that encode what was done and what comes next

**2. Select** — Pull relevant context in at the right time.
- RAG (retrieval-augmented generation) for codebase knowledge
- Path-scoped rules that activate only when relevant
- Just-in-time MCP tool calls that fetch library documentation on demand (rather than loading it statically)
- Sparse memory loading: an index in CLAUDE.md routes to at most 1–2 memory files per task type

**3. Compress** — Reduce context without losing signal.
- Claude Code's auto-compact: triggers at 95% context window usage, summarizes the full session trajectory into a new context
- Hierarchical summarization in subagent architectures: subagents return 1,000–2,000 token summaries of work that may have consumed tens of thousands of tokens
- CLAUDE.md pruning: a root file beyond ~500 lines consumes context that could be used for actual work

**4. Isolate** — Split context across separate agents or sandboxes.
- Subagent architectures: specialized subagents handle focused tasks with clean context windows; the main agent sees only their condensed output
- This is the same structural choice MyVocaList's workflow enforces: main agent orchestrates shell commands; subagents execute all file creation and editing with their own fresh context
- Context isolation prevents one subagent's deep exploration from polluting another's window

### The context stack

A practical context window at any given inference step contains these layers:

| Position | Layer | Content |
|----------|-------|---------|
| 1 | System instructions | CLAUDE.md, rules files, skills — stable project context |
| 2 | Long-term memory | MEMORY.md, topic files — accumulated project knowledge |
| 3 | Retrieved documents | MCP tool output, RAG results — just-in-time knowledge |
| 4 | Tool definitions | Available MCP tools, agent tools — capability declarations |
| 5 | Conversation history | Recent turns — short-term working memory |
| 6 | Current task | The active request |

Context engineering is the discipline of keeping each layer appropriately sized and making deliberate tradeoffs between them. The guiding principle, per Anthropic: find the smallest set of high-signal tokens that maximize the likelihood of the desired outcome.

### CLAUDE.md bloat as a failure mode

Practitioners consistently identify CLAUDE.md growth as the most common context engineering failure in long-running SDD projects. A file that starts at 150 lines reaches 1,000+ lines within six months as rules accumulate. At that size, a significant fraction of every session's context window is consumed by instructions before any work begins, capping the complexity of tasks the agent can handle. The mitigation is regular pruning: move stable detailed patterns to memory files or skills; keep only the highest-value rules and routing tables in CLAUDE.md.

---

## S4.3 — External Integrations (MCP Servers)

### What MCP is

The Model Context Protocol (MCP) is an open standard for connecting AI agents to external tools and data sources. Claude Code can connect to hundreds of MCP servers, gaining access to databases, issue trackers, documentation repositories, monitoring dashboards, design tools, communication platforms, and more. MCP servers give agents real-time access to external systems — replacing the pattern of a developer manually copying data from another tool into a chat session.

MCP was introduced by Anthropic in late 2024 and had become the de facto standard for AI-tool integration by mid-2025. The ecosystem grew rapidly: by early 2026, Context7 alone indexed over 200 library documentation sources.

### How MCP integrates with context

From a context engineering perspective, MCP is a selective-loading mechanism. Rather than loading all external knowledge into CLAUDE.md at startup (which would consume enormous context), MCP servers expose tools that the agent calls only when it needs specific information. Claude Code's MCP Tool Search (introduced in v2.1.7, January 2026) makes this explicit: only tool names load at session start; full tool definitions are deferred and retrieved on demand. An agent with 50 MCP tools configured pays nearly zero context cost at startup.

When a tool is actually called, its output enters the context window — but only for the duration of the session and only when relevant to the current task.

### Context7 — the canonical documentation MCP

Context7 (github.com/upstash/context7, launched March 2025, 54,000+ GitHub stars by early 2026) is the most widely used MCP server for AI coding workflows. It pulls up-to-date, version-specific library documentation directly into the agent's context at the point of need, replacing the agent's reliance on potentially stale training data.

Context7 exposes two primary tools:
- `resolve-library-id` — resolves a library name into a Context7-compatible ID
- `query-docs` — retrieves documentation for a specific library and question

In MyVocaList's configuration, Context7 is auto-triggered for all .NET MAUI, DevExpress, EF Core, and MediatR documentation queries. This eliminates a major source of context engineering waste: the developer does not need to maintain local copies of library documentation in CLAUDE.md or memory files; Context7 supplies it on demand, always at the correct version.

Context7 also exposes a `docs-researcher` subagent pattern: spawn a subagent to look up documentation in a fresh context window, then receive only the answer — preventing documentation tool call output from polluting the main session context.

### Common MCP integrations in SDD workflows

| Integration type | Example servers | How it fits SDD |
|-----------------|-----------------|-----------------|
| Issue tracking | Jira, Linear, GitHub Issues | Agent reads spec-linked issues; creates tasks from requirements |
| Documentation | Context7, Confluence | Live library/org docs on demand; no stale training-data hallucinations |
| Databases | SQLite MCP, PostgreSQL | Agent queries real data for repository tests, content analysis |
| Code hosting | GitHub MCP | Agent reads PRs, creates branches, checks CI status |
| Design | Figma MCP | Agent reads designs directly; no copy-paste between tools |
| Monitoring | Sentry, Statsig | Agent reads live error data when debugging |

### MCP security considerations

MCP servers can access the internet, APIs, and external systems. Prompt injection through untrusted MCP output is a real attack vector — malicious content fetched from an external source could instruct an agent to write to memory stores or modify files. The standard mitigations: use trusted, reviewed MCP servers; set memory stores to `read_only` when the agent processes untrusted input; scope server permissions explicitly in CLAUDE.md.

---

## Relationship to Other S4 Topics

- **S4.1 — Memory Bank / Context Files** (deep): CLAUDE.md patterns, rules file design, auto memory, agent memory scopes, the Memory Bank methodology, Managed Agents memory stores.
- **S4.1.1 — Cross-Session Context Loss** (deep): Why no framework fully solves persistent architectural context; the residual gap after CLAUDE.md, auto memory, and Memory Bank approaches are applied; documented failure modes.
- **S4.2 — Context Engineering** (deep): Write/select/compress/isolate strategies with implementation patterns; CLAUDE.md sizing guidelines; subagent return protocols; compaction configuration.
- **S4.3 — External Integrations** (deep): MCP server selection and configuration; Context7 setup and routing rules; Jira/Confluence integration patterns; security and prompt injection mitigations; MCP Tool Search mechanics.

---

## Sources

- [How Claude remembers your project — Claude Code Docs (Anthropic)](https://docs.anthropic.com/en/docs/claude-code/memory)
- [Memory tool — Claude API Docs (Anthropic)](https://docs.claude.com/en/docs/agents-and-tools/tool-use/memory-tool)
- [Effective context engineering for AI agents — Anthropic Engineering Blog (Sept 2025)](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)
- [Context Engineering for Agents — LangChain Blog (July 2025)](https://blog.langchain.com/context-engineering-for-agents)
- [Context Engineering for Agents — Lance Martin / LangChain (June 2025)](https://rlancemartin.github.io/2025/06/23/context_engineering/)
- [Anatomy of a Context Window — Letta Blog](https://letta.com/blog/guide-to-context-engineering)
- [Context Engineering for AI Agents: Practical Guide — Prompt Builder (Nov 2025)](https://promptbuilder.cc/blog/context-engineering-agents-guide-2025)
- [Connect Claude Code to tools via MCP — Claude Code Docs (Anthropic)](https://code.claude.com/docs/en/mcp)
- [Context7 Platform — GitHub (upstash/context7)](https://github.com/upstash/context7)
- [Claude Code — Context7 MCP Integration Docs](https://context7.com/docs/clients/claude-code)
- [Agent Memory (subagent memory) — Claude Code Best Practice / Mintlify](https://www.mintlify.com/shanraisshan/claude-code-best-practice/reports/agent-memory)
- [Persistent Memory for Claude Managed Agents — AI Codex (April 2026)](https://www.aicodex.to/articles/claude-managed-agents-memory)
- [Claude Code Memory for Large Codebases — OpenAIToolsHub (April 2026)](https://www.openaitoolshub.org/en/blog/claude-code-memory-large-codebases)
- [Claude Code Memory, CLAUDE.md, Persistent Instructions — Data Studios (April 2026)](https://www.datastudios.org/post/claude-code-memory-claude-md-persistent-instructions-and-project-context-how-anthropic-s-coding)
- [Claude Code Memory Bank — hudrazine/claude-code-memory-bank (GitHub, July 2025)](https://github.com/hudrazine/claude-code-memory-bank)
- [MCP Integration — Agent Factory / Panaversity](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/general-agents/mcp-integration)
