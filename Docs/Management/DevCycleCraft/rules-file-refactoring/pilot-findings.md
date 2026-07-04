# Phase 0 Spike — Findings

**Date:** 2026-07-04
**Pilot file:** `.claude/rules/code-principles.md`
**Time-box:** 90 min — completed well within box.
**Question:** Does extracting sections from `code-principles.md` → library file + routing table + skill invocation work end-to-end without workflow changes?

**Verdict: PASS.** The routing-table + `.claude/library/` extraction pattern works with **zero content loss** and **zero agent-workflow change**. Four concrete gotchas were discovered that materially change how Tasks 01–12 must be executed — captured below.

---

## What was done

| File | Before | After | Change |
|------|--------|-------|--------|
| `.claude/rules/code-principles.md` | 268 lines (~3k tok, unconditionally loaded) | 44 lines (~0.5k tok) | Rewritten as routing table |
| `.claude/library/code-style-reference.md` | — | 263 lines (on-demand only) | NEW — full detail moved here verbatim |
| `~/.claude/skills/myvocalist-coding/SKILL.md` | 7 mapping rows | 8 mapping rows | Added row for code-style-reference.md |

**Net unconditional-load reduction for this file: ~2.5k tokens** (268→44 lines of always-loaded memory; the 263-line library file loads only when the `myvocalist-coding` skill routes to it). Aligns with the ~2k BACKLOG estimate.

**Content integrity affidavit:** All 11 sections of the original file are accounted for — 9 moved verbatim to `code-style-reference.md`; 2 (`Architecture Constraints`, `EF Core / SQLite`) were already thin pointers and are kept as routing entries. No line was dropped or reworded. Verified by section-by-section diff.

---

## GOTCHA 1 (HIGH) — Inbound `§ Section` anchors must be preserved or links break

Other files link to `code-principles.md` **by section anchor**, not just by filename:

| Referencing file | Anchor it depends on |
|------------------|----------------------|
| `CLAUDE.md` (Constitutional Role + Constraints) | `§ Architecture Constraints`, `§ C# Style / Naming`, `§ DI Registration Conventions`, `§ Exception Handling` |
| `.claude/rules/constraints-registry.md` | `§ UI Thread Performance — ObservableRangeCollection` |
| `.claude/library/dialogs-validation.md` | `§ Service Return Patterns` |

**Mitigation applied:** the routing table **keeps those exact headings** as one-line stubs pointing to the library file. Inbound `§` references still resolve.

**Rule for Tasks 01–12:** Before rewriting any rules file, `grep -rn "<filename>.md" .claude CLAUDE.md Docs` to enumerate inbound `§` anchors, and preserve every referenced heading in the routing table. This is now a mandatory pre-step per file. (Task 05 `constraints-registry.md` and Tasks 06–08 `workflow.md` have the most inbound references — budget for it.)

## GOTCHA 2 (MEDIUM) — New library files must be registered in the `myvocalist-coding` skill map

The `myvocalist-coding` skill (`~/.claude/skills/myvocalist-coding/SKILL.md`) is the primary discovery mechanism — it maps task types → library files. A newly extracted library file is **not discoverable via the skill** until a row is added to that table. The routing table in the rules file is the *second* discovery path; both should exist.

**Rule for Tasks 01–12:** every task that creates a `.claude/library/*.md` file must add a corresponding row to the `myvocalist-coding` skill map in the same commit. (This skill lives at **user level**, outside the repo — it is not `.sln`-registered and not covered by the repo commit; note it in the task-log as an out-of-repo edit.)

## GOTCHA 3 (MEDIUM) — `dotnet-skills`/superpowers enablement is deferred, NOT part of the spike

Spike success criterion #3 named "invoke `dotnet-skills` and confirm it loads." Deliberately **not flipping** `enabledPlugins` during the spike, because:
1. `settings.json` plugin flips have **session-wide blast radius** — other concurrent terminals/sessions inherit the change mid-work (see BACKLOG "Enforce Git Worktrees" concurrency concern).
2. A newly enabled plugin **does not load until session restart**, so it cannot be verified in-session anyway.
3. The discovery route that actually matters for `code-principles.md` is `myvocalist-coding` → library file, which needs **no plugin enablement** and is validated here.

**Consequence:** the `dotnet-skills:*` pointers written into the new routing table are forward-references. All plugin enablement (dotnet-skills + the 4 superpowers) is consolidated into **Task 11**, done once, verified after a clean restart. The per-file tasks (01–10) must not flip `enabledPlugins`.

## GOTCHA 4 (LOW) — `.claude/library` and `.claude/rules` files are NOT `.sln`-registered

Confirmed: zero `.claude/library/*` or `.claude/rules/*` entries exist in `MyVocaList.sln` (the `.sln` HARD GATE is applied to `Docs/` files in practice, not `.claude/` subfiles). Therefore new library files created by this refactor do **not** need `.sln` entries. Only artifacts under `Docs/` (like this `pilot-findings.md`) do.

---

## Impact on the plan (recommended adjustments)

1. **Add a mandatory pre-step to every task:** "grep inbound `§` anchors → preserve referenced headings." (Gotcha 1)
2. **Add to every extraction task's exit checklist:** "register new library file in `myvocalist-coding` skill map." (Gotcha 2)
3. **Confirm Tasks 01–10 never touch `enabledPlugins`;** all enablement lives in Task 11. (Gotcha 3)
4. **Task 01 is effectively subsumed** — this spike already produced the finished `code-principles.md` routing table + `code-style-reference.md`, not a throwaway. Task 01 becomes a *verification/finalize* pass (confirm the DI section split into a separate file is unnecessary — DI is small and reads fine inside `code-style-reference.md`; recommend **not** creating `ddi-registration-conventions.md`, contrary to the tasks.md draft, to avoid over-fragmentation).
5. **Over-fragmentation guard:** tasks.md proposes many tiny library files (e.g. separate DI file, 3 separate constraint files). Spike recommendation: **one cohesive library file per rules file** unless a section exceeds ~2 pages or has independent inbound references. Fewer, well-indexed files are more discoverable than many stubs.

## Discoverability validation (AC-1 / failure-criterion check)

- ✅ Agent route A: `myvocalist-coding` skill → new mapping row → `code-style-reference.md`. Works, no config.
- ✅ Agent route B: reads `code-principles.md` routing table → follows pointer to `code-style-reference.md § X`. Works, no config.
- ✅ No special configuration required; no workflow step changed.
- ✅ Failure criteria (undiscoverable files / skill needs config / unrecoverable content loss) — **none triggered.**

## Helder gate

Per plan.md handoff table, **"Spike findings valid"** is a Helder async-review gate. This file is the review artifact. Recommend Helder confirm the 5 plan adjustments above (esp. #4 over-fragmentation stance) before Tasks 02–10 proceed, since they change the target library-file count.
