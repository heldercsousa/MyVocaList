# S4.1 — Memory Bank / Context Files

**Status:** Researched
**Predecessor(s) ID:** S4

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Research and content completed |

---

## Overview

Memory Bank and Context Files are the persistent layer that solve the fundamental problem in AI-assisted development: an LLM has no inherent memory between sessions. Without deliberate engineering, the knowledge, decisions, constraints, and patterns that shaped one session are invisible to the next — forcing the agent to re-derive context from code or hallucinate conventions that contradict earlier decisions.

This section covers the mechanisms that practitioners use to persist project context across session boundaries: explicit instruction files (CLAUDE.md, AGENTS.md), automated memory systems (auto memory, agent memory), the Memory Bank methodology, and governance patterns that keep context lean and authoritative.

---

## Scope and Definitions

### Context Files (Explicit, Manually Maintained)

| File | Format | Scope | Who Writes | Persistence |
|------|--------|-------|-----------|-------------|
| **CLAUDE.md** | Plain Markdown | Project (Claude Code native) | Developer | Version-controlled |
| **AGENTS.md** | Plain Markdown | Cross-tool standard | Developer | Version-controlled |
| **Rules files** | Markdown (path-scoped) | Directory-specific | Developer | Version-controlled |
| **.cursorrules** | Plain Markdown | Cursor-specific | Developer | Version-controlled |
| **copilot-instructions.md** | Plain Markdown | GitHub Copilot-specific | Developer | Version-controlled |

### Automatic Memory Systems

| System | Format | Location | Who Writes | Loaded at startup |
|--------|--------|----------|-----------|------------------|
| **Auto memory (Claude Code)** | Markdown | `~/.claude/projects/<project>/memory/` | Claude automatically | First 200 lines of MEMORY.md |
| **Agent memory (Claude Code v2.1.33+)** | Markdown | `~/.claude/agent-memory/<agent>/` or `.claude/agent-memory/<agent>/` | Agent automatically | First 200 lines of MEMORY.md per agent |
| **Copilot Memory** | Proprietary | Microsoft cloud | Model learns automatically | Implicit |

---

## CLAUDE.md — The Canonical Context File

### What It Is

CLAUDE.md is a plain-text Markdown file that Claude Code automatically loads at the start of every session. In Anthropic's official documentation, CLAUDE.md is described as "the place for instructions and rules that should persist across sessions."

### Location and Precedence

Claude Code reads CLAUDE.md files by walking up the directory tree from the current working directory:

```
~/.claude/CLAUDE.md                    ← Global (applies to ALL projects)
/path/to/MyVocaList/CLAUDE.md          ← Project root
/path/to/MyVocaList/src/CLAUDE.md      ← Subdirectory (nested)
/path/to/MyVocaList/.claude/CLAUDE.md  ← Project-level alternative location
```

All discovered files are **concatenated** into context rather than overriding each other. Content is ordered from the filesystem root down to the working directory, so instructions closer to where the agent is launched appear last and take priority. At each directory level, `CLAUDE.local.md` (gitignored) is appended after `CLAUDE.md`, allowing personal notes to override shared project instructions.

### Typical Content

A project CLAUDE.md typically contains:

- **Build and test commands** — `dotnet build`, `dotnet test`, deployment procedures
- **Coding standards and conventions** — naming, style, patterns specific to the team
- **Architectural decisions** — why certain technologies were chosen, non-negotiables, module boundaries
- **Workflow rules** — commit discipline, branch strategy, code review expectations
- **Library-specific patterns** — for frameworks or tools the project uses heavily (e.g., Entity Framework, DevExpress)
- **Domain-specific rules** — business logic constraints, validation rules, security requirements

### Context Window Impact

CLAUDE.md is loaded into the context window at the start of every session, consuming tokens. Anthropic's guidance (as of Feb 2026) recommends keeping CLAUDE.md under 200–300 lines and no larger than ~50–100 KB. Beyond that threshold, context is consumed by instructions before the agent has space for actual working memory. The "lost in the middle" phenomenon — where critical instructions are buried deep in a large file — is documented as a failure mode in long sessions.

**Token budget:** Estimate ~0.25 tokens per character. A 300-line CLAUDE.md (~8–10 KB) costs ~2,000–2,500 tokens at startup. A 1,000-line file costs ~2,500–3,500 tokens — potentially 25% of a subagent's working context.

### Best Practices

1. **Keep it concise.** Focus on information the agent cannot infer from the code: hidden conventions, architectural decisions, constraints.
2. **Prioritize.** Place the most critical rules first (they're more likely to be preserved in context).
3. **Link to deeper docs.** Use references like "See `.claude/rules/database-indexing.md` for EF Core patterns" rather than inlining all details.
4. **Use a routing table.** A CLAUDE.md with a quick-reference section helps agents locate relevant rules.
5. **Version it.** Track CLAUDE.md in git and review changes in pull requests as you would code.

---

## AGENTS.md — The Cross-Tool Standard

### Background

AGENTS.md emerged as a convention for sharing project instructions across multiple AI coding agents. In December 2025, AGENTS.md was donated to the Agentic AI Foundation (AAIF) — a directed fund under the Linux Foundation — positioning it as a cross-vendor standard.

The motivation: before AGENTS.md standardization, teams maintained a patchwork of tool-specific files (CLAUDE.md, .cursorrules, copilot-instructions.md, etc.), often drifting apart. AGENTS.md offers a single, canonical source of truth that every major AI coding agent can read.

### Format and Interoperability

AGENTS.md is plain Markdown, stored at the repository root. Supported by:

- **Claude Code** (via CLAUDE.md import)
- **Cursor** (native .cursor/rules/ system, with AGENTS.md compatibility)
- **GitHub Copilot** (via copilot-instructions.md or Spec Kit bridge)
- **Windsurf** (partial support)
- **Cline, Aider, OpenClaw** (via native AGENTS.md reading)

For multi-tool teams, the symlink pattern ensures consistency:

```bash
# CLAUDE.md is a symlink to AGENTS.md — they are the same file
ln -s AGENTS.md CLAUDE.md
```

### Typical Structure

An AGENTS.md file usually contains:

```markdown
# AGENTS.md — MyVocaList

## Stack & Build
- .NET MAUI 10, C# 13, EF Core 10, SQLite
- `dotnet build`, `dotnet test`, `dotnet run`

## Coding Standards
- All English (no translated code or comments)
- Use modern C# (13+) features: records, pattern matching, primary constructors
- Async/await mandatory for I/O; no `.Result` or `.Wait()`

## Architecture
- Domain / Contracts / Infra / Services / MAUI (UI)
- Business logic in Services only
- Repository interfaces in Domain, implementations in Infra

## Key Rules
- No `DisplayAlert` or `DisplayActionSheet` — use `dx:BottomSheet`
- DevExpress first; stock MAUI only as fallback
- SafeAreaEdges="Container" on all ContentPages
- See `.claude/rules/` for detailed patterns per area

## When to escalate
- Database schema changes
- Breaking API changes
- Architectural decisions
```

---

## Rules Files — Path-Scoped Context

### What They Are

Rules files are targeted instruction files stored in `.claude/rules/` (or equivalent directories) that activate conditionally when the agent works on files matching specified patterns.

### Structure

```
.claude/rules/
├── database-indexing.md       ← Activates for EF Core / Infra files
├── dialogs-validation.md      ← Activates for UI / dialog pages
├── devexpress-patterns.md     ← Activates for DevExpress components
├── ux-patterns.md             ← Activates for XAML / UX files
└── code-principles.md         ← Activates for all code files
```

### Benefits

- **Lean startup context:** Rules for database indexing don't consume tokens during a UI-only session.
- **Specificity:** When an agent edits an EF Core file, database-specific guidance loads automatically.
- **Maintenance:** Changes to a specific pattern stay in one place, not scattered across CLAUDE.md.
- **Reuse:** Rules can be imported across projects (e.g., `.NET MAUI best practices`).

### Example: MyVocaList

MyVocaList uses `.claude/rules/` extensively:

- `code-principles.md` — nullable reference types, exception handling, DI patterns
- `testing.md` — unit/integration test structure, TDD workflow
- `mediatr-patterns.md` — MediatR command/query/event templates (reference for future use)
- `workflow.md` — spec-first discipline, subagent delegation, commit gates

---

## Memory Bank Methodology

### Definition

The Memory Bank is a structured set of files organized in the repository (typically in `.claude/memory-bank/` or `.memory/`) that together answer: What is this project? What has been done? What comes next? What patterns matter?

This is a community-evolved pattern, not a tool-native feature. It extends the Claude Code memory primitives (CLAUDE.md + auto memory) into a comprehensive knowledge management system.

### Typical Structure

```
.claude/memory-bank/
├── MEMORY.md                # Index (first 200 lines loaded at session start)
├── projectbrief.md          # Core project overview: what, why, who
├── productContext.md        # Product requirements, user journeys, goals
├── activeContext.md         # Current work focus, recent decisions, next steps
├── systemPatterns.md        # Architecture, design patterns, component relationships
├── techContext.md           # Stack details, setup, dependencies, constraints
├── progress.md              # What works, what's left, known issues
├── decisions/
│   ├── architecture.md      # Architecture decision records (ADRs)
│   ├── naming-conventions.md
│   └── technology-choices.md
├── patterns/
│   ├── service-extension.md
│   ├── error-handling.md
│   └── testing-strategy.md
├── troubleshooting/
│   ├── common-errors.md
│   ├── debugging-checklist.md
│   └── known-gotchas.md
└── archive/                 # Historical snapshots (read-only, dated)
```

### When to Update

- **End of session:** Major features, pattern discoveries, architectural decisions
- **Milestone completion:** After a feature is merged or a phase completes
- **Incident resolution:** Document the fix and add to troubleshooting
- **Dependency updates:** Record version bumps and migration notes

### Just-In-Time (JIT) Retrieval

The Memory Bank is never loaded in full. Instead, use on-demand retrieval:

- **At session start:** Load only MEMORY.md (first 200 lines, ~25 KB)
- **During session:** Query for specific files as needed (`/memory` command in Claude Code, or agent reads them explicitly)
- **Across sessions:** Agents can archive old snapshots and promote frequently-needed content into MEMORY.md

---

## Automatic Memory Systems

### Auto Memory (Claude Code Native)

Introduced as a built-in feature of Claude Code, auto memory lets Claude automatically save patterns and learnings without developer action:

- **Location:** `~/.claude/projects/<project>/memory/`
- **What gets saved:** Build commands, debugging insights, architecture notes, code style preferences, workflow habits
- **When it's saved:** Claude decides what's worth remembering; not every session triggers a save
- **How it loads:** First 200 lines (or 25 KB) of MEMORY.md are loaded at session start; deeper topic files are available on-demand via the `/memory` command
- **Scope:** Per-project, per-user (not team-shared)

**Structure:**
```
~/.claude/projects/MyVocaList/memory/
├── MEMORY.md                  # Index (auto-loaded)
├── debugging-patterns.md      # Topic files (on-demand)
├── performance-notes.md
└── testing-insights.md
```

### Agent Memory (Claude Code v2.1.33+)

Introduced in February 2026, agent memory gives each named subagent its own persistent markdown-based knowledge store. This enables specialized agents (e.g., a code-reviewer) to accumulate patterns over time without polluting the main session context.

**Memory Scopes:**

| Scope | Location | Shared | Use Case |
|-------|----------|--------|----------|
| User | `~/.claude/agent-memory/<agent>/` | Cross-project | Agent-specific knowledge across all projects |
| Project | `.claude/agent-memory/<agent>/` | Team (version-controlled) | Team-shared agent patterns for this project |
| Local | `.claude/agent-memory-local/<agent>/` | Personal (git-ignored) | Personal notes for this project only |

**How It Works:**

1. When an agent is invoked with a `memory` frontmatter field, its agent-scoped MEMORY.md is loaded
2. First 200 lines of the agent's MEMORY.md are injected into its system prompt
3. The agent reads/writes to its memory directory freely (Read, Write, Edit auto-enabled)
4. If MEMORY.md exceeds 200 lines, the agent moves details into topic-specific files
5. At agent return, the calling session only receives the agent's output, not its internal memory

---

## Cross-Session Context Loss — Why CLAUDE.md Isn't Enough

### The Residual Problem

Even with CLAUDE.md and auto memory in place, three categories of context loss remain:

**1. Just-Learned Constraints**
The agent discovers a new constraint during implementation — e.g., "EF Core migrations must never drop columns in production." Without a session-end update ritual, this constraint is invisible to the next session and the same mistake recurs.

**2. Architectural Decisions Made in Conversation**
The developer and agent discuss a design choice in chat and agree on it. But the decision lives only in conversation history, which is lost at session end. The next session, the agent re-derives or contradicts the decision.

**3. Spec Staleness**
Specifications written before implementation diverge from reality within hours. New constraints surface, dependencies shift, and the code-spec gap widens. CLAUDE.md cannot capture "the spec needs updating" unless a human manually edits it.

### Mitigation Patterns

1. **Session-End Spec Updates**
At the end of each session, capture:
   - Decisions finalized
   - Constraints discovered
   - Open questions remaining
   Commit to version control (e.g., update `Docs/specs/venues/design.md`).

2. **Constraints Registry**
Maintain a file listing discovered limits:
   ```
   - EF Core migrations: never drop columns in production
   - DevExpress: CollectionView Reset event triggers full re-render
   - SQLite: PRAGMA synchronous = NORMAL required for performance
   ```

3. **Spec-as-Living Document**
Tools like Intent and Kiro auto-update specs as agents complete work, reflecting what was actually built. Manual CLAUDE.md alone cannot solve this problem at scale.

---

## Cross-Tool Considerations

### Claude Code vs. Cursor vs. Copilot

| Aspect | Claude Code | Cursor | GitHub Copilot |
|--------|-----------|--------|-----------------|
| Primary config file | CLAUDE.md | .cursor/rules/ | copilot-instructions.md |
| Format | Markdown | Markdown + YAML frontmatter | Markdown |
| Path-scoped rules | Via `.claude/rules/` | Native in .cursor/rules/ | Via .instructions.md in directories |
| Auto memory | Built-in (MEMORY.md) | Unknown | Copilot Memory (implicit) |
| AGENTS.md compat | Via import in CLAUDE.md | Documented support | Yes (Spec Kit bridge) |
| Subagent memory | Yes (v2.1.33+) | Unknown | Unknown |

### Symlink Strategy for Multi-Tool Teams

If a team uses Claude Code, Cursor, and Copilot:

```bash
# Create a canonical AGENTS.md
echo "# AGENTS.md — MyVocaList\n..." > AGENTS.md

# Symlink other tools to it
ln -s AGENTS.md CLAUDE.md
ln -s AGENTS.md .instructions.md
ln -s AGENTS.md .cursor/rules/project-conventions.md
```

---

## Governance and Permissions

### Constitutional Constraints

To prevent agents from altering critical context, use `.claude/settings.json`:

```json
{
  "permissions": {
    "deny": {
      "Edit": [".claude/rules/**", "CLAUDE.md"],
      "Delete": [".claude/memory-bank/**"]
    }
  }
}
```

This prevents an agent from accidentally (or via prompt injection) modifying the project's instruction layer.

### Approval Gates for Memory Updates

For team projects, consider a workflow where memory bank updates are reviewed:

1. Agent completes task and proposes memory updates
2. Developer reviews the updates
3. Developer commits and pushes (agent cannot push)

This is the pattern MyVocaList enforces: subagents update memory via `/memory` command; main agent (developer) approves and commits.

---

## Relationship to Other S4 Topics

- **S4 — Context & Memory (overview):** The three-layer response (S4.1 memory files, S4.2 context engineering, S4.3 external integrations)
- **S4.1.1 — Cross-Session Context Loss:** Why no framework fully solves persistent architectural context; residual gaps after CLAUDE.md + auto memory + Memory Bank
- **S4.2 — Context Engineering:** Compress/write/select/isolate strategies; CLAUDE.md sizing guidelines; subagent return protocols
- **S4.3 — External Integrations:** MCP servers as just-in-time knowledge sources; Context7 for library docs

---

## Sources

- [How Claude remembers your project — Claude Code Docs (Anthropic)](https://docs.anthropic.com/en/docs/claude-code/memory)
- [Claude Code Memory: CLAUDE.md, Auto Memory & Context Management (2026 Guide) — Skills Playground](https://skillsplayground.com/guides/claude-code-memory/)
- [Claude Memory Bank — Persistent Context Management for Claude Code (Nam Seob Seo, Mar 2026)](https://nsclass.github.io/2026/03/15/claude-memory-bank)
- [Agent Memory — Claude Code Best Practice (Mintlify)](https://www.mintlify.com/shanraisshan/claude-code-best-practice/reports/agent-memory)
- [How to Build Your AGENTS.md (2026): The Context File That Makes AI Agents Reliable — Augment Code](https://www.augmentcode.com/guides/how-to-build-agents-md)
- [AGENT-ZERO: Operational Framework for AI-Assisted Software Development (msitarzewski, GitHub)](https://github.com/msitarzewski/AGENT-ZERO)
- [Memory Bank — Cline Documentation](https://docs.cline.net.cn/features/memory-bank)
- [The Session-End Spec Update That Keeps AI Agents on Track Across Days (Augment Code, Apr 2026)](https://www.augmentcode.com/guides/session-end-spec-update-ai-agents)
- [Specification-Driven Development: Build with a Persistent Spec (AgentPatterns.ai)](https://agentpatterns.ai/workflows/spec-driven-development/)
- [Agent Context System — Persistent Memory for AI Coding Agents (Agent Context System)](https://agents.mainbranch.dev/)
- [Structured Context Specification — SCS](https://structuredcontext.dev/)
- [ctx: Context as Deterministic State (ActiveMemory, Mar 2026)](https://pkg.go.dev/github.com/ActiveMemory/ctx)
