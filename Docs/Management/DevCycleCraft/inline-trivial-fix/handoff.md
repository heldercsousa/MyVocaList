# Inline Trivial Fix (ITF) Lane — Handoff

> **Read this first if you are resuming ITF work.** Everything is shipped except one gate: Helder has not yet observed a live ITF fix end-to-end. That gate is *opportunistic* — it waits for a genuinely qualifying fix to appear in normal work. Do not manufacture a fix to close it.

**Status:** 🟡 In Progress · rules amended, Guard 3 merged, 38/38 tests green on develop (2026-07-21).
**BACKLOG row:** Dev Cycle Craft / 2026-07-12 — *Inline Trivial Fix (ITF) lane*.

## What is already done — do not redo

| Item | Commit |
|------|--------|
| Spec (requirements + design + proposed-diffs), spec-reviewer PASS | `9c1dc26` |
| Rule amendments: `workflow.md` Rules 2 & 3, `orchestrator.md`, `.gitignore`, changelog, BACKLOG, `.sln` | `6e0323b` |
| Guard 3 + 33 tests + task-log; verifier CONDITIONAL PASS, all findings resolved | `4b35048`, merged `34a8114` |
| Spec fix — C5 enforcement column (verifier W3) | `e3161ef` |
| `.sln` registration + BACKLOG gate advanced | `548db36` |
| Migrations registry corrected (`*Migration.cs` → `Migrations/` folder) | `738d0ab` |
| `worktree-base-check` MISBASED/BEHIND split + `TaskCreated` wiring, 38/38 green | `af9e752`, merged `a4c198a` |

Helder reviewed and approved the spec **and** the amended rule text on 2026-07-21 (authorship gate satisfied — `CLAUDE.md § Continuous Enhancement`).

## The one remaining gate

**Helder observes the first live ITF fix end-to-end.** Until then the BACKLOG row stays 🟡.

### How to recognise a qualifying fix

All of C0–C8 must hold (`requirements.md § Eligibility conditions`). The practical filter, in order:

1. Is the root cause, exact file, and exact line **already recorded** — in a subagent report, a task-log, or a BACKLOG row — before anyone opens the file? If you'd have to grep, it does not qualify (C2).
2. Does `bug-tracking.md` mandate a regression test? If yes → **dispatch, not ITF** (C6). This excludes every Critical bug and every Major bug that is testable at Service/ViewModel/Repository level.
3. 1 file, ≤ 5 changed lines, not `.xaml`/`.xaml.cs`, not a governed component, not in the sequential-only registry.

**Realistic candidates:** a log-message typo, a wrong constant, a missing null guard already covered by an existing test, a Minor cosmetic bug in a non-governed file, a UI-only Major bug whose verification is a documented manual E2E.

**Not candidates:** BUG-050/051/052 (Critical/Major, test-bearing), anything touching a governed component, anything needing exploration.

### How to run it when one appears

1. Confirm the diagnosis is already recorded. Do not open the file to find the bug.
2. Enter a worktree on a task branch based on develop (C7 — Guard 1 blocks develop/main regardless).
3. Write `<worktree>/.itf-active`:
   ```json
   {"id":"BUG-NNN","file":"relative/path/File.cs","expected_lines":1,"declared_at":"<UTC ISO-8601>"}
   ```
   `expected_lines` is audit-only — Guard 3 does not read it.
4. Log one line in the feature's `task-log.md`: `ITF: BUG-NNN — File.cs — root cause: <one sentence> — expected N lines.`
5. Read **only** that file. Apply the fix.
6. Build (0 errors) + affected tests green (C8).
7. Commit with the Bug Fix Pattern message **plus** `Lane: ITF (1 file, N lines)`.
8. **Delete the marker** as the final step (lifecycle state 3 — no mechanical backstop; a 30-minute expiry is only the dead-session safety net).
9. Show Helder the run: the declaration, the guard permitting the edit, the commit, the deleted marker.

If the fix turns out to need a second file or > 5 lines mid-way: delete the marker, dispatch an implementor, and note the misclassification in the task-log — that data feeds the calibration review.

### Then

Flip the BACKLOG row to ✅ and archive it per the rotation rule. Add a changelog line recording the first live use.

## Calibration review — the follow-up after this gate

After ~20 commits carrying `Lane: ITF`, run `git log --grep "Lane: ITF"` and review with Helder:
- Observed line-count distribution → does C1's 5-line bound tighten or loosen?
- Did C6 exclude fixes that should have qualified? Rev 2 deliberately excluded essentially all test-bearing bugs; if the lane turns out too narrow to be worth having, the candidate widening is "1 production file + 1 test file, ≤ 5 lines each" — **not yet specced, not approved**.
- Any evidence of multi-declaration chaining (prose violation, visible as multiple `ITF:` task-log lines against one commit).

## Known gaps carried forward (all deliberate, all documented)

| Gap | Where recorded |
|-----|----------------|
| Lane is opt-in — undeclared inline editing is prose-enforced only, as before | `requirements.md § Enforcement model` |
| Multi-declaration chaining is not mechanically blocked | same |
| Marker deletion has no backstop inside the 30-min window | `requirements.md § Declaration lifecycle` |
| `Directory.Build.props` / `tasks.md` in C5 are not hook-reachable (behind `_CODE_SUFFIXES`) | `requirements.md § Note on C5` |
| Path comparison is case-insensitive (Windows assumption) | comment at the comparison site in `constitutional-guard.py` |
| C4 matches component name-stems, not paths — over-blocks a same-named file elsewhere | `design.md`, verifier verdict in `task-log.md` |

## Unrelated finding needing Helder's decision

Four `agent-*` worktrees are genuinely **misbased** (forked from main, ~378 commits behind), each holding **one unmerged commit**:

| Worktree | Commit |
|----------|--------|
| `agent-a284a1b6d18225d42` | `6a525e4a docs(form-validation): add plan for form-validation guide (task 01)` |
| `agent-a78dcb730dbd16e4a` | `69393654 feat: suggestion DTOs (Contracts) — form-ux-redesign` |
| `agent-aaae95a5d8a6c56ec` | `c1814652 docs(validation): establish canonical form-validation standard in .claude/library` |
| `agent-aabbb9b1bfc037fad` | `f65a8d5b feat: collation batch name/title lookups (Infra) — form-ux-redesign` |

Not deleted — each may contain work not represented on develop. Someone must check whether each commit's content already landed via another path, then cherry-pick or discard. **Do not remove these worktrees without that check.**
