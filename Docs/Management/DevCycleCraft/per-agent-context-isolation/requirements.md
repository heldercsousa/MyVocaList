# Per-Agent Context Isolation — Requirements (MVP)

**BACKLOG row:** 2026-06-27 "Per-Agent MCP/Skill Context Isolation" — PRIORITIZED ASAP (Helder, 2026-07-09).
**Scope decision (Helder, 2026-07-09):** Slim config-only MVP. Research phase closed this session — see `context-baseline.md` (measured baseline) and `design.md § Research findings` (platform mechanism answers). This gates restarting business implementation (BUG-027 next); keep tight.

## Problem statement

Every subagent dispatched in a wave cold-starts with context it does not need. Measured 2026-07-09: `general-purpose` 38,127 tokens with zero tool calls; a wave of 4 implementors pays this 4×. The 2026-07-09 platform state (MCP schema deferral) and the rules-file refactoring already banked the earlier levers; the remaining *reachable* waste is tool schemas, skill preloads, and skills-listing exposure controlled by agent-brief frontmatter.

## User stories

- **US-1:** As the orchestrator dispatching 4-implementor waves, I want each implementor to cold-start without tool schemas it never uses, so each wave costs measurably fewer tokens.
- **US-2:** As Helder auditing token spend, I want the unreachable context floor (CLAUDE.md/rules inheritance, harness text) documented with evidence, so future "context is too big" discussions start from facts, not re-research.
- **US-3:** As a reviewer subagent (spec/plan/verifier), I want no implementation-skill preload, so my report-only context stays minimal.

## Acceptance criteria

- **REQ-CTXISO-01:** `implementor` agent brief carries `disallowedTools: Agent, Artifact, NotebookEdit, PowerShell` and retains its `skills: myvocalist-coding` preload. A post-change 0-tool probe of `implementor` cold-starts **measurably below the 38,127-token general-purpose baseline** (pass: ≤35,127; the probe result is recorded in `context-baseline.md`). *Comparator caveat: implementor was never probed pre-change — the general-purpose figure is the comparator; implementor's own definition + skill preload sit on top of it, so ≤35,127 confirms a net win (see `design.md § Verification`).*
- **REQ-CTXISO-02:** `orchestrator` agent brief carries `disallowedTools: Artifact, NotebookEdit`, retains Agent/Edit/Write/Bash access, and its `skills:` preload is removed.
- **REQ-CTXISO-03:** `spec-reviewer`, `plan-reviewer`, `verifier` briefs have the `skills:` preload removed; their existing `tools:` allowlists are unchanged.
- **REQ-CTXISO-04:** Each of the 5 agent briefs' documented protocol (body text) references no tool that its frontmatter now denies (checked line-by-line; risk table in `design.md`).
- **REQ-CTXISO-05:** `design.md` records the non-levers with doc citations: no per-agent CLAUDE.md/rules scoping mechanism exists; MCP schemas already deferred; memory not injected into subagents; skills-listing block not per-agent controllable.
- **REQ-CTXISO-06:** After Task 1, a dispatch of the `verifier` agent (verifying Task 1's commit) completes successfully with the reduced frontmatter — live validation that report-only agents still function without the `skills:` preload. (This spec's own spec-review runs pre-change and does NOT count.)
- **REQ-CTXISO-07:** BACKLOG row 174 updated: research items (a)–(d) answered, candidate worktree-overlay design marked obsolete, status advanced.

## Out of scope

- Global skills-listing reduction via `disable-model-invocation:` audit (40 skills, touches user-level/plugin skills — separate opportunistic task if ever).
- Worktree-per-agent `.mcp.json` overlay (BACKLOG row's original candidate design — obsolete: MCP schemas are deferred platform-side and rules inheritance has no scoping mechanism regardless of worktree).
- `mcpServers:` per-agent frontmatter scoping (<0.5k value at current deferral behavior — documented non-lever).
- Any hook machinery, load/unload patterns, or settings.json changes.
- **Path-scoped rules evaluation** (`paths:` frontmatter on `.claude/rules/*.md` — discovered post-spec-review, see `design.md § Research findings` addendum): a real conditional-loading lever, but orthogonal to per-agent scoping and requiring its own probe (does it apply at subagent cold-start?) + a never-miss/HARD-RULE partition of each rules file. Follow-up candidate, not MVP.
- "Deactivate on completion" from the original row — meaningless for subagents (context dies with the agent).

## Validation rules

- Frontmatter keys used must be officially documented (`tools`, `disallowedTools`, `skills` — code.claude.com/docs/en/sub-agents.md).
- Verification is token-thrifty (GATE-B precedent): exactly ONE post-change throwaway probe (implementor), arithmetic + live-dispatch validation for the rest.
