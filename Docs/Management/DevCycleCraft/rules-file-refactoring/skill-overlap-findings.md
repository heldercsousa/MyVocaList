# Skill-Overlap Findings — testing.md & workflow.md vs. available skills

**Date:** 2026-07-05
**Author:** Claude (main agent), overnight autonomous session — flagged for Helder async review.
**Method:** Read the **actual bodies** of every skill that could plausibly overlap `testing.md`/`workflow.md` from the on-disk plugin cache (disabled skills still exist on disk). Claims below are grounded in the skill files themselves, not their ~140-token descriptions.

**Purpose:** Pre-answer GATE-B question 2 ("is a meaningful fraction of the core-file body *not* needed / better sourced from a skill?") for `workflow.md` + `testing.md`, and correct two latent errors in the existing plan before Tasks 09–10 are executed.

**Skills read (primary sources):**
- `superpowers:test-driven-development` (6.1.1)
- `superpowers:brainstorming` (6.1.1)
- `superpowers:writing-plans` (6.1.1)
- `superpowers:subagent-driven-development` (6.1.1)
- project `maui-unit-testing` (disabled)
- `dotnet-skills:testcontainers` (1.4.1)
- `dotnet-skills:snapshot-testing` (1.4.1)

---

## Headline corrections to earlier assumptions

1. **`dotnet-skills` testing entries do NOT fit this project.** `testcontainers` is Docker-container integration testing (SQL Server / Postgres images, `.StartAsync()`); this project deliberately uses a **real SQLite temp file, no containers** (testing.md documents *why* — SQLite LIKE/collation quirks the in-memory provider can't replicate). `snapshot-testing` is Verify (HTML emails / API surfaces / serialized output) — not this project's Moq+tuple-return model. → **Drop `dotnet-skills` from the testing-refactor plan.** The premise "dotnet-skills has powerful testing expertise [we can reuse]" is true in general but false for the entries that matter here.

2. **The one clean testing win is the project's own `maui-unit-testing` skill** (currently disabled). It duplicates ~1–1.5k of `testing.md` almost verbatim: csproj + conditional `OutputType=Library`, the `CreateSut` Moq ViewModel pattern, the `dotnet test` command list, and "wrap `Shell.Current` behind `INavigationService`". It is project-authored → **no authority conflict.** Enabling it is the safe route for the generic testing *how-to*.

---

## Two concrete conflicts (invisible from skill descriptions — found by reading the bodies)

These reinforce Helder's 2026-07-04 decision (Task 11) to NOT re-enable `test-driven-development`/`code-review`, and add evidence for why.

### Conflict #1 — TDD absolutism vs. the project's risk-tiered TDD
`test-driven-development` states an **Iron Law**: `NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST`, "No exceptions" (only throwaway/generated/config, and only with the human's permission). The project's `testing.md` **TDD Level Guidance** says **Level C** (plumbing, DI registration, DTO records, trivial getters) has **"no mandatory test"**, and `bug-tracking.md` says a **Minor** bug's regression test is **optional**. Enabling the skill imports a stricter second master that overrides the project's own calibrated risk-tiering. → Confirms: keep TDD guidance as project rules only.

### Conflict #2 — "design for everything" vs. the ceremony decision table
`brainstorming` has a **HARD-GATE**: *"Do NOT write any code … until you have presented a design and the user has approved it. This applies to EVERY project regardless of perceived simplicity"* — explicitly including "a config change." The project's `workflow.md` ceremony decision table says **typo / cosmetic / single-file bug fix = no spec required**. Direct contradiction. → If `brainstorming` is re-enabled (Task 11 keeps it), CLAUDE.md's authority hierarchy must **explicitly state project rules win**, and expect the skill to nag toward designing trivial changes.

> Net: re-enabling skills is **not free**. Task 11's narrowing to `brainstorming` + `writing-plans` is correct; even those two need an explicit "project rules override skill defaults" note in CLAUDE.md (the hierarchy already asserts this — Task 12 should make it pointed).

---

## testing.md — evidence-based keep / route / delete / extract

Of ~11.6k tokens: **~4k genuinely skill-replaceable**, **~5k delete-or-move-to-library** (NOT skill-routable), **~3.5k must stay** as project core.

| Section | Covered by a skill? | Action |
|---|---|---|
| Red/Green/Refactor workflow | Yes — `test-driven-development` (more forceful) | Route (project rule keeps a 2-line pointer + the Level-C exception note) |
| `dotnet test` command list | Yes — `maui-unit-testing` (identical) | Route |
| csproj / GlobalUsings / OutputType full listings | Yes — `maui-unit-testing` | Move verbatim to library; leave one-line pointer (awareness-only) |
| ViewModel test pattern (CreateSut, assert props not bindings) | Yes — `maui-unit-testing` | Slim examples → library |
| Integration/Repository (real SQLite), `TestDbContextFactory` | **No** — testcontainers is Docker (mismatch) | **STAYS / library** (project-specific) |
| AC traceability (tags + matrix) | **No** | **STAYS** (project SDD core) |
| TDD Level Guidance A/B/C | **No** | **STAYS** (and see Conflict #1) |
| Stryker.NET (mutation testing) | **No** — not in any skill | **Extract to on-demand `library/`** (rarely run) |
| FsCheck (property-based testing) | **No** | **Extract to on-demand `library/`** (rarely used) |
| Tuple-return service idiom; MAUI/DX/SQLite anti-patterns | **No** | **STAYS** (project-specific) |

**Reduced-samples decision (Helder's question):** yes — replace exhaustive verbatim code dumps (full Venue service/VM/repo test classes, 6-line GlobalUsings, full csproj) with **reduced illustrative snippets in the rules file + full versions in the library file**, loaded on demand. Awareness that the pattern exists + a pointer is enough for the unconditional file.

**Stryker / FsCheck decision (Helder's question):** yes — each becomes its **own on-demand library file** (`library/mutation-testing-stryker.md`, `library/property-based-testing-fscheck.md`), not inline. Low usage + zero skill coverage = pure unconditional overhead today.

---

## testing.md — CORRECTION to the existing plan (Tasks 09–10)

`tasks.md` Task 09–10 currently specify a **6-file split** for testing.md
(`test-driven-development-levels.md`, `acceptance-criteria-format.md`, `unit-test-patterns.md`, `integration-test-patterns.md`, `testing-anti-patterns.md`, `test-naming-conventions.md`).

**This violates the spike's own over-fragmentation guard (pilot-findings.md #5)** — the exact rule that Task 05 was already corrected to honor ("one cohesive library file per rules file unless a section has independent inbound references or exceeds ~2 pages").

**Recommended correction (pending Helder confirm at GATE-B):**
- **One** cohesive `library/testing-reference.md` (`##` sections: Test Project Setup · Unit/Service · ViewModel · Integration/SQLite · Naming · Anti-Patterns · Quality Audit), **plus**
- Two genuinely-separable on-demand files that are rarely needed: `mutation-testing-stryker.md`, `property-based-testing-fscheck.md`.

Total: **3 library files**, not 6.

---

## workflow.md — evidence-based verdict (GATE-B input)

Skill overlap is concentrated in **Rules 1–2 only**:
- Rule 1 (Spec-First) process narrative overlaps `brainstorming`.
- Rule 2 (Subagent Delegation) overlaps `subagent-driven-development` (fresh-subagent-per-task, model selection, file handoffs, progress ledger) — **and also overlaps the project's own `orchestrator.md`/`implementor.md`**, so enabling the skill creates a *third* source, not a clean replacement.

**Rules 3–8 are in no skill** — commit gates, tasks.md `[P]`/`[~]` markers, DRY-Onion waves, task-log format, research-tool gate, **session-start reading order + lease reclaim**, GitHub collision check. These are project-operational and must stay hot (a skill can't carry session-start guidance — by the time it would load, session-start decisions are already made).

**Conclusion for workflow.md:** the recoverable tokens (~6–7k of 13.9k) come almost entirely from **deleting redundant prose** (ROI J-Curve essay, discovery-mode narrative, paragraphs that merely re-point to `orchestrator.md`/`implementor.md`) and **extracting reference detail to library files** — **not** from skill-routing. This is consistent with GATE-A's finding that core-file skill-routing nets ≈zero. The Tasks 06–08 split remains worthwhile as **unconditional→routing-table**, but its value is prose-deletion + extraction, not skill substitution.

---

## Bottom line for GATE-B

- GATE-A already measured the sticky per-agent win (~16–19k recovered by reducing rules bodies). GATE-B's question 2 for the core files is answered here: **split them (real per-agent saving for the common trivial-task case), but do NOT skill-substitute them** — delete/extract instead.
- **Enable `maui-unit-testing`** (project skill) as part of Task 09–10; **drop `dotnet-skills`** from the testing plan.
- **Correct Tasks 09–10 to 3 library files, not 6.**
- Keep Task 11's narrowing (`brainstorming` + `writing-plans` only); Task 12 must add a pointed "project rules override skill defaults" line to CLAUDE.md to neutralize Conflicts #1–#2.
