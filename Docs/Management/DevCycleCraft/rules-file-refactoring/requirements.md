# Rules File Refactoring — Requirements

## Problem Statement

The `.claude/rules/*.md` files load unconditionally in every session **and in every subagent** (proven — GATE-A, `findings-measurement.md`), consuming ~**18–22k tokens** of the context budget (7 files, 1,789 lines). This duplicates procedures already documented in superpowers skills (`brainstorming`, `writing-plans`, `test-driven-development`, `code-review`) — which are currently disabled to save context.

> **Baseline note (2026-07-04):** earlier drafts cited 17.2k / 28.4k / 33.3k — all unmeasured estimates. Measurement supersedes them: a fresh subagent cold-starts at **60,492 tokens with 0 tool calls** (full system prompt + CLAUDE.md + RTK + all rules); the rules portion is ~18–22k.

**Impact:** In a multi-agent wave (up to 5 concurrent subagents per workflow.md Rule 2), the rules load multiplies — each subagent inherits the full set, so ~18–22k × 5 ≈ 90–110k tokens spent on rules content before any agent does work. Reducing the rules body to routing tables recovers ~16–19k **per subagent**, sticky regardless of skill re-enablement.

## Goals

1. **Reduce unconditional load** — condense rules files from 17.2k to ~2–3k tokens by extracting detailed patterns to `.claude/library/` files
2. **Re-enable superpowers skills** — activate `brainstorming`, `writing-plans`, `test-driven-development`, `code-review` plugins on-demand instead of always-loaded
3. **Recover 70k+ tokens per multi-agent wave** — 5 agents × 14k on-demand recovery = 70k context saved per parallel wave
4. **Maintain zero behavioral change** — agents follow identical workflows; only the documentation container changes

## Acceptance Criteria

### AC-1: Spike validates routing-table pattern
- Given: `code-principles.md` (1.2k tokens, all reference content)
- When: Extract 2 sections to `~/library/`, rewrite as 1-page routing table
- Then: (1) Skill fires correctly on invocation, (2) no content loss, (3) no workflow change needed, findings recorded

### AC-2: Incremental refactoring plan is complete
- Given: 12 nested tasks identified (spike + 11 refactors)
- When: All tasks are written to `tasks.md` with dependency order + time estimates
- Then: Task-log shows successful handoff to implementation wave(s)

### AC-3: Context efficiency measured before/after
- Given: Superpowers skills re-enabled after final refactor
- When: `/context fresh` run in a clean session
- Then: (1) Memory files show <20k tokens for rules (down from 33.3k), (2) Skills load descriptions only (~100 tok), (3) First invocation of a skill loads its full body on-demand, (4) `/context` shows net ~14k token recovery per-agent per-session

### AC-4: No rule or workflow change required by agents
- Given: Refactored rules + enabled superpowers
- When: Agents run a complete workflow (spec → plan → implement → review)
- Then: All agent actions remain identical; only where knowledge is sourced changes (from always-loaded file → on-demand skill)

## Out of Scope

- **Changing rule content** — rules are correct; only documentation container changes
- **Changing skill content** — superpowers skills already have the authoritative procedures
- **Disabling non-superpowers skills** (Context7, SQLite MCP, etc.) — only rules files are refactored
- **MCP server consolidation** — separate initiative

## Validation Rules

1. **No silent content loss** — every rule line in a refactored file must either: (a) move to a library file + skill pointer, or (b) be moved to library, or (c) documented as intentionally removed with rationale
2. **Routing tables must be minimal** — rules files after refactor should fit on 1–2 pages; if > 2 pages, the refactor is too shallow
3. **Skill invocation must work** — every routing table entry must be testable by invoking the linked skill and confirming it fires
4. **CLAUDE.md pointer update** — after final refactor, CLAUDE.md § Skill & MCP Lookup table is updated with enabled superpowers + removed wordy paragraphs

## Success Metrics

> **Framing correction (2026-07-04, F3):** the *primary* KPI is the **sticky per-agent unconditional reduction** — tokens removed from every subagent's cold-start whether or not any skill is later invoked. The "on-demand recovery per skill" figure is a *ceiling*, not an expected value (TDD/code-review skill bodies reload per-agent anyway — see `tasks.md` Task 11 honesty note); it is demoted to a secondary, informational metric. Baseline is now **measured**, not estimated: a fresh subagent cold-starts at **60,492 tokens with 0 tool calls** (`findings-measurement.md`), of which the 7 rules files are ~18–22k.

| Metric | Priority | Target | Measurement |
|--------|----------|--------|-------------|
| **Sticky per-agent unconditional reduction** | **PRIMARY** | rules load ~18–22k → ~2–3k (≈16–19k recovered per subagent) | GATE-A/B probe: subagent cold-start token delta, 0 tools |
| Unconditional rules token load (absolute) | Primary | ~2–3k | `/context` fresh session, sum of .claude/rules/*.md size |
| Rules file line count (all) | Primary | <300 total | Sum of .claude/rules/*.md line counts after refactor |
| On-demand recovery per skill invoked | Secondary (ceiling, not expected) | ≤ body size | before/after `/context`; report *net* across a real wave, not gross ceiling |
| Enabled superpowers skill coverage | Secondary | brainstorming + writing-plans load descriptions only at start, bodies on-demand | invoke each of the 2 narrowed skills once |
