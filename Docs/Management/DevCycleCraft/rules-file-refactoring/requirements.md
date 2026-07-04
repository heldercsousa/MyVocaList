# Rules File Refactoring — Requirements

## Problem Statement

The `.claude/rules/*.md` files load unconditionally in every session, consuming **17.2k tokens** of the context budget. This duplicates procedures already documented in superpowers skills (`brainstorming`, `writing-plans`, `test-driven-development`, `code-review`) — which are currently disabled to save context.

**Impact:** 17.2k tokens / 200k budget = 8.6% of context used before the user even starts work. In a multi-agent wave (5 concurrent agents), this multiplies: 5 agents × 17.2k = 86k tokens burned on stale skill documentation alone.

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

| Metric | Target | Measurement |
|--------|--------|-------------|
| Unconditional rules token load | ~2–3k | `/context` fresh session, sum of .claude/rules/*.md size |
| Per-agent on-demand recovery | 14k | Typical subagent session comparing before/after `/context` |
| Multi-agent wave recovery | 70k | 5 agents × 14k |
| Rules file line count (all) | <300 total | Sum of .claude/rules/*.md line counts after refactor |
| Superpowers skill invocation coverage | 100% | Every rules routing table entry has a corresponding enabled skill; invoke each skill once |
