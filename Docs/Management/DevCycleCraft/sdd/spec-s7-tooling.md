# S7 — Tooling

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

SDD workflows depend on a layered tooling stack. At the top are **spec-first IDEs and toolkits** that structure the requirements → design → tasks → implementation pipeline. Beneath them sit **AI coding assistants** — the agents that consume specs and generate code. Connecting both layers are **MCP servers**, which give agents real-time access to external tools, documentation, and services. Each layer is evolving rapidly and introduces its own risks: tool-specific spec formats that create switching friction, vendor lock-in from model and IDE coupling, and a young MCP protocol with known security gaps.

This section provides an overview of the S7.x subtopics. Deep coverage of vendor lock-in and protocol risks is in S7.1.1, S7.1.2, and S7.3.1.

---

## S7.1 — Spec-First IDEs and Tools

Three tools defined the spec-first tooling landscape in 2025–2026: **Amazon Kiro**, **GitHub Spec Kit**, and **Tessl**. All three structure development around a requirements → design → tasks artifact chain, but differ substantially in philosophy, scope, and SDD maturity level.

### Amazon Kiro

Kiro is a VS Code fork and CLI built by AWS, launched in July 2025. It is the most integrated of the three: a full agentic IDE where natural language prompts are transformed into structured spec artifacts (requirements, design, tasks) that then drive agentic code generation.

Key characteristics:
- **Three-file spec structure:** `requirements.md` (user stories + EARS-notation acceptance criteria), `design.md` (architecture and sequence diagrams), `tasks.md` (discrete, sequenced implementation tasks).
- **Feature and Bugfix Specs:** Two spec types. Feature Specs support Requirements-First or Design-First workflows. Bugfix Specs focus on root-cause analysis with regression prevention.
- **Agent Hooks:** Automated triggers on file-save and other events — agents run in the background to generate tests, documentation, or optimized code without user intervention.
- **Steering files:** Project-level markdown files analogous to rules files in CLAUDE.md, configuring agent behavior across all sessions.
- **MCP integration:** Native MCP support for external tool connectivity.
- **Model selection:** Uses Claude Sonnet 4.5 by default, or an "Auto" mode that blends frontier models for latency and cost.
- **SDD level:** Primarily spec-first; no documented spec-anchored lifecycle for long-running features.

Martin Fowler's Thoughtworks colleague Birgitta Böckeler noted that Kiro generated 16 acceptance criteria for a simple bug fix — illustrating the overhead risk at the lightweight end of the task spectrum. Kiro is best suited for structured feature work where the cost of upfront spec clarity is justified by implementation complexity.

### GitHub Spec Kit

Spec Kit is an open-source CLI toolkit released by GitHub in September 2025. It is the most widely adopted of the three, with over 92,000 GitHub stars by mid-2026, and supports 30+ AI coding agents including Copilot, Claude Code, Gemini CLI, Cursor, and Windsurf.

Key characteristics:
- **CLI-first:** Initializes a `.specify/` directory in any project with spec templates, constitution file, and agent prompt scaffolds.
- **Slash-command workflow:** `/speckit.constitution` → `/speckit.specify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`. Commands are issued inside the agent of choice.
- **Constitution:** A project-level "immutable principles" file — equivalent to the rules files in MyVocaList's workflow — that agents apply to every change.
- **Artifact set:** `spec.md`, `plan.md`, `tasks/` directory, optional `constitution.md`. These are checked into the workspace.
- **SDD level:** Effectively spec-first. Spec Kit creates a new spec branch per change request, which suggests it treats specs as per-feature artifacts rather than long-lived system documentation. The Tessl blog noted this confusion around spec lifetime.
- **Open source:** MIT-licensed. The most customizable of the three; teams can modify templates, add extensions, and presets.

### Tessl

Tessl launched two products in September 2025: the **Tessl Framework** (closed beta) and the **Tessl Spec Registry** (open beta). It is the most ambitious SDD tool and the only one explicitly targeting spec-as-source.

Key characteristics:
- **Tessl Framework:** Guides agents through structured spec-before-code workflows. Specs are stored in a `specs/` directory in the codebase as long-term memory. Supports both human-authored specs and AI-written "vibe-spec" workflows. Enforces hard guardrails through test integration — specs can link to tests via `[@test]` syntax.
- **Spec-as-source aspiration:** The most advanced Tessl deployments mark generated code with `// GENERATED FROM SPEC - DO NOT EDIT` and maintain a 1:1 spec-to-file mapping. As of beta, this is file-granular rather than component-granular.
- **Tessl Spec Registry:** A package manager and registry for agent context ("skills") and library documentation. Over 10,000 version-matched library specs that prevent API hallucinations. Teams publish internal specs (APIs, conventions, security rules) as versioned packages consumable by any agent.
- **Agent-agnostic:** Skills and context work across Claude Code, Cursor, Copilot CLI, Gemini, Codex, and Windsurf without lock-in.
- **MCP server:** The Tessl CLI doubles as an MCP server, providing agents with spec context at runtime.
- **SDD level:** The only tool actively working toward spec-anchored and spec-as-source levels; still maturing.

### Comparison Summary

| Dimension | Kiro | Spec Kit | Tessl |
|-----------|------|----------|-------|
| Distribution | Proprietary IDE + CLI | Open-source CLI | CLI + Registry (freemium) |
| SDD level achieved | Spec-first | Spec-first | Spec-first → Spec-as-source (beta) |
| Agent support | Kiro IDE / CLI only | 30+ agents | Claude Code, Cursor, Copilot, Gemini, Codex, Windsurf |
| Spec lifetime | Per-task | Per-feature branch | Long-lived codebase artifact |
| Vendor lock-in risk | High (AWS ecosystem) | Low (open source) | Medium (registry dependency) |
| Maturity | GA (July 2025) | GA (Sept 2025) | Beta (Sept 2025) |

---

## S7.2 — AI Coding Assistants

The three dominant AI coding assistants in the 2025–2026 SDD landscape are **Claude Code**, **Cursor**, and **GitHub Copilot**. Each has a different architectural philosophy that shapes how well it fits SDD workflows.

### Claude Code

Claude Code is Anthropic's terminal-native agentic coding tool. It operates at the file-system level — reading and writing files, running shell commands, and reasoning across entire repositories. It is the AI assistant most explicitly designed for SDD-style workflows.

SDD-relevant strengths:
- **Spec consumption:** Claude Code reads spec files (requirements.md, design.md, tasks.md) as first-class context. The tool's design assumes structured markdown context files.
- **CLAUDE.md / rules files:** Native support for project-level constitutional rules — the pattern used in MyVocaList's workflow. Multiple rules files can be scoped to different topics.
- **Agentic multi-step execution:** Naturally executes task-by-task sequences from `tasks.md`, checking off items and building across files.
- **MCP integration:** Native MCP client. Connects to any MCP server for real-time tool access.
- **Subagent delegation:** Supports spawning subagents — a pattern central to the MyVocaList workflow for parallel task execution.
- **Model:** Built on Claude Sonnet family. Also used as the backend for Kiro and, optionally, Cursor and Copilot.

Claude Code is the tool of choice for senior engineers doing complex multi-file work, large refactors, and spec-driven feature implementation. O'Reilly ran a dedicated live event on SDD with Claude Code in 2025. It is the reference implementation tool for the MyVocaList SDD workflow.

### Cursor

Cursor is a VS Code fork that places AI at the center of the editing experience. It supports all major frontier models (Anthropic, OpenAI, Gemini, xAI) and crossed 1 million paid users in 2024.

SDD-relevant characteristics:
- **Codebase indexing:** `@codebase` semantic search retrieves relevant context across the full project automatically. Strong for referencing existing patterns when implementing against a spec.
- **Composer mode:** Multi-file generation and large refactors with human review at each step.
- **Rules files (.cursorrules):** Project-level instruction files analogous to CLAUDE.md — can encode constitutional constraints.
- **Spec Kit integration:** Spec Kit supports Cursor via slash commands.
- **MCP support:** Cursor supports MCP servers in agent mode.
- **Limitation for pure SDD:** More interactive/IDE-centric than task-sequential. Better for human-in-the-loop editing than autonomous sequential spec execution.

### GitHub Copilot

GitHub Copilot is the most widely deployed AI coding assistant, with the broadest IDE coverage (VS Code, Visual Studio, JetBrains suite, Vim/Neovim, Emacs).

SDD-relevant characteristics:
- **Agent mode (GA 2025):** Autonomous multi-step coding tasks — reads files, proposes edits, runs commands, self-heals on errors.
- **Plan agent:** Built-in agent that produces a structured implementation plan before writing code — a spec-adjacent capability.
- **Spec Kit integration:** Spec Kit's primary integration target. Copilot executes `/specify`, `/plan`, `/tasks` slash commands.
- **Custom instructions:** Repository-level `copilot-instructions.md` encodes coding conventions and constraints.
- **MCP support:** GA support for MCP servers in agent mode.
- **Project Padawan / cloud agents:** Assigns GitHub issues to Copilot, which generates fully tested PRs asynchronously — closest to spec-as-source for GitHub-native teams.
- **Limitation:** Best suited for teams already invested in the GitHub ecosystem. Enterprise compliance focus. Less suited to complex terminal-based SDD workflows than Claude Code.

### Assistant Positioning for SDD

| Capability | Claude Code | Cursor | GitHub Copilot |
|------------|------------|--------|----------------|
| Spec file consumption | Native | Via rules / chat | Via custom instructions |
| Sequential task execution | Native | Composer mode | Agent mode |
| Terminal-native | Yes | No (IDE-based) | No (IDE-based) |
| MCP support | Native | Yes | Yes (GA 2025) |
| Best fit | Complex SDD, multi-step agents | Interactive codebase-aware editing | GitHub-native, enterprise teams |

---

## S7.3 — MCP Servers

The Model Context Protocol (MCP) is an open-source client–server protocol, created by Anthropic and released in November 2024, that standardizes how AI agents discover and invoke external tools and data sources. Rather than building custom integrations for every tool–agent pair, MCP defines a single interface that any MCP client (Claude Code, Cursor, Copilot, VS Code) can use to talk to any MCP server (GitHub, databases, documentation services, CI systems).

### Protocol Architecture

MCP follows a client–server model:
- **Host:** The application running the AI (e.g., VS Code, Claude Code).
- **Client:** The MCP client component within the host that manages server connections.
- **Server:** An MCP server program that exposes tools, prompts, and resources. Servers can run locally (stdio) or remotely (Streamable HTTP, SSE).

Agents discover available capabilities at runtime by querying servers — no hard-coded tool knowledge is required. This enables dynamic composition: an agent can query a database MCP server, find an error, search Sentry for related exceptions, and open a GitHub issue, all through three different MCP servers in a single workflow.

### Ecosystem Scale (2026)

MCP grew from a protocol announcement to an ecosystem of over 12,000 public servers in eighteen months. OpenAI, Google, and Microsoft adopted the protocol. It was donated to the Linux Foundation's Agentic AI Foundation for governance. Pinterest deployed it in production. PulseMCP, a registry, indexes 12,000+ servers. The protocol became the de facto standard, ahead of competing protocols (Google's A2A, Cisco's AGNTCY).

Notable MCP server categories in the SDD context:
- **Documentation:** Context7 (most popular MCP server; fetches current library docs to prevent API hallucination). Tessl (version-matched library specs + project skills).
- **Source control:** GitHub MCP (issues, PRs, code search, file operations — official GitHub server).
- **Cloud infrastructure:** Azure MCP, AWS MCP, Cloudflare.
- **Project management:** Linear, Jira.
- **Browser automation:** Playwright MCP (accessibility-snapshot-based, fast and deterministic).
- **Databases:** Supabase, Postgres, SQLite servers.

### MCP in SDD Workflows

MCP is a critical enabler for SDD at scale. Spec-first workflows require agents to have reliable context about:
- **Library APIs:** Without current documentation, agents hallucinate API signatures. Context7 and Tessl's registry address this.
- **Project state:** GitHub MCP provides live issue/PR status, enabling agents to reference task tracking from specs.
- **External services:** CI/CD, cloud infrastructure, and database servers allow agents to verify deployment state, run migrations, and check test results as part of spec execution.

The MyVocaList workflow uses Context7 for library documentation and a SQLite MCP for live database inspection during development — both canonical SDD use cases for MCP.

### Protocol Immaturity and Security Risks (S7.3.1 overview)

Despite rapid adoption, the MCP ecosystem carries significant immaturity risk. This is covered in depth in S7.3.1; key issues:

- **Security vulnerabilities:** Over 30 CVEs were filed against MCP servers in the first 60 days of 2026. Attack classes include tool poisoning (malicious tool descriptions that manipulate agent behavior), supply chain compromise (malicious packages masquerading as legitimate servers), SSRF (36.7% of 7,000+ analyzed servers vulnerable), and cross-tenant data leakage. The April 2026 OX Security disclosure identified an architectural flaw in official MCP SDKs across Python, TypeScript, Java, and Rust affecting 150M+ downloads. Anthropic declined to patch at the protocol level.
- **Authentication gaps:** 41% of MCP registry servers have no authentication. OAuth 2.1 integration was added to the spec in 2025 but adoption lags.
- **Context explosion:** Google dropped MCP from its Workspace CLI after testing revealed tool definitions from multiple servers inflating context windows to 40,000–100,000 tokens, degrading reasoning quality.
- **Registry poisoning:** Nine of eleven MCP registries were successfully poisoned in the OX April 2026 proof-of-concept. Supply chain discipline (pinned versions, integrity checks) is the primary mitigation.
- **Governance:** Linux Foundation governance reduces single-vendor control risk, but NIST's framework for agentic AI identity governance is still in draft (expected mid-2026).

### Vendor Lock-In and Tool-Switching Friction (S7.1.1 / S7.1.2 overview)

Tool choice at the spec-first layer carries structural lock-in risks:

- **Model coupling:** Kiro uses Claude Sonnet by default. Teams that build workflows around Kiro's spec format and model assumptions face migration costs if they need to switch models or IDEs. Spec Kit and Tessl are explicitly agent-agnostic to avoid this.
- **Spec format portability:** Kiro's three-file spec structure (requirements/design/tasks) is similar to Spec Kit's and MyVocaList's approach, but tooling-specific conventions (EARS notation formatting, hook triggers, steering file paths) differ. Migrating accumulated specs between tools requires reformatting.
- **Constitutional encoding:** Each tool's "constitution" or "steering" mechanism encodes project assumptions in tool-specific files (Kiro steering files, Spec Kit constitution.md, Cursor .cursorrules, CLAUDE.md). These are not portable across tools.
- **MCP mitigates agent lock-in:** Because MCP servers are agent-agnostic, tool integrations built as MCP servers (Context7, GitHub, Tessl) survive a switch from Claude Code to Cursor or Copilot. This is a key argument for MCP investment over direct-integration approaches.

---

## Sources

### Tier 1 — Primary
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler / Birgitta Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)
- [Specs documentation — Kiro](https://kiro.dev/docs/specs/)
- [Kiro — Agentic AI development from prototype to production](https://www.kiro.dev/)
- [kirodotdev/Kiro — GitHub repository](https://github.com/kirodotdev/Kiro)
- [github/spec-kit — GitHub repository](https://github.com/github/spec-kit)
- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog](https://resources.github.com/increasing-collaborative-development-with-ai/)

### Tier 2 — Secondary
- [Spec-Driven Development: Write the Spec, Not the Code — Bobby B / Substack](https://robbyb910.substack.com/p/spec-driven-development-write-the)
- [A look at Spec Kit, GitHub's spec-driven software development toolkit — Tessl Blog](https://tessl.io/blog/a-look-at-spec-kit-githubs-spec-driven-software-development-toolkit/)
- [How Tessl's Products Pioneer Spec-Driven Development — Tessl Blog](https://tessl.io/blog/how-tessls-products-pioneer-spec-driven-development/)
- [Spec-Driven Development with Tessl — Tessl Docs](https://docs.tessl.io/use/spec-driven-development-with-tessl)
- [What is Tessl? — Tessl Docs](https://docs.tessl.io/)
- [GitHub Copilot: The agent awakens — GitHub Blog](https://github.blog/news-insights/product-news/github-copilot-the-agent-awakens)
- [Introducing GitHub Copilot agent mode — VS Code Blog](https://code.visualstudio.com/blogs/2025/02/24/introducing-copilot-agent-mode)
- [GitHub Copilot vs Cursor vs Claude Code — AIToolVS](https://aitoolvs.com/github-copilot-vs-cursor-vs-claude-code-2025/)
- [What is the Model Context Protocol (MCP)? — GitHub Resources](https://github.com/resources/articles/what-is-mcp)
- [The MCP Server Ecosystem: A Developer's Guide for 2026 — Developers Digest](https://www.developersdigest.tech/blog/mcp-server-ecosystem-developers-guide)

### Tier 3 — Tertiary / Security Analysis
- [MCP security: The current situation — Red Hat](https://www.redhat.com/de/blog/mcp-security-current-situation)
- [MCP Security Supply Chain Crisis April 2026 — Cyber Strategy Institute](https://cyberstrategyinstitute.com/mcp-security-supply-chain-crisis/)
- [The State of MCP Security in 2026 — MCPBlog.dev](https://mcpblog.dev/blog/2026-03-12-state-of-mcp-security)
- [Securing MCP servers: the attack surface your AI agent just opened — notraced](https://notraced.com/articles/securing-mcp-servers)
- [Model Context Protocol and the Battle for AI Agent Standardisation — SoftwareSeni](https://www.softwareseni.com/model-context-protocol-and-the-battle-for-ai-agent-standardisation-across-frameworks-and-platforms/)
