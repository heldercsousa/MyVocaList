# S4.3 — External Integrations

**Status:** Researched  
**Predecessor(s) ID:** S4

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

External integrations — MCP servers, issue trackers, documentation repositories, design tools — are a critical component of the context engineering layer (S4.2). Rather than loading all external knowledge into CLAUDE.md at startup, MCP servers expose tools and resources that agents call on demand. This selective-loading mechanism keeps startup context lean while providing just-in-time access to live data.

This section covers what MCP is, how it fits the SDD context pipeline, canonical integration patterns (Context7, Jira, Confluence, GitHub), and practical security considerations.

---

## What MCP Is

The **Model Context Protocol (MCP)** is an open standard (introduced by Anthropic in November 2024, stewarded by the Agentic AI Foundation as of 2025) that enables seamless integration between LLM applications and external data sources and tools. It uses a client-host-server architecture with JSON-RPC 2.0 messaging.

| Role | Definition |
|------|-----------|
| **Host** | The LLM application (Claude Desktop, Claude Code, Cursor, Continue, Zed, Copilot) that initiates MCP connections |
| **Client** | Connectors within the host that manage individual server connections |
| **Server** | Services that expose capabilities (tools, resources, prompts) to the client |

MCP was adopted rapidly: by early 2026, Cursor, Continue, Zed, Cline, and other AI coding agents had implemented MCP support. The ecosystem includes hundreds of public MCP servers and reference implementations.

### The three primitives

MCP servers expose three types of capabilities:

| Primitive | Direction | Use case | Example |
|-----------|-----------|----------|---------|
| **Tools** | Server → Model (model-controlled) | Functions the AI can invoke for actions | `create_jira_issue`, `run_query`, `search_docs` |
| **Resources** | Server → Model/User (app-controlled) | Contextual data and information the model can read | File contents, database records, Confluence pages, live data feeds |
| **Prompts** | User → Model (user-controlled) | Reusable templates and workflows users can trigger | Slash commands, interaction patterns, predefined workflows |

---

## How MCP Fits the SDD Context Pipeline

From a context engineering perspective (S4.2), MCP is a selective-loading mechanism that replaces the older pattern of embedding all external knowledge into CLAUDE.md:

**Before MCP:** Load library docs, API references, schema definitions into CLAUDE.md or memory files. This consumes context at session startup for information the agent may never need. Updates become manual.

**With MCP:** Declare MCP servers in configuration. Only tool names load at startup (~zero context cost with Claude Code's MCP Tool Search, introduced January 2026). When the agent needs to look up documentation, query Jira, or fetch a GitHub PR, it calls the tool on demand. The tool result enters context only for that moment, then is discarded.

This pattern scales to complex workflows: an agent can read a Jira ticket, link through to a Confluence page, search a GitHub PR, and fetch documentation from Context7 — all without bloating startup context, because each tool call is made only when relevant to the current task.

### MCP Tool Search (Claude Code, v2.1.7+)

Claude Code introduced MCP Tool Search in January 2026: tool names are indexed and searchable, but full tool definitions are deferred and retrieved only when a tool is actually invoked. This further reduces startup overhead.

---

## Context7 — The Canonical Documentation MCP

**Context7** (github.com/upstash/context7, launched March 2025, 54,000+ stars by early 2026) is the most widely adopted MCP server for AI coding workflows. It solves a critical problem: keeping library documentation current without relying on stale training data.

Context7 fetches version-specific documentation directly from source — GitHub repos, official docs, PyPI pages — and presents it to the agent in real time. No hallucination about APIs that don't exist in the current library version. No training-data staleness.

### How it works

1. **Resolve library ID** — Agent calls `resolve-library-id` with a library name (e.g., ".NET MAUI"). Context7 returns a canonical ID (e.g., `/microsoft/maui`).
2. **Query docs** — Agent calls `query-docs` with the ID and a natural-language question. Context7 returns relevant documentation sections, code examples, and API signatures.

### Configuration in SDD workflows

In MyVocaList's CLAUDE.md, Context7 is auto-triggered for all `.NET MAUI`, `DevExpress`, `EF Core`, and `MediatR` documentation queries. This eliminates the need to maintain local copies of API docs or worry about version mismatches.

**Optional API key:** Context7 works without an API key (subject to rate limits). With a free API key from context7.com/dashboard, rate limits increase and private repositories can be indexed.

### Rule-based auto-invocation

To avoid typing "use context7" in every prompt, add a rule to CLAUDE.md or the MCP client configuration:

```
When the agent needs library/API documentation, setup instructions, configuration details, 
version migration guides, library-specific debugging, or CLI tool usage — automatically invoke 
Context7. This applies to .NET MAUI, DevExpress, EF Core, MediatR, and other libraries.
```

---

## Jira & Confluence Integration

Atlassian Cloud sites (Jira + Confluence) expose an official **Rovo MCP Server** (beta as of May 2025, production-ready by early 2026). This server enables agents to:

- Summarize and search Jira issues with JQL queries
- Summarize and search Confluence pages with CQL queries
- Create Jira issues, Confluence pages, and bulk operations
- Transition issues through workflows
- Perform multi-step actions (e.g., create issue, link to Confluence page, add comment)

### Authentication

Rovo uses OAuth 2.1: the first user to authorize via browser grants the MCP app permissions to access Jira and Confluence. Subsequently, any user with access to those systems can use the integration without additional setup.

Alternatively, organizations can allow API token authentication (configurable by admin in Rovo MCP server settings).

### Configuration

In Claude Code or Cursor, add the Rovo server to your MCP configuration:

```json
{
  "mcpServers": {
    "atlassian": {
      "type": "http",
      "url": "https://mcp.atlassian.com/v1/mcp"
    }
  }
}
```

### Community alternatives

For Jira Data Center / Server (non-Cloud), use community-maintained servers like `sooperset/mcp-atlassian` (requires personal access token authentication).

### Practical use cases in SDD

- **Sprint planning:** Agent reads all Jira tickets for the sprint, cross-references Confluence docs, flags tickets that reference outdated specs.
- **Context-aware debugging:** Developer asks Claude to help with a bug; Claude pulls the ticket details from Jira, follows Confluence links in the ticket description, searches GitHub for related PRs, and synthesizes a full status summary.
- **Bulk operations:** Agent can create related Jira issues in parallel, linking them to the same epic or Confluence page in one operation.

---

## Other Common MCP Integrations

| System | Server | What it enables |
|--------|--------|-----------------|
| **GitHub** | `@modelcontextprotocol/server-github` (official) | Read/create issues, PRs, branches; check CI status; search repos; view security alerts |
| **Figma** | Figma MCP (HTTP, requires account) | Read design files, components, variables; extract design tokens; verify design adherence in code |
| **Databases** | `sqlite` MCP (reference), `postgresql` MCP | Query live data; verify schema; test migrations; generate test data |
| **Slack** | Community MCPs | Send/read messages; post status updates; integrate agent decisions into team channels |
| **Browser automation** | Playwright MCP | Run end-to-end interactions; capture screenshots; verify visual regressions; test accessibility |

---

## MCP in the SDD Specification Pipeline

MCP integrations support all three SDD phases:

| Phase | Example | Benefit |
|-------|---------|---------|
| **Planning** | Agent reads GitHub issues and Jira backlog; Context7 pulls library docs for feasibility assessment | Fresh context for estimation and design decisions |
| **Implementation** | Agent calls Context7 for setup steps, DevExpress docs, EF Core patterns; checks GitHub CI status | Up-to-date API docs; early feedback on code quality |
| **Verification** | Agent queries SQLite for test data; reads Jira acceptance criteria; searches GitHub for related PRs | Live data for assertions; full acceptance traceability |

---

## Security Considerations

MCP opens new attack surfaces. Servers can access the internet, APIs, and external systems. Untrusted MCP output can contain prompt injection payloads.

### Mitigations

1. **Use trusted, reviewed MCP servers** — Prefer official servers (Context7, Rovo, GitHub MCP, Figma MCP) over community implementations unless they have strong reputation and maintenance.

2. **Set memory stores to read-only** — If the agent processes untrusted input (e.g., reading user-provided Jira descriptions), mount memory stores in read-only mode to prevent writes triggered by injection.

3. **Scope server permissions** — Configure MCP servers with minimal permissions. A GitHub MCP server that can only read should not have push access.

4. **OAuth 2.1 over API tokens** — Prefer OAuth (used by Rovo, Figma) over static tokens. OAuth enables per-session authorization and easier revocation.

5. **IP allowlisting (Atlassian)** — If using Rovo MCP with corporate firewalls, configure IP allowlists in Atlassian Administration to restrict which IPs can connect.

6. **Audit logging** — Enable audit logs in systems that support them (Jira, Confluence, GitHub). MCP operations are logged as "AI agent" or integration actions and appear in audit trails.

---

## MCP Protocol Evolution (2025–2026)

The MCP specification evolved rapidly as the ecosystem matured:

| Release | Date | Key additions | SDD impact |
|---------|------|----------------|-----------|
| **Initial release** | Nov 2024 | Base protocol, tools, resources, prompts; stdio transport | Foundation established |
| **March 2025** | Mar 2025 | OAuth 2.1 support; Streamable HTTP transport (replacing HTTP+SSE) | Remote, production-grade servers became viable |
| **November 2025** | Nov 2025 | Structured tool outputs with output schemas; tool annotations; elicitation; session management | Better type safety and human-in-the-loop patterns |
| **Current (2026)** | 2026 | MCP Tool Search (Claude Code, Jan 2026); OAuth improvements; resource subscriptions | Agents can discover and call hundreds of tools without context bloat |

The "Streamable HTTP" transport (March 2025 release) was a critical fix for remote MCP: it replaced the dual-endpoint HTTP+SSE pattern with a single HTTP endpoint accepting POST requests, significantly simplifying deployment.

---

## Limitations and Open Problems

### 1. Tool descriptions are critical but underspecified

The `description` field on each tool is the primary interface between the model and the external system. Vague descriptions ("manages orders") lead to misuse. Precise descriptions (inputs, outputs, side effects, when to use this tool vs. alternatives) are the difference between reliable agents and frustrating ones. **The MCP spec does not mandate description quality.**

### 2. Fine-grained authorization is not solved

MCP defines OAuth 2.1 at the connection level but does not specify fine-grained authorization at the tool or resource level. Whether a specific agent can invoke a specific tool with specific parameters is an application-level concern, not a protocol concern. Organizations must implement their own access control middleware.

### 3. Tool versioning and evolution

Tool definitions change over time — parameters get added, output formats evolve, tool behavior shifts. The `tools/list_changed` notification helps clients detect changes, but managing the transition is the responsibility of the operator.

### 4. Prompt injection via untrusted MCP output

MCP servers can fetch untrusted content (GitHub comments, Jira descriptions, Confluence pages) and return it to the agent. Malicious content can contain prompt injection payloads. Standard mitigations apply (read-only memory stores, sandboxing), but **the MCP spec itself does not address this.**

---

## Relationship to Other S4 Topics

- **S4.1 — Memory Bank / Context Files:** CLAUDE.md declares which MCP servers are auto-triggered. Example: "use Context7 for .NET MAUI docs."
- **S4.2 — Context Engineering:** MCP is the selective-loading mechanism that replaces the older pattern of embedding all external knowledge into CLAUDE.md. Reduces startup context; improves just-in-time retrieval.
- **S3.x — Implementation Phase:** Agents call MCP tools during implementation to fetch documentation, check CI status, verify acceptance criteria against Jira, etc.

---

## Sources

- [Model Context Protocol Specification — modelcontextprotocol.io](https://modelcontextprotocol.io/specification/2025-11-25)
- [Introducing the Model Context Protocol — Anthropic (Nov 2024)](https://www.anthropic.com/news/model-context-protocol)
- [Architecture Overview — MCP Docs](https://modelcontextprotocol.io/docs/learn)
- [Context7 MCP Server — GitHub](https://github.com/upstash/context7)
- [Context7 MCP — Up-to-date Code Docs](https://abansinsi.github.io/context7/)
- [Getting Started with Atlassian Rovo MCP Server — Atlassian Support](https://support.atlassian.com/rovo/docs/getting-started-with-the-atlassian-remote-mcp-server)
- [Introducing Atlassian's Remote MCP Server — Atlassian Blog (May 2025)](https://www.atlassian.com/blog/announcements/remote-mcp-server)
- [Connecting Claude to Jira, GitHub, and Confluence: MCP Server Setup Guide — Ready Solutions (March 2026)](https://readysolutions.ai/blog/2026-03-29-connecting-claude-jira-github-confluence-mcp/)
- [JiraMCP — Give AI Agents Full Control Over Jira & Confluence](https://www.jiramcp.com/)
- [Jira & Confluence MCP Server — GitHub (thamaraiselvam)](https://github.com/thamaraiselvam/mcp-jira-confluence)
- [What Is MCP? A Practitioner's Guide to Model Context Protocol — Agentic Academy (Jan 2026)](https://agentic-academy.ai/posts/mcp-deep-dive/)
- [What Is MCP? Model Context Protocol Guide — PipeLab](https://pipelab.org/learn/what-is-mcp/)
- [MCP Setup — Copilot Collections](https://copilot-collections.tsh.io/docs/getting-started/mcp-setup)
