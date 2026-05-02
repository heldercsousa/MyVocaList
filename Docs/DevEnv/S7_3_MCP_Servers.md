# S7.3 — MCP Servers

**Status:** Researched  
**Predecessor(s) ID:** S7

## Changelog

| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; 16 authoritative sources analyzed |

---

## Overview

The Model Context Protocol (MCP) is an open-source client–server protocol, created by Anthropic and released in November 2024, that standardizes how AI agents discover and invoke external tools, data sources, and context. Rather than building custom integrations for every tool–agent pair, MCP defines a single interface that any MCP client (Claude Code, Cursor, VS Code, GitHub Copilot) can use to talk to any MCP server (GitHub, databases, documentation services, CI systems).

MCP has grown from a protocol announcement to an ecosystem of over 12,000 public servers in eighteen months. OpenAI, Google, and Microsoft adopted the protocol. It was donated to the Linux Foundation's Agentic AI Foundation for governance. The protocol became the de facto standard, ahead of competing protocols (Google's A2A, Cisco's AGNTCY).

MCP is a critical enabler for SDD at scale because spec-first workflows require agents to have reliable, real-time context about APIs, project state, external services, and library documentation. Without MCP, agents hallucinate. With it, they stay grounded.

---

## Protocol Architecture

MCP follows a client–server model with three core components:

- **Host:** The application running the AI (e.g., VS Code, Claude Code, GitHub Copilot).
- **Client:** The MCP client component within the host that manages server connections and tool discovery.
- **Server:** An MCP server program that exposes tools, prompts, and resources. Servers can run locally (stdio) or remotely (Streamable HTTP, SSE — deprecated).

### Transport Modes

| Mode | Use Case | Latency | Scope |
|------|----------|---------|-------|
| **Stdio** | Local development, CLI tools | Sub-millisecond | Single-process |
| **Streamable HTTP** | Local or remote servers; low overhead | ~10–50ms | HTTP/WebSocket |
| **SSE (deprecated)** | Legacy remote servers | High; variable buffering | HTTP polling |

Agents discover available capabilities at runtime by querying servers — no hard-coded tool knowledge is required. This enables dynamic composition: an agent can query a database MCP server, find an error, search Sentry for related exceptions, and open a GitHub issue, all through three different MCP servers in a single workflow.

### Core Capabilities

MCP servers expose three types of capabilities:

1. **Tools:** Functions that agents can invoke to take actions (write databases, call APIs, modify files). Tools are model-controlled — the LLM decides when to use them based on context. Agents must have user approval or policy consent before invoking sensitive tools.

2. **Resources:** Passive data sources that provide read-only context (file contents, database schemas, API documentation, library docs). Applications decide how to use them — whether selecting relevant portions, searching with embeddings, or passing to the LLM.

3. **Prompts:** Reusable instruction templates for structured workflows. Prompts are user-controlled, invoked explicitly via slash commands (e.g., `/server.prompt-name`), and can reference available resources and tools to create comprehensive workflows.

---

## Ecosystem Scale and Maturity (2026)

The MCP ecosystem has reached production scale:

- **12,000+ public servers** indexed in PulseMCP registry and other registries
- **30+ AI agents supported** via Spec Kit, including Claude Code, Cursor, GitHub Copilot, Gemini CLI, Codex, Windsurf
- **Industry adoption:** Pinterest, Microsoft, Google, OpenAI, Anthropic, GitHub
- **Foundation governance:** Donated to Linux Foundation's Agentic AI Foundation (reduces single-vendor control risk)

### Notable MCP Server Categories in the SDD Context

| Category | Example Servers | Purpose |
|----------|-----------------|---------|
| **Documentation** | Context7, Tessl Registry | Prevent API hallucination by providing current library docs + version-matched specs |
| **Source Control** | GitHub MCP (official) | Issues, PRs, code search, file operations — read repo state live |
| **Project Management** | Linear, Jira, Azure DevOps | Task tracking, issue data, sprint planning integrated into agent workflows |
| **Infrastructure** | Azure MCP, AWS MCP, Cloudflare | EC2/VM state, deployment logs, infrastructure queries — live cloud context |
| **Databases** | Supabase, Postgres, SQLite servers | Schema discovery, live queries, real-time data access during development |
| **Browser Automation** | Playwright MCP | Accessibility snapshots + deterministic DOM queries for agent-driven UI testing |
| **Cloud Analytics** | Microsoft Fabric RTI (Real-Time Intelligence) | KQL queries, real-time data access from Eventhouse, natural language to SQL translation |
| **Code Analysis** | Supermodel MCP | Pre-computed code graphs for instant codebase understanding (symbol lookup, call-graph traversal) |

---

## MCP in SDD Workflows

MCP is essential for spec-first development because agents require reliable, up-to-date context about three domains:

### 1. Library APIs (Preventing Hallucination)

**Problem:** Without current documentation, LLMs hallucinate API signatures, parameter names, and method availability. This is the #1 cause of agent-generated bugs in SDD.

**Solution:** MCP documentation servers (Context7, Tessl Registry) expose:
- Current library documentation (continuously updated, never stale)
- Version-matched specs — agents know exactly which methods exist in the target version
- Deprecation warnings — guides agents away from obsolete APIs
- Code examples — agents can cite real usage patterns from the docs

**Example workflow:**
- Agent reads spec: "Validate user input with FluentValidation v14.2"
- Agent queries Context7 MCP server: "Show FluentValidation v14.2 API"
- Agent receives: current class names, method signatures, async patterns, validation rule examples
- Agent generates correct code without hallucination

**MyVocaList example:** The project uses Context7 MCP to fetch current EF Core 10 and MAUI APIs, preventing agent drift from the installed versions.

### 2. Project State (Live Information)

**Problem:** Specs become stale. Code changes during implementation. Agents need to reason about the current system, not outdated documentation.

**Solution:** GitHub MCP and project state servers expose:
- Live issue/PR status — agents reference current task tracking from specs
- Repository metadata — branch state, commit history, protected branches
- Code search results — agents verify implementation patterns before proposing code
- Deployment logs — agents check if a feature is already deployed before generating it

**Example workflow:**
- Agent reads spec: "Implement user registration"
- Agent queries GitHub MCP: "Is there an open PR for user registration?"
- Agent receives: PR#42 already in progress by another subagent
- Agent avoids duplicate work, coordinates instead

### 3. External Services (Verification and State Management)

**Problem:** Agents generate code that assumes certain state (database schema, permissions, deployment status) without verifying those assumptions.

**Solution:** Infrastructure and database MCP servers allow agents to:
- Verify database migrations have run before generating schema-dependent queries
- Query CI/CD status to confirm deployments succeeded
- Check feature flags and configuration before generating conditional code
- Trace the current state of cloud resources before proposing infrastructure changes

**Example workflow:**
- Agent generates migration: "Add `verified_at` column to users table"
- Agent queries SQLite MCP: "Does `users` table have `verified_at` column?"
- Agent receives: No — migration needed. Yes — migration already applied.
- Agent avoids duplicate or conflicting migrations

---

## MCP Security Landscape (April 2026)

Despite rapid adoption, the MCP ecosystem carries significant immaturity risk that impacts SDD deployments at scale.

### Vulnerability Classes (30+ CVEs filed in first 60 days of 2026)

| Attack Class | Impact | Example / CVEs |
|--------------|--------|---|
| **Tool Poisoning** | Malicious tool descriptions manipulate agent behavior (prompt injection via tool schema) | Agents tricked into unsafe operations or data exfiltration by crafted tool names, descriptions, or parameter hints |
| **Supply Chain Compromise** | Malicious packages masquerade as legitimate servers (npm/PyPI poisoning) | Typosquatting (`context7-docs` vs `context7`), abandoned package takeovers, dependency confusion attacks |
| **SSRF (Server-Side Request Forgery)** | 36.7% of 7,000+ analyzed MCP servers vulnerable | Agents unknowingly make requests to internal infrastructure, exfiltrate secrets, trigger internal APIs |
| **Cross-Tenant Data Leakage** | Shared MCP server instances expose data across users/organizations | One user's API key accidentally returned in responses to another user |
| **SDK Vulnerabilities** | Flaws in MCP client libraries affect 150M+ downloads | April 2026 OX Security disclosure: architectural flaw in official Python/TypeScript/Java/Rust SDKs affecting authentication and message routing |
| **Registry Poisoning** | Attackers add malicious servers to public registries | 9 of 11 MCP registries successfully poisoned in OX April 2026 proof-of-concept |
| **Authentication Gaps** | 41% of registry servers have no authentication; OAuth adoption lags | Servers accept any request without identity verification |
| **Context Explosion** | Tool definitions inflate context windows to 40,000–100,000 tokens | Google dropped MCP from Workspace CLI after testing; context bloat degraded reasoning quality |

### Governance Gaps

- **NIST agentic AI identity governance framework** still in draft (expected mid-2026)
- **MCP doesn't define policy enforcement** — standardizes connectivity and discovery, not authorization. Client/host must implement approval gates, rate limiting, and audit logging.
- **Anthropic declined to patch at protocol level** after OX disclosure — security layer is client responsibility

### Mitigation Strategies (Production Deployments)

Enterprises deploying MCP at scale implement a **governance proxy layer** (e.g., Permit MCP Gateway, Agent Governance Toolkit, REVA AI Trust Gateway, IBM Context Forge):

1. **Tool definition scanning** — scan incoming tool descriptions for hidden instructions, typosquatting, and adversarial patterns before exposing to agents
2. **Per-call authorization** — declarative rules (YAML, OPA/Rego, Cedar) evaluated before every tool invocation
3. **Identity propagation** — agents receive cryptographic identities (Ed25519 + quantum-safe ML-DSA-65) with trust scores on a 0–1000 scale
4. **Response validation** — tool outputs checked against content policies before returning to agent
5. **Audit logging** — every tool call logged with agent identity, user identity, tool name, outcome
6. **Supply chain discipline** — pinned versions, integrity checks, registry allowlists (approved servers only)

### Practical Risk Management for SDD

For MyVocaList and similar projects:

- **Local MCP servers only** (Context7, SQLite, GitHub via official channels) until registry poisoning is addressed
- **OAuth 2.1 enforcement** on external servers; reject unauthenticated servers
- **Pinned server versions** in configuration — no "auto-update" from public registries
- **Read-only tool allowlists** — explicitly enumerate which tools agents are permitted to use
- **Monitor CVE feeds** — watch NVD and official MCP advisories for SDK updates

---

## MCP Operational Patterns in SDD

### Pattern 1: Context7 for Documentation (Preventing Hallucination)

**Setup:**
```
Host: Claude Code (or Cursor, Copilot)
MCP Server: Context7 (remote HTTP)
Transport: Streamable HTTP
Configuration: .claude/settings.json or workspace config
```

**Workflow:**
- Agent encounters unknown library API in spec
- Agent queries Context7: `resolve-library-id` → `query-docs`
- Agent receives current docs + code examples
- Agent generates implementation without hallucination

**MyVocaList usage:** Context7 is auto-triggered for .NET MAUI, DevExpress, EF Core, MediatR documentation (see CLAUDE.md).

### Pattern 2: Tessl Registry for Project Skills and Specs

**Setup:**
```
Host: Claude Code, Cursor, Copilot, Gemini, Codex, Windsurf (agent-agnostic)
MCP Server: Tessl Registry (remote HTTP)
Transport: Streamable HTTP
Configuration: Tessl CLI installation + workspace .tessl directory
```

**Features:**
- **Version-matched library specs** — prevents API drift across project dependencies
- **Internal project skills** — teams publish domain-specific specs and patterns as versioned packages
- **Agent-agnostic delivery** — same skill works across all major AI assistants (no lock-in)

**Use case:** A team publishes internal "User Authentication Spec" (v2.1) via Tessl Registry. Any agent (Claude, Cursor, Copilot) can consume it without re-explaining the company's auth patterns.

### Pattern 3: GitHub MCP for Live Project State

**Setup:**
```
Host: GitHub Copilot, Claude Code, Cursor
MCP Server: GitHub MCP (official; defaults to read-only)
Transport: HTTP (OAuth via GitHub)
Configuration: Repository secrets + copilot-instructions.md or CLAUDE.md
```

**Tools exposed:**
- `list_issues` / `get_issue` — live issue status
- `list_pull_requests` / `get_pull_request` — PR review state, CI results
- `search_code` — verify implementation patterns exist in the codebase
- `create_issue` / `create_pull_request` — agents propose changes as PRs (with approval gates)

**Workflow in SDD:**
- Spec says: "Implement feature X, ensure no PR already in progress"
- Agent queries GitHub MCP: `search_code` for feature X branch + `list_pull_requests` with labels
- Agent receives: PR#99 in draft, assigned to subagent-B
- Agent coordinates instead of duplicating work

### Pattern 4: Database/Infrastructure MCP for State Verification

**Setup:**
```
Host: Claude Code, local agents
MCP Server: SQLite (local development), Postgres, Azure, AWS
Transport: Stdio (local), HTTP (remote with auth)
Configuration: Connection strings in environment, pinned in .mcpd.toml
```

**Use case:** Agents verify schema state before generating queries, check feature flag state before conditional logic, query real data samples during development.

**MyVocaList usage:** SQLite MCP at `.claude/MyVocaList.db` (pulled from emulator via `adb exec-out`). Agents can verify current schema before writing EF Core configurations.

### Pattern 5: Orchestration Layer for Multiple Servers

**Tools:** Mozilla `mcpd`, IBM Context Forge, Permit MCP Gateway, REVA Trust Gateway

**Problem:** Managing multiple MCP servers across local, dev, and prod environments — secrets management, version pinning, auth, observability — becomes complex.

**Solution:** Orchestration layers provide:
- **Declarative configuration** (`.mcpd.toml`, `context-forge.yaml`) — version-pinned servers, per-environment secrets
- **Unified endpoint** — single MCP client connects to orchestrator; orchestrator federates to 10+ servers
- **Governance** — centralized policy enforcement, per-server auth, audit logging
- **SDKs** — language-specific clients (`mcpd_sdk` for Python) that call tools like native functions
- **Observability** — OpenTelemetry tracing across federated servers

**Workflow:**
- Agent calls: `mcpd_sdk.call('github', 'list_issues')`
- Orchestrator routes to GitHub MCP, handles auth, logs, enforces policy
- Agent receives result without auth boilerplate

---

## MCP Specification Evolution and Future Directions

### Emerging Features (Post-November 2024)

| Feature | Status | Impact |
|---------|--------|--------|
| **OAuth 2.1 integration** | Added 2025, adoption lags | Improved authentication; 41% of servers still unauthenticated |
| **Tool batching** | In development | Reduce round-trips: agent sends 10 tool calls in one request |
| **Streaming tool outputs** | Proposed | Long-running tools (migrations, builds) stream results instead of waiting for completion |
| **Sampling / LLM calls from MCP** | GA 2025 in VS Code | MCP servers can request LLM inference (e.g., MCP server summarizes data, calls LLM for analysis) |
| **MCP Apps (interactive UI)** | GA 2026 in VS Code | Tools can return interactive components (drag-drop lists, forms, visualizations) rendered inline |
| **Workflow-level policies** | Roadmap 2026 | Policy engine evaluates sequences of tool calls (not just individual invocations) for anomaly detection |

### Competing Protocols

- **Google A2A (Agent-to-Agent)** — Focuses on agent-to-agent communication; narrower scope than MCP
- **Cisco AGNTCY** — Enterprise-focused; closed; less community adoption

MCP's lead is structural: first-mover advantage, Linux Foundation governance, 12,000+ servers, multi-vendor support. Competitors have not displaced it.

---

## Anti-Patterns and Failure Modes

### Anti-Pattern 1: Too Many Servers in One Agent

**Problem:** Agent context window inflated by 10+ MCP server tool definitions (40,000–100,000 tokens). Reasoning quality degrades.

**Solution:**
- Use **tool discovery** instead of upfront exposure: declare which tools the agent might need; MCP client loads definitions on-demand
- Use **orchestration layer** to curate tool set per agent role: planner gets fewer tools than implementer

### Anti-Pattern 2: Blocking on Slow Remote MCP Servers

**Problem:** Agent queries a remote MCP server (Jira, Confluence) that times out. Agent stalls.

**Solution:**
- **Local caching** — cache tool definitions and recent results in-process
- **Timeout + fallback** — if remote MCP server doesn't respond in 5s, agent continues with cached state or skips that context
- **Parallel queries** — query multiple servers in parallel; don't wait for slowest

### Anti-Pattern 3: Trusting Unauthenticated MCP Servers in Production

**Problem:** 41% of MCP registry servers have no authentication. Agent connects to imposter server via typosquatting (e.g., `context7-docs` instead of `context7`).

**Solution:**
- **Allowlists only** — enumerate approved servers in configuration; block all others
- **Pinned versions** — don't auto-update from public registries
- **Registry integrity** — use official GitHub orgs / Anthropic-published servers where possible
- **Supply chain scanning** — scan dependencies for MCP server version pins

### Anti-Pattern 4: Exposing All Tools to Agents Autonomously

**Problem:** Agent has access to `delete_all_issues`, `truncate_database`, `approve_pr_without_review` without safeguards.

**Solution:**
- **Read-only tool allowlists** — agent can only call `list_issues`, not `delete_issue`
- **Per-tool consent** — sensitive tools require user approval before execution
- **Governance proxy** — enforcement layer (Permit, REVA, AGT) gates dangerous tools

---

## Sources

### Tier 1 — Primary

- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Model Context Protocol Official Specification](https://modelcontextprotocol.org/docs/learn/server-concepts) — LF-governed spec
- [What is the Model Context Protocol (MCP)? — GitHub](https://github.com/resources/articles/what-is-mcp) — Official GitHub overview (April 2026)
- [Understanding MCP servers — Model Context Protocol](https://modelcontextprotocol.org/docs/learn/server-concepts)
- [Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — Martin Fowler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html) — SDD context + MCP role in spec execution
- [Tools specification — Model Context Protocol](https://modelcontextprotocol.io/specification/latest/server/tools)

### Tier 2 — Secondary

- [Model Context Protocol (MCP) in Zed Editor](https://zed.dev/docs/ai/mcp)
- [MCP developer guide — VS Code Extension API](https://code.visualstudio.com/api/extension-guides/mcp) — Current MCP capabilities in VS Code
- [Securing MCP: A Control Plane for Agent Tool Execution — Microsoft for Developers](https://developer.microsoft.com/blog/securing-mcp-a-control-plane-for-agent-tool-execution) — Agent Governance Toolkit (AGT), April 2026
- [Connect agents to external tools — GitHub Copilot Docs](https://docs.github.com/copilot/customizing-copilot/using-model-context-protocol/extending-copilot-coding-agent-with-mcp) — GitHub's official MCP integration and security model
- [Model Context Protocol (MCP) — OpenAI Agents SDK](https://openai.github.io/openai-agents-js/guides/mcp) — OpenAI's MCP support, Streamable HTTP, hosted tools
- [Build Agents using Model Context Protocol on Azure — Microsoft Learn](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp) — Azure MCP server, cloud integration patterns
- [Permit MCP Gateway — Documentation](https://docs.permit.io/permit-mcp-gateway/overview/) — Authorization proxy for MCP, policy enforcement, audit logging
- [Runtime Security for MCP Servers — REVA AI](https://www.reva.ai/solutions/mcp-server-security) — Trust Gateway, behavioral risk scoring, workflow-level policies
- [mcpd documentation — Mozilla AI](https://mozilla-ai.github.io/mcpd/) — Orchestration toolchain, zero-config server setup, declarative configuration
- [IBM/mcp-context-forge — GitHub](https://github.com/IBM/mcp-context-forge) — Registry, proxy, and governance federation for MCP
- [Introducing MCP Support for Real-Time Intelligence (RTI) — Microsoft Fabric Blog](https://blog.fabric.microsoft.com/en-us/blog/introducing-mcp-support-for-real-time-intelligence-rti/) — Live data access pattern (KQL queries, schema discovery)

### Tier 3 — Tertiary and Specialized

- [yi-john-huang/sdd-mcp — GitHub](https://github.com/yi-john-huang/sdd-mcp) — MCP server implementing SDD workflows; agent skills, steering, rules, hooks (v3.3, March 2026)
- [supermodeltools/mcp — GitHub](https://github.com/supermodeltools/mcp) — Codebase analysis MCP (code graphs, symbol context, GraphRAG mode)
- [MCP Security: The Current Situation — Red Hat](https://www.redhat.com/de/blog/mcp-security-current-situation) — Vulnerability taxonomy, mitigation strategies
- [The State of MCP Security in 2026 — MCPBlog.dev](https://mcpblog.dev/blog/2026-03-12-state-of-mcp-security) — Ecosystem vulnerability analysis, registry poisoning, auth gaps
- [Securing MCP servers: the attack surface your AI agent just opened — notraced](https://notraced.com/articles/securing-mcp-servers) — SSRF, tool poisoning, supply chain compromise threat models
