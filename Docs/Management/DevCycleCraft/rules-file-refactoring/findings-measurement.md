# Measurement Gate — Findings (GATE-A: subagent inheritance)

**Date:** 2026-07-04
**Method:** One throwaway `general-purpose` subagent, **zero tool uses**, instructed to introspect only its injected context (no Read/Grep/Bash). This isolates the *unconditional* context cost a fresh subagent pays before doing any work.

---

## Q1 — Do subagents inherit `.claude/rules/*.md`? **YES (decisive).**

All **7** rules files are injected **in full** into a fresh subagent's context, proven by verbatim quotes the probe reproduced without reading any file:

| File | Lines (on disk) | Present in subagent context | Proof quote (verbatim) |
|------|-----------------|-----------------------------|------------------------|
| workflow.md | 671 | FULL | "These rules are enforced by hooks. Violating them costs rework." |
| testing.md | 724 | FULL | "TDD and SDD are complementary, not competing disciplines." |
| code-principles.md | 44 | **Reduced (routing table)** | "This file is a routing table." |
| constraints-registry.md | 74 | FULL | "Discovered during implementation. Supersedes documented best practices…" |
| bug-tracking.md | 86 | FULL | "Bugs use a sequential ID: `BUG-001`, `BUG-002`" |
| component-change-governance.md | 73 | FULL | "Governance begins at the second consumer." |
| mediatr-patterns.md | 117 | FULL | "Command (mutates state, no return value)" |

**Conclusion:** The per-wave multiplication premise holds. Every subagent in a wave pays the full rules load. Reducing a rules file's body reduces load **in every subagent**, not just the top-level session — confirmed live: `code-principles.md` (already spike-reduced to 44 lines) arrived in its short routing-table form, while the un-refactored files arrived at full size.

---

## Q2 — The measured anchor (fixes the unmeasured-baseline inconsistency)

**A fresh subagent cold-starts at 60,492 tokens with 0 tool calls.**

This is the total injected context (system prompt + global `CLAUDE.md` + `RTK.md` + project `CLAUDE.md` ~600 lines + all 7 rules files + skill/tool descriptions). It supersedes the three disagreeing spec estimates:

| Source | Claimed rules load | Status |
|--------|--------------------|--------|
| requirements.md / BACKLOG | 17.2k | estimate, unmeasured |
| design.md (total) | 28.4k | estimate, unmeasured |
| design.md ("in memory") | 33.3k | estimate, unmeasured |
| **This measurement (whole cold-start, 0 tools)** | **60,492 total** | **measured** |

The rules-file portion of that 60.5k is ~1,789 lines ≈ **18–22k tokens** (consistent with the low estimate). The refactoring target (~2–3k unconditional routing tables) therefore recovers **~16–19k tokens per subagent**, which is the *sticky, bankable* saving — realized in every agent of every wave regardless of skill re-enablement.

---

## Q3 — GATE-B input (workflow.md / testing.md split economics)

Loading is confirmed unconditional: a subagent doing a trivial task (e.g. XAML cosmetic, `.sln` registration) still receives all 724 lines of `testing.md` and all 671 of `workflow.md` it never uses. So the 06–10 splits **do** save real per-agent tokens for the (common) case where an agent doesn't need the full body.

**Caveat carried forward to GATE-B / Task 11:** this justifies splitting the *rules files* (unconditional → routing table). It does **not** by itself justify re-enabling the generic `test-driven-development` / `code-review` superpowers — those bodies reload per-agent anyway and duplicate the project's customized rules (see Task 11 scope decision, narrowed to `brainstorming` + `writing-plans` per Helder 2026-07-04).

---

## Verdict

- **GATE-A: PASS** — subagents inherit rules in full; per-agent savings are real; baseline now measured (60,492 cold-start).
- **Phase 1 (02→04→03→05):** GO — every reduction sticks per-agent.
- **GATE-B (before 06–10):** leaning GO on evidence above; Helder confirms go/no-go with a post-Phase-1 re-measure.
