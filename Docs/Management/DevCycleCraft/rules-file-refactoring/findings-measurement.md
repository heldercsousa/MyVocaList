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

---

## GATE-B — decision (2026-07-05): **GO**, decided analytically (no subagent)

**Belief corrected (per Helder "caution to your early beliefs about needed actions"):** the earlier plan said GATE-B needs a throwaway-subagent re-measure (~60k cold-start). It does **not**, and spending ~60k of a near-cap weekly budget on it would be wasteful. Reasons:
- GATE-A's probe existed to prove *subagents inherit the full rules* — already PROVEN (PASS) and immutable. Re-running it only reconfirms inheritance.
- GATE-B Q1 (post-Phase-1 rules total) is **deterministic** — derivable from known file sizes; and a fresh `/context` in a new session validates it **for free** (Helder plans to check anyway).
- GATE-B Q2 (are the core-file bodies worth splitting?) is **already answered** by `skill-overlap-findings.md` + GATE-A Q3.

### Q1 — post-Phase-1 unconditional rules load (arithmetic, `wc -l` grounded)
| File | Before | After (lines) | Status |
|------|--------|---------------|--------|
| mediatr-patterns.md | ~1.1k | deleted | Task 02 |
| code-principles.md | ~3k → 1.2k | 44 | spike |
| component-change-governance.md | ~1.4k | 27 | Task 04 |
| bug-tracking.md | ~1.7k | 30 | Task 03 |
| constraints-registry.md | ~3.2k | 23 | Task 05 |
| **workflow.md** | ~13.9k | **671** | **pending (06–08)** |
| **testing.md** | ~11.6k | **724** | **pending (09–10)** |

The situational set + code-principles dropped from ~7.5k → ~2k unconditional (124 lines across 4 files), plus mediatr −1.1k. **The entire remaining reducible bulk is now workflow.md (671) + testing.md (724) = ~25.5k** unconditional, every session and every subagent. Confirms the per-agent sticky win is real and the big files hold the rest of it.

### Q2 — core-file split economics (from `skill-overlap-findings.md`)
GO on splitting workflow.md/testing.md **unconditional→routing-table** (GATE-A Q3 proved loading is unconditional, so a trivial-task agent that never needs the body still pays it). BUT the win is **delete redundant prose + extract to library**, NOT skill-substitution (core files reload per-agent anyway; skills conflict — see Conflicts #1–#2).

### Verdict: **GO to Tasks 06–10.** Direction (validated, right-facing):
- **testing.md (09–10):** ONE cohesive `testing-reference.md` + 2 rarely-used on-demand files (Stryker, FsCheck) = **3 files, not 6**. Preserve the `§ Regression tests` heading (inbound anchor). Forward-reference `maui-unit-testing`; do NOT flip `enabledPlugins` (Gotcha 3 — all enablement in Task 11). Reduced code snippets inline, full samples in library.
- **workflow.md (06–08):** anchor-heavy — preserve every referenced heading (`Rule 1`, `Rule 7`, `Bug Fix Pattern`, `Spike validation task pattern`, `Spec quality four-gate review`, `Sequential-only file registry`; audit the 3 orchestrator.md refs for pre-existing dangling links). Win is prose-deletion (J-Curve essay, discovery narrative, duplicated orchestrator/implementor pointers) + extraction; Rules 3–8 operational core stays inline.

## Post-implementation reconciliation (2026-07-07 — Task 17 record-correction, audit R6)

**Measured final state** (fresh-session `/context`, 2026-07-07): rules files total **11.0k tokens** (was ~18–22k per GATE-A) → **~8–11k per-agent sticky reduction**; memory category 29.2k → 20.8k; total cold start ~40.4k main-session vs 60,492 GATE-A probe (~20k off — note: not strictly like-for-like, main session vs 0-tool subagent).

**Corrections to the record:**
- The per-task "recovered" figures in BACKLOG rows 196–207 (~28k summed) were **estimates presented as measurements**; the per-line token assumption (~17–20 tok/line) was low vs actual (~29 tok/line), and never-miss HARD RULEs deliberately kept inline reduced achievable savings. Measured total stands at ~8–11k.
- The ~2–3k unconditional routing-table target was missed ~4× (workflow.md alone 5.1k) — a defensible safety trade-off, now recorded as such rather than as target-met.
- `tasks.md` Success Criteria #1/#4 corrected same date (were checked ✅ but false/incoherent).

**Like-for-like GATE-A post-probe (0-tool subagent): deliberately SKIPPED** — costs a ~40k+ throwaway cold-start for a number already derivable from `/context` + `wc -l`; token thrift takes precedence (same rationale as GATE-B's analytical decision). Run it only if Helder wants the ceremonial closing number.

---

### Budget-aware execution recommendation (2026-07-05)
Weekly budget at 86% (resets Jul 7). Recommend Tasks 06–10 run in the **fresh post-reset session**, where (a) full weekly budget covers workflow.md's risky 3-wave anchor-heavy refactor, and (b) Helder gets the free empirical confirmation of the Phase-1 drop via a fresh `/context` first (his stated plan). If executed before reset, do **testing.md first** (1 anchor, cleaner, bigger safe deletion) and leave workflow.md's 3 waves for full-budget headroom.
