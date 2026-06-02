# S7.2 — AI Coding Assistants

**Status:** Researched  
**Predecessor(s) ID:** S7

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; comprehensive comparison of Claude Code, Cursor, and GitHub Copilot for SDD workflows |

---

## Overview

Three AI coding assistants have become the reference implementations for spec-driven development in 2025–2026: **Claude Code** (Anthropic's CLI agent), **Cursor** (VS Code-based IDE), and **GitHub Copilot** (Microsoft's widely-integrated assistant). Each brings a distinct architectural philosophy to the SDD paradigm: terminal-native agentic execution vs. visual IDE-centric editing vs. enterprise-integrated autocomplete. Selection depends on team maturity level, workflow model (autonomous task execution vs. interactive editing), and organizational constraints.

This section defines each assistant's SDD strengths, limitations, positioning, and real-world adoption patterns observed in 2025–2026.

---

## Claude Code — Terminal-First Autonomous Agentic Coding

Claude Code is Anthropic's official terminal-native AI coding agent, launched in May 2025. It operates as a proper CLI tool — reading files, writing files, running shell commands, and iterating autonomously until tasks complete. Claude Code is the AI assistant most explicitly designed for spec-driven workflows.

### SDD-Relevant Strengths

- **Spec consumption as first-class context:** Claude Code is built on the assumption of structured markdown context files. It reads `CLAUDE.md` at session start (from `~/.claude/CLAUDE.md` for global rules, then `./CLAUDE.md` for project rules, then `./.claude/rules/*.md` for modular rules). This layered configuration mirrors the spec-first paradigm.
- **CLAUDE.md and rules files:** Native support for project-level constitutional rules. Multiple rules files can be scoped to different domains (architecture rules, UI patterns, testing conventions). Auto-loaded on session start — no manual file referencing required.
- **Agentic multi-step execution:** Naturally executes task sequences from `tasks.md`. Can read an ordered checklist of implementation tasks and autonomously complete them one after another, reporting progress and building across files without human intervention between steps.
- **MCP integration (Model Context Protocol):** Native MCP client. Connects to any MCP server — Context7 for library documentation, GitHub for issue status, SQLite for database inspection, etc. MCP servers are agent-agnostic; an MCP server built for Claude Code works identically in Cursor or Copilot.
- **Subagent delegation:** Supports spawning child agents from within a task, enabling parallel execution patterns central to the MyVocaList workflow. A main agent can delegate disjoint tasks to multiple subagents and await completion.
- **Terminal-native workflow:** Works in any editor (Vim, Emacs, VS Code, JetBrains, or no editor at all). Composes with shell tools, git commands, and CI/CD pipelines naturally. Suitable for headless execution and automation.
- **Context window depth:** Supports up to 1M token context with Claude Opus 4.7, enabling reasoning across entire mid-sized codebases in a single session.
- **Model:** Built on Claude Sonnet family (Sonnet 4.5 default, or Opus 4.6+ via the Max plan). Anthropic ships new Claude models directly to Claude Code users first, before broader availability.

### SDD-Relevant Limitations

- **Autocomplete is not a strength:** Claude Code does not offer inline type-ahead suggestions as you type. It is not a replacement for editor-integrated tools like Copilot or Cursor's tab completion.
- **Cost structure:** Priced by token consumption via Anthropic API. No flat monthly rate. Typical usage ranges $50–150/month depending on session frequency and codebase size. Complex tasks that require reading many files can spike costs.
- **Manual editor integration required:** Requires bringing your own editor. No built-in IDE means no visual diff preview, file tree, or graphical debugging. Terminal-based tools like `git diff`, `cat`, and shell redirection are your interface.
- **Steeper learning curve:** Developers accustomed to IDE-based tools must learn shell commands, file paths, terminal redirection, and how to structure agentic prompts. For non-senior engineers, this is a barrier.
- **No built-in codebase indexing:** Context loading requires either explicit file pinning or broad directory reading. For very large repos (>50k files), context cost can become prohibitive without an MCP-based context engine.

### Best Fit

Claude Code excels for:
- Complex, multi-file refactors and architectural changes
- Senior engineers doing specification-driven feature work
- Projects following explicit SDD workflows with `requirements.md`, `design.md`, `tasks.md`
- Teams that treat the terminal as their primary development environment
- Autonomous task sequences where the agent runs for 30+ minutes without human steering
- Codebases where reasoning across 20+ files at once is necessary (large legacy migrations)

Claude Code is suboptimal for:
- Teams that prefer visual diffing before accepting code changes
- Developers accustomed to IDE-driven workflows
- Projects where autocomplete productivity is a major factor
- Tight-budget teams (relative to Cursor or Copilot's flat-rate pricing)

---

## Cursor — AI-First IDE with Composer Mode

Cursor is a VS Code fork that embeds AI at the center of the editing experience. It crossed 1 million paid users in 2024 and represents the most polished IDE-centric approach to AI-assisted coding.

### SDD-Relevant Strengths

- **Codebase indexing and semantic search:** Cursor maintains an indexed, searchable codebase. The `@codebase` command in chat allows semantic search across files — useful for finding patterns and understanding existing code structure before implementing against a spec.
- **Composer mode (multi-file agent):** Multi-file generation with human review at each step. Similar to Claude Code's autonomy but with a visual diff interface. Can execute terminal commands and iterate on errors. Good for refactors and larger features.
- **Rules files (`.cursorrules` and `.cursor/rules/`):** Project-level instruction files equivalent to CLAUDE.md. The legacy `.cursorrules` file is plain text/markdown in the root. The newer `.cursor/rules/` directory system supports `.mdc` files with YAML frontmatter for controlling when rules apply (glob patterns, always-on, agent-decided, or manual). Modular rules organization is more granular than CLAUDE.md's.
- **Spec Kit integration:** Spec Kit (GitHub's SDD toolkit) explicitly supports Cursor via slash commands. Teams using Spec Kit can run `/specify`, `/plan`, `/tasks` directly in Cursor's chat.
- **MCP support:** GA support for MCP servers as of 2025. Can connect to Context7, Tessl, GitHub, and other servers.
- **IDE familiarity:** Built on VS Code, so existing extensions, keybindings, and settings carry over. Minimal adoption friction for VS Code users.
- **Inline tab completion and autocomplete:** Cursor's tab completion rivals GitHub Copilot's for speed and accuracy. Useful for day-to-day productivity.
- **Model flexibility:** Supports Claude, OpenAI's GPT-4, Google's Gemini, and xAI's Grok. Not locked to a single model vendor.

### SDD-Relevant Limitations

- **Less agentic autonomy than Claude Code:** Composer mode can execute multi-step tasks but is less autonomous than Claude Code for long-running, self-healing workflows. Tends to require more human-in-the-loop direction.
- **Context management for large codebases:** Semantic search is smart, but for very large repos, Cursor can miss cross-file dependencies that a global-reasoning agent like Claude Code would catch. No explicit dependency graph context.
- **Session memory:** Limited to the current conversation. Cursor does not maintain project memory across sessions the way CLAUDE.md does.
- **Spec-first vs spec-anchored:** More interactive and responsive to in-the-moment edits than structured spec-first workflows. Better for rapid iteration than for implementing a predetermined design.
- **Privacy concerns:** Code indexing is done on Cursor's servers for the semantic search feature. Some teams have policies against sending code to third parties.
- **Pricing at scale:** $20/month Pro or $40/month Pro+ can add up for large teams. Fast model requests are capped; heavy users fall back to slower models.

### Best Fit

Cursor excels for:
- Day-to-day inline editing and rapid iteration
- Teams that want a unified IDE experience with AI baked in
- Medium-sized codebases (10k–50k files) where semantic indexing works well
- Developers who prioritize visual feedback and diff review before changes
- Projects that blend interactive editing with lighter-weight agentic tasks
- VS Code users who want minimal tool-switching

Cursor is suboptimal for:
- Pure SDD workflows requiring 100% spec-first discipline (Composer is too interactive)
- Very large codebases where reasoning depth matters more than context retrieval
- Terminal-first teams
- Projects requiring long autonomous execution without human steering

---

## GitHub Copilot — Integrated Enterprise Coding Assistant

GitHub Copilot is the most widely deployed AI coding assistant, with the broadest IDE coverage (VS Code, Visual Studio, JetBrains IDEs, Vim/Neovim, Emacs, and more). It began as an autocomplete tool and evolved into an agent by 2025.

### SDD-Relevant Strengths

- **Autocomplete primacy:** Fastest inline suggestion generation (~300ms). Tab completion and ghost text are best-in-class. For boilerplate, repetitive code, and test case generation, Copilot's autocomplete is the most productive tool available.
- **Agent mode (GA 2025):** Autonomous multi-step coding tasks. Can read files, propose edits, run commands, and iterate. Improving rapidly as of 2026.
- **Plan agent:** Built-in agent that produces a structured implementation plan before writing code. Spec-adjacent capability for teams that need visible planning before execution.
- **Spec Kit integration (primary):** Spec Kit's main integration target. Spec Kit supports Copilot across VS Code, JetBrains, and CLI. Teams using Spec Kit CLI get deep Copilot integration.
- **GitHub integration (native):** PR summaries, issue context, code review assistance, and issue-to-PR automation. Copilot Workspace operates on GitHub issues directly — given an issue, Workspace generates a full PR with tests, no local IDE required.
- **Enterprise maturity:** SOC 2, GDPR, IP indemnification, audit logs, content filtering. The most polished enterprise story of the three. Microsoft sales and compliance experience backed the product.
- **IDE breadth:** Works in every major IDE (VS Code, JetBrains, Visual Studio, Vim, Emacs, Neovim). Minimal switching cost for adopting teams.
- **Pricing accessibility:** $10/month Individual, $19/month Business, $39/month Enterprise. Cheapest of the three tools.
- **MCP support:** GA support as of 2025. Can connect to external tools and services.

### SDD-Relevant Limitations

- **Less agentic autonomy than Claude Code:** Agent mode exists but is less autonomous for multi-step tasks requiring complex reasoning and error recovery. Tends to get stuck on edge cases.
- **Shallow codebase reasoning:** Context loading is broad but not deep. Can miss structural relationships and architectural dependencies. Code generation quality on complex logic lags behind Claude-based tools.
- **No native CLAUDE.md or rule file equivalents:** Uses `.github/copilot-instructions.md` for project context. Less flexible than CLAUDE.md's layered configuration or Cursor's `.cursor/rules/` modular approach.
- **Autocomplete-first mental model:** Designed for incremental suggestions, not whole-task specification. Spec-first workflows are less natural than in Claude Code or Kiro.
- **Model lock-in:** Primary models are OpenAI's (GPT-4 family). Copilot users do not get immediate access to frontier Anthropic or Google models.
- **Spec-anchored complexity:** Maintaining specs and generated code in sync is less built-in than in Kiro or Tessl, which have versioning and override tracking.

### Best Fit

GitHub Copilot excels for:
- Enterprise teams where approval, audit, and compliance are mandatory
- Teams already invested in the GitHub ecosystem (GitHub Enterprise, GitHub Actions, GitHub Issues)
- Autocomplete-heavy workflows (rapid prototype development, boilerplate generation)
- Projects with low-complexity logic where code generation quality is less critical
- Teams wanting the lowest-friction adoption (works everywhere, minimal setup)
- Cost-sensitive organizations (cheapest option at scale)

GitHub Copilot is suboptimal for:
- Complex architectural refactors requiring deep codebase reasoning
- Spec-first workflows (interactive editing is more natural than spec-driven execution)
- Teams that value model agility (switching between Claude, GPT, Gemini as needed)
- Non-GitHub-centric teams (native GitHub integration advantage disappears)

---

## Comparative Analysis — Positioning by Use Case

The honest 2026 answer from practitioners: **no single tool is best for all situations**. Teams shipping the fastest typically use **two or three of these tools in concert**, selecting the right tool for the right task.

### Claude Code vs. Cursor vs. Copilot: Feature Comparison

| Capability | Claude Code | Cursor | GitHub Copilot |
|------------|------------|--------|----------------|
| **Form factor** | Terminal CLI | VS Code fork (IDE) | IDE extension (universal) |
| **Interface** | Shell commands | Visual diffs, chat | Inline suggestions + chat |
| **Context window** | Up to 1M tokens | ~200K tokens | ~64K typical |
| **Agentic autonomy** | Highest (native agent) | Moderate (Composer) | Growing (still improving) |
| **Autocomplete quality** | No autocomplete | Excellent | Best-in-class (fastest) |
| **Multi-file editing** | Autonomous (20+ files) | Manual/Composer | Manual or agent mode |
| **Terminal integration** | Native | Plugin only | Plugin only |
| **Rules files** | CLAUDE.md + `./.claude/rules/` | `.cursorrules`, `.cursor/rules/` | `copilot-instructions.md` |
| **MCP support** | Native client | GA support | GA support |
| **Model flexibility** | Claude only | Claude, GPT, Gemini, Grok | GPT, Claude (variable) |
| **IDE ecosystem** | None (terminal) | Full VS Code ecosystem | Works in 6+ IDEs |
| **Spec Kit integration** | Supported | Supported | Primary target |
| **Enterprise compliance** | Improving (BAA available) | SOC 2 | Best-in-class (mature) |
| **Pricing** | API-priced ($50–150/mo) | Flat ($20–40/mo) | Flat ($10–39/mo) |
| **Learning curve** | Steeper (terminal + shell) | Moderate (VS Code) | Low (familiar IDE) |

### Team Archetypes and Tool Selection

| Team Profile | Recommended Primary | Secondary | Rationale |
|--------------|-------------------|-----------|-----------|
| **Senior engineers, SDD-focused** | Claude Code | Cursor | Spec-first discipline, multi-file autonomy, architectural depth |
| **Fast-iterating startup** | Cursor | Copilot | Visual workflow, cost-efficient, rapid feedback |
| **Enterprise, compliance-driven** | Copilot | Cursor | Enterprise trust, audit trail, IDE breadth, cost predictability |
| **Open-source, mixed tooling** | Cursor + Claude Code | Copilot | Tool diversity, no vendor lock-in, spec-friendly |
| **Greenfield project (fast prototype)** | Cursor | Claude Code | Rapid iteration speed, then architectural depth when codebase stabilizes |
| **Large legacy refactor** | Claude Code | Cursor | Reasoning depth for cross-file changes, then IDE for detailed edits |

### Honest Productivity Numbers (2025–2026 Surveys)

Based on developer-reported productivity studies:

- **Routine coding (autocomplete):** GitHub Copilot fastest (~5x typing speed). Cursor competitive. Claude Code overhead for simple tasks.
- **Multi-file refactors (complex logic):** Claude Code wins by 2–5x on time-to-completion. Cursor requires multiple passes. Copilot often infeasible.
- **Greenfield prototypes:** Cursor and Claude Code comparable (2–3x faster). Copilot slower due to lack of agentic long-task support.
- **Debugging existing code:** Claude Code wins (reads logs, understands error context, runs tests). Cursor requires more manual iteration.
- **Cost per task (normalized):** Copilot ($10–39/mo flat) < Cursor ($20–40/mo) < Claude Code (API-priced, $2–8 per session). Claude Code's per-session cost is low; monthly cost depends on usage pattern.

### The Reference Stack (Observed in Early 2026)

High-performing teams converge on this pattern:

- **Daily editing:** Cursor or VS Code + Copilot for inline suggestions
- **Complex autonomous tasks:** Claude Code for the CLI
- **MCP context:** Context7 (library docs) + SQLite (database inspection) + GitHub (issue tracking)
- **Rules discipline:** CLAUDE.md for project context + `.cursorrules` or `.cursor/rules/` for Cursor users
- **Spec infrastructure:** `requirements.md`, `design.md`, `tasks.md` checked into version control
- **Optional:** Spec Kit or cc-sdd for structured spec generation and task management

This stack costs ~$70–170/month per developer but delivers measurable 2–4x productivity improvements on complex tasks — a strong ROI for senior engineers.

---

## SDD Workflow Integration Patterns

### Claude Code + Spec Files

Claude Code is purpose-built for this pattern:

```
project/
├── CLAUDE.md                    # Project rules, context, architecture
├── .claude/
│   ├── rules/
│   │   ├── testing.md          # Testing conventions
│   │   └── ui-patterns.md      # UI component patterns
│   └── settings.json           # Hook config, permissions
├── Docs/specs/
│   └── feature-name/
│       ├── requirements.md     # User stories, EARS notation
│       ├── design.md           # Architecture, sequence diagrams
│       └── tasks.md            # Checklist of implementation tasks
└── ... (source code)
```

Claude Code reads all context files at session start. A developer runs:
```bash
claude-code @Docs/specs/feature-name/tasks.md
```

Claude reads the entire spec context and autonomously executes tasks one by one, checking off `tasks.md` as it completes them.

### Cursor + Spec Kit / `.cursor/rules/`

Cursor integrates with Spec Kit:

```bash
/speckit.constitution          # Load project rules
/speckit.specify feature-name  # Auto-generate requirements
/speckit.plan feature-name     # Auto-generate design
/speckit.tasks feature-name    # Auto-generate tasks
/speckit.implement feature-name # Execute implementation in Composer
```

Alternatively, teams can populate `.cursor/rules/` manually with conventions and run Composer (`Cmd+I`) to handle multi-file edits with visual diffs.

### GitHub Copilot + GitHub Issues + Copilot Workspace

Enterprise pattern:

```
1. Create GitHub Issue with feature spec
2. Assign to Copilot Workspace
3. Copilot reads issue, generates implementation plan
4. Copilot writes code, runs tests, opens PR
5. Human reviews and merges PR
```

This is closest to spec-as-source for GitHub-native teams. Less customization than Claude Code or Cursor, but high integration with GitHub's native workflows.

---

## Integration with MyVocaList Workflow

The MyVocaList SDD workflow (documented in CLAUDE.md) is **optimized for Claude Code**:

- **CLAUDE.md as source of truth:** Project rules, architecture constraints, DI patterns, EF Core conventions.
- **Subagent delegation:** Specs spawn subagent tasks. Coordinator agent reads `tasks.md`, delegates disjoint work to 2–4 subagents in parallel, integrates results.
- **Spec-first discipline:** Developers read specs before coding. Specs are reviewed and approved before agents touch code.
- **Task-driven execution:** `tasks.md` is the source of sequential work. Agents check off tasks as they complete.
- **MCP integration:** Context7 for .NET MAUI / EF Core / DevExpress docs; SQLite MCP for live database inspection.

**Why Claude Code is the best fit for MyVocaList:**

1. Subagent delegation matches the workflow's parallel task pattern.
2. CLAUDE.md's layered rules (global + project + personal) map to project structure.
3. Spec consumption is a first-class feature — specs drive agent behavior.
4. Terminal integration enables headless CI/CD automation if needed.
5. Context window depth (1M tokens) supports reasoning across the entire codebase.

**When to use Cursor in MyVocaList workflow:**

- Visual diffing before accepting large changes.
- Rapid iteration on UI code (XAML, page structure) where inline editing is faster.
- Teams that prefer IDE-based workflows over terminal-based agents.

**When to use Copilot in MyVocaList workflow:**

- Autocomplete productivity for routine boilerplate (tests, DTO construction).
- Integrating with GitHub Issues if the team wants issue-driven task tracking.
- Enterprise compliance gates (audit logs, IP indemnification).

---

## Sources

### Tier 1 — Practitioner Surveys and Head-to-Head Comparisons (2026)
- [Cursor vs Claude Code vs GitHub Copilot 2026: Honest Comparison — TechVinta](https://techvinta.com/blog/cursor-vs-claude-code-vs-github-copilot-2026)
- [Cursor vs Claude Code vs Copilot 2026: Honest Comparison — vexp Dev](https://vexp.dev/blog/cursor-vs-claude-code-vs-copilot-2026)
- [Claude Code vs GitHub Copilot vs Cursor (2026) — StackNotice](https://stacknotice.com/blog/claude-code-vs-github-copilot-vs-cursor)
- [Claude Code vs Cursor vs GitHub Copilot: Honest Comparison (2026) — Artifilog](https://www.artifilog.com/posts/claude-code-vs-cursor-vs-copilot)
- [Claude vs Cursor vs Copilot: 2026 Comparison — Chudi Nnorukam](https://chudi.dev/blog/claude-code-vs-cursor-vs-copilot)
- [Claude Code vs Cursor vs GitHub Copilot: feature... — Vladimir Siedykh](https://vladimirsiedykh.com/blog/ai-coding-assistant-comparison-claude-code-github-copilot-cursor-feature-analysis-2025)

### Tier 2 — Configuration and Workflow Integration
- [The Complete Guide to AI Coding Rules: .cursorrules, CLAUDE.md & More — DevTk.AI](https://devtk.ai/en/blog/complete-guide-cursorrules/)
- [CLAUDE.md Best Practices: Write Files That Actually Work — Heyuan110](https://www.heyuan110.com/posts/ai/2026-03-05-claude-code-claudemd-best-practices/)
- [CLAUDE.md vs .cursorrules: Complete Comparison Guide — Keeborg Blog](https://www.keeborg.com/blog/claude-md-vs-cursorrules)
- [Cursor Rules Guide - AI Configuration — design.dev](https://design.dev/guides/cursor-rules/)
- [Project Rules — Developer Toolkit](https://developertoolkit.ai/en/cursor-ide/quick-start/project-rules/)

### Tier 2 — SDD Tooling and Spec Kit Integration
- [gotalab/cc-sdd — GitHub](https://github.com/gotalab/claude-code-spec)
- [darcyg/cc-sdd-kiro — GitHub](https://github.com/codeaudit/cc-sdd-kiro)
