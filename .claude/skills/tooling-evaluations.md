# Tooling Evaluations & Migrations

Reference material for evaluating new tools, frameworks, and MCP servers. This skill is invoked when you're considering adoption of Tessl Registry, Spec Kit, Cursor, sdd-mcp, or other integrations. **Moved from CLAUDE.md to reduce per-session context overhead** (these sections are rarely needed except during evaluation phases).

## Tessl Registry (Evaluation)

Tessl Spec Registry provides version-matched library skills for DevExpress, EF Core, and MediatR — complementing Context7 with higher-level usage patterns and project skill packages.

**Evaluate for adoption when:**
- Context7 returns incomplete or hallucinated DevExpress MAUI API results
- Project reaches Spec-Anchored maturity with spec-to-test linkage
- Internal skills (DX patterns, domain rules) are published for multi-agent reuse

**To trial:** `tessl install devexpress-maui` and compare generated code quality against Context7-only sessions.

## sdd-mcp (Evaluation)

`yi-john-huang/sdd-mcp` (v3.3, March 2026) is an MCP server that exposes SDD workflow primitives as callable tools: spec state queries, agent skills, steering rules, task hooks.

**Evaluate when:**
- The main agent repeatedly needs to query "which tasks are complete?" across sessions
- Spec-anchored maturity requires programmatic spec-to-test linkage
- The current prompt-driven task-log workflow shows reproducibility gaps

## Complementary Tooling — Cursor (optional, for human review sessions)

**When to use:** Visual diffing of large XAML changes before accepting; rapid UI iteration.

**Setup:** Install `.cursor/rules/` mirroring the key rules from CLAUDE.md and `.claude/rules/`.

**Critical constraints:**
- **Do NOT** use Cursor for autonomous task execution — Claude Code's subagent model is the only delegation mechanism
- **Do NOT** create `.cursorrules` with content that contradicts CLAUDE.md rules

## Migration Path (if Spec Kit adoption becomes warranted)

The current spec format (`requirements.md`, `design.md`, `tasks.md` in `Docs/Management/`) maps directly to GitHub Spec Kit's artifact set. **Adopting Spec Kit would require:**

1. `speckit init` — creates `.specify/` directory with templates
2. Moving specs from `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/` to `.specify/specs/[feature]/`
3. Merging `CLAUDE.md` + `rules/*.md` into `.specify/constitution.md`
4. Replacing `workflow.md` Rule 1-4 trigger patterns with `/specify`, `/plan`, `/tasks` slash commands

**Estimated migration effort:** 1–2 weeks (per research). Claude Code is explicitly supported by Spec Kit — no agent switch required.
